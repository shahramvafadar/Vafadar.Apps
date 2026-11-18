using System.Globalization;
using System.Security.Cryptography;

namespace Vafadar.Core.Security;

/// <summary>
/// What is kept of a password: never the password, only a value derived from it with a salt of its own and so
/// much work that guessing is slow (PBKDF2 with SHA-256). The kept text names the method, the work, the salt and
/// the derived value, so the work can be raised later without losing what was kept before.
/// </summary>
public static class PasswordHash
{
    /// <summary>The work a new value is derived with: the rounds of PBKDF2.</summary>
    public const int Work = 600_000;

    private const string Method = "pbkdf2-sha256";
    private const int SaltLength = 16;
    private const int KeyLength = 32;

    /// <summary>Returns the text to keep for a password.</summary>
    public static string Derive(string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        var salt = RandomNumberGenerator.GetBytes(SaltLength);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Work, HashAlgorithmName.SHA256, KeyLength);
        return string.Create(CultureInfo.InvariantCulture, $"{Method}${Work}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}");
    }

    /// <summary>
    /// Returns whether a password is the one a kept text was derived from. The comparison takes the same time
    /// whether it matches or not; a text that is no kept value matches nothing.
    /// </summary>
    public static bool Matches(string password, string derived)
    {
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(derived);
        var parts = derived.Split('$');
        if (parts is not [Method, var work, var salt, var key] || !int.TryParse(work, NumberStyles.None, CultureInfo.InvariantCulture, out var rounds) || rounds < 1)
        {
            return false;
        }

        try
        {
            var expected = Convert.FromBase64String(key);
            var given = Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(salt), rounds, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(given, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
