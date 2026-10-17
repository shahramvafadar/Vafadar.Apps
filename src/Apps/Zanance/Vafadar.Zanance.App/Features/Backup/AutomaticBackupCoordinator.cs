using System.Collections.Concurrent;
using Vafadar.Backup;
using Vafadar.Core.Settings;

namespace Vafadar.Zanance.App.Features.Backup;

/// <summary>Coalesces foreground checks for device-only recovery points without relying on background execution.</summary>
public sealed class AutomaticBackupCoordinator(IAutomaticLocalBackupService backup, ISettingsStore settings,
    BackupOptions options, IBackupStorage storage, Func<Action> capture, Func<CancellationToken, Task<bool>> available, Action<Action> publish)
{
    private const string IntervalKey = "backup.automaticIntervalDays";
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly ConcurrentDictionary<string, bool> failures = new(StringComparer.Ordinal);

    /// <summary>Notifies the visible status surface through the supplied UI publication port.</summary>
    public event EventHandler? Changed;

    /// <summary>Gets or sets the device-only interval: zero disables, one is daily, seven is weekly.</summary>
    public int IntervalDays
    {
        get => int.TryParse(settings.Get(IntervalKey), out var days) && days is 0 or 1 or 7
            ? days : options.AutomaticBackupInterval is null ? 0 : 1;
        set
        {
            if (value is not (0 or 1 or 7)) throw new ArgumentOutOfRangeException(nameof(value));
            settings.Set(IntervalKey, value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Notify();
        }
    }

    /// <summary>Gets the current profile's last completed automatic copy, independent of portable export.</summary>
    public DateTimeOffset? LastBackupAt => backup.LastAutomaticBackupAt;

    /// <summary>Gets whether the last attempt for the current set failed; exception details never become UI data.</summary>
    public bool HasFailure => failures.GetValueOrDefault(options.FileSet?.Invoke() ?? string.Empty);

    /// <summary>Checks once on foreground entry; overlapping requests coalesce and failed copies retry next time.</summary>
    public async Task RunAsync(CancellationToken ct = default)
    {
        if (!await gate.WaitAsync(0, ct).ConfigureAwait(false)) return;
        var set = string.Empty;
        try
        {
            set = options.FileSet?.Invoke() ?? string.Empty;
            // Native safety discovery owns maintenance briefly; asynchronous readiness waits rather than failing its synchronous read gate.
            if (!await available(ct).ConfigureAwait(false)) return;
            if (!string.Equals(set, options.FileSet?.Invoke() ?? string.Empty, StringComparison.Ordinal))
                throw new InvalidOperationException("Automatic backup readiness belongs to another profile set.");
            var days = IntervalDays;
            if (days == 0) return;
            var validate = capture();
            await backup.CreateWhenDueAsync(storage, TimeSpan.FromDays(days), () =>
            {
                validate();
                if (days != IntervalDays) throw new InvalidOperationException("Automatic backup preference changed.");
            }, ct).ConfigureAwait(false);
            // A fully validated not-due result also recovers an earlier transient failure without creating another copy.
            failures[set] = false;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception error) when (error is not OutOfMemoryException) { failures[set] = true; }
        finally { gate.Release(); Notify(); }
    }

    private void Notify() => publish(() => Changed?.Invoke(this, EventArgs.Empty));
}
