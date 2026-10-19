using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.DataFiles;

namespace Vafadar.Zanance.Core.Tests.DataFiles;

public sealed class CsvQuoteSyntaxTests
{
    [Theory, Trait("AT", "AT-387")]
    [InlineData("\"12\"34")]
    [InlineData("\"12\" 34")]
    [InlineData("\"12\"\t34")]
    [InlineData("12\"34")]
    [InlineData("\"12\"x")]
    [InlineData("\"12\" ")]
    [InlineData(" \"12\"")]
    [InlineData("\"\"12")]
    public void Misplaced_quotes_refuse_the_complete_source_instead_of_joining_financial_digits(string amount)
    {
        var text = "date,amount,title\r\n2026-10-19," + amount + ",Fictitious source\r\n";
        Assert.Throws<CsvMalformedInputException>(() => Csv.ReadDetected(text));
    }

    [Fact, Trait("AT", "AT-387")]
    public void Quoted_then_unquoted_digits_cannot_become_an_importable_larger_amount()
    {
        var source = "date,amount,title\r\n2026-10-19,\"12\"34,Fictitious source\r\n";
        Assert.Throws<CsvMalformedInputException>(() =>
        {
            var rows = Csv.ReadDetected(source);
            var account = new Account { Name = "Fictitious quote-bound account", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) };
            var mapping = new ImportMapping(account.Id, 0, "yyyy-MM-dd", PeriodCalendar.Gregorian, 1, '.', SignMode.AllExpenses, TitleColumn: 2);
            var preview = CsvImport.PreviewGeneric(rows, mapping, [account], [], _ => "", []);
            Assert.Equal(123400L, preview.Single().Entry!.Amount);
        });
    }

    [Theory, Trait("AT", "AT-387")]
    [InlineData(',', false)][InlineData(',', true)]
    [InlineData(';', false)][InlineData(';', true)]
    [InlineData('\t', false)][InlineData('\t', true)]
    public void Original_writer_quotes_Unicode_whitespace_and_multiline_fields_round_trip_completely(char separator, bool bom)
    {
        string[] fields = ["", " ", " 12 ", "12\"34", "\"", "\"12\"", "12,34", "12;34", "12\t34",
            "fictitious\r\nsecond line", "fictitious\rthird line", "fictitious\nfourth line", "\u0641\u0627\u0631\u0633\u06cc", "\u00e9\u20ac", "end"];
        var text = (bom ? "\uFEFF" : "") + Csv.Write([fields, fields], separator);
        var rows = Csv.ReadDetected(text);
        Assert.Equal(2, rows.Count);
        Assert.All(rows, row => Assert.Equal(fields, row));
    }

    [Theory, Trait("AT", "AT-387")]
    [InlineData("\r\n")][InlineData("\n")][InlineData("\r")]
    public void A_complete_quoted_amount_ends_at_the_original_record_boundary(string ending)
    {
        var rows = Csv.Read("amount" + ending + "\"12\"" + ending + "\"34\"", ',');
        Assert.Equal(3, rows.Count); Assert.Equal("12", Assert.Single(rows[1])); Assert.Equal("34", Assert.Single(rows[2]));
    }

    [Fact, Trait("AT", "AT-387")]
    public void Empty_quoted_columns_and_a_complete_quoted_end_of_file_retain_their_positions()
    {
        var rows = Csv.Read("a,b,c\r\n\"\",\"\",\"12\"", ',');
        Assert.Equal(new[] { "", "", "12" }, rows[1]);
    }
}
