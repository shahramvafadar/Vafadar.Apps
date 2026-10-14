using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Vafadar.Core.Settings;

namespace Vafadar.Data;

/// <summary>Constructs a receiver from existing recorded ancestry, including cold pre-selection rollback access.</summary>
/// <remarks>Never creates history, an intent, a missing database or a replacement key. The host retains its
/// drained maintenance owner and supplies real preference persistence and final session publication.</remarks>
public static class SqlCipherRetainedRestore
{
    /// <summary>Binds the complete existing reference to its original namespace and explicit native effects.</summary>
    public static SqlCipherPackageRestoreReceiver Create(DatabaseProfileIdentity identity, string original,
        ProfileRestoreReference reference, IDatabaseGenerationKeyStore keys, IDatabaseGenerationFileDurability durability,
        ISettingsStore settings, IReadOnlyList<string> portableKeys, Action validate,
        Func<SqliteConnection, CancellationToken, Task> upgrade, Func<CancellationToken, Task> publish,
        Func<CancellationToken, Task> persistPreferences, int maximumBytes, string version)
    {
        ArgumentNullException.ThrowIfNull(identity); ArgumentNullException.ThrowIfNull(reference);
        ArgumentNullException.ThrowIfNull(keys); ArgumentNullException.ThrowIfNull(durability);
        ArgumentNullException.ThrowIfNull(settings); ArgumentNullException.ThrowIfNull(portableKeys);
        ArgumentNullException.ThrowIfNull(validate); ArgumentNullException.ThrowIfNull(upgrade);
        ArgumentNullException.ThrowIfNull(publish); ArgumentNullException.ThrowIfNull(persistPreferences);
        ArgumentException.ThrowIfNullOrWhiteSpace(original); ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumBytes);
        if (!Path.IsPathFullyQualified(original) || Path.GetExtension(original) != ".db" || reference.Sequence < 1
            || reference.OriginalGeneration == Guid.Empty || reference.CandidateGeneration == Guid.Empty
            || reference.CandidateGeneration == reference.OriginalGeneration)
            throw new ArgumentException("Retained restore requires an absolute original and a later distinct recorded generation.");
        original = Path.GetFullPath(original);
        var candidate = Candidate(original, reference.CandidateGeneration);
        var binding = new DatabaseKeyBinding(identity.ProfileId, identity.InstallationId, reference.CandidateGeneration);
        var captured = new DatabaseGenerationCheckpoint(identity.ProfileId, reference.OriginalGeneration, reference.CandidateGeneration);
        var history = new SqliteProfileRestoreHistory(original + ".restore-history.sqlite", identity.InitialCandidateKey, validate);
        return new(captured, new SqliteDatabaseGenerationJournal(candidate + ".restore.sqlite", captured), candidate,
            binding, keys, durability, settings, portableKeys.ToArray(), OpenPreviousAsync, upgrade, publish, validate,
            maximumBytes, version, persistPreferences, history);

        async Task<SqliteConnection> OpenPreviousAsync(CancellationToken ct)
        {
            void Check() { ct.ThrowIfCancellationRequested(); validate(); }
            Check(); await history.RequireAsync(captured, ct).ConfigureAwait(false);
            var rows = await history.ReadAsync(ct).ConfigureAwait(false); Check();
            var prior = rows[^2];
            if (rows[^1].Sequence != reference.Sequence || prior.CandidateGeneration != reference.OriginalGeneration || !prior.Selected || rows[^1].Selected)
                throw new IOException("Previous generation differs from the recorded selected ancestry.");
            var path = Candidate(original, prior.CandidateGeneration);
            if (!File.Exists(path) || (File.GetAttributes(path) & (FileAttributes.ReparsePoint | FileAttributes.Directory)) != 0)
                throw new IOException("Recorded previous ciphertext is missing or redirected.");
            var priorBinding = new DatabaseKeyBinding(identity.ProfileId, identity.InstallationId, prior.CandidateGeneration);
            var key = await keys.ReadAsync(priorBinding, ct).ConfigureAwait(false);
            SqliteConnection? connection = null;
            try
            {
                Check(); if (key.Length != 32) throw new CryptographicException("Previous generation key is invalid.");
                connection = new(new SqliteConnectionStringBuilder { DataSource = path, Password = Convert.ToBase64String(key),
                    Mode = SqliteOpenMode.ReadWrite, Pooling = false, ForeignKeys = true }.ToString());
                await connection.OpenAsync(ct).ConfigureAwait(false); Check();
                using var check = connection.CreateCommand(); check.CommandText = "PRAGMA cipher_version";
                if ((Convert.ToString(await check.ExecuteScalarAsync(ct).ConfigureAwait(false)) ?? "").Split(' ')[0] != version)
                    throw new IOException("Previous generation requires the reviewed native cipher.");
                check.CommandText = "PRAGMA cipher_integrity_check";
                await using (var authentication = await check.ExecuteReaderAsync(ct).ConfigureAwait(false))
                    if (await authentication.ReadAsync(ct).ConfigureAwait(false)) throw new CryptographicException("Previous cipher authentication failed.");
                check.CommandText = "PRAGMA integrity_check";
                if (!string.Equals(await check.ExecuteScalarAsync(ct).ConfigureAwait(false) as string, "ok", StringComparison.Ordinal))
                    throw new IOException("Previous generation integrity failed.");
                Check(); await history.RequireAsync(captured, ct).ConfigureAwait(false); Check();
                ProtectedSqliteAccess.Register(connection, Check);
                return connection;
            }
            catch { if (connection is not null) await connection.DisposeAsync().ConfigureAwait(false); throw; }
            finally { CryptographicOperations.ZeroMemory(key); }
        }
    }

    private static string Candidate(string original, Guid generation) => original + "." + generation.ToString("N") + ".cipher.db";
}
