using Vafadar.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Features.Sync;

/// <summary>Native public configuration and credential/lifetime adapters; construction does not open storage or network.</summary>
internal sealed class MauiPersonalSyncEnvironment(ISettingsStore settings, IPersonalSyncTokenStore tokens,
    PersonalSyncLifetime lifetime, ZananceStore finances, TimeProvider time) : IPersonalSyncEnvironment
{
    /// <inheritdoc />
    public async Task<PersonalSyncRenderedPage> RenderAttachmentPageAsync(PersonalSyncAttachmentContent content, int pageIndex, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested(); byte[] image; int count;
        if (content.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            var page = await Vafadar.Documents.Maui.PdfPageImages.ReadPageAsync(content.Data, pageIndex)
                ?? throw new InvalidDataException("The attachment cannot be rendered.");
            image = page.Data; count = page.PageCount;
        }
        else if (content.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) && pageIndex == 0)
        {
            image = await Vafadar.Documents.Maui.DocumentImages.PrepareAsync(content.Data, 1600, 0.9f); count = 1;
        }
        else throw new InvalidDataException("The attachment media type cannot be reviewed in this app.");
        try
        {
            var (width, height) = await Vafadar.Documents.Maui.DocumentImages.ReadPreviewSizeAsync(image);
            ct.ThrowIfCancellationRequested(); return new(count, image, width, height);
        }
        catch { System.Security.Cryptography.CryptographicOperations.ZeroMemory(image); throw; }
    }
    private const string AddressKey = "personalSync.localAddress";
    private const string PinKey = "personalSync.localCertificate";
    private const string InterestKey = "personalSync.hasConnectionInterest";
    /// <inheritdoc />
    public event EventHandler? StatusChanged;
    /// <summary>A device-only scheduling hint; the actual profile binding always grants or refuses authority.</summary>
    internal bool HasConnectionInterest => settings.Get(InterestKey) == "true";
    /// <summary>Raised only after an explicit opt-in/pause/sign-out so native scheduling can recheck actual bindings.</summary>
    internal event EventHandler? ConnectionStateChanged;
    /// <inheritdoc />
    public bool Available => PersonalSyncBuild.IsAvailable;
    /// <inheritdoc />
    public PersonalSyncConnection ReadConnection() => new(settings.Get(AddressKey) ??
        (OperatingSystem.IsAndroid() ? "https://10.0.2.2:5048/" : "https://127.0.0.1:5048/"), settings.Get(PinKey) ?? "");
    /// <inheritdoc />
    public void RememberPeer(LocalSyncPeer peer)
    { settings.Set(AddressKey, peer.Address.AbsoluteUri); settings.Set(PinKey, peer.Fingerprint); }
    /// <inheritdoc />
    public PersonalSyncAccess Capture() => lifetime.Capture();
    /// <inheritdoc />
    public PersonalSyncTransport Open(LocalSyncPeer peer, Action verifyAccess) => new(peer, tokens, time, verifyAccess);
    /// <inheritdoc />
    public Task PublishAppliedAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return MainThread.InvokeOnMainThreadAsync(() =>
        {
            // A queued dispatcher publication can outlive foreground access even after its network task completed.
            ct.ThrowIfCancellationRequested(); lifetime.Capture().Validate(); finances.ResetSession();
        });
    }
    /// <inheritdoc />
    public void ConnectionChanged()
    {
        settings.Set(InterestKey, "true");
        ConnectionStateChanged?.Invoke(this, EventArgs.Empty);
    }
    /// <summary>Publishes content-free status only on the native presentation dispatcher.</summary>
    internal void PublishStatusChanged() => StatusChanged?.Invoke(this, EventArgs.Empty);
    /// <inheritdoc />
    public Guid CreationOperation(string requestDigest)
    {
        if (!Core.Sync.SyncProtocolDigest.IsDigest(requestDigest)) throw new ArgumentException("A bounded reviewed creation digest is required.");
        var key = "personalSync.creation." + requestDigest;
        if (settings.Get(key) is { } previous)
            return Guid.TryParseExact(previous, "N", out var retained) && retained != Guid.Empty ? retained : throw new InvalidDataException("The retained personal creation intent is invalid.");
        var operation = Guid.NewGuid(); settings.Set(key, operation.ToString("N")); return operation;
    }
    /// <inheritdoc />
    public void CompleteCreation(string requestDigest, Guid operation)
    {
        var key = "personalSync.creation." + requestDigest;
        if (settings.Get(key) != operation.ToString("N")) throw new InvalidOperationException("The personal creation intent changed.");
        settings.Remove(key);
    }
}
