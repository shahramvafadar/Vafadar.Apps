using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original complete schedule formulas, read ownership, source handoff and publication follow-through.</summary>
public sealed class LoanSchedulePublicationFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static LoanWorkStore Reader(FlowFixture f) => new(AccountDetailOriginalBoundaryTests.Factory(f), f.Services.GetRequiredService<ICommercialWriteAccessSource>(), f.Time);

    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task Original_loan_or_lent_calculator_and_every_original_row_remain_exact(bool lent)
    {
        using var f = new FlowFixture(); var account = await AccountDetailOriginalBoundaryTests.AccountAsync(f, AccountType.Loan);
        if (lent)
        {
            await using var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct);
            await db.Accounts.Where(item => item.Id == account.Id).ExecuteUpdateAsync(set => set.SetProperty(item => item.Type, AccountType.Lent).SetProperty(item => item.OpeningBalance, 100000), Ct);
        }
        var review = await Reader(f).ReadAsync(Ct); var original = review.Accounts.Single(); var schedule = review.Schedule(original);
        var vm = LoanScheduleOriginalBoundaryTests.Create(f); LoanScheduleOriginalBoundaryTests.Open(vm, account.Id); await vm.LoadAsync();
        Assert.True(vm.Loading.IsReady); Assert.Equal(schedule.Installments.Count, vm.Rows.Count);
        string Money(long value) => MoneyText.Format(value, "EUR", f.Localization.CurrentCulture);
        var dates = new DateFormatter(f.Localization);
        Assert.Equal(f.Translator.Format("Loan_ScheduleSummary", Money(review.Outstanding(original)), schedule.Installments.Count, Money(schedule.TotalInterest)), vm.SummaryText);
        Assert.Equal(schedule.Installments.Select(row => new LoanRow(f.Translator.Format("Loan_Row", row.Number, dates.Format(row.Date, DateFormatStyle.MonthYear)),
            f.Translator.Format("Loan_RowSplit", Money(row.Principal), Money(row.Interest)), Money(row.Payment), f.Translator.Format("Loan_RowRemaining", Money(row.Remaining)))), vm.Rows);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Actual_page_departure_cancels_pending_reader_without_late_name_rows_or_ready()
    {
        using var f = new FlowFixture(); var account = await AccountDetailOriginalBoundaryTests.AccountAsync(f, AccountType.Loan);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(f)) { Before = async ct => { entered.TrySetResult(); await release.Task.WaitAsync(ct); } };
        var vm = LoanScheduleOriginalBoundaryTests.Create(f, factory); LoanScheduleOriginalBoundaryTests.Open(vm, account.Id); vm.SetActive(true); var load = vm.LoadAsync();
        await entered.Task.WaitAsync(Ct); vm.SetActive(false); release.SetResult(); await load;
        Assert.Null(vm.Name); Assert.Empty(vm.Rows); Assert.False(vm.Loading.IsReady);
        await vm.RefreshCommand.ExecuteAsync(null); Assert.False(vm.Loading.IsReady);
    }

    [Fact]
    public async Task Query_change_from_first_publication_callback_cannot_publish_old_summary_rows_or_ready()
    {
        using var f = new FlowFixture(); var account = await AccountDetailOriginalBoundaryTests.AccountAsync(f, AccountType.Loan);
        var vm = LoanScheduleOriginalBoundaryTests.Create(f); LoanScheduleOriginalBoundaryTests.Open(vm, account.Id); var changed = false;
        vm.PropertyChanged += (_, args) => { if (changed || args.PropertyName != nameof(vm.Name) || vm.Name is null) return; changed = true; LoanScheduleOriginalBoundaryTests.Open(vm, Guid.NewGuid()); };
        await vm.LoadAsync(); Assert.True(changed); Assert.False(vm.Loading.IsReady); Assert.Null(vm.Name); Assert.Null(vm.SummaryText); Assert.Empty(vm.Rows);
    }

    [Fact]
    public async Task Local_failure_and_explicit_retry_do_not_create_settings_or_financial_entries()
    {
        using var f = new FlowFixture(); var account = await AccountDetailOriginalBoundaryTests.AccountAsync(f, AccountType.Loan); var fail = true;
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(f))
        { Before = _ => { if (fail) { fail = false; throw new IOException("Declared original schedule read refusal."); } return Task.CompletedTask; } };
        var vm = LoanScheduleOriginalBoundaryTests.Create(f, factory); LoanScheduleOriginalBoundaryTests.Open(vm, account.Id); await vm.LoadAsync();
        Assert.True(vm.Loading.HasFailed); Assert.NotNull(vm.Feedback); Assert.Null(vm.Name); Assert.Empty(vm.Rows);
        await vm.RefreshCommand.ExecuteAsync(null); Assert.True(vm.Loading.IsReady); Assert.Equal(account.Name, vm.Name); Assert.Null(vm.Feedback);
        await using var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct); Assert.Empty(await db.Settings.ToListAsync(Ct)); Assert.Empty(await db.Entries.ToListAsync(Ct));
    }

    [Fact]
    public async Task Original_handoff_refuses_a_foreign_file_even_after_explicit_retry()
    {
        using var f = new FlowFixture(); using var other = new FlowFixture(); var account = await AccountDetailOriginalBoundaryTests.AccountAsync(f, AccountType.Loan);
        var original = await Reader(f).ReadAsync(Ct);
        await using (var db = await AccountDetailOriginalBoundaryTests.Factory(other).CreateDbContextAsync(Ct))
        {
            var replacement = new Account { Name = "Foreign restored same id", Type = AccountType.Loan, CurrencyCode = "EUR", OpeningBalance = -200000, OpeningDate = account.OpeningDate, Installment = 10000, InterestRate = 12 };
            db.Accounts.Add(replacement); db.Entry(replacement).Property(item => item.Id).CurrentValue = account.Id; await db.SaveChangesAsync(Ct);
        }
        var vm = LoanScheduleOriginalBoundaryTests.Create(f, AccountDetailOriginalBoundaryTests.Factory(other));
        vm.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = account.Id, ["loanReview"] = original }); await vm.LoadAsync();
        Assert.True(vm.Loading.HasFailed); Assert.Empty(vm.Rows); Assert.Null(vm.Name);
        await vm.RefreshCommand.ExecuteAsync(null); Assert.True(vm.Loading.HasFailed); Assert.Empty(vm.Rows); Assert.Null(vm.Name);
    }

    [Fact]
    public async Task Display_change_retires_held_original_read_and_publishes_only_current_captions()
    {
        using var f = new FlowFixture(); var account = await AccountDetailOriginalBoundaryTests.AccountAsync(f, AccountType.Loan);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var first = true;
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(f)) { Before = async ct => { if (!first) return; first = false; entered.SetResult(); await release.Task.WaitAsync(ct); } };
        var vm = LoanScheduleOriginalBoundaryTests.Create(f, factory); LoanScheduleOriginalBoundaryTests.Open(vm, account.Id); vm.SetActive(true); var old = vm.LoadAsync();
        await entered.Task.WaitAsync(Ct); f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(item => item.CultureName == "de")); release.SetResult(); await old; await vm.LoadAsync();
        Assert.True(vm.Loading.IsReady); Assert.Equal(account.Name, vm.Name); Assert.NotEmpty(vm.Rows);
        var review = await Reader(f).ReadAsync(Ct); var schedule = review.Schedule(review.Accounts.Single());
        Assert.Equal(f.Translator.Format("Loan_ScheduleSummary", MoneyText.Format(100000, "EUR", f.Localization.CurrentCulture), schedule.Installments.Count, MoneyText.Format(schedule.TotalInterest, "EUR", f.Localization.CurrentCulture)), vm.SummaryText);
        vm.SetActive(false);
    }

    [Fact]
    public async Task Shared_original_appearance_reads_have_one_source_and_validation_and_one_publication()
    {
        using var f = new FlowFixture(); var account = await AccountDetailOriginalBoundaryTests.AccountAsync(f, AccountType.Loan);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var calls = 0;
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(f)) { Before = async ct => { if (Interlocked.Increment(ref calls) != 1) return; entered.SetResult(); await release.Task.WaitAsync(ct); } };
        var vm = LoanScheduleOriginalBoundaryTests.Create(f, factory); LoanScheduleOriginalBoundaryTests.Open(vm, account.Id); var resets = 0; vm.Rows.CollectionChanged += (_, _) => resets++;
        var first = vm.LoadAsync(); await entered.Task.WaitAsync(Ct); var second = vm.LoadAsync(); release.SetResult(); await Task.WhenAll(first, second);
        Assert.True(vm.Loading.IsReady); Assert.Equal(2, calls); Assert.Equal(1, resets);
    }

    [Fact]
    public async Task Day_changed_during_original_acquisition_refuses_then_explicit_retry_uses_current_original_day()
    {
        using var f = new FlowFixture(); var account = await AccountDetailOriginalBoundaryTests.AccountAsync(f, AccountType.Loan); var first = true;
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(f)) { Before = _ => { if (first) { first = false; f.Time.Now = f.Time.Now.AddDays(1); } return Task.CompletedTask; } };
        var vm = LoanScheduleOriginalBoundaryTests.Create(f, factory); LoanScheduleOriginalBoundaryTests.Open(vm, account.Id); await vm.LoadAsync(); Assert.True(vm.Loading.HasFailed); Assert.Null(vm.Name); Assert.Empty(vm.Rows);
        await vm.RefreshCommand.ExecuteAsync(null); Assert.True(vm.Loading.IsReady); Assert.NotEmpty(vm.Rows);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
}
