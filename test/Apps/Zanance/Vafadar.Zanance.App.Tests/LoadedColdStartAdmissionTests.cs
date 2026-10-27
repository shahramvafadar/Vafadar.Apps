using Vafadar.Zanance.App;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-574: loaded-cold evidence must not turn a quick covered frame or another native lifetime into readiness.</summary>
[Trait("AT", "AT-574")]
public sealed class LoadedColdStartAdmissionTests
{
    private static LoadedColdStartFacts Original => new(123, 123, 10381, 10381, true, true, true,
        1_000_000_000, 1_400_000_000, 4_000_000_000, 4_020_000_000, true, true, true,
        "a59cd1c2-bad0-4ba6-94da-25a18df307b3", "a59cd1c2-bad0-4ba6-94da-25a18df307b3", true, true);

    [Fact]
    public void Slow_loaded_cold_result_keeps_the_full_OS_launch_to_ready_interval() =>
        Assert.Equal(3020d, LoadedColdStartPolicy.RequireLoadedColdMilliseconds(Original));

    [Fact]
    public void A_native_first_frame_cannot_replace_the_still_covered_Home() =>
        Assert.Throws<InvalidOperationException>(() => LoadedColdStartPolicy.RequireLoadedColdMilliseconds(
            Original with { OriginalHomeUncovered = false, OriginalPublicationCompleted = false,
                ReadyDrawNanoseconds = Original.InitialFrameNanoseconds, ReadyFrameNanoseconds = 1_420_000_000 }));

    [Fact]
    public void A_warm_activity_and_a_new_service_process_are_both_outside_the_cold_activity_boundary()
    {
        Assert.Throws<InvalidOperationException>(() => LoadedColdStartPolicy.RequireLoadedColdMilliseconds(
            Original with { ProcessWasAbsent = false, NativeColdActivity = false }));
        Assert.Throws<InvalidOperationException>(() => LoadedColdStartPolicy.RequireLoadedColdMilliseconds(
            Original with { NativeColdActivity = false }));
    }

    [Fact]
    public void A_late_native_callback_cannot_inherit_another_process_or_boot()
    {
        Assert.Throws<InvalidOperationException>(() => LoadedColdStartPolicy.RequireLoadedColdMilliseconds(
            Original with { ActualPid = 124 }));
        Assert.Throws<InvalidOperationException>(() => LoadedColdStartPolicy.RequireLoadedColdMilliseconds(
            Original with { AfterBoot = "2630a6a0-a395-4a15-ac59-3104d68f0443" }));
    }

    [Fact]
    public void Fast_text_in_a_foreign_or_source_unverified_profile_is_not_loaded_acceptance()
    {
        Assert.Throws<InvalidOperationException>(() => LoadedColdStartPolicy.RequireLoadedColdMilliseconds(
            Original with { ExactOwnedProfile = false }));
        Assert.Throws<InvalidOperationException>(() => LoadedColdStartPolicy.RequireLoadedColdMilliseconds(
            Original with { OriginalSourceMatched = false }));
        Assert.Throws<InvalidOperationException>(() => LoadedColdStartPolicy.RequireLoadedColdMilliseconds(
            Original with { ExpectedUid = 10380, ActualUid = 10380 }));
    }

    [Fact]
    public void A_clock_domain_or_missing_draw_cannot_produce_a_plausible_duration()
    {
        Assert.Throws<InvalidOperationException>(() => LoadedColdStartPolicy.RequireLoadedColdMilliseconds(
            Original with { InitialFrameNanoseconds = 999_999_999 }));
        Assert.Throws<InvalidOperationException>(() => LoadedColdStartPolicy.RequireLoadedColdMilliseconds(
            Original with { ReadyFrameNanoseconds = Original.ReadyDrawNanoseconds }));
    }
}
