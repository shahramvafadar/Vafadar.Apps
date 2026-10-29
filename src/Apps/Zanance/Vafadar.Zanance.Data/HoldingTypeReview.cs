using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>Original optional type and its measurement/valuation history, read coherently from one file.</summary>
public sealed record HoldingTypeInputs(AssetType? Type, List<AssetEvent> Events, List<AssetValuation> Valuations);

/// <summary>Single-use original type editor authority; a new type owns one stable absent identity.</summary>
public sealed class HoldingTypeReview
{
    private readonly string _path, _packet;
    private readonly CommercialFileAccess _access;
    private int _state;
    /// <summary>Captures immutable original identity, file authority and source facts before publication.</summary>
    internal HoldingTypeReview(Guid id, bool editing, string path, CommercialFileAccess access, HoldingTypeInputs inputs)
    { Id = id; Editing = editing; _path = path; _access = access; Inputs = inputs; _packet = Packet(inputs); }
    /// <summary>Gets the original existing or reserved new identity.</summary>
    public Guid Id { get; }
    /// <summary>Gets whether the original source must still exist.</summary>
    public bool Editing { get; }
    /// <summary>Gets the original display facts; mutation invalidates their authority.</summary>
    public HoldingTypeInputs Inputs { get; }
    /// <summary>Gets the actual captured file for the narrow original form boundary.</summary>
    internal string DatabasePath => _path;
    /// <summary>Checks only immutable cached access for repeated native frame guards.</summary>
    internal void EnsureAccessCurrent() => _access.EnsureSnapshotCurrent();
    /// <summary>Refuses expired access or caller-mutated original type/history without opening a connection.</summary>
    internal void EnsureCurrent()
    {
        EnsureAccessCurrent();
        if (_packet != Packet(Inputs)) throw new HoldingEventReviewChangedException("Original type editor inputs changed in memory.");
    }
    /// <summary>Refuses another configured file before opening it for an original form validation.</summary>
    internal void RequireFile(ZananceDbContext db)
    {
        EnsureAccessCurrent();
        if (_path != DatabaseFileIdentity.ConfiguredPath(db))
            throw new HoldingEventReviewChangedException("Original type editor file changed.");
    }
    /// <summary>Compares complete original inputs for explicit reload while permitting replacement of expired rights.</summary>
    internal bool HasSameInputs(HoldingTypeReview previous)
    {
        EnsureCurrent();
        if (previous._packet != Packet(previous.Inputs)) throw new HoldingEventReviewChangedException("Previous original type inputs changed in memory.");
        return Id == previous.Id && Editing == previous.Editing && _path == previous._path && _packet == previous._packet
            && Equals(_access.Context, previous._access.Context);
    }
    /// <summary>Claims one original unmodified writer, refusing concurrent or consumed authority.</summary>
    internal void Begin()
    {
        _access.EnsureSnapshotCurrent();
        if (_packet != Packet(Inputs) || Interlocked.CompareExchange(ref _state, 1, 0) != 0)
            throw new HoldingEventReviewChangedException("Original type editor review changed or was consumed.");
    }
    /// <summary>Consumes known acceptance even if following notification fails.</summary>
    internal void End(bool committed) => Volatile.Write(ref _state, committed ? 2 : 0);
    /// <summary>Revalidates exact original source and file under the existing writer.</summary>
    internal async Task ValidateAsync(ZananceDbContext db, CancellationToken ct)
    {
        _access.EnsureSnapshotCurrent();
        if (_path != DatabaseFileIdentity.ConfiguredPath(db) || _packet != Packet(Inputs)
            || _packet != Packet(await ReadAsync(db, Id, Editing, ct)))
            throw new HoldingEventReviewChangedException("Original type editor source changed.");
    }
    /// <summary>Reads deterministic source/history facts in the caller's existing coherent transaction.</summary>
    internal static async Task<HoldingTypeInputs> ReadAsync(ZananceDbContext db, Guid id, bool editing, CancellationToken ct)
    {
        var type = await db.AssetTypes.AsNoTracking().SingleOrDefaultAsync(t => t.Id == id, ct);
        if ((type is not null) != editing) throw new HoldingEventReviewChangedException("Original existing/absent type identity changed.");
        return new(type, await db.AssetEvents.AsNoTracking().Where(e => e.AssetTypeId == id).OrderBy(e => e.Id).ToListAsync(ct),
            await db.AssetValuations.AsNoTracking().Where(v => v.AssetTypeId == id).OrderBy(v => v.Id).ToListAsync(ct));
    }
    private static string Packet(HoldingTypeInputs inputs) => JsonSerializer.Serialize(inputs, HoldingTypeJson.Default.HoldingTypeInputs);
    /// <summary>Copies complete original metadata without mutating its review or generating a fresh identity.</summary>
    public static AssetType Copy(AssetType type) => new(type.Id)
    { Name = type.Name, Kind = type.Kind, Dimension = type.Dimension, Metal = type.Metal, PurityPer10000 = type.PurityPer10000,
      UnitWeightMg = type.UnitWeightMg, CountUnitName = type.CountUnitName, Divisible = type.Divisible,
      PriceCurrencyCode = type.PriceCurrencyCode, Note = type.Note, Icon = type.Icon, IsArchived = type.IsArchived,
      SortOrder = type.SortOrder, CreatedAt = type.CreatedAt, UpdatedAt = type.UpdatedAt };
}

/// <summary>Explicit original type editor serialization metadata for trim/AOT hosts.</summary>
[JsonSerializable(typeof(HoldingTypeInputs))]
internal sealed partial class HoldingTypeJson : JsonSerializerContext;

public sealed partial class HoldingStore
{
    /// <summary>Reads original type facts or one reserved absent new identity in a coherent actual-file transaction.</summary>
    public async Task<HoldingTypeReview> ReadTypeReviewAsync(Guid id, bool editing, CancellationToken ct = default)
    {
        if (id == Guid.Empty) throw new ArgumentException("Original type identity required.", nameof(id));
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var path = DatabaseFileIdentity.ConfiguredPath(db); var access = new CommercialFileAccess(commercialAccess, path);
        access.EnsureSnapshotCurrent(); await db.Database.OpenConnectionAsync(ct);
        await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
        await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
        var result = new HoldingTypeReview(id, editing, path, access, await HoldingTypeReview.ReadAsync(db, id, editing, ct));
        access.EnsureSnapshotCurrent(); return result;
    }
    /// <summary>Saves one exact original type draft with fresh rights, dimension protection and known accepted outcome.</summary>
    public async Task<bool> SaveReviewedTypeAsync(HoldingTypeReview review, AssetType type, Func<bool> isCurrent, CancellationToken ct = default,
        Action? accepted = null, AssetTypeEditorSnapshot? originalSource = null)
    {
        ArgumentNullException.ThrowIfNull(review); ArgumentNullException.ThrowIfNull(type); ArgumentNullException.ThrowIfNull(isCurrent);
        review.Begin(); var committed = false;
        try
        {
            if (type.Id != review.Id) throw new HoldingEventReviewChangedException("Original proposed type identity differs.");
            var copy = HoldingTypeReview.Copy(type);
            await using var db = await contextFactory.CreateDbContextAsync(ct);
            await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, ct, requireTransaction: true);
            write.DemandFeature(review.Editing ? CommercialFeature.Corrections : CommercialFeature.ManageHoldings);
            await review.ValidateAsync(db, ct);
            if (originalSource is not null)
            {
                if (!ReferenceEquals(originalSource.Review, review)) throw new HoldingEventReviewChangedException("Original type form review changed.");
                await Task.Run(() => originalSource.ValidateAsync(db, ct), ct);
            }
            if (!isCurrent()) throw new HoldingEventReviewChangedException("Original type draft retired.");
            var original = review.Inputs.Type;
            if (copy.IsArchived != (original?.IsArchived ?? false) || copy.Icon != original?.Icon
                || copy.SortOrder != (original?.SortOrder ?? 0))
                throw new HoldingEventReviewChangedException("Original noneditable type metadata changed.");
            if (original is not null && original.Dimension != copy.Dimension && review.Inputs.Events.Count > 0) return false;
            if (review.Editing) db.AssetTypes.Update(copy); else db.AssetTypes.Add(copy);
            write.EnsureCurrent(); await db.SaveChangesAsync(ct); write.EnsureCurrent();
            if (!isCurrent()) throw new HoldingEventReviewChangedException("Original type draft retired before commit.");
            await write.CommitAsync(ct); committed = true;
            accepted?.Invoke();
            CommittedWriteNotification.Publish(() => Changed?.Invoke(this, EventArgs.Empty)); return true;
        }
        catch (CommittedWriteNotificationException) { committed = true; throw; }
        finally { review.End(committed); }
    }
}
