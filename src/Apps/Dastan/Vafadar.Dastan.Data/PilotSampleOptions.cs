namespace Vafadar.Dastan.Data;

/// <summary>Where a sample lives and which translations belong to it.</summary>
public sealed class PilotSampleOptions
{
    /// <summary>Gets the folder of the sample file; the local paths of the sample are relative to it.</summary>
    public required string SampleDirectory { get; init; }

    /// <summary>
    /// Gets the folder that holds the files of the collection. A media file of the sample has to be inside it, and
    /// the read model names the file relative to it ("media/diagram.png").
    /// </summary>
    public required string ContentRoot { get; init; }

    /// <summary>
    /// Gets the folder that the editors' own working documents have to be inside. The sample may point to them for
    /// reviewers; they are checked and never become part of the read model.
    /// </summary>
    public required string ReviewRoot { get; init; }

    /// <summary>Gets a value indicating whether a local file the sample names has to exist.</summary>
    public bool CheckFiles { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether only reviewed texts are taken: a translation that is a draft is checked
    /// like any other and then left out. This is how a collection is read for the public.
    /// </summary>
    public bool ReviewedTextsOnly { get; init; }

    /// <summary>Gets the translation documents of the sample (<see cref="PilotSample.TranslationVersion"/>), as JSON.</summary>
    public IReadOnlyList<string> Translations { get; init; } = [];
}
