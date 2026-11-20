using Vafadar.Documents.Maui;

namespace Vafadar.Documents.Tests
{
    /// <summary>Exercises the production document reader with synthetic buffers returned by platform doubles.</summary>
    public sealed class DocumentReaderBufferTests
    {
        [Fact]
        public async Task Prepared_photo_is_cleared_when_ocr_returns()
        {
            byte[] source = [7, 8, 9];
            byte[] prepared = [4, 5, 6];
            DocumentImages.Prepared = prepared;
            TextRecognizer.Reads = [];
            TextRecognizer.FailOnRead = false;
            try
            {
                await DocumentReader.ReadDocumentAsync(source, "image/jpeg");

                Assert.Equal(new byte[] { 4, 5, 6 }, Assert.Single(TextRecognizer.Reads));
                Assert.All(prepared, value => Assert.Equal(0, value));
                Assert.Equal(new byte[] { 7, 8, 9 }, source);
            }
            finally { DocumentImages.Prepared = []; TextRecognizer.Reads = []; }
        }

        [Fact]
        public async Task Scanned_page_images_are_cleared_even_when_ocr_never_reaches_the_second_page()
        {
            var source = MinimalPdf.Create([[], []]);
            var expected = source.ToArray();
            byte[] first = [6, 7], second = [8, 9];
            PdfPageImages.Images = [first, second];
            TextRecognizer.Reads = [];
            TextRecognizer.FailOnRead = true;
            try
            {
                var layout = await DocumentReader.ReadDocumentAsync(source, "application/pdf");

                Assert.Null(layout.Text);
                Assert.Equal(new byte[] { 6, 7 }, Assert.Single(TextRecognizer.Reads));
                Assert.All(first, value => Assert.Equal(0, value));
                Assert.All(second, value => Assert.Equal(0, value));
                Assert.Equal(expected, source);
            }
            finally { PdfPageImages.Images = []; TextRecognizer.Reads = []; TextRecognizer.FailOnRead = false; }
        }
    }
}

namespace Vafadar.Documents.Maui
{
    /// <summary>Supplies the newly allocated photo that a platform normalizer would return.</summary>
    internal static class DocumentImages
    {
        internal const int RecognitionEdge = 3200;
        internal static byte[] Prepared = [];
        internal static Task<byte[]> PrepareAsync(byte[] data, int edge, float quality) => Task.FromResult(Prepared);
    }

    /// <summary>Supplies already rendered page images, including pages that OCR may never read.</summary>
    internal static class PdfPageImages
    {
        internal static bool IsSupported => true;
        internal static IReadOnlyList<byte[]> Images = [];
        internal static Task<IReadOnlyList<byte[]>> RenderAsync(byte[] pdf, int maximum) => Task.FromResult(Images);
    }

    /// <summary>Captures synthetic input as observed by the reader and can fail during the first read.</summary>
    internal static class TextRecognizer
    {
        internal static bool IsSupported => true;
        internal static bool FailOnRead;
        internal static List<byte[]> Reads = [];
        internal static Task<IReadOnlyList<LayoutWord>> RecognizeAsync(byte[] image)
        {
            Reads.Add(image.ToArray());
            return FailOnRead
                ? Task.FromException<IReadOnlyList<LayoutWord>>(new IOException("Synthetic OCR failure."))
                : Task.FromResult<IReadOnlyList<LayoutWord>>([]);
        }
    }
}
