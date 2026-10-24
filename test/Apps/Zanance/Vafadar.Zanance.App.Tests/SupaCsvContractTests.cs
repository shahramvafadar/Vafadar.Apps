using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.DataFiles;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-516: independently selected SUPA protocol boundaries, without replaying accepted source-flow cases.</summary>
[Trait("AT", "AT-516")]
[Collection("Entry validation units")]
public sealed class SupaCsvContractTests
{
    [Theory]
    [InlineData("BookgDt,ValDt,Amt,AmtCcy,CdtDbtInd\r\n2026-10-24,2026-10-24,\"1\"2,EUR,DBIT\r\n")]
    [InlineData("BookgDt,ValDt,Amt,AmtCcy,CdtDbtInd\r\n2026-10-24,2026-10-24,1\"2\",EUR,DBIT\r\n")]
    [InlineData("BookgDt,ValDt,Amt,AmtCcy,CdtDbtInd\r\n2026-10-24,2026-10-24,\"1,EUR,DBIT\r\n")]
    [InlineData("BookgDt,ValDt,Amt,AmtCcy,CdtDbtInd,Amt\r\n2026-10-24,2026-10-24,1,EUR,DBIT,1\r\n")]
    [InlineData("BookgDt,Amt,AmtCcy,CdtDbtInd\r\n2026-10-24,1,EUR,DBIT\r\n")]
    [InlineData("BookgDt,ValDt,Amt,AmtCcy,CdtDbtInd,UnknownFutureRelationship\r\n2026-10-24,2026-10-24,1,EUR,DBIT,x\r\n")]
    [InlineData("BookgDt,ValDt,Amt,AmtCcy,CdtDbtInd\r\n2026-10-24,2026-10-24,1,EUR\r\n")]
    public void Malformed_or_unreviewed_column_contract_is_refused_before_financial_mapping(string source) =>
        Assert.ThrowsAny<FormatException>(() => SupaCsv.Read(source));

    [Theory]
    [InlineData("JPY", "123", 123L)][InlineData("JPY", "123.1", -1L)]
    [InlineData("KWD", "1.234", 1234L)][InlineData("KWD", "1.2345", -1L)]
    [InlineData("EUR", "0.01", 1L)][InlineData("EUR", "0", -1L)]
    [InlineData("EUR", " 1.00", -1L)][InlineData("EUR", "1.00 ", -1L)]
    [InlineData("EUR", "1e2", -1L)][InlineData("EUR", "١.٢٣", -1L)]
    public void Original_ascii_magnitude_and_currency_minor_units_are_preserved_without_conversion(string currency, string amount, long expected)
    {
        var rows = SupaCsv.Read(SupaCsvFlowTests.Fixture(count: 1)); var values = rows[1].ToArray();
        values[rows[0].ToList().IndexOf("Amt")] = amount; values[rows[0].ToList().IndexOf("AmtCcy")] = currency;
        var account = new Account { Name = "Original currency target", CurrencyCode = currency, OpeningDate = new(2026, 1, 1) };
        var preview = Assert.Single(SupaCsv.Preview([rows[0], values], account, [], [], new HashSet<Guid>()));
        if (expected < 0) { Assert.NotNull(preview.Error); Assert.Null(preview.Entry); }
        else { Assert.Null(preview.Error); Assert.Equal(expected, preview.Entry!.Amount); Assert.Equal(EntryKind.Income, preview.Entry.Kind); }
    }

    [Fact]
    public void Technical_record_limit_accepts_the_complete_bound_and_refuses_one_extra_instead_of_truncating()
    {
        Assert.Equal(StarMoneyCsv.MaximumRows + 1, SupaCsv.Read(SupaCsvFlowTests.Fixture(count: StarMoneyCsv.MaximumRows)).Count);
        var failure = Assert.Throws<CsvReadLimitException>(() => SupaCsv.Read(SupaCsvFlowTests.Fixture(count: StarMoneyCsv.MaximumRows + 1)));
        Assert.Equal(CsvReadLimit.Rows, failure.Limit);
    }

    [Fact]
    public void Source_account_and_target_scope_disambiguate_equal_original_software_ids()
    {
        var account = new Account { Name = "Original target", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) };
        var first = Assert.Single(SupaCsv.Preview(SupaCsv.Read(SupaCsvFlowTests.Fixture(count: 1)), account, [], [], new HashSet<Guid>()));
        var otherSource = Assert.Single(SupaCsv.Preview(SupaCsv.Read(SupaCsvFlowTests.Fixture(count: 1, field: "AcctId", value: "another-original-source")), account, [], [], new HashSet<Guid>()));
        var otherTarget = Assert.Single(SupaCsv.Preview(SupaCsv.Read(SupaCsvFlowTests.Fixture(count: 1)),
            new Account { Name = "Second target", CurrencyCode = "EUR", OpeningDate = account.OpeningDate }, [], [], new HashSet<Guid>()));
        Assert.NotEqual(first.Entry!.Id, otherSource.Entry!.Id); Assert.NotEqual(first.Entry.Id, otherTarget.Entry!.Id);
    }

    [Fact]
    public void Bank_reference_without_original_account_and_row_identity_never_proves_an_already_imported_booking()
    {
        var rows = SupaCsv.Read(SupaCsvFlowTests.Fixture(count: 1));
        var header = rows[0].ToArray(); var values = rows[1].ToArray(); values[Array.IndexOf(header, "AcctId")] = "";
        var account = new Account { Name = "Original target", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) };
        var first = Assert.Single(SupaCsv.Preview([header, values], account, [], [], new HashSet<Guid>()));
        var repeat = Assert.Single(SupaCsv.Preview([header, values], account, [], [first.Entry!], new HashSet<Guid> { first.Entry!.Id }));
        Assert.False(repeat.AlreadyImported); Assert.True(repeat.PossibleDuplicate); Assert.NotEqual(first.Entry.Id, repeat.Entry!.Id);
    }

    [Fact]
    public void Original_literal_remittance_is_not_unprotected_or_used_to_guess_a_transfer_or_refund()
    {
        var rows = SupaCsv.Read(SupaCsvFlowTests.Fixture(count: 1, field: "RmtInf", value: "'=Original \"literal\", transfer refund text"));
        var account = new Account { Name = "Original target", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) };
        var preview = Assert.Single(SupaCsv.Preview(rows, account, [], [], new HashSet<Guid>()));
        Assert.Contains("'=Original \"literal\", transfer refund text", preview.Entry!.Note!);
        Assert.Equal(EntryKind.Income, preview.Entry.Kind); Assert.Null(preview.Entry.ToAccountId); Assert.Null(preview.Entry.RefundOfId);
    }
}
