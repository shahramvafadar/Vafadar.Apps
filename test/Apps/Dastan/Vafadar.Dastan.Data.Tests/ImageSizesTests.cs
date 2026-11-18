using System.Text;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// Reading and writing images (step 17.3, D-056): a picture a camera took turned is kept upright, and a
/// smaller size is made of an image for the pages that show it small.
/// </summary>
public sealed class ImageSizesTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "dastan-images-" + Guid.NewGuid().ToString("N"));

    public ImageSizesTests() => Directory.CreateDirectory(Samples);

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private string Samples => Path.Combine(_root, "content", "samples");

    private string Write(string name, byte[] bytes)
    {
        var path = Path.Combine(_root, name);
        File.WriteAllBytes(path, bytes);
        return path;
    }

    [Fact]
    public void The_pictures_of_the_tests_are_what_they_say()
    {
        Assert.Equal((40, 20, "red green blue white"), RealImages.Look(RealImages.Jpeg(40, 20)));
        Assert.Equal((40, 20, "red green blue white"), RealImages.Look(RealImages.Png(40, 20)));
    }

    [Theory]
    [InlineData(3, 40, 20, "white blue green red")] // upside down
    [InlineData(6, 20, 40, "blue red white green")] // the camera was turned to the right
    [InlineData(8, 20, 40, "green white red blue")] // the camera was turned to the left
    [InlineData(2, 40, 20, "green red white blue")] // a mirror
    public void A_picture_that_says_it_was_taken_turned_is_turned_upright(int orientation, int width, int height, string corners)
    {
        var upright = ImageSizes.Upright(RealImages.Jpeg(40, 20, orientation));

        Assert.Equal((width, height, corners), RealImages.Look(upright));
        Assert.DoesNotContain("Exif", Encoding.Latin1.GetString(upright), StringComparison.Ordinal);
    }

    [Fact]
    public void A_picture_that_is_upright_or_cannot_be_read_stays_as_it_is()
    {
        var (plain, noted, none) = (RealImages.Jpeg(40, 20), RealImages.Jpeg(40, 20, orientation: 1), TestFiles.Jpeg());

        Assert.Same(plain, ImageSizes.Upright(plain));
        Assert.Same(noted, ImageSizes.Upright(noted));
        Assert.Same(none, ImageSizes.Upright(none));
    }

    [Fact]
    public void A_picture_a_camera_took_turned_is_taken_in_upright_and_without_the_note()
    {
        using var content = new MemoryStream(RealImages.Jpeg(1200, 800, orientation: 6));

        var (taken, problem) = MediaIntake.Take(Samples, "media-photo", "file", null, content);

        Assert.Null(problem);
        Assert.Equal((800, 1200), (taken!.Width, taken.Height));
        var kept = File.ReadAllBytes(taken.Path);
        Assert.Equal((800, 1200, "blue red white green"), RealImages.Look(kept));
        Assert.DoesNotContain("Exif", Encoding.Latin1.GetString(kept), StringComparison.Ordinal);
        Assert.Equal(kept.Length, taken.Length);
    }

    [Theory]
    [InlineData(2000, 1000, 800, 800, 400)]
    [InlineData(1000, 2000, 800, 400, 800)]
    [InlineData(801, 3, 800, 800, 3)]
    public void A_smaller_size_is_the_same_picture_with_its_longer_side_as_long_as_asked(int width, int height, int longest, int smallWidth, int smallHeight)
    {
        var file = Write("photo.jpg", RealImages.Jpeg(width, height));

        var small = ImageSizes.Smaller(file, longest, "photo-small.jpg");

        Assert.NotNull(small);
        Assert.Equal((smallWidth, smallHeight), (small.Width, small.Height));
        Assert.Equal(Path.Combine(_root, "photo-small.jpg"), small.Path);
        var written = File.ReadAllBytes(small.Path);
        Assert.Equal(written.Length, small.Length);
        Assert.True(written.Length < new FileInfo(file).Length);
        var (lookedWidth, lookedHeight, corners) = RealImages.Look(written);
        Assert.Equal((smallWidth, smallHeight), (lookedWidth, lookedHeight));
        if (smallHeight > 8)
        {
            Assert.Equal("red green blue white", corners);
        }
    }

    [Fact]
    public void A_png_stays_a_png_and_everything_else_becomes_a_jpeg()
    {
        var png = ImageSizes.Smaller(Write("drawing.png", RealImages.Png(1000, 500)), 400, "drawing-small.png")!;
        var jpeg = ImageSizes.Smaller(Write("photo.jpg", RealImages.Jpeg(1000, 500)), 400, "photo-small.jpg")!;

        Assert.Equal([0x89, (byte)'P', (byte)'N', (byte)'G'], File.ReadAllBytes(png.Path)[..4]);
        Assert.Equal([0xFF, 0xD8], File.ReadAllBytes(jpeg.Path)[..2]);
    }

    [Fact]
    public void The_photographs_that_have_no_smaller_size_yet_get_one_and_nothing_else_is_touched()
    {
        // Brought in before the site made sizes: a large one, one that has its size, a little one, a file that is no picture.
        Write("large-file.jpg", RealImages.Jpeg(1600, 1200));
        Write("done-file.jpg", RealImages.Jpeg(1600, 1200));
        var had = Write("done-file-small.jpg", RealImages.Jpeg(8, 6));
        Write("little-file.png", RealImages.Png(400, 300));
        Write("none-file.jpg", TestFiles.Jpeg());
        Write(".arriving-1", RealImages.Jpeg(1600, 1200));
        var before = File.ReadAllBytes(Path.Combine(_root, "large-file.jpg"));

        Assert.Equal(1, PhotoSizes.MakeMissing(_root));

        Assert.Equal((800, 600, "red green blue white"), RealImages.Look(File.ReadAllBytes(Path.Combine(_root, "large-file-small.jpg"))));
        Assert.Equal(before, File.ReadAllBytes(Path.Combine(_root, "large-file.jpg")));
        Assert.Equal((8, 6), (RealImages.Look(File.ReadAllBytes(had)).Width, RealImages.Look(File.ReadAllBytes(had)).Height));
        Assert.Equal(7, Directory.GetFiles(_root).Length);

        // A second run finds nothing to do: a smaller size gets no smaller size of its own.
        Assert.Equal(0, PhotoSizes.MakeMissing(_root));
        Assert.Equal(7, Directory.GetFiles(_root).Length);
    }

    [Fact]
    public void The_command_makes_the_missing_sizes_of_a_folder_and_says_how_many()
    {
        Write("large-file.jpg", RealImages.Jpeg(1600, 1200));
        using var output = new StringWriter();
        var day = new DateOnly(2026, 11, 18);

        var done = Vafadar.Dastan.Content.ContentCommands.Run(["photos", "sizes", _root], output, day);
        var noFolder = Vafadar.Dastan.Content.ContentCommands.Run(["photos", "sizes", _root + "-none"], output, day);

        Assert.Equal((0, 1), (done, noFolder));
        Assert.Contains("Made 1 smaller size(s).", output.ToString(), StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(_root, "large-file-small.jpg")));
    }

    [Theory]
    [InlineData("photo-file.jpg", "photo-file-small.jpg")]
    [InlineData("0123abcd-file.png", "0123abcd-file-small.png")]
    public void The_name_of_a_smaller_size_is_made_from_the_name_of_its_photograph_and_leads_back_to_it(string name, string small)
    {
        Assert.Equal(small, PhotoSizes.SmallName(name));
        Assert.Equal(name, PhotoSizes.OfSmall(small));
        Assert.Null(PhotoSizes.OfSmall(name));
    }

    [Fact]
    public void An_image_that_is_small_already_or_cannot_be_read_gets_no_smaller_size()
    {
        var small = Write("small.jpg", RealImages.Jpeg(800, 600));
        var none = Write("none.jpg", TestFiles.Jpeg());

        Assert.Null(ImageSizes.Smaller(small, 800, "small-small.jpg"));
        Assert.Null(ImageSizes.Smaller(none, 800, "none-small.jpg"));
        Assert.Null(ImageSizes.Smaller(Path.Combine(_root, "missing.jpg"), 800, "missing-small.jpg"));

        Assert.Equal(["none.jpg", "small.jpg"], Directory.GetFiles(_root).Select(Path.GetFileName).Order());
        Assert.Throws<ArgumentException>(() => ImageSizes.Smaller(small, 400, "../elsewhere.jpg"));
    }
}
