# Vafadar.Documents.Maui

Reading photos and PDF files on the device (Android, iOS, Windows). Builds on
[Vafadar.Documents](../Vafadar.Documents/README.md). Nothing leaves the device.

| Type | Purpose |
|---|---|
| `DocumentReader` | One entry point: `CanRead(contentType)` and `ReadAsync(bytes, contentType)` return the text rows of a photo or PDF |
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
3. A photo: recognised directly.

Android apps using this library should remove the network permissions ML Kit declares
(`ACCESS_NETWORK_STATE`, and `INTERNET` in release builds), as Zanance does in its manifests. ML Kit reads Latin script
only; the Windows engine uses the languages installed in the user profile.

Why not the Syncfusion OCR Processor or Smart Data Extractor: the OCR Processor (Tesseract) has no Android or iOS
runtime and would only wrap these same engines there; the Smart Data Extractor ships about 180 MB of ONNX models, too
large for a phone app. Both remain options for desktop-only features (Zanance decision D-33).
