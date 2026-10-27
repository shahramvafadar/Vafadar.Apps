using Vafadar.Zanance.PhysicalReaderGestures;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-585: a same-length Main upgrade must require fresh candidate admission.</summary>
[Trait("AT", "AT-585")]
public sealed class PhysicalReaderCurrentCandidateTests
{
    /// <summary>The old category reader candidate cannot authorize the current corrected product.</summary>
    [Fact]
    public void Corrected_product_is_admitted_and_previous_same_length_candidate_is_refused()
    {
        var device = new PhysicalReaderDevice("samsung", "SM-A176B", 36, "arm64-v8a", 450);
        var install = new ReaderOwnerInstall(10380, false, "/data/app/fictitious/pro.vafadar.zanance/base.apk", 100);

        Assert.True(PhysicalReaderPolicy.Admits(device, install,
            "f7488d1da0bbed5b9910a7ace960ef5f883da008f31dd24e173e9421996ae9c7"));
        Assert.False(PhysicalReaderPolicy.Admits(device, install,
            "510a3197cb6d77bf69df03d10f3d4079286726a82fecce557f8a4bebeef3b879"));
    }
}
