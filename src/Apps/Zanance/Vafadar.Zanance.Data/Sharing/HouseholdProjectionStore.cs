using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Vafadar.Data;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.Data.Sharing;

/// <summary>Display-only source choice; amounts and dates retain their actual account units and never come from a private counterpart.</summary>
public sealed record HouseholdProjectionAccount(Guid SourceId, string Name, string CurrencyCode, long OpeningBalance, bool OpeningBalanceKnown, DateOnly OpeningDate);

/// <summary>Owned in-memory explicit source review; source keys, sessions and open database handles are never retained.</summary>
public sealed class HouseholdProjectionReview : IDisposable
{
    private bool disposed;
    private readonly Dictionary<string, byte[]> blobs;
    internal HouseholdProjectionReview(string sourcePath, HouseholdProjectionSelection selection, string sourceDigest,
        HouseholdProjectedGraph graph, Dictionary<string, byte[]> blobs, HouseholdProjectionAccount[] accounts, DateTimeOffset capturedAt)
    { SourcePath = sourcePath; Selection = selection; SourceDigest = sourceDigest; Graph = graph; this.blobs = blobs;
        Accounts = Array.AsReadOnly(accounts); CapturedAt = capturedAt; }
    internal string SourcePath { get; }
    internal string SourceDigest { get; }
    internal HouseholdProjectionSelection Selection { get; }
    internal HouseholdProjectedGraph Graph { get; }
    internal bool Rechecked { get; private set; }
    internal void MarkRechecked(bool value) { Check(); Rechecked = value; }
    /// <summary>One independently generated reviewed seed identity, retained through its actual atomic application.</summary>
    public Guid Id { get; } = Guid.CreateVersion7();
    /// <summary>Actual explicit selected source accounts, detached from mutable app entities.</summary>
    public IReadOnlyList<HouseholdProjectionAccount> Accounts { get; }
    /// <summary>Clock time of this coherent protected source capture, separate from financial dates.</summary>
    public DateTimeOffset CapturedAt { get; }
    /// <summary>Number of selected historical entries, not new money postings in the source.</summary>
    public int Entries => Graph.Records.Count(row => row.Record.Table == "Entries");
    /// <summary>Number of explicitly included files; attachments default to excluded.</summary>
    public int Attachments => Graph.Records.Count(row => row.Record.Table == "Attachments");
    /// <summary>Private-side transfer references replaced by neutral excluded counterparts.</summary>
    public int PrivateTransfers => Graph.PrivateTransfers;
    /// <summary>Selected refunds whose unselected private purchase link is omitted.</summary>
    public int DetachedRefunds => Graph.DetachedRefunds;
    internal void Check() => ObjectDisposedException.ThrowIf(disposed, this);
    internal Task<byte[]> ReadBlobAsync(string hash, long length, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested(); Check();
        if (!blobs.TryGetValue(hash, out var value) || value.LongLength != length || Convert.ToHexStringLower(SHA256.HashData(value)) != hash)
            throw new InvalidDataException("The reviewed household source attachment changed.");
        return Task.FromResult(value.ToArray());
    }
    /// <inheritdoc />
    public void Dispose() { if (disposed) return; disposed = true; foreach (var bytes in blobs.Values) CryptographicOperations.ZeroMemory(bytes); blobs.Clear(); }
    /// <inheritdoc />
    public override string ToString() => nameof(HouseholdProjectionReview);
}

/// <summary>Captures only explicit selected source history on its actual keyed generation; it neither switches profiles nor sends financial HTTP.</summary>
public sealed class HouseholdProjectionStore(IDbContextFactory<ZananceDbContext> factory, TimeProvider time)
{
    /// <summary>Reads one consistent protected view and closes it before returning an immutable explicit-Save review.</summary>
    public Task<HouseholdProjectionReview> ReviewAsync(HouseholdProjectionSelection selection, CancellationToken ct = default) => Task.Run(async () =>
    {
        var frozen = Freeze(selection); using var source = await CaptureAsync(frozen, ct);
        var graph = HouseholdProjection.Create(frozen, source.Rows, source.PrivateCurrencies);
        var accounts = source.Rows.Where(row => row.Record.Table == "Accounts").Select(item =>
        {
            var row = item.Values!.Value;
            return new HouseholdProjectionAccount(row.GetProperty("Id").GetGuid(), row.GetProperty("Name").GetString()!, row.GetProperty("CurrencyCode").GetString()!,
                row.GetProperty("OpeningBalance").GetInt64(), row.GetProperty("OpeningBalanceKnown").GetInt64() != 0,
                DateOnly.ParseExact(row.GetProperty("OpeningDate").GetString()!, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        }).ToArray();
        return new HouseholdProjectionReview(source.Path, frozen, source.Digest, graph, source.DetachBlobs(), accounts, time.GetUtcNow());
    }, ct);

    /// <summary>Rechecks the reviewed source before a host switches to/creates the separate target; stale history requires a new explicit review.</summary>
    public Task RequireCurrentAsync(HouseholdProjectionReview review, CancellationToken ct = default) => Task.Run(async () =>
    {
        ArgumentNullException.ThrowIfNull(review); review.Check(); review.MarkRechecked(false); using var current = await CaptureAsync(review.Selection, ct);
        if (current.Path != review.SourcePath || current.Digest != review.SourceDigest) throw new InvalidOperationException("The selected source changed after household review.");
        review.MarkRechecked(true);
    }, ct);

    /// <summary>Rechecks the same source while profile maintenance excludes writers and owns the original factory.</summary>
    public Task RequireCurrentForMaintenanceAsync(HouseholdProjectionReview review, DatabaseAccessGate.ExclusiveLease owner,
        CancellationToken ct = default) => Task.Run(async () =>
    {
        ArgumentNullException.ThrowIfNull(review); review.Check(); review.MarkRechecked(false);
        using var current = await CaptureAsync(review.Selection, ct, owner);
        if (current.Path != review.SourcePath || current.Digest != review.SourceDigest)
            throw new InvalidOperationException("The selected source changed after household review.");
        review.MarkRechecked(true);
    }, ct);

    private static HouseholdProjectionSelection Freeze(HouseholdProjectionSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        if (selection.AccountIds.Count > SyncGraphValidation.MaximumRows || selection.AccountIds.Any(id => id == Guid.Empty) ||
            selection.AccountIds.Distinct().Count() != selection.AccountIds.Count || selection.IncludeAttachments && !selection.IncludeHistory ||
            string.IsNullOrWhiteSpace(selection.ExcludedCounterpartName) || selection.ExcludedCounterpartName.Length > 100) throw new ArgumentException("An explicit bounded household source selection is required.");
        return selection with { AccountIds = Array.AsReadOnly(selection.AccountIds.Order().ToArray()) };
    }

    private async Task<CapturedSource> CaptureAsync(HouseholdProjectionSelection selection, CancellationToken ct, DatabaseAccessGate.ExclusiveLease? owner = null)
    {
        await using var db = owner is null ? await factory.CreateDbContextAsync(ct)
            : await ((ILocalDbContextFactory<ZananceDbContext>)factory).CreateDbContextAsync(owner, ct);
        if (db.Database.GetDbConnection() is not SqliteConnection connection) throw new InvalidOperationException("A captured native household source is required.");
        ProtectedSqliteAccess.Require(connection); await db.Database.OpenConnectionAsync(ct); ProtectedSqliteAccess.Require(connection);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // Re-sharing another household requires its separate portable/export consent, not a
        // personal-source creation shortcut that silently republishes another room's history.
        if (await db.Set<HouseholdLocalBinding>().AnyAsync(ct) || await db.Set<SyncBindingRow>().AnyAsync(row => row.TransportKind == ProfileSynchronizationKind.Household, ct))
            throw new InvalidOperationException("Household source selection requires a personal profile.");
        var result = new CapturedSource(Path.GetFullPath(DatabaseFileIdentity.ConfiguredPath(db)));
        try
        {
            await ReadAsync("Accounts", "lower(source.\"Id\") IN (SELECT value FROM json_each($ids))", selection.AccountIds);
            if (result.Rows.Count != selection.AccountIds.Count) throw new InvalidOperationException("A selected household source account is unavailable.");
            if (selection.IncludeHistory && selection.AccountIds.Count != 0)
            {
                await ReadAsync("Entries", "(lower(source.\"AccountId\") IN (SELECT value FROM json_each($ids)) OR lower(source.\"ToAccountId\") IN (SELECT value FROM json_each($ids)))", selection.AccountIds);
                var entries = result.Rows.Where(row => row.Record.Table == "Entries").ToArray();
                var needed = entries.Select(row => OptionalGuid(row.Values!.Value, "CategoryId")).Where(id => id is not null).Select(id => id!.Value).ToHashSet();
                var seen = new HashSet<Guid>();
                while (needed.Count != 0)
                {
                    var current = needed.Where(id => !seen.Contains(id)).ToArray(); if (current.Length == 0) break;
                    foreach (var id in current) seen.Add(id);
                    var start = result.Rows.Count; await ReadAsync("Categories", "lower(source.\"Id\") IN (SELECT value FROM json_each($ids))", current);
                    if (result.Rows.Count - start != current.Length) throw new InvalidDataException("The selected source category ancestry is incomplete.");
                    needed = result.Rows.Skip(start).Select(row => OptionalGuid(row.Values!.Value, "ParentId")).Where(id => id is not null).Select(id => id!.Value).ToHashSet();
                }
                if (selection.IncludeAttachments)
                    await ReadAsync("Attachments", "lower(source.\"EntryId\") IN (SELECT value FROM json_each($ids))", entries.Select(row => row.Values!.Value.GetProperty("Id").GetGuid()).ToArray());
                var selected = selection.AccountIds.ToHashSet();
                var incoming = entries.Where(row => row.Values!.Value.GetProperty("Kind").GetInt64() == 2 &&
                    !selected.Contains(row.Values!.Value.GetProperty("AccountId").GetGuid()) && row.Values!.Value.GetProperty("ToAmount").ValueKind == JsonValueKind.Null)
                    .Select(row => row.Values!.Value.GetProperty("AccountId").GetGuid()).Distinct().ToArray();
                foreach (var row in await db.Accounts.AsNoTracking().Where(row => incoming.Contains(row.Id)).Select(row => new { row.Id, row.CurrencyCode }).ToArrayAsync(ct))
                    result.PrivateCurrencies.Add(row.Id, row.CurrencyCode);
            }
            using (var choice = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(choice))
                {
                    writer.WriteStartArray(); foreach (var id in selection.AccountIds) writer.WriteStringValue(id.ToString("D"));
                    writer.WriteBooleanValue(selection.IncludeHistory); writer.WriteBooleanValue(selection.IncludeAttachments);
                    writer.WriteStringValue(selection.ExcludedCounterpartName); writer.WriteEndArray();
                }
                result.Digest = SyncProtocolDigest.Round([Fingerprint(result.Rows, result.PrivateCurrencies), Convert.ToHexStringLower(SHA256.HashData(choice.ToArray()))]);
            }
            ProtectedSqliteAccess.Require(connection); await transaction.CommitAsync(ct); return result;
        }
        catch { result.Dispose(); throw; }

        async Task ReadAsync(string name, string predicate, IReadOnlyCollection<Guid> ids)
        {
            ct.ThrowIfCancellationRequested();
            var table = HouseholdFinancialSchema.Manifest.Tables.Single(table => table.Name == name);
            await using var command = connection.CreateCommand(); command.Transaction = (SqliteTransaction)transaction.GetDbTransaction();
            command.CommandText = SyncTriggerSql.ReadExistingAliased(table) + " WHERE " + predicate + ";";
            using var json = new MemoryStream();
            using (var writer = new Utf8JsonWriter(json)) { writer.WriteStartArray(); foreach (var id in ids) writer.WriteStringValue(id.ToString("D")); writer.WriteEndArray(); }
            command.Parameters.AddWithValue("$ids", Encoding.UTF8.GetString(json.ToArray()));
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                ProtectedSqliteAccess.Require(connection); var key = new SyncRecordKey(name, reader.GetString(0)); var body = reader.GetString(1);
                result.SourceBytes = checked(result.SourceBytes + Encoding.UTF8.GetByteCount(body));
                if (result.SourceBytes > 3 * SyncGraphValidation.MaximumProfileBytes || result.Rows.Count >= SyncGraphValidation.MaximumRows) throw new SyncProfileBoundException();
                result.Rows.Add(new(key, 0, SyncRowCodec.ToWire(key, body, (hash, bytes) =>
                {
                    if (result.Blobs.ContainsKey(hash)) return;
                    if (result.Blobs.Values.Sum(value => (long)value.Length) + bytes.Length > SyncGraphValidation.MaximumProfileBytes) throw new SyncProfileBoundException();
                    result.Blobs.Add(hash, bytes.ToArray());
                })));
            }
        }
    }

    internal static string Fingerprint(IEnumerable<SyncRecord> rows, IReadOnlyDictionary<Guid, string>? privateCurrencies = null)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var row in rows.OrderBy(row => row.Record.Table, StringComparer.Ordinal).ThenBy(row => row.Record.Key, StringComparer.Ordinal))
            hash.AppendData(JsonSerializer.SerializeToUtf8Bytes(row with { Version = 0 }, PersonalSyncJsonContext.Default.SyncRecord));
        if (privateCurrencies is not null)
            foreach (var item in privateCurrencies.OrderBy(item => item.Key)) hash.AppendData(Encoding.ASCII.GetBytes(item.Key.ToString("D") + ":" + item.Value + "\n"));
        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }
    private static Guid? OptionalGuid(JsonElement row, string name) => row.GetProperty(name).ValueKind == JsonValueKind.Null ? null : row.GetProperty(name).GetGuid();
    private sealed class CapturedSource(string path) : IDisposable
    {
        internal string Path { get; } = path;
        internal string Digest { get; set; } = "";
        internal long SourceBytes { get; set; }
        internal List<SyncRecord> Rows { get; } = [];
        internal Dictionary<Guid, string> PrivateCurrencies { get; } = [];
        internal Dictionary<string, byte[]> Blobs { get; private set; } = new(StringComparer.Ordinal);
        internal Dictionary<string, byte[]> DetachBlobs() { var result = Blobs; Blobs = new(StringComparer.Ordinal); return result; }
        public void Dispose() { foreach (var bytes in Blobs.Values) CryptographicOperations.ZeroMemory(bytes); Blobs.Clear(); }
    }
}
