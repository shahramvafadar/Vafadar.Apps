namespace Vafadar.Authentication.Tests;

public sealed class CloudSignInMatrixTests
{
    private const string Microsoft = "00000000-0000-0000-0000-000000000001";
    private const string GoogleAndroid = "1-android.apps.googleusercontent.com";
    private const string GoogleIos = "1-ios.apps.googleusercontent.com";
    private const string GoogleWindows = "1-desktop.apps.googleusercontent.com";

    [Theory]
    [InlineData(SignInPlatform.Android)]
    [InlineData(SignInPlatform.Ios)]
    [InlineData(SignInPlatform.Windows)]
    public void Both_providers_are_offered_on_android_ios_and_windows(SignInPlatform platform)
    {
        Assert.True(Available(ExternalIdentityProvider.Microsoft, platform));
        Assert.True(Available(ExternalIdentityProvider.Google, platform));
    }

    [Fact]
    public void Other_platforms_offer_nothing()
    {
        Assert.False(Available(ExternalIdentityProvider.Microsoft, SignInPlatform.Other));
        Assert.False(Available(ExternalIdentityProvider.Google, SignInPlatform.Other));
    }

    [Theory]
    [InlineData(SignInPlatform.Android)]
    [InlineData(SignInPlatform.Ios)]
    [InlineData(SignInPlatform.Windows)]
    public void A_build_without_clients_is_offline(SignInPlatform platform)
    {
        Assert.False(CloudSignInMatrix.IsAvailable(ExternalIdentityProvider.Microsoft, platform, "", null, null, null));
        Assert.False(CloudSignInMatrix.IsAvailable(ExternalIdentityProvider.Google, platform, "", null, null, null));
    }

    [Fact]
    public void Each_platform_uses_only_its_own_google_client()
    {
        // Only the Android client: Google on Android only.
        Assert.True(CloudSignInMatrix.IsAvailable(ExternalIdentityProvider.Google, SignInPlatform.Android, null, GoogleAndroid, null, null));
        Assert.False(CloudSignInMatrix.IsAvailable(ExternalIdentityProvider.Google, SignInPlatform.Ios, null, GoogleAndroid, null, null));
        Assert.False(CloudSignInMatrix.IsAvailable(ExternalIdentityProvider.Google, SignInPlatform.Windows, null, GoogleAndroid, null, null));

        // Only the desktop client: Google on Windows only.
        Assert.True(CloudSignInMatrix.IsAvailable(ExternalIdentityProvider.Google, SignInPlatform.Windows, null, null, null, GoogleWindows));
        Assert.False(CloudSignInMatrix.IsAvailable(ExternalIdentityProvider.Google, SignInPlatform.Ios, null, null, null, GoogleWindows));

        Assert.Equal(GoogleIos, CloudSignInMatrix.GoogleClientId(SignInPlatform.Ios, GoogleAndroid, GoogleIos, GoogleWindows));
    }

    [Theory]
    [InlineData(SignInPlatform.Ios)]
    [InlineData(SignInPlatform.Windows)]
    public void A_malformed_google_client_counts_as_not_configured_where_the_redirect_needs_it(SignInPlatform platform)
    {
        Assert.False(CloudSignInMatrix.IsAvailable(ExternalIdentityProvider.Google, platform, null, null, "not-a-client-id", "not-a-client-id"));
    }

    private static bool Available(ExternalIdentityProvider provider, SignInPlatform platform) =>
        CloudSignInMatrix.IsAvailable(provider, platform, Microsoft, GoogleAndroid, GoogleIos, GoogleWindows);
}
