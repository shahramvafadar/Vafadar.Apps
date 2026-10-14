using Vafadar.Core.Settings;
using Vafadar.Data;
using Vafadar.Zanance.App.Profiles;

namespace Vafadar.Zanance.App.Security;

internal abstract partial class ProtectedStorageHost
{
    /// <summary>Creates native durability effects only inside covered preparation.</summary>
    protected abstract IDatabaseGenerationFileDurability CreateDurability(string root);
    /// <summary>Creates the platform device-key owner without preparing replacement keys.</summary>
    protected abstract RecoveryCapturingGenerationKeyStore CreateKeys(string root, Action validate);
    /// <summary>Captures exact platform-owned reference objects under the supplied maintenance authority.</summary>
    protected abstract IProfileRetirementReferenceFiles CreateFiles(IReadOnlyDictionary<string, string> roots, Action validate);
    /// <summary>Binds shared retention to native file and independently owned alias effects.</summary>
    protected abstract ProtectedSafetyRetention CreateRetention(string root, IReadOnlyDictionary<string, string> roots,
        RecoveryCapturingGenerationKeyStore keys, SqliteRecoveryEnvelopeCatalogue? recovery,
        IDatabaseGenerationFileDurability durability, Action validate);
    /// <summary>Composes reviewed profile retirement with the platform's complete native cleanup barriers.</summary>
    protected abstract ProtectedProfileRetirementHost CreateRetirement(string root, IReadOnlyDictionary<string, string> roots,
        string identityPath, ISettingsPersistence persistence, RecoveryCapturingGenerationKeyStore keys,
        IDatabaseGenerationFileDurability durability, Action validate, SqliteRecoveryEnvelopeCatalogue? recovery);
}

/// <summary>Replays an explicitly staged portable recovery before protected startup resolves installation authority.</summary>
internal interface IProtectedBackupRecovery
{
    /// <summary>Completes the exact previously selected installation and reports whether the session root changed.</summary>
    Task<bool> ReplayAsync(CancellationToken ct);
    /// <summary>Requalifies the selected installation's durable activation receipt.</summary>
    Task ValidateSelectedAsync(CancellationToken ct);
}
