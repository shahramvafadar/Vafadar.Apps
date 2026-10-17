using Microsoft.Data.Sqlite;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text.Json;
using Vafadar.Data;

namespace Vafadar.SqlCipher.Compatibility;

/// <summary>Checks actual protected key persistence across independent Windows processes using fictitious files only.</summary>
[SupportedOSPlatform("windows")]
internal static class WindowsKeyCompatibility
{
    /// <summary>Creates or reopens one independently owned encrypted database and DPAPI envelope.</summary>
    internal static async Task RunAsync(string evidencePath, bool create)
    {
        var folder = Path.GetFullPath(evidencePath);
        var root = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "native-sqlcipher"));
        if (!folder.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Key proof must remain in independently owned native artifacts.");
        if (create)
        {
            if (Directory.Exists(folder)) throw new InvalidOperationException("Use a fresh key proof directory.");
            Directory.CreateDirectory(folder);
        }
        else if (!Directory.Exists(folder)) throw new InvalidOperationException("Owned key proof is unavailable.");
        var contextPath = Path.Combine(folder, "fictitious-context.json");
        var store = new WindowsGenerationKeyStore(folder, () => { });
        var durability = new WindowsGenerationFileDurability(folder);
        DatabaseKeyBinding binding;
        byte[] key;
        if (create)
        {
            binding = new DatabaseKeyBinding(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            await store.PrepareAsync(binding, CancellationToken.None);
            await File.WriteAllTextAsync(contextPath, JsonSerializer.Serialize(new[] { binding.ProfileId, binding.InstallationId, binding.GenerationId }));
            await durability.SynchronizeFileAsync(contextPath, CancellationToken.None);
            key = await store.ReadAsync(binding, CancellationToken.None);
        }
        else
        {
            var identities = JsonSerializer.Deserialize<Guid[]>(await File.ReadAllTextAsync(contextPath))!;
            binding = new DatabaseKeyBinding(identities[0], identities[1], identities[2]);
            // Cold verification only reads the exact existing native envelope; it never calls preparation.
            key = await store.ReadAsync(binding, CancellationToken.None);
        }
        try
        {
            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlcipher());
            var options = new SqliteConnectionStringBuilder { DataSource = Path.Combine(folder, "fictitious-keyed.db"),
                Password = Convert.ToBase64String(key), Pooling = false,
                Mode = create ? SqliteOpenMode.ReadWriteCreate : SqliteOpenMode.ReadOnly };
            // Password API strings cannot be reliably erased; the mutable owned key buffer is cleared below.
            await using var connection = new SqliteConnection(options.ToString());
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA cipher_version";
            if (!((string?)await command.ExecuteScalarAsync())!.StartsWith("4.19.0", StringComparison.Ordinal))
                throw new InvalidOperationException("Protected-key proof requires the reviewed cipher.");
            command.CommandText = "PRAGMA cipher_provider_version";
            if (!((string?)await command.ExecuteScalarAsync())!.Contains("3.5.9", StringComparison.Ordinal))
                throw new InvalidOperationException("Protected-key proof requires the reviewed crypto provider.");
            if (create)
            {
                command.CommandText = "CREATE TABLE packet(amount INTEGER); INSERT INTO packet VALUES(-9223372036854775808)";
                await command.ExecuteNonQueryAsync();
            }
            command.CommandText = "SELECT amount FROM packet";
            if ((long)(await command.ExecuteScalarAsync())! != long.MinValue) throw new InvalidOperationException("Protected key did not preserve encrypted data.");
            if (!create)
            {
                // A later native-engine qualification reuses this original fixture without replacing its evidence.
                var proofPath = Path.Combine(folder, "key-reopen-proof.json");
                if (File.Exists(proofPath)) proofPath = Path.Combine(folder, $"key-reopen-proof-{Guid.NewGuid():N}.json");
                await using var proof = new FileStream(proofPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                await proof.WriteAsync("{\"CurrentUserDpapiAcrossProcesses\":true,\"ActualGenerationKeyStore\":true,\"NativeFileAndDirectoryFlush\":true,\"ColdReadOnly\":true,\"EncryptedNativeDatabaseReopened\":true,\"FictitiousFilesOnly\":true}"u8.ToArray());
            }
            Console.WriteLine(create ? "Fictitious protected key and encrypted database created." : "Protected key and encrypted database reopened in a new process.");
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }
}
