using Vafadar.Zanance.App.Features.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original list adoption preserves known acceptance and rejects a retired pending native response.</summary>
public sealed class TemplateCenterPublicationTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    [Fact]
    public async Task A_retired_original_confirmation_cannot_delete_or_retire_a_later_complete_list_publication()
    {
        using var fixture = new FlowFixture(); var id = await TemplateCenterOriginalActionTests.SeedAsync(fixture);
        var actions = new QuickResourceActions(fixture.Store, fixture.Translator, fixture.Platform);
        actions.PublishTemplateCenter(await fixture.Store.ReadTemplateCenterAsync(cancellationToken: Ct));
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var answer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Platform.PendingConfirmation = () => { entered.SetResult(); return answer.Task; };
        var pending = actions.DeleteTemplateAsync(id, () => actions.LoadAsync()); await entered.Task.WaitAsync(Ct);
        actions.CoverTemplateCenter();
        var replacement = await fixture.Store.ReadTemplateCenterAsync(cancellationToken: Ct); actions.PublishTemplateCenter(replacement);
        var before = await GoalChooserFlowTests.SnapshotAsync(fixture); answer.SetResult(true); await pending;
        Assert.Same(replacement, actions.Review); Assert.True(actions.CanInteract); Assert.False(actions.Accepted);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(fixture));
        Assert.Equal(id, Assert.Single(await fixture.Store.GetTemplatesAsync(Ct)).Id);
        Assert.Empty(fixture.Platform.Failures); Assert.Empty(fixture.Platform.Alerts);
    }

    [Fact]
    public async Task A_complete_postcommit_list_refresh_keeps_the_original_deletion_accepted()
    {
        using var fixture = new FlowFixture(); var id = await TemplateCenterOriginalActionTests.SeedAsync(fixture);
        var actions = new QuickResourceActions(fixture.Store, fixture.Translator, fixture.Platform);
        actions.PublishTemplateCenter(await fixture.Store.ReadTemplateCenterAsync(cancellationToken: Ct));
        fixture.Platform.Confirmations.Enqueue(true);
        await actions.DeleteTemplateAsync(id, async () =>
        {
            actions.CoverTemplateCenter(); Assert.True(actions.Accepted);
            actions.PublishTemplateCenter(await fixture.Store.ReadTemplateCenterAsync(cancellationToken: Ct));
        });
        Assert.True(actions.Accepted); Assert.True(actions.CanInteract); Assert.Empty(actions.Review!.Templates);
        Assert.Empty(await fixture.Store.GetTemplatesAsync(Ct)); Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: Ct));
        Assert.Empty(fixture.Platform.Failures); Assert.Empty(fixture.Platform.Alerts);
    }
}
