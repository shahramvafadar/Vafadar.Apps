using Vafadar.Zanance.App;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-576: the experimental source port cannot read Main, another caller or a not-yet-prepared process.</summary>
[Trait("AT", "AT-576")]
public sealed class CoreClrRuntimeReadPolicyTests
{
    private static readonly string Run = new('a', 32);

    /// <summary>Refuses ordinary Main, the retained caller and privileged/self callers before source access.</summary>
    [Fact]
    public void Only_the_actual_separate_coreclr_UID_and_authorized_shell_can_request_source()
    {
        Assert.True(Owned(CoreClrRuntimeReadPolicy.Package, 10399, 2000));
        Assert.False(Owned("pro.vafadar.zanance", 10380, 2000));
        Assert.False(Owned("pro.vafadar.zanance.androidhomereview", 10381, 2000));
        Assert.False(Owned(CoreClrRuntimeReadPolicy.Package, 10399, 0));
        Assert.False(Owned(CoreClrRuntimeReadPolicy.Package, 10399, 10399));
    }

    /// <summary>Refuses unbounded selectors and a runtime that does not match the admitted experiment.</summary>
    [Fact]
    public void Arbitrary_SQL_paths_unknown_stages_and_another_runtime_cannot_enter_the_port()
    {
        Assert.False(CoreClrRuntimeReadPolicy.IsRequest("../foreign", "seal"));
        Assert.False(CoreClrRuntimeReadPolicy.IsRequest(Run, "../ledger"));
        Assert.False(CoreClrRuntimeReadPolicy.IsRequest(Run, "save"));
        Assert.False(CoreClrRuntimeReadPolicy.IsOwnedShellRequest(CoreClrRuntimeReadPolicy.Package, 10399, 2000,
            36, "SM-A176B", false, Run, "seal"));
    }

    /// <summary>Each unavailable original readiness condition leaves the source callback completely uncalled.</summary>
    [Theory]
    [InlineData(false, true, true, true)]
    [InlineData(true, false, true, true)]
    [InlineData(true, true, false, true)]
    [InlineData(true, true, true, false)]
    public async Task A_cold_covered_locked_or_foreign_profile_request_performs_no_source_read(
        bool ready, bool focused, bool unlocked, bool ownMain)
    {
        var reads = 0;
        await Assert.ThrowsAsync<InvalidOperationException>(() => CoreClrRuntimeReadPolicy.ReadOnlyWhenReadyAsync(
            ready, focused, unlocked, ownMain, () => { reads++; return Task.FromResult(42); }));
        Assert.Equal(0, reads);
    }

    private static bool Owned(string package, int uid, int caller) => CoreClrRuntimeReadPolicy.IsOwnedShellRequest(
        package, uid, caller, 36, "SM-A176B", true, Run, "seal");
}
