using Vafadar.Zanance.Core.Forecasts;

namespace Vafadar.Zanance.Core.Analysis;

/// <summary>One anonymous, signed path event. Excluded events never affect the projected balance.</summary>
public sealed record AiForecastEvent(DateOnly On, long? Effect, ForecastSource Source, bool Estimated,
    bool Excluded, bool Moved, bool AssumedAmount);

/// <summary>One original projection, including its first negative point separately from its minimum.</summary>
public sealed record AiForecastPath(long Opening, long Closing, long Minimum, DateOnly MinimumOn,
    DateOnly? FirstShortfallOn, long? FirstShortfallBalance, int UnknownCount, int EventCount,
    IReadOnlyList<AiForecastEvent> Events);

/// <summary>Same-file, same-currency, same-horizon baseline and optional original local scenario.</summary>
public sealed record AiForecastFacts(string Currency, AiForecastPath Baseline, AiForecastPath? Scenario);

/// <summary>Projects a bounded fact list from existing calculator outputs, leaving names and IDs out of the wire model.</summary>
public static class AiForecastEvidence
{
    /// <summary>Prioritizes changed rows and events at the first shortfall, then earlier outgoing and unknown amounts.</summary>
    public static (AiForecastPath Path, IReadOnlyList<string> Labels) Project(CurrencyForecast source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var first = source.Path.Where(point => point.Balance < 0).Select(point => (ForecastPoint?)point).FirstOrDefault();
        var rows = source.Items.OrderByDescending(item => item.IsExcluded || item.IsMoved || item.IsAmountAssumed)
            .ThenByDescending(item => first is { } crossing && item.Date == crossing.Date && item.Effect < 0)
            .ThenByDescending(item => item.Effect is null || item.Effect < 0 && (first is null || item.Date <= first.Value.Date))
            .ThenBy(item => item.Date).Take(AiProtocol.MaximumReportRows).ToArray();
        return (new(source.StartBalance, source.EndBalance, source.Minimum, source.MinimumDate,
            first?.Date, first?.Balance, source.UnknownCount, source.Items.Count, Array.AsReadOnly(rows.Select(item =>
                new AiForecastEvent(item.Date, item.Effect, item.Source, item.IsEstimate, item.IsExcluded, item.IsMoved, item.IsAmountAssumed)).ToArray())),
            Array.AsReadOnly(rows.Select(item => item.Name).ToArray()));
    }
}
