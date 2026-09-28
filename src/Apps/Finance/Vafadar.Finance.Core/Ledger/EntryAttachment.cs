using Vafadar.Core.Domain;

namespace Vafadar.Finance.Core.Ledger;

/// <summary>An attachment without its content, for lists.</summary>
public sealed record AttachmentInfo(Guid Id, Guid EntryId, string FileName, string ContentType, int Size, DateTimeOffset CreatedAt)
{
    /// <summary>Gets a value indicating whether the attachment is an image that can be previewed.</summary>
    public bool IsImage => ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// A receipt photo or document attached to an entry (F2-TX-04). It is stored in the local database, so encrypted
/// backups contain it; it is never part of CSV exports.
/// </summary>
public sealed class EntryAttachment : Entity, IAuditableEntity
{
    /// <summary>Largest accepted attachment in bytes (after images are scaled down).</summary>
    public const int MaxBytes = 5 * 1024 * 1024;

    /// <summary>Gets or sets the entry.</summary>
    public Guid EntryId { get; set; }

    /// <summary>Gets or sets the original file name, shown to the user.</summary>
    public required string FileName { get; set; }

    /// <summary>Gets or sets the media type, e.g. <c>image/jpeg</c> or <c>application/pdf</c>.</summary>
    public required string ContentType { get; set; }

    /// <summary>Gets or sets the content.</summary>
    public byte[] Data { get; set; } = [];

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Gets a value indicating whether the attachment is an image that can be previewed.</summary>
    public bool IsImage => ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
}