using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Goals;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>D-845: original goal percentage drafts retain exact value and original numeric meaning.</summary>
[Collection("Entry validation units")]
public sealed class GoalPercentExactInputTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class Notifications : IGoalEditorNotifications
    {
        public bool IsSupported => false;
        public Task<bool> EnableAsync() => throw new InvalidOperationException("Percent validation cannot request permission.");
    }
    private static async Task<(GoalEditorViewModel Editor, GoalStore Store, Goal Goal)> OpenAsync(FlowFixture f, decimal percent)
    {
        await f.Store.GetSettingsAsync(Ct);
        var goal = new Goal { Name = "Original exact percent", Type = GoalType.Earmark, CurrencyCode = "EUR", TargetAmount = 1000 };
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        {
            (await db.Settings.SingleAsync(Ct)).Mode = ExperienceMode.Advanced;
            db.Goals.Add(goal); db.ContributionPlans.Add(new ContributionPlan { GoalId = goal.Id, Method = ContributionMethod.ShareOfIncome,
                Percent = percent, Rule = new() { Frequency = Frequency.Monthly, Start = new(2026, 10, 1) } });
            await db.SaveChangesAsync(Ct);
        }
        var store = f.Services.GetRequiredService<GoalStore>(); var dates = new DateFormatter(f.Localization);
        var editor = new GoalEditorViewModel(f.Store, store, f.Translator, f.Localization, dates, f.Time,
            f.Services.GetRequiredService<HoldingStore>(), new HoldingText(f.Translator, f.Localization, dates), f.Platform, new Notifications());
        await editor.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = goal.Id });
        Assert.True(editor.IsReady); Assert.True(editor.CanSave);
        return (editor, store, goal);
    }
    private static async Task<decimal?> PercentAsync(GoalStore store) => Assert.Single(await store.GetContributionPlansAsync(Ct)).Percent;

    /// <summary>Provides all six cultures and two stored values that must never be rounded on display.</summary>
    public static TheoryData<string, string> LoadedRates
    {
        get
        {
            var data = new TheoryData<string, string>();
            foreach (var culture in new[] { "en-US", "de-DE", "fa-IR", "es-ES", "fr-FR", "it-IT" })
                foreach (var raw in new[] { "0.9999999999999999999999999999", "0.0000000000000000000000000001" })
                    data.Add(culture, raw);
            return data;
        }
    }
    /// <summary>Opening the original editor displays every stored decimal digit without an implicit write.</summary>
    [Theory]
    [MemberData(nameof(LoadedRates))]
    public async Task Loaded_share_percent_keeps_complete_original_value(string cultureName, string raw)
    {
        using var f = new FlowFixture(); f.Localization.SetFormattingCulture(cultureName);
        var value = decimal.Parse(raw, CultureInfo.InvariantCulture); var s = await OpenAsync(f, value);
        Assert.Equal(value.ToString(CultureInfo.GetCultureInfo(cultureName)), s.Editor.PercentText);
        Assert.Equal(value, await PercentAsync(s.Store)); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
    /// <summary>A tiny retained valid percent cannot block an unrelated metadata name correction.</summary>
    [Fact]
    public async Task Tiny_existing_percent_does_not_block_name_only_metadata_save()
    {
        using var f = new FlowFixture(); var s = await OpenAsync(f, 0.0000000000000000000000000001m);
        s.Editor.Name = "Name-only correction"; await s.Editor.SaveCommand.ExecuteAsync(null);
        Assert.Equal("Name-only correction", (await s.Store.GetGoalAsync(s.Goal.Id, Ct))!.Name);
        Assert.Equal(0.0000000000000000000000000001m, await PercentAsync(s.Store));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
    /// <summary>Provides three different separator contexts and inexact decimal values, including a tiny control.</summary>
    public static TheoryData<string, string> InexactRates
    {
        get
        {
            var data = new TheoryData<string, string>();
            foreach (var culture in new[] { "en-US", "de-DE", "fa-IR" })
                foreach (var raw in new[] { "100.00000000000000000000000001", "0.12345678901234567890123456789", "0.99999999999999999999999999999", "0.00000000000000000000000000001" })
                    data.Add(culture, raw);
            return data;
        }
    }
    /// <summary>A rejected new percent leaves the exact stored goal and plan unchanged and retains raw text.</summary>
    [Theory]
    [MemberData(nameof(InexactRates))]
    public async Task Inexact_percent_cannot_reach_original_goal_writer(string cultureName, string raw)
    {
        using var f = new FlowFixture(); f.Localization.SetFormattingCulture(cultureName); var s = await OpenAsync(f, 1.25m);
        var before = JsonSerializer.Serialize(await s.Store.GetContributionPlansAsync(Ct));
        var beforeGoal = JsonSerializer.Serialize(await s.Store.GetGoalAsync(s.Goal.Id, Ct));
        var text = raw.Replace(".", CultureInfo.GetCultureInfo(cultureName).NumberFormat.NumberDecimalSeparator, StringComparison.Ordinal);
        s.Editor.PercentText = text; await s.Editor.SaveCommand.ExecuteAsync(null);
        Assert.NotNull(s.Editor.PercentError); Assert.Equal(text, s.Editor.PercentText);
        Assert.Equal(before, JsonSerializer.Serialize(await s.Store.GetContributionPlansAsync(Ct)));
        Assert.Equal(beforeGoal, JsonSerializer.Serialize(await s.Store.GetGoalAsync(s.Goal.Id, Ct)));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
    /// <summary>Appending a digit after a display change preserves the original decimal separator.</summary>
    [Fact]
    public async Task Partial_percent_edit_keeps_original_culture()
    {
        using var f = new FlowFixture(); f.Localization.SetFormattingCulture("de-DE"); var s = await OpenAsync(f, 0.5m);
        f.Localization.SetFormattingCulture("en-US"); s.Editor.RefreshDisplay(); s.Editor.PercentText += "0";
        await s.Editor.SaveCommand.ExecuteAsync(null);
        Assert.Equal(0.5m, await PercentAsync(s.Store)); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
    /// <summary>Complete clearing renews only the percent's original culture before explicit new typing.</summary>
    [Fact]
    public async Task Complete_clearing_renews_percent_culture()
    {
        using var f = new FlowFixture(); f.Localization.SetFormattingCulture("de-DE"); var s = await OpenAsync(f, 0.5m);
        f.Localization.SetFormattingCulture("en-US"); s.Editor.RefreshDisplay(); s.Editor.PercentText = ""; s.Editor.PercentText = "0.25";
        await s.Editor.SaveCommand.ExecuteAsync(null); Assert.Equal(0.25m, await PercentAsync(s.Store));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
    /// <summary>Preserves the original positive sign/grouping syntax and supported percentage digit scripts.</summary>
    [Theory]
    [InlineData("en-US", "+1.5", "1.5")]
    [InlineData("en-US", "1.5+", "1.5")]
    [InlineData("en-US", "0,50", "50")]
    [InlineData("de-DE", "0.50", "50")]
    [InlineData("fr-FR", "0\u202F50", "50")]
    [InlineData("en-US", ".25%", "0.25")]
    [InlineData("fa-IR", "۰٫۵٪", "0.5")]
    public async Task Exact_original_syntax_keeps_its_meaning(string culture, string raw, string expected)
    {
        using var f = new FlowFixture(); f.Localization.SetFormattingCulture(culture); var s = await OpenAsync(f, 1.25m);
        s.Editor.PercentText = raw; await s.Editor.SaveCommand.ExecuteAsync(null);
        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), await PercentAsync(s.Store));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
    /// <summary>One more fractional digit beyond the exact 100+epsilon control really rounds down in the framework.</summary>
    [Theory]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    [InlineData("fa-IR")]
    public async Task Rounded_above_maximum_is_refused_exactly(string cultureName)
    {
        var invariant = "100." + new string('0', 26) + "1";
        Assert.True(decimal.TryParse(invariant, NumberStyles.Number, CultureInfo.InvariantCulture, out var rounded));
        Assert.Equal(100m, rounded);
        using var f = new FlowFixture(); f.Localization.SetFormattingCulture(cultureName); var s = await OpenAsync(f, 1.25m);
        var before = JsonSerializer.Serialize(await s.Store.GetContributionPlansAsync(Ct));
        s.Editor.PercentText = invariant.Replace(".", f.Localization.CurrentCulture.NumberFormat.NumberDecimalSeparator, StringComparison.Ordinal);
        await s.Editor.SaveCommand.ExecuteAsync(null);
        Assert.NotNull(s.Editor.PercentError); Assert.Equal(before, JsonSerializer.Serialize(await s.Store.GetContributionPlansAsync(Ct)));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    /// <summary>The retained original culture is visible until this field is completely cleared.</summary>
    [Fact]
    public async Task Original_percent_format_note_tracks_partial_edit_and_clearing()
    {
        using var f = new FlowFixture(); f.Localization.SetFormattingCulture("de-DE"); var s = await OpenAsync(f, 0.5m);
        Assert.Null(s.Editor.PercentFormatNote); f.Localization.SetFormattingCulture("en-US"); s.Editor.RefreshDisplay();
        Assert.Contains("1,25", s.Editor.PercentFormatNote); s.Editor.PercentText += "0";
        Assert.Contains("1,25", s.Editor.PercentFormatNote); s.Editor.PercentText = ""; s.Editor.PercentText = "0.25";
        Assert.Null(s.Editor.PercentFormatNote); Assert.Equal(0.5m, await PercentAsync(s.Store));
    }
    /// <summary>The actual inline error explains precision in every language without a financial command.</summary>
    [Theory]
    [InlineData("en", "Enter a percentage above 0 and up to 100 that can be stored without rounding.")]
    [InlineData("fa", "درصدی بیشتر از صفر و حداکثر صد وارد کنید که بدون گرد کردن قابل ذخیره باشد.")]
    [InlineData("de", "Geben Sie einen Prozentsatz über 0 bis höchstens 100 ein, der ohne Rundung gespeichert werden kann.")]
    [InlineData("es", "Introduce un porcentaje mayor que 0 y de hasta 100 que se pueda guardar sin redondear.")]
    [InlineData("fr", "Saisissez un pourcentage supérieur à 0 et au plus égal à 100, enregistrable sans arrondi.")]
    [InlineData("it", "Inserisci una percentuale maggiore di 0 e fino a 100 che possa essere salvata senza arrotondamenti.")]
    public async Task Exact_storage_caption_is_translated(string language, string expected)
    {
        using var f = new FlowFixture(); f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(row => row.CultureName == language));
        f.Localization.SetFormattingCulture("en-US"); var s = await OpenAsync(f, 1.25m);
        s.Editor.PercentText = "100." + new string('0', 26) + "1"; await s.Editor.SaveCommand.ExecuteAsync(null);
        Assert.Equal(expected, s.Editor.PercentError); Assert.Equal(1.25m, await PercentAsync(s.Store));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    /// <summary>Uses the actual persisted decimal scale, while proving its value still equals the original seed.</summary>
    [Theory]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    [InlineData("fa-IR")]
    [InlineData("es-ES")]
    [InlineData("fr-FR")]
    [InlineData("it-IT")]
    public async Task Loaded_decimal_text_matches_actual_stored_source(string cultureName)
    {
        const decimal seed = 12.345678901234567890123456780m;
        using var f = new FlowFixture(); f.Localization.SetFormattingCulture(cultureName); var s = await OpenAsync(f, seed);
        var stored = (await PercentAsync(s.Store))!.Value;
        Assert.Equal(seed, stored);
        Assert.Equal(stored.ToString(f.Localization.CurrentCulture), s.Editor.PercentText);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

}
