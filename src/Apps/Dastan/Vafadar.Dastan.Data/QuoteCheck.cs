using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Vafadar.Dastan.Data;

/// <summary>What the comparison of the quotes of a collection with the copies of its sources found.</summary>
/// <param name="Compared">How many quotes were looked for in a copy of their source.</param>
/// <param name="Found">How many of them stand in it.</param>
/// <param name="Problems">What is wrong, one line each: a quote that is not in its source, a record or a source that does not exist, a record that does not cite the source.</param>
/// <param name="WithoutCopy">The sources a quote names of which the folder holds no copy; their quotes were not compared.</param>
/// <param name="WithoutQuote">How many statements of the collection – relations, events and statements with a date – have no quote in the file.</param>
public sealed record QuoteReport(int Compared, int Found, ImmutableArray<string> Problems, ImmutableArray<string> WithoutCopy, int WithoutQuote)
{
    /// <summary>Gets a value indicating whether every quote was compared and found.</summary>
    public bool InOrder => Problems.IsEmpty && WithoutCopy.IsEmpty;
}

/// <summary>
/// Compares the passages the statements of a collection rest on with the sources they are taken from (backlog
/// E4). The passages are a file of the writer – record, source, the words quoted – and the sources are copies
/// the writer saved as plain text, one file per source. Neither is part of a collection and neither is read by
/// the site: a quote of a source and a copy of it are somebody else's text, kept by who works with it. The
/// check asks no address of the network, changes nothing and records no review.
/// </summary>
public static class QuoteCheck
{
    /// <summary>The version of the file of quotes.</summary>
    public const string FormatVersion = "dastan-quotes-0.1";

    /// <summary>Compares the quotes of a file with the copies of the sources in a folder.</summary>
    /// <param name="collection">The file of the collection.</param>
    /// <param name="quotes">The file of quotes.</param>
    /// <param name="sources">The folder of the copies: <c>{source id}.txt</c>.</param>
    public static QuoteReport Compare(string collection, string quotes, string sources)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(quotes);
        ArgumentNullException.ThrowIfNull(sources);

        var problems = ImmutableArray.CreateBuilder<string>();
        if (Read(collection, problems) is not { } written || Read(quotes, problems) is not { } stated)
        {
            return new(0, 0, problems.ToImmutable(), [], 0);
        }

        // What a statement of the collection is, and the sources it cites.
        var cited = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var statements = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (group, key) in new[] { ("entities", "id"), ("relations", "id"), ("events", "id"), ("media", "id") })
        {
            foreach (var record in written[group] as JsonArray ?? [])
            {
                if (Text(record?[key]) is not { } id)
                {
                    continue;
                }

                cited[id] = Ids(record!["sourceIds"]);
                if (group is "relations" or "events")
                {
                    statements.Add(id);
                }

                foreach (var dated in record["dated"] as JsonArray ?? [])
                {
                    if (Text(dated?["key"]) is { } statement)
                    {
                        cited[statement] = Ids(dated!["sourceIds"]);
                        statements.Add(statement);
                    }
                }
            }
        }

        var known = (written["sources"] as JsonArray ?? []).Select(source => Text(source?["id"])).OfType<string>().ToHashSet(StringComparer.Ordinal);
        if (Text(stated["formatVersion"]) != FormatVersion)
        {
            problems.Add($"{Path.GetFileName(quotes)}: formatVersion is not \"{FormatVersion}\".");
        }

        if (Text(stated["datasetId"]) is { } dataset && dataset != Text(written["datasetId"]))
        {
            problems.Add($"{Path.GetFileName(quotes)}: datasetId \"{dataset}\" is not the collection \"{Text(written["datasetId"])}\".");
        }

        var copies = new Dictionary<string, string?>(StringComparer.Ordinal);
        var withoutCopy = new SortedSet<string>(StringComparer.Ordinal);
        var quoted = new HashSet<string>(StringComparer.Ordinal);
        int compared = 0, found = 0, line = 0;
        foreach (var entry in stated["quotes"] as JsonArray ?? [])
        {
            line++;
            var record = Text(entry?["record"]);
            var source = Text(entry?["sourceId"]);
            var words = Text(entry?["quote"]);
            if (record is null || source is null || string.IsNullOrWhiteSpace(words))
            {
                problems.Add($"quote {line}: record, sourceId and quote are all needed.");
                continue;
            }

            if (!cited.TryGetValue(record, out var sourcesOfRecord))
            {
                problems.Add($"quote {line}: the collection has no record \"{record}\".");
                continue;
            }

            quoted.Add(record);
            if (!known.Contains(source))
            {
                problems.Add($"quote {line} ({record}): the collection has no source \"{source}\".");
                continue;
            }

            if (!sourcesOfRecord.Contains(source))
            {
                problems.Add($"quote {line}: {record} does not cite {source}.");
                continue;
            }

            if (!copies.TryGetValue(source, out var copy))
            {
                var file = Path.Combine(sources, source + ".txt");
                copy = File.Exists(file) ? Plain(File.ReadAllText(file)) : null;
                copies[source] = copy;
            }

            if (copy is null)
            {
                withoutCopy.Add(source);
                continue;
            }

            compared++;
            if (copy.Contains(Plain(words), StringComparison.Ordinal))
            {
                found++;
            }
            else
            {
                problems.Add($"quote {line} ({record}): not found in {source}: \"{Shown(words)}\"");
            }
        }

        return new(compared, found, problems.ToImmutable(), [.. withoutCopy], statements.Count(statement => !quoted.Contains(statement)));
    }

    /// <summary>
    /// Returns a text as it is compared: letters and digits only matter, not how a page sets them. Runs of
    /// blanks are one blank; the forms of a letter that mean the same are one (compatibility forms, the Arabic
    /// and the Persian yeh and kaf); marks that a copy may drop – vowel signs, the lengthening stroke, the
    /// joiners, a soft hyphen – are left out; quotation marks and dashes are of one kind; capitals are not told
    /// from small letters.
    /// </summary>
    public static string Plain(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var plain = new StringBuilder(text.Length);
        var blank = true;
        foreach (var character in text.Normalize(NormalizationForm.FormKC))
        {
            var letter = character switch
            {
                'ي' or 'ى' => 'ی',
                'ك' => 'ک',
                'ة' => 'ه',
                '‘' or '’' or '‚' or '′' or '`' => '\'',
                '“' or '”' or '„' or '«' or '»' or '″' => '"',
                '‐' or '‑' or '‒' or '–' or '—' or '−' => '-',
                _ => character,
            };
            if (letter is '‌' or '‍' or '­' or 'ـ' or '﻿' || CharUnicodeInfo.GetUnicodeCategory(letter) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsWhiteSpace(letter))
            {
                if (!blank)
                {
                    plain.Append(' ');
                    blank = true;
                }

                continue;
            }

            plain.Append(char.ToLowerInvariant(letter));
            blank = false;
        }

        return plain.ToString().TrimEnd();
    }

    private static string Shown(string words)
    {
        var one = string.Join(' ', words.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return one.Length <= 120 ? one : one[..120] + "…";
    }

    private static JsonObject? Read(string file, ImmutableArray<string>.Builder problems)
    {
        if (!File.Exists(file))
        {
            problems.Add($"{file}: there is no such file.");
            return null;
        }

        try
        {
            if (JsonNode.Parse(File.ReadAllText(file)) is JsonObject read)
            {
                return read;
            }
        }
        catch (JsonException)
        {
            // Said below, as for a file that holds something else.
        }

        problems.Add($"{file}: the file is no JSON object.");
        return null;
    }

    private static string? Text(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    private static HashSet<string> Ids(JsonNode? node) => (node as JsonArray ?? []).Select(Text).OfType<string>().ToHashSet(StringComparer.Ordinal);
}
