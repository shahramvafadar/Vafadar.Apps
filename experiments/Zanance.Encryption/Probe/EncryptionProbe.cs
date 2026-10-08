using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;

namespace Zanance.Encryption.Probe;

/// <summary>Fictitious-only evidence from a single run; contains no connection string or key.</summary>
public sealed record ProbeResult(string Platform, string Runtime, string SqliteVersion, string CipherVersion,
    int Rows, long PlainWriteMs, long EncryptedWriteMs, long ReopenMs, long DatabaseBytes, long WalBytes, long ShmBytes,
    bool PlainControlFound, bool DatabaseMarkerAbsent, bool WalMarkerAbsent, bool ShmMarkerAbsent,
    bool WrongKeyRejected, bool NoKeyRejected, bool CorruptionRejected, bool RotationPassed, bool ExportPassed)
{
    /// <summary>Serializes a bounded evidence report without reflection in a trimmed build.</summary>
    public string ToJson() => JsonSerializer.Serialize(this, ProbeJsonContext.Default.ProbeResult);
}

[JsonSerializable(typeof(ProbeResult))]
internal sealed partial class ProbeJsonContext : JsonSerializerContext;

/// <summary>Exercises encryption in a new fictitious directory; never references an app or accepts a database path.</summary>
public static class EncryptionProbe
{
    /// <summary>A deliberately detectable fictitious text/blob marker, not a credential.</summary>
    public const string Marker = "FICTITIOUS_ZANANCE_RECEIPT_TOTAL_EUR_12345_SEC01";
    /// <summary>Number of fictitious rows including attachment-like BLOBs.</summary>
    public const int RowCount = 500;
    private static readonly byte[] MarkerBytes = Encoding.UTF8.GetBytes(Marker);

    /// <summary>Runs against newly created files below a caller-owned experiment root only.</summary>
    public static ProbeResult Run(string experimentRoot)
    {
        SQLitePCL.Batteries_V2.Init();
        var run = Path.Combine(experimentRoot, "run-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(run);
        var plainPath = Path.Combine(run, "control.sqlite");
        var encryptedPath = Path.Combine(run, "encrypted.sqlite");
        var key = RandomNumberGenerator.GetBytes(32);
        var rotated = RandomNumberGenerator.GetBytes(32);
        var wrong = RandomNumberGenerator.GetBytes(32);
        try
        {
            var clock = Stopwatch.StartNew();
            using (var plain = Open(plainPath, null)) WriteRows(plain);
            var plainMs = clock.ElapsedMilliseconds;
            var plainFound = ContainsMarker(plainPath);
            Require(plainFound, "Plaintext control must expose the marker.");

            clock.Restart();
            long walBytes;
            long shmBytes;
            string sqlite;
            string cipher;
            using (var encrypted = Open(encryptedPath, key))
            {
                sqlite = Scalar(encrypted, "SELECT sqlite_version();").ToString()!;
                cipher = Scalar(encrypted, "PRAGMA cipher_version;").ToString()!;
                Require(!string.IsNullOrWhiteSpace(cipher), "Cipher provider must be active.");
                Require(Scalar(encrypted, "PRAGMA journal_mode=WAL;").ToString() == "wal", "WAL must be active.");
                Execute(encrypted, "PRAGMA wal_autocheckpoint=0;");
                WriteRows(encrypted);
                var walPath = encryptedPath + "-wal";
                var shmPath = encryptedPath + "-shm";
                walBytes = new FileInfo(walPath).Length;
                shmBytes = new FileInfo(shmPath).Length;
                Require(walBytes > 0 && shmBytes > 0, "Sidecar evidence must be present while connection is open.");
                Require(!ContainsMarker(walPath), "WAL page data exposed a marker.");
                // SHM is an unencrypted wal-index, not encrypted data pages. Test for leakage, not ciphertext.
                Require(!ContainsMarker(shmPath), "SHM exposed a marker.");
                Require(Scalar(encrypted, "PRAGMA integrity_check;").ToString() == "ok", "Integrity check failed.");
                Require(ReadCount(encrypted) == RowCount, "Rows must be intact.");
                Execute(encrypted, "PRAGMA wal_checkpoint(TRUNCATE);");
            }
            var encryptedMs = clock.ElapsedMilliseconds;
            Require(!ContainsMarker(encryptedPath), "Database exposed a marker.");
            var header = File.ReadAllBytes(encryptedPath).AsSpan(0, 16);
            Require(!header.SequenceEqual(Encoding.ASCII.GetBytes("SQLite format 3\0")), "Encrypted header must differ.");
            Require(Rejects(encryptedPath, wrong), "Wrong key accepted.");
            Require(Rejects(encryptedPath, null), "Missing key accepted.");
            clock.Restart();
            using (var reopened = Open(encryptedPath, key))
            {
                Require(ReadCount(reopened) == RowCount, "Reopen changed row count.");
                Require(Convert.ToInt64(Scalar(reopened, "SELECT sum(amount) FROM proof;")) == 12345L * RowCount, "Amounts changed.");
                Require(Scalar(reopened, "SELECT note FROM proof LIMIT 1;").ToString() == Marker, "Text changed.");
                Require(Convert.ToInt32(Scalar(reopened, "SELECT length(attachment) FROM proof LIMIT 1;")) == 2048, "Attachment changed.");
            }
            var reopenMs = clock.ElapsedMilliseconds;
            var damaged = Path.Combine(run, "damaged.sqlite");
            var damagedBytes = File.ReadAllBytes(encryptedPath);
            damagedBytes[100] ^= 1;
            File.WriteAllBytes(damaged, damagedBytes);
            Require(Rejects(damaged, key), "Tampered first-page ciphertext accepted.");

            using (var rotate = Open(encryptedPath, key))
            {
                // Ephemeral generated keys never leave this process or enter evidence output.
                Execute(rotate, "PRAGMA rekey=\"" + RawKey(rotated) + "\";");
            }
            Require(Rejects(encryptedPath, key), "Old key accepted after rotation.");
            using (var afterRotation = Open(encryptedPath, rotated)) Require(ReadCount(afterRotation) == RowCount, "Rotation lost rows.");

            var exported = Path.Combine(run, "exported.sqlite");
            using (var source = Open(plainPath, null))
            {
                using var attach = source.CreateCommand();
                attach.CommandText = "ATTACH DATABASE $path AS encrypted KEY $key;";
                attach.Parameters.AddWithValue("$path", exported);
                attach.Parameters.AddWithValue("$key", RawKey(key));
                attach.ExecuteNonQuery();
                Execute(source, "SELECT sqlcipher_export('encrypted');");
                Execute(source, "DETACH DATABASE encrypted;");
            }
            using (var target = Open(exported, key))
            {
                Require(ReadCount(target) == RowCount, "Export lost rows.");
                Require(Scalar(target, "PRAGMA integrity_check;").ToString() == "ok", "Export integrity failed.");
            }
            Require(!ContainsMarker(exported) && Rejects(exported, null), "Export left readable plaintext.");
            return new(Environment.OSVersion.ToString(), Environment.Version.ToString(), sqlite, cipher, RowCount,
                plainMs, encryptedMs, reopenMs, new FileInfo(encryptedPath).Length, walBytes, shmBytes,
                true, true, true, true, true, true, true, true, true);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(rotated);
            CryptographicOperations.ZeroMemory(wrong);
        }
    }

    /// <summary>Opens only a probe path; a null key is used solely as a plaintext negative control.</summary>
    public static SqliteConnection Open(string path, byte[]? key)
    {
        var builder = new SqliteConnectionStringBuilder { DataSource = path, Pooling = false };
        if (key is not null) builder.Password = RawKey(key);
        var connection = new SqliteConnection(builder.ToString());
        try
        {
            connection.Open();
            Execute(connection, "PRAGMA temp_store=MEMORY;");
            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    private static string RawKey(byte[] key) => "x'" + Convert.ToHexString(key) + "'";
    private static void WriteRows(SqliteConnection connection)
    {
        Execute(connection, "CREATE TABLE proof(id INTEGER PRIMARY KEY, amount INTEGER NOT NULL, currency TEXT NOT NULL, note TEXT NOT NULL, attachment BLOB NOT NULL);");
        using var transaction = connection.BeginTransaction();
        using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = "INSERT INTO proof(amount,currency,note,attachment) VALUES(12345,'EUR',$note,$blob);";
        insert.Parameters.AddWithValue("$note", Marker);
        var blob = new byte[2048];
        MarkerBytes.CopyTo(blob, 128);
        insert.Parameters.AddWithValue("$blob", blob);
        for (var row = 0; row < RowCount; row++) insert.ExecuteNonQuery();
        transaction.Commit();
    }

    private static bool ContainsMarker(string path)
    {
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var bytes = new MemoryStream();
        file.CopyTo(bytes);
        var content = bytes.ToArray().AsSpan();
        return content.IndexOf(MarkerBytes) >= 0 || content.IndexOf(Encoding.Unicode.GetBytes(Marker)) >= 0
            || content.IndexOf(Encoding.UTF8.GetBytes("CREATE TABLE proof")) >= 0;
    }

    private static bool Rejects(string path, byte[]? key)
    {
        try { using var connection = Open(path, key); _ = ReadCount(connection); return false; }
        catch (SqliteException exception) when (exception.SqliteErrorCode is 11 or 26) { return true; }
    }

    private static long ReadCount(SqliteConnection connection) => Convert.ToInt64(Scalar(connection, "SELECT count(*) FROM proof;"));
    private static object Scalar(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand(); command.CommandText = sql;
        return command.ExecuteScalar() ?? throw new InvalidOperationException("Probe scalar was missing.");
    }
    private static void Execute(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand(); command.CommandText = sql; command.ExecuteNonQuery();
    }
    private static void Require(bool condition, string safeMessage)
    {
        if (!condition) throw new InvalidOperationException(safeMessage);
    }
}
