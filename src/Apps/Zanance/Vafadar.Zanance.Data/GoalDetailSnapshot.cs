using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Settings;

namespace Vafadar.Zanance.Data;

/// <summary>Complete original detail financial inputs from one actual keyed file and deferred transaction.</summary>
/// <param name="Finances">Original goal/work/account/ledger/holding packet.</param>
/// <param name="Settings">Original preferences, or detached defaults when no row exists; never initialized here.</param>
/// <param name="Categories">Original complete ordered categories for essential coverage.</param>
/// <param name="Schedules">Original ordered plans used by coverage and capacity.</param>
/// <param name="States">Original stored occurrence states for capacity.</param>
public sealed record GoalDetailSnapshot(GoalListSnapshot Finances, ZananceSettings Settings,
    List<Category> Categories, List<Schedule> Schedules, List<OccurrenceState> States);
