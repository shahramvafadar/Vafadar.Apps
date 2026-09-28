using Vafadar.Finance.Core.Ledger;

namespace Vafadar.Finance.Core.Accounts;

/// <summary>One installment of a loan estimate: what is paid, how it splits and what remains afterwards.</summary>
public sealed record LoanInstallment(int Number, DateOnly Date, long Payment, long Interest, long Principal, long Remaining);

/// <summary>
/// An estimated repayment schedule. <see cref="PaysOff"/> is <see langword="false"/> when the installment does not even
/// cover the interest, or when the loan would run longer than <see cref="LoanCalculator.MaxInstallments"/>.
/// </summary>
public sealed record LoanSchedule(IReadOnlyList<LoanInstallment> Installments, long TotalInterest, bool PaysOff)
{
    /// <summary>Gets the date of the last installment, if the loan is paid off.</summary>
    public DateOnly? LastDate => PaysOff && Installments.Count > 0 ? Installments[^1].Date : null;
}

/// <summary>
/// Estimates loan repayments with a fixed nominal annual rate, monthly interest on the outstanding balance and a fixed
/// monthly installment (annuity) – the common case (F2-DEBT-02). It never claims to reproduce an actual contract: fees,
/// rate changes, day counts and special payments are not known, so the app always labels the result as an estimate.
/// Amounts are positive minor units; interest is rounded to the minor unit each month.
/// </summary>
public static class LoanCalculator
{
    /// <summary>The longest schedule that is calculated (50 years of monthly installments).</summary>
    public const int MaxInstallments = 600;

    /// <summary>Returns what is still owed on a loan, or still owed to the user on money lent, as a positive amount.</summary>
    public static long Outstanding(AccountType type, long balance) => type switch
    {
        AccountType.Loan => Math.Max(0, -balance),
        AccountType.Lent => Math.Max(0, balance),
        _ => 0,
    };

    /// <summary>Returns the interest of one month on <paramref name="outstanding"/>.</summary>
    public static long Interest(long outstanding, decimal annualRatePercent) =>
        outstanding <= 0 || annualRatePercent <= 0 ? 0 : (long)Math.Round(outstanding * annualRatePercent / 1200m, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Splits a payment into interest and principal. The interest of the month is paid first; the principal part never
    /// exceeds the outstanding amount, so a final installment may be smaller than the payment.
    /// </summary>
    public static (long Interest, long Principal) Split(long outstanding, decimal annualRatePercent, long payment)
    {
        if (payment <= 0 || outstanding <= 0)
        {
            return (0, 0);
        }

        var interest = Math.Min(Interest(outstanding, annualRatePercent), payment);
        return (interest, Math.Min(payment - interest, outstanding));
    }

    /// <summary>Returns the monthly installment that repays <paramref name="principal"/> in <paramref name="months"/>, rounded up.</summary>
    public static long Installment(long principal, decimal annualRatePercent, int months)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(months, 1);
        if (principal <= 0)
        {
            return 0;
        }

        if (annualRatePercent <= 0)
        {
            return (principal + months - 1) / months;
        }

        var rate = (double)(annualRatePercent / 1200m);
        var factor = Math.Pow(1 + rate, months);
        return (long)Math.Ceiling(principal * rate * factor / (factor - 1));
    }

    /// <summary>
    /// Calculates the installments from <paramref name="firstDate"/> on, one per month on the same day (clamped to the
    /// month end), until <paramref name="outstanding"/> is repaid.
    /// </summary>
    public static LoanSchedule Schedule(long outstanding, decimal annualRatePercent, long installment, DateOnly firstDate)
    {
        var rows = new List<LoanInstallment>();
        if (outstanding <= 0)
        {
            return new LoanSchedule(rows, 0, PaysOff: true);
        }

        if (installment <= Interest(outstanding, annualRatePercent))
        {
            return new LoanSchedule(rows, 0, PaysOff: false);
        }

        var remaining = outstanding;
        long totalInterest = 0;
        for (var number = 1; remaining > 0; number++)
        {
            if (number > MaxInstallments)
            {
                return new LoanSchedule(rows, totalInterest, PaysOff: false);
            }

            var (interest, principal) = Split(remaining, annualRatePercent, installment);
            remaining -= principal;
            totalInterest += interest;

            // Months are counted from the first date, so 31 January gives 28/29 February and then 31 March again.
            rows.Add(new LoanInstallment(number, firstDate.AddMonths(number - 1), interest + principal, interest, principal, remaining));
        }

        return new LoanSchedule(rows, totalInterest, PaysOff: true);
    }

    /// <summary>
    /// Creates the entries of one installment (F2-DEBT-01/02): the principal is a transfer between
    /// <paramref name="cashAccountId"/> and the loan account, never income or spending; the interest is a separate
    /// expense of a loan, or income of money lent. An installment without interest is only the transfer.
    /// </summary>
    public static IReadOnlyList<LedgerEntry> CreateInstallment(
        Account account, Guid cashAccountId, long outstanding, decimal annualRatePercent, long payment, DateOnly date, Guid interestCategoryId, string interestTitle)
    {
        ArgumentNullException.ThrowIfNull(account);
        if (!account.Type.IsDebt())
        {
            throw new ArgumentException("Only loans and money lent have installments.", nameof(account));
        }

        var (interest, principal) = Split(outstanding, annualRatePercent, payment);
        var loan = account.Type == AccountType.Loan;
        var entries = new List<LedgerEntry>();
        if (principal > 0)
        {
            entries.Add(new LedgerEntry
            {
                Kind = EntryKind.Transfer,
                AccountId = loan ? cashAccountId : account.Id,
                ToAccountId = loan ? account.Id : cashAccountId,
                Amount = principal,
                Date = date,
            });
        }

        if (interest > 0)
        {
            entries.Add(new LedgerEntry
            {
                Kind = loan ? EntryKind.Expense : EntryKind.Income,
                AccountId = cashAccountId,
                Amount = interest,
                Date = date,
                CategoryId = interestCategoryId,
                Title = interestTitle,
            });
        }

        return entries;
    }
}