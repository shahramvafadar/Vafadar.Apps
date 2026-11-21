using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Vafadar.Dastan.People;
using Vafadar.Dastan.Tests;
using Vafadar.Dastan.Web.Site;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Browser.Tests;

/// <summary>
/// The whole way of a drawing, measured (step 20.12): a family tree as large as a tree may be – 500 people, a
/// family that branches as families do, with partners, several relations between the same people and long
/// names in both languages – kept in a real PostgreSQL, asked of the store, placed, written as the answer of
/// the site and drawn in a browser. Each part is timed by itself: the question, the placing, the whole answer,
/// the first drawing one can use, one step; with them the size of the answer and of the scripts and the memory
/// of the page. The budgets were set before the last measurement, for the machine of development; where the
/// environment says <c>DASTAN_BUDGETS=record</c> (the runner of CI, which measures itself) the times are
/// written down (<c>DASTAN_MEASURES</c>) and not judged, and what does not depend on a machine – the sizes,
/// the bound of what one answer holds – is judged everywhere.
/// </summary>
public sealed partial class WholeWayBrowserTests(AccountsBrowserSite site) : IClassFixture<AccountsBrowserSite>
{
    // The budgets on the machine of development, in milliseconds, bytes and megabytes.
    private const int QuestionBudget = 400;
    private const int PlacingBudget = 400;
    private const int AnswerBudget = 1_500;
    private const int UsableBudget = 5_000;
    private const int StepBudget = 500;
    private const int AnswerSize = 400 * 1024;
    private const int OwnScriptsSize = 300 * 1024;
    private const int HeapBudget = 200;

    [GeneratedRegex("href=\"people/([a-f0-9]{32})\"")]
    private static partial Regex Me();

    // A family that branches: eight couples begin it; every couple has three children; every child finds a
    // partner from outside and is a couple in turn. Every tenth child also has a guardian who later adopts
    // them – two relations between the same two people, beside the parents by birth.
    private static (int People, int Links) Family(PeopleStore store, string owner, string tree, int people)
    {
        var made = 0;
        var links = 0;
        string Person(int generation)
        {
            var number = made++;
            var born = new Occurrence(CalendarPosition.OfYear(CalendarSystem.Unspecified, Era.CommonEra, 1700 + (generation * 27) + (number % 9)));
            return store.AddPerson(owner, tree, $"شخص شمارهٔ {number} از خاندانِ ساختگیِ آزمون (Person {number})", born, null, deceased: generation < 8)
                ?? throw new InvalidOperationException($"Person {number} was not kept.");
        }

        void Link(string kind, string from, string to)
        {
            store.Link(owner, tree, kind, from, to);
            links++;
        }

        var couples = new Queue<(string One, string Other, int Generation)>();
        var elders = new List<string>();
        for (var index = 0; index < 8; index++)
        {
            var (one, other) = (Person(0), Person(0));
            Link("partner", one, other);
            couples.Enqueue((one, other, 0));
            elders.Add(one);
        }

        while (made + 2 <= people && couples.TryDequeue(out var couple))
        {
            for (var child = 0; child < 3 && made + 2 <= people; child++)
            {
                var born = Person(couple.Generation + 1);
                Link("parent", couple.One, born);
                Link("parent", couple.Other, born);
                if (made % 10 == 0)
                {
                    var elder = elders[made % elders.Count];
                    Link("guardian", elder, born);
                    Link("adoptive-parent", elder, born);
                }

                var partner = Person(couple.Generation + 1);
                Link("partner", born, partner);
                couples.Enqueue((born, partner, couple.Generation + 1));
            }
        }

        return (made, links);
    }

    private static long Median(IEnumerable<long> values)
    {
        var sorted = values.Order().ToList();
        return sorted[sorted.Count / 2];
    }

    [Fact]
    public async Task The_largest_tree_goes_the_whole_way_within_its_budgets()
    {
        MailCatcher.Needed();
        var page = await site.Visitor(1280, graph: true);
        var address = MailCatcher.NewAddress("way");
        await AccountJourneyBrowserTests.Ask(page, "fa", AccountJourneyBrowserTests.NewName("Way"), address);
        await page.GotoAsync(await AccountJourneyBrowserTests.Link(address, "تأیید"));
        await page.Locator("main form button[type=submit]").ClickAsync();
        await BrowserSite.Arrived(page, new Regex(@"/account\?said=welcome$"));
        await page.GotoAsync("/fa/account/people");
        var owner = Me().Match(await page.ContentAsync()).Groups[1].Value;
        Assert.Equal(32, owner.Length);

        // The tree, kept in the database of the site through the store – as large as a tree may be.
        var desk = site.Services.GetRequiredService<AccountDesk>();
        string tree;
        (int People, int Links) family;
        using (var store = desk.Open())
        {
            tree = store.AddTree(owner, "خاندانِ ساختگیِ بزرگ") ?? throw new InvalidOperationException("The tree was not kept.");
            family = Family(store, owner, tree, PeopleStore.MostPeople);
        }

        Assert.InRange(family.People, PeopleStore.MostPeople - 1, PeopleStore.MostPeople);

        // The question and the placing, each by itself: the first time (cold) and the middle of five (warm).
        var question = new List<long>();
        var placing = new List<long>();
        TreeView? seen = null;
        for (var round = 0; round < 6; round++)
        {
            using var store = desk.Open();
            var watch = Stopwatch.StartNew();
            seen = store.Tree(owner, tree);
            question.Add(watch.ElapsedMilliseconds);
            watch.Restart();
            Assert.NotNull(TreeEndpoints.Placement(seen!));
            placing.Add(watch.ElapsedMilliseconds);
        }

        Assert.Equal(family.People, seen!.People.Length);
        var (questionWarm, placingWarm) = (Median(question.Skip(1)), Median(placing.Skip(1)));

        // The whole answer of the site, asked as the page asks for it: the time and the size.
        var answers = new List<long>();
        var size = 0;
        for (var round = 0; round < 4; round++)
        {
            var watch = Stopwatch.StartNew();
            var answer = await page.APIRequest.GetAsync($"/fa/account/trees/{tree}/graph.json");
            var body = await answer.BodyAsync();
            answers.Add(watch.ElapsedMilliseconds);
            Assert.True(answer.Ok);
            size = body.Length;
        }

        var answerWarm = Median(answers.Skip(1));

        // The page: from asking for it to the first drawing one can use, the drawing itself, one step, the memory.
        var usable = Stopwatch.StartNew();
        await page.GotoAsync($"/fa/account/trees/{tree}");
        var host = page.Locator("dastan-graph");
        await Assertions.Expect(host).ToHaveAttributeAsync("data-state", "live", new() { Timeout = 120_000 });
        var usableIn = usable.ElapsedMilliseconds;
        var drawnIn = int.Parse((await host.GetAttributeAsync("data-drawn-in"))!, CultureInfo.InvariantCulture);
        Assert.Equal(family.People, await host.EvaluateAsync<int>("host => host.graph.seen.cards.length"));
        await host.Locator("[data-graph-canvas]").FocusAsync();
        var step = await page.EvaluateAsync<double>(
            @"async () => {
                const canvas = document.querySelector('[data-graph-canvas]');
                const press = key => canvas.dispatchEvent(new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true }));
                press('ArrowDown');
                const began = performance.now();
                press('ArrowDown');
                press('+');
                await new Promise(done => requestAnimationFrame(() => requestAnimationFrame(done)));
                return performance.now() - began;
            }");
        var heap = await page.EvaluateAsync<double>("() => performance.memory ? performance.memory.usedJSHeapSize / 1048576 : -1");

        // The scripts the page asked for: Dastan's own, and the control of the diagram, which is somebody else's.
        var scripts = await page.EvaluateAsync<string[]>(
            "() => [...new Set([...document.querySelectorAll('script[src]')].map(script => script.src).concat(performance.getEntriesByType('resource').filter(entry => entry.name.split('?')[0].endsWith('.js')).map(entry => entry.name)))]");
        var (own, others) = (0, 0);
        foreach (var script in scripts)
        {
            var length = (await (await page.APIRequest.GetAsync(script)).BodyAsync()).Length;
            if (script.Contains("graph", StringComparison.Ordinal))
            {
                others += length;
            }
            else
            {
                own += length;
            }
        }

        if (Environment.GetEnvironmentVariable("DASTAN_MEASURES") is { Length: > 0 } file)
        {
            await File.AppendAllTextAsync(
                file,
                string.Create(CultureInfo.InvariantCulture, $"{Environment.GetEnvironmentVariable("DASTAN_BROWSER_ENGINE") ?? "chromium"}\twhole way\t{family.People} people, {family.Links} links\tquestion {question[0]} ms cold, {questionWarm} ms warm\tplacing {placing[0]} ms cold, {placingWarm} ms warm\tanswer {answers[0]} ms cold, {answerWarm} ms warm, {size / 1024} kB\tusable in {usableIn} ms\tdrawn in {drawnIn} ms\tstep {step:0} ms\theap {heap:0} MB\tscripts {own / 1024} kB own, {others / 1024} kB the diagram{Environment.NewLine}"),
                TestContext.Current.CancellationToken);
        }

        // What does not depend on the machine is judged everywhere: one answer holds one tree, a tree holds so
        // many people at most, and the answer and the scripts of Dastan's own stay as small as budgeted.
        Assert.True(size <= AnswerSize, $"The answer for {family.People} people is {size / 1024} kB; the budget is {AnswerSize / 1024} kB.");
        Assert.True(own <= OwnScriptsSize, $"The scripts of Dastan's own are {own / 1024} kB; the budget is {OwnScriptsSize / 1024} kB.");
        using (var store = desk.Open())
        {
            Assert.Null(store.AddPerson(owner, tree, "One too many", null, null, deceased: true));
        }

        if (Environment.GetEnvironmentVariable("DASTAN_BUDGETS") != "record")
        {
            Assert.True(questionWarm <= QuestionBudget, $"The question took {questionWarm} ms; the budget is {QuestionBudget} ms.");
            Assert.True(placingWarm <= PlacingBudget, $"The placing took {placingWarm} ms; the budget is {PlacingBudget} ms.");
            Assert.True(answerWarm <= AnswerBudget, $"The answer took {answerWarm} ms; the budget is {AnswerBudget} ms.");
            Assert.True(usableIn <= UsableBudget, $"The drawing could be used after {usableIn} ms; the budget is {UsableBudget} ms.");
            Assert.True(step <= StepBudget, $"A step took {step:0} ms; the budget is {StepBudget} ms.");
            Assert.True(heap < 0 || heap <= HeapBudget, $"The page holds {heap:0} MB; the budget is {HeapBudget} MB.");
        }
    }
}
