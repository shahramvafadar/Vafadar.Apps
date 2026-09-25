using Microsoft.EntityFrameworkCore;

namespace Vafadar.Data.Tests;

public sealed class LocalDbContextTests : IDisposable
{
    private readonly TestDatabase _database = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Audit_timestamps_are_set_on_insert_and_update()
    {
        var created = _database.Time.GetUtcNow();
        Guid id;
        await using (var context = _database.CreateContext())
        {
            var note = new Note { Text = "first" };
            context.Notes.Add(note);
            await context.SaveChangesAsync(Ct);
            id = note.Id;
        }

        _database.Time.Advance(TimeSpan.FromHours(3));
        await using (var context = _database.CreateContext())
        {
            var note = await context.Notes.SingleAsync(n => n.Id == id, Ct);
            note.Text = "edited";
            await context.SaveChangesAsync(Ct);
        }

        await using (var context = _database.CreateContext())
        {
            var note = await context.Notes.SingleAsync(n => n.Id == id, Ct);
            Assert.Equal(created, note.CreatedAt);
            Assert.Equal(created.AddHours(3), note.UpdatedAt);
        }
    }

    [Fact]
    public async Task DateTimeOffset_values_can_be_filtered_and_sorted_in_SQL()
    {
        var baseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        await using (var context = _database.CreateContext())
        {
            context.Notes.AddRange(
                new Note { Text = "late", RemindAt = baseTime.AddDays(2) },
                new Note { Text = "early, other offset", RemindAt = new DateTimeOffset(2026, 1, 1, 14, 0, 0, TimeSpan.FromHours(3.5)) },
                new Note { Text = "none" });
            await context.SaveChangesAsync(Ct);
        }

        await using (var context = _database.CreateContext())
        {
            var texts = await context.Notes
                .Where(n => n.RemindAt != null && n.RemindAt > baseTime.AddHours(-2))
                .OrderBy(n => n.RemindAt)
                .Select(n => n.Text)
                .ToListAsync(Ct);

            Assert.Equal(["early, other offset", "late"], texts);
        }
    }

    [Fact]
    public async Task DateTimeOffset_values_are_read_back_as_the_same_instant_in_utc()
    {
        var local = new DateTimeOffset(2026, 3, 20, 23, 30, 0, TimeSpan.FromHours(3.5));
        await using (var context = _database.CreateContext())
        {
            context.Notes.Add(new Note { Text = "x", RemindAt = local });
            await context.SaveChangesAsync(Ct);
        }

        await using (var context = _database.CreateContext())
        {
            var stored = (await context.Notes.SingleAsync(Ct)).RemindAt!.Value;
            Assert.Equal(local, stored);
            Assert.Equal(TimeSpan.Zero, stored.Offset);
        }
    }
}
