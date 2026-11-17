using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Vafadar.Dastan.Store;

/// <summary>A collection the editors work on: everything of its file that is not one of its records.</summary>
public sealed class StoredCollection
{
    /// <summary>Gets or sets the identifier of the collection (<c>datasetId</c> of its file).</summary>
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the file of the collection without its records, as JSON: the groups of records are empty
    /// lists in it and keep their place, so that a file written from the store reads like the one that came in.
    /// </summary>
    public required string Header { get; set; }

    /// <summary>Gets or sets when the collection came into the store.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets when the collection was last changed.</summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>One record of a collection – a source, an entity, a relation, an event or a media item – as it is now.</summary>
public sealed class StoredRecord
{
    /// <summary>Gets or sets the collection.</summary>
    public required string CollectionId { get; set; }

    /// <summary>Gets or sets the group of the record in the file: <c>sources</c>, <c>entities</c>, <c>relations</c>, <c>events</c>, <c>media</c>.</summary>
    public required string Group { get; set; }

    /// <summary>Gets or sets the identifier of the record.</summary>
    public required string RecordId { get; set; }

    /// <summary>Gets or sets the place of the record in its group. The order of a group is the editors' order.</summary>
    public int Position { get; set; }

    /// <summary>Gets or sets the record as it is written in the file of a collection, as JSON.</summary>
    public required string Body { get; set; }

    /// <summary>Gets or sets how far the review of a statement has come, as the record says it; nothing for a record that is no statement.</summary>
    public string? ReviewStatus { get; set; }

    /// <summary>Gets or sets how often the record was written: 1 when it came in, one more with every change.</summary>
    public int Version { get; set; }

    /// <summary>Gets or sets when the record was last changed.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Gets or sets who changed it last.</summary>
    public required string UpdatedBy { get; set; }
}

/// <summary>A translation of a collection into one language, as its file.</summary>
public sealed class StoredTranslation
{
    /// <summary>Gets or sets the collection.</summary>
    public required string CollectionId { get; set; }

    /// <summary>Gets or sets the language of the translation.</summary>
    public required string Language { get; set; }

    /// <summary>Gets or sets the translation document, as JSON.</summary>
    public required string Body { get; set; }

    /// <summary>Gets or sets when the translation was last changed.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Gets or sets who changed it last.</summary>
    public required string UpdatedBy { get; set; }
}

/// <summary>What happened to a record.</summary>
public enum RecordChangeKind
{
    /// <summary>The record came into the store.</summary>
    Added = 0,

    /// <summary>The record reads differently than before.</summary>
    Changed = 1,

    /// <summary>The record was taken out of the collection.</summary>
    Removed = 2,
}

/// <summary>
/// One change of one record (GOV-005): who, when, why, and the record before and after. Changes are only added –
/// the history of a record is never rewritten.
/// </summary>
public sealed class RecordChange
{
    /// <summary>Gets or sets the number of the change in the store.</summary>
    public long Id { get; set; }

    /// <summary>Gets or sets the collection.</summary>
    public required string CollectionId { get; set; }

    /// <summary>Gets or sets the group of the record; <c>translation</c> for a translation document.</summary>
    public required string Group { get; set; }

    /// <summary>Gets or sets the identifier of the record; the language of a translation.</summary>
    public required string RecordId { get; set; }

    /// <summary>Gets or sets what happened.</summary>
    public RecordChangeKind Kind { get; set; }

    /// <summary>Gets or sets the version of the record after the change.</summary>
    public int Version { get; set; }

    /// <summary>Gets or sets who made the change.</summary>
    public required string By { get; set; }

    /// <summary>Gets or sets when.</summary>
    public DateTime At { get; set; }

    /// <summary>Gets or sets why, in the words of whoever made the change.</summary>
    public string? Reason { get; set; }

    /// <summary>Gets or sets the record before the change, as JSON; nothing for a record that came in.</summary>
    public string? Before { get; set; }

    /// <summary>Gets or sets the record after the change, as JSON; nothing for a record that was taken out.</summary>
    public string? After { get; set; }
}

/// <summary>The database of the editors' store.</summary>
public sealed class StoreContext(DbContextOptions<StoreContext> options) : DbContext(options)
{
    /// <summary>Gets the collections.</summary>
    public DbSet<StoredCollection> Collections => Set<StoredCollection>();

    /// <summary>Gets the records as they are now.</summary>
    public DbSet<StoredRecord> Records => Set<StoredRecord>();

    /// <summary>Gets the translations.</summary>
    public DbSet<StoredTranslation> Translations => Set<StoredTranslation>();

    /// <summary>Gets the history of every record.</summary>
    public DbSet<RecordChange> Changes => Set<RecordChange>();

    /// <summary>Gets the published revisions, each as it was published.</summary>
    public DbSet<StoredRevision> Revisions => Set<StoredRevision>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<StoredCollection>(collection =>
        {
            collection.HasKey(item => item.Id);
            collection.Property(item => item.Id).HasMaxLength(200);
        });

        modelBuilder.Entity<StoredRecord>(record =>
        {
            record.HasKey(item => new { item.CollectionId, item.Group, item.RecordId });
            record.Property(item => item.Group).HasMaxLength(40);
            record.Property(item => item.RecordId).HasMaxLength(200);
            record.Property(item => item.ReviewStatus).HasMaxLength(80);
            record.Property(item => item.UpdatedBy).HasMaxLength(200);
            record.HasIndex(item => new { item.CollectionId, item.Group, item.Position });
            record.HasIndex(item => new { item.CollectionId, item.ReviewStatus });
            record.HasOne<StoredCollection>().WithMany().HasForeignKey(item => item.CollectionId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StoredTranslation>(translation =>
        {
            translation.HasKey(item => new { item.CollectionId, item.Language });
            translation.Property(item => item.Language).HasMaxLength(40);
            translation.Property(item => item.UpdatedBy).HasMaxLength(200);
            translation.HasOne<StoredCollection>().WithMany().HasForeignKey(item => item.CollectionId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StoredRevision>(revision =>
        {
            revision.HasKey(item => new { item.CollectionId, item.Number });
            revision.Property(item => item.By).HasMaxLength(200);
            revision.HasOne<StoredCollection>().WithMany().HasForeignKey(item => item.CollectionId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RecordChange>(change =>
        {
            change.HasKey(item => item.Id);
            change.Property(item => item.Group).HasMaxLength(40);
            change.Property(item => item.RecordId).HasMaxLength(200);
            change.Property(item => item.By).HasMaxLength(200);
            change.Property(item => item.Kind).HasConversion<string>().HasMaxLength(20);
            change.HasIndex(item => new { item.CollectionId, item.Group, item.RecordId, item.Version });
            change.HasOne<StoredCollection>().WithMany().HasForeignKey(item => item.CollectionId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

/// <summary>Lets the tools of the framework build the database of the store without an app around it.</summary>
internal sealed class StoreContextFactory : IDesignTimeDbContextFactory<StoreContext>
{
    public StoreContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<StoreContext>().UseSqlite("Data Source=design-time.db").Options);
}
