using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Vafadar.Localization.Formatting;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Zanance.App.Presentation;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.App.Features.Accounts;

/// <summary>One estimated installment in the schedule.</summary>
public sealed record LoanRow(string Title, string Split, string Payment, string Remaining)
{
    /// <summary>Returns what a screen reader says for the row (Windows names list rows after it).</summary>
    public override string ToString() => $"{Title}, {Payment}, {Split}, {Remaining}";
}

/// <summary>
/// The estimated repayment schedule of a loan or money lent (F2-DEBT-02), calculated from the current balance and the
/// rate and installment the user entered. It is an estimate, never the contract.
/// </summary>
public sealed partial class LoanScheduleViewModel(
    LoanWorkStore reader,
    Translator translator,
    ILocalizationService localization,
    IDateFormatter dates) : ViewModelBase
{
    private Guid _id;
    private LoanReadReview? _review;
    private LoanReadReview? _handoff;
    /// <summary>Gets complete covered original schedule publication.</summary>
    public SnapshotLoadState Loading { get; } = new();
    /// <summary>Gets complete translated access/read feedback with original inputs retained.</summary>
    [ObservableProperty] public partial string? Feedback { get; private set; }
    /// <summary>Gets the essential underpayment warning independently of paid schedule access.</summary>
    [ObservableProperty] public partial string? EssentialWarning { get; private set; }


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
        if (query.TryGetValue("loanReview", out var handoff)) _handoff = handoff as LoanReadReview ?? throw new ArgumentException("Unsupported loan review.", nameof(query));
        if (query.TryGetValue("id", out var value) && value is Guid id)
        {
            _id = id;
        }
    }

    public async Task LoadAsync()
    {
        try
        {
            await Loading.RunAsync(async () =>
            {
                if (_handoff is not null) await reader.ValidateAsync(_handoff);
                if (_review is not null) await reader.ValidateFileAsync(_review);
                var reviewed = await reader.ReadAsync();
                Rows.Clear(); Feedback = SummaryText = EssentialWarning = null;
                _review = reviewed;
                if (reviewed.Accounts.FirstOrDefault(item => item.Id == _id) is not { Installment: > 0 } account
                    || !account.Type.IsDebt() || account.IsArchived)
                { IsEmpty = true; return; }
                Name = account.Name; EssentialWarning = reviewed.HasUnderpaymentWarning(account) ? translator["Loan_NeverPaidOff"] : null;
                if (reviewed.Permission(CommercialFeature.LoanAnalysis) != FeaturePermission.Allowed)
                {
                    Feedback = CommercialFailureMessage.Describe(new CommercialWriteRejectedException(CommercialFeature.LoanAnalysis,
                        reviewed.Permission(CommercialFeature.LoanAnalysis)), translator);
                    IsEmpty = true; return;
                }
                var schedule = reviewed.Schedule(account);
                string Money(long value) => MoneyText.Format(value, account.CurrencyCode, localization.CurrentCulture);
                SummaryText = schedule.PaysOff ? translator.Format("Loan_ScheduleSummary", Money(reviewed.Outstanding(account)), schedule.Installments.Count, Money(schedule.TotalInterest))
                    : translator["Loan_NeverPaidOff"];
                foreach (var row in schedule.Installments)
                    Rows.Add(new LoanRow(translator.Format("Loan_Row", row.Number, dates.Format(row.Date, DateFormatStyle.MonthYear)),
                        translator.Format("Loan_RowSplit", Money(row.Principal), Money(row.Interest)), Money(row.Payment), translator.Format("Loan_RowRemaining", Money(row.Remaining))));
                await reader.ValidateAsync(reviewed);
                if (_handoff is not null) await reader.ValidateAsync(_handoff);
                _handoff = null; IsEmpty = Rows.Count == 0;
            }, () => { });
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { Feedback = CommercialFailureMessage.OrFallback(exception, translator, "Loan_ReadFailed"); }
    }

    /// <summary>Explicitly refreshes current original facts without moving this retained page to a foreign file.</summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (Loading.IsLoading) return;
        try { if ((_handoff ?? _review) is { } original) await reader.ValidateFileAsync(original); _handoff = null; await LoadAsync(); }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { Feedback = CommercialFailureMessage.OrFallback(exception, translator, "Loan_ReadFailed"); }
    }
}
