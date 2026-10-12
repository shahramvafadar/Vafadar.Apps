using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-167: original read-only availability explains blocked Save without changing raw form or history.</summary>
[Trait("AT", "AT-167")]
public sealed class EditorReadinessFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class Access : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        internal bool Refuse;
        public CommercialWriteAccess Capture(string path) => Refuse ? throw new InvalidOperationException("Fictitious cached availability refusal.") : Current;
    }
    private static readonly Guid ScopeId = Guid.Parse("67676767-1212-3434-4545-565656565656");
    private static void SetTier(FlowFixture f, Access access, ProductPlan tier) => access.Current = new(f.Directory.Combine("zanance.db"),
        new(tier, new(EntitlementScopeKind.PersonalProfile, ScopeId)));
    private static (ZananceStore Store, PlanStore Plans) Stores(FlowFixture f, Access access)
    {
        var store = new ZananceStore(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), f.Time, access);
        return (store, new(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), store, access));
    }
    private static ReminderService Reminders(FlowFixture f, ZananceStore store, PlanStore plans) => new(new NoReminderScheduler(), store, plans,
        f.Services.GetRequiredService<GoalStore>(), f.Services.GetRequiredService<HoldingStore>(), f.Preferences, f.Translator,
        new DateFormatter(f.Localization), f.Localization, f.Time);

    [Theory]
    [InlineData("en", false)][InlineData("fa", false)][InlineData("de", false)][InlineData("es", false)][InlineData("fr", false)][InlineData("it", false)]
    [InlineData("en", true)][InlineData("fa", true)][InlineData("de", true)][InlineData("es", true)][InlineData("fr", true)][InlineData("it", true)]
    public async Task Known_full_quota_disables_save_before_any_attempt_and_explicit_review_retains_every_raw_field(string language, bool plan)
    {
        using var f = new FlowFixture(); f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(item => item.CultureName == language));
        await f.Store.GetSettingsAsync(Ct); var a = new Account { Name = "Fictitious original funding", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
        await f.Store.SaveAccountAsync(a, Ct);
        if (plan)
            for (var i = 0; i < 5; i++) await f.Services.GetRequiredService<PlanStore>().SaveScheduleAsync(new() { Name = "Fictitious original plan " + i,
                AccountId = a.Id, Amount = 1234, Rule = new() { Frequency = Frequency.Once, Start = a.OpeningDate } }, Ct);
        else for (var i = 0; i < 2; i++) await f.Store.SaveAccountAsync(new() { Name = "Fictitious original account " + i, CurrencyCode = "EUR", OpeningDate = a.OpeningDate }, Ct);
        var access = new Access(); SetTier(f, access, ProductPlan.Free); var (store, plans) = Stores(f, access); await using var reminders = Reminders(f, store, plans);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        if (plan)
        {
            var vm = new PlanEditorViewModel(store, plans, f.Translator, f.Localization, new DateFormatter(f.Localization), f.Time, reminders, f.Platform);
            await vm.ApplyQueryAsync(new Dictionary<string, object>()); vm.Name = "  Complete raw plan  "; vm.Note = "Complete raw note\nRetained second line";
            vm.AmountText = " " + MoneyText.ForInput(45678, "EUR", f.Localization.CurrentCulture) + " "; vm.Start = new(2026, 11, 20);
            var raw = (vm.Name, vm.Note, vm.AmountText, vm.Start, vm.Account!.Id, vm.ReminderDaysText, vm.ReminderTime);
            Assert.False(vm.CanSave); Assert.True(vm.CanInteract); Assert.Contains(f.Translator["Tab_Plans"], vm.WorkText!); Assert.Null(vm.SaveError);
            await vm.SaveCommand.ExecuteAsync(null); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Empty(f.Platform.Routes);
            SetTier(f, access, ProductPlan.Plus); await vm.ReviewWorkCommand.ExecuteAsync(null); Assert.True(vm.CanSave); Assert.Null(vm.WorkText);
            Assert.Equal(raw, (vm.Name, vm.Note, vm.AmountText, vm.Start, vm.Account!.Id, vm.ReminderDaysText, vm.ReminderTime));
            access.Refuse = true; await vm.ReviewWorkCommand.ExecuteAsync(null); Assert.False(vm.CanSave); Assert.True(vm.CanInteract);
            Assert.Equal(f.Translator["Editor_WorkReadFailed"], vm.WorkText); Assert.Equal(raw, (vm.Name, vm.Note, vm.AmountText, vm.Start, vm.Account!.Id, vm.ReminderDaysText, vm.ReminderTime));
            access.Refuse = false; await vm.ReviewWorkCommand.ExecuteAsync(null); Assert.True(vm.CanSave);
        }
        else
        {
            var vm = new AccountEditorViewModel(store, plans, f.Translator, f.Localization, f.Time, f.Platform); await vm.ApplyQueryAsync(new Dictionary<string, object>());
            vm.Form.Name = "  Complete raw account  "; vm.Form.OpeningText = " " + MoneyText.ForInput(45678, "EUR", f.Localization.CurrentCulture) + " ";
            vm.IsDefault = true; var raw = vm.Form.Snapshot(); Assert.False(vm.CanSave); Assert.True(vm.CanInteract);
            Assert.Contains(f.Translator["Accounts_Title"], vm.WorkText!); Assert.Null(vm.SaveError); await vm.SaveCommand.ExecuteAsync(null);
            Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Empty(f.Platform.Routes);
            SetTier(f, access, ProductPlan.Plus); await vm.ReviewWorkCommand.ExecuteAsync(null); Assert.True(vm.CanSave); Assert.Null(vm.WorkText);
            Assert.Equal(raw, vm.Form.Snapshot()); Assert.True(vm.IsDefault);
            access.Refuse = true; await vm.ReviewWorkCommand.ExecuteAsync(null); Assert.False(vm.CanSave); Assert.True(vm.CanInteract);
            Assert.Equal(f.Translator["Editor_WorkReadFailed"], vm.WorkText); Assert.Equal(raw, vm.Form.Snapshot()); Assert.True(vm.IsDefault);
            access.Refuse = false; await vm.ReviewWorkCommand.ExecuteAsync(null); Assert.True(vm.CanSave);
        }
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Changed_advanced_plan_fields_update_only_the_original_permission_reason_and_keep_saved_rule_corrections()
    {
        using var f = new FlowFixture(); await f.Store.GetSettingsAsync(Ct); var a = new Account { Name = "Fictitious original", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
        await f.Store.SaveAccountAsync(a, Ct); var access = new Access(); SetTier(f, access, ProductPlan.Free); var (store, plans) = Stores(f, access);
        await using var reminders = Reminders(f, store, plans); var vm = new PlanEditorViewModel(store, plans, f.Translator, f.Localization, new DateFormatter(f.Localization), f.Time, reminders, f.Platform);
        await vm.ApplyQueryAsync(new Dictionary<string, object>()); vm.Name = "Complete unsaved name"; vm.AmountText = "12"; var raw = (vm.Name, vm.AmountText, vm.Start);
        Assert.True(vm.CanSave); vm.ContractProvider = "  Fictitious provider  "; Assert.False(vm.CanSave); Assert.Equal(f.Translator["Commercial_RequiresPlus"], vm.WorkText);
        vm.ContractProvider = string.Empty; Assert.True(vm.CanSave); vm.PresetIndex = 3; vm.HasSecondDay = true; vm.SecondDayText = "20";
        Assert.False(vm.CanSave); Assert.Equal(f.Translator["Commercial_RequiresPlus"], vm.WorkText); vm.HasSecondDay = false; Assert.True(vm.CanSave);
        Assert.Equal(raw, (vm.Name, vm.AmountText, vm.Start)); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await vm.ReviewWorkCommand.ExecuteAsync(null); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        access.Current = CommercialWriteAccess.Inactive;
        var existing = new Schedule { Name = "Fictitious retained advanced", AccountId = a.Id, Amount = 1234, AutoPost = true,
            ContractProvider = "Fictitious retained contract", Rule = new() { Frequency = Frequency.Monthly, Start = a.OpeningDate, SecondDay = 20 } };
        await plans.SaveScheduleAsync(existing, Ct); SetTier(f, access, ProductPlan.Free);
        var edit = new PlanEditorViewModel(store, plans, f.Translator, f.Localization, new DateFormatter(f.Localization), f.Time, reminders, f.Platform);
        await edit.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = existing.Id }); edit.Name = "Complete original correction";
        Assert.True(edit.CanSave); Assert.Null(edit.WorkText);
    }

    [Fact]
    public async Task Stored_choice_counts_and_malformed_repair_match_original_writer_while_existing_corrections_remain_available()
    {
        using var f = new FlowFixture(); await f.Store.GetSettingsAsync(Ct); var access = new Access(); var (store, plans) = Stores(f, access); var ids = new List<Guid>();
        for (var i = 0; i < 4; i++) { var a = new Account { Name = "Fictitious original " + i, CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) }; await store.SaveAccountAsync(a, Ct); ids.Add(a.Id); }
        SetTier(f, access, ProductPlan.Free); var choices = new ResourceChoiceStore(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), access);
        await choices.SaveAsync(await choices.ReadAsync(QuotaKind.FinancialAccounts, Ct), ids.Take(2), Ct);
        var reviewed = await store.ReadEditorWorkAsync(Ct); Assert.Null(reviewed.AccountRestriction(Guid.NewGuid(), false));
        await choices.SaveAsync(await choices.ReadAsync(QuotaKind.FinancialAccounts, Ct), ids.Take(3), Ct);
        reviewed = await store.ReadEditorWorkAsync(Ct); Assert.Equal(3, reviewed.AccountRestriction(Guid.NewGuid(), false)!.Current);
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
            await db.Database.ExecuteSqlRawAsync("UPDATE ResourceSelections SET IdentitySet='malformed owned choice';", Ct);
        reviewed = await store.ReadEditorWorkAsync(Ct); Assert.True(reviewed.AccountRestriction(Guid.NewGuid(), false)!.RequiresSelection);
        Assert.Null(reviewed.AccountRestriction(ids[0], false)); SetTier(f, access, ProductPlan.Plus);
        Assert.Null((await store.ReadEditorWorkAsync(Ct)).AccountRestriction(Guid.NewGuid(), false));
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var vm = new AccountEditorViewModel(store, plans, f.Translator, f.Localization, f.Time, f.Platform);
        await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = ids[0] }); Assert.True(vm.CanSave); Assert.Null(vm.WorkText);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Inactive_readiness_never_creates_or_opens_an_uninitialized_database_or_manufactures_context()
    {
        using var f = new FlowFixture(); var path = f.Directory.Combine("independent-unopened.db"); var access = new Access();
        using var services = new ServiceCollection().AddSingleton<ICommercialWriteAccessSource>(access).AddZananceData(path).BuildServiceProvider();
        var reader = new EditorWorkStore(services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), access);
        var reviewed = await reader.ReadAsync(Ct); Assert.False(File.Exists(path)); Assert.Null(reviewed.AccountRestriction(Guid.NewGuid(), false));
        Assert.Null(reviewed.PlanRestriction(new() { Name = "Fictitious uninitialized comparison" }));
        access.Current = new(path, new(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, ScopeId)));
        Assert.Throws<InvalidOperationException>(reviewed.EnsureCurrent); Assert.False(File.Exists(path));
    }
}
