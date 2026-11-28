using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Vafadar.Dastan.Core;

namespace Vafadar.Dastan.Data;

/// <summary>
/// Reads the list of kinds of a content root (ADR 0029): <c>kinds.json</c> beside the folders <c>samples</c> and
/// <c>comparisons</c>, and its translations <c>kinds.&lt;language&gt;.json</c> beside it. The reader is strict like
/// the readers of collections: a field it does not know, a type it does not know, a parent that is not in the
/// list and a loop of parents are problems, and nothing of the list is used while one is open.
/// </summary>
public static partial class KindsFile
{
    /// <summary>The version of the format of the list this reader understands.</summary>
    public const string FormatVersion = "dastan-kinds-0.1";

    /// <summary>The version of the format of a translation of the list.</summary>
    public const string TranslationVersion = "dastan-kinds-translation-0.1";

    /// <summary>The name of the file of the list in a content root.</summary>
    public const string FileName = "kinds.json";

    /// <summary>
    /// Whether every entity of a collection has to name a kind (K4 of the plan of kinds). It is off while the
    /// library is being classified: the check only counts the entities without one. Turned on, a collection
    /// with an entity without a kind cannot be used – the one line to change when the library is classified.
    /// </summary>
    public const bool EveryRecordHasAKind = false;

    // The language the list is written in; a translation gives another.
    private const string SourceLanguage = "en";

    /// <summary>
    /// Reads the list of a content root with its translations. A content root without <c>kinds.json</c> has no
    /// kinds: then no record may name one.
    /// </summary>
    /// <param name="contentRoot">The folder that holds <c>samples</c>, <c>comparisons</c> and <c>media</c>.</param>
    /// <param name="reviewedTextsOnly">Whether a translation that is a draft is left out, as for the public.</param>
    /// <exception cref="SampleFormatException">The list or a translation cannot be used; the problems name the file.</exception>
    public static KindList Load(string contentRoot, bool reviewedTextsOnly = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentRoot);
        var file = Path.Combine(contentRoot, FileName);
        if (!File.Exists(file))
        {
            return KindList.Empty;
        }

        var translations = Directory
            .EnumerateFiles(contentRoot, Path.GetFileNameWithoutExtension(FileName) + ".*.json")
            .Order(StringComparer.Ordinal)
            .Select(File.ReadAllText)
            .ToArray();
        try
        {
            return Read(File.ReadAllText(file), translations, reviewedTextsOnly);
        }
        catch (SampleFormatException refused)
        {
            throw new SampleFormatException([.. refused.Problems.Select(problem => $"{FileName}: {problem}")], refused);
        }
    }

    /// <summary>Reads a list from its JSON and the JSON of its translations.</summary>
    /// <param name="json">The list.</param>
    /// <param name="translations">The translations, as JSON.</param>
    /// <param name="reviewedTextsOnly">Whether a translation that is a draft is left out, as for the public.</param>
    /// <exception cref="SampleFormatException">The list or a translation cannot be used.</exception>
    public static KindList Read(string json, IReadOnlyList<string>? translations = null, bool reviewedTextsOnly = false)
    {
        ArgumentNullException.ThrowIfNull(json);
        var problems = new List<string>();
        using var document = PilotSample.Parse(json, "the list of kinds", problems);
        if (document is null)
        {
            throw new SampleFormatException(problems);
        }

        var root = new JsonRecord(document.RootElement, "the list of kinds", problems);
        var version = root.Text("formatVersion");
        if (version is not null && version != FormatVersion)
        {
            throw new SampleFormatException([$"formatVersion is \"{version}\"; this reader understands \"{FormatVersion}\"."]);
        }

        root.Notes("note");
        var written = new List<Written>();
        foreach (var record in root.Children("kinds"))
        {
            if (ReadKind(record) is { } kind)
            {
                if (written.Exists(other => other.Key == kind.Key))
                {
                    record.Problem("the key is given to two kinds.");
                }
                else
                {
                    written.Add(kind);
                }
            }
        }

        root.Finish();
        var texts = ReadTranslations(translations ?? [], written, problems);
        if (reviewedTextsOnly)
        {
            texts.RemoveAll(translation => translation.Review == TextReview.Draft);
        }

        // A parent is a kind of the list, and following the parents comes to an end.
        foreach (var kind in written.Where(kind => kind.Parent is not null))
        {
            if (!written.Exists(other => other.Key == kind.Parent))
            {
                problems.Add($"{kind.Key}: the parent {kind.Parent} is not a kind of the list.");
            }
            else if (Loops(kind, written))
            {
                problems.Add($"{kind.Key}: following its parents leads back to it.");
            }
        }

        if (problems.Count > 0)
        {
            throw new SampleFormatException(problems);
        }

        var made = new Dictionary<string, Kind>(StringComparer.Ordinal);
        Kind Make(Written kind) => made.TryGetValue(kind.Key, out var done) ? done : made[kind.Key] = new Kind
        {
            Key = kind.Key,
            Label = Localized(kind.Label, texts, translation => translation.Labels.GetValueOrDefault(kind.Key)),
            Meaning = Localized(kind.Meaning, texts, translation => translation.Meanings.GetValueOrDefault(kind.Key)),
            Types = kind.Types,
            Icon = kind.Icon,
            Parent = kind.Parent is null ? null : Make(written.Single(other => other.Key == kind.Parent)),
            Retired = kind.Retired,
            Order = written.IndexOf(kind),
        };

        return new KindList(written.Select(Make));
    }

    /// <summary>Returns the fields of the list that take one of a fixed set of values, with the values this reader understands.</summary>
    internal static ImmutableArray<ContentChoices> Choices() =>
    [
        new("kinds[].types", "The types of entity a kind may be given to, by the keys of the types; a comparison's periods count as period and its events as event.", [.. EntityTypes.All.Select(type => new ContentChoice(type.Key, type.Meaning))]),
        new("translation: review", "The state of a translation document as a whole.", [new("draft"), new("reviewed")]),
    ];

    private static Written? ReadKind(JsonRecord record)
    {
        var key = record.Identifier("key");
        if (key is not null)
        {
            record.Name = $"the kind {key}";
            if (!KeyPattern().IsMatch(key))
            {
                record.Problem("the key is not a name of small and capital letters and digits that begins with a small letter: war, naturalEvent.");
                key = null;
            }
        }

        var label = record.Text("label");
        var meaning = record.Text("meaning");
        var typeKeys = record.Texts("types");
        var icon = record.Identifier("icon");
        var parent = record.Identifier("parent", required: false);
        var retired = record.Flag("retired", required: false) ?? false;
        record.Notes("note");
        var types = ImmutableArray.CreateBuilder<EntityType>();
        foreach (var typeKey in typeKeys)
        {
            if (EntityTypes.Find(typeKey) is { } type)
            {
                if (types.Contains(type))
                {
                    record.Problem($"types names {typeKey} twice.");
                }
                else
                {
                    types.Add(type);
                }
            }
            else
            {
                record.Problem($"types names {typeKey}, which is no type of entity; the types are {string.Join(", ", EntityTypes.All.Select(known => known.Key))}.");
            }
        }

        if (record.IsStated("types") && typeKeys.IsEmpty)
        {
            record.Problem("types names no type: a kind is given to at least one type of entity.");
        }

        if (icon is not null && !KeyPattern().IsMatch(icon))
        {
            record.Problem("icon is not the name of a drawing of the site: small letters and digits.");
            icon = null;
        }

        record.Finish();
        return key is null || label is null || meaning is null || icon is null || types.Count == 0 || types.Count != typeKeys.Length
            ? null
            : new Written(key, label, meaning, types.ToImmutable(), icon, parent, retired);
    }

    private static bool Loops(Written kind, List<Written> written)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal) { kind.Key };
        for (var parent = kind.Parent; parent is not null; parent = written.Find(other => other.Key == parent)?.Parent)
        {
            if (!seen.Add(parent))
            {
                return true;
            }
        }

        return false;
    }

    private static List<Translation> ReadTranslations(IReadOnlyList<string> translations, List<Written> written, List<string> problems)
    {
        var read = new List<Translation>();
        for (var index = 0; index < translations.Count; index++)
        {
            var name = $"translation {index + 1}";
            using var document = PilotSample.Parse(translations[index], name, problems);
            if (document is null)
            {
                continue;
            }

            var record = new JsonRecord(document.RootElement, name, problems);
            var version = record.Text("formatVersion");
            if (version is not null && version != TranslationVersion)
            {
                record.Problem($"the format {version} is not understood; this reader reads {TranslationVersion}.");
                continue;
            }

            var language = record.Text("language");
            if (language is not null)
            {
                record.Name = $"translation {language}";
                if (!LanguagePattern().IsMatch(language) || language == SourceLanguage)
                {
                    record.Problem($"language is not the tag of a language other than the one of the list ({SourceLanguage}).");
                    language = null;
                }
                else if (read.Exists(other => other.Language == language))
                {
                    record.Problem("the list has two translations into this language.");
                    language = null;
                }
            }

            var reviewWord = record.Text("review");
            TextReview? review = reviewWord switch
            {
                "draft" => TextReview.Draft,
                "reviewed" => TextReview.Reviewed,
                _ => null,
            };
            if (reviewWord is not null && review is null)
            {
                record.Problem($"review has the unknown value \"{reviewWord}\"; it is draft or reviewed.");
            }

            record.Notes("note");
            var translation = new Translation(language ?? name, review ?? TextReview.Draft);
            foreach (var (key, texts) in record.Named("kinds"))
            {
                if (!written.Exists(kind => kind.Key == key))
                {
                    texts.Problem("it translates a kind the list does not have.");
                }

                if (texts.Text("label", required: false) is { } label)
                {
                    translation.Labels[key] = label;
                }

                if (texts.Text("meaning", required: false) is { } meaning)
                {
                    translation.Meanings[key] = meaning;
                }

                texts.Finish();
            }

            record.Finish();
            if (language is not null && review is not null)
            {
                read.Add(translation);
            }
        }

        return read;
    }

    private static LocalizedText Localized(string text, List<Translation> translations, Func<Translation, string?> wording)
    {
        var localized = LocalizedText.Of(SourceLanguage, text);
        foreach (var translation in translations)
        {
            if (wording(translation) is { Length: > 0 } translated)
            {
                localized = localized.With(translation.Language, translated, translation.Review);
            }
        }

        return localized;
    }

    [GeneratedRegex("^[a-z][A-Za-z0-9]*\\z", RegexOptions.CultureInvariant)]
    private static partial Regex KeyPattern();

    [GeneratedRegex("^[a-z]{2,3}(-[A-Za-z0-9]{2,8})*\\z", RegexOptions.CultureInvariant)]
    private static partial Regex LanguagePattern();

    // A kind as the file writes it, before its parent is found.
    private sealed record Written(string Key, string Label, string Meaning, ImmutableArray<EntityType> Types, string Icon, string? Parent, bool Retired);

    // The texts of one translation, by the key of the kind.
    private sealed class Translation(string language, TextReview review)
    {
        public string Language => language;

        public TextReview Review => review;

        public Dictionary<string, string> Labels { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, string> Meanings { get; } = new(StringComparer.Ordinal);
    }
}
