using Vafadar.Core.Settings;
using Vafadar.Data;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.App.Startup;

namespace Vafadar.Zanance.App.Security;

/// <summary>Binds shared covered storage lifecycle to Android's actual private files and Keystore.</summary>
internal sealed class AndroidProtectedStorageHost(IServiceProvider services,
    Func<CancellationToken, Task<ProtectedStorageInitialization>> initialization, Func<CancellationToken, Task> complete)
    : ProtectedStorageHost(services, initialization, complete)
{
    /// <inheritdoc />
    protected override IDatabaseGenerationFileDurability CreateDurability(string root) => new AndroidGenerationFileDurability(root);
    /// <inheritdoc />
    protected override RecoveryCapturingGenerationKeyStore CreateKeys(string root, Action validate) =>
        new AndroidRecoveryKeyStore(new AndroidGenerationKeyStore(root));
    /// <inheritdoc />
    protected override IProfileRetirementReferenceFiles CreateFiles(IReadOnlyDictionary<string, string> roots, Action validate) =>
        new AndroidProfileRetirementFiles(roots, validate);
    /// <inheritdoc />
    protected override ProtectedSafetyRetention CreateRetention(string root, IReadOnlyDictionary<string, string> roots,
        RecoveryCapturingGenerationKeyStore keys, SqliteRecoveryEnvelopeCatalogue? recovery,
        IDatabaseGenerationFileDurability durability, Action validate) =>
        new AndroidSafetyRetention(root, roots, keys, recovery, durability, validate);
    /// <inheritdoc />
    protected override ProtectedProfileRetirementHost CreateRetirement(string root, IReadOnlyDictionary<string, string> roots,
        string identityPath, ISettingsPersistence persistence, RecoveryCapturingGenerationKeyStore keys,
        IDatabaseGenerationFileDurability durability, Action validate, SqliteRecoveryEnvelopeCatalogue? recovery)
    {
        var files = new AndroidProfileRetirementFiles(roots, validate);
        var aliases = new AndroidProfileAliasRetirement(root, roots, keys, durability, validate);
        return new(root, identityPath, persistence, keys, durability, files,
            aliases.RemoveAsync, aliases.VerifyAsync, recovery, aliases.CaptureAsync);
    }
}
