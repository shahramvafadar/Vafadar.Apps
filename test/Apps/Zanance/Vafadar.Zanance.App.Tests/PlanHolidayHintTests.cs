using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-515: a weekend caption must describe the actual optional holiday behavior without denying it.</summary>
[Trait("AT", "AT-515")]
[Collection("Entry validation units")]
public sealed class PlanHolidayHintTests
{
    /// <summary>Checks the original loaded form, real holiday preview and truthful caption in the four affected languages.</summary>
    [Theory]
    [InlineData("en", "Supported public holidays are considered only when the holiday option is enabled.")]
    [InlineData("es", "Los festivos incluidos solo se tienen en cuenta si activas la opción de festivos.")]
    [InlineData("fr", "Les jours fériés pris en charge sont pris en compte uniquement si vous activez l’option correspondante.")]
    [InlineData("it", "Le festività supportate vengono considerate solo se attivi l’opzione dedicata.")]
    public async Task Weekend_hint_explains_optional_holidays_without_contradicting_the_original_preview(string language, string policy)
    {
        var ct = TestContext.Current.CancellationToken;
        using var fixture = new FlowFixture();
        fixture.Localization.SetLanguage(fixture.Localization.SupportedLanguages.Single(item => item.CultureName == language));
        fixture.Localization.SetRegion("DE");
        var account = new Account { Name = "Original holiday preview cash", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 9) };
        await fixture.Store.SaveAccountAsync(account, ct);
        var plans = fixture.Services.GetRequiredService<PlanStore>();
        var dates = new DateFormatter(fixture.Localization);
        await using var reminders = new ReminderService(new NoReminderScheduler(), fixture.Store, plans,
            fixture.Services.GetRequiredService<GoalStore>(), fixture.Services.GetRequiredService<HoldingStore>(),
            fixture.Preferences, fixture.Translator, dates, fixture.Localization, fixture.Time);
        var editor = new PlanEditorViewModel(fixture.Store, plans, fixture.Translator, fixture.Localization,
            dates, fixture.Time, reminders, fixture.Platform);
        await editor.ApplyQueryAsync(new Dictionary<string, object>());
        Assert.True(editor.Loading.IsReady);
        editor.Name = "Original holiday draft";
        editor.AmountText = "1";
        editor.Start = new(2026, 12, 25);
        editor.PresetIndex = 3;
        editor.EndIndex = 2;
        editor.CountText = "2";
        editor.WeekendIndex = 2;
        editor.SkipHolidays = true;
        Assert.True(editor.CanSkipHolidays);
        Assert.False(string.IsNullOrWhiteSpace(editor.HolidayHint));
        // Christmas and its following weekend move to Monday; only the stale caption was wrong.
        Assert.Equal(dates.Format(new DateOnly(2026, 12, 28), DateFormatStyle.Long), editor.Preview[0]);
        Assert.Contains(policy, editor.WeekendHint, StringComparison.Ordinal);
        editor.SkipHolidays = false;
        Assert.Equal(dates.Format(new DateOnly(2026, 12, 25), DateFormatStyle.Long), editor.Preview[0]);
        Assert.Contains(policy, editor.WeekendHint, StringComparison.Ordinal);
        Assert.Empty(await plans.GetSchedulesAsync(ct));
        Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: ct));
    }
}
