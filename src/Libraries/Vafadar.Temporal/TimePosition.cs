namespace Vafadar.Temporal;

/// <summary>
/// One stated position in time, exactly as precise as the source: a calendar date or part of one
/// (<see cref="CalendarPosition"/>), an age before a reference (<see cref="AgePosition"/>) or a place in an ordered
/// story (<see cref="NarrativePosition"/>). What is asserted about a position – that something happened then, began
/// then, lies somewhere between two of them – is a <see cref="TemporalExpression"/>.
/// </summary>
public abstract record TimePosition
{
    private protected TimePosition()
    {
    }
}
