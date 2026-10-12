using Microsoft.Data.Sqlite;

namespace Vafadar.Data.Tests;

public sealed class SqliteSnapshotVerifierTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task<SqliteConnection> Database(string data)
    {
        var connection = new SqliteConnection("Data Source=:memory:;Pooling=False");
        await connection.OpenAsync(Ct);
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE packet(value,label,content); PRAGMA user_version=73; PRAGMA application_id=123456; " + data;
        await command.ExecuteNonQueryAsync(Ct); return connection;
    }

    [Fact, Trait("AT", "AT-186")]
    public async Task Row_order_is_irrelevant_but_duplicate_multiplicity_and_typed_packets_are_preserved()
    {
        const string first = "INSERT INTO packet VALUES(-9223372036854775808,'Fictitious بانک Ü',x'0001FF'),(NULL,'same',x''),(NULL,'same',x'');";
        const string second = "INSERT INTO packet VALUES(NULL,'same',x''),(NULL,'same',x''),(-9223372036854775808,'Fictitious بانک Ü',x'0001FF');";
        await using var a = await Database(first); await using var b = await Database(second);
        await SqliteSnapshotVerifier.VerifyAsync(a,b,Ct);
    }

    [Theory, Trait("AT", "AT-186")]
    [InlineData("INSERT INTO packet VALUES(1,'same',x'0102');", "INSERT INTO packet VALUES(2,'same',x'0102');")]
    [InlineData("INSERT INTO packet VALUES(1,'same',x'0102');", "INSERT INTO packet VALUES(1.0,'same',x'0102');")]
    [InlineData("INSERT INTO packet VALUES(1,'same',x'0102');", "INSERT INTO packet VALUES(1,'changed',x'0102');")]
    [InlineData("INSERT INTO packet VALUES(1,'same',x'0102');", "INSERT INTO packet VALUES(1,'same',x'0103');")]
    [InlineData("INSERT INTO packet VALUES(1,'same',x'0102'),(1,'same',x'0102');", "INSERT INTO packet VALUES(1,'same',x'0102');")]
    public async Task Changed_content_type_or_duplicate_count_is_refused(string first,string second)
    {
        await using var a = await Database(first); await using var b = await Database(second);
        await Assert.ThrowsAsync<InvalidDataException>(() => SqliteSnapshotVerifier.VerifyAsync(a,b,Ct));
    }

    [Theory]
    [InlineData("PRAGMA user_version=74")]
    [InlineData("PRAGMA application_id=123457")]
    [InlineData("CREATE INDEX additional ON packet(label)")]
    public async Task Changed_header_or_schema_is_refused(string change)
    {
        await using var a = await Database(""); await using var b = await Database("");
        using var command = b.CreateCommand(); command.CommandText=change; await command.ExecuteNonQueryAsync(Ct);
        await Assert.ThrowsAsync<InvalidDataException>(() => SqliteSnapshotVerifier.VerifyAsync(a,b,Ct));
    }

    [Fact]
    public async Task Cancellation_keeps_both_callers_usable()
    {
        await using var a = await Database(""); await using var b = await Database("");
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => SqliteSnapshotVerifier.VerifyAsync(a,b,new CancellationToken(true)));
        await SqliteSnapshotVerifier.VerifyAsync(a,b,Ct);
    }
    [Fact]
    public async Task Equal_but_invalid_foreign_key_rows_are_not_accepted()
    {
        await using var a = await Database(""); await using var b = await Database("");
        foreach (var connection in new[] { a,b })
        {
            using var command = connection.CreateCommand();
            command.CommandText="PRAGMA foreign_keys=OFF; CREATE TABLE owner(id PRIMARY KEY); CREATE TABLE dependent(id REFERENCES owner(id)); INSERT INTO dependent VALUES(7);";
            await command.ExecuteNonQueryAsync(Ct);
        }
        await Assert.ThrowsAsync<InvalidDataException>(() => SqliteSnapshotVerifier.VerifyAsync(a,b,Ct));
    }

    [Fact]
    public async Task Large_blob_comparison_checks_the_last_byte_without_driver_metadata_lookup()
    {
        await using var a = await Database(""); await using var b = await Database("");
        var blob = new byte[200000]; blob[^1] = 7;
        foreach (var connection in new[] { a,b })
        {
            using var command=connection.CreateCommand(); command.CommandText="INSERT INTO packet VALUES(1,'large',$bytes)";
            command.Parameters.AddWithValue("$bytes",blob); await command.ExecuteNonQueryAsync(Ct);
        }
        await SqliteSnapshotVerifier.VerifyAsync(a,b,Ct);
        blob[^1] = 8;
        using var changed=b.CreateCommand(); changed.CommandText="UPDATE packet SET content=$bytes";
        changed.Parameters.AddWithValue("$bytes",blob); await changed.ExecuteNonQueryAsync(Ct);
        await Assert.ThrowsAsync<InvalidDataException>(() => SqliteSnapshotVerifier.VerifyAsync(a,b,Ct));
    }
}
