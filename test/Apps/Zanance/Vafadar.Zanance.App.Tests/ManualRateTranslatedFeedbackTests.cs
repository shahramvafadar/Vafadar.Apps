using Vafadar.Zanance.Core.Rates;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-437: actual retained outcomes and original rate-version notes translate without write replay.</summary>
[Trait("AT", "AT-437")]
public sealed class ManualRateTranslatedFeedbackTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    [Fact]
    public async Task Accepted_rate_feedback_changes_language_without_resubmitting_the_original_value()
    {
        using var f = new FlowFixture(); var model = await ManualRateContextTests.CreateAsync(f); model.SetPresentationActive(true); model.RateText = "1.25";
        void Fail(object? sender, EventArgs args) => throw new IOException("Fictitious known notification refusal");
        f.Store.Changed += Fail; await model.AddCommand.ExecuteAsync(null); f.Store.Changed -= Fail;
        var accepted = Assert.Single(await f.Store.GetRatesAsync(Ct)); Assert.Equal(f.Translator["Rates_SavedFollowUpFailed"], model.Error);
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(row => row.CultureName == "de"));
        Assert.Equal(f.Translator["Rates_SavedFollowUpFailed"], model.Error); Assert.Equal("", model.RateText);
        Assert.Equal(accepted.Id, Assert.Single(await f.Store.GetRatesAsync(Ct)).Id); Assert.False(model.CanAdd);
        model.SetPresentationActive(false);
    }
    [Fact]
    public async Task Reloading_newer_rates_does_not_renew_a_nonempty_original_raw_review()
    {
        using var f = new FlowFixture(); var model = await ManualRateContextTests.CreateAsync(f); model.RateText = "1.25";
        await f.Store.SaveRateAsync(new ExchangeRate { Date = model.Date, FromCurrencyCode = "USD", ToCurrencyCode = "EUR", Rate = 2.50m }, Ct);
        await model.LoadAsync(); Assert.Equal("1.25", model.RateText); Assert.NotNull(model.RateContextError);
        await model.AddCommand.ExecuteAsync(null); Assert.Equal(2.50m, Assert.Single(await f.Store.GetRatesAsync(Ct)).Rate);
        model.RateText = ""; model.RateText = "1.50"; await model.AddCommand.ExecuteAsync(null);
        Assert.Equal(1.50m, Assert.Single(await f.Store.GetRatesAsync(Ct)).Rate); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
}
