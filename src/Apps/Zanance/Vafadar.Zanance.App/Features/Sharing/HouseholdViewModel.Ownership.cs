using CommunityToolkit.Mvvm.Input;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.App.Features.Sharing;

public sealed partial class HouseholdViewModel
{
    private HouseholdOwnershipObservation? ownership;
    private HouseholdOwnershipOffer? ownershipOffer;

    /// <summary>Only a retained original participant operation exposes continuation; a local role alone does not transfer authority.</summary>
    public bool HasOwnershipTransfer => ownership is not null;
    /// <summary>Original pending operations can be declined/canceled; published membership cannot be undone here.</summary>
    public bool CanCancelOwnership => CanInteract && ownership is { Stage: >= HouseholdOwnershipStage.Offered and <= HouseholdOwnershipStage.Accepted };
    /// <summary>Only the original source/target device performs its next explicitly reviewed step.</summary>
    public bool CanAdvanceOwnership => CanInteract && ownership is not null && state is not null && IsOriginalOwnershipActor();
    /// <summary>Only a verified final receipt or authenticated cancellation/expiry may release retained original intent.</summary>
    public bool CanFinishOwnership => CanInteract && ownership is { Stage: HouseholdOwnershipStage.Published or HouseholdOwnershipStage.Canceled or HouseholdOwnershipStage.Expired };
    /// <summary>Translated original stage; no cryptographic key, credential or inferred purchase state.</summary>
    public string OwnershipStatus => translator[ownership?.Stage switch
    {
        HouseholdOwnershipStage.Offered => "HouseholdOwnership_Offered",
        HouseholdOwnershipStage.Prepared => "HouseholdOwnership_Prepared",
        HouseholdOwnershipStage.Authorized => "HouseholdOwnership_Authorized",
        HouseholdOwnershipStage.Accepted => "HouseholdOwnership_Accepted",
        HouseholdOwnershipStage.Published => "HouseholdOwnership_Published",
        HouseholdOwnershipStage.Canceled => "HouseholdOwnership_Canceled",
        HouseholdOwnershipStage.Expired => "HouseholdOwnership_Expired",
        _ => "HouseholdOwnership_None",
    }];

    [RelayCommand] private Task BeginOwnershipAsync(HouseholdApprovedDeviceItem item) => RunAsync(async (access, ct) =>
    {
        if (!ApprovedDevices.Contains(item) || !item.CanTransfer || membership is null || state is null || ownership is not null) return;
        var captured = state; var version = membership.Version;
        var leave = translator["HouseholdOwnership_Leave"];
        var disposition = await interaction.ChooseAsync(translator["HouseholdOwnership_Title"], translator["Common_Cancel"], leave,
            Role(HouseholdRole.Viewer), Role(HouseholdRole.Editor));
        if (disposition is null || disposition == translator["Common_Cancel"]) return;
        var former = disposition == leave ? HouseholdFormerOwnerRole.Leave : disposition == Role(HouseholdRole.Editor) ? HouseholdFormerOwnerRole.Editor : HouseholdFormerOwnerRole.Viewer;
        var confirmed = await interaction.PromptAsync(translator["HouseholdOwnership_Title"], translator["Household_DeviceFingerprintPrompt"],
            translator["Sync_Confirm"], translator["Common_Cancel"], maxLength: 64);
        if (confirmed is null || !await interaction.ConfirmAsync(translator["HouseholdOwnership_Title"],
            item.Caption + "\n\n" + disposition + "\n\n" + translator["HouseholdOwnership_Confirm"], translator["Sync_Confirm"], translator["Common_Cancel"])) return;
        access.Validate(); ownership = await workspace.BeginOwnershipAsync(transport!, captured.Authority, version, item.IdentityId,
            item.Source.Id, ConfirmedFingerprint(confirmed), former, ct); await ReadLocalAsync(ct);
    });
    [RelayCommand] private Task ReadOwnershipAsync() => RunAsync(async (access, ct) =>
    {
        var captured = state ?? throw new InvalidOperationException();
        ownership = await workspace.ReadOwnershipAsync(transport!, captured.Authority, ct); access.Validate(); await ReadLocalAsync(ct);
    });
    [RelayCommand] private Task AdvanceOwnershipAsync() => RunAsync(async (access, ct) =>
    {
        var captured = state ?? throw new InvalidOperationException(); var original = ownership ?? throw new InvalidOperationException();
        if (!IsOriginalOwnershipActor() || !await interaction.ConfirmAsync(translator["HouseholdOwnership_Title"],
            OwnershipStatus + "\n\n" + translator["HouseholdOwnership_Confirm"], translator["Sync_Confirm"], translator["Common_Cancel"])) return;
        access.Validate(); ownership = await workspace.AdvanceOwnershipAsync(transport!, captured.Authority, original.OperationId!.Value, original.Stage, ct);
        await ReadLocalAsync(ct);
    });
    [RelayCommand] private Task CancelOwnershipAsync() => RunAsync(async (access, ct) =>
    {
        var original = ownership ?? throw new InvalidOperationException();
        if (!await ConfirmAsync("HouseholdOwnership_CancelConfirm")) return;
        access.Validate(); ownership = await workspace.CancelOwnershipAsync(transport!, state!.Authority, original.OperationId!.Value, ct); await ReadLocalAsync(ct);
    });
    [RelayCommand] private Task FinishOwnershipAsync() => RunAsync(async (access, ct) =>
    {
        var original = ownership ?? throw new InvalidOperationException();
        if (!await ConfirmAsync("HouseholdOwnership_FinishConfirm")) return;
        access.Validate(); await workspace.FinishOwnershipAsync(transport!, state!.Authority, original.OperationId!.Value, ct); await ReadLocalAsync(ct);
    });

    /// <summary>Uses only the protected previously verified offer; refresh never infers another participant/device's consent.</summary>
    private bool IsOriginalOwnershipActor()
    {
        if (ownershipOffer is not { } offer || ownership is null || state is null) return false;
        return ownership.Stage switch
        {
            HouseholdOwnershipStage.Offered or HouseholdOwnershipStage.Authorized => offer.TargetId == identity && offer.TargetDeviceId == state.DeviceId ||
                ownership.Stage == HouseholdOwnershipStage.Offered && offer.OwnerId == identity && offer.OwnerDeviceId == state.DeviceId,
            HouseholdOwnershipStage.Prepared or HouseholdOwnershipStage.Accepted => offer.OwnerId == identity && offer.OwnerDeviceId == state.DeviceId,
            HouseholdOwnershipStage.Published => true,
            _ => false,
        };
    }

    /// <summary>Presentation uses the original independently verified historical signing root, never an unverified JWT payload.</summary>
    private async Task<HouseholdOwnershipOffer> ReadOwnershipOfferAsync(Vafadar.Zanance.Data.Sharing.HouseholdJournalState current,
        HouseholdOwnershipObservation original, CancellationToken ct)
    {
        var anchor = await journal.ReadMembershipVersionAsync(current.Authority, original.AnchorVersion, ct);
        return Vafadar.Cryptography.StandardJoseSignatures.VerifyJson(original.SignedOffer!, anchor.Root, HouseholdOwnershipProtocol.OfferType, 8192,
            HouseholdMetadataJsonContext.Default.HouseholdOwnershipOffer);
    }

    /// <summary>Publishes live translated statuses and command availability without replacing original operation bytes.</summary>
    private void RefreshOwnership()
    {
        foreach (var property in new[] { nameof(HasOwnershipTransfer), nameof(CanCancelOwnership), nameof(CanAdvanceOwnership), nameof(CanFinishOwnership), nameof(OwnershipStatus) })
            OnPropertyChanged(property);
    }
}
