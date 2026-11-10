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
    private bool disposed;
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
            // D-1038: late or reentrant native notifications cannot resurrect terminal access.
            if (disposed) return;
            if (active is { } next) foreground = next;
            epoch++; previous = current; current = new();
        }
        // Native callbacks never wait for HTTP or a database lease. Existing callers own their linked tokens.
        // D-1037: a failing registered callback still retires its native cancellation resources.
        try { previous.Cancel(); }
        finally { previous.Dispose(); }
        Changed?.Invoke(this, EventArgs.Empty);
    }
    /// <summary>Captures current actual device catalogue and location without opening a financial connection.</summary>
    public PersonalSyncAccess Capture() => CaptureCore(PersonalSyncBuild.IsAvailable);
    /// <summary>Captures the same native lock/foreground/catalogue boundary for the separately selected billing edition.</summary>
    internal PersonalSyncAccess CaptureBilling() => CaptureCore(Commerce.Billing.BillingBuild.IsAvailable);
    /// <summary>Shares the native foreground/profile boundary for local aggregate review, without enabling offline networking.</summary>
    internal PersonalSyncAccess CaptureAnalysis(bool network) => CaptureCore(!network || PersonalSyncBuild.IsAvailable || Commerce.Billing.BillingBuild.IsAvailable);
    /// <summary>Captures public market-reference work without giving an offline edition a network capability.</summary>
    internal PersonalSyncAccess CaptureMarket(bool network) => CaptureCore(!network || PersonalSyncBuild.IsAvailable || Commerce.Billing.BillingBuild.IsAvailable);
    private PersonalSyncAccess CaptureCore(bool available)
    {
        long captured; CancellationToken cancellation;
        lock (state)
        {
            if (disposed || !available || !foreground || appLock.IsLocked)
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
        CancellationTokenSource retired;
        lock (state)
        {
            if (disposed) return;
            disposed = true; foreground = false; epoch++; retired = current;
        }
        appLock.AccessChanged -= OnAccessChanged; profiles.Changed -= OnAccessChanged;
        // Retire authority before invoking callbacks, outside the state lock. A callback failure
        // remains observable but cannot prevent disposal of this exact source or reopen access.
        try { retired.Cancel(); }
        finally { retired.Dispose(); }
    }
}
