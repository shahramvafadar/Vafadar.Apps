using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;

namespace Vafadar.Data.Tests;

/// <summary>Actual factory publication/refusal contracts with isolated SQLite; actual keyed writes use the native probe.</summary>
public sealed class ProtectedLocalDatabaseFactoryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static SqliteConnection Connection(string path) => new(new SqliteConnectionStringBuilder
    { DataSource = path, Mode = SqliteOpenMode.ReadWrite, Pooling = false, Password = Convert.ToBase64String(new byte[32]) }.ToString());

    [Fact, Trait("AT", "AT-196")]
    public async Task Protected_factory_refuses_plaintext_before_the_default_provider_can_open_it()
    {
        using var database = new TestDatabase();
        var location = database.Services.GetRequiredService<LocalDatabaseLocation<TestDbContext>>();
        var path = location.Path; location.MoveToProtected(path, () => Connection(path), "4.19.0");
        await using var context = await database.Services.GetRequiredService<IDbContextFactory<TestDbContext>>().CreateDbContextAsync(Ct);
        await Assert.ThrowsAsync<InvalidDataException>(() => context.Notes.ToListAsync(Ct));
    }

    [Fact]
    public async Task Protected_missing_file_is_not_recreated_by_context_query()
    {
        using var database = new TestDatabase();
        var path = database.PathOf("data/missing.db");
        database.Services.GetRequiredService<LocalDatabaseLocation<TestDbContext>>().MoveToProtected(path, () => Connection(path), "4.19.0");
        await using var context = database.CreateContext();
        await Assert.ThrowsAsync<IOException>(() => context.Notes.ToListAsync(Ct)); Assert.False(File.Exists(path));
    }

    [Theory]
    [InlineData("create")]
    [InlineData("pool")]
    [InlineData("key")]
    [InlineData("path")]
    public void Unsafe_or_foreign_connection_creator_is_rejected_before_a_context_is_returned(string fault)
    {
        using var database = new TestDatabase();
        var location = database.Services.GetRequiredService<LocalDatabaseLocation<TestDbContext>>(); var path = location.Path;
        location.MoveToProtected(path, () =>
        {
            var options = new SqliteConnectionStringBuilder(Connection(path).ConnectionString);
            if (fault == "create") options.Mode = SqliteOpenMode.ReadWriteCreate;
            if (fault == "pool") options.Pooling = true;
            if (fault == "key") options.Password = "";
            if (fault == "path") options.DataSource = database.PathOf("other.db");
            return new(options.ToString());
        }, "4.19.0");
        Assert.Throws<InvalidOperationException>(() => database.CreateContext());
    }

    [Fact]
    public void Retiring_publication_inside_credential_creation_refuses_the_old_result()
    {
        using var database = new TestDatabase();
        var location = database.Services.GetRequiredService<LocalDatabaseLocation<TestDbContext>>(); var path = location.Path;
        location.MoveToProtected(path, () =>
        { location.MoveToProtected(path, () => Connection(path), "4.19.0"); return Connection(path); }, "4.19.0");
        Assert.Throws<InvalidOperationException>(() => database.CreateContext());
    }

    [Fact]
    public void Protected_location_cannot_be_republished_as_unkeyed_even_at_the_same_path()
    {
        using var database = new TestDatabase();
        var location = database.Services.GetRequiredService<LocalDatabaseLocation<TestDbContext>>(); var path = location.Path;
        location.MoveToProtected(path, () => Connection(path), "4.19.0");
        Assert.Throws<InvalidOperationException>(() => location.MoveTo(path)); Assert.Equal(path, location.Path);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Legacy_backup_adapter_refuses_keyed_context_before_consuming_or_writing_data(bool restore)
    {
        using var database = new TestDatabase();
        var location = database.Services.GetRequiredService<LocalDatabaseLocation<TestDbContext>>(); var path = location.Path;
        location.MoveToProtected(path, () => Connection(path), "4.19.0");
        var backup = database.Services.GetRequiredService<IBackupSource>(); using var stream = new MemoryStream([1, 2, 3]);
        if (restore) await Assert.ThrowsAsync<InvalidOperationException>(() => backup.RestoreAsync(stream, Ct));
        else await Assert.ThrowsAsync<InvalidOperationException>(() => backup.WriteAsync(stream, Ct));
        Assert.Equal(0, stream.Position); Assert.Equal(3, stream.Length);
    }
}
