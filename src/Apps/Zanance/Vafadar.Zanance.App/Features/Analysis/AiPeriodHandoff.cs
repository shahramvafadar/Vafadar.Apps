using Vafadar.Zanance.Core.Analysis;

namespace Vafadar.Zanance.App.Features.Analysis;

/// <summary>One read-only navigation packet from the displayed financial period. It grants no disclosure authority.</summary>
internal sealed record AiPeriodHandoff(string File, DateOnly From, DateOnly To, bool ConfirmedOnly)
{
    internal const string QueryKey = "analysisPeriod";
    internal IReadOnlyList<Guid> Accounts { get; private init; } = [];
    internal bool ComparePrevious { get; init; }
    internal CancellationToken FeatureSession { get; private init; }
    internal AiReportDetails? Report { get; init; }
    internal AiFundingFacts? Funding { get; init; }
    internal AiForecastFacts? Forecast { get; init; }
    internal bool IsDetailed => Report is not null || Funding is not null || Forecast is not null;
    internal Func<Task>? OpenDraftAsync { get; init; }
    internal DateOnly? CompareFrom { get; init; }
    internal DateOnly? CompareTo { get; init; }
    internal IReadOnlyList<string> ReportLabels { get; init; } = [];
    internal Func<CancellationToken, Task>? ValidateReportAsync { get; init; }

    internal static AiPeriodHandoff? Capture(string file, DateOnly from, DateOnly to, bool confirmedOnly, IEnumerable<Guid> accounts)
    {
        var original = accounts.Distinct().Order().ToArray();
        return original.Length == 0 ? null : new(file, from, to, confirmedOnly) { Accounts = Array.AsReadOnly(original) };
    }

    internal Dictionary<string, object> Query(bool compare, CancellationToken feature)
        => new() { [QueryKey] = this with { ComparePrevious = compare, FeatureSession = feature } };
}
