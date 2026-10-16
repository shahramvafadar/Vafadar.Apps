using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Core.Text;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Features.Sharing;

/// <summary>One actual independently confirmed pending device; text is public metadata, never a claim capability.</summary>
public sealed record HouseholdPendingItem(HouseholdPendingClaim Source, string Caption);
/// <summary>One independently self-proven new device of an already accepted identity.</summary>
public sealed record HouseholdPendingDeviceItem(HouseholdPendingDevice Source, string Caption);
/// <summary>One original signed approved device; removal remains a fresh owner operation.</summary>
public sealed record HouseholdApprovedDeviceItem(string IdentityId, HouseholdDeviceDefinition Source, string Caption, bool CanRevoke, bool CanTransfer = false);
/// <summary>One original signed member and current translated role.</summary>
public sealed record HouseholdMemberItem(HouseholdMemberDefinition Source, string Caption)
{
    /// <summary>Ownership has a separate two-sided transfer flow, never the ordinary role picker.</summary>
    public bool CanChange => Source.Role != HouseholdRole.Owner;
}

/// <summary>Explicit household setup/administration using the original protected journal and encrypted financial path.</summary>
public sealed partial class HouseholdViewModel(HouseholdWorkspace workspace, HouseholdJournal journal, HouseholdSyncCoordinator coordinator,
    PersonalSyncStore original, ZananceStore finances, IPersonalSyncEnvironment environment, IPersonalSyncTokenStore tokens,
    ProfileService profiles, IAppFlowHost host, IAppInteraction interaction, Translator translator, TimeProvider time,
    HouseholdProjectionStore sourceProjection, HouseholdProjectionHandoff sourceHandoff) : ViewModelBase
{
    private CancellationTokenSource? visible;
    private HouseholdServiceTransport? transport;
    private HouseholdJournalState? state;
    private HouseholdAccessSnapshot? accessState;
    private HouseholdMembershipStatement? membership;
    private string? identity;
    private string? errorKey;
    private bool ready;
    private bool personalBound;
    private bool pendingAdministration;
    private bool connected;
    private HouseholdInvitationNotice? pendingInvitation;
    private Func<Task>? closeOnboarding;
    private Func<Task>? finishOnboarding;
#if DEBUG
    /// <summary>Independent native qualification receives only exception type/status metadata, never messages, requests or keys.</summary>
    internal Action<string, int?, int>? ReviewActionFailure { get; set; }
#endif

    /// <summary>Explicit network edition only; an offline app never gains a network permission from this screen.</summary>
    public bool Available => environment.Available;
    /// <summary>Complete local snapshot publication controls the initial cover.</summary>
    public bool IsReady => ready;
    /// <summary>No page/navigation mutation may retire a currently submitted setup action.</summary>
    public bool CanLeave => !IsBusy;
    /// <summary>Only a visible unlocked current profile can submit another action.</summary>
    public bool CanInteract => Available && ready && visible is not null && !IsBusy;
    /// <summary>A personal sync binding cannot be relabeled as encrypted household sharing.</summary>
    public bool CanSetUp => CanInteract && identity is not null && !personalBound && state is null;
    /// <summary>Setup visibility stays stable while its submitted action runs.</summary>
    public bool ShowSetup => ready && !personalBound && state is null;
    /// <summary>Only a complete original signed roster exposes refresh/sync administration.</summary>
    public bool HasMembership => state is { MembershipVersion: > 0 };
    /// <summary>Only the original owner draft can complete retained creation.</summary>
    public bool CanCompleteOwner => CanInteract && state is { OwnerCreation: true } && !connected && pendingSource is null && !HasPendingSourceReceipt;
    /// <summary>Last verified owning identity; mutable administration additionally requires current device write authority.</summary>
    public bool IsOwner => accessState?.Role == HouseholdRole.Owner;
    /// <summary>Known owner status alone does not permit invitations while expired, paused, unverified or awaiting original administration.</summary>
    public bool CanAdminister => CanInteract && IsOwner && accessState?.CanWrite == true && !pendingAdministration && deletion is null;
    /// <summary>Pending original publication remains distinct from a new invitation/member action.</summary>
    public bool HasPendingAdministration => pendingAdministration;
    /// <summary>The original private invitation can be explicitly finished without retaining an old clipboard copy.</summary>
    public bool HasPendingInvitation => pendingInvitation is not null;
    /// <summary>Pending claims are a waiting state, never a grant.</summary>
    public bool CanCheckAdmission => CanInteract && state is { OwnerCreation: false } && !IsOwnerRecoveryDraft;
    /// <summary>Only an actual connected room can run the original financial coordinator.</summary>
    public bool CanSynchronize => CanInteract && HasSynchronizableMembership;
    /// <summary>Explicit remote refusal overrides the last positive grant snapshot; metadata-only privacy reads never renew financial/audit access.</summary>
    private bool HasReadableRemoteMembership => state is { MembershipVersion: > 0 } && deletion is null &&
        accessState is { Retention: HouseholdRetentionState.Active or HouseholdRetentionState.ReadOnlyGrace,
            BlockedBy: not HouseholdWriteBlockReason.RemoteRefused };
    /// <summary>Manual financial transport additionally needs its real connected ledger, independently of the visible busy-state gate.</summary>
    private bool HasSynchronizableMembership => connected && HasReadableRemoteMembership;
    /// <summary>First-run completion stays explicit after actual connected financial setup.</summary>
    public bool CanFinish => CanInteract && finishOnboarding is not null && connected && state is { MembershipVersion: > 0 };
    /// <summary>The original wizard remains attached behind this page.</summary>
    public bool IsOnboarding => closeOnboarding is not null;
    /// <summary>Fixed translated feedback, never a remote payload or credential.</summary>
    public string? Error => errorKey is null ? null : translator[errorKey];
    /// <summary>Last actually verified role/write state, separate from service connectivity and purchases.</summary>
    public string StatusText => personalBound ? translator["Household_SeparateRequired"] : state is null ? translator["Household_NotConnected"] :
        state.MembershipVersion == 0 ? translator["Household_Waiting"] : translator.Format("Household_Access", Role(accessState?.Role),
            translator[accessState?.CanWrite == true ? "Household_Writable" : "Household_ReadOnly"]);
    /// <summary>Independent public fingerprints, with complete labels and no private key bytes.</summary>
    public string Fingerprints => state is null ? "" : translator.Format("Household_Fingerprints", membership is null ? state.PinnedRoot.Fingerprint :
        new Vafadar.Cryptography.PublicSigningRecipientIdentity(membership.OwnerKeys.SigningKey, membership.OwnerKeys.WrappingKey).Fingerprint, state.DeviceFingerprint);
    /// <summary>Actual pending claims, retained only while the page is visible.</summary>
    public ObservableCollection<HouseholdPendingItem> Pending { get; } = [];
    /// <summary>Complete original signed membership, never inferred from email/purchase state.</summary>
    public ObservableCollection<HouseholdMemberItem> Members { get; } = [];
    /// <summary>Only owner-scoped original pending self-enrollments, never directory-based approval.</summary>
    public ObservableCollection<HouseholdPendingDeviceItem> PendingDevices { get; } = [];
    /// <summary>Actual pinned member-device inventory, with no inferred friendly identity.</summary>
    public ObservableCollection<HouseholdApprovedDeviceItem> ApprovedDevices { get; } = [];
    /// <summary>Reviewed public service address.</summary>
    [ObservableProperty] public partial string Address { get; set; } = "";
    /// <summary>Optional independent local certificate pin.</summary>
    [ObservableProperty] public partial string Fingerprint { get; set; } = "";
    /// <summary>Explicit user-entered identity address.</summary>
    [ObservableProperty] public partial string Email { get; set; } = "";
    /// <summary>In-memory only identity password, cleared after use/retirement.</summary>
    [ObservableProperty] public partial string Password { get; set; } = "";
    /// <summary>Local profile/connection label; no clear financial name is sent to the household service.</summary>
    [ObservableProperty] public partial string Name { get; set; } = "";
    /// <summary>Explicit copy/paste invitation capability; retained bytes live only in protected setup storage.</summary>
    [ObservableProperty] public partial string InvitationText { get; set; } = "";
    /// <summary>Root fingerprint independently compared with the inviter, never auto-filled from an untrusted directory.</summary>
    [ObservableProperty] public partial string ConfirmedRoot { get; set; } = "";

    /// <summary>Preserves the original first-run page and explicit close/finish actions.</summary>
    public void BeginOnboarding(Func<Task> close, Func<Task> finish) { closeOnboarding = close; finishOnboarding = finish; }
    /// <summary>Starts only a covered original local read after the native page appears.</summary>
    public async Task ActivateAsync()
    {
        visible?.Cancel(); visible?.Dispose(); visible = new(); ready = false;
        translator.PropertyChanged -= TranslationChanged; translator.PropertyChanged += TranslationChanged;
        if (Address.Length == 0) { var saved = environment.ReadConnection(); Address = saved.Address; Fingerprint = saved.Fingerprint; }
        if (Name.Length == 0) Name = profiles.NameOf(profiles.Current);
        await ReloadAsync();
    }
    /// <summary>Retires visible capability/password and all stale page completions without deleting durable intent.</summary>
    public void Deactivate()
    {
        visible?.Cancel(); visible?.Dispose(); visible = null; transport?.Dispose(); transport = null;
        Password = ""; InvitationText = ""; ConfirmedRoot = ""; identity = null; Pending.Clear(); Members.Clear();
        PendingDevices.Clear(); ApprovedDevices.Clear();
        pendingInvitation = null;
        ownership = null;
        ownershipOffer = null;
        lifecycle = null; deletion = null; auditPage = null; AuditItems.Clear();
        RetireRecovery();
        RetireSourceSelection();
        translator.PropertyChanged -= TranslationChanged;
    }

    [RelayCommand] private Task ReloadAsync() => RunAsync(async (_, ct) => await ReadLocalAsync(ct), covered: true);
    [RelayCommand] private Task SignInAsync() => RunAsync(async (access, ct) =>
    {
        var password = Password; Password = ""; identity = (await transport!.SignInAsync(Email, password, ct)).Id;
        access.Validate(); environment.RememberPeer(transport.Peer); await RebindExistingAsync(ct); await ReadLocalAsync(ct);
    });
    [RelayCommand] private Task DiscoverAsync() => RunAsync(async (access, ct) =>
    { identity = (await transport!.IdentityAsync(ct)).Id; access.Validate(); await RebindExistingAsync(ct); await ReadLocalAsync(ct); });
    [RelayCommand] private Task BeginOwnerAsync() => RunAsync(async (access, ct) =>
    {
        if (!CanSetUpWhileBusy() || !await ConfirmAsync("Household_CreateConfirm")) return;
        access.Validate(); state = await workspace.BeginOwnerAsync(transport!, ct); await ReadLocalAsync(ct);
    });
    [RelayCommand] private Task CompleteOwnerAsync() => RunAsync(async (access, ct) =>
    {
        if (pendingSource is not null || HasPendingSourceReceipt) throw new InvalidOperationException();
        var captured = state ?? throw new InvalidOperationException();
        if (!await ConfirmAsync("Household_PublishConfirm")) return;
        access.Validate(); state = await workspace.CompleteOwnerAsync(transport!, captured.Authority, Name.Trim(), ct);
        _ = await coordinator.SynchronizeAsync(transport!, state.Authority, ct); access.Validate();
        environment.ConnectionChanged(); await ReadLocalAsync(ct);
    });
    [RelayCommand] private Task CreateInvitationAsync() => RunAsync(async (access, ct) =>
    {
        var selected = await interaction.ChooseAsync(translator["Household_Invite"], translator["Common_Cancel"], Role(HouseholdRole.Viewer), Role(HouseholdRole.Editor));
        if (selected is null || selected == translator["Common_Cancel"]) return;
        var role = selected == Role(HouseholdRole.Viewer) ? HouseholdRole.Viewer : HouseholdRole.Editor;
        access.Validate(); var package = await workspace.InviteAsync(transport!, state!.Authority, role, ct); access.Validate();
        InvitationText = JsonSerializer.Serialize(package, HouseholdMetadataJsonContext.Default.HouseholdInvitationPackage); await ReadLocalAsync(ct);
    });
    [RelayCommand] private Task FinishInvitationAsync() => RunAsync(async (access, ct) =>
    {
        var original = pendingInvitation;
        if (Encoding.UTF8.GetByteCount(InvitationText) > 8192 || !await ConfirmAsync("Household_InvitationFinishConfirm")) return;
        access.Validate();
        if (string.IsNullOrWhiteSpace(InvitationText))
        {
            if (original is null) return;
            await workspace.FinishPendingInvitationAsync(transport!, state!.Authority, original, ct);
        }
        else
        {
            var package = JsonSerializer.Deserialize(InvitationText, HouseholdMetadataJsonContext.Default.HouseholdInvitationPackage) ?? throw new InvalidDataException();
            await workspace.FinishDisplayedInvitationAsync(transport!, state!.Authority, package, ct);
        }
        InvitationText = ""; await ReadLocalAsync(ct);
    });
    [RelayCommand] private Task ClaimAsync() => RunAsync(async (access, ct) =>
    {
        if (personalBound || Encoding.UTF8.GetByteCount(InvitationText) > 8192 || !await ConfirmAsync("Household_JoinConfirm")) return;
        var package = JsonSerializer.Deserialize(InvitationText, HouseholdMetadataJsonContext.Default.HouseholdInvitationPackage) ?? throw new InvalidDataException();
        access.Validate(); state = await workspace.ClaimAsync(transport!, package, ConfirmedFingerprint(ConfirmedRoot), ct); InvitationText = ""; await ReadLocalAsync(ct);
    });
    [RelayCommand] private Task CheckAdmissionAsync() => RunAsync(async (access, ct) =>
    {
        if (state is { MembershipVersion: 0 } && HouseholdSetupAnchors.Kind(state.SignedInvitation) == HouseholdSetupAnchors.DeviceAccess)
            state = await workspace.ResumeOwnDeviceAsync(transport!, state.Authority, ct);
        var complete = await workspace.CompleteJoinAsync(transport!, state!.Authority, Name.Trim(), ct); access.Validate();
        if (complete) { environment.ConnectionChanged(); await environment.PublishAppliedAsync(ct); }
        await ReadLocalAsync(ct);
    });
    [RelayCommand] private Task CreateDeviceAccessAsync(HouseholdMemberItem item) => RunAsync(async (access, ct) =>
    {
        if (!Members.Contains(item) || !await ConfirmAsync("HouseholdDeviceAccess_ExportConfirm")) return;
        access.Validate(); var package = await workspace.CreateDeviceAccessAsync(transport!, state!.Authority, item.Source.IdentityId, ct);
        InvitationText = JsonSerializer.Serialize(package, HouseholdMetadataJsonContext.Default.HouseholdDeviceAccessPackage);
    });
    [RelayCommand] private Task EnrollOwnDeviceAsync() => RunAsync(async (access, ct) =>
    {
        if (identity is null || personalBound || Encoding.UTF8.GetByteCount(InvitationText) > 8192 || !await ConfirmAsync("HouseholdDeviceAccess_RequestConfirm")) return;
        var package = JsonSerializer.Deserialize(InvitationText, HouseholdMetadataJsonContext.Default.HouseholdDeviceAccessPackage) ?? throw new InvalidDataException();
        access.Validate(); state = await workspace.EnrollOwnDeviceAsync(transport!, package, ConfirmedFingerprint(ConfirmedRoot), ct);
        InvitationText = ""; await ReadLocalAsync(ct);
    });
    [RelayCommand] private Task RefreshMembersAsync() => RunAsync(async (access, ct) =>
    {
        state = await coordinator.RefreshAuthorityAsync(transport!, state!.Authority, ct); access.Validate(); await ReadLocalAsync(ct);
    });
    [RelayCommand] private Task ApproveAsync(HouseholdPendingItem item) => RunAsync(async (access, ct) =>
    {
        if (!Pending.Contains(item)) throw new InvalidOperationException();
        var confirmation = await interaction.PromptAsync(translator["Household_Approve"], translator["Household_DeviceFingerprintPrompt"],
            translator["Sync_Confirm"], translator["Common_Cancel"], maxLength: 64);
        if (confirmation is null) return; access.Validate();
        await workspace.ApproveAsync(transport!, state!.Authority, item.Source, ConfirmedFingerprint(confirmation), ct); await ReadLocalAsync(ct);
    });
    [RelayCommand] private Task ChangeMemberAsync(HouseholdMemberItem item) => RunAsync(async (access, ct) =>
    {
        if (!Members.Contains(item) || item.Source.Role == HouseholdRole.Owner || membership is null) return;
        var version = membership.Version; var next = item.Source.Role == HouseholdRole.Viewer ? HouseholdRole.Editor : HouseholdRole.Viewer;
        var choice = await interaction.ChooseAsync(translator["Household_Members"], translator["Common_Cancel"], Role(next), translator["Household_Revoke"]);
        if (choice is null || choice == translator["Common_Cancel"] || !await ConfirmAsync("Household_MemberChangeConfirm")) return;
        access.Validate(); await workspace.ChangeMemberAsync(transport!, state!.Authority, version, item.Source.IdentityId,
            choice == translator["Household_Revoke"] ? null : next, ct); await ReadLocalAsync(ct);
    });
    [RelayCommand] private Task ApproveDeviceAsync(HouseholdPendingDeviceItem item) => RunAsync(async (access, ct) =>
    {
        if (!PendingDevices.Contains(item) || membership is null) throw new InvalidOperationException();
        var version = membership.Version;
        var confirmed = await interaction.PromptAsync(translator["Household_Approve"], translator["Household_DeviceFingerprintPrompt"],
            translator["Sync_Confirm"], translator["Common_Cancel"], maxLength: 64);
        if (confirmed is null) return; access.Validate();
        await workspace.ApproveDeviceAsync(transport!, state!.Authority, version, item.Source, ConfirmedFingerprint(confirmed), ct); await ReadLocalAsync(ct);
    });
    [RelayCommand] private Task RevokeDeviceAsync(HouseholdApprovedDeviceItem item) => RunAsync(async (access, ct) =>
    {
        if (!ApprovedDevices.Contains(item) || !item.CanRevoke || membership is null || !IsOwner) throw new InvalidOperationException();
        var version = membership.Version;
        if (!await interaction.ConfirmAsync(translator["Household_Devices"], item.Caption + "\n\n" + translator["Household_RevokeDeviceConfirm"],
            translator["Household_RevokeDevice"], translator["Common_Cancel"])) return;
        var fingerprint = new Vafadar.Cryptography.PublicSigningRecipientIdentity(item.Source.Keys.SigningKey, item.Source.Keys.WrappingKey).Fingerprint;
        access.Validate(); state = await workspace.RevokeDeviceAsync(transport!, state!.Authority, version, item.IdentityId, item.Source.Id, fingerprint, ct);
        await ReadLocalAsync(ct);
    });
    [RelayCommand] private Task ResumeAdministrationAsync() => RunAsync(async (_, ct) =>
    { state = await workspace.ResumePendingAdministrationAsync(transport!, state!.Authority, ct); await ReadLocalAsync(ct); });
    [RelayCommand] private Task SynchronizeAsync() => RunAsync(async (access, ct) =>
    {
        if (!HasSynchronizableMembership) return;
        var result = await coordinator.SynchronizeAsync(transport!, state!.Authority, ct); access.Validate();
        if (result.Applied) await environment.PublishAppliedAsync(ct);
        if (result.NeedsReview) errorKey = "Household_ConflictReview";
        await ReadLocalAsync(ct);
    });
    [RelayCommand] private Task CostsAsync() => CanInteract && connected
        ? interaction.NavigateAsync(AppRoutes.HouseholdCostsRoute) : Task.CompletedTask;
    [RelayCommand] private Task ReviewChangesAsync() => CanInteract && connected && HasMembership
        ? interaction.NavigateAsync(AppRoutes.PersonalSyncRoute) : Task.CompletedTask;
    [RelayCommand] private Task FinishAsync() => RunAsync(async (access, ct) =>
    {
        if (!connected || finishOnboarding is null || state is not { MembershipVersion: > 0 } || !await ConfirmAsync("Household_FinishConfirm")) return;
        access.Validate(); var settings = await finances.GetSettingsAsync(ct); settings.OnboardingCompleted = true; finances.SaveSettings(settings);
        access.Validate(); await finishOnboarding();
    });
    [RelayCommand] private Task CloseAsync() => !IsBusy && closeOnboarding is not null ? closeOnboarding() : Task.CompletedTask;
    [RelayCommand] private async Task CreateSeparateProfileAsync()
    {
        if (!CanInteract) return; IsBusy = true; Refresh();
        try
        {
            var access = environment.Capture(); var review = profiles.ReadWorkReview();
            var name = await interaction.PromptAsync(translator["Household_NewProfile"], translator["Profile_NamePrompt"], translator["Common_Save"],
                translator["Common_Cancel"], maxLength: ProfileService.MaxNameLength);
            if (string.IsNullOrWhiteSpace(name)) return; access.Validate(); var token = visible!.Token;
            var created = await profiles.CreateReviewedAsync(review, name, token); token.ThrowIfCancellationRequested();
            if (await profiles.SwitchReviewedAsync(profiles.ReadWorkReview(), created)) host.ShowCurrentProfile();
        }
        catch (OperationCanceledException) { }
        catch (Exception error) when (error is not OutOfMemoryException) { errorKey = "Household_ActionFailed"; }
        finally { IsBusy = false; Refresh(); }
    }

    private async Task ReadLocalAsync(CancellationToken ct)
    {
        var review = await original.ReviewAsync(ct); var status = await original.StatusAsync(ct);
        personalBound = review.RemoteProfile is not null && review.TransportKind == ProfileSynchronizationKind.Personal;
        var next = identity is not null && transport is not null ? await journal.CaptureAsync(transport.Peer, identity, ct) : null;
        var nextAccess = next is null ? null : await journal.ReadAccessAsync(next.Authority, ct);
        var nextMembership = next is { MembershipVersion: > 0 } ? (await journal.ReadCurrentMembershipAsync(next.Authority, ct)).Snapshot() : null;
        var pending = next is not null && await journal.ReadMembershipProposalAsync(next.Authority, ct) is not null;
        var invitation = next is not null && nextAccess?.Role == HouseholdRole.Owner ? await journal.ReadInvitationNoticeAsync(next.Authority, ct) : null;
        var claims = nextAccess is { Role: HouseholdRole.Owner, CanWrite: true } && next is { MembershipVersion: > 0 }
            ? await transport!.PendingClaimsAsync(next.SpaceId, next.DeviceId, ct) : [];
        var deviceRequests = nextAccess is { Role: HouseholdRole.Owner, CanWrite: true } && next is { MembershipVersion: > 0 }
            ? await transport!.PendingDevicesAsync(next.SpaceId, next.DeviceId, ct) : [];
        await ReadRecoveryStatusAsync(next, nextAccess, review.GenerationPath, ct);
        await ReadSourceSelectionAsync(next, ct);
        var nextOwnership = next is null ? null : await journal.ReadOwnershipAsync(next.Authority, ct);
        var nextOwnershipOffer = nextOwnership is null || next is null ? null : await ReadOwnershipOfferAsync(next, nextOwnership, ct);
        var nextDeletion = next is null ? null : await journal.ReadDeletionAsync(next.Authority, ct);
        ct.ThrowIfCancellationRequested(); state = next; accessState = nextAccess; membership = nextMembership;
        pendingAdministration = pending; connected = status.Connected && review.TransportKind == ProfileSynchronizationKind.Household;
        pendingInvitation = invitation;
        ownership = nextOwnership;
        ownershipOffer = nextOwnershipOffer;
        deletion = nextDeletion;
        Pending.Clear(); foreach (var row in claims) Pending.Add(PendingItem(row));
        PendingDevices.Clear(); foreach (var row in deviceRequests) PendingDevices.Add(PendingDeviceItem(row));
        Members.Clear(); if (membership is not null) foreach (var row in membership.Members) Members.Add(new(row, translator.Format("Household_Member", row.IdentityId, Role(row.Role))));
        RefreshApprovedDevices();
        ready = true;
    }
    private async Task RunAsync(Func<PersonalSyncAccess, CancellationToken, Task> action, bool covered = false)
    {
        if (!Available || visible is null || IsBusy || !covered && !ready) return;
        IsBusy = true; errorKey = null; Refresh();
        try
        {
            var access = environment.Capture(); using var linked = CancellationTokenSource.CreateLinkedTokenSource(access.Cancellation, visible.Token);
            var peer = new LocalSyncPeer(Address, Fingerprint); transport?.Dispose(); transport = new(peer, tokens, time, access.Validate);
            await action(access, linked.Token); access.Validate();
        }
        catch (OperationCanceledException) { }
        catch (PersonalSyncHttpException error)
        {
#if DEBUG
            ReviewActionFailure?.Invoke(error.GetType().FullName ?? "HttpException", (int)error.Status, error.HResult);
#endif
            await RefreshRecordedRefusalAsync(error.Status);
            errorKey = error.Status == System.Net.HttpStatusCode.Unauthorized ? "Sync_SignInRequired" : "Household_ActionFailed";
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
#if DEBUG
            ReviewActionFailure?.Invoke(error.GetType().FullName ?? "Exception", null, error.HResult);
#endif
            errorKey = "Household_ActionFailed";
        }
        finally { Password = ""; RecoveryPassword = RecoveryPasswordConfirm = ""; IsBusy = false; Refresh(); }
    }
    /// <summary>Publishes only a refusal already recorded by the scoped transport; no HTTP retry or fresh grant is inferred.</summary>
    private async Task RefreshRecordedRefusalAsync(System.Net.HttpStatusCode status)
    {
        if (status is not (System.Net.HttpStatusCode.Forbidden or System.Net.HttpStatusCode.NotFound or System.Net.HttpStatusCode.Gone) ||
            state is not { MembershipVersion: > 0 } retained || visible is not { IsCancellationRequested: false } active) return;
        try
        {
            // A warm page still holds its previous positive presentation snapshot.
            // Read the durable local barrier without reloading owner metadata over
            // the refused connection. Keep original finance, keys and pending work.
            var latest = await journal.ReadAccessAsync(retained.Authority, active.Token);
            if (ReferenceEquals(state, retained) && ReferenceEquals(visible, active) && !active.IsCancellationRequested &&
                latest.BlockedBy == HouseholdWriteBlockReason.RemoteRefused) accessState = latest;
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            // A retired/corrupt local context cannot preserve an enabled old hint.
            // It never becomes a new setup destination or another profile's grant.
            if (ReferenceEquals(state, retained) && ReferenceEquals(visible, active)) accessState = null;
        }
    }
    private async Task RebindExistingAsync(CancellationToken ct)
    {
        var retained = await journal.CaptureAsync(transport!.Peer, identity!, ct);
        if (retained is { SpaceId: var space, DeviceId: var device } && space != Guid.Empty && device != Guid.Empty)
        {
            // A rejected reconnect does not turn this existing profile into an
            // empty setup/recovery destination. Retaining context grants no role,
            // key or connectivity; those still require successful verification.
            state = retained;
            try { state = await workspace.RebindCurrentDeviceAsync(transport, retained.Authority, ct); }
            catch (PersonalSyncHttpException error) when (error.Status == System.Net.HttpStatusCode.NotFound && retained.MembershipVersion == 0)
            { state = retained; /* Pending local setup cannot infer that the server has approved its device. */ }
            catch (PersonalSyncHttpException error) when (error.Status == System.Net.HttpStatusCode.NotFound)
            {
                // A departed source owner can still authenticate its original private
                // device to observe only this retained handoff receipt, never room data.
                if (await journal.ReadOwnershipAsync(retained.Authority, ct) is null && await journal.ReadDeletionAsync(retained.Authority, ct) is null) throw;
                state = retained;
            }
        }
    }
    private bool CanSetUpWhileBusy() => identity is not null && !personalBound && state is null;
    private string Role(HouseholdRole? role) => translator[role switch { HouseholdRole.Owner => "Household_Owner", HouseholdRole.Editor => "Household_Editor", HouseholdRole.Viewer => "Household_Viewer", _ => "Household_Unverified" }];
    private Task<bool> ConfirmAsync(string key) => interaction.ConfirmAsync(translator["Household_Title"], translator[key], translator["Sync_Confirm"], translator["Common_Cancel"]);
    private static string ConfirmedFingerprint(string value) => Digits.ToAscii(value.Trim()).ToLowerInvariant();
    private void TranslationChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
    {
        var claims = Pending.Select(item => item.Source).ToArray(); Pending.Clear(); foreach (var row in claims) Pending.Add(PendingItem(row));
        var devices = PendingDevices.Select(item => item.Source).ToArray(); PendingDevices.Clear(); foreach (var row in devices) PendingDevices.Add(PendingDeviceItem(row));
        Members.Clear(); if (membership is not null) foreach (var row in membership.Members) Members.Add(new(row, translator.Format("Household_Member", row.IdentityId, Role(row.Role))));
        RefreshApprovedDevices();
        Refresh();
    }
    private HouseholdPendingItem PendingItem(HouseholdPendingClaim row) => new(row, translator.Format("Household_Device", row.IdentityId,
        new Vafadar.Cryptography.PublicSigningRecipientIdentity(row.DeviceKeys.SigningKey, row.DeviceKeys.WrappingKey).Fingerprint));
    private HouseholdPendingDeviceItem PendingDeviceItem(HouseholdPendingDevice row) => new(row, translator.Format("Household_Device", row.IdentityId,
        new Vafadar.Cryptography.PublicSigningRecipientIdentity(row.DeviceKeys.SigningKey, row.DeviceKeys.WrappingKey).Fingerprint));
    private void RefreshApprovedDevices()
    {
        ApprovedDevices.Clear(); if (membership is null) return;
        foreach (var member in membership.Members)
        foreach (var device in member.Devices)
            ApprovedDevices.Add(new(member.IdentityId, device, translator.Format("Household_ApprovedDevice", member.IdentityId, Role(member.Role),
                new Vafadar.Cryptography.PublicSigningRecipientIdentity(device.Keys.SigningKey, device.Keys.WrappingKey).Fingerprint),
                member.Devices.Length > 1 && device.Id != state?.DeviceId, member.Role != HouseholdRole.Owner));
    }
    private void Refresh()
    {
        RefreshRecovery();
        RefreshSourceSelection();
        RefreshOwnership();
        RefreshLifecycle();
        foreach (var property in new[] { nameof(IsReady), nameof(CanLeave), nameof(CanInteract), nameof(CanSetUp), nameof(ShowSetup), nameof(HasMembership), nameof(CanCompleteOwner), nameof(IsOwner), nameof(CanAdminister),
            nameof(HasPendingAdministration), nameof(HasPendingInvitation), nameof(CanCheckAdmission), nameof(CanSynchronize), nameof(CanFinish), nameof(IsOnboarding), nameof(Error), nameof(StatusText), nameof(Fingerprints) }) OnPropertyChanged(property);
    }
}
