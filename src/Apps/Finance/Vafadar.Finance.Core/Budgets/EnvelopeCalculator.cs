namespace Vafadar.Finance.Core.Budgets;

/// <summary>
/// Where the money at hand stands in an envelope budget (§10.3, BUD-11/12), all in the budget currency.
/// </summary>
/// <param name="Available">The balance of the budget's cash accounts today.</param>
/// <param name="Earmarked">The part set aside for savings goals in those accounts.</param>
/// <param name="InEnvelopes">What is still unspent in the envelopes of this month.</param>
/// <param name="Overspent">How much envelopes are overspent; that money is already gone from the balance.</param>
public sealed record EnvelopeSummary(long Available, long Earmarked, long InEnvelopes, long Overspent)
{
    /// <summary>Gets the money not assigned to an envelope or a goal; negative when more is assigned than there is.</summary>
    public long Unassigned => Available - Earmarked - InEnvelopes;

    /// <summary>Gets a value indicating whether more money is assigned than the accounts hold.</summary>
    public bool IsOverAssigned => Unassigned < 0;
}

/// <summary>
/// Envelope arithmetic. Every amount is counted once (BUD-12): spent money has already left the balance, so only the
/// unspent rest of an envelope is still assigned; goal earmarks are a separate share of the same balance; an overspent
/// envelope assigns nothing more and its overspending is shown, not subtracted again.
/// </summary>
public static class EnvelopeCalculator
{
    /// <summary>Summarises the envelopes of a month against the balance at hand.</summary>
    /// <param name="available">The balance of the budget's cash accounts.</param>
    /// <param name="earmarked">The goal earmarks in those accounts (at most their balance each).</param>
    /// <param name="envelopes">The status of each envelope (assigned amount including carry, net spending).</param>
    public static EnvelopeSummary Summarize(long available, long earmarked, IEnumerable<BudgetStatus> envelopes)
    {
        ArgumentNullException.ThrowIfNull(envelopes);
        long inEnvelopes = 0, overspent = 0;
        foreach (var envelope in envelopes)
        {
            if (envelope.Remaining >= 0)
            {
                inEnvelopes += envelope.Remaining;
            }
            else
            {
                overspent -= envelope.Remaining;
            }
        }

        return new EnvelopeSummary(available, Math.Max(0, earmarked), inEnvelopes, overspent);
    }
}