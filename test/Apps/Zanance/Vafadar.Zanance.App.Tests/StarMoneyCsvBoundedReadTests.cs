using Vafadar.Zanance.Core.DataFiles;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-297: source field budgets count original decoded data, preserving the established whole-file refusals.</summary>
[Trait("AT", "AT-297")]
public sealed class StarMoneyCsvBoundedReadTests
{
    [Fact]
    public void More_than_the_original_bank_row_budget_is_refused()
    {
        var original = Csv.Read(BankCsvFlowTests.Fixture(count: 1), ';');
        var source = Csv.Write(new[] { original[0] }.Concat(Enumerable.Repeat(original[1], StarMoneyCsv.MaximumRows + 1)), ';');
        Assert.ThrowsAny<FormatException>(() => StarMoneyCsv.Read(source));
    }

    [Fact]
    public void Excessive_bank_columns_are_refused()
    {
        var header = Enumerable.Repeat("BuchDatum", 129).ToArray();
        Assert.ThrowsAny<FormatException>(() => StarMoneyCsv.Read(Csv.Write(new[] { header, header }, ';')));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void An_oversized_decoded_bank_field_is_refused(bool quoted)
    {
        var value = new string(quoted ? ';' : 'x', 4097);
        Assert.ThrowsAny<FormatException>(() => StarMoneyCsv.Read(BankCsvFlowTests.Fixture(count: 1, changeColumn: "Kommentar", changeValue: value)));
    }

    [Theory]
    [InlineData("plain")]
    [InlineData("separator")]
    [InlineData("escaped-quote")]
    [InlineData("line-break")]
    [InlineData("unicode")]
    public void The_original_maximum_decoded_field_survives_csv_syntax_without_truncation(string shape)
    {
        var value = shape switch
        {
            "plain" => new string('x', 4096),
            "separator" => new string(';', 4096),
            "escaped-quote" => new string('"', 4096),
            "line-break" => new string('x', 4094) + "\r\n",
            "unicode" => new string('\u0646', 4096),
            _ => throw new InvalidOperationException("Unknown bounded source shape."),
        };
        var rows = StarMoneyCsv.Read(BankCsvFlowTests.Fixture(count: 1, changeColumn: "Kommentar", changeValue: value));
        Assert.Equal(value, rows[1][rows[0].ToList().IndexOf("Kommentar")]);
    }

    [Fact]
    public void Every_row_at_the_original_maximum_is_retained_in_order()
    {
        var original = Csv.Read(BankCsvFlowTests.Fixture(count: 1), ';');
        var rows = StarMoneyCsv.Read(Csv.Write(new[] { original[0] }
            .Concat(Enumerable.Repeat(original[1], StarMoneyCsv.MaximumRows)), ';'));
        Assert.Equal(StarMoneyCsv.MaximumRows + 1, rows.Count);
        Assert.Equal(original[0], rows[0]);
        Assert.All(rows.Skip(1), row => Assert.Equal(original[1], row));
    }
}
