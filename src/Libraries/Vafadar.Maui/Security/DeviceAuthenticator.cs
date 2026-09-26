namespace Vafadar.Maui.Security;

/// <summary>Outcome of a device authentication.</summary>
public enum AuthenticationOutcome
{
    /// <summary>The user proved their identity.</summary>
    Success,

    /// <summary>The user cancelled or authentication failed.</summary>
    Failed,

    /// <summary>The device has no secure lock or biometric set up, or the platform is not supported.</summary>
    NotAvailable,
}

/// <summary>
/// Asks the operating system to confirm the device owner: biometrics with the device PIN, pattern or password as
/// fallback. The app never stores its own PIN, so the lock is as strong as the device lock and has the device's
/// recovery path (SEC-01).
/// </summary>
public interface IDeviceAuthenticator
{
    /// <summary>Returns whether the device can authenticate the owner.</summary>
    Task<bool> IsAvailableAsync();

    /// <summary>Asks the user to authenticate; <paramref name="reason"/> is shown in the system dialog.</summary>
    Task<AuthenticationOutcome> AuthenticateAsync(string reason);
}

/// <summary>The platform implementation of <see cref="IDeviceAuthenticator"/>.</summary>
public sealed partial class DeviceAuthenticator : IDeviceAuthenticator
{
#if !ANDROID && !IOS && !WINDOWS
    /// <inheritdoc />
    public Task<bool> IsAvailableAsync() => Task.FromResult(false);

    /// <inheritdoc />
    public Task<AuthenticationOutcome> AuthenticateAsync(string reason) => Task.FromResult(AuthenticationOutcome.NotAvailable);
#endif
}
