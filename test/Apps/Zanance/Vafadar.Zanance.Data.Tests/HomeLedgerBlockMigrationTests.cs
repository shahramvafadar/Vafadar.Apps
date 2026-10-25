using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using LedgerBlockMigration = Vafadar.Zanance.Data.Migrations.HomeLedgerReadBlocks;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>Actual migration statements invalidate derived blocks atomically for original SQLite writers.</summary>
public sealed class HomeLedgerBlockMigrationTests
{
    [Theory, Trait("AT", "AT-82")]
    [InlineData("UPDATE Entries SET Title='changed' WHERE rowid=511;", "0")]
    [InlineData("DELETE FROM Entries WHERE rowid=511;", "0")]
    [InlineData("UPDATE Entries SET rowid=-1 WHERE rowid=511;", "-1,0")]
    public void Raw_mutations_invalidate_publication_and_capture_all_affected_buckets(string mutation, string buckets)
        => AssertInvalidated(mutation, buckets);

    [Fact, Trait("AT", "AT-82")]
    public void Raw_insert_invalidates_publication_and_captures_the_new_bucket()
        => AssertInvalidated("INSERT INTO Entries(rowid, Id, Title) VALUES(1024, 'new-id', 'new');", "2");

    private static void AssertInvalidated(string mutation, string buckets)
    {
        using var db = Open();
        Execute(db, "DELETE FROM HomeLedgerDirtyBlocks; UPDATE HomeLedgerReadState SET PublishedRevision=Revision;");
        var previous = Scalar(db, "SELECT Revision FROM HomeLedgerReadState;");
        Execute(db, mutation);
        Assert.NotEqual(previous, Scalar(db, "SELECT Revision FROM HomeLedgerReadState;"));
        Assert.Equal(previous, Scalar(db, "SELECT PublishedRevision FROM HomeLedgerReadState;"));
        Assert.Equal(buckets, Scalar(db, "SELECT group_concat(BlockId) FROM (SELECT BlockId FROM HomeLedgerDirtyBlocks ORDER BY BlockId);"));
    }

    [Fact, Trait("AT", "AT-82")]
    public void Rollback_restores_source_revision_and_dirty_set_together()
    {
        using var db = Open();
        var before = Scalar(db, "SELECT Revision FROM HomeLedgerReadState;");
        Execute(db, "BEGIN; DELETE FROM HomeLedgerDirtyBlocks; UPDATE Entries SET rowid=1024, Title='temporary'; ROLLBACK;");
        Assert.Equal(before, Scalar(db, "SELECT Revision FROM HomeLedgerReadState;"));
        Assert.Equal("original", Scalar(db, "SELECT Title FROM Entries WHERE rowid=511;"));
        Assert.Equal("0", Scalar(db, "SELECT group_concat(BlockId) FROM HomeLedgerDirtyBlocks;"));
    }

    [Theory, Trait("AT", "AT-82")]
    [InlineData(long.MinValue), InlineData(-513L), InlineData(-512L), InlineData(-1L)]
    [InlineData(0L), InlineData(512L), InlineData(long.MaxValue)]
    public void SQLite_bucket_identity_matches_the_typed_codec_for_signed_boundaries(long rowId)
    {
        using var db = Open();
        using var command = db.CreateCommand();
        command.CommandText = "SELECT $row >> 9;"; command.Parameters.AddWithValue("$row", rowId);
        Assert.Equal(rowId >> HomeLedgerBlockCodec.BlockShift, (long)command.ExecuteScalar()!);
    }

    [Fact, Trait("AT", "AT-82")]
    public void Migration_starts_unpublished_and_down_preserves_original_rows()
    {
        using var db = Open();
        Assert.Equal("1", Scalar(db, "SELECT PublishedRevision IS NULL AND SchemaVersion IS NULL AND CodecVersion IS NULL AND RowCount IS NULL FROM HomeLedgerReadState;"));
        foreach (var operation in new LedgerBlockMigration().DownOperations.Cast<SqlOperation>()) Execute(db, operation.Sql);
        Assert.Equal("original", Scalar(db, "SELECT Title FROM Entries WHERE rowid=511;"));
        Assert.Equal("0", Scalar(db, "SELECT count(*) FROM sqlite_master WHERE name LIKE 'HomeLedger%';"));
    }

    private static SqliteConnection Open()
    {
        var db = new SqliteConnection("Data Source=:memory:"); db.Open();
        Execute(db, "CREATE TABLE Entries(Id TEXT NOT NULL PRIMARY KEY, Title TEXT); INSERT INTO Entries(rowid, Id, Title) VALUES(511, 'original-id', 'original');");
        foreach (var operation in new LedgerBlockMigration().UpOperations.Cast<SqlOperation>()) Execute(db, operation.Sql);
        return db;
    }

    private static void Execute(SqliteConnection db, string sql)
    { using var command = db.CreateCommand(); command.CommandText = sql; command.ExecuteNonQuery(); }

    private static string Scalar(SqliteConnection db, string sql)
    { using var command = db.CreateCommand(); command.CommandText = sql; return Convert.ToString(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture)!; }
}
