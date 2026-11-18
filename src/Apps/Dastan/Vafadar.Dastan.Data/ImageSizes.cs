using SkiaSharp;

namespace Vafadar.Dastan.Data;

/// <summary>A smaller size of an image that was made beside it.</summary>
/// <param name="Path">Where it lies.</param>
/// <param name="Width">Its width in pixels.</param>
/// <param name="Height">Its height in pixels.</param>
/// <param name="Length">Its length in bytes.</param>
public sealed record SmallerImage(string Path, int Width, int Height, long Length);

/// <summary>
/// Reading and writing images (D-056, SkiaSharp): a picture that says it was taken with the camera turned is
/// turned upright before what it says about its making is taken out, and a smaller size of an image is made
/// beside it for a page that shows it small. An image that is written here says nothing about its making:
/// only the picture is written.
/// </summary>
public static class ImageSizes
{
    /// <summary>The quality a JPEG is written with: what the eye does not tell from the original at the size of a page.</summary>
    public const int Quality = 85;

    /// <summary>
    /// Returns a JPEG upright when it says that it was taken turned – a camera writes the picture as its sensor
    /// lies and notes how it was held; without the note the picture would lie on its side. A JPEG that is
    /// upright, and what is no picture that can be read, is returned as it is.
    /// </summary>
    public static byte[] Upright(byte[] jpeg)
    {
        ArgumentNullException.ThrowIfNull(jpeg);
        try
        {
            using var codec = SKCodec.Create(new MemoryStream(jpeg));
            if (codec is null || codec.EncodedOrigin is SKEncodedOrigin.TopLeft or SKEncodedOrigin.Default)
            {
                return jpeg;
            }

            using var read = SKBitmap.Decode(codec);
            if (read is null)
            {
                return jpeg;
            }

            var origin = codec.EncodedOrigin;
            var turns = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
            var (width, height) = turns ? (read.Height, read.Width) : (read.Width, read.Height);
            using var upright = new SKBitmap(new SKImageInfo(width, height, read.ColorType, read.AlphaType));
            using (var canvas = new SKCanvas(upright))
            {
                canvas.SetMatrix(origin switch
                {
                    SKEncodedOrigin.TopRight => SKMatrix.CreateScale(-1, 1).PostConcat(SKMatrix.CreateTranslation(width, 0)),
                    SKEncodedOrigin.BottomRight => SKMatrix.CreateRotationDegrees(180).PostConcat(SKMatrix.CreateTranslation(width, height)),
                    SKEncodedOrigin.BottomLeft => SKMatrix.CreateScale(1, -1).PostConcat(SKMatrix.CreateTranslation(0, height)),
                    SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
                    SKEncodedOrigin.RightTop => SKMatrix.CreateRotationDegrees(90).PostConcat(SKMatrix.CreateTranslation(width, 0)),
                    SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, width, -1, 0, height, 0, 0, 1),
                    _ => SKMatrix.CreateRotationDegrees(270).PostConcat(SKMatrix.CreateTranslation(0, height)),
                });
                // A turn by a quarter or a mirror moves every point onto a point: nothing is mixed.
                using var source = SKImage.FromBitmap(read);
                canvas.DrawImage(source, 0, 0, new SKSamplingOptions(SKFilterMode.Nearest));
            }

            using var image = SKImage.FromBitmap(upright);
            using var written = image.Encode(SKEncodedImageFormat.Jpeg, 92);
            return written is null ? jpeg : written.ToArray();
        }
        catch (Exception unreadable) when (unreadable is ArgumentException or InvalidOperationException or IOException)
        {
            return jpeg;
        }
    }

    /// <summary>
    /// Makes a smaller size of an image beside it: the same picture, its longer side no longer than asked for.
    /// A PNG stays a PNG – it may be see-through –, a WebP a WebP, everything else becomes a JPEG. Returns
    /// nothing, and writes nothing, when the image is that small already or cannot be read.
    /// </summary>
    /// <param name="file">The image.</param>
    /// <param name="longest">The longer side of the smaller size, in pixels.</param>
    /// <param name="name">The name of the smaller size, without a folder; it lies beside the image.</param>
    public static SmallerImage? Smaller(string file, int longest, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(file);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(longest, 16);
        if (name != Path.GetFileName(name))
        {
            throw new ArgumentException("The name of a smaller size is a plain file name.", nameof(name));
        }

        try
        {
            using var read = SKBitmap.Decode(file);
            if (read is null || Math.Max(read.Width, read.Height) <= longest)
            {
                return null;
            }

            var scale = (double)longest / Math.Max(read.Width, read.Height);
            var (width, height) = (Math.Max(1, (int)Math.Round(read.Width * scale)), Math.Max(1, (int)Math.Round(read.Height * scale)));
            using var small = read.Resize(new SKImageInfo(width, height, read.ColorType, read.AlphaType), new SKSamplingOptions(SKCubicResampler.Mitchell));
            if (small is null)
            {
                return null;
            }

            var format = Path.GetExtension(name).ToUpperInvariant() switch
            {
                ".PNG" => SKEncodedImageFormat.Png,
                ".WEBP" => SKEncodedImageFormat.Webp,
                _ => SKEncodedImageFormat.Jpeg,
            };
            using var image = SKImage.FromBitmap(small);
            using var written = image.Encode(format, Quality);
            if (written is null)
            {
                return null;
            }

            var path = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(file))!, name);
            using (var target = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                written.SaveTo(target);
            }

            return new(path, width, height, new FileInfo(path).Length);
        }
        catch (Exception unreadable) when (unreadable is ArgumentException or InvalidOperationException or IOException)
        {
            return null;
        }
    }
}
