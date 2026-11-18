using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Vafadar.Dastan.People;

/// <summary>The account of a visitor: a name to sign in with and what is kept of a password. Nothing else about the person.</summary>
public sealed class StoredVisitor
{
    /// <summary>Gets or sets the identifier: made by the store, it says nothing about the person and never changes.</summary>
    public required string Id { get; set; }

    /// <summary>Gets or sets the name as the visitor wrote it.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the name as it is compared: no two accounts have the same.</summary>
    public required string NameKey { get; set; }

    /// <summary>Gets or sets what is kept of the password: a derived value, never the password.</summary>
    public required string Password { get; set; }

    /// <summary>Gets or sets a value that changes with the password: a sign-in made before holds no longer.</summary>
    public required string Stamp { get; set; }

    /// <summary>Gets or sets when the account was made.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets how many times in a row the password was wrong.</summary>
    public int Failures { get; set; }

    /// <summary>Gets or sets until when the account takes no password, after too many wrong ones.</summary>
    public DateTime? LockedUntil { get; set; }

    /// <summary>Gets or sets when the visitor closed the account. It is deleted for good after the time of the store.</summary>
    public DateTime? ClosedAt { get; set; }
}

/// <summary>A code to get into an account whose password is lost. Only a value derived from it is kept; it is used once.</summary>
public sealed class StoredRecoveryCode
{
    /// <summary>Gets or sets the number of the code in the store.</summary>
    public long Number { get; set; }

    /// <summary>Gets or sets the account the code belongs to.</summary>
    public required string VisitorId { get; set; }

    /// <summary>Gets or sets the value derived from the code.</summary>
    public required string Code { get; set; }

    /// <summary>Gets or sets when the code was used.</summary>
    public DateTime? UsedAt { get; set; }
}

/// <summary>How far an account has read a collection.</summary>
public sealed class StoredReadingPosition
{
    /// <summary>Gets or sets the account.</summary>
    public required string VisitorId { get; set; }

    /// <summary>Gets or sets the name of the collection in addresses.</summary>
    public required string Collection { get; set; }

    /// <summary>Gets or sets the reading stage.</summary>
    public int Level { get; set; }
}

/// <summary>A page in the list of an account.</summary>
public sealed class StoredSavedPage
{
    /// <summary>Gets or sets the number of the entry: a page kept later has a greater one.</summary>
    public long Number { get; set; }

    /// <summary>Gets or sets the account.</summary>
    public required string VisitorId { get; set; }

    /// <summary>Gets or sets the name of the collection in addresses.</summary>
    public required string Collection { get; set; }

    /// <summary>Gets or sets the kind of page, as addresses write it.</summary>
    public required string Kind { get; set; }

    /// <summary>Gets or sets the identifier of what the page is about.</summary>
    public required string PageId { get; set; }
}

/// <summary>An invitation an account made for somebody without one.</summary>
public sealed class StoredInvitation
{
    /// <summary>Gets or sets the code: what the address of the invitation carries. It says nothing about anybody.</summary>
    public required string Id { get; set; }

    /// <summary>Gets or sets the account that made it.</summary>
    public required string By { get; set; }

    /// <summary>Gets or sets what it is for (<c>InvitationPurpose</c>).</summary>
    public int Purpose { get; set; }

    /// <summary>Gets or sets when it was made.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the last day it can be used.</summary>
    public DateOnly LastDay { get; set; }

    /// <summary>Gets or sets the account that used it; nothing once that account is deleted.</summary>
    public string? UsedBy { get; set; }

    /// <summary>Gets or sets the day it was used.</summary>
    public DateOnly? UsedOn { get; set; }

    /// <summary>Gets or sets when it was taken back.</summary>
    public DateTime? RevokedAt { get; set; }
}

/// <summary>The database of the store of the visitors.</summary>
public sealed class PeopleContext(DbContextOptions<PeopleContext> options) : DbContext(options)
{
    /// <summary>Gets the accounts.</summary>
    public DbSet<StoredVisitor> Visitors => Set<StoredVisitor>();

    /// <summary>Gets the recovery codes.</summary>
    public DbSet<StoredRecoveryCode> RecoveryCodes => Set<StoredRecoveryCode>();

    /// <summary>Gets how far the accounts have read.</summary>
    public DbSet<StoredReadingPosition> ReadingPositions => Set<StoredReadingPosition>();

    /// <summary>Gets the lists of the accounts.</summary>
    public DbSet<StoredSavedPage> SavedPages => Set<StoredSavedPage>();

    /// <summary>Gets the invitations.</summary>
    public DbSet<StoredInvitation> Invitations => Set<StoredInvitation>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<StoredVisitor>(visitor =>
        {
            visitor.HasKey(item => item.Id);
            visitor.Property(item => item.Id).HasMaxLength(40);
            visitor.Property(item => item.Name).HasMaxLength(80);
            visitor.Property(item => item.NameKey).HasMaxLength(80);
            visitor.Property(item => item.Password).HasMaxLength(200);
            visitor.Property(item => item.Stamp).HasMaxLength(40);
            visitor.HasIndex(item => item.NameKey).IsUnique();
        });

        modelBuilder.Entity<StoredRecoveryCode>(code =>
        {
            code.HasKey(item => item.Number);
            code.Property(item => item.VisitorId).HasMaxLength(40);
            code.Property(item => item.Code).HasMaxLength(100);
            code.HasIndex(item => item.VisitorId);
            code.HasOne<StoredVisitor>().WithMany().HasForeignKey(item => item.VisitorId).OnDelete(DeleteBehavior.Cascade);
        });

        // What belongs to an account goes with it when it is deleted.
        modelBuilder.Entity<StoredReadingPosition>(position =>
        {
            position.HasKey(item => new { item.VisitorId, item.Collection });
            position.Property(item => item.VisitorId).HasMaxLength(40);
            position.Property(item => item.Collection).HasMaxLength(120);
            position.HasOne<StoredVisitor>().WithMany().HasForeignKey(item => item.VisitorId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StoredSavedPage>(page =>
        {
            page.HasKey(item => item.Number);
            page.Property(item => item.VisitorId).HasMaxLength(40);
            page.Property(item => item.Collection).HasMaxLength(120);
            page.Property(item => item.Kind).HasMaxLength(20);
            page.Property(item => item.PageId).HasMaxLength(120);
            page.HasIndex(item => new { item.VisitorId, item.Collection, item.Kind, item.PageId }).IsUnique();
            page.HasOne<StoredVisitor>().WithMany().HasForeignKey(item => item.VisitorId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StoredInvitation>(invitation =>
        {
            invitation.HasKey(item => item.Id);
            invitation.Property(item => item.Id).HasMaxLength(40);
            invitation.Property(item => item.By).HasMaxLength(40);
            invitation.Property(item => item.UsedBy).HasMaxLength(40);
            invitation.HasIndex(item => item.By);
            invitation.HasOne<StoredVisitor>().WithMany().HasForeignKey(item => item.By).OnDelete(DeleteBehavior.Cascade);

            // That an invitation was used stays when who used it is gone.
            invitation.HasOne<StoredVisitor>().WithMany().HasForeignKey(item => item.UsedBy).OnDelete(DeleteBehavior.SetNull);
        });
    }
}

/// <summary>Lets the tools of the framework build the database of the store without an app around it.</summary>
internal sealed class PeopleContextFactory : IDesignTimeDbContextFactory<PeopleContext>
{
    public PeopleContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<PeopleContext>().UseSqlite("Data Source=design-time.db").Options);
}
