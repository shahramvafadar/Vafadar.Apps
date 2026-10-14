#if DEBUG && WINDOWS
using System.Resources;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Core.Settings;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Settings;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Reviews the original shared view and native selection against memory-only display preferences.</summary>
    private static async Task ReviewHolidayCatalogAsync(App app, IServiceProvider services, string folder)
    {
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.storageacceptance") throw new IOException("Independent holiday installation required.");
        var before = await ProtectedRowsFingerprintAsync(services);
        var appLocalization = services.GetRequiredService<ILocalizationService>();
        var themes = services.GetRequiredService<Presentation.ThemeService>();
        var observations = new List<object>();
        foreach (var theme in new[] { Presentation.ThemeChoice.Light, Presentation.ThemeChoice.Dark })
        foreach (var language in new[] { "en", "fa", "de" })
        foreach (var width in new[] { 360, 412, 1280 })
        {
            themes.Set(theme); await SetLanguageAsync(app, appLocalization, language);
            app.Windows[0].Width = width; app.Windows[0].Height = 820;
            var memory = new InMemorySettingsStore(); var translator = new Translator();
            var options = new LocalizationOptions();
            options.Resources.Add(new ResourceManager("Vafadar.Zanance.App.Resources.Strings.AppResources", typeof(App).Assembly));
            var local = new LocalizationService(options, memory, translator);
            local.Initialize(); local.SetLanguage(AppLanguages.All.Single(value => value.CultureName == language)); local.SetFormattingCulture("de-DE"); local.SetFirstDayOfWeek(DayOfWeek.Thursday); local.SetRegion("CA");
            var vm = new RegionalPreferencesViewModel(local, translator, TimeProvider.System); vm.Refresh();
            var view = new RegionalPreferencesView { BindingContext = vm };
            var page = new ContentPage { Content = new ScrollView { Content = view, Padding = 16 }, FlowDirection = local.CurrentLanguage.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
            page.SetDynamicResource(VisualElement.BackgroundColorProperty, "PageBackground");
            var shell = app.Windows[0].Page as AppShell ?? throw new IOException("Original Shell unavailable.");
            await shell.Navigation.PushAsync(page); await Task.Delay(300);
            var picker = VisualDescendants(page).OfType<Picker>().Single(p => ReferenceEquals(p.ItemsSource,vm.HolidayRegions));
            var nativeScroll = VisualDescendants(page).OfType<ScrollView>().Single().Handler?.PlatformView as Microsoft.UI.Xaml.FrameworkElement;
            var scrollPeer = nativeScroll is null ? null : Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(nativeScroll);
            if (scrollPeer?.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Scroll) is Microsoft.UI.Xaml.Automation.Provider.IScrollProvider scrolling && scrolling.VerticallyScrollable)
                scrolling.SetScrollPercent(-1,100);
            await Task.Delay(100);
            var hint = VisualDescendants(view).OfType<Label>().Single(label => label.Text == vm.HolidayHint);
            CheckCaption(hint,observations);
            if (picker.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.ComboBox combo || combo.ActualHeight < 44 || combo.SelectedIndex != Array.FindIndex(vm.HolidayRegions,option => option.Code == "CA"))
                throw new IOException("Original country picker did not publish its selected state/target.");
            if (local.FormattingCultureName != "de-DE" || local.FirstDayOfWeek != DayOfWeek.Thursday)
                throw new IOException("Initial holiday country changed independent display preferences.");
            await CaptureWindowAsync(app.Windows[0],Path.Combine(folder,$"{language}-{theme}-{width}-holidays.png"));
            await shell.Navigation.PopAsync();
        }
        if (before != await ProtectedRowsFingerprintAsync(services)) throw new IOException("Holiday review wrote finance.");
        File.WriteAllText(Path.Combine(folder,"holiday-catalog-proof.json"),JsonSerializer.Serialize(new { Contexts=18, NativeSelectionsPerformedByThisRoute=0, NativeSelectionScope="Layout-only route; actual selections require separate native evidence",
            CompleteScopeCaptions=true, IndependentFormatsAndWeekStart=true, MemoryPreferencesOnly=true, FinancialRowsUnchanged=true }));
    }

}
#endif
