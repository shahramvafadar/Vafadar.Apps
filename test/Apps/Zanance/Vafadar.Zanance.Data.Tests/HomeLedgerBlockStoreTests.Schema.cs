using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Vafadar.Zanance.Data.Tests;

public sealed partial class HomeLedgerBlockStoreTests
{
    /// <summary>Unsupported optional columns cannot replace original rows or cause a getter to repair the cache.</summary>
    [Theory]
    [InlineData("HomeLedgerReadState", "CodecVersion")]
    [InlineData("HomeLedgerReadState", "PublishedRevision")]
    [InlineData("HomeLedgerReadState", "SchemaVersion")]
    [InlineData("HomeLedgerReadBlocks", "Payload")]
    [InlineData("HomeLedgerReadBlocks", "Digest")]
    [InlineData("HomeLedgerReadBlocks", "RowCount")]
    [InlineData("HomeLedgerDirtyBlocks", "BlockId")]
    public async Task Unsupported_derived_columns_decline_reads_without_repair(string table, string column)
    {
        await using var db = await SeedAsync();
        await RenameOptionalColumnAsync(db, table, column);
        await db.Database.OpenConnectionAsync(Ct);
        if (!(table == "HomeLedgerReadState" && column == "SchemaVersion"))
        {
            // This is an explicit fault input, not normalization of a comparison: isolate the actual column contract.
            var cookie = await SchemaCookieAsync(db);
            await db.Database.ExecuteSqlRawAsync("UPDATE HomeLedgerReadState SET SchemaVersion={0}", [cookie], Ct);
        }
        await using var transaction = await db.Database.BeginTransactionAsync(Ct);
        var expected = JsonSerializer.Serialize((await CompleteHomeLedgerReader.ReadAsync(db, Ct))
            .OrderByDescending(entry => entry.Date).ThenByDescending(entry => entry.CreatedAt));
        var cache = await CaptureOptionalTablesAsync(db);
        Assert.Null(await HomeLedgerReadBlocks.TryReadAsync(db, Ct));
        Assert.Equal(expected, JsonSerializer.Serialize(await ZananceStore.ReadCompleteHomeLedgerAsync(db, Ct)));
        Assert.Equal(cache, await CaptureOptionalTablesAsync(db));
    }

    /// <summary>Original account metadata remains writable when optional transport has an unsupported column.</summary>
    [Theory]
    [InlineData("HomeLedgerReadState", "CodecVersion")]
    [InlineData("HomeLedgerReadState", "PublishedRevision")]
    [InlineData("HomeLedgerReadState", "SchemaVersion")]
    [InlineData("HomeLedgerReadBlocks", "Payload")]
    [InlineData("HomeLedgerReadBlocks", "Digest")]
    [InlineData("HomeLedgerReadBlocks", "RowCount")]
    [InlineData("HomeLedgerDirtyBlocks", "BlockId")]
    public async Task Unsupported_derived_columns_do_not_reject_original_account_writer(string table, string column)
    {
        await using var db = await SeedAsync();
        await RenameOptionalColumnAsync(db, table, column);
        var cache = await CaptureOptionalTablesAsync(db);
        var entries = JsonSerializer.Serialize(await db.Entries.AsNoTracking().OrderBy(entry => entry.Id).ToListAsync(Ct));
        (await db.Accounts.SingleAsync(Ct)).Name = "Accepted original metadata";
        await db.SaveChangesAsync(Ct);
        db.ChangeTracker.Clear();
        Assert.Equal("Accepted original metadata", (await db.Accounts.SingleAsync(Ct)).Name);
        Assert.Equal(entries, JsonSerializer.Serialize(await db.Entries.AsNoTracking().OrderBy(entry => entry.Id).ToListAsync(Ct)));
        Assert.Equal(cache, await CaptureOptionalTablesAsync(db));
    }

    private static Task RenameOptionalColumnAsync(ZananceDbContext db, string table, string column)
    {
        // Identifiers cannot be SQL parameters; admit only the closed fictitious schema faults above.
        var sql = (table, column) switch
        {
            ("HomeLedgerReadState", "CodecVersion") => "ALTER TABLE HomeLedgerReadState RENAME COLUMN CodecVersion TO UnsupportedColumn",
            ("HomeLedgerReadState", "PublishedRevision") => "ALTER TABLE HomeLedgerReadState RENAME COLUMN PublishedRevision TO UnsupportedColumn",
            ("HomeLedgerReadState", "SchemaVersion") => "ALTER TABLE HomeLedgerReadState RENAME COLUMN SchemaVersion TO UnsupportedColumn",
            ("HomeLedgerReadBlocks", "Payload") => "ALTER TABLE HomeLedgerReadBlocks RENAME COLUMN Payload TO UnsupportedColumn",
            ("HomeLedgerReadBlocks", "Digest") => "ALTER TABLE HomeLedgerReadBlocks RENAME COLUMN Digest TO UnsupportedColumn",
            ("HomeLedgerReadBlocks", "RowCount") => "ALTER TABLE HomeLedgerReadBlocks RENAME COLUMN RowCount TO UnsupportedColumn",
            ("HomeLedgerDirtyBlocks", "BlockId") => "ALTER TABLE HomeLedgerDirtyBlocks RENAME COLUMN BlockId TO UnsupportedColumn",
            _ => throw new InvalidOperationException("Unknown fictitious schema fault.")
        };
        return db.Database.ExecuteSqlRawAsync(sql, Ct);
    }

    private static async Task<string> CaptureOptionalTablesAsync(ZananceDbContext db)
    {
        await db.Database.OpenConnectionAsync(Ct);
        var captured = new List<object>();
        foreach (var table in new[] { "HomeLedgerReadState", "HomeLedgerReadBlocks", "HomeLedgerDirtyBlocks" })
        {
            using var command = db.Database.GetDbConnection().CreateCommand();
            command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
            command.CommandText = $"SELECT * FROM {table} ORDER BY rowid";
            await using var reader = await command.ExecuteReaderAsync(Ct);
            var names = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray();
            var rows = new List<object?[]>();
            while (await reader.ReadAsync(Ct))
                rows.Add(Enumerable.Range(0, reader.FieldCount).Select(index => reader.IsDBNull(index) ? null : reader.GetValue(index)).ToArray());
            captured.Add(new { Table = table, Columns = names, Rows = rows });
        }
        return JsonSerializer.Serialize(captured);
    }
}
