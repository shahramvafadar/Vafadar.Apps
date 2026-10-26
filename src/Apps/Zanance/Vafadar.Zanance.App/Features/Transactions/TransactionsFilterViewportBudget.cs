namespace Vafadar.Zanance.App.Features.Transactions;

/// <summary>Reserves original transaction-result space from the actual arranged header and separate contextual rows.</summary>
internal static class TransactionsFilterViewportBudget
{
    /// <summary>Bounds only the original scrollable filters, including header safe-area space without a fixed inset.</summary>
    internal static double Calculate(double height, double arrangedHeaderBottom, double contextualRows, double bottomPadding) =>
        Math.Max(44, height - arrangedHeaderBottom - contextualRows - bottomPadding - Math.Min(144, height / 3));
}
