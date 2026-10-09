namespace Vafadar.Zanance.App.Security;

/// <summary>Native lock covers and confirmations; the access policy has no dependency on a window or control.</summary>
public interface IAppLockHost
{
    /// <summary>Refreshes the platform's screenshot and recent-apps protection.</summary>
    void ApplyProtection();
    /// <summary>Creates a cover, or returns null while no window is available. This does not grant access.</summary>
    ILockCover? CreateCover(AppLockService service, bool prompt);
    /// <summary>Asks for the app PIN for a sensitive operation, returning false when no window is available.</summary>
    Task<bool> ConfirmPinAsync(AppLockService service, string reason);
    /// <summary>Asks whether to remove the forgotten PIN after device authentication.</summary>
    Task<bool> ConfirmRecoveryAsync();
}

/// <summary>A native cover whose lifetime is controlled by the application access gate.</summary>
public interface ILockCover
{
    /// <summary>Gets whether the cover still belongs to the current window.</summary>
    bool IsOnScreen { get; }
    /// <summary>Gets or sets whether an existing lock preceded the background transition.</summary>
    bool WasLockedBeforeSleep { get; set; }
    /// <summary>Shows the cover after the access gate has retained its identity.</summary>
    Task ShowAsync();
    /// <summary>Prompts authentication without replacing the cover.</summary>
    Task PromptAsync();
    /// <summary>Removes this cover after successful authentication.</summary>
    Task CloseAsync();
}
