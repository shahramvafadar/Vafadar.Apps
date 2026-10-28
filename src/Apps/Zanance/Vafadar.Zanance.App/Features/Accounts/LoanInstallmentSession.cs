using CommunityToolkit.Mvvm.ComponentModel;
using Vafadar.Localization;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Features.Accounts;

/// <summary>Original explicit installment consent and atomic payment lifetime; no estimate is posted merely by opening a page.</summary>
public sealed partial class LoanInstallmentSession(LoanWorkStore reader, ZananceStore writer, Translator translator,
    ILocalizationService localization, IAppInteraction interaction) : ObservableObject
{
    private LoanReadReview? _acceptedReview;
    private Guid _acceptedLoan;
    private Guid _ownerLoan;
    private bool _ownerActive;
    private bool _ownerConfigured;
    private long _ownerRevision;
    /// <summary>Gets the actual last accepted loan independently of a retired native page or failed notification.</summary>
    public Guid? AcceptedLoanId => _acceptedLoan == Guid.Empty ? null : _acceptedLoan;
    /// <summary>Gets the native choice/confirmation/write lifetime.</summary>
    [ObservableProperty] public partial bool IsBusy { get; private set; }
    /// <summary>Gets translated retained pre-write or accepted follow-through feedback.</summary>
    [ObservableProperty] public partial string? Error { get; private set; }
    /// <summary>Gets the completed primary result independently of subscribers or native refresh.</summary>
    [ObservableProperty] public partial string? Result { get; private set; }

    /// <summary>Retires the old original page's consent while retaining a known accepted payment and another owner's drafts.</summary>
    public void SetOwner(Guid loanId, bool active)
    {
        if (_ownerConfigured && _ownerLoan == loanId && _ownerActive == active) return;
        Interlocked.Increment(ref _ownerRevision);
        if (_ownerLoan != loanId) Error = Result = null;
        _ownerConfigured = true; _ownerLoan = loanId; _ownerActive = active;
    }

    private string DisplayIdentity() => $"{localization.CurrentLanguage.CultureName}|{localization.CurrentCulture.Name}|{localization.CurrentCalendar}|{localization.CurrentDigits}|{localization.FormattingCultureName}";

    /// <summary>Runs one original reviewed installment only while its page, choices, display and writer authority stay current.</summary>
    public async Task RecordAsync(LoanReadReview reviewed, Guid loanId, Func<bool>? isCurrent = null)
    {
        var revision = Volatile.Read(ref _ownerRevision); var display = DisplayIdentity();
        bool OwnerCurrent() => revision == Volatile.Read(ref _ownerRevision) && (!_ownerConfigured || _ownerActive && _ownerLoan == loanId)
            && display == DisplayIdentity();
        bool Current() => OwnerCurrent() && isCurrent?.Invoke() != false;
        if (IsBusy || !Current() || ReferenceEquals(reviewed, _acceptedReview) && loanId == _acceptedLoan) return;
        IsBusy = true; Error = Result = null; var accepted = false;
        try
        {
            await reader.ValidateAsync(reviewed); if (!Current()) return; reviewed.Demand(CommercialFeature.LoanAnalysis);
            var account = reviewed.Accounts.Single(item => item.Id == loanId);
            if (account.Installment is not > 0 || reviewed.Outstanding(account) <= 0) return;
            var cash = reviewed.Accounts.Where(item => !item.IsArchived && !item.Type.IsOutsideCash() && item.CurrencyCode == account.CurrencyCode).ToList();
            if (cash.Count == 0)
            { if (Current()) await interaction.AlertAsync(translator["Loan_RecordInstallment"], translator["Loan_NoCashAccount"], translator["Common_Ok"]); return; }
            var loan = account.Type == AccountType.Loan;
            var labels = cash.Select((item, i) => cash.Count(other => other.Name == item.Name) > 1 ? $"{item.Name} ({i + 1})" : item.Name).ToArray();
            var chosen = cash.Count == 1 ? labels[0] : await interaction.ChooseAsync(translator[loan ? "Loan_PayFrom" : "Loan_ReceiveInto"], translator["Common_Cancel"], labels);
            if (!Current()) return;
            var index = Array.IndexOf(labels, chosen); if (index < 0) return;
            await reader.ValidateAsync(reviewed); if (!Current()) return; reviewed.Demand(CommercialFeature.LoanAnalysis);
            var (interest, principal) = reviewed.Split(account);
            string Money(long value) => MoneyText.Format(value, account.CurrencyCode, localization.CurrentCulture);
            var message = translator.Format(loan ? "Loan_ConfirmPay" : "Loan_ConfirmReceive", Money(principal), Money(interest), cash[index].Name);
            if (!await interaction.ConfirmAsync(translator["Loan_RecordInstallment"], message, translator["Common_Save"], translator["Common_Cancel"]) || !Current()) return;
            await reader.ValidateAsync(reviewed); if (!Current()) return;
            SaveResult saved; var notificationFailed = false;
            try { saved = await writer.SaveLoanInstallmentAsync(reviewed, account.Id, cash[index].Id, translator.Format("Loan_InterestTitle", account.Name), isCurrent: Current); }
            catch (CommittedWriteNotificationException<SaveResult> completed) { saved = completed.Result; accepted = true; notificationFailed = true; }
            if (!saved.Succeeded)
            { if (Current()) Error = string.Join(Environment.NewLine, saved.Errors.Select(item => translator[$"LedgerError_{item}"])); return; }
            // Capture a known commit before any current-view publication or subscriber can retire that view.
            accepted = true; _acceptedReview = reviewed; _acceptedLoan = loanId;
            if (OwnerCurrent()) Result = translator["Loan_Recorded"];
            if (OwnerCurrent() && notificationFailed) Error = translator["Loan_RecordedFollowUpFailed"];
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { if (accepted ? OwnerCurrent() : Current()) Error = accepted ? translator["Loan_RecordedFollowUpFailed"] : CommercialFailureMessage.OrFallback(exception, translator, "Loan_ReadFailed"); }
        finally { IsBusy = false; }
    }
}
