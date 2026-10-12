using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Budget;
using Vafadar.Zanance.App.Features.Goals;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-163: original pre-Save reasons retain actual Goal/Budget drafts and complete isolated SQLite data.</summary>
[Trait("AT", "AT-163")]
public sealed class PreSaveReadinessFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class Access : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        public CommercialWriteAccess Capture(string databasePath) => Current;
    }
    private sealed class Notifications : IGoalEditorNotifications
    {
        public bool IsSupported => false;
        public Task<bool> EnableAsync() => throw new InvalidOperationException("No native permission belongs to a pre-Save review.");
    }
    private static GoalEditorViewModel GoalEditor(FlowFixture f, GoalStore goals)
    {
        var dates = new DateFormatter(f.Localization); var holdings = f.Services.GetRequiredService<HoldingStore>();
        return new(f.Store, goals, f.Translator, f.Localization, dates, f.Time, holdings,
            new HoldingText(f.Translator, f.Localization, dates), f.Platform, new Notifications());
    }
    private static void Enable(FlowFixture f, Access access, ProductPlan plan = ProductPlan.Free) => access.Current = new(f.Directory.Combine("zanance.db"),
        new(plan, new(EntitlementScopeKind.PersonalProfile, Guid.Parse("83838383-3434-4545-6767-898989898989"))));

    [Theory]
    [InlineData("en", false)][InlineData("fa", false)][InlineData("de", false)]
    [InlineData("en", true)][InlineData("fa", true)][InlineData("de", true)]
    [InlineData("es", true)][InlineData("fr", true)][InlineData("it", true)]
    public async Task New_balance_goal_full_capacity_explains_the_actual_count_without_an_attempt_or_discarding_any_input(string language, bool advanced)
    {
        using var f = new FlowFixture(); f.Localization.SetLanguage(f.Localization.SupportedLanguages.First(item => item.CultureName == language));
        var settings = await f.Store.GetSettingsAsync(Ct); settings.Mode = advanced ? ExperienceMode.Advanced : ExperienceMode.Simple;
        await f.Store.SaveSettingsAsync(settings, Ct);
        var a = new Account { Name = "Fictitious original goal funding", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
        var b = new Account { Name = "Fictitious draft funding", CurrencyCode = "EUR", OpeningDate = a.OpeningDate };
        await f.Store.SaveAccountAsync(a, Ct); await f.Store.SaveAccountAsync(b, Ct);
        await f.Services.GetRequiredService<GoalStore>().SaveGoalAsync(new Goal { Name = "Fictitious original", Type = GoalType.AccountBalance,
            AccountId = a.Id, CurrencyCode = "EUR", TargetAmount = 10000 }, Ct);
        var access = new Access(); Enable(f, access);
        var goals = new GoalStore(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), access); var editor = GoalEditor(f, goals);
        await editor.ApplyQueryAsync(new Dictionary<string, object>()); editor.Account = editor.Accounts.Single(item => item.Id == b.Id);
        editor.Name = "  Complete unsaved goal name  "; editor.Note = "Complete unsaved note\nRetained second line";
        editor.AmountText = " " + MoneyText.ForInput(45678, "EUR", f.Localization.CurrentCulture) + " ";
        editor.HasTargetDate = true; editor.TargetDate = new(2027, 2, 28);
        var raw = (editor.Name, editor.Note, editor.AmountText, editor.Account.Id, editor.TargetDate, editor.FirstDate);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); Assert.False(editor.CanSave); Assert.True(editor.CanInteract);
        var block = Assert.IsType<WorkRestriction>((await goals.GetWorkSnapshotAsync(Ct)).Work.CreationRestriction(GoalType.AccountBalance));
        Assert.Equal((1, 1, 1), (block.Maximum, block.Current, block.Requested));
        Assert.Equal(CommercialFailureMessage.Describe(block, f.Translator), editor.WorkText);
        Assert.DoesNotContain(f.Translator["Commercial_DraftKept"], editor.WorkText!);
        await editor.SaveCommand.ExecuteAsync(null);
        Assert.Empty(f.Platform.Failures); Assert.Empty(f.Platform.Routes); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        editor.RefreshDisplay();
        Assert.Equal(raw, (editor.Name, editor.Note, editor.AmountText, editor.Account!.Id, editor.TargetDate, editor.FirstDate));
    }
    [Theory]
    [InlineData("en")][InlineData("fa")][InlineData("de")][InlineData("es")][InlineData("fr")][InlineData("it")]
    public async Task Current_new_budget_definition_explains_net_capacity_but_a_matching_original_definition_stays_correctable(string language)
    {
        using var f = new FlowFixture(); f.Localization.SetLanguage(f.Localization.SupportedLanguages.First(item => item.CultureName == language));
        var a = new Account { Name = "Fictitious selected original scope", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
        await f.Store.SaveAccountAsync(a, Ct);
        var original = new Budget { Year = 2026, Month = 10, CurrencyCode = "EUR", AccountIds = [a.Id], TotalLimit = 12345 };
        await f.Store.SaveBudgetAsync(original, Ct);
        var access = new Access(); Enable(f, access); var store = new ZananceStore(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), f.Time, access);
        var editor = new BudgetEditorViewModel(store, f.Translator, f.Localization, f.Platform);
        await editor.ApplyQueryAsync(new Dictionary<string, object> { ["year"] = 2026, ["month"] = 10, ["calendar"] = PeriodCalendar.Gregorian, ["currency"] = "USD" });
        editor.TotalText = " " + MoneyText.ForInput(98765, "USD", f.Localization.CurrentCulture) + " ";
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var raw = editor.TotalText;
        Assert.False(editor.CanSave); Assert.Contains(f.Translator["Commercial_BudgetDefinitions"], editor.WorkText!);
        await editor.SaveCommand.ExecuteAsync(null); Assert.Equal(raw, editor.TotalText); Assert.Empty(f.Platform.Failures);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        var correction = new BudgetEditorViewModel(store, f.Translator, f.Localization, f.Platform);
        await correction.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = original.Id });
        Assert.True(correction.CanSave); Assert.Null(correction.WorkText); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task Malformed_original_goal_choice_requires_review_before_new_work_without_blocking_retained_correction_or_granting_unlimited_rights()
    {
        using var f = new FlowFixture();
        var a = new Account { Name = "Fictitious original", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) }; await f.Store.SaveAccountAsync(a, Ct);
        var original = new Goal { Name = "Fictitious retained goal", Type = GoalType.AccountBalance, AccountId = a.Id, CurrencyCode = "EUR", TargetAmount = 10000 };
        await f.Services.GetRequiredService<GoalStore>().SaveGoalAsync(original, Ct);
        var access = new Access(); Enable(f, access); var factory = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var choices = new ResourceChoiceStore(factory, access); await choices.SaveAsync(await choices.ReadAsync(QuotaKind.Goals, Ct), [], Ct);
        await using (var db = await factory.CreateDbContextAsync(Ct)) await db.Database.ExecuteSqlRawAsync("UPDATE ResourceSelections SET IdentitySet='fictitious-invalid-value'", Ct);
        var goals = new GoalStore(factory, access); var review = await goals.GetWorkSnapshotAsync(Ct); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        Assert.True(review.Work.CreationRestriction(GoalType.AccountBalance)!.RequiresSelection); Assert.False(review.Work.CanReopenGoal(GoalType.AccountBalance));
        Assert.True(review.Work.CanCorrect);
        Enable(f, access, ProductPlan.Plus); var unlimited = await goals.GetWorkSnapshotAsync(Ct);
        Assert.Null(unlimited.Work.CreationRestriction(GoalType.AccountBalance)); Assert.True(unlimited.Work.CanReopenGoal(GoalType.AccountBalance));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Theory]
    [InlineData(ProductPlan.Free)][InlineData(ProductPlan.Plus)][InlineData(ProductPlan.Pro)]
    public void Captured_goal_reason_matches_original_reopening_policy_for_types_paired_work_reminders_and_choice_capacity(ProductPlan plan)
    {
        var id = Guid.Parse("85858585-3434-4545-6767-898989898989");
        var context = new CapabilityContext(plan, new(EntitlementScopeKind.PersonalProfile, id));
        var scope = new QuotaScope(QuotaScopeKind.PersonalProfile, id);
        foreach (var count in new[] { 0, 1, 2 })
        {
            var originals = Enumerable.Range(0, count).Select(index => new Goal { Name = "Fictitious original " + index,
                Type = GoalType.AccountBalance, CurrencyCode = "EUR", State = index == 1 ? GoalState.Paused : GoalState.Active }).ToArray();
            foreach (var choice in new ResourceSelection?[] { null, new(QuotaKind.Goals, scope, []),
                new(QuotaKind.Goals, scope, originals.Take(1).Select(goal => goal.Id)) })
            {
                var work = GoalWorkPolicy.Resolve(context, scope, originals, choice);
                foreach (var type in Enum.GetValues<GoalType>())
                    foreach (var contribution in new ContributionPlan?[] { null,
                        new() { Amount = 1000, Rule = new() { Frequency = Vafadar.Zanance.Core.Plans.Frequency.Monthly } },
                        new() { ReminderEnabled = true, Rule = new() { Frequency = Vafadar.Zanance.Core.Plans.Frequency.Monthly } } })
                        Assert.Equal(work.CanReopenGoal(type, contribution), work.CreationRestriction(type, contribution) is null);
            }
        }
    }
}
