namespace Vafadar.Zanance.Core.Ledger;

/// <summary>
/// What an entry means for the numbers of every view (ZEX-S0104). Home, reports, budgets, the PDF and the KPIs all
/// classify entries with <see cref="EntryClassification.Of"/>, so one concept has one definition.
/// </summary>
public enum EntryClass
{
    /// <summary>Income and its reversals: counted in income.</summary>
    Income = 0,

    /// <summary>Expenses and their refunds: counted in spending (consumption).</summary>
    Consumption = 1,

    /// <summary>Money moved between the user's own accounts: never income or spending.</summary>
    Transfer = 2,

    /// <summary>Balance corrections after reconciliation: never income or spending.</summary>
    Correction = 3,

    /// <summary>Exchanging money for a holding or back (asset purchases and sales, later phases): never income or spending.</summary>
    Capital = 4,
}

/// <summary>The one mapping from <see cref="EntryKind"/> to <see cref="EntryClass"/>.</summary>
public static class EntryClassification
{
    /// <summary>Returns the class of <paramref name="kind"/>.</summary>
    public static EntryClass Of(EntryKind kind) => kind switch
    {
        EntryKind.Income or EntryKind.IncomeReversal => EntryClass.Income,
        EntryKind.Expense or EntryKind.Refund => EntryClass.Consumption,
        EntryKind.Transfer => EntryClass.Transfer,
        EntryKind.Adjustment => EntryClass.Correction,
        _ => EntryClass.Capital,
    };

    /// <summary>Returns whether <paramref name="kind"/> counts in income or spending.</summary>
    public static bool CountsInResult(EntryKind kind) => Of(kind) is EntryClass.Income or EntryClass.Consumption;

    /// <summary>Returns whether <paramref name="kind"/> reduces its class (an income reversal or a refund).</summary>
    public static bool IsReduction(EntryKind kind) => kind is EntryKind.IncomeReversal or EntryKind.Refund;
}
