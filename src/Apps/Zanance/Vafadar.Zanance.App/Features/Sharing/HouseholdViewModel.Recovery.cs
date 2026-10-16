using System.Security.Cryptography;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Features.Sharing;

public sealed partial class HouseholdViewModel
{
    private byte[]? pickedRecovery;
    private string? savedRecoveryFingerprint;
    private HouseholdJournalAuthority? savedRecoveryAuthority;
    private Guid? enrolledRecovery;

    /// <summary>Encrypted file support is distinct from identity login and room authority.</summary>
    public bool RecoveryFilesAvailable => interaction.HouseholdRecoveryFilesAvailable;
    /// <summary>A retained recovery import is not an unaccepted invitation; it uses its own explicit continuation.</summary>
    public bool IsOwnerRecoveryDraft => state is { OwnerCreation: false, SignedInvitation: { } anchor } && Vafadar.Zanance.Core.Sharing.HouseholdSetupAnchors.Kind(anchor) == Vafadar.Zanance.Core.Sharing.HouseholdSetupAnchors.OwnerRecovery && !connected;
    /// <summary>Only a current approved owner may create/replace an independent recovery kit.</summary>
    public bool CanExportRecovery => RecoveryFilesAvailable && CanAdminister;
    /// <summary>Enabling requires an actual completed native Save for this exact captured original kit/profile.</summary>
    public bool CanEnableRecovery => CanInteract && IsOwner && savedRecoveryFingerprint is not null && savedRecoveryAuthority == state?.Authority;
    /// <summary>Picking a kit never overlays an existing personal or shared financial ledger.</summary>
    public bool CanPickRecovery => RecoveryFilesAvailable && CanSetUp;
    /// <summary>Private-kit import additionally requires explicit actual identity sign-in and a selected encrypted file.</summary>
    public bool CanImportRecovery => CanSetUp && pickedRecovery is not null;
    /// <summary>Only the reviewed current enrolled kit can be retired through another explicit root transition.</summary>
    public bool CanRevokeRecovery => CanAdminister && enrolledRecovery is not null;
    /// <summary>Content-free actual discovery/file-handoff status, never a private key or file path.</summary>
    public string RecoveryStatus => pickedRecovery is not null ? translator["HouseholdRecovery_FilePicked"] : savedRecoveryFingerprint is not null
        ? translator["HouseholdRecovery_FileSaved"] : translator[enrolledRecovery is null ? "HouseholdRecovery_Off" : "HouseholdRecovery_Active"];
    /// <summary>In-memory file password only; it is never a service sign-in credential or portable preference.</summary>
    [ObservableProperty] public partial string RecoveryPassword { get; set; } = "";
    /// <summary>Export confirmation is independent of the sign-in password and is cleared on every submission/retirement.</summary>
    [ObservableProperty] public partial string RecoveryPasswordConfirm { get; set; } = "";

    [RelayCommand] private async Task PickRecoveryAsync()
    {
        if (!CanPickRecovery || visible is null) return;
        IsBusy = true; errorKey = null; Refresh();
        RecoveryPassword = RecoveryPasswordConfirm = "";
        try
        {
            var access = environment.Capture(); var captured = await original.ReviewAsync(visible.Token); access.Validate();
            var peer = new LocalSyncPeer(Address, Fingerprint); var selectingIdentity = identity ?? throw new InvalidOperationException();
            // Native pickers may background/lock the app. Retain only encrypted bytes here;
            // importing them is a separate action with a fresh unlocked/profile capture.
            using var nativeDeadline = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            var bytes = await interaction.PickHouseholdRecoveryAsync(translator["HouseholdRecovery_Pick"], nativeDeadline.Token);
            if (bytes is not null)
            {
                try { workspace.RememberPickedRecoveryFile(captured.GenerationPath, peer, selectingIdentity, bytes); }
                finally { CryptographicOperations.ZeroMemory(bytes); }
                // A lock modal can retire this page while the native picker is open. The singleton
                // keeps only encrypted bytes; fresh same-profile discovery can adopt them after unlock.
                if (visible is not null && identity == selectingIdentity)
                {
                    var fresh = environment.Capture(); var current = await original.ReviewAsync(visible.Token); fresh.Validate();
                    if (current.GenerationPath == captured.GenerationPath)
                    {
                        var selected = workspace.TakePickedRecoveryFile(current.GenerationPath, peer, selectingIdentity);
                        if (selected is not null) { if (pickedRecovery is not null) CryptographicOperations.ZeroMemory(pickedRecovery); pickedRecovery = selected; }
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception error) when (error is not OutOfMemoryException) { errorKey = "Household_ActionFailed"; }
        finally { IsBusy = false; Refresh(); }
    }

    [RelayCommand] private async Task ExportRecoveryAsync()
    {
        if (!CanExportRecovery || visible is null || state is null) return;
        if (RecoveryPassword.Length < 8 || RecoveryPassword != RecoveryPasswordConfirm) { errorKey = "HouseholdRecovery_PasswordError"; Refresh(); return; }
        IsBusy = true; errorKey = null; Refresh();
        var password = RecoveryPassword; RecoveryPassword = RecoveryPasswordConfirm = "";
        try
        {
            var access = environment.Capture(); using var linked = CancellationTokenSource.CreateLinkedTokenSource(access.Cancellation, visible.Token);
            var captured = state.Authority; var peer = new LocalSyncPeer(Address, Fingerprint);
            transport?.Dispose(); transport = new(peer, tokens, time, access.Validate);
            using var export = await workspace.ExportRecoveryKitAsync(transport, captured, password, linked.Token);
            password = ""; access.Validate();
            // The already password-encrypted file may pass through a native external document
            // picker. Do not continue storage/network authority across its background/lock cycle.
            using var nativeDeadline = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            var saved = await interaction.SaveHouseholdRecoveryAsync("zanance-household-recovery-" + export.Fingerprint[..12] + ".zhr", export.Content, nativeDeadline.Token);
            if (saved)
            {
                workspace.RememberSavedRecoveryFile(captured, export.Fingerprint);
                if (visible is not null && state?.Authority == captured)
                { savedRecoveryFingerprint = export.Fingerprint; savedRecoveryAuthority = captured; }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception error) when (error is not OutOfMemoryException) { errorKey = "Household_ActionFailed"; }
        finally { password = ""; IsBusy = false; Refresh(); }
    }

    [RelayCommand] private Task EnableRecoveryAsync() => RunAsync(async (access, ct) =>
    {
        var captured = state?.Authority;
        if (captured is null || savedRecoveryAuthority != captured || savedRecoveryFingerprint is null || !await ConfirmAsync("HouseholdRecovery_EnableConfirm")) return;
        access.Validate(); await workspace.ConfirmRecoveryExportAsync(transport!, captured, savedRecoveryFingerprint, ct);
        state = await workspace.CompleteRecoveryEnrollmentAsync(transport!, captured, ct); access.Validate();
        savedRecoveryFingerprint = null; savedRecoveryAuthority = null; await ReadLocalAsync(ct);
    });

    [RelayCommand] private Task ImportRecoveryAsync() => RunAsync(async (access, ct) =>
    {
        if (!CanSetUpWhileBusy() || pickedRecovery is null || RecoveryPassword.Length == 0 || !await ConfirmAsync("HouseholdRecovery_ImportConfirm")) return;
        var password = RecoveryPassword; RecoveryPassword = RecoveryPasswordConfirm = "";
        using var kit = HouseholdRecoveryKit.ImportEncrypted(pickedRecovery, password); password = "";
        access.Validate(); state = await workspace.BeginOwnerRecoveryAsync(transport!, kit, ct);
        CryptographicOperations.ZeroMemory(pickedRecovery); pickedRecovery = null; await ReadLocalAsync(ct);
    });

    [RelayCommand] private Task CompleteRecoveryAsync() => RunAsync(async (access, ct) =>
    {
        if (!IsOwnerRecoveryDraft || !await ConfirmAsync("HouseholdRecovery_ImportConfirm")) return;
        access.Validate(); state = await workspace.CompleteOwnerRecoveryAsync(transport!, state!.Authority, Name.Trim(), ct);
        access.Validate(); environment.ConnectionChanged(); await environment.PublishAppliedAsync(ct); await ReadLocalAsync(ct);
    });

    [RelayCommand] private Task RevokeRecoveryAsync() => RunAsync(async (access, ct) =>
    {
        var selected = enrolledRecovery; var version = membership?.Version;
        if (selected is null || version is null || !await ConfirmAsync("HouseholdRecovery_RevokeConfirm")) return;
        access.Validate(); state = await workspace.RevokeRecoveryAsync(transport!, state!.Authority, selected.Value, version.Value, ct);
        await ReadLocalAsync(ct);
    });

    private void RetireRecovery()
    {
        RecoveryPassword = RecoveryPasswordConfirm = ""; savedRecoveryFingerprint = null; savedRecoveryAuthority = null; enrolledRecovery = null;
        if (pickedRecovery is not null) CryptographicOperations.ZeroMemory(pickedRecovery); pickedRecovery = null;
    }

    private async Task ReadRecoveryStatusAsync(HouseholdJournalState? next, HouseholdAccessSnapshot? access, string generationPath, CancellationToken ct)
    {
        Guid? selected = null;
        if (next is { MembershipVersion: > 0 } && access?.Role == Core.Sharing.HouseholdRole.Owner)
        {
            try { selected = (await transport!.RecoveryAsync(next.SpaceId, ct)).EnrollmentId; }
            catch (PersonalSyncHttpException error) when (error.Status == System.Net.HttpStatusCode.NotFound) { }
        }
        ct.ThrowIfCancellationRequested(); enrolledRecovery = selected;
        if (identity is not null && transport is not null)
        {
            var picked = workspace.TakePickedRecoveryFile(generationPath, transport.Peer, identity);
            if (picked is not null) { if (pickedRecovery is not null) CryptographicOperations.ZeroMemory(pickedRecovery); pickedRecovery = picked; }
        }
        var saved = next is null ? null : workspace.SavedRecoveryFileFingerprint(next.Authority);
        savedRecoveryFingerprint = saved; savedRecoveryAuthority = saved is null ? null : next!.Authority;
    }

    private void RefreshRecovery()
    {
        foreach (var property in new[] { nameof(RecoveryFilesAvailable), nameof(IsOwnerRecoveryDraft), nameof(CanExportRecovery), nameof(CanEnableRecovery),
            nameof(CanPickRecovery), nameof(CanImportRecovery), nameof(CanRevokeRecovery), nameof(RecoveryStatus) }) OnPropertyChanged(property);
    }
}
