using Vafadar.Dastan.Web.Site;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The window that counts how often who sent a request asked in the last hour: it slides with the clock, so
/// that no moment of the clock opens it again – the full hour did, and a test that ran across it failed.
/// </summary>
public sealed class RequestWindowTests
{
    private sealed class Clock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }

    // A minute before a full hour: the moment at which the window of the full hour opened again.
    private readonly Clock _clock = new(new DateTimeOffset(2026, 11, 23, 16, 59, 0, TimeSpan.Zero));

    [Fact]
    public void A_key_is_allowed_as_often_as_the_hour_allows_and_then_refused()
    {
        var window = new RequestWindow(_clock);

        Assert.All(Enumerable.Range(0, 3), _ => Assert.True(window.Allows("try a", 3)));
        Assert.False(window.Allows("try a", 3));

        // Another key and another kind are counted apart.
        Assert.True(window.Allows("try b", 3));
        Assert.True(window.Allows("mail a", 3));
    }

    [Fact]
    public void The_full_hour_does_not_open_the_window()
    {
        var window = new RequestWindow(_clock);
        Assert.All(Enumerable.Range(0, 3), _ => Assert.True(window.Allows("try a", 3)));

        // Two minutes later the clock has passed a full hour; the three requests are two minutes old.
        _clock.Now += TimeSpan.FromMinutes(2);

        Assert.False(window.Allows("try a", 3));
    }

    [Fact]
    public void A_request_is_allowed_again_when_the_oldest_of_the_hour_is_an_hour_old()
    {
        var window = new RequestWindow(_clock);
        Assert.True(window.Allows("try a", 2));
        _clock.Now += TimeSpan.FromMinutes(10);
        Assert.True(window.Allows("try a", 2));
        Assert.False(window.Allows("try a", 2));

        // Just before the first is an hour old nothing changes; then one place is free, and only one.
        _clock.Now += TimeSpan.FromMinutes(50) - TimeSpan.FromSeconds(1);
        Assert.False(window.Allows("try a", 2));
        _clock.Now += TimeSpan.FromSeconds(1);
        Assert.True(window.Allows("try a", 2));
        Assert.False(window.Allows("try a", 2));
    }

    [Fact]
    public void A_request_that_is_refused_is_not_counted()
    {
        var window = new RequestWindow(_clock);
        Assert.True(window.Allows("try a", 1));
        for (var refused = 0; refused < 100; refused++)
        {
            Assert.False(window.Allows("try a", 1));
        }

        // Who kept asking is let in an hour after the one request that was counted.
        _clock.Now += RequestWindow.Length;
        Assert.True(window.Allows("try a", 1));
    }

    [Fact]
    public void Who_asked_is_forgotten_when_their_hour_is_over()
    {
        var window = new RequestWindow(_clock);
        foreach (var sender in Enumerable.Range(0, 50))
        {
            window.Allows($"try {sender}", 5);
        }

        Assert.Equal(50, window.Remembered);

        _clock.Now += RequestWindow.Length + TimeSpan.FromSeconds(1);
        Assert.True(window.Allows("try somebody else", 5));

        Assert.Equal(1, window.Remembered);
    }

    [Fact]
    public void Many_requests_at_the_same_moment_are_counted_each_once()
    {
        var window = new RequestWindow(_clock);

        var allowed = 0;
        Parallel.For(0, 1_000, _ =>
        {
            if (window.Allows("try a", 30))
            {
                Interlocked.Increment(ref allowed);
            }
        });

        Assert.Equal(30, allowed);
    }

    [Fact]
    public void A_window_that_allows_nothing_refuses_everything()
    {
        var window = new RequestWindow(_clock);

        Assert.False(window.Allows("try a", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => window.Allows("try a", -1));
    }
}
