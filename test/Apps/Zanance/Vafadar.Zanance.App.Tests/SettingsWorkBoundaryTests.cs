using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Settings;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>D-648: actual original SQLite preference and explicit profile-reset transaction boundaries.</summary>
public sealed class SettingsWorkBoundaryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static async Task SeedAsync(FlowFixture f)
    {
        await f.Store.GetSettingsAsync(Ct);
        await f.Store.SaveAccountAsync(new Account { Name = "D648 original account", CurrencyCode = "EUR", OpeningBalance = 0, OpeningDate = new(2026, 10, 1) }, Ct);
    }
    private static Task<SettingsWorkSnapshot> ReadAsync(FlowFixture f) => f.Store.ReadSettingsWorkSnapshotAsync(PeriodCalendar.Gregorian, cancellationToken: Ct);
    /// <summary>Two explicit serialized changes from one original native view preserve each other's unrelated fields.</summary>
    [Fact]
    public async Task Original_preference_changes_preserve_unrelated_fresh_fields()
    {
        using var f = new FlowFixture(); await SeedAsync(f); var original = await ReadAsync(f);
        await f.Store.UpdateSettingsWorkAsync(original, settings => settings.ReportCurrencyCode = "USD", () => true, Ct);
        await f.Store.UpdateSettingsWorkAsync(original, settings => settings.MonthStartDay = 17, () => true, Ct);
        var current = await f.Store.GetSettingsAsync(Ct); Assert.Equal("USD", current.ReportCurrencyCode); Assert.Equal(17, current.MonthStartDay);
        Assert.Equal(original.Settings.DefaultCurrencyCode, current.DefaultCurrencyCode); Assert.Equal(original.Settings.Id, current.Id);
        Assert.Equal("D648 original account", Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: Ct)).Name); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
    /// <summary>Actual native retirement before or after preference SQL rolls back the original field change.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Preference_writer_rolls_back_retired_native_owner(bool afterSql)
    {
        using var f = new FlowFixture(); await SeedAsync(f); var original = await ReadAsync(f); var before = await GoalChooserFlowTests.SnapshotAsync(f); var checks = 0;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.UpdateSettingsWorkAsync(original, settings => settings.MonthStartDay = 17,
            () => ++checks < (afterSql ? 4 : 1), Ct));
        Assert.Equal(afterSql ? 4 : 1, checks); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    /// <summary>A real original packet cannot change another valid profile's preference row.</summary>
    [Fact]
    public async Task Original_preference_packet_refuses_another_actual_file()
    {
        using var first = new FlowFixture(); using var other = new FlowFixture(); await SeedAsync(first); await SeedAsync(other);
        var original = await ReadAsync(first); var before = await GoalChooserFlowTests.SnapshotAsync(other);
        await Assert.ThrowsAsync<InvalidOperationException>(() => other.Store.UpdateSettingsWorkAsync(original, settings => settings.MonthStartDay = 17, () => true, Ct));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(other));
    }
    /// <summary>Read-only native availability can finish later without keeping a departed settings acquisition alive.</summary>
    [Fact]
    public async Task Cancelled_original_availability_read_does_not_require_a_native_answer_to_finish()
    {
        using var f = new FlowFixture(); await SeedAsync(f); using var cancel = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var held = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = SettingsSnapshot.ReadAsync(f.Store, f.Localization, f.Time, () => { entered.TrySetResult(); return held.Task; }, false, () => Task.FromResult(false), cancel.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.WaitAsync(TimeSpan.FromSeconds(10), Ct)); held.SetResult(false);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
    /// <summary>Actual original profile reset refuses both initial retirement and retirement after its destructive SQL.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Profile_reset_rolls_back_when_original_consent_owner_retires(bool afterSql)
    {
        using var f = new FlowFixture(); await SeedAsync(f); var reviewed = await f.Store.ReadProfileResetReviewAsync(await ReadAsync(f), Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var checks = 0; var accepted = 0;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.DeleteAllDataAsync(Ct, reviewed, () => ++checks < (afterSql ? 3 : 1), () => accepted++));
        Assert.Equal(afterSql ? 3 : 1, checks); Assert.Equal(0, accepted); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    /// <summary>Metadata outside the preference/suggestion packet still invalidates original full-profile deletion consent.</summary>
    [Fact]
    public async Task Profile_reset_refuses_an_auxiliary_source_added_after_native_consent_capture()
    {
        using var f = new FlowFixture(); await SeedAsync(f); var reviewed = await f.Store.ReadProfileResetReviewAsync(await ReadAsync(f), Ct);
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        { db.AssetLocations.Add(new Core.Holdings.AssetLocation { Name = "D648 after consent" }); await db.SaveChangesAsync(Ct); }
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var accepted = 0;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.DeleteAllDataAsync(Ct, reviewed, () => true, () => accepted++));
        Assert.Equal(0, accepted); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        await using var current = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        Assert.Equal("D648 after consent", (await current.AssetLocations.SingleAsync(Ct)).Name);
    }
    /// <summary>The real original reset writer never targets another profile even when its native owner callback is true.</summary>
    [Fact]
    public async Task Profile_reset_refuses_another_actual_file_and_preserves_both_profiles()
    {
        using var first = new FlowFixture(); using var other = new FlowFixture(); await SeedAsync(first); await SeedAsync(other);
        var reviewed = await first.Store.ReadProfileResetReviewAsync(await ReadAsync(first), Ct); var before = await GoalChooserFlowTests.SnapshotAsync(other);
        await Assert.ThrowsAsync<InvalidOperationException>(() => other.Store.DeleteAllDataAsync(Ct, reviewed, () => true));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(other)); Assert.Single(await first.Store.GetAccountsAsync(cancellationToken: Ct));
    }
    /// <summary>A reset commit receipt precedes even a failing original Changed observer; another profile remains exact.</summary>
    [Fact]
    public async Task Profile_reset_receipt_precedes_notification_without_deleting_another_profile()
    {
        using var f = new FlowFixture(); using var other = new FlowFixture(); await SeedAsync(f); await SeedAsync(other);
        var reviewed = await f.Store.ReadProfileResetReviewAsync(await ReadAsync(f), Ct); var accepted = 0; var observerSawReceipt = false; var beforeOther = await GoalChooserFlowTests.SnapshotAsync(other);
        f.Store.Changed += (_, _) => { observerSawReceipt = accepted == 1; throw new InvalidOperationException("D648 original post-commit observer refusal"); };
        var error = await Assert.ThrowsAsync<CommittedWriteNotificationException<Guid>>(() => f.Store.DeleteAllDataAsync(Ct, reviewed, () => true, () => accepted++));
        Assert.Equal(reviewed.Id, error.Result); Assert.True(observerSawReceipt); Assert.Equal(1, accepted); Assert.Equal(beforeOther, await GoalChooserFlowTests.SnapshotAsync(other));
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        Assert.Equal(0, await db.Accounts.CountAsync(Ct)); Assert.Equal(0, await db.Settings.CountAsync(Ct)); Assert.Equal(0, await db.Entries.CountAsync(Ct));
    }
    /// <summary>Rejecting the actual initial file happens before the original first-settings initializer can write.</summary>
    [Fact]
    public async Task Refused_initial_settings_file_does_not_create_its_settings_row()
    {
        using var f = new FlowFixture();
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.ReadSettingsWorkSnapshotAsync(PeriodCalendar.Gregorian, _ => false, Ct));
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        Assert.Equal(0, await db.Settings.CountAsync(Ct)); Assert.Equal(0, await db.Entries.CountAsync(Ct));
    }

    /// <summary>Explicit profile deletion includes opaque historical source documents and financial display contacts, without provider/authentication acceptance.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Explicit_profile_reset_removes_original_owned_auxiliary_records(bool referenceReceipt)
    {
        using var f = new FlowFixture(); using var other = new FlowFixture(); await SeedAsync(f); await SeedAsync(other);
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        {
            if (referenceReceipt) db.MarketReferenceReceipts.Add(new Core.Rates.MarketReferenceReceipt
                { TargetId = Guid.NewGuid(), TargetFingerprint = new string('a', 64), QuoteDigest = new string('b', 64), QuoteJson = "{}", MappingJson = "{}" });
            else db.HouseholdContacts.Add(new Core.Sharing.HouseholdContact { Name = "D648 original financial display contact" });
            await db.SaveChangesAsync(Ct);
        }
        var otherBefore = await GoalChooserFlowTests.SnapshotAsync(other);
        var review = await f.Store.ReadProfileResetReviewAsync(await ReadAsync(f), Ct); await f.Store.DeleteAllDataAsync(Ct, review, () => true);
        await using var current = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        Assert.Equal(0, referenceReceipt ? await current.MarketReferenceReceipts.CountAsync(Ct) : await current.HouseholdContacts.CountAsync(Ct));
        Assert.Equal(otherBefore, await GoalChooserFlowTests.SnapshotAsync(other));
    }

    /// <summary>A local unbound historical cost/refund/allocation graph clears in FK order without service authority or ledger posting.</summary>
    [Fact]
    public async Task Unbound_local_profile_reset_clears_original_cost_refund_and_settlement_graph()
    {
        using var f = new FlowFixture(); using var other = new FlowFixture(); await SeedAsync(f); await SeedAsync(other);
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        {
            var first = new Core.Sharing.HouseholdContact { Name = "D648 local first" }; var second = new Core.Sharing.HouseholdContact { Name = "D648 local second" };
            var cost = new Core.Sharing.HouseholdCost { Title = "D648 original local cost", Kind = Core.Sharing.HouseholdCostKind.Expense,
                Date = new(2026, 10, 1), Amount = 100, CurrencyCode = "EUR", PaymentKind = Core.Sharing.HouseholdPaymentKind.Private, PaidByContactId = first.Id };
            var refund = new Core.Sharing.HouseholdCost { Title = "D648 original local refund", Kind = Core.Sharing.HouseholdCostKind.Refund,
                Date = new(2026, 10, 2), Amount = 10, CurrencyCode = "EUR", PaymentKind = Core.Sharing.HouseholdPaymentKind.Private, PaidByContactId = first.Id, RefundOfCostId = cost.Id };
            db.HouseholdContacts.AddRange(first, second); db.HouseholdCosts.AddRange(cost, refund);
            db.HouseholdAllocations.AddRange(new() { CostId = cost.Id, ContactId = first.Id, Amount = 50 }, new() { CostId = cost.Id, ContactId = second.Id, Amount = 50 },
                new() { CostId = refund.Id, ContactId = first.Id, Amount = 5 }, new() { CostId = refund.Id, ContactId = second.Id, Amount = 5 });
            db.HouseholdSettlements.Add(new() { FromContactId = second.Id, ToContactId = first.Id, Amount = 10, CurrencyCode = "EUR", Date = new(2026, 10, 3) });
            await db.SaveChangesAsync(Ct);
        }
        var beforeOther = await GoalChooserFlowTests.SnapshotAsync(other); var review = await f.Store.ReadProfileResetReviewAsync(await ReadAsync(f), Ct);
        await f.Store.DeleteAllDataAsync(Ct, review, () => true);
        await using var current = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        Assert.Equal(0, await current.HouseholdCosts.CountAsync(Ct)); Assert.Equal(0, await current.HouseholdContacts.CountAsync(Ct));
        Assert.Equal(0, await current.HouseholdAllocations.CountAsync(Ct)); Assert.Equal(0, await current.HouseholdSettlements.CountAsync(Ct));
        Assert.Equal(0, await current.Entries.CountAsync(Ct)); Assert.Equal(beforeOther, await GoalChooserFlowTests.SnapshotAsync(other));
    }

    /// <summary>An original visible default-account choice cannot be saved after its actual account becomes archived.</summary>
    [Fact]
    public async Task Changed_original_account_cannot_be_saved_as_a_stale_default_choice()
    {
        using var f = new FlowFixture(); await SeedAsync(f); var original = await ReadAsync(f); var account = Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: Ct));
        account.IsArchived = true; await f.Store.SaveAccountAsync(account, Ct); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.UpdateSettingsWorkAsync(original, settings => settings.DefaultAccountId = account.Id, () => true, Ct));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

}
