using System.Data.Common;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Data.Tests;

public sealed partial class HomeLedgerBlockStoreTests
{
    [Fact]
    public async Task Each_complete_read_owns_one_reusable_buffer_and_clears_it_before_returning_independent_rows()
    {
        await using var db = await SeedMultipleBufferBlocksAsync();
        await using var read = await db.Database.BeginTransactionAsync(Ct);
        var expected = await CompleteHomeLedgerReader.ReadAsync(db, Ct);
        var before = await StateAsync(db); var owners = 0; var arrays = new List<byte[]>();
        HomeLedgerBlockBuffer Create() { owners++; return new(count => { var bytes = new byte[count]; arrays.Add(bytes); return bytes; }); }
        var first = await HomeLedgerReadBlocks.TryReadAsync(db, Ct, Create); Assert.NotNull(first);
        Assert.Equal(CanonicalEntries(expected), CanonicalEntries(first)); Assert.Equal(1, owners);
        Assert.All(arrays, bytes => Assert.True(BufferCleared(bytes))); Assert.InRange(arrays.Count, 1, 2);
        first[0].Title = "changed only returned row";
        var second = await HomeLedgerReadBlocks.TryReadAsync(db, Ct, Create); Assert.NotNull(second);
        Assert.Equal(CanonicalEntries(expected), CanonicalEntries(second)); Assert.Equal(2, owners);
        Assert.NotSame(first[0], second[0]); Assert.NotSame(first[0].Tags, second[0].Tags);
        Assert.All(arrays, bytes => Assert.True(BufferCleared(bytes))); Assert.InRange(arrays.Count, 2, 4);
        Assert.Equal(before, await StateAsync(db));
    }

    [Fact]
    public async Task A_later_digest_valid_bad_block_clears_prior_plaintext_before_complete_original_scalar_fallback()
    {
        await using var db = await SeedMultipleBufferBlocksAsync(); await db.Database.OpenConnectionAsync(Ct);
        long id; byte[] stored;
        using (var command = db.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = "SELECT BlockId, Payload FROM HomeLedgerReadBlocks ORDER BY BlockId DESC LIMIT 1 OFFSET 1";
            await using var row = await command.ExecuteReaderAsync(Ct); Assert.True(await row.ReadAsync(Ct));
            id = row.GetInt64(0); stored = (byte[])row.GetValue(1);
        }
        var damaged = stored[..^1]; BinaryPrimitives.WriteInt32LittleEndian(damaged.AsSpan(20), damaged.Length - 24);
        await db.Database.ExecuteSqlRawAsync("UPDATE HomeLedgerReadBlocks SET Payload={0}, Digest={1} WHERE BlockId={2}",
            [damaged, SHA256.HashData(damaged), id], Ct);
        await using var read = await db.Database.BeginTransactionAsync(Ct);
        var expected = await CompleteHomeLedgerReader.ReadAsync(db, Ct); var before = await StateAsync(db);
        var arrays = new List<byte[]>(); var owners = 0;
        var transport = await HomeLedgerReadBlocks.TryReadAsync(db, Ct, () =>
        { owners++; return new(count => { var bytes = new byte[count]; arrays.Add(bytes); return bytes; }); });
        Assert.Null(transport); Assert.Equal(1, owners); Assert.NotEmpty(arrays);
        Assert.All(arrays, bytes => Assert.True(BufferCleared(bytes)));
        Assert.Equal(CanonicalEntries(expected), CanonicalEntries(await ZananceStore.ReadCompleteHomeLedgerAsync(db, Ct)));
        Assert.Equal(before, await StateAsync(db));
        using var verify = db.Database.GetDbConnection().CreateCommand(); verify.Transaction = read.GetDbTransaction();
        verify.CommandText = "SELECT Payload FROM HomeLedgerReadBlocks WHERE BlockId=" + id.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(damaged, Assert.IsType<byte[]>(await verify.ExecuteScalarAsync(Ct)));
    }

    [Fact]
    public async Task Cancellation_during_later_expansion_clears_the_same_read_owner_and_preserves_original_source()
    {
        await using var db = await SeedMultipleBufferBlocksAsync(); await using var read = await db.Database.BeginTransactionAsync(Ct);
        var expected = CanonicalEntries(await CompleteHomeLedgerReader.ReadAsync(db, Ct)); var before = await StateAsync(db);
        using var cancelled = CancellationTokenSource.CreateLinkedTokenSource(Ct); var calls = 0; var arrays = new List<byte[]>();
        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => HomeLedgerReadBlocks.TryReadAsync(db, cancelled.Token,
            () => new HomeLedgerBlockBuffer(count =>
            {
                var bytes = new byte[count]; arrays.Add(bytes); if (++calls == 2) cancelled.Cancel(); return bytes;
            })));
        Assert.Equal(cancelled.Token, error.CancellationToken); Assert.Equal(2, calls);
        Assert.All(arrays, bytes => Assert.True(BufferCleared(bytes)));
        Assert.Equal(expected, CanonicalEntries(await CompleteHomeLedgerReader.ReadAsync(db, Ct)));
        Assert.Equal(before, await StateAsync(db));
    }

    private async Task<ZananceDbContext> SeedMultipleBufferBlocksAsync()
    {
        var db = await SeedAsync(); var account = await db.Accounts.Select(value => value.Id).SingleAsync(Ct);
        db.Entries.AddRange(Enumerable.Range(0, 1030).Select(index => new LedgerEntry
        {
            AccountId = account, Date = new(2026, 10, 4), Amount = index + 1, Kind = EntryKind.Expense,
            Title = "Original buffer row " + index, Note = "Unchanged fictitious source", Tags = []
        }));
        await db.SaveChangesAsync(Ct); Assert.True(db.CanUseHomeLedgerBlocks); return db;
    }

    private static bool BufferCleared(byte[] bytes) => bytes.AsSpan().IndexOfAnyExcept((byte)0) < 0;

    private static string CanonicalEntries(IEnumerable<LedgerEntry> entries) => JsonSerializer.Serialize(entries.OrderBy(entry => entry.Id));
}
