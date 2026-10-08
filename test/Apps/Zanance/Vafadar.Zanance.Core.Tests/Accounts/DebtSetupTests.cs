using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Core.Tests.Debt;

/// <summary>Debt direction, sign and reminder drafts never confuse principal with spending (D-65).</summary>
[Trait("AT", "AT-72")]
public sealed class DebtSetupTests
{
    [Theory]
    [InlineData(AccountType.Loan, -100_00)]
    [InlineData(AccountType.Lent, 100_00)]
    public void Positive_amount_gets_the_sign_of_the_chosen_direction(AccountType type, long expected) =>
        Assert.Equal(expected, DebtSetup.OpeningBalance(type, 100_00));

    [Theory]
    [InlineData(AccountType.Loan)]
    [InlineData(AccountType.Lent)]
    public void Negative_input_is_rejected_instead_of_reversing_the_debt(AccountType type) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => DebtSetup.OpeningBalance(type, -1));

    [Fact]
    public void Ordinary_accounts_cannot_be_used_as_debts() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => DebtSetup.OpeningBalance(AccountType.Checking, 100));

    [Theory]
    [InlineData(AccountType.Loan)]
    [InlineData(AccountType.Lent)]
    public void Reminder_is_a_transfer_in_the_correct_direction_with_no_automatic_principal_or_interest(AccountType type)
    {
        var debt = new Account { Name = "Debt", Type = type, CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1), Installment = 185_00, InterestRate = 4.9m };
        var cash = Guid.NewGuid();
        var plan = DebtSetup.Reminder(debt, cash, new(2026, 10, 20), PeriodCalendar.Gregorian);

        Assert.Equal(EntryKind.Transfer, plan.Kind);
        Assert.Equal(type == AccountType.Loan ? cash : debt.Id, plan.AccountId);
        Assert.Equal(type == AccountType.Loan ? debt.Id : cash, plan.ToAccountId);
        Assert.Equal(AmountMode.Unknown, plan.AmountMode);
        Assert.Null(plan.Amount);
        Assert.Null(plan.ToAmount);
        Assert.False(plan.AutoPost);
        Assert.True(plan.ReminderEnabled);
        Assert.Equal(Frequency.Monthly, plan.Rule.Frequency);
        Assert.Equal([new DateOnly(2026, 10, 20), new(2026, 11, 20), new(2026, 12, 20)], Recurrence.Next(plan.Rule, plan.Rule.Start, 3).Select(x => x.Date));
        Assert.Equal(185_00, debt.Installment);
    }

    [Fact]
    public void Debt_without_installments_starts_with_a_single_reminder()
    {
        var debt = new Account { Name = "Friend", Type = AccountType.Lent, CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
        var plan = DebtSetup.Reminder(debt, Guid.NewGuid(), new(2026, 12, 1), PeriodCalendar.Persian);
        Assert.Equal(Frequency.Once, plan.Rule.Frequency);
        Assert.Equal(PeriodCalendar.Persian, plan.Rule.Calendar);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Reminder_requires_a_separate_money_account(bool sameAccount)
    {
        var debt = new Account { Name = "Debt", Type = AccountType.Loan, CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
        Assert.Throws<ArgumentException>(() => DebtSetup.Reminder(debt, sameAccount ? debt.Id : Guid.Empty, new(2026, 10, 20), PeriodCalendar.Gregorian));
    }
}
