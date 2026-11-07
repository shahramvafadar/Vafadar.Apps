using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original account reconciliation commands cannot borrow drafts edited during their own synchronous publication callbacks.</summary>
public sealed class ReconciliationCommandPublicationTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Changed_amount_or_date_during_error_or_busy_publication_does_not_compare(bool busy, bool date)
    {
        using var f = new FlowFixture(); var account = await AccountDetailOriginalBoundaryTests.AccountAsync(f);
        var calls = 0; var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(f))
            { Before = _ => { calls++; return Task.CompletedTask; } };
        var store = new ZananceStore(factory, f.Time, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var vm = AccountDetailOriginalBoundaryTests.Create(f, factory, store);
        AccountDetailOriginalBoundaryTests.Open(vm, account.Id); await vm.LoadAsync(); Assert.True(vm.Loading.IsReady);
        vm.ObservedText = "200"; vm.Error = "Earlier inline error"; var originalDate = vm.ReconcileDate;
        var before = await GoalChooserFlowTests.SnapshotAsync(f); calls = 0; var edited = false;
        vm.PropertyChanged += (_, e) =>
        {
            if (edited || e.PropertyName != (busy ? nameof(vm.IsBusy) : nameof(vm.Error)) || (busy && !vm.IsBusy)) return;
            edited = true; if (date) vm.ReconcileDate = originalDate.AddDays(-1); else vm.ObservedText = "300";
        };
        await vm.CompareCommand.ExecuteAsync(null);
        Assert.True(edited); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Equal(0, calls); Assert.False(vm.CanAdjust); Assert.Null(vm.ResultText);
        Assert.Equal(date ? "200" : "300", vm.ObservedText);
        Assert.Equal(date ? originalDate.AddDays(-1) : originalDate, vm.ReconcileDate);
        Assert.Empty(f.Platform.ConfirmationDialogs); Assert.Empty(f.Platform.Routes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Changed_reason_during_error_or_busy_publication_does_not_request_native_consent(bool busy)
    {
        using var f = new FlowFixture(); var account = await AccountDetailOriginalBoundaryTests.AccountAsync(f);
        var vm = AccountDetailOriginalBoundaryTests.Create(f);
        AccountDetailOriginalBoundaryTests.Open(vm, account.Id); await vm.LoadAsync(); Assert.True(vm.Loading.IsReady);
        vm.ObservedText = "200"; await vm.CompareCommand.ExecuteAsync(null); Assert.True(vm.CanAdjust);
        vm.Reason = "Original attempted reason"; vm.Error = "Earlier inline error";
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var edited = false;
        vm.PropertyChanged += (_, e) =>
        {
            if (edited || e.PropertyName != (busy ? nameof(vm.IsBusy) : nameof(vm.Error)) || (busy && !vm.IsBusy)) return;
            edited = true; vm.Reason = "Newer reason belongs to a new command";
        };
        await vm.AdjustCommand.ExecuteAsync(null);
        Assert.True(edited); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Empty(f.Platform.ConfirmationDialogs); Assert.Empty(f.Platform.Routes);
        Assert.Equal("Newer reason belongs to a new command", vm.Reason); Assert.Equal("200", vm.ObservedText);
        // The platform declines if the baseline wrongly asks; no adjustment is accepted.
    }
    [Fact]
    public async Task Changing_observation_during_result_publication_cannot_reactivate_old_adjustment()
    {
        using var f = new FlowFixture(); var account = await AccountDetailOriginalBoundaryTests.AccountAsync(f);
        var vm = AccountDetailOriginalBoundaryTests.Create(f);
        AccountDetailOriginalBoundaryTests.Open(vm, account.Id); await vm.LoadAsync();
        vm.ObservedText = "200"; var before = await GoalChooserFlowTests.SnapshotAsync(f); var edited = false;
        vm.PropertyChanged += (_, e) =>
        {
            if (edited || e.PropertyName != nameof(vm.ResultText) || vm.ResultText is null) return;
            edited = true; vm.ObservedText = "300";
        };
        await vm.CompareCommand.ExecuteAsync(null);
        Assert.True(edited); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.False(vm.CanAdjust); Assert.Null(vm.ResultText); Assert.Null(vm.HintText);
        Assert.Equal("300", vm.ObservedText); Assert.Empty(f.Platform.ConfirmationDialogs);
    }
    [Fact]
    public async Task Reason_edited_and_restored_during_consent_still_belongs_to_a_new_attempt()
    {
        using var f = new FlowFixture(); var account = await AccountDetailOriginalBoundaryTests.AccountAsync(f);
        var vm = AccountDetailOriginalBoundaryTests.Create(f);
        AccountDetailOriginalBoundaryTests.Open(vm, account.Id); await vm.LoadAsync();
        vm.ObservedText = "200"; await vm.CompareCommand.ExecuteAsync(null); Assert.True(vm.CanAdjust);
        vm.Reason = "Original reason"; var before = await GoalChooserFlowTests.SnapshotAsync(f);
        f.Platform.PendingConfirmation = () =>
        { vm.Reason = "A later edit"; vm.Reason = "Original reason"; return Task.FromResult(true); };
        await vm.AdjustCommand.ExecuteAsync(null);
        Assert.Single(f.Platform.ConfirmationDialogs); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Empty(f.Platform.Routes); Assert.Equal("Original reason", vm.Reason); Assert.Equal("200", vm.ObservedText);
    }
}
