using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.Performance;

/// <summary>Retains original connections/leases for explicit scalar controls or observed candidate reads, with no external logging.</summary>
internal sealed class IsolatedHomeReadFactory(IDbContextFactory<ZananceDbContext> original, TimeProvider time, bool useDerivedTransport = false) : IDbContextFactory<ZananceDbContext>, IDisposable
{
    private readonly List<(ZananceDbContext Captured, ZananceDbContext Reader)> captures = [];
    private readonly ScalarObserver observer = new();
    private readonly ReadAudit audit = new();
    private ILoggerFactory? logs;
    private readonly Lock gate = new();
    private bool disposed;

    /// <summary>Gets complete scalar commands actually executed, not inferred from a requested mode.</summary>
    internal int CompleteScalarReads => audit.CompleteReads;
    /// <summary>Gets any unexpected optional transport read in the independent control.</summary>
    internal int DerivedTransportReads => audit.DerivedReads;
    /// <summary>Gets actual completed payload-reader commands in candidate mode.</summary>
    internal int DerivedBlockReads => audit.BlockReads;

    /// <inheritdoc />
    public ZananceDbContext CreateDbContext() => Wrap(original.CreateDbContext());
    /// <inheritdoc />
    public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
        Wrap(await original.CreateDbContextAsync(cancellationToken).ConfigureAwait(false));

    private ZananceDbContext Wrap(ZananceDbContext captured)
    {
        try
        {
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(disposed, this);
                // Retain the opaque original options and registered protection checks. Never extract a key,
                // create a foreign connection or release the actual factory lease before its readers finish.
                var options = captured.GetService<IDbContextOptions>() as DbContextOptions<ZananceDbContext>
                    ?? throw new InvalidOperationException("Original typed context options are required.");
                logs ??= LoggerFactory.Create(logging => logging.SetMinimumLevel(LogLevel.Information).AddProvider(audit));
                var builder = new DbContextOptionsBuilder<ZananceDbContext>(options).UseLoggerFactory(logs);
                if (!useDerivedTransport) builder.AddInterceptors(observer);
                var reader = new ZananceDbContext(builder.Options, time);
                captures.Add((captured, reader)); return reader;
            }
        }
        catch { captured.Dispose(); throw; }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            foreach (var capture in captures)
            {
                try { _ = capture.Reader.Database; }
                catch (ObjectDisposedException) { continue; }
                throw new InvalidOperationException("Finish every independent scalar reader before releasing its original factory lease.");
            }
            disposed = true;
            foreach (var capture in captures) capture.Captured.Dispose();
            captures.Clear();
            logs?.Dispose();
        }
    }

    private sealed class ScalarObserver : DbCommandInterceptor
    {
        public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
        { DemandReadOnly(command); return result; }
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        { cancellationToken.ThrowIfCancellationRequested(); DemandReadOnly(command); return ValueTask.FromResult(result); }
        private static void DemandReadOnly(DbCommand command)
        {
            if (command.CommandText.Trim() != "PRAGMA cache_size = -32768")
                throw new InvalidOperationException("Independent scalar controls cannot execute financial or schema writes.");
        }
    }

    private sealed class ReadAudit : ILoggerProvider, ILogger
    {
        internal int CompleteReads;
        internal int DerivedReads;
        internal int BlockReads;
        public ILogger CreateLogger(string categoryName) => this;
        public bool IsEnabled(LogLevel logLevel) => true;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (eventId.Id != RelationalEventId.CommandExecuted.Id) return;
            var sql = formatter(state, exception); // Observe only; never persist SQL, parameters or source values.
            if (sql.Contains("FROM \"Entries\" NOT INDEXED ORDER BY rowid DESC", StringComparison.Ordinal)) Interlocked.Increment(ref CompleteReads);
            if (sql.Contains("HomeLedgerRead", StringComparison.Ordinal) || sql.Contains("HomeLedgerDirtyBlocks", StringComparison.Ordinal)) Interlocked.Increment(ref DerivedReads);
            if (sql.Contains("FROM HomeLedgerReadBlocks ORDER BY BlockId DESC", StringComparison.Ordinal)) Interlocked.Increment(ref BlockReads);
        }
        public void Dispose() { }
    }
}
