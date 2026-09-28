using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Ledger;
using Vafadar.Finance.Core.Money;
using Vafadar.Finance.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Finance.App.Features.Accounts;

/// <summary>One estimated installment in the schedule.</summary>
public sealed record LoanRow(string Title, string Split, string Payment, string Remaining);

/// <summary>
/// The estimated repayment schedule of a loan or money lent (F2-DEBT-02), calculated from the current balance and the
/// rate and installment the user entered. It is an estimate, never the contract.
/// </summary>
public sealed partial class LoanScheduleViewModel(
    FinanceStore store,
    Translator translator,
    ILocalizationService localization,
    IDateFormatter dates,
    TimeProvider time) : ViewModelBase, IQueryAttributable
{
    private Guid _id;

    public ObservableCollection<LoanRow> Rows { get; } = [];

    [ObservableProperty]
    public partial string? Name { get; set; }

    [ObservableProperty]
    public partial string? SummaryText { get; set; }

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.TryGetValue("id", out var value) && value is Guid id)
        {
            _id = id;
        }
    }

    public async Task LoadAsync()
    {
        Rows.Clear();
        if ((await store.GetAccountsAsync()).FirstOrDefault(a => a.Id == _id) is not { Installment: > 0 } account)
        {
            IsEmpty = true;
            return;
        }

        var culture = localization.CurrentCulture;
        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        string Money(long value) => MoneyText.Format(value, account.CurrencyCode, culture);
        var outstanding = LoanCalculator.Outstanding(account.Type, LedgerCalculator.Balance(account, await store.GetEntriesAsync(), today));
        var schedule = LoanCalculator.Schedule(outstanding, account.InterestRate ?? 0, account.Installment.Value, today.AddMonths(1));
        Name = account.Name;
        SummaryText = schedule.PaysOff
            ? translator.Format("Loan_ScheduleSummary", Money(outstanding), schedule.Installments.Count, Money(schedule.TotalInterest))
            : translator["Loan_NeverPaidOff"];
        foreach (var row in schedule.Installments)
        {
            Rows.Add(new LoanRow(
                translator.Format("Loan_Row", row.Number, dates.Format(row.Date, DateFormatStyle.MonthYear)),
                translator.Format("Loan_RowSplit", Money(row.Principal), Money(row.Interest)),
                Money(row.Payment),
                translator.Format("Loan_RowRemaining", Money(row.Remaining))));
        }

        IsEmpty = Rows.Count == 0;
    }
}