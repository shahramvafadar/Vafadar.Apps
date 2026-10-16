using System.Data.Common;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.Data.Sync;

public sealed partial class PersonalSyncStore
{
    /// <summary>Applies the complete staged graph with current local edits, or retains explicit conflicts without a financial write.</summary>
    internal async Task<bool> ApplyFeedAsync(SyncRoundWork work, CancellationToken ct, bool refreshReviewOnly = false, Guid? ownershipReception = null)
    {
        var applied = false;
        await InRoundAsync(work, async (db, round, binding) =>
        {
            if (round.Phase != 2 || round.ThroughCursor is not { } through || round.ReadCursor != through)
                throw new InvalidOperationException("The personal feed is incomplete.");
            await ReconcileAttachmentCaptureAsync(db, round.Cutoff, ct);
            await ReconcileGroupCaptureAsync(db, round.Cutoff, ct);
            var contract = SyncSchema.ForScope(binding.TransportKind == ProfileSynchronizationKind.Household);
            var local = await ReadFinancialAsync(db, ct, contract);
            var known = await db.Set<SyncKnownRow>().ToDictionaryAsync(row => new SyncRecordKey(row.TableName, row.RecordKey), ct);
            var incoming = (await db.Set<SyncIncomingRow>().AsNoTracking().ToArrayAsync(ct))
                .ToDictionary(row => new SyncRecordKey(row.TableName, row.RecordKey), row => new SyncRecord(new(row.TableName, row.RecordKey), row.Version, SyncRowCodec.Parse(row.Body)));
            var pending = await db.Set<SyncOutboxRow>().Where(row => row.Sequence > round.Cutoff)
                .GroupBy(row => new { row.TableName, row.RecordKey })
                .Select(group => new { group.Key.TableName, group.Key.RecordKey, Sequence = group.Max(row => row.Sequence) }).ToArrayAsync(ct);
            var boundaries = pending.ToDictionary(row => new SyncRecordKey(row.TableName, row.RecordKey), row => row.Sequence);
            var outgoing = work.Parts.SelectMany(part => part.Rows).ToDictionary(row => row.Record);
            var reviews = (await db.Set<SyncConflictRow>().AsNoTracking().ToArrayAsync(ct)).ToDictionary(row => new SyncRecordKey(row.TableName, row.RecordKey));
            var projected = new Dictionary<SyncRecordKey, JsonElement?>(local);
            var conflicts = new Dictionary<SyncRecordKey, SyncConflict>();
            // A reviewed proposal can select an absent remote row and produce no new event. Its
            // acknowledgement still has to remove that local row instead of leaving an unqueued duplicate.
            var affected = incoming.Keys.Concat(outgoing.Keys).Concat(reviews.Keys).Concat(boundaries.Keys).ToHashSet();
            var remoteGraph = known.ToDictionary(row => row.Key, row => new SyncRecord(row.Key, row.Value.Version, SyncRowCodec.Parse(row.Value.Body)));
            foreach (var (key, remote) in incoming) remoteGraph[key] = remote;
            var proposals = boundaries.Keys.ToDictionary(key => key, key => new SyncProposal(key, known.GetValueOrDefault(key)?.Version ?? 0, local.GetValueOrDefault(key)));
            var baselines = boundaries.Keys.ToDictionary(key => key, key => new SyncRecord(key, known.GetValueOrDefault(key)?.Version ?? 0,
                outgoing.TryGetValue(key, out var sent) ? sent.Values : SyncRowCodec.Parse(known.GetValueOrDefault(key)?.Body)));
            foreach (var key in affected)
            {
                var remote = incoming.GetValueOrDefault(key) ?? new SyncRecord(key, known.GetValueOrDefault(key)?.Version ?? 0, SyncRowCodec.Parse(known.GetValueOrDefault(key)?.Body));
                local.TryGetValue(key, out var current);
                if (!boundaries.TryGetValue(key, out var boundary)) { projected[key] = remote.Values; continue; }
                var baseline = outgoing.TryGetValue(key, out var submitted) ? submitted.Values : SyncRowCodec.Parse(known.GetValueOrDefault(key)?.Body);
                var proposal = new SyncProposal(key, known.GetValueOrDefault(key)?.Version ?? 0, current);
                var knownDeletion = baseline is null && remote.Values is null &&
                    (submitted is { Values: null } || known.GetValueOrDefault(key) is { Body: null } previous && previous.Version == remote.Version);
                var merged = SyncRowMerge.Merge(baseline, remote.Values, current, remote.Values is null && remote.Version > 0, knownDeletion);
                if (!refreshReviewOnly && reviews.TryGetValue(key, out var review) && review.UseProposal is { } choice && review.LocalSequence == boundary)
                {
                    var original = JsonSerializer.Deserialize(review.Payload, PersonalSyncJsonContext.Default.SyncConflict);
                    if (original is not null && original.Current?.Version == remote.Version && Same(original.Proposal.Values, current))
                    { projected[key] = choice ? current : remote.Values; continue; }
                }
                if (merged.Conflicts.Count != 0)
                {
                    conflicts[key] = new(proposal, new SyncRecord(key, proposal.BaseVersion, baseline), remote, merged.Conflicts, "CompetingChanges");
                    continue;
                }
                projected[key] = merged.Values;
            }
            var graph = projected.Select(row => new SyncRecord(row.Key, 0, row.Value)).ToArray();
            if (conflicts.Count == 0)
            {
                foreach (var issue in SyncFinancialValidation.Validate(local.Select(row => new SyncRecord(row.Key, 0, row.Value)).ToArray(), graph, contract: contract))
                {
                    var key = boundaries.ContainsKey(issue.Record) ? issue.Record
                        : issue.RelatedRecord is { } related && boundaries.ContainsKey(related) ? related : issue.Record;
                    if (!boundaries.ContainsKey(key)) throw new InvalidDataException("The received complete financial graph cannot be safely applied.");
                    var remote = incoming.GetValueOrDefault(key) ?? new SyncRecord(key, known.GetValueOrDefault(key)?.Version ?? 0, SyncRowCodec.Parse(known.GetValueOrDefault(key)?.Body));
                    conflicts[key] = new(new(key, known.GetValueOrDefault(key)?.Version ?? 0, local.GetValueOrDefault(key)), null, remote,
                        issue.Fields, issue.Reason, RelatedRecord: incoming.GetValueOrDefault(issue.Record));
                }
            }
            // A later edit to any member invalidates the whole displayed group, even if that row can merge alone.
            foreach (var retained in reviews.Values)
            {
                var original = JsonSerializer.Deserialize(retained.Payload, PersonalSyncJsonContext.Default.SyncConflict);
                if (original?.LinkedGroup is not { } group) continue;
                var key = original.Proposal.Record;
                var currentRemote = remoteGraph.GetValueOrDefault(key);
                if (refreshReviewOnly || retained.UseProposal is null || retained.LocalSequence != boundaries.GetValueOrDefault(key) ||
                    !Same(original.Proposal.Values, local.GetValueOrDefault(key)) || original.Current?.Version != currentRemote?.Version)
                    conflicts[key] = new(proposals.GetValueOrDefault(key) ?? original.Proposal, baselines.GetValueOrDefault(key), currentRemote,
                        ["GroupId"], "LinkedGroupReview", LinkedGroup: group);
            }
            foreach (var group in proposals.Values.GroupBy(proposal => (Family: SyncConflictGrouping.Family(proposal.Record), Id: SyncConflictGrouping.Group(proposal.Record, proposal.Values) ??
                SyncConflictGrouping.Group(proposal.Record, remoteGraph.GetValueOrDefault(proposal.Record)?.Values) ??
                SyncConflictGrouping.Group(proposal.Record, baselines.GetValueOrDefault(proposal.Record)?.Values))).Where(group => group.Key.Id is not null))
            {
                var allApproved = !refreshReviewOnly && group.All(proposal => reviews.TryGetValue(proposal.Record, out var choice) &&
                    choice.UseProposal is not null && !conflicts.ContainsKey(proposal.Record)) &&
                    group.Select(proposal => reviews[proposal.Record].UseProposal).Distinct().Count() == 1;
                if (!allApproved && group.Any(proposal => !Same(proposal.Values, baselines[proposal.Record].Values)) &&
                    (group.Any(proposal => !Same(remoteGraph.GetValueOrDefault(proposal.Record)?.Values, baselines[proposal.Record].Values)) ||
                        remoteGraph.Values.Any(record => SyncConflictGrouping.Family(record.Record) == group.Key.Family && SyncConflictGrouping.Group(record.Record, record.Values) == group.Key.Id && !proposals.ContainsKey(record.Record))) &&
                    group.Any(proposal => !Same(proposal.Values, remoteGraph.GetValueOrDefault(proposal.Record)?.Values)))
                    foreach (var proposal in group)
                        conflicts[proposal.Record] = new(proposal, baselines[proposal.Record], remoteGraph.GetValueOrDefault(proposal.Record),
                            ["GroupId"], "LinkedGroupReview", LinkedGroup: group.Key.Id);
            }
            if (conflicts.Count != 0)
            {
                var page = SyncConflictGrouping.Page(SyncConflictGrouping.Expand(conflicts.Values, proposals, remoteGraph, baselines));
                // Keep other completed choices across bounded review pages. Only reissued snapshots lose consent.
                var replaced = page.Select(conflict => conflict.Proposal.Record).ToHashSet();
                foreach (var old in reviews.Where(row => refreshReviewOnly || row.Value.UseProposal is null || replaced.Contains(row.Key)))
                    await db.Set<SyncConflictRow>().Where(row => row.TableName == old.Key.Table && row.RecordKey == old.Key.Key).ExecuteDeleteAsync(ct);
                foreach (var conflict in page)
                {
                    var key = conflict.Proposal.Record;
                    db.Add(new SyncConflictRow { TableName = key.Table, RecordKey = key.Key, LocalSequence = boundaries.GetValueOrDefault(key),
                        Payload = JsonSerializer.Serialize(conflict with { Context = SyncReviewRelationships.Capture(conflict, remoteGraph) }, PersonalSyncJsonContext.Default.SyncConflict) });
                }
                return;
            }
            await db.Set<SyncConflictRow>().ExecuteDeleteAsync(ct);
            // Explicit review refresh invalidates old choices but preserves the acknowledged round,
            // feed, outbox and all financial rows, even when new edits remove every conflict.
            if (refreshReviewOnly) return;
            // Device-local security settings are never deleted or replaced by a remote singleton tombstone.
            var settingsKey = new SyncRecordKey("Settings", "[]");
            if (local.ContainsKey(settingsKey) && projected.GetValueOrDefault(settingsKey) is null)
                throw new InvalidDataException("Personal sync cannot remove the local settings singleton.");
            if (ownershipReception is { } operation)
            {
                if (binding.TransportKind != ProfileSynchronizationKind.Household || outgoing.Count != 0 || reviews.Count != 0 || pending.Length != 0) throw new InvalidOperationException("Ownership reception cannot publish local work.");
                await Sharing.HouseholdJournal.RequireOwnershipReceptionAsync(db, operation, ct, through);
            }
            binding.Applying = true; await db.SaveChangesAsync(ct);
            await db.Database.ExecuteSqlRawAsync("PRAGMA defer_foreign_keys=ON;", ct);
            var changed = projected.Where(row => !Same(local.GetValueOrDefault(row.Key), row.Value)).ToArray();
            using var householdApply = binding.TransportKind == ProfileSynchronizationKind.Household
                ? db.ArmVerifiedHouseholdMaintenance(changed.Select(row => row.Key).ToHashSet()) : null;
            var indexes = await DropSwapIndexesAsync(db, changed.Select(row => row.Key.Table).ToHashSet(StringComparer.Ordinal), ct);
            foreach (var (key, selected) in changed.Where(row => row.Value is not null))
                await WriteFinancialAsync(db, key, selected!.Value, local.GetValueOrDefault(key), ct);
            var tableOrder = DependencyOrder();
            foreach (var (key, _) in changed.Where(row => row.Value is null).OrderByDescending(row => tableOrder[row.Key.Table]))
                await DeleteFinancialAsync(db, key, ct);
            foreach (var sql in indexes) await db.Database.ExecuteSqlRawAsync(sql, ct);
            await using (var check = Command(db, "PRAGMA foreign_key_check;"))
            await using (var failures = await check.ExecuteReaderAsync(ct))
                if (await failures.ReadAsync(ct)) throw new InvalidDataException("The complete personal graph violates an original foreign key.");
            var actual = await ReadFinancialAsync(db, ct, contract);
            if (projected.Count(row => row.Value is not null) != actual.Count || projected.Any(row => !Same(row.Value, actual.GetValueOrDefault(row.Key))))
                throw new InvalidDataException("The applied personal graph does not match its complete reviewed projection.");
            foreach (var (key, remote) in incoming)
            {
                if (!known.TryGetValue(key, out var target))
                { target = new() { TableName = key.Table, RecordKey = key.Key }; db.Add(target); }
                target.Version = remote.Version; target.Body = remote.Values?.GetRawText();
            }
            await db.Set<SyncOutboxRow>().Where(row => row.Sequence <= round.Cutoff).ExecuteDeleteAsync(ct);
            foreach (var key in affected.Where(boundaries.ContainsKey))
            {
                await db.Set<SyncOutboxRow>().Where(row => row.TableName == key.Table && row.RecordKey == key.Key).ExecuteDeleteAsync(ct);
                var remote = incoming.GetValueOrDefault(key)?.Values ?? (incoming.ContainsKey(key) ? null : SyncRowCodec.Parse(known.GetValueOrDefault(key)?.Body));
                if (!Same(projected.GetValueOrDefault(key), remote)) await CaptureOneAsync(db, key, projected.GetValueOrDefault(key) is null, ct);
            }
            // The financial rows, known baselines, rebased queue and applied cursor become visible together.
            binding.Cursor = through; binding.LastSuccessAt = (time ?? TimeProvider.System).GetUtcNow(); binding.LastFailure = null; binding.Applying = false;
            await db.Set<SyncIncomingRow>().ExecuteDeleteAsync(ct);
            await db.Set<SyncBlobPart>().ExecuteDeleteAsync(ct); await db.Set<SyncBlobTransfer>().ExecuteDeleteAsync(ct);
            db.Remove(round); applied = true;
        }, ct);
        return applied;
    }

    internal static async Task<Dictionary<SyncRecordKey, JsonElement?>> ReadFinancialAsync(ZananceDbContext db, CancellationToken ct, SyncSchemaManifest? contract = null)
    {
        var result = new Dictionary<SyncRecordKey, JsonElement?>(); long bytes = 0;
        foreach (var table in (contract ?? SyncSchema.Manifest).Tables)
        {
            await using var command = Command(db, SyncTriggerSql.ReadLiveExisting(table));
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var key = new SyncRecordKey(table.Name, reader.GetString(0)); var body = reader.GetString(1);
                bytes += System.Text.Encoding.UTF8.GetByteCount(body);
                if (result.Count >= SyncGraphValidation.MaximumRows || bytes > 3 * SyncGraphValidation.MaximumProfileBytes) throw new SyncProfileBoundException();
                if (!result.TryAdd(key, SyncRowCodec.ToWire(key, body))) throw new InvalidDataException("The local settings singleton is ambiguous.");
            }
        }
        return result;
    }

    private Task WriteFinancialAsync(ZananceDbContext db, SyncRecordKey key, JsonElement selected, JsonElement? before, CancellationToken ct) =>
        WriteProjectedFinancialAsync(db, key, selected, before, (time ?? TimeProvider.System).GetUtcNow(),
            (hash, length, token) => VerifiedBlobAsync(db, hash, length, token), ct);

    /// <summary>The original parameterized writer also installs an explicitly reviewed isolated household seed; blob bytes remain caller-owned and verified.</summary>
    internal static async Task WriteProjectedFinancialAsync(ZananceDbContext db, SyncRecordKey key, JsonElement selected, JsonElement? before,
        DateTimeOffset timestamp, Func<string, long, CancellationToken, Task<byte[]>> readBlob, CancellationToken ct)
    {
        var table = SyncSchema.Describe(key.Table);
        if (table.Singleton && before is null)
        {
            // Original defaults construct all device-only columns; the peer updates only its reviewed allowlist.
            db.Settings.Add(new ZananceSettings()); await db.SaveChangesAsync(ct);
            before = (await ReadFinancialAsync(db, ct, SyncSchema.ForScope(key.Table.StartsWith("Household", StringComparison.Ordinal)))).GetValueOrDefault(key);
        }
        var columns = table.Columns.Where(column => before is null || !table.PrimaryKey.Contains(column.Name)).ToArray();
        var updates = new List<string>(); var names = new List<string>(); var values = new List<string>(); var secrets = new List<byte[]>();
        await using var command = Command(db, "");
        try
        {
            for (var index = 0; index < columns.Length; index++)
            {
                var column = columns[index]; var cell = selected.GetProperty(column.Name);
                // A retained local attachment can remain in its original cell; only new remote bytes need assembly.
                if (before is { } old && column.Kind == "blob" && JsonElement.DeepEquals(old.GetProperty(column.Name), cell)) continue;
                var parameter = "$v" + index; object? value;
                if (cell.ValueKind == JsonValueKind.Null) value = null;
                else if (column.Kind == "blob")
                {
                    var content = await readBlob(cell.GetProperty("sha256").GetString()!, cell.GetProperty("length").GetInt64(), ct);
                    secrets.Add(content); value = content;
                }
                else value = column.Kind switch { "integer" or "ticks" => cell.GetInt64(), "real" => cell.GetDouble(), _ => (object?)cell.GetString() };
                Parameter(command, parameter, value); names.Add(Quote(column.Name)); values.Add(parameter); updates.Add(Quote(column.Name) + "=" + parameter);
            }
            if (await HasAuditColumnAsync(db, table.Name, ct))
            {
                Parameter(command, "$audit", timestamp.UtcTicks);
                names.Add("\"UpdatedAt\""); values.Add("$audit"); updates.Add("\"UpdatedAt\"=$audit");
            }
            command.CommandText = before is null ? "INSERT INTO " + Quote(table.Name) + "(" + string.Join(',', names) + ") VALUES(" + string.Join(',', values) + ");"
                : "UPDATE " + Quote(table.Name) + " SET " + string.Join(',', updates) + " WHERE " + KeyPredicate(command, table, key) + ";";
            if (await command.ExecuteNonQueryAsync(ct) != 1) throw new InvalidDataException("The personal financial write did not affect its exact original row.");
        }
        finally { foreach (var content in secrets) CryptographicOperations.ZeroMemory(content); }
    }

    private static async Task DeleteFinancialAsync(ZananceDbContext db, SyncRecordKey key, CancellationToken ct)
    {
        var table = SyncSchema.Describe(key.Table);
        if (table.Singleton) throw new InvalidDataException("Personal sync cannot remove a local settings singleton.");
        await using var command = Command(db, "");
        command.CommandText = "DELETE FROM " + Quote(table.Name) + " WHERE " + KeyPredicate(command, table, key) + ";";
        await command.ExecuteNonQueryAsync(ct);
    }
    private static async Task CaptureOneAsync(ZananceDbContext db, SyncRecordKey key, bool tombstone, CancellationToken ct)
    {
        if (tombstone)
        { db.Add(new SyncOutboxRow { TableName = key.Table, RecordKey = key.Key }); return; }
        var table = SyncSchema.Describe(key.Table);
        // Explicit result aliases avoid depending on SQLite's expression-derived column captions.
        await using var command = Command(db, "INSERT INTO \"SyncOutbox\"(\"TableName\",\"RecordKey\",\"Body\") SELECT $table,record_key,record_body FROM (" +
            SyncTriggerSql.ReadLiveExistingAliased(table) + ") WHERE record_key=$key;");
        Parameter(command, "$table", key.Table); Parameter(command, "$key", key.Key);
        if (await command.ExecuteNonQueryAsync(ct) != 1) throw new InvalidDataException("The rebased personal change is unavailable.");
    }
    private static string KeyPredicate(DbCommand command, SyncTable table, SyncRecordKey key)
    {
        if (table.Singleton) return "1=1";
        using var source = JsonDocument.Parse(key.Key); var clauses = new List<string>();
        for (var index = 0; index < table.PrimaryKey.Length; index++)
        {
            var cell = source.RootElement[index]; var name = "$k" + index;
            Parameter(command, name, cell.ValueKind == JsonValueKind.Number ? (object)cell.GetInt64() : cell.GetString());
            clauses.Add(Quote(table.PrimaryKey[index]) + "=" + name);
        }
        return string.Join(" AND ", clauses);
    }
    private static async Task<bool> HasAuditColumnAsync(ZananceDbContext db, string table, CancellationToken ct)
    {
        await using var command = Command(db, "SELECT 1 FROM pragma_table_info($table) WHERE name='UpdatedAt';");
        Parameter(command, "$table", table); return await command.ExecuteScalarAsync(ct) is not null;
    }
    private static async Task<string[]> DropSwapIndexesAsync(ZananceDbContext db, HashSet<string> changed, CancellationToken ct)
    {
        var sqls = new List<string>();
        foreach (var table in Core.Sharing.HouseholdFinancialSchema.Manifest.Tables.Where(table => changed.Contains(table.Name)))
            foreach (var unique in table.UniqueKeys)
            {
                var name = "IX_" + table.Name + "_" + string.Join('_', unique.Columns);
                var expected = "CREATE UNIQUE INDEX " + Quote(name) + " ON " + Quote(table.Name) + " (" + string.Join(", ", unique.Columns.Select(Quote)) + ")" +
                    (unique.FullSettlementOnly ? " WHERE \"ScheduleId\" IS NOT NULL AND \"IsPartialPayment\" = 0" : "");
                await using var query = Command(db, "SELECT sql FROM sqlite_schema WHERE type='index' AND name=$name AND tbl_name=$table;");
                Parameter(query, "$name", name); Parameter(query, "$table", table.Name);
                if (await query.ExecuteScalarAsync(ct) is not string actual || Compact(actual) != Compact(expected))
                    throw new InvalidDataException("An original personal sync unique constraint differs from its reviewed contract.");
                var drop = "DROP INDEX " + Quote(name) + ";";
                await db.Database.ExecuteSqlRawAsync(drop, ct); sqls.Add(expected + ";");
            }
        return sqls.ToArray();
    }
    private static string Compact(string sql) => string.Concat(sql.Where(character => !char.IsWhiteSpace(character) && character != ';'));
    private static string Quote(string name)
    {
        if (name.Length is < 1 or > 200 || name.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '_'))
            throw new InvalidDataException("The original relational identifier is invalid.");
        return "\"" + name + "\"";
    }
    private static Dictionary<string, int> DependencyOrder()
    {
        var result = new Dictionary<string, int>(StringComparer.Ordinal); var visiting = new HashSet<string>(StringComparer.Ordinal);
        foreach (var table in Core.Sharing.HouseholdFinancialSchema.Manifest.Tables) Visit(table);
        return result;
        int Visit(SyncTable table)
        {
            if (result.TryGetValue(table.Name, out var rank)) return rank;
            if (!visiting.Add(table.Name)) throw new InvalidDataException("The original financial dependency order is cyclic.");
            rank = table.ForeignKeys.Where(key => key.PrincipalTable != table.Name)
                .Select(key => Visit(SyncSchema.Describe(key.PrincipalTable)) + 1).DefaultIfEmpty(0).Max();
            visiting.Remove(table.Name); result[table.Name] = rank; return rank;
        }
    }
}
