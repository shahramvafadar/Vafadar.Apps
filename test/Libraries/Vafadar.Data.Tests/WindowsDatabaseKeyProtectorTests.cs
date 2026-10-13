using System.Runtime.Versioning;
using System.Security.Cryptography;

namespace Vafadar.Data.Tests;

[SupportedOSPlatform("windows")]
public sealed class WindowsDatabaseKeyProtectorTests
{
    public static bool IsWindows => OperatingSystem.IsWindows();
    private static DatabaseKeyBinding Binding() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

    [Fact(Skip = "Windows DPAPI requires Windows.", SkipUnless = nameof(IsWindows)), Trait("AT", "AT-191")]
    public void Current_user_envelope_round_trips_without_changing_caller_key()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var binding = Binding();
        var envelope = WindowsDatabaseKeyProtector.Protect(key, binding);
        var restored = WindowsDatabaseKeyProtector.Unprotect(envelope, binding);
        try { Assert.Equal(key, restored); Assert.NotEqual(key, envelope); Assert.Equal(32, key.Length); }
        finally { CryptographicOperations.ZeroMemory(key); CryptographicOperations.ZeroMemory(restored); }
    }

    [Theory(Skip = "Windows DPAPI requires Windows.", SkipUnless = nameof(IsWindows))]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void Changed_profile_installation_or_generation_is_refused(int changed)
    {
        var binding = Binding(); var key = RandomNumberGenerator.GetBytes(32);
        try
        {
            var envelope = WindowsDatabaseKeyProtector.Protect(key, binding);
            var other = new DatabaseKeyBinding(changed == 0 ? Guid.NewGuid() : binding.ProfileId,
                changed == 1 ? Guid.NewGuid() : binding.InstallationId,
                changed == 2 ? Guid.NewGuid() : binding.GenerationId);
            Assert.Throws<CryptographicException>(() => WindowsDatabaseKeyProtector.Unprotect(envelope, other));
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    [Fact(Skip = "Windows DPAPI requires Windows.", SkipUnless = nameof(IsWindows))]
    public void Modified_envelope_is_refused_without_replacement_key()
    {
        var key = RandomNumberGenerator.GetBytes(32); var binding = Binding();
        try
        {
            var envelope = WindowsDatabaseKeyProtector.Protect(key, binding);
            envelope[^1] ^= 1;
            Assert.Throws<CryptographicException>(() => WindowsDatabaseKeyProtector.Unprotect(envelope, binding));
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    [Theory(Skip = "Windows DPAPI requires Windows.", SkipUnless = nameof(IsWindows))]
    [InlineData(0)] [InlineData(4097)]
    public void Missing_or_oversized_envelope_is_refused(int length) =>
        Assert.Throws<CryptographicException>(() => WindowsDatabaseKeyProtector.Unprotect(new byte[length], Binding()));

    [Fact(Skip = "Windows DPAPI requires Windows.", SkipUnless = nameof(IsWindows))]
    public void Native_valid_blob_with_wrong_key_length_is_refused()
    {
        var binding = Binding();
        var invalid = ProtectedData.Protect(new byte[31], binding.GetAuthenticatedContext(), DataProtectionScope.CurrentUser);
        Assert.Throws<CryptographicException>(() => WindowsDatabaseKeyProtector.Unprotect(invalid, binding));
        Assert.Throws<ArgumentException>(() => WindowsDatabaseKeyProtector.Protect(new byte[31], binding));
    }
}
