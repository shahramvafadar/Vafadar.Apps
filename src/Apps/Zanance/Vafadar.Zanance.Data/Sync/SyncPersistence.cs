using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Vafadar.Zanance.Data.Sync;

/// <summary>Device-local personal binding; credentials and certificate private keys never enter this database.</summary>
internal sealed class SyncBindingRow
{
    /// <summary>One binding per local financial profile.</summary>
    public int Id { get; set; } = 1;
    /// <summary>Actual authenticated personal space.</summary>
    public Guid ProfileId { get; set; }
    /// <summary>Actual revocable service device.</summary>
    public Guid DeviceId { get; set; }
    /// <summary>Actual authenticated identity; changing sign-in cannot silently reuse another owner's queue.</summary>
    public string IdentityId { get; set; } = "";
    /// <summary>Reviewed endpoint/certificate/identity binding, without a token or password.</summary>
    public string PeerKey { get; set; } = "";
    /// <summary>Cached owner-visible space label for offline status.</summary>
    public string RemoteName { get; set; } = "";
    /// <summary>The captured protected generation; restore cannot adopt old binding authority.</summary>
    public string GenerationPath { get; set; } = "";
    /// <summary>Reviewed relational contract.</summary>
    public string SchemaHash { get; set; } = "";
    /// <summary>Explicit local opt-in.</summary>
    public bool Enabled { get; set; }
    /// <summary>Set only inside the incoming writer transaction, suppressing echoed outbox records.</summary>
    public bool Applying { get; set; }
    /// <summary>Last completely applied immutable event sequence.</summary>
    public long Cursor { get; set; }
    /// <summary>Last successful synchronization, separate from financial dates.</summary>
    public DateTimeOffset? LastSuccessAt { get; set; }
    /// <summary>Content-free local failure category only.</summary>
    public string? LastFailure { get; set; }
}

/// <summary>Same-transaction complete after-value or tombstone, never a network effect during Save.</summary>
internal sealed class SyncOutboxRow
{
    /// <summary>Monotonic local committed change identity.</summary>
    public long Sequence { get; set; }
    /// <summary>Allowed original relational table.</summary>
    public string TableName { get; set; } = "";
    /// <summary>Ordered original primary-key JSON.</summary>
    public string RecordKey { get; set; } = "";
    /// <summary>Complete stored scalars; null means deletion.</summary>
    public string? Body { get; set; }
}

/// <summary>Authentic received baseline used to preserve local edits made while a request was in flight.</summary>
internal sealed class SyncKnownRow
{
    /// <summary>Allowed original table.</summary>
    public string TableName { get; set; } = "";
    /// <summary>Ordered key.</summary>
    public string RecordKey { get; set; } = "";
    /// <summary>Actual server event version.</summary>
    public long Version { get; set; }
    /// <summary>Received canonical row, with content-addressed blob references; null is a tombstone.</summary>
    public string? Body { get; set; }
}

/// <summary>One durable immutable outgoing cutoff and resumable incoming feed per profile.</summary>
internal sealed class SyncClientRound
{
    /// <summary>One active local round.</summary>
    public int Id { get; set; } = 1;
    /// <summary>Stable request identity; retries cannot become a second posting.</summary>
    public Guid OperationId { get; set; }
    /// <summary>Actual service staging identity once created.</summary>
    public Guid? RemoteId { get; set; }
    /// <summary>Whole committed outbox snapshot boundary.</summary>
    public long Cutoff { get; set; }
    /// <summary>Capture, staging, commit or feed-resume state.</summary>
    public int Phase { get; set; }
    /// <summary>Immutable part count once sealed.</summary>
    public int PartCount { get; set; }
    /// <summary>Confirmed immutable prefix.</summary>
    public int SentParts { get; set; }
    /// <summary>Digest of ordered part digests.</summary>
    public string ContentHash { get; set; } = "";
    /// <summary>Captured complete remote feed boundary.</summary>
    public long? ThroughCursor { get; set; }
    /// <summary>Confirmed private incoming staging prefix, not the applied financial cursor.</summary>
    public long ReadCursor { get; set; }
}

/// <summary>Private feed staging survives interruption without exposing partially applied financial changes.</summary>
internal sealed class SyncIncomingRow
{
    /// <summary>Allowed table.</summary>
    public string TableName { get; set; } = "";
    /// <summary>Ordered key.</summary>
    public string RecordKey { get; set; } = "";
    /// <summary>Newest staged event for this key.</summary>
    public long Version { get; set; }
    /// <summary>Complete verified body or tombstone.</summary>
    public string? Body { get; set; }
}

/// <summary>Explicit review is bound to the original local change and actual current remote version.</summary>
internal sealed class SyncConflictRow
{
    /// <summary>Allowed table.</summary>
    public string TableName { get; set; } = "";
    /// <summary>Ordered key.</summary>
    public string RecordKey { get; set; } = "";
    /// <summary>Complete typed conflict, never a logged financial diagnostic.</summary>
    public string Payload { get; set; } = "";
    /// <summary>Local edit boundary reviewed by the owner.</summary>
    public long LocalSequence { get; set; }
    /// <summary>Explicit whole-row choice; null means unresolved.</summary>
    public bool? UseProposal { get; set; }
}

/// <summary>Device-private content-addressed transfer state; exported copies never retain it.</summary>
internal sealed class SyncBlobTransfer
{
    /// <summary>Standard lowercase SHA-256 content digest.</summary>
    public string Hash { get; set; } = "";
    /// <summary>Original complete byte length.</summary>
    public long Length { get; set; }
    /// <summary>Owned immutable private object parts after verified upload.</summary>
    public string? Manifest { get; set; }
}

/// <summary>Bounded protected incoming blob pieces, never plaintext staging files.</summary>
internal sealed class SyncBlobPart
{
    /// <summary>Expected complete digest.</summary>
    public string Hash { get; set; } = "";
    /// <summary>Ordered bounded part number.</summary>
    public int Part { get; set; }
    /// <summary>Bytes in the protected profile database only.</summary>
    public byte[] Data { get; set; } = [];
}

/// <summary>Maps device-local synchronization metadata separately from the allowlisted financial contract.</summary>
internal sealed class SyncPersistenceConfiguration : IEntityTypeConfiguration<SyncBindingRow>,
    IEntityTypeConfiguration<SyncOutboxRow>, IEntityTypeConfiguration<SyncKnownRow>, IEntityTypeConfiguration<SyncClientRound>,
    IEntityTypeConfiguration<SyncIncomingRow>, IEntityTypeConfiguration<SyncConflictRow>,
    IEntityTypeConfiguration<SyncBlobTransfer>, IEntityTypeConfiguration<SyncBlobPart>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SyncBindingRow> builder)
    {
        builder.ToTable("SyncBinding", table => table.HasCheckConstraint("CK_SyncBinding_Singleton", "\"Id\" = 1"));
        builder.HasKey(row => row.Id); builder.Property(row => row.Id).ValueGeneratedNever();
        builder.Property(row => row.GenerationPath).HasMaxLength(4096);
        builder.Property(row => row.SchemaHash).HasMaxLength(64);
        builder.Property(row => row.IdentityId).HasMaxLength(450); builder.Property(row => row.PeerKey).HasMaxLength(64);
        builder.Property(row => row.RemoteName).HasMaxLength(100); builder.Property(row => row.LastFailure).HasMaxLength(64);
    }
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SyncOutboxRow> builder)
    {
        builder.ToTable("SyncOutbox"); builder.HasKey(row => row.Sequence);
        builder.Property(row => row.Sequence).ValueGeneratedOnAdd();
        builder.Property(row => row.TableName).HasMaxLength(128); builder.Property(row => row.RecordKey).HasMaxLength(2048);
        builder.HasIndex(row => new { row.TableName, row.RecordKey, row.Sequence });
    }
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SyncKnownRow> builder)
    {
        builder.ToTable("SyncKnown"); builder.HasKey(row => new { row.TableName, row.RecordKey });
        builder.Property(row => row.TableName).HasMaxLength(128); builder.Property(row => row.RecordKey).HasMaxLength(2048);
    }
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SyncClientRound> builder)
    {
        builder.ToTable("SyncClientRound", table => table.HasCheckConstraint("CK_SyncClientRound_Singleton", "\"Id\" = 1"));
        builder.HasKey(row => row.Id); builder.Property(row => row.Id).ValueGeneratedNever();
        builder.Property(row => row.ContentHash).HasMaxLength(64);
    }
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SyncIncomingRow> builder)
    {
        builder.ToTable("SyncIncoming"); builder.HasKey(row => new { row.TableName, row.RecordKey });
        builder.Property(row => row.TableName).HasMaxLength(128); builder.Property(row => row.RecordKey).HasMaxLength(2048);
    }
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SyncConflictRow> builder)
    {
        builder.ToTable("SyncConflicts"); builder.HasKey(row => new { row.TableName, row.RecordKey });
        builder.Property(row => row.TableName).HasMaxLength(128); builder.Property(row => row.RecordKey).HasMaxLength(2048);
    }
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SyncBlobTransfer> builder)
    {
        builder.ToTable("SyncBlobTransfers"); builder.HasKey(row => row.Hash); builder.Property(row => row.Hash).HasMaxLength(64);
    }
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SyncBlobPart> builder)
    {
        builder.ToTable("SyncBlobParts"); builder.HasKey(row => new { row.Hash, row.Part }); builder.Property(row => row.Hash).HasMaxLength(64);
    }
}
