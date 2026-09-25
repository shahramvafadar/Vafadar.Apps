using System.Text;
using Vafadar.Backup.Security;

namespace Vafadar.Backup.Tests.Security;

public sealed class BackupEncryptionTests
{
    // A low iteration count keeps the tests fast; the format stores the count, so decryption is unaffected.
    private const int TestIterations = 1_000;

    private static readonly byte[] Plaintext = Encoding.UTF8.GetBytes("Personal finance data: ۱۲۳ تومان");

    [Fact]
    public void Round_trips_with_the_default_iteration_count()
    {
        var encrypted = BackupEncryption.Encrypt(Plaintext, "correct horse");

        Assert.True(BackupEncryption.IsEncrypted(encrypted));
        Assert.Equal(Plaintext, BackupEncryption.Decrypt(encrypted, "correct horse"));
    }

    [Fact]
    public void Encrypting_twice_produces_different_output()
    {
        var first = BackupEncryption.Encrypt(Plaintext, "pw", TestIterations);
        var second = BackupEncryption.Encrypt(Plaintext, "pw", TestIterations);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Wrong_password_is_rejected()
    {
        var encrypted = BackupEncryption.Encrypt(Plaintext, "right", TestIterations);

        var error = Assert.Throws<BackupException>(() => BackupEncryption.Decrypt(encrypted, "wrong"));
        Assert.Equal(BackupError.InvalidPasswordOrCorrupted, error.Error);
    }

    [Theory]
    [InlineData(5)]   // PBKDF2 iteration count (header)
    [InlineData(10)]  // salt (header)
    [InlineData(30)]  // nonce (header)
    [InlineData(40)]  // tag
    [InlineData(60)]  // ciphertext
    public void Any_modification_is_detected(int index)
    {
        var encrypted = BackupEncryption.Encrypt(Plaintext, "pw", TestIterations);
        encrypted[index] ^= 0x01;

        var error = Assert.Throws<BackupException>(() => BackupEncryption.Decrypt(encrypted, "pw"));
        Assert.True(error.Error is BackupError.InvalidPasswordOrCorrupted or BackupError.InvalidFormat);
    }

    [Fact]
    public void Unencrypted_data_is_not_detected_as_encrypted()
    {
        Assert.False(BackupEncryption.IsEncrypted("PK\u0003\u0004 zip"u8));
        Assert.Throws<BackupException>(() => BackupEncryption.Decrypt("PK\u0003\u0004 zip"u8, "pw"));
    }

    [Fact]
    public void Persian_passwords_typed_on_different_keyboards_decrypt_the_same_backup()
    {
        // Persian Yeh/Keheh and Persian digits vs. Arabic Yeh/Kaf and ASCII digits.
        const string persianKeyboard = "کلیدی۱۲۳";
        const string arabicKeyboard = "كلیدي123";

        var encrypted = BackupEncryption.Encrypt(Plaintext, persianKeyboard, TestIterations);

        Assert.Equal(Plaintext, BackupEncryption.Decrypt(encrypted, arabicKeyboard));
    }
}
