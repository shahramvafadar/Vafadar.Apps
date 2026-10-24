using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.DataFiles;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-516: structured owner-account identifiers are not copied into financial notes.</summary>
[Trait("AT", "AT-516")]
public sealed class SupaCsvPrivacyTests
{
    [Theory]
    [InlineData("OwnrVrtlAcctIBAN", "DE00000000000000000516")]
    [InlineData("OwnrAcctCcy", "EUR")]
    public void Structured_owner_account_fields_stay_out_of_notes_while_original_counterparty_metadata_remains(string field, string value)
    {
        var rows = SupaCsv.Read(Csv.Write([
            new[] { "BookgDt", "ValDt", "Amt", "AmtCcy", "CdtDbtInd", field, "RmtdAcctIBAN", "Notes" },
            new[] { "2026-10-24", "2026-10-24", "1.00", "EUR", "DBIT", value, "GB00000000000000000516", "Original retained user note" },
        ]));
        var target = new Account { Name = "Original protected source target", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) };
        var row = Assert.Single(SupaCsv.Preview(rows, target, [], [], new HashSet<Guid>()));
        Assert.Null(row.Error); Assert.DoesNotContain(field + ":", row.Entry!.Note!); Assert.DoesNotContain(value, row.Entry.Note!);
        Assert.Contains("RmtdAcctIBAN: GB00000000000000000516", row.Entry.Note!); Assert.Contains("Original retained user note", row.Entry.Note!);
    }

    [Fact]
    public void An_explicit_original_user_note_is_preserved_even_when_it_mentions_its_own_source_reference()
    {
        const string original = "Keep this original reference: DE00000000000000000516";
        var rows = SupaCsv.Read(Csv.Write([
            new[] { "BookgDt", "ValDt", "Amt", "AmtCcy", "CdtDbtInd", "OwnrVrtlAcctIBAN", "Notes" },
            new[] { "2026-10-24", "2026-10-24", "1.00", "EUR", "DBIT", "DE00000000000000000516", original },
        ]));
        var target = new Account { Name = "Original source target", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) };
        var row = Assert.Single(SupaCsv.Preview(rows, target, [], [], new HashSet<Guid>()));
        Assert.Null(row.Error); Assert.Contains("Notes: " + original, row.Entry!.Note!); Assert.DoesNotContain("OwnrVrtlAcctIBAN:", row.Entry.Note!);
    }
}
