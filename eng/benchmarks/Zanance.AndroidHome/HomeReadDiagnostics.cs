using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Vafadar.Zanance.App;

/// <summary>Observes bounded EF timing events for one independent owned connection path, without changing commands.</summary>
internal sealed class HomeReadDiagnostics : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>, IDisposable
{
    private readonly string _path;
    private readonly object _gate = new();
    private readonly List<IDisposable> _subscriptions = [];
    private readonly List<ReadTiming> _events = [];
    private readonly IDisposable _listeners;
    private string _stage = "unmeasured";

    /// <summary>Subscribes only to execution, reader lifetime and connection-open events for the owned fixture.</summary>
    internal HomeReadDiagnostics(string path)
    {
        _path = Path.GetFullPath(path);
        _listeners = DiagnosticListener.AllListeners.Subscribe(this);
    }

    /// <summary>Names an explicit measured public operation; names never come from stored financial data.</summary>
    internal void Begin(string stage) { lock (_gate) _stage = stage; }

    /// <summary>Returns timing/count packets only; no SQL, parameters, paths, keys or financial values leave this observer.</summary>
    internal ReadTiming[] Capture() { lock (_gate) return [.. _events]; }

    /// <inheritdoc />
    public void OnNext(DiagnosticListener listener)
    {
        if (listener.Name == "Microsoft.EntityFrameworkCore")
            _subscriptions.Add(listener.Subscribe(this, name => name is
                "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuted" or
                "Microsoft.EntityFrameworkCore.Database.Command.DataReaderDisposing" or
                "Microsoft.EntityFrameworkCore.Database.Connection.ConnectionOpened"));
    }

    /// <inheritdoc />
    public void OnNext(KeyValuePair<string, object?> item)
    {
        string? path; string boundary; string table; double milliseconds; int? reads = null;
        switch (item.Value)
        {
            case DataReaderDisposingEventData reader:
                path = reader.Command.Connection?.DataSource; boundary = "reader";
                table = Table(reader.Command.CommandText); milliseconds = reader.Duration.TotalMilliseconds; reads = reader.ReadCount;
                break;
            case CommandExecutedEventData command:
                path = command.Command.Connection?.DataSource; boundary = "execute";
                table = Table(command.Command.CommandText); milliseconds = command.Duration.TotalMilliseconds;
                break;
            case ConnectionEndEventData connection:
                path = connection.Connection.DataSource; boundary = "open"; table = "connection";
                milliseconds = connection.Duration.TotalMilliseconds;
                break;
            default: return;
        }
        if (string.IsNullOrEmpty(path) || Path.GetFullPath(path) != _path) return;
        lock (_gate)
        {
            if (_events.Count >= 1000) throw new InvalidOperationException("Independent timing event bound exceeded.");
            _events.Add(new(_stage, boundary, table, milliseconds, reads));
        }
    }

    // Only fixed schema labels are published; the original SQL text is never retained or serialized.
    private static string Table(string sql)
    {
        foreach (var table in new[] { "Entries", "Accounts", "Schedules", "OccurrenceStates", "Goals", "Budgets",
            "Templates", "SavedFilters", "Categories", "Settings", "ResourceSelections", "AssetTypes", "AssetEvents" })
            if (sql.Contains("FROM \"" + table + "\"", StringComparison.Ordinal)) return table;
        return "other";
    }

    /// <inheritdoc />
    public void OnCompleted() { }
    /// <inheritdoc />
    public void OnError(Exception error) => throw new InvalidOperationException("Independent timing observation failed.", error);

    /// <summary>Stops observation before preservation exports or another fixture's work begins.</summary>
    public void Dispose()
    {
        _listeners.Dispose();
        foreach (var subscription in _subscriptions) subscription.Dispose();
    }

    /// <summary>Durations overlap; EF read calls are not row counts when the caller directly consumes DbDataReader.</summary>
    internal sealed record ReadTiming(string Stage, string Boundary, string Table, double Milliseconds, int? EfReadCalls);
}
