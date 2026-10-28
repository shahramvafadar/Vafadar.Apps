using System.Collections.Specialized;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual original schedule publication and query-boundary regressions using the original view model and SQLite.</summary>
public sealed class LoanScheduleOriginalBoundaryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    internal static LoanScheduleViewModel Create(FlowFixture f, Microsoft.EntityFrameworkCore.IDbContextFactory<Vafadar.Zanance.Data.ZananceDbContext>? factory = null)
        => new(new LoanWorkStore(factory ?? AccountDetailOriginalBoundaryTests.Factory(f),
            f.Services.GetRequiredService<Vafadar.Zanance.Data.Commerce.ICommercialWriteAccessSource>(), f.Time),
            f.Translator, f.Localization, new DateFormatter(f.Localization));

    internal static void Open(LoanScheduleViewModel vm, Guid id)
        => vm.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = id });

    [Fact]
    public async Task Original_complete_schedule_publishes_one_reset_instead_of_partial_installment_additions()
    {
        using var f = new FlowFixture(); var loan = await AccountDetailOriginalBoundaryTests.AccountAsync(f, Vafadar.Zanance.Core.Accounts.AccountType.Loan);
        var vm = Create(f); Open(vm, loan.Id); var actions = new List<NotifyCollectionChangedAction>();
        vm.Rows.CollectionChanged += (_, change) => actions.Add(change.Action);
        await vm.LoadAsync(); Assert.True(vm.Loading.IsReady); Assert.True(vm.Rows.Count > 1);
        Assert.Equal([NotifyCollectionChangedAction.Reset], actions);
    }

    [Fact]
    public async Task Actual_final_source_refusal_never_publishes_unvalidated_name_or_rows()
    {
        using var f = new FlowFixture(); var loan = await AccountDetailOriginalBoundaryTests.AccountAsync(f, Vafadar.Zanance.Core.Accounts.AccountType.Loan);
        var calls = 0; var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(f))
        { Before = async ct =>
            {
                if (Interlocked.Increment(ref calls) != 2) return;
                await using var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(ct);
                await db.Accounts.Where(item => item.Id == loan.Id).ExecuteUpdateAsync(set => set.SetProperty(item => item.Name, "Changed before final source validation"), ct);
            }
        };
        var vm = Create(f, factory); Open(vm, loan.Id); await vm.LoadAsync();
        Assert.True(vm.Loading.HasFailed); Assert.Null(vm.Name); Assert.Empty(vm.Rows);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Another_missing_original_identity_does_not_keep_the_previous_loan_name()
    {
        using var f = new FlowFixture(); var loan = await AccountDetailOriginalBoundaryTests.AccountAsync(f, Vafadar.Zanance.Core.Accounts.AccountType.Loan);
        var vm = Create(f); Open(vm, loan.Id); await vm.LoadAsync(); Assert.Equal(loan.Name, vm.Name);
        Open(vm, Guid.NewGuid()); await vm.LoadAsync();
        Assert.True(vm.IsEmpty); Assert.Null(vm.Name); Assert.Empty(vm.Rows);
    }

    [Fact]
    public async Task An_original_request_waiting_for_its_reader_cannot_publish_after_its_query_identity_changes()
    {
        using var f = new FlowFixture(); var loan = await AccountDetailOriginalBoundaryTests.AccountAsync(f, Vafadar.Zanance.Core.Accounts.AccountType.Loan);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var first = true;
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(f))
        { Before = async ct => { if (!first) return; first = false; entered.SetResult(); await release.Task.WaitAsync(ct); } };
        var vm = Create(f, factory); Open(vm, loan.Id); var read = vm.LoadAsync();
        await entered.Task.WaitAsync(Ct); Open(vm, Guid.NewGuid()); release.SetResult(); await read;
        Assert.False(vm.Loading.IsReady); Assert.Null(vm.Name); Assert.Empty(vm.Rows);
    }
}
