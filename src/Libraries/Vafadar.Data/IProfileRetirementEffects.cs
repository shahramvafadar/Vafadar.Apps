namespace Vafadar.Data;

/// <summary>Native effects for one captured, reviewed non-main profile under exclusive maintenance ownership.</summary>
/// <remarks>Each operation must be idempotent and verify its actual durable outcome before returning.
/// The host captures the complete artifact/key-reference set and validates catalogue/identity bindings;
/// this port never authorizes guessed filename cleanup or replacement keys.</remarks>
public interface IProfileRetirementEffects
{
    /// <summary>Rechecks exclusive ownership, the reviewed request and every completed external barrier.</summary>
    Task ValidateAsync(ProfileRetirementCheckpoint checkpoint, CancellationToken ct);
    /// <summary>Removes only the reviewed device catalogue row and verifies durable native readback.</summary>
    Task RemoveCatalogueAsync(ProfileRetirementCheckpoint checkpoint, CancellationToken ct);
    /// <summary>Removes and synchronizes the captured complete financial/backup artifact set.</summary>
    Task RemoveArtifactsAsync(ProfileRetirementCheckpoint checkpoint, CancellationToken ct);
    /// <summary>Retires stable identity only after reconciling all durable key/artifact references.</summary>
    Task RetireIdentityAsync(ProfileRetirementCheckpoint checkpoint, CancellationToken ct);
}
