using Microsoft.EntityFrameworkCore;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original free history and essential loan warnings remain available after paid-analysis action refusal.</summary>
public sealed class AccountDetailReadPermissionTests
{
    [Fact]
    public async Task Paid_schedule_and_installment_refusals_keep_original_free_balance_and_essential_warning_ready()
    {
        var ct = TestContext.Current.CancellationToken; using var f = new FlowFixture(); await f.Store.EnsureDefaultCategoriesAsync(ct);
        var loan = await AccountDetailOriginalBoundaryTests.AccountAsync(f, AccountType.Loan); await AccountDetailOriginalBoundaryTests.AccountAsync(f);
        var factory = AccountDetailOriginalBoundaryTests.Factory(f);
        await using (var db = await factory.CreateDbContextAsync(ct)) await db.Database.ExecuteSqlRawAsync("UPDATE Accounts SET Installment=500 WHERE Type=4", ct);
        var access = new FreeAccess(f.Directory.Combine("zanance.db")); var reader = new LoanWorkStore(factory, access, f.Time);
        var writer = new ZananceStore(factory, f.Time, access); var session = new LoanInstallmentSession(reader, writer, f.Translator, f.Localization, f.Platform);
        var vm = new AccountDetailViewModel(writer, f.Translator, f.Localization, new DateFormatter(f.Localization), f.Time, reader, session,
            new(new HoldingStore(factory, access), f.Translator, f.Localization, f.Time, f.Platform), f.Platform);
        AccountDetailOriginalBoundaryTests.Open(vm, loan.Id); await vm.LoadAsync();
        Assert.True(vm.Loading.IsReady); Assert.True(vm.HasLoanTerms); Assert.False(vm.ShowLoanEstimate); Assert.NotNull(vm.LoanEssentialWarning);
        var originalBalance = vm.BalanceText; var originalWarning = vm.LoanEssentialWarning;
        await vm.ShowScheduleCommand.ExecuteAsync(null); Assert.True(vm.Loading.IsReady); Assert.NotNull(vm.LoanWorkFeedback);
        await vm.RecordInstallmentCommand.ExecuteAsync(null); Assert.True(vm.Loading.IsReady); Assert.NotNull(session.Error); Assert.Null(session.AcceptedLoanId);
        Assert.Equal(originalBalance, vm.BalanceText); Assert.Equal(originalWarning, vm.LoanEssentialWarning);
        Assert.Empty(f.Platform.Routes); Assert.Empty(f.Platform.ConfirmationDialogs); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: ct));
    }

    /// <summary>Supplies only fixed fictitious original-file Free facts, never a production entitlement or activation.</summary>
    private sealed class FreeAccess(string file) : ICommercialWriteAccessSource
    {
        private readonly CommercialWriteAccess _current = new(file, new(ProductPlan.Free,
            new(EntitlementScopeKind.PersonalProfile, Guid.Parse("d6290000-0000-4000-8000-000000000001"))));
        public CommercialWriteAccess Capture(string databasePath) => _current;
    }
}
