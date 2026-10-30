using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>Opaque original profile-file/source consent, never exported data or a reusable authentication result.</summary>
public sealed class ProfileResetReview
{
    private readonly CommercialFileAccess _access;
    private readonly string _stamp;
    /// <summary>Gets the identity of this one explicit destructive review, without persisting another model.</summary>
    public Guid Id { get; } = Guid.NewGuid();
    /// <summary>Gets the actual original file named by this consent.</summary>
    public string DatabasePath { get; }
    internal ProfileResetReview(string path, CommercialFileAccess access, string stamp) { DatabasePath = path; _access = access; _stamp = stamp; }
    internal void RequireFile(ZananceDbContext db) => _access.RequireFile(DatabaseFileIdentity.ConfiguredPath(db));
    internal async Task ValidateAsync(ZananceDbContext db, CancellationToken ct)
    {
        RequireFile(db);
        if (_stamp != await CaptureStampAsync(db, ct)) throw new InvalidOperationException("The reviewed profile contents changed; review deletion again.");
        RequireFile(db);
    }
    // This rare destructive review streams original typed cells; it never materializes a backup, exports a key or logs source values.
    internal static async Task<string> CaptureStampAsync(ZananceDbContext db, CancellationToken ct)
    {
        var connection = (SqliteConnection)db.Database.GetDbConnection();
        var transaction = (SqliteTransaction?)db.Database.CurrentTransaction?.GetDbTransaction(); var tables = new List<(string Name, string Sql)>();
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        void Cell(byte type, ReadOnlySpan<byte> bytes)
        {
            Span<byte> header = stackalloc byte[9]; header[0] = type; BinaryPrimitives.WriteInt64LittleEndian(header[1..], bytes.Length);
            hash.AppendData(header); hash.AppendData(bytes);
        }
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction; command.CommandText = "SELECT type,name,tbl_name,coalesce(sql,'') FROM sqlite_master WHERE name NOT LIKE 'sqlite_%' ORDER BY type,name";
            using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                for (var i = 0; i < 4; i++) Cell(1, Encoding.UTF8.GetBytes(reader.GetString(i)));
                if (reader.GetString(0) == "table") tables.Add((reader.GetString(1), reader.GetString(3)));
            }
        }
        foreach (var (name, sql) in tables)
        {
            ct.ThrowIfCancellationRequested(); Cell(2, Encoding.UTF8.GetBytes(name));
            // Optional derived read transport is not financial authority and may refresh independently.
            if (name is "HomeLedgerReadState" or "HomeLedgerReadBlocks" or "HomeLedgerDirtyBlocks" or "HomeFinancialRevision") continue;
            var quoted = "\"" + name.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
            using var command = connection.CreateCommand(); command.Transaction = transaction; command.CommandText = "SELECT * FROM " + quoted;
            // All ordinary EF tables have rowid. A future WITHOUT ROWID table uses its declared result columns instead.
            var order = "rowid";
            if (sql.Contains("WITHOUT ROWID", StringComparison.OrdinalIgnoreCase))
            {
                var columns = new List<string>(); using var metadata = connection.CreateCommand(); metadata.Transaction = transaction; metadata.CommandText = "PRAGMA table_info(" + quoted + ")";
                using var fields = await metadata.ExecuteReaderAsync(ct);
                while (await fields.ReadAsync(ct)) columns.Add("\"" + fields.GetString(1).Replace("\"", "\"\"", StringComparison.Ordinal) + "\"");
                order = string.Join(',', columns);
            }
            command.CommandText += " ORDER BY " + order; using var reader = await command.ExecuteReaderAsync(ct);
            for (var i = 0; i < reader.FieldCount; i++) Cell(3, Encoding.UTF8.GetBytes(reader.GetName(i)));
            while (await reader.ReadAsync(ct))
            {
                Cell(4, []);
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    if (reader.IsDBNull(i)) Cell(0, []);
                    else if (reader.GetValue(i) is byte[] blob) Cell(5, blob);
                    else if (reader.GetValue(i) is long integer) { var bytes = new byte[8]; BinaryPrimitives.WriteInt64LittleEndian(bytes, integer); Cell(6, bytes); }
                    else if (reader.GetValue(i) is double number) { var bytes = new byte[8]; BinaryPrimitives.WriteInt64LittleEndian(bytes, BitConverter.DoubleToInt64Bits(number)); Cell(7, bytes); }
                    else Cell(8, Encoding.UTF8.GetBytes(reader.GetString(i)));
                }
            }
        }
        return Convert.ToHexString(hash.GetHashAndReset());
    }
}

public sealed partial class ZananceStore
{
    /// <summary>Captures only a rare explicit reset consent from the original actual file and one deferred transaction.</summary>
    public async Task<ProfileResetReview> ReadProfileResetReviewAsync(SettingsWorkSnapshot settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings); await using var db = await contextFactory.CreateDbContextAsync(cancellationToken); settings.RequireOriginal(db);
        var path = DatabaseFileIdentity.ConfiguredPath(db); var access = new CommercialFileAccess(commercialAccess, path);
        return await Task.Run(async () =>
        {
            access.EnsureSnapshotCurrent(); await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, cancellationToken);
            var result = new ProfileResetReview(path, access, await ProfileResetReview.CaptureStampAsync(db, cancellationToken)); access.EnsureSnapshotCurrent(); return result;
        }, cancellationToken);
    }
}
