using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;

namespace Vafadar.Dastan.People;

/// <summary>A person of a tree file.</summary>
/// <param name="Key">The name of the record inside the file; it is kept nowhere.</param>
/// <param name="Name">The name of the person.</param>
/// <param name="Born">The year the person was born in, of the common era; nothing when the file says none.</param>
/// <param name="Died">The year the person died in; nothing when the file says none.</param>
/// <param name="Deceased">Whether the person is said to have died.</param>
public sealed record TreeFilePerson(string Key, string Name, int? Born, int? Died, bool Deceased);

/// <summary>A link of a tree file between two of its people.</summary>
/// <param name="Kind">One of <see cref="PeopleStore.LinkKinds"/>.</param>
/// <param name="From">The key of the parent, or of the first partner.</param>
/// <param name="To">The key of the child, or of the second partner.</param>
public sealed record TreeFileLink(string Kind, string From, string To);

/// <summary>
/// A family tree as a file (delivery plan, 25.2): what who runs the site writes into an account that asked for
/// it. The file says of a person a name, the years and the family and has no field for more (PRIV-003). It is
/// read strictly – a file that is not understood whole is not read at all – and is kept by nobody.
/// </summary>
/// <param name="Name">The name of the tree.</param>
/// <param name="People">The people, in the order of the file.</param>
/// <param name="Links">The links: parents first, then partners, each in the order of the file.</param>
public sealed record TreeFile(string Name, ImmutableArray<TreeFilePerson> People, ImmutableArray<TreeFileLink> Links)
{
    /// <summary>The format a tree file states.</summary>
    public const string Format = "dastan-tree-0.1";

    private static readonly string[] Fields = ["format", "name", "people", "parents", "partners"];
    private static readonly string[] PersonFields = ["key", "name", "born", "died", "deceased"];
    private static readonly string[] ParentFields = ["parent", "child", "kind"];

    /// <summary>
    /// Reads a tree file. Returns the tree, or every problem of the file – each with its place – and no tree.
    /// </summary>
    /// <param name="json">The text of the file.</param>
    public static (TreeFile? Tree, ImmutableArray<string> Problems) Read(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json, new JsonDocumentOptions { AllowTrailingCommas = false, CommentHandling = JsonCommentHandling.Disallow });
        }
        catch (JsonException refused)
        {
            return (null, [$"the file is not valid JSON ({refused.Message})"]);
        }

        using (document)
        {
            var problems = ImmutableArray.CreateBuilder<string>();
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return (null, ["the file is not a JSON object."]);
            }

            Unknown(root, Fields, "the file", problems);
            if (Text(root, "format") != Format)
            {
                problems.Add($"format: the file states {Text(root, "format") ?? "no format"}; this program reads {Format}.");
            }

            var name = Text(root, "name");
            if (string.IsNullOrWhiteSpace(name))
            {
                problems.Add("name: the tree has no name.");
            }

            var people = ImmutableArray.CreateBuilder<TreeFilePerson>();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var (person, index) in Items(root, "people", problems))
            {
                var place = $"people[{index}]";
                if (person.ValueKind != JsonValueKind.Object)
                {
                    problems.Add($"{place}: a person is an object.");
                    continue;
                }

                Unknown(person, PersonFields, place, problems);
                var key = Text(person, "key");
                var called = Text(person, "name");
                if (string.IsNullOrWhiteSpace(key))
                {
                    problems.Add($"{place}: the person has no key.");
                }
                else if (!keys.Add(key))
                {
                    problems.Add($"{place}: the key {key} is used twice.");
                }

                if (string.IsNullOrWhiteSpace(called))
                {
                    problems.Add($"{place}: the person has no name.");
                }

                var born = Year(person, "born", place, problems);
                var died = Year(person, "died", place, problems);
                var deceased = Flag(person, "deceased", place, problems);
                if (died is not null && deceased == false)
                {
                    problems.Add($"{place}: the person has a year of death and is said not to have died.");
                }

                if (born is { } first && died is { } last && last < first)
                {
                    problems.Add($"{place}: the person died before they were born.");
                }

                people.Add(new TreeFilePerson(key ?? string.Empty, called ?? string.Empty, born, died, deceased == true || died is not null));
            }

            if (people.Count == 0)
            {
                problems.Add("people: the tree has nobody.");
            }

            if (people.Count > PeopleStore.MostPeople)
            {
                problems.Add($"people: the tree has {people.Count.ToString(CultureInfo.InvariantCulture)} people; a tree holds {PeopleStore.MostPeople.ToString(CultureInfo.InvariantCulture)} at most.");
            }

            var links = ImmutableArray.CreateBuilder<TreeFileLink>();
            var linked = new HashSet<(string, string)>();
            foreach (var (parent, index) in Items(root, "parents", problems, needed: false))
            {
                var place = $"parents[{index}]";
                if (parent.ValueKind != JsonValueKind.Object)
                {
                    problems.Add($"{place}: a line of descent is an object with parent and child.");
                    continue;
                }

                Unknown(parent, ParentFields, place, problems);
                var kind = parent.TryGetProperty("kind", out _) ? Text(parent, "kind") : "parent";
                if (kind is null || !PeopleStore.IsParentage(kind))
                {
                    problems.Add($"{place}: the kind is one of parent, adoptive-parent and guardian.");
                    kind = "parent";
                }

                Link(kind, Text(parent, "parent"), Text(parent, "child"), place);
            }

            foreach (var (pair, index) in Items(root, "partners", problems, needed: false))
            {
                var place = $"partners[{index}]";
                if (pair.ValueKind != JsonValueKind.Array || pair.GetArrayLength() != 2 || pair.EnumerateArray().Any(each => each.ValueKind != JsonValueKind.String))
                {
                    problems.Add($"{place}: partners are a pair of two keys.");
                    continue;
                }

                Link("partner", pair[0].GetString(), pair[1].GetString(), place);
            }

            if (Ancestor(links) is { } loop)
            {
                problems.Add($"parents: {loop} would be their own ancestor.");
            }

            return problems.Count > 0 ? (null, problems.ToImmutable()) : (new TreeFile(name!.Trim(), people.ToImmutable(), links.ToImmutable()), []);

            void Link(string kind, string? from, string? to, string place)
            {
                foreach (var key in new[] { from, to })
                {
                    if (key is null || !keys.Contains(key))
                    {
                        problems.Add($"{place}: {(key is null ? "a key is missing" : $"there is no person with the key {key}")}.");
                    }
                }

                if (from is null || to is null || !keys.Contains(from) || !keys.Contains(to))
                {
                    return;
                }

                if (from == to)
                {
                    problems.Add($"{place}: {from} is linked with themself.");
                }
                else if (!linked.Add(string.CompareOrdinal(from, to) < 0 ? (from, to) : (to, from)))
                {
                    problems.Add($"{place}: {from} and {to} are linked already; two people hold one family role.");
                }
                else
                {
                    links.Add(new TreeFileLink(kind, from, to));
                }
            }
        }
    }

    // The first key that descends from itself, if any.
    private static string? Ancestor(IEnumerable<TreeFileLink> links)
    {
        var children = links.Where(link => PeopleStore.IsParentage(link.Kind)).ToLookup(link => link.From, link => link.To, StringComparer.Ordinal);
        foreach (var start in children.Select(group => group.Key))
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var open = new Stack<string>(children[start]);
            while (open.Count > 0)
            {
                var next = open.Pop();
                if (next == start)
                {
                    return start;
                }

                if (seen.Add(next))
                {
                    foreach (var child in children[next])
                    {
                        open.Push(child);
                    }
                }
            }
        }

        return null;
    }

    private static void Unknown(JsonElement element, string[] known, string place, ImmutableArray<string>.Builder problems)
    {
        foreach (var field in element.EnumerateObject().Where(field => !known.Contains(field.Name, StringComparer.Ordinal)))
        {
            problems.Add($"{place}: the field {field.Name} is not one a tree file has. A tree file says a name, the years and the family and nothing else.");
        }
    }

    private static string? Text(JsonElement element, string field) =>
        element.TryGetProperty(field, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static IEnumerable<(JsonElement Item, int Index)> Items(JsonElement root, string field, ImmutableArray<string>.Builder problems, bool needed = true)
    {
        if (!root.TryGetProperty(field, out var list))
        {
            if (needed)
            {
                problems.Add($"{field}: the list is missing.");
            }

            return [];
        }

        if (list.ValueKind != JsonValueKind.Array)
        {
            problems.Add($"{field}: this is a list.");
            return [];
        }

        return list.EnumerateArray().Select((item, index) => (item, index)).ToList();
    }

    private static int? Year(JsonElement person, string field, string place, ImmutableArray<string>.Builder problems)
    {
        if (!person.TryGetProperty(field, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var year) && year is >= 1 and <= 9999)
        {
            return year;
        }

        problems.Add($"{place}: {field} is a year of the common era, as a number – a year, never a day.");
        return null;
    }

    private static bool? Flag(JsonElement person, string field, string place, ImmutableArray<string>.Builder problems)
    {
        if (!person.TryGetProperty(field, out var value))
        {
            return null;
        }

        if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return value.GetBoolean();
        }

        problems.Add($"{place}: {field} is true or false.");
        return null;
    }
}
