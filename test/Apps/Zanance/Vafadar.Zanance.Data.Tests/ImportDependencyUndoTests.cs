using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;
using Vafadar.Core.Hosting;
using Vafadar.Core.Settings;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Holdings;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>Retains original financial group dependencies when a durable imported batch is undone.</summary>
public sealed class ImportDependencyUndoTests : IDisposable
{
    private static readonly DateOnly Day = new(2026, 10, 23);
    private readonly TemporaryDirectory _directory = new();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <inheritdoc />
    public void Dispose()
    {
        SqliteTestPools.Clear(_directory);
        _directory.Dispose();
    }

    [Fact, Trait("AT", "AT-79")]
    public async Task A_later_fee_in_the_original_imported_transfer_group_blocks_cold_undo()
    {
        await using var services = Install();
        var store = services.GetRequiredService<ZananceStore>();
        var (source, destination) = await AccountsAsync(store);
        var transfer = Transfer(source, destination);
        var imported = await store.ImportAsync([transfer], Ct);
        Assert.Equal(1, imported.Imported);
        var retained = (await store.GetEntryAsync(transfer.Id, Ct))!;
        var fee = new LedgerEntry { Kind = EntryKind.Expense, AccountId = source.Id, Amount = 100,
            Date = Day, Title = "Later original transfer fee", GroupId = retained.GroupId };
        Assert.Empty((await store.SaveEntriesAsync([retained, fee], [], Ct)).Errors);
        var before = await RowsAsync(store);
        await using var reopened = Install();
        var original = reopened.GetRequiredService<ZananceStore>();
        var undone = await original.TryUndoImportAsync(imported.BatchId!.Value, Ct);
        Assert.True(undone.Conflict);
        Assert.Equal(0, undone.Removed);
        Assert.Equal(before, await RowsAsync(original));
        Assert.Single(await original.GetImportBatchesAsync(Ct));
    }

    [Fact, Trait("AT", "AT-79")]
    public async Task An_imported_fee_can_be_undone_without_removing_its_unchanged_preexisting_transfer()
    {
        await using var services = Install();
        var store = services.GetRequiredService<ZananceStore>();
        var (source, destination) = await AccountsAsync(store);
        var transfer = Transfer(source, destination);
        Assert.Empty((await store.SaveEntryAsync(transfer, Ct)).Errors);
        var before = await RowsAsync(store);
        var fee = new LedgerEntry { Kind = EntryKind.Expense, AccountId = source.Id, Amount = 100,
            Date = Day, Title = "Imported existing transfer fee", GroupId = transfer.GroupId };
        var imported = await store.ImportAsync([fee], Ct);
        Assert.Equal(1, imported.Imported);
        await using var reopened = Install();
        var original = reopened.GetRequiredService<ZananceStore>();
        var undone = await original.TryUndoImportAsync(imported.BatchId!.Value, Ct);
        Assert.False(undone.Conflict);
        Assert.Equal(1, undone.Removed);
        Assert.Equal(before, await RowsAsync(original));
        Assert.Empty(await original.GetImportBatchesAsync(Ct));
    }

    [Fact, Trait("AT", "AT-79")]
    public async Task A_complete_imported_transfer_and_fee_group_undoes_together()
    {
        await using var services = Install(); var store = services.GetRequiredService<ZananceStore>();
        var (source, destination) = await AccountsAsync(store); var transfer = Transfer(source, destination);
        var imported = await store.ImportAsync([transfer, Fee(source, transfer.GroupId)], Ct);
        var result = await store.TryUndoImportAsync(imported.BatchId!.Value, Ct);
        Assert.False(result.Conflict); Assert.Equal(2, result.Removed);
        Assert.Empty(await store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact, Trait("AT", "AT-79")]
    public async Task Grouped_imports_require_reverse_dependency_order_without_consuming_the_refused_journal()
    {
        await using var services = Install(); var store = services.GetRequiredService<ZananceStore>();
        var (source, destination) = await AccountsAsync(store); var transfer = Transfer(source, destination);
        var first = await store.ImportAsync([transfer], Ct);
        var second = await store.ImportAsync([Fee(source, transfer.GroupId)], Ct);
        var before = await RowsAsync(store);
        Assert.True((await store.TryUndoImportAsync(first.BatchId!.Value, Ct)).Conflict);
        Assert.Equal(before, await RowsAsync(store)); Assert.Equal(2, (await store.GetImportBatchesAsync(Ct)).Count);
        Assert.False((await store.TryUndoImportAsync(second.BatchId!.Value, Ct)).Conflict);
        Assert.False((await store.TryUndoImportAsync(first.BatchId!.Value, Ct)).Conflict);
        Assert.Empty(await store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact, Trait("AT", "AT-79")]
    public async Task A_later_edit_of_a_preexisting_transfer_requires_review_before_imported_fee_undo()
    {
        await using var services = Install(); var store = services.GetRequiredService<ZananceStore>();
        var (source, destination) = await AccountsAsync(store); var transfer = Transfer(source, destination);
        await store.SaveEntryAsync(transfer, Ct); var fee = Fee(source, transfer.GroupId);
        var imported = await store.ImportAsync([fee], Ct);
        var parent = (await store.GetEntryAsync(transfer.Id, Ct))!; parent.Note = "Later original group note";
        var retained = (await store.GetEntryAsync(fee.Id, Ct))!;
        Assert.Empty((await store.SaveEntriesAsync([parent, retained], [], Ct)).Errors);
        var before = await RowsAsync(store);
        Assert.True((await store.TryUndoImportAsync(imported.BatchId!.Value, Ct)).Conflict);
        Assert.Equal(before, await RowsAsync(store));
    }

    [Theory, InlineData(false), InlineData(true), Trait("AT", "AT-79")]
    public async Task Legacy_group_snapshots_require_review_only_when_external_members_cannot_be_proven(bool external)
    {
        await using var services = Install(); var store = services.GetRequiredService<ZananceStore>();
        var (source, destination) = await AccountsAsync(store); var transfer = Transfer(source, destination);
        ImportResult imported;
        if (external) { await store.SaveEntryAsync(transfer, Ct); imported = await store.ImportAsync([Fee(source, transfer.GroupId)], Ct); }
        else imported = await store.ImportAsync([transfer, Fee(source, transfer.GroupId)], Ct);
        await using (var db = await services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        {
            var journal = await db.ImportLinks.SingleAsync(Ct); var state = JsonNode.Parse(journal.StateJson)!.AsObject();
            state["Version"] = 1; state.Remove("GroupDependencyDigest"); journal.StateJson = state.ToJsonString(); await db.SaveChangesAsync(Ct);
        }
        var before = await RowsAsync(store); var result = await store.TryUndoImportAsync(imported.BatchId!.Value, Ct);
        Assert.Equal(external, result.Conflict);
        if (external) Assert.Equal(before, await RowsAsync(store));
        else Assert.Empty(await store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact, Trait("AT", "AT-79")]
    public async Task Group_dependency_proof_survives_portable_restore_and_detects_a_later_fee()
    {
        await using var services = Install(); var store = services.GetRequiredService<ZananceStore>();
        var (source, destination) = await AccountsAsync(store); var transfer = Transfer(source, destination);
        var imported = await store.ImportAsync([transfer], Ct);
        var packet = await services.GetRequiredService<IBackupService>().CreatePackageAsync(null, Ct);
        await using var restored = Install("restored"); await restored.GetRequiredService<IBackupService>().RestorePackageAsync(packet, null, Ct);
        var target = restored.GetRequiredService<ZananceStore>(); var parent = (await target.GetEntryAsync(transfer.Id, Ct))!;
        Assert.Empty((await target.SaveEntriesAsync([parent, Fee(source, parent.GroupId)], [], Ct)).Errors);
        var before = await RowsAsync(target);
        Assert.True((await target.TryUndoImportAsync(imported.BatchId!.Value, Ct)).Conflict);
        Assert.Equal(before, await RowsAsync(target));
    }

    [Theory, InlineData(false), InlineData(true), Trait("AT", "AT-79")]
    public async Task Existing_holding_group_context_is_preserved_and_later_holding_edits_require_review(bool edit)
    {
        await using var services = Install(); var store = services.GetRequiredService<ZananceStore>();
        var holdings = services.GetRequiredService<HoldingStore>(); var (source, _) = await AccountsAsync(store);
        var type = new AssetType { Name = "Fictitious counted holding", Kind = AssetKind.CountableItem,
            Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR" };
        Assert.True(await holdings.SaveTypeAsync(type, Ct)); var location = await holdings.EnsureDefaultLocationAsync("Original location", Ct);
        var item = new AssetEvent { AssetTypeId = type.Id, LocationId = location.Id, Date = Day,
            Kind = AssetEventKind.Purchase, Quantity = 1000, BasisAmount = 2500, Note = "Original holding note" };
        var cash = new LedgerEntry { Kind = EntryKind.AssetPurchase, AccountId = source.Id, Amount = 2500, Date = Day };
        Assert.True((await holdings.SaveEventAsync(item, [cash], Ct)).Succeeded);
        var fee = Fee(source, item.GroupId); var imported = await store.ImportAsync([fee], Ct);
        if (edit)
        {
            item = Assert.Single(await holdings.GetEventsAsync(cancellationToken: Ct)); item.Note = "Later holding note";
            Assert.True((await holdings.SaveEventAsync(item, await store.GetGroupAsync(item.GroupId!.Value, Ct), Ct)).Succeeded);
        }
        var before = await RowsAsync(store); var beforeEvents = JsonSerializer.Serialize(await holdings.GetEventsAsync(cancellationToken: Ct));
        var result = await store.TryUndoImportAsync(imported.BatchId!.Value, Ct); Assert.Equal(edit, result.Conflict);
        Assert.Equal(beforeEvents, JsonSerializer.Serialize(await holdings.GetEventsAsync(cancellationToken: Ct)));
        if (edit) Assert.Equal(before, await RowsAsync(store));
        else { Assert.Equal(1, result.Removed); Assert.Equal(cash.Id, Assert.Single(await store.GetEntriesAsync(cancellationToken: Ct)).Id); }
    }

    private static LedgerEntry Fee(Account account, Guid? group) => new()
    { Kind = EntryKind.Expense, AccountId = account.Id, Amount = 100, Date = Day, GroupId = group, Title = "Fictitious grouped fee" };

    private static LedgerEntry Transfer(Account source, Account destination) => new()
    { Kind = EntryKind.Transfer, AccountId = source.Id, ToAccountId = destination.Id, Amount = 2500,
        ToAmount = 2500, Date = Day, Title = "Original imported transfer", GroupId = Guid.NewGuid() };

    private static async Task<(Account Source, Account Destination)> AccountsAsync(ZananceStore store)
    {
        var source = new Account { Name = "Original source", CurrencyCode = "EUR", OpeningDate = Day.AddDays(-1), OpeningBalance = 10000 };
        var destination = new Account { Name = "Original destination", CurrencyCode = "EUR", OpeningDate = Day.AddDays(-1) };
        await store.SaveAccountAsync(source, Ct); await store.SaveAccountAsync(destination, Ct);
        return (source, destination);
    }

    private static async Task<string> RowsAsync(ZananceStore store) =>
        JsonSerializer.Serialize((await store.GetEntriesAsync(cancellationToken: Ct)).OrderBy(entry => entry.Id));

    private ServiceProvider Install(string name = "original")
    {
        var services = new ServiceCollection().AddZananceData(_directory.Combine(name + ".db"))
            .AddVafadarBackup().AddSingleton<IAppEnvironment>(new StaticAppEnvironment("pro.vafadar.zanance", "Zanance", new Version(1, 0), "Import groups", "Tests"))
            .AddSingleton<ISettingsStore, InMemorySettingsStore>().BuildServiceProvider();
        services.MigrateLocalDatabase<ZananceDbContext>();
        return services;
    }
}
