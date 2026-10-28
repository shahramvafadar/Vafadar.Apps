using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original account detail page lifetime and actual-file source failures through the real view model and SQLite.</summary>
public sealed class AccountDetailOriginalBoundaryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    internal static IDbContextFactory<ZananceDbContext> Factory(FlowFixture f) => f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();

    internal static AccountDetailViewModel Create(FlowFixture f, IDbContextFactory<ZananceDbContext>? factory = null, ZananceStore? writer = null)
    {
        factory ??= Factory(f); var access = f.Services.GetRequiredService<ICommercialWriteAccessSource>();
        var reader = new LoanWorkStore(factory, access, f.Time);
        return new(writer ?? f.Store, f.Translator, f.Localization, new DateFormatter(f.Localization), f.Time,
            reader, new(reader, writer ?? f.Store, f.Translator, f.Localization, f.Platform),
            new(f.Services.GetRequiredService<HoldingStore>(), f.Translator, f.Localization, f.Time, f.Platform), f.Platform);
    }

    internal static async Task<Account> AccountAsync(FlowFixture f, AccountType type = AccountType.Checking)
    {
        await using var db = await Factory(f).CreateDbContextAsync(Ct);
        var account = new Account { Name = "Original detail", Type = type, CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1),
            OpeningBalance = type == AccountType.Loan ? -100000 : 10000,
            Installment = type == AccountType.Loan ? 10000 : null, InterestRate = type == AccountType.Loan ? 12 : null };
        db.Accounts.Add(account); await db.SaveChangesAsync(Ct); return account;
    }

    internal static void Open(AccountDetailViewModel vm, Guid id)
    { vm.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = id }); vm.SetReconciliationActive(true); }

    [Fact]
    public async Task Retiring_the_original_page_while_its_first_read_waits_never_publishes_ready()
    {
        using var f = new FlowFixture(); var account = await AccountAsync(f);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var first = true;
        var factory = new ControlledFactory(Factory(f)) { Before = async ct =>
        { if (!first) return; first = false; entered.SetResult(); await release.Task.WaitAsync(ct); } };
        var vm = Create(f, factory); Open(vm, account.Id); var load = vm.LoadAsync();
        await entered.Task.WaitAsync(Ct); vm.SetReconciliationActive(false); release.SetResult(); await load;
        Assert.False(vm.Loading.IsReady); Assert.Null(vm.Name);
    }

    [Fact]
    public async Task A_retired_original_detail_cannot_navigate_to_edit()
    {
        using var f = new FlowFixture(); var account = await AccountAsync(f); var vm = Create(f); Open(vm, account.Id); await vm.LoadAsync();
        Assert.True(vm.Loading.IsReady); vm.SetReconciliationActive(false); await vm.EditCommand.ExecuteAsync(null);
        Assert.Empty(f.Platform.Routes);
    }

    [Fact]
    public async Task Retirement_during_original_native_installment_consent_never_posts_a_payment()
    {
        using var f = new FlowFixture(); await f.Store.EnsureDefaultCategoriesAsync(Ct);
        var loan = await AccountAsync(f, AccountType.Loan); await AccountAsync(f); var vm = Create(f); Open(vm, loan.Id); await vm.LoadAsync();
        Assert.True(vm.Loading.IsReady); Assert.True(vm.HasLoanTerms);
        f.Platform.PendingConfirmation = () => { vm.SetReconciliationActive(false); return Task.FromResult(true); };
        await vm.RecordInstallmentCommand.ExecuteAsync(null);
        Assert.Single(f.Platform.ConfirmationDialogs); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Temporary_profile_switch_cannot_publish_another_files_goal_earmark_into_the_original_account()
    {
        using var f = new FlowFixture(); using var other = new FlowFixture(); var account = await AccountAsync(f);
        await using (var db = await Factory(other).CreateDbContextAsync(Ct))
        {
            var restored = new Account { Name = "Other file same account", Type = AccountType.Checking, CurrencyCode = "EUR", OpeningBalance = 10000, OpeningDate = account.OpeningDate };
            db.Accounts.Add(restored); db.Entry(restored).Property(item => item.Id).CurrentValue = account.Id;
            var goal = new Goal { Name = "Only other profile", CurrencyCode = "EUR", TargetAmount = 5000, Type = GoalType.Earmark };
            db.Goals.Add(goal); db.GoalAllocations.Add(new GoalAllocation { GoalId = goal.Id, AccountId = account.Id, Amount = 2500, Date = new(2026, 10, 2) });
            await db.SaveChangesAsync(Ct);
        }
        var calls = 0; var factory = new ControlledFactory(Factory(f))
        { Select = () => ++calls is 2 or 3 ? Factory(other) : Factory(f) };
        var vm = Create(f, factory); Open(vm, account.Id); await vm.LoadAsync();
        Assert.Null(vm.EarmarkText);
        Assert.Empty(await f.Services.GetRequiredService<GoalStore>().GetGoalsAsync(Ct));
    }

    /// <summary>Changes only actual factory waits/selection; every returned context and financial read remains real SQLite.</summary>
    internal sealed class ControlledFactory(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
    {
        internal Func<CancellationToken, Task>? Before;
        internal Func<IDbContextFactory<ZananceDbContext>>? Select;
        public ZananceDbContext CreateDbContext() => (Select?.Invoke() ?? original).CreateDbContext();
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        { if (Before is { } before) await before(cancellationToken); return await (Select?.Invoke() ?? original).CreateDbContextAsync(cancellationToken); }
    }
}
