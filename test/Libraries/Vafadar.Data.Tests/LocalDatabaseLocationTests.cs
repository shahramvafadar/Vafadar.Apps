using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Vafadar.Data.Tests;

public sealed class LocalDatabaseLocationTests
{
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
