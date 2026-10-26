using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using OriginalBlocks = Vafadar.Zanance.Data.Migrations.HomeLedgerReadBlocks;
using ConflictGuards = Vafadar.Zanance.Data.Migrations.HomeLedgerDirtyBlockConflictGuards;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>Exercises actual additive trigger migration under original outer and foreign-key write policies.</summary>
public sealed class HomeLedgerDirtyBlockConflictTests
{
    [Theory, Trait("AT", "AT-82")]
    [InlineData("ABORT")]
    [InlineData("FAIL")]
    [InlineData("ROLLBACK")]
    public void Existing_dirty_bucket_does_not_override_an_explicit_source_insert_policy(string policy)
    {
        using var db = Open();
        Sql(db, "INSERT INTO HomeLedgerDirtyBlocks VALUES(1);");
        Sql(db, $"INSERT OR {policy} INTO Entries(rowid,Id,Title) VALUES(512,'next','next title');");
        Assert.Equal("next title", Scalar(db, "SELECT Title FROM Entries WHERE Id='next';"));
        Assert.Equal("0,1", Buckets(db));
    }

    [Fact, Trait("AT", "AT-82")]
    public void Same_bucket_old_and_new_update_markers_do_not_abort_the_original_update()
    {
        using var db = Open();
        var revision = Scalar(db, "SELECT Revision FROM HomeLedgerReadState;");
        Sql(db, "UPDATE OR ABORT Entries SET Title='changed' WHERE Id='purchase';");
        Assert.Equal("changed", Scalar(db, "SELECT Title FROM Entries WHERE Id='purchase';"));
        Assert.NotEqual(revision, Scalar(db, "SELECT Revision FROM HomeLedgerReadState;"));
        Assert.Equal("0", Buckets(db));
    }

    [Fact, Trait("AT", "AT-82")]
    public void Purchase_delete_preserves_refund_and_clears_its_link_with_one_dirty_bucket()
    {
        using var db = Open();
        Sql(db, "DELETE FROM Entries WHERE Id='purchase';");
        Assert.Equal("refund", Scalar(db, "SELECT Title FROM Entries WHERE Id='refund';"));
        Assert.Equal("1", Scalar(db, "SELECT RefundOfEntryId IS NULL FROM Entries WHERE Id='refund';"));
        Assert.Equal("1", Scalar(db, "SELECT count(*) FROM Entries;"));
        Assert.Equal("0", Buckets(db));
    }

    [Fact, Trait("AT", "AT-82")]
    public void Rolled_back_cross_bucket_update_preserves_source_revision_and_original_dirty_set()
    {
        using var db = Open();
        var before = Rows(db);
        var revision = Scalar(db, "SELECT Revision FROM HomeLedgerReadState;");
        Sql(db, "BEGIN; UPDATE OR ABORT Entries SET rowid=1024,Title='temporary' WHERE Id='purchase'; ROLLBACK;");
        Assert.Equal(before, Rows(db));
        Assert.Equal(revision, Scalar(db, "SELECT Revision FROM HomeLedgerReadState;"));
        Assert.Equal("0", Buckets(db));
    }

    [Fact, Trait("AT", "AT-82")]
    public void Source_identity_conflict_still_rejects_the_financial_write()
    {
        using var db = Open();
        var before = Rows(db);
        var error = Assert.Throws<SqliteException>(() => Sql(db, "INSERT OR ABORT INTO Entries(Id,Title) VALUES('purchase','must not replace');"));
        Assert.Equal(19, error.SqliteErrorCode);
        Assert.Equal(before, Rows(db));
    }

    [Fact, Trait("AT", "AT-82")]
    public void Additive_upgrade_and_down_preserve_every_source_row_and_original_trigger_definitions()
    {
        using var db = Open(upgrade: false);
        var before = Rows(db);
        var revision = Scalar(db, "SELECT Revision FROM HomeLedgerReadState;");
        var definitions = Triggers(db);
        Apply(db, new ConflictGuards().UpOperations.Cast<SqlOperation>());
        Assert.Equal(before, Rows(db));
        Assert.Equal(revision, Scalar(db, "SELECT Revision FROM HomeLedgerReadState;"));
        Assert.Equal("0", Buckets(db));
        Assert.NotEqual(definitions, Triggers(db));
        Apply(db, new ConflictGuards().DownOperations.Cast<SqlOperation>());
        Assert.Equal(definitions, Triggers(db));
        Assert.Equal(before, Rows(db));
    }

    private static SqliteConnection Open(bool upgrade = true)
    {
        var db = new SqliteConnection("Data Source=:memory:"); db.Open();
        Sql(db, """
            PRAGMA foreign_keys=ON;
            CREATE TABLE Entries(Id TEXT NOT NULL PRIMARY KEY,Title TEXT,
                RefundOfEntryId TEXT REFERENCES Entries(Id) ON DELETE SET NULL);
            INSERT INTO Entries(rowid,Id,Title) VALUES(510,'purchase','purchase');
            INSERT INTO Entries(rowid,Id,Title,RefundOfEntryId) VALUES(511,'refund','refund','purchase');
            """);
        Apply(db, new OriginalBlocks().UpOperations.Cast<SqlOperation>());
        if (upgrade) Apply(db, new ConflictGuards().UpOperations.Cast<SqlOperation>());
        return db;
    }

    private static void Apply(SqliteConnection db, IEnumerable<SqlOperation> operations)
    { foreach (var operation in operations) Sql(db, operation.Sql); }

    private static void Sql(SqliteConnection db, string sql)
    { using var command = db.CreateCommand(); command.CommandText = sql; command.ExecuteNonQuery(); }

    private static string Scalar(SqliteConnection db, string sql)
    { using var command = db.CreateCommand(); command.CommandText = sql; return Convert.ToString(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture)!; }

    private static string Buckets(SqliteConnection db) => Scalar(db,
        "SELECT group_concat(BlockId) FROM (SELECT BlockId FROM HomeLedgerDirtyBlocks ORDER BY BlockId);");

    private static string Rows(SqliteConnection db) => Scalar(db,
        "SELECT group_concat(rowid||':'||Id||':'||Title||':'||coalesce(RefundOfEntryId,'null')) FROM (SELECT rowid,* FROM Entries ORDER BY rowid);");

    private static string Triggers(SqliteConnection db) => Scalar(db,
        "SELECT group_concat(sql) FROM (SELECT sql FROM sqlite_master WHERE type='trigger' ORDER BY name);");
}
