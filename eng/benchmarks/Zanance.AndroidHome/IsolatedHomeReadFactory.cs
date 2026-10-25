using System.Data.Common;
using System.Diagnostics;
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
    private readonly ReadEvents readEvents = new();
    private ILoggerFactory? logs;
    private readonly Lock gate = new();
    private bool disposed;

    /// <summary>Gets complete scalar commands actually executed, not inferred from a requested mode.</summary>
    internal int CompleteScalarReads => audit.CompleteReads;
    /// <summary>Gets any unexpected optional transport read in the independent control.</summary>
    internal int DerivedTransportReads => audit.DerivedReads;
    /// <summary>Gets actual completed payload-reader commands in candidate mode.</summary>
    internal int DerivedBlockReads => audit.BlockReads;

    /// <summary>Copies bounded numeric events from owned readers only; SQL, source values, paths and keys are never retained.</summary>
    internal ReadEventTrace CaptureReadEvents() => readEvents.Capture();

    /// <summary>One monotonic observer-arrival mark, not a claim about the cause of an unobserved interval.</summary>
    internal readonly record struct ReadEvent(int Context, string Phase, string Kind, double Milliseconds);

    /// <summary>Separates bounded diagnostic observations from original data or financial controls.</summary>
    internal sealed record ReadEventTrace(int OwnedContexts, int DroppedEvents, long FirstTimestampTicks, ReadEvent[] Events);

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
                readEvents.Own(reader);
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
            readEvents.Dispose();
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

    /// <summary>Observes original EF events without adding an interceptor or changing transport eligibility.</summary>
    private sealed class ReadEvents : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>, IDisposable
    {
        private const int MaximumEvents = 512;
        private readonly Lock gate = new();
        private readonly Dictionary<DbContext, int> contexts = new(ReferenceEqualityComparer.Instance);
        private readonly List<ReadEvent> events = [];
        private readonly List<IDisposable> sources = [];
        private readonly IDisposable all;
        private long first;
        private int dropped;
        private bool disposed;

        internal ReadEvents() => all = DiagnosticListener.AllListeners.Subscribe(this);

        internal void Own(DbContext context)
        {
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(disposed, this);
                contexts.Add(context, contexts.Count + 1);
            }
        }

        internal ReadEventTrace Capture()
        {
            lock (gate) return new(contexts.Count, dropped, first, events.ToArray());
        }

        void IObserver<DiagnosticListener>.OnNext(DiagnosticListener listener)
        {
            if (listener.Name != DbLoggerCategory.Name) return;
            lock (gate)
                if (!disposed) sources.Add(listener.Subscribe(this, name => Phase(name) is not null));
        }

        void IObserver<KeyValuePair<string, object?>>.OnNext(KeyValuePair<string, object?> item)
        {
            if (Phase(item.Key) is not { } phase || item.Value is not DbContextEventData { Context: { } context }) return;
            lock (gate)
            {
                if (disposed || !contexts.TryGetValue(context, out var number)) return;
                if (events.Count == MaximumEvents) { dropped++; return; }
                var stamp = Stopwatch.GetTimestamp();
                if (events.Count == 0) first = stamp;
                events.Add(new(number, phase, item.Value is CommandEventData command ? QueryKind(command.Command.CommandText) : "connection",
                    Stopwatch.GetElapsedTime(first, stamp).TotalMilliseconds));
            }
        }

        /// <summary>Emits only fixed query-family labels, never SQL, parameters, names or values.</summary>
        private static string QueryKind(string sql)
        {
            // EF's outer FROM begins at column zero; nested subqueries are indented. Unknown SQL stays unclassified.
            if (!sql.StartsWith("SELECT ", StringComparison.Ordinal)) return "other";
            var start = sql.IndexOf("\nFROM \"", StringComparison.Ordinal);
            if (start < 0) return "other";
            var table = sql.AsSpan(start + 7);
            var end = table.IndexOf('"');
            if (end < 0) return "other";
            return table[..end] switch
            {
                "Accounts" => "accounts",
                "Categories" => "categories",
                "Settings" => "settings",
                "Schedules" => "plans",
                "OccurrenceStates" => "plan-states",
                "Goals" => "goals",
                "GoalContributionPlans" => "goal-plans",
                "GoalAllocations" => "goal-allocations",
                "Budgets" => "budgets",
                "ExchangeRates" => "rates",
                "AssetTypes" => "holding-types",
                "AssetEvents" => "holding-events",
                "QuickTemplates" => "templates",
                "SavedFilters" => "filters",
                _ => "other"
            };
        }

        // These observers never format event payloads, exceptions, SQL or connection strings.
        public void OnCompleted() { }
        public void OnError(Exception error) { }

        private static string? Phase(string name) => name switch
        {
            "Microsoft.EntityFrameworkCore.Database.Connection.ConnectionOpening" => "connection-opening",
            "Microsoft.EntityFrameworkCore.Database.Connection.ConnectionOpened" => "connection-opened",
            "Microsoft.EntityFrameworkCore.Database.Connection.ConnectionClosing" => "connection-closing",
            "Microsoft.EntityFrameworkCore.Database.Connection.ConnectionClosed" => "connection-closed",
            "Microsoft.EntityFrameworkCore.Database.Connection.ConnectionError" => "connection-error",
            "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuting" => "command-executing",
            "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuted" => "command-executed",
            "Microsoft.EntityFrameworkCore.Database.Command.CommandError" => "command-error",
            _ => null
        };

        public void Dispose()
        {
            IDisposable[] detached;
            lock (gate)
            {
                if (disposed) return;
                disposed = true;
                detached = sources.ToArray();
                sources.Clear(); contexts.Clear();
            }
            // Listener registration has its own lock; detach outside ours to avoid lock-order inversion.
            all.Dispose();
            foreach (var source in detached) source.Dispose();
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
