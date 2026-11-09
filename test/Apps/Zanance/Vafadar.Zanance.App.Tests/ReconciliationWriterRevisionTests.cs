using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Monotonic original reason ownership survives native consent and every await inside the existing writer.</summary>
public sealed class ReconciliationWriterRevisionTests
{
    [Fact]
    public async Task Reason_edited_and_restored_after_consent_refuses_before_original_EF_save()
    {
        using var f = new FlowFixture(); var account = await AccountDetailOriginalBoundaryTests.AccountAsync(f);
        var factory = new WriterProbe(AccountDetailOriginalBoundaryTests.Factory(f));
        var store = new ZananceStore(factory, f.Time, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var vm = AccountDetailOriginalBoundaryTests.Create(f, factory, store);
        AccountDetailOriginalBoundaryTests.Open(vm, account.Id); await vm.LoadAsync();
        vm.ObservedText = "200"; vm.Reason = "Original reviewed reason";
        await vm.CompareCommand.ExecuteAsync(null); Assert.True(vm.CanAdjust);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var edited = false;
        factory.Acquired = () => { edited = true; vm.Reason = "A newer edit inside the writer"; vm.Reason = "Original reviewed reason"; };
        f.Platform.PendingConfirmation = () => { factory.Armed = true; return Task.FromResult(true); };
        await vm.AdjustCommand.ExecuteAsync(null);
        Assert.True(edited); Assert.Single(f.Platform.ConfirmationDialogs);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Equal(0, factory.SaveReached); Assert.False(vm.CanAdjust); Assert.NotNull(vm.Error);
        Assert.Equal("200", vm.ObservedText); Assert.Equal("Original reviewed reason", vm.Reason); Assert.Empty(f.Platform.Routes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Retired_command_guard_before_or_during_consent_never_writes(bool duringConsent)
    {
        using var f = new FlowFixture(); var account = await AccountDetailOriginalBoundaryTests.AccountAsync(f);
        var session = new Vafadar.Zanance.App.Features.Accounts.AccountReconciliationSession(f.Store);
        await session.CompareAsync(account.Id, 20000, DateOnly.FromDateTime(f.Time.GetLocalNow().DateTime));
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var current = duringConsent; var confirmations = 0;
        var result = await session.AdjustAsync(() => { confirmations++; current = false; return Task.FromResult(true); },
            () => "New guarded reason", () => current);
        Assert.Null(result); Assert.Equal(duringConsent ? 1 : 0, confirmations); Assert.False(session.Committed);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Current_command_guard_still_reaches_the_original_save_sentinel_without_committing()
    {
        using var f = new FlowFixture(); var account = await AccountDetailOriginalBoundaryTests.AccountAsync(f);
        var factory = new WriterProbe(AccountDetailOriginalBoundaryTests.Factory(f));
        var store = new ZananceStore(factory, f.Time, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var session = new Vafadar.Zanance.App.Features.Accounts.AccountReconciliationSession(store);
        await session.CompareAsync(account.Id, 20000, DateOnly.FromDateTime(f.Time.GetLocalNow().DateTime));
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<IOException>(() => session.AdjustAsync(() => { factory.Armed = true; return Task.FromResult(true); },
            () => "New still-current reason", () => true));
        Assert.Equal(1, factory.SaveReached); Assert.False(session.Committed);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    /// <summary>Returns original keyed SQLite contexts; a pre-save sentinel prevents an unguarded baseline from committing money.</summary>
    private sealed class WriterProbe(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
    {
        internal bool Armed; internal Action? Acquired; internal int SaveReached;
        public ZananceDbContext CreateDbContext() => Observe(original.CreateDbContext());
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken ct = default) => Observe(await original.CreateDbContextAsync(ct));
        private ZananceDbContext Observe(ZananceDbContext db)
        {
            if (!Armed) return db;
            Armed = false; Acquired?.Invoke();
            db.SavingChanges += (_, _) => { SaveReached++; throw new IOException("D943 pre-save sentinel: no baseline financial commit"); };
            return db;
        }
    }
}
