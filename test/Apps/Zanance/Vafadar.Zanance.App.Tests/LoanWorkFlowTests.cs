using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual loan schedule/session, original financial calculators and the real atomic ledger writer.</summary>
[Trait("AT", "AT-172")]
public sealed class LoanWorkFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly Guid Scope = Guid.Parse("71717171-6262-5353-4444-353535353535");
    private sealed class Access : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        public CommercialWriteAccess Capture(string databasePath) => Current;
    }
    private sealed class Factory(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
    {
        internal IDbContextFactory<ZananceDbContext> Current = original;
        public ZananceDbContext CreateDbContext() => Current.CreateDbContext();
        public Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Current.CreateDbContextAsync(cancellationToken);
    }
    private sealed class Native : IAppInteraction
    {
        internal string? Choice;
        internal bool Confirm = true;
        internal int Choices, Confirmations;
        internal string[] Labels = [];
        internal Func<Task>? DuringChoice, DuringConfirm;
        public Task AlertAsync(string title, string message, string cancel) => Task.CompletedTask;
        public async Task<string?> ChooseAsync(string title, string cancel, params string[] actions)
        { Choices++; Labels = actions; if (DuringChoice is not null) await DuringChoice(); return Choice; }
        public async Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
        { Confirmations++; if (DuringConfirm is not null) await DuringConfirm(); return Confirm; }
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1) => throw new InvalidOperationException("Unexpected loan prompt.");
        public Task NavigateAsync(string route, IDictionary<string, object>? parameters = null) => throw new InvalidOperationException("Unexpected loan navigation.");
        public Task ShowFailureAsync(Exception exception) => throw new InvalidOperationException("Unexpected loan exception publication.");
    }
    private static ServiceProvider Provider(FlowFixture f, Access access) => new ServiceCollection().AddSingleton<TimeProvider>(f.Time)
        .AddSingleton<ICommercialWriteAccessSource>(access).AddZananceData(f.Directory.Combine("zanance.db")).BuildServiceProvider();
    private static void Enable(FlowFixture f, Access access, ProductPlan plan = ProductPlan.Free, bool shared = false, bool member = true, bool host = true) =>
        access.Current = new(f.Directory.Combine("zanance.db"), new(plan, new(shared ? EntitlementScopeKind.SharedSpace : EntitlementScopeKind.PersonalProfile, Scope),
            shared ? new(Scope, member, host) : null));
    private static async Task<(Account Loan, Account Cash)> SeedAsync(FlowFixture f, bool lent = false, long payment = 10000, bool secondCash = false)
    {
        // The real startup creates original system categories before these financial entry points are available.
        await f.Store.EnsureDefaultCategoriesAsync(Ct);
        var loan = new Account { Name = "Original reviewed debt", CurrencyCode = "EUR", Type = lent ? AccountType.Lent : AccountType.Loan,
            OpeningBalance = lent ? 100000 : -100000, OpeningDate = new(2026, 10, 1), Installment = payment, InterestRate = 12 };
        var cash = new Account { Name = "Original cash", CurrencyCode = "EUR", OpeningBalance = 100000, OpeningDate = new(2026, 10, 1) };
        await f.Store.SaveAccountAsync(loan, Ct); await f.Store.SaveAccountAsync(cash, Ct);
        if (secondCash) await f.Store.SaveAccountAsync(new Account { Name = cash.Name, CurrencyCode = "EUR", OpeningDate = cash.OpeningDate }, Ct);
        return (loan, cash);
    }
    private static LoanInstallmentSession Session(FlowFixture f, ServiceProvider p, Native native) =>
        new(p.GetRequiredService<LoanWorkStore>(), p.GetRequiredService<ZananceStore>(), f.Translator, f.Localization, native);
    private static LoanScheduleViewModel Schedule(FlowFixture f, ServiceProvider p, Guid id) => Open(new(p.GetRequiredService<LoanWorkStore>(), f.Translator, f.Localization,
        new Vafadar.Localization.Formatting.DateFormatter(f.Localization)), id);
    private static LoanScheduleViewModel Open(LoanScheduleViewModel vm, Guid id) { vm.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = id }); return vm; }
    [Theory]
    [InlineData("en")][InlineData("fa")][InlineData("de")][InlineData("es")][InlineData("fr")][InlineData("it")]
    public async Task Free_retains_original_balance_and_essential_warning_without_computing_or_saving_paid_installment(string language)
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f, payment: 500); f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(item => item.CultureName == language));
        var access = new Access(); using var p = Provider(f, access); Enable(f, access); var reader = p.GetRequiredService<LoanWorkStore>(); var review = await reader.ReadAsync(Ct);
        var account = review.Accounts.Single(item => item.Id == seed.Loan.Id); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        Assert.Equal(-100000, review.Balance(account)); Assert.Equal(100000, review.Outstanding(account)); Assert.True(review.HasUnderpaymentWarning(account));
        Assert.Throws<CommercialWriteRejectedException>(() => review.Schedule(account)); var native = new Native(); var session = Session(f, p, native);
        await session.RecordAsync(review, account.Id); Assert.NotNull(session.Error); Assert.Equal(0, native.Confirmations); Assert.Equal(0, native.Choices); Assert.Null(session.Result);
        var vm = Schedule(f, p, account.Id); await vm.LoadAsync(); Assert.True(vm.Loading.IsReady); Assert.Empty(vm.Rows); Assert.NotNull(vm.Feedback); Assert.NotNull(vm.EssentialWarning);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Theory]
    [InlineData(false,-1)][InlineData(false,1)][InlineData(false,2)][InlineData(true,1)][InlineData(true,2)]
    public async Task Confirmed_original_loan_or_lent_payment_preserves_transfer_principal_and_only_interest_is_spending_or_income(bool lent, int plan)
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f, lent); var access = new Access(); using var p = Provider(f, access); if (plan >= 0) Enable(f, access, (ProductPlan)plan);
        var reader = p.GetRequiredService<LoanWorkStore>(); var review = await reader.ReadAsync(Ct); var native = new Native(); var session = Session(f, p, native);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var vm = Schedule(f, p, seed.Loan.Id); await vm.LoadAsync(); Assert.NotEmpty(vm.Rows); Assert.Null(vm.Feedback);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); await session.RecordAsync(review, seed.Loan.Id);
        Assert.Null(session.Error); Assert.Equal(f.Translator["Loan_Recorded"], session.Result); Assert.Equal(1, native.Confirmations);
        var entries = await f.Store.GetEntriesAsync(cancellationToken: Ct); Assert.Equal(2, entries.Count); var principal = Assert.Single(entries, item => item.Kind == EntryKind.Transfer);
        var interest = Assert.Single(entries, item => item.Kind != EntryKind.Transfer); Assert.Equal(9000, principal.Amount); Assert.Equal(1000, interest.Amount);
        Assert.Equal(lent ? EntryKind.Income : EntryKind.Expense, interest.Kind); Assert.Equal(seed.Cash.Id, interest.AccountId);
        Assert.Equal(lent ? seed.Loan.Id : seed.Cash.Id, principal.AccountId); Assert.Equal(lent ? seed.Cash.Id : seed.Loan.Id, principal.ToAccountId);
        var current = await reader.ReadAsync(Ct); Assert.Equal(91000, current.Outstanding(current.Accounts.Single(item => item.Id == seed.Loan.Id)));
        var accepted = await GoalChooserFlowTests.SnapshotAsync(f); await session.RecordAsync(review, seed.Loan.Id); Assert.Equal(1, native.Confirmations);
        Assert.Equal(f.Translator["Loan_Recorded"], session.Result); Assert.Equal(accepted, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task Cash_choice_or_confirmation_cancellation_never_records_estimated_financial_rows(bool choose)
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f, secondCash: choose); var access = new Access(); using var p = Provider(f, access);
        var review = await p.GetRequiredService<LoanWorkStore>().ReadAsync(Ct); var native = new Native { Confirm = false }; var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Session(f, p, native).RecordAsync(review, seed.Loan.Id); Assert.Equal(choose ? 1 : 0, native.Choices); Assert.Equal(choose ? 0 : 1, native.Confirmations);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Theory]
    [InlineData("terms")][InlineData("ledger")][InlineData("categories")][InlineData("cash")][InlineData("rights")][InlineData("day")]
    public async Task Native_confirmation_cannot_save_changed_original_inputs_even_when_audit_time_is_unchanged(string change)
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f); var access = new Access(); using var p = Provider(f, access); Enable(f, access, ProductPlan.Plus);
        var review = await p.GetRequiredService<LoanWorkStore>().ReadAsync(Ct); var native = new Native(); var events = 0; p.GetRequiredService<ZananceStore>().Changed += (_, _) => events++;
        native.DuringConfirm = async () =>
        {
            if (change == "rights") { Enable(f, access); return; }
            if (change == "day") { f.Time.Now = f.Time.Now.AddDays(1); return; }
            await using var db = await p.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
            await db.Database.ExecuteSqlRawAsync(change switch { "terms" => "UPDATE Accounts SET Installment=Installment+1 WHERE Name='Original reviewed debt'",
                "cash" => "UPDATE Accounts SET CurrencyCode='USD' WHERE Name='Original cash'", "categories" => "UPDATE Categories SET Name='Changed original labels'",
                _ => "UPDATE Accounts SET OpeningBalance=OpeningBalance-100 WHERE Name='Original reviewed debt'" }, Ct);
        };
        var session = Session(f, p, native); await session.RecordAsync(review, seed.Loan.Id); Assert.NotNull(session.Error); Assert.Null(session.Result);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct)); Assert.Equal(0, events); Assert.Equal(1, native.Confirmations);
    }
    [Fact]
    public async Task Direct_writer_checks_paid_analysis_before_any_money_audit_or_notification()
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f); var access = new Access(); using var p = Provider(f, access); Enable(f, access);
        var review = await p.GetRequiredService<LoanWorkStore>().ReadAsync(Ct); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var rejected = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => p.GetRequiredService<ZananceStore>().SaveLoanInstallmentAsync(review, seed.Loan.Id, seed.Cash.Id, "Original interest", Ct));
        Assert.Equal(CommercialFeature.LoanAnalysis, rejected.Feature); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task Real_interest_insert_failure_rolls_back_whole_principal_and_interest_batch()
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f); var access = new Access(); using var p = Provider(f, access); var review = await p.GetRequiredService<LoanWorkStore>().ReadAsync(Ct);
        await using var db = await p.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectLoanInterest BEFORE INSERT ON Entries WHEN NEW.Kind=1 BEGIN SELECT RAISE(ABORT, 'Fictitious interest failure'); END;", Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var session = Session(f, p, new Native()); await session.RecordAsync(review, seed.Loan.Id);
        Assert.NotNull(session.Error); Assert.Null(session.Result); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task Real_trigger_changed_original_terms_after_insert_roll_back_both_payment_and_trigger_mutation()
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f); var access = new Access(); using var p = Provider(f, access); var review = await p.GetRequiredService<LoanWorkStore>().ReadAsync(Ct);
        await using var db = await p.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER ChangeLoanTerm AFTER INSERT ON Entries WHEN NEW.Kind=2 BEGIN UPDATE Accounts SET Installment=Installment+1 WHERE Name='Original reviewed debt'; END;", Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var session = Session(f, p, new Native()); await session.RecordAsync(review, seed.Loan.Id);
        Assert.NotNull(session.Error); Assert.Null(session.Result); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Theory]
    [InlineData("en")][InlineData("fa")][InlineData("de")][InlineData("es")][InlineData("fr")][InlineData("it")]
    public async Task Accepted_subscriber_failure_keeps_actual_payment_result_and_cannot_replay_the_old_review(string language)
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f); f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(item => item.CultureName == language));
        var access = new Access(); using var p = Provider(f, access); var review = await p.GetRequiredService<LoanWorkStore>().ReadAsync(Ct);
        p.GetRequiredService<ZananceStore>().Changed += (_, _) => throw new IOException("Fictitious original subscriber");
        var native = new Native(); var session = Session(f, p, native); await session.RecordAsync(review, seed.Loan.Id);
        Assert.Equal(f.Translator["Loan_Recorded"], session.Result); Assert.Equal(f.Translator["Loan_RecordedFollowUpFailed"], session.Error);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await session.RecordAsync(review, seed.Loan.Id); Assert.Equal(1, native.Confirmations);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Equal(2, (await f.Store.GetEntriesAsync(cancellationToken: Ct)).Count);
    }
    [Theory]
    [InlineData(0)][InlineData(1)][InlineData(2)]
    public async Task Shared_loan_analysis_does_not_inherit_personal_payment_or_guess_membership(int plan)
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f); var access = new Access(); using var p = Provider(f, access); var reader = p.GetRequiredService<LoanWorkStore>();
        Enable(f, access, (ProductPlan)plan, true, false); await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => reader.ReadAsync(Ct));
        LoanReadReview denied;
        Enable(f, access, (ProductPlan)plan, true, true, false); denied = await reader.ReadAsync(Ct); Assert.Throws<CommercialWriteRejectedException>(() => denied.Schedule(denied.Accounts.Single(item => item.Id == seed.Loan.Id)));
        Enable(f, access, (ProductPlan)plan, true); var permitted = await reader.ReadAsync(Ct); Assert.NotEmpty(permitted.Schedule(permitted.Accounts.Single(item => item.Id == seed.Loan.Id)).Installments);
    }
    [Fact]
    public async Task Absent_settings_review_is_stable_and_does_not_create_a_settings_row_during_read_validation()
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f); var access = new Access(); using var p = Provider(f, access); var reader = p.GetRequiredService<LoanWorkStore>();
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var review = await reader.ReadAsync(Ct); await reader.ValidateAsync(review, Ct);
        Assert.Equal(1, review.Settings.MonthStartDay); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task Mutable_caller_review_cannot_substitute_a_different_rate_in_an_otherwise_unchanged_writer()
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f); var access = new Access(); using var p = Provider(f, access); var review = await p.GetRequiredService<LoanWorkStore>().ReadAsync(Ct);
        review.Accounts.Single(item => item.Id == seed.Loan.Id).InterestRate = 99; var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<InvalidOperationException>(() => p.GetRequiredService<ZananceStore>().SaveLoanInstallmentAsync(review, seed.Loan.Id, seed.Cash.Id, "Changed caller estimate", Ct));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task Retired_typed_schedule_requires_explicit_refresh_and_keeps_the_original_account_identity()
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f); var access = new Access(); using var p = Provider(f, access); Enable(f, access, ProductPlan.Plus);
        var reader = p.GetRequiredService<LoanWorkStore>(); var review = await reader.ReadAsync(Ct); var vm = Schedule(f, p, seed.Loan.Id);
        vm.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = seed.Loan.Id, ["loanReview"] = review }); Enable(f, access);
        await vm.LoadAsync(); Assert.True(vm.Loading.HasFailed); Assert.False(vm.Loading.IsReady); Assert.Empty(vm.Rows);
        await vm.RefreshCommand.ExecuteAsync(null); Assert.True(vm.Loading.IsReady); Assert.NotNull(vm.Feedback); Assert.Empty(vm.Rows); Assert.Equal(seed.Loan.Name, vm.Name);
    }
    [Fact]
    public async Task Duplicate_cash_names_map_to_the_selected_original_id_and_choice_retirement_cannot_confirm()
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f, secondCash: true); var access = new Access(); using var p = Provider(f, access); Enable(f, access, ProductPlan.Plus);
        var review = await p.GetRequiredService<LoanWorkStore>().ReadAsync(Ct); var cash = review.Accounts.Where(item => item.Name == seed.Cash.Name).ToArray();
        var native = new Native { Choice = "Original cash (2)" }; var session = Session(f, p, native);
        native.DuringChoice = () => { Assert.True(session.IsBusy); return Task.CompletedTask; };
        await session.RecordAsync(review, seed.Loan.Id); Assert.Null(session.Error); Assert.Equal(new[] { "Original cash (1)", "Original cash (2)" }, native.Labels);
        Assert.All(await f.Store.GetEntriesAsync(cancellationToken: Ct), item => Assert.Equal(cash[1].Id, item.AccountId));
        var current = await p.GetRequiredService<LoanWorkStore>().ReadAsync(Ct); native.DuringChoice = () => { Enable(f, access); return Task.CompletedTask; };
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await session.RecordAsync(current, seed.Loan.Id); Assert.NotNull(session.Error); Assert.Equal(1, native.Confirmations);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task Actual_factory_switch_during_consent_and_foreign_writer_cannot_redirect_the_reviewed_payment()
    {
        using var f = new FlowFixture(); using var other = new FlowFixture(); var seed = await SeedAsync(f); await SeedAsync(other);
        var access = new Access(); var factory = new Factory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var reader = new LoanWorkStore(factory, access, f.Time); var review = await reader.ReadAsync(Ct); var native = new Native();
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var otherBefore = await GoalChooserFlowTests.SnapshotAsync(other);
        native.DuringConfirm = () => { factory.Current = other.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(); return Task.CompletedTask; };
        var session = new LoanInstallmentSession(reader, f.Store, f.Translator, f.Localization, native); await session.RecordAsync(review, seed.Loan.Id);
        Assert.NotNull(session.Error); Assert.Null(session.Result); Assert.Equal(1, native.Confirmations);
        await Assert.ThrowsAsync<InvalidOperationException>(() => other.Store.SaveLoanInstallmentAsync(review, seed.Loan.Id, seed.Cash.Id, "Foreign original file", Ct));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Equal(otherBefore, await GoalChooserFlowTests.SnapshotAsync(other));
    }
    [Fact]
    public async Task Final_installment_is_clamped_to_original_outstanding_principal_and_monthly_interest()
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f, payment: 200000); var access = new Access(); using var p = Provider(f, access);
        var review = await p.GetRequiredService<LoanWorkStore>().ReadAsync(Ct); var session = Session(f, p, new Native()); await session.RecordAsync(review, seed.Loan.Id);
        Assert.Null(session.Error); var entries = await f.Store.GetEntriesAsync(cancellationToken: Ct);
        Assert.Equal(100000, Assert.Single(entries, item => item.Kind == EntryKind.Transfer).Amount);
        Assert.Equal(1000, Assert.Single(entries, item => item.Kind == EntryKind.Expense).Amount);
        var current = await p.GetRequiredService<LoanWorkStore>().ReadAsync(Ct); Assert.Equal(0, current.Outstanding(current.Accounts.Single(item => item.Id == seed.Loan.Id)));
    }
}
