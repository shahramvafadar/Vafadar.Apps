# Vafadar.Documents.Maui

Reading photos and PDF files on the device (Android, iOS, Windows). Builds on
[Vafadar.Documents](../Vafadar.Documents/README.md). Nothing leaves the device.

| Type | Purpose |
|---|---|
| `DocumentReader` | `CanRead`, compatibility `ReadAsync`, and `ReadDocumentAsync` returning text plus layout uncertainty |
| `TextRecognizer` | Text recognition with the system engines: Vision (iOS), Windows.Media.Ocr (Windows), ML Kit with its bundled Latin model (Android) |
| `PdfPageImages` | Renders PDF pages with the system renderer: PdfRenderer (Android), PDFKit (iOS), Windows.Data.Pdf (Windows) |

```csharp
if (DocumentReader.CanRead(attachment.ContentType)
    && await DocumentReader.ReadAsync(attachment.Data, attachment.ContentType) is { } text)
{
    var suggestion = ReceiptParser.Parse(text);
}
```

How a file is read:

1. A PDF with a text layer: the text is taken from the file (exact, no OCR).
2. A scanned PDF: at most four pages (the first three and the last) are rendered at about 200 dpi and recognised.
3. A photo: a separate bounded, upright JPEG is prepared at up to 3200 px / quality 95 and recognised. This must receive the transient original when available, not an app's smaller stored attachment.

Android apps using this library should remove the network permissions ML Kit declares
(`ACCESS_NETWORK_STATE`, and `INTERNET` in release builds), as Zanance does in its manifests. ML Kit reads Latin script
only; the Windows engine uses the languages installed in the user profile.

Why not the Syncfusion OCR Processor or Smart Data Extractor: the OCR Processor (Tesseract) has no Android or iOS
runtime and would only wrap these same engines there; the Smart Data Extractor ships about 180 MB of ONNX models, too
large for a phone app. Both remain options for desktop-only features (Zanance decision D-33).

`DocumentImages.PrepareAsync(bytes, maxEdge, quality)` writes metadata-free upright JPEG pixels using existing system
APIs. Android bounds-checks before power-of-two decoding (at most 12 million decoded pixels), then handles all EXIF
rotations/mirrors; iOS uses ImageIO thumbnail decoding with the orientation transform; Windows uses a scaled WIC
bitmap decode with EXIF respected and a fresh JPEG encoder. Recognition and storage policies are separate: Zanance
stores only 1600 px / quality 80 and never persists the transient original. Rereading that copy cannot recover detail.

Engine metadata is retained only where available: Android line/block/angle/confidence, Windows line identities/full
boxes and the source text angle ([BoundingRect](https://learn.microsoft.com/en-us/uwp/api/windows.media.ocr.ocrword.boundingrect)
uses the rotated image), Vision line bounds/baseline angle/confidence. No invented
confidence threshold is used for purchase-total semantics. Scanned PDF pages keep explicit form-feed boundaries.
`ReadDocumentAsync` propagates uncertain legacy layout, catches unreadable-file errors as empty text, and leaves
interpretation/review to the app. No new OCR model or network service, automatic crop or perspective correction.
