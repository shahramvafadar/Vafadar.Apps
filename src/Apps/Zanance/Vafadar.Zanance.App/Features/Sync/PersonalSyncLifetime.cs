using Vafadar.Data;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Sync;

/// <summary>Native app lifetime only; construction performs no key, financial database or network work.</summary>
public sealed class PersonalSyncLifetime : IDisposable
{
    private readonly AppLockService appLock;
    private readonly ProfileService profiles;
    private readonly LocalDatabaseLocation<ZananceDbContext> location;
    private readonly object state = new();
    private CancellationTokenSource current = new();
    private long epoch;
    private bool foreground;
    /// <summary>Raised after access changes so an opted-in foreground worker can recheck, never bypass a lock.</summary>
    public event EventHandler? Changed;
    /// <summary>Attaches only to original native access/catalogue notifications.</summary>
    public PersonalSyncLifetime(AppLockService appLock, ProfileService profiles, LocalDatabaseLocation<ZananceDbContext> location)
    {
        this.appLock = appLock; this.profiles = profiles; this.location = location;
        appLock.AccessChanged += OnAccessChanged; profiles.Changed += OnAccessChanged;
    }
    /// <summary>Marks the app's original foreground transition, independently of whether its optional PIN is enabled.</summary>
    public void Resume() { Change(true); }
    /// <summary>Cancels financial network work as soon as the original app leaves the foreground.</summary>
    public void Suspend() { Change(false); }
    /// <summary>Retires pending automatic publication before navigation can expose a mutable draft.</summary>
    public void RetireNavigation() { Change(null); }
    private void OnAccessChanged(object? sender, EventArgs e) => Change(null);
    private void Change(bool? active)
    {
        CancellationTokenSource previous;
        lock (state)
        {
            if (active is { } next) foreground = next;
            epoch++; previous = current; current = new();
        }
        // Native callbacks never wait for HTTP or a database lease. Existing callers own their linked tokens.
        previous.Cancel(); previous.Dispose(); Changed?.Invoke(this, EventArgs.Empty);
    }
    /// <summary>Captures current actual device catalogue and location without opening a financial connection.</summary>
    public PersonalSyncAccess Capture()
    {
        long captured; CancellationToken cancellation;
        lock (state)
        {
            if (!PersonalSyncBuild.IsAvailable || !foreground || appLock.IsLocked)
                throw new OperationCanceledException("Personal sync is unavailable outside unlocked foreground access.");
            captured = epoch; cancellation = current.Token;
        }
        var catalogue = profiles.CaptureStorage(); var path = location.Path;
        void Validate()
        {
            cancellation.ThrowIfCancellationRequested();
            lock (state)
                if (captured != epoch || !foreground || appLock.IsLocked || path != location.Path)
                    throw new OperationCanceledException("Captured personal access was retired.");
            catalogue.Validate();
        }
        Validate(); return new(Validate, cancellation, path);
    }
    /// <inheritdoc />
    public void Dispose()
    {
        appLock.AccessChanged -= OnAccessChanged; profiles.Changed -= OnAccessChanged;
        lock (state) { foreground = false; epoch++; current.Cancel(); current.Dispose(); }
    }
}
