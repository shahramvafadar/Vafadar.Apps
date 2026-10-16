using System.Security.Cryptography;
using System.Text.Json;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.Data.Sharing;

/// <summary>Complete original signed metadata after every literal ciphertext part has reached the caller's bounded protected sink.</summary>
internal sealed record HouseholdSealedPublication(HouseholdOpenRound Open, HouseholdCommitRound Commit, SyncPublicationPlan Plan,
    long FinancialCursor, IReadOnlyList<HouseholdPartCommitment> Parts, long CiphertextBytes);

/// <summary>Complete immutable prepared ciphertext, to be durably journaled before upload; no financial plaintext is a service request.</summary>
public sealed class HouseholdPreparedPublication
{
    internal HouseholdPreparedPublication(HouseholdOpenRound open, HouseholdOpaquePart[] parts, HouseholdCommitRound commit,
        SyncPublicationPlan plan, long financialCursor)
    { Open = open; Parts = Array.AsReadOnly(parts); Commit = commit; Plan = plan; FinancialCursor = financialCursor; }
    /// <summary>Actual observed cursor/membership/epoch and immutable logical-operation/attempt identity.</summary>
    public HouseholdOpenRound Open { get; }
    /// <summary>Complete ordered standard ciphertext, never regenerated for an acknowledged attempt.</summary>
    public IReadOnlyList<HouseholdOpaquePart> Parts { get; }
    /// <summary>Exact signed complete manifest, retained across an unobserved successful response.</summary>
    public HouseholdCommitRound Commit { get; }
    /// <summary>Original common financial projection or explicit conflicts; a conflict has no ciphertext parts.</summary>
    public SyncPublicationPlan Plan { get; }
    /// <summary>Original per-financial-row sequence expected after this atomic publication, independent of opaque blob parts.</summary>
    public long FinancialCursor { get; }
}

/// <summary>Original verified household financial graph/history. Protected persistence and final app application are separate mandatory boundaries.</summary>
public sealed partial class HouseholdFinancialReplica
{
    private Dictionary<SyncRecordKey, SyncRecord> current = [];
    private Dictionary<(SyncRecordKey Record, long Version), SyncRecord> history = [];
    private Dictionary<string, long> opaqueVersions = new(StringComparer.Ordinal);
    /// <summary>Household resource fixed before receiving any financial packet.</summary>
    public Guid SpaceId { get; }
    /// <summary>Last completely verified atomic ciphertext cursor.</summary>
    public long Cursor { get; private set; }
    /// <summary>Last completely verified original financial-row sequence; blob pieces do not consume financial versions.</summary>
    public long FinancialCursor { get; private set; }
    /// <summary>Exact signed publication head, not a service-supplied unauthenticated checkpoint.</summary>
    public string PublicationHead { get; private set; } = new('0', 64);

    /// <summary>Starts an empty verified feed at a fixed actual room; it does not create a financial app/profile or access local storage.</summary>
    public HouseholdFinancialReplica(Guid spaceId)
    { if (spaceId == Guid.Empty) throw new ArgumentException("An exact household space is required.", nameof(spaceId)); SpaceId = spaceId; }
    /// <summary>Original protected journal construction additionally permits bounded historical-content reads after resident buffers are retired.</summary>
    internal HouseholdFinancialReplica(Guid spaceId, IHouseholdAttachmentArchive archive, IHouseholdFinancialArchive financialArchive) : this(spaceId)
    { this.archive = archive; this.financialArchive = financialArchive; }

    /// <summary>Returns detached original immutable row values; no caller can alter this replica's dictionary membership.</summary>
    public IReadOnlyDictionary<SyncRecordKey, SyncRecord> Snapshot()
    { ObjectDisposedException.ThrowIf(disposed, this); return new Dictionary<SyncRecordKey, SyncRecord>(current); }

    /// <summary>Supplies an exact bounded verified original row sequence to the unchanged local financial apply mechanism; ciphertext/blob counts are not financial versions.</summary>
    public SyncFeedPage FinancialFeed(long after, long through)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (financialArchive is not null || after < 0 || through < after || through > FinancialCursor) throw Refusal();
        var rows = new List<SyncRecord>(); long size = 0; var next = after;
        foreach (var row in history.Values.Where(row => row.Version > after && row.Version <= through).OrderBy(row => row.Version).Take(128))
        {
            if (row.Version != next + 1) throw Refusal();
            var length = JsonSerializer.SerializeToUtf8Bytes(row, PersonalSyncJsonContext.Default.SyncRecord).Length;
            if (length > HouseholdFinancialFraming.MaximumRecordBytes) throw Refusal();
            if (rows.Count != 0 && size + length > 2 * 1024 * 1024) break;
            size += length; rows.Add(row); next = row.Version;
        }
        if (next != through && rows.Count == 0) throw Refusal();
        return new(rows, next, through, next == through);
    }

    /// <summary>Uses original linked-row/ledger/three-way review, then seals exact selected rows and attachment bytes for one observed CAS attempt.</summary>
    public HouseholdPreparedPublication Prepare(HouseholdVerifiedMembership membership, HouseholdEpochMaterial epoch, Guid authorDevice,
        SigningRecipientIdentity author, Guid operation, Guid attempt, IReadOnlyList<SyncProposal> proposals,
        IReadOnlyList<SyncResolution> resolutions, IReadOnlyDictionary<string, byte[]> outgoingAttachments, HouseholdCryptography cryptography)
    {
        var parts = new List<HouseholdOpaquePart>();
        var sealedMetadata = SealTo(membership, epoch, authorDevice, author, operation, attempt, proposals, resolutions,
            new MemoryAttachmentSource(outgoingAttachments), cryptography, parts.Add, CancellationToken.None);
        return new(sealedMetadata.Open, parts.ToArray(), sealedMetadata.Commit, sealedMetadata.Plan, sealedMetadata.FinancialCursor);
    }

    /// <summary>The same original financial planner/sealer emits one bounded ciphertext part at a time; completion metadata exists only after every source hash and part passed.</summary>
    internal HouseholdSealedPublication SealTo(HouseholdVerifiedMembership membership, HouseholdEpochMaterial epoch, Guid authorDevice,
        SigningRecipientIdentity author, Guid operation, Guid attempt, IReadOnlyList<SyncProposal> proposals,
        IReadOnlyList<SyncResolution> resolutions, IHouseholdAttachmentSource outgoingAttachments, HouseholdCryptography cryptography,
        Action<HouseholdOpaquePart> sink, CancellationToken ct, IReadOnlyDictionary<SyncRecordKey, SyncRecord>? archivedBaselines = null, Func<string, long>? opaqueVersion = null)
    {
        ct.ThrowIfCancellationRequested();
        RequireScope(membership, epoch);
        var financialBoundary = membership.Snapshot();
        if (Cursor < financialBoundary.PublicationCursor || Cursor == financialBoundary.PublicationCursor && PublicationHead != financialBoundary.PublicationHead)
            throw Refusal();
        if (membership.Device(authorDevice, requireWrite: true) != author.Public || operation == Guid.Empty || attempt == Guid.Empty)
            throw Refusal();
        var proposed = ValidateProposals(proposals); var choices = ValidateResolutions(resolutions, proposed);
        var baselines = financialArchive is null ? proposed.Values.Where(row => row.BaseVersion > 0 && history.ContainsKey((row.Record, row.BaseVersion)))
            .ToDictionary(row => row.Record, row => history[(row.Record, row.BaseVersion)]) : archivedBaselines ?? throw Refusal();
        var plan = SyncPublicationPlanner.Plan(current, proposed, baselines, choices, FinancialCursor, Cursor == 0, HouseholdFinancialSchema.Manifest);
        var opening = new HouseholdOpenRound(authorDevice, operation, attempt, Cursor, membership.Version, epoch.Epoch, HouseholdFinancialSchema.Hash);
        if (!plan.Succeeded) return new(opening, new(authorDevice, ""), plan, FinancialCursor, [], 0);
        var parts = new List<HouseholdPartCommitment>(); var sequence = FinancialCursor; long encryptedBytes = 0;
        var changed = plan.Projected.Values.OrderBy(row => row.Record.Table, StringComparer.Ordinal).ThenBy(row => row.Record.Key, StringComparer.Ordinal)
            .Where(row => !Same(current.GetValueOrDefault(row.Record)?.Values, row.Values));
        var compact = changed.Take(129).ToArray();
        if (compact.Length > 128 || compact.Any(row => row.Values is { } values && System.Text.Encoding.UTF8.GetByteCount(values.GetRawText()) > 32 * 1024))
        {
            HouseholdRecordStream.Write(VersionedRows(), block => Add(new(1, HouseholdFinancialSchema.Hash, attempt, parts.Count,
                HouseholdFinancialContentKind.RecordStream, null, null, block), StreamAddress(epoch, block.Index)));
        }
        else
        {
            foreach (var row in compact)
            {
                var next = row with { Version = checked(++sequence) };
                var address = epoch.RecordAddress(row.Record.Table, row.Record.Key);
                Add(new(1, HouseholdFinancialSchema.Hash, attempt, parts.Count, HouseholdFinancialContentKind.Record, next, null), address);
            }
        }
        AddOutgoingAttachments(plan.Projected.Values, outgoingAttachments, epoch, attempt, Add, () => parts.Count, ct);
        if (parts.Count > HouseholdPublicationDigest.MaximumParts || encryptedBytes > HouseholdPublicationDigest.MaximumBytes)
            throw new SyncProfileBoundException();
        if (parts.Count == 0) return new(opening, new(authorDevice, ""), plan, sequence, [], 0);
        var manifest = HouseholdPublicationDigest.Manifest(parts);
        var statement = new HouseholdCommitStatement(1, SpaceId, authorDevice, operation, attempt, Cursor, checked(Cursor + 1), membership.Version,
            epoch.Epoch, PublicationHead, parts.Count, manifest);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(statement, HouseholdMetadataJsonContext.Default.HouseholdCommitStatement);
        var signed = StandardJoseSignatures.Sign(bytes, author, "zanance-household-commit+jwt");
        ct.ThrowIfCancellationRequested();
        return new(opening, new(authorDevice, signed), plan, sequence, parts.AsReadOnly(), encryptedBytes);

        IEnumerable<SyncRecord> VersionedRows()
        { foreach (var row in changed) yield return row with { Version = checked(++sequence) }; }

        void Add(HouseholdFinancialPayload payload, string address)
        {
            ct.ThrowIfCancellationRequested();
            if (parts.Count >= HouseholdPublicationDigest.MaximumParts) throw new SyncProfileBoundException();
            var clear = JsonSerializer.SerializeToUtf8Bytes(payload, HouseholdFinancialJsonContext.Default.HouseholdFinancialPayload);
            try
            {
                var encrypted = epoch.Encrypt(cryptography, new(SpaceId, epoch.Epoch, operation, HouseholdProtectionPurpose.Financial, "members"), clear, author);
                encryptedBytes += encrypted.Length;
                if (encryptedBytes > HouseholdPublicationDigest.MaximumBytes) throw new SyncProfileBoundException();
                var part = new HouseholdOpaquePart(parts.Count, address, (opaqueVersion?.Invoke(address) ?? opaqueVersions.GetValueOrDefault(address)), encrypted, HouseholdPublicationDigest.Ciphertext(encrypted));
                sink(part); parts.Add(new(part.Index, part.RecordId, part.BaseVersion, part.Digest));
            }
            finally { CryptographicOperations.ZeroMemory(clear); }
        }
    }

    /// <summary>Verifies the complete signed ciphertext transaction and original complete financial graph before advancing any local replica state.</summary>
    public void Apply(HouseholdVerifiedMembership membershipAtPublication, HouseholdEpochMaterial epoch, HouseholdPublicationReceipt receipt,
        IReadOnlyList<HouseholdOpaqueEvent> events, HouseholdCryptography cryptography, HouseholdVerifiedMembership? nextMembership = null)
    {
        if (events is null || events.Count is < 1 or > HouseholdPublicationDigest.MaximumParts || events.Any(item => item is null)) throw Refusal();
        ApplyOrdered(membershipAtPublication, epoch, receipt, events.OrderBy(item => item.Index), events.Count, cryptography, nextMembership, CancellationToken.None);
    }

    /// <summary>Uses the same complete verifier with a bounded original native cursor; ciphertext packets are never materialized as a whole.</summary>
    internal void ApplyOrdered(HouseholdVerifiedMembership membershipAtPublication, HouseholdEpochMaterial epoch, HouseholdPublicationReceipt receipt,
        IEnumerable<HouseholdOpaqueEvent> events, int count, HouseholdCryptography cryptography, HouseholdVerifiedMembership? nextMembership, CancellationToken ct,
        Func<string, long>? opaqueVersion = null, Action<SyncRecord, int, long, int>? recordSource = null)
    {
        RequireScope(membershipAtPublication, epoch);
        if (events is null || count is < 1 or > HouseholdPublicationDigest.MaximumParts) throw Refusal();
        ct.ThrowIfCancellationRequested();
        using var ordered = events.GetEnumerator();
        if (!ordered.MoveNext() || ordered.Current is not { } first) throw Refusal();
        ct.ThrowIfCancellationRequested();
        var commit = HouseholdTrustChain.Publication(membershipAtPublication, receipt, first.DeviceId, first.OperationId, first.AttemptId, Cursor, PublicationHead);
        if (nextMembership is not null) HouseholdTrustChain.RequirePublicationHorizon(membershipAtPublication, nextMembership, receipt, commit.Cursor);
        if (receipt.Parts.Length != count) throw Refusal();
        var projected = new Dictionary<SyncRecordKey, SyncRecord>(current);
        var nextHistory = financialArchive is null ? new Dictionary<(SyncRecordKey Record, long Version), SyncRecord>(history) : null;
        var nextOpaque = opaqueVersion is null ? new Dictionary<string, long>(opaqueVersions, StringComparer.Ordinal) : null;
        var nextBlobs = CopyAttachments(); var sequence = FinancialCursor;
        long newBlobBytes = 0; var projectedWeight = current.Values.Sum(Weight);
        var sources = new List<HouseholdAttachmentLocator>();
        var sourceKeys = new HashSet<(string Hash, int Index)>();
        var seenRecords = new HashSet<SyncRecordKey>(); var legacyRecords = false;
        var eventIndex = -1; var streamStart = -1;
        HouseholdRecordStreamReader stream = null!;
        using var ownedStream = stream = new HouseholdRecordStreamReader(AcceptRecord);
        try
        {
        for (var index = 0; index < count; index++)
        {
            ct.ThrowIfCancellationRequested();
            if (index != 0 && !ordered.MoveNext()) throw Refusal();
            ct.ThrowIfCancellationRequested();
            eventIndex = index;
            var item = ordered.Current ?? throw Refusal(); var expected = receipt.Parts[index];
            if (item.Index != index || item.RoundId != receipt.RoundId || item.Cursor != commit.Cursor || item.KeyEpoch != epoch.Epoch ||
                item.MembershipVersion != membershipAtPublication.Version || item.DeviceId != first.DeviceId || item.OperationId != first.OperationId ||
                item.AttemptId != first.AttemptId || item.RecordId != expected.RecordId || item.BaseVersion != expected.BaseVersion || item.Digest != expected.Digest ||
                HouseholdPublicationDigest.Ciphertext(item.Ciphertext) != item.Digest || item.BaseVersion != (opaqueVersion?.Invoke(item.RecordId) ?? opaqueVersions.GetValueOrDefault(item.RecordId))) throw Refusal();
            var clear = epoch.Decrypt(cryptography, new(SpaceId, epoch.Epoch, first.OperationId, HouseholdProtectionPurpose.Financial, "members"),
                item.Ciphertext, membershipAtPublication.Device(first.DeviceId, requireWrite: true));
            try
            {
                var payload = StrictJsonPayload.Deserialize(clear, HouseholdFinancialJsonContext.Default.HouseholdFinancialPayload, HouseholdCryptography.MaximumPlaintextBytes);
                if (payload.Format != 1 || payload.SchemaHash != HouseholdFinancialSchema.Hash || payload.AttemptId != first.AttemptId || payload.Index != index) throw Refusal();
                if (payload.Kind == HouseholdFinancialContentKind.Record)
                {
                    if (payload.Record is not { } record || payload.Attachment is not null || payload.Stream is not null || payload.Blobs is not null || stream.Started) throw Refusal();
                    if (epoch.RecordAddress(record.Record.Table, record.Record.Key) != item.RecordId) throw Refusal();
                    legacyRecords = true; AcceptRecord(record);
                }
                else if (payload.Kind == HouseholdFinancialContentKind.Attachment)
                {
                    if (payload.Record is not null || payload.Attachment is not { } blob || payload.Stream is not null || payload.Blobs is not null || stream.Started && !stream.Complete) throw Refusal();
                    Source(blob, item, -1);
                    AcceptBlob(blob, item.RecordId);
                }
                else if (payload.Kind == HouseholdFinancialContentKind.RecordStream)
                {
                    if (payload.Record is not null || payload.Attachment is not null || payload.Stream is not { } block || payload.Blobs is not null || legacyRecords ||
                        StreamAddress(epoch, block.Index) != item.RecordId) throw Refusal();
                    if (!stream.Started) streamStart = index;
                    stream.Add(block);
                }
                else if (payload.Kind == HouseholdFinancialContentKind.AttachmentBatch)
                {
                    if (payload.Record is not null || payload.Attachment is not null || payload.Stream is not null ||
                        stream.Started && !stream.Complete || BatchAddress(epoch, index) != item.RecordId || payload.Blobs is not { Length: >= 1 and <= 512 } blobs) throw Refusal();
                    try
                    {
                        if (blobs.Any(blob => blob is null || blob.Bytes is null || blob.Bytes.Length > 4096) ||
                            blobs.Select(blob => (blob.Sha256, blob.Index)).Distinct().Count() != blobs.Length) throw Refusal();
                        for (var position = 0; position < blobs.Length; position++)
                        {
                            var blob = blobs[position]; Source(blob, item, position);
                            AcceptBlob(blob, epoch.RecordAddress("attachment-piece", FormattableString.Invariant($"{blob.Sha256}:{blob.Index}")));
                        }
                    }
                    finally { foreach (var blob in blobs) if (blob?.Bytes is { } bytes) CryptographicOperations.ZeroMemory(bytes); }
                }
                else throw Refusal();
                if (nextOpaque is not null) nextOpaque[item.RecordId] = commit.Cursor;
            }
            catch (JsonException) { throw Refusal(); }
            finally { CryptographicOperations.ZeroMemory(clear); }
        }
        if (ordered.MoveNext() || stream.Started && !stream.Complete) throw Refusal();
        ct.ThrowIfCancellationRequested();
        var newEntries = projected.Values.Where(row => row.Record.Table == "Entries" && row.Values is not null &&
            current.GetValueOrDefault(row.Record)?.Values is null).Select(row => row.Values!.Value.GetProperty("Id").GetGuid()).ToHashSet();
        if (SyncFinancialValidation.Validate(current.Values.ToArray(), projected.Values.ToArray(), Cursor == 0 ? null : newEntries, HouseholdFinancialSchema.Manifest).Count != 0) throw Refusal();
        var verifiedBlobs = VerifyAttachments(projected.Values, nextBlobs);
        // Only complete verified graph/content replaces state. A rejected author packet changes none of these references/cursors.
        var nextHead = HouseholdTrustChain.Digest(receipt.SignedCommit);
        var retired = archive is null ? [] : UnreferencedAttachmentKeys(projected.Values, nextBlobs);
        attachmentSources.EnsureCapacity(checked(attachmentSources.Count + sources.Count));
        ct.ThrowIfCancellationRequested();
        current = projected; if (nextHistory is not null) history = nextHistory; if (nextOpaque is not null) opaqueVersions = nextOpaque; attachments = nextBlobs; attachmentLengths = verifiedBlobs;
        FinancialCursor = sequence; Cursor = commit.Cursor; PublicationHead = nextHead;
        attachmentSources.AddRange(sources);
        foreach (var key in retired) { CryptographicOperations.ZeroMemory(attachments[key].Bytes); attachments.Remove(key); }
        }
        catch { ClearRejectedAttachmentCandidates(nextBlobs); throw; }

        void AcceptRecord(SyncRecord record)
        {
            if (record.Version != checked(++sequence) || !seenRecords.Add(record.Record)) throw Refusal();
            RequireRecord(record);
            if (!projected.ContainsKey(record.Record) && projected.Count >= SyncGraphValidation.MaximumRows) throw Refusal();
            projectedWeight = checked(projectedWeight - Weight(projected.GetValueOrDefault(record.Record)) + Weight(record));
            // A valid replacement can temporarily coexist with rows deleted later in the packet.
            // Two complete original profile bounds cover that candidate, never arbitrary wire bytes.
            if (projectedWeight > 2 * SyncGraphValidation.MaximumProfileBytes) throw Refusal();
            projected[record.Record] = record; nextHistory?.Add((record.Record, record.Version), record);
            recordSource?.Invoke(record, streamStart >= 0 ? streamStart : eventIndex,
                streamStart >= 0 ? stream.RecordOffset : -1, streamStart >= 0 ? stream.RecordLength : 0);
        }
        void AcceptBlob(HouseholdAttachmentPiece piece, string address)
        {
            if (piece.Bytes is null) throw Refusal();
            if (!nextBlobs.ContainsKey((piece.Sha256, piece.Index)))
            {
                newBlobBytes = checked(newBlobBytes + piece.Bytes.Length);
                if (newBlobBytes > SyncGraphValidation.MaximumProfileBytes) { CryptographicOperations.ZeroMemory(piece.Bytes); throw Refusal(); }
            }
            AcceptAttachment(nextBlobs, piece, epoch, address);
        }
        void Source(HouseholdAttachmentPiece piece, HouseholdOpaqueEvent item, int element)
        {
            if (!SyncProtocolDigest.IsDigest(piece.Sha256) || piece.Bytes is null) throw Refusal();
            if (archive is null || attachmentLengths.ContainsKey(piece.Sha256)) return;
            if (!sourceKeys.Add((piece.Sha256, piece.Index))) throw Refusal();
            sources.Add(new(piece.Sha256, piece.Length, piece.Index, item.Cursor, item.Index, element, Convert.ToHexStringLower(SHA256.HashData(piece.Bytes))));
        }
    }

    /// <summary>Transient staging weight follows the original body/declared-blob accounting; it does not replace the final financial validator.</summary>
    private static long Weight(SyncRecord? row)
    {
        if (row?.Values is not { } body) return 0;
        long bytes = System.Text.Encoding.UTF8.GetByteCount(body.GetRawText());
        foreach (var column in HouseholdFinancialSchema.Manifest.Tables.Single(table => table.Name == row.Record.Table).Columns.Where(column => column.Kind == "blob"))
        { var value = body.GetProperty(column.Name); if (value.ValueKind != JsonValueKind.Null) bytes = checked(bytes + value.GetProperty("length").GetInt64()); }
        return bytes;
    }

    /// <summary>Stable opaque stream slots hide financial row identities while retaining exact original ciphertext predecessors under the global signed CAS cursor.</summary>
    private static string StreamAddress(HouseholdEpochMaterial epoch, int index) => epoch.RecordAddress("financial-stream",
        index.ToString(System.Globalization.CultureInfo.InvariantCulture));
    /// <summary>Stable opaque small-attachment slots contain no public plain attachment hash or filename.</summary>
    private static string BatchAddress(HouseholdEpochMaterial epoch, int index) => epoch.RecordAddress("attachment-batch",
        index.ToString(System.Globalization.CultureInfo.InvariantCulture));

    /// <summary>Validates exact original row identities/scalars before selecting an authenticated baseline or financial choice.</summary>
    private static Dictionary<SyncRecordKey, SyncProposal> ValidateProposals(IReadOnlyList<SyncProposal> rows)
    {
        if (rows is null || rows.Count > SyncGraphValidation.MaximumRows) throw Refusal();
        var result = new Dictionary<SyncRecordKey, SyncProposal>();
        foreach (var row in rows)
        {
            if (row is null || row.BaseVersion < 0) throw Refusal();
            RequireRecord(new(row.Record, row.BaseVersion, row.Values));
            if (!result.TryAdd(row.Record, row)) throw Refusal();
        }
        return result;
    }
    /// <summary>Reuses original complete reviewed identity/version/context semantics; a malformed explicit choice never becomes financial input.</summary>
    private static Dictionary<SyncRecordKey, SyncResolution> ValidateResolutions(IReadOnlyList<SyncResolution> rows, IReadOnlyDictionary<SyncRecordKey, SyncProposal> proposals)
    {
        if (rows is null || rows.Count > SyncGraphValidation.MaximumRows) throw Refusal();
        var result = new Dictionary<SyncRecordKey, SyncResolution>();
        foreach (var row in rows)
            if (row is null || row.Record is null || !proposals.ContainsKey(row.Record) || row.ReviewedVersion < 0 || row.RelatedVersion < 0 ||
                (row.RelatedRecord is null) != (row.RelatedVersion is null) || row.RelatedRecord is { } related && !ValidKey(related) ||
                row.ContextVersions is { } context && (context.Count > 128 || context.Any(item => item is null || item.Record is null || item.Version < 0 || !ValidKey(item.Record)) ||
                    context.Select(item => item.Record).Distinct().Count() != context.Count) || !result.TryAdd(row.Record, row)) throw Refusal();
        return result;
    }
    /// <summary>Checks the shipped relational contract without accepting external SQL/table names or reinterpreting money.</summary>
    private static void RequireRecord(SyncRecord record)
    {
        if (record.Record is null || record.Version < 0 || !ValidKey(record.Record)) throw Refusal();
        var table = HouseholdFinancialSchema.Manifest.Tables.Single(row => row.Name == record.Record.Table);
        if (record.Values is { } values && SyncGraphValidation.ValidateRow(table, record.Record, values).Count != 0) throw Refusal();
    }
    /// <summary>Only original canonical table/key shapes are accepted inside decrypted financial content.</summary>
    private static bool ValidKey(SyncRecordKey key)
    {
        if (key.Table is null || key.Key is null || key.Key.Length > 2048) return false;
        var table = HouseholdFinancialSchema.Manifest.Tables.SingleOrDefault(row => row.Name == key.Table);
        return table is not null && SyncGraphValidation.ValidIdentity(table, key);
    }
    /// <summary>Membership/key material cannot redirect this verified graph into another room or generation.</summary>
    private void RequireScope(HouseholdVerifiedMembership membership, HouseholdEpochMaterial epoch)
    { ObjectDisposedException.ThrowIf(disposed, this); if (membership.SpaceId != SpaceId || membership.Epoch != epoch.Epoch || membership.Snapshot().KeyCommitment != epoch.KeyCommitment ||
        membership.Snapshot().AddressKeyCommitment != epoch.AddressKeyCommitment) throw Refusal(); }
    /// <summary>Original nullable scalar equality without numeric conversion or lost sign/precision.</summary>
    private static bool Same(JsonElement? left, JsonElement? right) => left is null ? right is null : right is not null && JsonElement.DeepEquals(left.Value, right.Value);
    /// <summary>Content-free refusal without raw financial content, ciphertext, keys or native exception messages.</summary>
    private static CryptographicException Refusal() => new("Household financial publication was refused.");
}
