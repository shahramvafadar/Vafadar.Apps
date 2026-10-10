#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Settings;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Reports;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    // AT-111 compares the actual bound Settings publication with complete fictitious history, without saving estimates.
    private static async Task ReviewSettingsSuggestionAsync(IServiceProvider services, SettingsPage page, string folder, string language)
    {
        var vm = (SettingsViewModel)page.BindingContext;
        await vm.LoadAsync();
        var store = services.GetRequiredService<ZananceStore>();
        var localization = services.GetRequiredService<ILocalizationService>();
        var translator = services.GetRequiredService<Translator>();
        var time = services.GetRequiredService<TimeProvider>();
        var settings = await store.GetSettingsAsync();
        var accounts = await store.GetAccountsAsync();
        var entries = await store.GetEntriesAsync();
        var before = JsonSerializer.Serialize(new { Settings = settings, Accounts = accounts, Entries = entries });
        var currency = settings.EssentialEstimateCurrency ?? settings.DefaultCurrencyCode;
        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        var calendar = Calendars.ToPeriod(localization.CurrentCalendar);
        var expected = LiquidityCalculator.SuggestPerDay(accounts, entries, currency, today, calendar, settings.MonthStartDay);
        var text = expected is { } value and > 0
            ? translator.Format("Settings_EssentialSuggestion", MoneyText.Format(value, currency, localization.CurrentCulture)) : null;
        if (vm.EssentialSuggestionText != text || !vm.Loading.IsReady
            || page.FindByName<ScrollView>("SettingsContent") is not { IsVisible: true, IsEnabled: true })
        { throw new InvalidOperationException("The bound complete Settings suggestion differs from full history."); }
        var draft = vm.EssentialText; var period = vm.EssentialPeriodIndex;
        try
        {
            vm.EssentialText = "271.09"; vm.EssentialPeriodIndex = (int)EstimatePeriod.Week;
            await vm.LoadAsync();
            if (vm.EssentialText != "271.09" || vm.EssentialPeriodIndex != (int)EstimatePeriod.Week
                || vm.EssentialSuggestionText != text || !vm.Loading.IsReady)
            { throw new InvalidOperationException("A fresh suggestion reload lost the unsaved estimate or complete publication."); }
            var after = JsonSerializer.Serialize(new { Settings = await store.GetSettingsAsync(),
                Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync() });
            if (before != after) { throw new InvalidOperationException("Reading a suggestion changed fictitious stored data."); }
            File.WriteAllText(Path.Combine(folder, $"{language}-settings-suggestion-proof.json"), JsonSerializer.Serialize(new
            { CompleteHistoryEntries = entries.Count, FullHistorySuggestion = expected, BoundCaptionMatches = true,
                ReadyForm = true, RawUnsavedEstimatePreserved = true, CompleteStoredDataUnchanged = true }));
        }
        finally { vm.EssentialText = draft; vm.EssentialPeriodIndex = period; }
    }
}
#endif
