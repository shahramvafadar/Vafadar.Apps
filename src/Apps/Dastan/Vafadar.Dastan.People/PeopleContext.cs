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

    /// <summary>Gets or sets a value indicating whether the account was closed by who runs the site and not by its visitor: only the operator opens it again.</summary>
    public bool ClosedByOperator { get; set; }

    /// <summary>Gets or sets the version of the terms and of the privacy notice the visitor said they read when the account was made.</summary>
    public string? TermsVersion { get; set; }

    /// <summary>Gets or sets when the visitor said so.</summary>
    public DateTime? TermsAcceptedAt { get; set; }
}

/// <summary>
/// A restriction who runs the site imposed on something of an account, with the statement of reasons its
/// visitor reads (Article 17 of Regulation (EU) 2022/2065).
/// </summary>
public sealed class StoredRestriction
{
    /// <summary>Gets or sets the number of the restriction.</summary>
    public long Number { get; set; }

    /// <summary>Gets or sets the account it concerns.</summary>
    public required string Account { get; set; }

    /// <summary>Gets or sets what it is about (<see cref="RestrictionKind"/>).</summary>
    public int Kind { get; set; }

    /// <summary>Gets or sets the photograph or the tree; nothing for the account.</summary>
    public string? Target { get; set; }

    /// <summary>Gets or sets the facts the decision rests on.</summary>
    public required string Reason { get; set; }

    /// <summary>Gets or sets the law or the rule of the terms it rests on.</summary>
    public required string Ground { get; set; }

    /// <summary>Gets or sets a value indicating whether it followed a notice.</summary>
    public bool Noticed { get; set; }

    /// <summary>Gets or sets when it was imposed.</summary>
    public DateTime ImposedAt { get; set; }

    /// <summary>Gets or sets when it was lifted, once it was.</summary>
    public DateTime? LiftedAt { get; set; }
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

    /// <summary>Gets or sets when its owner asked to make it public (D-053); nothing when that was not asked or was taken back.</summary>
    public DateTime? PublicationAskedAt { get; set; }

    /// <summary>Gets or sets when its owner made it public. It is shown to everybody only while everybody it names agrees.</summary>
    public DateTime? PublishedAt { get; set; }
}

/// <summary>That an account agreed to be named in a tree that is public.</summary>
public sealed class StoredPublicationConsent
{
    /// <summary>Gets or sets the tree.</summary>
    public required string Tree { get; set; }

    /// <summary>Gets or sets the account that agreed.</summary>
    public required string Account { get; set; }

    /// <summary>Gets or sets when it agreed.</summary>
    public DateTime At { get; set; }
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

    /// <summary>Gets or sets a value indicating whether the person agreed that the photograph is shown to everybody (D-053).</summary>
    public bool Everybody { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the person said themselves that the photograph shows them
    /// (D-059) – an objection to its being shown to everybody – instead of being named by its owner.
    /// </summary>
    public bool Objection { get; set; }
}

/// <summary>An album: photographs its owner put together under a name (step 16.10).</summary>
public sealed class StoredAlbum
{
    /// <summary>Gets or sets the number of the album.</summary>
    public long Number { get; set; }

    /// <summary>Gets or sets the account it belongs to.</summary>
    public required string Owner { get; set; }

    /// <summary>Gets or sets its name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets what its owner wrote about it.</summary>
    public string Note { get; set; } = string.Empty;
}

/// <summary>That a photograph stands in an album, and where.</summary>
public sealed class StoredAlbumPhoto
{
    /// <summary>Gets or sets the album.</summary>
    public long Album { get; set; }

    /// <summary>Gets or sets the photograph.</summary>
    public required string Photo { get; set; }

    /// <summary>Gets or sets its place in the album: the lower, the earlier.</summary>
    public int Position { get; set; }
}

/// <summary>That an account can be signed in through a provider (D-048). Nothing the provider knows about the person is kept.</summary>
public sealed class StoredExternalLogin
{
    /// <summary>Gets or sets the provider: <c>google</c>, <c>microsoft</c>.</summary>
    public required string Provider { get; set; }

    /// <summary>Gets or sets a hash of the identifier the provider gives the person. The identifier itself is not kept.</summary>
    public required string Key { get; set; }

    /// <summary>Gets or sets the account.</summary>
    public required string VisitorId { get; set; }
}

/// <summary>The database of the store of the visitors.</summary>
public sealed class PeopleContext(DbContextOptions<PeopleContext> options) : DbContext(options)
{
    /// <summary>Gets the accounts as ASP.NET Core Identity keeps them, in the schema <c>identity</c> (ADR 0016).</summary>
    public DbSet<VisitorAccount> Accounts => Set<VisitorAccount>();

    /// <summary>Gets the tokens of the accounts: what a link in a mail proves, kept as a hash until it is used or ends.</summary>
    public DbSet<Microsoft.AspNetCore.Identity.IdentityUserToken<string>> AccountTokens => Set<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>();

    /// <summary>Gets what belongs to the visitors in Dastan, by the identifier of their account.</summary>
    public DbSet<StoredVisitor> Visitors => Set<StoredVisitor>();

    /// <summary>Gets the providers the accounts can be signed in through.</summary>
    public DbSet<StoredExternalLogin> ExternalLogins => Set<StoredExternalLogin>();

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

    /// <summary>Gets who agreed to be named in a tree that is public.</summary>
    public DbSet<StoredPublicationConsent> PublicationConsents => Set<StoredPublicationConsent>();

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

    /// <summary>Gets the albums.</summary>
    public DbSet<StoredAlbum> Albums => Set<StoredAlbum>();

    /// <summary>Gets which photographs stand in the albums.</summary>
    public DbSet<StoredAlbumPhoto> AlbumPhotos => Set<StoredAlbumPhoto>();

    /// <summary>Gets the restrictions who runs the site imposed.</summary>
    public DbSet<StoredRestriction> Restrictions => Set<StoredRestriction>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(PeopleStore.Schema);
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // The accounts of the visitors as Identity keeps them, in a schema of their own (ADR 0016). A name
        // and a mail address each belong to one account, by the database itself: two forms that arrive at the
        // same moment cannot both be kept.
        const string identity = "identity";
        modelBuilder.Entity<VisitorAccount>(account =>
        {
            account.ToTable("VisitorAccounts", identity);
            account.HasKey(item => item.Id);
            account.Property(item => item.Id).HasMaxLength(40);
            account.Property(item => item.UserName).HasMaxLength(80);
            account.Property(item => item.NormalizedUserName).HasMaxLength(80);
            account.Property(item => item.Email).HasMaxLength(Vafadar.Dastan.Data.MailOutbox.LongestAddress);
            account.Property(item => item.NormalizedEmail).HasMaxLength(Vafadar.Dastan.Data.MailOutbox.LongestAddress);
            account.Property(item => item.Language).HasMaxLength(20);
            account.Property(item => item.ConcurrencyStamp).IsConcurrencyToken();
            account.HasIndex(item => item.NormalizedUserName).IsUnique();
            account.HasIndex(item => item.NormalizedEmail).IsUnique();

            // Dastan asks for no telephone and has no second factor; what is not asked for has no column.
            account.Ignore(item => item.PhoneNumber);
            account.Ignore(item => item.PhoneNumberConfirmed);
            account.Ignore(item => item.TwoFactorEnabled);
        });

        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>(token =>
        {
            token.ToTable("VisitorTokens", identity);
            token.HasKey(item => new { item.UserId, item.LoginProvider, item.Name });
            token.Property(item => item.UserId).HasMaxLength(40);
            token.Property(item => item.LoginProvider).HasMaxLength(40);
            token.Property(item => item.Name).HasMaxLength(40);
            token.HasOne<VisitorAccount>().WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>(login =>
        {
            login.ToTable("VisitorLogins", identity);
            login.HasKey(item => new { item.LoginProvider, item.ProviderKey });
            login.Property(item => item.LoginProvider).HasMaxLength(40);
            login.Property(item => item.ProviderKey).HasMaxLength(200);
            login.Property(item => item.UserId).HasMaxLength(40);
            login.HasOne<VisitorAccount>().WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>(claim =>
        {
            claim.ToTable("VisitorClaims", identity);
            claim.HasKey(item => item.Id);
            claim.Property(item => item.UserId).HasMaxLength(40);
            claim.HasOne<VisitorAccount>().WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StoredVisitor>(visitor =>
        {
            visitor.HasKey(item => item.Id);
            visitor.Property(item => item.Id).HasMaxLength(40);
            visitor.Property(item => item.Name).HasMaxLength(80);
            visitor.Property(item => item.NameKey).HasMaxLength(80);
            visitor.Property(item => item.Password).HasMaxLength(200);
            visitor.Property(item => item.Stamp).HasMaxLength(40);
            visitor.Property(item => item.TermsVersion).HasMaxLength(20);
            visitor.HasIndex(item => item.NameKey).IsUnique();
        });

        modelBuilder.Entity<StoredRestriction>(restriction =>
        {
            restriction.HasKey(item => item.Number);
            restriction.Property(item => item.Account).HasMaxLength(40);
            restriction.Property(item => item.Target).HasMaxLength(40);
            restriction.Property(item => item.Reason).HasMaxLength(2000);
            restriction.Property(item => item.Ground).HasMaxLength(2000);
            restriction.HasIndex(item => item.Account);

            // A restriction goes with the account it concerns: what is deleted needs no reason any more.
            restriction.HasOne<StoredVisitor>().WithMany().HasForeignKey(item => item.Account).OnDelete(DeleteBehavior.Cascade);
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

        modelBuilder.Entity<StoredPublicationConsent>(consent =>
        {
            consent.HasKey(item => new { item.Tree, item.Account });
            consent.Property(item => item.Tree).HasMaxLength(40);
            consent.Property(item => item.Account).HasMaxLength(40);
            consent.HasOne<StoredTree>().WithMany().HasForeignKey(item => item.Tree).OnDelete(DeleteBehavior.Cascade);
            consent.HasOne<StoredVisitor>().WithMany().HasForeignKey(item => item.Account).OnDelete(DeleteBehavior.Cascade);
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

        // An album goes with its owner; a place in it goes with the album and with the photograph.
        modelBuilder.Entity<StoredAlbum>(album =>
        {
            album.HasKey(item => item.Number);
            album.Property(item => item.Owner).HasMaxLength(40);
            album.Property(item => item.Name).HasMaxLength(160);
            album.Property(item => item.Note).HasMaxLength(800);
            album.HasIndex(item => item.Owner);
            album.HasOne<StoredVisitor>().WithMany().HasForeignKey(item => item.Owner).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StoredAlbumPhoto>(placed =>
        {
            placed.HasKey(item => new { item.Album, item.Photo });
            placed.Property(item => item.Photo).HasMaxLength(40);
            placed.HasIndex(item => item.Photo);
            placed.HasOne<StoredAlbum>().WithMany().HasForeignKey(item => item.Album).OnDelete(DeleteBehavior.Cascade);
            placed.HasOne<StoredPhoto>().WithMany().HasForeignKey(item => item.Photo).OnDelete(DeleteBehavior.Cascade);
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

        modelBuilder.Entity<StoredExternalLogin>(login =>
        {
            login.HasKey(item => new { item.Provider, item.Key });
            login.Property(item => item.Provider).HasMaxLength(20);
            login.Property(item => item.Key).HasMaxLength(64);
            login.Property(item => item.VisitorId).HasMaxLength(40);
            // One sign-in of a provider for an account: the store holds to it, whoever asks at the same moment.
            login.HasIndex(item => new { item.VisitorId, item.Provider }).IsUnique();
            login.HasOne<StoredVisitor>().WithMany().HasForeignKey(item => item.VisitorId).OnDelete(DeleteBehavior.Cascade);
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
        new(PeopleStore.Options("Host=design-time;Database=design-time"));
}
