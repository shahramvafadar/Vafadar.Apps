#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Controls;
using Vafadar.Zanance.App.Features.Forecast;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Forecasts;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Only cached rights are fictitious, scoped to the independently owned forecast file.</summary>
    private sealed class ForecastReviewAccess : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        public CommercialWriteAccess Capture(string databasePath) => Current;
    }
    /// <summary>Explicit native prompt/consent/return answers; calculations and snapshot writers stay original.</summary>
    private sealed class ForecastReviewInteraction : IForecastInteraction
    {
        internal int Prompts, Returned;
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string initialValue, int maxLength, bool numeric = false)
        { Prompts++; return Task.FromResult<string?>("Fictitious native saved forecast"); }
        public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) => Task.FromResult<string?>(null);
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) => Task.FromResult(true);
        public Task NavigateAsync(string route, IDictionary<string, object>? query = null) { Returned++; return Task.CompletedTask; }
    }
    // D-169 / AT-170: actual native horizon/reset/refresh/Save and retained original saved values with real SQLite.
    private static async Task ReviewForecastWorkAsync(App app, IServiceProvider services, string folder, string[] languages, bool multiCurrency = false)
    {
        var path = Path.Combine(folder, multiCurrency ? "forecast-currency-work.db" : "forecast-work.db"); var access = new ForecastReviewAccess();
        await using var fixture = new ServiceCollection().AddSingleton<TimeProvider>(TimeProvider.System)
            .AddSingleton<ICommercialWriteAccessSource>(access).AddZananceData(path).BuildServiceProvider();
        fixture.MigrateLocalDatabase<ZananceDbContext>(); var store = fixture.GetRequiredService<ZananceStore>();
        var work = fixture.GetRequiredService<ForecastWorkStore>(); var factory = fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        await ApplyOwnedReviewModeAsync(store); var day = DateOnly.FromDateTime(TimeProvider.System.GetLocalNow().DateTime);
        var account = new Account { Name = "Fictitious original forecast account", CurrencyCode = "EUR", OpeningDate = day, OpeningBalance = 12345 };
        await store.SaveAccountAsync(account);
        var other = new Account { Name = "Fictitious other currency account", CurrencyCode = "USD", OpeningDate = day, OpeningBalance = 9876 };
        if (multiCurrency) await store.SaveAccountAsync(other);
        var plan = new Schedule { Name = "Fictitious original forecast bill", AccountId = account.Id, Amount = 1234,
            Rule = new() { Frequency = Frequency.Once, Start = day.AddDays(1) } };
        await fixture.GetRequiredService<PlanStore>().SaveScheduleAsync(plan);
        var originalRead = await work.ReadAsync(); var forecast = ForecastCalculator.Compute(originalRead.Accounts, originalRead.Entries,
            originalRead.Schedules, originalRead.States, day, day.AddDays(3)).Single(item => item.CurrencyCode == "EUR");
        var retained = ForecastSnapshot.From("Fictitious original saved path", forecast, multiCurrency ? [account.Id, other.Id] : [account.Id], day, "Fictitious original assumptions", "0.1.0");
        await store.SaveForecastSnapshotAsync(retained);
        async Task<string> StoredAsync()
        {
            await using var db = await factory.CreateDbContextAsync(); await db.Database.OpenConnectionAsync();
            using var command = db.Database.GetDbConnection().CreateCommand(); var tables = new List<string>();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            using (var reader = await command.ExecuteReaderAsync()) while (await reader.ReadAsync()) tables.Add(reader.GetString(0));
            if (tables.Count != 25) throw new InvalidOperationException("Incomplete forecast fixture schema.");
            var all = new SortedDictionary<string, List<string>>();
            foreach (var table in tables)
            {
                command.CommandText = "SELECT * FROM \"" + table + "\""; using var reader = await command.ExecuteReaderAsync(); var rows = new List<string>();
                while (await reader.ReadAsync()) { var values = new object?[reader.FieldCount]; for (var i = 0; i < values.Length; i++) values[i] = reader.IsDBNull(i) ? null : reader.GetValue(i); rows.Add(JsonSerializer.Serialize(values)); }
                rows.Sort(StringComparer.Ordinal); all[table] = rows;
            }
            return JsonSerializer.Serialize(all);
        }
        var before = await StoredAsync(); var translator = services.GetRequiredService<Translator>(); var localization = services.GetRequiredService<ILocalizationService>();
        var scope = new EntitlementScope(EntitlementScopeKind.PersonalProfile, Guid.Parse("cdcdcdcd-4545-6767-8989-121212121212"));
        foreach (var language in languages)
        {
            await SetLanguageAsync(app, localization, language); access.Current = new(path, new(ProductPlan.Free, scope));
            var effects = new ForecastReviewInteraction(); var vm = new ForecastViewModel(store, translator, new DateFormatter(localization), localization, TimeProvider.System, work, effects);
            var page = new ForecastPage(vm, new DateFormatter(localization)); ProfileNativeReads.Add(page); await Shell.Current.Navigation.PushModalAsync(page); await Task.Delay(250);
            if (!vm.CanInteract || vm.CanSave || vm.Cards.Count != (multiCurrency ? 2 : 1)) throw new InvalidOperationException("The original Free financial-month forecast was lost.");
            var chips = VisualDescendants(page).OfType<ChoiceChips>().Single();
            // The compact original horizon chips own another horizontal ScrollView; review the page's body only.
            var scroll = VisualDescendants(page).OfType<ScrollView>().Single(item => item.Orientation == ScrollOrientation.Vertical);
            await CaptureAsync(app, folder, language + "-forecast-work-basic");
            await ScrollToViewIfNeededAsync(scroll, chips); InvokeChoiceReview(VisualDescendants(chips).OfType<Button>().ElementAt(2), ProfileNativeReads); await vm.LoadAsync(); await Task.Delay(200);
            if (vm.HorizonIndex != 2 || vm.Cards.Count != 0 || vm.Workspace.Error is null || vm.CanSave) throw new InvalidOperationException("The refused native horizon changed the original draft or published paid work.");
            CheckCaption(VisualDescendants(page).OfType<Label>().Single(label => Visible(label) && label.Text == vm.Workspace.Error), ProfileNativeReads);
            var refresh = WrappingActions(page).Single(item => Visible(item) && item.Command == vm.RefreshAccessCommand);
            CheckCaption(VisualDescendants(refresh).OfType<Label>().Single(), ProfileNativeReads);
            await CaptureAsync(app, folder, language + "-forecast-work-refused-horizon");
            access.Current = new(path, new(ProductPlan.Plus, scope)); InvokeChoiceReview(VisualDescendants(refresh).OfType<Button>().Single(), ProfileNativeReads);
            if (vm.RefreshAccessCommand.ExecutionTask is { } pending) await pending; await Task.Delay(200);
            if (vm.Cards.Count != (multiCurrency ? 2 : 1) || vm.Workspace.Forecasts.Single(item => item.CurrencyCode == "EUR").Path[^1].Date != day.AddDays(90)) throw new InvalidOperationException("Explicit native refresh lost the selected original horizon.");
            vm.Workspace.EditScenario(s => s.SetAmount(plan.Id, plan.Rule.Start, 4321)); await vm.LoadAsync();
            if (!vm.HasScenario || !vm.Workspace.Forecasts.Single(item => item.CurrencyCode == "EUR").Items.Single().IsAmountAssumed) throw new InvalidOperationException("The original scenario was not projected.");
            if (vm.IsAdvanced)
            {
                var save = VisualDescendants(page).OfType<Button>().Single(button => button.Command == vm.SaveSnapshotCommand);
                await ScrollToViewIfNeededAsync(scroll, save); InvokeChoiceReview(save, ProfileNativeReads);
                if (vm.SaveSnapshotCommand.ExecutionTask is { } saving) await saving; await Task.Delay(200);
                if (effects.Prompts != 1 || vm.CanSave || vm.Workspace.Result != translator["Forecast_Saved"]) throw new InvalidOperationException("The native accepted snapshot stayed resubmittable.");
                CheckCaption(VisualDescendants(page).OfType<Label>().Single(label => Visible(label) && label.Text == vm.Workspace.Result), ProfileNativeReads);
                await CaptureAsync(app, folder, language + "-forecast-work-saved");
                var added = (await store.GetForecastSnapshotsAsync()).Where(item => item.Id != retained.Id).ToArray();
                if (added.Length != (multiCurrency ? 2 : 1) || added.Any(item => item.Scope().Count != 1
                    || item.Scope()[0] != (item.CurrencyCode == "EUR" ? account.Id : other.Id)))
                    throw new InvalidOperationException("Original saved currency scopes included another currency's minor units.");
                foreach (var item in added) await store.DeleteForecastSnapshotAsync(item.Id);
            }
            access.Current = new(path, new(ProductPlan.Free, scope)); await vm.LoadAsync();
            if (vm.HorizonIndex != 2 || !vm.HasScenario || vm.Workspace.Error is null) throw new InvalidOperationException("Downgrade silently erased the original scenario.");
            var reset = VisualDescendants(page).OfType<Button>().Single(button => button.Command == vm.ResetScenarioCommand);
            await ScrollToViewIfNeededAsync(scroll, reset); InvokeChoiceReview(reset, ProfileNativeReads);
            if (vm.ResetScenarioCommand.ExecutionTask is { } resetting) await resetting; await Task.Delay(150);
            await ScrollToViewIfNeededAsync(scroll, chips); InvokeChoiceReview(VisualDescendants(chips).OfType<Button>().ElementAt(0), ProfileNativeReads); await vm.LoadAsync();
            if (vm.HasScenario || vm.Cards.Count != (multiCurrency ? 2 : 1) || vm.HorizonIndex != 0 || before != await StoredAsync()) throw new InvalidOperationException("Native basic return wrote financial data or retained a paid scenario.");
            await Shell.Current.Navigation.PopModalAsync();
            var savedVm = new SnapshotViewModel(store, translator, new DateFormatter(localization), localization, TimeProvider.System, work, effects);
            savedVm.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = retained.Id }); var savedPage = new SnapshotPage(savedVm); ProfileNativeReads.Add(savedPage);
            await Shell.Current.Navigation.PushModalAsync(savedPage); await Task.Delay(250);
            if (savedVm.CanCompare || savedVm.Points.Count != 0 || savedVm.SavedRows.Count != retained.Points().Count || !savedVm.DeleteCommand.CanExecute(null))
                throw new InvalidOperationException("Original saved values/deletion disappeared or a Free comparison was computed.");
            var savedScroll = VisualDescendants(savedPage).OfType<ScrollView>().Single(item => item.Orientation == ScrollOrientation.Vertical);
            foreach (var row in new[] { savedVm.SavedRows[0], savedVm.SavedRows[^1] })
            {
                var value = VisualDescendants(savedPage).OfType<AmountReadout>().First(item => item.AmountText == row.SavedText);
                await ScrollToViewIfNeededAsync(savedScroll, value); await Task.Delay(100);
            }
            await CaptureAsync(app, folder, language + "-forecast-work-retained-saved-path");
            if (multiCurrency)
            {
                access.Current = new(path, new(ProductPlan.Plus, scope)); await savedVm.LoadAsync();
                if (!savedVm.CanCompare || savedVm.Points.Single().Actual != 123.45 || savedVm.Points.Single().Saved != 123.45)
                    throw new InvalidOperationException("The original legacy comparison added foreign currency money.");
                await CaptureAsync(app, folder, language + "-forecast-work-legacy-currency-comparison");
            }
            await Shell.Current.Navigation.PopModalAsync();
            if (before != await StoredAsync()) throw new InvalidOperationException("Forecast review changed an original stored packet.");
            await File.WriteAllTextAsync(Path.Combine(folder, language + "-forecast-work-proof.json"), JsonSerializer.Serialize(new
            { ActualNativeHorizonRefreshReset = true, OriginalBasicMonthAndAdvanced90Days = true, RetainedScenarioAcrossRefusal = true,
                OriginalSavedPathWithoutNewComparison = true, NativeSaveInAdvancedMode = vm.IsAdvanced, ExplicitFictitiousPromptConsentReturnAndRights = true,
                MultiCurrencyScopeAndLegacyComparison = multiCurrency, Complete25OriginalTablesRestored = true }));
        }
    }
}
#endif
