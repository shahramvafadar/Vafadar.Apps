using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Vafadar.Dastan.Data.Tests;
using Vafadar.Dastan.Store;

namespace Vafadar.Dastan.Studio.Tests;

public sealed partial class Desk
{
    /// <summary>
    /// Sends the form of that name with a file, as a browser does: every input with what it stands at, the given
    /// inputs filled in, and the file under the name of its input.
    /// </summary>
    public async Task<StudioPage> Upload(string address, string form, string fileName, byte[] file, params (string Name, string? Value)[] changes)
    {
        var page = await Get(address);
        var markup = Forms().Matches(page.Html).Select(match => match.Value).Single(candidate => candidate.Contains($"name=\"_handler\" value=\"{form}\"", StringComparison.Ordinal));
        var action = WebUtility.HtmlDecode(Action().Match(markup).Groups[1].Value);
        var changed = changes.Select(change => change.Name).ToHashSet(StringComparer.Ordinal);
        using var content = new MultipartFormDataContent();
        foreach (var (name, value) in Standing(markup).Where(input => !changed.Contains(input.Name) && input.Name != "file")
            .Concat(changes.Where(change => change.Value is not null).Select(change => (change.Name, change.Value!))))
        {
            content.Add(new StringContent(value, Encoding.UTF8), name);
        }

        var sent = new ByteArrayContent(file);
        sent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(sent, "file", fileName);
        using var response = await _client.PostAsync(new Uri($"/en/{action}", UriKind.Relative), content);
        if (response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.SeeOther && response.Headers.Location is { } back)
        {
            var target = back.IsAbsoluteUri ? back.PathAndQuery : new Uri(new Uri("http://localhost/en/"), back).PathAndQuery;
            return await Get(target) with { Arrived = target };
        }

        return await Page(response);
    }
}

/// <summary>
/// Images and videos brought in through the tool (step 13.7): a new item with its file, and a file for a part
/// of an item that is there. A record that is refused leaves no file behind.
/// </summary>
public sealed class StudioMediaIntakeTests : IDisposable
{
    private const string Collection = Studio.Collection;
    private const string New = $"/en/{Collection}/records/media/new";
    private const string Video = $"/en/{Collection}/record/media/media-dastan-parentage-video";
    private static readonly (string, string?)[] Item =
        [("label", "A picture of the tests"), ("caption", "Test data."), ("creator", "The tests"), ("statement", "Drawn for the tests."), ("Adding.By", "a writer"), ("Adding.Reason", "A new picture.")];

    private readonly Studio _studio = new();
    private readonly Desk _desk;

    public StudioMediaIntakeTests() => _desk = new Desk(_studio);

    public void Dispose() => _studio.Dispose();

    private string Media
    {
        get
        {
            using var store = _studio.Store();
            return Path.GetFullPath(Path.Combine(store.FilesFolder!, "..", "media"));
        }
    }

    [Theory]
    [InlineData("en", "A new image or video")]
    [InlineData("fa", "تصویر یا ویدئوی تازه")]
    public async Task The_form_of_a_new_item_asks_for_its_file_and_is_reached_from_the_list(string language, string title)
    {
        var page = await _desk.Get($"/{language}/{Collection}/records/media/new");
        var list = await _desk.Get($"/{language}/{Collection}/records/media");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains(title, page.Text, StringComparison.Ordinal);
        Assert.Contains("enctype=\"multipart/form-data\"", page.Html, StringComparison.Ordinal);
        Assert.Contains("type=\"file\"", page.Html, StringComparison.Ordinal);
        Assert.Contains($"{Collection}/records/media/new", list.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_new_image_is_kept_with_its_file_and_is_shown_to_nobody_until_an_editor_says_so()
    {
        var page = await _desk.Upload(New, "new", "holiday snapshot.PNG", TestFiles.Png(640, 480), [("id", "media-new"), .. Item]);

        Assert.EndsWith("record/media/media-new?done=added", page.Arrived, StringComparison.Ordinal);
        using var store = _studio.Store();
        var record = store.Record(Collection, "media", "media-new")!.Body;
        Assert.Equal(("image", "../media/media-new-file.png", 640, 480), ((string?)record["type"], (string?)record["url"], (int)record["dimensions"]!["width"]!, (int)record["dimensions"]!["height"]!));
        Assert.Equal(("original_project_content", "Drawn for the tests.", false), ((string?)record["rights"]!["status"], (string?)record["rights"]!["statement"], (bool)record["demoRenderEligibility"]!["allowed"]!));
        Assert.True(File.Exists(Path.Combine(Media, "media-new-file.png")));

        // The name the file had on the machine of the editor is used nowhere.
        Assert.DoesNotContain("holiday", record.ToJsonString(), StringComparison.Ordinal);
        Assert.Equal(("a writer", "A new picture."), (store.History(Collection, "media", "media-new").Single().By, store.History(Collection, "media", "media-new").Single().Reason));
    }

    [Fact]
    public async Task A_large_image_gets_smaller_sizes_that_its_record_names_and_a_small_one_gets_none()
    {
        var page = await _desk.Upload(New, "new", "IMG_0001.jpg", RealImages.Jpeg(2000, 1000), [("id", "media-new"), .. Item]);

        Assert.EndsWith("record/media/media-new?done=added", page.Arrived, StringComparison.Ordinal);
        using (var store = _studio.Store())
        {
            var record = store.Record(Collection, "media", "media-new")!.Body;
            Assert.Equal("""[{"width":480,"url":"../media/media-new-file-480.jpg"},{"width":1200,"url":"../media/media-new-file-1200.jpg"}]""", record["sizes"]!.ToJsonString());
            Assert.Equal((2000, 1000), ((int)record["dimensions"]!["width"]!, (int)record["dimensions"]!["height"]!));
        }

        Assert.True(File.Exists(Path.Combine(Media, "media-new-file-480.jpg")));
        Assert.Equal((1200, 600, "red green blue white"), RealImages.Look(File.ReadAllBytes(Path.Combine(Media, "media-new-file-1200.jpg"))));

        // Another file in its place: a small one, which has no smaller sizes – the record names none of the old.
        var changed = await _desk.Upload($"/en/{Collection}/record/media/media-new", "upload", "little.jpg", RealImages.Jpeg(400, 200), [("part", "file"), ("Uploading.By", "a writer"), ("Uploading.Reason", "A smaller picture.")]);

        Assert.Contains("done=changed", changed.Arrived, StringComparison.Ordinal);
        using var after = _studio.Store();
        var now = after.Record(Collection, "media", "media-new")!.Body;
        Assert.False(now.ContainsKey("sizes"));
        Assert.Equal(400, (int)now["dimensions"]!["width"]!);
    }

    [Fact]
    public async Task A_file_that_is_no_image_and_an_item_the_collection_cannot_take_leave_no_record_and_no_file()
    {
        var before = Directory.GetFiles(Media).Length;
        var svg = Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>");

        var script = await _desk.Upload(New, "new", "picture.svg", svg, [("id", "media-new"), .. Item]);
        var taken = await _desk.Upload(New, "new", "picture.png", TestFiles.Png(), [("id", "character-rostam"), .. Item]);
        var nameless = await _desk.Upload(New, "new", "picture.png", TestFiles.Png(), [("id", "media-new"), ("label", " "), .. Item.Skip(1)]);
        var rightless = await _desk.Upload(New, "new", "picture.png", TestFiles.Png(), [("id", "media-new"), ("statement", " "), .. Item.Where(input => input.Item1 != "statement")]);

        Assert.Contains("An SVG is not taken", script.Text, StringComparison.Ordinal);
        Assert.Contains("character-rostam", taken.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("?done=", nameless.Arrived ?? string.Empty, StringComparison.Ordinal);
        Assert.Equal("Test data.", Desk.Standing(nameless.Html).First(input => input.Name == "caption").Value);
        Assert.Contains("Say why the file may be shown", rightless.Text, StringComparison.Ordinal);
        Assert.Equal(before, Directory.GetFiles(Media).Length);
        using var store = _studio.Store();
        Assert.Null(store.Record(Collection, "media", "media-new"));
    }

    [Fact]
    public async Task Captions_in_a_language_and_a_poster_are_brought_in_for_a_video_that_is_there()
    {
        (string, string?)[] who = [("Uploading.By", "a writer"), ("Uploading.Reason", "Persian captions.")];

        var captions = await _desk.Upload(Video, "upload", "fa.vtt", TestFiles.Vtt(), [("part", "captions"), ("language", "fa"), .. who]);
        var poster = await _desk.Upload(Video, "upload", "poster.jpg", TestFiles.Jpeg(1280, 720), [("part", "poster"), .. who]);

        Assert.EndsWith("?done=changed", captions.Arrived, StringComparison.Ordinal);
        Assert.EndsWith("?done=changed", poster.Arrived, StringComparison.Ordinal);
        using var store = _studio.Store();
        var record = store.Record(Collection, "media", "media-dastan-parentage-video")!.Body;
        Assert.Equal(["en", "fa"], record["captions"]!.AsArray().Select(track => (string?)track!["language"]));
        Assert.Equal("../media/media-dastan-parentage-video-captions-fa.vtt", (string?)record["captions"]![1]!["url"]);
        Assert.Equal("../media/media-dastan-parentage-video-poster.jpg", (string?)record["posterUrl"]);

        // The poster the video had stays on the machine: the history of the item still names it.
        Assert.True(File.Exists(Path.Combine(Media, "dastan-parentage-poster.png")));
    }

    [Fact]
    public async Task A_file_of_another_kind_than_its_part_or_its_item_is_refused_and_not_kept()
    {
        var before = Directory.GetFiles(Media).Length;
        (string, string?)[] who = [("Uploading.By", "a writer"), ("Uploading.Reason", "A try.")];

        var image = await _desk.Upload(Video, "upload", "picture.png", TestFiles.Png(), [("part", "file"), .. who]);
        var text = await _desk.Upload(Video, "upload", "notes.txt", Encoding.UTF8.GetBytes("Notes."), [("part", "poster"), .. who]);
        var noLanguage = await _desk.Upload(Video, "upload", "fa.vtt", TestFiles.Vtt(), [("part", "captions"), .. who]);

        Assert.Contains("The file is of another kind than the item", image.Text, StringComparison.Ordinal);
        Assert.Contains("the poster of a video is an image.", text.Text, StringComparison.Ordinal);
        Assert.Contains("captions say their language", noLanguage.Text, StringComparison.Ordinal);
        Assert.Equal(before, Directory.GetFiles(Media).Length);
    }

    [Fact]
    public async Task Only_the_page_of_an_image_or_a_video_brings_files_in()
    {
        var entity = await _desk.Get($"/en/{Collection}/record/entities/character-rostam");
        var video = await _desk.Get(Video);

        Assert.DoesNotContain("value=\"upload\"", entity.Html, StringComparison.Ordinal);
        Assert.Contains("name=\"_handler\" value=\"upload\"", video.Html, StringComparison.Ordinal);
    }
}
