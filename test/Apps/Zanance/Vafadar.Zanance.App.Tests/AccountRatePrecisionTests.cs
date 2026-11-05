using System.Globalization;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.Core.Accounts;

namespace Vafadar.Zanance.App.Tests;

/// <summary>D-843: account metadata edits preserve exact decimal rates without intermediate rounding.</summary>
[Collection("Entry validation units")]
public sealed class AccountRatePrecisionTests
{
    /// <summary>Provides independent original cultures and rate boundaries, including stored decimal scale.</summary>
    public static TheoryData<string, string> LoadedRates
    {
        get
        {
            var data = new TheoryData<string, string>();
            foreach (var culture in new[] { "en-US", "de-DE", "fa-IR", "es-ES", "fr-FR", "it-IT" })
                foreach (var rate in new[] { "0.9999999999999999999999999999", "0.0000000000000000000000000001", "12.345678901234567890123456780" })
                    data.Add(culture, rate);
            return data;
        }
    }

    /// <summary>Loading and applying only a name change keeps the original value and its decimal scale.</summary>
    [Theory]
    [MemberData(nameof(LoadedRates))]
    public void Name_only_edit_preserves_loaded_rate(string cultureName, string raw)
    {
        using var f = new FlowFixture();
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var rate = decimal.Parse(raw, CultureInfo.InvariantCulture);
        var account = new Account { Name = "Original rate", Type = AccountType.Loan, CurrencyCode = "EUR",
            OpeningBalance = -600, OpeningDate = new DateOnly(2026, 11, 5), InterestRate = rate, Installment = 600 };
        var form = new AccountFormModel(f.Translator, f.Time, f.Localization);
        form.Load(account, culture, AccountCurrencyLock.None);
        form.Name = "Renamed rate";
        Assert.Equal(rate.ToString(culture), form.RateText);
        Assert.True(form.TryApply(account, culture));
        Assert.Equal(decimal.GetBits(rate), decimal.GetBits(account.InterestRate!.Value));
        Assert.Equal(-600, account.OpeningBalance);
    }

    /// <summary>Decimal parsing may round before a range check; an inexact value must never be committed.</summary>
    [Theory]
    [InlineData("100.00000000000000000000000001")]
    [InlineData("0.12345678901234567890123456789")]
    [InlineData("0.00000000000000000000000000001")]
    [InlineData("99.999999999999999999999999999")]
    [InlineData("1.23456789012345678901234567891")]
    public void Inexact_rate_is_refused_before_any_target_mutation(string raw)
    {
        using var f = new FlowFixture(); var culture = CultureInfo.GetCultureInfo("en-US");
        var account = new Account { Name = "Original", Type = AccountType.Loan, CurrencyCode = "EUR",
            OpeningDate = new DateOnly(2026, 11, 5), InterestRate = 1.25m };
        var form = new AccountFormModel(f.Translator, f.Time, f.Localization);
        form.Load(account, culture, AccountCurrencyLock.None); form.Name = "Must not apply"; form.RateText = raw;
        Assert.False(form.TryApply(account, culture));
        Assert.NotNull(form.TermsError); Assert.Equal(raw, form.RateText);
        Assert.Equal("Original", account.Name); Assert.Equal(1.25m, account.InterestRate);
    }

    /// <summary>Exact redundant zeros, culture separators, digit scripts and percentage suffixes remain supported.</summary>
    [Theory]
    [InlineData("en-US", "0001.2500000000000000000000000000%", "1.25")]
    [InlineData("de-DE", "1,2500", "1.25")]
    [InlineData("fa-IR", "۱٫۲۵٪", "1.25")]
    [InlineData("en-US", ".25", "0.25")]
    [InlineData("en-US", "100.0000000000000000000000000000", "100")]
    [InlineData("en-US", "0.0000000000000000000000000000", "0")]
    public void Exact_existing_input_syntax_remains_accepted(string cultureName, string raw, string expected)
    {
        using var f = new FlowFixture(); var culture = CultureInfo.GetCultureInfo(cultureName);
        f.Localization.SetFormattingCulture(cultureName);
        var form = new AccountFormModel(f.Translator, f.Time, f.Localization)
            { Name = "Exact rate", TypeIndex = (int)AccountType.Lent, RateText = raw };
        var account = new Account { Name = "Unchanged", CurrencyCode = "EUR" };
        Assert.True(form.TryApply(account, culture));
        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), account.InterestRate);
    }
}
