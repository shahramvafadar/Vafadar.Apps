using Vafadar.Zanance.PhysicalReaderGestures;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-572: actual independent reader policy refuses another device, install or retired observation lifetime.</summary>
[Trait("AT", "AT-572")]
public sealed class PhysicalReaderAdmissionTests
{
    private static readonly PhysicalReaderDevice Device = new("samsung", "SM-A176B", 36, "arm64-v8a", 450);
    private static readonly ReaderOwnerInstall Install = new(10380, false, "/data/app/fictitious/pro.vafadar.zanance/base.apk", 100);

    [Fact]
    public void Only_the_reviewed_public_candidate_is_admitted() =>
        Assert.True(PhysicalReaderPolicy.Admits(Device, Install, PhysicalReaderPolicy.OwnerApkSha256));

    [Theory]
    [InlineData("manufacturer")]
    [InlineData("model")]
    [InlineData("api")]
    [InlineData("abi")]
    [InlineData("density")]
    [InlineData("uid")]
    [InlineData("debug")]
    [InlineData("private-path")]
    [InlineData("unknown-update")]
    [InlineData("apk")]
    public void Another_public_target_is_refused_before_any_reader_input(string change)
    {
        var device = change switch
        {
            "manufacturer" => Device with { Manufacturer = "different" },
            "model" => Device with { Model = "sdk_gphone64_x86_64" },
            "api" => Device with { Api = 35 },
            "abi" => Device with { Abi = "x86_64" },
            "density" => Device with { DensityDpi = 160 },
            _ => Device,
        };
        var install = change switch
        {
            "uid" => Install with { Uid = 10381 },
            "debug" => Install with { Debuggable = true },
            "private-path" => Install with { SourceApk = "/data/user/0/pro.vafadar.zanance/files/private" },
            "unknown-update" => Install with { UpdatedAt = 0 },
            _ => Install,
        };
        Assert.False(PhysicalReaderPolicy.Admits(device, install,
            change == "apk" ? new string('0', 64) : PhysicalReaderPolicy.OwnerApkSha256));
    }

    [Theory]
    [InlineData("update")]
    [InlineData("source")]
    [InlineData("uid")]
    [InlineData("debug")]
    public void Replacing_the_installed_generation_invalidates_a_prior_hash(string change)
    {
        var replacement = change switch
        {
            "update" => Install with { UpdatedAt = 101 },
            "source" => Install with { SourceApk = "/data/app/replaced/pro.vafadar.zanance/base.apk" },
            "uid" => Install with { Uid = 10381 },
            _ => Install with { Debuggable = true },
        };
        Assert.False(PhysicalReaderPolicy.SameInstall(Install, replacement));
    }

    [Theory]
    [InlineData(-1, 0, true)]
    [InlineData(300000, 0, true)]
    [InlineData(1, 97, true)]
    [InlineData(1, -1, true)]
    [InlineData(1, 0, false)]
    public void Expired_or_disconnected_observations_are_refused_too(long elapsed, int commands, bool connected) =>
        Assert.False(PhysicalReaderPolicy.SessionOpen(1000 + elapsed, 1000, commands, connected));

    [Fact]
    public void The_last_bounded_command_has_no_unlimited_observation_tail()
    {
        Assert.True(PhysicalReaderPolicy.SessionOpen(299999, 0, 96, true));
        Assert.False(PhysicalReaderPolicy.SessionOpen(300000, 0, 96, true));
    }

    [Theory]
    [InlineData(1, 2, true)]
    [InlineData(1, 1, false)]
    public void A_late_callback_cannot_reactivate_a_retired_binding(int requested, int actual, bool connected) =>
        Assert.False(PhysicalReaderPolicy.CurrentCallback(requested, actual, connected));

    [Fact]
    public void Fractional_physical_density_allows_only_the_recorded_subpixel_rounding()
    {
        Assert.Equal(123, PhysicalReaderPolicy.MinimumTargetPixels(450f / 160));
        Assert.Equal(44, PhysicalReaderPolicy.MinimumTargetPixels(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PhysicalReaderPolicy.MinimumTargetPixels(float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => PhysicalReaderPolicy.MinimumTargetPixels(0));
    }
}
