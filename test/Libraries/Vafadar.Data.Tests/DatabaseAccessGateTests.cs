using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Vafadar.Data.Tests;

/// <summary>Verifies actual factory context lifetimes and maintenance ownership, not a replacement database algorithm.</summary>
public sealed class DatabaseAccessGateTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact, Trait("AT", "AT-202")]
    public async Task Exclusive_work_drains_all_actual_contexts_and_blocks_new_factory_work()
    {
        using var db = new TestDatabase(); var location = Location(db); var factory = Factory(db);
        using var first = factory.CreateDbContext(); var second = await factory.CreateDbContextAsync(Ct);
        var pending = location.Access.AcquireExclusiveAsync(Ct); Assert.False(pending.IsCompleted);
        Assert.Throws<InvalidOperationException>(() => factory.CreateDbContext());
        var reader = factory.CreateDbContextAsync(Ct); Assert.False(reader.IsCompleted);
        first.Dispose(); Assert.False(pending.IsCompleted); await second.DisposeAsync();
        using (var exclusive = await pending)
        {
            Assert.False(reader.IsCompleted);
            await using var owned = await factory.CreateDbContextAsync(exclusive, Ct);
            Assert.Empty(await owned.Notes.ToListAsync(Ct));
            Assert.Throws<InvalidOperationException>(() => location.MoveTo(location.Path));
            location.MoveTo(location.Path, exclusive);
        }
        await using var resumed = await reader; Assert.Empty(await resumed.Notes.ToListAsync(Ct));
    }

    [Fact, Trait("AT", "AT-202")]
    public async Task Closing_owner_keeps_other_work_excluded_until_owned_context_is_disposed()
    {
        using var db = new TestDatabase(); var location = Location(db); var factory = Factory(db);
        var owner = await location.Access.AcquireExclusiveAsync(Ct);
        var owned = await factory.CreateDbContextAsync(owner, Ct); await owned.Database.OpenConnectionAsync(Ct);
        var reader = factory.CreateDbContextAsync(Ct); owner.Dispose(); Assert.False(reader.IsCompleted);
        Assert.Throws<InvalidOperationException>(owner.Validate);
        await Assert.ThrowsAsync<InvalidOperationException>(() => owned.Notes.ToListAsync(Ct));
        await owned.DisposeAsync(); owner.Dispose();
        await using var resumed = await reader; Assert.Empty(await resumed.Notes.ToListAsync(Ct));
    }

    [Fact, Trait("AT", "AT-202")]
    public async Task Cancelled_pending_writer_reopens_reader_admission_without_leaking_ownership()
    {
        using var db = new TestDatabase(); var factory = Factory(db); using var first = factory.CreateDbContext();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        var pending = Location(db).Access.AcquireExclusiveAsync(cancellation.Token);
        Assert.False(pending.IsCompleted); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        using var second = factory.CreateDbContext(); first.Dispose(); second.Dispose();
        using var next = await Location(db).Access.AcquireExclusiveAsync(Ct); next.Validate();
    }

    [Fact, Trait("AT", "AT-202")]
    public async Task Cancelled_reader_does_not_prevent_next_exclusive_owner()
    {
        using var db = new TestDatabase(); var location = Location(db);
        var owner = await location.Access.AcquireExclusiveAsync(Ct);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        var reader = Factory(db).CreateDbContextAsync(cancellation.Token); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reader); owner.Dispose();
        using var next = await location.Access.AcquireExclusiveAsync(Ct); next.Validate();
    }

    [Fact, Trait("AT", "AT-202")]
    public async Task Foreign_or_retired_owner_cannot_construct_context_or_publish_path()
    {
        using var db = new TestDatabase(); var foreignGate = new DatabaseAccessGate();
        using var foreign = await foreignGate.AcquireExclusiveAsync(Ct);
        Assert.Throws<InvalidOperationException>(() => Factory(db).CreateDbContext(foreign));
        Assert.Throws<InvalidOperationException>(() => Location(db).MoveTo(Location(db).Path, foreign));
        var owner = await Location(db).Access.AcquireExclusiveAsync(Ct); owner.Dispose();
        Assert.Throws<InvalidOperationException>(() => Factory(db).CreateDbContext(owner));
        using var context = Factory(db).CreateDbContext();
    }

    [Fact, Trait("AT", "AT-202")]
    public async Task Failed_factory_creation_releases_ordinary_and_owned_context_leases()
    {
        using var db = new TestDatabase(); var location = Location(db); var path = location.Path;
        location.MoveToProtected(path, () => new Microsoft.Data.Sqlite.SqliteConnection("Data Source=" + path), "4.19.0");
        Assert.Throws<InvalidOperationException>(() => Factory(db).CreateDbContext());
        using var exclusive = await location.Access.AcquireExclusiveAsync(Ct);
        Assert.Throws<InvalidOperationException>(() => Factory(db).CreateDbContext(exclusive));
        exclusive.Validate();
    }

    private static LocalDatabaseLocation<TestDbContext> Location(TestDatabase db) => db.Services.GetRequiredService<LocalDatabaseLocation<TestDbContext>>();
    private static ILocalDbContextFactory<TestDbContext> Factory(TestDatabase db) => db.Services.GetRequiredService<ILocalDbContextFactory<TestDbContext>>();
}
