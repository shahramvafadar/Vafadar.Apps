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
    /// <summary>Gets the native choice/confirmation/write lifetime.</summary>
    [ObservableProperty] public partial bool IsBusy { get; private set; }
    /// <summary>Gets translated retained pre-write or accepted follow-through feedback.</summary>
    [ObservableProperty] public partial string? Error { get; private set; }
    /// <summary>Gets the completed primary result independently of subscribers or native refresh.</summary>
    [ObservableProperty] public partial string? Result { get; private set; }
    /// <summary>Runs one explicitly reviewed installment, preserving original cash choice mapping and confirmation.</summary>
    public async Task RecordAsync(LoanReadReview reviewed, Guid loanId)
    {
        if (IsBusy || ReferenceEquals(reviewed, _acceptedReview) && loanId == _acceptedLoan) return;
        IsBusy = true; Error = Result = null; var accepted = false;
        try
        {
            await reader.ValidateAsync(reviewed); reviewed.Demand(CommercialFeature.LoanAnalysis);
            var account = reviewed.Accounts.Single(item => item.Id == loanId);
            if (account.Installment is not > 0 || reviewed.Outstanding(account) <= 0) return;
            var cash = reviewed.Accounts.Where(item => !item.IsArchived && !item.Type.IsOutsideCash() && item.CurrencyCode == account.CurrencyCode).ToList();
            if (cash.Count == 0)
            { await interaction.AlertAsync(translator["Loan_RecordInstallment"], translator["Loan_NoCashAccount"], translator["Common_Ok"]); return; }
            var loan = account.Type == AccountType.Loan;
            var labels = cash.Select((item, i) => cash.Count(other => other.Name == item.Name) > 1 ? $"{item.Name} ({i + 1})" : item.Name).ToArray();
            var chosen = cash.Count == 1 ? labels[0] : await interaction.ChooseAsync(translator[loan ? "Loan_PayFrom" : "Loan_ReceiveInto"], translator["Common_Cancel"], labels);
            var index = Array.IndexOf(labels, chosen); if (index < 0) return;
            await reader.ValidateAsync(reviewed); reviewed.Demand(CommercialFeature.LoanAnalysis);
            var (interest, principal) = reviewed.Split(account);
            string Money(long value) => MoneyText.Format(value, account.CurrencyCode, localization.CurrentCulture);
            var message = translator.Format(loan ? "Loan_ConfirmPay" : "Loan_ConfirmReceive", Money(principal), Money(interest), cash[index].Name);
            if (!await interaction.ConfirmAsync(translator["Loan_RecordInstallment"], message, translator["Common_Save"], translator["Common_Cancel"])) return;
            await reader.ValidateAsync(reviewed);
            SaveResult saved;
            try { saved = await writer.SaveLoanInstallmentAsync(reviewed, account.Id, cash[index].Id, translator.Format("Loan_InterestTitle", account.Name)); }
            catch (CommittedWriteNotificationException<SaveResult> completed) { saved = completed.Result; accepted = true; Error = translator["Loan_RecordedFollowUpFailed"]; }
            if (!saved.Succeeded) { Error = string.Join(Environment.NewLine, saved.Errors.Select(item => translator[$"LedgerError_{item}"])); return; }
            accepted = true; _acceptedReview = reviewed; _acceptedLoan = loanId; Result = translator["Loan_Recorded"];
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { Error = accepted ? translator["Loan_RecordedFollowUpFailed"] : CommercialFailureMessage.OrFallback(exception, translator, "Loan_ReadFailed"); }
        finally { IsBusy = false; }
    }
}
