using System.Security.Cryptography;
using Vafadar.Backup.Security;

namespace Vafadar.Backup.Tests.Security;

/// <summary>Checks authenticated device envelopes independently of platform key persistence.</summary>
public sealed class DeviceSafetyEncryptionTests
{
    [Fact]
    public void Round_trip_uses_fresh_nonce_without_mutating_caller_buffers()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var identity = RandomNumberGenerator.GetBytes(48);
        var plaintext = "fictitious financial content"u8.ToArray();
        var first = DeviceSafetyEncryption.Protect(plaintext, key, identity);
        var second = DeviceSafetyEncryption.Protect(plaintext, key, identity);
        Assert.NotEqual(first, second);
        Assert.Equal(identity, DeviceSafetyEncryption.ReadIdentity(first));
        Assert.Equal(plaintext, DeviceSafetyEncryption.Open(first, key));
        Assert.Equal("fictitious financial content"u8.ToArray(), plaintext);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(51)]
    [InlineData(52)]
    [InlineData(63)]
    [InlineData(64)]
    [InlineData(79)]
    [InlineData(80)]
    public void Modified_reference_nonce_tag_or_content_never_releases_plaintext(int offset)
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var envelope = DeviceSafetyEncryption.Protect("content"u8, key, new byte[48]);
        envelope[offset] ^= 1;
        Assert.ThrowsAny<CryptographicException>(() => DeviceSafetyEncryption.Open(envelope, key));
    }

    [Fact]
    public void Missing_content_and_wrong_key_are_rejected()
    {
        Assert.Throws<BackupException>(() => DeviceSafetyEncryption.ReadIdentity("VBS1"u8));
        var envelope = DeviceSafetyEncryption.Protect("content"u8, new byte[32], new byte[48]);
        Assert.ThrowsAny<CryptographicException>(() => DeviceSafetyEncryption.Open(envelope, RandomNumberGenerator.GetBytes(32)));
    }
}
