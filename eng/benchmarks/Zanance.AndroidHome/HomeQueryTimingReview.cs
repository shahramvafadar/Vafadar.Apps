#if HOME_QUERY_TIMING_REVIEW
using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Vafadar.Zanance.App;

/// <summary>Buffers bounded typed EF timing metadata around one original publication; never reads commands, parameters or source values.</summary>
internal sealed class HomeQueryTimingReview : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>, IDisposable
{
    private const int Limit = 8192;
    private const string CommandExecuted = "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuted";
    private const string ReaderDisposing = "Microsoft.EntityFrameworkCore.Database.Command.DataReaderDisposing";
    private const string CompilationStarting = "Microsoft.EntityFrameworkCore.Query.QueryCompilationStarting";
    private const string ExecutionPlanned = "Microsoft.EntityFrameworkCore.Query.QueryExecutionPlanned";
    private readonly object _gate = new();
    private readonly List<Timing> _events = [];
    private readonly Dictionary<Guid, int> _commands = [];
    private readonly List<IDisposable> _subscriptions = [];
    private readonly IDisposable _listeners;
    private readonly string _path;
    private volatile bool _active = true;
    private int _dropped;

    /// <summary>Requires the explicit emulator-only measure request after separate complete source controls.</summary>
    internal HomeQueryTimingReview(string root, string comparison)
    {
        var activity = Platform.CurrentActivity;
        if (activity?.PackageName != "pro.vafadar.zanance.androidhomereview"
            || Android.OS.Build.Hardware != "ranchu" || Stopwatch.Frequency != 1_000_000_000
            || activity.Intent?.GetStringExtra("review_phase") != "home-current-stages"
            || activity.Intent.GetStringExtra("review_current_cpu_oracle") != "measure"
            || !activity.Intent.GetBooleanExtra("review_current_cpu_window", false)
            || !activity.Intent.GetBooleanExtra("review_current_query_timing", false)
            || !Guid.TryParseExact(comparison, "N", out _))
            throw new InvalidDataException("Explicit original x64-emulator query-timing request required.");
        _path = Path.Combine(root, "query-timing-" + comparison + ".json");
        if (File.Exists(_path)) throw new InvalidDataException("Preserve the original timing observation; never replay it.");
        _listeners = DiagnosticListener.AllListeners.Subscribe(this);
    }

    /// <inheritdoc />
    public void OnNext(DiagnosticListener listener)
    {
        if (listener.Name != "Microsoft.EntityFrameworkCore") return;
        lock (_gate)
        {
            if (_active) _subscriptions.Add(listener.Subscribe(this, name => _active
                && name is CommandExecuted or ReaderDisposing or CompilationStarting or ExecutionPlanned));
        }
    }

    /// <inheritdoc />
    public void OnNext(KeyValuePair<string, object?> item)
    {
        var ticks = Stopwatch.GetTimestamp();
        if (!_active) return;
        lock (_gate)
        {
            if (!_active) return;
            if (_events.Count == Limit) { _dropped++; return; }
            // Read only these typed metadata fields. Never format an event payload or access its Command/Context/Reader.
            var (kind, command, duration) = (item.Key, item.Value) switch
            {
                (CommandExecuted, CommandExecutedEventData value) => ("command-executed", value.CommandId, (long?)value.Duration.Ticks),
                (ReaderDisposing, DataReaderDisposingEventData value) => ("reader-disposing", value.CommandId, (long?)value.Duration.Ticks),
                (CompilationStarting, _) => ("compilation-starting", Guid.Empty, null),
                (ExecutionPlanned, _) => ("execution-planned", Guid.Empty, null),
                _ => (string.Empty, Guid.Empty, null),
            };
            if (kind.Length == 0) return;
            var ordinal = 0;
            if (command != Guid.Empty && !_commands.TryGetValue(command, out ordinal))
                _commands.Add(command, ordinal = _commands.Count + 1);
            _events.Add(new(kind, ticks, Environment.CurrentManagedThreadId, ordinal, duration));
        }
    }

    /// <inheritdoc />
    public void OnCompleted() { }

    /// <inheritdoc />
    public void OnError(Exception error) { lock (_gate) _dropped++; }

    /// <summary>Stops observation before serializing fixed metadata outside the measured publication.</summary>
    public void Dispose()
    {
        lock (_gate)
        {
            if (!_active) return;
            _active = false;
        }
        _listeners.Dispose();
        foreach (var subscription in _subscriptions) subscription.Dispose();
        using var output = new FileStream(_path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        JsonSerializer.Serialize(output, new { StopwatchFrequency = Stopwatch.Frequency, DurationFrequency = TimeSpan.TicksPerSecond,
            Dropped = _dropped, Events = _events, NoSqlParametersPathsKeysOrPayloadStrings = true,
            InstrumentedDiagnosticOnly = true, ExclusiveCauseOrSpeedupAccepted = false });
    }

    /// <summary>Contains only fixed event kind, monotonic metadata and an observation-local ordinal.</summary>
    private sealed record Timing(string Kind, long Ticks, int Thread, int Command, long? DurationTicks);
}
#endif
