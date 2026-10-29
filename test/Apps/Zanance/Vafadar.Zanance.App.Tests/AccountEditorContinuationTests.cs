using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>D-639: original read-only recovery, drafts and committed account/default outcomes with real SQLite.</summary>
[Collection("Entry validation units")]
public sealed class AccountEditorContinuationTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class ControlledFactory(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
    {
        internal bool Refuse;
        internal TaskCompletionSource? Wait;
        internal TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ZananceDbContext CreateDbContext() => original.CreateDbContext();
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            if (Refuse) throw new IOException("D639 original read refusal.");
            var wait = Wait; Wait = null;
            if (wait is not null) { Entered.TrySetResult(); await wait.Task.WaitAsync(cancellationToken); }
            return await original.CreateDbContextAsync(cancellationToken);
        }
    }
    private sealed class Access : ICommercialWriteAccessSource
    {
        internal bool Refuse;
        public CommercialWriteAccess Capture(string path) => Refuse ? throw new IOException("D639 readiness refusal.") : CommercialWriteAccess.Inactive;
    }
    private static AccountEditorViewModel Editor(FlowFixture f, ZananceStore? store = null, IDbContextFactory<ZananceDbContext>? factory = null,
        ICommercialWriteAccessSource? access = null)
    {
        store ??= f.Store; factory ??= AccountDetailOriginalBoundaryTests.Factory(f);
        access ??= f.Services.GetRequiredService<ICommercialWriteAccessSource>();
        return new(store, new PlanStore(factory, store, access), f.Translator, f.Localization, f.Time, f.Platform);
    }

    /// <summary>Initial native appearance must not silently retry an acquisition that already failed.</summary>
    [Fact]
    public async Task Initial_native_appearance_preserves_failed_read_until_explicit_retry()
    {
        using var f = new FlowFixture(); var account = await AccountDetailOriginalBoundaryTests.AccountAsync(f);
        var factory = new ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(f)) { Refuse = true };
        var access = new Access(); var store = new ZananceStore(factory, f.Time, access); var vm = Editor(f, store, factory, access);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = account.Id });
        Assert.True(vm.Loading.HasFailed); factory.Refuse = false;
        vm.SetPresentationActive(true);
        Assert.True(vm.Loading.HasFailed); Assert.False(vm.CanInteract);
        await vm.ReloadCommand.ExecuteAsync(null);
        Assert.True(vm.Loading.IsReady); Assert.True(vm.CanInteract);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    /// <summary>An empty original reader never initializes settings, categories or financial rows.</summary>
    [Fact]
    public async Task New_original_account_reader_and_validation_do_not_initialize_storage()
    {
        using var f = new FlowFixture(); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var source = await f.Store.ReadAccountEditorAsync(null, cancellationToken: Ct);
        Assert.Null(source.Inputs.Settings); Assert.Empty(source.Inputs.Accounts);
        await f.Store.ValidateAccountEditorAsync(source, Ct);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    /// <summary>An actual pending original reader cannot uncover its retired presentation.</summary>
    [Fact]
    public async Task Departed_pending_account_reader_never_publishes_ready()
    {
        using var f = new FlowFixture(); var factory = new ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(f));
        factory.Wait = new(TaskCreationOptions.RunContinuationsAsynchronously); var release = factory.Wait;
        var access = new Access(); var store = new ZananceStore(factory, f.Time, access); var vm = Editor(f, store, factory, access);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var pending = vm.ApplyQueryAsync(new Dictionary<string, object>()); await factory.Entered.Task.WaitAsync(Ct);
        vm.SetPresentationActive(false); release.TrySetResult(); await pending;
        Assert.False(vm.CanInteract); Assert.False(vm.Loading.IsReady);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    /// <summary>Returning to the actual same form resumes an interrupted read without recreating source data.</summary>
    [Fact]
    public async Task Same_form_reappearance_resumes_only_its_interrupted_pending_read()
    {
        using var f = new FlowFixture(); var account = await AccountDetailOriginalBoundaryTests.AccountAsync(f);
        var factory = new ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(f));
        factory.Wait = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var access = new Access(); var store = new ZananceStore(factory, f.Time, access); var vm = Editor(f, store, factory, access);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var pending = vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = account.Id });
        await factory.Entered.Task.WaitAsync(Ct); vm.SetPresentationActive(false); await pending;
        Assert.False(vm.Loading.IsReady); vm.SetPresentationActive(true); await vm.LoadAsync();
        Assert.True(vm.Loading.IsReady); Assert.True(vm.CanInteract); Assert.Equal(account.Name, vm.Form.Name);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    /// <summary>A covered retry retains complete raw name and monetary text without another write.</summary>
    [Fact]
    public async Task Same_original_account_retry_keeps_raw_fields_and_complete_source()
    {
        using var f = new FlowFixture(); var account = await AccountDetailOriginalBoundaryTests.AccountAsync(f);
        var factory = new ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(f)); var access = new Access();
        var store = new ZananceStore(factory, f.Time, access); var vm = Editor(f, store, factory, access);
        await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = account.Id });
        vm.Form.Name = "  D639 retained raw name  "; vm.Form.OpeningText = " 123.4500 "; var before = await GoalChooserFlowTests.SnapshotAsync(f);
        factory.Refuse = true; await vm.LoadAsync(); Assert.True(vm.Loading.HasFailed); Assert.False(vm.CanInteract);
        factory.Refuse = false; await vm.ReloadCommand.ExecuteAsync(null);
        Assert.True(vm.Loading.IsReady); Assert.True(vm.CanInteract); Assert.True(vm.IsDirty);
        Assert.Equal("  D639 retained raw name  ", vm.Form.Name); Assert.Equal(" 123.4500 ", vm.Form.OpeningText);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    /// <summary>Explicit quota-read failure keeps the already displayed draft editable but cannot authorize Save.</summary>
    [Fact]
    public async Task Readiness_refusal_keeps_original_draft_and_next_explicit_review_rebinds_only_same_inputs()
    {
        using var f = new FlowFixture(); var access = new Access(); var factory = AccountDetailOriginalBoundaryTests.Factory(f);
        var store = new ZananceStore(factory, f.Time, access); var vm = Editor(f, store, factory, access);
        await vm.ApplyQueryAsync(new Dictionary<string, object>()); vm.Form.Name = "D639 readiness draft"; vm.Form.OpeningText = " 42.0000 ";
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        access.Refuse = true; await vm.ReviewWorkCommand.ExecuteAsync(null);
        Assert.True(vm.CanInteract); Assert.False(vm.CanSave); Assert.Equal(f.Translator["Editor_WorkReadFailed"], vm.WorkText);
        access.Refuse = false; await vm.ReviewWorkCommand.ExecuteAsync(null);
        Assert.Null(vm.WorkText); Assert.True(vm.CanSave); Assert.Equal("D639 readiness draft", vm.Form.Name); Assert.Equal(" 42.0000 ", vm.Form.OpeningText);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    /// <summary>Complete detached account fields are authority, never a caller-editable shortcut.</summary>
    [Fact]
    public async Task Mutating_detached_account_country_refuses_original_validation_without_write()
    {
        using var f = new FlowFixture(); var account = await AccountDetailOriginalBoundaryTests.AccountAsync(f);
        var source = await f.Store.ReadAccountEditorAsync(account.Id, cancellationToken: Ct); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        source.Inputs.Accounts.Single(x => x.Id == account.Id).CountryCode = "DE";
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.ValidateAccountEditorAsync(source, Ct));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    /// <summary>An actual audit-time owner retirement rolls account and default back together.</summary>
    [Fact]
    public async Task Retired_original_account_writer_rolls_back_account_and_explicit_default_atomically()
    {
        using var f = new FlowFixture(); var account = await AccountDetailOriginalBoundaryTests.AccountAsync(f);
        var source = await f.Store.ReadAccountEditorAsync(account.Id, cancellationToken: Ct); var target = account.CopyForEditing(); target.Name = "D639 must roll back";
        var current = true; var before = await GoalChooserFlowTests.SnapshotAsync(f); f.Time.BeforeRead = () => current = false;
        try
        {
            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => f.Store.SaveAccountAsync(target, Ct,
                isCurrent: () => current, originalWork: source.Work, originalSource: source, makeDefault: true));
        }
        finally { f.Time.BeforeRead = null; }
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    /// <summary>The actual form consumes one account/default commit before a fallible observer and cannot replay it.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Accepted_original_account_and_default_survive_observer_failure_without_second_save(bool preferencesExist)
    {
        using var f = new FlowFixture(); var original = await AccountDetailOriginalBoundaryTests.AccountAsync(f);
        if (preferencesExist)
        {
            var settings = await f.Store.GetSettingsAsync(Ct); settings.DefaultCurrencyCode = "CHF"; await f.Store.SaveSettingsAsync(settings, Ct);
        }
        var vm = Editor(f); await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = original.Id });
        vm.Form.Name = "D639 accepted original name"; vm.IsDefault = true;
        f.Store.Changed += (_, _) => throw new IOException("D639 accepted notification failure.");
        await vm.SaveCommand.ExecuteAsync(null);
        var saved = Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: Ct));
        Assert.Equal("D639 accepted original name", saved.Name); Assert.Equal(original.OpeningBalance, saved.OpeningBalance);
        var left = JsonSerializer.SerializeToNode(original)!.AsObject(); var right = JsonSerializer.SerializeToNode(saved)!.AsObject();
        foreach (var key in new[] { "Name", "UpdatedAt" }) { left.Remove(key); right.Remove(key); }
        Assert.True(JsonNode.DeepEquals(left, right));
        var currentSettings = await f.Store.GetSettingsAsync(Ct); Assert.Equal(saved.Id, currentSettings.DefaultAccountId);
        Assert.Equal(preferencesExist ? "CHF" : "EUR", currentSettings.DefaultCurrencyCode);
        Assert.False(vm.CanSave); Assert.NotNull(vm.SaveError); Assert.Empty(f.Platform.Routes);
        var beforeReplay = await GoalChooserFlowTests.SnapshotAsync(f); await vm.SaveCommand.ExecuteAsync(null);
        Assert.Equal(beforeReplay, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
}
