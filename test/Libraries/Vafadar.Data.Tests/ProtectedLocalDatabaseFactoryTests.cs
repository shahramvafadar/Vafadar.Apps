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

    [Fact, Trait("AT", "AT-213")]
    public async Task Session_cannot_resolve_or_retire_a_key_while_owned_contexts_remain()
    {
        using var database = new TestDatabase();
        var location = database.Services.GetRequiredService<LocalDatabaseLocation<TestDbContext>>();
        var resolved = false;
        var session = new SqlCipherProfileSession<TestDbContext>(location, (_, _, _) =>
        { resolved = true; throw new IOException("Unexpected native resolution."); });
        using var owner = await location.Access.AcquireExclusiveAsync(Ct);
        using var context = database.Services.GetRequiredService<ILocalDbContextFactory<TestDbContext>>().CreateDbContext(owner);
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.StartAsync(location.Path, owner, Ct));
        Assert.Throws<InvalidOperationException>(() => session.Close(owner)); Assert.False(resolved);
    }

    [Fact, Trait("AT", "AT-213")]
    public async Task Session_requires_started_state_before_switching_or_accepting_a_decision()
    {
        using var database = new TestDatabase(); var location = database.Services.GetRequiredService<LocalDatabaseLocation<TestDbContext>>();
        var session = new SqlCipherProfileSession<TestDbContext>(location, (_, _, _) => throw new IOException("Unexpected native resolution."));
        using var owner = await location.Access.AcquireExclusiveAsync(Ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.BeginSwitchAsync(location.Path, owner, Ct));
        Assert.Throws<InvalidOperationException>(() => session.FinishSwitch(true, owner));
        session.Close(owner);
    }

    [Fact, Trait("AT", "AT-212")]
    public async Task Publication_rollback_retains_key_creator_and_cannot_be_reused()
    {
        using var database = new TestDatabase();
        var location = database.Services.GetRequiredService<LocalDatabaseLocation<TestDbContext>>(); var original = location.Path;
        var keyCalls = 0; location.MoveToProtected(original, () => { keyCalls++; return Connection(original); }, "4.19.0");
        using var owner = await location.Access.AcquireExclusiveAsync(Ct);
        var publication = location.CapturePublication(owner);
        var destination = database.PathOf("other.db"); location.MoveToProtected(destination, () => Connection(destination), "4.19.0", owner);
        publication.Restore(owner);
        using var context = database.Services.GetRequiredService<ILocalDbContextFactory<TestDbContext>>().CreateDbContext(owner);
        Assert.Equal(original, location.Path); Assert.Equal(1, keyCalls);
        Assert.Throws<InvalidOperationException>(() => publication.Restore(owner));
        Assert.Throws<InvalidOperationException>(() => location.MoveTo(original, owner));
    }

    [Fact, Trait("AT", "AT-212")]
    public async Task Publication_rollback_refuses_plaintext_fallback_and_foreign_owner()
    {
        using var database = new TestDatabase(); var location = database.Services.GetRequiredService<LocalDatabaseLocation<TestDbContext>>();
        using var owner = await location.Access.AcquireExclusiveAsync(Ct); var publication = location.CapturePublication(owner);
        var path = location.Path; location.MoveToProtected(path, () => Connection(path), "4.19.0", owner);
        using var other = await new DatabaseAccessGate().AcquireExclusiveAsync(Ct);
        Assert.Throws<InvalidOperationException>(() => publication.Restore(other));
        Assert.Throws<InvalidOperationException>(() => publication.Restore(owner));
        Assert.Throws<InvalidOperationException>(() => location.MoveTo(path, owner));
    }

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

    [Fact]
    public async Task Protected_backup_rejects_plaintext_source_before_output()
    {
        using var database = new TestDatabase();
        var location = database.Services.GetRequiredService<LocalDatabaseLocation<TestDbContext>>(); var path = location.Path;
        location.MoveToProtected(path, () => Connection(path), "4.19.0");
        using var output = new MemoryStream();
        await Assert.ThrowsAsync<InvalidDataException>(() => database.Services.GetRequiredService<IBackupSource>().WriteAsync(output, Ct));
        Assert.Equal(0, output.Length);
    }

    [Fact]
    public async Task Legacy_restore_adapter_refuses_keyed_context_before_consuming_data()
    {
        using var database = new TestDatabase();
        var location = database.Services.GetRequiredService<LocalDatabaseLocation<TestDbContext>>(); var path = location.Path;
        location.MoveToProtected(path, () => Connection(path), "4.19.0");
        var backup = database.Services.GetRequiredService<IBackupSource>(); using var stream = new MemoryStream([1, 2, 3]);
        await Assert.ThrowsAsync<InvalidOperationException>(() => backup.RestoreAsync(stream, Ct));
        Assert.Equal(0, stream.Position); Assert.Equal(3, stream.Length);
    }
}
