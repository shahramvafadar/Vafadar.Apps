using System.Buffers.Binary;
using System.Text;
using System.Text.RegularExpressions;

namespace Vafadar.Dastan.Data;

/// <summary>What a file that was taken in is.</summary>
public enum MediaFileKind
{
    /// <summary>A still image: PNG, JPEG or WebP.</summary>
    Image = 0,

    /// <summary>A video: MP4 or WebM.</summary>
    Video = 1,

    /// <summary>Captions of a video: WebVTT.</summary>
    Captions = 2,

    /// <summary>A plain text: the transcript of a video.</summary>
    Text = 3,
}

/// <summary>A file that was taken in, as it now lies beside the collection.</summary>
/// <param name="Url">Its path as a record names it: relative to the folder of the samples, with forward slashes.</param>
/// <param name="Kind">What it is.</param>
/// <param name="ContentType">The type it is delivered as.</param>
/// <param name="Width">The width of an image, in pixels.</param>
/// <param name="Height">The height of an image, in pixels.</param>
/// <param name="Length">Its length in bytes, as it was kept.</param>
/// <param name="Path">Where it lies on the machine.</param>
public sealed record TakenFile(string Url, MediaFileKind Kind, string ContentType, int? Width, int? Height, long Length, string Path)
{
    /// <summary>Gets the smaller sizes that were made of an image beside it, the narrowest first; none when none were asked for or the image is small.</summary>
    public IReadOnlyList<SmallerImage> Sizes { get; init; } = [];

    /// <summary>Returns the address of a smaller size as a record names it: beside the file.</summary>
    public static string UrlOf(SmallerImage size)
    {
        ArgumentNullException.ThrowIfNull(size);
        return $"../media/{System.IO.Path.GetFileName(size.Path)}";
    }
}

/// <summary>
/// Takes a file for a media item in (step 13.7): what the file is is read from its content, never from its name
/// or from what the sender says; its name on the machine is made here, from the item it belongs to; and what an
/// image says about where and with which device it was made is taken out before it is kept. Nothing is converted:
/// a file is kept as the editor made it.
/// </summary>
public static partial class MediaIntake
{
    /// <summary>The largest image, in bytes.</summary>
    public const long ImageLimit = 25L * 1024 * 1024;

    /// <summary>The largest video, in bytes.</summary>
    public const long VideoLimit = 250L * 1024 * 1024;

    /// <summary>The largest text or captions file, in bytes.</summary>
    public const long TextLimit = 2L * 1024 * 1024;

    /// <summary>The parts of a media item a file can be: its file, the poster of a video, captions, a transcript, a further rendition.</summary>
    public static IReadOnlyList<string> Parts { get; } = ["file", "poster", "captions", "transcript", "rendition"];

    private const int Head = 64 * 1024;

    /// <summary>
    /// Takes a file in and keeps it in the folder <c>media</c> beside the folder of the samples. Returns the file, or
    /// why it was not taken; a file that is not taken leaves nothing behind.
    /// </summary>
    /// <param name="samplesFolder">The folder of the samples of the collection.</param>
    /// <param name="item">The identifier of the media item the file belongs to.</param>
    /// <param name="part">What part of the item the file is; one of <see cref="Parts"/>.</param>
    /// <param name="language">For captions: their language, as a language tag.</param>
    /// <param name="content">The file.</param>
    /// <param name="sizes">For the file of an image: the longer sides, in pixels, of the smaller sizes to make beside it (D-056). None: none are made.</param>
    public static (TakenFile? File, string? Problem) Take(string samplesFolder, string item, string part, string? language, Stream content, IReadOnlyList<int>? sizes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(samplesFolder);
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(part);
        ArgumentNullException.ThrowIfNull(content);
        if (!Name().IsMatch(item))
        {
            return (null, $"{item}: a file is kept under the identifier of its item, which is lower-case words with hyphens.");
        }

        if (!Parts.Contains(part))
        {
            return (null, $"{part}: a file is the {string.Join(", the ", Parts)} of an item.");
        }

        if (part == "captions" && (language is null || !Language().IsMatch(language)))
        {
            return (null, "captions say their language with a language tag such as en or fa.");
        }

        var folder = Path.GetFullPath(Path.Combine(samplesFolder, "..", "media"));
        Directory.CreateDirectory(folder);
        var arriving = Path.Combine(folder, $".arriving-{Guid.NewGuid():N}");
        try
        {
            // The file is written where it will lie, under a name nothing refers to, and no further than the
            // largest file of any kind is long.
            long length = 0;
            var head = new byte[Head];
            var headLength = 0;
            using (var target = new FileStream(arriving, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var buffer = new byte[81920];
                int read;
                while ((read = content.Read(buffer, 0, buffer.Length)) > 0)
                {
                    if (headLength < Head)
                    {
                        var kept = Math.Min(read, Head - headLength);
                        buffer.AsSpan(0, kept).CopyTo(head.AsSpan(headLength));
                        headLength += kept;
                    }

                    length += read;
                    if (length > VideoLimit)
                    {
                        return (null, $"the file is longer than {VideoLimit / (1024 * 1024)} MB.");
                    }

                    target.Write(buffer, 0, read);
                }
            }

            if (length == 0)
            {
                return (null, "the file is empty.");
            }

            if (Recognise(head.AsSpan(0, headLength), length <= TextLimit ? arriving : null) is not var (kind, extension, type))
            {
                return (null, "the file is none of what an item is made of: a PNG, JPEG or WebP image, an MP4 or WebM video, WebVTT captions or a plain text. An SVG is not taken: it can carry a script.");
            }

            var allowed = part switch
            {
                "file" or "rendition" => kind is MediaFileKind.Image or MediaFileKind.Video,
                "poster" => kind == MediaFileKind.Image,
                "captions" => kind == MediaFileKind.Captions,
                _ => kind is MediaFileKind.Text,
            };
            if (!allowed)
            {
                return (null, part switch
                {
                    "poster" => "the poster of a video is an image.",
                    "captions" => "captions are a WebVTT file; it begins with the line WEBVTT.",
                    "transcript" => "a transcript is a plain text file.",
                    _ => "the file of an item is an image or a video.",
                });
            }

            var limit = kind switch { MediaFileKind.Image => ImageLimit, MediaFileKind.Video => VideoLimit, _ => TextLimit };
            if (length > limit)
            {
                return (null, $"the file is longer than {limit / (1024 * 1024)} MB, the most for {(kind == MediaFileKind.Image ? "an image" : kind == MediaFileKind.Video ? "a video" : "a text")}.");
            }

            (int Width, int Height)? size = null;
            if (kind == MediaFileKind.Image)
            {
                // An image is small enough to be read whole: what it says about its making is taken out.
                var arrived = File.ReadAllBytes(arriving);

                // A camera notes how it was held beside the picture. The note goes with everything else a
                // picture says about its making, so the picture is turned upright first (D-056).
                var bytes = extension == ".jpg" ? ImageSizes.Upright(arrived) : arrived;
                var (cleaned, problem) = WithoutMetadata(bytes, extension);
                if (problem is not null)
                {
                    return (null, problem);
                }

                size = Size(cleaned!, extension);
                if (size is null)
                {
                    return (null, "the image cannot be read: its size is not where an image of its kind says it.");
                }

                if (cleaned!.Length != arrived.Length || !ReferenceEquals(bytes, arrived))
                {
                    File.WriteAllBytes(arriving, cleaned);
                    length = cleaned.Length;
                }
            }

            // The name is made here: the item, the part and, where there are several, a number.
            var stem = part == "captions" ? $"{item}-captions-{language}" : $"{item}-{part}";
            var name = $"{stem}{extension}";
            for (var number = 2; File.Exists(Path.Combine(folder, name)); number++)
            {
                name = $"{stem}-{number}{extension}";
            }

            var kept2 = Path.Combine(folder, name);
            File.Move(arriving, kept2);

            // Smaller sizes of the picture lie beside it, under its name and their width.
            var smaller = new List<SmallerImage>();
            if (kind == MediaFileKind.Image && part == "file")
            {
                foreach (var longest in (sizes ?? []).Distinct().Order())
                {
                    if (ImageSizes.Smaller(kept2, longest, $"{Path.GetFileNameWithoutExtension(name)}-{longest.ToString(System.Globalization.CultureInfo.InvariantCulture)}{extension}") is { } made)
                    {
                        smaller.Add(made);
                    }
                }
            }

            return (new TakenFile($"../media/{name}", kind, type, size?.Width, size?.Height, length, kept2) { Sizes = smaller }, null);
        }
        finally
        {
            if (File.Exists(arriving))
            {
                File.Delete(arriving);
            }
        }
    }

    /// <summary>Takes a file back that was taken in and is not kept after all – the record that named it was refused.</summary>
    public static void Discard(TakenFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        foreach (var path in file.Sizes.Select(size => size.Path).Prepend(file.Path))
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    // What a file is, by how it begins. A text is recognised by being text: valid UTF-8 without a NUL.
    private static (MediaFileKind Kind, string Extension, string ContentType)? Recognise(ReadOnlySpan<byte> head, string? smallFile)
    {
        if (head.StartsWith<byte>([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]))
        {
            return (MediaFileKind.Image, ".png", "image/png");
        }

        if (head.StartsWith<byte>([0xFF, 0xD8, 0xFF]))
        {
            return (MediaFileKind.Image, ".jpg", "image/jpeg");
        }

        if (head.Length >= 12 && head[..4].SequenceEqual("RIFF"u8) && head[8..12].SequenceEqual("WEBP"u8))
        {
            return (MediaFileKind.Image, ".webp", "image/webp");
        }

        if (head.Length >= 12 && head[4..8].SequenceEqual("ftyp"u8))
        {
            return (MediaFileKind.Video, ".mp4", "video/mp4");
        }

        if (head.StartsWith<byte>([0x1A, 0x45, 0xDF, 0xA3]))
        {
            return (MediaFileKind.Video, ".webm", "video/webm");
        }

        if (smallFile is null)
        {
            return null;
        }

        string text;
        try
        {
            text = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(File.ReadAllBytes(smallFile));
        }
        catch (DecoderFallbackException)
        {
            return null;
        }

        if (text.Contains('\0', StringComparison.Ordinal) || text.TrimStart('﻿').TrimStart().StartsWith('<'))
        {
            // Markup – an SVG, a page – is not a text of an item.
            return null;
        }

        return text.TrimStart('﻿').StartsWith("WEBVTT", StringComparison.Ordinal)
            ? (MediaFileKind.Captions, ".vtt", "text/vtt")
            : (MediaFileKind.Text, ".txt", "text/plain");
    }

    // An image without what it says about its making: where, when, with which device, by whom.
    private static (byte[]? Image, string? Problem) WithoutMetadata(byte[] image, string extension)
    {
        try
        {
            return extension switch
            {
                ".jpg" => (JpegWithoutMetadata(image), null),
                ".png" => (PngWithoutMetadata(image), null),
                _ => WebpHasMetadata(image)
                    ? (null, "this WebP image carries notes about its making (EXIF or XMP), which can say where a picture was taken; save it without them, or as PNG or JPEG.")
                    : (image, null),
            };
        }
        catch (Exception refused) when (refused is ArgumentOutOfRangeException or IndexOutOfRangeException or InvalidDataException)
        {
            return (null, "the image cannot be read: it ends before it says it does.");
        }
    }

    // A JPEG is a row of segments; APP1 holds EXIF and XMP, APP13 the notes of an agency. They are left out.
    private static byte[] JpegWithoutMetadata(byte[] image)
    {
        using var cleaned = new MemoryStream(image.Length);
        cleaned.Write(image, 0, 2);
        var at = 2;
        while (at + 4 <= image.Length && image[at] == 0xFF)
        {
            var marker = image[at + 1];
            if (marker == 0xDA)
            {
                // The picture itself begins: everything from here on is kept as it is.
                break;
            }

            var length = BinaryPrimitives.ReadUInt16BigEndian(image.AsSpan(at + 2));
            if (length < 2 || at + 2 + length > image.Length)
            {
                throw new InvalidDataException();
            }

            if (marker is not (0xE1 or 0xED))
            {
                cleaned.Write(image, at, 2 + length);
            }

            at += 2 + length;
        }

        cleaned.Write(image, at, image.Length - at);
        return cleaned.ToArray();
    }

    // A PNG is a row of chunks; the texts, the time and the EXIF chunk are left out.
    private static byte[] PngWithoutMetadata(byte[] image)
    {
        using var cleaned = new MemoryStream(image.Length);
        cleaned.Write(image, 0, 8);
        var at = 8;
        while (at + 12 <= image.Length)
        {
            var length = BinaryPrimitives.ReadUInt32BigEndian(image.AsSpan(at));
            if (length > int.MaxValue || at + 12 + (long)length > image.Length)
            {
                throw new InvalidDataException();
            }

            var type = Encoding.ASCII.GetString(image, at + 4, 4);
            if (type is not ("tEXt" or "iTXt" or "zTXt" or "eXIf" or "tIME"))
            {
                cleaned.Write(image, at, 12 + (int)length);
            }

            at += 12 + (int)length;
        }

        return cleaned.ToArray();
    }

    private static bool WebpHasMetadata(byte[] image)
    {
        for (var at = 12; at + 8 <= image.Length;)
        {
            var type = Encoding.ASCII.GetString(image, at, 4);
            if (type is "EXIF" or "XMP ")
            {
                return true;
            }

            var length = BinaryPrimitives.ReadUInt32LittleEndian(image.AsSpan(at + 4));
            at += 8 + (int)Math.Min(length + (length & 1), int.MaxValue - at - 8);
        }

        return false;
    }

    // The width and the height of an image, from where its kind says them.
    private static (int Width, int Height)? Size(byte[] image, string extension)
    {
        try
        {
            switch (extension)
            {
                case ".png":
                    return image.Length >= 24 && Encoding.ASCII.GetString(image, 12, 4) == "IHDR"
                        ? Positive(BinaryPrimitives.ReadInt32BigEndian(image.AsSpan(16)), BinaryPrimitives.ReadInt32BigEndian(image.AsSpan(20)))
                        : null;

                case ".jpg":
                    for (var at = 2; at + 9 <= image.Length && image[at] == 0xFF;)
                    {
                        var marker = image[at + 1];
                        var length = BinaryPrimitives.ReadUInt16BigEndian(image.AsSpan(at + 2));
                        if (marker is >= 0xC0 and <= 0xCF and not (0xC4 or 0xC8 or 0xCC))
                        {
                            return Positive(BinaryPrimitives.ReadUInt16BigEndian(image.AsSpan(at + 7)), BinaryPrimitives.ReadUInt16BigEndian(image.AsSpan(at + 5)));
                        }

                        if (marker == 0xDA || length < 2)
                        {
                            return null;
                        }

                        at += 2 + length;
                    }

                    return null;

                default:
                    if (image.Length < 30)
                    {
                        return null;
                    }

                    return Encoding.ASCII.GetString(image, 12, 4) switch
                    {
                        "VP8X" => Positive(1 + (image[24] | (image[25] << 8) | (image[26] << 16)), 1 + (image[27] | (image[28] << 8) | (image[29] << 16))),
                        "VP8 " => Positive(BinaryPrimitives.ReadUInt16LittleEndian(image.AsSpan(26)) & 0x3FFF, BinaryPrimitives.ReadUInt16LittleEndian(image.AsSpan(28)) & 0x3FFF),
                        "VP8L" => Positive(1 + (int)(BinaryPrimitives.ReadUInt32LittleEndian(image.AsSpan(21)) & 0x3FFF), 1 + (int)((BinaryPrimitives.ReadUInt32LittleEndian(image.AsSpan(21)) >> 14) & 0x3FFF)),
                        _ => null,
                    };
            }
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private static (int Width, int Height)? Positive(int width, int height) => width > 0 && height > 0 ? (width, height) : null;

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex Name();

    [GeneratedRegex("^[a-z]{2,3}(-[A-Za-z0-9]{2,8})*$", RegexOptions.CultureInvariant)]
    private static partial Regex Language();
}
