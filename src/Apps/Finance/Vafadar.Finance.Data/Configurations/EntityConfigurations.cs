using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Budgets;
using Vafadar.Finance.Core.Categories;
using Vafadar.Finance.Core.Ledger;
using Vafadar.Finance.Core.Rates;
using Vafadar.Finance.Core.Settings;

namespace Vafadar.Finance.Data.Configurations;

internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.Property(a => a.Name).HasMaxLength(100);
        builder.Property(a => a.CurrencyCode).HasMaxLength(3);
        builder.Property(a => a.Icon).HasMaxLength(64);
    }
}

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.Property(c => c.Name).HasMaxLength(100);
        builder.Property(c => c.SystemKey).HasMaxLength(64);
        builder.Property(c => c.Color).HasMaxLength(9);
        builder.Property(c => c.Icon).HasMaxLength(64);
        builder.HasOne<Category>().WithMany().HasForeignKey(c => c.ParentId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable("Entries");
        builder.Property(e => e.Title).HasMaxLength(200);
        builder.Property(e => e.Payee).HasMaxLength(200);
        builder.Property(e => e.Note).HasMaxLength(4000);
        builder.Property(e => e.Icon).HasMaxLength(64);
        builder.Property(e => e.OriginalCurrencyCode).HasMaxLength(3);

        builder.HasOne<Account>().WithMany().HasForeignKey(e => e.AccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Account>().WithMany().HasForeignKey(e => e.ToAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Category>().WithMany().HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LedgerEntry>().WithMany().HasForeignKey(e => e.RefundOfId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.Date);
        builder.HasIndex(e => new { e.AccountId, e.Date });
        builder.HasIndex(e => e.GroupId);

        // One settlement per plan occurrence – a second automatic posting is impossible (D-07, REC-21).
        builder.HasIndex(e => new { e.ScheduleId, e.OccurrenceDate })
            .IsUnique()
            .HasFilter("\"ScheduleId\" IS NOT NULL");
    }
}

internal sealed class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.Property(b => b.CurrencyCode).HasMaxLength(3);
        builder.HasIndex(b => new { b.Year, b.Month, b.Calendar, b.CurrencyCode }).IsUnique();
        builder.OwnsMany(b => b.CategoryLimits, limits =>
        {
            limits.ToTable("BudgetCategoryLimits");
            limits.WithOwner().HasForeignKey("BudgetId");
            limits.HasKey("BudgetId", nameof(BudgetCategoryLimit.CategoryId));
        });
    }
}

internal sealed class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRate>
{
    public void Configure(EntityTypeBuilder<ExchangeRate> builder)
    {
        builder.Property(r => r.FromCurrencyCode).HasMaxLength(3);
        builder.Property(r => r.ToCurrencyCode).HasMaxLength(3);
        builder.HasIndex(r => new { r.Date, r.FromCurrencyCode, r.ToCurrencyCode }).IsUnique();
    }
}

internal sealed class FinanceSettingsConfiguration : IEntityTypeConfiguration<FinanceSettings>
{
    public void Configure(EntityTypeBuilder<FinanceSettings> builder)
    {
        builder.ToTable("Settings");
        builder.Property(s => s.ReportCurrencyCode).HasMaxLength(3);
    }
}
