using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Core.Commerce;

/// <summary>Original Plan operation classification shared by atomic writes and read-only editor readiness.</summary>
public static class PlanEditPolicy
{
    /// <summary>Classifies newly enabled tools independently of preserved original rules/state and financial history.</summary>
    public static CommercialFeature Feature(Schedule target, Schedule? original)
    {
        ArgumentNullException.ThrowIfNull(target);
        var counted = target.State is ScheduleState.Active or ScheduleState.Paused;
        var newUse = original is null || (counted && original.State == ScheduleState.Ended);
        var addsAdvanced = (newUse || counted) && AddsAdvancedTools(target, newUse ? null : original);
        return addsAdvanced ? CommercialFeature.AdvancedPlans : newUse ? CommercialFeature.BasicPlans : CommercialFeature.Corrections;
    }

    /// <summary>Detects newly enabled paid tools while retained tool corrections remain available after expiry.</summary>
    private static bool AddsAdvancedTools(Schedule target, Schedule? original)
    {
        static bool Weekday(Schedule s) => s.Rule.Frequency is Frequency.Monthly or Frequency.Yearly && s.Rule.DayRule.IsWeekday();
        static bool SecondDay(Schedule s) => s.Rule.Frequency == Frequency.Monthly && s.Rule.SecondDay is not null;
        static bool Shift(Schedule s) => s.Rule.WeekendShift != WeekendShift.None;
        static bool Contract(Schedule s) => s.HasContract || s.ContractReference is not null || s.ContractRenews;
        return (target.AutoPost && original?.AutoPost != true)
            || (Weekday(target) && (original is null || !Weekday(original)))
            || (SecondDay(target) && (original is null || !SecondDay(original)))
            || (Shift(target) && (original is null || !Shift(original)))
            || (Contract(target) && (original is null || !Contract(original)));
    }

 }
