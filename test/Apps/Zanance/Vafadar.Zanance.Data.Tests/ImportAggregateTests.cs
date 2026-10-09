using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;
using Vafadar.Core.Hosting;
using Vafadar.Core.Settings;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.DataFiles;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>Real SQLite transactions, durable identity, partial overlaps and Undo for linked imports (D-72).</summary>
public sealed class ImportAggregateTests : IDisposable
{
    private static readonly DateOnly Day = new(2026, 9, 1);
    private readonly TemporaryDirectory _directory = new();
    private readonly ServiceProvider _services;
    private readonly ZananceStore _store;
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public ImportAggregateTests()
    {
        _services = Install("original");
        _store = _services.GetRequiredService<ZananceStore>();
    }

    public void Dispose()
    {
        _services.Dispose();
        SqliteConnection.ClearAllPools();
        _directory.Dispose();
    }

    [Fact, Trait("AT", "AT-79")]
    public async Task Six_imported_details_keep_the_september_total_at_412_and_undo_restores_the_aggregate()
    {
        var aggregate = await AggregateAsync();
        var details = new long[] { 6000, 7000, 6500, 8000, 5500, 6500 }.Select((amount, i) => Detail(aggregate, amount, Day.AddDays(i * 4))).ToList();
        var result = await ImportAsync(details);
        Assert.Null(result.Conflict);
        Assert.Equal(6, result.Imported);
        Assert.Equal(1700, (await _store.GetEntryAsync(aggregate.Id, Ct))!.Amount);
        Assert.Equal(41200, (await _store.GetEntriesAsync(cancellationToken: Ct)).Sum(e => e.Amount));
        Assert.Equal(6, await _store.UndoImportAsync(result.BatchId!.Value, Ct));
        var restored = Assert.Single(await _store.GetEntriesAsync(cancellationToken: Ct));
        Assert.Equal((aggregate.Id, 41200), (restored.Id, restored.Amount));
        Assert.Empty(await _store.GetImportBatchesAsync(Ct));
    }

    [Fact, Trait("AT", "AT-79")]
    public async Task Keep_both_is_an_explicit_choice_and_undo_does_not_change_the_original()
    {
        var aggregate = await AggregateAsync();
        var result = await ImportAsync([Detail(aggregate, 10000)], link: false);
        Assert.Equal(51200, (await _store.GetEntriesAsync(cancellationToken: Ct)).Sum(e => e.Amount));
        Assert.Equal(1, await _store.UndoImportAsync(result.BatchId!.Value, Ct));
        Assert.Equal(41200, Assert.Single(await _store.GetEntriesAsync(cancellationToken: Ct)).Amount);
    }

    [Fact, Trait("AT", "AT-79")]
    public async Task An_overlap_without_a_choice_saves_nothing()
    {
        var aggregate = await AggregateAsync();
        var result = await _store.ImportAsync([Detail(aggregate, 10000)], Ct);
        Assert.Equal(ImportOverlapConflict.MissingChoice, result.Conflict);
        Assert.Single(await _store.GetEntriesAsync(cancellationToken: Ct));
        Assert.Empty(await _store.GetImportBatchesAsync(Ct));
    }

    [Fact, Trait("AT", "AT-79")]
    public async Task A_second_partial_import_only_subtracts_its_new_details_and_undo_requires_reverse_order()
    {
        var aggregate = await AggregateAsync();
        var first = await ImportAsync([Detail(aggregate, 10000)]);
        var second = await ImportAsync([Detail(aggregate, 5000)]);
        Assert.Equal(26200, (await _store.GetEntryAsync(aggregate.Id, Ct))!.Amount);
        Assert.True((await _store.TryUndoImportAsync(first.BatchId!.Value, Ct)).Conflict);
        Assert.Equal(26200, (await _store.GetEntryAsync(aggregate.Id, Ct))!.Amount);
        Assert.False((await _store.TryUndoImportAsync(second.BatchId!.Value, Ct)).Conflict);
        Assert.Equal(31200, (await _store.GetEntryAsync(aggregate.Id, Ct))!.Amount);
        Assert.False((await _store.TryUndoImportAsync(first.BatchId!.Value, Ct)).Conflict);
        Assert.Equal(41200, Assert.Single(await _store.GetEntriesAsync(cancellationToken: Ct)).Amount);
    }

    [Theory, InlineData(41200), InlineData(50000), Trait("AT", "AT-79")]
    public async Task Full_coverage_removes_the_aggregate_and_restart_undo_keeps_metadata_and_attachments(long amount)
    {
        var aggregate = await AggregateAsync();
        aggregate.Title = "September groceries";
        aggregate.Note = "Original aggregate note";
        aggregate.Payee = "Fictitious shops";
        aggregate.Tags = ["groceries", "period"];
        await _store.SaveEntryAsync(aggregate, Ct);
        var attachment = new EntryAttachment { EntryId = aggregate.Id, FileName = "fixture.txt", ContentType = "text/plain", Data = [1, 2, 3] };
        await _store.AddAttachmentAsync(attachment, Ct);
        var original = (await _store.GetEntryAsync(aggregate.Id, Ct))!;
        var result = await ImportAsync([Detail(aggregate, amount)]);
        Assert.Null(await _store.GetEntryAsync(aggregate.Id, Ct));
        Assert.Contains(aggregate.Id, await _store.GetConsumedImportIdsAsync(Ct));
        Assert.Equal(0, await _store.PurgeOrphanAttachmentsAsync(Ct));

        await using var reopened = Install("original");
        var restarted = reopened.GetRequiredService<ZananceStore>();
        Assert.False((await restarted.TryUndoImportAsync(result.BatchId!.Value, Ct)).Conflict);
        var restored = Assert.Single(await restarted.GetEntriesAsync(cancellationToken: Ct));
        Assert.Equal(original.Id, restored.Id);
        Assert.Equal(original.CreatedAt, restored.CreatedAt);
        Assert.Equal((41200, original.Title, original.Note, original.Payee), (restored.Amount, restored.Title, restored.Note, restored.Payee));
        Assert.Equal(original.Tags, restored.Tags);
        Assert.Equal(attachment.Id, Assert.Single(await restarted.GetAttachmentsAsync(restored.Id, Ct)).Id);
        Assert.Empty(await restarted.GetConsumedImportIdsAsync(Ct));
    }

    [Fact, Trait("AT", "AT-79")]
    public async Task An_imported_aggregate_can_link_existing_details_and_undo_leaves_those_details()
    {
        var aggregate = await AggregateAsync(save: false);
        var detail = Detail(aggregate, 10000);
        await _store.SaveEntryAsync(detail, Ct);
        var result = await ImportAsync([aggregate]);
        Assert.Equal(1, result.Imported);
        Assert.Equal(31200, (await _store.GetEntryAsync(aggregate.Id, Ct))!.Amount);
        Assert.Equal(41200, (await _store.GetEntriesAsync(cancellationToken: Ct)).Sum(e => e.Amount));
        Assert.False((await _store.TryUndoImportAsync(result.BatchId!.Value, Ct)).Conflict);
        Assert.Equal(detail.Id, Assert.Single(await _store.GetEntriesAsync(cancellationToken: Ct)).Id);
    }

    [Fact, Trait("AT", "AT-79")]
    public async Task Two_fully_consumed_imported_aggregates_keep_distinct_identities_history_and_idempotency()
    {
        var first = await AggregateAsync(save: false);
        var second = new LedgerEntry { AccountId = first.AccountId, Amount = 41200, Kind = EntryKind.Expense,
            Date = Day.AddMonths(1), IsAggregated = true, AggregatedFrom = Day.AddMonths(1), AggregatedTo = Day.AddMonths(1).AddDays(30) };
        await _store.SaveEntryAsync(Detail(first, 41200), Ct);
        await _store.SaveEntryAsync(Detail(second, 41200, Day.AddMonths(1)), Ct);
        var result = await ImportAsync([first, second]);
        Assert.Equal(2, result.Imported);
        Assert.Equal(2, (await _store.GetConsumedImportIdsAsync(Ct)).Count);
        Assert.Equal(2, Assert.Single(await _store.GetImportBatchesAsync(Ct)).Count);
        var repeated = await _store.ImportAsync([first, second], Ct);
        Assert.Equal((0, 2), (repeated.Imported, repeated.Skipped));
        Assert.Null(repeated.BatchId);
        Assert.Equal(2, (await _store.TryUndoImportAsync(result.BatchId!.Value, Ct)).Removed);
        Assert.Empty(await _store.GetImportBatchesAsync(Ct));
        Assert.Equal(2, (await _store.GetEntriesAsync(cancellationToken: Ct)).Count);
    }

    [Fact, Trait("AT", "AT-79")]
    public async Task A_consumed_aggregate_from_an_older_import_keeps_its_batch_until_the_later_import_is_undone()
    {
        var aggregate = await AggregateAsync(save: false);
        var first = await _store.ImportAsync([aggregate], Ct);
        var second = await ImportAsync([Detail(aggregate, 41200)]);
        Assert.Equal(2, (await _store.GetImportBatchesAsync(Ct)).Count);
        Assert.True((await _store.TryUndoImportAsync(first.BatchId!.Value, Ct)).Conflict);
        Assert.False((await _store.TryUndoImportAsync(second.BatchId!.Value, Ct)).Conflict);
        Assert.Equal(first.BatchId, (await _store.GetEntryAsync(aggregate.Id, Ct))!.ImportBatchId);
        Assert.False((await _store.TryUndoImportAsync(first.BatchId!.Value, Ct)).Conflict);
        Assert.Empty(await _store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Theory, InlineData("aggregate"), InlineData("detail"), Trait("AT", "AT-79")]
    public async Task Undo_never_overwrites_a_later_edit(string target)
    {
        var aggregate = await AggregateAsync();
        var detail = Detail(aggregate, 10000);
        var result = await ImportAsync([detail]);
        var edited = (await _store.GetEntryAsync(target == "aggregate" ? aggregate.Id : detail.Id, Ct))!;
        edited.Note = "Later user edit";
        await _store.SaveEntryAsync(edited, Ct);
        Assert.True((await _store.TryUndoImportAsync(result.BatchId!.Value, Ct)).Conflict);
        Assert.Equal("Later user edit", (await _store.GetEntryAsync(edited.Id, Ct))!.Note);
        Assert.Equal(2, (await _store.GetEntriesAsync(cancellationToken: Ct)).Count);
    }

    [Fact, Trait("AT", "AT-79")]
    public async Task A_stale_preview_is_rejected_before_any_row_is_saved()
    {
        var aggregate = await AggregateAsync();
        var detail = Detail(aggregate, 10000);
        var choices = await ChoicesAsync([detail]);
        aggregate.Amount = 40000;
        await _store.SaveEntryAsync(aggregate, Ct);
        var result = await _store.ImportAsync([detail], choices, Ct);
        Assert.Equal(ImportOverlapConflict.ChangedPreview, result.Conflict);
        Assert.Equal(40000, Assert.Single(await _store.GetEntriesAsync(cancellationToken: Ct)).Amount);
    }

    [Fact, Trait("AT", "AT-79")]
    public async Task One_detail_cannot_reduce_two_aggregates_but_one_link_and_one_keep_both_is_explicit()
    {
        var aggregate = await AggregateAsync();
        var other = await AggregateAsync();
        other.AccountId = aggregate.AccountId;
        await _store.SaveEntryAsync(other, Ct);
        var detail = Detail(aggregate, 10000);
        var choices = await ChoicesAsync([detail]);
        var rejected = await _store.ImportAsync([detail], choices, Ct);
        Assert.Equal(ImportOverlapConflict.SharedDetail, rejected.Conflict);
        Assert.Equal(2, (await _store.GetEntriesAsync(cancellationToken: Ct)).Count);
        var accepted = await _store.ImportAsync([detail], [choices[0], choices[1] with { Link = false }], Ct);
        Assert.Null(accepted.Conflict);
        Assert.Equal(82400, (await _store.GetEntriesAsync(cancellationToken: Ct)).Sum(e => e.Amount));
    }

    [Fact, Trait("AT", "AT-79")]
    public async Task Invalid_rows_roll_back_both_import_and_aggregate_reduction()
    {
        var aggregate = await AggregateAsync();
        var detail = Detail(aggregate, 10000);
        var invalid = Detail(aggregate, 0);
        var result = await ImportAsync([detail, invalid]);
        Assert.Single(result.Errors);
        Assert.Equal(41200, Assert.Single(await _store.GetEntriesAsync(cancellationToken: Ct)).Amount);
        Assert.Empty(await _store.GetImportBatchesAsync(Ct));
    }

    [Fact, Trait("AT", "AT-79")]
    public async Task A_refund_relationship_is_not_unlinked_by_aggregate_replacement_or_import_undo()
    {
        var aggregate = await AggregateAsync();
        var refund = Detail(aggregate, 500);
        refund.Kind = EntryKind.Refund;
        refund.RefundOfId = aggregate.Id;
        await _store.SaveEntryAsync(refund, Ct);
        var rejected = await ImportAsync([Detail(aggregate, 10000)]);
        Assert.Equal(ImportOverlapConflict.LinkedAggregate, rejected.Conflict);
        Assert.Equal(aggregate.Id, (await _store.GetEntryAsync(refund.Id, Ct))!.RefundOfId);

        var detail = Detail(aggregate, 10000);
        var imported = await ImportAsync([detail], link: false);
        refund.RefundOfId = detail.Id;
        await _store.SaveEntryAsync(refund, Ct);
        Assert.True((await _store.TryUndoImportAsync(imported.BatchId!.Value, Ct)).Conflict);
        Assert.Equal(detail.Id, (await _store.GetEntryAsync(refund.Id, Ct))!.RefundOfId);
    }

    [Fact, Trait("AT", "AT-79")]
    public async Task Backup_restore_preserves_the_journal_and_delete_all_clears_it()
    {
        var aggregate = await AggregateAsync();
        await _store.AddAttachmentAsync(new EntryAttachment { EntryId = aggregate.Id, FileName = "fixture.txt", ContentType = "text/plain", Data = [4, 5, 6] }, Ct);
        var imported = await ImportAsync([Detail(aggregate, 41200)]);
        var package = await _services.GetRequiredService<IBackupService>().CreatePackageAsync(null, Ct);
        await using var fresh = Install("restored");
        await fresh.GetRequiredService<IBackupService>().RestorePackageAsync(package, null, Ct);
        var restored = fresh.GetRequiredService<ZananceStore>();
        Assert.Contains(aggregate.Id, await restored.GetConsumedImportIdsAsync(Ct));
        Assert.False((await restored.TryUndoImportAsync(imported.BatchId!.Value, Ct)).Conflict);
        Assert.Equal(aggregate.Id, Assert.Single(await restored.GetEntriesAsync(cancellationToken: Ct)).Id);
        var restoredAttachment = Assert.Single(await restored.GetAttachmentsAsync(aggregate.Id, Ct));
        Assert.Equal(new byte[] { 4, 5, 6 }, (await restored.GetAttachmentAsync(restoredAttachment.Id, Ct))!.Data);
        await _store.DeleteAllDataAsync(Ct);
        Assert.Empty(await _store.GetConsumedImportIdsAsync(Ct));
        Assert.Empty(await _store.GetImportBatchesAsync(Ct));
    }

    [Fact, Trait("AT", "AT-79")]
    public async Task Partial_file_overlap_only_links_matching_details_and_preserves_balance_and_expense_rules()
    {
        var aggregate = await AggregateAsync();
        var inside = Detail(aggregate, 10000, Day);
        var outside = Detail(aggregate, 2000, Day.AddMonths(1));
        var income = Detail(aggregate, 3000); income.Kind = EntryKind.Income;
        var result = await ImportAsync([inside, outside, income]);
        Assert.Null(result.Conflict);
        Assert.Equal(31200, (await _store.GetEntryAsync(aggregate.Id, Ct))!.Amount);
        var rows = await _store.GetEntriesAsync(cancellationToken: Ct);
        var account = Assert.Single(await _store.GetAccountsAsync(cancellationToken: Ct));
        Assert.Equal(-40200, LedgerCalculator.Balance(account, rows, Day.AddMonths(1)));
        Assert.Equal(43200, rows.Where(e => e.Kind == EntryKind.Expense).Sum(e => e.Amount));
        Assert.False((await _store.TryUndoImportAsync(result.BatchId!.Value, Ct)).Conflict);
        Assert.Equal(aggregate.Id, Assert.Single(await _store.GetEntriesAsync(cancellationToken: Ct)).Id);
    }

    [Fact, Trait("AT", "AT-79")]
    public async Task Own_csv_preview_skips_the_identity_of_a_consumed_aggregate()
    {
        var aggregate = await AggregateAsync();
        var accounts = (await _store.GetAccountsAsync(cancellationToken: Ct)).ToDictionary(a => a.Id);
        var csv = Csv.Read(CsvExport.Write([aggregate], accounts, _ => string.Empty, includeNotes: true), ',');
        await ImportAsync([Detail(aggregate, 41200)]);
        var preview = CsvImport.PreviewOwn(csv, accounts.Values.ToList(), [], _ => string.Empty,
            await _store.GetEntriesAsync(cancellationToken: Ct), await _store.GetConsumedImportIdsAsync(Ct));
        Assert.True(Assert.Single(preview).AlreadyImported);
        Assert.Equal(aggregate.Id, Assert.Single(preview).Entry!.Id);
    }

    [Fact, Trait("AT", "AT-79")]
    public async Task Existing_details_already_used_in_a_linked_import_cannot_reduce_another_imported_aggregate()
    {
        var aggregate = await AggregateAsync();
        var detail = Detail(aggregate, 10000);
        await ImportAsync([detail]);
        var second = new LedgerEntry { AccountId = aggregate.AccountId, Kind = EntryKind.Expense, Amount = 10000,
            IsAggregated = true, Date = Day, AggregatedFrom = Day, AggregatedTo = Day.AddDays(29) };
        var result = await ImportAsync([second]);
        Assert.Equal(ImportOverlapConflict.SharedDetail, result.Conflict);
        Assert.Null(await _store.GetEntryAsync(second.Id, Ct));
        Assert.Equal(41200, (await _store.GetEntriesAsync(cancellationToken: Ct)).Sum(e => e.Amount));
    }

    private async Task<ImportResult> ImportAsync(IReadOnlyList<LedgerEntry> entries, bool link = true) =>
        await _store.ImportAsync(entries, await ChoicesAsync(entries, link), Ct);

    private async Task<List<ImportAggregateChoice>> ChoicesAsync(IReadOnlyList<LedgerEntry> entries, bool link = true) =>
        AggregatedEntries.FindForImport(entries, await _store.GetEntriesAsync(cancellationToken: Ct)).Select(o => new ImportAggregateChoice(o, link)).ToList();

    private async Task<LedgerEntry> AggregateAsync(bool save = true)
    {
        var account = new Account { Name = "Fictitious EUR account", CurrencyCode = "EUR", OpeningDate = Day.AddMonths(-1) };
        await _store.SaveAccountAsync(account, Ct);
        var entry = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 41200, Date = Day.AddDays(29),
            IsAggregated = true, AggregatedFrom = Day, AggregatedTo = Day.AddDays(29) };
        if (save) { await _store.SaveEntryAsync(entry, Ct); }
        return entry;
    }

    private static LedgerEntry Detail(LedgerEntry aggregate, long amount, DateOnly? date = null) => new()
    {
        AccountId = aggregate.AccountId, CategoryId = aggregate.CategoryId, Kind = aggregate.Kind, Amount = amount, Date = date ?? Day.AddDays(3),
    };

    private ServiceProvider Install(string name)
    {
        Directory.CreateDirectory(_directory.Combine(name));
        var services = new ServiceCollection().AddZananceData(_directory.Combine(Path.Combine(name, "zanance.db")))
            .AddVafadarBackup().AddSingleton<IAppEnvironment>(new StaticAppEnvironment("pro.vafadar.zanance", "Zanance", new Version(1, 0), name, "Tests"))
            .AddSingleton<ISettingsStore, InMemorySettingsStore>().BuildServiceProvider();
        services.MigrateLocalDatabase<ZananceDbContext>();
        return services;
    }
}
