using Vafadar.Zanance.App.Startup;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual root transitions and cancellation preserve one startup publication without borrowing replacements.</summary>
public sealed class StartupPublicationOwnerTests
{
    /// <summary>This owner's actual root setter event is not mistaken for a different presentation.</summary>
    [Fact]
    public void Own_publication_follows_its_exact_target_without_cancelling_itself()
    {
        var original = new object(); var target = new object(); object? root = original; var released = 0;
        StartupPublicationOwner<object>? owner = null;
        owner = new(original, () => root, value => { root = value; owner!.ObserveRoot(); }, () => true);
        owner.Released += () => released++;
        owner.Publish(target); Assert.True(owner.IsCurrent); Assert.True(owner.HasPublished); Assert.False(owner.Token.IsCancellationRequested);
        owner.EndPreparation(true); Assert.Equal(1, released); Assert.True(owner.IsCurrent);
    }

    /// <summary>Retirement cancels the worker but does not release its presentation resources before actual cleanup ends.</summary>
    [Fact]
    public async Task Retired_worker_cancellation_keeps_observers_until_actual_preparation_ends()
    {
        var original = new object(); object? root = original; var released = 0;
        var owner = new StartupPublicationOwner<object>(original, () => root, value => root = value, () => true);
        owner.Released += () => released++;
        var worker = Task.Delay(Timeout.InfiniteTimeSpan, owner.Token);
        root = new object(); owner.ObserveRoot();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => worker);
        Assert.Equal(0, released); Assert.False(owner.HasPublished);
        owner.EndPreparation(false); Assert.Equal(1, released);
    }

    /// <summary>A successful preparation can remain covered until its original deferred unlock callback actually publishes.</summary>
    [Fact]
    public void Deferred_publication_remains_live_after_preparation_then_releases_once()
    {
        var original = new object(); var target = new object(); object? root = original; var released = 0;
        var owner = new StartupPublicationOwner<object>(original, () => root, value => root = value, () => true);
        owner.Released += () => released++;
        owner.EndPreparation(true); Assert.Equal(0, released); Assert.True(owner.IsCurrent);
        owner.Publish(target); Assert.Equal(1, released); Assert.True(owner.HasPublished); Assert.True(owner.IsCurrent);
        Assert.Throws<InvalidOperationException>(() => owner.Publish(new object())); Assert.Same(target, root);
    }

    /// <summary>A queued old publication cannot overwrite a replacement or revive when its former cover returns.</summary>
    [Fact]
    public void Retired_deferred_publication_never_adopts_or_overwrites_new_root()
    {
        var original = new object(); var replacement = new object(); object? root = original; var writes = 0;
        var owner = new StartupPublicationOwner<object>(original, () => root, value => { writes++; root = value; }, () => true);
        owner.EndPreparation(true); root = replacement; owner.ObserveRoot();
        Assert.ThrowsAny<OperationCanceledException>(() => owner.Publish(new object()));
        Assert.Equal(0, writes); Assert.Same(replacement, root);
        root = original; Assert.False(owner.IsCurrent);
    }

    /// <summary>Native failure before an actual setter effect leaves no accepted publication or ready receipt.</summary>
    [Fact]
    public void Native_failure_before_actual_root_change_has_no_publication_receipt()
    {
        var original = new object(); object? root = original;
        var owner = new StartupPublicationOwner<object>(original, () => root, _ => throw new IOException("Original setter failed."), () => true);
        Assert.Throws<IOException>(() => owner.Publish(new object()));
        Assert.False(owner.HasPublished); Assert.True(owner.IsCurrent); Assert.Same(original, root);
        owner.EndPreparation(false); Assert.True(owner.Token.IsCancellationRequested);
    }

    /// <summary>A native continuation failure after a real setter effect cannot cause that publication to repeat.</summary>
    [Fact]
    public void Native_failure_after_actual_root_change_keeps_exact_publication_receipt()
    {
        var original = new object(); var target = new object(); object? root = original; var writes = 0;
        var owner = new StartupPublicationOwner<object>(original, () => root, value => { writes++; root = value; throw new IOException("Original setter follow-through failed."); }, () => true);
        Assert.Throws<IOException>(() => owner.Publish(target));
        Assert.True(owner.HasPublished); Assert.True(owner.IsCurrent);
        Assert.Throws<InvalidOperationException>(() => owner.Publish(target)); Assert.Equal(1, writes);
        owner.EndPreparation(true);
    }

    /// <summary>An owner-model change refuses even if the native root reference itself has not changed.</summary>
    [Fact]
    public void Replaced_original_model_refuses_before_any_native_setter()
    {
        var original = new object(); var current = true; var writes = 0;
        var owner = new StartupPublicationOwner<object>(original, () => original, _ => writes++, () => current);
        current = false; owner.ObserveRoot(); Assert.ThrowsAny<OperationCanceledException>(() => owner.Publish(new object()));
        Assert.Equal(0, writes); owner.EndPreparation(false);
    }

    /// <summary>A setter that silently refuses the original target cannot produce a ready/publication receipt.</summary>
    [Fact]
    public void Unchanged_native_root_after_setter_refuses_publication()
    {
        var original = new object(); var owner = new StartupPublicationOwner<object>(original, () => original, _ => { }, () => true);
        Assert.ThrowsAny<OperationCanceledException>(() => owner.Publish(new object()));
        Assert.False(owner.HasPublished); owner.EndPreparation(false);
    }

    /// <summary>The completed receipt checks its own current target and never borrows a later root after observers detach.</summary>
    [Fact]
    public void Completed_publication_receipt_still_refuses_an_unrelated_root()
    {
        var original = new object(); object? root = original;
        var owner = new StartupPublicationOwner<object>(original, () => root, value => root = value, () => true);
        owner.Publish(new object()); owner.EndPreparation(true); root = new object();
        Assert.False(owner.IsCurrent); Assert.ThrowsAny<OperationCanceledException>(() => owner.RequireCurrent());
    }

    /// <summary>A scope first captured after the original root retired cannot start another startup owner.</summary>
    [Fact]
    public void Initially_replaced_root_refuses_construction()
    {
        Assert.ThrowsAny<OperationCanceledException>(() => new StartupPublicationOwner<object>(new object(), () => new object(), _ => { }, () => true));
    }
}
