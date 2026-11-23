using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The site of the dated records with a poet who was at a court from 994 to 999 – and once more, without a
/// time – and at a school from 1020 to 1030, and with the illustration of the site as a picture of the year
/// 1020: test data, written onto the sample.
/// </summary>
public class YearsSite : DatedSite
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var path = Path.Combine(Content, "samples", "shahnameh-pilot.json");
        var first = !File.Exists(path);
        base.ConfigureWebHost(builder);
        if (!first)
        {
            return;
        }

        var sample = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        JsonObject Entity(string id, string label) => new()
        {
            ["id"] = id, ["type"] = id.StartsWith("person", StringComparison.Ordinal) ? "Person" : "Organisation", ["label"] = label, ["summary"] = "Test data.",
            ["sourceIds"] = new JsonArray("src-ganjoor-index"), ["spoilerLevel"] = 0,
        };
        JsonObject Relation(string id, string to, int? from, int? until)
        {
            var relation = new JsonObject
            {
                ["id"] = id, ["from"] = "person-poet", ["to"] = to, ["type"] = "memberOf", ["label"] = "Member of",
                ["sourceIds"] = new JsonArray("src-ganjoor-index"), ["spoilerLevel"] = 0,
                ["reviewStatus"] = "source_backed_editorial_review_pending",
                ["scope"] = new JsonObject { ["kind"] = "outside_the_story", ["storyId"] = "story-rostam-sohrab", ["eventIds"] = new JsonArray() },
                ["directionality"] = "directed",
            };
            if (from is not null)
            {
                relation["time"] = JsonNode.Parse($$"""
                    {
                      "kind": "period",
                      "start": { "state": "known", "at": { "frame": "calendar", "era": "commonEra", "year": {{from}} } },
                      "end": { "state": "known", "at": { "frame": "calendar", "era": "commonEra", "year": {{until}} } },
                      "continuity": "notAsserted"
                    }
                    """);
            }

            return relation;
        }

        var entities = sample["entities"]!.AsArray();

        // Two people who were partners from 1000 to 1010, with nobody else: a family of one line.
        entities.Add(Entity("person-first-partner", "The first partner"));
        entities.Add(Entity("person-second-partner", "The second partner"));
        entities.Add(Entity("person-poet", "The poet"));
        entities.Add(Entity("organisation-court", "The court"));
        entities.Add(Entity("organisation-school", "The school"));
        var relations = sample["relations"]!.AsArray();
        var partners = Relation("relation-partners", "person-second-partner", 1000, 1010);
        (partners["from"], partners["type"], partners["label"], partners["directionality"]) = ("person-first-partner", "spouseOf", "Partner of", "symmetric");
        relations.Add(partners);
        relations.Add(Relation("relation-court-dated", "organisation-court", 994, 999));
        relations.Add(Relation("relation-court-undated", "organisation-court", null, null));
        relations.Add(Relation("relation-school", "organisation-school", 1020, 1030));
        sample["media"]!.AsArray().Select(item => item!.AsObject()).Single(item => (string?)item["id"] == "media-dastan-family-illustration")["depictedTime"] =
            JsonNode.Parse("""{ "kind": "occurrence", "at": { "frame": "calendar", "era": "commonEra", "year": 1020 } }""");
        File.WriteAllText(path, sample.ToJsonString());
    }
}

/// <summary>
/// The years a visitor chose on the pages of a name (D-084): its connections and its pictures keep to them as
/// its chart of time does, what states no time stands apart and is not hidden, what was left out is counted and
/// said, and the years are asked for and given up again on the bar of exploring.
/// </summary>
public sealed class YearsOfANamePagesTests(YearsSite site) : IClassFixture<YearsSite>
{
    private const string Story = "story-rostam-sohrab";
    private const string Poet = $"/en/{Story}/entity/person-poet";
    private const string Rostam = $"/en/{Story}/entity/character-rostam";
    private const string Illustration = "media-dastan-family-illustration";
    private const string Video = "media-dastan-parentage-video";

    private static string Html(Page page) => WebUtility.HtmlDecode(page.Html);

    private async Task<Browser> Reader()
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(4);
        return visitor;
    }

    [Fact]
    public async Task Without_years_the_connections_are_one_list_and_the_bar_offers_to_ask_for_years()
    {
        var page = await (await Reader()).Get($"{Poet}/connections");
        var html = Html(page);

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("<summary>Years: all</summary>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<details class=\"fold explore-years\" id=\"years\" open", html, StringComparison.Ordinal);
        Assert.Contains("name=\"from\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Without a stated time", html, StringComparison.Ordinal);
        Assert.DoesNotContain("All years", html, StringComparison.Ordinal);
        Assert.Contains("The school", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_connections_of_a_name_keep_to_the_years_and_tell_apart_what_states_no_time()
    {
        var visitor = await Reader();
        var page = await visitor.Get($"{Poet}/connections?from=990&to=1000");
        var html = Html(page);

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("Shown for the years 990 CE – 1000 CE: what held in them, and what states no time.", html, StringComparison.Ordinal);
        Assert.Contains("Left out because they state another time: 1.", html, StringComparison.Ordinal);
        Assert.Contains("<h3 id=\"in-years\">Held in the years 990 CE – 1000 CE</h3>", html, StringComparison.Ordinal);
        Assert.Contains("<h3 id=\"without-time\">Without a stated time</h3>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("The school", html, StringComparison.Ordinal);

        // The bar: the years it was told, to change and to give up; and nothing is said to be set aside.
        Assert.Contains("<details class=\"fold explore-years\" id=\"years\" open", html, StringComparison.Ordinal);
        Assert.Contains("<summary>Years: 990 CE – 1000 CE</summary>", html, StringComparison.Ordinal);
        Assert.Matches("name=\"from\" value=\"990\"", html);
        Assert.Matches("name=\"to\" value=\"1000\"", html);
        Assert.Matches("<a class=\"action secondary\" href=\"story-rostam-sohrab/entity/person-poet/connections\" data-explore>All years</a>", html);
        Assert.DoesNotContain("Not left out in this view", html, StringComparison.Ordinal);

        // The other views are led to with the years.
        Assert.Contains("entity/person-poet/timeline?from=990&to=1000", html, StringComparison.Ordinal);

        // The drawing that moves is asked for the same years and gets the same neighbourhood.
        var drawing = await visitor.Get($"{Poet}/connections.json?from=990&to=1000");
        Assert.Contains("organisation-court", drawing.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("organisation-school", drawing.Html, StringComparison.Ordinal);
        Assert.Contains("organisation-school", (await visitor.Get($"{Poet}/connections.json")).Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Years_nothing_held_in_say_so_and_show_what_states_no_time()
    {
        var page = await (await Reader()).Get($"{Poet}/connections?from=1500&to=1600");
        var html = Html(page);

        Assert.Contains("Nothing here is stated to have held in the years 1500 CE – 1600 CE; what states no time is shown.", html, StringComparison.Ordinal);
        Assert.Contains("Left out because they state another time: 2.", html, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"in-years\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"without-time\"", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("from=1000&to=990", "1000", "990")]
    [InlineData("from=nine&to=ten", "nine", "ten")]
    public async Task What_is_no_stretch_of_years_is_said_and_leaves_nothing_out(string query, string from, string to)
    {
        var page = await (await Reader()).Get($"{Poet}/connections?{query}");
        var html = Html(page);

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("That is no stretch of years", html, StringComparison.Ordinal);
        Assert.Contains("<details class=\"fold explore-years\" id=\"years\" open", html, StringComparison.Ordinal);
        Assert.Contains($"name=\"from\" value=\"{from}\"", html, StringComparison.Ordinal);
        Assert.Contains($"name=\"to\" value=\"{to}\"", html, StringComparison.Ordinal);
        Assert.Contains("The school", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Without a stated time", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Years_typed_in_Persian_are_read_and_the_page_says_them_in_Persian()
    {
        var page = await (await Reader()).Get($"/fa/{Story}/entity/person-poet/connections?from={Uri.EscapeDataString("۹۹۰")}&to={Uri.EscapeDataString("۱۰۰۰")}");
        var html = Html(page);

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("سال‌ها: ", html, StringComparison.Ordinal);
        Assert.Contains("بدون زمانِ گفته‌شده", html, StringComparison.Ordinal);
        Assert.Contains("کنار گذاشته‌شده چون زمان دیگری دارند: ۱.", html, StringComparison.Ordinal);
        Assert.Matches("name=\"from\" value=\"990\"", html);
    }

    [Fact]
    public async Task The_form_of_the_years_sends_the_rest_of_what_is_explored_along()
    {
        var page = await (await Reader()).Get($"{Rostam}/connections?open=character-zal&sel=character-zal&kind=image");
        var html = Html(page);
        var form = html[html.IndexOf("<form class=\"search explore-years-form\"", StringComparison.Ordinal)..];
        form = form[..form.IndexOf("</form>", StringComparison.Ordinal)];

        Assert.Contains("action=\"story-rostam-sohrab/entity/character-rostam/connections\"", form, StringComparison.Ordinal);
        Assert.Contains("<input type=\"hidden\" name=\"sel\" value=\"character-zal\"", form, StringComparison.Ordinal);
        Assert.Contains("<input type=\"hidden\" name=\"open\" value=\"character-zal\"", form, StringComparison.Ordinal);
        Assert.Contains("<input type=\"hidden\" name=\"kind\" value=\"image\"", form, StringComparison.Ordinal);
        Assert.DoesNotContain("type=\"hidden\" name=\"from\"", form, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_pictures_of_a_name_keep_to_the_years_with_what_states_no_time_apart()
    {
        var visitor = await Reader();
        var page = await visitor.Get($"{Rostam}/gallery?from=1000&to=1050");
        var html = Html(page);

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("Shown for the years 1000 CE – 1050 CE: what shows them, and below what states no time.", html, StringComparison.Ordinal);
        Assert.Contains("<h2 id=\"without-time\">Without a stated time</h2>", html, StringComparison.Ordinal);
        Assert.True(html.IndexOf($"id=\"{Illustration}\"", StringComparison.Ordinal) < html.IndexOf("id=\"without-time\"", StringComparison.Ordinal));
        Assert.True(html.IndexOf($"id=\"{Video}\"", StringComparison.Ordinal) > html.IndexOf("id=\"without-time\"", StringComparison.Ordinal));
        Assert.DoesNotContain("Left out because they show another time", html, StringComparison.Ordinal);
        Assert.Contains("<summary>Years: 1000 CE – 1050 CE</summary>", html, StringComparison.Ordinal);

        // A picture is opened with the years, leads back with them, and the next one is the one the gallery lists next.
        Assert.Contains($"media/{Illustration}?entity=character-rostam&from=1000&to=1050", html, StringComparison.Ordinal);
        var item = Html(await visitor.Get($"/en/{Story}/media/{Illustration}?entity=character-rostam&from=1000&to=1050"));
        Assert.Contains($"entity/character-rostam/gallery?from=1000&to=1050#{Illustration}", item, StringComparison.Ordinal);
        Assert.Contains($"rel=\"next\" href=\"{Story}/media/{Video}?entity=character-rostam&from=1000&to=1050\"", item, StringComparison.Ordinal);

        // Other years: the picture of 1020 is left out and counted, what states no time stays.
        var other = Html(await visitor.Get($"{Rostam}/gallery?from=1100&to=1200"));
        Assert.Contains("Nothing here is stated to show the years 1100 CE – 1200 CE; what states no time is shown.", other, StringComparison.Ordinal);
        Assert.Contains("Left out because they show another time: 1.", other, StringComparison.Ordinal);
        Assert.DoesNotContain($"id=\"{Illustration}\"", other, StringComparison.Ordinal);
        Assert.Contains($"id=\"{Video}\"", other, StringComparison.Ordinal);

        // Without years the gallery is one list.
        var whole = Html(await visitor.Get($"{Rostam}/gallery"));
        Assert.DoesNotContain("id=\"without-time\"", whole, StringComparison.Ordinal);
        Assert.Contains($"id=\"{Illustration}\"", whole, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("en", "Shown for the years 990 CE – 1000 CE", "Left out because they state another time: 1.", "Stated for the years", "Held in the years 990 CE – 1000 CE", "Without a stated time", "All years")]
    [InlineData("fa", "برای سال‌های", "کنار گذاشته‌شده چون زمان دیگری دارند: ۱.", "گفته‌شده برای سال‌های", "برقرار در سال‌های", "بدون زمانِ گفته‌شده", "همهٔ سال‌ها")]
    public async Task The_page_of_a_name_keeps_to_the_years(string language, string shown, string leftOut, string stated, string held, string without, string all)
    {
        var visitor = await Reader();
        var poet = Poet.Replace("/en/", $"/{language}/", StringComparison.Ordinal);

        var html = Html(await visitor.Get($"{poet}?from=990&to=1000"));

        // What is said: the years, and how much of another time was left out, with the way to all years.
        Assert.Contains("entity-years", html, StringComparison.Ordinal);
        Assert.Contains(shown, html, StringComparison.Ordinal);
        Assert.Contains(leftOut, html, StringComparison.Ordinal);
        Assert.Contains(all, html, StringComparison.Ordinal);
        Assert.DoesNotContain("Not left out in this view", html, StringComparison.Ordinal);
        Assert.DoesNotContain(stated, html, StringComparison.Ordinal);

        // The connections: the court of those years, the court without a time apart, the school not at all.
        var connections = html[html.IndexOf("id=\"connections\"", StringComparison.Ordinal)..];
        Assert.True(connections.IndexOf(held, StringComparison.Ordinal) < connections.IndexOf(without, StringComparison.Ordinal));
        Assert.Contains("organisation-court", connections[..connections.IndexOf("</section>", StringComparison.Ordinal)], StringComparison.Ordinal);
        Assert.DoesNotContain("organisation-school", connections[..connections.IndexOf("</section>", StringComparison.Ordinal)], StringComparison.Ordinal);

        // The bar asks for the years on this page too, and the other views are reached with them.
        Assert.Contains("id=\"explore-from\"", html, StringComparison.Ordinal);
        Assert.Contains("connections?from=990&to=1000", html, StringComparison.Ordinal);

        // Without years the page is what it was.
        var whole = Html(await visitor.Get(poet));
        Assert.DoesNotContain("entity-years", whole, StringComparison.Ordinal);
        Assert.Contains("organisation-school", whole, StringComparison.Ordinal);
        Assert.DoesNotContain(without, whole, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Years_in_which_nothing_is_stated_leave_what_states_no_time()
    {
        var visitor = await Reader();

        var html = Html(await visitor.Get($"{Poet}?from=1500&to=1600"));

        Assert.Contains("Nothing here is stated for the years 1500 CE – 1600 CE", html, StringComparison.Ordinal);
        Assert.Contains("Left out because they state another time: 2.", html, StringComparison.Ordinal);
        Assert.Contains("organisation-court", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_family_tree_keeps_to_the_years_and_draws_descent_at_every_time()
    {
        var visitor = await Reader();

        var html = Html(await visitor.Get($"{Rostam}/family?from=1000&to=1050"));

        Assert.Contains("family-years", html, StringComparison.Ordinal);
        Assert.Contains("Shown for the years 1000 CE – 1050 CE. Descent states no time and is drawn at every time", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Not left out in this view", html, StringComparison.Ordinal);
        Assert.Contains("character-zal", html, StringComparison.Ordinal);
        Assert.Contains("id=\"explore-from\"", html, StringComparison.Ordinal);

        // The drawing that moves is asked with the same years.
        var graph = await visitor.Get($"{Rostam}/family.json?from=1000&to=1050");
        Assert.Equal(HttpStatusCode.OK, graph.Status);
        Assert.Contains("character-zal", graph.Html, StringComparison.Ordinal);

        Assert.DoesNotContain("family-years", Html(await visitor.Get($"{Rostam}/family")), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_partnership_of_another_time_is_left_out_of_the_tree_and_of_its_drawing_and_said_so()
    {
        var visitor = await Reader();
        const string Partner = $"/en/{Story}/entity/person-first-partner";

        // In its years the partnership is drawn, on the page and in what the moving drawing is given.
        var held = Html(await visitor.Get($"{Partner}/family?from=1000&to=1010"));
        Assert.Contains("person-second-partner", held, StringComparison.Ordinal);
        Assert.DoesNotContain("Left out because they state another time", held, StringComparison.Ordinal);
        Assert.Contains("relation-partners", (await visitor.Get($"{Partner}/family.json?from=1000&to=1010")).Html, StringComparison.Ordinal);

        // In other years it is left out of both, counted, and the way to all years is offered.
        var other = Html(await visitor.Get($"{Partner}/family?from=1100&to=1200"));
        Assert.Contains("family-years", other, StringComparison.Ordinal);
        Assert.Contains("Left out because they state another time, with what reaches this name only through them: 1.", other, StringComparison.Ordinal);
        Assert.Contains($"{Story}/entity/person-first-partner/family\"", other.Replace("?", "\"", StringComparison.Ordinal), StringComparison.Ordinal);
        var graph = (await visitor.Get($"{Partner}/family.json?from=1100&to=1200")).Html;
        Assert.DoesNotContain("relation-partners", graph, StringComparison.Ordinal);
        Assert.DoesNotContain("person-second-partner", graph, StringComparison.Ordinal);

        // Without years the tree is whole.
        Assert.Contains("relation-partners", (await visitor.Get($"{Partner}/family.json")).Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Years_in_which_nothing_is_stated_about_a_name_say_so_where_its_statements_stand()
    {
        var visitor = await Reader();
        const string Work = $"/en/{Story}/entity/work-shahnameh";

        var whole = Html(await visitor.Get(Work));
        Assert.Contains("id=\"stated-work-written\"", whole, StringComparison.Ordinal);
        Assert.DoesNotContain("Everything stated about this name states another time", whole, StringComparison.Ordinal);

        // The section stays, says why it is empty, and the statements themselves are not shown.
        var later = Html(await visitor.Get($"{Work}?from=1500&to=1600"));
        Assert.Contains("id=\"stated\"", later, StringComparison.Ordinal);
        Assert.Contains("Everything stated about this name states another time than the years you chose.", later, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"stated-work-written\"", later, StringComparison.Ordinal);
    }
}
