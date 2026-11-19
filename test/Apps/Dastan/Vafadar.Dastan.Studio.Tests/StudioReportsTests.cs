using System.Net;
using Vafadar.Dastan.Data;

namespace Vafadar.Dastan.Studio.Tests;

/// <summary>
/// The reports of visitors in the tool of the editors (step 14.8): the list of what waits, the way to the
/// record a report is about, and the note that somebody has looked. Test data.
/// </summary>
public sealed class StudioReportsTests : IDisposable
{
    private static readonly DateOnly Day = new(2026, 11, 16);
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-studio-reports-" + Guid.NewGuid().ToString("N"));
    private readonly Studio _studio;
    private readonly Desk _desk;
    private readonly ReportBox _box;

    public StudioReportsTests()
    {
        _studio = new Studio(accounts: false) { Reports = true };
        _desk = new Desk(_studio);
        _box = new ReportBox(_studio.Database);
    }

    public void Dispose()
    {
        _desk.Dispose();
        _studio.Dispose();
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Theory]
    [InlineData("en", "Reports of visitors", "No report waits.")]
    [InlineData("fa", "گزارش‌های بازدیدکنندگان", "گزارشی در انتظار نیست.")]
    public async Task The_tool_has_a_page_of_the_reports_that_says_when_none_waits(string language, string title, string none)
    {
        var home = await _desk.Get($"/{language}/");
        var page = await _desk.Get($"/{language}/reports");

        Assert.Contains("href=\"reports\"", home.Html, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains(title, page.Text, StringComparison.Ordinal);
        Assert.Contains(none, page.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_tool_that_was_told_no_folder_has_no_such_page_and_no_link_to_it()
    {
        using var plain = new Studio();
        using var desk = new Desk(plain);

        Assert.DoesNotContain("href=\"reports\"", (await desk.Get("/en/")).Html, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, (await desk.Get("/en/reports")).Status);
    }

    [Fact]
    public async Task A_report_is_listed_with_its_words_and_the_way_to_its_record()
    {
        _box.Take(Studio.Collection, 1, "fa", "evidence/entity/character-rostam", "The summary <b>names</b> the wrong father.", Day);
        _box.Take(Studio.Collection, null, "en", "evidence/event/event-01/entity/character-rostam", "He was not there.", Day.AddDays(1));

        var page = await _desk.Get("/en/reports");

        Assert.Contains("Waiting: 2", page.Text, StringComparison.Ordinal);
        Assert.Contains("The summary <b>names</b> the wrong father.", page.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("<b>names</b>", page.Html, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Studio.Collection}/record/entities/character-rostam\"", page.Html, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Studio.Collection}/record/events/event-01\"", page.Html, StringComparison.Ordinal);
        Assert.Contains("revision 1", page.Text, StringComparison.Ordinal);
        Assert.Contains("2026-11-16", page.Text, StringComparison.Ordinal);

        // The way leads to a page of the tool.
        Assert.Equal(HttpStatusCode.OK, (await _desk.Get($"/en/{Studio.Collection}/record/entities/character-rostam")).Status);
    }

    [Fact]
    public async Task An_editor_notes_that_a_report_was_looked_at_and_it_waits_no_longer()
    {
        _box.Take(Studio.Collection, null, "en", "evidence/entity/character-rostam", "The first.", Day);
        _box.Take(Studio.Collection, null, "en", "evidence/entity/character-sohrab", "The second.", Day);
        var first = _box.Waiting().Single(report => report.Text == "The first.");

        var done = await _desk.Submit("/en/reports", "settle", ("Settling.Id", first.Id), ("Settling.By", "a reviewer"));

        Assert.Equal("/en/reports?done=settled", done.Arrived);
        Assert.Contains("Noted: the report was looked at.", done.Text, StringComparison.Ordinal);
        Assert.Equal("The second.", Assert.Single(_box.Waiting()).Text);
        var looked = Assert.Single(_box.Looked());
        Assert.Equal(("The first.", "a reviewer"), (looked.Text, looked.SettledBy));
        Assert.Contains("looked at by a reviewer", done.Text, StringComparison.Ordinal);

        // The store of the editors was not touched: a report changes nothing by itself.
        using var store = _studio.Store();
        Assert.DoesNotContain(store.History(Studio.Collection), change => (change.Reason ?? string.Empty).Contains("report", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task A_report_somebody_else_has_looked_at_meanwhile_and_an_act_without_a_name_are_said_so()
    {
        _box.Take(Studio.Collection, null, "en", "evidence/entity/character-rostam", "The only one.", Day);
        var report = Assert.Single(_box.Waiting());

        var nameless = await _desk.Submit("/en/reports", "settle", ("Settling.Id", report.Id), ("Settling.By", " "));
        Assert.Single(_box.Waiting());
        Assert.Null(nameless.Arrived);

        var opened = await _desk.Get("/en/reports");
        Assert.True(_box.Settle(report.Id, "somebody else", Day));
        var late = await _desk.Send(opened, "settle", ("Settling.Id", report.Id), ("Settling.By", "a reviewer"));

        Assert.Contains("That report waits no longer", late.Text, StringComparison.Ordinal);
        Assert.Equal("somebody else", Assert.Single(_box.Looked()).SettledBy);
    }
}
