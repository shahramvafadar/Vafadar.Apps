namespace Vafadar.Temporal;

/// <summary>Which order of a story a position belongs to. The two differ whenever a story looks back or ahead.</summary>
public enum NarrativeOrder
{
    /// <summary>The order in which the work presents its events to the reader.</summary>
    Presentation = 0,

    /// <summary>The order in which the events happen inside the story's world.</summary>
    InWorld = 1,
}

/// <summary>
/// A place in an ordered sequence of a story: "the second event of this telling". It is an order, not a time – the
/// step from 2 to 3 says nothing about how long anything took, and no calendar date follows from it. Positions are
/// compared only inside the same sequence and the same order.
/// </summary>
public sealed record NarrativePosition : TimePosition
{
    private NarrativePosition(string sequence, NarrativeOrder order, int ordinal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sequence);
        if (!Enum.IsDefined(order))
        {
            throw new ArgumentOutOfRangeException(nameof(order), order, "Unknown order.");
        }

        if (ordinal < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(ordinal), ordinal, "Positions of a sequence start at 1.");
        }

        Sequence = sequence;
        Order = order;
        Ordinal = ordinal;
    }

    /// <summary>Gets the identifier of the sequence – a story in one edition, as the app names it.</summary>
    public string Sequence { get; }

    /// <summary>Gets which order of the story the sequence follows.</summary>
    public NarrativeOrder Order { get; }

    /// <summary>Gets the place in the sequence, starting at 1.</summary>
    public int Ordinal { get; }

    /// <summary>Creates a position in a sequence.</summary>
    public static NarrativePosition Of(string sequence, NarrativeOrder order, int ordinal) => new(sequence, order, ordinal);
}
