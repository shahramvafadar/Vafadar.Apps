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
        var osText = activity.Intent?.GetBooleanExtra("review_receipt_os_text", false) == true;
        var fontScale = osText ? ReceiptSourceOsTextReview.RequireScale(activity) : activity.Resources?.Configuration?.FontScale ?? 0;
        var exactWidths = activity.Intent?.GetBooleanExtra("review_source_help_exact_widths", false) == true;
        if (exactWidths && (!osText || !resume)) throw new ReviewCheckException("Exact large-text continuation requires its existing successful source scope.");
        Dictionary<string, System.Text.Json.JsonElement>? exactBaseline = null;
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
            if (exactWidths)
            {
                var reportPath = Path.Combine(root, "receipt-source-help-" + baseline + "-result.json");
                using var complete = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(reportPath, ct));
                var packet = complete.RootElement;
                if (packet.GetProperty("Comparison").GetString() != baseline
                    || !packet.GetProperty("OriginalOSScaledText").GetBoolean()
                    || Math.Abs(packet.GetProperty("ActualOSFontScale").GetSingle() - 2f) > .001f
                    || packet.GetProperty("FinancialSaves").GetInt32() != 0 || packet.GetProperty("CameraOrFileOpened").GetBoolean()
                    || packet.GetProperty("Device").GetProperty("IndependentPackage").GetString() != Package)
                    throw new ReviewCheckException("Original complete large-text source report required.");
                var cases = packet.GetProperty("Results").EnumerateArray().ToArray();
                if (cases.Length != 18 || !cases.Any(value =>
                    Math.Abs(value.GetProperty("ActualHelpDialogDp").GetDouble() - value.GetProperty("RequestedDialogDp").GetInt32()) > 1))
                    throw new ReviewCheckException("Only genuine missing exact widths may continue a successful source matrix.");
                exactBaseline = cases.ToDictionary(value => $"{value.GetProperty("Language").GetString()}-{value.GetProperty("Theme").GetString()}-{value.GetProperty("RequestedDialogDp").GetInt32()}",
                    value => value.Clone(), StringComparer.Ordinal);
            }
            else if (!originalFailed) throw new ReviewCheckException("Only the original terminal failed observer may be continued.");
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
            if (osText) return await ReceiptSourceOsTextReview.FindAsync(activity, expected, ct);
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
            if (osText) await Task.Delay(80, ct); // Let the real large-text descendants complete the resized native layout.
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
                            || value.GetProperty("CameraOrFileOpened").GetBoolean() || value.GetProperty("FinancialSaves").GetInt32() != 0
                            || osText && (!value.TryGetProperty("ActualOSFontScale", out var scale) || Math.Abs(scale.GetSingle() - 2f) > .001f))
                            throw new ReviewCheckException("Retained source-help case differs; do not replay it.");
                        if (exactWidths && (exactBaseline is null || !exactBaseline.TryGetValue($"{language}-{theme}-{requested}", out var originalCase)
                            || !System.Text.Json.JsonElement.DeepEquals(originalCase, value)))
                            throw new ReviewCheckException("Original accepted source case changed; never replace its receipt.");
                        results.Add(value.Clone()); retained.Add(Path.GetFileName(casePath));
                        var missingExact = exactWidths
                            ? Math.Abs(value.GetProperty("ActualSourceDialogDp").GetDouble() - requested) > 1
                                || Math.Abs(value.GetProperty("ActualHelpDialogDp").GetDouble() - requested) > 1
                            : requested == 412 && value.GetProperty("ActualHelpDialogDp").GetDouble() < 411;
                        if (!missingExact) continue;
                        // Retain the original narrower observations and run only genuinely missing exact targets.
                        casePath = Path.Combine(root, $"{language}-{theme}-{requested}-exact.json");
                        if (File.Exists(casePath))
                        {
                            using var exact = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(casePath, ct));
                            if (Math.Abs(exact.RootElement.GetProperty("ActualHelpDialogDp").GetDouble() - requested) > 1
                                || exactWidths && (Math.Abs(exact.RootElement.GetProperty("ActualSourceDialogDp").GetDouble() - requested) > 1
                                    || Math.Abs(exact.RootElement.GetProperty("ActualOSFontScale").GetSingle() - 2f) > .001f)
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
                    var source = osText
                        ? await ReceiptSourceOsTextReview.ActionAsync(activity, translator["Receipt_ImageHelp"], ct)
                        : Geometry(await Text(translator["Receipt_ImageHelp"]));
                    if (osText) help = await Text(translator["Receipt_ImageHelp"]); // Native list scrolling may recycle the old row.
                    Invoke(help);
                    _receiptSourceStage = $"{language}/{theme}/{requested}/help";
                    var message = await Text(translator["Receipt_ImageHelpText"]); var helpWidth = await Width(requested);
                    var body = osText
                        ? await ReceiptSourceOsTextReview.BodyAsync(activity, translator["Receipt_ImageHelpText"], ct)
                        : Geometry(await Text(translator["Receipt_ImageHelpText"]));
                    var ok = await Text(translator["Common_Ok"]);
                    var okGeometry = osText ? await ReceiptSourceOsTextReview.ActionAsync(activity, translator["Common_Ok"], ct) : null;
                    Invoke(ok);
                    _receiptSourceStage = $"{language}/{theme}/{requested}/cancel";
                    await Text(translator["Receipt_ImageHelp"]); await Width(requested);
                    var cancel = await Text(translator["Common_Cancel"]);
                    var cancelGeometry = osText ? await ReceiptSourceOsTextReview.ActionAsync(activity, translator["Common_Cancel"], ct) : null;
                    Invoke(cancel);
                    if (await choice.WaitAsync(TimeSpan.FromSeconds(10), ct) != ReceiptSourceChoice.Cancelled)
                        throw new ReviewCheckException("Help/cancel unexpectedly selected a native source.");
                    if (exactWidths && (Math.Abs(sourceWidth - requested) > 1 || Math.Abs(helpWidth - requested) > 1))
                        throw new ReviewCheckException("The original missing width is still capped; preserve the actual scene without claiming its target.");
                    var result = new { Language = language, Theme = theme.ToString(), RequestedDialogDp = requested,
                        ActualSourceDialogDp = sourceWidth, ActualHelpDialogDp = helpWidth,
                        Source = source, Body = body, Ok = okGeometry, Cancel = cancelGeometry, ActualOSFontScale = fontScale,
                        NativeClicks = 3, CameraOrFileOpened = false, FinancialSaves = 0 };
                    await File.WriteAllTextAsync(casePath, System.Text.Json.JsonSerializer.Serialize(result), ct); results.Add(result);
                }
            }
        }
        catch (Exception error) when (osText && error is not OutOfMemoryException)
        {
            // Preserve the actual failed native scene before the normal theme/orientation restoration.
            LogReport("receipt-source-os-failure", Path.GetFileName(root), new
            { Comparison = comparison, Stage = _receiptSourceStage, Evidence = ReceiptSourceOsTextReview.LastEvidence,
                NoCameraFileReadOrFinancialSave = true });
            throw;
        }
        finally
        {
            activity.RequestedOrientation = originalOrientation;
            themes.Set(originalTheme); await SetLanguageAsync(app, local, originalLanguage, ct);
        }
        return new { Comparison = comparison, Results = results, RetainedCompletedCases = retained, OriginalSourceChooser = true,
            OriginalNativeInteraction = true, FinancialSaves = 0, CameraOrFileOpened = false,
            MainPackageTouched = false, SystemSettingsChanged = false, Device = DeviceEvidence(),
            ActualOSFontScale = fontScale, OriginalOSScaledText = osText, ExactWidthContinuation = exactWidths,
            OriginalSuccessfulBaseline = exactWidths ? baseline : null,
            Boundary = osText
                ? "Actual OS200% native dialog text/actions/scroll reachability only; no spoken/gesture/camera/source-read/general stability acceptance"
                : "Owned dialog widths/app orientation; no OS scaling, spoken, camera, source-read or general stability acceptance" };
    }
}
