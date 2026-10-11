namespace Vafadar.Zanance.Data;

/// <summary>The actual writer's result for a previously discovered automatic-posting candidate.</summary>
public enum AutomaticPostStatus
{
    /// <summary>Current eligible money and settlement were committed together.</summary>
    Posted,
    /// <summary>The actual open due occurrence requires manual review, with no money written.</summary>
    NeedsReview,
    /// <summary>The candidate was removed, paused, moved into the future, skipped or already settled.</summary>
    NoLongerDue,
}
