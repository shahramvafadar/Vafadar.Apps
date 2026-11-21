using CommunityToolkit.Mvvm.Input;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Features.Sync;

public sealed partial class PersonalSyncViewModel
{
    private (string User, string Binding)? deletionOwner;
    private AccountDeletionReceipt? deletionReceipt;
    private string deletionPassword = "";

    /// <summary>Gets whether the signed-in account may open deletion intake.</summary>
    public bool ShowAccountDeletion => ShowPersonalSignedIn && !IsOnboardingRestore;

    /// <summary>Shows the request form only after a successful status read for this account and peer.</summary>
    public bool ShowAccountDeletionForm => OwnsAccountDeletionStatus && deletionReceipt is null;

    /// <summary>Shows the original pending receipt for this account and peer.</summary>
    public bool ShowAccountDeletionReceipt => OwnsAccountDeletionStatus && deletionReceipt is not null;

    /// <summary>Gets the pending receipt reference without retaining password proof.</summary>
    public string? AccountDeletionReference => ShowAccountDeletionReceipt ? deletionReceipt?.ReceiptId.ToString("D") : null;

    /// <summary>Gets or sets the transient password dedicated to confirming the deletion request.</summary>
    public string DeletionPassword
    {
        get => deletionPassword;
        set
        {
            var input = value ?? "";
            if (input.Length != 0 && (!CanInteract || !ShowAccountDeletionForm)) return;
            SetProperty(ref deletionPassword, input);
        }
    }

    private (string User, string Binding)? CurrentDeletionOwner =>
        ShowAccountDeletion && identity is { } current && transport is { } client
            ? (current.Id, client.Peer.BindingKey(current.Id))
            : null;

    private bool OwnsAccountDeletionStatus => deletionOwner is { } owner && CurrentDeletionOwner == owner;

    [RelayCommand]
    private Task ReadAccountDeletionAsync()
    {
        if (!CanInteract || CurrentDeletionOwner is not { } owner) return Task.CompletedTask;
        return RunAsync(async (access, ct) =>
        {
            ClearAccountDeletionPassword();
            deletionOwner = null;
            RefreshAccountDeletionProperties();
            access.Validate();
            var receipt = await transport!.ReadAccountDeletionAsync(owner.User, ct);
            access.Validate();
            deletionOwner = owner;
            deletionReceipt = receipt;
        }, validateRequest: () => ValidateDeletionOwner(owner));
    }

    [RelayCommand]
    private Task RequestAccountDeletionAsync()
    {
        if (!CanInteract || !ShowAccountDeletionForm || CurrentDeletionOwner is not { } owner)
            return Task.CompletedTask;
        var password = deletionPassword;
        return RunAsync(async (access, ct) =>
        {
            ClearAccountDeletionPassword();
            if (password.Length is < 12 or > 256)
                throw new ArgumentException("Fresh account credential proof is required.");
            // Hide the submission form before the request. A lost response can be reconciled via status.
            deletionOwner = null;
            RefreshAccountDeletionProperties();
            access.Validate();
            var receipt = await transport!.RequestAccountDeletionAsync(owner.User, password, ct);
            access.Validate();
            deletionReceipt = receipt;
            deletionOwner = owner;
        }, validateRequest: () => ValidateDeletionOwner(owner));
    }

    private void ValidateDeletionOwner((string User, string Binding) owner)
    {
        if (CurrentDeletionOwner != owner)
            throw new OperationCanceledException("The original deletion request owner retired.");
        RequireSession();
    }

    private void ClearAccountDeletionPassword()
    {
        if (deletionPassword.Length == 0) return;
        deletionPassword = "";
        OnPropertyChanged(nameof(DeletionPassword));
    }

    private void RefreshAccountDeletionProperties()
    {
        OnPropertyChanged(nameof(ShowAccountDeletion));
        OnPropertyChanged(nameof(ShowAccountDeletionForm));
        OnPropertyChanged(nameof(ShowAccountDeletionReceipt));
        OnPropertyChanged(nameof(AccountDeletionReference));
    }
}
