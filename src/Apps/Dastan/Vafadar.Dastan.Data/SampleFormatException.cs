namespace Vafadar.Dastan.Data;

/// <summary>
/// An editorial sample cannot be used: it is not in the format the reader understands, or the collection it describes
/// is not consistent. Every problem names its record, so an editor can find it.
/// </summary>
public sealed class SampleFormatException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="SampleFormatException"/> class.</summary>
    /// <param name="problems">The problems; at least one.</param>
    /// <param name="innerException">The exception the problems come from, if any.</param>
    public SampleFormatException(IReadOnlyList<string> problems, Exception? innerException = null)
        : base($"The sample cannot be used ({problems.Count} problems). First: {problems[0]}", innerException)
    {
        Problems = problems;
    }

    /// <summary>Gets every problem that was found, each starting with the name of its record.</summary>
    public IReadOnlyList<string> Problems { get; }
}
