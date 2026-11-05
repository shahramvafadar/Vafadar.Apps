using System.Globalization;
using System.Text.Json;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original conflict choices display exact wire rates and refuse rounding before consent.</summary>
public sealed class SyncExactDecimalReviewTests
{
    /// <summary>Combines each exact or inexact wire value with every supported display language.</summary>
    public static IEnumerable<object[]> Cases =>
        from language in new[] { "en", "fa", "de", "es", "fr", "it" }
        from sample in new[] { ("1e-28", true), ("1.2345e2", true),
            ("0.12345678901234567890123456789", false), ("79228162514264337593543950335.1", false) }
        select new object[] { language, sample.Item1, sample.Item2 };

    /// <summary>Complete original conflict cards never authorize a rounded substitute for the submitted rate.</summary>
    [Theory]
    [MemberData(nameof(Cases))]
    public void Original_complete_rate_review_keeps_exact_magnitude_before_any_choice(string language, string raw, bool valid)
    {
        using var f = new FlowFixture();
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.First(value => value.CultureName == language));
        var id = Guid.NewGuid().ToString("D").ToUpperInvariant();
        var body = JsonSerializer.SerializeToElement(new { Id = id, CreatedAt = 0L, Date = "2026-11-05",
            FromCurrencyCode = "EUR", ToCurrencyCode = "USD", IsEstimate = 0L, Rate = raw });
        var key = new SyncRecordKey("ExchangeRates", JsonSerializer.Serialize(new[] { id }));
        var record = new SyncRecord(key, 1, body);
        var conflict = new SyncConflict(new(key, 0, body), null, record, ["Rate"], "CompetingChanges");
        var review = new PersonalSyncConflictReview(new("fictitious-unopened-profile", null, null, null),
            Guid.NewGuid(), 1, 0, new string('1', 64), conflict, [record]);
        var before = body.GetRawText();
        var presenter = new PersonalSyncReviewPresenter(f.Translator, new DateFormatter(f.Localization), f.Localization);
        var item = presenter.Create(review, []);
        Assert.Equal(valid, item.CanChooseLocal); Assert.Equal(valid, item.CanChooseRemote);
        if (valid)
        {
            var expected = NativeDigits.Apply(decimal.Parse(raw, NumberStyles.Float, CultureInfo.InvariantCulture)
                .ToString(f.Localization.CurrentCulture))!;
            Assert.Contains(item.Versions[0].Fields, field => field.Value.Contains(expected, StringComparison.Ordinal)
                && field.Value.Contains("EUR", StringComparison.Ordinal) && field.Value.Contains("USD", StringComparison.Ordinal));
        }
        else Assert.Equal(f.Translator["Sync_ReviewIncomplete"], item.Hint);
        Assert.Equal(before, body.GetRawText());
    }
}
