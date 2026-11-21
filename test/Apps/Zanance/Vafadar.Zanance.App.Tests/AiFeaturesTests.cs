using Vafadar.Zanance.App.Features.Analysis;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Device opt-out persistence and session retirement without platform or provider calls.</summary>
public sealed class AiFeaturesTests
{
    [Fact]
    public void Off_finishes_and_notifies_even_when_one_original_operation_throws_during_cancellation()
    {
        var saved = true;
        var settings = new AiFeatures(saved, value => saved = value);
        var original = settings.Capture();
        var completedCancellation = false;
        using var ordinary = original.Register(() => completedCancellation = true);
        using var faulty = original.Register(() => throw new InvalidOperationException("Synthetic cancellation callback failure."));
        var notices = new List<bool>();
        settings.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(AiFeatures.Enabled)) notices.Add(settings.Enabled);
        };

        settings.Enabled = false;
        Assert.False(saved);
        Assert.True(original.IsCancellationRequested);
        Assert.True(completedCancellation);
        Assert.True(settings.Capture().IsCancellationRequested);
        Assert.Equal(new[] { false }, notices);

        settings.Enabled = true;
        Assert.True(saved);
        Assert.True(original.IsCancellationRequested);
        Assert.False(settings.Capture().IsCancellationRequested);
        Assert.Equal(new[] { false, true }, notices);
    }

    /// <summary>Reopening the app preserves the choice and enabling it creates a new lifetime.</summary>
    [Fact]
    public void Off_persists_and_reenabling_never_revives_original_work()
    {
        var saved = true;
        var notices = new List<string?>();
        var settings = new AiFeatures(saved, value => saved = value);
        settings.PropertyChanged += (_, args) => notices.Add(args.PropertyName);
        var original = settings.Capture();

        settings.Enabled = false;
        settings.Enabled = false;
        Assert.False(saved);
        Assert.True(original.IsCancellationRequested);
        Assert.True(settings.Capture().IsCancellationRequested);
        Assert.False(new AiFeatures(saved, _ => { }).Enabled);

        settings.Enabled = true;
        Assert.True(saved);
        Assert.True(original.IsCancellationRequested);
        Assert.False(settings.Capture().IsCancellationRequested);
        Assert.Equal(new[] { nameof(AiFeatures.Enabled), nameof(AiFeatures.Enabled) }, notices);
    }
}
