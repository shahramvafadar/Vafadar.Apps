using Vafadar.Zanance.App.Features.Analysis;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Device opt-out persistence and session retirement without platform or provider calls.</summary>
public sealed class AiFeaturesTests
{
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
