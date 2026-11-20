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
    public CancellationToken FeatureSession => AiFeatures.Current.Capture();
    /// <inheritdoc />
    public bool NetworkAvailable => AiFeatures.Current.Enabled && (PersonalSyncBuild.IsAvailable || Commerce.Billing.BillingBuild.IsAvailable);
    /// <inheritdoc />
    public AiUiAccess Capture(string originalFile)
    {
        var feature = FeatureSession;
        feature.ThrowIfCancellationRequested();
        var access = lifetime.CaptureAnalysis(false);
        if (access.Scope != originalFile) throw new OperationCanceledException("Original analysis file changed.");
        return new(() => { feature.ThrowIfCancellationRequested(); access.Validate(); }, access.Cancellation);
    }
    /// <inheritdoc />
    public Task<bool> ConfirmDisclosureAsync() => AiFeatures.Current.Enabled
        ? appLock.ConfirmAsync(text["Lock_ConfirmExport"]) : Task.FromResult(false);
    /// <inheritdoc />
    public Task ShareAsync(string exactText)
    {
        FeatureSession.ThrowIfCancellationRequested();
        if (appLock.IsLocked || System.Text.Encoding.UTF8.GetByteCount(exactText) > 32768) throw new InvalidOperationException("Original analysis disclosure unavailable.");
        // The user chooses the actual recipient in the native sheet; there is no model/API/clipboard operation here.
        return Share.Default.RequestAsync(new ShareTextRequest { Title = text["Ai_Title"], Text = exactText });
    }
    /// <inheritdoc />
    public async Task<IAiRemote> OpenAsync(string originalFile, CancellationToken ct)
    {
        var feature = FeatureSession;
        feature.ThrowIfCancellationRequested();
        if (!NetworkAvailable) throw new InvalidOperationException("Analysis networking is not selected in this edition.");
        var access = lifetime.CaptureAnalysis(true);
        if (access.Scope != originalFile) throw new OperationCanceledException("Original analysis file changed.");
        // Financial sync preferences never choose an AI credit issuer or arbitrary data destination.
        var peer = new LocalSyncPeer("https://api.zanance.vafadar.pro/", "");
        void Validate() { feature.ThrowIfCancellationRequested(); access.Validate(); }
        var transport = new PersonalSyncTransport(peer, tokens, time, Validate);
        try
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, access.Cancellation, feature);
            var user = await transport.BillingIdentityAsync(linked.Token) ?? throw new InvalidOperationException("AiIdentityRequired");
            Validate();
            return new AiConnectedRemote(transport, storage, user, originalFile, new(Validate, access.Cancellation), feature);
        }
        catch { transport.Dispose(); throw; }
    }
}
