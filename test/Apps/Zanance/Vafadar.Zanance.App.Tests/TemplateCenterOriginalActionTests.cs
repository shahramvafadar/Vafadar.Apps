using Vafadar.Zanance.App.Features.Commerce;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual original template deletion must retain native owner and known commit boundaries.</summary>
public sealed class TemplateCenterOriginalActionTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    /// <summary>Creates one isolated nonfinancial definition without a ledger entry for the original flow.</summary>
    internal static async Task<Guid> SeedAsync(FlowFixture fixture)
    {
        var account = new Account { Name = "Original template account", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
        await fixture.Store.SaveAccountAsync(account, Ct);
        var template = new EntryTemplate { Name = "Original definition", Kind = EntryKind.Expense, AccountId = account.Id, Amount = 1000 };
        await fixture.Store.SaveTemplateAsync(template, Ct);
        return template.Id;
    }

    [Fact]
    public async Task Retirement_inside_the_original_delete_writer_rolls_back_the_definition_removal()
    {
        using var fixture = new FlowFixture(); var id = await SeedAsync(fixture);
        var actions = new QuickResourceActions(fixture.Store, fixture.Translator, fixture.Platform);
        await actions.LoadAsync(); var before = await GoalChooserFlowTests.SnapshotAsync(fixture);
        fixture.Platform.Confirmations.Enqueue(true); fixture.Time.BeforeRead = actions.Retire;
        try { await actions.DeleteTemplateAsync(id, () => actions.LoadAsync()); }
        finally { fixture.Time.BeforeRead = null; }
        Assert.False(actions.Accepted);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(fixture));
        Assert.Equal(id, Assert.Single(await fixture.Store.GetTemplatesAsync(Ct)).Id);
        Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task An_original_delete_with_a_failed_postcommit_notification_stays_accepted_and_cannot_be_submitted_again()
    {
        using var fixture = new FlowFixture(); var id = await SeedAsync(fixture);
        var actions = new QuickResourceActions(fixture.Store, fixture.Translator, fixture.Platform);
        await actions.LoadAsync(); fixture.Platform.Confirmations.Enqueue(true); var notifications = 0;
        fixture.Store.Changed += (_, _) => { notifications++; throw new IOException("Fictitious original template delete notification failure."); };
        await actions.DeleteTemplateAsync(id, () => actions.LoadAsync());
        Assert.Empty(await fixture.Store.GetTemplatesAsync(Ct)); Assert.Equal(1, notifications);
        Assert.True(actions.Accepted); Assert.Null(actions.Review);
        Assert.Contains(fixture.Translator["Occurrence_SavedRefreshFailed"], actions.Feedback!);
        Assert.Empty(fixture.Platform.Failures);
        var before = await GoalChooserFlowTests.SnapshotAsync(fixture);
        await actions.DeleteTemplateAsync(id, () => actions.LoadAsync());
        Assert.Equal(1, notifications); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(fixture));
        Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: Ct));
    }
}
