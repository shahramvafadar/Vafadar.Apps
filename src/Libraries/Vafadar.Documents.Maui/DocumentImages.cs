namespace Vafadar.Documents.Maui;

/// <summary>Bounds image decoding and writes upright, metadata-free JPEGs for recognition or storage.</summary>
public static class DocumentImages
{
    /// <summary>The recognition edge limit; separate from an app's smaller attachment limit.</summary>
    public const int RecognitionEdge = 3200;

    /// <summary>
    /// Re-encodes pixels, respecting all EXIF orientations. Size is read before decoding; camera originals are never
    /// decoded without a pixel bound or returned as a fallback that could retain location metadata.
    /// </summary>
    public static async Task<byte[]> PrepareAsync(byte[] data, int maxEdge, float quality)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxEdge, 1);
#if ANDROID
        return await Task.Run(() => PrepareAndroid(data, maxEdge, quality));
#elif WINDOWS
        return await PrepareWindowsAsync(data, maxEdge, quality);
#elif IOS || MACCATALYST
        return await Task.Run(() => PrepareApple(data, maxEdge, quality));
#else
        await Task.CompletedTask;
        throw new InvalidDataException("Image preparation is unavailable.");
#endif
    }

#if ANDROID
    private static byte[] PrepareAndroid(byte[] data, int maxEdge, float quality)
    {
        using var bounds = new Android.Graphics.BitmapFactory.Options { InJustDecodeBounds = true };
        Android.Graphics.BitmapFactory.DecodeByteArray(data, 0, data.Length, bounds);
        if (bounds.OutWidth <= 0 || bounds.OutHeight <= 0)
        {
            throw new InvalidDataException("The image has no pixels.");
        }

        var sample = 1;
        while (Math.Max(bounds.OutWidth, bounds.OutHeight) / sample > maxEdge * 2
            || (long)bounds.OutWidth * bounds.OutHeight / sample / sample > 12_000_000)
        {
            sample *= 2;
        }

        using var options = new Android.Graphics.BitmapFactory.Options { InSampleSize = sample };
        using var bitmap = Android.Graphics.BitmapFactory.DecodeByteArray(data, 0, data.Length, options)
            ?? throw new InvalidDataException("The image could not be decoded.");
        using var input = new MemoryStream(data);
        using var exif = new Android.Media.ExifInterface(input);
        var orientation = exif.GetAttributeInt(Android.Media.ExifInterface.TagOrientation, 1);
        var degrees = orientation switch { 3 or 4 => 180, 6 or 7 => 90, 5 or 8 => 270, _ => 0 };
        var scale = Math.Min(1f, maxEdge / (float)Math.Max(bitmap.Width, bitmap.Height));
        using var matrix = new Android.Graphics.Matrix();
        matrix.PostScale(orientation is 2 or 4 or 5 or 7 ? -scale : scale, scale);
        matrix.PostRotate(degrees);
        using var upright = Android.Graphics.Bitmap.CreateBitmap(bitmap, 0, 0, bitmap.Width, bitmap.Height, matrix, true);
        try
        {
            using var output = new MemoryStream();
            if (!upright.Compress(Android.Graphics.Bitmap.CompressFormat.Jpeg!, (int)(quality * 100), output))
            {
                throw new InvalidDataException("The image could not be encoded.");
            }

            return output.ToArray();
        }
        finally
        {
            upright.Recycle();
            bitmap.Recycle();
        }
    }
#endif

#if WINDOWS
    private static async Task<byte[]> PrepareWindowsAsync(byte[] data, int maxEdge, float quality)
    {
        using var input = new global::Windows.Storage.Streams.InMemoryRandomAccessStream();
        using (var writer = new global::Windows.Storage.Streams.DataWriter(input.GetOutputStreamAt(0)))
        {
            writer.WriteBytes(data);
            await writer.StoreAsync();
            await writer.FlushAsync();
            writer.DetachStream();
        }

        input.Seek(0);
        var decoder = await global::Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(input);
        var scale = Math.Min(1d, maxEdge / (double)Math.Max(decoder.PixelWidth, decoder.PixelHeight));
        var transform = new global::Windows.Graphics.Imaging.BitmapTransform
        {
            ScaledWidth = Math.Max(1, (uint)(decoder.PixelWidth * scale)),
            ScaledHeight = Math.Max(1, (uint)(decoder.PixelHeight * scale)),
        };
        using var bitmap = await decoder.GetSoftwareBitmapAsync(
            global::Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,
            global::Windows.Graphics.Imaging.BitmapAlphaMode.Ignore, transform,
            global::Windows.Graphics.Imaging.ExifOrientationMode.RespectExifOrientation,
            global::Windows.Graphics.Imaging.ColorManagementMode.DoNotColorManage);
        using var output = new global::Windows.Storage.Streams.InMemoryRandomAccessStream();
        var properties = new global::Windows.Graphics.Imaging.BitmapPropertySet
        {
            ["ImageQuality"] = new global::Windows.Graphics.Imaging.BitmapTypedValue(quality, global::Windows.Foundation.PropertyType.Single),
        };
        var encoder = await global::Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(
            global::Windows.Graphics.Imaging.BitmapEncoder.JpegEncoderId, output, properties);
        encoder.SetSoftwareBitmap(bitmap);
        await encoder.FlushAsync();
        output.Seek(0);
        using var reader = new global::Windows.Storage.Streams.DataReader(output.GetInputStreamAt(0));
        await reader.LoadAsync((uint)output.Size);
        var bytes = new byte[output.Size];
        reader.ReadBytes(bytes);
        return bytes;
    }
#endif

#if IOS || MACCATALYST
    private static byte[] PrepareApple(byte[] data, int maxEdge, float quality)
    {
        using var input = Foundation.NSData.FromArray(data);
        using var source = ImageIO.CGImageSource.FromData(input)
            ?? throw new InvalidDataException("The image could not be opened.");
        using var thumbnail = source.CreateThumbnail(0, new ImageIO.CGImageThumbnailOptions
        {
            CreateThumbnailFromImageAlways = true, CreateThumbnailWithTransform = true, MaxPixelSize = maxEdge,
        }) ?? throw new InvalidDataException("The image could not be decoded.");
        using var image = UIKit.UIImage.FromImage(thumbnail);
        using var encoded = image.AsJPEG(quality) ?? throw new InvalidDataException("The image could not be encoded.");
        return encoded.ToArray();
    }
#endif
}
