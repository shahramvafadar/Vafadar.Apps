using Windows.Security.Credentials.UI;

namespace Vafadar.Maui.Security;

/// <summary>Windows: Windows Hello (face, fingerprint or PIN).</summary>
public sealed partial class DeviceAuthenticator
{
    /// <inheritdoc />
    public async Task<bool> IsAvailableAsync() =>
        await UserConsentVerifier.CheckAvailabilityAsync() == UserConsentVerifierAvailability.Available;

    /// <inheritdoc />
    public async Task<AuthenticationOutcome> AuthenticateAsync(string reason)
    {
        if (!await IsAvailableAsync())
        {
            return AuthenticationOutcome.NotAvailable;
        }

        var result = await UserConsentVerifier.RequestVerificationAsync(reason);
        return result == UserConsentVerificationResult.Verified ? AuthenticationOutcome.Success : AuthenticationOutcome.Failed;
    }
}
