using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.DataFiles;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.DataFiles;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.App.Tests;

public sealed partial class DataFileOutcomeFlowTests
{
    [Fact]
    [Trait("AT", "AT-332")]
    public async Task Page_arrival_signal_follows_only_actual_published_page_changes_without_writing()
    {
        using var f = new FlowFixture(); var files = new Files(); var (account, _) = await LedgerAsync(f, files);
        var rows = Enumerable.Range(1, 23).Select(index => new LedgerEntry
            { AccountId = account.Id, Date = new(2026, 10, 9), Kind = EntryKind.Expense, Amount = index }).ToArray();
        files.Picked = new("fictitious-arrival.csv", CsvExport.Write(rows,
            new Dictionary<Guid, Account> { [account.Id] = account }, _ => "", true));
        var (_, _, _, vm) = Create(f, new(), files); await vm.LoadAsync();
        var arrivals = new List<(int FirstLine, int Count)>();
        vm.LedgerPreviewPageChanged += (_, _) => arrivals.Add((vm.LedgerPreviewRows[0].SourceLine, vm.LedgerPreviewRows.Count));
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await vm.PickCommand.ExecuteAsync(null); vm.PreviousLedgerPageCommand.Execute(null); Assert.Empty(arrivals);
        vm.NextLedgerPageCommand.Execute(null); vm.NextLedgerPageCommand.Execute(null);
        Assert.Equal(new[] { (12, 10), (22, 3) }, arrivals);
        vm.NextLedgerPageCommand.Execute(null); vm.IsBusy = true; vm.PreviousLedgerPageCommand.Execute(null);
        Assert.Equal(2, arrivals.Count);
        vm.IsBusy = false; vm.PreviousLedgerPageCommand.Execute(null);
        Assert.Equal((12, 10), arrivals[^1]);
        vm.SkipDuplicates = true; await vm.RefreshPreviewCommand.ExecuteAsync(null);
        Assert.Equal(3, arrivals.Count); Assert.Equal(2, vm.LedgerPreviewRows[0].SourceLine);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Theory]
    [InlineData("en")][InlineData("fa")][InlineData("de")][InlineData("es")][InlineData("fr")][InlineData("it")]
    [Trait("AT", "AT-331")]
    public async Task Interpreted_ledger_pages_expose_source_and_destination_money_without_writing(string language)
    {
        using var f = new FlowFixture(); f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(x => x.CultureName == language));
        var files = new Files(); var (account, _) = await LedgerAsync(f, files);
        var destination = new Account { Name = "Fictitious dollar cash", CurrencyCode = "USD", OpeningDate = account.OpeningDate };
        await f.Store.SaveAccountAsync(destination, Ct);
        var rows = Enumerable.Range(1, 23).Select(i => new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense,
            Date = new(2026, 10, 9), Amount = i * 123, Title = $"Fictitious row {i}" }).ToList();
        rows[0].Kind = EntryKind.Transfer; rows[0].ToAccountId = destination.Id; rows[0].ToAmount = 456;
        rows[1].OriginalAmount = 789; rows[1].OriginalCurrencyCode = "USD";
        files.Picked = new("fictitious-paged.csv", CsvExport.Write(rows, new Dictionary<Guid, Account>
            { [account.Id] = account, [destination.Id] = destination }, _ => "", true));
        var (_, _, _, vm) = Create(f, new(), files); await vm.LoadAsync(); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await vm.PickCommand.ExecuteAsync(null);
        Assert.True(vm.HasLedgerPreview); Assert.Equal(10, vm.LedgerPreviewRows.Count);
        var first = vm.LedgerPreviewRows[0];
        Assert.Contains(account.Name, first.Accounts); Assert.Contains(destination.Name, first.Accounts);
        Assert.Equal(MoneyText.Format(123, "EUR", f.Localization.CurrentCulture), first.Amount);
        Assert.Equal(MoneyText.Format(456, "USD", f.Localization.CurrentCulture), first.DestinationAmount);
        Assert.Equal(MoneyText.Format(789, "USD", f.Localization.CurrentCulture), vm.LedgerPreviewRows[1].OriginalAmount);
        var lines = vm.LedgerPreviewRows.Select(x => x.SourceLine).ToList();
        vm.NextLedgerPageCommand.Execute(null); Assert.Equal(10, vm.LedgerPreviewRows.Count); lines.AddRange(vm.LedgerPreviewRows.Select(x => x.SourceLine));
        vm.NextLedgerPageCommand.Execute(null); Assert.Equal(3, vm.LedgerPreviewRows.Count); lines.AddRange(vm.LedgerPreviewRows.Select(x => x.SourceLine));
        Assert.False(vm.NextLedgerPageCommand.CanExecute(null)); Assert.Equal(Enumerable.Range(2, 23), lines);
        vm.PreviousLedgerPageCommand.Execute(null); Assert.Equal(12, vm.LedgerPreviewRows[0].SourceLine);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.True(vm.CanRunImport);
    }

    [Theory]
    [InlineData("en")][InlineData("fa")][InlineData("de")][InlineData("es")][InlineData("fr")][InlineData("it")]
    [Trait("AT", "AT-331")]
    public async Task Invalid_rows_beyond_summary_limit_and_known_ids_remain_reachable(string language)
    {
        using var f = new FlowFixture(); f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(x => x.CultureName == language));
        var files = new Files(); var (account, entry) = await LedgerAsync(f, files); await f.Store.SaveEntryAsync(entry, Ct);
        var rows = Csv.Read(files.Picked!.Text, ',').Select(row => row.ToArray()).ToList();
        for (var i = 0; i < 12; i++) { var invalid = rows[1].ToArray(); invalid[0] = "not-an-id"; rows.Add(invalid); }
        files.Picked = new("fictitious-errors.csv", Csv.Write(rows));
        var (_, _, _, vm) = Create(f, new(), files); await vm.LoadAsync(); await vm.PickCommand.ExecuteAsync(null);
        Assert.Contains(f.Translator["BankCsv_Known"], vm.LedgerPreviewRows[0].Status);
        Assert.Equal(8, vm.InvalidLines.Count); Assert.False(vm.CanRunImport);
        vm.NextLedgerPageCommand.Execute(null); Assert.Equal(3, vm.LedgerPreviewRows.Count);
        Assert.All(vm.LedgerPreviewRows, row => { Assert.Null(row.Amount); Assert.Equal(f.Translator["Import_ReviewedInvalid"], row.Status); });
        Assert.Equal(14, vm.LedgerPreviewRows[^1].SourceLine);
        Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Theory]
    [InlineData("en")][InlineData("fa")][InlineData("de")][InlineData("es")][InlineData("fr")][InlineData("it")]
    [Trait("AT", "AT-331")]
    public async Task Changed_generic_interpretation_requires_refresh_and_duplicate_choice_updates_visible_status(string language)
    {
        using var f = new FlowFixture(); f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(x => x.CultureName == language));
        var files = new Files(); var (account, _) = await LedgerAsync(f, files);
        await f.Store.SaveEntryAsync(new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Date = new(2026, 10, 9), Amount = 1234 }, Ct);
        files.Picked = new("fictitious-generic.csv", "date;amount;title\r\n2026-10-09;-12,34;Fictitious interpreted amount\r\n");
        var (_, _, _, vm) = Create(f, new(), files); await vm.LoadAsync(); await vm.PickCommand.ExecuteAsync(null);
        vm.CalendarIndex = 0; vm.SeparatorIndex = 1; await vm.PreviewCommand.ExecuteAsync(null);
        Assert.Single(vm.LedgerPreviewRows); Assert.Equal(MoneyText.Format(1234, "EUR", f.Localization.CurrentCulture), vm.LedgerPreviewRows[0].Amount);
        Assert.Equal(f.Translator["Import_ReviewedDuplicateIncluded"], vm.LedgerPreviewRows[0].Status);
        vm.SkipDuplicates = true; Assert.Equal(f.Translator["Import_ReviewedDuplicateSkipped"], vm.LedgerPreviewRows[0].Status);
        vm.SkipDuplicates = false; Assert.True(vm.CanRunImport);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var captured = vm.LedgerPreviewRows[0];
        vm.SignIndex = 2; Assert.False(vm.CanRunImport); Assert.Equal(f.Translator["Import_InterpretationChanged"], vm.ImportError);
        Assert.Equal(captured, vm.LedgerPreviewRows[0]); await vm.ImportCommand.ExecuteAsync(null);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        await vm.RefreshPreviewCommand.ExecuteAsync(null); Assert.Null(vm.ImportError);
        Assert.Contains(f.Translator["EntryKind_Income"], vm.LedgerPreviewRows[0].Heading); Assert.True(vm.CanRunImport);
    }

    [Fact]
    [Trait("AT", "AT-331")]
    public async Task Cancelled_picker_keeps_page_busy_navigation_is_refused_and_accepted_import_retires_rows()
    {
        using var f = new FlowFixture(); var files = new Files(); var (account, _) = await LedgerAsync(f, files);
        var entries = Enumerable.Range(1, 11).Select(i => new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense,
            Date = new(2026, 10, 9), Amount = i, Title = "Fictitious paged source" }).ToList();
        files.Picked = new("fictitious-paged.csv", CsvExport.Write(entries, new Dictionary<Guid, Account> { [account.Id] = account }, _ => "", true));
        var (_, _, _, vm) = Create(f, new(), files); await vm.LoadAsync(); await vm.PickCommand.ExecuteAsync(null);
        vm.NextLedgerPageCommand.Execute(null); var row = Assert.Single(vm.LedgerPreviewRows);
        var selected = new TaskCompletionSource<PickedDataFile?>(TaskCreationOptions.RunContinuationsAsynchronously); files.PendingPick = () => selected.Task;
        var pending = vm.PickCommand.ExecuteAsync(null); Assert.False(vm.PreviousLedgerPageCommand.CanExecute(null));
        vm.PreviousLedgerPageCommand.Execute(null); Assert.Equal(row, Assert.Single(vm.LedgerPreviewRows));
        selected.SetResult(null); await pending; Assert.Equal(row, Assert.Single(vm.LedgerPreviewRows));
        await vm.ImportCommand.ExecuteAsync(null); Assert.False(vm.HasLedgerPreview); Assert.Empty(vm.LedgerPreviewRows);
        Assert.False(vm.NextLedgerPageCommand.CanExecute(null)); Assert.False(vm.PreviousLedgerPageCommand.CanExecute(null));
    }
}
