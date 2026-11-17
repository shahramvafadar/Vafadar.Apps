using System.Text;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>Files made for the tests: the smallest ones of each kind that say what they are.</summary>
public static class TestFiles
{
    /// <summary>A PNG of that size with a text chunk that says who made it.</summary>
    public static byte[] Png(int width = 640, int height = 480)
    {
        using var file = new MemoryStream();
        file.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        Chunk(file, "IHDR", [.. Big(width), .. Big(height), 8, 2, 0, 0, 0]);
        Chunk(file, "tEXt", Encoding.ASCII.GetBytes("Author\0Somebody at a place"));
        Chunk(file, "IDAT", [1, 2, 3]);
        Chunk(file, "IEND", []);
        return file.ToArray();

        static void Chunk(Stream stream, string type, byte[] data)
        {
            stream.Write(Big(data.Length));
            stream.Write(Encoding.ASCII.GetBytes(type));
            stream.Write(data);
            stream.Write([0, 0, 0, 0]);
        }

        static byte[] Big(int value) => [(byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value];
    }

    /// <summary>A JPEG of that size with an EXIF segment that says where it was taken.</summary>
    public static byte[] Jpeg(int width = 800, int height = 600)
    {
        var exif = Encoding.ASCII.GetBytes("Exif\0\0GPS 35.7N 51.4E");
        return
        [
            0xFF, 0xD8,
            0xFF, 0xE1, (byte)((exif.Length + 2) >> 8), (byte)(exif.Length + 2), .. exif,
            0xFF, 0xC0, 0, 11, 8, (byte)(height >> 8), (byte)height, (byte)(width >> 8), (byte)width, 1, 1, 0x11, 0,
            0xFF, 0xDA, 0, 2, 1, 2, 3,
            0xFF, 0xD9,
        ];
    }

    /// <summary>A WebP of that size, with or without an EXIF chunk.</summary>
    public static byte[] Webp(bool withExif, int width = 320, int height = 200)
    {
        byte[] canvas = [0, 0, 0, 0, (byte)(width - 1), (byte)((width - 1) >> 8), 0, (byte)(height - 1), (byte)((height - 1) >> 8), 0];
        byte[] body = [.. "WEBP"u8, .. "VP8X"u8, 10, 0, 0, 0, .. canvas, .. withExif ? [.. "EXIF"u8, 4, 0, 0, 0, 1, 2, 3, 4] : Array.Empty<byte>()];
        return [.. "RIFF"u8, (byte)body.Length, 0, 0, 0, .. body];
    }

    /// <summary>What an MP4 begins with.</summary>
    public static byte[] Mp4() => [0, 0, 0, 24, .. "ftypisom"u8, 0, 0, 2, 0, .. "isomiso2"u8, 1, 2, 3];

    /// <summary>Captions.</summary>
    public static byte[] Vtt() => Encoding.UTF8.GetBytes("WEBVTT\n\n00:00.000 --> 00:02.000\nزیرنویس آزمون\n");
}
