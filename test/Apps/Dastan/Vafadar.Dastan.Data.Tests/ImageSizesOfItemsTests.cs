using System.Text.Json.Nodes;
using Vafadar.Dastan.Core;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// The smaller sizes of the image of a media item (step 17.8, D-056): made beside the file when it is brought
/// in, named by the record, and read as sizes – the same picture, narrower – not as other formats.
/// </summary>
public sealed class ImageSizesOfItemsTests : IDisposable
{
    private const string Diagram = "media-dastan-family-illustration";
    private const string Small = "../media/dastan-family-connections.png";
    private readonly string _root = Path.Combine(Path.GetTempPath(), "dastan-sizes-" + Guid.NewGuid().ToString("N"));

    public ImageSizesOfItemsTests() => Directory.CreateDirectory(Samples);

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private string Samples => Path.Combine(_root, "content", "samples");

    private string Media => Path.Combine(_root, "content", "media");

    private (TakenFile? File, string? Problem) Take(byte[] bytes, string part = "file", params int[] sizes)
    {
        using var content = new MemoryStream(bytes);
        return MediaIntake.Take(Samples, "media-test", part, null, content, sizes);
    }

    // The diagram of the sample, said to be so wide, with the sizes a test gives it.
    private static Catalogue With(string type, int width, string sizes) => SampleFiles.Read(sample =>
    {
        var item = sample["media"]!.AsArray().Select(node => node!.AsObject()).First(candidate => type == "video" ? (string?)candidate["type"] == type : (string?)candidate["id"] == Diagram);
        item["dimensions"] = new JsonObject { ["width"] = width, ["height"] = width / 2 };
        item["sizes"] = JsonNode.Parse(sizes);
    });

    [Fact]
    public void Smaller_sizes_are_made_beside_an_image_that_is_brought_in_and_go_with_it_when_it_is_not_kept()
    {
        var (taken, problem) = Take(RealImages.Jpeg(2000, 1000), "file", 1200, 480);

        Assert.Null(problem);
        Assert.Equal([(480, 240), (1200, 600)], taken!.Sizes.Select(size => (size.Width, size.Height)));
        Assert.Equal(["../media/media-test-file-480.jpg", "../media/media-test-file-1200.jpg"], taken.Sizes.Select(TakenFile.UrlOf));
        Assert.Equal(["media-test-file-1200.jpg", "media-test-file-480.jpg", "media-test-file.jpg"], Directory.GetFiles(Media).Select(Path.GetFileName).Order());
        Assert.Equal((480, 240, "red green blue white"), RealImages.Look(File.ReadAllBytes(taken.Sizes[0].Path)));

        MediaIntake.Discard(taken);

        Assert.Empty(Directory.GetFiles(Media));
    }

    [Fact]
    public void An_image_gets_only_the_sizes_that_are_smaller_than_it_and_only_its_file_gets_any()
    {
        var (between, _) = Take(RealImages.Jpeg(1000, 500), "file", 480, 1200);
        var (little, _) = Take(RealImages.Jpeg(400, 200), "file", 480, 1200);
        var (poster, _) = Take(RealImages.Jpeg(2000, 1000), "poster", 480, 1200);
        var (unasked, _) = Take(RealImages.Jpeg(2000, 1000));

        Assert.Equal([480], between!.Sizes.Select(size => size.Width));
        Assert.Empty(little!.Sizes);
        Assert.Empty(poster!.Sizes);
        Assert.Empty(unasked!.Sizes);
    }

    [Fact]
    public void A_record_names_the_smaller_sizes_of_its_image_and_they_are_read_the_narrowest_first()
    {
        var catalogue = With("image", 2000, $$"""[{ "width": 1200, "url": "{{Small}}" }, { "width": 480, "url": "{{Small}}" }]""");

        var item = catalogue.Find(new MediaId(Diagram))!;

        Assert.Equal([480, 1200], item.Sizes.Select(size => size.Width));
        Assert.All(item.Sizes, size => Assert.Equal("media/dastan-family-connections.png", size.File));

        // A size is no other format: the renditions of the item are what they were.
        Assert.Equal([("image/svg+xml", "media/dastan-family-connections.svg")], item.Renditions);
        Assert.Empty(SampleFiles.Read().Find(new MediaId(Diagram))!.Sizes);
    }

    [Theory]
    [InlineData("image", """[{ "width": 0, "url": "../media/dastan-family-connections.png" }]""", "narrower than the image")]
    [InlineData("image", """[{ "width": 2000, "url": "../media/dastan-family-connections.png" }]""", "narrower than the image")]
    [InlineData("image", """[{ "width": 480, "url": "../media/dastan-family-connections.png" }, { "width": 480, "url": "../media/dastan-family-connections.png" }]""", "two smaller sizes are 480 pixels wide")]
    [InlineData("image", """[{ "width": 480 }]""", "url")]
    [InlineData("video", """[{ "width": 480, "url": "../media/dastan-family-connections.png" }]""", "only an image has smaller sizes")]
    public void Sizes_that_are_none_are_refused(string type, string sizes, string problem)
    {
        var refused = Assert.Throws<SampleFormatException>(() => With(type, 2000, sizes));

        Assert.Contains(refused.Problems, said => said.Contains(problem, StringComparison.Ordinal));
    }
}
