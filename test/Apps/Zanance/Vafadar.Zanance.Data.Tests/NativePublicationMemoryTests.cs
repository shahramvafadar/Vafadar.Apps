using System.Text.Json;
using Vafadar.Zanance.Performance;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>Diagnostic counters are scoped, complete and never double-count Android's existing proportional swap.</summary>
public sealed class NativePublicationMemoryTests
{
    [Fact]
    public void Original_total_pss_is_not_increased_again_by_reported_private_swap()
    {
        var actual = NativePublicationMemory.Parse("VmRSS:\t150 kB\nVmSwap:\t80 kB\n", 200);
        Assert.Equal(200, actual.PssIncludingSwapKiB); Assert.Equal(150, actual.ResidentRssKiB); Assert.Equal(80, actual.PrivateVmSwapKiB);
    }

    [Fact]
    public void Unknown_status_fields_and_fictitious_source_values_are_never_copied_to_the_packet()
    {
        const string marker = "DO_NOT_RETAIN_FICTITIOUS_SOURCE";
        var actual = NativePublicationMemory.Parse("Name:\t" + marker + "\r\nVmRSS:\t123 kB\r\nVmSwap:\t0 kB\r\nPath:\t/private/fictitious\r\n", 321);
        var json = JsonSerializer.Serialize(actual); Assert.DoesNotContain(marker, json, StringComparison.Ordinal);
        Assert.DoesNotContain("/private/", json, StringComparison.Ordinal); Assert.Equal(0, actual.PrivateVmSwapKiB);
    }

    [Theory]
    [InlineData("VmRSS: 1 kB\n", 1)]
    [InlineData("VmSwap: 0 kB\n", 1)]
    [InlineData("VmRSS: 1 kB\nVmRSS: 2 kB\nVmSwap: 0 kB", 1)]
    [InlineData("VmRSS: 1 kB\nVmSwap: 0 kB\nVmSwap: 1 kB", 1)]
    [InlineData("VmRSS: -1 kB\nVmSwap: 0 kB", 1)]
    [InlineData("VmRSS: 0 kB\nVmSwap: 0 kB", 1)]
    [InlineData("VmRSS: 1 KB\nVmSwap: 0 kB", 1)]
    [InlineData("VmRSS: 9223372036854775808 kB\nVmSwap: 0 kB", 1)]
    [InlineData("VmRSS: 1 kB extra\nVmSwap: 0 kB", 1)]
    [InlineData("VmRSS: 1 kB\nVmSwap: 0 kB", 0)]
    public void Missing_duplicated_malformed_or_unowned_counter_shapes_fail_closed(string status, long pss) =>
        Assert.Throws<InvalidDataException>(() => NativePublicationMemory.Parse(status, pss));
}
