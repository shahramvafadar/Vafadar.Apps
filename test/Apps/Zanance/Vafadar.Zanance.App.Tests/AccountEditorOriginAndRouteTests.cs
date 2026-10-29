using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data.Auditing;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>D-639: actual original route origins, root returns, discard and audit boundaries.</summary>
[Collection("Entry validation units")]
public sealed class AccountEditorOriginAndRouteTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static AccountEditorViewModel Editor(FlowFixture f) => new(f.Store,
        f.Services.GetRequiredService<PlanStore>(), f.Translator, f.Localization, f.Time, f.Platform);

    /// <summary>A captured actual parent file rejects the first foreign context, even with the same account ID.</summary>
    [Fact]
    public async Task Parent_origin_pins_the_file_before_the_editor_first_context()
    {
        using var f = new FlowFixture(); using var other = new FlowFixture();
        var account = await AccountDetailOriginalBoundaryTests.AccountAsync(f); var origin = await f.Store.ReadAccountOverviewAsync(Ct);
        await using (var db = await AccountDetailOriginalBoundaryTests.Factory(other).CreateDbContextAsync(Ct))
        {
            db.Accounts.Add(new Account(account.Id) { Name = "D639 foreign parent target", CurrencyCode = "EUR", Type = account.Type,
                OpeningDate = account.OpeningDate, OpeningBalance = 99900 }); await db.SaveChangesAsync(Ct);
        }
        var factory = AccountDetailOriginalBoundaryTests.Factory(other); var access = f.Services.GetRequiredService<ICommercialWriteAccessSource>();
        var store = new ZananceStore(factory, f.Time, access); var vm = new AccountEditorViewModel(store, new PlanStore(factory, store, access),
            f.Translator, f.Localization, f.Time, f.Platform);
        var before = await GoalChooserFlowTests.SnapshotAsync(other);
        await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = account.Id, ["accountEditorOrigin"] = origin });
        Assert.True(vm.Loading.HasFailed); Assert.False(vm.CanInteract); Assert.False(vm.CanSave);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(other));
    }

    /// <summary>Duplicate route callbacks retain the draft; a changed identity stays covered until an explicit own discard.</summary>
    [Fact]
    public async Task Repeated_route_keeps_raw_draft_and_retired_route_can_only_close_its_own_confirmed_draft()
    {
        using var f = new FlowFixture(); var account = await AccountDetailOriginalBoundaryTests.AccountAsync(f); var vm = Editor(f);
        var query = new Dictionary<string, object> { ["id"] = account.Id }; await vm.ApplyQueryAsync(query);
        vm.Form.Name = "  D639 own unsaved route  "; vm.Form.OpeningText = " 456.7800 ";
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await vm.ApplyQueryAsync(new Dictionary<string, object>(query));
        Assert.True(vm.CanInteract); Assert.Equal("  D639 own unsaved route  ", vm.Form.Name); Assert.Equal(" 456.7800 ", vm.Form.OpeningText);
        await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = Guid.NewGuid() });
        Assert.True(vm.Loading.HasFailed); Assert.False(vm.CanInteract); Assert.False(vm.CanSave);
        f.Platform.PendingConfirmation = () => Task.FromResult(false); await vm.CancelCommand.ExecuteAsync(null); Assert.Empty(f.Platform.Routes);
        f.Platform.PendingConfirmation = () => Task.FromResult(true); await vm.CancelCommand.ExecuteAsync(null);
        Assert.Equal("..", Assert.Single(f.Platform.Routes).Route); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    /// <summary>Neither receiver nor prior detached data can be edited to fake an explicit same-source readiness review.</summary>
    [Fact]
    public async Task Fresh_readiness_packet_caller_mutation_never_matches_unchanged_original_source()
    {
        using var f = new FlowFixture(); var account = await AccountDetailOriginalBoundaryTests.AccountAsync(f);
        var previous = await f.Store.ReadAccountEditorAsync(account.Id, cancellationToken: Ct);
        var fresh = await f.Store.ReadAccountEditorAsync(account.Id, cancellationToken: Ct);
        fresh.Inputs.Accounts.Single().CountryCode = "DE";
        Assert.False(fresh.HasSameInputs(previous)); Assert.False(previous.HasSameInputs(fresh));
    }

    /// <summary>An accepted new debt can close normally but cannot navigate its old ID into a replacement root.</summary>
    [Fact]
    public async Task Accepted_debt_return_never_opens_detail_in_a_root_replaced_during_close()
    {
        using var f = new FlowFixture(); var vm = Editor(f); var root = true; vm.BindRootOwner(() => root);
        await vm.ApplyQueryAsync(new Dictionary<string, object> { ["debt"] = true }); vm.Form.Name = "D639 original debt"; vm.Form.OpeningText = "50";
        f.Platform.PendingNavigation = () => { root = false; return Task.CompletedTask; };
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.Equal("..", Assert.Single(f.Platform.Routes).Route);
        var saved = Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: Ct)); Assert.Equal(AccountType.Loan, saved.Type);
        Assert.False(vm.CanSave); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct)); Assert.Empty(f.Platform.Alerts);
    }

    /// <summary>Post-commit root retirement preserves the real archive but cannot show an alert on another root.</summary>
    [Fact]
    public async Task Accepted_archive_from_retired_root_keeps_result_without_late_alert_or_navigation()
    {
        using var f = new FlowFixture(); var account = await AccountDetailOriginalBoundaryTests.AccountAsync(f);
        var settings = await f.Store.GetSettingsAsync(Ct); settings.DefaultAccountId = account.Id; await f.Store.SaveSettingsAsync(settings, Ct);
        var vm = Editor(f); var root = true; vm.BindRootOwner(() => root);
        await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = account.Id });
        f.Platform.PendingConfirmation = () => Task.FromResult(true); f.Store.Changed += (_, _) => root = false;
        await vm.ToggleArchiveCommand.ExecuteAsync(null);
        Assert.True((await f.Store.GetAccountsAsync(cancellationToken: Ct)).Single().IsArchived);
        Assert.Null((await f.Store.GetSettingsAsync(Ct)).DefaultAccountId); Assert.False(vm.CanSave);
        Assert.Empty(f.Platform.Alerts); Assert.Empty(f.Platform.Routes); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    /// <summary>The real audit interceptor must still own the same consent before either account or default can commit.</summary>
    [Fact]
    public async Task Original_archive_retired_at_actual_audit_rolls_back_every_source_and_keeps_draft()
    {
        using var f = new FlowFixture(); var account = await AccountDetailOriginalBoundaryTests.AccountAsync(f);
        var settings = await f.Store.GetSettingsAsync(Ct); settings.DefaultAccountId = account.Id; await f.Store.SaveSettingsAsync(settings, Ct);
        var vm = Editor(f); await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = account.Id }); vm.Form.Name = "  D639 retained archive draft  ";
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var auditReached = false;
        f.Platform.PendingConfirmation = () =>
        {
            f.Time.BeforeRead = () =>
            {
                if (new StackTrace().GetFrames().Any(frame => frame.GetMethod()?.DeclaringType == typeof(AuditingSaveChangesInterceptor)))
                { auditReached = true; vm.SetPresentationActive(false); }
            };
            return Task.FromResult(true);
        };
        try { await vm.ToggleArchiveCommand.ExecuteAsync(null); } finally { f.Time.BeforeRead = null; }
        Assert.True(auditReached); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Equal("  D639 retained archive draft  ", vm.Form.Name); Assert.Empty(f.Platform.Routes); Assert.Empty(f.Platform.Alerts);
    }
}
