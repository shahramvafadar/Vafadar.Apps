using System.ComponentModel;

namespace Vafadar.Zanance.App.Startup;

/// <summary>Binds original startup publication to one actual window/root/model without performing storage work.</summary>
internal sealed class StartupPreparationPresentation
{
    private readonly Window window;
    private readonly Page cover;
    private readonly StartupPublicationOwner<Page> owner;

    /// <summary>Captures the existing covered window and subscribes only until actual preparation/publication end.</summary>
    internal StartupPreparationPresentation(Window window, Page cover, Func<bool> ownsWindow)
    {
        this.window = window; this.cover = cover;
        owner = new(cover, () => window.Page, page => window.Page = page, ownsWindow);
        owner.Released += Detach;
        window.PropertyChanged += OnWindowChanged; window.Destroying += OnWindowDestroying;
        cover.BindingContextChanged += OnCoverChanged;
    }
    /// <summary>Gets the cancellation token borrowed by protected preparation, never a UI validator called on a worker.</summary>
    internal CancellationToken Token => owner.Token;
    /// <summary>Gets whether the original or its own actually published page still owns this window.</summary>
    internal bool IsCurrent => owner.IsCurrent;
    /// <summary>Gets whether the prepared page was actually published.</summary>
    internal bool HasPublished => owner.HasPublished;
    /// <summary>Refuses a retired presentation before another original side effect.</summary>
    internal void RequireCurrent() => owner.RequireCurrent();
    /// <summary>Publishes only this owner's captured prepared page.</summary>
    internal void Publish(Page page) => owner.Publish(page);
    /// <summary>Ends the actual protected preparation while preserving a still-pending unlock continuation.</summary>
    internal void EndPreparation(bool succeeded) => owner.EndPreparation(succeeded);
    private void OnWindowChanged(object? sender, PropertyChangedEventArgs e)
    { if (e.PropertyName == nameof(Window.Page)) owner.ObserveRoot(); }
    private void OnWindowDestroying(object? sender, EventArgs e) => owner.Retire();
    private void OnCoverChanged(object? sender, EventArgs e) => owner.ObserveRoot();
    private void Detach()
    {
        window.PropertyChanged -= OnWindowChanged; window.Destroying -= OnWindowDestroying;
        cover.BindingContextChanged -= OnCoverChanged; owner.Released -= Detach;
    }
}
