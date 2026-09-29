namespace Vafadar.Documents.Maui;

/// <summary>
/// On-device text recognition of images with the system engines: Vision on iOS, Windows.Media.Ocr on Windows and ML Kit
/// with its bundled Latin model on Android (Android has no system engine). Nothing leaves the device: the model ships
/// in the app, and apps remove the network permissions ML Kit asks for.
/// </summary>
public static class TextRecognizer
{
    /// <summary>Gets a value indicating whether this device can recognise text in images.</summary>
    public static bool IsSupported =>
#if IOS || MACCATALYST
        OperatingSystem.IsIOSVersionAtLeast(13) || OperatingSystem.IsMacCatalystVersionAtLeast(13);
#elif WINDOWS || ANDROID
        true;
#else
        false;
#endif

    /// <summary>Returns the recognised words of an encoded image (JPEG, PNG …); empty when nothing could be read.</summary>
    public static async Task<IReadOnlyList<LayoutWord>> RecognizeAsync(byte[] image)
    {
        ArgumentNullException.ThrowIfNull(image);
#if IOS || MACCATALYST
        return await Task.Run(() => RecognizeApple(image));
#elif WINDOWS
        return await RecognizeWindowsAsync(image);
#elif ANDROID
        return await RecognizeAndroidAsync(image);
#else
        await Task.CompletedTask;
        return [];
#endif
    }

#if IOS || MACCATALYST
    private static List<LayoutWord> RecognizeApple(byte[] image)
    {
        var words = new List<LayoutWord>();
        using var request = new Vision.VNRecognizeTextRequest((request, error) =>
        {
            foreach (var observation in request.GetResults<Vision.VNRecognizedTextObservation>() ?? [])
            {
                if (observation.TopCandidates(1).FirstOrDefault()?.String is { Length: > 0 } text)
                {
                    // Vision's origin is the lower left corner; read from the top.
                    var box = observation.BoundingBox;
                    words.Add(new LayoutWord(1 - box.Y - box.Height, 1 - box.Y, box.X, text));
                }
            }
        })
        {
            RecognitionLevel = Vision.VNRequestTextRecognitionLevel.Accurate,
            UsesLanguageCorrection = true,
        };

        using var data = Foundation.NSData.FromArray(image);
        using var handler = new Vision.VNImageRequestHandler(data, new Foundation.NSDictionary());
        return handler.Perform([request], out _) ? words : [];
    }
#endif

#if ANDROID
    private static async Task<IReadOnlyList<LayoutWord>> RecognizeAndroidAsync(byte[] image)
    {
        using var bitmap = Android.Graphics.BitmapFactory.DecodeByteArray(image, 0, image.Length);
        if (bitmap is null)
        {
            return [];
        }

        using var input = Xamarin.Google.MLKit.Vision.Common.InputImage.FromBitmap(bitmap, 0);
        using var recognizer = Xamarin.Google.MLKit.Vision.Text.TextRecognition.GetClient(Xamarin.Google.MLKit.Vision.Text.Latin.TextRecognizerOptions.DefaultOptions);
        Xamarin.Google.MLKit.Vision.Text.Text result;
        try
        {
            result = await Android.Gms.Extensions.TasksExtensions.AsAsync<Xamarin.Google.MLKit.Vision.Text.Text>(recognizer.Process(input));
        }
        finally
        {
            // Dispose only releases the .NET handle; the recognizer and the decoded bitmap hold native memory.
            recognizer.Close();
            bitmap.Recycle();
        }

        var words = new List<LayoutWord>();
        foreach (var block in result.TextBlocks)
        {
            foreach (var line in block.Lines)
            {
                foreach (var element in line.Elements)
                {
                    if (element.BoundingBox is { } box && !string.IsNullOrEmpty(element.Text))
                    {
                        words.Add(new LayoutWord(box.Top, box.Bottom, box.Left, element.Text));
                    }
                }
            }
        }

        return words;
    }
#endif

#if WINDOWS
    private static async Task<IReadOnlyList<LayoutWord>> RecognizeWindowsAsync(byte[] image)
    {
        var engine = global::Windows.Media.Ocr.OcrEngine.TryCreateFromUserProfileLanguages();
        if (engine is null)
        {
            return [];
        }

        using var stream = new global::Windows.Storage.Streams.InMemoryRandomAccessStream();
        using (var writer = new global::Windows.Storage.Streams.DataWriter(stream.GetOutputStreamAt(0)))
        {
            writer.WriteBytes(image);
            await writer.StoreAsync();
            await writer.FlushAsync();
            writer.DetachStream();
        }

        stream.Seek(0);
        var decoder = await global::Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream);
        var scale = Math.Min(1d, global::Windows.Media.Ocr.OcrEngine.MaxImageDimension / (double)Math.Max(decoder.PixelWidth, decoder.PixelHeight));
        var transform = new global::Windows.Graphics.Imaging.BitmapTransform
        {
            ScaledWidth = (uint)(decoder.PixelWidth * scale),
            ScaledHeight = (uint)(decoder.PixelHeight * scale),
        };
        using var bitmap = await decoder.GetSoftwareBitmapAsync(
            global::Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,
            global::Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied,
            transform,
            global::Windows.Graphics.Imaging.ExifOrientationMode.RespectExifOrientation,
            global::Windows.Graphics.Imaging.ColorManagementMode.DoNotColorManage);
        var result = await engine.RecognizeAsync(bitmap);
        return [.. result.Lines.SelectMany(l => l.Words).Select(w => new LayoutWord(w.BoundingRect.Y, w.BoundingRect.Y + w.BoundingRect.Height, w.BoundingRect.X, w.Text))];
    }
#endif
}
