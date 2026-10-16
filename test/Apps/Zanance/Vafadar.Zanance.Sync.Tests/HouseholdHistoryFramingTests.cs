using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data.Sharing;

namespace Vafadar.Zanance.Sync.Tests;

/// <summary>Original stream positions must still identify complete rows when either a binary prefix or a JSON body crosses encrypted block boundaries.</summary>
[Trait("AT", "AT-294")]
public sealed class HouseholdHistoryFramingTests
{
    /// <summary>The archive indexes exact body positions, including a following prefix split across blocks.</summary>
    [Theory]
    [InlineData(-5)]
    [InlineData(-2)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(9)]
    public void Original_row_positions_remain_exact_across_prefix_and_large_body_boundaries(int delta)
    {
        var key = new SyncRecordKey("Accounts", "00000000-0000-0000-0000-000000000001");
        SyncRecord Row(long version, string name) => new(key, version, JsonSerializer.SerializeToElement(new { Name = name }));
        var emptyLength = JsonSerializer.SerializeToUtf8Bytes(Row(1, ""), PersonalSyncJsonContext.Default.SyncRecord).Length;
        var records = new[] { Row(1, new('a', HouseholdFinancialFraming.BlockBytes + delta - emptyLength)),
            Row(2, "Fictitious following row"), Row(3, new('b', HouseholdFinancialFraming.BlockBytes * 2 + 1)) };
        var blocks = new List<HouseholdRecordBlock>();
        HouseholdRecordStream.Write(records, block => blocks.Add(block with { Bytes = block.Bytes.ToArray() }));
        var original = blocks.SelectMany(block => block.Bytes).ToArray(); var seen = 0; long end = 0;
        HouseholdRecordStreamReader reader = null!;
        using var owned = reader = new HouseholdRecordStreamReader(record =>
        {
            var expected = JsonSerializer.SerializeToUtf8Bytes(records[seen], PersonalSyncJsonContext.Default.SyncRecord);
            Assert.Equal(++seen, record.Version); Assert.Equal(end + 4, reader.RecordOffset);
            Assert.Equal(expected.Length, reader.RecordLength);
            Assert.Equal(expected.Length, BinaryPrimitives.ReadInt32BigEndian(original.AsSpan((int)reader.RecordOffset - 4, 4)));
            Assert.Equal(expected, original.AsSpan((int)reader.RecordOffset, reader.RecordLength).ToArray());
            end = reader.RecordOffset + reader.RecordLength;
        });
        foreach (var block in blocks) reader.Add(block);
        Assert.True(reader.Complete); Assert.Equal(records.Length, seen); Assert.Equal(original.LongLength, end);
        Assert.All(blocks, block => Assert.All(block.Bytes, value => Assert.Equal((byte)0, value)));
    }

    /// <summary>A corrupt declared JSON length cannot allocate unbounded memory or publish any derived row position.</summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(HouseholdFinancialFraming.MaximumRecordBytes + 1)]
    public void Invalid_framed_lengths_are_refused_before_any_row_is_indexed(int length)
    {
        var bytes = new byte[4]; BinaryPrimitives.WriteInt32BigEndian(bytes, length); var accepted = 0;
        using var reader = new HouseholdRecordStreamReader(_ => accepted++);
        Assert.Throws<CryptographicException>(() => reader.Add(new(0, true, 4, 1, bytes)));
        Assert.Equal(0, accepted); Assert.False(reader.Complete); Assert.All(bytes, value => Assert.Equal((byte)0, value));
    }
}
