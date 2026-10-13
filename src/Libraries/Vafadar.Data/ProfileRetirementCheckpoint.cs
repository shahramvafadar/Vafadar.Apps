namespace Vafadar.Data;

/// <summary>Ordered irreversible retirement barriers; phases do not themselves authorize filesystem deletion.</summary>
public enum ProfileRetirementPhase
{
    /// <summary>The reviewed owner's deletion request has been recorded before catalogue publication.</summary>
    Authorized,
    /// <summary>The native host durably removed the reviewed profile from its device catalogue.</summary>
    CatalogueRemoved,
    /// <summary>The captured native financial/backup artifact set was removed and synchronized.</summary>
    ArtifactsRemoved,
    /// <summary>The stable identity was retired after artifact/key-reference reconciliation.</summary>
    Completed
}

/// <summary>Non-secret exact profile/installation/request identity captured for one retirement operation.</summary>
public sealed record ProfileRetirementCheckpoint
{
    /// <summary>Creates an explicit non-main profile retirement identity; no value is inferred from missing files.</summary>
    public ProfileRetirementCheckpoint(string legacyProfileId, Guid profileId, Guid installationId, Guid operationId,
        ProfileRetirementPhase phase = ProfileRetirementPhase.Authorized)
    {
        if (!Guid.TryParseExact(legacyProfileId, "N", out var legacy) || legacy == Guid.Empty || legacy.ToString("N") != legacyProfileId
            || profileId == Guid.Empty || installationId == Guid.Empty || operationId == Guid.Empty || !Enum.IsDefined(phase))
            throw new ArgumentException("Retirement requires complete canonical non-main identities and a known phase.");
        LegacyProfileId = legacyProfileId; ProfileId = profileId; InstallationId = installationId; OperationId = operationId; Phase = phase;
    }
    /// <summary>The reviewed device catalogue profile, never main or a display label.</summary>
    public string LegacyProfileId { get; }
    /// <summary>The exact stable cryptographic profile identity.</summary>
    public Guid ProfileId { get; }
    /// <summary>The existing private installation identity.</summary>
    public Guid InstallationId { get; }
    /// <summary>The immutable owner-approved retirement request.</summary>
    public Guid OperationId { get; }
    /// <summary>The last actual host barrier recorded by the journal.</summary>
    public ProfileRetirementPhase Phase { get; }
    /// <summary>Advances exactly one barrier; rewinds and completion repeats are refused.</summary>
    public ProfileRetirementCheckpoint Advance()
    {
        if (Phase == ProfileRetirementPhase.Completed) throw new InvalidOperationException("Retirement already completed.");
        return new(LegacyProfileId, ProfileId, InstallationId, OperationId, Phase + 1);
    }
}
