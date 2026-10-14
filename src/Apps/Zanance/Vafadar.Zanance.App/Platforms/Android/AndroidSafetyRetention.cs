using Vafadar.Data;

namespace Vafadar.Zanance.App.Security;

/// <summary>Binds shared safety retention to Android's native owned files and independent wrapping aliases.</summary>
internal sealed class AndroidSafetyRetention(string root, IReadOnlyDictionary<string, string> roots,
    RecoveryCapturingGenerationKeyStore keys, SqliteRecoveryEnvelopeCatalogue? recovery,
    IDatabaseGenerationFileDurability durability, Action validate)
    : ProtectedSafetyRetention(root, roots, keys, recovery, durability, validate,
        static (paths, check) => new AndroidProfileRetirementFiles(paths, check),
        static path => new AndroidGenerationFileDurability(path),
        (artifact, authority, ct) => new AndroidProfileRetirementFiles(roots, validate)
            .RemoveRetainedArtifactAsync(artifact, authority, ct), new AndroidRetainedKeyAliases());
