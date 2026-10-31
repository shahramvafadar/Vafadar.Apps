using System.Text;
using System.Text.Json;
using Vafadar.Zanance.Performance;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>Exercises mixed native frames without exporting rejected names, paths or non-code payloads.</summary>
public sealed class NativeExitModuleSubsetTests
{
    [Theory]
    [InlineData("[vdso]")]
    [InlineData("/data/user/0/owned/PRIVATE-TRACE-MARKER.oat")]
    public void Mixed_frames_keep_only_library_code_and_count_discarded_locations(string rejectedName)
    {
        using var stream = new MemoryStream(Packet(Frame(rejectedName), Frame("/data/app/owned/lib/libmonosgen-2.0.so")));
        var result = NativeExitFrames.Read(stream, 42, 10381);
        var frame = Assert.Single(result.Frames);
        Assert.Equal("libmonosgen-2.0.so", frame.Module);
        Assert.Equal(123ul, frame.RelativePc);
        Assert.Equal("abcdef", frame.BuildId);
        var encoded = JsonSerializer.Serialize(result);
        Assert.DoesNotContain("PRIVATE", encoded, StringComparison.Ordinal);
        Assert.DoesNotContain("vdso", encoded, StringComparison.Ordinal);
        Assert.DoesNotContain("/data/", encoded, StringComparison.Ordinal);
        using var json = JsonDocument.Parse(encoded);
        Assert.Equal(1, json.RootElement.GetProperty("DiscardedNonModuleFrames").GetInt32());
        Assert.Equal(1, json.RootElement.GetProperty("Frames")[0].GetProperty("OriginalFrameIndex").GetInt32());
    }

    [Fact]
    public void Discarded_locations_still_consume_the_original_frame_bound()
    {
        var frames = Enumerable.Repeat(Frame("[vdso]"), 129)
            .Append(Frame("libmonosgen-2.0.so")).ToArray();
        using var stream = new MemoryStream(Packet(frames));
        var error = Assert.Throws<InvalidDataException>(() => NativeExitFrames.Read(stream, 42, 10381));
        Assert.Equal("Native frame bound exceeded.", error.Message);
    }

    [Fact]
    public void No_allowed_library_location_remains_a_refusal()
    {
        using var stream = new MemoryStream(Packet(Frame("[vdso]")));
        Assert.Throws<InvalidDataException>(() => NativeExitFrames.Read(stream, 42, 10381));
    }

    [Fact]
    public void Allowed_library_with_invalid_build_identity_remains_a_refusal()
    {
        using var stream = new MemoryStream(Packet(Frame("[vdso]"), Frame("libmonosgen-2.0.so", "PRIVATE-NOT-A-BUILD-ID")));
        Assert.Throws<InvalidDataException>(() => NativeExitFrames.Read(stream, 42, 10381));
    }

    private static byte[] Frame(string module, string buildId = "abcdef") =>
        Bytes(4, Join(Number(1, 123), Number(2, 987654), Number(3, 987654),
            Text(4, "PRIVATE-TRACE-MARKER"), Text(6, module), Text(8, buildId)));

    private static byte[] Packet(params byte[][] frames) =>
        Join(Number(5, 42), Number(6, 77), Number(7, 10381),
            Bytes(16, Join(Number(1, 77), Bytes(2, Join(frames)))));

    private static byte[] Number(int field, ulong value) => Join(Varint((ulong)(field * 8)), Varint(value));
    private static byte[] Text(int field, string value) => Bytes(field, Encoding.UTF8.GetBytes(value));
    private static byte[] Bytes(int field, byte[] value) => Join(Varint((ulong)(field * 8 + 2)), Varint((ulong)value.Length), value);
    private static byte[] Join(params byte[][] values) => values.SelectMany(value => value).ToArray();

    private static byte[] Varint(ulong value)
    {
        var bytes = new List<byte>();
        do { var next = (byte)(value & 127); value >>= 7; bytes.Add((byte)(next | (value > 0 ? 128 : 0))); } while (value > 0);
        return bytes.ToArray();
    }
}
