using System.Runtime.InteropServices;
using Microsoft.Data.Sqlite;
using SQLitePCL;
using Vafadar.Testing;

namespace Vafadar.Data.Tests;

/// <summary>AT-198: the actual staging-space boundary refuses before key or candidate effects.</summary>
[Trait("AT", "AT-198")]
public sealed class StorageSpaceClassificationTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Insufficient_original_bound_is_a_typed_storage_refusal_without_key_or_output_effects()
    {
        using var directory = new TemporaryDirectory();
        var candidate = directory.Combine("candidate.db");
        await using var connection = new SqliteConnection("Data Source=:memory:;Pooling=False");
        await connection.OpenAsync(Ct);
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE packet(value INTEGER); INSERT INTO packet VALUES(123);";
        await command.ExecuteNonQueryAsync(Ct);
        var pointer = raw.sqlite3_serialize(connection.Handle!, "main", out var length, 0);
        var bytes = new byte[checked((int)length)];
        try { Marshal.Copy(pointer, bytes, 0, bytes.Length); }
        finally { raw.sqlite3_free(pointer); }
        var original = bytes.ToArray();
        using var portable = new MemoryStream(bytes, writable: false);
        var keys = new UnexpectedKeys();
        const int maximum = 1024 * 1024;
        var durability = new AvailableSpace(2L * maximum + 16 * 1024 * 1024 - 1);

        await Assert.ThrowsAsync<InsufficientDatabaseStorageException>(() => SqlCipherRestoreStaging.PrepareAsync(
            portable, candidate, new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), keys, durability,
            (_, _) => throw new InvalidOperationException("Unexpected upgrade effect."), () => { }, maximum, "4.19.0", Ct));

        Assert.Equal(0, keys.Prepares);
        Assert.False(File.Exists(candidate));
        Assert.Equal(original, bytes);
    }

    private sealed class UnexpectedKeys : IDatabaseGenerationKeyStore
    {
        internal int Prepares;
        public Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken ct)
        { Prepares++; throw new InvalidOperationException("Unexpected key effect."); }
        public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken ct)
            => throw new InvalidOperationException("Unexpected key read.");
    }

    private sealed class AvailableSpace(long bytes) : IDatabaseGenerationFileDurability
    {
        public long AvailableBytes => bytes;
        public Task SynchronizeFileAsync(string path, CancellationToken ct) => throw new InvalidOperationException("Unexpected disk effect.");
        public Task SynchronizeDirectoryAsync(CancellationToken ct) => throw new InvalidOperationException("Unexpected disk effect.");
    }
}
