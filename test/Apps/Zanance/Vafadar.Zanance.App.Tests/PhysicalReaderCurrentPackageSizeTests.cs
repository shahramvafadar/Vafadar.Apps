using Vafadar.Zanance.PhysicalReaderGestures;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-579: retain exact public APK length admission while updating the reviewed candidate generation.</summary>
[Trait("AT", "AT-579")]
public sealed class PhysicalReaderCurrentPackageSizeTests
{
    /// <summary>The observed old-length refusal must be corrected without accepting an arbitrary file length.</summary>
    [Fact]
    public void Current_public_size_is_required_before_the_independent_digest_guard()
    {
        Assert.True(PhysicalReaderPolicy.PublicApkLengthMatches(97058719));
        Assert.False(PhysicalReaderPolicy.PublicApkLengthMatches(97075103));
        Assert.False(PhysicalReaderPolicy.PublicApkLengthMatches(0));
        Assert.False(PhysicalReaderPolicy.PublicApkLengthMatches(-1));
        Assert.False(PhysicalReaderPolicy.PublicApkLengthMatches(97058718));
        Assert.False(PhysicalReaderPolicy.PublicApkLengthMatches(97058720));
    }
}
