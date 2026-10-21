using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Rates;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-437: actual manual-rate inputs remain original through display changes and publication.</summary>
[Trait("AT", "AT-437")]
public sealed class ManualRateContextTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    internal static async Task<RatesViewModel> CreateAsync(FlowFixture f)
    {
        var model = new RatesViewModel(f.Store, f.Translator, new DateFormatter(f.Localization), f.Localization, f.Time, f.Platform);
        await model.LoadAsync(); model.FromCurrency = "USD"; model.ToCurrency = "EUR"; return model;
    }

    [Fact]
    public async Task Actual_manual_ratio_keeps_its_original_decimal_format_after_display_change()
    {
        using var f = new FlowFixture(); var model = await CreateAsync(f);
        f.Localization.SetFormattingCulture("de-DE"); model.RateText = "1,25";
        f.Localization.SetFormattingCulture("en-US"); await model.AddCommand.ExecuteAsync(null);
        var saved = Assert.Single(await f.Store.GetRatesAsync(Ct)); Assert.Equal(1.25m, saved.Rate);
        Assert.Equal("USD", saved.FromCurrencyCode); Assert.Equal("EUR", saved.ToCurrencyCode);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Known_manual_rate_notification_failure_cannot_rearm_the_submitted_raw_value()
    {
        using var f = new FlowFixture(); var model = await CreateAsync(f); model.RateText = "1.25";
        void Fail(object? sender, EventArgs args) => throw new IOException("Fictitious accepted rate notification");
        f.Store.Changed += Fail;
        try { await model.AddCommand.ExecuteAsync(null); } catch (IOException) { }
        finally { f.Store.Changed -= Fail; }
        Assert.Single(await f.Store.GetRatesAsync(Ct)); Assert.Equal(string.Empty, model.RateText);
        model.ToCurrency = "GBP"; await model.AddCommand.ExecuteAsync(null);
        Assert.Single(await f.Store.GetRatesAsync(Ct)); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Changed_and_returned_raw_rate_during_the_original_writer_is_refused()
    {
        using var f = new FlowFixture(); var model = await CreateAsync(f); model.RateText = "1.25";
        f.Time.BeforeRead = () => { model.RateText = "2.50"; model.RateText = "1.25"; };
        await model.AddCommand.ExecuteAsync(null); f.Time.BeforeRead = null;
        Assert.Empty(await f.Store.GetRatesAsync(Ct)); Assert.Equal("1.25", model.RateText);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
}
