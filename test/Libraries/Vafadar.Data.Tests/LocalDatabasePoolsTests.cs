using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;
using Vafadar.Testing;

namespace Vafadar.Data.Tests;

/// <summary>Real native-session evidence for exact-file cleanup, profile moves and restore.</summary>
public sealed class LocalDatabasePoolsTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static string Connection(string path, SqliteOpenMode? mode = SqliteOpenMode.ReadWriteCreate)
    {
        var builder = new SqliteConnectionStringBuilder { DataSource = Path.GetFullPath(path) };
        if (mode.HasValue) builder.Mode = mode.Value;
        return builder.ToString();
    }

    [Theory, Trait("AT", "AT-139")]
    [InlineData(null), InlineData(SqliteOpenMode.ReadWriteCreate), InlineData(SqliteOpenMode.ReadWrite), InlineData(SqliteOpenMode.ReadOnly)]
    public void File_cleanup_retires_only_its_known_native_session(SqliteOpenMode? mode)
    {
        using var owned = new TestDatabase(); using var other = new TestDatabase();
        var path = owned.Services.GetRequiredService<LocalDatabaseLocation<TestDbContext>>().Path;
        var otherPath = other.Services.GetRequiredService<LocalDatabaseLocation<TestDbContext>>().Path;
        var target = Connection(path, mode); var foreign = Connection(otherPath);
        SqlitePoolProbe.Mark(target); SqlitePoolProbe.Mark(foreign);
        Assert.Equal("retained", SqlitePoolProbe.Read(target));
        LocalDatabasePools.ClearFile(path);
        Assert.Null(SqlitePoolProbe.Read(target));
        Assert.Equal("retained", SqlitePoolProbe.Read(foreign));
    }

    [Fact, Trait("AT", "AT-139")]
    public void Moving_a_profile_retires_the_old_file_without_retiring_another_database()
    {
        using var owned = new TestDatabase(); using var other = new TestDatabase();
        var location = owned.Services.GetRequiredService<LocalDatabaseLocation<TestDbContext>>();
        var original = Connection(location.Path);
        var foreign = Connection(other.Services.GetRequiredService<LocalDatabaseLocation<TestDbContext>>().Path);
        SqlitePoolProbe.Mark(original); SqlitePoolProbe.Mark(foreign);
        location.MoveTo(owned.PathOf("profiles/new.db"));
        Assert.Null(SqlitePoolProbe.Read(original));
        Assert.Equal("retained", SqlitePoolProbe.Read(foreign));
    }

    [Fact, Trait("AT", "AT-139")]
    public async Task Restoring_a_snapshot_does_not_retire_another_database_session()
    {
        using var owned = new TestDatabase(); using var other = new TestDatabase();
        var source = owned.Services.GetRequiredService<IBackupSource>();
        using var snapshot = new MemoryStream(); await source.WriteAsync(snapshot, Ct);
        await using (var context = owned.CreateContext())
        {
            context.Notes.Add(new Note { Text = "Fictitious later row" }); await context.SaveChangesAsync(Ct);
        }
        var foreign = Connection(other.Services.GetRequiredService<LocalDatabaseLocation<TestDbContext>>().Path);
        SqlitePoolProbe.Mark(foreign); snapshot.Position = 0;
        await source.RestoreAsync(snapshot, Ct);
        Assert.Equal("retained", SqlitePoolProbe.Read(foreign));
        await using var restored = owned.CreateContext(); Assert.Empty(await restored.Notes.ToListAsync(Ct));
    }

    [Fact, Trait("AT", "AT-139")]
    public async Task Restore_retires_its_captured_destination_even_after_the_current_profile_moves()
    {
        using var owned = new TestDatabase(); using var other = new TestDatabase();
        var location = owned.Services.GetRequiredService<LocalDatabaseLocation<TestDbContext>>(); var firstPath = location.Path;
        var otherLocation = other.Services.GetRequiredService<LocalDatabaseLocation<TestDbContext>>();
        var source = owned.Services.GetRequiredService<IBackupSource>(); using var snapshot = new MemoryStream();
        await source.WriteAsync(snapshot, Ct);
        await using (var context = owned.CreateContext())
        {
            context.Notes.Add(new Note { Text = "Fictitious later row" }); await context.SaveChangesAsync(Ct);
        }
        await using (var context = other.CreateContext())
        {
            context.Notes.Add(new Note { Text = "Fictitious unrelated profile" }); await context.SaveChangesAsync(Ct);
        }
        var foreign = Connection(otherLocation.Path); SqlitePoolProbe.Mark(foreign);
        using var input = new MoveOnCopyStream(snapshot.ToArray(), () => location.MoveTo(otherLocation.Path));
        await source.RestoreAsync(input, Ct);
        Assert.Equal(otherLocation.Path, location.Path); Assert.Equal("retained", SqlitePoolProbe.Read(foreign));
        await using (var context = other.CreateContext()) Assert.Equal("Fictitious unrelated profile", (await context.Notes.SingleAsync(Ct)).Text);
        location.MoveTo(firstPath);
        await using var restored = owned.CreateContext(); Assert.Empty(await restored.Notes.ToListAsync(Ct));
    }

    [Fact, Trait("AT", "AT-139")]
    public void Fixture_cleanup_includes_nested_owned_files_and_preserves_foreign_sessions()
    {
        using var directory = new TemporaryDirectory(); using var other = new TestDatabase();
        var nested = directory.Combine("profiles/nested.db"); Directory.CreateDirectory(Path.GetDirectoryName(nested)!);
        var firstPath = directory.Combine("first.db");
        File.WriteAllBytes(firstPath, []); File.WriteAllBytes(nested, []);
        var first = Connection(firstPath); var second = Connection(nested);
        var foreign = Connection(other.Services.GetRequiredService<LocalDatabaseLocation<TestDbContext>>().Path);
        SqlitePoolProbe.Mark(first); SqlitePoolProbe.Mark(second); SqlitePoolProbe.Mark(foreign);
        SqliteTestPools.Clear(directory);
        Assert.Null(SqlitePoolProbe.Read(first)); Assert.Null(SqlitePoolProbe.Read(second));
        Assert.Equal("retained", SqlitePoolProbe.Read(foreign));
        // Read opens fresh owned connections; release those before deleting the isolated directory as well.
        SqliteTestPools.Clear(directory);
    }

    private sealed class MoveOnCopyStream(byte[] bytes, Action move) : MemoryStream(bytes)
    {
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            move();
            return base.CopyToAsync(destination, bufferSize, cancellationToken);
        }
    }
}
