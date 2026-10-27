using Vafadar.Zanance.Core.Reminders;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original SQLite-backed publication survives null snooze cache entries without financial writes.</summary>
public sealed class ReminderSnoozeCorruptionFlowTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Null_cache_elements_do_not_abort_original_queue_publication_or_erase_a_valid_snooze(bool withValid)
    {
        await using var fixture = new ProfileReminderFlowTests.Fixture();
        await fixture.SeedAsync();
        fixture.Data.Enable();
        const string key = "reminders.snoozed";
        string? originalCache = null;
        if (withValid)
        {
            await fixture.Service.RefreshAsync();
            var original = Assert.Single(fixture.Native.Pending);
            await fixture.Service.SnoozeAsync(new(original, SnoozeChoice.OneHour));
            originalCache = fixture.Data.Preferences.Get(key);
            Assert.NotNull(originalCache);
        }
        fixture.Data.Preferences.Set(key, withValid ? "[null," + originalCache![1..^1] + ",null]" : "[null]");
        var originalRows = await fixture.RowsAsync();
        var replacements = fixture.Native.Replacements;

        await fixture.Service.RefreshAsync();

        Assert.Equal(replacements + 1, fixture.Native.Replacements);
        Assert.Equal(withValid ? 2 : 1, fixture.Native.Pending.Count);
        Assert.Equal(originalCache, fixture.Data.Preferences.Get(key));
        Assert.Equal(originalRows, await fixture.RowsAsync());
        Assert.Empty(fixture.Native.Shown);
    }
}
