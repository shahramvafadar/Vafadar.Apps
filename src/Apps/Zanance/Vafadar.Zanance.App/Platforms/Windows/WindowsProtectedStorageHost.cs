using Vafadar.Core.Settings;
using Vafadar.Data;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.App.Startup;

namespace Vafadar.Zanance.App.Security;

/// <summary>Binds the complete shared covered storage lifecycle to Windows DPAPI and exact owned native files.</summary>
internal sealed class WindowsProtectedStorageHost(IServiceProvider services,
    Func<CancellationToken, Task<ProtectedStorageInitialization>> initialization, Func<CancellationToken, Task> complete)
    : ProtectedStorageHost(services, initialization, complete)
{
    /// <inheritdoc />
    protected override IDatabaseGenerationFileDurability CreateDurability(string root) => new WindowsGenerationFileDurability(root);
    /// <inheritdoc />
    protected override RecoveryCapturingGenerationKeyStore CreateKeys(string root, Action validate) =>
        new WindowsRecoveryKeyStore(new WindowsGenerationKeyStore(root, validate));
    /// <inheritdoc />
    protected override IProfileRetirementReferenceFiles CreateFiles(IReadOnlyDictionary<string, string> roots, Action validate) =>
        new WindowsProfileRetirementFiles(roots, validate);
    /// <inheritdoc />
    protected override ProtectedSafetyRetention CreateRetention(string root, IReadOnlyDictionary<string, string> roots,
        RecoveryCapturingGenerationKeyStore keys, SqliteRecoveryEnvelopeCatalogue? recovery,
        IDatabaseGenerationFileDurability durability, Action validate) =>
        new ProtectedSafetyRetention(root, roots, keys, recovery, durability, validate,
            static (paths, check) => new WindowsProfileRetirementFiles(paths, check),
            static path => new WindowsGenerationFileDurability(path),
            (artifact, authority, ct) => new WindowsProfileRetirementFiles(roots, validate)
                .RemoveRetainedArtifactAsync(artifact, authority, ct), new WindowsRetainedKeyAliases());
    /// <inheritdoc />
    protected override ProtectedProfileRetirementHost CreateRetirement(string root, IReadOnlyDictionary<string, string> roots,
        string identityPath, ISettingsPersistence persistence, RecoveryCapturingGenerationKeyStore keys,
        IDatabaseGenerationFileDurability durability, Action validate, SqliteRecoveryEnvelopeCatalogue? recovery)
    {
        var files = new WindowsProfileRetirementFiles(roots, validate);
        // DPAPI owns no independent application wrapping alias; exact envelope cleanup is the complete native effect.
        return new(root, identityPath, persistence, keys, durability, files,
            (state, journal, ct) => files.RemoveAsync(state, journal, durability, ct), files.VerifyRemovedAsync, recovery);
    }
}
