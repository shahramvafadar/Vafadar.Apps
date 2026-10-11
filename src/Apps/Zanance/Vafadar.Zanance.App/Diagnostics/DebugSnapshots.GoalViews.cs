#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Controls;
using Vafadar.Zanance.App.Features.Goals;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    // D-146 / AT-148: real pages in an independently owned fixture; never replace the inactive release registration.
    private static async Task ReviewGoalWorkViewsAsync(App app, IServiceProvider services, string folder, string[] languages)
    {
        var path = Path.Combine(folder, "goal-view-fixture.db");
        await using var fixture = new ServiceCollection().AddSingleton<TimeProvider>(TimeProvider.System)
            .AddZananceData(path).BuildServiceProvider();
        fixture.MigrateLocalDatabase<ZananceDbContext>();
        var factory = fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var store = fixture.GetRequiredService<ZananceStore>(); var originalWriter = fixture.GetRequiredService<GoalStore>();
        var holdings = fixture.GetRequiredService<HoldingStore>(); var plans = fixture.GetRequiredService<PlanStore>();
        var day = DateOnly.FromDateTime(DateTime.Now);
        var account = new Account { Name = "Fictitious original funding", CurrencyCode = "EUR", OpeningDate = day, OpeningBalance = 10000 };
        var retained = new Account { Name = "Fictitious archived funding", CurrencyCode = "EUR", OpeningDate = day, OpeningBalance = 5000, IsArchived = true };
        await store.SaveAccountAsync(account); await store.SaveAccountAsync(retained);
        var type = new AssetType { Name = "Fictitious original holding", Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR", Divisible = true };
        await holdings.SaveTypeAsync(type);
        var items = Enumerable.Range(0, 4).Select(index => new Goal
        {
            Name = "Fictitious retained household savings goal " + index, CurrencyCode = "EUR", TargetAmount = 50000,
            TargetDate = day.AddDays(30), HomePin = index < 2 ? index + 1 : null,
            Type = index == 0 ? GoalType.AccountBalance : index == 3 ? GoalType.HoldingQuantity : GoalType.Earmark,
            AccountId = index == 0 ? account.Id : null, AssetTypeId = index == 3 ? type.Id : null,
            State = index == 2 ? GoalState.Completed : GoalState.Active,
        }).ToArray();
        foreach (var goal in items) await originalWriter.SaveGoalWithContributionPlanAsync(goal, new ContributionPlan
        { GoalId = goal.Id, Amount = 1000, Rule = new() { Start = day, Frequency = Frequency.Monthly },
            AssumedPricePerUnitMilli = goal.Type == GoalType.HoldingQuantity ? 1000000 : null });
        await originalWriter.AddAllocationAsync(new() { GoalId = items[1].Id, AccountId = account.Id, Amount = 8000, Date = day });
        await originalWriter.AddAllocationAsync(new() { GoalId = items[2].Id, AccountId = retained.Id, Amount = 2400, Date = day });
        await PrepareGoalViewSettingsAsync(store);
        var access = new ActiveItemsReviewAccess { Current = new(path,
            new(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, Guid.Parse("41414141-5252-6363-7474-858585858585")))) };
        var goals = new GoalStore(factory, access); var choices = new ResourceChoiceStore(factory, access);
        await choices.SaveAsync(await choices.ReadAsync(QuotaKind.Goals), [items[0].Id]);
        async Task<string> StoredAsync()
        {
            await using var db = await factory.CreateDbContextAsync(); await db.Database.OpenConnectionAsync();
            var connection = db.Database.GetDbConnection(); var tables = new List<string>();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            using (var reader = await command.ExecuteReaderAsync()) { while (await reader.ReadAsync()) tables.Add(reader.GetString(0)); }
            if (tables.Count != 25) throw new InvalidOperationException("Incomplete goal-view fixture schema.");
            var all = new SortedDictionary<string, List<string>>();
            foreach (var table in tables)
            {
                command.CommandText = "SELECT * FROM \"" + table + "\"";
                using var reader = await command.ExecuteReaderAsync(); var rows = new List<string>();
                while (await reader.ReadAsync())
                {
                    var values = new object?[reader.FieldCount];
                    for (var index = 0; index < values.Length; index++) values[index] = reader.IsDBNull(index) ? null : reader.GetValue(index);
                    rows.Add(JsonSerializer.Serialize(values));
                }
                rows.Sort(StringComparer.Ordinal); all[table] = rows;
            }
            return JsonSerializer.Serialize(all);
        }
        var before = await StoredAsync(); var localization = services.GetRequiredService<ILocalizationService>();
        var translator = services.GetRequiredService<Translator>(); var interaction = services.GetRequiredService<IAppInteraction>();
        foreach (var language in languages)
        {
            await SetLanguageAsync(app, localization, language); var dates = services.GetRequiredService<IDateFormatter>();
            var presenter = new GoalPresenter(translator, dates, localization, holdings, new HoldingText(translator, localization, dates));
            if (Environment.GetEnvironmentVariable("VAFADAR_SNAPSHOT_ONLY") == "goal-work-release-hint")
            {
                var historical = new GoalDetailViewModel(store, goals, plans, holdings, presenter, translator, dates, localization, TimeProvider.System, interaction);
                historical.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = items[2].Id });
                var historicalPage = new GoalDetailPage(historical); await Shell.Current.Navigation.PushAsync(historicalPage);
                await historical.LoadAsync(); await Task.Delay(250);
                var hint = VisualDescendants(historicalPage).OfType<Label>().Single(label => label.Text == translator["Goal_SetAsideHint"]);
                CheckCaption(hint); await ScrollToViewIfNeededAsync(FindScrollView(historicalPage)!, hint);
                await CaptureAsync(app, folder, language + "-goal-work-release-hint");
                if (historical.DirectionIndex != 1 || historical.SelectedAccount?.Id != retained.Id)
                    throw new InvalidOperationException("Historical release lost its original default action/account.");
                InvokeSnapshotBack(historicalPage); await Task.Delay(150);
                if (await StoredAsync() != before) throw new InvalidOperationException("Release-hint review wrote complete original rows.");
                File.WriteAllText(Path.Combine(folder, language + "-goal-work-release-hint-proof.json"), JsonSerializer.Serialize(new
                { CompleteNativeReleaseHint = true, HistoricalOriginalActionAndAccount = true, Complete25TablesUnchanged = true,
                    NativeBack = true, NoSaveInvoked = true }));
                continue;
            }
            var vm = new GoalsViewModel(store, goals, presenter, translator, TimeProvider.System, interaction);
            var page = new GoalsPage(vm); await Shell.Current.Navigation.PushAsync(page); await vm.LoadAsync(); await Task.Delay(300);
            var observations = new List<object>();
            async Task CheckWorkAsync(ContentPage current, string scenario)
            {
                var labels = VisualDescendants(current).OfType<Label>().Where(label => Visible(label) && label.Text == translator["ActiveItems_ReadOnly"]).ToArray();
                if (labels.Length == 0) throw new InvalidOperationException("Missing separate native new-work caption.");
                foreach (var label in labels)
                {
                    CheckCaption(label); observations.Add(new { scenario, label.Width, label.Height, label.Text });
                    if (FindScrollView(current) is { } scroll) await ScrollToViewIfNeededAsync(scroll, label);
                    await CaptureAsync(app, folder, language + "-goal-work-" + scenario + "-" + observations.Count);
                }
            }
            await CheckWorkAsync(page, "list");
            var row = vm.Active.Single(row => row.Id == items[1].Id);
            var target = VisualDescendants(page).OfType<Button>().Single(button => ReferenceEquals(button.CommandParameter, row));
            if (target.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button nativeTarget || nativeTarget.ActualWidth < 44 || nativeTarget.ActualHeight < 44)
                throw new InvalidOperationException("Goal list lost its real native target.");
            // Native invocation is reviewed on the registered release route separately; this fixture owns its actual detail page.
            var detail = new GoalDetailViewModel(store, goals, plans, holdings, presenter, translator, dates, localization, TimeProvider.System, interaction);
            detail.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = items[1].Id });
            var detailPage = new GoalDetailPage(detail); await Shell.Current.Navigation.PushAsync(detailPage); await detail.LoadAsync(); await Task.Delay(300);
            await CheckWorkAsync(detailPage, "detail");
            if (detail.CanContribute || detail.DirectionIndex != 1 || detail.Summary?.ProgressColor != Presentation.Palette.SavingText)
                throw new InvalidOperationException("Readonly work changed the original financial tone or allowed positive earmarking.");
            detail.AmountText = MoneyText.ForInput(123, "EUR", localization.CurrentCulture);
            detail.SelectedAccount = detail.Accounts.Single(item => item.Id == account.Id);
            var directions = VisualDescendants(detailPage).OfType<ChoiceChips>().Single();
            InvokeChoiceReview(VisualDescendants(directions).OfType<Button>().First()); await Task.Delay(100);
            if (detail.CanAllocate) throw new InvalidOperationException("A native readonly positive direction enabled new work.");
            var release = VisualDescendants(directions).OfType<Button>().Last();
            await ScrollToViewIfNeededAsync(FindScrollView(detailPage)!, release); InvokeChoiceReview(release); await Task.Delay(150);
            if (detail.DirectionIndex != 1 || !detail.CanAllocate) throw new InvalidOperationException("Actual native Release did not enable the retained correction.");
            var save = VisualDescendants(detailPage).OfType<Button>().Single(button => button.Command == detail.AllocateCommand);
            if (save.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button nativeSave || !nativeSave.IsEnabled || nativeSave.ActualWidth < 44 || nativeSave.ActualHeight < 44)
                throw new InvalidOperationException("The retained release lost its enabled native Save target.");
            await CaptureAsync(app, folder, language + "-goal-work-release-draft");
            var originalAmount = detail.AmountText; access.Fail = true;
            try { await detail.LoadAsync(); throw new InvalidOperationException("A failed detail read unexpectedly published."); }
            catch (InvalidOperationException) when (detail.NeedsReload) { }
            await Task.Delay(200);
            var retry = VisualDescendants(detailPage).OfType<Presentation.WrappingAction>().Single(action => action.Command == detail.ReloadCommand);
            if (detail.CanInteract || !Visible(retry) || detail.AmountText != originalAmount)
                throw new InvalidOperationException("Failed detail publication enabled writes or discarded its draft.");
            var feedback = VisualDescendants(detailPage).OfType<Label>().Single(label => label.Text == detail.SaveError);
            CheckCaption(feedback); await CaptureAsync(app, folder, language + "-goal-work-failed-read");
            access.Fail = false; InvokeChoiceReview(VisualDescendants(retry).OfType<Button>().Single());
            if (detail.ReloadCommand.ExecutionTask is { } reloaded) await reloaded;
            await Task.Delay(150);
            if (!detail.CanInteract || detail.AmountText != originalAmount || detail.DirectionIndex != 1)
                throw new InvalidOperationException("Native detail retry lost its original amount/direction draft.");
            InvokeSnapshotBack(detailPage); await Task.Delay(200);
            detail.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = items[2].Id });
            detailPage = new GoalDetailPage(detail); await Shell.Current.Navigation.PushAsync(detailPage); await detail.LoadAsync(); await Task.Delay(250);
            if (!detail.CanSetAside || detail.CanContribute || detail.DirectionIndex != 1
                || detail.SelectedAccount?.Id != retained.Id || !detail.Accounts.Any(account => account.Id == retained.Id && !account.CanFund))
                throw new InvalidOperationException("Historical earmark release on the original archived account disappeared.");
            await ScrollToViewIfNeededAsync(FindScrollView(detailPage)!, VisualDescendants(detailPage).OfType<ChoiceChips>().Single());
            await CaptureAsync(app, folder, language + "-goal-work-historical-release"); InvokeSnapshotBack(detailPage); await Task.Delay(200);
            detail.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = items[3].Id });
            detailPage = new GoalDetailPage(detail); await Shell.Current.Navigation.PushAsync(detailPage); await detail.LoadAsync(); await Task.Delay(250);
            detail.AssumedPriceText = MoneyText.ForInput(1234, "EUR", localization.CurrentCulture); await detail.LoadAsync(); await Task.Delay(150);
            var price = VisualDescendants(detailPage).OfType<Entry>().Single(entry => entry.Text == detail.AssumedPriceText);
            if (price.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBox nativePrice || !nativePrice.IsEnabled || !detail.CanSaveAssumedPrice)
                throw new InvalidOperationException("A retained original assumed-price correction lost its native input.");
            await ScrollToViewIfNeededAsync(FindScrollView(detailPage)!, price); await CaptureAsync(app, folder, language + "-goal-work-price-draft");
            var exactPrice = detail.AssumedPriceText;
            await using (var db = await factory.CreateDbContextAsync())
                await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE AssetTypes SET PriceCurrencyCode={"USD"} WHERE Id={type.Id}");
            try
            {
                await detail.LoadAsync(); await Task.Delay(200);
                if (detail.CanSaveAssumedPrice || detail.AssumedPriceText != exactPrice || detail.PriceError is null)
                    throw new InvalidOperationException("Changed price currency reinterpreted or enabled the retained draft.");
                var currencyFeedback = VisualDescendants(detailPage).OfType<Label>().Single(label => label.Text == detail.PriceError);
                CheckCaption(currencyFeedback); await ScrollToViewIfNeededAsync(FindScrollView(detailPage)!, currencyFeedback);
                await CaptureAsync(app, folder, language + "-goal-work-price-currency-review");
            }
            finally
            {
                // Restore only the independently owned fixture's exact reference field, including unchanged audit columns.
                await using var db = await factory.CreateDbContextAsync();
                await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE AssetTypes SET PriceCurrencyCode={"EUR"} WHERE Id={type.Id}");
            }
            await detail.LoadAsync();
            InvokeSnapshotBack(detailPage); await Task.Delay(200); InvokeSnapshotBack(page); await Task.Delay(250);
            await Shell.Current.GoToAsync("//home"); await Task.Delay(400);
            if (Shell.Current.CurrentPage is not HomePage { BindingContext: HomeViewModel home } homePage)
                throw new InvalidOperationException("The actual Home page did not publish.");
            var prior = home.PinnedGoals.ToArray(); var priorHas = home.HasPinnedGoals;
            try
            {
                var view = await GoalHomeProjection.ReadAsync(goals, presenter, translator, await store.GetAccountsAsync(), await store.GetEntriesAsync(), day);
                home.PinnedGoals.Clear(); foreach (var item in view.Pinned) home.PinnedGoals.Add(item); home.HasPinnedGoals = true;
                await Task.Delay(250);
                foreach (var label in VisualDescendants(homePage).OfType<Label>().Where(label => home.PinnedGoals.Any(row => row.Name == label.Text))) CheckCaption(label);
                await CheckWorkAsync(homePage, "home");
                if (home.PinnedGoals.Count != 2 || home.PinnedGoals[1].Id != items[1].Id)
                    throw new InvalidOperationException("Home replaced the original two pins with a guessed subset.");
            }
            finally { home.PinnedGoals.Clear(); foreach (var item in prior) home.PinnedGoals.Add(item); home.HasPinnedGoals = priorHas; }
            if (await StoredAsync() != before) throw new InvalidOperationException("Goal-view presentation wrote any complete original row.");
            File.WriteAllText(Path.Combine(folder, language + "-goal-work-views-proof.json"), JsonSerializer.Serialize(new
            { Complete25TablesUnchanged = true, ActualListDetailAndHome = true, SeparateNativeWorkCaptions = true,
                RetainedHistoricalRelease = true, NativeReleaseDraftAndSaveTarget = true, RetainedPriceDraft = true,
                ChangedPriceCurrencyRequiresReentry = true, NativeFailedReadRetryKeepsDraft = true, CompleteHomeIdentity = true,
                NativeBack = true, OriginalPinsAndFinancialTone = true, NoSaveInvoked = true, FictitiousFreeOnly = true, observations }));
        }
    }
}
#endif
