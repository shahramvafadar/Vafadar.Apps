using Vafadar.Backup;

namespace Vafadar.Zanance.App.Features.Backup;

/// <summary>One original explicit portable creation, with a durable stored receipt before fallible follow-through.</summary>
public sealed class PortableBackupCreation(IBackupService backup, IBackupStorage storage, string? password,
    Action validateOriginal, CancellationToken ct)
{
    private bool _started;
    /// <summary>Gets the actual original upload receipt; later listing/share cannot turn it into an unstarted request.</summary>
    public BackupFileInfo? StoredFile { get; private set; }
    /// <summary>Awaits original authentication once, then uses the original service/storage without borrowing a later owner.</summary>
    public async Task<BackupFileInfo?> RunAsync(Func<Task<bool>> authenticate)
    {
        if (_started) throw new InvalidOperationException("The original portable creation already started.");
        _started = true; Check();
        if (!await authenticate()) return null;
        Check();
        StoredFile = await backup.CreateBackupAsync(new OriginalStorage(storage, Check), password, ct);
        return StoredFile;
    }
    private void Check() { ct.ThrowIfCancellationRequested(); validateOriginal(); }

    /// <summary>Checks original native/input/source/destination authority at each actual storage effect.</summary>
    private sealed class OriginalStorage(IBackupStorage original, Action validate) : IBackupStorage
    {
        public string Id => original.Id;
        public Task<IReadOnlyList<BackupFileInfo>> ListAsync(CancellationToken token = default)
        { validate(); return original.ListAsync(token); }
        public Task<Stream> OpenReadAsync(string id, CancellationToken token = default)
        { validate(); return original.OpenReadAsync(id, token); }
        public Task DeleteAsync(string id, CancellationToken token = default)
        { validate(); return original.DeleteAsync(id, token); }
        public Task<BackupFileInfo> UploadAsync(string name, Stream content, CancellationToken token = default)
        {
            validate();
            // Do not reject a known successful return after its real storage commit. The original service retains
            // that receipt while cancelled retention or a retired native caller refuses further effects.
            return original.UploadAsync(name, content, token);
        }
    }
}
