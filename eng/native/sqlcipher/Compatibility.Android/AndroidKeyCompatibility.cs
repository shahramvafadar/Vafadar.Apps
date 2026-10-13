using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using Vafadar.Data;
using Vafadar.Zanance.App.Security;

namespace Vafadar.SqlCipher.Compatibility.Android;

/// <summary>Exercises original Android key code in this independent package with fictitious keys/files only.</summary>
internal static class AndroidKeyCompatibility
{
    /// <summary>Creates/reopens a protected encrypted fixture or checks native failure behavior, never main app data.</summary>
    internal static async Task<string> RunAsync(string ownedRoot, string phase)
    {
        var root = Path.Combine(ownedRoot, "key-fixture");
        var contextFile = Path.Combine(root, "context.txt");
        var envelopeFile = Path.Combine(root, "envelope.bin");
        var dataFile = Path.Combine(root, "fictitious.db");
        DatabaseKeyBinding binding;
        if (phase == "keys-create")
        {
            if (Directory.Exists(root)) throw new InvalidOperationException("Key fixture must be fresh.");
            Directory.CreateDirectory(root);
            binding = new DatabaseKeyBinding(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            await File.WriteAllTextAsync(contextFile, string.Join("\n", binding.ProfileId.ToString("N"),
                binding.InstallationId.ToString("N"), binding.GenerationId.ToString("N")));
            AndroidDatabaseKeyProtector.PrepareWrappingKey(binding.InstallationId);
        }
        else
        {
            var ids = (await File.ReadAllTextAsync(contextFile)).Split('\n');
            binding = new DatabaseKeyBinding(Guid.ParseExact(ids[0], "N"), Guid.ParseExact(ids[1], "N"), Guid.ParseExact(ids[2], "N"));
        }
        var key = phase == "keys-create" ? RandomNumberGenerator.GetBytes(32)
            : AndroidDatabaseKeyProtector.Unprotect(await File.ReadAllBytesAsync(envelopeFile), binding);
        try
        {
            if (phase == "keys-create") await File.WriteAllBytesAsync(envelopeFile, AndroidDatabaseKeyProtector.Protect(key, binding));
            var envelope = await File.ReadAllBytesAsync(envelopeFile);
            var restored = AndroidDatabaseKeyProtector.Unprotect(envelope, binding);
            try { if (!CryptographicOperations.FixedTimeEquals(key, restored)) throw new InvalidOperationException("Native key changed."); }
            finally { CryptographicOperations.ZeroMemory(restored); }
            if (phase == "keys-negative") return CheckRefusals(key, envelope, binding);
            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlcipher());
            var options = new SqliteConnectionStringBuilder { DataSource = dataFile, Password = Convert.ToBase64String(key),
                Pooling = false, Mode = phase == "keys-create" ? SqliteOpenMode.ReadWriteCreate : SqliteOpenMode.ReadOnly };
            await using var connection = new SqliteConnection(options.ToString());
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA cipher_version";
            if (!(Convert.ToString(await command.ExecuteScalarAsync()) ?? "").StartsWith("4.19.0", StringComparison.Ordinal))
                throw new InvalidOperationException("Native key check requires reviewed cipher.");
            if (phase == "keys-create")
            {
                command.CommandText = "CREATE TABLE packet(amount INTEGER); INSERT INTO packet VALUES(-9223372036854775808)";
                await command.ExecuteNonQueryAsync();
            }
            command.CommandText = "SELECT amount FROM packet";
            if ((long)(await command.ExecuteScalarAsync())! != long.MinValue) throw new InvalidOperationException("Encrypted native packet changed.");
            var level = AndroidDatabaseKeyProtector.ReadSecurityLevel(binding.InstallationId);
            return "{\"AndroidKeyProtected\":true,\"EncryptedNativeDatabaseReopened\":true,\"Phase\":\"" + phase +
                "\",\"ReportedSecurityLevel\":" + (level?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null") +
                ",\"NotPhysicalHardwareAcceptance\":true}";
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    private static string CheckRefusals(byte[] key, byte[] envelope, DatabaseKeyBinding binding)
    {
        var count = 0;
        void Refused(Action action)
        {
            try { action(); throw new InvalidOperationException("Unsafe native envelope accepted."); }
            catch (CryptographicException) { count++; }
        }
        void Reject(byte[] packet, DatabaseKeyBinding context)
        {
            Refused(() => { var unexpected = AndroidDatabaseKeyProtector.Unprotect(packet, context); CryptographicOperations.ZeroMemory(unexpected); });
        }
        Reject(envelope, new DatabaseKeyBinding(Guid.NewGuid(), binding.InstallationId, binding.GenerationId));
        Reject(envelope, new DatabaseKeyBinding(binding.ProfileId, binding.InstallationId, Guid.NewGuid()));
        var wrongInstallation = Guid.NewGuid();
        Reject(envelope, new DatabaseKeyBinding(binding.ProfileId, wrongInstallation, binding.GenerationId));
        using var store = Java.Security.KeyStore.GetInstance("AndroidKeyStore")!;
        store.Load(null);
        if (store.ContainsAlias(AndroidDatabaseKeyProtector.GetWrappingKeyAlias(wrongInstallation)))
            throw new InvalidOperationException("Unwrap created a replacement alias.");
        foreach (var offset in new[] { 4, 5, 17, envelope.Length - 1 })
        { var changed = envelope.ToArray(); changed[offset] ^= 1; Reject(changed, binding); }
        foreach (var length in new[] { 0, 64, 66 }) Reject(new byte[length], binding);
        foreach (var length in new[] { 0, 31, 33 })
        {
            try { AndroidDatabaseKeyProtector.Protect(new byte[length], binding); throw new InvalidOperationException("Invalid DEK size accepted."); }
            catch (ArgumentException) { count++; }
        }
        var otherEnvelope = AndroidDatabaseKeyProtector.Protect(key, binding);
        if (otherEnvelope.AsSpan().SequenceEqual(envelope)) throw new InvalidOperationException("Native GCM nonce repeated.");
        count++;
        var missing = new DatabaseKeyBinding(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        AndroidDatabaseKeyProtector.PrepareWrappingKey(missing.InstallationId);
        var missingEnvelope = AndroidDatabaseKeyProtector.Protect(key, missing);
        // Delete only this proof's independently generated alias in its distinct package.
        store.DeleteEntry(AndroidDatabaseKeyProtector.GetWrappingKeyAlias(missing.InstallationId));
        Reject(missingEnvelope, missing);
        if (store.ContainsAlias(AndroidDatabaseKeyProtector.GetWrappingKeyAlias(missing.InstallationId)))
            throw new InvalidOperationException("Missing wrapping key was silently regenerated.");
        return "{\"AndroidKeyBehaviorCases\":" + count + ",\"NoReplacementKey\":true}";
    }
}
