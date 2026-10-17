using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vafadar.Zanance.Core.Rates;

namespace Vafadar.Zanance.Data.Configurations;

/// <summary>Separate profile-owned source documents; do not silently change the delivered financial v1/v2 row contracts.</summary>
internal sealed class MarketReferenceConfiguration : IEntityTypeConfiguration<MarketReferenceReceipt>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MarketReferenceReceipt> builder)
    {
        builder.ToTable("MarketReferenceReceipts");
        builder.Property(row => row.TargetFingerprint).HasMaxLength(64);
        builder.Property(row => row.QuoteDigest).HasMaxLength(64);
        builder.Property(row => row.QuoteJson).HasMaxLength(MarketReference.MaximumQuoteBytes);
        builder.Property(row => row.MappingJson).HasMaxLength(4096);
        // Two devices may independently confirm the same source/value under different original receipt ids.
        // Preserve both literal histories during companion transfer; original-id replay remains idempotent.
        builder.HasIndex(row => new { row.Target, row.TargetId, row.TargetFingerprint, row.QuoteDigest });
        builder.HasIndex(row => new { row.Target, row.TargetId, row.AcceptedAt });
        // The target may be temporarily absent during ordered financial synchronization or retained as an audit after deletion.
        // No polymorphic FK or source document can create financial existence/ownership authority.
    }
}
