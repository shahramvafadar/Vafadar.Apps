namespace Vafadar.Zanance.App.Features.Backup;

/// <summary>Original safety/recovery and native UI/security follow-through, without a second restore algorithm.</summary>
public interface IBackupRestoreRuntime
{
    /// <summary>Requests the existing explicit replacement consent.</summary>
    Task<bool> ConfirmReplacementAsync();
    /// <summary>Requests the existing app/device authentication before replacement.</summary>
    Task<bool> AuthenticateAsync();
    /// <summary>Creates the original profile-local safety copy before anything is replaced.</summary>
    Task CreateSafetyCopyAsync();
    /// <summary>Restores all original backup sources while the original automatic-post processor is excluded.</summary>
    Task RestorePrimaryAsync(byte[] package, string? password);
    /// <summary>Publishes restored portable display choices on the UI thread.</summary>
    Task ApplyDisplayAsync();
    /// <summary>Marks restored onboarding complete only when an original account exists.</summary>
    Task CompleteOnboardingAsync();
    /// <summary>Reloads actual device access state before opening the restored profile.</summary>
    Task ReloadLockAsync();
    /// <summary>Runs original idempotent automatic posting and reminder refresh for the injected local day.</summary>
    Task RefreshFinancialWorkAsync(DateOnly day);
    /// <summary>Returns from first-run restoration once and opens the actual restored profile.</summary>
    Task OpenRestoredProfileAsync(bool onboarding);
}
