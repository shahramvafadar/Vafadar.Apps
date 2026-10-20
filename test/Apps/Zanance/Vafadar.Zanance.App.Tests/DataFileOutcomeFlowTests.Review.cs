using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

public sealed partial class DataFileOutcomeFlowTests
{
    [Theory, Trait("AT", "AT-405")]
    [InlineData(false)][InlineData(true)]
    public async Task A_changed_opening_boundary_cannot_post_rows_under_an_older_ordinary_CSV_review(bool generic)
    {
        using var f = new FlowFixture(); var files = new Files(); var (account, _) = await LedgerAsync(f, files);
        account.OpeningDate = new(2026, 10, 20); await f.Store.SaveAccountAsync(account, Ct);
        if (generic) files.Picked = new("fictitious-baseline.csv", "date;amount;title\r\n2026-10-09;-12.34;Fictitious historical purchase\r\n");
        var (store, _, _, vm) = Create(f, new(), files); await vm.LoadAsync(); await vm.PickCommand.ExecuteAsync(null);
        if (generic) { vm.CalendarIndex = 0; vm.SeparatorIndex = 0; await vm.PreviewCommand.ExecuteAsync(null); }
        Assert.True(vm.CanRunImport); Assert.Contains(f.Translator.Format("Import_BeforeOpening", 1), vm.WarningText!);
        // Moving the baseline makes this previously historical row affect the recorded balance.
        account.OpeningDate = new(2026, 10, 1); await f.Store.SaveAccountAsync(account, Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await vm.ImportCommand.ExecuteAsync(null);
        Assert.Empty(await store.GetEntriesAsync(cancellationToken: Ct));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.NotNull(vm.ImportError);
        Assert.Null(vm.ImportResult); Assert.False(vm.CanRunImport);
        await vm.RefreshPreviewCommand.ExecuteAsync(null); Assert.True(vm.CanRunImport); Assert.Null(vm.WarningText);
        await vm.ImportCommand.ExecuteAsync(null);
        var saved = Assert.Single(await store.GetEntriesAsync(cancellationToken: Ct)); Assert.Equal(1234, saved.Amount);
        Assert.Equal(88766, LedgerCalculator.Balance(account, [saved], new(2026, 10, 20)));
    }
    [Theory, Trait("AT", "AT-405")]
    [InlineData(false, false)][InlineData(true, false)][InlineData(false, true)][InlineData(true, true)]
    public async Task Ordinary_CSV_review_refuses_changed_opening_amount_or_new_duplicate_history(bool generic, bool history)
    {
        using var f = new FlowFixture(); var files = new Files(); var (account, original) = await LedgerAsync(f, files);
        if (generic) files.Picked = new("fictitious-current.csv", "date;amount;title\r\n2026-10-09;-12.34;Fictitious current purchase\r\n");
        var (store, _, _, vm) = Create(f, new(), files); await vm.LoadAsync(); await vm.PickCommand.ExecuteAsync(null);
        if (generic) { vm.CalendarIndex = 0; vm.SeparatorIndex = 0; await vm.PreviewCommand.ExecuteAsync(null); }
        Assert.True(vm.CanRunImport);
        if (history) await f.Store.SaveEntryAsync(new LedgerEntry { AccountId = account.Id, Kind = original.Kind,
            Date = original.Date, Amount = original.Amount, Title = "Fictitious later duplicate candidate" }, Ct);
        else { account.OpeningBalance++; await f.Store.SaveAccountAsync(account, Ct); }
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await vm.ImportCommand.ExecuteAsync(null);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.False(vm.CanRunImport);
        Assert.Equal(f.Translator["Import_Overlap_ChangedPreview"], vm.ImportError); Assert.Null(vm.ImportResult);
        Assert.True(vm.CanReviewPreview); await vm.RefreshPreviewCommand.ExecuteAsync(null);
        Assert.True(vm.CanRunImport); Assert.Equal(history && generic, vm.HasDuplicates);
    }

    [Theory, Trait("AT", "AT-405")]
    [InlineData(false)][InlineData(true)]
    public async Task Departed_ordinary_CSV_form_retains_raw_input_but_requires_a_new_financial_review(bool generic)
    {
        using var f = new FlowFixture(); var files = new Files(); await LedgerAsync(f, files);
        if (generic) files.Picked = new("fictitious-departed.csv", "date;amount;title\r\n2026-10-09;-12.34;Fictitious original source\r\n");
        var (store, _, _, vm) = Create(f, new(), files); await vm.LoadAsync(); await vm.PickCommand.ExecuteAsync(null);
        if (generic) { vm.CalendarIndex = 0; vm.SeparatorIndex = 0; await vm.PreviewCommand.ExecuteAsync(null); }
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var draft = Draft(vm);
        vm.RetireBankReview(); Assert.Equal(draft, Draft(vm)); Assert.False(vm.CanRunImport); Assert.True(vm.CanReviewPreview);
        await vm.ImportCommand.ExecuteAsync(null); Assert.Empty(await store.GetEntriesAsync(cancellationToken: Ct));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        await vm.RefreshPreviewCommand.ExecuteAsync(null); Assert.True(vm.CanRunImport); Assert.Equal(draft, Draft(vm));
    }

}
