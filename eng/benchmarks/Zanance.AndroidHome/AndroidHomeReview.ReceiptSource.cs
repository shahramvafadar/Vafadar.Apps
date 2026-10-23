using Android.Content.PM;
using Android.Views;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Receipts;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    private static string? _receiptSourceStage;

    /// <summary>Invokes the original source/help/cancel dialogs in a new disposable identity, without opening a source.</summary>
    private static async Task<object> ReviewReceiptSourceHelpAsync(App app, IServiceProvider services,
        string root, string comparison, string? baseline, bool resume, CancellationToken ct)
    {
        await AwaitOriginalMoneyWindowAsync(ct); app.OpenReviewShell(); await AwaitRootHomeAsync(ct);
        var local = services.GetRequiredService<ILocalizationService>();
        var themes = services.GetRequiredService<ThemeService>();
        var translator = services.GetRequiredService<Translator>();
        var interaction = services.GetRequiredService<IAppInteraction>();
        var activity = Platform.CurrentActivity ?? throw new ReviewCheckException("Owned source-help activity required.");
        var originalOrientation = activity.RequestedOrientation;
        var originalLanguage = local.CurrentLanguage.CultureName; var originalTheme = themes.Choice;
        var results = new List<object>();
        var retained = new List<string>();
        if (resume)
        {
            if (baseline is null || baseline == comparison) throw new ReviewCheckException("Original failed source-help identity required.");
            var originalFailed = false;
            foreach (var path in Directory.EnumerateFiles(root, "failure-*.json"))
            {
                using var previous = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(path, ct));
                originalFailed |= previous.RootElement.GetProperty("Failed").GetBoolean()
                    && previous.RootElement.GetProperty("Comparison").GetString() == baseline;
            }
            if (!originalFailed) throw new ReviewCheckException("Only the original terminal failed observer may be continued.");
        }

        IEnumerable<Android.Views.View> Walk(Android.Views.View view)
        {
            yield return view;
            if (view is Android.Views.ViewGroup group)
                for (var i = 0; i < group.ChildCount; i++) if (group.GetChildAt(i) is { } child)
                    foreach (var nested in Walk(child)) yield return nested;
        }
        Android.Views.View? FocusedDialog() => Android.Views.Inspectors.WindowInspector.GlobalWindowViews
            .SingleOrDefault(v => v.Context?.PackageName == Package && v.IsShown && v.HasWindowFocus
                && v != activity.Window?.DecorView && v.LayoutParameters is WindowManagerLayoutParams);
        async Task<Android.Widget.TextView> Text(string expected)
        {
            for (var i = 0; i < 100; i++)
            {
                ct.ThrowIfCancellationRequested();
                var target = FocusedDialog() is { } dialog ? Walk(dialog).OfType<Android.Widget.TextView>()
                    .FirstOrDefault(v => v.IsShown && v.Text == expected && v.Layout is not null) : null;
                if (target is not null) return target;
                await Task.Delay(40, ct);
            }
            throw new ReviewCheckException("Exact current native source-help text unavailable.");
        }
        async Task<double> Width(int requested)
        {
            var dialog = FocusedDialog() ?? throw new ReviewCheckException("Focused owned native dialog required.");
            var density = dialog.Resources?.DisplayMetrics?.Density ?? 0;
            if (density <= 0 || dialog.LayoutParameters is not WindowManagerLayoutParams parameters)
                throw new ReviewCheckException("Actual dialog dimensions unavailable.");
            // Only this new caller's own native window is resized; no display/font/OS setting changes.
            var pixels = Math.Min((int)Math.Round(requested * density), activity.Window!.DecorView!.Width);
            parameters.Width = pixels; activity.WindowManager!.UpdateViewLayout(dialog, parameters);
            for (var i = 0; i < 50 && dialog.Width != pixels; i++) await Task.Delay(40, ct);
            if (dialog.Width != pixels) throw new ReviewCheckException("Requested owned dialog width did not settle.");
            return pixels / (double)density;
        }
        object Geometry(Android.Widget.TextView text)
        {
            using var bounds = new Android.Graphics.Rect();
            var layout = text.Layout ?? throw new ReviewCheckException("Native text layout unavailable.");
            if (text.Text is not { Length: > 0 } value || layout.LineCount == 0
                || layout.GetLineEnd(layout.LineCount - 1) != value.Length
                || Enumerable.Range(0, layout.LineCount).Any(i => layout.GetEllipsisCount(i) != 0))
                throw new ReviewCheckException("Native source-help text is clipped or ellipsized.");
            return new { Text = value, Lines = layout.LineCount, text.Width, text.Height,
                Visible = text.GetGlobalVisibleRect(bounds), VisibleWidth = bounds.Width(),
                VisibleHeight = bounds.Height(), Color = text.CurrentTextColor,
                Direction = text.LayoutDirection.ToString() };
        }
        void Invoke(Android.Widget.TextView target)
        {
            if (!target.IsShown || !target.Enabled || FocusedDialog() is null)
                throw new ReviewCheckException("Current owned native action required.");
            if (target is Android.Widget.Button button)
            {
                if (!button.PerformClick()) throw new ReviewCheckException("Native dialog action refused.");
                return;
            }
            for (Android.Views.IViewParent? parent = target.Parent; parent is not null; parent = parent.Parent)
                if (parent is Android.Widget.ListView list)
                {
                    var position = list.GetPositionForView(target);
                    if (position < 0 || list.Adapter is null || !list.PerformItemClick(target, position, list.Adapter.GetItemId(position)))
                        throw new ReviewCheckException("Native source action refused.");
                    return;
                }
            throw new ReviewCheckException("Original source action has no native invocation target.");
        }
        try
        {
            foreach (var language in new[] { "en", "fa", "de" })
            foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            {
                themes.Set(theme); await SetLanguageAsync(app, local, language, ct); await AwaitRootHomeAsync(ct);
                foreach (var requested in new[] { 360, 412, 680 })
                {
                    _receiptSourceStage = $"{language}/{theme}/{requested}/focus";
                    var casePath = Path.Combine(root, $"{language}-{theme}-{requested}.json");
                    if (resume && File.Exists(casePath))
                    {
                        using var completed = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(casePath, ct));
                        var value = completed.RootElement;
                        if (value.GetProperty("Language").GetString() != language || value.GetProperty("Theme").GetString() != theme.ToString()
                            || value.GetProperty("RequestedDialogDp").GetInt32() != requested || value.GetProperty("NativeClicks").GetInt32() != 3
                            || value.GetProperty("CameraOrFileOpened").GetBoolean() || value.GetProperty("FinancialSaves").GetInt32() != 0)
                            throw new ReviewCheckException("Retained source-help case differs; do not replay it.");
                        results.Add(value.Clone()); retained.Add(Path.GetFileName(casePath));
                        if (requested != 412 || value.GetProperty("ActualHelpDialogDp").GetDouble() >= 411) continue;
                        // This phone's unchanged portrait viewport is384dp. Retain that successful narrower
                        // observation and complete only the missing exact412dp dialog in our own landscape window.
                        casePath = Path.Combine(root, $"{language}-{theme}-412-exact.json");
                        if (File.Exists(casePath))
                        {
                            using var exact = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(casePath, ct));
                            if (exact.RootElement.GetProperty("ActualHelpDialogDp").GetDouble() is < 411 or > 413
                                || exact.RootElement.GetProperty("Language").GetString() != language
                                || exact.RootElement.GetProperty("Theme").GetString() != theme.ToString()
                                || exact.RootElement.GetProperty("CameraOrFileOpened").GetBoolean())
                                throw new ReviewCheckException("Retained exact412dp case differs.");
                            results.Add(exact.RootElement.Clone()); retained.Add(Path.GetFileName(casePath)); continue;
                        }
                    }
                    activity.RequestedOrientation = requested >= 412 ? ScreenOrientation.Landscape : ScreenOrientation.Portrait;
                    for (var i = 0; i < 75; i++)
                    {
                        var metrics = activity.Resources?.DisplayMetrics;
                        if (metrics is not null && (requested >= 412 ? metrics.WidthPixels > metrics.HeightPixels : metrics.HeightPixels > metrics.WidthPixels)) break;
                        await Task.Delay(40, ct);
                    }
                    // Orientation/configuration changes briefly detach focus. Wait for the same actual
                    // activity to own the window; never weaken the foreground guard or invoke behind it.
                    for (var i = 0; i < 100; i++)
                    {
                        if (Platform.CurrentActivity is { HasWindowFocus: true } current && current.PackageName == Package)
                        { activity = current; break; }
                        await Task.Delay(40, ct);
                    }
                    RequireHomeWindowFocus();
                    _receiptSourceStage = $"{language}/{theme}/{requested}/source";
                    var choice = ReceiptSourceChooser.ChooseAsync(translator, interaction, true, true);
                    var help = await Text(translator["Receipt_ImageHelp"]); var sourceWidth = await Width(requested);
                    var source = Geometry(await Text(translator["Receipt_ImageHelp"])); Invoke(help);
                    _receiptSourceStage = $"{language}/{theme}/{requested}/help";
                    var message = await Text(translator["Receipt_ImageHelpText"]); var helpWidth = await Width(requested);
                    var body = Geometry(await Text(translator["Receipt_ImageHelpText"]));
                    var ok = await Text(translator["Common_Ok"]); Invoke(ok);
                    _receiptSourceStage = $"{language}/{theme}/{requested}/cancel";
                    await Text(translator["Receipt_ImageHelp"]); await Width(requested);
                    var cancel = await Text(translator["Common_Cancel"]); Invoke(cancel);
                    if (await choice.WaitAsync(TimeSpan.FromSeconds(10), ct) != ReceiptSourceChoice.Cancelled)
                        throw new ReviewCheckException("Help/cancel unexpectedly selected a native source.");
                    var result = new { Language = language, Theme = theme.ToString(), RequestedDialogDp = requested,
                        ActualSourceDialogDp = sourceWidth, ActualHelpDialogDp = helpWidth,
                        Source = source, Body = body, NativeClicks = 3, CameraOrFileOpened = false, FinancialSaves = 0 };
                    await File.WriteAllTextAsync(Path.Combine(root, $"{language}-{theme}-{requested}.json"),
                        System.Text.Json.JsonSerializer.Serialize(result), ct); results.Add(result);
                }
            }
        }
        finally
        {
            activity.RequestedOrientation = originalOrientation;
            themes.Set(originalTheme); await SetLanguageAsync(app, local, originalLanguage, ct);
        }
        return new { Comparison = comparison, Results = results, RetainedCompletedCases = retained, OriginalSourceChooser = true,
            OriginalNativeInteraction = true, FinancialSaves = 0, CameraOrFileOpened = false,
            MainPackageTouched = false, SystemSettingsChanged = false, Device = DeviceEvidence(),
            Boundary = "Owned dialog widths/app orientation; no OS scaling, spoken, camera, source-read or general stability acceptance" };
    }
}
