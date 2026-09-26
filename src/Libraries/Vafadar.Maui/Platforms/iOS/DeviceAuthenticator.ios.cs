using Foundation;
using LocalAuthentication;

namespace Vafadar.Maui.Security;

/// <summary>iOS: Face ID or Touch ID with the device passcode (<see cref="LAPolicy.DeviceOwnerAuthentication"/>).</summary>
public sealed partial class DeviceAuthenticator
{
    /// <inheritdoc />
    public Task<bool> IsAvailableAsync()
    {
        using var context = new LAContext();
        return Task.FromResult(context.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthentication, out NSError _));
    }

    /// <inheritdoc />
    public async Task<AuthenticationOutcome> AuthenticateAsync(string reason)
    {
        using var context = new LAContext();
        if (!context.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthentication, out NSError _))
        {
            return AuthenticationOutcome.NotAvailable;
        }

        var (success, _) = await context.EvaluatePolicyAsync(LAPolicy.DeviceOwnerAuthentication, reason);
        return success ? AuthenticationOutcome.Success : AuthenticationOutcome.Failed;
    }
}
