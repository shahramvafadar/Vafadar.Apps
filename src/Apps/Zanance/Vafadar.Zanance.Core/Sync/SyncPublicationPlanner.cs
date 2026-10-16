using System.Text.Json;

namespace Vafadar.Zanance.Core.Sync;

/// <summary>Content-free finite planning failures; transport/storage adapters select their own response without exposing financial input.</summary>
public enum SyncPublicationFailure
{
    /// <summary>The retained graph has an invalid relationship without any actual causal submitted proposal.</summary>
    StoredGraphUnavailable,
    /// <summary>A complete linked review exceeds its finite context/response bound.</summary>
    ReviewBound,
}

/// <summary>A content-free refusal from the original common financial publication planner.</summary>
public sealed class SyncPublicationException(SyncPublicationFailure failure) : Exception("Financial publication planning was refused.")
{
    /// <summary>Typed failure only, never a financial body, credential or native exception.</summary>
    public SyncPublicationFailure Failure { get; } = failure;
}

/// <summary>Complete proposed graph or bounded explicit original conflicts, before any financial or remote mutation.</summary>
public sealed record SyncPublicationPlan(bool Succeeded, IReadOnlyDictionary<SyncRecordKey, SyncRecord> Projected,
    IReadOnlyList<SyncConflict> Conflicts);

/// <summary>Original linked-row/three-way/financial review algorithm, shared by readable personal service and encrypted household clients.</summary>
public static class SyncPublicationPlanner
{
    /// <summary>Plans the complete graph against authentic current/baseline versions; no algorithm, money or related choice is duplicated by an encrypted adapter.</summary>
    public static SyncPublicationPlan Plan(IReadOnlyDictionary<SyncRecordKey, SyncRecord> current,
        IReadOnlyDictionary<SyncRecordKey, SyncProposal> proposals, IReadOnlyDictionary<SyncRecordKey, SyncRecord> baselines,
        IReadOnlyDictionary<SyncRecordKey, SyncResolution> resolutions, long cursor, bool initializing, SyncSchemaManifest? contract = null)
    {
        ArgumentNullException.ThrowIfNull(current); ArgumentNullException.ThrowIfNull(proposals);
        ArgumentNullException.ThrowIfNull(baselines); ArgumentNullException.ThrowIfNull(resolutions);
        ArgumentOutOfRangeException.ThrowIfNegative(cursor);
        var projected = new Dictionary<SyncRecordKey, SyncRecord>(current);
        var conflicts = new List<SyncConflict>();
        // A group is one original financial write. Independent row approvals cannot select a hybrid of two writes.
        foreach (var group in proposals.Values.GroupBy(proposal => (Family: SyncConflictGrouping.Family(proposal.Record), Id: SyncConflictGrouping.Group(proposal.Record, proposal.Values) ??
            SyncConflictGrouping.Group(proposal.Record, current.GetValueOrDefault(proposal.Record)?.Values) ??
            SyncConflictGrouping.Group(proposal.Record, baselines.GetValueOrDefault(proposal.Record)?.Values))).Where(group => group.Key.Id is not null))
        {
            var choices = group.Where(proposal => resolutions.ContainsKey(proposal.Record)).Select(proposal => resolutions[proposal.Record]).ToArray();
            if (choices.Length == 0)
            {
                // Even disjoint field edits can belong to different complete holding/transfer writes.
                // Row-level merging must not silently combine their quantities, payments or fees.
                if (group.Any(proposal => !Same(proposal.Values, baselines.GetValueOrDefault(proposal.Record)?.Values)) &&
                    (group.Any(proposal => !Same(current.GetValueOrDefault(proposal.Record)?.Values, baselines.GetValueOrDefault(proposal.Record)?.Values)) ||
                        current.Values.Any(record => SyncConflictGrouping.Family(record.Record) == group.Key.Family && SyncConflictGrouping.Group(record.Record, record.Values) == group.Key.Id && !proposals.ContainsKey(record.Record))) &&
                    group.Any(proposal => !Same(proposal.Values, current.GetValueOrDefault(proposal.Record)?.Values)))
                    conflicts.AddRange(group.Select(proposal => new SyncConflict(proposal, baselines.GetValueOrDefault(proposal.Record),
                        current.GetValueOrDefault(proposal.Record), group.Key.Family == "ledger" ? ["GroupId"] : [], "LinkedGroupReview", LinkedGroup: group.Key.Id)));
                continue;
            }
            var missingRemoteMember = choices.Any(choice => choice.UseProposal) && current.Values.Any(record =>
                SyncConflictGrouping.Family(record.Record) == group.Key.Family && SyncConflictGrouping.Group(record.Record, record.Values) == group.Key.Id && !proposals.ContainsKey(record.Record));
            if (choices.Length != group.Count() || choices.Select(choice => choice.UseProposal).Distinct().Count() != 1 || missingRemoteMember)
                conflicts.AddRange(group.Select(proposal => new SyncConflict(proposal, baselines.GetValueOrDefault(proposal.Record),
                    current.GetValueOrDefault(proposal.Record), group.Key.Family == "ledger" ? ["GroupId"] : [], "LinkedGroupReview", LinkedGroup: group.Key.Id)));
        }
        if (conflicts.Count != 0) return Refused(conflicts);
        foreach (var proposal in proposals.Values)
        {
            current.TryGetValue(proposal.Record, out var remote);
            var baseline = baselines.GetValueOrDefault(proposal.Record);
            JsonElement? selected;
            if (resolutions.TryGetValue(proposal.Record, out var resolution))
            {
                var counterpart = UniqueCounterpart(proposal, current.Values);
                var related = resolution.RelatedRecord is { } key ? current.GetValueOrDefault(key) : null;
                var expectedContext = ReviewContext(new(proposal, baseline, remote, [], "ReviewStale", RelatedRecord: counterpart ?? related));
                if (resolution.ReviewedVersion != (remote?.Version ?? 0) ||
                    !SameContext(resolution.ContextVersions, expectedContext) ||
                    resolution.RelatedVersion is { } version && version != (related?.Version ?? 0) ||
                    counterpart is not null && (resolution.RelatedRecord != counterpart.Record || resolution.RelatedVersion != counterpart.Version))
                {
                    conflicts.Add(new(proposal, baseline, remote, [], "ReviewStale",
                        ExistingSettlement: counterpart?.Record.Table == "Entries" ? counterpart.Record : null,
                        RelatedRecord: counterpart ?? related)); continue;
                }
                selected = resolution.UseProposal ? proposal.Values : remote?.Values;
            }
            else
            {
                if (proposal.BaseVersion > 0 && baseline is null)
                { conflicts.Add(new(proposal, null, remote, [], "BaselineUnavailable")); continue; }
                var merged = SyncRowMerge.Merge(baseline?.Values, remote?.Values, proposal.Values, remote is { Values: null },
                    baseline is { Values: null } && baseline.Version == remote?.Version);
                if (merged.Conflicts.Count != 0)
                { conflicts.Add(new(proposal, baseline, remote, merged.Conflicts, "CompetingChanges")); continue; }
                selected = merged.Values;
            }
            if (remote is null && selected is null) projected.Remove(proposal.Record);
            else projected[proposal.Record] = new(proposal.Record, remote?.Version ?? 0, selected);
        }
        if (conflicts.Count != 0) return Refused(conflicts);
        var newEntries = projected.Values.Where(row => row.Record.Table == "Entries" && row.Values is not null &&
                (!current.TryGetValue(row.Record, out var old) || old.Values is null))
            .Select(row => row.Values!.Value.GetProperty("Id").GetGuid()).ToHashSet();
        var issues = SyncFinancialValidation.Validate(current.Values.ToArray(), projected.Values.ToArray(), initializing ? null : newEntries, contract);
        if (issues.Count != 0)
        {
            foreach (var issue in issues)
            {
                var proposal = FindCausalProposal(issue, proposals, projected);
                if (proposal is null) throw new SyncPublicationException(SyncPublicationFailure.StoredGraphUnavailable);
                conflicts.Add(new(proposal, null, current.GetValueOrDefault(proposal.Record), issue.Fields, issue.Reason,
                    ExistingSettlement: issue.Reason == "OccurrenceAlreadySettled" && issue.RelatedRecord is { } settlement && current.ContainsKey(settlement) ? settlement : null,
                    RelatedRecord: issue.RelatedRecord is { } related && current.TryGetValue(related, out var relatedRecord) ? relatedRecord
                        : issue.Record == proposal.Record ? null : current.GetValueOrDefault(issue.Record)));
            }
            return Refused(conflicts.DistinctBy(conflict => conflict.Proposal.Record));
        }
        return new(true, projected, []);

        SyncPublicationPlan Refused(IEnumerable<SyncConflict> rows)
        {
            IReadOnlyList<SyncConflict> page;
            try { page = SyncConflictGrouping.Page(SyncConflictGrouping.Expand(rows, proposals, current, baselines)); }
            catch (InvalidDataException) { throw new SyncPublicationException(SyncPublicationFailure.ReviewBound); }
            var bounded = page.Select(conflict => conflict with { Context = ReviewContext(conflict) }).ToArray();
            var response = new SyncCommitResult(false, cursor, bounded);
            if (JsonSerializer.SerializeToUtf8Bytes(response, PersonalSyncJsonContext.Default.SyncCommitResult).Length > 2 * 1024 * 1024)
                throw new SyncPublicationException(SyncPublicationFailure.ReviewBound);
            return new(false, new Dictionary<SyncRecordKey, SyncRecord>(current), bounded);
        }
        IReadOnlyList<SyncRecord> ReviewContext(SyncConflict conflict)
        {
            try { return SyncReviewRelationships.Capture(conflict, current); }
            catch (InvalidDataException) { throw new SyncPublicationException(SyncPublicationFailure.ReviewBound); }
        }
        static bool SameContext(IReadOnlyList<SyncRecordVersion>? actual, IReadOnlyList<SyncRecord> expected) =>
            (actual?.Count ?? 0) == expected.Count && expected.All(row => actual!.Any(item => item.Record == row.Record && item.Version == row.Version));
    }

    /// <summary>Finds an actual submitted dependency responsible for an original financial validation issue.</summary>
    private static SyncProposal? FindCausalProposal(SyncGraphIssue issue, IReadOnlyDictionary<SyncRecordKey, SyncProposal> proposals,
        IReadOnlyDictionary<SyncRecordKey, SyncRecord> projected)
    {
        if (proposals.TryGetValue(issue.Record, out var direct)) return direct;
        if (issue.RelatedRecord is { } related && proposals.TryGetValue(related, out var cause)) return cause;
        // Domain validators can report an existing child after its account, category or refund
        // original changed. Bind review to an actual submitted relationship, never an invented proposal.
        if (projected.GetValueOrDefault(issue.Record)?.Values is { } body)
        {
            var table = SyncSchema.Describe(issue.Record.Table);
            foreach (var dependency in table.ForeignKeys)
            {
                if (dependency.Columns.Any(name => body.GetProperty(name).ValueKind == JsonValueKind.Null)) continue;
                var key = new SyncRecordKey(dependency.PrincipalTable, SyncGraphValidation.KeyOf(body, dependency.Columns));
                if (proposals.TryGetValue(key, out cause)) return cause;
            }
        }
        foreach (var proposal in proposals.Values.Where(proposal => proposal.Values is not null))
        {
            var table = SyncSchema.Describe(proposal.Record.Table);
            foreach (var dependency in table.ForeignKeys.Where(dependency => dependency.PrincipalTable == issue.Record.Table))
                if (!dependency.Columns.Any(name => proposal.Values!.Value.GetProperty(name).ValueKind == JsonValueKind.Null) &&
                    SyncGraphValidation.KeyOf(proposal.Values!.Value, dependency.Columns) == issue.Record.Key) return proposal;
        }
        return null;
    }

    /// <summary>Finds the existing original unique settlement/related identity that an explicit review must also authorize.</summary>
    private static SyncRecord? UniqueCounterpart(SyncProposal proposal, IEnumerable<SyncRecord> current)
    {
        // A duplicate's proposed id can stay absent forever. Its zero version cannot protect an approval
        // of the different existing payment/state, so require that actual counterpart's identity/version too.
        if (proposal.Values is not { } values) return null;
        var table = SyncSchema.Describe(proposal.Record.Table);
        foreach (var unique in table.UniqueKeys)
        {
            if (unique.FullSettlementOnly && (values.GetProperty("ScheduleId").ValueKind == JsonValueKind.Null ||
                values.GetProperty("IsPartialPayment").GetInt64() != 0)) continue;
            if (unique.Columns.Any(column => values.GetProperty(column).ValueKind == JsonValueKind.Null)) continue;
            foreach (var row in current.Where(row => row.Record.Table == table.Name && row.Record != proposal.Record && row.Values is not null))
            {
                var candidate = row.Values!.Value;
                if (unique.FullSettlementOnly && (candidate.GetProperty("ScheduleId").ValueKind == JsonValueKind.Null ||
                    candidate.GetProperty("IsPartialPayment").GetInt64() != 0)) continue;
                if (unique.Columns.All(column => JsonElement.DeepEquals(values.GetProperty(column), candidate.GetProperty(column)))) return row;
            }
        }
        return null;
    }

    /// <summary>Original nullable JSON-value equality, without serializing or altering numeric values.</summary>
    private static bool Same(JsonElement? left, JsonElement? right) => left is null && right is null ||
        left is { } first && right is { } second && JsonElement.DeepEquals(first, second);
}
