using System.Buffers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace Vafadar.Data;

/// <summary>Owns a bounded read-only portable SQLite image in memory, without a plaintext disk snapshot.</summary>
public sealed class SqliteMemorySnapshot : IAsyncDisposable
{
    private IntPtr _buffer;
    private readonly int _length;
    private bool _disposed;

    private SqliteMemorySnapshot(SqliteConnection connection, IntPtr buffer, int length)
    {
        Connection = connection; _buffer = buffer; _length = length;
    }

    /// <summary>The captured read-only in-memory snapshot, valid until disposal.</summary>
    public SqliteConnection Connection { get; }

    /// <summary>Reads and validates one bounded portable image; source and native buffers remain caller-independent.</summary>
    public static async Task<SqliteMemorySnapshot> OpenAsync(Stream source, int maximumBytes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source); ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumBytes);
        using var content = new MemoryStream();
        var scratch = ArrayPool<byte>.Shared.Rent(81920);
        IntPtr native = IntPtr.Zero;
        SqliteConnection? connection = null;
        try
        {
            int count;
            while ((count = await source.ReadAsync(scratch.AsMemory(), cancellationToken)) != 0)
            {
                if (content.Length + count > maximumBytes) throw new InvalidDataException("Portable image exceeds the allowed size.");
                await content.WriteAsync(scratch.AsMemory(0, count), cancellationToken);
            }
            if (content.Length < 100 || !content.GetBuffer().AsSpan(0, 16).SequenceEqual("SQLite format 3\0"u8))
                throw new InvalidDataException("Portable input is not a plaintext SQLite image.");
            connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = ":memory:", Pooling = false }.ToString());
            await connection.OpenAsync(cancellationToken);
            var length = checked((int)content.Length);
            native = raw.sqlite3_malloc64(length);
            if (native == IntPtr.Zero) throw new OutOfMemoryException("Could not allocate a portable database image.");
            // SQLite deserialize cannot open WAL-mode headers; normalize only the owned in-memory image (upstream sqlite3.h).
            if (content.GetBuffer()[18] == 2 && content.GetBuffer()[19] == 2)
                content.GetBuffer()[18] = content.GetBuffer()[19] = 1;
            Marshal.Copy(content.GetBuffer(), 0, native, length);
            // READONLY without FREEONCLOSE leaves allocation ownership here, including failure paths.
            var result = raw.sqlite3_deserialize(connection.Handle!, "main", native, length, length, 4);
            if (result != raw.SQLITE_OK) throw new InvalidDataException("Portable database image could not be opened.");
            await using var check = connection.CreateCommand(); check.CommandText = "PRAGMA integrity_check";
            if (!string.Equals(await check.ExecuteScalarAsync(cancellationToken) as string, "ok", StringComparison.Ordinal))
                throw new InvalidDataException("Portable database integrity failed.");
            var snapshot = new SqliteMemorySnapshot(connection, native, length);
            connection = null; native = IntPtr.Zero;
            return snapshot;
        }
        finally
        {
            if (connection is not null) await connection.DisposeAsync();
            if (native != IntPtr.Zero) ClearAndFree(native, checked((int)content.Length));
            CryptographicOperations.ZeroMemory(scratch);
            ArrayPool<byte>.Shared.Return(scratch);
            CryptographicOperations.ZeroMemory(content.GetBuffer().AsSpan());
        }
    }

    /// <summary>Exports this immutable snapshot into a new caller-owned encrypted staging file, never an existing database.</summary>
    /// <remarks>The caller verifies and commits the staging generation. An interrupted candidate is never promoted here.</remarks>
    public async Task ExportEncryptedAsync(string candidatePath, string password, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(candidatePath); ArgumentException.ThrowIfNullOrEmpty(password);
        var fullPath = Path.GetFullPath(candidatePath);
        // Reserve one exact new path. Do not overwrite an existing generation or guess ownership of old sidecars.
        foreach (var suffix in new[] { "-wal", "-shm", "-journal" })
            if (File.Exists(fullPath + suffix)) throw new IOException("Encrypted candidate sidecars already exist.");
        await using (var reserve = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            await reserve.FlushAsync(cancellationToken);
        var alias = "encrypted_" + Guid.NewGuid().ToString("N");
        var attached = false;
        try
        {
            await using var attach = Connection.CreateCommand();
            attach.CommandText = $"ATTACH DATABASE $path AS {alias} KEY $key;";
            attach.Parameters.AddWithValue("$path", fullPath); attach.Parameters.AddWithValue("$key", password);
            await attach.ExecuteNonQueryAsync(cancellationToken); attached = true;
            await using var version = Connection.CreateCommand(); version.CommandText = "PRAGMA user_version";
            var userVersion = Convert.ToInt64(await version.ExecuteScalarAsync(cancellationToken));
            version.CommandText = "PRAGMA application_id";
            var applicationId = Convert.ToInt64(await version.ExecuteScalarAsync(cancellationToken));
            await using var transaction = Connection.BeginTransaction(deferred: true);
            await using var export = Connection.CreateCommand(); export.Transaction = transaction;
            export.CommandText = FormattableString.Invariant($"SELECT sqlcipher_export('{alias}'); PRAGMA {alias}.user_version={userVersion}; PRAGMA {alias}.application_id={applicationId};");
            await export.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            if (attached)
            {
                await using var detach = Connection.CreateCommand(); detach.CommandText = $"DETACH DATABASE {alias};";
                await detach.ExecuteNonQueryAsync(CancellationToken.None);
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try { await Connection.DisposeAsync(); }
        finally { ClearAndFree(_buffer, _length); _buffer = IntPtr.Zero; }
    }

    private static void ClearAndFree(IntPtr pointer, int length)
    {
        var zeros = new byte[Math.Min(length, 81920)];
        for (var offset = 0; offset < length;)
        {
            var count = Math.Min(zeros.Length, length - offset);
            Marshal.Copy(zeros, 0, IntPtr.Add(pointer, offset), count); offset += count;
        }
        raw.sqlite3_free(pointer);
    }
}
