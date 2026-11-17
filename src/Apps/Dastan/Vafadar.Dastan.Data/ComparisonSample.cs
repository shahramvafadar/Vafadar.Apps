using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Vafadar.Dastan.Core;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Data;

/// <summary>
/// Reads a comparison – lanes of periods and single events over one kind of time – from its editorial file and the
/// translations beside it (<c>name.json</c>, <c>name.fa.json</c>). Reading is strict in the same way as for a
/// collection: a field the reader does not know, a missing one, a name it has no meaning for or a time it cannot
/// read is a refusal with the list of what is wrong, never a guess.
/// </summary>
public static partial class ComparisonSample
{
    /// <summary>The version of the format of a comparison this reader understands.</summary>
    public const string FormatVersion = "dastan-comparison-0.1";

    /// <summary>The version of the format of a translation of a comparison.</summary>
    public const string TranslationVersion = "dastan-comparison-translation-0.1";

    private const string SourceLanguage = "en";

    private static readonly JsonSerializerOptions Options = CreateOptions();

    /// <summary>Gets how a time is read from JSON: the form of <c>Vafadar.Temporal</c>, with nothing unknown accepted.</summary>
    internal static JsonSerializerOptions TimeOptions => Options;

    /// <summary>Gets the names of the review states as the files write them, with a small first letter.</summary>
    internal static IEnumerable<string> ReviewNames => Enum.GetNames<ReviewStatus>().Select(name => char.ToLowerInvariant(name[0]) + name[1..]);

    /// <summary>Reads every comparison of a folder, in the order of their file names. A folder that does not exist holds none.</summary>
    /// <exception cref="SampleFormatException">A file cannot be used; the problems name the file.</exception>
    public static ImmutableArray<Comparison> LoadAll(string folder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        if (!Directory.Exists(folder))
        {
            return [];
        }

        return
        [
            .. Directory.EnumerateFiles(folder, "*.json")
                .Where(file => !Path.GetFileNameWithoutExtension(file).Contains('.', StringComparison.Ordinal))
                .Order(StringComparer.Ordinal)
                .Select(Load),
        ];
    }

    /// <summary>Reads a comparison with the translations that lie beside it.</summary>
    /// <exception cref="SampleFormatException">The file or one of its translations cannot be used.</exception>
    public static Comparison Load(string file)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(file);
        var path = Path.GetFullPath(file);
        var translations = Directory
            .EnumerateFiles(Path.GetDirectoryName(path)!, Path.GetFileNameWithoutExtension(path) + ".*.json")
            .Order(StringComparer.Ordinal)
            .Select(File.ReadAllText)
            .ToArray();
        try
        {
            return Read(File.ReadAllText(path), translations);
        }
        catch (SampleFormatException refused)
        {
            throw new SampleFormatException([.. refused.Problems.Select(problem => $"{Path.GetFileName(path)}: {problem}")], refused);
        }
    }

    /// <summary>Reads a comparison from its JSON and the JSON of its translations.</summary>
    /// <exception cref="SampleFormatException">The comparison or a translation cannot be used.</exception>
    public static Comparison Read(string json, IReadOnlyList<string>? translations = null)
    {
        ArgumentNullException.ThrowIfNull(json);
        var document = Parse<ComparisonDocument>(json, "The comparison");
        var problems = new List<string>();
        if (document.FormatVersion != FormatVersion)
        {
            throw new SampleFormatException([$"formatVersion is \"{document.FormatVersion}\"; this reader understands \"{FormatVersion}\"."]);
        }

        var texts = (translations ?? []).Select(text => Parse<TranslationDocument>(text, "A translation")).ToList();
        foreach (var translation in texts)
        {
            Check(translation, document, problems);
        }

        if (document.Status is not ("draft" or "reviewed"))
        {
            problems.Add($"status has the unknown value \"{document.Status}\".");
        }

        if (!Enum.TryParse<ComparisonFrame>(document.Frame, ignoreCase: true, out var frame) || !Enum.IsDefined(frame) || document.Frame.Any(char.IsDigit))
        {
            problems.Add($"frame has the unknown value \"{document.Frame}\".");
        }

        // The identifier is a part of an address, a key is the name of a row on the page.
        foreach (var name in document.Lanes
            .SelectMany(lane => lane.Periods.Concat(lane.Events).Select(item => item.Key).Prepend(lane.Key))
            .Prepend(document.Id)
            .Where(name => !AddressName().IsMatch(name)))
        {
            problems.Add($"\"{name}\" is not a name for an address: lower-case letters, digits and single hyphens between them.");
        }

        var sources = document.Sources.Select(source => ReadSource(source, problems)).ToList();
        var lanes = document.Lanes.Select(lane => ReadLane(lane, texts, problems)).ToList();
        if (problems.Count > 0)
        {
            throw new SampleFormatException(problems);
        }

        try
        {
            return Comparison.Create(
                new ComparisonId(document.Id),
                Localized(document.Title, texts, translation => translation.Title),
                Localized(document.Summary, texts, translation => translation.Summary),
                frame,
                document.Show.From,
                document.Show.To,
                [.. lanes],
                [.. sources.OfType<Source>()],
                new PublicationState(document.Publication.LocalDemoAllowed, document.Publication.PublicReleaseApproved));
        }
        catch (ArgumentException refused)
        {
            throw new SampleFormatException([refused.Message], refused);
        }
    }

    /// <summary>Returns the fields that take one of a fixed set of values, with the values this reader understands.</summary>
    internal static ImmutableArray<ContentChoices> Choices()
    {
        // The readers compare without regard to case; the files write the names with a small first letter.
        static ImmutableArray<ContentChoice> Of(IEnumerable<string> names) => [.. names.Select(name => new ContentChoice(char.ToLowerInvariant(name[0]) + name[1..]))];

        return
        [
            new("status", "The state of the file as a whole, for editors.", Of(["draft", "reviewed"])),
            new("frame", "The kind of time all lanes share. Years of the common era and before it are years of the Gregorian calendar (D-043).", Of(Enum.GetNames<ComparisonFrame>())),
            new("sources[].kind", "What kind of source it is.", Of(Enum.GetNames<SourceKind>())),
            new("lanes[].periods[].reviewStatus, lanes[].events[].reviewStatus", "How far a statement has been checked.", Of(Enum.GetNames<ReviewStatus>())),
            new("translation: review", "The state of a translation document as a whole.", Of(["draft", "reviewed"])),
        ];
    }

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex AddressName();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };
        foreach (var converter in TemporalJson.Converters)
        {
            options.Converters.Add(converter);
        }

        return options;
    }

    private static T Parse<T>(string json, string what)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, Options) ?? throw new SampleFormatException([$"{what} is empty."]);
        }
        catch (JsonException refused)
        {
            throw new SampleFormatException([$"{what} cannot be read: {refused.Message}"], refused);
        }
    }

    // A translation belongs to this comparison, names a language of its own once, and translates only what exists.
    private static void Check(TranslationDocument translation, ComparisonDocument document, List<string> problems)
    {
        var name = $"The translation \"{translation.Language}\"";
        if (translation.FormatVersion != TranslationVersion)
        {
            problems.Add($"{name}: formatVersion is \"{translation.FormatVersion}\"; this reader understands \"{TranslationVersion}\".");
        }

        if (translation.Id != document.Id)
        {
            problems.Add($"{name} is for \"{translation.Id}\", not for \"{document.Id}\".");
        }

        if (translation.Language == SourceLanguage || !IsLanguage(translation.Language))
        {
            problems.Add($"{name}: language is not a language tag of a translation.");
        }

        if (translation.Review is not ("draft" or "reviewed"))
        {
            problems.Add($"{name}: review has the unknown value \"{translation.Review}\".");
        }

        var lanes = document.Lanes.Select(lane => lane.Key).ToHashSet(StringComparer.Ordinal);
        var items = document.Lanes.SelectMany(lane => lane.Periods.Select(period => period.Key).Concat(lane.Events.Select(single => single.Key))).ToHashSet(StringComparer.Ordinal);
        problems.AddRange(translation.Lanes.Keys.Where(key => !lanes.Contains(key)).Select(key => $"{name} names the lane \"{key}\", which the comparison does not have."));
        problems.AddRange(translation.Items.Keys.Where(key => !items.Contains(key)).Select(key => $"{name} names \"{key}\", which the comparison does not have."));
    }

    private static bool IsLanguage(string tag) => tag.Length is >= 2 and <= 8 && tag.All(character => character is (>= 'a' and <= 'z') or '-');

    private static LocalizedText Localized(string text, List<TranslationDocument> translations, Func<TranslationDocument, string?> wording)
    {
        var localized = LocalizedText.Of(SourceLanguage, text);
        foreach (var translation in translations)
        {
            if (wording(translation) is { Length: > 0 } translated)
            {
                localized = localized.With(translation.Language, translated, translation.Review == "reviewed" ? TextReview.Reviewed : TextReview.Draft);
            }
        }

        return localized;
    }

    private static ComparisonLane ReadLane(LaneDocument lane, List<TranslationDocument> translations, List<string> problems) => new()
    {
        Key = lane.Key,
        Label = Localized(lane.Label, translations, translation => translation.Lanes.GetValueOrDefault(lane.Key)),
        Periods =
        [
            .. lane.Periods.Select(period =>
            {
                if (period.Time is not Period)
                {
                    problems.Add($"The time of the period \"{period.Key}\" is not a period.");
                }

                return new ComparisonPeriod
                {
                    Key = period.Key,
                    Label = Localized(period.Label, translations, translation => translation.Items.GetValueOrDefault(period.Key)?.Label),
                    Summary = period.Summary is null ? null : Localized(period.Summary, translations, translation => translation.Items.GetValueOrDefault(period.Key)?.Summary),
                    Time = period.Time as Period ?? new Period(PeriodStart.Unknown, PeriodEnd.Unknown),
                    Sources = [.. period.SourceIds.Select(id => new SourceId(id))],
                    Review = Review(period.ReviewStatus, period.Key, problems),
                };
            }),
        ],
        Events =
        [
            .. lane.Events.Select(single => new ComparisonEvent
            {
                Key = single.Key,
                Label = Localized(single.Label, translations, translation => translation.Items.GetValueOrDefault(single.Key)?.Label),
                Summary = single.Summary is null ? null : Localized(single.Summary, translations, translation => translation.Items.GetValueOrDefault(single.Key)?.Summary),
                Time = single.Time,
                Sources = [.. single.SourceIds.Select(id => new SourceId(id))],
                Review = Review(single.ReviewStatus, single.Key, problems),
            }),
        ],
    };

    private static ReviewStatus Review(string name, string key, List<string> problems)
    {
        if (Enum.TryParse<ReviewStatus>(name, ignoreCase: true, out var review) && Enum.IsDefined(review) && !name.Any(char.IsDigit))
        {
            return review;
        }

        problems.Add($"reviewStatus of \"{key}\" has the unknown value \"{name}\".");
        return ReviewStatus.Proposed;
    }

    private static Source? ReadSource(SourceDocument source, List<string> problems)
    {
        var known = Enum.TryParse<SourceKind>(source.Kind, ignoreCase: true, out var kind) && Enum.IsDefined(kind) && !source.Kind.Any(char.IsDigit);
        if (!known)
        {
            problems.Add($"kind of the source \"{source.Id}\" has the unknown value \"{source.Kind}\".");
        }

        // A source is opened by a visitor: only an address of the web that is protected on its way is one.
        if (!Uri.TryCreate(source.Url, UriKind.Absolute, out var address) || address.Scheme != Uri.UriSchemeHttps)
        {
            problems.Add($"url of the source \"{source.Id}\" is not an https address.");
            return null;
        }

        if (!DateOnly.TryParseExact(source.AccessedAt, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var accessed))
        {
            problems.Add($"accessedAt of the source \"{source.Id}\" is not a day written as yyyy-MM-dd.");
            return null;
        }

        return known
            ? new Source
            {
                Id = new SourceId(source.Id),
                Title = source.Title,
                Url = address,
                Kind = kind,
                AccessedOn = accessed,
                PassageLocator = source.PassageLocator,
                Level = new ReadingLevel(0),
            }
            : null;
    }

    // The shapes of the files. Every property is one of the format; "required" ones must be there.
    private sealed class ComparisonDocument
    {
        public required string FormatVersion { get; init; }

        public required string Id { get; init; }

        public required string Status { get; init; }

        public string? Note { get; init; }

        public required string Frame { get; init; }

        public required string Title { get; init; }

        public required string Summary { get; init; }

        public required ShowDocument Show { get; init; }

        public required PublicationDocument Publication { get; init; }

        public required List<SourceDocument> Sources { get; init; }

        public required List<LaneDocument> Lanes { get; init; }
    }

    private sealed class ShowDocument
    {
        public required TimePosition From { get; init; }

        public required TimePosition To { get; init; }
    }

    private sealed class PublicationDocument
    {
        public required bool LocalDemoAllowed { get; init; }

        public required bool PublicReleaseApproved { get; init; }
    }

    private sealed class SourceDocument
    {
        public required string Id { get; init; }

        public required string Title { get; init; }

        public required string Url { get; init; }

        public required string Kind { get; init; }

        public required string AccessedAt { get; init; }

        public string? PassageLocator { get; init; }
    }

    private sealed class LaneDocument
    {
        public required string Key { get; init; }

        public required string Label { get; init; }

        public required List<ItemDocument> Periods { get; init; }

        public required List<ItemDocument> Events { get; init; }
    }

    private sealed class ItemDocument
    {
        public required string Key { get; init; }

        public required string Label { get; init; }

        public string? Summary { get; init; }

        public required TemporalExpression Time { get; init; }

        [JsonPropertyName("sourceIds")]
        public required List<string> SourceIds { get; init; }

        public required string ReviewStatus { get; init; }
    }

    private sealed class TranslationDocument
    {
        public required string FormatVersion { get; init; }

        public required string Id { get; init; }

        public required string Language { get; init; }

        public required string Review { get; init; }

        public string? Note { get; init; }

        public string? Title { get; init; }

        public string? Summary { get; init; }

        public Dictionary<string, string> Lanes { get; init; } = [];

        public Dictionary<string, ItemTranslation> Items { get; init; } = [];
    }

    private sealed class ItemTranslation
    {
        public required string Label { get; init; }

        public string? Summary { get; init; }
    }
}
