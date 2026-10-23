using System.Text.Json;
using Android.Views;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Diagnostics;
using Vafadar.Zanance.App.Features.Forecast;
using Vafadar.Zanance.App.Features.Reports;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Checks actual retained Shell-tab activation against changed original protected fixture facts.</summary>
    private static async Task<object> ReviewForecastReturnAsync(App app, IServiceProvider services, string root, string comparison, CancellationToken ct)
    {
        if (Directory.Exists(root) || RedirectedAncestors(root).Length != 0)
            throw new ReviewCheckException("Fresh native forecast-return source required; retain accepted actions.");
        if (services.GetRequiredService<AppLockService>() is { IsEnabled: true } or { IsLocked: true })
            throw new ReviewCheckException("This independent cohort does not configure or bypass credentials.");
        Directory.CreateDirectory(root);
        var profiles = services.GetRequiredService<ProfileService>();
        if (profiles.All.Any(profile => profile.Name?.StartsWith("Forecast return ", StringComparison.Ordinal) == true))
            throw new ReviewCheckException("Retain an existing partial owned profile instead of creating another source.");
        var originalProfile = profiles.Current;
        var originalCount = profiles.All.Count;
        var original = await ImportFinancialNativeReview.StoredAsync(services, ct);
        var owned = await profiles.CreateReviewedAsync(profiles.ReadWorkReview(), "Forecast return " + comparison[..8]);
        await File.WriteAllTextAsync(Path.Combine(root, "owned-profile.json"), JsonSerializer.Serialize(new { owned.Id, owned.Name }), ct);
        if (!await profiles.SwitchToAsync(owned)) throw new ReviewCheckException("Original protected fixture switch refused.");
        var store = services.GetRequiredService<ZananceStore>();
        var plans = services.GetRequiredService<PlanStore>();
        var local = services.GetRequiredService<ILocalizationService>();
        var translator = services.GetRequiredService<Translator>();
        var today = DateOnly.FromDateTime(services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime);
        if ((await store.GetAccountsAsync(cancellationToken: ct)).Count != 0 || (await store.GetEntriesAsync(cancellationToken: ct)).Count != 0)
            throw new ReviewCheckException("New original protected source is not empty; never reseed.");
        var account = new Account { Name = "Native forecast return", CurrencyCode = "EUR", OpeningDate = today, OpeningBalance = 10000 };
        if (!await store.SaveAccountAsync(account, ct)) throw new ReviewCheckException("Original fixture account refused.");
        var plan = new Schedule { Name = "Native retained plan", AccountId = account.Id, Amount = 2000,
            Rule = new RecurrenceRule { Frequency = Frequency.Once, Start = today.AddDays(1) } };
        await plans.SaveScheduleAsync(plan, ct);
        await store.UpdateSettingsAsync(settings => { settings.OnboardingCompleted = true; settings.Mode = ExperienceMode.Advanced; }, ct);
        var workspace = new ForecastWorkspace(services.GetRequiredService<ForecastWorkStore>(), store, translator);
        await workspace.LoadAsync(1, PeriodCalendar.Gregorian, today);
        await workspace.SaveAsync("Native retained snapshot", null, "0.1.0");
        var saved = (await store.GetForecastSnapshotsAsync(ct)).Single();
        var immutable = JsonSerializer.Serialize(saved);
        await File.WriteAllTextAsync(Path.Combine(root, "source-created.json"), JsonSerializer.Serialize(new { Account = account.Id, Plan = plan.Id, Snapshot = saved.Id }), ct);
        var shell = app.OpenReviewShell();
        await AwaitRootHomeAsync(ct);
        var navigation = new List<string>();
        void Navigated(object? sender, ShellNavigatedEventArgs args) => navigation.Add(args.Source + ":" + shell.CurrentPage?.GetType().Name);
        shell.Navigated += Navigated;
        async Task Ready(Func<bool> predicate)
        {
            for (var attempt = 0; attempt < 300; attempt++)
            {
                if (predicate()) return;
                await Task.Delay(50, ct);
            }
            throw new ReviewCheckException("Original retained page did not publish current facts; preserve same native source.");
        }
        IEnumerable<Android.Views.View> NativeViews(Android.Views.View view)
        {
            yield return view;
            if (view is ViewGroup group)
                for (var index = 0; index < group.ChildCount; index++)
                    foreach (var child in NativeViews(group.GetChildAt(index)!)) yield return child;
        }
        async Task TapTabAsync(string caption)
        {
            await AwaitOriginalMoneyWindowAsync(ct);
            var rootView = Platform.CurrentActivity?.Window?.DecorView ?? throw new ReviewCheckException("Original native window missing.");
            var target = NativeViews(rootView).Single(view => view.ContentDescription == caption && view.Clickable && view.IsShown && view.Enabled);
            if (!target.HasWindowFocus || !target.PerformClick()) throw new ReviewCheckException("Actual native tab target refused.");
            await Task.Yield();
        }
        async Task VisibleAmountAsync(Page page, string text)
        {
            var labels = PlanEditorNativeReview.Views(page).OfType<Label>().Where(label => label.Text == text).ToArray();
            if (labels.Length == 0) throw new ReviewCheckException("Original current amount label is absent.");
            var label = labels[^1];
            var viewport = PlanEditorNativeReview.Views(page).OfType<ScrollView>().Single(view => view.Orientation == ScrollOrientation.Vertical);
            await viewport.ScrollToAsync(label, ScrollToPosition.Center, animated: false);
            await Ready(() => label.Handler?.PlatformView is Android.Widget.TextView { IsShown: true, HasWindowFocus: true, Height: > 0 });
            if (label.Handler?.PlatformView is not Android.Widget.TextView native || native.Text != text)
                throw new ReviewCheckException("Native amount does not match original published current facts.");
        }
        try
        {
            await shell.GoToAsync("//insights/forecast");
            await Ready(() => shell.CurrentPage is ForecastPage { BindingContext: ForecastViewModel vm } && vm.Loading.IsReady);
            var forecastPage = (ForecastPage)shell.CurrentPage;
            var forecast = (ForecastViewModel)forecastPage.BindingContext;
            await shell.GoToAsync(AppRoutes.SnapshotRoute, new Dictionary<string, object> { ["id"] = saved.Id });
            await Ready(() => shell.CurrentPage is SnapshotPage { BindingContext: SnapshotViewModel vm } && vm.Loading.IsReady);
            var snapshotPage = (SnapshotPage)shell.CurrentPage;
            var snapshot = (SnapshotViewModel)snapshotPage.BindingContext;
            string Money(long value) => MoneyText.Format(value, "EUR", local.CurrentCulture, showPlus: true);
            if (snapshot.Parts.Single(part => part.Label == translator["Snapshot_Difference"]).Amount != Money(0))
                throw new ReviewCheckException("Original unchanged comparison must start at zero.");
            await TapTabAsync(translator["Tab_Home"]);
            await Ready(() => shell.CurrentPage is Features.Home.HomePage);
            if (!(await store.SaveEntryAsync(new LedgerEntry { AccountId = account.Id, Date = today, Kind = EntryKind.Expense,
                    Amount = 1000, Title = "Native later ten" }, ct)).Succeeded) throw new ReviewCheckException("Original isolated later entry refused.");
            await File.WriteAllTextAsync(Path.Combine(root, "later-entry-accepted.json"), "{\"ExpenseMinor\":1000}", ct);
            await TapTabAsync(translator["Tab_Insights"]);
            await Ready(() => ReferenceEquals(shell.CurrentPage, snapshotPage) && snapshotPage.PendingPublication.IsCompletedSuccessfully
                && snapshot.Loading.IsReady && snapshot.Parts.Single(part => part.Label == translator["Snapshot_Difference"]).Amount == Money(-1000));
            await VisibleAmountAsync(snapshotPage, Money(-1000));
            await File.WriteAllTextAsync(Path.Combine(root, "snapshot-return-accepted.json"), JsonSerializer.Serialize(new
            { SameNativePage = true, CurrentDifference = -1000, FinancialReplays = 0, OriginalSnapshotUnchanged = immutable == JsonSerializer.Serialize((await store.GetForecastSnapshotsAsync(ct)).Single()) }), ct);
            await shell.GoToAsync("..");
            await Ready(() => ReferenceEquals(shell.CurrentPage, forecastPage) && forecast.Loading.IsReady);
            forecast.Workspace.Scenario.SetExcluded(plan.Id, today.AddDays(1), true);
            await forecastPage.RefreshForNavigationAsync();
            if (!forecast.HasScenario || forecast.Workspace.Forecasts.Single().EndBalance != 9000)
                throw new ReviewCheckException("Original fictional what-if draft was not prepared.");
            await TapTabAsync(translator["Tab_Home"]);
            await Ready(() => shell.CurrentPage is Features.Home.HomePage);
            if (!(await store.SaveEntryAsync(new LedgerEntry { AccountId = account.Id, Date = today, Kind = EntryKind.Income,
                    Amount = 700, Title = "Native later seven" }, ct)).Succeeded) throw new ReviewCheckException("Original isolated later income refused.");
            await File.WriteAllTextAsync(Path.Combine(root, "later-income-accepted.json"), "{\"IncomeMinor\":700}", ct);
            await TapTabAsync(translator["Tab_Insights"]);
            await Ready(() => ReferenceEquals(shell.CurrentPage, forecastPage) && forecastPage.PendingPublication.IsCompletedSuccessfully
                && forecast.Loading.IsReady && forecast.Workspace.Forecasts.Single().EndBalance == 9700 && forecast.HasScenario);
            await VisibleAmountAsync(forecastPage, forecast.Cards.Single().EndText);
            if (immutable != JsonSerializer.Serialize((await store.GetForecastSnapshotsAsync(ct)).Single())
                || (await store.GetEntriesAsync(cancellationToken: ct)).Count != 2 || (await plans.GetSchedulesAsync(ct)).Count != 1)
                throw new ReviewCheckException("Retained tab refresh changed original source identities or saved path.");
            await File.WriteAllTextAsync(Path.Combine(root, "forecast-return-accepted.json"), JsonSerializer.Serialize(new
            { SameNativePage = true, CurrentEndMinor = 9700, WhatIfRetained = true, FinancialReplays = 0 }), ct);
        }
        finally { shell.Navigated -= Navigated; }
        if (!await profiles.SwitchToAsync(originalProfile)) throw new ReviewCheckException("Original independent profile return refused.");
        await profiles.DeleteAsync(owned);
        app.ShowCurrentProfile();
        await Ready(() => profiles.Current.Id == originalProfile.Id && profiles.All.Count == originalCount);
        if (original != await ImportFinancialNativeReview.StoredAsync(services, ct))
            throw new ReviewCheckException("Original protected base changed across owned-profile retirement.");
        return new { Comparison = comparison, Completed = true, NativeTabClicks = 4, RetainedPages = 2,
            SnapshotCurrentDifferenceMinor = -1000, ForecastCurrentEndMinor = 9700, WhatIfRetained = true,
            OriginalSnapshotUnchanged = true, OriginalBaseUnchanged = true, OwnedProfileDeleted = true,
            FictionalStoreWrites = true, NativeFinancialFormAcceptance = false, Navigation = navigation, Device = DeviceEvidence() };
    }
}
