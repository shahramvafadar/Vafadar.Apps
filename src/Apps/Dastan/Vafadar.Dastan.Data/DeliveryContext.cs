using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Vafadar.Dastan.Data;

/// <summary>A report of a visitor as the table keeps it (<see cref="VisitorReport"/>).</summary>
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

/// <summary>A message that waits to be sent, or was, as the table keeps it (<see cref="MailOutbox"/>).</summary>
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

/// <summary>A notice about content as the table keeps it (<see cref="ContentNotice"/>).</summary>
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

/// <summary>
/// The tables of the schema <c>delivery</c> as they are made and changed (ADR 0015). Nothing reads or writes
/// through this context: <see cref="ReportBox"/>, <see cref="NoticeBox"/> and <see cref="MailOutbox"/> ask the
/// tables plainly. It is here so that the tables have one owner and a history of their changes of form.
/// </summary>
public sealed class DeliveryContext(DbContextOptions<DeliveryContext> options) : DbContext(options)
{
    /// <summary>Gets the reports of visitors.</summary>
    public DbSet<StoredReport> Reports => Set<StoredReport>();

    /// <summary>Gets the notices about content.</summary>
    public DbSet<StoredNotice> Notices => Set<StoredNotice>();

    /// <summary>Gets the mail that waits to be sent, or was.</summary>
    public DbSet<StoredMail> Mail => Set<StoredMail>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(Delivery.Schema);

        modelBuilder.Entity<StoredReport>(report =>
        {
            report.ToTable("Reports", Delivery.Schema);
            report.HasKey(item => item.Id);
            report.Property(item => item.Id).HasMaxLength(32);
            report.Property(item => item.Collection).HasMaxLength(120);
            report.Property(item => item.Language).HasMaxLength(120);
            report.Property(item => item.Statement).HasMaxLength(300);
            report.Property(item => item.Text).HasMaxLength(ReportBox.LongestText);
            report.Property(item => item.SettledBy).HasMaxLength(200);
            report.HasIndex(item => new { item.SettledOn, item.On });
        });

        modelBuilder.Entity<StoredMail>(mail =>
        {
            mail.ToTable("Mail", Delivery.Schema);
            mail.HasKey(item => item.Key);
            mail.Property(item => item.Key).HasMaxLength(100);
            mail.Property(item => item.Kind).HasMaxLength(40);
            mail.Property(item => item.To).HasMaxLength(MailOutbox.LongestAddress);
            mail.Property(item => item.Language).HasMaxLength(40);
            mail.Property(item => item.Subject).HasMaxLength(300);
            mail.Property(item => item.LastError).HasMaxLength(200);

            // What is due is found without reading what was sent long ago.
            mail.HasIndex(item => item.NextTry).HasFilter("""("SentAt" IS NULL AND "GaveUpAt" IS NULL)""");
        });

        modelBuilder.Entity<StoredNotice>(notice =>
        {
            notice.ToTable("Notices", Delivery.Schema);
            notice.HasKey(item => item.Id);
            notice.Property(item => item.Id).HasMaxLength(17);
            notice.Property(item => item.Language).HasMaxLength(40);
            notice.Property(item => item.Address).HasMaxLength(NoticeBox.LongestLine);
            notice.Property(item => item.Kind).HasMaxLength(40);
            notice.Property(item => item.Explanation).HasMaxLength(NoticeBox.LongestExplanation);
            notice.Property(item => item.Name).HasMaxLength(NoticeBox.LongestLine);
            notice.Property(item => item.Mail).HasMaxLength(NoticeBox.LongestLine);
            notice.HasIndex(item => new { item.SettledAt, item.ReceivedAt });
        });
    }
}

/// <summary>Makes the context for the tools of the design time, which need no database to write a change of form.</summary>
internal sealed class DeliveryContextFactory : IDesignTimeDbContextFactory<DeliveryContext>
{
    public DeliveryContext CreateDbContext(string[] args) => new(Delivery.Options("Host=design-time;Database=design-time"));
}
