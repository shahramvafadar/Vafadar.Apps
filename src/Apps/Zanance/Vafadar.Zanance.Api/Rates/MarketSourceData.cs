using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Api.Sync;

namespace Vafadar.Zanance.Api.Rates;

/// <summary>Original personal source document, protected at rest and scoped by the actual financial profile foreign key.</summary>
public sealed class PersonalMarketSourceRow
{
    /// <summary>Monotonic server feed position, never a financial-event cursor or write permission.</summary>
    public long Sequence { get; set; }
    /// <summary>Actual owner-scoped profile, never selected from the source document's financial target fields.</summary>
    public Guid ProfileId { get; set; }
    /// <summary>Original immutable local receipt identity; replay cannot create a replacement copy.</summary>
    public Guid DocumentId { get; set; }
    /// <summary>Exact document comparison digest, not a signature or authority grant.</summary>
    public string Digest { get; set; } = "";
    /// <summary>Framework-protected exact source packet, including private local asset captions.</summary>
    public string ProtectedDocument { get; set; } = "";
    /// <summary>Bounded original encoded size for finite owner storage accounting, independent of billing tiers.</summary>
    public int Bytes { get; set; }
}

/// <summary>Additive independent source companion storage; delivered financial tables/contracts remain unchanged.</summary>
internal static class MarketSourceModel
{
    internal static void Configure(ModelBuilder builder)
    {
        var rows = builder.Entity<PersonalMarketSourceRow>(); rows.ToTable("PersonalMarketSources"); rows.HasKey(row => row.Sequence);
        rows.Property(row => row.Sequence).ValueGeneratedOnAdd(); rows.Property(row => row.Digest).HasMaxLength(64);
        rows.Property(row => row.ProtectedDocument).HasMaxLength(131072);
        rows.HasIndex(row => new { row.ProfileId, row.DocumentId }).IsUnique(); rows.HasIndex(row => new { row.ProfileId, row.Sequence });
        rows.HasOne<SyncSpaceRow>().WithMany().HasForeignKey(row => row.ProfileId).OnDelete(DeleteBehavior.Cascade);
    }
}
