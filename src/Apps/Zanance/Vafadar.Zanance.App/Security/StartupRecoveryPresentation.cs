using System.ComponentModel;

namespace Vafadar.Zanance.App.Security;

/// <summary>Owns one actual covered startup root and its non-secret original storage capture.</summary>
internal sealed class StartupRecoveryPresentation : IDisposable
{
    private readonly Window window;
    private readonly Func<bool> ownsStartup;
    private bool disposed;

    /// <summary>Binds existing source authority to the actual original window and startup model, without key or database work.</summary>
    internal StartupRecoveryPresentation(Window window, Page page, RecoverySetupAuthority source, Func<bool> ownsStartup)
    {
        this.window = window; Page = page; Source = source; this.ownsStartup = ownsStartup;
        RequireCurrent();
        window.PropertyChanged += OnWindowChanged; window.Destroying += OnWindowDestroying;
        page.BindingContextChanged += OnStartupChanged;
    }

    /// <summary>Gets the original covered page, never a root first captured after native authentication.</summary>
    internal Page Page { get; }
    /// <summary>Gets only original non-secret storage and cancellation authority for protected workers.</summary>
    internal RecoverySetupAuthority Source { get; }
    /// <summary>Gets the original cancellation token, also used by borrowed native input and protected waits.</summary>
    internal CancellationToken Token => Source.Token;
    /// <summary>Reports actual current UI/source authority without changing authentication or accepting a native result.</summary>
    internal bool IsCurrent
    {
        get
        {
            if (disposed || Token.IsCancellationRequested || !ReferenceEquals(window.Page, Page) || !ownsStartup()) return false;
            try { Source.RequireCurrent(); return true; }
            catch (Exception failure) when (failure is not OutOfMemoryException) { return false; }
        }
    }
    /// <summary>Refuses a retired original frame before any new UI or worker handoff.</summary>
    internal void RequireCurrent() => StartupRecoveryContinuation.RequireCurrent(Source, () => IsCurrent);
    /// <summary>Awaits the actual native result while retaining the original captured startup owner.</summary>
    internal Task<T> AwaitAsync<T>(Func<Task<T>> action) => StartupRecoveryContinuation.AwaitAsync(action, Source, () => IsCurrent);
    /// <summary>Awaits actual non-result native navigation or feedback with the same departure cancellation.</summary>
    internal async Task AwaitAsync(Func<Task> action)
    { await AwaitAsync(async () => { await action(); return true; }); }

    private void OnWindowChanged(object? sender, PropertyChangedEventArgs e)
    { if (e.PropertyName == nameof(Window.Page) && !IsCurrent) Source.Retire(); }
    private void OnStartupChanged(object? sender, EventArgs e)
    { if (!IsCurrent) Source.Retire(); }
    private void OnWindowDestroying(object? sender, EventArgs e) => Source.Retire();

    /// <summary>Detaches the original UI observers only after its actual borrowed protected operation has ended.</summary>
    public void Dispose()
    {
        if (disposed) return;
        window.PropertyChanged -= OnWindowChanged; window.Destroying -= OnWindowDestroying;
        Page.BindingContextChanged -= OnStartupChanged; disposed = true; Source.Dispose();
    }
}
