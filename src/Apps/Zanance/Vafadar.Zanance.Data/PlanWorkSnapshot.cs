using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Data;

/// <summary>Original plan/state inputs and their independently evaluated new-work policy for one actual opened file.</summary>
/// <param name="Schedules">Complete original schedules, also used by calculations and historical views.</param>
/// <param name="States">Complete original occurrence overrides and settlements.</param>
/// <param name="Work">The separate new-work projection; never mutate schedules to represent read-only access.</param>
public sealed record PlanWorkSnapshot(List<Schedule> Schedules, List<OccurrenceState> States, PlanWorkPolicy Work);
