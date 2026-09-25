using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;
using Vafadar.Backup.Storage;
using Vafadar.Core.Hosting;
using Vafadar.Core.Settings;
using Vafadar.Testing;

namespace Vafadar.Data.Tests;

public sealed class SqliteDatabaseBackupSourceTests : IDisposable
{
    private readonly TestDatabase _database = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _database.Dispose();

    [Fact]
    public void The_database_is_registered_as_a_backup_source()
    {
        var source = Assert.Single(_database.Services.GetServices<IBackupSource>());

        Assert.Equal(SqliteDatabaseBackupSource<TestDbContext>.EntryName, source.Name);
    }

    [Fact]
    public async Task Restore_brings_back_the_snapshot()
    {
        await AddNotesAsync("a", "b");
        var source = _database.Services.GetRequiredService<IBackupSource>();

        using var snapshot = new MemoryStream();
        await source.WriteAsync(snapshot, Ct);

        await AddNotesAsync("c");
        await using (var context = _database.CreateContext())
        {
            context.Notes.Remove(await context.Notes.SingleAsync(n => n.Text == "a", Ct));
            await context.SaveChangesAsync(Ct);
        }

        snapshot.Position = 0;
        await source.RestoreAsync(snapshot, Ct);

        Assert.Equal(["a", "b"], await ReadNotesAsync());
    }

    [Fact]
    public async Task Restoring_something_that_is_not_a_database_fails_and_keeps_the_data()
    {
        await AddNotesAsync("keep me");
        var source = _database.Services.GetRequiredService<IBackupSource>();

        using var garbage = new MemoryStream("this is not a SQLite database, but it is long enough to look like one"u8.ToArray());
        var error = await Assert.ThrowsAsync<BackupException>(() => source.RestoreAsync(garbage, Ct));

        Assert.Equal(BackupError.Corrupted, error.Error);
        Assert.Equal(["keep me"], await ReadNotesAsync());
    }

    [Fact]
    public async Task Encrypted_backup_to_a_folder_and_restore_end_to_end()
    {
        using var folder = new TemporaryDirectory();
        var storage = new LocalFolderBackupStorage(folder.Path);
        var service = new BackupService(
            _database.Services.GetServices<IBackupSource>(),
            new StaticAppEnvironment("pro.vafadar.test", "Test", new Version(1, 0)),
            new InMemorySettingsStore(),
            _database.Time,
            new BackupOptions());

        await AddNotesAsync("salary", "rent");
        var file = await service.CreateBackupAsync(storage, "p@ss", Ct);
        await AddNotesAsync("coffee");

        await service.RestoreAsync(storage, file, "p@ss", Ct);

        Assert.Equal(["rent", "salary"], await ReadNotesAsync());
    }

    private async Task AddNotesAsync(params string[] texts)
    {
        await using var context = _database.CreateContext();
        context.Notes.AddRange(texts.Select(text => new Note { Text = text }));
        await context.SaveChangesAsync(Ct);
    }

    private async Task<List<string>> ReadNotesAsync()
    {
        await using var context = _database.CreateContext();
        return await context.Notes.Select(n => n.Text).OrderBy(t => t).ToListAsync(Ct);
    }
}
