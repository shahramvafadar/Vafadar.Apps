using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Dastan.Core;
using Vafadar.Dastan.Web.Site;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>The galleries, the page of a media item and the route that delivers its files.</summary>
public sealed class GalleryAndMediaTests(DemoSite site) : IClassFixture<DemoSite>
{
    private const string Story = "story-rostam-sohrab";
    private const string Diagram = "media-dastan-family-illustration";
    private const string Video = "media-dastan-parentage-video";

    private static string File(string item, string part) => $"/media/{Story}/{item}/{part}";

    private static List<string> Cards(Page page) =>
        [.. Regex.Matches(page.Html, "<li class=\"card media-card\" id=\"([^\"]+)\"").Select(match => match.Groups[1].Value)];

    private Projection At(int level) => Projection.Of(site.Catalogue, new ReadingLevel(level), Audience.LocalDemonstration)!;

    private string OnDisk(string name) =>
        Path.Combine(site.Services.GetRequiredService<IWebHostEnvironment>().ContentRootPath, "..", "content", "media", name);

    // --- The gallery ---

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task Before_the_media_are_reached_the_gallery_is_empty_and_gives_nothing_away(int level)
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(level);

        var collection = await visitor.Get($"/en/{Story}");
        var gallery = await visitor.Get($"/en/{Story}/gallery");

        Assert.Contains($"{Story}/gallery", collection.Links);
        Assert.Equal(HttpStatusCode.OK, gallery.Status);
        Assert.Contains("There is nothing to show here.", gallery.Main, StringComparison.Ordinal);
        Assert.Contains("Images and videos are shown only as far as the story has been read.", gallery.Main, StringComparison.Ordinal);
        Assert.Empty(Cards(gallery));

        // No picture, no address of a file, no count, no name of what comes later.
        Assert.DoesNotContain("<img", gallery.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("/media/", gallery.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("Shown here", gallery.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("diagram", gallery.Main, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public async Task From_the_third_stage_the_gallery_shows_the_items_of_the_projection_in_its_order(int level)
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(level);

        var page = await visitor.Get($"/en/{Story}/gallery");

        // The items that are only references to a museum are not released for showing and are not counted.
        Assert.Equal(At(level).Gallery().Items.Select(item => item.Id.Value), Cards(page));
        Assert.Equal([Video, Diagram], Cards(page));
        Assert.Contains("Shown here: 2", page.Main, StringComparison.Ordinal);
        Assert.Contains("The order says nothing about dates.", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("There is nothing to show here.", page.Main, StringComparison.Ordinal);

        var card = Regex.Match(page.Html, $"<li class=\"card media-card\" id=\"{Video}\">.*?</li>", RegexOptions.Singleline).Value;
        Assert.Contains($"<img src=\"{File(Video, "poster")}\" alt loading=\"lazy\"", card, StringComparison.Ordinal);
        Assert.Matches("<p class=\"meta\">Video – Explanatory diagram made today</p>", card);
        Assert.Matches($"<h2><a href=\"{Story}/media/{Video}\"><bdi>A connected family in the Shahnameh</bdi></a></h2>", card);
        Assert.Contains("Made: 15 November 2026 CE", card, StringComparison.Ordinal);
        Assert.Contains($"<img src=\"{File(Diagram, "file")}\" alt loading=\"lazy\"", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_gallery_of_an_entity_holds_what_shows_it_and_is_offered_only_when_it_holds_something()
    {
        var visitor = site.Visitor();
        var before = await visitor.Get($"/en/{Story}/entity/character-rostam");
        await visitor.ReadTo(2);

        var rostam = await visitor.Get($"/en/{Story}/entity/character-rostam");
        var gallery = await visitor.Get($"/en/{Story}/entity/character-rostam/gallery");
        var rakhsh = await visitor.Get($"/en/{Story}/entity/character-rakhsh");
        var empty = await visitor.Get($"/en/{Story}/entity/character-rakhsh/gallery");

        Assert.DoesNotContain($"{Story}/entity/character-rostam/gallery", before.Links);
        Assert.Contains($"{Story}/entity/character-rostam/gallery", rostam.Links);
        Assert.Contains("<h1>Images and videos of Rostam</h1>", gallery.Html, StringComparison.Ordinal);
        Assert.Equal([Video, Diagram], Cards(gallery));

        // A card of this gallery leads to the item and says where the visitor came from.
        Assert.Contains($"{Story}/media/{Diagram}?entity=character-rostam", gallery.Links);
        Assert.Contains($"{Story}/gallery", gallery.Links);

        Assert.DoesNotContain($"{Story}/entity/character-rakhsh/gallery", rakhsh.Links);
        Assert.Equal(HttpStatusCode.OK, empty.Status);
        Assert.Empty(Cards(empty));
        Assert.Contains("There is nothing to show here.", empty.Main, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("entity/character-sohrab/gallery")]
    [InlineData("entity/character-nobody/gallery")]
    [InlineData("event/event-08/gallery")]
    [InlineData("event/event-99/gallery")]
    [InlineData($"media/{Diagram}")]
    [InlineData($"media/{Video}")]
    [InlineData("media/media-met-453265")]
    [InlineData("media/media-nothing")]
    public async Task What_is_not_read_yet_and_what_does_not_exist_are_not_found(string address)
    {
        Assert.Equal(HttpStatusCode.NotFound, (await site.Visitor().Get($"/en/{Story}/{address}")).Status);
    }

    [Fact]
    public async Task The_gallery_of_a_step_exists_for_a_step_that_was_read_and_is_empty_when_nothing_shows_it()
    {
        var page = await site.Visitor().Get($"/en/{Story}/event/event-01/gallery");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("Images and videos of the step “A missing horse and a welcome”", page.Main, StringComparison.Ordinal);
        Assert.Empty(Cards(page));
        Assert.Contains($"{Story}/story#step-1", page.Links);
    }

    // --- The page of an item ---

    [Fact]
    public async Task The_page_of_the_diagram_shows_it_with_what_it_is_who_made_it_and_what_it_shows()
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(2);

        var page = await visitor.Get($"/en/{Story}/media/{Diagram}");
        var main = page.Main;

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("<h1><bdi>Family connections in the Shahnameh</bdi></h1>", page.Html, StringComparison.Ordinal);
        Assert.Matches($"<img src=\"{File(Diagram, "file")}\" alt=\"Family relationship diagram: Zal is the father of Rostam;[^\"]*\"", page.Html);
        Assert.Contains("<figcaption><bdi>A present-day diagram connecting Zal to Rostam", page.Html, StringComparison.Ordinal);

        // What kind of picture it is, when it was made – named as the time of the picture – who made it, the rights.
        Assert.Contains("Image – Explanatory diagram made today", main, StringComparison.Ordinal);
        Assert.Contains("It is not a historical record and not evidence.", main, StringComparison.Ordinal);
        Assert.Contains("15 November 2026 CE", main, StringComparison.Ordinal);
        Assert.Contains("When this was made – not a time in the story.", main, StringComparison.Ordinal);
        Assert.Contains("Original Dastan project diagram using geometric shapes and text", main, StringComparison.Ordinal);
        Assert.Contains("Original content of this project", main, StringComparison.Ordinal);
        Assert.Contains("Checked on 15 November 2026 CE", main, StringComparison.Ordinal);
        Assert.DoesNotContain("draft that has not been checked", main, StringComparison.Ordinal);

        // Its subjects and the relations it explains are links to follow; the other format is offered.
        var view = At(2).MediaItem(new MediaId(Diagram))!;
        Assert.All(view.Depicted, entity => Assert.Contains($"{Story}/entity/{entity.Id.Value}", page.Links));
        Assert.Equal(3, view.Relations.Length);
        Assert.Equal(view.Relations.Length, Regex.Matches(page.Html, "<li><span class=\"statement\">").Count);
        Assert.Contains(File(Diagram, "rendition-0"), page.Links);
        Assert.Contains($"{Story}/gallery#{Diagram}", page.Links);
        Assert.Contains($"{Story}/media/{Video}", page.Links);

        Assert.DoesNotContain("<script", page.Own, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch("\\sstyle=\"", page.Html);
    }

    [Fact]
    public async Task The_page_of_the_video_offers_a_player_that_starts_nothing_with_captions_and_the_transcript()
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(2);

        var page = await visitor.Get($"/en/{Story}/media/{Video}");
        var player = Regex.Match(page.Html, "<video.*?</video>", RegexOptions.Singleline).Value;

        Assert.Matches("<video controls preload=\"metadata\" playsinline", player);
        Assert.DoesNotContain("autoplay", player, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"poster=\"{File(Video, "poster")}\"", player, StringComparison.Ordinal);
        Assert.Contains("width=\"1280\" height=\"720\"", player, StringComparison.Ordinal);
        Assert.Contains($"<source src=\"{File(Video, "file")}\" type=\"video/mp4\"", player, StringComparison.Ordinal);
        Assert.Matches($"<track kind=\"captions\" src=\"{File(Video, "captions-en")}\" srclang=\"en\"\\s+label=\"English\" default", player);

        Assert.Contains("Video – Explanatory diagram made today", page.Main, StringComparison.Ordinal);
        Assert.Contains("20 seconds", page.Main, StringComparison.Ordinal);
        Assert.Contains("No sound", page.Main, StringComparison.Ordinal);
        Assert.Matches("<div class=\"transcript\" lang=\"en\" dir=\"ltr\">A connected family in the Shahnameh", page.Html);
        Assert.Contains("Zal, also called Dastan, is Rostam's father.", page.Main, StringComparison.Ordinal);
        Assert.Contains("Arrow direction describes parentage, not chronology", page.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_Persian_page_of_an_item_writes_its_date_as_the_Gregorian_day_it_is_and_marks_English_text()
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(2, "fa");

        var gallery = await visitor.Get($"/fa/{Story}/gallery");
        var page = await visitor.Get($"/fa/{Story}/media/{Video}");

        Assert.Contains("نمایش‌داده‌شده در این‌جا: ۲", gallery.Main, StringComparison.Ordinal);
        Assert.Contains("خانواده‌ای به‌هم‌پیوسته در شاهنامه", page.Main, StringComparison.Ordinal);
        Assert.Contains("۱۵ نوامبر ۲۰۲۶ میلادی", page.Main, StringComparison.Ordinal);
        Assert.Contains("۲۰ ثانیه", page.Main, StringComparison.Ordinal);
        Assert.Contains("بدون صدا", page.Main, StringComparison.Ordinal);

        // The transcript and the editors' notes exist in English only and say so.
        Assert.Matches("<p class=\"note\">\\(به انگلیسی\\)</p>\\s*<div class=\"transcript\" lang=\"en\" dir=\"ltr\">", WebUtility.HtmlDecode(page.Html));
        Assert.Matches("<span class=\"foreign\" lang=\"en\" dir=\"ltr\">Original Dastan concept-stage production", page.Html);

        // Addresses of files are the same in every language.
        Assert.Contains($"<source src=\"{File(Video, "file")}\"", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_page_of_an_item_returns_to_the_gallery_it_was_opened_from_as_far_as_that_gallery_shows_it()
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(2);

        var fromRostam = await visitor.Get($"/en/{Story}/media/{Diagram}?entity=character-rostam");
        var fromRakhsh = await visitor.Get($"/en/{Story}/media/{Diagram}?entity=character-rakhsh");
        var fromNobody = await visitor.Get($"/en/{Story}/media/{Diagram}?entity=character-nobody&event=event-08");

        Assert.Contains($"{Story}/entity/character-rostam/gallery#{Diagram}", fromRostam.Links);
        Assert.Contains("Back to Images and videos of Rostam", fromRostam.Main, StringComparison.Ordinal);
        Assert.Contains($"{Story}/media/{Video}?entity=character-rostam", fromRostam.Links);

        // A gallery that does not show the item, or one the visitor cannot know of, is not where they came from.
        foreach (var page in new[] { fromRakhsh, fromNobody })
        {
            Assert.Equal(HttpStatusCode.OK, page.Status);
            Assert.Contains($"{Story}/gallery#{Diagram}", page.Links);
            Assert.Contains($"{Story}/media/{Video}", page.Links);
            // The page names its own address (the link that skips to the content); no link to another page carries the claim on.
            Assert.DoesNotContain(page.Links, link => link.StartsWith($"{Story}/media/", StringComparison.Ordinal) && link.Contains('?', StringComparison.Ordinal) && !link.EndsWith("#content", StringComparison.Ordinal));
            Assert.DoesNotContain(page.Links, link => link.Contains("rakhsh", StringComparison.Ordinal) && !link.EndsWith("#content", StringComparison.Ordinal) && !link.StartsWith('/'));
        }
    }

    // --- The files ---

    [Fact]
    public async Task At_the_third_stage_the_files_of_the_items_are_delivered_as_what_they_are()
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(2);

        var image = await visitor.Get(File(Diagram, "file"));
        var drawing = await visitor.Get(File(Diagram, "rendition-0"));
        var poster = await visitor.Get(File(Video, "poster"));
        var captions = await visitor.Get(File(Video, "captions-en"));
        var transcript = await visitor.Get(File(Video, "transcript"));

        Assert.Equal(HttpStatusCode.OK, image.Status);
        Assert.Equal("image/png", image.Response.Content.Headers.ContentType!.ToString());
        Assert.Equal(await System.IO.File.ReadAllBytesAsync(OnDisk("dastan-family-connections.png"), TestContext.Current.CancellationToken), await image.Bytes());
        Assert.Equal("image/svg+xml", drawing.Response.Content.Headers.ContentType!.ToString());
        Assert.Equal("image/png", poster.Response.Content.Headers.ContentType!.ToString());
        Assert.Equal("text/vtt; charset=utf-8", captions.Response.Content.Headers.ContentType!.ToString());
        Assert.StartsWith("WEBVTT", captions.Html, StringComparison.Ordinal);
        Assert.Equal("text/plain; charset=utf-8", transcript.Response.Content.Headers.ContentType!.ToString());

        // The answer belongs to this visitor at this reading position: nobody in between keeps it, the browser asks
        // again, and a file is never taken for something else than its type.
        foreach (var file in new[] { image, drawing, poster, captions, transcript })
        {
            Assert.True(file.Response.Headers.CacheControl is { Private: true, NoCache: true, Public: false });
            Assert.Contains("Cookie", file.Response.Headers.Vary);
            Assert.Equal("nosniff", Assert.Single(file.Response.Headers.GetValues("X-Content-Type-Options")));
        }
    }

    [Fact]
    public async Task The_video_is_delivered_in_the_pieces_a_player_asks_for()
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(2);
        var whole = await System.IO.File.ReadAllBytesAsync(OnDisk("dastan-parentage-explainer.mp4"), TestContext.Current.CancellationToken);

        var all = await visitor.Get(File(Video, "file"));
        var piece = await visitor.GetRange(File(Video, "file"), "bytes=100-299");

        Assert.Equal(HttpStatusCode.OK, all.Status);
        Assert.Equal("video/mp4", all.Response.Content.Headers.ContentType!.ToString());
        Assert.Equal("bytes", Assert.Single(all.Response.Headers.AcceptRanges));
        Assert.Equal(whole.Length, (await all.Bytes()).Length);

        Assert.Equal(HttpStatusCode.PartialContent, piece.Status);
        Assert.Equal($"bytes 100-299/{whole.Length}", piece.Response.Content.Headers.ContentRange!.ToString());
        Assert.Equal(whole[100..300], await piece.Bytes());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task Before_the_media_are_reached_none_of_their_files_is_delivered_and_the_answer_says_nothing(int level)
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(level);

        var unknown = await visitor.Get(File("media-nothing", "file"));
        Assert.Equal(HttpStatusCode.NotFound, unknown.Status);
        Assert.Empty(unknown.Html);

        foreach (var address in new[]
        {
            File(Diagram, "file"), File(Diagram, "rendition-0"),
            File(Video, "file"), File(Video, "poster"), File(Video, "captions-en"), File(Video, "transcript"),
        })
        {
            // A hidden file answers exactly like one that does not exist: the status, no page, the same headers.
            var hidden = await visitor.Get(address);
            Assert.Equal(HttpStatusCode.NotFound, hidden.Status);
            Assert.Empty(hidden.Html);
            Assert.Equal(unknown.Response.Headers.CacheControl!.ToString(), hidden.Response.Headers.CacheControl!.ToString());
            Assert.Equal(unknown.Response.Content.Headers.ContentType?.ToString(), hidden.Response.Content.Headers.ContentType?.ToString());
            Assert.Equal(
                unknown.Response.Headers.Select(header => header.Key).Order(StringComparer.Ordinal),
                hidden.Response.Headers.Select(header => header.Key).Order(StringComparer.Ordinal));
        }
    }

    [Theory]
    [InlineData($"/media/{Story}/{Diagram}/rendition-1")]
    [InlineData($"/media/{Story}/{Diagram}/rendition-x")]
    [InlineData($"/media/{Story}/{Diagram}/poster")]
    [InlineData($"/media/{Story}/{Diagram}/dastan-family-connections.png")]
    [InlineData($"/media/{Story}/{Video}/captions-fa")]
    [InlineData($"/media/{Story}/{Video}/provenance")]
    [InlineData($"/media/{Story}/media-met-452419/file")]
    [InlineData($"/media/{Story}/media-met-453265/file")]
    [InlineData($"/media/{Story}/media-umma-1963-1-46/file")]
    [InlineData($"/media/another-story/{Diagram}/file")]
    [InlineData($"/media/{Story}/{Diagram}/..%2F..%2Fsamples%2Fshahnameh-pilot.json")]
    [InlineData("/media/samples/shahnameh-pilot.json")]
    [InlineData("/content/samples/shahnameh-pilot.json")]
    [InlineData("/content/media/dastan-family-connections.png")]
    [InlineData("/samples/shahnameh-pilot.json")]
    [InlineData("/media/dastan-family-connections.png")]
    public async Task At_the_end_of_the_story_nothing_but_the_named_files_of_released_items_is_delivered(string address)
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(4);

        var answer = await visitor.Get(address);

        Assert.Equal(HttpStatusCode.NotFound, answer.Status);
        Assert.DoesNotContain("schemaVersion", answer.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task After_moving_back_the_page_and_the_files_of_an_item_are_gone_again()
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(2);
        Assert.Equal(HttpStatusCode.OK, (await visitor.Get($"/en/{Story}/media/{Diagram}")).Status);
        Assert.Equal(HttpStatusCode.OK, (await visitor.Get(File(Diagram, "file"))).Status);

        await visitor.ReadTo(1);

        Assert.Equal(HttpStatusCode.NotFound, (await visitor.Get($"/en/{Story}/media/{Diagram}")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.Get(File(Diagram, "file"))).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.Get(File(Video, "poster"))).Status);
        Assert.Empty(Cards(await visitor.Get($"/en/{Story}/gallery")));

        // Another visitor who has read on is not affected, and the first one is not helped by them.
        var other = site.Visitor();
        await other.ReadTo(2);
        Assert.Equal(HttpStatusCode.OK, (await other.Get(File(Diagram, "file"))).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.Get(File(Diagram, "file"))).Status);
    }

    [Fact]
    public void A_part_of_an_address_names_one_file_of_the_item_and_nothing_else()
    {
        var video = At(2).Find(new MediaId(Video))!;
        var diagram = At(2).Find(new MediaId(Diagram))!;

        Assert.Equal("media/dastan-parentage-explainer.mp4", MediaFiles.PathOf(video, "file"));
        Assert.Equal("media/dastan-parentage-poster.png", MediaFiles.PathOf(video, "poster"));
        Assert.Equal("media/dastan-parentage-en.vtt", MediaFiles.PathOf(video, "captions-en"));
        Assert.Equal("media/dastan-parentage-transcript-en.txt", MediaFiles.PathOf(video, "transcript"));
        Assert.Equal("media/dastan-family-connections.svg", MediaFiles.PathOf(diagram, "rendition-0"));
        Assert.Null(MediaFiles.PathOf(diagram, "rendition-1"));
        Assert.Null(MediaFiles.PathOf(diagram, "rendition--1"));
        Assert.Null(MediaFiles.PathOf(diagram, "transcript"));
        Assert.Null(MediaFiles.PathOf(video, "captions-"));
        Assert.Null(MediaFiles.PathOf(video, "FILE"));
        Assert.Null(MediaFiles.TypeOf("notes.md"));
        Assert.Null(MediaFiles.TypeOf("sample.json"));
    }
}

/// <summary>The site on a machine where files of the collection went missing after it started.</summary>
public sealed class MissingFilesSite : DemoSite
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-tests-" + Guid.NewGuid().ToString("N"));

    /// <summary>Gets the folder of media of the copy the site reads.</summary>
    public string Media => Path.Combine(_folder, "content", "media");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, configuration) =>
        {
            // A copy of the collection with what its reader checks beside it, so that files can be taken away.
            var app = Path.GetFullPath(Path.Combine(context.HostingEnvironment.ContentRootPath, ".."));
            foreach (var file in Directory.EnumerateFiles(Path.Combine(app, "content"), "*", SearchOption.AllDirectories)
                .Append(Path.Combine(app, "docs", "product", "17-flagship-content-and-source-plan.md")))
            {
                var copy = Path.Combine(_folder, Path.GetRelativePath(app, file));
                Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
                System.IO.File.Copy(file, copy);
            }

            configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Dastan:Content"] = Path.Combine(_folder, "content") });
        });
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

/// <summary>What a visitor gets when a file of an item cannot be delivered.</summary>
public sealed class MissingFilesTests(MissingFilesSite site) : IClassFixture<MissingFilesSite>
{
    private const string Story = "story-rostam-sohrab";

    [Fact]
    public async Task An_item_whose_file_is_missing_keeps_its_page_and_says_that_it_cannot_be_shown()
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(2);
        System.IO.File.Delete(Path.Combine(site.Media, "dastan-family-connections.png"));
        System.IO.File.Delete(Path.Combine(site.Media, "dastan-parentage-explainer.mp4"));
        System.IO.File.Delete(Path.Combine(site.Media, "dastan-parentage-poster.png"));

        var gallery = await visitor.Get($"/en/{Story}/gallery");
        var diagram = await visitor.Get($"/en/{Story}/media/media-dastan-family-illustration");
        var video = await visitor.Get($"/en/{Story}/media/media-dastan-parentage-video");
        var file = await visitor.Get($"/media/{Story}/media-dastan-family-illustration/file");

        // The items are still what the story has; their cards stand without a picture.
        Assert.Equal(2, Regex.Matches(gallery.Html, "<li class=\"card media-card\"").Count);
        Assert.DoesNotContain("<img", gallery.Html, StringComparison.Ordinal);

        foreach (var page in new[] { diagram, video })
        {
            Assert.Equal(HttpStatusCode.OK, page.Status);
            Assert.Contains("This file cannot be shown right now.", page.Main, StringComparison.Ordinal);
            Assert.DoesNotContain("<img", page.Html, StringComparison.Ordinal);
            Assert.DoesNotContain("<video", page.Html, StringComparison.Ordinal);
            Assert.Contains("Original content of this project", page.Main, StringComparison.Ordinal);
            Assert.Contains($"{Story}/entity/character-rostam", page.Links);
        }

        // What is still there is still offered: the transcript of the video.
        Assert.Contains("Zal, also called Dastan, is Rostam's father.", video.Main, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, file.Status);
        Assert.Empty(file.Html);
    }
}
