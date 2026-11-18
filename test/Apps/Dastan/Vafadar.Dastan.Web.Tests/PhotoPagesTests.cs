using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Vafadar.Dastan.Data.Tests;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>The demonstration site, told a file for the store of its visitors and a folder for their photographs.</summary>
public sealed class PhotosSite : DemoSite
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-site-photos-" + Guid.NewGuid().ToString("N"));

    /// <summary>Gets the folder the files of the photographs lie in.</summary>
    public string Media => Path.Combine(_folder, "photos", "media");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Dastan:People", Path.Combine(_folder, "people.db"));
        builder.UseSetting("Dastan:Photos", Path.Combine(_folder, "photos"));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }
}

/// <summary>
/// The photographs of the visitors on the site (step 16.5, PRIV-012, PRIV-013): bringing one in, the one
/// address of its file that asks its audience, tags the tagged person approves, the life of a person and
/// recent shares. Every test has a site of its own.
/// </summary>
public sealed class PhotoPagesTests : IDisposable
{
    private const string Password = "a long pass phrase";
    private const string Account = "/en/account";
    private const string Photos = "/en/account/photos";
    private const string People = "/en/account/people";
    private const string Forms = "/preferences/photos";
    private readonly PhotosSite _site = new();

    public void Dispose() => _site.Dispose();

    private async Task<Browser> Signed(string name)
    {
        var visitor = _site.Visitor();
        var made = await visitor.Post("/preferences/account/register", Account, ("name", name), ("age", "on"), ("password", Password), ("again", Password), ("returnUrl", Account));
        Assert.Contains("said=welcome", made.Response.Headers.Location?.OriginalString, StringComparison.Ordinal);
        return visitor;
    }

    private static async Task<string> Me(Browser visitor) =>
        Regex.Match((await visitor.Get(People)).Html, "href=\"people/([a-f0-9]{32})\"[^>]*>Your page").Groups[1].Value;

    // The two show a relationship.
    private static async Task Friends(Browser first, Browser second, string secondName)
    {
        await first.Post("/preferences/people/ask", People, ("name", secondName), ("kind", "friend"), ("returnUrl", People));
        var relationship = Regex.Match((await second.Get(People)).Html, "name=\"relationship\" value=\"([a-f0-9]{32})\"").Groups[1].Value;
        await second.Post("/preferences/people/answer", People, ("relationship", relationship), ("answer", "accept"), ("returnUrl", People));
    }

    // Brings a photograph in and returns its identifier.
    private static async Task<string> Bring(Browser owner, string scope = "private", string caption = "At the sea", string taken = "1998-07-04", byte[]? file = null)
    {
        var known = Ids(await owner.Get(Photos));
        var added = await owner.Upload($"{Forms}/add", Photos, "IMG_0001.png", file ?? TestFiles.Png(), ("caption", caption), ("taken", taken), ("scope", scope), ("confirmed", "on"), ("returnUrl", Photos));
        Assert.Equal($"{Photos}?said=added#mine", added.Response.Headers.Location?.OriginalString);
        return Ids(await owner.Get(Photos)).Except(known).Single();
    }

    private static string[] Ids(Page page) => [.. Regex.Matches(page.Html, "<img src=\"/photo/([a-f0-9]{32})\"").Select(match => match.Groups[1].Value).Distinct()];

    private static Task<Page> Send(Browser visitor, string form, params (string, string)[] fields) =>
        visitor.Post($"{Forms}/{form}", Photos, [.. fields, ("returnUrl", Photos)]);

    [Fact]
    public async Task A_site_without_a_folder_for_them_has_no_photographs()
    {
        using var site = new AccountsSite();
        var visitor = site.Visitor();
        await visitor.Post("/preferences/account/register", Account, ("name", "Mina"), ("age", "on"), ("password", Password), ("again", Password), ("returnUrl", Account));

        Assert.Equal(HttpStatusCode.NotFound, (await visitor.Get(Photos)).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.Get("/photo/0123456789abcdef0123456789abcdef")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.Post($"{Forms}/remove", Account, ("photo", "x"))).Status);
        Assert.DoesNotContain("href=\"account/photos\"", (await visitor.Get(Account)).Html, StringComparison.Ordinal);

        var sent = await _site.Visitor().Get(Photos);
        Assert.Equal(HttpStatusCode.Redirect, sent.Status);
        Assert.EndsWith(Account, sent.Response.Headers.Location?.OriginalString, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("en", "Photographs", "Bring a photograph in", "Only you see it until you say who else may")]
    [InlineData("fa", "عکس‌ها", "آوردن عکس", "فقط خودتان آن را می‌بینید")]
    public async Task The_page_says_in_the_language_of_the_page_whose_a_photograph_is(string language, string title, string bring, string rule)
    {
        var mina = await Signed("Mina");

        var page = await mina.Get($"/{language}/account/photos");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains(title, page.Main, StringComparison.Ordinal);
        Assert.Contains(bring, page.Main, StringComparison.Ordinal);
        Assert.Contains(rule, page.Main, StringComparison.Ordinal);
        Assert.Contains("enctype=\"multipart/form-data\"", page.Html, StringComparison.Ordinal);
        Assert.Contains("<meta name=\"robots\" content=\"noindex, nofollow\"", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", page.Own, StringComparison.Ordinal);
        Assert.Contains("href=\"account/photos\"", (await mina.Get($"/{language}/account")).Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_photograph_is_brought_in_kept_without_what_it_says_about_its_making_and_shown_to_its_owner_only()
    {
        var mina = await Signed("Mina");
        var sara = await Signed("Sara");
        await Friends(mina, sara, "Sara");

        var id = await Bring(mina);

        var page = await mina.Get(Photos);
        Assert.Matches($"<img src=\"/photo/{id}\" alt=\"At the sea\" width=\"640\" height=\"480\" loading=\"lazy\"", page.Html);
        Assert.Contains("taken 1998-07-04", page.Main, StringComparison.Ordinal);
        Assert.Contains("The photograph is kept.", (await mina.Get($"{Photos}?said=added")).Main, StringComparison.Ordinal);

        // The file has a name the site made, and says nothing any more about who made it.
        var kept = Assert.Single(Directory.GetFiles(_site.Media));
        Assert.Equal($"{id}-file.png", Path.GetFileName(kept));
        Assert.DoesNotContain("Somebody at a place", Encoding.ASCII.GetString(File.ReadAllBytes(kept)), StringComparison.Ordinal);

        // The address of the file gives it to its owner and to nobody else – not to a friend either.
        var file = await mina.Get($"/photo/{id}");
        Assert.Equal(HttpStatusCode.OK, file.Status);
        Assert.Equal("image/png", file.Response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(File.ReadAllBytes(kept), await file.Bytes());
        Assert.Contains("private", file.Response.Headers.CacheControl?.ToString(), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, (await sara.Get($"/photo/{id}")).Status);
        var nobody = await _site.Visitor().Get($"/photo/{id}");
        Assert.Equal(HttpStatusCode.NotFound, nobody.Status);
        Assert.Empty(nobody.Html);
        Assert.DoesNotContain(id, (await sara.Get(Photos)).Html, StringComparison.Ordinal);
        Assert.DoesNotContain(id, (await sara.Get($"/en/people/{await Me(mina)}")).Html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("text", "1998-07-04", "private", "photo-file", "The file is no PNG, JPEG or WebP image")]
    [InlineData("svg", "1998-07-04", "private", "photo-file", "The file is no PNG, JPEG or WebP image")]
    [InlineData("png", "last summer", "private", "photo-form", "a day is written like 1998-07-04")]
    [InlineData("png", "2999-01-01", "private", "photo-form", "is not in the future")]
    [InlineData("png", "", "everybody", "photo-form", "Please choose a picture")]
    [InlineData("large", "", "private", "photo-large", "The picture is larger than 10 MB.")]
    public async Task What_cannot_be_kept_is_not_kept_and_the_page_says_why(string kind, string taken, string scope, string problem, string said)
    {
        var mina = await Signed("Mina");
        var file = kind switch
        {
            "text" => Encoding.UTF8.GetBytes("This is no picture."),
            "svg" => Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>"),
            "large" => [.. TestFiles.Png(), .. new byte[(10 * 1024 * 1024) + 1]],
            _ => TestFiles.Png(),
        };

        var answer = await mina.Upload($"{Forms}/add", Photos, "picture.png", file, ("caption", "At the sea"), ("taken", taken), ("scope", scope), ("returnUrl", Photos));

        Assert.Equal($"{Photos}?form=add&problem={problem}#add", answer.Response.Headers.Location?.OriginalString);
        var page = await mina.Get($"{Photos}?form=add&problem={problem}");
        Assert.Contains(said, page.Main, StringComparison.Ordinal);
        Assert.Contains("You have brought in no photograph yet.", page.Main, StringComparison.Ordinal);

        // Nothing is left behind.
        Assert.Empty(Directory.GetFiles(_site.Media));
    }

    [Fact]
    public async Task The_audience_its_owner_chooses_holds_at_once_for_the_file_and_for_every_page()
    {
        var mina = await Signed("Mina");
        var sara = await Signed("Sara");
        var dara = await Signed("Dara");
        await Friends(mina, sara, "Sara");
        await Friends(mina, dara, "Dara");
        await mina.Post("/preferences/people/circle-add", People, ("name", "Trusted"), ("returnUrl", People));
        var circle = Regex.Match((await mina.Get(People)).Html, "name=\"circle\" value=\"(\\d+)\"").Groups[1].Value;
        await mina.Post("/preferences/people/circle-set", People, ("circle", circle), ("member", await Me(sara)), ("returnUrl", People));
        var id = await Bring(mina, $"circle-{circle}");
        var page = $"/en/people/{await Me(mina)}";

        // For a circle: who is in it, and nobody else – at the address of the file, on the page of the owner
        // and among what was shared lately.
        Assert.Equal(HttpStatusCode.OK, (await sara.Get($"/photo/{id}")).Status);
        Assert.Contains(id, (await sara.Get(page)).Html, StringComparison.Ordinal);
        var recent = await sara.Get(Photos);
        Assert.Contains("Shared with you lately", recent.Main, StringComparison.Ordinal);
        Assert.Contains(id, recent.Html, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, (await dara.Get($"/photo/{id}")).Status);
        Assert.DoesNotContain(id, (await dara.Get(page)).Html, StringComparison.Ordinal);
        Assert.DoesNotContain("Shared with you lately", (await dara.Get(Photos)).Main, StringComparison.Ordinal);
        Assert.Matches($"<option value=\"circle-{circle}\" selected[^>]*>Circle: Trusted</option>", (await mina.Get(Photos)).Html);

        // For everybody.
        Assert.Equal($"{Photos}?said=shared#mine", (await Send(mina, "share", ("photo", id), ("scope", "public"), ("confirmed", "on"))).Response.Headers.Location?.OriginalString);
        Assert.Equal(HttpStatusCode.OK, (await _site.Visitor().Get($"/photo/{id}")).Status);
        Assert.Contains(id, (await _site.Visitor().Get(page)).Html, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, (await dara.Get($"/photo/{id}")).Status);

        // And for nobody again: gone at once, everywhere.
        await Send(mina, "share", ("photo", id), ("scope", "private"));
        Assert.Equal(HttpStatusCode.NotFound, (await _site.Visitor().Get($"/photo/{id}")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await sara.Get($"/photo/{id}")).Status);
        Assert.DoesNotContain(id, (await sara.Get(page)).Html, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, (await mina.Get($"/photo/{id}")).Status);
    }

    [Fact]
    public async Task Somebody_a_photograph_shows_is_asked_and_approving_shows_it_to_nobody_new()
    {
        var mina = await Signed("Mina");
        var sara = await Signed("Sara");
        var dara = await Signed("Dara");
        await Friends(mina, sara, "Sara");
        await Friends(sara, dara, "Dara");
        await mina.Post("/preferences/people/circle-add", People, ("name", "Trusted"), ("returnUrl", People));
        var circle = Regex.Match((await mina.Get(People)).Html, "name=\"circle\" value=\"(\\d+)\"").Groups[1].Value;
        await mina.Post("/preferences/people/circle-set", People, ("circle", circle), ("member", await Me(sara)), ("returnUrl", People));
        var id = await Bring(mina, $"circle-{circle}");
        var saras = $"/en/people/{await Me(sara)}";

        var tagged = await Send(mina, "tag", ("photo", id), ("account", await Me(sara)));

        Assert.Equal($"{Photos}?said=tagged#mine", tagged.Response.Headers.Location?.OriginalString);
        Assert.Matches("<bdi>Sara</bdi>\\s*<span class=\"meta\">– waits for their answer", (await mina.Get(Photos)).Main);
        var asked = await sara.Get(Photos);
        Assert.Contains("Are you in these photographs?", asked.Main, StringComparison.Ordinal);
        Assert.Contains("nobody sees it who could not before", asked.Main, StringComparison.Ordinal);

        // Until she answers the photograph is not among hers.
        Assert.DoesNotContain(id, (await mina.Get(saras)).Html, StringComparison.Ordinal);
        Assert.Equal("/refused", (await Send(dara, "answer", ("photo", id), ("answer", "approve"))).Response.Headers.Location?.OriginalString);
        Assert.Equal("/refused", (await Send(mina, "answer", ("photo", id), ("answer", "approve"))).Response.Headers.Location?.OriginalString);

        var approved = await Send(sara, "answer", ("photo", id), ("answer", "approve"));

        Assert.Equal($"{Photos}?said=approved", approved.Response.Headers.Location?.OriginalString);
        Assert.DoesNotContain("Are you in these photographs?", (await sara.Get(Photos)).Main, StringComparison.Ordinal);
        Assert.Matches("<bdi>Sara</bdi>\\s*<span class=\"meta\">– is shown in it", (await mina.Get(Photos)).Main);

        // It stands among Sara's photographs for who Mina lets see it – with whose it is – and for nobody else:
        // Dara is Sara's friend, not in Mina's circle.
        var life = await mina.Get(saras);
        Assert.Contains(id, life.Html, StringComparison.Ordinal);
        Assert.Matches("<bdi>Mina</bdi></a>", life.Main);
        Assert.Contains(id, (await sara.Get(saras)).Html, StringComparison.Ordinal);
        Assert.DoesNotContain(id, (await dara.Get(saras)).Html, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, (await dara.Get($"/photo/{id}")).Status);
        Assert.DoesNotContain(id, (await _site.Visitor().Get(saras)).Html, StringComparison.Ordinal);

        // She takes it away and is not asked again.
        Assert.Equal($"{Photos}?said=tag-removed", (await Send(sara, "answer", ("photo", id), ("answer", "remove"))).Response.Headers.Location?.OriginalString);
        Assert.DoesNotContain(id, (await mina.Get(saras)).Html, StringComparison.Ordinal);
        Assert.Matches("<bdi>Sara</bdi>\\s*<span class=\"meta\">– said it is not them", (await mina.Get(Photos)).Main);
        Assert.Equal("/refused", (await Send(mina, "untag", ("photo", id), ("account", await Me(sara)))).Response.Headers.Location?.OriginalString);
        Assert.Equal("/refused", (await Send(mina, "tag", ("photo", id), ("account", await Me(sara)))).Response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Only_its_owner_changes_tags_or_removes_a_photograph_and_removing_deletes_the_file()
    {
        var mina = await Signed("Mina");
        var sara = await Signed("Sara");
        var zal = await Signed("Zal");
        await Friends(mina, sara, "Sara");
        var id = await Bring(mina, "public");

        Assert.Equal("/refused", (await Send(sara, "share", ("photo", id), ("scope", "private"))).Response.Headers.Location?.OriginalString);
        Assert.Equal("/refused", (await Send(sara, "remove", ("photo", id))).Response.Headers.Location?.OriginalString);
        Assert.Equal("/refused", (await Send(sara, "tag", ("photo", id), ("account", await Me(sara)))).Response.Headers.Location?.OriginalString);
        Assert.Equal("/refused", (await Send(mina, "share", ("photo", id), ("scope", "circle-99"))).Response.Headers.Location?.OriginalString);

        // A stranger is not tagged, and is not offered.
        Assert.Equal("/refused", (await Send(mina, "tag", ("photo", id), ("account", await Me(zal)))).Response.Headers.Location?.OriginalString);
        Assert.DoesNotContain(await Me(zal), (await mina.Get(Photos)).Html, StringComparison.Ordinal);
        Assert.Single(Directory.GetFiles(_site.Media));

        var removed = await Send(mina, "remove", ("photo", id));

        Assert.Equal($"{Photos}?said=removed#mine", removed.Response.Headers.Location?.OriginalString);
        Assert.Empty(Directory.GetFiles(_site.Media));
        Assert.Equal(HttpStatusCode.NotFound, (await mina.Get($"/photo/{id}")).Status);
        Assert.Contains("You have brought in no photograph yet.", (await mina.Get(Photos)).Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_photograph_that_names_somebody_is_shown_to_everybody_only_when_they_agreed()
    {
        var mina = await Signed("Mina");
        var sara = await Signed("Sara");
        await Friends(mina, sara, "Sara");
        var id = await Bring(mina, "public");
        var stranger = _site.Visitor();
        Assert.Equal(HttpStatusCode.OK, (await stranger.Get($"/photo/{id}")).Status);

        await Send(mina, "tag", ("photo", id), ("account", await Me(sara)));

        // Held back at once: for a stranger there is no such photograph, its owner is told why, she is asked.
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.Get($"/photo/{id}")).Status);
        var owners = await mina.Get(Photos);
        Assert.Contains("Held back", owners.Main, StringComparison.Ordinal);
        Assert.Contains("has not agreed yet that everybody sees it", owners.Main, StringComparison.Ordinal);
        var asked = await sara.Get(Photos);
        Assert.Contains("Shown to everybody?", asked.Main, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, (await sara.Get($"/photo/{id}")).Status);

        // Nobody agrees for her – not the owner either.
        Assert.Equal("/refused", (await Send(mina, "agree", ("photo", id), ("answer", "agree"))).Response.Headers.Location?.OriginalString);
        Assert.Equal("/refused", (await Send(sara, "agree", ("photo", id), ("answer", "perhaps"))).Response.Headers.Location?.OriginalString);

        var agreed = await Send(sara, "agree", ("photo", id), ("answer", "agree"));

        Assert.Equal($"{Photos}?said=agreed#asked", agreed.Response.Headers.Location?.OriginalString);
        Assert.Equal(HttpStatusCode.OK, (await stranger.Get($"/photo/{id}")).Status);
        Assert.DoesNotContain("Held back", (await mina.Get(Photos)).Main, StringComparison.Ordinal);
        Assert.Contains("agreed that everybody sees it", (await mina.Get(Photos)).Main, StringComparison.Ordinal);
        Assert.Contains("Take the agreement back", (await sara.Get(Photos)).Main, StringComparison.Ordinal);

        var back = await Send(sara, "agree", ("photo", id), ("answer", "back"));

        Assert.Equal($"{Photos}?said=agreement-back#asked", back.Response.Headers.Location?.OriginalString);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.Get($"/photo/{id}")).Status);
    }

    [Fact]
    public async Task A_photograph_is_shown_to_everybody_only_when_its_owner_says_that_the_people_in_it_agree()
    {
        var mina = await Signed("Mina");
        var sara = await Signed("Sara");
        Directory.CreateDirectory(_site.Media);
        var files = Directory.GetFiles(_site.Media, "*", SearchOption.AllDirectories).Length;

        // Brought in for everybody without the words: nothing is kept, and the page says why.
        var refused = await mina.Upload($"{Forms}/add", Photos, "IMG_0001.png", TestFiles.Png(), ("caption", "At the sea"), ("taken", string.Empty), ("scope", "public"), ("returnUrl", Photos));
        Assert.Equal($"{Photos}?form=add&problem=photo-agreement#add", refused.Response.Headers.Location?.OriginalString);
        Assert.Empty(Ids(await mina.Get(Photos)));
        Assert.Equal(files, Directory.GetFiles(_site.Media, "*", SearchOption.AllDirectories).Length);
        Assert.Contains("the people in it agree", (await mina.Get($"{Photos}?form=add&problem=photo-agreement")).Main, StringComparison.Ordinal);

        // For the owner alone or a circle nothing is asked.
        var id = await Bring(mina, "private");
        var kept = await Send(mina, "share", ("photo", id), ("scope", "public"));
        Assert.Equal($"{Photos}?form=mine&problem=photo-agreement#mine", kept.Response.Headers.Location?.OriginalString);
        Assert.Equal(HttpStatusCode.NotFound, (await sara.Get($"/photo/{id}")).Status);
        Assert.Contains("role=\"alert\"", (await mina.Get($"{Photos}?form=mine&problem=photo-agreement")).Html, StringComparison.Ordinal);

        Assert.Equal($"{Photos}?said=shared#mine", (await Send(mina, "share", ("photo", id), ("scope", "public"), ("confirmed", "on"))).Response.Headers.Location?.OriginalString);
        Assert.Equal(HttpStatusCode.OK, (await sara.Get($"/photo/{id}")).Status);

        // Somebody else changes nothing with the words.
        Assert.Equal("/refused", (await Send(sara, "share", ("photo", id), ("scope", "public"), ("confirmed", "on"))).Response.Headers.Location?.OriginalString);
        Assert.Equal("/refused", (await Send(sara, "share", ("photo", id), ("scope", "public"))).Response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task An_album_names_photographs_and_shows_each_viewer_those_they_may_see()
    {
        var mina = await Signed("Mina");
        var sara = await Signed("Sara");
        var open = await Bring(mina, "public", caption: "The beach");
        var own = await Bring(mina, "private", caption: "The house");
        var minas = $"/en/people/{await Me(mina)}";

        var made = await Send(mina, "album-add", ("name", "Summer 1998"), ("note", "At the Caspian"));

        Assert.Equal($"{Photos}?said=album-added#albums", made.Response.Headers.Location?.OriginalString);
        var number = Regex.Match((await mina.Get(Photos)).Html, "name=\"album\" value=\"(\\d+)\"").Groups[1].Value;
        Assert.Contains("Nothing in it yet", (await mina.Get(Photos)).Main, StringComparison.Ordinal);

        // An album that names nothing a viewer may see is not on the page of the person.
        Assert.DoesNotContain("Summer 1998", (await sara.Get(minas)).Main, StringComparison.Ordinal);

        Assert.Equal($"{Photos}?said=placed#albums", (await Send(mina, "album-place", ("album", number), ("photo", own))).Response.Headers.Location?.OriginalString);
        Assert.DoesNotContain("Summer 1998", (await sara.Get(minas)).Main, StringComparison.Ordinal);
        await Send(mina, "album-place", ("album", number), ("photo", open));
        Assert.Equal("/refused", (await Send(mina, "album-place", ("album", number), ("photo", open))).Response.Headers.Location?.OriginalString);

        // Sara and a stranger see the album with the one photograph everybody may see; Mina sees both.
        foreach (var viewer in new[] { sara, _site.Visitor() })
        {
            var seen = await viewer.Get(minas);
            Assert.Contains("Summer 1998", seen.Main, StringComparison.Ordinal);
            Assert.Contains("At the Caspian", seen.Main, StringComparison.Ordinal);
            Assert.Contains(open, seen.Html, StringComparison.Ordinal);
            Assert.DoesNotContain(own, seen.Html, StringComparison.Ordinal);
        }

        Assert.Contains(own, (await mina.Get(minas)).Html, StringComparison.Ordinal);

        // Nobody else puts anything into it or takes it apart.
        Assert.Equal("/refused", (await Send(sara, "album-take", ("album", number), ("photo", open))).Response.Headers.Location?.OriginalString);
        Assert.Equal("/refused", (await Send(sara, "album-remove", ("album", number))).Response.Headers.Location?.OriginalString);
        Assert.Equal("/refused", (await Send(sara, "album-change", ("album", number), ("name", "Hers"), ("note", string.Empty))).Response.Headers.Location?.OriginalString);

        // A name another album has is said; a new name is kept.
        await Send(mina, "album-add", ("name", "Winter"), ("note", string.Empty));
        var taken = await Send(mina, "album-change", ("album", number), ("name", "winter"), ("note", string.Empty));
        Assert.Equal($"{Photos}?form=albums&problem=album-name#albums", taken.Response.Headers.Location?.OriginalString);
        Assert.Contains("role=\"alert\"", (await mina.Get($"{Photos}?form=albums&problem=album-name")).Html, StringComparison.Ordinal);
        Assert.Equal($"{Photos}?form=albums&problem=album-name#albums", (await Send(mina, "album-add", ("name", " "), ("note", string.Empty))).Response.Headers.Location?.OriginalString);
        await Send(mina, "album-change", ("album", number), ("name", "The summer of 1998"), ("note", string.Empty));
        Assert.Contains("The summer of 1998", (await sara.Get(minas)).Main, StringComparison.Ordinal);

        // Out of the album the photograph is still hers and still seen; without the album too.
        Assert.Equal($"{Photos}?said=taken-out#albums", (await Send(mina, "album-take", ("album", number), ("photo", open))).Response.Headers.Location?.OriginalString);
        Assert.DoesNotContain("The summer of 1998", (await sara.Get(minas)).Main, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, (await sara.Get($"/photo/{open}")).Status);
        Assert.Equal($"{Photos}?said=album-removed#albums", (await Send(mina, "album-remove", ("album", number))).Response.Headers.Location?.OriginalString);
        Assert.Equal([own, open], Ids(await mina.Get(Photos)).Order().ToArray() is var both && both.Length == 2 ? [own, open] : []);
    }

    [Fact]
    public async Task Photographs_are_gone_through_by_the_year_they_were_taken()
    {
        var mina = await Signed("Mina");
        var old = await Bring(mina, "public", caption: "Long ago", taken: "1998-07-04");
        var late = await Bring(mina, "public", caption: "Lately", taken: "2024-03-01");
        var hidden = await Bring(mina, "private", caption: "For me", taken: "2011-01-01");
        var undated = await Bring(mina, "public", caption: "Some day", taken: string.Empty);
        var minas = $"/en/people/{await Me(mina)}";

        var all = await mina.Get(Photos);
        Assert.Matches("href=\"account/photos\\?year=2024#mine\"[^>]*>2024</a>.*href=\"account/photos\\?year=2011#mine\".*href=\"account/photos\\?year=1998#mine\"", all.Html.ReplaceLineEndings(" "));
        Assert.Equal(4, Ids(all).Length);

        var year = await mina.Get($"{Photos}?year=1998");
        Assert.Equal([old], Ids(year));
        Assert.Contains("aria-current=\"true\">1998</a>", year.Html, StringComparison.Ordinal);

        // A year nothing was taken in is all years.
        Assert.Equal(4, Ids(await mina.Get($"{Photos}?year=1500")).Length);

        // A stranger is offered the years of what they may see: not the year of a photograph that is hers alone.
        var strangers = await _site.Visitor().Get(minas);
        Assert.Contains($"people/{await Me(mina)}?year=2024#person-photos", strangers.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("year=2011", strangers.Html, StringComparison.Ordinal);
        Assert.Equal([late], Ids(await _site.Visitor().Get($"{minas}?year=2024")));
        Assert.Equal(3, Ids(await _site.Visitor().Get($"{minas}?year=2011")).Length);
        Assert.DoesNotContain(hidden, strangers.Html, StringComparison.Ordinal);
        Assert.Contains(undated, strangers.Html, StringComparison.Ordinal);

        // In Persian a year is written in Persian digits and without a separator.
        Assert.Contains(">۱۹۹۸</a>", (await mina.Get("/fa/account/photos")).Main, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("add")]
    [InlineData("agree")]
    [InlineData("album-add")]
    [InlineData("album-change")]
    [InlineData("album-remove")]
    [InlineData("album-place")]
    [InlineData("album-take")]
    [InlineData("share")]
    [InlineData("remove")]
    [InlineData("tag")]
    [InlineData("untag")]
    [InlineData("answer")]
    public async Task Every_form_is_refused_to_who_is_not_signed_in(string form)
    {
        Directory.CreateDirectory(_site.Media);
        // The form that brings a photograph in carries a file; the others are plain forms.
        (string, string)[] fields = [("photo", "x"), ("scope", "public"), ("account", "x"), ("answer", "approve"), ("album", "1"), ("name", "x"), ("note", "x"), ("returnUrl", Photos)];
        var answer = form == "add"
            ? await _site.Visitor().Upload($"{Forms}/{form}", Account, "picture.png", TestFiles.Png(), fields)
            : await _site.Visitor().Post($"{Forms}/{form}", Account, fields);

        Assert.Equal("/refused", answer.Response.Headers.Location?.OriginalString);
        Assert.Empty(Directory.GetFiles(_site.Media));
    }

    [Theory]
    [InlineData("0123456789abcdef0123456789abcdef")]
    [InlineData("..%2Fpeople.db")]
    [InlineData("x")]
    public async Task An_address_that_names_no_photograph_gives_nothing(string id)
    {
        var mina = await Signed("Mina");
        await Bring(mina, "public");

        var answer = await mina.Get($"/photo/{id}");

        Assert.Equal(HttpStatusCode.NotFound, answer.Status);
        Assert.Empty(answer.Html);
    }

    [Fact]
    public async Task A_file_that_belongs_to_no_photograph_is_deleted_and_one_that_only_just_arrived_is_left()
    {
        // Before the site is asked for anything: a file of long ago that nothing names, and one of just now.
        Directory.CreateDirectory(_site.Media);
        var old = Path.Combine(_site.Media, "0123456789abcdef0123456789abcdef-file.png");
        var fresh = Path.Combine(_site.Media, "fedcba9876543210fedcba9876543210-file.png");
        File.WriteAllBytes(old, TestFiles.Png());
        File.WriteAllBytes(fresh, TestFiles.Png());
        File.SetLastWriteTimeUtc(old, DateTime.UtcNow.AddDays(-2));

        var mina = await Signed("Mina");
        var id = await Bring(mina);

        Assert.False(File.Exists(old));
        Assert.True(File.Exists(fresh));
        Assert.True(File.Exists(Path.Combine(_site.Media, $"{id}-file.png")));
    }
}
