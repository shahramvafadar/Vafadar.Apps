using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Core.Analysis;
using Vafadar.Zanance.Data.Analysis;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Features.Analysis;

/// <summary>Original native foreground/profile/lock, deliberately separate manual chooser and fixed owned identity service.</summary>
internal sealed class MauiAiRuntime(PersonalSyncLifetime lifetime, AppLockService appLock, Translator text,
    IPersonalSyncTokenStore tokens, IAiPrivateStorage storage, TimeProvider time) : IAiRuntime
{
    /// <inheritdoc />
    public bool NetworkAvailable => PersonalSyncBuild.IsAvailable || Commerce.Billing.BillingBuild.IsAvailable;
    /// <inheritdoc />
    public AiUiAccess Capture(string originalFile)
    {
        var access = lifetime.CaptureAnalysis(false);
        if (access.Scope != originalFile) throw new OperationCanceledException("Original analysis file changed.");
        return new(access.Validate, access.Cancellation);
    }
    /// <inheritdoc />
    public Task<bool> ConfirmDisclosureAsync() => appLock.ConfirmAsync(text["Lock_ConfirmExport"]);
    /// <inheritdoc />
    public Task ShareAsync(string exactText)
    {
        if (appLock.IsLocked || System.Text.Encoding.UTF8.GetByteCount(exactText) > 32768) throw new InvalidOperationException("Original analysis disclosure unavailable.");
        // The user chooses the actual recipient in the native sheet; there is no model/API/clipboard operation here.
        return Share.Default.RequestAsync(new ShareTextRequest { Title = text["Ai_Title"], Text = exactText });
    }
    /// <inheritdoc />
    public async Task<IAiRemote> OpenAsync(string originalFile, CancellationToken ct)
    {
        if (!NetworkAvailable) throw new InvalidOperationException("Analysis networking is not selected in this edition.");
        var access = lifetime.CaptureAnalysis(true);
        if (access.Scope != originalFile) throw new OperationCanceledException("Original analysis file changed.");
        // Financial sync preferences never choose an AI credit issuer or arbitrary data destination.
        var peer = new LocalSyncPeer("https://api.zanance.vafadar.pro/", "");
        var transport = new PersonalSyncTransport(peer, tokens, time, access.Validate);
        try
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, access.Cancellation);
            var user = await transport.BillingIdentityAsync(linked.Token) ?? throw new InvalidOperationException("AiIdentityRequired"); access.Validate();
            return new AiConnectedRemote(transport, storage, user, originalFile, new(access.Validate, access.Cancellation));
        }
        catch { transport.Dispose(); throw; }
    }
}
