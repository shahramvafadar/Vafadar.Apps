using System.Text.Json;
using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.Core.Tests;

/// <summary>Received decimal text must retain its exact magnitude before any merge or financial validation.</summary>
public sealed class SyncExactDecimalAdmissionTests
{
    /// <summary>Scalar admission checks exact value independently of its accepted invariant spelling.</summary>
    [Theory]
    [InlineData("0.12345678901234567890123456789", false)]
    [InlineData("-0.12345678901234567890123456789", false)]
    [InlineData("100.000000000000000000000000001", false)]
    [InlineData("79228162514264337593543950335.1", false)]
    [InlineData("1e-29", false)]
    [InlineData("-1e-29", false)]
    [InlineData("1.00000000000000000000000000001e-1", false)]
    [InlineData("99999999999999999999999999999e-29", false)]
    [InlineData("1e-28", true)]
    [InlineData("-1e-28", true)]
    [InlineData("0.1234567890123456789012345678", true)]
    [InlineData("79228162514264337593543950335", true)]
    [InlineData("-79228162514264337593543950335", true)]
    [InlineData("1.2345000000000000000000000000000000", true)]
    [InlineData("  +001.2500e+2  ", true)]
    [InlineData("-0.0000000000000000000000000000000000", true)]
    [InlineData("0e-9999", true)]
    [InlineData("1,25", false)]
    [InlineData("NaN", false)]
    [InlineData("1e29", false)]
    public void Complete_received_row_refuses_rounding_and_preserves_existing_float_syntax(string raw, bool valid)
    {
        var table = new SyncTable("DecimalBoundary", false,
            [new("Id", "integer", false, null, null, false), new("Value", "decimal", false, null, null, false)],
            ["Id"], [], []);
        var key = new SyncRecordKey(table.Name, "[1]");
        var row = JsonSerializer.SerializeToElement(new { Id = 1L, Value = raw });
        var before = row.GetRawText();
        var issues = SyncGraphValidation.ValidateRow(table, key, row);
        if (valid) Assert.Empty(issues);
        else
        {
            var issue = Assert.Single(issues);
            Assert.Equal("InvalidScalar", issue.Reason);
            Assert.Equal(new[] { "Value" }, issue.Fields);
        }
        Assert.Equal(before, row.GetRawText());
    }
}
