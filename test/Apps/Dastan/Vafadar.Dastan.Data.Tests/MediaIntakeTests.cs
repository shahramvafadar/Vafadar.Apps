using System.Text;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// Taking a file for a media item in (step 13.7): what it is is read from the file, its name is made from its
/// item, an image loses what it says about its making, and what is no part of an item is refused without a trace.
/// </summary>
public sealed class MediaIntakeTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "dastan-intake-" + Guid.NewGuid().ToString("N"));

    public MediaIntakeTests() => Directory.CreateDirectory(Samples);

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private string Samples => Path.Combine(_root, "content", "samples");

    private string Media => Path.Combine(_root, "content", "media");

    private (TakenFile? File, string? Problem) Take(byte[] bytes, string part = "file", string? language = null, string item = "media-test")
    {
        using var content = new MemoryStream(bytes);
        return MediaIntake.Take(Samples, item, part, language, content);
    }

    [Fact]
    public void An_image_is_kept_under_the_name_of_its_item_with_its_size_and_without_what_it_says_about_its_making()
    {
        var (png, _) = Take(TestFiles.Png(640, 480));
        var (jpeg, _) = Take(TestFiles.Jpeg(800, 600), item: "media-photo");
        var (webp, _) = Take(TestFiles.Webp(withExif: false, 320, 200), part: "poster");

        Assert.Equal(("../media/media-test-file.png", MediaFileKind.Image, "image/png", 640, 480), (png!.Url, png.Kind, png.ContentType, png.Width, png.Height));
        Assert.Equal(("../media/media-photo-file.jpg", "image/jpeg", 800, 600), (jpeg!.Url, jpeg.ContentType, jpeg.Width, jpeg.Height));
        Assert.Equal(("../media/media-test-poster.webp", 320, 200), (webp!.Url, webp.Width, webp.Height));

        // The text chunk of the PNG and the EXIF segment of the JPEG are gone; the picture is still there.
        var keptPng = Encoding.Latin1.GetString(File.ReadAllBytes(png.Path));
        var keptJpeg = Encoding.Latin1.GetString(File.ReadAllBytes(jpeg.Path));
        Assert.DoesNotContain("Somebody at a place", keptPng, StringComparison.Ordinal);
        Assert.Contains("IDAT", keptPng, StringComparison.Ordinal);
        Assert.DoesNotContain("GPS", keptJpeg, StringComparison.Ordinal);
        Assert.Equal(png.Length, new FileInfo(png.Path).Length);
        Assert.True(png.Length < TestFiles.Png().Length);
    }

    [Fact]
    public void A_second_file_of_the_same_part_gets_a_name_of_its_own_and_never_replaces_the_first()
    {
        var (first, _) = Take(TestFiles.Png());
        var (second, _) = Take(TestFiles.Png(100, 50));

        Assert.Equal("../media/media-test-file-2.png", second!.Url);
        Assert.True(File.Exists(first!.Path));
        Assert.Equal(100, second.Width);
    }

    [Fact]
    public void A_video_captions_and_a_transcript_are_recognised_by_what_they_are()
    {
        var (video, _) = Take(TestFiles.Mp4());
        var (captions, _) = Take(TestFiles.Vtt(), "captions", "fa");
        var (transcript, _) = Take(Encoding.UTF8.GetBytes("What the video says.\n"), "transcript");

        Assert.Equal(("../media/media-test-file.mp4", MediaFileKind.Video, "video/mp4"), (video!.Url, video.Kind, video.ContentType));
        Assert.Null(video.Width);
        Assert.Equal(("../media/media-test-captions-fa.vtt", MediaFileKind.Captions), (captions!.Url, captions.Kind));
        Assert.Equal(("../media/media-test-transcript.txt", MediaFileKind.Text, "text/plain"), (transcript!.Url, transcript.Kind, transcript.ContentType));
    }

    [Fact]
    public void What_is_no_part_of_an_item_is_refused_and_leaves_nothing_behind()
    {
        var svg = Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>");
        byte[] program = [0x4D, 0x5A, 0x90, 0, 3, 0, 0, 0];

        Assert.Contains("An SVG is not taken", Take(svg).Problem, StringComparison.Ordinal);
        Assert.Contains("none of what an item is made of", Take(program).Problem, StringComparison.Ordinal);
        Assert.Equal("the file is empty.", Take([]).Problem);
        Assert.Equal("the file of an item is an image or a video.", Take(TestFiles.Vtt()).Problem);
        Assert.Equal("the poster of a video is an image.", Take(TestFiles.Mp4(), "poster").Problem);
        Assert.Equal("captions are a WebVTT file; it begins with the line WEBVTT.", Take(Encoding.UTF8.GetBytes("Just a text."), "captions", "en").Problem);
        Assert.Equal("a transcript is a plain text file.", Take(TestFiles.Png(), "transcript").Problem);
        Assert.Equal("captions say their language with a language tag such as en or fa.", Take(TestFiles.Vtt(), "captions").Problem);
        Assert.Contains("carries notes about its making", Take(TestFiles.Webp(withExif: true)).Problem, StringComparison.Ordinal);
        Assert.Contains("cannot be read", Take([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 99]).Problem, StringComparison.Ordinal);

        Assert.False(Directory.Exists(Media) && Directory.EnumerateFileSystemEntries(Media).Any());
    }

    [Theory]
    [InlineData("../climbing", "file")]
    [InlineData("Media Test", "file")]
    [InlineData("media-test", "cover")]
    [InlineData("media-test", "../file")]
    public void A_name_or_a_part_that_is_none_is_refused_before_anything_is_written(string item, string part)
    {
        var (file, problem) = Take(TestFiles.Png(), part, item: item);

        Assert.Null(file);
        Assert.NotNull(problem);
        Assert.False(Directory.Exists(Media));
    }

    [Fact]
    public void A_file_that_was_taken_in_for_a_record_that_is_refused_is_taken_back()
    {
        var (file, _) = Take(TestFiles.Png());

        MediaIntake.Discard(file!);

        Assert.False(File.Exists(file!.Path));
    }

    [Fact]
    public void A_text_that_is_too_long_for_a_text_is_no_text()
    {
        var long1 = new byte[MediaIntake.TextLimit + 1];
        Array.Fill(long1, (byte)'a');

        Assert.NotNull(Take(long1, "transcript").Problem);
        Assert.False(Directory.EnumerateFiles(Media).Any());
    }
}
