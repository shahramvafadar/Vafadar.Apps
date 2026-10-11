namespace Vafadar.Zanance.Core.Commerce;

/// <summary>Original local profile activity, separate from the financial entitlement scope and paid rights.</summary>
public sealed record PersonalProfileWork
{
    /// <summary>Creates immutable original activity and an opaque complete-review stamp supplied by the file-bound source.</summary>
    public PersonalProfileWork(string? originalId, bool canStartNewWork, bool requiresSelection, string reviewStamp)
    {
        if (originalId is not null) LocalProfileWorkPolicy.ValidateId(originalId);
        ArgumentException.ThrowIfNullOrWhiteSpace(reviewStamp);
        if (canStartNewWork && (requiresSelection || originalId is null))
            throw new ArgumentException("An unresolved profile choice cannot start new work.", nameof(canStartNewWork));
        OriginalId = originalId;
        CanStartNewWork = canStartNewWork;
        RequiresSelection = requiresSelection;
        ReviewStamp = reviewStamp;
    }

    /// <summary>Gets the exact catalogue id, including the original empty main id; absent when no original can be resolved.</summary>
    public string? OriginalId { get; }
    /// <summary>Gets whether this original may start local financial work; it grants no paid feature.</summary>
    public bool CanStartNewWork { get; }
    /// <summary>Gets whether a missing or invalid device choice needs explicit review.</summary>
    public bool RequiresSelection { get; }
    /// <summary>Gets the opaque original file/catalogue/choice/provider stamp used to reject retired operations.</summary>
    public string ReviewStamp { get; }
}
