using System.Text.Json;
using Vafadar.Dastan.Core;

namespace Vafadar.Dastan.Data;

/// <summary>
/// Reads an editorial sample of the format <see cref="SchemaVersion"/> into the read model. The reader is strict: a
/// version it does not know, a field it does not know and a value it would have to guess are problems, and nothing
/// of the sample is used while one problem is open. The file is editorial input – it holds the whole story including
/// its ending and notes for reviewers – and is never served to a visitor.
/// </summary>
public static class PilotSample
{
    /// <summary>The version of the sample format this reader understands.</summary>
    public const string SchemaVersion = "dastan-pilot-0.2";

    /// <summary>The version of the translation documents this reader understands.</summary>
    public const string TranslationVersion = "dastan-translation-0.1";

    /// <summary>
    /// Reads a sample file with the translations next to it: for <c>story.json</c> every <c>story.&lt;language&gt;.json</c>
    /// of the same folder. The folder above the sample's folder holds the files of the collection, and the folder
    /// above that one the editors' working documents.
    /// </summary>
    /// <param name="sampleFile">The path of the sample.</param>
    /// <param name="reviewedTextsOnly">Whether a translation that is a draft is left out, as for the public.</param>
    /// <returns>The collection.</returns>
    /// <exception cref="SampleFormatException">The sample or a translation cannot be used.</exception>
    public static Catalogue Load(string sampleFile, bool reviewedTextsOnly = false)
    {
        var file = Path.GetFullPath(sampleFile);
        var directory = Path.GetDirectoryName(file)!;
        var content = Path.GetDirectoryName(directory) ?? directory;
        var translations = Directory
            .EnumerateFiles(directory, Path.GetFileNameWithoutExtension(file) + ".*.json")
            .Order(StringComparer.Ordinal)
            .Select(File.ReadAllText)
            .ToArray();
        return Read(File.ReadAllText(file), new PilotSampleOptions
        {
            SampleDirectory = directory,
            ContentRoot = content,
            ReviewRoot = Path.GetDirectoryName(content) ?? content,
            Translations = translations,
            ReviewedTextsOnly = reviewedTextsOnly,
        });
    }

    /// <summary>Reads a sample from its JSON text.</summary>
    /// <param name="json">The sample.</param>
    /// <param name="options">Where the sample lives and which translations belong to it.</param>
    /// <returns>The collection.</returns>
    /// <exception cref="SampleFormatException">The sample or a translation cannot be used.</exception>
    public static Catalogue Read(string json, PilotSampleOptions options)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(options);

        var problems = new List<string>();
        var reader = new PilotReader(options, problems);
        using var document = Parse(json, "sample", problems);
        if (document is not null)
        {
            reader.Read(document.RootElement);
        }

        if (problems.Count > 0)
        {
            throw new SampleFormatException(problems);
        }

        try
        {
            return reader.Build();
        }
        catch (CatalogueException exception)
        {
            throw new SampleFormatException(exception.Problems, exception);
        }
    }

    /// <summary>Parses a document, reporting text that is not JSON, a field given twice and anything but an object.</summary>
    internal static JsonDocument? Parse(string json, string name, List<string> problems)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json, new JsonDocumentOptions { AllowDuplicateProperties = false });
        }
        catch (JsonException exception)
        {
            problems.Add($"{name}: it is not valid JSON ({exception.Message})");
            return null;
        }

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            document.Dispose();
            problems.Add($"{name}: it is not a JSON object.");
            return null;
        }

        return document;
    }
}
