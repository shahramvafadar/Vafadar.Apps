using System.Buffers;
using System.Data;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace Vafadar.Data;

/// <summary>Creates portable plaintext SQLite images entirely in memory from an open keyed SQLCipher connection.</summary>
/// <remarks>The caller owns profile authorization and stream protection. No plaintext temporary file is created.</remarks>
public static class SqlCipherPortableSnapshot
{
    /// <summary>Writes a bounded consistent snapshot while preserving SQLite header identifiers and the caller's connection.</summary>
    public static Task WriteAsync(SqliteConnection source, Stream destination, int maximumBytes,
        CancellationToken cancellationToken = default) => WriteAsync(source, destination, maximumBytes, null, cancellationToken);

    /// <summary>Writes a coherent bounded snapshot with cached captured-access checks before native effects and every output chunk.</summary>
    /// <remarks>The check must not query the database, move profiles or perform network work; partial output is caller-owned on failure.</remarks>
    public static async Task WriteAsync(SqliteConnection source, Stream destination, int maximumBytes, Action? verifyAccess,
        CancellationToken cancellationToken = default,
        Func<SqliteConnection, string, CancellationToken, Task>? preparePortableImage = null,
        IReadOnlyCollection<string>? emptyRowTables = null)
    {
        ArgumentNullException.ThrowIfNull(source); ArgumentNullException.ThrowIfNull(destination);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumBytes);
        if (source.State != ConnectionState.Open || source.Handle is null)
            throw new InvalidOperationException("Open the captured keyed source before exporting.");
        if (raw.sqlite3_get_autocommit(source.Handle) == 0)
            throw new InvalidOperationException("The portable exporter requires its own snapshot transaction.");
        verifyAccess?.Invoke();
        var omitted = emptyRowTables?.ToHashSet(StringComparer.Ordinal) ?? [];
        if (omitted.Count > 256 || omitted.Any(name => name.Length is < 1 or > 128 || name.StartsWith("sqlite_", StringComparison.OrdinalIgnoreCase) ||
            name.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '_')))
            throw new ArgumentException("Only finite trusted ordinary table names can omit portable rows.", nameof(emptyRowTables));
        var alias = "portable_" + Guid.NewGuid().ToString("N");
        var attached = false;
        var authorizing = false;
        IntPtr serialized = IntPtr.Zero;
        long size = 0;
        var buffer = ArrayPool<byte>.Shared.Rent(81920);
        try
        {
            verifyAccess?.Invoke();
            await using (var attach = source.CreateCommand())
            {
                attach.CommandText = $"ATTACH DATABASE ':memory:' AS {alias} KEY '';";
                await attach.ExecuteNonQueryAsync(cancellationToken); attached = true;
                verifyAccess?.Invoke();
            }
            if (omitted.Count != 0)
            {
                // This is a fresh memory schema. Enforce the existing output budget before native
                // allocation, rather than first materializing an arbitrarily large private journal.
                await using var bound = source.CreateCommand(); bound.CommandText = $"PRAGMA {alias}.page_size;";
                var pageSize = Convert.ToInt64(await bound.ExecuteScalarAsync(cancellationToken));
                if (pageSize is < 512 or > 65536) throw new InvalidDataException("Portable page size was refused.");
                bound.CommandText = FormattableString.Invariant($"PRAGMA {alias}.max_page_count={Math.Max(1, maximumBytes / pageSize)};");
                await bound.ExecuteNonQueryAsync(cancellationToken);
                strdelegate_authorizer authorize = (_, action, table, _, database, _) =>
                    action == raw.SQLITE_INSERT && database == alias && table is not null && omitted.Contains(table) ? raw.SQLITE_IGNORE : raw.SQLITE_OK;
                // SQLite IGNORE on this exact destination INSERT suppresses only its row copy.
                // sqlcipher_export still owns schema, indexes, triggers and all non-private rows.
                // The application factory owns this connection; no pre-existing authorizer is replaced.
                var result = raw.sqlite3_set_authorizer(source.Handle, authorize, null);
                if (result != raw.SQLITE_OK) throw new InvalidOperationException("Portable row authorization could not be installed.");
                authorizing = true;
            }
            // The read transaction holds a single WAL view throughout schema, rows and header copying.
            await using (var transaction = source.BeginTransaction(deferred: true))
            {
                async Task<long> ReadHeader(string name)
                {
                    await using var command = source.CreateCommand(); command.Transaction = transaction;
                    command.CommandText = "PRAGMA main." + name;
                    return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
                }
                var userVersion = await ReadHeader("user_version");
                var applicationId = await ReadHeader("application_id");
                verifyAccess?.Invoke();
                await using var export = source.CreateCommand(); export.Transaction = transaction;
                export.CommandText = FormattableString.Invariant($"SELECT sqlcipher_export('{alias}'); PRAGMA {alias}.user_version={userVersion}; PRAGMA {alias}.application_id={applicationId};");
                await export.ExecuteNonQueryAsync(cancellationToken);
                verifyAccess?.Invoke();
                await transaction.CommitAsync(cancellationToken);
            }
            if (authorizing)
            {
                if (raw.sqlite3_set_authorizer(source.Handle, (strdelegate_authorizer?)null, null) != raw.SQLITE_OK)
                    throw new InvalidOperationException("Portable row authorization could not be released.");
                authorizing = false;
            }
            // sqlcipher_export created fresh logical pages in this memory-only copy. Application
            // policies may securely clear device metadata here without touching the keyed source.
            if (preparePortableImage is not null)
            {
                verifyAccess?.Invoke(); await preparePortableImage(source, alias, cancellationToken);
                verifyAccess?.Invoke();
            }
            serialized = raw.sqlite3_serialize(source.Handle, alias, out size, 0);
            verifyAccess?.Invoke();
            if (serialized == IntPtr.Zero || size <= 0 || size > maximumBytes)
                throw new InvalidDataException("Portable database image exceeds the allowed size or could not be serialized.");
            for (var offset = 0; offset < size;)
            {
                cancellationToken.ThrowIfCancellationRequested();
                verifyAccess?.Invoke();
                // D-198: ArrayPool may rent a larger bucket; output chunk bounds are independent of capacity.
                var count = (int)Math.Min(81920, size - offset);
                Marshal.Copy(IntPtr.Add(serialized, offset), buffer, 0, count);
                await destination.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
                verifyAccess?.Invoke();
                offset += count;
            }
        }
        finally
        {
            if (authorizing) raw.sqlite3_set_authorizer(source.Handle, (strdelegate_authorizer?)null, null);
            CryptographicOperations.ZeroMemory(buffer);
            if (serialized != IntPtr.Zero)
            {
                // Clear the owned plaintext serialization allocation before returning it to SQLite.
                for (long offset = 0; offset < size;)
                {
                    var count = (int)Math.Min(buffer.Length, size - offset);
                    Marshal.Copy(buffer, 0, new IntPtr(checked(serialized.ToInt64() + offset)), count);
                    offset += count;
                }
                raw.sqlite3_free(serialized);
            }
            ArrayPool<byte>.Shared.Return(buffer);
            if (attached)
            {
                await using var detach = source.CreateCommand(); detach.CommandText = $"DETACH DATABASE {alias};";
                await detach.ExecuteNonQueryAsync(CancellationToken.None);
            }
        }
    }
}
