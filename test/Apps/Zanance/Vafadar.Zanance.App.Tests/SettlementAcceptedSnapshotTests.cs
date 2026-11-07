using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.App.Tests;

/// <summary>A distinct legitimate bill still commits once through the new original snapshot contract and retains its accepted receipt.</summary>
public sealed class SettlementAcceptedSnapshotTests
{
    [Fact]
    public async Task A_new_bound_GBP_bill_keeps_its_accepted_receipt_after_notification_failure()
    {
        using var f = new FlowFixture(); var ct = TestContext.Current.CancellationToken;
        var id = await SettlementOwnershipSnapshotTests.SeedAsync(f);
        await using (var db = await OccurrenceOriginFailureTests.Factory(f).CreateDbContextAsync(ct))
        {
            var account = await db.Accounts.SingleAsync(ct); account.CurrencyCode = "GBP";
            var plan = await db.Schedules.SingleAsync(ct); plan.Name = "Distinct bound GBP bill";
            await db.SaveChangesAsync(ct);
        }
        var original = await f.Store.ReadAdvanceSettlementDraftAsync(id, ct);
        var before = JsonSerializer.Serialize(new { original.Plan, original.Account, original.Entries });
        var model = new SettlementViewModel(f.Store, f.Translator, f.Localization, f.Time, f.Platform);
        await model.LoadAsync(id); model.From = OccurrenceOriginFailureTests.Day; model.To = OccurrenceOriginFailureTests.Day; model.ActualText = "3.45";
        Assert.True(model.CanSave); var acceptedNotifications = 0;
        f.Store.Changed += (_, _) => { acceptedNotifications++; throw new IOException("New bound bill post-commit notification failure"); };
        await model.SaveCommand.ExecuteAsync(null);
        Assert.True(model.HasCommitted, $"Error={model.Error}; acceptedNotifications={acceptedNotifications}; feedback={model.SaveFeedback}"); Assert.False(model.CanEdit); Assert.False(model.CanSave);
        Assert.Equal(1, acceptedNotifications); Assert.Equal("3.45", model.ActualText);
        Assert.Equal(f.Translator["Common_Back"], model.SaveText);
        Assert.Equal(f.Translator["Settlement_Recorded"], model.SaveFeedback);
        Assert.Equal(f.Translator["Settlement_RecordedFollowUpFailed"], model.Error); Assert.Empty(f.Platform.Routes);
        var after = await f.Store.ReadAdvanceSettlementDraftAsync(id, ct);
        var bill = Assert.Single(after.Entries, e => original.Entries.All(old => old.Id != e.Id));
        Assert.Equal(222, bill.Amount); Assert.Equal(EntryKind.Expense, bill.Kind); Assert.Equal(ReviewState.Confirmed, bill.Review);
        Assert.Equal(original.Account.Id, bill.AccountId); Assert.Equal(DateOnly.FromDateTime(f.Time.GetLocalNow().DateTime), bill.Date); Assert.Null(bill.ScheduleId);
        var retained = after.Entries.Where(e => e.Id != bill.Id).ToList();
        Assert.Equal(before, JsonSerializer.Serialize(new { after.Plan, after.Account, Entries = retained }));
        // Do not press Save/Undo/Reload again: this new accepted-operation checkpoint is terminal.
    }
}
