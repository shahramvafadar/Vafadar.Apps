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

/// <summary>Language-only refresh retains invalid original drafts and retranslates existing field validation without another Save.</summary>
[Collection("Entry validation units")]
public sealed class GoalValidationLanguageTests
{
    private sealed class Notifications : IGoalEditorNotifications
    {
        public bool IsSupported => false;
        public Task<bool> EnableAsync() => throw new InvalidOperationException("No permission request allowed.");
    }
    /// <summary>Uses each supported target language with both independently validated contribution methods.</summary>
    public static IEnumerable<object[]> Cases => from language in new[] { "en", "fa", "de", "es", "fr", "it" }
        from share in new[] { false, true } select new object[] { language, share };

    /// <summary>Only initial rejected validation executes; changing language cannot write, clear or reinterpret any input.</summary>
    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Existing_validation_captions_follow_language_without_replaying_Save(string language, bool share)
    {
        var ct = TestContext.Current.CancellationToken;
        using var f = new FlowFixture();
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(value => value.CultureName == (language == "en" ? "de" : "en")));
        f.Localization.SetFormattingCulture("en-US");
        await f.Store.GetSettingsAsync(ct);
        var goal = new Goal { Name = "Fictitious retained validation", Type = GoalType.Earmark, CurrencyCode = "EUR", TargetAmount = 1000 };
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct))
        {
            (await db.Settings.SingleAsync(ct)).Mode = ExperienceMode.Advanced;
            db.Goals.Add(goal); db.ContributionPlans.Add(new ContributionPlan { GoalId = goal.Id,
                Method = share ? ContributionMethod.ShareOfIncome : ContributionMethod.FixedAmount, Amount = share ? null : 100,
                Percent = share ? 1.25m : null, Rule = new() { Frequency = Frequency.Monthly, Start = new(2026, 10, 1), End = EndKind.OnDate, EndDate = new(2026, 10, 31) } });
            await db.SaveChangesAsync(ct);
        }
        var dates = new DateFormatter(f.Localization);
        var editor = new GoalEditorViewModel(f.Store, f.Services.GetRequiredService<GoalStore>(), f.Translator, f.Localization, dates, f.Time,
            f.Services.GetRequiredService<HoldingStore>(), new HoldingText(f.Translator, f.Localization, dates), f.Platform, new Notifications());
        await editor.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = goal.Id });
        Assert.True(editor.IsReady);
        editor.Name = " "; editor.AmountText = "invalid retained target"; editor.FirstDate = new(2026, 11, 5);
        if (share) editor.PercentText = "100.000000000000000000000000001";
        else editor.ContributionText = "invalid retained contribution";
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await editor.SaveCommand.ExecuteAsync(null);
        Assert.NotNull(editor.NameError); Assert.NotNull(editor.AmountError); Assert.NotNull(editor.RuleError);
        Assert.NotNull(share ? editor.PercentError : editor.ContributionError);
        var draft = (editor.Name, editor.AmountText, editor.ContributionText, editor.PercentText, editor.FirstDate);
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(value => value.CultureName == language));
        editor.RefreshDisplay();
        Assert.Equal(f.Translator["Goal_NameRequired"], editor.NameError);
        Assert.Equal(f.Translator["Amount_Invalid"], editor.AmountError);
        Assert.Equal(f.Translator["Plan_RuleInvalid"], editor.RuleError);
        Assert.Equal(f.Translator[share ? "Goal_PercentInvalid" : "Amount_Invalid"], share ? editor.PercentError : editor.ContributionError);
        Assert.Equal(draft, (editor.Name, editor.AmountText, editor.ContributionText, editor.PercentText, editor.FirstDate));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: ct)); Assert.Empty(f.Platform.Routes);
    }
}
