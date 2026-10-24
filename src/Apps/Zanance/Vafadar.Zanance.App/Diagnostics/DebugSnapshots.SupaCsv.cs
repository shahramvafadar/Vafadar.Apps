#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Media;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Localization;
using Vafadar.Zanance.App.Features.DataFiles;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.DataFiles;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Checks only new original SUPA controls, consent, Help and native Cancel without a financial write.</summary>
    private static async Task ReviewSupaCsvAsync(App app, IServiceProvider services, string folder)
    {
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.supacsvreview") throw new IOException("Independent SUPA identity required.");
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(20)); var ct = deadline.Token;
        var path = Path.Combine(folder, "supa-native.db"); var setup = Path.Combine(folder, "supa-setup.json");
        var resume = Environment.GetEnvironmentVariable("VAFADAR_SUPA_NATIVE_RESUME") == "1";
        var finalHelp = Environment.GetEnvironmentVariable("VAFADAR_SUPA_FINAL_HELP") == "1";
        var largeText = Environment.GetEnvironmentVariable("VAFADAR_SUPA_LARGE_TEXT") == "1";
        var systemScale = 1d;
        if (largeText)
        {
            if (!resume || finalHelp) throw new IOException("Large-text review requires only the retained fictitious source.");
            using var scale = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(folder, "font-scale.json"), ct));
            if (scale.RootElement.GetProperty("requestedScale").GetDouble() != 2) throw new IOException("Explicit double-size process-local review required.");
            systemScale = scale.RootElement.GetProperty("systemScale").GetDouble();
        }
        if (File.Exists(path) != resume || File.Exists(setup) != resume || !finalHelp && File.Exists(Path.Combine(folder, "complete.json")) || finalHelp && !resume)
            throw new IOException("Preserve incomplete original SUPA work; completed native contexts must not replay.");
        await using var fixture = new ServiceCollection().AddSingleton<TimeProvider>(TimeProvider.System).AddZananceData(path).BuildServiceProvider();
        var store = fixture.GetRequiredService<ZananceStore>(); string before;
        if (!resume)
        {
            fixture.MigrateLocalDatabase<ZananceDbContext>(); await store.EnsureDefaultCategoriesAsync(ct);
            var target = new Account { Name = "Original SUPA target / Beispielkonto / حساب نمونه", CurrencyCode = "EUR", Type = AccountType.Checking, OpeningDate = new(2026, 1, 1) };
            await store.SaveAccountAsync(target, ct);
            await store.UpdateSettingsAsync(settings => { settings.Mode = ExperienceMode.Advanced; settings.OnboardingCompleted = true; settings.DefaultAccountId = target.Id; }, ct);
            before = await ImportFinancialNativeReview.StoredAsync(fixture, ct);
            await using var output = new FileStream(setup, FileMode.CreateNew, FileAccess.Write);
            await JsonSerializer.SerializeAsync(output, new { Before = before }, cancellationToken: ct);
        }
        else
        {
            using var retained = JsonDocument.Parse(await File.ReadAllTextAsync(setup, ct));
            before = retained.RootElement.GetProperty("Before").GetString() ?? throw new IOException("Original SUPA setup missing.");
            if (before != await ImportFinancialNativeReview.StoredAsync(fixture, ct)) throw new IOException("Retained original SUPA data changed.");
        }
        var local = services.GetRequiredService<ILocalizationService>(); var text = services.GetRequiredService<Translator>();
        var themes = services.GetRequiredService<ThemeService>(); var window = app.Windows[0]; var completed = new List<string>();
        var oldLanguage = local.CurrentLanguage.CultureName; var oldTheme = themes.Choice; var oldWidth = window.Width; var oldHeight = window.Height;
        try
        {
            window.Height = 892;
            foreach (var language in new[] { "en", "fa", "de", "es", "fr", "it" })
            foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            foreach (var width in finalHelp ? new[] { 360 } : new[] { 360, 412, 1280 })
            {
                var key = (finalHelp ? "final-help-" : largeText ? "large-text-" : "") + $"{language}-{theme}-{width}"; var receipt = Path.Combine(folder, key + "-complete.json");
                await SetStoreThemeAsync(app, themes, theme); await SetLanguageAsync(app, local, language);
                var helpText = text["Help_SupaCsv_Text"] + Environment.NewLine + Environment.NewLine + text.Format("Help_Example", text["Help_SupaCsv_Example"]);
                if (File.Exists(receipt))
                {
                    using var kept = JsonDocument.Parse(await File.ReadAllTextAsync(receipt, ct));
                    if (kept.RootElement.GetProperty("Warning").GetString() != text["SupaCsv_Warning"]
                        || kept.RootElement.GetProperty("Consent").GetString() != text["SupaCsv_Acknowledge"]
                        || kept.RootElement.GetProperty("Help").GetString() != helpText
                        || !kept.RootElement.GetProperty("RowsUnchanged").GetBoolean()) throw new IOException("Completed original SUPA context source differs.");
                    completed.Add(key); continue;
                }
                var files = new SupaLayoutFiles(); var model = new ImportExportViewModel(store, text, new DateFormatter(local), local,
                    TimeProvider.System, fixture.GetRequiredService<HoldingStore>(), files, services.GetRequiredService<IAppInteraction>());
                var page = new ImportExportPage(model).WithFlowDirection(local); ProfileNativeReads.Add(page);
                var root = window.Page ?? throw new IOException("Independent original root missing.");
                await root.Navigation.PushModalAsync(new NavigationPage(page).WithFlowDirection(local), false);
                try
                {
                    await PlanEditorNativeReview.WaitAsync(() => model.Accounts.Count == 1 && page.IsLoaded && !model.IsBusy, ct);
                    window.Width = width;
                    for (var attempt = 0; attempt < 4; attempt++) { await Task.Delay(180, ct); if (Math.Abs(page.Width - width) <= 1) break; window.Width += width - page.Width; }
                    if (Math.Abs(page.Width - width) > 1) throw new IOException("Original SUPA page width differs.");
                    var scroll = VisualDescendants(page).OfType<ScrollView>().Single(); var reads = new List<object>();
                    if (!largeText)
                    {
                        var help = VisualDescendants(page).OfType<Vafadar.Maui.Controls.HelpButton>().Single(button => button.Topic == "SupaCsv");
                        await ScrollToViewIfNeededAsync(scroll, help); InvokeChoiceReview(VisualDescendants(help).OfType<Button>().Single(), reads);
                        await CheckSupaHelpAsync(window, helpText, text["Common_Ok"], Path.Combine(folder, key), reads, ct);
                    }
                    if (files.Picks != 0 || before != await ImportFinancialNativeReview.StoredAsync(fixture, ct))
                        throw new IOException("Original help selected a source or changed stored rows.");
                    if (finalHelp)
                    {
                        // Only the example prefix changed after the complete review; never repeat picker/consent/Cancel.
                        await File.WriteAllTextAsync(receipt, JsonSerializer.Serialize(new { Warning = text["SupaCsv_Warning"],
                            Consent = text["SupaCsv_Acknowledge"], Help = helpText, RowsUnchanged = true, FinancialWrites = 0, PickerCalls = files.Picks }), ct);
                        completed.Add(key); continue;
                    }
                    var pick = VisualDescendants(page).OfType<WrappingAction>().Single(action => ReferenceEquals(action.Command, model.PickSupaCommand));
                    await ScrollToViewIfNeededAsync(scroll, pick); InvokeChoiceReview(VisualDescendants(pick).OfType<Button>().Single(), reads);
                    await PlanEditorNativeReview.WaitAsync(() => !model.IsBusy, ct);
                    if (!model.HasPreview || model.ImportCount != 27 || model.BankLines.Count != 25 || files.Picks != 1 || model.CanRunImport)
                        throw new IOException("Original complete SUPA review/consent differs: " + model.ImportError);
                    var toggle = VisualDescendants(page).OfType<Switch>().Single(item => SemanticProperties.GetDescription(item) == model.BankPresetAcknowledgement);
                    await ScrollToViewIfNeededAsync(scroll, toggle);
                    if (toggle.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.ToggleSwitch nativeToggle
                        || FrameworkElementAutomationPeer.CreatePeerForElement(nativeToggle)?.GetPattern(PatternInterface.Toggle) is not IToggleProvider consent)
                        throw new IOException("Original SUPA consent has no native Toggle.");
                    consent.Toggle(); await Task.Delay(80, ct);
                    if (!model.BankAcknowledged || !model.CanRunImport) throw new IOException("Original SUPA consent did not enable current review.");
                    var chooser = page.FindByName<VerticalStackLayout>("BankPresetChooser"); var review = page.FindByName<VerticalStackLayout>("BankPresetReview");
                    var footer = largeText ? VisualDescendants(page).OfType<WrappingAction>().Where(action => Visible(action)
                        && (ReferenceEquals(action.Command, model.ImportCommand) || ReferenceEquals(action.Command, model.RefreshPreviewCommand))).ToArray() : [];
                    var footerLabels = footer.SelectMany(action => VisualDescendants(action).OfType<Label>()).ToHashSet();
                    var labels = VisualDescendants(chooser).OfType<Label>().Where(label => label.Text is { Length: > 1 })
                        .Concat(VisualDescendants(review).OfType<Label>().Where(label => label.Text == model.BankSourceText
                            || label.Text == model.BankPresetWarning || label.Text == model.BankPresetAcknowledgement)).Concat(footerLabels).Distinct().ToArray();
                    var index = 0; var scaledReads = new List<object>();
                    foreach (var label in labels)
                    {
                        if (!footerLabels.Contains(label)) await ScrollToViewIfNeededAsync(scroll, label);
                        await Task.Delay(60, ct); CheckCaption(label, reads);
                        await CaptureWindowAsync(window, Path.Combine(folder, $"{key}-{index++}.png"));
                        if (largeText) scaledReads.Add(await CheckSupaLargeCaptionAsync(label, scroll, systemScale,
                            footerLabels.Contains(label), Path.Combine(folder, $"{key}-{index - 1}-end.png"), window, reads, ct));
                    }
                    if (largeText)
                    {
                        if (footer.Length != 2 || scroll.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.ScrollViewer viewport || viewport.ViewportHeight < 44)
                            throw new IOException("Large original SUPA actions leave no reachable content viewport.");
                        var actions = footer.Select(action => VisualDescendants(action).OfType<Button>().Single()).ToArray();
                        foreach (var action in actions)
                            if (action.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native || !native.IsEnabled || native.ActualWidth < 44 || native.ActualHeight < 44)
                                throw new IOException("Original large-text SUPA action target is unavailable.");
                        if (before != await ImportFinancialNativeReview.StoredAsync(fixture, ct)) throw new IOException("Large-text review changed original rows.");
                        await File.WriteAllTextAsync(receipt, JsonSerializer.Serialize(new { Warning = model.BankPresetWarning,
                            Consent = model.BankPresetAcknowledgement, Help = helpText, RequestedFontScale = 2, PageWidth = page.Width,
                            NativeFonts = scaledReads, ContentViewportHeight = viewport.ViewportHeight, RowsUnchanged = true,
                            FinancialWrites = 0, UnchangedNativeDialogsNotReplayed = true, NativeImportNotInvoked = true }), ct);
                        completed.Add(key); continue;
                    }
                    if (language == "en" && theme == ThemeChoice.Light && width == 360)
                    {
                        var next = VisualDescendants(page).OfType<WrappingAction>().Single(action => ReferenceEquals(action.Command, model.BankNextCommand));
                        await ScrollToViewIfNeededAsync(scroll, next); InvokeChoiceReview(VisualDescendants(next).OfType<Button>().Single(), reads);
                        if (model.BankLines.Count != 2 || !model.BankLines.Last().Detail.Contains("original-026", StringComparison.Ordinal))
                            throw new IOException("Last original SUPA rows are not reachable.");
                        var previous = VisualDescendants(page).OfType<WrappingAction>().Single(action => ReferenceEquals(action.Command, model.BankPreviousCommand));
                        await ScrollToViewIfNeededAsync(scroll, previous); InvokeChoiceReview(VisualDescendants(previous).OfType<Button>().Single(), reads);
                        if (model.BankLines.Count != 25) throw new IOException("Original first SUPA page changed.");
                    }
                    var import = VisualDescendants(page).OfType<WrappingAction>().Single(action => ReferenceEquals(action.Command, model.ImportCommand));
                    await ScrollToViewIfNeededAsync(scroll, import); InvokeChoiceReview(VisualDescendants(import).OfType<Button>().Single(), reads);
                    if (window.Handler?.PlatformView is not Microsoft.UI.Xaml.Window { Content.XamlRoot: { } xamlRoot }) throw new IOException("Original native root missing.");
                    Microsoft.UI.Xaml.Controls.Button? cancel = null;
                    for (var attempt = 0; attempt < 200 && cancel is null; attempt++)
                    {
                        cancel = VisualTreeHelper.GetOpenPopupsForXamlRoot(xamlRoot).SelectMany(popup => SupaNativeNodes(popup.Child))
                            .OfType<Microsoft.UI.Xaml.Controls.Button>().FirstOrDefault(button => FrameworkElementAutomationPeer.CreatePeerForElement(button)?.GetName() == text["Common_Cancel"]);
                        if (cancel is null) await Task.Delay(25, ct);
                    }
                    if (cancel is null || FrameworkElementAutomationPeer.CreatePeerForElement(cancel)?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider close)
                        throw new IOException("Original native SUPA confirmation Cancel missing.");
                    await CaptureWindowAsync(window, Path.Combine(folder, key + "-confirmation.png")); close.Invoke();
                    await PlanEditorNativeReview.WaitAsync(() => !model.IsBusy, ct);
                    if (!model.CanRunImport || before != await ImportFinancialNativeReview.StoredAsync(fixture, ct)) throw new IOException("Original Cancel changed source finance/consent.");
                    await using var output = new FileStream(receipt, FileMode.CreateNew, FileAccess.Write);
                    await JsonSerializer.SerializeAsync(output, new { Language = language, Theme = theme.ToString(), PageWidth = page.Width,
                        Warning = model.BankPresetWarning, Consent = model.BankPresetAcknowledgement, Help = helpText, NativeReads = reads.Count, OwnPNGs = index + 3,
                        RowsUnchanged = true, FinancialWrites = 0, NativeConfirmationCancel = true, NativeFilePicker = false }, cancellationToken: ct);
                    completed.Add(key);
                }
                finally { await root.Navigation.PopModalAsync(false); }
            }
            await File.WriteAllTextAsync(Path.Combine(folder, finalHelp ? "final-help-complete.json" : "complete.json"), JsonSerializer.Serialize(new { Contexts = completed,
                ProcessId = Environment.ProcessId, RowsUnchanged = before == await ImportFinancialNativeReview.StoredAsync(fixture, ct), FinancialWrites = 0 }), ct);
        }
        finally { window.Width = oldWidth; window.Height = oldHeight; await SetStoreThemeAsync(app, themes, oldTheme); await SetLanguageAsync(app, local, oldLanguage); }
    }

    /// <summary>Proves actual double-size native text and reaches the final line of a tall original block.</summary>
    private static async Task<object> CheckSupaLargeCaptionAsync(Label label, ScrollView scroll, double systemScale,
        bool outsideViewport, string finalImage, Window window, List<object> reads, CancellationToken ct)
    {
        if (label.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock native || label.FontSize <= 0
            || !label.FontAutoScalingEnabled || Math.Abs(native.FontSize * systemScale / label.FontSize - 2) > 0.05)
            throw new IOException("Original SUPA native font did not receive the explicit double-size review.");
        reads.Add(native);
        var reachedEnd = false;
        if (!outsideViewport && scroll.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.ScrollViewer viewport
            && native.ActualHeight > viewport.ViewportHeight)
        {
            var position = native.TransformToVisual(viewport).TransformPoint(new Windows.Foundation.Point(0, 0));
            var wanted = Math.Clamp(viewport.VerticalOffset + position.Y + native.ActualHeight - viewport.ViewportHeight, 0, viewport.ScrollableHeight);
            NativeScrollProvider(viewport).SetScrollPercent(-1, wanted / viewport.ScrollableHeight * 100);
            await Task.Delay(180, ct);
            var bottom = native.TransformToVisual(viewport).TransformPoint(new Windows.Foundation.Point(0, native.ActualHeight));
            if (bottom.Y < 1 || bottom.Y > viewport.ViewportHeight + 2) throw new IOException("Original tall SUPA text end remains unreachable.");
            reachedEnd = true; await CaptureWindowAsync(window, finalImage);
        }
        return new { Text = label.Text, VirtualFontSize = label.FontSize, NativeFontSize = native.FontSize,
            SystemScale = systemScale, ActualFontRatio = native.FontSize * systemScale / label.FontSize,
            native.ActualWidth, native.ActualHeight, OutsideScrollViewport = outsideViewport, TallBlockEndReached = reachedEnd };
    }

    /// <summary>Original supplier-contract fictitious UTF-8 source; no external picker, bank or financial writer.</summary>
    private sealed class SupaLayoutFiles : IDataFileInteraction
    {
        internal int Picks;
        public Task<PickedDataFile?> PickCsvAsync(string title) => throw new IOException("No generic source belongs to SUPA review.");
        public Task<PickedDataFile?> PickBankCsvAsync(string title, BankCsvEncoding encoding)
        {
            if (encoding != BankCsvEncoding.Utf8) throw new IOException("Original SUPA source must use UTF-8.");
            Picks++; var rows = new List<IReadOnlyList<string>> { new[] { "Id", "AcctId", "BookgDt", "ValDt", "Amt", "AmtCcy", "CdtDbtInd", "BookgSts", "BookgTxt", "RmtdNm", "RmtInf" } };
            for (var index = 0; index < 27; index++) rows.Add(new[] { "original-" + index.ToString("D3", System.Globalization.CultureInfo.InvariantCulture),
                "native-source-0516", "2026-10-24", "2026-10-25", index == 0 ? "100.00" : "12.34", "EUR", index == 0 ? "CRDT" : "DBIT", "BOOK",
                "Original fictitious cashflow", "Fictitious Müller / نمونه", "Original, quoted \"reference\"" });
            return Task.FromResult<PickedDataFile?>(new("fictitious-supa4.csv", Csv.Write(rows)));
        }
        public Task<bool> ConfirmExportAsync(string reason) => throw new IOException("No export belongs to SUPA review.");
        public Task ShareCsvAsync(string name, string title, string text) => throw new IOException("No sharing belongs to SUPA review.");
    }
    private static IEnumerable<Microsoft.UI.Xaml.DependencyObject> SupaNativeNodes(Microsoft.UI.Xaml.DependencyObject node)
    {
        yield return node;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(node); index++) foreach (var child in SupaNativeNodes(VisualTreeHelper.GetChild(node, index))) yield return child;
    }

    /// <summary>Reads the original complete help body and closes it through its own native OK pattern.</summary>
    private static async Task CheckSupaHelpAsync(Window window, string expected, string ok, string prefix, List<object> reads, CancellationToken ct)
    {
        if (window.Handler?.PlatformView is not Microsoft.UI.Xaml.Window { Content.XamlRoot: { } root }) throw new IOException("Original help root missing.");
        Microsoft.UI.Xaml.Controls.TextBlock? body = null; Microsoft.UI.Xaml.Controls.Button? close = null;
        for (var attempt = 0; attempt < 200 && (body is null || close is null); attempt++)
        {
            var nodes = VisualTreeHelper.GetOpenPopupsForXamlRoot(root).SelectMany(popup => SupaNativeNodes(popup.Child)).ToArray();
            body = nodes.OfType<Microsoft.UI.Xaml.Controls.TextBlock>().FirstOrDefault(block => block.Text == expected && block.ActualWidth > 0);
            close = nodes.OfType<Microsoft.UI.Xaml.Controls.Button>().FirstOrDefault(button => FrameworkElementAutomationPeer.CreatePeerForElement(button)?.GetName() == ok);
            if (body is null || close is null) await Task.Delay(25, ct);
        }
        if (body is null || close is null || body.ActualHeight <= 0) throw new IOException("Complete original SUPA help body/OK not realized.");
        reads.Add(body); reads.Add(close); await CaptureWindowAsync(window, prefix + "-help-top.png");
        Microsoft.UI.Xaml.Controls.ScrollViewer? viewport = null;
        for (Microsoft.UI.Xaml.DependencyObject? parent = VisualTreeHelper.GetParent(body); parent is not null; parent = VisualTreeHelper.GetParent(parent))
            if (parent is Microsoft.UI.Xaml.Controls.ScrollViewer scroll) { viewport = scroll; break; }
        if (viewport is not null && viewport.ScrollableHeight > 1)
        {
            if (FrameworkElementAutomationPeer.CreatePeerForElement(viewport)?.GetPattern(PatternInterface.Scroll) is not IScrollProvider scrolling)
                throw new IOException("Original long help has no native Scroll pattern.");
            // UI Automation uses -1 to preserve the horizontal position while revealing the original final example.
            scrolling.SetScrollPercent(-1, 100); await Task.Delay(100, ct);
            if (viewport.VerticalOffset + 1 < viewport.ScrollableHeight) throw new IOException("Original help example remains unreachable.");
        }
        await CaptureWindowAsync(window, prefix + "-help-end.png");
        if (FrameworkElementAutomationPeer.CreatePeerForElement(close)?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider invoke)
            throw new IOException("Original help OK has no native Invoke pattern.");
        invoke.Invoke();
        for (var attempt = 0; attempt < 40; attempt++)
        {
            if (!VisualTreeHelper.GetOpenPopupsForXamlRoot(root).SelectMany(popup => SupaNativeNodes(popup.Child))
                .OfType<Microsoft.UI.Xaml.Controls.TextBlock>().Any(block => block.Text == expected)) return;
            await Task.Delay(25, ct);
        }
        throw new IOException("Original help did not close; do not open another source.");
    }
}
#endif
