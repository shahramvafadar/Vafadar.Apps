using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>D-639: an accepted account result cannot silently discard a later native-bound text callback.</summary>
[Collection("Entry validation units")]
public sealed class AccountEditorPostCommitDraftTests
{
    /// <summary>Preserves only newer unsaved text and requires its explicit discard, without repeating the accepted Save.</summary>
    [Fact]
    public async Task Accepted_account_save_keeps_later_text_on_its_original_form_until_explicit_discard()
    {
        using var f = new FlowFixture(); var account = await AccountDetailOriginalBoundaryTests.AccountAsync(f);
        var vm = new AccountEditorViewModel(f.Store, f.Services.GetRequiredService<PlanStore>(), f.Translator, f.Localization, f.Time, f.Platform);
        await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = account.Id }); vm.Form.Name = "  D639 submitted  ";
        f.Store.Changed += (_, _) => vm.Form.Name = "  D639 queued later text  ";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Empty(f.Platform.Routes); Assert.True(vm.IsDirty); Assert.False(vm.CanSave);
        Assert.Equal("  D639 queued later text  ", vm.Form.Name);
        Assert.Equal("D639 submitted", Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: TestContext.Current.CancellationToken)).Name);
        var accepted = await GoalChooserFlowTests.SnapshotAsync(f);
        await vm.SaveCommand.ExecuteAsync(null); Assert.Equal(accepted, await GoalChooserFlowTests.SnapshotAsync(f));
        await vm.CancelCommand.ExecuteAsync(null); Assert.Empty(f.Platform.Routes);
        f.Platform.PendingConfirmation = () => Task.FromResult(true); await vm.CancelCommand.ExecuteAsync(null);
        Assert.Equal("..", Assert.Single(f.Platform.Routes).Route); Assert.Equal(accepted, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    /// <summary>Only the draft explicitly discarded in archive consent may be cleared; later text stays visible.</summary>
    [Fact]
    public async Task Accepted_archive_keeps_newer_text_that_was_not_part_of_its_discard_consent()
    {
        using var f = new FlowFixture(); var account = await AccountDetailOriginalBoundaryTests.AccountAsync(f);
        var vm = new AccountEditorViewModel(f.Store, f.Services.GetRequiredService<PlanStore>(), f.Translator, f.Localization, f.Time, f.Platform);
        await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = account.Id }); vm.Form.Name = "  D639 archive draft before consent  ";
        f.Platform.PendingConfirmation = () => Task.FromResult(true);
        f.Store.Changed += (_, _) => vm.Form.Name = "  D639 queued after archive  ";

        await vm.ToggleArchiveCommand.ExecuteAsync(null);

        Assert.Empty(f.Platform.Routes); Assert.True(vm.IsDirty); Assert.False(vm.CanInteract);
        Assert.Equal("  D639 queued after archive  ", vm.Form.Name);
        var saved = Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: TestContext.Current.CancellationToken));
        Assert.True(saved.IsArchived); Assert.Equal(account.Name, saved.Name);
        var accepted = await GoalChooserFlowTests.SnapshotAsync(f);
        f.Platform.PendingConfirmation = () => Task.FromResult(false); await vm.CancelCommand.ExecuteAsync(null); Assert.Empty(f.Platform.Routes);
        f.Platform.PendingConfirmation = () => Task.FromResult(true); await vm.CancelCommand.ExecuteAsync(null);
        Assert.Equal("..", Assert.Single(f.Platform.Routes).Route); Assert.Equal(accepted, await GoalChooserFlowTests.SnapshotAsync(f));
    }
}
