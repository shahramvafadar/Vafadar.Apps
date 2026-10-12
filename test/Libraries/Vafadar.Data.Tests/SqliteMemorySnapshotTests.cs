using System.Runtime.InteropServices;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace Vafadar.Data.Tests;

public sealed class SqliteMemorySnapshotTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task<byte[]> ImageAsync()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:;Pooling=False");
        await connection.OpenAsync(Ct);
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE packet(amount INTEGER, label TEXT); INSERT INTO packet VALUES(-9223372036854775808, 'Fictitious بانک Ü'); PRAGMA user_version=73;";
        await command.ExecuteNonQueryAsync(Ct);
        var pointer = raw.sqlite3_serialize(connection.Handle!, "main", out var length, 0);
        try { var bytes = new byte[checked((int)length)]; Marshal.Copy(pointer, bytes, 0, bytes.Length); return bytes; }
        finally { raw.sqlite3_free(pointer); }
    }

    [Fact, Trait("AT", "AT-185")]
    public async Task Memory_image_preserves_signed_amount_and_unicode_and_refuses_writes()
    {
        using var input = new MemoryStream(await ImageAsync());
        await using var snapshot = await SqliteMemorySnapshot.OpenAsync(input, 1024 * 1024, Ct);
        using var command = snapshot.Connection.CreateCommand(); command.CommandText = "SELECT amount FROM packet";
        Assert.Equal(long.MinValue, await command.ExecuteScalarAsync(Ct));
        command.CommandText = "SELECT label FROM packet";
        Assert.Equal("Fictitious بانک Ü", await command.ExecuteScalarAsync(Ct));
        command.CommandText = "DELETE FROM packet";
        await Assert.ThrowsAsync<SqliteException>(() => command.ExecuteNonQueryAsync(Ct));
    }

    [Fact, Trait("AT", "AT-185")]
    public async Task Wal_header_is_normalized_only_in_the_owned_memory_copy()
    {
        var bytes = await ImageAsync(); bytes[18] = bytes[19] = 2;
        using var input = new MemoryStream(bytes);
        await using var snapshot = await SqliteMemorySnapshot.OpenAsync(input, 1024 * 1024, Ct);
        using var command = snapshot.Connection.CreateCommand(); command.CommandText = "SELECT COUNT(*) FROM packet";
        Assert.Equal(1L, await command.ExecuteScalarAsync(Ct)); Assert.Equal(2, bytes[18]); Assert.Equal(2, bytes[19]);
    }

    [Fact]
    public async Task Oversized_input_is_refused_before_native_deserialization()
    {
        using var input = new MemoryStream(await ImageAsync());
        await Assert.ThrowsAsync<InvalidDataException>(() => SqliteMemorySnapshot.OpenAsync(input, 100, Ct));
    }

    [Fact]
    public async Task Non_database_input_is_refused()
    {
        using var input = new MemoryStream(new byte[500]);
        await Assert.ThrowsAsync<InvalidDataException>(() => SqliteMemorySnapshot.OpenAsync(input, 1000, Ct));
    }

    [Fact]
    public async Task Cancellation_is_not_reported_as_success()
    {
        using var input = new MemoryStream(await ImageAsync());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => SqliteMemorySnapshot.OpenAsync(input, 1024 * 1024, new CancellationToken(true)));
    }

    [Fact]
    public async Task Disposing_twice_does_not_release_the_native_image_twice()
    {
        using var input = new MemoryStream(await ImageAsync());
        var snapshot = await SqliteMemorySnapshot.OpenAsync(input, 1024 * 1024, Ct);
        await snapshot.DisposeAsync(); await snapshot.DisposeAsync();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => snapshot.ExportEncryptedAsync("unused", "fictitious", Ct));
    }
}
