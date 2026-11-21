using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Analysis;
using Vafadar.Zanance.App.Features.Goals;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Exercises goal funding after the original AI shortcut is retired.</summary>
[Collection("Classical feature preference")]
public sealed class AiGoalShortcutTests
{
    /// <summary>A retired goal shortcut cannot show its late error or block a fresh analysis.</summary>
    [Fact]
    public async Task Reenabled_AI_discards_the_old_goal_shortcut_failure_and_allows_one_fresh_shortcut()
    {
        using var fixture = new FlowFixture();
        var cancellation = TestContext.Current.CancellationToken;
        await fixture.Store.SaveAccountAsync(new Account
        {
            Name = "Synthetic funding account", CurrencyCode = "EUR", OpeningDate = new(2026, 9, 1)
        }, cancellation);
        var goals = fixture.Services.GetRequiredService<GoalStore>();
        var goal = new Goal { Name = "Synthetic funding target", CurrencyCode = "EUR", TargetAmount = 10000 };
        await goals.SaveGoalAsync(goal, cancellation);
        var dates = new DateFormatter(fixture.Localization);
        var holdings = fixture.Services.GetRequiredService<HoldingStore>();
        var holdingText = new HoldingText(fixture.Translator, fixture.Localization, dates);
        var presenter = new GoalPresenter(fixture.Translator, dates, fixture.Localization, holdings, holdingText);
        var model = new GoalDetailViewModel(fixture.Store, goals, fixture.Services.GetRequiredService<PlanStore>(),
            holdings, presenter, fixture.Translator, dates, fixture.Localization, fixture.Time, fixture.Platform);
        model.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = goal.Id });
        await model.LoadAsync();
        Assert.True(model.CanInteract);
        Assert.True(model.HasFundingAnalysis);
        var originalRows = await GoalChooserFlowTests.SnapshotAsync(fixture);

        var originalPreference = AiFeatures.Current;
        try
        {
            var preference = new AiFeatures(true, _ => { });
            AiFeatures.Current = preference;
            var originalSession = preference.Capture();
            var navigationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var navigation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            fixture.Platform.PendingNavigation = () => { navigationStarted.SetResult(); return navigation.Task; };
            var command = model.ExplainFundingCommand.ExecuteAsync(null);
            await Task.WhenAny(navigationStarted.Task, command).WaitAsync(cancellation);
            Assert.True(navigationStarted.Task.IsCompletedSuccessfully);
            preference.Enabled = false;
            preference.Enabled = true;
            navigation.SetException(new InvalidOperationException("Synthetic goal navigation failure."));
            await command;

            var retiredRoute = Assert.Single(fixture.Platform.Routes);
            Assert.Equal("analysis", retiredRoute.Route);
            Assert.Equal(originalSession, Assert.IsType<AiPeriodHandoff>(
                retiredRoute.Parameters![AiPeriodHandoff.QueryKey]).FeatureSession);
            Assert.True(originalSession.IsCancellationRequested);
            Assert.Empty(fixture.Platform.Failures);
            Assert.True(model.CanInteract);

            fixture.Platform.PendingNavigation = null;
            await model.ExplainFundingCommand.ExecuteAsync(null);
            Assert.Equal(2, fixture.Platform.Routes.Count);
            var freshRoute = fixture.Platform.Routes[1];
            Assert.Equal("analysis", freshRoute.Route);
            var fresh = Assert.IsType<AiPeriodHandoff>(freshRoute.Parameters![AiPeriodHandoff.QueryKey]);
            Assert.Equal(preference.Capture(), fresh.FeatureSession);
            Assert.NotNull(fresh.Funding);
            Assert.Empty(fixture.Platform.Failures);
            Assert.Equal(originalRows, await GoalChooserFlowTests.SnapshotAsync(fixture));
        }
        finally { AiFeatures.Current = originalPreference; }
    }
}
