using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Data.Configurations;

/// <summary>Maps financial contacts separately from public service membership/security metadata.</summary>
internal sealed class HouseholdContactConfiguration : IEntityTypeConfiguration<HouseholdContact>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<HouseholdContact> model)
    {
        model.ToTable("HouseholdContacts"); model.Property(row => row.Name).HasMaxLength(100).IsRequired();
        model.Property(row => row.IdentityId).HasMaxLength(128);
    }
}

/// <summary>One actual expense/refund with independent payment provenance and a single optional existing cash movement.</summary>
internal sealed class HouseholdCostConfiguration : IEntityTypeConfiguration<HouseholdCost>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<HouseholdCost> model)
    {
        model.ToTable("HouseholdCosts"); model.Property(row => row.CurrencyCode).HasMaxLength(3).IsRequired();
        model.Property(row => row.Title).HasMaxLength(200).IsRequired(); model.Property(row => row.Note).HasMaxLength(4000);
        model.HasOne<HouseholdContact>().WithMany().HasForeignKey(row => row.PaidByContactId).OnDelete(DeleteBehavior.Restrict);
        model.HasOne<LedgerEntry>().WithMany().HasForeignKey(row => row.EntryId).OnDelete(DeleteBehavior.Restrict);
        model.HasOne<HouseholdCost>().WithMany().HasForeignKey(row => row.RefundOfCostId).OnDelete(DeleteBehavior.Restrict);
        // SQLite unique nullable values permit multiple independent private payments but only one
        // cost per actual linked entry. No filtered-index rule is hidden from the wire contract.
        model.HasIndex(row => row.EntryId).IsUnique(); model.HasIndex(row => row.Date);
    }
}

/// <summary>Composite identities keep a person's single exact share in the same transaction as its cost.</summary>
internal sealed class HouseholdCostAllocationConfiguration : IEntityTypeConfiguration<HouseholdCostAllocation>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<HouseholdCostAllocation> model)
    {
        model.ToTable("HouseholdAllocations"); model.HasKey(row => new { row.CostId, row.ContactId });
        model.HasOne<HouseholdCost>().WithMany().HasForeignKey(row => row.CostId).OnDelete(DeleteBehavior.Cascade);
        model.HasOne<HouseholdContact>().WithMany().HasForeignKey(row => row.ContactId).OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Repayment records retain exact parties/units and optionally link one existing transfer without posting another.</summary>
internal sealed class HouseholdSettlementConfiguration : IEntityTypeConfiguration<HouseholdSettlement>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<HouseholdSettlement> model)
    {
        model.ToTable("HouseholdSettlements"); model.Property(row => row.CurrencyCode).HasMaxLength(3).IsRequired();
        model.Property(row => row.Note).HasMaxLength(4000);
        model.HasOne<HouseholdContact>().WithMany().HasForeignKey(row => row.FromContactId).OnDelete(DeleteBehavior.Restrict);
        model.HasOne<HouseholdContact>().WithMany().HasForeignKey(row => row.ToContactId).OnDelete(DeleteBehavior.Restrict);
        model.HasOne<LedgerEntry>().WithMany().HasForeignKey(row => row.EntryId).OnDelete(DeleteBehavior.Restrict);
        model.HasIndex(row => row.EntryId).IsUnique(); model.HasIndex(row => row.Date);
    }
}
