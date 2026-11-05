using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Goals;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original earmark detail/editor readers keep complete pair nets and unaccepted drafts without writes.</summary>
[Collection("Entry validation units")]
public sealed class EarmarkNetOriginalFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class Notifications : IGoalEditorNotifications
    {
        public bool IsSupported => false;
        public Task<bool> EnableAsync() => throw new InvalidOperationException("No notification request belongs to a read.");
    }

    /// <summary>The complete original detail and its too-large release refusal preserve all source tables.</summary>
    [Fact]
    public async Task Original_detail_reads_the_net_and_refuses_an_excess_release_without_writing()
    {
        using var f = new FlowFixture(); var (goal, account) = await PrepareAsync(f);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var dates = new DateFormatter(f.Localization);
        var holdings = f.Services.GetRequiredService<HoldingStore>(); var goals = f.Services.GetRequiredService<GoalStore>();
        var presenter = new GoalPresenter(f.Translator, dates, f.Localization, holdings, new HoldingText(f.Translator, f.Localization, dates));
        var vm = new GoalDetailViewModel(f.Store, goals, f.Services.GetRequiredService<PlanStore>(), holdings,
            presenter, f.Translator, dates, f.Localization, f.Time, f.Platform);
        vm.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = goal.Id }); await vm.LoadAsync();
        Assert.True(vm.CanView); Assert.False(vm.HasReadFailed); Assert.True(vm.CanSetAside);
        Assert.Equal(3, vm.History.Count); Assert.Equal(account.Id, Assert.Single(vm.Accounts).Id);
        Assert.Contains("92,233,720,368,547,758.06", Assert.Single(vm.PerAccount), StringComparison.Ordinal);
        vm.DirectionIndex = 1; vm.SelectedAccount = Assert.Single(vm.Accounts); vm.AmountText = "92233720368547758.07";
        Assert.True(vm.CanAllocate); await vm.AllocateCommand.ExecuteAsync(null);
        Assert.NotNull(vm.AmountError); Assert.Null(vm.SaveError); Assert.Equal("92233720368547758.07", vm.AmountText);
        Assert.True(vm.CanView); Assert.False(vm.IsBusy);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Empty(f.Platform.Failures); Assert.Empty(f.Platform.Routes); Assert.Empty(f.Platform.ConfirmationDialogs);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    /// <summary>The original editor publishes the exact current earmark and keeps a new raw target/note draft.</summary>
    [Fact]
    public async Task Original_editor_reads_the_net_and_retains_raw_draft_without_save()
    {
        using var f = new FlowFixture(); var (goal, _) = await PrepareAsync(f);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var dates = new DateFormatter(f.Localization);
        var holdings = f.Services.GetRequiredService<HoldingStore>();
        var vm = new GoalEditorViewModel(f.Store, f.Services.GetRequiredService<GoalStore>(), f.Translator,
            f.Localization, dates, f.Time, holdings, new HoldingText(f.Translator, f.Localization, dates), f.Platform, new Notifications());
        await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = goal.Id });
        Assert.True(vm.IsReady); Assert.Null(vm.SaveError);
        Assert.Contains("92,233,720,368,547,758.06", vm.PreviewProgress, StringComparison.Ordinal);
        vm.AmountText = "123.4500"; vm.Note = "Retained earmark draft";
        Assert.Equal("123.4500", vm.AmountText); Assert.Equal("Retained earmark draft", vm.Note);
        // Original EUR parsing refuses more than two fraction digits, even zeros; keep the raw draft without a guessed preview.
        Assert.Null(vm.PreviewProgress);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Empty(f.Platform.Failures); Assert.Empty(f.Platform.Routes); Assert.Empty(f.Platform.ConfirmationDialogs);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    private static async Task<(Goal Goal, Account Account)> PrepareAsync(FlowFixture f)
    {
        f.Time.Now = new DateTimeOffset(2026, 11, 5, 12, 0, 0, TimeSpan.Zero); f.Localization.SetFormattingCulture("en-US");
        await f.Store.GetSettingsAsync(Ct);
        var day = new DateOnly(2026, 11, 1);
        var account = new Account { Name = "Fictitious full-range funding", Type = AccountType.Checking,
            CurrencyCode = "EUR", OpeningDate = day, OpeningBalance = long.MaxValue };
        var goal = new Goal { Name = "Fictitious signed-net goal", Type = GoalType.Earmark, CurrencyCode = "EUR", TargetAmount = long.MaxValue };
        var allocations = new[]
        {
            new GoalAllocation { GoalId = goal.Id, AccountId = account.Id, Amount = long.MaxValue, Date = day },
            new GoalAllocation { GoalId = goal.Id, AccountId = account.Id, Amount = long.MaxValue - 1, Date = day.AddDays(2) },
            new GoalAllocation { GoalId = goal.Id, AccountId = account.Id, Amount = -long.MaxValue, Date = day.AddDays(1) }
        };
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await using var transaction = await db.Database.BeginTransactionAsync(Ct);
        db.Accounts.Add(account); db.Goals.Add(goal); db.GoalAllocations.AddRange(allocations);
        for (var i = 0; i < allocations.Length; i++) db.Entry(allocations[i]).Property(a => a.Id).CurrentValue =
            Guid.Parse("d8780000-0000-4000-8000-" + (i + 3).ToString("D12", System.Globalization.CultureInfo.InvariantCulture));
        await db.SaveChangesAsync(Ct); await transaction.CommitAsync(Ct); return (goal, account);
    }
}
