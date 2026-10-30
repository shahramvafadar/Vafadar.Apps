using Vafadar.Backup;

namespace Vafadar.Zanance.App.Features.Backup;

public sealed partial class BackupViewModel
{
    private readonly IBackupCreationRuntime _creationRuntime;
    private bool _creationAuthenticating, _portableCreationActive;
    // Discovery uses the shared busy affordance too. Its old completion cannot release this original command's lifetime.
    private void BeginPortableCreation()
    { _portableCreationActive = true; IsBusy = true; NotifyPortableAvailability(); }
    private void EndPortableCreation()
    { _portableCreationActive = false; IsBusy = false; NotifyPortableAvailability(); }
    private void NotifyPortableAvailability()
    { OnPropertyChanged(nameof(CanUseBackupTools)); OnPropertyChanged(nameof(CanLeave)); }
    /// <summary>Gets whether this exact original page owns a temporary native sensitive confirmation.</summary>
    internal bool IsConfirmingSensitiveOperation => Restore.IsAuthenticating || _creationAuthenticating;
    /// <summary>Captures literal submitted protection independently of later typing or a retired native dialog.</summary>
    private sealed record CreationInput(bool Protected, string Password, string Confirmation);
    private CreationInput CaptureCreationInput() => new(UsePassword, Password, PasswordConfirm);
    private bool SameCreationInput(CreationInput input) => UsePassword == input.Protected
        && (!input.Protected || (Password == input.Password && PasswordConfirm == input.Confirmation));
    private async Task<bool> ConfirmCreationAsync()
    {
        _creationAuthenticating = true;
        try { return await _creationRuntime.ConfirmAsync(); }
        finally { _creationAuthenticating = false; }
    }
    private void ConsumeOnlyOriginalCreationInput(CreationInput input)
    {
        if (!SameCreationInput(input)) return;
        Password = string.Empty; PasswordConfirm = string.Empty;
    }
    private static void CheckCreationFrame(BackupRestoreSession.PreparationOwner owner)
    { owner.EnsureCurrent(); owner.SourceValidation(); }
    private PortableBackupCreation CreationRequest(IBackupStorage storage, CreationInput input,
        BackupRestoreSession.PreparationOwner owner, Action? validateDestination = null) => new(_backup, storage,
        input.Protected ? input.Password : null, () =>
        {
            CheckCreationFrame(owner); validateDestination?.Invoke();
            if (!SameCreationInput(input)) throw new OperationCanceledException("The original portable input changed.", owner.Token);
        }, owner.Token);
}
