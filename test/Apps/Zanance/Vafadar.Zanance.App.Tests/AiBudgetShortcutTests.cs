using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Analysis;
using Vafadar.Zanance.App.Features.Budget;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AI shortcut feedback belongs to the enabled session that opened it.</summary>
[Collection("Classical feature preference")]
public sealed class AiBudgetShortcutTests
{
    /// <summary>A failed native route can report its error only while the original AI session is current.</summary>
    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task Budget_suggestion_route_failure_respects_the_original_AI_session(bool disable, bool reenable)
    {
        using var fixture = new FlowFixture();
        var cancellation = TestContext.Current.CancellationToken;
        var model = await OpenSuggestionAsync(fixture, fixture.Store, cancellation);

        var old = AiFeatures.Current;
        try
        {
            AiFeatures.Current = new(true, _ => { });
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var navigation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            fixture.Platform.PendingNavigation = () => { entered.SetResult(); return navigation.Task; };
            var command = model.ExplainSuggestionCommand.ExecuteAsync(null);
            await Task.WhenAny(entered.Task, command).WaitAsync(cancellation);
            Assert.True(entered.Task.IsCompletedSuccessfully);
            if (disable) AiFeatures.Current.Enabled = false;
            if (reenable) AiFeatures.Current.Enabled = true;
            navigation.SetException(new InvalidOperationException("Synthetic route failure."));
            await command;

            Assert.Equal("analysis", Assert.Single(fixture.Platform.Routes).Route);
            Assert.Equal(disable ? 0 : 1, fixture.Platform.Failures.Count);
            Assert.False(model.IsBusy);
            Assert.NotNull(model.TotalSuggestionText);
        }
        finally { AiFeatures.Current = old; }
    }

    /// <summary>Opting out releases the original suggestion check without changing the classical draft.</summary>
    [Fact]
    public async Task Opting_out_cancels_a_budget_suggestion_read_without_changing_the_draft()
    {
        using var fixture = new FlowFixture();
        var cancellation = TestContext.Current.CancellationToken;
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(fixture));
        var store = new ZananceStore(factory, fixture.Time, fixture.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var model = await OpenSuggestionAsync(fixture, store, cancellation);
        var suggestion = model.TotalSuggestionText;
        var originalRows = await GoalChooserFlowTests.SnapshotAsync(fixture);

        var previous = AiFeatures.Current;
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            var preference = new AiFeatures(true, _ => { });
            AiFeatures.Current = preference;
            var entered = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
            factory.Before = async token =>
            {
                entered.TrySetResult(token);
                await release.Task.WaitAsync(token);
            };

            var check = model.ExplainSuggestionCommand.ExecuteAsync(null);
            var pendingToken = await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellation);
            preference.Enabled = false;
            preference.Enabled = true;
            Assert.True(pendingToken.IsCancellationRequested);
            await check.WaitAsync(TimeSpan.FromSeconds(10), cancellation);
            Assert.False(release.Task.IsCompleted);
            Assert.Empty(fixture.Platform.Routes);
            Assert.Empty(fixture.Platform.Failures);
            Assert.True(model.CanInteract);
            Assert.Equal(suggestion, model.TotalSuggestionText);
            Assert.Equal(originalRows, await GoalChooserFlowTests.SnapshotAsync(fixture));
        }
        finally
        {
            factory.Before = null;
            release.TrySetResult();
            AiFeatures.Current = previous;
        }
    }

    private static async Task<BudgetEditorViewModel> OpenSuggestionAsync(FlowFixture fixture, ZananceStore store, CancellationToken cancellation)
    {
        var account = new Account { Name = "Synthetic budget", CurrencyCode = "EUR", OpeningDate = new(2026, 9, 1) };
        await fixture.Store.SaveAccountAsync(account, cancellation);
        await fixture.Store.SaveEntryAsync(new LedgerEntry
        {
            AccountId = account.Id, Date = new(2026, 9, 20), Kind = EntryKind.Expense, Amount = 1000
        }, cancellation);
        var model = new BudgetEditorViewModel(store, fixture.Translator, fixture.Localization, fixture.Platform);
        model.SetViewActive(true);
        await model.ApplyQueryAsync(new Dictionary<string, object> { ["year"] = 2026, ["month"] = 10, ["currency"] = "EUR" });
        Assert.True(model.CanInteract);
        Assert.NotNull(model.TotalSuggestionText);
        return model;
    }
}
