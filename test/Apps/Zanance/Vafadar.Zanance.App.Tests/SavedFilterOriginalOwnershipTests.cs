using Vafadar.Zanance.App.Features.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual original saved-query menu, writer retirement and known acceptance boundaries.</summary>
public sealed class SavedFilterOriginalOwnershipTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static SavedFilter Draft() => new() { Name = string.Empty, Period = 3, Kind = 1,
        CategoryIds = [], Search = "Original captured query", UnreviewedOnly = true, InTotalsOnly = true };
    private static async Task<QuickResourceActions> PrepareAsync(FlowFixture fixture, bool deleting)
    {
        await fixture.Store.SaveSavedFilterAsync(new SavedFilter { Name = "Original query", Period = 3, Search = "Original retained search" }, Ct);
        var actions = new QuickResourceActions(fixture.Store, fixture.Translator, fixture.Platform); await actions.LoadAsync();
        fixture.Platform.Choices.Enqueue(fixture.Translator[deleting ? "Filter_Remove" : "Filter_SaveCurrent"]);
        if (deleting) { fixture.Platform.Choices.Enqueue("1. Original query"); fixture.Platform.Confirmations.Enqueue(true); }
        else fixture.Platform.Inputs.Enqueue("New original query");
        return actions;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task An_original_saved_query_writer_retiring_at_SaveChanges_preserves_all_original_rows(bool deleting)
    {
        using var fixture = new FlowFixture(); var actions = await PrepareAsync(fixture, deleting);
        var before = await GoalChooserFlowTests.SnapshotAsync(fixture); fixture.Time.BeforeRead = actions.Retire;
        try { await actions.FilterMenuAsync(Draft(), () => actions.LoadAsync()); }
        finally { fixture.Time.BeforeRead = null; }
        Assert.False(actions.Accepted); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(fixture));
        Assert.Equal("Original query", Assert.Single(await fixture.Store.GetSavedFiltersAsync(Ct)).Name);
        Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task An_original_saved_query_commit_with_a_failed_notification_remains_accepted(bool deleting)
    {
        using var fixture = new FlowFixture(); var actions = await PrepareAsync(fixture, deleting); var notifications = 0;
        fixture.Store.Changed += (_, _) => { notifications++; throw new IOException("Fictitious original saved-query notification failure."); };
        await actions.FilterMenuAsync(Draft(), () => actions.LoadAsync());
        Assert.Equal(deleting ? 0 : 2, (await fixture.Store.GetSavedFiltersAsync(Ct)).Count);
        Assert.Equal(1, notifications); Assert.True(actions.Accepted); Assert.Null(actions.Review);
        Assert.Contains(fixture.Translator["Occurrence_SavedRefreshFailed"], actions.Feedback!);
        Assert.Empty(fixture.Platform.Failures); Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task A_retired_original_name_response_cannot_overwrite_a_new_saved_query_publications_draft()
    {
        using var fixture = new FlowFixture(); var actions = await PrepareAsync(fixture, deleting: false);
        var replacement = await fixture.Store.GetQuickResourceReviewAsync(cancellationToken: Ct);
        fixture.Platform.PendingInput = () =>
        {
            actions.Retire(); actions.PublishOriginal(replacement);
            return Task.FromResult<string?>("Retired original name");
        };
        var before = await GoalChooserFlowTests.SnapshotAsync(fixture);
        await actions.FilterMenuAsync(Draft(), () => actions.LoadAsync());
        Assert.Same(replacement, actions.Review); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(fixture));
        Assert.Null(actions.FilterNameDraft); Assert.Empty(fixture.Platform.Failures);
    }

    [Fact]
    public async Task A_departed_page_cannot_open_a_menu_or_accept_its_late_name_response()
    {
        using var fixture = new FlowFixture(); var actions = await PrepareAsync(fixture, deleting: false);
        var active = false; var before = await GoalChooserFlowTests.SnapshotAsync(fixture);
        await actions.FilterMenuAsync(Draft(), () => actions.LoadAsync(), () => active);
        Assert.Empty(fixture.Platform.OfferedActions);
        active = true;
        fixture.Platform.PendingInput = () =>
        {
            active = false;
            return Task.FromResult<string?>("Name after page departure");
        };
        await actions.FilterMenuAsync(Draft(), () => actions.LoadAsync(), () => active);
        Assert.Null(actions.FilterNameDraft); Assert.False(actions.Accepted);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(fixture));
    }

    [Fact]
    public async Task A_complete_original_transaction_reload_preserves_the_accepted_saved_query_result()
    {
        using var fixture = new FlowFixture(); var actions = await PrepareAsync(fixture, deleting: false);
        await actions.FilterMenuAsync(Draft(), async () =>
        {
            actions.CoverTransactionCenter();
            var source = await fixture.Store.ReadTransactionsAsync(cancellationToken: Ct);
            await fixture.Store.ValidateTransactionsAsync(source, Ct);
            actions.PublishTransactionCenter(source.Resources);
        });
        Assert.True(actions.Accepted); Assert.NotNull(actions.Review);
        Assert.Equal(2, actions.Review.Filters.Count); Assert.Empty(fixture.Platform.Failures);
        Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Departure_during_actual_source_validation_cannot_open_a_late_original_menu()
    {
        using var f = new FlowFixture(); var prepared = await PrepareAsync(f, deleting: false); var active = true;
        var original = AccountDetailOriginalBoundaryTests.Factory(f);
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(original)
        { Select = () => { active = false; return original; } };
        var store = new ZananceStore(factory, f.Time, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var actions = new QuickResourceActions(store, f.Translator, f.Platform);
        actions.PublishOriginal(prepared.Review!);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await actions.FilterMenuAsync(Draft(), () => actions.LoadAsync(), () => active);
        Assert.Empty(f.Platform.OfferedActions); Assert.Null(actions.FilterNameDraft);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
}
