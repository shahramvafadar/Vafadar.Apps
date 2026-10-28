using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Current original loan consent is checked inside the writer, and a known commit survives owner/view retirement.</summary>
public sealed class AccountDetailLoanOwnerTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static IDbContextFactory<ZananceDbContext> Factory(FlowFixture f) => AccountDetailOriginalBoundaryTests.Factory(f);
    private static async Task<Guid> SourceAsync(FlowFixture f)
    {
        await f.Store.EnsureDefaultCategoriesAsync(Ct); var loan = await AccountDetailOriginalBoundaryTests.AccountAsync(f, AccountType.Loan);
        await AccountDetailOriginalBoundaryTests.AccountAsync(f); return loan.Id;
    }

    [Fact]
    public async Task Actual_insert_retirement_rolls_back_the_whole_original_principal_interest_batch()
    {
        using var f = new FlowFixture(); var loanId = await SourceAsync(f); var current = true;
        var factory = new TriggerFactory(Factory(f), () => current = false); var access = f.Services.GetRequiredService<ICommercialWriteAccessSource>();
        var review = await new LoanWorkStore(factory, access, f.Time).ReadAsync(Ct);
        await using (var db = await Factory(f).CreateDbContextAsync(Ct))
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RetireLoanOwner AFTER INSERT ON Entries BEGIN SELECT RetireOriginalLoanOwner(); END;", Ct);
        var writer = new ZananceStore(factory, f.Time, access); var notices = 0; writer.Changed += (_, _) => notices++;
        var cash = review.Accounts.Single(item => item.Id != loanId);
        await Assert.ThrowsAsync<OperationCanceledException>(() => writer.SaveLoanInstallmentAsync(review, loanId, cash.Id, "Original interest", Ct, () => current));
        Assert.False(current); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct)); Assert.Equal(0, notices);
        var accounts = await f.Store.GetAccountsAsync(cancellationToken: Ct); Assert.Equal(-100000, accounts.Single(item => item.Id == loanId).OpeningBalance);
    }

    [Fact]
    public async Task A_known_commit_is_retained_after_the_original_owner_retires_inside_a_failed_notification()
    {
        using var f = new FlowFixture(); var loanId = await SourceAsync(f); var reader = f.Services.GetRequiredService<LoanWorkStore>(); var review = await reader.ReadAsync(Ct);
        var session = new LoanInstallmentSession(reader, f.Store, f.Translator, f.Localization, f.Platform); session.SetOwner(loanId, true);
        var notices = 0; f.Store.Changed += (_, _) => { notices++; session.SetOwner(loanId, false); throw new IOException("Owned post-commit notice refusal"); };
        f.Platform.Confirmations.Enqueue(true); await session.RecordAsync(review, loanId);
        Assert.Equal(loanId, session.AcceptedLoanId); Assert.Null(session.Result); Assert.Null(session.Error); Assert.Equal(2, (await f.Store.GetEntriesAsync(cancellationToken: Ct)).Count);
        session.SetOwner(loanId, true); await session.RecordAsync(review, loanId);
        Assert.Single(f.Platform.ConfirmationDialogs); Assert.Equal(1, notices); Assert.Equal(2, (await f.Store.GetEntriesAsync(cancellationToken: Ct)).Count);
    }

    [Fact]
    public async Task An_own_post_commit_read_can_replace_the_old_source_without_losing_the_current_pages_saved_result()
    {
        using var f = new FlowFixture(); var loanId = await SourceAsync(f); var vm = AccountDetailOriginalBoundaryTests.Create(f); AccountDetailOriginalBoundaryTests.Open(vm, loanId); await vm.LoadAsync();
        Task? actualRead = null; f.Store.Changed += (_, _) => { actualRead = vm.LoadAsync(); throw new IOException("Owned read then subscriber refusal"); };
        f.Platform.Confirmations.Enqueue(true); await vm.RecordInstallmentCommand.ExecuteAsync(null); Assert.NotNull(actualRead); await actualRead;
        Assert.True(vm.Loading.IsReady); Assert.Equal(loanId, vm.Installment.AcceptedLoanId); Assert.Equal(f.Translator["Loan_Recorded"], vm.Installment.Result);
        Assert.Equal(f.Translator["Loan_RecordedFollowUpFailed"], vm.Installment.Error); Assert.Equal(2, (await f.Store.GetEntriesAsync(cancellationToken: Ct)).Count);
    }

    /// <summary>Registers only a real owned SQLite callback, so retirement occurs during the original insert transaction.</summary>
    private sealed class TriggerFactory(IDbContextFactory<ZananceDbContext> actual, Action retire) : IDbContextFactory<ZananceDbContext>
    {
        private ZananceDbContext Bind(ZananceDbContext db)
        { ((SqliteConnection)db.Database.GetDbConnection()).CreateFunction("RetireOriginalLoanOwner", () => { retire(); return 1; }); return db; }
        public ZananceDbContext CreateDbContext() => Bind(actual.CreateDbContext());
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Bind(await actual.CreateDbContextAsync(cancellationToken));
    }
}
