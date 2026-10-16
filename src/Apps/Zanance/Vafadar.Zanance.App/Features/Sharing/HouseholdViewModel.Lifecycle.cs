using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.App.Features.Sharing;

/// <summary>Public operational audit text; no financial contents, grant ciphertext or credential is presented.</summary>
public sealed record HouseholdAuditItemView(string Caption);

public sealed partial class HouseholdViewModel
{
    private HouseholdLifecycleStatus? lifecycle;
    private HouseholdDeletionIntent? deletion;
    private HouseholdAuditPage? auditPage;

    /// <summary>Remote permanent deletion is an explicit owner action, independently of purchased financial write authority.</summary>
    public bool CanDeleteRemote => CanInteract && IsOwner && HasMembership && deletion is null && ownership is null && !pendingAdministration;
    /// <summary>Original consent remains visible through response/session/process loss, including after remote membership retirement.</summary>
    public bool HasRemoteDeletion => deletion is not null;
    /// <summary>Only an authenticated unaccepted expired request can release the local block, through a separate explicit action.</summary>
    public bool CanFinishRejectedDeletion => CanInteract && deletion is { Rejected: true, Receipt: null };
    /// <summary>Actual readable membership is required for service audit data; lifecycle/privacy metadata stays independent.</summary>
    public bool CanReadAudit => CanInteract && HasReadableRemoteMembership;
    /// <summary>Each explicit page advances only the previously frozen original audit prefix.</summary>
    public bool CanReadMoreAudit => CanReadAudit && auditPage is { Complete: false };
    /// <summary>Only this exact current finite audit page is retained on the visible form.</summary>
    public ObservableCollection<HouseholdAuditItemView> AuditItems { get; } = [];
    /// <summary>Translated current service observation; absence never implies a paid hosting grant.</summary>
    public string LifecycleText => lifecycle is null ? translator["HouseholdLifecycle_Unknown"] : translator[lifecycle.Retention switch
    {
        HouseholdRetentionState.Active => "HouseholdLifecycle_Active",
        HouseholdRetentionState.ReadOnlyGrace => "HouseholdLifecycle_Grace",
        HouseholdRetentionState.Retained => "HouseholdLifecycle_Retained",
        HouseholdRetentionState.DeletionDue => "HouseholdLifecycle_Due",
        _ => "HouseholdLifecycle_Deleted",
    }];
    /// <summary>Exact operational UTC deadlines, kept separate from financial calendars and device-local dates.</summary>
    public string LifecycleDeadlines => lifecycle is null or { Retention: HouseholdRetentionState.Deleted } ? "" :
        translator.Format("HouseholdLifecycle_Deadlines", Deadline(lifecycle.ReadUntilUnixSeconds), Deadline(lifecycle.DeleteAfterUnixSeconds));
    /// <summary>Original request status; accepted access revocation and current physical retirement are distinct.</summary>
    public string RemoteDeletionText => translator[deletion switch
    {
        { Rejected: true } => "HouseholdLifecycle_Rejected",
        { Receipt.StorageRetired: true } => "HouseholdLifecycle_Retired",
        { Receipt: not null } => "HouseholdLifecycle_Accepted",
        _ => "HouseholdLifecycle_Pending",
    }];

    [RelayCommand] private Task ReadLifecycleAsync() => RunAsync(async (_, ct) =>
    { lifecycle = await workspace.ReadLifecycleAsync(transport!, state!.Authority, ct); await ReadLocalAsync(ct); });

    [RelayCommand] private Task DeleteRemoteAsync() => RunAsync(async (access, ct) =>
    {
        var captured = state ?? throw new InvalidOperationException();
        if (!IsOwner || deletion is not null || ownership is not null || pendingAdministration) return;
        var reviewed = await workspace.ReviewDeletionCheckpointAsync(transport!, captured.Authority, ct);
        if (!await ConfirmAsync("HouseholdLifecycle_DeleteConfirm")) return;
        access.Validate();
        try { deletion = await workspace.BeginDeletionAsync(transport!, captured.Authority, reviewed, ct); }
        finally { deletion = await journal.ReadDeletionAsync(captured.Authority, ct); }
        await ReadLocalAsync(ct);
    });

    [RelayCommand] private Task ResumeRemoteDeletionAsync() => RunAsync(async (_, ct) =>
    { deletion = await workspace.ResumeDeletionAsync(transport!, state!.Authority, ct); await ReadLocalAsync(ct); });

    [RelayCommand] private Task ReviewRemoteDeletionAsync() => RunAsync(async (_, ct) =>
    { deletion = await workspace.ReviewDeletionAsync(transport!, state!.Authority, ct); await ReadLocalAsync(ct); });

    [RelayCommand] private Task FinishRejectedDeletionAsync() => RunAsync(async (access, ct) =>
    {
        if (deletion is not { Rejected: true, Receipt: null } original || !await ConfirmAsync("HouseholdLifecycle_FinishConfirm")) return;
        var anchor = await journal.ReadMembershipVersionAsync(state!.Authority, original.AnchorVersion, ct);
        var declaration = Vafadar.Cryptography.StandardJoseSignatures.VerifyJson(original.Request.SignedDeclaration, anchor.Root, HouseholdLifecycleProtocol.DeleteType,
            HouseholdLifecycleProtocol.DeclarationBytes, HouseholdMetadataJsonContext.Default.HouseholdDeleteStatement);
        access.Validate(); await workspace.FinishRejectedDeletionAsync(transport!, state.Authority, declaration.OperationId, ct); await ReadLocalAsync(ct);
    });

    [RelayCommand] private Task ReadAuditAsync() => ReadAuditPageAsync(false);
    [RelayCommand] private Task ReadMoreAuditAsync() => ReadAuditPageAsync(true);
    private Task ReadAuditPageAsync(bool more) => RunAsync(async (_, ct) =>
    {
        if (!HasReadableRemoteMembership) return;
        if (more && auditPage is not { Complete: false }) return;
        auditPage = await workspace.ReadAuditAsync(transport!, state!.Authority, more ? auditPage!.NextAfter : 0, more ? auditPage!.Through : null, ct);
        RefreshAuditCaptions();
    });

    /// <summary>Rebuilds translations from the same verified finite page without another HTTP request or financial write.</summary>
    private void RefreshAuditCaptions()
    {
        AuditItems.Clear(); if (auditPage is null) return;
        foreach (var row in auditPage.Items)
            AuditItems.Add(new(translator.Format("HouseholdLifecycle_AuditItem", translator[row.Action switch
            {
                HouseholdAuditAction.MembershipPublished => "HouseholdLifecycle_Membership",
                HouseholdAuditAction.EncryptedUpdate => "HouseholdLifecycle_Publication",
                HouseholdAuditAction.MemberLeft => "HouseholdLifecycle_Left",
                _ => "HouseholdLifecycle_Device",
            }], Deadline(row.OccurredAtUnixSeconds), row.ActorId)));
    }
    private string Deadline(long? seconds) => seconds is { } value ? DateTimeOffset.FromUnixTimeSeconds(value).ToString("u", CultureInfo.InvariantCulture) : translator["HouseholdLifecycle_NoDeadline"];
    private void RefreshLifecycle()
    {
        RefreshAuditCaptions();
        foreach (var property in new[] { nameof(CanDeleteRemote), nameof(HasRemoteDeletion), nameof(CanFinishRejectedDeletion), nameof(CanReadAudit), nameof(CanReadMoreAudit),
            nameof(LifecycleText), nameof(LifecycleDeadlines), nameof(RemoteDeletionText) }) OnPropertyChanged(property);
    }
}
