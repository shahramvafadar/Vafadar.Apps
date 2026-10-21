using Vafadar.Core.Text;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-437: the actual stored manual ratio is not displayed as zero or a different reviewed value.</summary>
[Trait("AT", "AT-437")]
public sealed class ManualRatePrecisionTests
{
    [Fact]
    public async Task A_small_positive_manual_rate_keeps_its_full_stored_precision_in_the_actual_list()
    {
        using var f = new FlowFixture(); var model = await ManualRateContextTests.CreateAsync(f);
        f.Localization.SetFormattingCulture("en-US"); model.RateText = "0.0000000123456789";
        await model.AddCommand.ExecuteAsync(null);
        var saved = Assert.Single(await f.Store.GetRatesAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0.0000000123456789m, saved.Rate);
        Assert.Contains("0.0000000123456789", Digits.ToAscii(Assert.Single(model.Rates).Text));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: TestContext.Current.CancellationToken));
    }
}
