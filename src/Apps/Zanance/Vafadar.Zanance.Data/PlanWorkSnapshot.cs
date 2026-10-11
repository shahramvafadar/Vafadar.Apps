using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Data;

/// <summary>Original plan/state inputs and their independently evaluated new-work policy for one actual opened file.</summary>
/// <param name="Schedules">Complete original schedules, also used by calculations and historical views.</param>
/// <param name="States">Complete original occurrence overrides and settlements.</param>
/// <param name="Work">The separate new-work projection; never mutate schedules to represent read-only access.</param>
public sealed record PlanWorkSnapshot(List<Schedule> Schedules, List<OccurrenceState> States, PlanWorkPolicy Work)
{
    /// <summary>Gets the actual file's opaque local notification identity; it grants no rights.</summary>
    public string ReminderScope { get; internal init; } = string.Empty;

    /// <summary>Gets the actual captured file, retained only inside the data boundary.</summary>
    internal string DatabasePath { get; init; } = string.Empty;

    /// <summary>Gets the cached-rights guard captured by the real read operation.</summary>
    internal Commerce.CommercialFileAccess? Access { get; init; }
}
