namespace Vafadar.Zanance.App.Features.Goals;

/// <summary>Original financial meaning of the goal color, independent of native theme resources.</summary>
public enum GoalTone
{
    /// <summary>An active goal with no financial warning.</summary>
    Saving,
    /// <summary>An essential shortfall or incomplete-data warning.</summary>
    Warning,
    /// <summary>The recorded progress has reached the target.</summary>
    Reached,
    /// <summary>An original completed or archived goal.</summary>
    Historical,
}
