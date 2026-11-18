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

/// <summary>A relationship two accounts show together, or the request for one.</summary>
public sealed class StoredRelationship
{
    /// <summary>Gets or sets the identifier.</summary>
    public required string Id { get; set; }

    /// <summary>Gets or sets the account that asked.</summary>
    public required string From { get; set; }

    /// <summary>Gets or sets the account that was asked.</summary>
    public required string To { get; set; }

    /// <summary>Gets or sets what the relationship is called.</summary>
    public required string Kind { get; set; }

    /// <summary>Gets or sets where it stands (<c>RelationshipState</c>).</summary>
    public int State { get; set; }

    /// <summary>Gets or sets who the side that asked lets see it (<c>ScopeKind</c>).</summary>
    public int FromScope { get; set; }

    /// <summary>Gets or sets the circle of the side that asked, when it shows the relationship to one.</summary>
    public long? FromCircle { get; set; }

    /// <summary>Gets or sets who the side that was asked lets see it (<c>ScopeKind</c>).</summary>
    public int ToScope { get; set; }

    /// <summary>Gets or sets the circle of the side that was asked, when it shows the relationship to one.</summary>
    public long? ToCircle { get; set; }

    /// <summary>Gets or sets when it was asked for.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets when it last moved.</summary>
    public DateTime ChangedAt { get; set; }
}

/// <summary>A circle: a name an account gives to some of the people it has a relationship with.</summary>
public sealed class StoredCircle
{
    /// <summary>Gets or sets the number of the circle.</summary>
    public long Number { get; set; }

    /// <summary>Gets or sets the account it belongs to.</summary>
    public required string Owner { get; set; }

    /// <summary>Gets or sets its name.</summary>
    public required string Name { get; set; }
}

/// <summary>An account in a circle.</summary>
public sealed class StoredCircleMember
{
    /// <summary>Gets or sets the circle.</summary>
    public long Circle { get; set; }

    /// <summary>Gets or sets the account that is in it.</summary>
    public required string Member { get; set; }
}

/// <summary>A family tree: it has an owner, who says who else may see or change it (D-054).</summary>
public sealed class StoredTree
{
    /// <summary>Gets or sets the identifier.</summary>
    public required string Id { get; set; }

    /// <summary>Gets or sets the account it belongs to.</summary>
    public required string Owner { get; set; }

    /// <summary>Gets or sets its name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets when it was made.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>An account that may see or change a tree.</summary>
public sealed class StoredTreeMember
{
    /// <summary>Gets or sets the tree.</summary>
    public required string Tree { get; set; }

    /// <summary>Gets or sets the account.</summary>
    public required string Account { get; set; }

    /// <summary>Gets or sets what it may do (<c>TreeRole</c>).</summary>
    public int Role { get; set; }
}

/// <summary>The record of a person in a tree. It says little on purpose (PRIV-003).</summary>
public sealed class StoredPerson
{
    /// <summary>Gets or sets the identifier.</summary>
    public required string Id { get; set; }

    /// <summary>Gets or sets the tree.</summary>
    public required string Tree { get; set; }

    /// <summary>Gets or sets the name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the year of birth, when it is known.</summary>
    public int? Born { get; set; }

    /// <summary>Gets or sets the year of death, when it is known.</summary>
    public int? Died { get; set; }

    /// <summary>Gets or sets a value indicating whether the person has died.</summary>
    public bool Deceased { get; set; }

    /// <summary>Gets or sets the account that made the record; nothing once that account is deleted.</summary>
    public string? CreatedBy { get; set; }

    /// <summary>Gets or sets when the record was made.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the account the record is, once that account claimed it and who made the record confirmed
    /// the claim (D-052); nothing once that account is deleted.
    /// </summary>
    public string? Account { get; set; }
}

/// <summary>The claim of an account that a record is the same person, waiting to be confirmed or refused.</summary>
public sealed class StoredClaim
{
    /// <summary>Gets or sets the number of the claim.</summary>
    public long Number { get; set; }

    /// <summary>Gets or sets the tree.</summary>
    public required string Tree { get; set; }

    /// <summary>Gets or sets the record that is claimed.</summary>
    public required string Person { get; set; }

    /// <summary>Gets or sets the account that claims it.</summary>
    public required string Account { get; set; }

    /// <summary>Gets or sets when it was claimed.</summary>
    public DateTime At { get; set; }
}

/// <summary>A link between two records of a tree: a parent and a child, or two partners.</summary>
public sealed class StoredKinship
{
    /// <summary>Gets or sets the number of the link.</summary>
    public long Number { get; set; }

    /// <summary>Gets or sets the tree.</summary>
    public required string Tree { get; set; }

    /// <summary>Gets or sets the kind: <c>parent</c> or <c>partner</c>.</summary>
    public required string Kind { get; set; }

    /// <summary>Gets or sets the first record: the parent, or one partner.</summary>
    public required string From { get; set; }

    /// <summary>Gets or sets the second record: the child, or the other partner.</summary>
    public required string To { get; set; }
}

/// <summary>A change of a tree, kept with who made it (D-054).</summary>
public sealed class StoredTreeChange
{
    /// <summary>Gets or sets the number of the change.</summary>
    public long Number { get; set; }

    /// <summary>Gets or sets the tree.</summary>
    public required string Tree { get; set; }

    /// <summary>Gets or sets the account that made it; nothing once that account is deleted.</summary>
    public string? By { get; set; }

    /// <summary>Gets or sets when.</summary>
    public DateTime At { get; set; }

    /// <summary>Gets or sets what was done.</summary>
    public required string What { get; set; }

    /// <summary>Gets or sets what it was done to, as it was named then.</summary>
    public required string Subject { get; set; }
}

/// <summary>A photograph of a visitor: whose it is, who may see it, and where its file lies.</summary>
public sealed class StoredPhoto
{
    /// <summary>Gets or sets the identifier.</summary>
    public required string Id { get; set; }

    /// <summary>Gets or sets the account it belongs to.</summary>
    public required string Owner { get; set; }

    /// <summary>Gets or sets what its owner wrote about it.</summary>
    public required string Caption { get; set; }

    /// <summary>Gets or sets the day it was taken, when its owner said so.</summary>
    public DateOnly? Taken { get; set; }

    /// <summary>Gets or sets when it was brought in.</summary>
    public DateTime SharedAt { get; set; }

    /// <summary>Gets or sets who its owner lets see it (<c>ScopeKind</c>).</summary>
    public int Scope { get; set; }

    /// <summary>Gets or sets the circle of its owner, when it is shown to one.</summary>
    public long? Circle { get; set; }

    /// <summary>Gets or sets the name of its file in the folder of the photographs.</summary>
    public required string File { get; set; }

    /// <summary>Gets or sets the type the file is delivered as.</summary>
    public required string ContentType { get; set; }

    /// <summary>Gets or sets the width of the image in pixels.</summary>
    public int Width { get; set; }

    /// <summary>Gets or sets the height of the image in pixels.</summary>
    public int Height { get; set; }

    /// <summary>Gets or sets the length of the file in bytes.</summary>
    public long Length { get; set; }
}

/// <summary>That a photograph is said to show an account, and what that account answered.</summary>
public sealed class StoredPhotoTag
{
    /// <summary>Gets or sets the photograph.</summary>
    public required string Photo { get; set; }

    /// <summary>Gets or sets the account it is said to show.</summary>
    public required string Account { get; set; }

    /// <summary>Gets or sets where the tag stands (<c>TagState</c>).</summary>
    public int State { get; set; }
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

    /// <summary>Gets the relationships and the requests for them.</summary>
    public DbSet<StoredRelationship> Relationships => Set<StoredRelationship>();

    /// <summary>Gets the circles.</summary>
    public DbSet<StoredCircle> Circles => Set<StoredCircle>();

    /// <summary>Gets who is in the circles.</summary>
    public DbSet<StoredCircleMember> CircleMembers => Set<StoredCircleMember>();

    /// <summary>Gets the family trees.</summary>
    public DbSet<StoredTree> Trees => Set<StoredTree>();

    /// <summary>Gets who may see or change the trees.</summary>
    public DbSet<StoredTreeMember> TreeMembers => Set<StoredTreeMember>();

    /// <summary>Gets the records of people.</summary>
    public DbSet<StoredPerson> Persons => Set<StoredPerson>();

    /// <summary>Gets the claims that wait.</summary>
    public DbSet<StoredClaim> Claims => Set<StoredClaim>();

    /// <summary>Gets the links between records.</summary>
    public DbSet<StoredKinship> Kinships => Set<StoredKinship>();

    /// <summary>Gets the changes of the trees.</summary>
    public DbSet<StoredTreeChange> TreeChanges => Set<StoredTreeChange>();

    /// <summary>Gets the photographs.</summary>
    public DbSet<StoredPhoto> Photos => Set<StoredPhoto>();

    /// <summary>Gets who the photographs are said to show.</summary>
    public DbSet<StoredPhotoTag> PhotoTags => Set<StoredPhotoTag>();

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

        // A relationship is gone when one of its two accounts is.
        modelBuilder.Entity<StoredRelationship>(relationship =>
        {
            relationship.HasKey(item => item.Id);
            relationship.Property(item => item.Id).HasMaxLength(40);
            relationship.Property(item => item.From).HasMaxLength(40);
            relationship.Property(item => item.To).HasMaxLength(40);
            relationship.Property(item => item.Kind).HasMaxLength(40);
            relationship.HasIndex(item => item.From);
            relationship.HasIndex(item => item.To);
            relationship.HasOne<StoredVisitor>().WithMany().HasForeignKey(item => item.From).OnDelete(DeleteBehavior.Cascade);
            relationship.HasOne<StoredVisitor>().WithMany().HasForeignKey(item => item.To).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StoredCircle>(circle =>
        {
            circle.HasKey(item => item.Number);
            circle.Property(item => item.Owner).HasMaxLength(40);
            circle.Property(item => item.Name).HasMaxLength(80);
            circle.HasIndex(item => item.Owner);
            circle.HasOne<StoredVisitor>().WithMany().HasForeignKey(item => item.Owner).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StoredCircleMember>(member =>
        {
            member.HasKey(item => new { item.Circle, item.Member });
            member.Property(item => item.Member).HasMaxLength(40);
            member.HasOne<StoredCircle>().WithMany().HasForeignKey(item => item.Circle).OnDelete(DeleteBehavior.Cascade);
            member.HasOne<StoredVisitor>().WithMany().HasForeignKey(item => item.Member).OnDelete(DeleteBehavior.Cascade);
        });

        // A tree goes with its owner; what is in a tree goes with the tree.
        modelBuilder.Entity<StoredTree>(tree =>
        {
            tree.HasKey(item => item.Id);
            tree.Property(item => item.Id).HasMaxLength(40);
            tree.Property(item => item.Owner).HasMaxLength(40);
            tree.Property(item => item.Name).HasMaxLength(160);
            tree.HasIndex(item => item.Owner);
            tree.HasOne<StoredVisitor>().WithMany().HasForeignKey(item => item.Owner).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StoredTreeMember>(member =>
        {
            member.HasKey(item => new { item.Tree, item.Account });
            member.Property(item => item.Tree).HasMaxLength(40);
            member.Property(item => item.Account).HasMaxLength(40);
            member.HasIndex(item => item.Account);
            member.HasOne<StoredTree>().WithMany().HasForeignKey(item => item.Tree).OnDelete(DeleteBehavior.Cascade);
            member.HasOne<StoredVisitor>().WithMany().HasForeignKey(item => item.Account).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StoredPerson>(person =>
        {
            person.HasKey(item => item.Id);
            person.Property(item => item.Id).HasMaxLength(40);
            person.Property(item => item.Tree).HasMaxLength(40);
            person.Property(item => item.Name).HasMaxLength(160);
            person.Property(item => item.CreatedBy).HasMaxLength(40);
            person.HasIndex(item => item.Tree);
            person.HasOne<StoredTree>().WithMany().HasForeignKey(item => item.Tree).OnDelete(DeleteBehavior.Cascade);

            // The record stays in the tree when who made it is gone.
            person.HasOne<StoredVisitor>().WithMany().HasForeignKey(item => item.CreatedBy).OnDelete(DeleteBehavior.SetNull);

            // And it names nobody when the account it was is gone.
            person.Property(item => item.Account).HasMaxLength(40);
            person.HasOne<StoredVisitor>().WithMany().HasForeignKey(item => item.Account).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<StoredClaim>(claim =>
        {
            claim.HasKey(item => item.Number);
            claim.Property(item => item.Tree).HasMaxLength(40);
            claim.Property(item => item.Person).HasMaxLength(40);
            claim.Property(item => item.Account).HasMaxLength(40);
            claim.HasIndex(item => item.Tree);
            claim.HasOne<StoredTree>().WithMany().HasForeignKey(item => item.Tree).OnDelete(DeleteBehavior.Cascade);
            claim.HasOne<StoredPerson>().WithMany().HasForeignKey(item => item.Person).OnDelete(DeleteBehavior.Cascade);
            claim.HasOne<StoredVisitor>().WithMany().HasForeignKey(item => item.Account).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StoredKinship>(link =>
        {
            link.HasKey(item => item.Number);
            link.Property(item => item.Tree).HasMaxLength(40);
            link.Property(item => item.Kind).HasMaxLength(20);
            link.Property(item => item.From).HasMaxLength(40);
            link.Property(item => item.To).HasMaxLength(40);
            link.HasIndex(item => item.Tree);
            link.HasOne<StoredTree>().WithMany().HasForeignKey(item => item.Tree).OnDelete(DeleteBehavior.Cascade);
            link.HasOne<StoredPerson>().WithMany().HasForeignKey(item => item.From).OnDelete(DeleteBehavior.Cascade);
            link.HasOne<StoredPerson>().WithMany().HasForeignKey(item => item.To).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StoredTreeChange>(change =>
        {
            change.HasKey(item => item.Number);
            change.Property(item => item.Tree).HasMaxLength(40);
            change.Property(item => item.By).HasMaxLength(40);
            change.Property(item => item.What).HasMaxLength(40);
            change.Property(item => item.Subject).HasMaxLength(400);
            change.HasIndex(item => item.Tree);
            change.HasOne<StoredTree>().WithMany().HasForeignKey(item => item.Tree).OnDelete(DeleteBehavior.Cascade);

            // That something was changed stays when who changed it is gone.
            change.HasOne<StoredVisitor>().WithMany().HasForeignKey(item => item.By).OnDelete(DeleteBehavior.SetNull);
        });

        // A photograph goes with its owner, a tag with the photograph and with the account it names.
        modelBuilder.Entity<StoredPhoto>(photo =>
        {
            photo.HasKey(item => item.Id);
            photo.Property(item => item.Id).HasMaxLength(40);
            photo.Property(item => item.Owner).HasMaxLength(40);
            photo.Property(item => item.Caption).HasMaxLength(400);
            photo.Property(item => item.File).HasMaxLength(80);
            photo.Property(item => item.ContentType).HasMaxLength(40);
            photo.HasIndex(item => item.Owner);
            photo.HasOne<StoredVisitor>().WithMany().HasForeignKey(item => item.Owner).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StoredPhotoTag>(tag =>
        {
            tag.HasKey(item => new { item.Photo, item.Account });
            tag.Property(item => item.Photo).HasMaxLength(40);
            tag.Property(item => item.Account).HasMaxLength(40);
            tag.HasIndex(item => item.Account);
            tag.HasOne<StoredPhoto>().WithMany().HasForeignKey(item => item.Photo).OnDelete(DeleteBehavior.Cascade);
            tag.HasOne<StoredVisitor>().WithMany().HasForeignKey(item => item.Account).OnDelete(DeleteBehavior.Cascade);
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
