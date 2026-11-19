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

    /// <summary>Gets how far the review of a statement has come, as the record says it; nothing for a record that is no statement. The database reads it out of the record.</summary>
    public string? ReviewStatus { get; private set; }

    /// <summary>Gets what kind of thing the record is, as it says itself (<c>type</c>, or <c>kind</c> for a source). The database reads it out of the record.</summary>
    public string? Kind { get; private set; }

    /// <summary>Gets the name of the record (<c>label</c>, or <c>title</c> for a source). The database reads it out of the record.</summary>
    public string? Label { get; private set; }

    /// <summary>Gets how far a reader must have come to be shown the record. The database reads it out of the record.</summary>
    public int? SpoilerLevel { get; private set; }

    /// <summary>Gets or sets how often the record was written: 1 when it came in, one more with every change.</summary>
    public int Version { get; set; }

    /// <summary>Gets or sets when the record was last changed.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Gets or sets who changed it last.</summary>
    public required string UpdatedBy { get; set; }
}

/// <summary>A report of a visitor as the table keeps it (<see cref="Vafadar.Dastan.Data.VisitorReport"/>).</summary>
public sealed class StoredReport
{
    /// <summary>Gets or sets the name of the report.</summary>
    public required string Id { get; set; }

    /// <summary>Gets or sets the day it was written.</summary>
    public DateOnly On { get; set; }

    /// <summary>Gets or sets the collection of the statement.</summary>
    public required string Collection { get; set; }

    /// <summary>Gets or sets the published revision the visitor read.</summary>
    public int? Revision { get; set; }

    /// <summary>Gets or sets the language of the page.</summary>
    public required string Language { get; set; }

    /// <summary>Gets or sets the address of the statement inside its collection.</summary>
    public required string Statement { get; set; }

    /// <summary>Gets or sets what the visitor wrote.</summary>
    public required string Text { get; set; }

    /// <summary>Gets or sets the editor who looked at it.</summary>
    public string? SettledBy { get; set; }

    /// <summary>Gets or sets the day that was.</summary>
    public DateOnly? SettledOn { get; set; }
}

/// <summary>A message that waits to be sent, or was, as the table keeps it (<see cref="Vafadar.Dastan.Data.MailOutbox"/>).</summary>
public sealed class StoredMail
{
    /// <summary>Gets or sets what names the message for good.</summary>
    public required string Key { get; set; }

    /// <summary>Gets or sets what kind of message it is.</summary>
    public required string Kind { get; set; }

    /// <summary>Gets or sets the address it goes to.</summary>
    public required string To { get; set; }

    /// <summary>Gets or sets the language it is written in.</summary>
    public required string Language { get; set; }

    /// <summary>Gets or sets its subject.</summary>
    public required string Subject { get; set; }

    /// <summary>Gets or sets its body as plain text; emptied once it is sent.</summary>
    public required string Text { get; set; }

    /// <summary>Gets or sets its body as HTML; emptied once it is sent.</summary>
    public string? Html { get; set; }

    /// <summary>Gets or sets when it was handed over.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets when it is tried next.</summary>
    public DateTime NextTry { get; set; }

    /// <summary>Gets or sets how often sending it was tried.</summary>
    public int Attempts { get; set; }

    /// <summary>Gets or sets when it was sent.</summary>
    public DateTime? SentAt { get; set; }

    /// <summary>Gets or sets when sending it was given up.</summary>
    public DateTime? GaveUpAt { get; set; }

    /// <summary>Gets or sets what the last try failed with, in a word.</summary>
    public string? LastError { get; set; }
}

/// <summary>A notice about content as the table keeps it (<see cref="Vafadar.Dastan.Data.ContentNotice"/>).</summary>
public sealed class StoredNotice
{
    /// <summary>Gets or sets the reference of the notice.</summary>
    public required string Id { get; set; }

    /// <summary>Gets or sets when it came.</summary>
    public DateTime ReceivedAt { get; set; }

    /// <summary>Gets or sets the language of the page it was given on.</summary>
    public required string Language { get; set; }

    /// <summary>Gets or sets where on the site the content is.</summary>
    public required string Address { get; set; }

    /// <summary>Gets or sets what is held against it.</summary>
    public required string Kind { get; set; }

    /// <summary>Gets or sets why.</summary>
    public required string Explanation { get; set; }

    /// <summary>Gets or sets the name of who gave the notice.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets their mail address.</summary>
    public string? Mail { get; set; }

    /// <summary>Gets or sets a value indicating whether they declared the notice accurate and complete.</summary>
    public bool GoodFaith { get; set; }

    /// <summary>Gets or sets when it was decided.</summary>
    public DateTime? SettledAt { get; set; }

    /// <summary>Gets or sets what was decided.</summary>
    public string? Decision { get; set; }
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

/// <summary>
/// A document the store keeps as a whole: a comparison, or the translation of one. Its parts are not records
/// of their own.
/// </summary>
public sealed class StoredDocument
{
    /// <summary>Gets or sets what kind of document it is: <c>comparison</c>.</summary>
    public required string Kind { get; set; }

    /// <summary>Gets or sets the identifier of the document.</summary>
    public required string DocumentId { get; set; }

    /// <summary>Gets or sets the language of a translation; empty for the document itself.</summary>
    public required string Language { get; set; }

    /// <summary>Gets or sets the document, as JSON.</summary>
    public required string Body { get; set; }

    /// <summary>Gets or sets how many times the document was changed, the first time counted.</summary>
    public int Version { get; set; }

    /// <summary>Gets or sets when it was last changed.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Gets or sets who changed it last.</summary>
    public required string UpdatedBy { get; set; }
}

/// <summary>One change of a document: who, when, why, and the document before and after. Changes are only added.</summary>
public sealed class DocumentChange
{
    /// <summary>Gets or sets the number of the change in the store.</summary>
    public long Number { get; set; }

    /// <summary>Gets or sets what kind of document it is.</summary>
    public required string Kind { get; set; }

    /// <summary>Gets or sets the identifier of the document.</summary>
    public required string DocumentId { get; set; }

    /// <summary>Gets or sets the language of a translation; empty for the document itself.</summary>
    public required string Language { get; set; }

    /// <summary>Gets or sets what happened.</summary>
    public RecordChangeKind Change { get; set; }

    /// <summary>Gets or sets the version of the document after the change.</summary>
    public int Version { get; set; }

    /// <summary>Gets or sets who made the change.</summary>
    public required string By { get; set; }

    /// <summary>Gets or sets when.</summary>
    public DateTime At { get; set; }

    /// <summary>Gets or sets why, in the words of whoever made the change.</summary>
    public string? Reason { get; set; }

    /// <summary>Gets or sets the document before the change; nothing for one that came in.</summary>
    public string? Before { get; set; }

    /// <summary>Gets or sets the document after the change; nothing for one that was taken out.</summary>
    public string? After { get; set; }
}

/// <summary>The database of the editors' store.</summary>
public sealed class StoreContext(DbContextOptions<StoreContext> options) : DbContext(options)
{
    /// <summary>Gets the collections.</summary>
    public DbSet<StoredCollection> Collections => Set<StoredCollection>();

    /// <summary>Gets the records as they are now.</summary>
    public DbSet<StoredRecord> Records => Set<StoredRecord>();

    /// <summary>Gets the reports of visitors, as the table of them is made; they are read and written through <see cref="Vafadar.Dastan.Data.ReportBox"/>.</summary>
    public DbSet<StoredReport> Reports => Set<StoredReport>();

    /// <summary>Gets the mail that waits to be sent, as the table of it is made; it is read and written through <see cref="Vafadar.Dastan.Data.MailOutbox"/>.</summary>
    public DbSet<StoredMail> Mail => Set<StoredMail>();

    /// <summary>Gets the notices about content, as the table of them is made; they are read and written through <see cref="Vafadar.Dastan.Data.NoticeBox"/>.</summary>
    public DbSet<StoredNotice> Notices => Set<StoredNotice>();

    /// <summary>Gets what was said about which comparison the site shows, and as which text, one word after the other.</summary>
    public DbSet<ReleasedComparison> ReleasedComparisons => Set<ReleasedComparison>();

    /// <summary>Gets what was said about which revision of a collection the site shows, one after the other.</summary>
    public DbSet<StoredRelease> Releases => Set<StoredRelease>();

    /// <summary>Gets the links between the records: which record names which.</summary>
    public DbSet<RecordLink> Links => Set<RecordLink>();

    /// <summary>Gets the translations.</summary>
    public DbSet<StoredTranslation> Translations => Set<StoredTranslation>();

    /// <summary>Gets the history of every record.</summary>
    public DbSet<RecordChange> Changes => Set<RecordChange>();

    /// <summary>Gets the published revisions, each as it was published.</summary>
    public DbSet<StoredRevision> Revisions => Set<StoredRevision>();

    /// <summary>Gets the accounts of the editors.</summary>
    public DbSet<StoredEditor> Editors => Set<StoredEditor>();

    /// <summary>Gets the times the password of an account was tried.</summary>
    public DbSet<EditorSignIn> SignIns => Set<EditorSignIn>();

    /// <summary>Gets the documents kept as a whole: comparisons and their translations.</summary>
    public DbSet<StoredDocument> Documents => Set<StoredDocument>();

    /// <summary>Gets the history of every document.</summary>
    public DbSet<DocumentChange> DocumentChanges => Set<DocumentChange>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(EditorialStore.Schema);

        modelBuilder.Entity<StoredCollection>(collection =>
        {
            collection.HasKey(item => item.Id);
            collection.Property(item => item.Id).HasMaxLength(200);
            collection.Property(item => item.Header).HasColumnType("json");
        });

        modelBuilder.Entity<StoredRecord>(record =>
        {
            record.HasKey(item => new { item.CollectionId, item.Group, item.RecordId });
            record.Property(item => item.Group).HasMaxLength(40);
            record.Property(item => item.RecordId).HasMaxLength(200);
            // The record as it is written (D-081): checked JSON that keeps the order of its fields, which jsonb
            // would not. What is asked for often is read out of it by the database itself, so that a column
            // can never say something else than its record.
            record.Property(item => item.Body).HasColumnType("json");
            record.Property(item => item.ReviewStatus).HasComputedColumnSql("""("Body"::jsonb ->> 'reviewStatus')""", stored: true);
            record.Property(item => item.Kind).HasComputedColumnSql("""coalesce("Body"::jsonb ->> 'type', "Body"::jsonb ->> 'kind')""", stored: true);
            record.Property(item => item.Label).HasComputedColumnSql("""coalesce("Body"::jsonb ->> 'label', "Body"::jsonb ->> 'title')""", stored: true);
            record.Property(item => item.SpoilerLevel).HasComputedColumnSql("""CASE WHEN jsonb_typeof("Body"::jsonb -> 'spoilerLevel') = 'number' THEN ("Body"::jsonb ->> 'spoilerLevel')::numeric::integer END""", stored: true);
            record.HasIndex(item => new { item.CollectionId, item.Group, item.Kind });
            record.ToTable(table => table.HasCheckConstraint("CK_Records_Body_Names_Itself", """("Body"::jsonb ->> 'id') = "RecordId" """.Trim()));
            record.Property(item => item.UpdatedBy).HasMaxLength(200);
            record.HasIndex(item => new { item.CollectionId, item.Group, item.Position });
            record.HasIndex(item => new { item.CollectionId, item.ReviewStatus });
            record.HasOne<StoredCollection>().WithMany().HasForeignKey(item => item.CollectionId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RecordLink>(link =>
        {
            link.ToTable("RecordLinks");
            link.HasKey(item => new { item.CollectionId, item.FromGroup, item.FromId, item.Role, item.Ordinal });
            link.Property(item => item.CollectionId).HasMaxLength(200);
            link.Property(item => item.FromGroup).HasMaxLength(40);
            link.Property(item => item.FromId).HasMaxLength(200);
            link.Property(item => item.Role).HasMaxLength(40);
            link.Property(item => item.ToGroup).HasMaxLength(40);
            link.Property(item => item.ToId).HasMaxLength(200);
            link.HasIndex(item => new { item.CollectionId, item.ToGroup, item.ToId });

            // The links of a record go with it; a record that is still named by another cannot go.
            link.HasOne<StoredRecord>().WithMany().HasForeignKey(item => new { item.CollectionId, item.FromGroup, item.FromId }).OnDelete(DeleteBehavior.Cascade);
            link.HasOne<StoredRecord>().WithMany().HasForeignKey(item => new { item.CollectionId, item.ToGroup, item.ToId }).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StoredTranslation>(translation =>
        {
            translation.Property(item => item.Body).HasColumnType("json");
            translation.HasKey(item => new { item.CollectionId, item.Language });
            translation.Property(item => item.Language).HasMaxLength(40);
            translation.Property(item => item.UpdatedBy).HasMaxLength(200);
            translation.HasOne<StoredCollection>().WithMany().HasForeignKey(item => item.CollectionId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StoredRevision>(revision =>
        {
            // What was published lies apart from the work of the editors (ADR 0015): the site reads this schema
            // and no other of theirs.
            revision.ToTable("Revisions", Vafadar.Dastan.Data.PublishedRevision.Schema);
            revision.HasKey(item => new { item.CollectionId, item.Number });
            revision.Property(item => item.Hash).HasMaxLength(64);
            revision.Property(item => item.By).HasMaxLength(200);

            // A revision stays the text it was: json keeps what is written, character for character.
            revision.Property(item => item.Json).HasColumnType("json");
            revision.Property(item => item.Translations).HasColumnType("json");
            revision.HasOne<StoredCollection>().WithMany().HasForeignKey(item => item.CollectionId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StoredRelease>(release =>
        {
            release.ToTable("Releases", Vafadar.Dastan.Data.PublishedRevision.Schema);
            release.HasKey(item => item.Id);
            release.Property(item => item.CollectionId).HasMaxLength(200);
            release.Property(item => item.By).HasMaxLength(200);
            release.HasIndex(item => new { item.CollectionId, item.Id });

            // A release names a revision that was published; a withdrawal names none.
            release.HasOne<StoredRevision>().WithMany().HasForeignKey(item => new { item.CollectionId, item.Number }).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ReleasedComparison>(released =>
        {
            released.ToTable("Comparisons", Vafadar.Dastan.Data.PublishedRevision.Schema);
            released.HasKey(item => item.Number);
            released.Property(item => item.ComparisonId).HasMaxLength(200);
            released.Property(item => item.Json).HasColumnType("json");
            released.Property(item => item.Translations).HasColumnType("json");
            released.Property(item => item.Hash).HasMaxLength(64);
            released.Property(item => item.By).HasMaxLength(200);
            released.HasIndex(item => new { item.ComparisonId, item.Number });
        });

        // What comes in from outside and waits for somebody. The site and the tool reach these two tables
        // through Vafadar.Dastan.Data, with plain questions; they are named here so that they are made and
        // changed with the other tables of the database.
        modelBuilder.Entity<StoredReport>(report =>
        {
            report.ToTable("Reports", Vafadar.Dastan.Data.Delivery.Schema);
            report.HasKey(item => item.Id);
            report.Property(item => item.Id).HasMaxLength(32);
            report.Property(item => item.Collection).HasMaxLength(120);
            report.Property(item => item.Language).HasMaxLength(120);
            report.Property(item => item.Statement).HasMaxLength(300);
            report.Property(item => item.Text).HasMaxLength(Vafadar.Dastan.Data.ReportBox.LongestText);
            report.Property(item => item.SettledBy).HasMaxLength(200);
            report.HasIndex(item => new { item.SettledOn, item.On });
        });

        modelBuilder.Entity<StoredMail>(mail =>
        {
            mail.ToTable("Mail", Vafadar.Dastan.Data.Delivery.Schema);
            mail.HasKey(item => item.Key);
            mail.Property(item => item.Key).HasMaxLength(100);
            mail.Property(item => item.Kind).HasMaxLength(40);
            mail.Property(item => item.To).HasMaxLength(Vafadar.Dastan.Data.MailOutbox.LongestAddress);
            mail.Property(item => item.Language).HasMaxLength(40);
            mail.Property(item => item.Subject).HasMaxLength(300);
            mail.Property(item => item.LastError).HasMaxLength(200);

            // What is due is found without reading what was sent long ago.
            mail.HasIndex(item => item.NextTry).HasFilter("""("SentAt" IS NULL AND "GaveUpAt" IS NULL)""");
        });

        modelBuilder.Entity<StoredNotice>(notice =>
        {
            notice.ToTable("Notices", Vafadar.Dastan.Data.Delivery.Schema);
            notice.HasKey(item => item.Id);
            notice.Property(item => item.Id).HasMaxLength(17);
            notice.Property(item => item.Language).HasMaxLength(40);
            notice.Property(item => item.Address).HasMaxLength(Vafadar.Dastan.Data.NoticeBox.LongestLine);
            notice.Property(item => item.Kind).HasMaxLength(40);
            notice.Property(item => item.Explanation).HasMaxLength(Vafadar.Dastan.Data.NoticeBox.LongestExplanation);
            notice.Property(item => item.Name).HasMaxLength(Vafadar.Dastan.Data.NoticeBox.LongestLine);
            notice.Property(item => item.Mail).HasMaxLength(Vafadar.Dastan.Data.NoticeBox.LongestLine);
            notice.HasIndex(item => new { item.SettledAt, item.ReceivedAt });
        });

        modelBuilder.Entity<StoredEditor>(editor =>
        {
            editor.HasKey(item => item.Id);
            editor.Property(item => item.Id).HasMaxLength(200);
            editor.Property(item => item.Name).HasMaxLength(200);
            editor.Property(item => item.Password).HasMaxLength(200);
            editor.Property(item => item.Stamp).HasMaxLength(40);
            editor.Property(item => item.Roles).HasMaxLength(100).HasDefaultValue("writer,reviewer,publisher");
        });

        modelBuilder.Entity<EditorSignIn>(entry =>
        {
            entry.HasKey(item => item.Number);
            entry.Property(item => item.EditorId).HasMaxLength(200);
            entry.HasIndex(item => new { item.EditorId, item.Number });
        });

        modelBuilder.Entity<StoredDocument>(document =>
        {
            document.HasKey(item => new { item.Kind, item.DocumentId, item.Language });
            document.Property(item => item.Kind).HasMaxLength(40);
            document.Property(item => item.DocumentId).HasMaxLength(200);
            document.Property(item => item.Language).HasMaxLength(40);
            document.Property(item => item.UpdatedBy).HasMaxLength(200);
            document.Property(item => item.Body).HasColumnType("json");
        });

        modelBuilder.Entity<DocumentChange>(change =>
        {
            change.HasKey(item => item.Number);
            change.Property(item => item.Kind).HasMaxLength(40);
            change.Property(item => item.DocumentId).HasMaxLength(200);
            change.Property(item => item.Language).HasMaxLength(40);
            change.Property(item => item.By).HasMaxLength(200);
            change.Property(item => item.Change).HasConversion<string>().HasMaxLength(20);
            change.Property(item => item.Before).HasColumnType("json");
            change.Property(item => item.After).HasColumnType("json");
            change.HasIndex(item => new { item.Kind, item.DocumentId, item.Number });
        });

        modelBuilder.Entity<RecordChange>(change =>
        {
            change.HasKey(item => item.Id);
            change.Property(item => item.Group).HasMaxLength(40);
            change.Property(item => item.RecordId).HasMaxLength(200);
            change.Property(item => item.By).HasMaxLength(200);
            change.Property(item => item.Kind).HasConversion<string>().HasMaxLength(20);
            change.Property(item => item.Before).HasColumnType("json");
            change.Property(item => item.After).HasColumnType("json");
            change.HasIndex(item => new { item.CollectionId, item.Group, item.RecordId, item.Version });
            change.HasOne<StoredCollection>().WithMany().HasForeignKey(item => item.CollectionId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

/// <summary>Lets the tools of the framework build the database of the store without an app around it.</summary>
internal sealed class StoreContextFactory : IDesignTimeDbContextFactory<StoreContext>
{
    public StoreContext CreateDbContext(string[] args) =>
        new(EditorialStore.Options("Host=design-time;Database=design-time"));
}
