#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Core.Settings;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Controls;
using Vafadar.Zanance.App.Features.Goals;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    // D-157 / AT-159: the original detail page consumes the actual DI decorator in an independent database.
    // No production source is enabled and no native Save or authentication is invoked.
    private static async Task ReviewProfileFileWorkAsync(App app, IServiceProvider services, string folder, string[] languages)
    {
        var path = Path.Combine(folder, "zanance.db"); var preferences = new InMemorySettingsStore();
        var financial = new ActiveItemsReviewAccess { Current = CommercialWriteAccess.Inactive };
        var device = new ProfileReviewAccess(); var translator = services.GetRequiredService<Translator>();
        await using var fixture = new ServiceCollection().AddSingleton<TimeProvider>(TimeProvider.System)
            .AddSingleton<ICommercialWriteAccessSource>(financial).AddSingleton<IProfileWorkAccessSource>(device)
            .AddSingleton<ISettingsStore>(preferences).AddSingleton<IProfileRuntime, ProfileReviewRuntime>()
            .AddSingleton(translator).AddZananceData(path).AddSingleton<ProfileService>()
            .DecorateZananceCommercialAccess((_, source) => new ProfileCommercialAccessSource(source, preferences, device, path))
            .BuildServiceProvider();
        fixture.MigrateLocalDatabase<ZananceDbContext>();
        var store = fixture.GetRequiredService<ZananceStore>(); var goals = fixture.GetRequiredService<GoalStore>();
        var plans = fixture.GetRequiredService<PlanStore>(); var holdings = fixture.GetRequiredService<HoldingStore>();
        var factory = fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var profiles = fixture.GetRequiredService<ProfileService>(); var later = profiles.Create("Fictitious original later household");
        var day = DateOnly.FromDateTime(DateTime.Now);
        var account = new Account { Name = "Fictitious retained funding", CurrencyCode = "EUR", OpeningDate = day, OpeningBalance = 10000 };
        await store.SaveAccountAsync(account);
        var goal = new Goal { Name = "Fictitious complete original savings goal", CurrencyCode = "EUR", TargetAmount = 50000, Type = GoalType.Earmark };
        await goals.SaveGoalAsync(goal);
        var balanceGoal = new Goal { Name = "Fictitious original basic balance goal", CurrencyCode = "EUR", TargetAmount = 50000, Type = GoalType.AccountBalance, AccountId = account.Id };
        await goals.SaveGoalAsync(balanceGoal);
        await goals.AddAllocationAsync(new() { GoalId = goal.Id, AccountId = account.Id, Amount = 8000, Date = day });
        await PrepareGoalViewSettingsAsync(store);
        async Task<string> StoredAsync()
        {
            await using var db = await factory.CreateDbContextAsync(); await db.Database.OpenConnectionAsync();
            using var command = db.Database.GetDbConnection().CreateCommand(); var tables = new List<string>();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            using (var reader = await command.ExecuteReaderAsync()) while (await reader.ReadAsync()) tables.Add(reader.GetString(0));
            if (tables.Count != 25) throw new InvalidOperationException("Incomplete original file-work fixture schema.");
            var all = new SortedDictionary<string, List<string>>();
            foreach (var table in tables)
            {
                command.CommandText = "SELECT * FROM \"" + table + "\""; using var reader = await command.ExecuteReaderAsync(); var rows = new List<string>();
                while (await reader.ReadAsync()) { var values = new object?[reader.FieldCount]; for (var i = 0; i < values.Length; i++) values[i] = reader.IsDBNull(i) ? null : reader.GetValue(i); rows.Add(JsonSerializer.Serialize(values)); }
                rows.Sort(StringComparer.Ordinal); all[table] = rows;
            }
            return JsonSerializer.Serialize(all);
        }
        var originalPacket = preferences.Get("profiles.work.catalogue");
        financial.Current = new(path, new(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, Guid.Parse("42424242-5353-6464-7575-868686868686"))));
        device.Current = new(path, new(QuotaScopeKind.Device, Guid.Parse("72727272-8383-9494-a5a5-b6b6b6b6b6b6")),
            new(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, Guid.Parse("62626262-7373-8484-9595-a6a6a6a6a6a6"))));
        profiles.SaveWorkChoice(profiles.ReadWorkReview(), [""]);
        var choices = fixture.GetRequiredService<ResourceChoiceStore>();
        await choices.SaveAsync(await choices.ReadAsync(QuotaKind.Goals), [balanceGoal.Id]);
        var before = await StoredAsync();
        var localization = services.GetRequiredService<ILocalizationService>();
        foreach (var language in languages)
        {
            await SetLanguageAsync(app, localization, language);
            profiles.SaveWorkChoice(profiles.ReadWorkReview(), [""]);
            var dates = services.GetRequiredService<IDateFormatter>();
            var presenter = new GoalPresenter(translator, dates, localization, holdings, new HoldingText(translator, localization, dates));
            var listVm = new GoalsViewModel(store, goals, presenter, translator, TimeProvider.System, services.GetRequiredService<IAppInteraction>());
            var listPage = new GoalsPage(listVm); ProfileNativeReads.Add(listPage);
            await Shell.Current.Navigation.PushAsync(listPage); await listVm.LoadAsync(); await Task.Delay(250);
            var funded = listVm.Active.Single(row => row.Id == balanceGoal.Id).FundedText;
            if (listVm.Active.Single(row => row.Id == balanceGoal.Id).WorkText is not null)
                throw new InvalidOperationException("Selected original file lost its basic goal work.");
            await CaptureAsync(app, folder, language + "-profile-file-work-selected");
            profiles.SaveWorkChoice(profiles.ReadWorkReview(), [later.Id]); await listVm.LoadAsync(); await Task.Delay(250);
            if (listVm.Active.Single(row => row.Id == balanceGoal.Id).WorkText != translator["ActiveItems_ReadOnly"]
                || listVm.Active.Single(row => row.Id == balanceGoal.Id).FundedText != funded || listVm.Active.Count != 2)
                throw new InvalidOperationException("Unselected original file changed its original money or allowed basic goal work.");
            foreach (var label in VisualDescendants(listPage).OfType<Label>().Where(Visible)) CheckCaption(label, ProfileNativeReads);
            await CaptureAsync(app, folder, language + "-profile-file-work-unselected-list");
            var vm = new GoalDetailViewModel(store, goals, plans, holdings, presenter, translator, dates, localization, TimeProvider.System,
                services.GetRequiredService<IAppInteraction>());
            vm.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = goal.Id });
            var page = new GoalDetailPage(vm); ProfileNativeReads.Add(page);
            await Shell.Current.Navigation.PushAsync(page); await vm.LoadAsync(); await Task.Delay(250);
            if (vm.CanContribute || vm.DirectionIndex != 1 || vm.Summary is null || vm.History.Count != 1)
                throw new InvalidOperationException("Actual file choice lost retained history or allowed new work.");
            var observations = new List<object>();
            foreach (var label in VisualDescendants(page).OfType<Label>().Where(Visible)) CheckCaption(label, ProfileNativeReads);
            var readonlyCaption = VisualDescendants(page).OfType<Label>().Single(label => Visible(label) && label.Text == translator["ActiveItems_ReadOnly"]);
            await ScrollToViewIfNeededAsync(FindScrollView(page)!, readonlyCaption);
            await CaptureAsync(app, folder, language + "-profile-file-work-unselected-history");
            vm.AmountText = MoneyText.ForInput(123, "EUR", localization.CurrentCulture);
            vm.SelectedAccount = vm.Accounts.Single(item => item.Id == account.Id);
            var directions = VisualDescendants(page).OfType<ChoiceChips>().Single();
            var positive = VisualDescendants(directions).OfType<Button>().First();
            await ScrollToViewIfNeededAsync(FindScrollView(page)!, positive); InvokeChoiceReview(positive, ProfileNativeReads); await Task.Delay(100);
            if (vm.CanAllocate) throw new InvalidOperationException("The native positive direction granted unselected-file money.");
            var release = VisualDescendants(directions).OfType<Button>().Last();
            InvokeChoiceReview(release, ProfileNativeReads); await Task.Delay(100);
            var save = VisualDescendants(page).OfType<Button>().Single(button => button.Command == vm.AllocateCommand);
            if (!vm.CanAllocate || save.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button nativeSave
                || !nativeSave.IsEnabled || nativeSave.ActualWidth < 44 || nativeSave.ActualHeight < 44)
                throw new InvalidOperationException("Retained correction lost its actual native target.");
            ProfileNativeReads.Add(nativeSave);
            observations.Add(new { nativeSave.ActualWidth, nativeSave.ActualHeight, nativeSave.IsEnabled, vm.DirectionIndex });
            await CaptureAsync(app, folder, language + "-profile-file-work-retained-release-draft");
            var originalAmount = vm.AmountText;
            profiles.SaveWorkChoice(profiles.ReadWorkReview(), [""]); await vm.LoadAsync(); await Task.Delay(150);
            if (vm.CanContribute || vm.AmountText != originalAmount || vm.DirectionIndex != 1)
                throw new InvalidOperationException("Rechosen original file lost retained exact draft.");
            await CaptureAsync(app, folder, language + "-profile-file-work-rechosen-draft");
            InvokeSnapshotBack(page); await Task.Delay(150); await listVm.LoadAsync(); await Task.Delay(150);
            if (listVm.Active.Single(row => row.Id == balanceGoal.Id).WorkText is not null
                || listVm.Active.Single(row => row.Id == balanceGoal.Id).FundedText != funded)
                throw new InvalidOperationException("Rechosen original basic work did not return with unchanged money.");
            await CaptureAsync(app, folder, language + "-profile-file-work-rechosen-list");
            InvokeSnapshotBack(listPage); await Task.Delay(150);
            preferences.Set("profiles.work.catalogue", originalPacket);
            if (await StoredAsync() != before || preferences.Get("profiles.work.catalogue") != originalPacket || !profiles.Current.IsMain)
                throw new InvalidOperationException("Complete original file/profile restoration failed.");
            await File.WriteAllTextAsync(Path.Combine(folder, language + "-profile-file-work-proof.json"), JsonSerializer.Serialize(new
            { ActualDecoratorAndOriginalGoalPage = true, OriginalEmptyMainId = true, SelectedOriginalWorkReturns = true,
                UnselectedOriginalHistoryAndCorrection = true, Complete25TablesUnchanged = true, ExactOriginalDraftRetained = true,
                OriginalCataloguePacketRestored = true, ProductionRegistrationInactive = true, NoSaveInvoked = true, observations }));
        }
    }
}
#endif
