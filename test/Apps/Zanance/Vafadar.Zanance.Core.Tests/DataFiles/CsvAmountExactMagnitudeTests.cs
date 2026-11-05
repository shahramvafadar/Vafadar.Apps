using Vafadar.Zanance.Core.DataFiles;
namespace Vafadar.Zanance.Core.Tests.DataFiles;

/// <summary>Signed CSV amounts must never replace extra submitted precision with a rounded currency amount.</summary>
public sealed class CsvAmountExactMagnitudeTests
{
    /// <summary>Preserves original sign/grouping/currency grammar while refusing nonzero rounded tails.</summary>
    [Theory]
    [InlineData("EUR", "1.00000000000000000000000000001", '.', false, 0L)]
    [InlineData("EUR", "-1.00000000000000000000000000001", '.', false, 0L)]
    [InlineData("EUR", "(1.00000000000000000000000000001)", '.', false, 0L)]
    [InlineData("EUR", "1.00000000000000000000000000001-", '.', false, 0L)]
    [InlineData("EUR", "1,00000000000000000000000000001", ',', false, 0L)]
    [InlineData("EUR", "0.010000000000000000000000000001", '.', false, 0L)]
    [InlineData("JPY", "1.00000000000000000000000000001", '.', false, 0L)]
    [InlineData("KWD", "0.001000000000000000000000000001", '.', false, 0L)]
    [InlineData("EUR", "1,234.56000000000000000000000000001", '.', false, 0L)]
    [InlineData("EUR", "1.2300000000000000000000000000", '.', true, 123L)]
    [InlineData("EUR", "1.2300000000000000000000000000-", '.', true, -123L)]
    [InlineData("EUR", "1.234,5600000000000000000000000000", ',', true, 123456L)]
    [InlineData("JPY", "1.0000000000000000000000000000", '.', true, 1L)]
    [InlineData("KWD", "0.001000000000000000000000000000", '.', true, 1L)]
    public void Submitted_magnitude_is_preserved(string currency, string raw, char separator, bool expected, long minor)
    {
        Assert.Equal(expected, CsvImport.TryAmount(raw, currency, separator, out var actual));
        Assert.Equal(minor, actual);
    }
}
