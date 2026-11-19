using System.Collections.Frozen;
using Vafadar.Zanance.Core.Analysis;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Api.Analysis;

/// <summary>Fixed comparison and empty-period fixtures for the six app languages, with no real account data.</summary>
internal static class AiPilotLocaleFixtures
{
    private static readonly AiPeriod Empty = new(new(2025, 9, 1), new(2025, 9, 30), 1, true,
        Array.AsReadOnly<PeriodTotals>([new("EUR", 0, 0, 0, 0)]), 0);

    private static readonly AiPeriod July = new(new(2025, 7, 1), new(2025, 7, 31), 2, true,
        Array.AsReadOnly<PeriodTotals>([new("EUR", 100000, 0, 50000, 5000), new("USD", 32000, 0, 12000, 1000)]), 0);

    private static readonly AiPeriod August = new(new(2025, 8, 1), new(2025, 8, 31), 2, true,
        Array.AsReadOnly<PeriodTotals>([new("EUR", 90000, 2000, 60000, 15000), new("USD", 40000, 1000, 14000, 1000)]), 0);

    private static readonly (string Code, string Name)[] Languages =
        [("en", "English"), ("fa", "Persian"), ("de", "German"), ("es", "Spanish"), ("fr", "French"), ("it", "Italian")];

    private static readonly FrozenDictionary<string, Fixture> Corpus = Languages
        .SelectMany(language => new[] { NoData(language.Code, language.Name), Comparison(language.Code, language.Name) })
        .ToFrozenDictionary(item => AiProtocol.Digest(item.Payload), StringComparer.Ordinal);

    /// <summary>One exact fixture and the facts the model must return without change.</summary>
    internal sealed record Fixture(AiPayload Payload, string Prompt, IReadOnlyDictionary<string, object> Facts);

    /// <summary>Returns only the fixed approved locale scenario, never a user-defined aggregate or prompt.</summary>
    internal static Fixture? Match(AiPurpose purpose, string digest) =>
        Corpus.TryGetValue(digest, out var fixture) && fixture.Payload.Purpose == purpose ? fixture : null;

    /// <summary>Enumerates the frozen fictional corpus for evaluation.</summary>
    internal static IEnumerable<Fixture> All => Corpus.Values;

    private static Fixture NoData(string language, string name) => new(new(1, AiPurpose.PeriodExplanation, language, Empty, null),
        $"Synthetic Zanance empty period. The aggregate has one EUR account and no recorded activity in the selected period. The local net spending is zero. Return net_expenses_minor=0. In explanation, write one brief sentence in {name}. Describe only the absence of activity in the supplied period. Do not infer missing records, use digits or give advice.",
        new Dictionary<string, object> { ["net_expenses_minor"] = 0 }.ToFrozenDictionary(StringComparer.Ordinal));

    private static Fixture Comparison(string language, string name)
    {
        var facts = new Dictionary<string, object> { ["can_combine"] = false };
        for (var index = 0; index < August.Totals.Count; index++)
        {
            var current = August.Totals[index];
            var previous = July.Totals[index];
            var code = current.CurrencyCode.ToLowerInvariant();
            facts[$"{code}_spending_change"] = checked((int)(current.NetExpense - previous.NetExpense));
            facts[$"{code}_cash_flow_change"] = checked((int)(current.Result - previous.Result));
        }
        var raw = string.Join(" ", July.Totals.Select(total => Format("Previous", total))
            .Concat(August.Totals.Select(total => Format("Current", total))));
        var exact = string.Join(", ", facts.Where(item => item.Value is int)
            .Select(item => FormattableString.Invariant($"{item.Key}={item.Value}")));
        return new(new(1, AiPurpose.PeriodComparison, language, August, July),
            $"Synthetic Zanance comparison. Figures are minor units. {raw} Copy these locally calculated current-minus-previous results exactly: {exact}. Set can_combine=false because no conversion is available. In explanation, write one brief sentence in {name}. Explain that refunds reduce spending and discuss the currencies separately. Do not use digits, combine currencies, speculate about causes or give advice.",
            facts.ToFrozenDictionary(StringComparer.Ordinal));

        static string Format(string caption, PeriodTotals total) => FormattableString.Invariant(
            $"{caption} {total.CurrencyCode}: income {total.Income}, income reversals {total.IncomeReversals}, expenses {total.GrossExpense}, refunds {total.Refunds}.");
    }
}
