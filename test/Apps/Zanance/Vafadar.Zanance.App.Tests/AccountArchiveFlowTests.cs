using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-166: original archive/default/plan/choice changes commit together and preserve reviewed consent.</summary>
[Trait("AT", "AT-166")]
public sealed class AccountArchiveFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class Access : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        public CommercialWriteAccess Capture(string path) => Current;
    }
    private sealed record Seed(ZananceStore Store, PlanStore Plans, Access Access, Account Account, Account Other, Schedule Active, Schedule Paused);
    private static async Task<Seed> PrepareAsync(FlowFixture f)
    {
        var access = new Access(); var store = new ZananceStore(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), f.Time, access);
        var plans = new PlanStore(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), store, access);
        var a = new Account { Name = "Fictitious original archive account", CurrencyCode = "EUR", OpeningDate = new(2026, 9, 1), OpeningBalance = 12345 };
        var b = new Account { Name = "Fictitious retained other account", CurrencyCode = "EUR", OpeningDate = a.OpeningDate };
        await store.SaveAccountAsync(a, Ct); await store.SaveAccountAsync(b, Ct);
        var active = new Schedule { Name = "Fictitious original active plan", AccountId = a.Id, Amount = 1234, AutoPost = true,
            Rule = new() { Frequency = Frequency.Monthly, Start = a.OpeningDate } };
        var paused = new Schedule { Name = "Fictitious original paused transfer", AccountId = b.Id, ToAccountId = a.Id, Amount = 5678,
            State = ScheduleState.Paused, PausedFrom = new(2026, 10, 1), Rule = new() { Frequency = Frequency.Once, Start = new(2026, 12, 1) } };
        await plans.SaveSchedulesAsync([active, paused], Ct);
        await plans.SaveScheduleAsync(new() { Name = "Fictitious unaffected plan", AccountId = b.Id, Amount = 4321, Rule = new() { Start = a.OpeningDate } }, Ct);
        var settings = await store.GetSettingsAsync(Ct); settings.DefaultAccountId = a.Id; await store.SaveSettingsAsync(settings, Ct);
        return new(store, plans, access, a, b, active, paused);
    }
    private static AccountEditorViewModel Editor(FlowFixture f, Seed s) => new(s.Store, s.Plans, f.Translator, f.Localization, f.Time, f.Platform);

    [Theory]
    [InlineData("en")][InlineData("fa")][InlineData("de")][InlineData("es")][InlineData("fr")][InlineData("it")]
    public async Task Original_archive_discards_only_explicitly_confirmed_drafts_and_ends_both_directions_with_default_in_one_write(string language)
    {
        using var f = new FlowFixture(); f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(item => item.CultureName == language));
        var s = await PrepareAsync(f); var vm = Editor(f, s); await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = s.Account.Id });
        vm.Form.Name = "Unsaved financial identity"; vm.Form.OpeningText = "999"; var raw = vm.Form.Snapshot();
        var before = await GoalChooserFlowTests.SnapshotAsync(f); f.Platform.Confirmations.Enqueue(false); await vm.ToggleArchiveCommand.ExecuteAsync(null);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Equal(raw, vm.Form.Snapshot()); Assert.True(vm.CanInteract);
        Assert.Contains(f.Translator["Account_ArchiveDraftDiscard"], Assert.Single(f.Platform.ConfirmationDialogs).Message);
        f.Platform.Confirmations.Enqueue(true); await vm.ToggleArchiveCommand.ExecuteAsync(null); Assert.Null(vm.SaveError);
        var saved = (await s.Store.GetAccountsAsync(cancellationToken: Ct)).Single(row => row.Id == s.Account.Id);
        Assert.True(saved.IsArchived); Assert.Equal(s.Account.Name, saved.Name); Assert.Equal(12345, saved.OpeningBalance);
        Assert.Equal(s.Account.Name, vm.Form.Name); Assert.False(vm.CanInteract); Assert.Equal(f.Translator["Common_Close"], vm.CloseText);
        var plans = await s.Plans.GetSchedulesAsync(Ct); Assert.All(plans.Where(row => row.Id == s.Active.Id || row.Id == s.Paused.Id), row =>
        { Assert.Equal(ScheduleState.Ended, row.State); Assert.False(row.AutoPost); Assert.Null(row.PausedFrom); });
        Assert.Equal(ScheduleState.Active, plans.Single(row => row.Name == "Fictitious unaffected plan").State);
        Assert.Null((await s.Store.GetSettingsAsync(Ct)).DefaultAccountId); Assert.Empty(await s.Store.GetEntriesAsync(cancellationToken: Ct));
        var accepted = await GoalChooserFlowTests.SnapshotAsync(f); await vm.ToggleArchiveCommand.ExecuteAsync(null); Assert.Equal(accepted, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Theory]
    [InlineData("Accounts")][InlineData("Schedules")][InlineData("Settings")]
    public async Task A_real_sql_refusal_rolls_back_account_plans_default_and_persisted_choices_with_no_publication(string table)
    {
        using var f = new FlowFixture(); var s = await PrepareAsync(f);
        s.Access.Current = new(f.Directory.Combine("zanance.db"), new(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, Guid.Parse("56565656-0101-1212-3434-454545454545"))));
        var choices = new ResourceChoiceStore(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), s.Access);
        Assert.True(await choices.SaveAsync(await choices.ReadAsync(QuotaKind.FinancialAccounts, Ct), [s.Account.Id, s.Other.Id], Ct));
        Assert.True(await choices.SaveAsync(await choices.ReadAsync(QuotaKind.RecurringPlans, Ct), [s.Active.Id, s.Paused.Id], Ct));
        var trigger = table switch
        {
            "Accounts" => "CREATE TRIGGER OwnedArchiveRefusal BEFORE UPDATE ON Accounts BEGIN SELECT RAISE(ABORT, 'Owned atomic archive refusal'); END;",
            "Schedules" => "CREATE TRIGGER OwnedArchiveRefusal BEFORE UPDATE ON Schedules BEGIN SELECT RAISE(ABORT, 'Owned atomic archive refusal'); END;",
            "Settings" => "CREATE TRIGGER OwnedArchiveRefusal BEFORE UPDATE ON Settings BEGIN SELECT RAISE(ABORT, 'Owned atomic archive refusal'); END;",
            _ => throw new InvalidOperationException("Unknown owned test table."),
        };
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
            await db.Database.ExecuteSqlRawAsync(trigger, Ct);
        var reviewed = await s.Store.ReadAccountArchiveAsync(s.Account.Id, Ct); var before = await GoalChooserFlowTests.SnapshotAsync(f); var changes = 0;
        s.Store.Changed += (_, _) => changes++; s.Plans.Changed += (_, _) => changes++;
        await Assert.ThrowsAnyAsync<Exception>(() => s.Store.WriteAccountArchiveAsync(reviewed, true, s.Plans, Ct));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Equal(0, changes);
    }

    [Theory]
    [InlineData("account")][InlineData("plan")][InlineData("added")][InlineData("day")]
    public async Task Complete_review_rechecks_original_values_and_day_before_mutating_any_row(string mutation)
    {
        using var f = new FlowFixture(); var s = await PrepareAsync(f); var reviewed = await s.Store.ReadAccountArchiveAsync(s.Account.Id, Ct);
        if (mutation == "day") f.Time.Now = f.Time.Now.AddDays(1);
        else if (mutation == "added") await s.Plans.SaveScheduleAsync(new() { Name = "Fictitious later related plan", AccountId = s.Account.Id,
            Rule = new() { Start = new(2026, 10, 9) } }, Ct);
        else
        {
            await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
            if (mutation == "account") await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Accounts SET OpeningBalance=OpeningBalance+1 WHERE Id={s.Account.Id}", Ct);
            else await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Schedules SET Note={"Changed at the same audit time"} WHERE Id={s.Active.Id}", Ct);
        }
        var current = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<InvalidOperationException>(() => s.Store.WriteAccountArchiveAsync(reviewed, true, s.Plans, Ct));
        Assert.Equal(current, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task A_changed_other_default_is_preserved_and_missing_settings_are_never_created_by_archive_review()
    {
        using var f = new FlowFixture(); var s = await PrepareAsync(f); var reviewed = await s.Store.ReadAccountArchiveAsync(s.Account.Id, Ct);
        var prefs = await s.Store.GetSettingsAsync(Ct); prefs.DefaultAccountId = s.Other.Id; await s.Store.SaveSettingsAsync(prefs, Ct);
        var result = await s.Store.WriteAccountArchiveAsync(reviewed, true, s.Plans, Ct); Assert.False(result.DefaultCleared);
        Assert.Equal(s.Other.Id, (await s.Store.GetSettingsAsync(Ct)).DefaultAccountId);
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct)) await db.Settings.ExecuteDeleteAsync(Ct);
        var preview = await s.Store.ReadAccountArchiveAsync(s.Account.Id, Ct); await s.Store.WriteAccountArchiveAsync(preview, false, s.Plans, Ct);
        await using var checking = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct); Assert.False(await checking.Settings.AnyAsync(Ct));
        Assert.All((await s.Plans.GetSchedulesAsync(Ct)).Where(row => row.Id == s.Active.Id || row.Id == s.Paused.Id), row => Assert.Equal(ScheduleState.Ended, row.State));
    }

    [Fact]
    public async Task Unarchive_reuses_the_original_quota_writer_without_resuming_ended_plans_or_losing_input()
    {
        using var f = new FlowFixture(); var s = await PrepareAsync(f); await s.Store.WriteAccountArchiveAsync(await s.Store.ReadAccountArchiveAsync(s.Account.Id, Ct), true, s.Plans, Ct);
        for (var i = 0; i < 2; i++) await s.Store.SaveAccountAsync(new() { Name = "Fictitious later account " + i, CurrencyCode = "EUR", OpeningDate = s.Account.OpeningDate }, Ct);
        s.Access.Current = new(f.Directory.Combine("zanance.db"), new(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, Guid.Parse("56565656-0101-1212-3434-454545454545"))));
        var vm = Editor(f, s); await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = s.Account.Id }); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await vm.ToggleArchiveCommand.ExecuteAsync(null); Assert.Contains(f.Translator["Commercial_DraftKept"], vm.SaveError!); Assert.True(vm.IsArchived); Assert.True(vm.CanInteract);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Empty(f.Platform.Routes);
    }

    [Fact]
    public async Task A_post_commit_subscriber_failure_notifies_both_original_stores_and_cannot_repeat_archive()
    {
        using var f = new FlowFixture(); var s = await PrepareAsync(f); var vm = Editor(f, s); await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = s.Account.Id });
        var accountSignals = 0; var planSignals = 0;
        s.Store.Changed += (_, _) => { accountSignals++; throw new InvalidOperationException("Fictitious original account subscriber refusal."); };
        s.Plans.Changed += (_, _) => planSignals++;
        f.Platform.Confirmations.Enqueue(true); await vm.ToggleArchiveCommand.ExecuteAsync(null);
        Assert.Equal(f.Translator["Account_ArchiveSavedReview"], vm.SaveError); Assert.True(vm.IsArchived); Assert.False(vm.CanInteract);
        Assert.Equal(1, accountSignals); Assert.Equal(1, planSignals); var accepted = await GoalChooserFlowTests.SnapshotAsync(f);
        await vm.ToggleArchiveCommand.ExecuteAsync(null); Assert.Equal(accepted, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Empty(f.Platform.Routes);
    }

    [Fact]
    public async Task Foreign_file_or_notification_owner_and_changed_cached_access_reject_before_any_write()
    {
        using var f = new FlowFixture(); using var other = new FlowFixture(); var s = await PrepareAsync(f); var reviewed = await s.Store.ReadAccountArchiveAsync(s.Account.Id, Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<InvalidOperationException>(() => s.Store.WriteAccountArchiveAsync(reviewed, true, other.Services.GetRequiredService<PlanStore>(), Ct));
        await Assert.ThrowsAsync<InvalidOperationException>(() => other.Store.WriteAccountArchiveAsync(reviewed, true, other.Services.GetRequiredService<PlanStore>(), Ct));
        s.Access.Current = new(f.Directory.Combine("zanance.db"), new(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, Guid.Parse("56565656-0101-1212-3434-454545454545"))));
        await Assert.ThrowsAsync<InvalidOperationException>(() => s.Store.WriteAccountArchiveAsync(reviewed, true, s.Plans, Ct));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
}
