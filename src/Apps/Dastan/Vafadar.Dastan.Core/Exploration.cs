using System.Collections.Immutable;
using System.Globalization;
using System.Text;

namespace Vafadar.Dastan.Core;

/// <summary>The views there are of one thing.</summary>
public enum ExplorationView
{
    /// <summary>What it is: its page.</summary>
    Overview = 0,

    /// <summary>What it is connected with.</summary>
    Connections = 1,

    /// <summary>Its family.</summary>
    Family = 2,

    /// <summary>What is dated about it, over the years.</summary>
    Timeline = 3,

    /// <summary>The images and videos that show it.</summary>
    Pictures = 4,
}

/// <summary>What a visitor can leave out of a view.</summary>
public enum ExplorationFilter
{
    /// <summary>A stretch of years: only what lies in it.</summary>
    Years = 0,

    /// <summary>Without the single events of a chart of time.</summary>
    WithoutEvents = 1,

    /// <summary>Fewer lanes of a chart of time than there are.</summary>
    FewerLanes = 2,

    /// <summary>Only images, or only videos.</summary>
    KindOfPicture = 3,
}

/// <summary>
/// The state of exploring one thing (step 20.6, ADR 0025): what is explored, which view of it is shown, what is
/// chosen in it, what each view was told to show, and how far the reader has read. It is one contract for every
/// renderer – the still pages of the server, the drawing that moves, a picture that is saved – and knows none of
/// them: nothing here is a pixel, a zoom or a place on a canvas.
/// <para>
/// <b>The address.</b> The state is read from and written to the query of an address. Every view keeps the
/// names its own page always had, so every address that was ever passed on still means what it meant. What a
/// view was told is carried along while another view is shown, under a name no other view uses, so that coming
/// back finds the view as it was left. Two views call their manner <c>view</c>: carried, the direction of a
/// family tree is <c>tree</c> and the direction of a chart of time is <c>dir</c>.
/// </para>
/// <para>
/// <b>An address grants nothing.</b> Everything in it is a name of something public or a number; what it names
/// is asked of the projection of the visitor, which answers for how far they have read. A value that is not a
/// choice is not carried. How far the reader has read is part of the state and never of the address.
/// </para>
/// </summary>
public sealed class Exploration
{
    /// <summary>The name of what is chosen, in an address.</summary>
    public const string ChosenName = "sel";

    /// <summary>The most rows of a drawing of connections an address may name as open.</summary>
    public const int MostOpen = 40;

    /// <summary>The name of what is compared with what is explored, in an address.</summary>
    public const string ComparedName = "with";

    /// <summary>The most names that can be compared with what is explored: a chart of time stays one that can be read.</summary>
    public const int MostCompared = 5;

    private const int FurthestReach = 12;
    private const int MostLanes = 99;
    private const int MostScales = 7;

    // Every name of the state in the order an address writes them: the name it is carried under, the view it
    // belongs to (none: every view), the name the page of that view reads it by, and what makes a value one.
    private static readonly ImmutableArray<(string Name, ExplorationView? Of, string Native, Func<string, string?> Read)> Parts =
    [
        (ChosenName, null, ChosenName, Identifier),
        (ComparedName, null, ComparedName, value => Identifiers(value, MostCompared)),
        ("open", ExplorationView.Connections, "open", value => Identifiers(value, MostOpen)),
        ("up", ExplorationView.Family, "up", value => Number(value, 0, FurthestReach)),
        ("down", ExplorationView.Family, "down", value => Number(value, 0, FurthestReach)),
        ("beside", ExplorationView.Family, "beside", value => Number(value, 0, FurthestReach)),
        ("family", ExplorationView.Family, "family", value => OneOf(value, "whole")),
        ("tree", ExplorationView.Family, "view", value => OneOf(value, "across")),
        ("dir", ExplorationView.Timeline, "view", value => OneOf(value, "across", "down")),
        ("scale", ExplorationView.Timeline, "scale", value => Number(value, 0, MostScales - 1)),
        ("from", ExplorationView.Timeline, "from", Year),
        ("to", ExplorationView.Timeline, "to", Year),
        ("events", ExplorationView.Timeline, "events", value => OneOf(value, "off")),
        ("lanes", ExplorationView.Timeline, "lanes", value => Number(value, 1, MostLanes)),
        ("order", ExplorationView.Pictures, "order", value => OneOf(value, "oldest", "newest")),
        ("kind", ExplorationView.Pictures, "kind", value => OneOf(value, "image", "video")),
    ];

    // What belongs to the thing that is explored and to nothing else: how far its drawings are opened. A new
    // centre begins with drawings as they open; the manner of looking and what is left out go with the visitor.
    private static readonly ImmutableHashSet<string> OfTheSubject = ["open", "up", "down", "beside", "family", "lanes"];

    private readonly ImmutableDictionary<string, string> _said;

    private Exploration(EntityId subject, ExplorationView view, ReadingLevel boundary, ImmutableDictionary<string, string> said) =>
        (Subject, View, Boundary, _said) = (subject, view, boundary, said);

    /// <summary>Gets what is explored.</summary>
    public EntityId Subject { get; }

    /// <summary>Gets the view that is shown.</summary>
    public ExplorationView View { get; }

    /// <summary>Gets how far the reader has read: what every view asks its projection for. Never in an address.</summary>
    public ReadingLevel Boundary { get; }

    /// <summary>
    /// Gets what is chosen in the view – looked at closer without leaving –, or nothing. It names something; whether
    /// the visitor may see it is the projection's answer.
    /// </summary>
    public EntityId? Chosen => _said.TryGetValue(ChosenName, out var chosen) ? new EntityId(chosen) : null;

    /// <summary>Gets the rows of the drawing of connections that are open.</summary>
    public ImmutableArray<EntityId> Open =>
        _said.TryGetValue("open", out var open) ? [.. open.Split(',').Select(id => new EntityId(id))] : [];

    /// <summary>
    /// Gets what is compared with what is explored (step 20.8): names whose times stand beside its times in the
    /// chart of time. They name things; whether the visitor may see them is the projection's answer.
    /// </summary>
    public ImmutableArray<EntityId> Compared =>
        _said.TryGetValue(ComparedName, out var compared) ? [.. compared.Split(',').Select(id => new EntityId(id))] : [];

    /// <summary>Gets the stretch of years a chart of time was told to show, or nothing.</summary>
    public (int From, int To)? Years =>
        _said.TryGetValue("from", out var from) && _said.TryGetValue("to", out var to)
            ? (int.Parse(from, CultureInfo.InvariantCulture), int.Parse(to, CultureInfo.InvariantCulture))
            : null;

    /// <summary>Gets a value indicating whether a family tree grows across the page instead of down it.</summary>
    public bool TreeAcross => _said.ContainsKey("tree");

    /// <summary>
    /// Returns what the state says under a name it is carried by (<c>open</c>, <c>tree</c>, <c>from</c> …), or
    /// nothing.
    /// </summary>
    public string? this[string name] => _said.GetValueOrDefault(name);

    /// <summary>
    /// Reads the state of a page from its address.
    /// </summary>
    /// <param name="subject">What the page is about.</param>
    /// <param name="view">The view the page is.</param>
    /// <param name="boundary">How far the visitor has read.</param>
    /// <param name="query">The query of the address, as anybody wrote it. A name that stands twice counts once, the first time.</param>
    public static Exploration Read(EntityId subject, ExplorationView view, ReadingLevel boundary, IEnumerable<KeyValuePair<string, string?>> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var asked = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (name, value) in query)
        {
            if (value is not null)
            {
                asked.TryAdd(name, value);
            }
        }

        var said = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
        foreach (var part in Parts)
        {
            // The page of a view reads what is its own by the name it always had.
            if (asked.TryGetValue(part.Of == view ? part.Native : part.Name, out var value) && part.Read(value) is { } read)
            {
                said[part.Name] = read;
            }
        }

        // A stretch is both of its ends, in order; one end, or the ends the wrong way round, is no stretch.
        if (!(said.TryGetValue("from", out var from) && said.TryGetValue("to", out var to)
            && int.Parse(to, CultureInfo.InvariantCulture) > int.Parse(from, CultureInfo.InvariantCulture)))
        {
            said.Remove("from");
            said.Remove("to");
        }

        // What is explored is not chosen in itself.
        if (said.TryGetValue(ChosenName, out var chosen) && chosen == subject.Value)
        {
            said.Remove(ChosenName);
        }

        // And it is not compared with itself.
        if (said.TryGetValue(ComparedName, out var compared))
        {
            var others = string.Join(',', compared.Split(',').Where(id => id != subject.Value));
            if (others.Length == 0)
            {
                said.Remove(ComparedName);
            }
            else
            {
                said[ComparedName] = others;
            }
        }

        return new(subject, view, boundary, said.ToImmutable());
    }

    /// <summary>
    /// Returns the address of a view of what is explored, below the address of its collection, with everything
    /// the state says: the view finds itself as it was left, and carries what the other views were told.
    /// </summary>
    public string Address(ExplorationView view)
    {
        var page = view switch
        {
            ExplorationView.Connections => "/connections",
            ExplorationView.Family => "/family",
            ExplorationView.Timeline => "/timeline",
            ExplorationView.Pictures => "/gallery",
            _ => string.Empty,
        };
        return $"entity/{Subject}{page}{Query(view, all: true)}";
    }

    /// <summary>
    /// Returns an address of the view that is shown with what the other views were told added to it. The page
    /// of a view writes its own state as it always did and passes every link it makes through here, so that
    /// nothing of the other views is lost by a step inside this one.
    /// </summary>
    /// <param name="address">An address the page made, with or without a query and a place on the page.</param>
    public string Carry(string address)
    {
        ArgumentNullException.ThrowIfNull(address);
        var carried = Query(View, all: false);
        if (carried.Length == 0)
        {
            return address;
        }

        var place = address.IndexOf('#', StringComparison.Ordinal);
        var (before, after) = place < 0 ? (address, string.Empty) : (address[..place], address[place..]);
        return before + (before.Contains('?', StringComparison.Ordinal) ? "&" + carried[1..] : carried) + after;
    }

    /// <summary>
    /// Gets what the visitor left out in another view and this view does not leave out: it shows more than they
    /// asked for, and says so instead of dropping the wish in silence. Coming back to the other view finds it.
    /// </summary>
    public ImmutableArray<ExplorationFilter> SetAside
    {
        get
        {
            var aside = ImmutableArray.CreateBuilder<ExplorationFilter>();
            if (View != ExplorationView.Timeline)
            {
                if (Years is not null)
                {
                    aside.Add(ExplorationFilter.Years);
                }

                if (_said.ContainsKey("events"))
                {
                    aside.Add(ExplorationFilter.WithoutEvents);
                }

                if (_said.ContainsKey("lanes"))
                {
                    aside.Add(ExplorationFilter.FewerLanes);
                }
            }

            if (View != ExplorationView.Pictures && _said.ContainsKey("kind"))
            {
                aside.Add(ExplorationFilter.KindOfPicture);
            }

            return aside.ToImmutable();
        }
    }

    /// <summary>Returns the state with another view shown. Everything else stays: nothing is forgotten by looking elsewhere.</summary>
    public Exploration Show(ExplorationView view) => new(Subject, view, Boundary, _said);

    /// <summary>
    /// Returns the state with something chosen – or, with nothing, with nothing chosen. Choosing changes nothing
    /// else: what is explored stays in the middle.
    /// </summary>
    public Exploration Choose(EntityId? chosen) =>
        new(Subject, View, Boundary, chosen is { } one && one != Subject && Identifier(one.Value) is { } id ? _said.SetItem(ChosenName, id) : _said.Remove(ChosenName));

    /// <summary>
    /// Returns the state with something added to the comparison. What is explored is not compared with itself,
    /// nothing stands in it twice, and a name beyond <see cref="MostCompared"/> is not added: the state is then
    /// the same as before.
    /// </summary>
    public Exploration Compare(EntityId other)
    {
        var compared = Compared;
        return other == Subject || compared.Contains(other) || compared.Length >= MostCompared || Identifier(other.Value) is null
            ? this
            : new(Subject, View, Boundary, _said.SetItem(ComparedName, string.Join(',', compared.Append(other).Select(id => id.Value))));
    }

    /// <summary>Returns the state with something taken out of the comparison.</summary>
    public Exploration Uncompare(EntityId other)
    {
        var left = string.Join(',', Compared.Where(id => id != other).Select(id => id.Value));
        return new(Subject, View, Boundary, left.Length == 0 ? _said.Remove(ComparedName) : _said.SetItem(ComparedName, left));
    }

    /// <summary>
    /// Returns the state with exactly these rows of the drawing of connections open. The page of the drawing
    /// says so once the projection has answered: what an address asked to open and the visitor may not see, or
    /// what is not connected, is not open and is not carried on.
    /// </summary>
    public Exploration Opened(IEnumerable<EntityId> open)
    {
        ArgumentNullException.ThrowIfNull(open);
        var rows = Identifiers(string.Join(',', open.Select(row => row.Value)), MostOpen);
        return new(Subject, View, Boundary, rows is null ? _said.Remove("open") : _said.SetItem("open", rows));
    }

    /// <summary>
    /// Returns the state with something else in the middle: an act of its own, not what choosing does. How far
    /// the drawings of the earlier centre were opened is not said of the new one, and nothing is chosen in it;
    /// the manner of looking and what was left out stay with the visitor.
    /// </summary>
    public Exploration Centre(EntityId centre) =>
        new Exploration(centre, View, Boundary, _said.RemoveRange(OfTheSubject).Remove(ChosenName)).Uncompare(centre);

    // The query of an address of a view: what is the view's own under the names of its page, and – for the
    // whole state – what the other views were told under the names it is carried by.
    private string Query(ExplorationView view, bool all)
    {
        var query = new StringBuilder();
        foreach (var part in Parts)
        {
            var own = part.Of == view;
            if ((all || !own) && _said.TryGetValue(part.Name, out var value))
            {
                query.Append(query.Length == 0 ? '?' : '&').Append(own ? part.Native : part.Name).Append('=').Append(Uri.EscapeDataString(value).Replace("%2C", ",", StringComparison.Ordinal));
            }
        }

        return query.ToString();
    }

    private static string? Identifier(string value) =>
        value.Length is > 0 and <= 120 && value.All(letter => char.IsAsciiLetterOrDigit(letter) || letter is '-' or '_' or '.') ? value : null;

    private static string? Identifiers(string value, int most)
    {
        var ids = value.Split(',', StringSplitOptions.TrimEntries).Select(Identifier).Where(id => id is not null).Distinct().Take(most).ToList();
        return ids.Count == 0 ? null : string.Join(',', ids);
    }

    private static string? Number(string value, int least, int most) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number >= least && number <= most
            ? number.ToString(CultureInfo.InvariantCulture)
            : null;

    // A year of an address: positive in the common era, negative before it; there is no year zero.
    private static string? Year(string value) =>
        int.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var year) && year != 0 && Math.Abs(year) <= AskedTime.FurthestYear && value[0] != '+'
            ? year.ToString(CultureInfo.InvariantCulture)
            : null;

    private static string? OneOf(string value, params string[] choices) => choices.Contains(value, StringComparer.Ordinal) ? value : null;
}
