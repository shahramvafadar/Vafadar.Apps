#if DEBUG && WINDOWS
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Media;
using Font = Microsoft.Maui.Font;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>
/// Process-local layout stress for the fictitious snapshot walk-through; never changes a system setting.
/// This is not evidence of a Windows accessibility setting or an Android text renderer.
/// </summary>
internal sealed class DebugFontScale : IFontManager
{
    private readonly FontManager _inner;
    private readonly double _requestedScale;
    private readonly double _systemScale;

    private DebugFontScale(IServiceProvider services, double requestedScale)
    {
        _inner = new FontManager(services.GetRequiredService<IFontRegistrar>(), services);
        _requestedScale = requestedScale;
        _systemScale = new Windows.UI.ViewManagement.UISettings().TextScaleFactor;
    }

    /// <inheritdoc />
    public FontFamily DefaultFontFamily => _inner.DefaultFontFamily;

    /// <inheritdoc />
    public double DefaultFontSize => _inner.DefaultFontSize;

    /// <inheritdoc />
    public FontFamily GetFontFamily(Font font) => _inner.GetFontFamily(font);

    /// <inheritdoc />
    public double GetFontSize(Font font, double defaultFontSize = 0)
    {
        // WinUI applies the real system factor afterwards. Compensate once, preserving explicit opt-outs.
        var factor = font.AutoScalingEnabled ? _requestedScale / _systemScale : 1;
        return _inner.GetFontSize(font, defaultFontSize) * factor;
    }

    /// <summary>Enables stress only for an explicit snapshot run, before native text handlers are constructed.</summary>
    internal static void Register(IServiceCollection services)
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("VAFADAR_SNAPSHOTS"))
            || !double.TryParse(Environment.GetEnvironmentVariable("VAFADAR_SNAPSHOT_FONT_SCALE"),
                CultureInfo.InvariantCulture, out var scale) || !double.IsFinite(scale) || scale is < 1 or > 2)
        {
            return;
        }

        services.AddSingleton(provider => new DebugFontScale(provider, scale));
        services.AddSingleton<IFontManager>(provider => provider.GetRequiredService<DebugFontScale>());
    }

    /// <summary>Records the requested factor and unchanged system factor beside the rendered evidence.</summary>
    internal void WriteEvidence(string folder) => File.WriteAllText(Path.Combine(folder, "font-scale.json"),
        JsonSerializer.Serialize(new
        {
            kind = "Windows process-local layout stress, not system text-scale acceptance",
            requestedScale = _requestedScale,
            systemScale = _systemScale,
            compensatedFontFactor = _requestedScale / _systemScale,
            respectsAutoScalingOptOut = true,
        }, new JsonSerializerOptions { WriteIndented = true }));

    /// <summary>Checks the actual measured quick-action targets and names after the native layout pass.</summary>
    internal static void CheckQuickActions(Features.Home.HomePage page, string folder, string name)
    {
        // An empty profile intentionally hides quick actions; it has no native targets to measure.
        if (page.BindingContext is not Features.Home.HomeViewModel { HasAccounts: true }) { return; }
        if (page.FindByName<FlexLayout>("QuickActions") is not { } actions) { return; }
        var evidence = new List<object>();
        foreach (var row in actions.Children.OfType<Grid>())
        {
            var label = row.Children.OfType<Label>().Single();
            var button = row.Children.OfType<Button>().Single();
            if (label.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock text) { continue; }
            if (text.IsTextTrimmed || row.X < -1 || row.X + row.Width > actions.Width + 1
                || button.Width < 44 || button.Height < 44)
            {
                throw new InvalidOperationException("A quick action name is clipped or its measured target does not fit.");
            }
            evidence.Add(new { label.Text, text.FontSize, text.IsTextScaleFactorEnabled, text.IsTextTrimmed,
                row.X, row.Y, row.Width, row.Height, targetWidth = button.Width, targetHeight = button.Height });
        }
        if (evidence.Count != 4) { throw new InvalidOperationException("Four native quick-action labels must be measured."); }
        File.WriteAllText(Path.Combine(folder, name + "-quick-actions.json"),
            JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true }));
    }
}
#endif
