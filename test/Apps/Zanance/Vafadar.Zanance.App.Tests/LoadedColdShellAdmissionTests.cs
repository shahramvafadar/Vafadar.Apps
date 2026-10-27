using Vafadar.Zanance.App;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-574: separate native command dispatch must retain slow results and refuse foreign or replayed provenance.</summary>
[Trait("AT", "AT-574")]
public sealed class LoadedColdShellAdmissionTests
{
    private static LoadedColdShellFacts Original => new(new('a', 32), new('a', 32), new('b', 32), new('b', 32),
        "reference", "reference", 123, 10381, true, true, 1_000_000_000, 6_000_000_000,
        1_500_000_000, 6_500_000_000, 9_000_000_000, 14_000_000_000,
        "a59cd1c2-bad0-4ba6-94da-25a18df307b3", "a59cd1c2-bad0-4ba6-94da-25a18df307b3", true, true);

    [Fact]
    public void Slow_native_command_to_ready_interval_is_retained_separately_from_OS_launch() =>
        Assert.Equal(8000, LoadedColdShellPolicy.RequireShellToReadyMilliseconds(Original));

    [Fact]
    public void Stale_request_or_run_or_shape_never_matches_a_new_actual_activity()
    {
        Reject(Original with { IntentRequest = new('c', 32) });
        Reject(Original with { IntentRun = new('c', 32) });
        Reject(Original with { IntentShape = "tenfold" });
        Reject(Original with { RequestConsumedOnce = false });
        Reject(Original with { Request = "../foreign" });
    }

    [Fact]
    public void Existing_process_or_shipping_UID_or_changed_boot_is_refused()
    {
        Reject(Original with { ProcessWasAbsent = false });
        Reject(Original with { ActualUid = 10380 });
        Reject(Original with { ActualBoot = "2630a6a0-a395-4a15-ac59-3104d68f0443" });
    }

    [Fact]
    public void Covered_or_unverified_source_is_not_ready()
    {
        Reject(Original with { OriginalHomeReady = false });
        Reject(Original with { ExactProfileAndSource = false });
    }

    [Fact]
    public void Sleep_or_stale_clock_or_reversed_draw_cannot_make_a_plausible_duration()
    {
        Reject(Original with { ReadyBootTime = Original.ReadyBootTime + 1_000_000_000 });
        Reject(Original with { ActivityMonotonic = 70_000_000_000, ActivityBootTime = 75_000_000_000 });
        Reject(Original with { ReadyMonotonic = Original.ActivityMonotonic });
        Reject(Original with { ShellMonotonic = -1 });
    }

    private static void Reject(LoadedColdShellFacts facts) => Assert.Throws<InvalidOperationException>(
        () => LoadedColdShellPolicy.RequireShellToReadyMilliseconds(facts));
}
