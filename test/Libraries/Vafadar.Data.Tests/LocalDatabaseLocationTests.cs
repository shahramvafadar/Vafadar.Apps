using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Vafadar.Data.Tests;

public sealed class LocalDatabaseLocationTests
{
    [Fact, Trait("AT", "AT-281")]
    public void Captured_publication_refuses_switch_away_and_back_to_the_same_path()
    {
        using var directory = new Vafadar.Testing.TemporaryDirectory();
        var original = directory.Combine("original.db");
        var location = new LocalDatabaseLocation<TestDbContext>(original);
        var validate = location.CaptureValidation(); validate();
        location.MoveTo(directory.Combine("other.db")); location.MoveTo(original);
        Assert.Equal(Path.GetFullPath(original), location.Path);
        Assert.Throws<InvalidOperationException>(validate);
        location.CaptureValidation()();
    }

    [Fact, Trait("AT", "AT-238")]
    public async Task Covered_recovery_rebind_keeps_context_protection_and_never_creates_empty_replacement()
    {
        using var directory = new Vafadar.Testing.TemporaryDirectory();
        using var services = new ServiceCollection().AddLocalDatabase<TestDbContext>(directory.Combine("original.db"), protectionRequired: true).BuildServiceProvider();
        var location = services.GetRequiredService<LocalDatabaseLocation<TestDbContext>>();
        var next = directory.Combine("recovered/data.db");
        using var owner = await location.Access.AcquireExclusiveAsync(TestContext.Current.CancellationToken);
        location.RebindUnpublishedOriginal(next, owner);
        Assert.True(location.RequiresProtection); Assert.False(location.IsProtected); Assert.Equal(Path.GetFullPath(next), location.Path);
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await using var context = await services.GetRequiredService<ILocalDbContextFactory<TestDbContext>>()
                .CreateDbContextAsync(owner, TestContext.Current.CancellationToken);
        });
        Assert.False(File.Exists(next)); Assert.False(Directory.Exists(Path.GetDirectoryName(next)));
    }

    [Fact, Trait("AT", "AT-238")]
    public async Task Recovery_rebind_refuses_live_protected_endpoints_and_other_maintenance_owners()
    {
        using var directory = new Vafadar.Testing.TemporaryDirectory();
        var location = new LocalDatabaseLocation<TestDbContext>(directory.Combine("original.db"), protectionRequired: true);
        var another = new LocalDatabaseLocation<TestDbContext>(directory.Combine("other.db"), protectionRequired: true);
        using var owner = await location.Access.AcquireExclusiveAsync(TestContext.Current.CancellationToken);
        using var foreign = await another.Access.AcquireExclusiveAsync(TestContext.Current.CancellationToken);
        Assert.Throws<InvalidOperationException>(() => location.RebindUnpublishedOriginal(directory.Combine("next.db"), foreign));
        location.MoveToProtected(directory.Combine("cipher.db"), () => throw new IOException("Unexpected native read"), "4.19.0", owner);
        Assert.Throws<InvalidOperationException>(() => location.RebindUnpublishedOriginal(directory.Combine("next.db"), owner));
        Assert.True(location.IsProtected);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Protected_registration_refuses_unpublished_contexts_without_creating_files_or_leaking_leases(bool asynchronous, bool maintenance)
    {
        using var directory = new Vafadar.Testing.TemporaryDirectory();
        var path = directory.Combine("unpublished/data.db");
        using var services = new ServiceCollection().AddLocalDatabase<TestDbContext>(path, protectionRequired: true).BuildServiceProvider();
        var location = services.GetRequiredService<LocalDatabaseLocation<TestDbContext>>();
        var factory = services.GetRequiredService<ILocalDbContextFactory<TestDbContext>>();
        using (var owner = maintenance ? await location.Access.AcquireExclusiveAsync(TestContext.Current.CancellationToken) : null)
        {
            if (asynchronous)
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                {
                    await using var context = owner is null
                        ? await factory.CreateDbContextAsync(TestContext.Current.CancellationToken)
                        : await factory.CreateDbContextAsync(owner, TestContext.Current.CancellationToken);
                });
            else
                Assert.Throws<InvalidOperationException>(() =>
                {
                    using var context = owner is null ? factory.CreateDbContext() : factory.CreateDbContext(owner);
                });
        }
        // Refusal must release its context lease so covered startup can still drain the location.
        using var nextOwner = await location.Access.AcquireExclusiveAsync(TestContext.Current.CancellationToken);
        location.Access.RequireDrainedOwner(nextOwner);
        Assert.False(File.Exists(path));
        Assert.False(Directory.Exists(System.IO.Path.GetDirectoryName(path)));
        Assert.Equal(System.IO.Path.GetFullPath(path), location.Path);
    }

    [Fact]
    public async Task Protected_registration_refuses_plaintext_move_and_rollback_before_first_publication()
    {
        using var directory = new Vafadar.Testing.TemporaryDirectory();
        var location = new LocalDatabaseLocation<TestDbContext>(directory.Combine("original.db"), protectionRequired: true);
        using var owner = await location.Access.AcquireExclusiveAsync(TestContext.Current.CancellationToken);
        var original = location.Path;
        var publication = location.CapturePublication(owner);
        Assert.Throws<InvalidOperationException>(() => location.MoveTo(directory.Combine("other.db"), owner));
        Assert.Throws<InvalidOperationException>(() => publication.Restore(owner));
        Assert.Equal(original, location.Path);
        Assert.False(location.IsProtected);
        Assert.False(File.Exists(original));
    }

    [Fact]
    public async Task Moving_the_database_makes_new_contexts_use_the_other_file_and_back()
    {
        using var database = new TestDatabase();
        var location = database.Services.GetRequiredService<LocalDatabaseLocation<TestDbContext>>();
        var first = location.Path;
        await using (var context = database.CreateContext())
        {
            context.Notes.Add(new Note { Text = "Personal" });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var changed = 0;
        location.Changed += (_, _) => changed++;
        location.MoveTo(database.PathOf("profiles/work.db"));
        await using (var context = database.CreateContext())
        {
            await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            Assert.Empty(await context.Notes.ToListAsync(TestContext.Current.CancellationToken));
        }

        location.MoveTo(first);
        location.MoveTo(first);
        await using (var context = database.CreateContext())
        {
            Assert.Equal("Personal", (await context.Notes.SingleAsync(TestContext.Current.CancellationToken)).Text);
        }

        Assert.True(File.Exists(database.PathOf("profiles/work.db")));
        Assert.Equal(2, changed);
    }
}
