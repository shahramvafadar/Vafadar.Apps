using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Testing;

namespace Vafadar.Data.Tests;

/// <summary>Disposed protected factory objects must not become global service-cache roots.</summary>
public sealed class ProtectedSqliteServiceLifetimeTests
{
    /// <summary>The original live publisher and services may outlive a context without retaining its closed connection.</summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Protected_factory_disposal_releases_context_and_connection(bool asynchronous, bool maintenance)
    {
        using var directory = new TemporaryDirectory();
        var path = directory.Combine("never-opened.db");
        using var services = new ServiceCollection().AddLocalDatabase<TestDbContext>(path, protectionRequired: true).BuildServiceProvider();
        var location = services.GetRequiredService<LocalDatabaseLocation<TestDbContext>>();
        DatabaseAccessGate.ExclusiveLease? owner = await location.Access.AcquireExclusiveAsync(TestContext.Current.CancellationToken);
        try
        {
            // This non-secret marker satisfies only the closed creator contract; it is never used to open storage.
            location.MoveToProtected(path, () => new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = path, Mode = SqliteOpenMode.ReadWrite, Pooling = false, Password = nameof(ProtectedSqliteServiceLifetimeTests),
            }.ToString()), "4.19.0", owner);
            if (!maintenance) { owner.Dispose(); owner = null; }
            var observed = await CreateAndDisposeAsync(services.GetRequiredService<ILocalDbContextFactory<TestDbContext>>(), owner, asynchronous);
            await Task.Yield();
            // This bounded lifetime assertion is not a product collector policy or a timing benchmark.
            for (var attempt = 0; attempt < 3 && (observed.Context.IsAlive || observed.Connection.IsAlive); attempt++)
            {
                GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
                await Task.Yield();
            }
            Assert.False(observed.Context.IsAlive);
            Assert.False(observed.Connection.IsAlive);
            Assert.False(File.Exists(path));
            GC.KeepAlive(services);
            GC.KeepAlive(location);
        }
        finally { owner?.Dispose(); }
        using var finalOwner = location.Access.AcquireExclusive();
        finalOwner.Validate();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<Lifetime> CreateAndDisposeAsync(ILocalDbContextFactory<TestDbContext> factory,
        DatabaseAccessGate.ExclusiveLease? owner, bool asynchronous)
    {
        var context = asynchronous
            ? owner is null ? await factory.CreateDbContextAsync(TestContext.Current.CancellationToken)
                : await factory.CreateDbContextAsync(owner, TestContext.Current.CancellationToken)
            : owner is null ? factory.CreateDbContext() : factory.CreateDbContext(owner);
        try
        {
            var connection = context.Database.GetDbConnection();
            Assert.Equal(System.Data.ConnectionState.Closed, connection.State);
            _ = context.Model;
            return new(new WeakReference(context), new WeakReference(connection));
        }
        finally
        {
            if (asynchronous) await context.DisposeAsync();
            else context.Dispose();
        }
    }

    private sealed record Lifetime(WeakReference Context, WeakReference Connection);
}
