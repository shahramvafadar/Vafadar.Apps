using Vafadar.Data;

namespace Vafadar.Zanance.App.Security;

/// <summary>Binds shared explicit portable recovery to Android's actual private staging effects.</summary>
internal sealed class AndroidBackupRecovery(IServiceProvider services) : ProtectedBackupRecovery(services)
{
    /// <inheritdoc />
    protected override void CreateStagingDirectory(string parent, string path) => Android.Systems.Os.Mkdir(path, 448);
    /// <inheritdoc />
    protected override IDatabaseGenerationFileDurability CreateDurability(string root) => new AndroidGenerationFileDurability(root);
    /// <inheritdoc />
    protected override IProfileRetirementReferenceFiles CreateFiles(IReadOnlyDictionary<string, string> roots, Action validate) =>
        new AndroidProfileRetirementFiles(roots, validate);
    /// <inheritdoc />
    protected override ProtectedStorageHost CreateStageHost(IServiceProvider provider)
    {
        var installation = new AndroidProtectedInstallation(provider);
        return new AndroidProtectedStorageHost(provider, installation.BeginAsync, installation.CompleteAsync);
    }
}
