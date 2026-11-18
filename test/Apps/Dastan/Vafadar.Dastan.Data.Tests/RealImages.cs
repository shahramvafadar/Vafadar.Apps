using System.Text;
using SkiaSharp;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// Pictures made for the tests that a program can read: four fields of colour – red at the upper left, green
/// at the upper right, blue at the lower left, white at the lower right – so that a test can tell which way
/// up a picture is.
/// </summary>
public static class RealImages
{
    /// <summary>A JPEG of that size; with an orientation, it carries the note of a camera that was held that way.</summary>
    public static byte[] Jpeg(int width, int height, int? orientation = null)
    {
        var jpeg = Encode(width, height, SKEncodedImageFormat.Jpeg);
        if (orientation is null)
        {
            return jpeg;
        }

        // The note of the camera: an EXIF segment whose one entry is the orientation (tag 0x0112).
        byte[] tiff = [.. "II"u8, 42, 0, 8, 0, 0, 0, 1, 0, 0x12, 0x01, 3, 0, 1, 0, 0, 0, (byte)orientation, 0, 0, 0, 0, 0, 0, 0];
        byte[] exif = [.. Encoding.ASCII.GetBytes("Exif\0\0"), .. tiff];
        return [0xFF, 0xD8, 0xFF, 0xE1, (byte)((exif.Length + 2) >> 8), (byte)(exif.Length + 2), .. exif, .. jpeg.AsSpan(2)];
    }

    /// <summary>A PNG of that size.</summary>
    public static byte[] Png(int width, int height) => Encode(width, height, SKEncodedImageFormat.Png);

    /// <summary>Returns the size of a picture and the colours of its four corners: upper left, upper right, lower left, lower right.</summary>
    public static (int Width, int Height, string Corners) Look(byte[] picture)
    {
        using var read = SKBitmap.Decode(picture) ?? throw new InvalidOperationException("This is no picture.");
        var (right, bottom) = (read.Width - 2, read.Height - 2);
        return (read.Width, read.Height, string.Join(' ', new[] { read.GetPixel(1, 1), read.GetPixel(right, 1), read.GetPixel(1, bottom), read.GetPixel(right, bottom) }.Select(Name)));

        // A JPEG keeps a colour nearly: what is mostly red is red.
        static string Name(SKColor colour) => (colour.Red > 128, colour.Green > 128, colour.Blue > 128) switch
        {
            (true, false, false) => "red",
            (false, true, false) => "green",
            (false, false, true) => "blue",
            (true, true, true) => "white",
            _ => "other",
        };
    }

    private static byte[] Encode(int width, int height, SKEncodedImageFormat format)
    {
        using var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque));
        using (var canvas = new SKCanvas(bitmap))
        {
            var (halfWidth, halfHeight) = (width / 2f, height / 2f);
            using var paint = new SKPaint();
            foreach (var (colour, x, y) in new[] { (SKColors.Red, 0f, 0f), (SKColors.Lime, halfWidth, 0f), (SKColors.Blue, 0f, halfHeight), (SKColors.White, halfWidth, halfHeight) })
            {
                paint.Color = colour;
                canvas.DrawRect(x, y, halfWidth, halfHeight, paint);
            }
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var written = image.Encode(format, 90);
        return written.ToArray();
    }
}
