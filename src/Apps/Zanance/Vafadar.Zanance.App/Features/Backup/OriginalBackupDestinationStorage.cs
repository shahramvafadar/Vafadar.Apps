using Vafadar.Authentication;
using Vafadar.Backup;

namespace Vafadar.Zanance.App.Features.Backup;

/// <summary>Uses the original configured adapter only while its actual provider identity and native source remain current.</summary>
public sealed class OriginalBackupDestinationStorage(IBackupStorage original, IExternalSignInService signIn,
    ExternalAccount account, Action validateOriginal) : IBackupStorage
{
    /// <inheritdoc />
    public string Id => original.Id;
    private async Task CheckAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested(); validateOriginal();
        var actual = await signIn.GetCurrentAccountAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested(); validateOriginal();
        if (actual is null || actual.Provider != account.Provider || actual.Id != account.Id)
            throw new BackupDestinationChangedException(cancellationToken);
    }
    /// <inheritdoc />
    public async Task<IReadOnlyList<BackupFileInfo>> ListAsync(CancellationToken cancellationToken = default)
    {
        await CheckAsync(cancellationToken); var files = await original.ListAsync(cancellationToken);
        await CheckAsync(cancellationToken); return files;
    }
    /// <inheritdoc />
    public async Task<Stream> OpenReadAsync(string fileId, CancellationToken cancellationToken = default)
    {
        await CheckAsync(cancellationToken); var stream = await original.OpenReadAsync(fileId, cancellationToken);
        try { await CheckAsync(cancellationToken); return stream; }
        catch { await stream.DisposeAsync(); throw; }
    }
    /// <inheritdoc />
    public async Task<BackupFileInfo> UploadAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        await CheckAsync(cancellationToken);
        // Keep a known accepted upload; the original creation receipt refuses later stale retention/publication.
        return await original.UploadAsync(fileName, content, cancellationToken);
    }
    /// <inheritdoc />
    public async Task DeleteAsync(string fileId, CancellationToken cancellationToken = default)
    {
        await CheckAsync(cancellationToken);
        // A confirmed remote deletion remains an accepted effect even if native publication later retires.
        await original.DeleteAsync(fileId, cancellationToken);
    }
}
