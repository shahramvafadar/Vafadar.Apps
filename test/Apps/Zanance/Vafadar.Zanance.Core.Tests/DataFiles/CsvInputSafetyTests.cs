using System.Text;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.DataFiles;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Core.Tests.DataFiles;

/// <summary>Qualifies complete CSV input budgets, real decoder boundaries and the original large export contract.</summary>
[Trait("AT", "AT-385")]
public sealed class CsvInputSafetyTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("utf8")][InlineData("utf8bom")][InlineData("utf16le")][InlineData("utf16be")][InlineData("utf32le")][InlineData("utf32be")]
    public async Task Original_BOM_decoding_survives_one_byte_reads_and_embedded_Unicode(string kind)
    {
        var text = "date,title,note\r\n2026-10-19,آزمایش,\"fictitious\r\né🙂\"\r\n";
        Encoding encoding = kind switch { "utf8" => new UTF8Encoding(false), "utf8bom" => new UTF8Encoding(true),
            "utf16le" => new UnicodeEncoding(false, true), "utf16be" => new UnicodeEncoding(true, true),
            "utf32le" => new UTF32Encoding(false, true), _ => new UTF32Encoding(true, true) };
        var bytes = encoding.GetPreamble().Concat(encoding.GetBytes(text)).ToArray();
        using var source = new FragmentedStream(bytes, 1);
        Assert.Equal(text, await CsvInput.ReadTextAsync(source, cancellationToken: Ct));
        Assert.True(source.CanRead); Assert.Equal(bytes.Length, source.BytesRead);
    }

    [Theory]
    [InlineData(31, true)][InlineData(32, true)][InlineData(33, false)]
    public async Task An_unseekable_source_is_counted_and_only_one_excess_byte_can_be_read(int length, bool accepted)
    {
        var budget = CsvReadLimits.Import with { MaximumBytes = 32 };
        using var source = new FragmentedStream(Encoding.UTF8.GetBytes(new string('x', length)), 7);
        if (accepted) Assert.Equal(length, (await CsvInput.ReadTextAsync(source, budget, Ct)).Length);
        else { var error = await Assert.ThrowsAsync<CsvReadLimitException>(() => CsvInput.ReadTextAsync(source, budget, Ct)); Assert.Equal(CsvReadLimit.Bytes, error.Limit); Assert.Equal(32, error.Maximum); }
        Assert.Equal(Math.Min(length, 33), source.BytesRead); Assert.True(source.CanRead);
    }

    [Theory]
    [InlineData(7, true)][InlineData(8, true)][InlineData(9, false)]
    public async Task Decoded_characters_have_their_own_exact_bound(int length, bool accepted)
    {
        using var source = new FragmentedStream(Encoding.UTF8.GetBytes(new string('x', length)), 3);
        var budget = CsvReadLimits.Import with { MaximumCharacters = 8 };
        if (accepted) Assert.Equal(length, (await CsvInput.ReadTextAsync(source, budget, Ct)).Length);
        else Assert.Equal(CsvReadLimit.Characters, (await Assert.ThrowsAsync<CsvReadLimitException>(() => CsvInput.ReadTextAsync(source, budget, Ct))).Limit);
    }

    [Fact]
    public async Task Cancellation_before_the_first_read_never_consumes_or_closes_the_provider_stream()
    {
        using var source = new FragmentedStream([65, 66], 1); using var cancel = new CancellationTokenSource(); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CsvInput.ReadTextAsync(source, cancellationToken: cancel.Token));
        Assert.Equal(0, source.BytesRead); Assert.True(source.CanRead);
    }

    [Theory]
    [InlineData(CsvReadLimit.Rows)][InlineData(CsvReadLimit.Columns)][InlineData(CsvReadLimit.Cells)]
    [InlineData(CsvReadLimit.FieldCharacters)][InlineData(CsvReadLimit.Characters)]
    public void Complete_parser_budgets_accept_the_boundary_and_refuse_the_next_unit(CsvReadLimit limit)
    {
        var budget = limit switch { CsvReadLimit.Rows => CsvReadLimits.Import with { MaximumRows = 2 },
            CsvReadLimit.Columns => CsvReadLimits.Import with { MaximumColumns = 2 },
            CsvReadLimit.Cells => CsvReadLimits.Import with { MaximumCells = 2 },
            CsvReadLimit.FieldCharacters => CsvReadLimits.Import with { MaximumFieldCharacters = 2 },
            _ => CsvReadLimits.Import with { MaximumCharacters = 2 } };
        var exact = limit switch { CsvReadLimit.Rows => "a\r\nb", CsvReadLimit.Columns or CsvReadLimit.Cells => "a,b", _ => "ab" };
        var excess = limit switch { CsvReadLimit.Rows => "a\r\nb\r\nc", CsvReadLimit.Columns or CsvReadLimit.Cells => "a,b,c", _ => "abc" };
        Assert.NotEmpty(Csv.ReadDetected(exact, budget));
        Assert.Equal(limit, Assert.Throws<CsvReadLimitException>(() => Csv.ReadDetected(excess, budget)).Limit);
    }

    [Fact]
    public void Escaped_quotes_count_as_one_preserved_field_character_and_blank_records_consume_cell_budget()
    {
        var budget = CsvReadLimits.Import with { MaximumFieldCharacters = 2, MaximumCells = 2 };
        Assert.Equal("a\"", Assert.Single(Assert.Single(Csv.Read("\"a\"\"\"", ',', budget))));
        Assert.Equal(CsvReadLimit.Cells, Assert.Throws<CsvReadLimitException>(() => Csv.Read("\r\n\r\na", ',', budget)).Limit);
    }

    [Theory]
    [InlineData("\"")][InlineData("a,\"unfinished")][InlineData("a,\"unfinished\r\nsecond line")]
    public void An_unfinished_quoted_record_is_refused_instead_of_silently_becoming_a_partial_financial_row(string input)
    {
        Assert.Throws<CsvMalformedInputException>(() => Csv.ReadDetected(input));
    }

    [Fact]
    public async Task A_complete_hundred_thousand_row_original_export_retains_every_cell_without_budget_truncation()
    {
        var account = new Account { Name = "Fictitious input bound", CurrencyCode = "EUR", OpeningBalanceKnown = true };
        var entries = Enumerable.Range(0, 100000).Select(index => new LedgerEntry { AccountId = account.Id,
            Date = new DateOnly(2026, 10, 19).AddDays(-(index % 365)), Amount = index + 1, Kind = EntryKind.Expense,
            Title = "Fictitious Unicode é ردیف " + index, Note = "fictitious,\"quoted\"\r\nsecond line",
            Tags = ["fictitious"], CreatedAt = new DateTimeOffset(2026, 10, 19, 9, 0, 0, TimeSpan.Zero) }).ToArray();
        var text = CsvExport.Write(entries, new Dictionary<Guid, Account> { [account.Id] = account }, _ => "", true);
        Assert.True(Encoding.UTF8.GetByteCount(text) < CsvReadLimits.Import.MaximumBytes);
        var bytes = new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(text)).ToArray();
        using var source = new MemoryStream(bytes, writable: false);
        var read = await CsvInput.ReadTextAsync(source, cancellationToken: Ct);
        Assert.Equal(text, read);
        var rows = Csv.ReadDetected(read);
        Assert.Equal(100001, rows.Count); Assert.Equal(CsvExport.Columns.Count, rows[0].Count);
        Assert.All(rows, row => Assert.Equal(CsvExport.Columns.Count, row.Count));
        Assert.Equal(text, Csv.Write(rows));
        var preview = CsvImport.PreviewOwn(rows, [account], [], _ => "", []);
        Assert.Equal(entries.Length, preview.Count);
        Assert.Equal(entries.Length, preview.Select(row => row.Entry!.Id).Distinct().Count());
        var expectedById = entries.ToDictionary(entry => entry.Id);
        for (var index = 0; index < entries.Length; index++)
        {
            var actual = preview[index]; Assert.Null(actual.Error);
            var expected = expectedById[actual.Entry!.Id];
            Assert.Equal(expected.Id, actual.Entry!.Id); Assert.Equal(expected.AccountId, actual.Entry.AccountId);
            Assert.Equal(expected.Amount, actual.Entry.Amount); Assert.Equal(expected.Date, actual.Entry.Date);
            Assert.Equal(expected.Kind, actual.Entry.Kind); Assert.Equal(expected.Title, actual.Entry.Title);
            Assert.Equal(expected.Note, actual.Entry.Note); Assert.Equal(expected.Tags, actual.Entry.Tags);
        }
    }

    [Fact]
    public async Task Cancellation_during_a_fragmented_BOM_read_never_returns_a_partial_text()
    {
        using var cancel = new CancellationTokenSource(); using var source = new FragmentedStream([239, 187, 191, 65], 1);
        source.AfterRead = () => cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CsvInput.ReadTextAsync(source, cancellationToken: cancel.Token));
        Assert.Equal(1, source.BytesRead); Assert.True(source.CanRead);
    }

    [Fact]
    public async Task Provider_failure_after_partial_read_does_not_publish_partial_text_or_close_the_original_stream()
    {
        using var source = new FragmentedStream(Encoding.UTF8.GetBytes("fictitious long input"), 2);
        source.AfterRead = () => { if (source.BytesRead >= 4) throw new IOException("Fictitious provider refusal."); };
        await Assert.ThrowsAsync<IOException>(() => CsvInput.ReadTextAsync(source, cancellationToken: Ct));
        Assert.True(source.CanRead);
    }

    [Fact]
    public async Task A_seekable_provider_reporting_an_empty_length_cannot_bypass_the_actual_byte_budget()
    {
        using var source = new FragmentedStream(Encoding.UTF8.GetBytes(new string('x', 100)), 7) { ReportedLength = 0 };
        var error = await Assert.ThrowsAsync<CsvReadLimitException>(() => CsvInput.ReadTextAsync(source, CsvReadLimits.Import with { MaximumBytes = 32 }, Ct));
        Assert.Equal(CsvReadLimit.Bytes, error.Limit); Assert.Equal(33, source.BytesRead); Assert.Equal(0, source.LengthRequests);
    }

    /// <summary>A real nonseekable byte source with deliberately fragmented provider reads; Length is unavailable.</summary>
    private sealed class FragmentedStream(byte[] bytes, int fragment) : Stream
    {
        private bool _disposed;
        internal int BytesRead { get; private set; }
        public override bool CanRead => !_disposed;
        protected override void Dispose(bool disposing) { _disposed = true; base.Dispose(disposing); }
        internal Action? AfterRead;
        internal long? ReportedLength;
        internal int LengthRequests;
        public override bool CanSeek => ReportedLength is not null;
        public override bool CanWrite => false;
        public override long Length { get { LengthRequests++; return ReportedLength ?? throw new NotSupportedException(); } }
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count)
        {
            var length = Math.Min(Math.Min(count, fragment), bytes.Length - BytesRead);
            bytes.AsSpan(BytesRead, length).CopyTo(buffer.AsSpan(offset, length)); BytesRead += length; AfterRead?.Invoke(); return length;
        }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested(); var length = Math.Min(Math.Min(buffer.Length, fragment), bytes.Length - BytesRead);
            bytes.AsMemory(BytesRead, length).CopyTo(buffer); BytesRead += length; AfterRead?.Invoke(); return ValueTask.FromResult(length);
        }
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
