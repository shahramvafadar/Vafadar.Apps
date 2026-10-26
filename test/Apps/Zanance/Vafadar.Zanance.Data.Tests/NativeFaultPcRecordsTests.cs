using System.Buffers.Binary;
using Vafadar.Zanance.Performance;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>Exercises the actual bounded signal reader's privacy, provenance and code-interval boundaries.</summary>
public sealed class NativeFaultPcRecordsTests
{
    private const string Token = "000102030405060708090a0b0c0d0e0f";
    private static readonly IReadOnlyDictionary<byte, NativeFaultPcRecords.Module> Modules =
        new Dictionary<byte, NativeFaultPcRecords.Module> { [1] = new("libmonosgen-2.0.so", "abcdef12", [new(100, 20)]) };

    [Fact]
    public void Relative_code_location_retains_original_identity_without_claiming_a_fatal_crash()
    {
        using var stream = new MemoryStream(Packet());
        var result = NativeFaultPcRecords.Read(stream, 42, 10381, Token, 2, Modules);
        Assert.True(result.RequiresIndependentFatalEvidence);
        var location = Assert.Single(result.Locations);
        Assert.Equal(110ul, location.RelativePc);
        Assert.Equal("abcdef12", location.BuildId);
        Assert.Equal(43u, location.ThreadId);
    }

    [Fact]
    public void Concurrent_append_order_is_not_invented_as_signal_allocation_order()
    {
        using var stream = new MemoryStream([.. Packet(2), .. Packet(1)]);
        var result = NativeFaultPcRecords.Read(stream, 42, 10381, Token, 2, Modules);
        Assert.Equal(new uint[] { 1, 2 }, result.Locations.Select(item => item.Sequence));
    }

    [Theory]
    [InlineData(12)] // UID
    [InlineData(16)] // PID
    [InlineData(56)] // Original run token
    [InlineData(9)]  // Architecture
    [InlineData(11)] // Unreviewed module id
    public void Rejects_changed_ownership_or_artifact_binding(int offset)
    {
        var packet = Packet(); packet[offset] ^= 1;
        using var stream = new MemoryStream(packet);
        Assert.Throws<InvalidDataException>(() => NativeFaultPcRecords.Read(stream, 42, 10381, Token, 2, Modules));
    }

    [Theory]
    [InlineData("partial")]
    [InlineData("duplicate")]
    [InlineData("gap")]
    [InlineData("outside-code")]
    [InlineData("invalid-time")]
    [InlineData("overflow")]
    public void Rejects_ambiguous_or_unbounded_original_signal_records(string condition)
    {
        var bytes = condition switch
        {
            "partial" => Packet()[..71], "duplicate" => Packet().Concat(Packet()).ToArray(),
            "gap" => Packet(2), "overflow" => Enumerable.Range(1, 33).SelectMany(i => Packet((uint)i)).ToArray(),
            _ => Packet()
        };
        if (condition == "outside-code") BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(32), 120);
        if (condition == "invalid-time") BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(48), 1_000_000_000);
        using var stream = new MemoryStream(bytes);
        Assert.Throws<InvalidDataException>(() => NativeFaultPcRecords.Read(stream, 42, 10381, Token, 2, Modules));
    }

    [Fact]
    public void Controlled_native_origin_is_refused_without_an_explicit_test_boundary()
    {
        var modules = new Dictionary<byte, NativeFaultPcRecords.Module>
        { [4] = new("libzanance_fault_origin.so", "abcdef12", [new(100, 20)]) };
        var packet = Packet(); packet[11] = 4;
        using var stream = new MemoryStream(packet);
        Assert.Throws<ArgumentException>(() => NativeFaultPcRecords.Read(stream, 42, 10381, Token, 2, modules));
        Assert.Single(NativeFaultPcRecords.Read(stream, 42, 10381, Token, 2, modules, true).Locations);
    }

    [Fact]
    public void Cipher_code_identity_uses_the_actual_shipping_native_module_name()
    {
        var modules = new Dictionary<byte, NativeFaultPcRecords.Module>
        { [3] = new("libsqlcipher.so", "abcdef12", [new(100, 20)]) };
        var packet = Packet(); packet[11] = 3;
        using var stream = new MemoryStream(packet);
        Assert.Equal("libsqlcipher.so", Assert.Single(NativeFaultPcRecords.Read(stream, 42, 10381, Token, 2, modules).Locations).Module);
        modules[3] = modules[3] with { Name = "libe_sqlcipher.so" };
        Assert.Throws<ArgumentException>(() => NativeFaultPcRecords.Read(stream, 42, 10381, Token, 2, modules));
    }

    private static byte[] Packet(uint sequence = 1)
    {
        var packet = new byte[72]; "ZNCPC001"u8.CopyTo(packet);
        packet[8] = 1; packet[9] = 2; packet[10] = 11; packet[11] = 1;
        BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(12), 10381);
        BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(16), 42);
        BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(20), 43);
        BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(24), 2);
        BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(28), sequence);
        BinaryPrimitives.WriteUInt64LittleEndian(packet.AsSpan(32), 110);
        BinaryPrimitives.WriteUInt64LittleEndian(packet.AsSpan(40), 123);
        Convert.FromHexString(Token).CopyTo(packet, 56);
        return packet;
    }
}
