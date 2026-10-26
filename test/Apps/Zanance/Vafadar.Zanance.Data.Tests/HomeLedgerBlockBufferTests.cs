using System.Buffers.Binary;
using System.Text.Json;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>Read-owned plaintext memory is bounded, cleared and independent of every returned original row.</summary>
[Trait("AT", "AT-82")]
public sealed class HomeLedgerBlockBufferTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public void Smaller_blocks_reuse_only_the_same_read_owned_capacity_and_exact_extent()
    {
        var arrays = new List<byte[]>(); using var owner = new HomeLedgerBlockBuffer(Allocate);
        byte[] Allocate(int count) { var value = new byte[count]; arrays.Add(value); return value; }
        owner.GetSpan(900).Fill(17); Assert.Equal(900, owner.GetSpan(900).Length);
        Assert.Equal(20, owner.GetSpan(20).Length); Assert.Single(arrays);
        Assert.True(arrays[0].Length >= 900 && arrays[0].Length <= HomeLedgerBlockCodec.MaxUncompressedBytes);
        owner.GetSpan(20)[0] = 33; Assert.Equal((byte)33, arrays[0][0]);
    }

    [Fact]
    public void Growth_clears_retired_owned_capacity_including_its_spare_tail()
    {
        var arrays = new List<byte[]>(); using var owner = new HomeLedgerBlockBuffer(count =>
        { var bytes = Enumerable.Repeat((byte)171, count).ToArray(); arrays.Add(bytes); return bytes; });
        owner.GetSpan(40).Fill(99); owner.GetSpan(5000).Fill(7);
        Assert.Equal(2, arrays.Count); Assert.True(IsCleared(arrays[0]));
        Assert.Equal((byte)7, arrays[1][0]); owner.Dispose(); Assert.All(arrays, bytes => Assert.True(IsCleared(bytes)));
    }

    [Fact]
    public void Disposal_clears_a_previous_longer_block_and_every_owned_spare_byte()
    {
        byte[]? allocated = null; var owner = new HomeLedgerBlockBuffer(count => allocated = Enumerable.Repeat((byte)73, count).ToArray());
        owner.GetSpan(3000).Fill(37); owner.GetSpan(2).Fill(5); owner.Dispose();
        Assert.NotNull(allocated); Assert.True(IsCleared(allocated)); owner.Dispose(); Assert.True(IsCleared(allocated));
    }

    [Fact]
    public void A_disposed_owner_cannot_be_reused_or_reallocate_private_storage()
    {
        var allocations = 0; var owner = new HomeLedgerBlockBuffer(count => { allocations++; return new byte[count]; });
        owner.Dispose(); Assert.Throws<ObjectDisposedException>(() => { _ = owner.GetSpan(1).Length; }); Assert.Equal(0, allocations);
    }

    [Theory]
    [InlineData(-1), InlineData(0), InlineData(HomeLedgerBlockCodec.MaxUncompressedBytes + 1)]
    public void Invalid_extents_never_allocate_or_expand_the_original_transport_limit(int count)
    {
        var allocations = 0; using var owner = new HomeLedgerBlockBuffer(size => { allocations++; return new byte[size]; });
        Assert.Throws<InvalidDataException>(() => { _ = owner.GetSpan(count).Length; }); Assert.Equal(0, allocations);
    }

    [Fact]
    public void An_invalid_transferred_array_is_cleared_before_its_contract_is_rejected()
    {
        byte[]? transferred = null; using var owner = new HomeLedgerBlockBuffer(count =>
            transferred = Enumerable.Repeat((byte)113, count - 1).ToArray());
        Assert.Throws<InvalidOperationException>(() => { _ = owner.GetSpan(20).Length; });
        Assert.NotNull(transferred); Assert.True(IsCleared(transferred));
    }

    [Fact]
    public void A_failed_growth_still_clears_the_original_owned_capacity_when_the_read_leaves()
    {
        byte[]? first = null; var calls = 0;
        var owner = new HomeLedgerBlockBuffer(count => ++calls == 1 ? first = new byte[count] : throw new IOException("Fictitious allocator refusal."));
        try
        {
            owner.GetSpan(30).Fill(27); Assert.Throws<IOException>(() => { _ = owner.GetSpan(8000).Length; });
        }
        finally { owner.Dispose(); }
        Assert.NotNull(first); Assert.True(IsCleared(first));
    }

    [Fact]
    public void Reusing_expansion_bytes_preserves_all_original_rows_and_independent_mutable_tags()
    {
        var first = new HomeLedgerPhysicalRow[] { new(511, Entry(1, ["original", "دقیق", null!])), new(510, Entry(2, [])) };
        var second = new HomeLedgerPhysicalRow[] { new(1023, Entry(3, null!)) };
        var before = JsonSerializer.Serialize(first); var after = JsonSerializer.Serialize(second);
        var arrays = new List<byte[]>(); var owner = new HomeLedgerBlockBuffer(count => { var bytes = new byte[count]; arrays.Add(bytes); return bytes; });
        var one = HomeLedgerBlockCodec.DecodeWithBuffer(HomeLedgerBlockCodec.Encode(first, Ct), 0, owner, Ct);
        var two = HomeLedgerBlockCodec.DecodeWithBuffer(HomeLedgerBlockCodec.Encode(second, Ct), 1, owner, Ct); owner.Dispose();
        Assert.Equal(before, JsonSerializer.Serialize(one)); Assert.Equal(after, JsonSerializer.Serialize(two));
        Assert.NotSame(first[0].Entry.Tags, one[0].Entry.Tags); Assert.NotSame(first[1].Entry.Tags, one[1].Entry.Tags);
        Assert.Null(two[0].Entry.Tags); one[0].Entry.Tags.Add("changed return only"); Assert.Equal(before, JsonSerializer.Serialize(first));
        Assert.All(arrays, bytes => Assert.True(IsCleared(bytes)));
    }

    [Fact]
    public void Original_cancellation_does_not_publish_another_row_or_retain_previous_plaintext()
    {
        var arrays = new List<byte[]>(); var owner = new HomeLedgerBlockBuffer(count => { var bytes = new byte[count]; arrays.Add(bytes); return bytes; });
        var payload = HomeLedgerBlockCodec.Encode([new(1, Entry(1, []))], Ct);
        var accepted = HomeLedgerBlockCodec.DecodeWithBuffer(payload, 0, owner, Ct);
        using var cancelled = CancellationTokenSource.CreateLinkedTokenSource(Ct); cancelled.Cancel();
        try
        {
            var error = Assert.Throws<OperationCanceledException>(() => HomeLedgerBlockCodec.DecodeWithBuffer(payload, 0, owner, cancelled.Token));
            Assert.Equal(cancelled.Token, error.CancellationToken);
        }
        finally { owner.Dispose(); }
        Assert.Single(accepted); Assert.All(arrays, bytes => Assert.True(IsCleared(bytes)));
    }

    [Fact]
    public void Damaged_compression_after_a_valid_block_cannot_escape_or_retain_partial_plaintext()
    {
        var arrays = new List<byte[]>(); var owner = new HomeLedgerBlockBuffer(count => { var bytes = new byte[count]; arrays.Add(bytes); return bytes; });
        var payload = HomeLedgerBlockCodec.Encode([new(1, Entry(1, ["retained"]))], Ct);
        var accepted = HomeLedgerBlockCodec.DecodeWithBuffer(payload, 0, owner, Ct); var expected = JsonSerializer.Serialize(accepted);
        var damaged = payload[..^1]; BinaryPrimitives.WriteInt32LittleEndian(damaged.AsSpan(20), damaged.Length - 24);
        try { Assert.Throws<InvalidDataException>(() => HomeLedgerBlockCodec.DecodeWithBuffer(damaged, 0, owner, Ct)); }
        finally { owner.Dispose(); }
        Assert.Equal(expected, JsonSerializer.Serialize(accepted)); Assert.All(arrays, bytes => Assert.True(IsCleared(bytes)));
    }

    [Fact]
    public void A_large_valid_original_block_grows_within_the_existing_limit_without_a_new_small_buffer_cap()
    {
        var entry = Entry(1, ["large original"]); entry.Note = new string('x', 300000);
        var expected = JsonSerializer.Serialize(entry); var arrays = new List<byte[]>();
        using (var owner = new HomeLedgerBlockBuffer(count => { var bytes = new byte[count]; arrays.Add(bytes); return bytes; }))
        {
            var decoded = Assert.Single(HomeLedgerBlockCodec.DecodeWithBuffer(HomeLedgerBlockCodec.Encode([new(1, entry)], Ct), 0, owner, Ct));
            Assert.Equal(expected, JsonSerializer.Serialize(decoded.Entry));
        }
        Assert.Single(arrays); Assert.InRange(arrays[0].Length, 300000, HomeLedgerBlockCodec.MaxUncompressedBytes);
        Assert.True(IsCleared(arrays[0]));
    }

    private static bool IsCleared(byte[] bytes) => bytes.AsSpan().IndexOfAnyExcept((byte)0) < 0;

    private static LedgerEntry Entry(int id, List<string> tags) => new(new Guid(id, 1, 1, 1, 2, 3, 4, 5, 6, 7, 8))
    {
        AccountId = new Guid(2, 1, 1, 1, 2, 3, 4, 5, 6, 7, 8), Date = new(2026, 10, 25), Amount = 123,
        Kind = EntryKind.Expense, Title = "Original " + id, Note = "source note", Tags = tags,
        CreatedAt = new(2026, 10, 25, 9, 0, 0, TimeSpan.FromHours(1)),
        UpdatedAt = new(2026, 10, 25, 10, 0, 0, TimeSpan.FromHours(-2)), OriginalAmount = 71, OriginalCurrencyCode = "KWD"
    };
}
