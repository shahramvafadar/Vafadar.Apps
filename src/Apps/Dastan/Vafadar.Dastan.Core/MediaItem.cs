using System.Collections.Immutable;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Core;

/// <summary>How a media item relates to what it shows. A later painting of a story is not a picture from the story's time.</summary>
public enum MediaRepresentation
{
    /// <summary>An artwork made long after the story it illustrates.</summary>
    LaterArtwork = 0,

    /// <summary>A diagram made today to explain something. It is not evidence and not a historical record.</summary>
    PresentDayExplanatoryDiagram = 1,
}

/// <summary>What is known about the text alternative of an image.</summary>
public enum AltTextStatus
{
    /// <summary>The item has no text alternative of its own (a video has captions and a transcript instead).</summary>
    None = 0,

    /// <summary>No text alternative has been prepared.</summary>
    NotPrepared = 1,

    /// <summary>A draft exists that nobody has checked against the image.</summary>
    DraftPendingReview = 2,

    /// <summary>Written for exactly this image by the project.</summary>
    ProjectAuthored = 3,
}

/// <summary>What is established about the right to show a media item.</summary>
/// <param name="Status">The status.</param>
/// <param name="License">The license, when one is named.</param>
/// <param name="Statement">The editors' note on the rights.</param>
/// <param name="CheckedOn">The day the rights were looked up.</param>
public sealed record MediaRights(RightsStatus Status, string? License, string Statement, DateOnly CheckedOn)
{
    /// <summary>Gets the page of the holder that states the terms, when the editors recorded one.</summary>
    public Uri? PolicyPage { get; init; }
}

/// <summary>A captions file of a video in one language.</summary>
/// <param name="Language">The language of the captions.</param>
/// <param name="File">The file, relative to the content folder.</param>
public sealed record CaptionTrack(string Language, string File);

/// <summary>
/// An image or a video as content: what it shows and how, who made it and when, whether it may be shown. A file of
/// the item is a path inside the content folder; it is opened only by the route that delivers media, after the
/// projection has allowed the item for the visitor.
/// </summary>
public sealed class MediaItem
{
    /// <summary>Gets the identifier.</summary>
    public required MediaId Id { get; init; }

    /// <summary>Gets whether the item is an image or a video.</summary>
    public required MediaKind Kind { get; init; }

    /// <summary>Gets the title.</summary>
    public required LocalizedText Label { get; init; }

    /// <summary>Gets the caption.</summary>
    public required LocalizedText Caption { get; init; }

    /// <summary>Gets the text alternative of an image, when one exists.</summary>
    public LocalizedText? AltText { get; init; }

    /// <summary>Gets what is known about the text alternative.</summary>
    public AltTextStatus AltTextStatus { get; init; }

    /// <summary>Gets the file of the item relative to the content folder, or <see langword="null"/> for a reference to an item held elsewhere.</summary>
    public string? File { get; init; }

    /// <summary>Gets the address of the item where it is held elsewhere, when it is not bundled.</summary>
    public Uri? RemoteFile { get; init; }

    /// <summary>Gets the public page that describes the item, when there is one.</summary>
    public Uri? CataloguePage { get; init; }

    /// <summary>Gets who made the item, as far as it is known, in the editors' words.</summary>
    public required string Creator { get; init; }

    /// <summary>Gets what is established about the right to show the item.</summary>
    public required MediaRights Rights { get; init; }

    /// <summary>Gets how the item relates to what it shows.</summary>
    public required MediaRepresentation Representation { get; init; }

    /// <summary>Gets the editors' note on what exactly the item shows and what it does not.</summary>
    public string? RepresentationScope { get; init; }

    /// <summary>Gets when the item was made. It is the time of the item, never of the story it shows.</summary>
    public required TemporalExpression Created { get; init; }

    /// <summary>Gets the entities the item shows.</summary>
    public ImmutableArray<EntityId> DepictedEntities { get; init; } = [];

    /// <summary>Gets the events the item shows.</summary>
    public ImmutableArray<EventId> DepictedEvents { get; init; } = [];

    /// <summary>Gets the relations the item explains.</summary>
    public ImmutableArray<RelationId> RelatedRelations { get; init; } = [];

    /// <summary>Gets the sources that support the item's description.</summary>
    public required ImmutableArray<SourceId> Sources { get; init; }

    /// <summary>
    /// Gets the reading level from which the item may be shown – with its poster, caption, captions and transcript,
    /// which all give away what the item gives away.
    /// </summary>
    public required ReadingLevel Level { get; init; }

    /// <summary>
    /// Gets whether the editors allow the item to be shown in the local demonstration. It is one condition; the
    /// reading boundary is another.
    /// </summary>
    public required bool DemoRenderAllowed { get; init; }

    /// <summary>Gets the editors' reason for <see cref="DemoRenderAllowed"/>.</summary>
    public string? DemoRenderReason { get; init; }

    /// <summary>Gets the poster image of a video, relative to the content folder.</summary>
    public string? Poster { get; init; }

    /// <summary>Gets the captions of a video.</summary>
    public ImmutableArray<CaptionTrack> Captions { get; init; } = [];

    /// <summary>Gets the plain-text transcript of a video, relative to the content folder.</summary>
    public string? Transcript { get; init; }

    /// <summary>Gets other formats of the same image, as format and file relative to the content folder.</summary>
    public ImmutableArray<(string Format, string File)> Renditions { get; init; } = [];

    /// <summary>Gets the playing time of a video in seconds. It is not the time the video is about.</summary>
    public decimal? DurationSeconds { get; init; }

    /// <summary>Gets whether a video has sound.</summary>
    public bool? HasAudio { get; init; }

    /// <summary>Gets the width in pixels, when it is known.</summary>
    public int? Width { get; init; }

    /// <summary>Gets the height in pixels, when it is known.</summary>
    public int? Height { get; init; }

    /// <summary>Gets the title the holding institution gives the item.</summary>
    public string? CatalogueTitle { get; init; }

    /// <summary>Gets the number the holding institution gives the item.</summary>
    public string? AccessionNumber { get; init; }

    /// <summary>Gets the credit line the holding institution asks for.</summary>
    public string? CreditLine { get; init; }
}
