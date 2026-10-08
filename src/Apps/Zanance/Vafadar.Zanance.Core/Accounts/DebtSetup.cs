using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Core.Accounts;

/// <summary>Converts debt-form intent into signed balances and draft repayment reminders (D-65).</summary>
public static class DebtSetup
{
    /// <summary>Returns a debt or receivable balance from a positive amount at the reference date.</summary>
    public static long OpeningBalance(AccountType type, long amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        return type switch
        {
            AccountType.Loan => -amount,
            AccountType.Lent => amount,
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };
    }

    /// <summary>
    /// Prepares an unsaved transfer reminder. Principal is deliberately unknown: an installment includes interest,
    /// which must remain a separate expense. Neither a reminder nor an estimate is a payment or a ledger entry.
    /// </summary>
    public static Schedule Reminder(Account debt, Guid moneyAccountId, DateOnly firstDue, PeriodCalendar calendar)
    {
        ArgumentNullException.ThrowIfNull(debt);
        if (!debt.Type.IsDebt())
        {
            throw new ArgumentException("A repayment reminder requires a debt or receivable.", nameof(debt));
        }

        if (moneyAccountId == Guid.Empty || moneyAccountId == debt.Id)
        {
            throw new ArgumentException("A separate money account is required.", nameof(moneyAccountId));
        }

        return new Schedule
        {
            Name = debt.Name,
            Kind = EntryKind.Transfer,
            AccountId = debt.Type == AccountType.Loan ? moneyAccountId : debt.Id,
            ToAccountId = debt.Type == AccountType.Loan ? debt.Id : moneyAccountId,
            AmountMode = AmountMode.Unknown,
            AutoPost = false,
            ReminderEnabled = true,
            Rule = new RecurrenceRule
            {
                Start = firstDue,
                Calendar = calendar,
                Frequency = debt.Installment is > 0 ? Frequency.Monthly : Frequency.Once,
            },
        };
    }
}
