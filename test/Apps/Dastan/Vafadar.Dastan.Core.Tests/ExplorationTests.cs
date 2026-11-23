namespace Vafadar.Dastan.Core.Tests;

/// <summary>
/// The state of exploring (step 20.6, ADR 0025): read from an address and written to one, kept through a change
/// of the view, and never more than names of public things and numbers.
/// </summary>
public sealed class ExplorationTests
{
    private static readonly EntityId Rostam = new("character-rostam");
    private static readonly EntityId Zal = new("character-zal");

    private static Exploration Read(ExplorationView view, string query, EntityId? subject = null) =>
        Exploration.Read(
            subject ?? Rostam,
            view,
            new ReadingLevel(2),
            query.Split('&', StringSplitOptions.RemoveEmptyEntries).Select(pair => pair.Split('=', 2)).Select(pair => new KeyValuePair<string, string?>(pair[0], pair.Length > 1 ? Uri.UnescapeDataString(pair[1]) : null)));

    [Fact]
    public void An_address_without_a_query_is_the_view_as_it_opens()
    {
        var state = Read(ExplorationView.Connections, string.Empty);

        Assert.Equal((Rostam, ExplorationView.Connections, new ReadingLevel(2)), (state.Subject, state.View, state.Boundary));
        Assert.Null(state.Chosen);
        Assert.Empty(state.Open);
        Assert.Null(state.Years);
        Assert.Empty(state.SetAside);
        Assert.Equal("entity/character-rostam/connections", state.Address(ExplorationView.Connections));
        Assert.Equal("entity/character-rostam", state.Address(ExplorationView.Overview));
        Assert.Equal("a/b?x=1#chart", state.Carry("a/b?x=1#chart"));
    }

    [Fact]
    public void Each_view_finds_itself_as_it_was_left_after_a_look_at_another()
    {
        // Rows are opened in the drawing of connections, and something is chosen.
        var connections = Read(ExplorationView.Connections, "open=character-zal,place-samangan&sel=character-zal");
        Assert.Equal([Zal, new EntityId("place-samangan")], connections.Open);
        Assert.Equal(Zal, connections.Chosen);

        // The chart of time is asked with all of it, and told a stretch of years of its own.
        var toTimeline = connections.Address(ExplorationView.Timeline);
        Assert.Equal("entity/character-rostam/timeline?sel=character-zal&open=character-zal,place-samangan", toTimeline);
        var timeline = Read(ExplorationView.Timeline, toTimeline[(toTimeline.IndexOf('?', StringComparison.Ordinal) + 1)..] + "&from=900&to=1000&view=down");
        Assert.Equal((900, 1000), timeline.Years);
        Assert.Equal([Zal, new EntityId("place-samangan")], timeline.Open);

        // Back in the drawing of connections everything is there again – and the stretch is carried on.
        var back = timeline.Address(ExplorationView.Connections);
        Assert.Equal("entity/character-rostam/connections?sel=character-zal&open=character-zal,place-samangan&dir=down&from=900&to=1000", back);
        var again = Read(ExplorationView.Connections, back[(back.IndexOf('?', StringComparison.Ordinal) + 1)..]);
        Assert.Equal(connections.Open, again.Open);
        Assert.Equal(Zal, again.Chosen);
        Assert.Equal((900, 1000), again.Years);
        Assert.Equal("entity/character-rostam/timeline?sel=character-zal&open=character-zal,place-samangan&view=down&from=900&to=1000", again.Address(ExplorationView.Timeline));
    }

    [Fact]
    public void Two_views_call_their_manner_view_and_neither_takes_the_others()
    {
        // On the page of the family "view" is the direction of the tree; carried, it is "tree".
        var family = Read(ExplorationView.Family, "view=across&up=3");
        Assert.True(family.TreeAcross);
        Assert.Equal("entity/character-rostam/timeline?up=3&tree=across", family.Address(ExplorationView.Timeline));
        Assert.Equal("entity/character-rostam/family?up=3&view=across", family.Address(ExplorationView.Family));

        // On the page of the chart of time "view" is the direction of time, and "tree" what was carried.
        var timeline = Read(ExplorationView.Timeline, "up=3&tree=across&view=down");
        Assert.True(timeline.TreeAcross);
        Assert.Equal("down", timeline["dir"]);
        Assert.Equal("entity/character-rostam/family?up=3&view=across&dir=down", timeline.Address(ExplorationView.Family));

        // A page that is neither reads no "view" at all.
        var pictures = Read(ExplorationView.Pictures, "view=across");
        Assert.False(pictures.TreeAcross);
        Assert.Null(pictures["dir"]);
    }

    [Fact]
    public void A_step_inside_a_view_loses_nothing_of_the_others()
    {
        var family = Read(ExplorationView.Family, "up=3&sel=character-zal&from=900&to=1000&kind=image&open=character-zal");

        // The page writes its own state as it always did; what the other views were told is added.
        Assert.Equal("story/entity/character-rostam/family?up=4&sel=character-zal&open=character-zal&from=900&to=1000&kind=image#chart", family.Carry("story/entity/character-rostam/family?up=4#chart"));
        Assert.Equal("story/entity/character-rostam/family?sel=character-zal&open=character-zal&from=900&to=1000&kind=image", family.Carry("story/entity/character-rostam/family"));
    }

    [Fact]
    public void What_was_left_out_elsewhere_and_is_shown_here_is_named()
    {
        // The years are honoured by every view of a name (D-084, step 25.3): they are never set aside.
        Assert.Equal([ExplorationFilter.WithoutEvents, ExplorationFilter.FewerLanes, ExplorationFilter.KindOfPicture], Read(ExplorationView.Connections, "from=900&to=1000&events=off&lanes=2&kind=video").SetAside);
        Assert.Equal([ExplorationFilter.KindOfPicture], Read(ExplorationView.Timeline, "from=900&to=1000&events=off&lanes=2&kind=video").SetAside);
        Assert.Empty(Read(ExplorationView.Pictures, "from=900&to=1000&kind=video").SetAside);
        Assert.Equal([ExplorationFilter.KindOfPicture], Read(ExplorationView.Family, "from=900&to=1000&kind=video").SetAside);
        Assert.Empty(Read(ExplorationView.Overview, "from=900&to=1000").SetAside);

        // How far a drawing is opened and in which order pictures stand leave nothing out.
        Assert.Empty(Read(ExplorationView.Overview, "open=character-zal&up=3&order=oldest&tree=across&scale=4").SetAside);
    }

    [Fact]
    public void Choosing_keeps_the_centre_and_a_new_centre_is_an_act_of_its_own()
    {
        var state = Read(ExplorationView.Connections, "open=character-zal&from=900&to=1000&up=3&lanes=2&tree=across&order=oldest");

        var chosen = state.Choose(Zal);
        Assert.Equal((Rostam, Zal), (chosen.Subject, chosen.Chosen));
        Assert.Equal(state.Open, chosen.Open);
        Assert.Null(chosen.Choose(null).Chosen);
        Assert.Null(chosen.Choose(Rostam).Chosen);

        // With another centre the drawings begin as they open; how the visitor looks and what they left out stay.
        var centred = chosen.Centre(Zal);
        Assert.Equal(Zal, centred.Subject);
        Assert.Null(centred.Chosen);
        Assert.Empty(centred.Open);
        Assert.Equal("entity/character-zal/connections?tree=across&from=900&to=1000&order=oldest", centred.Address(ExplorationView.Connections));
        Assert.Equal(ExplorationView.Timeline, centred.Show(ExplorationView.Timeline).View);
    }

    [Theory]
    [InlineData("sel=character rostam")]
    [InlineData("sel=../secret")]
    [InlineData("sel=<script>")]
    [InlineData("sel=character-rostam")]
    [InlineData("open=,,")]
    [InlineData("up=99")]
    [InlineData("up=-1")]
    [InlineData("family=some")]
    [InlineData("tree=down")]
    [InlineData("dir=sideways")]
    [InlineData("scale=7")]
    [InlineData("from=900")]
    [InlineData("from=1000&to=900")]
    [InlineData("from=0&to=10")]
    [InlineData("from=+5&to=10")]
    [InlineData("from=1&to=99999999")]
    [InlineData("events=on")]
    [InlineData("lanes=0")]
    [InlineData("order=random")]
    [InlineData("kind=audio")]
    [InlineData("account=mina&token=abc&returnUrl=//elsewhere")]
    public void What_is_no_choice_is_not_carried(string query)
    {
        var state = Read(ExplorationView.Overview, query);

        Assert.Equal("entity/character-rostam", state.Address(ExplorationView.Overview));
        Assert.Equal("entity/character-rostam/connections", state.Address(ExplorationView.Connections));
    }

    [Fact]
    public void A_name_that_stands_twice_counts_once_and_rows_are_counted()
    {
        Assert.Equal(Zal, Read(ExplorationView.Overview, "sel=character-zal&sel=place-samangan").Chosen);

        var many = string.Join(',', Enumerable.Range(0, 60).Select(number => $"row-{number}").Append("row-1").Append("no row"));
        Assert.Equal(Exploration.MostOpen, Read(ExplorationView.Connections, "open=" + many).Open.Length);
    }

    [Fact]
    public void The_comparison_is_carried_on_every_view_and_holds_each_name_once()
    {
        var work = new EntityId("work-shahnameh");
        var state = Read(ExplorationView.Connections, "open=character-zal").Compare(work).Compare(Zal);

        Assert.Equal([work, Zal], state.Compared);
        Assert.Equal("entity/character-rostam/timeline?with=work-shahnameh,character-zal&open=character-zal", state.Address(ExplorationView.Timeline));

        // Twice is once; what is explored is not compared with itself; a name taken out is out.
        Assert.Same(state, state.Compare(work));
        Assert.Same(state, state.Compare(Rostam));
        Assert.Equal([Zal], state.Uncompare(work).Compared);
        Assert.Empty(state.Uncompare(work).Uncompare(Zal).Compared);
        Assert.Equal("entity/character-rostam", state.Uncompare(work).Uncompare(Zal).Choose(null).Opened([]).Address(ExplorationView.Overview));

        // Read from an address: the name in the middle is not in it, and it has room for so many.
        Assert.Equal([Zal], Read(ExplorationView.Timeline, "with=character-rostam,character-zal,character-zal").Compared);
        var many = string.Join(',', Enumerable.Range(0, 9).Select(number => $"name-{number}"));
        var full = Read(ExplorationView.Timeline, "with=" + many);
        Assert.Equal(Exploration.MostCompared, full.Compared.Length);
        Assert.Same(full, full.Compare(Zal));

        // Another name in the middle keeps the comparison – without itself.
        Assert.Equal([work], state.Centre(Zal).Compared);
        Assert.Equal("entity/character-zal/connections?with=work-shahnameh", state.Centre(Zal).Address(ExplorationView.Connections));
    }

    [Fact]
    public void Only_what_the_projection_says_is_open_is_carried_as_open()
    {
        var asked = Read(ExplorationView.Connections, "open=character-zal,character-sohrab,nobody&sel=character-zal");

        var answered = asked.Opened([Zal]);
        Assert.Equal([Zal], answered.Open);
        Assert.Equal("entity/character-rostam/timeline?sel=character-zal&open=character-zal", answered.Address(ExplorationView.Timeline));
        Assert.Equal("entity/character-rostam/timeline?sel=character-zal", asked.Opened([]).Address(ExplorationView.Timeline));
    }

    [Fact]
    public void How_far_the_reader_has_read_is_part_of_the_state_and_of_no_address()
    {
        var state = Read(ExplorationView.Connections, "stage=4&boundary=4&level=4&sel=character-zal");

        Assert.Equal(new ReadingLevel(2), state.Boundary);
        Assert.Equal(new ReadingLevel(2), state.Centre(Zal).Show(ExplorationView.Family).Boundary);
        Assert.DoesNotContain("2", state.Address(ExplorationView.Family), StringComparison.Ordinal);
    }

    [Fact]
    public void The_years_are_one_stretch_for_every_view_of_a_name()
    {
        var state = Read(ExplorationView.Connections, "from=-500&to=1000&open=character-zal&sel=character-zal&order=oldest");

        Assert.Equal((-500, 1000), state.Years);
        Assert.True(Exploration.HonoursYears(ExplorationView.Connections) && Exploration.HonoursYears(ExplorationView.Timeline) && Exploration.HonoursYears(ExplorationView.Pictures));
        Assert.True(Exploration.HonoursYears(ExplorationView.Overview) && Exploration.HonoursYears(ExplorationView.Family));

        // Both years are whole years of the axis: from the start of 500 BCE to the end of 1000 CE.
        Assert.Equal(new Vafadar.Temporal.YearSpan(new YearOfEra(Vafadar.Temporal.Era.BeforeCommonEra, 500).Place, new YearOfEra(Vafadar.Temporal.Era.CommonEra, 1000).Place + 1), state.Span);
        Assert.Null(Read(ExplorationView.Connections, "open=character-zal").Span);

        // Every view is led to with the years; they are named the same in each.
        Assert.Contains("from=-500&to=1000", state.Address(ExplorationView.Pictures), StringComparison.Ordinal);
        Assert.Contains("from=-500&to=1000", state.Address(ExplorationView.Timeline), StringComparison.Ordinal);

        // A form that asks for years sends the rest of the state along, under the names of the view that is shown.
        Assert.Equal(
            [KeyValuePair.Create("sel", "character-zal"), KeyValuePair.Create("open", "character-zal"), KeyValuePair.Create("order", "oldest")],
            state.SaidBesideYears);

        // Other years, and every year: nothing else changes.
        Assert.Equal((900, 950), state.WithYears((900, 950)).Years);
        Assert.Equal("entity/character-rostam/connections?sel=character-zal&open=character-zal&order=oldest", state.WithYears(null).Address(ExplorationView.Connections));
        Assert.Null(state.WithYears((950, 900)).Years);
    }

    [Theory]
    [InlineData("from=۹۹۰&to=۱۰۰۰", 990, 1000)]
    [InlineData("from=−500&to=-400", -500, -400)]
    [InlineData("from=%20990%20&to=1000", 990, 1000)]
    public void Years_are_read_as_somebody_typed_them(string query, int from, int to)
    {
        Assert.Equal((from, to), Read(ExplorationView.Pictures, query).Years);
    }

    [Theory]
    [InlineData("from=1000&to=990")]
    [InlineData("from=990")]
    [InlineData("from=0&to=10")]
    [InlineData("from=+990&to=1000")]
    [InlineData("from=nine&to=ten")]
    public void What_is_no_stretch_of_years_is_none(string query)
    {
        Assert.Null(Read(ExplorationView.Connections, query).Years);
    }
}
