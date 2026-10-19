using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Diagnostics;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Recovers only the original synthetic ECB receipt after dropped log chunks; never replays UI or financial work.</summary>
    private static async Task<object> ReadEcbReportAsync(string root, string comparison, string original, CancellationToken ct)
    {
        if (!Guid.TryParseExact(original, "N", out _) || RedirectedAncestors(root).Length != 0) throw new ReviewCheckException("Original source receipt required.");
        var path = Path.Combine(root, "ecb-reference-" + original + "-result.json");
        var info = new FileInfo(path); if (!info.Exists || info.Length is <= 0 or > 262144) throw new ReviewCheckException("Original source receipt bound refused.");
        var bytes = await File.ReadAllBytesAsync(path, ct); using var packet = JsonDocument.Parse(bytes); var report = packet.RootElement;
        var sourceMvid = Guid.Parse("3306778a-bf87-497e-8b82-5f06bf4553e9");
        var review = report.GetProperty("Review"); var observations = report.GetProperty("Observations").EnumerateArray().ToArray();
        if (report.GetProperty("Comparison").GetString() != original || observations.Length != 18 ||
            review.GetProperty("Contexts").GetArrayLength() != 6 || !review.GetProperty("SyntheticSourceEvidence").GetBoolean() ||
            !review.GetProperty("OriginalStoredInputsUnchanged").GetBoolean() || !review.GetProperty("OriginalThemeRestored").GetBoolean() ||
            !review.GetProperty("OriginalLanguageRestored").GetBoolean() || review.GetProperty("UpstreamRequests").GetInt32() != 0 ||
            review.GetProperty("PaymentOrFinancialCommandsInvoked").GetBoolean() ||
            observations.Any(value => value.GetProperty("SourceMvid").GetGuid() != sourceMvid ||
                value.GetProperty("Captions").EnumerateArray().Any(caption => caption.GetProperty("Ellipsis").GetInt32() != 0)))
            throw new ReviewCheckException("Original completed synthetic source receipt differs.");
        var external = Android.App.Application.Context.GetExternalFilesDir(null)?.AbsolutePath ?? throw new IOException();
        var output = Path.Combine(external, "ecb-public-proof-" + original + ".json");
        await using (var file = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.Read)) await file.WriteAsync(bytes, ct);
        return new { Comparison = comparison, OriginalComparison = original, OriginalSourceMvid = sourceMvid, Contexts = 6,
            Surfaces = observations.Length, NativeCaptions = observations.Sum(value => value.GetProperty("Captions").GetArrayLength()),
            OriginalReceiptSha256 = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(bytes)),
            OriginalStoredInputsUnchanged = true, OriginalThemeAndLanguageRestored = true, OriginalReceiptExported = true,
            UiOrFinancialWorkRepeated = false, MainPackageTouched = false };
    }
    /// <summary>Original Release/AOT daily reference and pre-payment disclosure over independently owned synthetic data.</summary>
    private static async Task<object> ReviewEcbAsync(App app, IServiceProvider services, string root, string comparison, CancellationToken ct)
    {
        Directory.CreateDirectory(root);
        if (RedirectedAncestors(root).Length != 0 || File.Exists(Path.Combine(root, "ecb-reference-" + comparison + "-result.json")))
            throw new ReviewCheckException("Fresh original ECB review required.");
        await AwaitReimbursementFocusAsync(ct); app.OpenReviewShell(); await AwaitRootHomeAsync(ct);
        var local = services.GetRequiredService<ILocalizationService>(); var observations = new List<object>();
        var result = await EcbReferenceReview.RunAsync(app, services, root, [412], async language =>
        {
            if (local.CurrentLanguage.CultureName != language)
            {
                var rebuilt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                void Rebuilt(object? sender, EventArgs args) => rebuilt.TrySetResult();
                app.ShellRebuilt += Rebuilt;
                try
                {
                    local.SetLanguage(local.SupportedLanguages.Single(value => value.CultureName == language));
                    await rebuilt.Task.WaitAsync(TimeSpan.FromSeconds(30), ct);
                }
                finally { app.ShellRebuilt -= Rebuilt; }
            }
            await AwaitRootHomeAsync(ct);
        }, async (page, context) =>
        {
            await Task.Delay(150, ct); await AwaitReimbursementFocusAsync(ct);
            var scroll = ReimbursementViews(page).OfType<ScrollView>().Single(); var readings = new List<object>();
            var disclosure = services.GetRequiredService<Translator>()["Market_EcbFreeOriginal"];
            if (!ReimbursementViews(page).OfType<Label>().Any(value => value.IsVisible && value.Text == disclosure))
                throw new ReviewCheckException("Original free-source notice unavailable.");
            foreach (var label in ReimbursementViews(scroll).OfType<Label>().Where(value => value.Handler?.PlatformView is Android.Widget.TextView { IsShown: true } &&
                value.Text is { } caption && (caption.Contains(disclosure, StringComparison.Ordinal) || caption == services.GetRequiredService<Translator>()["Market_EcbOpenSource"])))
            {
                await scroll.ScrollToAsync(label, ScrollToPosition.Center, false); await Task.Delay(50, ct);
                if (label.Handler?.PlatformView is not Android.Widget.TextView native || !native.IsShown || !native.HasWindowFocus)
                    throw new ReviewCheckException("Original source caption is not in the focused review.");
                await WaitLayoutAsync(native, ct); var layout = native.Layout;
                var expected = label.FlowDirection == FlowDirection.LeftToRight ? label.Text : NativeDigits.Apply(label.Text!);
                var ellipsis = layout is null ? -1 : Enumerable.Range(0, layout.LineCount).Sum(index => layout.GetEllipsisCount(index));
                if (native.Text != expected || layout is null || ellipsis != 0 || native.Height - native.TotalPaddingTop - native.TotalPaddingBottom < layout.Height)
                    throw new ReviewCheckException("Original daily reference text is clipped.");
                readings.Add(new { label.Text, native.Width, native.Height, layout.LineCount, Ellipsis = ellipsis });
            }
            observations.Add(new { Context = context, Captions = readings, SourceMvid = typeof(EcbReferenceReview).Module.ModuleVersionId });
        }, async button =>
        {
            Element parent = button; while (parent is not ContentPage) parent = parent.Parent ?? throw new IOException();
            // Persistent review/Save controls belong to the footer, outside the scrolling content.
            // MAUI refuses ScrollToAsync for a non-descendant; inspect that actual footer directly.
            await Task.Delay(75, ct); await AwaitReimbursementFocusAsync(ct);
            var native = button.Handler?.PlatformView as Android.Views.View;
            var model = (Features.Rates.MarketReferenceViewModel)((ContentPage)parent).BindingContext;
            if (native is null || !native.IsShown || !native.HasWindowFocus || !native.PerformClick())
                throw new ReviewCheckException("Original native daily review action refused: " + JsonSerializer.Serialize(new
                {
                    model.Loaded, model.IsBusy, model.CanReview, model.CanApply, Selected = model.Selected?.Quote.Symbol.Id,
                    button.IsVisible, button.IsEnabled, NativeType = native?.GetType().FullName,
                    NativeShown = native?.IsShown, NativeFocus = native?.HasWindowFocus, NativeEnabled = native?.Enabled,
                    NativeWidth = native?.Width, NativeHeight = native?.Height, SourceMvid = typeof(EcbReferenceReview).Module.ModuleVersionId,
                }));
        }, ct);
        await AwaitRootHomeAsync(ct);
        return new { Comparison = comparison, Review = result, Observations = observations, ActualConfiguration = DeviceEvidence(), SystemSettingsChanged = false };
    }
}
