using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Vafadar.Zanance.Api.Analysis;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>Checks dispatch worker recovery without a database or a provider connection.</summary>
[Trait("Qualification", "AiLocal")]
public sealed class AiPilotDispatchWorkerTests
{
    [Fact]
    public async Task Worker_keeps_running_after_a_transient_database_update_failure()
    {
        var ct = TestContext.Current.CancellationToken;
        var database = new TemporarilyUnavailableDatabase();
        var configuration = new ConfigurationBuilder().Build();
        var time = TimeProvider.System;
        var pilot = new AiPilotProvider(configuration, new RefusingProviderFactory(), time);
        var store = new AiRequestStore(database, new UnconfiguredAiProvider(), new EphemeralDataProtectionProvider(), time, pilot);
        var notices = new AiPilotCompletionNotices(configuration, database, store, time);
        using var worker = new AiPilotDispatchWorker(store, notices, NullLogger<AiPilotDispatchWorker>.Instance);
        await worker.StartAsync(ct);
        try
        {
            var stopped = worker.ExecuteTask ?? throw new InvalidOperationException("Dispatch worker did not start.");
            var nextTick = database.SecondAttempt.Task;
            Assert.Same(nextTick, await Task.WhenAny(nextTick, stopped).WaitAsync(TimeSpan.FromSeconds(25), ct));
        }
        finally { await worker.StopAsync(ct); }
    }

    private sealed class TemporarilyUnavailableDatabase : IDbContextFactory<ServiceDbContext>
    {
        internal TaskCompletionSource SecondAttempt { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int attempts;

        public ServiceDbContext CreateDbContext()
        {
            if (Interlocked.Increment(ref attempts) == 1)
                throw new DbUpdateException("Fictional database update failure.",
                    new PostgresException("Fictional serialization failure.", "ERROR", "ERROR", PostgresErrorCodes.SerializationFailure));
            SecondAttempt.TrySetResult();
            throw new TimeoutException("No database is used by this fixture.");
        }
    }

    private sealed class RefusingProviderFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => throw new InvalidOperationException("Provider access is outside this fixture.");
    }
}
