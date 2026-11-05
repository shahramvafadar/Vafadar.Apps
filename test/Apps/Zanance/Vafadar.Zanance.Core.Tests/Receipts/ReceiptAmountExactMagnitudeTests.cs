using System.Globalization;
using Vafadar.Zanance.Core.Receipts;
namespace Vafadar.Zanance.Core.Tests.Receipts;

/// <summary>The original recognized-number reader must preserve the exact submitted magnitude before total policy.</summary>
public sealed class ReceiptAmountExactMagnitudeTests
{
    /// <summary>Rejects fixed-point text that decimal parsing accepts only by discarding a nonzero digit.</summary>
    [Theory]
    [InlineData("7922816251426433759354395033.51")]
    [InlineData("7922816251426433759354395033,51")]
    [InlineData("1234567890123456789012345678.91")]
    [InlineData("1,234,567,890,123,456,789,012,345,678.91")]
    public void A_rounded_number_is_not_an_exact_recognized_amount(string raw)
    {
        Assert.False(ReceiptParser.TryAmount(raw, out var amount));
        Assert.Equal(0m, amount);
    }

    /// <summary>Retains exactly representable large decimals and the existing separate oversized-total refusal.</summary>
    [Theory]
    [InlineData("7922816251426433759354395033.50", "7922816251426433759354395033.5")]
    [InlineData("1234567890123456789012345678.90", "1234567890123456789012345678.9")]
    [InlineData("79228162514264337593543950335", "79228162514264337593543950335")]
    public void Exact_reader_value_does_not_override_the_total_size_policy(string raw, string expected)
    {
        Assert.True(ReceiptParser.TryAmount(raw, out var amount));
        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), amount);
        var row = "TOTAL " + raw + " EUR";
        var receipt = ReceiptParser.Parse("NORTH HARBOR MARKET\n" + row, new DateOnly(2026, 11, 5));
        Assert.Null(receipt.Amount); Assert.Equal(ReceiptAmountStatus.Review, receipt.AmountStatus);
        Assert.Empty(receipt.Candidates); Assert.Equal(row, receipt.AmountSource);
    }
}
