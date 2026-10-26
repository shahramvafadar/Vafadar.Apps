using System.Text;
using Vafadar.Zanance.Performance;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>Exercises the actual streaming diagnostic reader with synthetic AOSP-shaped protobufs only.</summary>
public sealed class NativeExitFramesTests
{
    [Fact]
    public void Returns_only_the_requested_crashed_thread_relative_module_location()
    {
        using var source = new MemoryStream(Packet());
        var result = NativeExitFrames.Read(source, 42, 10381);
        var frame = Assert.Single(result.Frames);
        Assert.Equal(77u, result.ThreadId);
        Assert.Equal(123ul, frame.RelativePc);
        Assert.Equal("libmonosgen-2.0.so", frame.Module);
        Assert.Equal("abcdef", frame.BuildId);
    }

    [Fact]
    public void Skips_unselected_thread_private_fields_and_unknown_root_payloads()
    {
        var unselected = Bytes(16, Join(Number(1, 99), Bytes(2, Text(5, "PRIVATE-TRACE-MARKER"))));
        using var source = new MemoryStream(Join(Packet(), unselected, Text(14, "PRIVATE-TRACE-MARKER"), Bytes(18, new byte[8192])));
        var result = NativeExitFrames.Read(source, 42, 10381);
        Assert.Single(result.Frames);
        Assert.DoesNotContain("PRIVATE", System.Text.Json.JsonSerializer.Serialize(result), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(41u, 10381u)]
    [InlineData(42u, 10380u)]
    public void Refuses_a_different_process_or_uid(uint pid, uint uid)
    {
        using var source = new MemoryStream(Packet());
        Assert.Throws<InvalidDataException>(() => NativeExitFrames.Read(source, pid, uid));
    }

    [Fact]
    public void Refuses_duplicate_identity_instead_of_accepting_a_later_override()
    {
        using var source = new MemoryStream(Join(Packet(), Number(5, 42)));
        Assert.Throws<InvalidDataException>(() => NativeExitFrames.Read(source, 42, 10381));
    }

    [Fact]
    public void Refuses_truncated_length_delimited_payload()
    {
        using var source = new MemoryStream(Join(Packet(), Varint(18 * 8 + 2), Varint(5), new byte[2]));
        Assert.Throws<EndOfStreamException>(() => NativeExitFrames.Read(source, 42, 10381));
    }

    [Fact]
    public void Refuses_excessive_trace_length_before_reading_the_payload()
    {
        using var source = new MemoryStream(Join(Packet(), Varint(18 * 8 + 2), Varint(9 * 1024 * 1024)));
        Assert.Throws<InvalidDataException>(() => NativeExitFrames.Read(source, 42, 10381));
    }

    [Fact]
    public void Refuses_invalid_wire_and_overflowed_integer()
    {
        using var wire = new MemoryStream([15]);
        Assert.Throws<InvalidDataException>(() => NativeExitFrames.Read(wire, 42, 10381));
        using var integer = new MemoryStream(Enumerable.Repeat((byte)255, 11).ToArray());
        Assert.Throws<InvalidDataException>(() => NativeExitFrames.Read(integer, 42, 10381));
    }

    [Fact]
    public void Refuses_unbounded_or_non_module_code_identity()
    {
        using var longName = new MemoryStream(Packet(new string('a', 1025)));
        Assert.Throws<InvalidDataException>(() => NativeExitFrames.Read(longName, 42, 10381));
        using var invalidName = new MemoryStream(Packet("private-not-a-library"));
        Assert.Throws<InvalidDataException>(() => NativeExitFrames.Read(invalidName, 42, 10381));
    }

    private static byte[] Packet(string module = "/data/app/owned/lib/arm64/libmonosgen-2.0.so")
    {
        var frame = Join(Number(1, 123), Number(2, 987654), Number(3, 987654),
            Text(4, "PRIVATE-TRACE-MARKER"), Text(6, module), Text(8, "abcdef"));
        var thread = Join(Text(2, "SGen worker"), Bytes(3, Text(1, "PRIVATE-TRACE-MARKER")),
            Bytes(5, new byte[100]), Bytes(4, frame));
        return Join(Number(5, 42), Number(6, 77), Number(7, 10381),
            Bytes(16, Join(Number(1, 77), Bytes(2, thread))));
    }

    private static byte[] Number(int field, ulong value) => Join(Varint((ulong)(field * 8)), Varint(value));
    private static byte[] Text(int field, string value) => Bytes(field, Encoding.UTF8.GetBytes(value));
    private static byte[] Bytes(int field, byte[] value) => Join(Varint((ulong)(field * 8 + 2)), Varint((ulong)value.Length), value);
    private static byte[] Join(params byte[][] values) => values.SelectMany(v => v).ToArray();

    private static byte[] Varint(ulong value)
    {
        var bytes = new List<byte>();
        do { var next = (byte)(value & 127); value >>= 7; bytes.Add((byte)(next | (value > 0 ? 128 : 0))); } while (value > 0);
        return bytes.ToArray();
    }
}
