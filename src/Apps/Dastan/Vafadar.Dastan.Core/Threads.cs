using System.Collections.Immutable;

namespace Vafadar.Dastan.Core;

/// <summary>
/// One stop of a thread: an entity, a step of a story or an image or video, with what the editors say about why
/// it is the next thing to look at. Exactly one of the three is named.
/// </summary>
/// <param name="Entity">The entity, when the stop is one.</param>
/// <param name="Event">The event, when the stop is a step of a story.</param>
/// <param name="Media">The item, when the stop is an image or a video.</param>
/// <param name="Note">Why the thread leads here: the reason a visitor is given before following it.</param>
public sealed record ThreadStop(EntityId? Entity, EventId? Event, MediaId? Media, LocalizedText Note);

/// <summary>
/// A thread the editors lay through a collection (R-EXP-04, B-025): a few stops in an order, each with the reason
/// it follows. A thread is a way to read, not a statement about the world – it has no sources of its own; its
/// stops have theirs.
/// </summary>
public sealed class DiscoveryThread
{
    /// <summary>Gets the identifier, unique among the threads of the collection.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the name of the thread.</summary>
    public required LocalizedText Label { get; init; }

    /// <summary>Gets what the thread is about, in a sentence.</summary>
    public required LocalizedText Summary { get; init; }

    /// <summary>Gets the reading stage from which the thread is offered. Its name and summary are read from then on.</summary>
    public required ReadingLevel Level { get; init; }

    /// <summary>Gets the stops, in the order of the thread; at least two.</summary>
    public required ImmutableArray<ThreadStop> Stops { get; init; }
}

/// <summary>A stop of a thread as a visitor gets it: with what it leads to.</summary>
/// <param name="Number">The number of the stop in its thread, from one.</param>
/// <param name="Stop">The stop.</param>
/// <param name="Entity">The entity, when the stop is one.</param>
/// <param name="Step">The step of a story, when the stop is one.</param>
/// <param name="Media">The image or video, when the stop is one.</param>
public sealed record ThreadStep(int Number, ThreadStop Stop, Entity? Entity, StoryStep? Step, MediaItem? Media);

/// <summary>A thread as far as the visitor may follow it.</summary>
/// <param name="Thread">The thread, with the stops that are shown and no other.</param>
/// <param name="Steps">The stops up to the first the visitor may not see yet.</param>
/// <param name="Continues">
/// Whether the thread goes on after what is shown. It says nothing about how far or where: a stop that is not
/// reached is neither named nor counted.
/// </param>
public sealed record ThreadView(DiscoveryThread Thread, ImmutableArray<ThreadStep> Steps, bool Continues);

/// <summary>Where a thread leads on from an entity: the thread, and its next stop with the reason.</summary>
/// <param name="Thread">The thread that passes the entity.</param>
/// <param name="Here">The stop at the entity.</param>
/// <param name="Next">The stop after it, when the visitor may see it.</param>
public sealed record ThreadOnward(DiscoveryThread Thread, ThreadStep Here, ThreadStep? Next);
