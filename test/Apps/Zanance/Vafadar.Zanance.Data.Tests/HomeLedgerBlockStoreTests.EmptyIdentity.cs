using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Vafadar.Zanance.Data.Tests;

public sealed partial class HomeLedgerBlockStoreTests
{
    /// <summary>A matching digest cannot make an empty cached identity override complete authoritative rows.</summary>
    [Theory]
    [InlineData(0), InlineData(1)]
    public async Task Empty_derived_identity_falls_back_without_partial_rows_or_repair(int damagedRow)
    {
        await using var db = await SeedAsync();
        await db.Database.OpenConnectionAsync(Ct);
        long blockId;
        byte[] damaged;
        using (var command = db.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = "SELECT BlockId, Payload FROM HomeLedgerReadBlocks ORDER BY BlockId LIMIT 1";
            await using var reader = await command.ExecuteReaderAsync(Ct);
            Assert.True(await reader.ReadAsync(Ct));
            blockId = reader.GetInt64(0);
            damaged = EmptyDerivedIdentityPayload.Damage((byte[])reader.GetValue(1), blockId, damagedRow);
        }
        var digest = SHA256.HashData(damaged);
        await db.Database.ExecuteSqlRawAsync("UPDATE HomeLedgerReadBlocks SET Payload={0}, Digest={1} WHERE BlockId={2}",
            [damaged, digest, blockId], Ct);
        await using var transaction = await db.Database.BeginTransactionAsync(Ct);
        var expected = (await CompleteHomeLedgerReader.ReadAsync(db, Ct)).OrderByDescending(entry => entry.Date)
            .ThenByDescending(entry => entry.CreatedAt).ToList();
        Assert.Equal(2, expected.Count);
        var state = await StateAsync(db);
        Assert.Null(await HomeLedgerReadBlocks.TryReadAsync(db, Ct));
        Assert.Equal(JsonSerializer.Serialize(expected),
            JsonSerializer.Serialize(await ZananceStore.ReadCompleteHomeLedgerAsync(db, Ct)));
        Assert.Equal(state, await StateAsync(db));
        using var retained = db.Database.GetDbConnection().CreateCommand();
        retained.Transaction = transaction.GetDbTransaction();
        retained.CommandText = "SELECT Payload, Digest, RowCount FROM HomeLedgerReadBlocks WHERE BlockId=$block";
        retained.Parameters.Add(new SqliteParameter("$block", blockId));
        await using var actual = await retained.ExecuteReaderAsync(Ct);
        Assert.True(await actual.ReadAsync(Ct));
        Assert.Equal(damaged, (byte[])actual.GetValue(0));
        Assert.Equal(digest, (byte[])actual.GetValue(1));
        Assert.Equal(2, actual.GetInt64(2));
    }
}

/// <summary>Changes only one original entry identity inside an otherwise valid recompressed derived block.</summary>
internal static class EmptyDerivedIdentityPayload
{
    /// <summary>Locates the exact row through original complete-row encoding rather than guessing variable field lengths.</summary>
    internal static byte[] Damage(byte[] payload, long bucket, int damagedRow)
    {
        var rows = HomeLedgerBlockCodec.Decode(payload, bucket, TestContext.Current.CancellationToken);
        Assert.InRange(damagedRow, 0, rows.Count - 1);
        var raw = new byte[BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(16))];
        Assert.True(BrotliDecoder.TryDecompress(payload.AsSpan(24), raw, out var expanded));
        Assert.Equal(raw.Length, expanded);
        var offset = sizeof(long);
        for (var index = 0; index < damagedRow; index++)
        {
            var prefix = HomeLedgerBlockCodec.Encode([rows[index]], TestContext.Current.CancellationToken);
            offset += BinaryPrimitives.ReadInt32LittleEndian(prefix.AsSpan(16));
        }
        Assert.Equal(rows[damagedRow].Entry.Id, new Guid(raw.AsSpan(offset, 16)));
        Assert.NotEqual(Guid.Empty, rows[damagedRow].Entry.Id);
        raw.AsSpan(offset, 16).Clear();
        var compressed = new byte[BrotliEncoder.GetMaxCompressedLength(raw.Length)];
        Assert.True(BrotliEncoder.TryCompress(raw, compressed, out var written, quality: 1, window: 22));
        var result = new byte[24 + written];
        payload.AsSpan(0, 24).CopyTo(result);
        BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(20), written);
        compressed.AsSpan(0, written).CopyTo(result.AsSpan(24));
        return result;
    }
}
