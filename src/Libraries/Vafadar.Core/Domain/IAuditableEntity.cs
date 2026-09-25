namespace Vafadar.Core.Domain;

/// <summary>
/// An entity whose creation and last modification times are tracked automatically by the data layer.
/// </summary>
/// <remarks>Timestamps are always stored in UTC.</remarks>
public interface IAuditableEntity
{
    /// <summary>Gets or sets when the entity was first saved (UTC).</summary>
    DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets when the entity was last saved (UTC).</summary>
    DateTimeOffset UpdatedAt { get; set; }
}
