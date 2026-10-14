using Vafadar.Data;

namespace Vafadar.Zanance.App.Security;

/// <summary>Binds shared explicit portable recovery to Windows's actual private staging effects.</summary>
internal sealed class WindowsBackupRecovery(IServiceProvider services) : ProtectedBackupRecovery(services)
{
    /// <inheritdoc />
    protected override void CreateStagingDirectory(string parent, string path) => WindowsPrivateStorage.CreateNewDirectory(parent, path);
    /// <inheritdoc />
    protected override IDatabaseGenerationFileDurability CreateDurability(string root) => new WindowsGenerationFileDurability(root);
    /// <inheritdoc />
    protected override IProfileRetirementReferenceFiles CreateFiles(IReadOnlyDictionary<string, string> roots, Action validate) =>
        new WindowsProfileRetirementFiles(roots, validate);
    /// <inheritdoc />
    protected override ProtectedStorageHost CreateStageHost(IServiceProvider provider)
    {
        var installation = new WindowsProtectedInstallation(provider);
        return new WindowsProtectedStorageHost(provider, installation.BeginAsync, installation.CompleteAsync);
    }
}
