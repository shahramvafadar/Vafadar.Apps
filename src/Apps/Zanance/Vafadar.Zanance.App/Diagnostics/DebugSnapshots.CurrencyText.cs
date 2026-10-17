#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Media;
using Syncfusion.Maui.Inputs;
using Vafadar.Localization;
using Vafadar.Maui.Localization;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data;
using NativeNode = Microsoft.UI.Xaml.DependencyObject;
using NativeElement = Microsoft.UI.Xaml.FrameworkElement;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Observes the production currency input in an unsaved original account form, never desktop input or financial writes.</summary>
    private static async Task ReviewCurrencyTextAsync(App app, IServiceProvider services, string folder)
    {
        var store = services.GetRequiredService<ZananceStore>();
        if ((await store.GetAccountsAsync()).Count == 0)
            await store.SaveAccountAsync(new Core.Accounts.Account { Name = "Currency review EUR", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) });
        async Task<string> StoredAsync() => JsonSerializer.Serialize(new
        { Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync() });
        var before = await StoredAsync();
        foreach (var language in new[] { "en", "fa", "de" })
        foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        foreach (var width in new[] { 360d, 412d, 1280d })
        {
            var destination = Path.Combine(folder, language + "-" + theme + "-" + width);
            Directory.CreateDirectory(destination);
            app.Windows[0].Width = width;
            await Task.Delay(250);
            await ReviewCurrencyTextCaseAsync(app, services, destination, language, theme);
            if (before != await StoredAsync()) throw new IOException("Currency review changed original account or entry rows.");
        }
        await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new
        { Contexts = 18, CompleteAccountAndEntryRowsUnchanged = true, NoSave = true, NativeTextScaleStress = 2,
            CommonListOnly = Environment.GetEnvironmentVariable("VAFADAR_CURRENCY_LIST_ONLY") == "1" }));
    }

    /// <summary>Measures one original native currency form and invokes its real list buttons.</summary>
    private static async Task ReviewCurrencyTextCaseAsync(App app, IServiceProvider services, string folder, string language, ThemeChoice theme)
    {
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.currencyreview"
            || !Path.GetFullPath(folder).StartsWith(Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(),
                "artifacts", "currency-text-d302")) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Independent owned currency layout required.");
        if (File.Exists(Path.Combine(folder, "observed.json"))) throw new IOException("Preserve the completed original observation.");
        var local = services.GetRequiredService<ILocalizationService>();
        await SetLanguageAsync(app, local, language);
        services.GetRequiredService<ThemeService>().Set(theme);
        var model = new AccountFormModel(services.GetRequiredService<Translator>(), services.GetRequiredService<TimeProvider>())
        { Name = "Fictitious currency draft", OpeningText = "619.07", CurrencyCode = "EUR" };
        var form = new AccountFormView { BindingContext = model };
        var scroll = new ScrollView { Content = form, Padding = 16 };
        var page = new ContentPage { Content = scroll }.WithFlowDirection(local);
        var root = app.Windows[0].Page ?? throw new IOException("Original independent root unavailable.");
        await root.Navigation.PushModalAsync(new NavigationPage(page).WithFlowDirection(local), false);
        try
        {
            var box = VisualDescendants(form).OfType<SfComboBox>().Single();
            await ScrollToViewIfNeededAsync(scroll, box);
            await Task.Delay(200);
            if (box.Handler?.PlatformView is not NativeElement native) throw new IOException("Actual currency native input unavailable.");
            // Win2D does not schedule its normal canvas draw for a hidden review window. Invoke the original
            // drawable into an owned bitmap, rather than inventing coordinates or activating the owner's desktop.
            using (var bitmap = new Microsoft.Graphics.Canvas.CanvasRenderTarget(
                Microsoft.Graphics.Canvas.CanvasDevice.GetSharedDevice(), (float)box.Width, (float)box.Height, 96))
            {
                using (var session = bitmap.CreateDrawingSession())
                {
                    var canvas = new Microsoft.Maui.Graphics.Win2D.W2DCanvas
                    { Session = session, CanvasSize = new((float)box.Width, (float)box.Height) };
                    ((Microsoft.Maui.Graphics.IDrawable)box).Draw(canvas,
                        new Microsoft.Maui.Graphics.RectF(0, 0, (float)box.Width, (float)box.Height));
                }
                using var output = File.Create(Path.Combine(folder, "original-drawable.png"));
                await bitmap.SaveAsync(output.AsRandomAccessStream(), Microsoft.Graphics.Canvas.CanvasBitmapFileFormat.Png);
            }
            native.UpdateLayout();
            await Task.Delay(100);
            var rows = new List<object>();
            foreach (var item in Nodes(native).OfType<NativeElement>())
            {
                var peer = FrameworkElementAutomationPeer.CreatePeerForElement(item);
                var origin = item.TransformToVisual(native).TransformPoint(default);
                rows.Add(new { Type = item.GetType().FullName, item.Name, item.ActualWidth, item.ActualHeight,
                    X = origin.X, Y = origin.Y,
                    Text = item is Microsoft.UI.Xaml.Controls.TextBox input ? input.Text : item is Microsoft.UI.Xaml.Controls.TextBlock text ? text.Text : null,
                    FontSize = item is Microsoft.UI.Xaml.Controls.TextBox entry ? entry.FontSize : item is Microsoft.UI.Xaml.Controls.TextBlock label ? label.FontSize : 0,
                    Family = item is Microsoft.UI.Xaml.Controls.TextBlock glyph ? glyph.FontFamily.Source : null,
                    AccessibleName = peer?.GetName(), Expand = peer?.GetPattern(PatternInterface.ExpandCollapse) is IExpandCollapseProvider,
                    Value = peer?.GetPattern(PatternInterface.Value) is Microsoft.UI.Xaml.Automation.Provider.IValueProvider });
            }
            await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, "selected.png"));
            await File.WriteAllTextAsync(Path.Combine(folder, "observed.json"), JsonSerializer.Serialize(new
            { box.FontSize, box.Height, box.Width, Rows = rows, model.CurrencyCode, model.Name, model.OpeningText, NoSave = true }));
            var action = VisualDescendants(box.DropDownButtonSettings.View).OfType<Button>().Single();
            if (action.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button nativeAction
                || FrameworkElementAutomationPeer.CreatePeerForElement(nativeAction)?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider invoke)
                throw new IOException("The original currency list action has no native Invoke pattern.");
            invoke.Invoke();
            await Task.Delay(250);
            if (!box.IsDropDownOpen) throw new IOException("Native currency list invocation did not open its original list.");
            var popupRows = new List<object>();
            foreach (var popup in VisualTreeHelper.GetOpenPopupsForXamlRoot(native.XamlRoot))
            foreach (var item in Nodes(popup.Child).OfType<NativeElement>())
            {
                var peer = FrameworkElementAutomationPeer.CreatePeerForElement(item);
                popupRows.Add(new { Type = item.GetType().FullName, item.ActualWidth, item.ActualHeight,
                    Text = item is Microsoft.UI.Xaml.Controls.TextBlock text ? text.Text : null,
                    Name = peer?.GetName(), Select = peer?.GetPattern(PatternInterface.SelectionItem) is ISelectionItemProvider });
            }
            await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, "opened.png"));
            await File.WriteAllTextAsync(Path.Combine(folder, "opened.json"), JsonSerializer.Serialize(popupRows));
            // Choose an actual realized original row through its native action, then restore the draft.
            var choice = VisualTreeHelper.GetOpenPopupsForXamlRoot(native.XamlRoot)
                .SelectMany(popup => Nodes(popup.Child)).OfType<Microsoft.UI.Xaml.Controls.Button>()
                .FirstOrDefault(button => FrameworkElementAutomationPeer.CreatePeerForElement(button)?.GetName() == "GBP")
                ?? throw new IOException("Original GBP row action unavailable.");
            if (FrameworkElementAutomationPeer.CreatePeerForElement(choice)?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider select)
                throw new IOException("Original currency row lacks native Invoke.");
            select.Invoke(); await Task.Delay(100);
            if (model.CurrencyCode != "GBP" || box.IsDropDownOpen) throw new IOException("Original currency selection failed.");
            model.CurrencyCode = "EUR";
        }
        finally { await root.Navigation.PopModalAsync(false); }
        if (Environment.GetEnvironmentVariable("VAFADAR_CURRENCY_LIST_ONLY") == "1") return;
        var store = services.GetRequiredService<ZananceStore>();
        if ((await store.GetAccountsAsync()).Count == 0)
            await store.SaveAccountAsync(new Core.Accounts.Account { Name = "Currency review EUR", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) });
        foreach (var kind in new[] { "rates", "goal", "entry" })
        {
            ContentPage constrained;
            if (kind == "rates") constrained = services.GetRequiredService<Features.Rates.RatesPage>();
            else if (kind == "goal")
            {
                var vm = services.GetRequiredService<Features.Goals.GoalEditorViewModel>();
                await vm.EnsureLoadedAsync(); vm.TypeIndex = 1; vm.AmountText = "619.07";
                constrained = new Features.Goals.GoalEditorPage(vm, local);
            }
            else
            {
                var vm = services.GetRequiredService<Features.Entries.EntryEditorViewModel>();
                ((IQueryAttributable)vm).ApplyQueryAttributes(new Dictionary<string, object>
                { ["account"] = (await store.GetAccountsAsync()).Single().Id.ToString() });
                for (var wait = 0; wait < 400 && vm.Account is null; wait++) await Task.Delay(25);
                if (vm.Account is null) throw new IOException("Independent account reference failed to load.");
                await Task.Delay(100);
                vm.ShowDetails = true; vm.ForeignEnabled = true; vm.ForeignCurrency = "EUR"; vm.ForeignAmountText = "619.07";
                constrained = new Features.Entries.EntryEditorPage(vm);
            }
            constrained.WithFlowDirection(local);
            await root.Navigation.PushModalAsync(new NavigationPage(constrained).WithFlowDirection(local), false);
            try
            {
                await Task.Delay(250);
                var viewport = VisualDescendants(constrained).OfType<ScrollView>().First();
                var reports = new List<object>();
                foreach (var box in VisualDescendants(constrained).OfType<SfComboBox>().Where(Visible))
                {
                    await ScrollToViewIfNeededAsync(viewport, box); await Task.Delay(100);
                    if (box.Handler?.PlatformView is not NativeElement actual) throw new IOException("Original currency input not loaded.");
                    var input = Nodes(actual).OfType<Microsoft.UI.Xaml.Controls.TextBox>().Single();
                    var textViewport = Nodes(input).OfType<Microsoft.UI.Xaml.Controls.ScrollContentPresenter>().First();
                    if (textViewport.ActualWidth < 95 || input.FontSize < 29 || box.Height < 44)
                        throw new IOException("Original currency code lacks its complete scalable input width.");
                    reports.Add(new { box.Width, box.Height, input.Text, input.FontSize, input.ActualWidth, TextViewportWidth = textViewport.ActualWidth });
                    await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, kind + "-" + reports.Count + ".png"));
                }
                if (reports.Count != (kind == "rates" ? 2 : 1)) throw new IOException("Original constrained currency fields not all visible.");
                await File.WriteAllTextAsync(Path.Combine(folder, kind + "-original-layout.json"), JsonSerializer.Serialize(reports));
            }
            finally { await root.Navigation.PopModalAsync(false); }
        }
        static IEnumerable<NativeNode> Nodes(NativeNode node)
        {
            yield return node;
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(node); index++)
                foreach (var child in Nodes(VisualTreeHelper.GetChild(node, index))) yield return child;
        }
    }
}
#endif
