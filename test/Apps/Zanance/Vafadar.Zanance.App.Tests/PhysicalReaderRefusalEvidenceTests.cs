using Vafadar.Zanance.PhysicalReaderGestures;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-585: rejected native observations retain correlation without exporting arbitrary payloads.</summary>
[Trait("AT", "AT-585")]
public sealed class PhysicalReaderRefusalEvidenceTests
{
    /// <summary>A refused original-window snapshot can be distinguished from an unrelated asynchronous event.</summary>
    [Fact]
    public void Original_window_refusal_retains_only_the_canonical_request_and_fixed_reason()
    {
        const string request = "0123456789abcdef0123456789abcdef";
        Assert.Equal(request, PhysicalReaderPolicy.RefusalRequest(request));
        Assert.Equal("owner-window-not-focused",
            PhysicalReaderPolicy.RefusalReason("Original application window is not focused."));
        Assert.Equal("bounded-owner-tree",
            PhysicalReaderPolicy.RefusalReason("Original bounded tree required."));
    }

    /// <summary>Untrusted exception or request text cannot become a public reader diagnostic.</summary>
    [Fact]
    public void Unknown_native_message_and_noncanonical_request_are_redacted()
    {
        Assert.Null(PhysicalReaderPolicy.RefusalRequest("fictitious private path or caption"));
        Assert.Null(PhysicalReaderPolicy.RefusalRequest("CC12C1DF54BD413EB3EC4CAF8A3994E90"));
        Assert.Equal("unclassified-native-refusal",
            PhysicalReaderPolicy.RefusalReason("fictitious private path or caption"));
        Assert.Equal("unclassified-native-refusal", PhysicalReaderPolicy.RefusalReason(null));
    }
}
