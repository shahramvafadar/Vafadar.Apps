using Vafadar.Authentication;
using Vafadar.Backup;
using Vafadar.Backup.Storage;
using Vafadar.Testing;
using Vafadar.Zanance.App.Features.Backup;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original destination consent boundaries with actual local file deletion and bounded fictitious identity effects.</summary>
public sealed class BackupDestinationOwnershipTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class Identity : IExternalSignInService
    {
        internal int Connections, Disconnections;
        internal Action? Accepted;
        internal string[]? RequestedScopes;
        internal ExternalAccount? Account;
        internal bool RequiresAuthentication;
        public ExternalIdentityProvider Provider => ExternalIdentityProvider.Google;
        public ValueTask<ExternalAccount?> GetCurrentAccountAsync(CancellationToken cancellationToken = default)
            => RequiresAuthentication ? throw new AuthenticationRequiredException() : ValueTask.FromResult(Account);
        public ValueTask<ExternalAccount> SignInAsync(IReadOnlyCollection<string> scopes, CancellationToken cancellationToken = default)
        {
            Connections++; RequestedScopes = scopes.ToArray(); var account = OriginalAccount; Account = account; RequiresAuthentication = false; Accepted?.Invoke();
            return ValueTask.FromResult(account);
        }
        public ValueTask SignOutAsync(CancellationToken cancellationToken = default)
        { Disconnections++; Account = null; Accepted?.Invoke(); return ValueTask.CompletedTask; }
        public ValueTask<string> GetAccessTokenAsync(IReadOnlyCollection<string> scopes, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("No credential access belongs to this destination-only test.");
    }
    private static ExternalAccount OriginalAccount => new(ExternalIdentityProvider.Google, "fictitious-original-account", null, "Fictitious original");
    private static Identity IdentityFor(string operation) => new() { Account = operation == "connect" ? null : OriginalAccount };
    private sealed class Storage(LocalFolderBackupStorage original) : IBackupStorage
    {
        internal int Deletes, Lists, Opens, Uploads;
        internal Action? Accepted;
        internal Action? ReadCompleted;
        internal Stream? LastRead;
        internal bool Refuse;
        public string Id => original.Id;
        public async Task<IReadOnlyList<BackupFileInfo>> ListAsync(CancellationToken cancellationToken = default)
        { Lists++; var files = await original.ListAsync(cancellationToken); ReadCompleted?.Invoke(); return files; }
        public async Task<Stream> OpenReadAsync(string fileId, CancellationToken cancellationToken = default)
        { Opens++; LastRead = await original.OpenReadAsync(fileId, cancellationToken); ReadCompleted?.Invoke(); return LastRead; }
        public async Task<BackupFileInfo> UploadAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
        { Uploads++; var file = await original.UploadAsync(fileName, content, cancellationToken); Accepted?.Invoke(); return file; }
        public async Task DeleteAsync(string fileId, CancellationToken cancellationToken = default)
        {
            Deletes++; if (Refuse) throw new IOException("Fictitious original storage refusal.");
            await original.DeleteAsync(fileId, cancellationToken); Accepted?.Invoke();
        }
    }
    private static Task RunAsync(BackupDestinationRequest request, string operation, BackupFileInfo file, Func<Task<bool>> confirm) => operation switch
    {
        "connect" => request.ConnectAsync(confirm, ["original-scope"]),
        "disconnect" => request.DisconnectAsync(confirm),
        _ => request.DeleteAsync(file, confirm),
    };
    private static async Task<BackupFileInfo> StoreAsync(Storage storage, string name)
    {
        using var content = new MemoryStream("fictitious-original-file"u8.ToArray());
        return await storage.UploadAsync(name, content, Ct);
    }

    /// <summary>A pending answer cannot grant effects to a changed original page/source or a cancelled generation.</summary>
    [Theory]
    [InlineData("connect", "owner")] [InlineData("disconnect", "owner")] [InlineData("delete", "owner")]
    [InlineData("connect", "source")] [InlineData("disconnect", "source")] [InlineData("delete", "source")]
    [InlineData("connect", "cancel")] [InlineData("disconnect", "cancel")] [InlineData("delete", "cancel")]
    [InlineData("connect", "identity")] [InlineData("disconnect", "identity")] [InlineData("delete", "identity")]
    public async Task Pending_original_consent_cannot_borrow_a_retired_destination(string operation, string change)
    {
        using var directory = new TemporaryDirectory(); var storage = new Storage(new(directory.Path)); var identity = IdentityFor(operation);
        var file = await StoreAsync(storage, "original.bin"); var foreign = await StoreAsync(storage, "untouched.bin");
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        var active = true; var source = "original";
        void Current() { if (!active || source != "original") throw new OperationCanceledException("Original destination retired."); }
        var request = new BackupDestinationRequest(identity, storage, Current, cancellation.Token, identity.Account);
        var answer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = RunAsync(request, operation, file, () => answer.Task);
        if (change == "owner") active = false; else if (change == "source") source = "foreign";
        else if (change == "identity") identity.Account = OriginalAccount with { Id = "another-stable-identity" };
        else cancellation.Cancel();
        answer.SetResult(true); await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.Equal(0, identity.Connections); Assert.Equal(0, identity.Disconnections); Assert.Equal(0, storage.Deletes);
        Assert.Null(request.ConnectedAccount); Assert.False(request.Disconnected); Assert.Null(request.DeletedFile);
        Assert.Equal(new[] { file.Id, foreign.Id }.Order(), (await storage.ListAsync(Ct)).Select(row => row.Id).Order());
    }

    /// <summary>A known original effect remains a receipt even when the native owner retires before publication.</summary>
    [Theory]
    [InlineData("connect")] [InlineData("disconnect")] [InlineData("delete")]
    public async Task Accepted_original_effect_is_retained_and_cannot_be_submitted_again(string operation)
    {
        using var directory = new TemporaryDirectory(); var storage = new Storage(new(directory.Path)); var identity = IdentityFor(operation);
        var file = await StoreAsync(storage, "original.bin"); var foreign = await StoreAsync(storage, "untouched.bin");
        var active = true; void Current() { if (!active) throw new OperationCanceledException("Original native publication retired."); }
        identity.Accepted = storage.Accepted = () => active = false;
        var request = new BackupDestinationRequest(identity, storage, Current, Ct, identity.Account);
        await RunAsync(request, operation, file, () => Task.FromResult(true));
        Assert.Equal(operation == "connect", request.ConnectedAccount is not null);
        Assert.Equal(operation == "disconnect", request.Disconnected); Assert.Equal(operation == "delete", request.DeletedFile == file);
        await Assert.ThrowsAsync<InvalidOperationException>(() => RunAsync(request, operation, file, () => Task.FromResult(true)));
        Assert.Equal(operation == "connect" ? 1 : 0, identity.Connections);
        Assert.Equal(operation == "disconnect" ? 1 : 0, identity.Disconnections);
        Assert.Equal(operation == "delete" ? 1 : 0, storage.Deletes);
        Assert.Contains(await storage.ListAsync(Ct), row => row.Id == foreign.Id);
    }

    /// <summary>An actual failed storage operation does not manufacture a known deleted receipt or affect another file.</summary>
    [Fact]
    public async Task Failed_original_storage_delete_has_no_accepted_receipt()
    {
        using var directory = new TemporaryDirectory(); var storage = new Storage(new(directory.Path)) { Refuse = true };
        var file = await StoreAsync(storage, "original.bin"); var identity = IdentityFor("delete");
        var request = new BackupDestinationRequest(identity, storage, () => { }, Ct, identity.Account);
        await Assert.ThrowsAsync<IOException>(() => request.DeleteAsync(file, () => Task.FromResult(true)));
        Assert.Null(request.DeletedFile); Assert.Single(await storage.ListAsync(Ct));
    }

    /// <summary>Consent is bound to the submitted literal scope set, without later array mutation or credential access.</summary>
    [Fact]
    public async Task Original_signin_scopes_are_frozen_before_pending_consent()
    {
        using var directory = new TemporaryDirectory(); var identity = new Identity();
        var request = new BackupDestinationRequest(identity, new LocalFolderBackupStorage(directory.Path), () => { }, Ct);
        var scopes = new[] { "original-scope" }; var answer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = request.ConnectAsync(() => answer.Task, scopes); scopes[0] = "later-scope";
        answer.SetResult(true); await pending; Assert.Equal(new[] { "original-scope" }, identity.RequestedScopes);
    }

    /// <summary>Declining the original disclosure never starts provider mutation or deletes a stored file.</summary>
    [Theory]
    [InlineData("connect")] [InlineData("disconnect")] [InlineData("delete")]
    public async Task Declined_original_consent_has_no_destination_effect(string operation)
    {
        using var directory = new TemporaryDirectory(); var storage = new Storage(new(directory.Path)); var identity = IdentityFor(operation);
        var file = await StoreAsync(storage, "original.bin");
        var request = new BackupDestinationRequest(identity, storage, () => { }, Ct, identity.Account);
        await RunAsync(request, operation, file, () => Task.FromResult(false));
        Assert.Equal(0, identity.Connections); Assert.Equal(0, identity.Disconnections); Assert.Equal(0, storage.Deletes);
        Assert.Null(request.ConnectedAccount); Assert.False(request.Disconnected); Assert.Null(request.DeletedFile);
        Assert.Single(await storage.ListAsync(Ct));
    }

    /// <summary>Changing the actual stable provider id cannot borrow the unchanged account display label for storage effects.</summary>
    [Theory]
    [InlineData("list")] [InlineData("open")] [InlineData("upload")] [InlineData("delete")]
    public async Task Actual_storage_account_change_refuses_before_any_original_adapter_effect(string operation)
    {
        using var directory = new TemporaryDirectory(); var storage = new Storage(new(directory.Path)); var identity = IdentityFor("delete");
        var file = await StoreAsync(storage, "original.bin");
        var bound = new OriginalBackupDestinationStorage(storage, identity, identity.Account!, () => { });
        identity.Account = identity.Account! with { Id = "other-account-with-same-display-label" };
        using var content = new MemoryStream("never-stored"u8.ToArray());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            if (operation == "list") await bound.ListAsync(Ct);
            else if (operation == "open") await bound.OpenReadAsync(file.Id, Ct);
            else if (operation == "upload") await bound.UploadAsync("never.bin", content, Ct);
            else await bound.DeleteAsync(file.Id, Ct);
        });
        Assert.Equal(0, storage.Lists); Assert.Equal(0, storage.Opens); Assert.Equal(0, storage.Deletes); Assert.Equal(1, storage.Uploads);
    }

    /// <summary>A provider completing a read for a retired native source cannot return its list or leak an opened stream.</summary>
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Original_storage_read_completion_refuses_retired_source_and_disposes_unpublished_stream(bool open)
    {
        using var directory = new TemporaryDirectory(); var storage = new Storage(new(directory.Path)); var identity = IdentityFor("delete");
        var file = await StoreAsync(storage, "original.bin"); var active = true;
        var bound = new OriginalBackupDestinationStorage(storage, identity, identity.Account!,
            () => { if (!active) throw new OperationCanceledException("Original source retired."); });
        storage.ReadCompleted = () => active = false;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        { if (open) await bound.OpenReadAsync(file.Id, Ct); else await bound.ListAsync(Ct); });
        if (open) Assert.False(Assert.IsAssignableFrom<Stream>(storage.LastRead).CanRead);
        Assert.Equal(0, storage.Deletes); Assert.Equal(1, storage.Uploads);
    }

    /// <summary>A known original upload is preserved even when native publication retires immediately after storage accepts it.</summary>
    [Fact]
    public async Task Accepted_original_storage_upload_is_not_erased_by_later_native_retirement()
    {
        using var directory = new TemporaryDirectory(); var storage = new Storage(new(directory.Path)); var identity = IdentityFor("delete");
        var active = true; var bound = new OriginalBackupDestinationStorage(storage, identity, identity.Account!,
            () => { if (!active) throw new OperationCanceledException("Original source retired."); });
        storage.Accepted = () => active = false; using var content = new MemoryStream("actual-fictitious-upload"u8.ToArray());
        var accepted = await bound.UploadAsync("original.bin", content, Ct);
        Assert.Equal("original.bin", accepted.FileName); Assert.Equal(1, storage.Uploads); Assert.Equal(0, storage.Deletes);
    }

    /// <summary>Post-effect identity review can refuse publication without converting the actual accepted result into a retry.</summary>
    [Theory]
    [InlineData("connect")] [InlineData("disconnect")] [InlineData("delete")]
    public async Task Publication_account_change_preserves_the_actual_destination_receipt(string operation)
    {
        using var directory = new TemporaryDirectory(); var storage = new Storage(new(directory.Path)); var identity = IdentityFor(operation);
        var file = await StoreAsync(storage, "original.bin");
        var request = new BackupDestinationRequest(identity, storage, () => { }, Ct, identity.Account);
        await RunAsync(request, operation, file, () => Task.FromResult(true));
        identity.Account = OriginalAccount with { Id = "changed-after-actual-effect" };
        await Assert.ThrowsAsync<BackupDestinationChangedException>(request.EnsurePublicationCurrentAsync);
        Assert.Equal(operation == "connect", request.ConnectedAccount is not null);
        Assert.Equal(operation == "disconnect", request.Disconnected); Assert.Equal(operation == "delete", request.DeletedFile == file);
        Assert.Equal(operation == "connect" ? 1 : 0, identity.Connections);
        Assert.Equal(operation == "disconnect" ? 1 : 0, identity.Disconnections);
        Assert.Equal(operation == "delete" ? 1 : 0, storage.Deletes);
    }

    /// <summary>Required interactive authentication remains available through original explicit connect, never through a destructive command.</summary>
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Required_authentication_allows_only_explicit_connect_recovery(bool destructive)
    {
        using var directory = new TemporaryDirectory(); var storage = new Storage(new(directory.Path));
        var identity = IdentityFor(destructive ? "delete" : "connect"); identity.RequiresAuthentication = true;
        var file = await StoreAsync(storage, "original.bin");
        var request = new BackupDestinationRequest(identity, storage, () => { }, Ct, identity.Account);
        if (destructive)
        {
            await Assert.ThrowsAsync<AuthenticationRequiredException>(() => request.DeleteAsync(file, () => Task.FromResult(true)));
            Assert.Null(request.DeletedFile); Assert.Equal(0, storage.Deletes); Assert.Equal(0, identity.Connections);
        }
        else
        {
            await request.ConnectAsync(() => Task.FromResult(true), ["original-scope"]);
            Assert.NotNull(request.ConnectedAccount); Assert.Equal(1, identity.Connections);
            await request.EnsurePublicationCurrentAsync();
        }
    }
}
