using System.Buffers.Binary;
using System.IO.Compression;
using System.Reflection;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>Complete typed-row transport retains original values/order and rejects unsupported derived bytes.</summary>
public sealed class HomeLedgerBlockCodecTests
{
    /// <summary>A malformed derived identity must decline optional transport, including after a valid prefix row.</summary>
    [Theory, Trait("AT", "AT-82")]
    [InlineData(0, false), InlineData(1, false), InlineData(0, true), InlineData(1, true)]
    public void Empty_derived_entry_identity_is_invalid_data(int damagedRow, bool buffered)
    {
        var payload = Encode([new(511, Entry(true)), new(510, Entry(false))]);
        var damaged = EmptyDerivedIdentityPayload.Damage(payload, 0, damagedRow);
        if (buffered)
        {
            using var buffer = new HomeLedgerBlockBuffer();
            Assert.Throws<InvalidDataException>(() => HomeLedgerBlockCodec.DecodeWithBuffer(damaged, 0, buffer,
                TestContext.Current.CancellationToken));
        }
        else Assert.Throws<InvalidDataException>(() => Decode(damaged, 0));
    }

    [Theory, Trait("AT", "AT-82")]
    [InlineData(false), InlineData(true)]
    public void Every_original_field_round_trips_without_normalization(bool populated)
    {
        var entry = Entry(populated);
        var row = Assert.Single(Decode(Encode([new(511, entry)]), 0));
        Assert.Equal(511, row.RowId); Assert.NotSame(entry, row.Entry);
        AssertFields(entry, row.Entry);
    }

    [Theory, Trait("AT", "AT-82")]
    [InlineData(-1L), InlineData(long.MinValue), InlineData(long.MaxValue), InlineData(0L)]
    public void Signed_physical_identity_keeps_its_original_bucket(long rowId)
    {
        var entry = Entry(true);
        var encoded = Encode([new(rowId, entry)]);
        var row = Assert.Single(Decode(encoded, rowId >> HomeLedgerBlockCodec.BlockShift));
        Assert.Equal(rowId, row.RowId); AssertFields(entry, row.Entry);
    }

    [Fact, Trait("AT", "AT-82")]
    public void Shared_or_empty_tags_become_independent_mutable_collections_each_read()
    {
        var first = Entry(false); var second = Entry(false); first.Tags = second.Tags = ["retained", null!];
        var encoded = Encode([new(511, first), new(510, second)]);
        var rows = Decode(encoded, 0);
        Assert.NotSame(first.Tags, rows[0].Entry.Tags); Assert.NotSame(rows[0].Entry.Tags, rows[1].Entry.Tags);
        rows[0].Entry.Tags.Add("local draft only");
        Assert.Equal(2, first.Tags.Count); Assert.Equal(2, rows[1].Entry.Tags.Count);
        Assert.Equal(2, Decode(encoded, 0)[0].Entry.Tags.Count);
        var empty = Entry(false);
        var emptyBytes = Encode([new(1, empty)]);
        var one = Assert.Single(Decode(emptyBytes, 0)).Entry;
        var two = Assert.Single(Decode(emptyBytes, 0)).Entry;
        Assert.Empty(one.Tags); one.Tags.Add("independent"); Assert.Empty(two.Tags);
    }

    [Fact, Trait("AT", "AT-82")]
    public void Original_null_tag_collection_and_null_elements_are_not_sanitized()
    {
        var entry = Entry(false); entry.Tags = null!;
        Assert.Null(Assert.Single(Decode(Encode([new(1, entry)]), 0)).Entry.Tags);
        entry.Tags = [null!, "", "\0", "دقیق"];
        AssertFields(entry, Assert.Single(Decode(Encode([new(1, entry)]), 0)).Entry);
    }

    [Fact, Trait("AT", "AT-82")]
    public void Unknown_enum_values_and_extreme_amounts_keep_their_typed_original_values()
    {
        var entry = Entry(true); entry.Kind = (EntryKind)int.MaxValue; entry.Direction = (AdjustmentDirection)int.MinValue;
        entry.Review = (ReviewState)(-27); entry.Source = (EntrySource)94;
        entry.Amount = long.MinValue; entry.OriginalAmount = long.MaxValue; entry.ToAmount = long.MinValue;
        entry.CreatedAt = DateTimeOffset.MinValue; entry.UpdatedAt = DateTimeOffset.MaxValue;
        AssertFields(entry, Assert.Single(Decode(Encode([new(1, entry)]), 0)).Entry);
    }

    [Fact, Trait("AT", "AT-82")]
    public void One_full_bucket_preserves_descending_source_order()
    {
        var rows = Enumerable.Range(0, HomeLedgerBlockCodec.MaxRows).Reverse().Select(i => new HomeLedgerPhysicalRow(i, Entry(i % 2 == 0))).ToArray();
        var decoded = Decode(Encode(rows), 0);
        Assert.Equal(rows.Select(row => row.RowId), decoded.Select(row => row.RowId));
        for (var i = 0; i < rows.Length; i++) AssertFields(rows[i].Entry, decoded[i].Entry);
    }

    [Fact, Trait("AT", "AT-82")]
    public void Empty_cross_bucket_duplicate_or_reordered_inputs_are_not_encoded()
    {
        Assert.Throws<InvalidDataException>(() => Encode([]));
        Assert.Throws<InvalidDataException>(() => Encode([new(512, Entry(false)), new(511, Entry(false))]));
        Assert.Throws<InvalidDataException>(() => Encode([new(1, Entry(false)), new(1, Entry(false))]));
        Assert.Throws<InvalidDataException>(() => Encode([new(1, Entry(false)), new(2, Entry(false))]));
    }

    [Theory, Trait("AT", "AT-82")]
    [InlineData(0, 0), InlineData(4, 2), InlineData(8, 31), InlineData(12, 0), InlineData(12, 513)]
    [InlineData(16, 0), InlineData(16, 33554433), InlineData(20, -1)]
    public void Unknown_or_oversized_header_is_refused(int ordinal, int replacement)
    {
        var bytes = Encode([new(1, Entry(false))]);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(ordinal), replacement);
        Assert.Throws<InvalidDataException>(() => Decode(bytes, 0));
    }

    [Fact, Trait("AT", "AT-82")]
    public void Missing_extra_or_incomplete_compressed_data_is_not_a_partial_ledger()
    {
        var bytes = Encode([new(1, Entry(true))]);
        Assert.Throws<InvalidDataException>(() => Decode(bytes[..23], 0));
        Assert.Throws<InvalidDataException>(() => Decode(bytes[..^1], 0));
        Assert.Throws<InvalidDataException>(() => Decode([.. bytes, 0], 0));
        Assert.Throws<InvalidDataException>(() => Decode(bytes, 1));
        var raw = Raw(bytes); Assert.Throws<InvalidDataException>(() => Decode(Repack(bytes, [.. raw, 0]), 0));
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(12), 2);
        Assert.Throws<InvalidDataException>(() => Decode(bytes, 0));
    }

    [Fact, Trait("AT", "AT-82")]
    public void Cancelled_work_never_returns_rows_or_an_encoded_block()
    {
        var entry = Entry(true); var bytes = Encode([new(1, entry)]);
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken); stop.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() => HomeLedgerBlockCodec.Encode([new(1, entry)], stop.Token));
        Assert.ThrowsAny<OperationCanceledException>(() => HomeLedgerBlockCodec.Decode(bytes, 0, stop.Token));
    }

    [Fact, Trait("AT", "AT-82")]
    public void Noncanonical_utf16_can_decline_caching_without_replacing_original_text()
    {
        var entry = Entry(false); entry.Note = "\ud800";
        Assert.Throws<InvalidDataException>(() => Encode([new(1, entry)]));
        Assert.Equal("\ud800", entry.Note);
    }

    private static byte[] Encode(IReadOnlyList<HomeLedgerPhysicalRow> rows) =>
        HomeLedgerBlockCodec.Encode(rows, TestContext.Current.CancellationToken);
    private static List<HomeLedgerPhysicalRow> Decode(byte[] bytes, long bucket) =>
        HomeLedgerBlockCodec.Decode(bytes, bucket, TestContext.Current.CancellationToken);

    private static LedgerEntry Entry(bool populated) => new(Guid.NewGuid())
    {
        AccountId = Guid.Empty, AggregatedFrom = populated ? DateOnly.MinValue : null, AggregatedTo = populated ? DateOnly.MaxValue : null,
        Amount = 12345, CategoryId = populated ? Guid.NewGuid() : null,
        CreatedAt = new DateTimeOffset(2026, 10, 25, 3, 14, 15, TimeSpan.FromMinutes(345)).AddTicks(123),
        Date = new(2026, 10, 25), Direction = populated ? AdjustmentDirection.Decrease : null,
        GroupId = populated ? Guid.NewGuid() : null, Icon = populated ? "icon\0value" : null,
        ImportBatchId = populated ? Guid.NewGuid() : null, IsAggregated = populated, IsPartialPayment = populated,
        Kind = EntryKind.Transfer, Note = populated ? "دقیق\r\nGöttingen 👨‍👩‍👧‍👦\0" : null,
        OccurrenceDate = populated ? new DateOnly(2024, 2, 29) : null, OriginalAmount = populated ? -789 : null,
        OriginalCurrencyCode = populated ? "KWD" : null, Payee = populated ? "" : null,
        RefundOfId = populated ? Guid.NewGuid() : null, ReimbursableAmount = populated ? 0 : null,
        ReimbursedBy = populated ? "particulier" : null, ReimbursementDueDate = populated ? new DateOnly(2030, 12, 31) : null,
        Review = ReviewState.Unreviewed, ScheduleId = populated ? Guid.NewGuid() : null, Source = EntrySource.Import,
        Tags = populated ? ["same", "same", "", null!, "برچسب"] : [], Title = populated ? "ß É €" : null,
        ToAccountId = populated ? Guid.NewGuid() : null, ToAmount = populated ? 9999 : null,
        UpdatedAt = new DateTimeOffset(2026, 10, 25, 3, 14, 15, TimeSpan.FromMinutes(-210)).AddTicks(456)
    };

    private static void AssertFields(LedgerEntry expected, LedgerEntry actual)
    {
        var properties = typeof(LedgerEntry).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        Assert.Equal(32, properties.Length);
        foreach (var property in properties)
        {
            if (property.Name == nameof(LedgerEntry.Tags))
            {
                if (expected.Tags is null) Assert.Null(actual.Tags);
                else Assert.Equal(expected.Tags, actual.Tags);
            }
            else if (property.GetValue(expected) is DateTimeOffset timestamp)
                Assert.True(timestamp.EqualsExact((DateTimeOffset)property.GetValue(actual)!));
            else Assert.Equal(property.GetValue(expected), property.GetValue(actual));
        }
    }

    private static byte[] Raw(byte[] encoded)
    {
        var raw = new byte[BinaryPrimitives.ReadInt32LittleEndian(encoded.AsSpan(16))];
        Assert.True(BrotliDecoder.TryDecompress(encoded.AsSpan(24), raw, out var written)); Assert.Equal(raw.Length, written); return raw;
    }

    private static byte[] Repack(byte[] header, byte[] raw)
    {
        var compressed = new byte[BrotliEncoder.GetMaxCompressedLength(raw.Length)];
        Assert.True(BrotliEncoder.TryCompress(raw, compressed, out var written, quality: 1, window: 22));
        var result = new byte[24 + written]; header.AsSpan(0, 24).CopyTo(result);
        BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(16), raw.Length); BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(20), written);
        compressed.AsSpan(0, written).CopyTo(result.AsSpan(24)); return result;
    }
}
