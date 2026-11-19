namespace Vafadar.Zanance.App.Features.Analysis;

/// <summary>One read-only navigation packet from the displayed financial period. It grants no disclosure authority.</summary>
internal sealed record AiPeriodHandoff(string File, DateOnly From, DateOnly To, bool ConfirmedOnly)
{
    internal const string QueryKey = "analysisPeriod";
    internal IReadOnlyList<Guid> Accounts { get; private init; } = [];
    internal bool ComparePrevious { get; init; }

    internal static AiPeriodHandoff? Capture(string file, DateOnly from, DateOnly to, bool confirmedOnly, IEnumerable<Guid> accounts)
    {
        var original = accounts.Distinct().Order().ToArray();
        return original.Length == 0 ? null : new(file, from, to, confirmedOnly) { Accounts = Array.AsReadOnly(original) };
    }

    internal Dictionary<string, object> Query(bool compare) => new() { [QueryKey] = this with { ComparePrevious = compare } };
}
