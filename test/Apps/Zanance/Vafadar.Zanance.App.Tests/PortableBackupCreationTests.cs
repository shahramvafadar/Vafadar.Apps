using Vafadar.Backup;
using Vafadar.Backup.Storage;
using Vafadar.Core.Hosting;
using Vafadar.Core.Settings;
using Vafadar.Testing;
using Vafadar.Zanance.App.Features.Backup;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual original package/storage creation through one captured native request, never a duplicated backup algorithm.</summary>
public sealed class PortableBackupCreationTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class Owner
    {
        internal bool Current = true;
        internal void Check() { if (!Current) throw new OperationCanceledException("Original owner retired."); }
    }
    private sealed class Source(Action? beforeWrite = null) : IBackupSource
    {
        internal int Writes;
        public string Name => "fictional-original.data";
        public async Task WriteAsync(Stream destination, CancellationToken ct)
        { Writes++; beforeWrite?.Invoke(); await destination.WriteAsync("original-fictional-source"u8.ToArray(), ct); }
        public Task RestoreAsync(Stream source, CancellationToken ct) => throw new InvalidOperationException("No restore or financial action.");
    }
    private sealed class Storage(LocalFolderBackupStorage original, Action? accepted = null) : IBackupStorage
    {
        internal int Uploads;
        public string Id => original.Id;
        public async Task<BackupFileInfo> UploadAsync(string name, Stream content, CancellationToken ct = default)
        { Uploads++; var file = await original.UploadAsync(name, content, ct); accepted?.Invoke(); return file; }
        public Task<IReadOnlyList<BackupFileInfo>> ListAsync(CancellationToken ct = default) => original.ListAsync(ct);
        public Task<Stream> OpenReadAsync(string id, CancellationToken ct = default) => original.OpenReadAsync(id, ct);
        public Task DeleteAsync(string id, CancellationToken ct = default) => original.DeleteAsync(id, ct);
    }
    private static BackupService Service(Source source) => new([source],
        new StaticAppEnvironment("pro.vafadar.test", "Test", new Version(1, 0), "Test device", "Tests"),
        new InMemorySettingsStore(), TimeProvider.System, new BackupOptions());

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Original_authentication_wait_cannot_borrow_a_departed_or_changed_input(bool changedInput)
    {
        using var directory = new TemporaryDirectory(); var owner = new Owner(); var source = new Source();
        var storage = new Storage(new(directory.Path)); var authentication = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var raw = "original";
        var request = new PortableBackupCreation(Service(source), storage, null, () =>
        { owner.Check(); if (raw != "original") throw new OperationCanceledException("Original input changed."); }, Ct);
        var pending = request.RunAsync(() => authentication.Task);
        if (changedInput) raw = "new input"; else owner.Current = false;
        authentication.SetResult(true); await Assert.ThrowsAsync<OperationCanceledException>(() => pending);
        Assert.Equal(0, source.Writes); Assert.Equal(0, storage.Uploads); Assert.Null(request.StoredFile);
    }

    [Fact]
    public async Task Original_source_or_destination_retired_during_package_copy_cannot_start_upload()
    {
        using var directory = new TemporaryDirectory(); var owner = new Owner(); var source = new Source(() => owner.Current = false);
        var storage = new Storage(new(directory.Path)); var request = new PortableBackupCreation(Service(source), storage, null, owner.Check, Ct);
        await Assert.ThrowsAsync<OperationCanceledException>(() => request.RunAsync(() => Task.FromResult(true)));
        Assert.Equal(1, source.Writes); Assert.Equal(0, storage.Uploads); Assert.Null(request.StoredFile);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Actual_stored_receipt_survives_later_retirement_or_cancellation_without_replaying_upload(bool cancel)
    {
        using var directory = new TemporaryDirectory(); using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        var owner = new Owner(); var source = new Source(); var original = new LocalFolderBackupStorage(directory.Path);
        var storage = new Storage(original, () => { if (cancel) lifetime.Cancel(); else owner.Current = false; });
        var request = new PortableBackupCreation(Service(source), storage, null, owner.Check, lifetime.Token);
        var file = await request.RunAsync(() => Task.FromResult(true));
        Assert.NotNull(file); Assert.Equal(file, request.StoredFile); Assert.Equal(file, Assert.Single(await original.ListAsync(Ct)));
        await Assert.ThrowsAsync<InvalidOperationException>(() => request.RunAsync(() => Task.FromResult(true)));
        Assert.Equal(1, source.Writes); Assert.Equal(1, storage.Uploads);
    }

    [Fact]
    public async Task Actual_authentication_cancel_has_no_source_copy_upload_or_accepted_receipt()
    {
        using var directory = new TemporaryDirectory(); var source = new Source(); var storage = new Storage(new(directory.Path));
        var request = new PortableBackupCreation(Service(source), storage, null, () => { }, Ct);
        Assert.Null(await request.RunAsync(() => Task.FromResult(false)));
        Assert.Null(request.StoredFile); Assert.Equal(0, source.Writes); Assert.Equal(0, storage.Uploads);
    }
}
