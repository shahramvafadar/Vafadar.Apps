using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Goals;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>The original share-income preview consumes the complete closed-month net without saving its draft.</summary>
[Collection("Entry validation units")]
public sealed class EligibleIncomeOriginalPreviewTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class Notifications : IGoalEditorNotifications
    {
        public bool IsSupported => false;
        public Task<bool> EnableAsync() => throw new InvalidOperationException("An original read must not request notification permission.");
    }

    /// <summary>The actual reader and retained-plan preview preserve all source tables through an unsaved note edit.</summary>
    [Fact]
    public async Task Original_share_preview_keeps_closed_month_net_and_unsaved_note_without_writing()
    {
        using var f = new FlowFixture(); f.Time.Now = new(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);
        f.Localization.SetFormattingCulture("en-US"); await f.Store.GetSettingsAsync(Ct);
        var from = new DateOnly(2026, 10, 1);
        var accounts = new[] { new Account { Name = "Fictitious preview A", Type = AccountType.Checking, CurrencyCode = "EUR", OpeningDate = from },
            new Account { Name = "Fictitious preview B", Type = AccountType.Checking, CurrencyCode = "EUR", OpeningDate = from } };
        var income = new LedgerEntry { Kind = EntryKind.Income, AccountId = accounts[0].Id, Amount = long.MaxValue, Date = from, Tags = ["Retained first income"] };
        var entries = new[] { income,
            new LedgerEntry { Kind = EntryKind.Income, AccountId = accounts[1].Id, Amount = long.MaxValue, Date = from.AddDays(1), Tags = ["Retained second income"] },
            new LedgerEntry { Kind = EntryKind.IncomeReversal, AccountId = accounts[0].Id, Amount = long.MaxValue, Date = from.AddDays(2), RefundOfId = income.Id, Tags = ["Retained linked reversal"] } };
        foreach (var account in accounts) foreach (var at in entries.Select(e => e.Date)) Assert.True(LedgerCalculator.Balance(account, entries, at) >= 0);
        var goal = new Goal { Name = "Fictitious share-income preview", Type = GoalType.Earmark, CurrencyCode = "EUR", TargetAmount = long.MaxValue };
        var plan = new ContributionPlan { GoalId = goal.Id, Method = ContributionMethod.ShareOfIncome, Percent = 10m,
            ReminderEnabled = false, Rule = new() { Frequency = Frequency.Monthly, Interval = 1, Start = new(2026, 11, 1) } };
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        {
            await using var transaction = await db.Database.BeginTransactionAsync(Ct);
            db.Accounts.AddRange(accounts); db.Entries.AddRange(entries); db.Goals.Add(goal); db.ContributionPlans.Add(plan);
            await db.SaveChangesAsync(Ct); await transaction.CommitAsync(Ct);
        }
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var dates = new DateFormatter(f.Localization);
        var holdings = f.Services.GetRequiredService<HoldingStore>();
        var vm = new GoalEditorViewModel(f.Store, f.Services.GetRequiredService<GoalStore>(), f.Translator, f.Localization,
            dates, f.Time, holdings, new HoldingText(f.Translator, f.Localization, dates), f.Platform, new Notifications());
        await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = goal.Id });
        Assert.True(vm.IsReady); Assert.Null(vm.SaveError); Assert.True(vm.IsShareMethod);
        var percentAtOpen = vm.PercentText;
        // SQLite retains decimal scale as 10.0; assert the intended value and keep the original raw field.
        Assert.Equal(10m, decimal.Parse(percentAtOpen, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Contains("Last month's eligible income", vm.PreviewMethod, StringComparison.Ordinal);
        Assert.Contains("92,233,720,368,547,758.07", vm.PreviewMethod, StringComparison.Ordinal);
        Assert.Contains("(transactions: 3) × 10 %", vm.PreviewMethod, StringComparison.Ordinal);
        Assert.Contains("9,223,372,036,854,775.81", vm.PreviewMethod, StringComparison.Ordinal);
        var preview = vm.PreviewMethod; vm.Note = "Retained income-share preview";
        Assert.Equal("Retained income-share preview", vm.Note); Assert.Equal(preview, vm.PreviewMethod);
        Assert.True(vm.IsDirty); Assert.Equal(percentAtOpen, vm.PercentText); Assert.Null(vm.SaveError);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Empty(f.Platform.Failures); Assert.Empty(f.Platform.Routes); Assert.Empty(f.Platform.ConfirmationDialogs);
        Assert.Equal(3, (await f.Store.GetEntriesAsync(cancellationToken: Ct)).Count);
    }
}
