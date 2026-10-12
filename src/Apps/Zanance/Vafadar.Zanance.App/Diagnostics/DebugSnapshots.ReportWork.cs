#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Syncfusion.Pdf.Parsing;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Maui.Controls;
using Vafadar.Zanance.App.Features.Reports;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Checks original report controls and actual detached PDF against an independently owned fictitious file.</summary>
    private static async Task ReviewReportWorkAsync(App app, IServiceProvider services, string folder, string[] languages)
    {
        var path = Path.Combine(folder, "report-work.db"); var access = new ForecastReviewAccess();
        await using var fixture = new ServiceCollection().AddSingleton<TimeProvider>(TimeProvider.System)
            .AddSingleton<ICommercialWriteAccessSource>(access).AddZananceData(path).BuildServiceProvider();
        fixture.MigrateLocalDatabase<ZananceDbContext>(); var store = fixture.GetRequiredService<ZananceStore>();
        await store.EnsureDefaultCategoriesAsync(); await ApplyOwnedReviewModeAsync(store);
        var day = DateOnly.FromDateTime(TimeProvider.System.GetLocalNow().DateTime);
        var cash = new Account { Name = "Fictitious original report cash", CurrencyCode = "EUR", OpeningBalance = 12345, OpeningDate = day.AddMonths(-4) };
        await store.SaveAccountAsync(cash);
        await store.SaveAccountAsync(new Account { Name = "Fictitious separate USD history", CurrencyCode = "USD", OpeningBalance = 9876, OpeningDate = day, IsArchived = true });
        var categories = await store.GetCategoriesAsync();
        await store.SaveEntryAsync(new LedgerEntry { Kind = EntryKind.Expense, AccountId = cash.Id, Amount = 2500,
            Date = day, Title = "Fictitious original report expense", CategoryId = categories.First(item => item.Kind == Core.Categories.CategoryKind.Expense).Id });
        Core.Goals.Goal? suggestedGoal = null;
        if (Environment.GetEnvironmentVariable("VAFADAR_SNAPSHOT_ONLY") == "report-pdf-work")
        {
            suggestedGoal = new Core.Goals.Goal { Name = "Fictitious original report contribution", CurrencyCode = "EUR", TargetAmount = 60000, TargetDate = day.AddMonths(6) };
            await fixture.GetRequiredService<GoalStore>().SaveGoalAsync(suggestedGoal);
            for (var months = 1; months <= 3; months++)
            {
                var prior = day.AddMonths(-months);
                await store.SaveEntryAsync(new LedgerEntry { Kind = EntryKind.Income, AccountId = cash.Id, Amount = 30000,
                    Date = new DateOnly(prior.Year, prior.Month, 1), Title = "Fictitious original prior income",
                    CategoryId = categories.First(item => item.Kind == Core.Categories.CategoryKind.Income).Id });
            }
        }
        var reader = fixture.GetRequiredService<ReportWorkStore>(); var translator = services.GetRequiredService<Translator>();
        var localization = services.GetRequiredService<ILocalizationService>();
        var scope = new EntitlementScope(EntitlementScopeKind.PersonalProfile, Guid.Parse("14141414-2525-3636-4747-585858585858"));
        var original = JsonSerializer.Serialize((await reader.ReadAsync()).Inputs);
        foreach (var language in languages)
        {
            await SetLanguageAsync(app, localization, language);
            var presenter = ActivatorUtilities.CreateInstance<Features.Goals.GoalPresenter>(services, fixture.GetRequiredService<HoldingStore>());
            var vm = ActivatorUtilities.CreateInstance<ReportsViewModel>(services, store, fixture.GetRequiredService<PlanStore>(),
                fixture.GetRequiredService<GoalStore>(), fixture.GetRequiredService<HoldingStore>(), reader, presenter);
            var page = new ReportsPage(vm); await Shell.Current.Navigation.PushModalAsync(page);
            for (var attempt = 0; attempt < 100 && !vm.ReportReady && vm.ReportFeedback is null; attempt++) await Task.Delay(50);
            if (!vm.ReportReady) throw new InvalidOperationException("The original report did not finish its covered publication.");
            foreach (var plan in new[] { ProductPlan.Free, ProductPlan.Plus })
            {
                access.Current = new CommercialWriteAccess(path, new CapabilityContext(plan, scope)); await vm.LoadAsync();
                var chips = VisualDescendants(page).OfType<ChoiceChips>().First();
                var pdfOnly = Environment.GetEnvironmentVariable("VAFADAR_SNAPSHOT_ONLY") == "report-pdf-work";
                foreach (var package in pdfOnly ? new[] { 0, 5 } : new[] { 0, 1, 2, 3, 4, 5 })
                {
                    var target = VisualDescendants(chips).OfType<Button>().ElementAt(package);
                    InvokeChoiceReview(target, ProfileNativeReads);
                    for (var attempt = 0; attempt < 100 && vm.ReportLoading; attempt++) await Task.Delay(50);
                    await Task.Delay(200);
                    if (!vm.ReportReady || vm.PackageIndex != package) throw new InvalidOperationException("The original native report package did not publish.");
                    if (plan == ProductPlan.Free && package is >= 1 and <= 4)
                    {
                        if (vm.ReportFeedback is null || vm.ShowCommitments || vm.ShowGoals || vm.ShowWealth || vm.ShowHistory)
                            throw new InvalidOperationException("A blocked original report lost its explicit selection or exposed paid data.");
                        CheckCaption(VisualDescendants(page).OfType<Label>().Single(label => Visible(label) && label.Text == vm.ReportFeedback), ProfileNativeReads);
                    }
                    await CaptureAsync(app, folder, $"{language}-report-work-{plan}-{package}");
                }
                var packet = JsonSerializer.Serialize(new { vm.PackageIndex, vm.PeriodKind, vm.AccountScopeIndex, vm.ScopeAccount,
                    vm.CurrencyIndex, vm.CurrencyNames, vm.ConfirmedOnly, vm.ChangeRangeIndex, vm.PeriodText, vm.ScopeText,
                    issues = vm.Issues.ToArray(), vm.StatusText });
                var pdf = await vm.CreatePdfAsync();
                if (packet != JsonSerializer.Serialize(new { vm.PackageIndex, vm.PeriodKind, vm.AccountScopeIndex, vm.ScopeAccount,
                    vm.CurrencyIndex, vm.CurrencyNames, vm.ConfirmedOnly, vm.ChangeRangeIndex, vm.PeriodText, vm.ScopeText,
                    issues = vm.Issues.ToArray(), vm.StatusText })) throw new InvalidOperationException("The PDF mutated the actual report page.");
                using var loaded = new PdfLoadedDocument(new MemoryStream(pdf));
                var text = string.Concat(Enumerable.Range(0, loaded.Pages.Count).Select(index => loaded.Pages[index].ExtractText()));
                if (text.Length < 40 || loaded.Pages.Count < 1 || (plan == ProductPlan.Free && text.Contains(translator["Report_NetWorth"], StringComparison.Ordinal)))
                    throw new InvalidOperationException("The actual portable PDF lost original content or included paid wealth analysis in Free.");
                if (language is "en" or "de" && !text.Contains(translator["Report_Closing"], StringComparison.Ordinal))
                    throw new InvalidOperationException("Basic original period balances were missing from the actual PDF.");
                await File.WriteAllBytesAsync(Path.Combine(folder, $"{language}-report-work-{plan}.pdf"), pdf);
                await File.WriteAllTextAsync(Path.Combine(folder, $"{language}-report-work-{plan}-pdf-text.txt"), text);
            }
            if (suggestedGoal is not null)
            {
                var chips = VisualDescendants(page).OfType<ChoiceChips>().First();
                InvokeChoiceReview(VisualDescendants(chips).OfType<Button>().ElementAt(2), ProfileNativeReads);
                for (var attempt = 0; attempt < 100 && vm.ReportLoading; attempt++) await Task.Delay(50);
                var suggestion = vm.Suggestions.Single(item => item.GoalId == suggestedGoal.Id && item.CanAccept);
                var accept = VisualDescendants(page).OfType<Button>().Single(button => Visible(button) && button.Command == vm.AcceptSuggestionCommand);
                var reportScroll = VisualDescendants(page).OfType<ScrollView>().First();
                await ScrollToViewIfNeededAsync(reportScroll, accept);
                InvokeChoiceReview(accept, ProfileNativeReads);
                if (vm.AcceptSuggestionCommand.ExecutionTask is { } accepting) await accepting;
                var saved = (await fixture.GetRequiredService<GoalStore>().GetContributionPlansAsync()).Single();
                if (saved.Amount != suggestion.Suggested || saved.GoalId != suggestedGoal.Id
                    || saved.Rule.Calendar != Presentation.Calendars.ToPeriod(localization.CurrentCalendar)
                    || vm.ReportFeedback is null || !vm.ReportReady)
                    throw new InvalidOperationException("The original reviewed report suggestion/calendar/outcome changed.");
                await Task.Delay(200);
                CheckCaption(VisualDescendants(page).OfType<Label>().Single(label => Visible(label) && label.Text == vm.ReportFeedback), ProfileNativeReads);
                await CaptureAsync(app, folder, language + "-report-work-accepted-original-suggestion");
                access.Current = CommercialWriteAccess.Inactive;
                await fixture.GetRequiredService<GoalStore>().SaveContributionPlanAsync(suggestedGoal.Id, null);
            }
            access.Current = CommercialWriteAccess.Inactive;
            if (original != JsonSerializer.Serialize((await reader.ReadAsync()).Inputs)) throw new InvalidOperationException("Report/PDF review changed original financial data.");
            await Shell.Current.Navigation.PopModalAsync();
            await File.WriteAllTextAsync(Path.Combine(folder, language + "-report-work-proof.json"), JsonSerializer.Serialize(new
            { ActualNativeSixPackageSelection = true, FreeTotalsAndEssentialStatusRetained = true, PaidProjectionGuarded = true,
                ActualOriginalPdfWriterAndExtraction = true, DetachedPdfPreservesCompletePageChoicesAndStatus = true,
                OriginalFinancialPacketUnchanged = true, FictitiousCachedRightsOnly = true }));
        }
    }
}
#endif
