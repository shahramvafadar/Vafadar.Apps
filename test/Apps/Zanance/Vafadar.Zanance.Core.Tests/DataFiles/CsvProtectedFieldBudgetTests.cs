using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.DataFiles;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Core.Tests.DataFiles;

/// <summary>The explicit format-three protection marker is transport, not an extra original text character.</summary>
public sealed class CsvProtectedFieldBudgetTests
{
    /// <summary>The complete original note survives the actual own exporter, bounded parser and own preview.</summary>
    [Theory]
    [InlineData("=")]
    [InlineData(" +")]
    [InlineData("\t")]
    [InlineData("\r")]
    [InlineData("\n")]
    [InlineData("'")]
    [InlineData("@")]
    public void Own_format_three_round_trips_a_complete_maximum_protected_note(string prefix)
    {
        var note = prefix + new string('x', 65536 - prefix.Length);
        var account = new Account { Name = "Fictitious cash", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) };
        var entry = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense,
            Amount = 123, Date = new(2026, 5, 1), Note = note };
        var wire = CsvExport.Write([entry], new Dictionary<Guid, Account> { [account.Id] = account }, _ => "", true);
        var rows = Csv.ReadDetected(wire);
        var preview = Assert.Single(CsvImport.PreviewOwn(rows, [account], [], _ => "", []));
        Assert.Null(preview.Error);
        Assert.Equal(entry.Id, preview.Entry!.Id);
        Assert.Equal(entry.AccountId, preview.Entry.AccountId);
        Assert.Equal(entry.Kind, preview.Entry.Kind);
        Assert.Equal(entry.Date, preview.Entry.Date);
        Assert.Equal(entry.Amount, preview.Entry.Amount);
        Assert.Equal(note, preview.Entry.Note);
    }

    /// <summary>A marker never lifts the original bound without the matching explicit reversible contract.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("2")]
    [InlineData("4")]
    public void Other_versions_cannot_borrow_the_protection_allowance(string version)
    {
        var wire = Csv.Write([new[] { "note", "format_version" }, new[] { "'=" + new string('x', 65535), version }]);
        AssertFieldLimit(wire);
    }

    /// <summary>Ordinary files retain the exact previous field budget.</summary>
    [Fact]
    public void A_generic_file_without_a_version_still_refuses_the_extra_character() =>
        AssertFieldLimit(Csv.Write([new[] { "note" }, new[] { "'=" + new string('x', 65535) }]));

    /// <summary>A plain apostrophe which the original decoder would retain cannot hide an oversized value.</summary>
    [Fact]
    public void A_non_marker_apostrophe_still_counts_as_original_text() =>
        AssertFieldLimit(Csv.Write([new[] { "note", "format_version" }, new[] { "'" + new string('x', 65536), "3" }]));

    /// <summary>Only one reversible transport character can be excluded, never an oversized decoded source.</summary>
    [Fact]
    public void An_oversized_original_note_is_still_refused() =>
        AssertFieldLimit(Csv.Write([new[] { "note", "format_version" }, new[] { "'=" + new string('x', 65536), "3" }]));

    /// <summary>Ambiguous version columns must not grant extra capacity to an otherwise oversized cell.</summary>
    [Fact]
    public void Duplicate_version_columns_cannot_grant_the_allowance() =>
        AssertFieldLimit(Csv.Write([new[] { "note", "format_version", "FORMAT_VERSION" },
            new[] { "'=" + new string('x', 65535), "3", "3" }]));

    /// <summary>A contract on later data rows does not turn a header into protected user text.</summary>
    [Fact]
    public void An_oversized_header_is_still_refused() =>
        AssertFieldLimit(Csv.Write([new[] { "'=" + new string('x', 65535), "format_version" }, new[] { "note", "3" }]));

    /// <summary>A generic producer's version label does not change its literal human-text interpretation.</summary>
    [Fact]
    public void A_generic_version_label_cannot_declare_the_own_file_contract()
    {
        var note = "=" + new string('x', 65535);
        var wire = Csv.Write([new[] { "date", "amount", "note", "format_version", "appended" },
            new[] { "2026-05-01", "-1.23", Csv.LiteralText(note), "3", "original metadata" }]);
        AssertFieldLimit(wire);
    }

    /// <summary>An earlier valid row cannot lend its contract to a later legacy row.</summary>
    [Fact]
    public void A_later_legacy_row_cannot_borrow_an_earlier_format_three_contract()
    {
        var protectedText = "'=" + new string('x', 65535);
        AssertFieldLimit(Csv.Write([CsvExport.Columns, OwnBudgetRow(protectedText, "3"), OwnBudgetRow(protectedText, "2")]));
    }

    /// <summary>A genuine own header still requires the current row's reversible version.</summary>
    [Theory]
    [InlineData("2")]
    [InlineData("4")]
    public void A_legacy_own_row_cannot_use_the_reversible_marker_budget(string version) =>
        AssertFieldLimit(Csv.Write([CsvExport.Columns, OwnBudgetRow("'=" + new string('x', 65535), version)]));

    /// <summary>Appended metadata does not displace the original unique version marker or text field.</summary>
    [Fact]
    public void An_own_version_marker_before_appended_metadata_keeps_the_original_note()
    {
        var protectedText = "'=" + new string('x', 65535);
        var rows = Csv.ReadDetected(Csv.Write([CsvExport.Columns.Append("appended").ToArray(),
            OwnBudgetRow(protectedText, "3").Append("short metadata").ToArray()]));
        Assert.Equal(protectedText, rows[1][12]);
        Assert.Equal("=" + new string('x', 65535), Csv.UnprotectLiteralText(rows[1][12]));
    }

    /// <summary>Both named holdings text paths use their actual exporter and decoder.</summary>
    [Theory]
    [InlineData("note")]
    [InlineData("name")]
    public void Holdings_text_round_trips_at_the_same_original_boundary(string field)
    {
        var text = "=" + new string('x', 65535);
        var type = new AssetType { Name = "Fictitious gold", PriceCurrencyCode = "EUR", Metal = Metal.Gold,
            PurityPer10000 = 7500, Note = text };
        var location = new AssetLocation { Name = text };
        var wire = field == "note" ? HoldingsCsv.Write([type], [], [], [], true)
            : HoldingsCsv.Write([], [location], [], [], true);
        var content = HoldingsCsv.Read(Csv.ReadDetected(wire), [], []);
        Assert.Empty(content.Errors);
        Assert.Equal(text, field == "note" ? Assert.Single(content.Types).Note : Assert.Single(content.Locations).Name);
    }

    /// <summary>A typed column never borrows a text-decoder allowance.</summary>
    [Fact]
    public void A_typed_own_column_keeps_its_original_limit()
    {
        var row = OwnBudgetRow("", "3"); row[3] = "'=" + new string('x', 65535);
        AssertFieldLimit(Csv.Write([CsvExport.Columns, row]));
    }

    /// <summary>Unknown future columns are not silently treated as protected human text.</summary>
    [Fact]
    public void An_unknown_appended_own_column_keeps_its_original_limit() =>
        AssertFieldLimit(Csv.Write([CsvExport.Columns.Append("unknown").ToArray(),
            OwnBudgetRow("", "3").Append("'=" + new string('x', 65535)).ToArray()]));

    /// <summary>Even a known own header cannot use two conflicting declarations.</summary>
    [Fact]
    public void A_duplicate_own_version_marker_cannot_grant_capacity() =>
        AssertFieldLimit(Csv.Write([CsvExport.Columns.Append("format_version").ToArray(),
            OwnBudgetRow("'=" + new string('x', 65535), "3").Append("3").ToArray()]));

    /// <summary>The holdings decoder consumes the first named column; a later ignored duplicate remains bounded.</summary>
    [Fact]
    public void An_ignored_duplicate_holdings_text_column_has_no_allowance()
    {
        var row = Enumerable.Repeat("", HoldingsCsv.Columns.Count).ToArray(); row[^1] = "3";
        AssertFieldLimit(Csv.Write([HoldingsCsv.Columns.Append("note").ToArray(),
            row.Append("'=" + new string('x', 65535)).ToArray()]));
    }

    /// <summary>Global decoded-size accounting is unchanged even when a row has a valid protection marker.</summary>
    [Fact]
    public void The_total_character_budget_still_includes_transport()
    {
        var wire = Csv.Write([new[] { "note", "format_version" }, new[] { "'=value", "3" }]);
        var error = Assert.Throws<CsvReadLimitException>(() => Csv.ReadDetected(wire,
            CsvReadLimits.Import with { MaximumCharacters = wire.Length - 1 }));
        Assert.Equal(CsvReadLimit.Characters, error.Limit);
    }

    private static void AssertFieldLimit(string wire)
    {
        var error = Assert.Throws<CsvReadLimitException>(() => Csv.ReadDetected(wire));
        Assert.Equal(CsvReadLimit.FieldCharacters, error.Limit);
        Assert.Equal(65536, error.Maximum);
    }

    // Refusal-only protocol rows deliberately do not claim to be valid financial imports.
    private static string[] OwnBudgetRow(string note, string version)
    {
        var row = Enumerable.Repeat("", CsvExport.Columns.Count).ToArray();
        row[12] = note; row[^1] = version; return row;
    }
}
