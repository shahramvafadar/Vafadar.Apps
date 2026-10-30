using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Sync;

/// <summary>Native foreground triggers only; never an OS background service or startup database initializer.</summary>
internal sealed class PersonalSyncForegroundWorker : IDisposable
{
    private readonly PersonalSyncForegroundCoordinator coordinator;
    private readonly MauiPersonalSyncEnvironment environment;
    private readonly PersonalSyncLifetime lifetime;
    private readonly ZananceStore finances;
    private readonly TimeProvider time;
    private readonly CancellationTokenSource stopped = new();
    private ITimer? timer;
    private bool started;

    /// <summary>Retains dependencies only; the covered startup worker starts scheduling after preparation completes.</summary>
    public PersonalSyncForegroundWorker(PersonalSyncForegroundCoordinator coordinator, MauiPersonalSyncEnvironment environment,
        PersonalSyncLifetime lifetime, ZananceStore finances, TimeProvider time)
    {
        this.coordinator = coordinator; this.environment = environment; this.lifetime = lifetime;
        this.finances = finances; this.time = time;
    }

    /// <summary>Attaches once, after database readiness, without awaiting a network operation on the native thread.</summary>
    public void Start()
    {
        if (started || !environment.Available) return;
        started = true; lifetime.Changed += OnChanged; finances.Changed += OnChanged;
        environment.ConnectionStateChanged += OnConnectionChanged; coordinator.Changed += OnStatusChanged;
        // Timer ticks only request a check. Captured foreground/unlocked authority is required before any database read.
        timer = time.CreateTimer(_ => Request(), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        Request();
    }

    private void OnChanged(object? sender, EventArgs e) => Request();
    private void OnConnectionChanged(object? sender, EventArgs e) { coordinator.Reset(); Request(); }
    private void OnStatusChanged(object? sender, EventArgs e) => MainThread.BeginInvokeOnMainThread(environment.PublishStatusChanged);
    private void Request()
    {
        if (!started || stopped.IsCancellationRequested) return;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!started || stopped.IsCancellationRequested || !environment.HasConnectionInterest) return;
            if (Application.Current?.Windows.FirstOrDefault()?.Page is not AppShell shell || shell.Navigation.ModalStack.Count != 0 ||
                shell.CurrentPage is not (Features.Home.HomePage or Features.More.MorePage or PersonalSyncPage or Features.Sharing.HouseholdPage)) return;
            if (shell.CurrentPage.BindingContext is PersonalSyncViewModel { HasPendingOperation: true }) return;
            if (shell.CurrentPage.BindingContext is Features.Sharing.HouseholdViewModel { IsBusy: true }) return;
            PersonalSyncAccess captured;
            try { captured = environment.Capture(); }
            catch (OperationCanceledException) { return; }
            // Capture on the native thread before queueing. Navigation can retire it before the worker starts.
            _ = Task.Run(async () =>
            {
                try { await coordinator.RunAsync(true, stopped.Token, captured); }
                catch (Exception error) when (error is not OutOfMemoryException) { /* Native teardown never logs private sync failures. */ }
            });
        });
    }

    /// <inheritdoc />
    public void Dispose()
    {
        started = false; stopped.Cancel(); timer?.Dispose();
        lifetime.Changed -= OnChanged; finances.Changed -= OnChanged;
        environment.ConnectionStateChanged -= OnConnectionChanged; coordinator.Changed -= OnStatusChanged;
        // Queued callbacks retain the token source until their final cancellation check; it owns no native handle.
    }
}
