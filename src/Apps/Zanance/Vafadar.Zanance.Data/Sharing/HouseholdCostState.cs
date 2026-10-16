using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.Data.Sharing;

/// <summary>Content-free domain refusal; callers translate stable issue codes, never exception text containing finance.</summary>
public sealed class HouseholdCostRejectedException(IReadOnlyList<HouseholdCostIssue> issues)
    : InvalidOperationException("The complete household cost graph is not valid.")
{
    /// <summary>Exact validation causes without amounts, names or private notes.</summary>
    public IReadOnlyList<HouseholdCostIssue> Issues { get; } = Array.AsReadOnly(issues.ToArray());
}

/// <summary>One actual transaction's complete costs and only the cash movements they explicitly reference.</summary>
internal sealed record HouseholdCostState(HouseholdContact[] Contacts, HouseholdCost[] Costs,
    HouseholdCostAllocation[] Allocations, HouseholdSettlement[] Settlements,
    IReadOnlyDictionary<Guid, LedgerEntry> Entries, IReadOnlyDictionary<Guid, Account> Accounts)
{
    /// <summary>Reads under the caller's retained transaction; native commit validation never reads another profile.</summary>
    internal static HouseholdCostState Read(ZananceDbContext db)
    {
        var contacts = db.HouseholdContacts.AsNoTracking().Take(SyncGraphValidation.MaximumRows + 1).ToArray();
        var costs = db.HouseholdCosts.AsNoTracking().Take(SyncGraphValidation.MaximumRows + 1).ToArray();
        var allocations = db.HouseholdAllocations.AsNoTracking().Take(SyncGraphValidation.MaximumRows + 1).ToArray();
        var settlements = db.HouseholdSettlements.AsNoTracking().Take(SyncGraphValidation.MaximumRows + 1).ToArray();
        if ((long)contacts.Length + costs.Length + allocations.Length + settlements.Length > SyncGraphValidation.MaximumRows)
            throw new InvalidDataException("The household graph exceeds the supported live profile bound.");
        var entries = new Dictionary<Guid, LedgerEntry>();
        foreach (var ids in costs.Select(row => row.EntryId).Concat(settlements.Select(row => row.EntryId))
            .Where(id => id.HasValue).Select(id => id!.Value).Distinct().Chunk(256))
            foreach (var row in db.Entries.AsNoTracking().Where(row => ids.Contains(row.Id)).ToArray()) entries.Add(row.Id, row);
        var accounts = new Dictionary<Guid, Account>();
        foreach (var ids in entries.Values.SelectMany(row => row.ToAccountId is { } destination ? new[] { row.AccountId, destination } : [row.AccountId])
            .Distinct().Chunk(256))
            foreach (var row in db.Accounts.AsNoTracking().Where(row => ids.Contains(row.Id)).ToArray()) accounts.Add(row.Id, row);
        return new(contacts, costs, allocations, settlements, entries, accounts);
    }

    /// <summary>Runs the original exact minor-unit/refund/link rules after all compound SQL and before commit.</summary>
    internal void DemandValid(IReadOnlySet<Guid>? newCosts = null, IReadOnlySet<Guid>? newSettlements = null,
        IReadOnlySet<(Guid CostId, Guid ContactId)>? newAllocations = null)
    {
        var issues = HouseholdCostLedger.Validate(Contacts, Costs, Allocations, Settlements, Entries, Accounts, newCosts, newSettlements, newAllocations);
        if (issues.Count != 0) throw new HouseholdCostRejectedException(issues);
    }

    /// <summary>Opaque optimistic review token over explicit cost facts, excluding operational timestamps and device state.</summary>
    internal string Fingerprint()
    {
        // BinaryWriter length-prefixes every string, so separate values cannot produce an ambiguous concatenation.
        // Incremental hashing keeps even a large retained graph out of a second serialized memory buffer.
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        using var stream = new HashStream(hash); using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        writer.Write(1); writer.Write(Contacts.Length);
        foreach (var row in Contacts.OrderBy(row => row.Id))
        { writer.Write(row.Id.ToByteArray()); writer.Write(row.Name); Text(row.IdentityId); writer.Write(row.IsArchived); }
        writer.Write(Costs.Length);
        foreach (var row in Costs.OrderBy(row => row.Id))
        {
            writer.Write(row.Id.ToByteArray()); writer.Write((int)row.Kind); writer.Write(row.Date.DayNumber); writer.Write(row.Amount);
            writer.Write(row.CurrencyCode); Id(row.PaidByContactId); writer.Write((int)row.PaymentKind); Id(row.EntryId); Id(row.RefundOfCostId);
            writer.Write(row.Title); Text(row.Note);
        }
        writer.Write(Allocations.Length);
        foreach (var row in Allocations.OrderBy(row => row.CostId).ThenBy(row => row.ContactId))
        { writer.Write(row.CostId.ToByteArray()); writer.Write(row.ContactId.ToByteArray()); writer.Write(row.Amount); }
        writer.Write(Settlements.Length);
        foreach (var row in Settlements.OrderBy(row => row.Id))
        {
            writer.Write(row.Id.ToByteArray()); writer.Write(row.FromContactId.ToByteArray()); writer.Write(row.ToContactId.ToByteArray());
            writer.Write(row.Date.DayNumber); writer.Write(row.Amount); writer.Write(row.CurrencyCode); Id(row.EntryId);
            writer.Write(row.UsesDestinationAmount); Text(row.Note);
        }
        writer.Flush(); return Convert.ToHexStringLower(hash.GetHashAndReset());
        void Id(Guid? value) { writer.Write(value.HasValue); if (value is { } id) writer.Write(id.ToByteArray()); }
        void Text(string? value) { writer.Write(value is not null); if (value is not null) writer.Write(value); }
    }

    /// <summary>Write-only adapter for a standard incremental digest; it retains no financial buffer.</summary>
    private sealed class HashStream(IncrementalHash hash) : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override void Write(byte[] buffer, int offset, int count) => hash.AppendData(buffer, offset, count);
        public override void Write(ReadOnlySpan<byte> buffer) => hash.AppendData(buffer);
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
