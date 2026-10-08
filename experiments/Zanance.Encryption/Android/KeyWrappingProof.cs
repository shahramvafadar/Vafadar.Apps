using System.Security.Cryptography;
using System.Text;
using Android.Security.Keystore;
using Javax.Crypto;
using Javax.Crypto.Spec;
using Zanance.Encryption.Probe;

namespace Zanance.Encryption.Android;

/// <summary>Device-key wrapping of an ephemeral fictitious profile, confined to the proof application's own identity.</summary>
internal static class KeyWrappingProof
{
    private const string Alias = "zanance-sec01-fictitious-wrap-v1";

    /// <summary>Returns evidence only; a subsequent process start verifies the existing wrapped profile.</summary>
    internal static string Run(string root)
    {
        using var store = Java.Security.KeyStore.GetInstance("AndroidKeyStore")!;
        store.Load(null);
        if (!store.ContainsAlias(Alias))
        {
            using var generator = KeyGenerator.GetInstance("AES", "AndroidKeyStore")!;
            using var spec = new KeyGenParameterSpec.Builder(Alias, KeyStorePurpose.Encrypt | KeyStorePurpose.Decrypt)
                .SetBlockModes(KeyProperties.BlockModeGcm)
                .SetEncryptionPaddings(KeyProperties.EncryptionPaddingNone)
                .SetRandomizedEncryptionRequired(true).Build();
            generator.Init(spec);
            using var generated = generator.GenerateKey();
        }
        using var entry = (Java.Security.KeyStore.SecretKeyEntry)store.GetEntry(Alias, null)!;
        using var osKey = entry.SecretKey ?? throw new InvalidOperationException("Proof device key was missing.");
        var envelopePath = Path.Combine(root, "wrapped-fictitious-profile.bin");
        var dbPath = Path.Combine(root, "wrapped-profile.sqlite");
        var existing = File.Exists(envelopePath);
        byte[] dataKey;
        if (!existing)
        {
            dataKey = RandomNumberGenerator.GetBytes(32);
            using var encrypt = Cipher.GetInstance("AES/GCM/NoPadding")!;
            encrypt.Init(Javax.Crypto.CipherMode.EncryptMode, osKey);
            encrypt.UpdateAAD(Encoding.UTF8.GetBytes("v1|fictitious-profile|generation-1"));
            var ciphertext = encrypt.DoFinal(dataKey)!;
            File.WriteAllBytes(envelopePath, [.. encrypt.GetIV()!, .. ciphertext]);
            using var db = EncryptionProbe.Open(dbPath, dataKey);
            using var create = db.CreateCommand();
            create.CommandText = "CREATE TABLE proof(value INTEGER); INSERT INTO proof VALUES(12345);";
            create.ExecuteNonQuery();
        }
        else dataKey = Unwrap(osKey, File.ReadAllBytes(envelopePath));
        try
        {
            using var db = EncryptionProbe.Open(dbPath, dataKey);
            using var read = db.CreateCommand(); read.CommandText = "SELECT value FROM proof;";
            if (Convert.ToInt64(read.ExecuteScalar()) != 12345) throw new InvalidOperationException("Wrapped profile changed.");
            var tampered = File.ReadAllBytes(envelopePath); tampered[^1] ^= 1;
            try
            {
                var unexpected = Unwrap(osKey, tampered);
                CryptographicOperations.ZeroMemory(unexpected);
                throw new InvalidOperationException("Envelope tampering accepted.");
            }
            catch (AEADBadTagException) { }
            return existing ? "PASS: existing device-wrapped profile reopened; tampering rejected."
                : "PASS: new device-wrapped profile created; tampering rejected.";
        }
        finally { CryptographicOperations.ZeroMemory(dataKey); }
    }

    private static byte[] Unwrap(ISecretKey key, byte[] envelope)
    {
        using var cipher = Cipher.GetInstance("AES/GCM/NoPadding")!;
        using var parameters = new GCMParameterSpec(128, envelope[..12]);
        cipher.Init(Javax.Crypto.CipherMode.DecryptMode, key, parameters);
        cipher.UpdateAAD(Encoding.UTF8.GetBytes("v1|fictitious-profile|generation-1"));
        return cipher.DoFinal(envelope[12..])!;
    }
}
