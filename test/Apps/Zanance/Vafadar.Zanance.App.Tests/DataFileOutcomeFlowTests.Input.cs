using Vafadar.Zanance.App.Features.DataFiles;
using Vafadar.Zanance.Core.DataFiles;

namespace Vafadar.Zanance.App.Tests;

public sealed partial class DataFileOutcomeFlowTests
{
    [Fact, Trait("AT", "AT-385")]
    public async Task An_unterminated_new_file_cannot_destroy_the_previous_complete_review()
    {
        using var f = new FlowFixture(); var files = new Files(); await LedgerAsync(f, files);
        var (_, _, _, vm) = Create(f, new(), files); await vm.LoadAsync(); await vm.PickCommand.ExecuteAsync(null);
        var name = vm.FileName; var summary = vm.PreviewText; var rows = vm.LedgerPreviewRows.ToArray();
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        files.Picked = new("fictitious-broken.csv", "date,amount,title\r\n2026-10-19,12.34,\"unfinished");
        await vm.PickCommand.ExecuteAsync(null);
        Assert.Equal(name, vm.FileName); Assert.Equal(summary, vm.PreviewText); Assert.Equal(rows, vm.LedgerPreviewRows);
        Assert.True(vm.HasPreview); Assert.True(vm.CanRunImport); Assert.False(vm.IsBusy); Assert.NotNull(vm.ImportError);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Theory, Trait("AT", "AT-385")]
    [InlineData("en")][InlineData("fa")][InlineData("de")][InlineData("es")][InlineData("fr")][InlineData("it")]
    public async Task Every_resource_refusal_keeps_the_previous_review_and_has_complete_localized_feedback(string language)
    {
        using var f = new FlowFixture(); f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(row => row.CultureName == language));
        var files = new Files(); await LedgerAsync(f, files); var (_, _, _, vm) = Create(f, new(), files);
        await vm.LoadAsync(); await vm.PickCommand.ExecuteAsync(null); var draft = Draft(vm); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        foreach (var limit in Enum.GetValues<CsvReadLimit>())
        {
            var maximum = limit == CsvReadLimit.Bytes ? CsvReadLimits.Import.MaximumBytes : 100;
            files.PendingPick = () => throw new CsvReadLimitException(limit, maximum);
            await vm.PickCommand.ExecuteAsync(null);
            Assert.Equal(draft, Draft(vm)); Assert.True(vm.CanRunImport); Assert.False(vm.IsBusy);
            Assert.Equal(f.Translator.Format("Import_ReadLimit", f.Translator.Format("Import_Limit_" + limit,
                limit == CsvReadLimit.Bytes ? 64 : 100)), vm.ImportError);
            Assert.DoesNotContain("Import_", vm.ImportError!);
        }
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Theory, Trait("AT", "AT-385")]
    [InlineData(false)][InlineData(true)]
    public async Task Malformed_selection_retains_the_complete_original_ledger_or_holding_review(bool holdings)
    {
        using var f = new FlowFixture(); var files = new Files(); if (holdings) await HoldingsAsync(f, files); else await LedgerAsync(f, files);
        var (_, _, _, vm) = Create(f, new(), files); await vm.LoadAsync(); await vm.PickCommand.ExecuteAsync(null);
        var draft = Draft(vm); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        files.Picked = new("fictitious-incomplete.csv", "a,b\r\n1,\"incomplete"); await vm.PickCommand.ExecuteAsync(null);
        Assert.Equal(draft, Draft(vm)); Assert.True(vm.CanRunImport); Assert.False(vm.IsBusy);
        Assert.Equal(f.Translator["Import_ReadMalformed"], vm.ImportError); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact, Trait("AT", "AT-385")]
    public async Task A_refused_input_after_success_cannot_reactivate_the_accepted_payload_or_erase_its_outcome()
    {
        using var f = new FlowFixture(); var files = new Files(); await LedgerAsync(f, files);
        var (_, _, _, vm) = Create(f, new(), files); await vm.LoadAsync(); await vm.PickCommand.ExecuteAsync(null); await vm.ImportCommand.ExecuteAsync(null);
        var outcome = vm.ImportResult; var batch = Assert.Single(vm.Batches); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        files.Picked = new("fictitious-broken-after-save.csv", "a,\"unfinished"); await vm.PickCommand.ExecuteAsync(null);
        Assert.Equal(outcome, vm.ImportResult); Assert.Same(batch, Assert.Single(vm.Batches)); Assert.False(vm.CanRunImport); Assert.False(vm.HasPreview);
        await vm.ImportCommand.ExecuteAsync(null); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact, Trait("AT", "AT-385")]
    public async Task A_new_valid_own_file_is_not_published_before_its_database_analysis_succeeds()
    {
        using var f = new FlowFixture(); var files = new Files(); await LedgerAsync(f, files);
        var (_, _, factory, vm) = Create(f, new(), files); await vm.LoadAsync(); await vm.PickCommand.ExecuteAsync(null);
        var draft = Draft(vm); var before = await GoalChooserFlowTests.SnapshotAsync(f); var next = files.Picked! with { Name = "fictitious-next.csv" };
        files.PendingPick = () => { factory.Refuse = true; return Task.FromResult<PickedDataFile?>(next); };
        await vm.PickCommand.ExecuteAsync(null); factory.Refuse = false;
        Assert.Equal(draft, Draft(vm)); Assert.True(vm.CanRunImport); Assert.Single(f.Platform.Failures); Assert.False(vm.IsBusy);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact, Trait("AT", "AT-385")]
    public async Task A_native_picker_return_for_a_retired_form_cannot_replace_the_original_review()
    {
        using var f = new FlowFixture(); var files = new Files(); await LedgerAsync(f, files);
        var (_, _, _, vm) = Create(f, new(), files); await vm.LoadAsync(); await vm.PickCommand.ExecuteAsync(null); var draft = Draft(vm);
        files.PendingPick = () => { vm.RetireBankReview(); return Task.FromResult<PickedDataFile?>(files.Picked! with { Name = "fictitious-retired.csv" }); };
        await vm.PickCommand.ExecuteAsync(null); Assert.Equal(draft, Draft(vm)); Assert.False(vm.IsBusy);
    }

    [Fact, Trait("AT", "AT-385")]
    public async Task The_original_application_can_review_a_hundred_thousand_rows_without_importing_or_truncating_them()
    {
        using var f = new FlowFixture(); var files = new Files(); var (account, _) = await LedgerAsync(f, files);
        var entries = Enumerable.Range(1, 100000).Select(index => new Vafadar.Zanance.Core.Ledger.LedgerEntry
            { AccountId = account.Id, Kind = Vafadar.Zanance.Core.Ledger.EntryKind.Expense, Date = new(2026, 10, 9), Amount = index,
                Title = "Fictitious complete source " + index }).ToArray();
        files.Picked = new("fictitious-complete-100k.csv", CsvExport.Write(entries,
            new Dictionary<Guid, Vafadar.Zanance.Core.Accounts.Account> { [account.Id] = account }, _ => "", true));
        var (_, _, _, vm) = Create(f, new(), files); await vm.LoadAsync(); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await vm.PickCommand.ExecuteAsync(null);
        Assert.Equal(100000, vm.ImportCount); Assert.True(vm.CanRunImport); Assert.Null(vm.ImportError); Assert.Equal(10, vm.LedgerPreviewRows.Count);
        Assert.Equal(2, vm.LedgerPreviewRows[0].SourceLine); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Theory, Trait("AT", "AT-385")]
    // D-405: one coherent financial snapshot replaces three separate reads; final file recheck is call4.
    [InlineData(2)][InlineData(4)]
    public async Task Mapping_changes_during_worker_reads_or_final_profile_revalidation_cannot_publish_an_obsolete_preview(int blockedCall)
    {
        using var f = new FlowFixture(); var files = new Files(); var (account, _) = await LedgerAsync(f, files);
        files.Picked = new("fictitious-generic-worker.csv", "date,amount,title\r\n2026-10-09,12.34,Fictitious worker input\r\n");
        var (_, _, factory, vm) = Create(f, new(), files); await vm.LoadAsync(); await vm.PickCommand.ExecuteAsync(null);
        vm.Account = vm.Accounts.Single(row => row.Id == account.Id); vm.DateFormat = "yyyy-MM-dd";
        await vm.RefreshPreviewCommand.ExecuteAsync(null); Assert.True(vm.CanRunImport);
        var originalRows = vm.LedgerPreviewRows.ToArray(); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var calls = 0;
        factory.BeforeCreateAsync = async () => { if (Interlocked.Increment(ref calls) == blockedCall) { entered.SetResult(); await release.Task; } };
        var pending = vm.RefreshPreviewCommand.ExecuteAsync(null);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct); vm.SignIndex = 2; release.SetResult(); await pending;
        factory.BeforeCreateAsync = null;
        Assert.False(vm.CanRunImport); Assert.Equal(f.Translator["Import_InterpretationChanged"], vm.ImportError);
        Assert.Equal(originalRows, vm.LedgerPreviewRows); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        await vm.RefreshPreviewCommand.ExecuteAsync(null); Assert.True(vm.CanRunImport);
        Assert.Contains(f.Translator["EntryKind_Income"], vm.LedgerPreviewRows[0].Heading);
    }

    [Fact, Trait("AT", "AT-385")]
    public async Task Provider_IO_failure_keeps_the_original_file_review_and_financial_data()
    {
        using var f = new FlowFixture(); var files = new Files(); await LedgerAsync(f, files);
        var (_, _, _, vm) = Create(f, new(), files); await vm.LoadAsync(); await vm.PickCommand.ExecuteAsync(null);
        var draft = Draft(vm); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        files.PendingPick = () => throw new IOException("Fictitious selected provider refusal."); await vm.PickCommand.ExecuteAsync(null);
        Assert.Equal(draft, Draft(vm)); Assert.True(vm.CanRunImport); Assert.False(vm.IsBusy); Assert.Single(f.Platform.Failures);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Theory, Trait("AT", "AT-385")]
    [InlineData(false, false)][InlineData(false, true)][InlineData(true, false)][InlineData(true, true)]
    public async Task Empty_or_header_only_selection_retains_the_previous_review_or_accepted_result(bool accepted, bool headerOnly)
    {
        using var f = new FlowFixture(); var files = new Files(); await LedgerAsync(f, files);
        var (_, _, _, vm) = Create(f, new(), files); await vm.LoadAsync(); await vm.PickCommand.ExecuteAsync(null);
        if (accepted) await vm.ImportCommand.ExecuteAsync(null);
        var draft = Draft(vm); var result = vm.ImportResult; var before = await GoalChooserFlowTests.SnapshotAsync(f);
        files.Picked = new("fictitious-empty.csv", headerOnly ? "date,amount,title\r\n" : ""); await vm.PickCommand.ExecuteAsync(null);
        Assert.Equal(draft, Draft(vm)); Assert.Equal(result, vm.ImportResult); Assert.Equal(!accepted, vm.CanRunImport);
        Assert.Equal(f.Translator["Import_Empty"], vm.ImportError); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
}
