using AndroidX.Biometric;
using AndroidX.Core.Content;
using AndroidX.Fragment.App;

namespace Vafadar.Maui.Security;

/// <summary>Android: <see cref="BiometricPrompt"/> with biometrics or the device credential.</summary>
public sealed partial class DeviceAuthenticator
{
    private const int Authenticators = BiometricManager.Authenticators.BiometricWeak | BiometricManager.Authenticators.DeviceCredential;

    /// <inheritdoc />
    public Task<bool> IsAvailableAsync()
    {
        var context = Platform.CurrentActivity ?? Android.App.Application.Context;
        return Task.FromResult(BiometricManager.From(context).CanAuthenticate(Authenticators) == BiometricManager.BiometricSuccess);
    }

    /// <inheritdoc />
    public async Task<AuthenticationOutcome> AuthenticateAsync(string reason)
    {
        if (Platform.CurrentActivity is not FragmentActivity activity || !await IsAvailableAsync())
        {
            return AuthenticationOutcome.NotAvailable;
        }

        var result = new TaskCompletionSource<AuthenticationOutcome>();
        var info = new BiometricPrompt.PromptInfo.Builder()
            .SetTitle(reason)
            .SetAllowedAuthenticators(Authenticators)
            .Build();

        activity.RunOnUiThread(() =>
        {
            var executor = ContextCompat.GetMainExecutor(activity) ?? throw new InvalidOperationException("No main executor.");
            var prompt = new BiometricPrompt(activity, executor, new Callback(result));
            prompt.Authenticate(info);
        });

        return await result.Task;
    }

    private sealed class Callback(TaskCompletionSource<AuthenticationOutcome> result) : BiometricPrompt.AuthenticationCallback
    {
        public override void OnAuthenticationSucceeded(BiometricPrompt.AuthenticationResult authResult) =>
            result.TrySetResult(AuthenticationOutcome.Success);

        // A single wrong finger keeps the prompt open; only an error or cancel ends it.
        public override void OnAuthenticationError(int errorCode, Java.Lang.ICharSequence errString) =>
            result.TrySetResult(AuthenticationOutcome.Failed);
    }
}
