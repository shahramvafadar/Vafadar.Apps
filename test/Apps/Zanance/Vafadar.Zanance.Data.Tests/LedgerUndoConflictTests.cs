using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>AT-131: short ledger Undo never reapplies obsolete refund relationships or changes currency labels.</summary>
[Trait("AT", "AT-131")]
public sealed class LedgerUndoConflictTests : IDisposable
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private readonly TemporaryDirectory _directory = new();
    private readonly List<ServiceProvider> _providers = [];
    private static readonly DateOnly Day = new(2026, 10, 10);

    private async Task<Fixture> FixtureAsync()
    {
        var services = new ServiceCollection().AddSingleton<TimeProvider>(new FixedTime()).AddZananceData(_directory.Combine(Guid.NewGuid()+".db")).BuildServiceProvider();
        _providers.Add(services); services.MigrateLocalDatabase<ZananceDbContext>(); var store = services.GetRequiredService<ZananceStore>();
        var account = new Account { Name = "Original EUR", CurrencyCode = "EUR", OpeningDate = Day };
        var other = new Account { Name = "Original USD", CurrencyCode = "USD", OpeningDate = Day };
        await store.SaveAccountAsync(account, Ct); await store.SaveAccountAsync(other, Ct);
        var original = new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, Date = Day, Amount = 1000,
            Title = "Original purchase", Note = "Original note", Tags = ["original"] };
        Assert.True((await store.SaveEntryAsync(original, Ct)).Succeeded);
        return new(services, store, account, other, original);
    }

    private sealed record Fixture(ServiceProvider Services, ZananceStore Store, Account Account, Account Other, LedgerEntry Original);

    [Theory]
    [InlineData("amount")]
    [InlineData("kind")]
    [InlineData("currency")]
    public async Task Changed_retained_refund_rejects_the_whole_old_Undo_without_touching_any_row(string change)
    {
        var f = await FixtureAsync(); var refund = EntryActions.CreateRefund(f.Original, 300, f.Account.Id, Day);
        Assert.True((await f.Store.SaveEntryAsync(refund, Ct)).Succeeded);
        var batch = await f.Store.DeleteEntryAsync(f.Original.Id, Ct); var current = (await f.Store.GetEntryAsync(refund.Id, Ct))!;
        if (change == "amount") current.Amount = 2000;
        else if (change == "kind") current.Kind = EntryKind.Income;
        else current.AccountId = f.Other.Id;
        Assert.True((await f.Store.SaveEntryAsync(current, Ct)).Succeeded);
        var before = await SnapshotAsync(f.Services); var events = 0; f.Store.Changed += (_, _) => events++;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.RestoreEntriesAsync(batch, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Services)); Assert.Equal(0, events); Assert.Null(await f.Store.GetEntryAsync(f.Original.Id, Ct));
    }

    [Fact]
    public async Task Changed_currency_after_deleting_the_accounts_only_entry_rejects_old_Undo_without_relabelling_money()
    {
        var f = await FixtureAsync(); var batch = await f.Store.DeleteEntryAsync(f.Original.Id, Ct);
        f.Account.CurrencyCode = "USD"; Assert.True(await f.Store.SaveAccountAsync(f.Account, Ct));
        var before = await SnapshotAsync(f.Services); var events = 0; f.Store.Changed += (_, _) => events++;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.RestoreEntriesAsync(batch, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Services)); Assert.Equal(0, events);
    }

    [Fact]
    public async Task Mutating_a_returned_deleted_row_or_its_tags_never_mutates_the_owned_Undo_snapshot()
    {
        var f = await FixtureAsync(); var before = await SnapshotAsync(f.Services); var batch = await f.Store.DeleteEntryAsync(f.Original.Id, Ct);
        batch[0].Amount = 2000; batch[0].Tags.Add("changed"); var enumeration = batch.Single(); enumeration.Title = "Changed caller copy";
        await f.Store.RestoreEntriesAsync(batch, Ct); Assert.Equal(before, await SnapshotAsync(f.Services));
    }
    [Theory]
    [InlineData("note")]
    [InlineData("tags")]
    [InlineData("review")]
    [InlineData("foreign")]
    public async Task Later_refund_metadata_edits_are_not_silently_attached_by_an_old_Undo(string change)
    {
        var f = await FixtureAsync(); var refund = EntryActions.CreateRefund(f.Original, 300, f.Account.Id, Day);
        await f.Store.SaveEntryAsync(refund, Ct); var batch = await f.Store.DeleteEntryAsync(f.Original.Id, Ct);
        var current = (await f.Store.GetEntryAsync(refund.Id, Ct))!;
        if (change == "note") current.Note = "Later explicit note";
        else if (change == "tags") current.Tags = ["later"];
        else if (change == "review") current.Review = ReviewState.Unreviewed;
        else { current.OriginalAmount = 280; current.OriginalCurrencyCode = "USD"; }
        Assert.True((await f.Store.SaveEntryAsync(current, Ct)).Succeeded);
        var before = await SnapshotAsync(f.Services);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.RestoreEntriesAsync(batch, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Services));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Deleted_or_recreated_refund_identity_requires_review_without_resurrecting_old_links(bool recreate)
    {
        var f = await FixtureAsync(); var refund = EntryActions.CreateRefund(f.Original, 300, f.Account.Id, Day);
        await f.Store.SaveEntryAsync(refund, Ct); var batch = await f.Store.DeleteEntryAsync(f.Original.Id, Ct);
        await f.Store.DeleteEntryAsync(refund.Id, Ct);
        if (recreate)
        {
            var replacement = refund.Copy(); replacement.RefundOfId = null; replacement.CreatedAt = refund.CreatedAt.AddTicks(1);
            Assert.True((await f.Store.SaveEntryAsync(replacement, Ct)).Succeeded);
        }
        var before = await SnapshotAsync(f.Services);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.RestoreEntriesAsync(batch, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Services));
    }

    [Fact]
    public async Task Explicit_relink_to_another_purchase_is_not_overwritten_or_combined_with_the_old_Undo()
    {
        var f = await FixtureAsync(); var refund = EntryActions.CreateRefund(f.Original, 300, f.Account.Id, Day);
        await f.Store.SaveEntryAsync(refund, Ct); var batch = await f.Store.DeleteEntryAsync(f.Original.Id, Ct);
        var purchase = new LedgerEntry { AccountId = f.Account.Id, Kind = EntryKind.Expense, Amount = 500, Date = Day };
        await f.Store.SaveEntryAsync(purchase, Ct); var current = (await f.Store.GetEntryAsync(refund.Id, Ct))!;
        current.RefundOfId = purchase.Id; Assert.True((await f.Store.SaveEntryAsync(current, Ct)).Succeeded);
        var before = await SnapshotAsync(f.Services); await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.RestoreEntriesAsync(batch, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Services)); Assert.Equal(purchase.Id, (await f.Store.GetEntryAsync(refund.Id, Ct))!.RefundOfId);
    }

    [Fact]
    public async Task Changed_transfer_destination_currency_cannot_relabel_the_old_received_amount()
    {
        var f = await FixtureAsync(); var transfer = new LedgerEntry { Kind = EntryKind.Transfer, AccountId = f.Account.Id,
            ToAccountId = f.Other.Id, Amount = 1000, ToAmount = 900, Date = Day };
        Assert.True((await f.Store.SaveEntryAsync(transfer, Ct)).Succeeded); var batch = await f.Store.DeleteEntryAsync(transfer.Id, Ct);
        f.Other.CurrencyCode = "EUR"; Assert.True(await f.Store.SaveAccountAsync(f.Other, Ct));
        var before = await SnapshotAsync(f.Services); await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.RestoreEntriesAsync(batch, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Services));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Partial_group_recovery_accepts_unchanged_siblings_and_rejects_changed_siblings(bool edit)
    {
        var f = await FixtureAsync(); var (parts, removed) = EntryActions.Split([f.Original.Copy()], [(null, 600), (null, 400)]);
        Assert.True((await f.Store.SaveEntriesAsync(parts, removed, Ct)).Succeeded); var original = await SnapshotAsync(f.Services);
        var batch = await f.Store.DeleteEntryAsync(parts[0].Id, Ct); await f.Store.RestoreEntriesAsync([batch[0]], Ct);
        if (edit)
        {
            var existing = (await f.Store.GetEntryAsync(batch[0].Id, Ct))!; existing.Amount++;
            Assert.True((await f.Store.SaveEntryAsync(existing, Ct)).Succeeded); var before = await SnapshotAsync(f.Services);
            await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.RestoreEntriesAsync(batch, Ct)); Assert.Equal(before, await SnapshotAsync(f.Services));
        }
        else { await f.Store.RestoreEntriesAsync(batch, Ct); Assert.Equal(original, await SnapshotAsync(f.Services)); }
    }

    [Fact]
    public async Task A_completed_Undo_is_a_no_op_after_later_refund_edits()
    {
        var f = await FixtureAsync(); var refund = EntryActions.CreateRefund(f.Original, 300, f.Account.Id, Day);
        await f.Store.SaveEntryAsync(refund, Ct); var batch = await f.Store.DeleteEntryAsync(f.Original.Id, Ct);
        await f.Store.RestoreEntriesAsync(batch, Ct); var current = (await f.Store.GetEntryAsync(refund.Id, Ct))!; current.Note = "Later retained note";
        Assert.True((await f.Store.SaveEntryAsync(current, Ct)).Succeeded); var before = await SnapshotAsync(f.Services); var events = 0;
        f.Store.Changed += (_, _) => events++; await f.Store.RestoreEntriesAsync(batch, Ct);
        Assert.Equal(before, await SnapshotAsync(f.Services)); Assert.Equal(0, events);
    }

    [Fact]
    public async Task Account_rename_and_archival_do_not_block_owned_Undo_in_the_original_currency()
    {
        var f = await FixtureAsync(); var batch = await f.Store.DeleteEntryAsync(f.Original.Id, Ct);
        f.Account.Name = "Later account name"; f.Account.IsArchived = true; Assert.True(await f.Store.SaveAccountAsync(f.Account, Ct));
        await f.Store.RestoreEntriesAsync(batch, Ct); Assert.Equal(f.Original.Amount, (await f.Store.GetEntryAsync(f.Original.Id, Ct))!.Amount);
        Assert.True((await f.Store.GetAccountsAsync(includeArchived: true, cancellationToken: Ct)).Single(a => a.Id == f.Account.Id).IsArchived);
    }

    private static async Task<string> SnapshotAsync(ServiceProvider provider, bool omitEntries = false)
    {
        await using var db = await provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.OpenConnectionAsync(Ct); var connection = db.Database.GetDbConnection(); var tables = new List<string>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            using var reader = await command.ExecuteReaderAsync(Ct); while (await reader.ReadAsync(Ct)) tables.Add(reader.GetString(0));
        }
        Assert.Equal(25, tables.Count); var all = new Dictionary<string, List<string>>();
        foreach (var table in tables)
        {
            if (omitEntries && table == "Entries") continue;
            using var command = connection.CreateCommand(); command.CommandText = "SELECT * FROM \"" + table + "\"";
            using var reader = await command.ExecuteReaderAsync(Ct); var rows = new List<string>();
            while (await reader.ReadAsync(Ct))
            {
                var values = new object?[reader.FieldCount];
                for (var i = 0; i < values.Length; i++) values[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                rows.Add(JsonSerializer.Serialize(values));
            }
            rows.Sort(StringComparer.Ordinal); all[table] = rows;
        }
        return JsonSerializer.Serialize(all);
    }

    /// <summary>Stable audit time for complete stored-column comparisons.</summary>
    private sealed class FixedTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    public void Dispose()
    {
        foreach (var provider in _providers) provider.Dispose(); SqliteTestPools.Clear(_directory); _directory.Dispose();
    }
}
