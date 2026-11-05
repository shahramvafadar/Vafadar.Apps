using System.Globalization;
using Vafadar.Zanance.App.Features.Rates;

namespace Vafadar.Zanance.App.Tests;

/// <summary>D-844: the original manual-rate command must not persist a rounded version of entered text.</summary>
public sealed class ManualRateExactInputTests
{
    /// <summary>Pairs six independent input cultures with four inexact positives and a tiny refusal control.</summary>
    public static TheoryData<string, string> InexactRates
    {
        get
        {
            var data = new TheoryData<string, string>();
            foreach (var culture in new[] { "en-US", "de-DE", "fa-IR", "es-ES", "fr-FR", "it-IT" })
                foreach (var raw in new[] { "0.12345678901234567890123456789", "1.23456789012345678901234567891",
                    "0.99999999999999999999999999999", "79228162514264337593543950335.1", "0.00000000000000000000000000001" })
                    data.Add(culture, raw);
            return data;
        }
    }

    /// <summary>Rejects inexact input inline, retaining the raw draft and leaving every rate/ledger unchanged.</summary>
    [Theory]
    [MemberData(nameof(InexactRates))]
    public async Task Original_command_refuses_inexact_rate_without_source_mutation(string cultureName, string invariantRaw)
    {
        using var f = new FlowFixture(); f.Localization.SetFormattingCulture(cultureName);
        var model = await ManualRateContextTests.CreateAsync(f);
        var raw = invariantRaw.Replace(".", CultureInfo.GetCultureInfo(cultureName).NumberFormat.NumberDecimalSeparator, StringComparison.Ordinal);
        model.RateText = raw;
        await model.AddCommand.ExecuteAsync(null);
        Assert.Empty(await f.Store.GetRatesAsync(TestContext.Current.CancellationToken));
        Assert.Equal(raw, model.RateText); Assert.NotNull(model.RateError);
        Assert.True(model.Loading.IsReady); Assert.Null(model.Error); Assert.Null(model.PairError);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: TestContext.Current.CancellationToken));
    }

    /// <summary>Preserves exact tiny/large ratios and redundant zeros without adding an account-rate percentage cap.</summary>
    [Theory]
    [InlineData("en-US", "0.0000000000000000000000000001", "0.0000000000000000000000000001")]
    [InlineData("de-DE", "0,1234567890123456789012345678", "0.1234567890123456789012345678")]
    [InlineData("fa-IR", "۱٫۲۵", "1.25")]
    [InlineData("en-US", "1000.1234567890123456789012345", "1000.1234567890123456789012345")]
    [InlineData("en-US", "79228162514264337593543950335", "79228162514264337593543950335")]
    [InlineData("en-US", "0001.2500000000000000000000000000", "1.25")]
    [InlineData("fr-FR", ",25", "0.25")]
    [InlineData("it-IT", "1,00000000000000000000000000000", "1")]
    public async Task Exact_positive_rate_keeps_its_original_value(string cultureName, string raw, string expected)
    {
        using var f = new FlowFixture(); f.Localization.SetFormattingCulture(cultureName);
        var model = await ManualRateContextTests.CreateAsync(f); model.RateText = raw;
        await model.AddCommand.ExecuteAsync(null);
        var saved = Assert.Single(await f.Store.GetRatesAsync(TestContext.Current.CancellationToken));
        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), saved.Rate);
        Assert.Equal("USD", saved.FromCurrencyCode); Assert.Equal("EUR", saved.ToCurrencyCode);
        Assert.Equal(string.Empty, model.RateText); Assert.Null(model.RateError);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: TestContext.Current.CancellationToken));
    }
    /// <summary>Explains precision in the actual retained error category without replay on a display change.</summary>
    [Theory]
    [InlineData("en", "Enter a positive rate that can be stored without rounding.")]
    [InlineData("fa", "نرخ مثبتی وارد کنید که بتوان آن را دقیق و بدون گرد کردن ذخیره کرد.")]
    [InlineData("de", "Geben Sie einen positiven Kurs ein, der ohne Rundung gespeichert werden kann.")]
    [InlineData("es", "Introduce un tipo de cambio positivo que se pueda guardar sin redondear.")]
    [InlineData("fr", "Saisissez un taux positif qui puisse être enregistré sans arrondi.")]
    [InlineData("it", "Inserisci un tasso positivo che possa essere salvato senza arrotondamenti.")]
    public async Task Current_caption_explains_exact_storage_in_each_language(string language, string expected)
    {
        using var f = new FlowFixture();
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(row => row.CultureName == language));
        f.Localization.SetFormattingCulture("en-US");
        var model = await ManualRateContextTests.CreateAsync(f);
        const string raw = "0.12345678901234567890123456789";
        model.RateText = raw; await model.AddCommand.ExecuteAsync(null);
        Assert.Equal(expected, model.RateError);
        f.Localization.SetFormattingCulture("de-DE");
        Assert.Equal(expected, model.RateError); Assert.Equal(raw, model.RateText);
        Assert.Empty(await f.Store.GetRatesAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: TestContext.Current.CancellationToken));
    }

}
