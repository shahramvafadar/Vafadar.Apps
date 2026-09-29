# Vafadar.Documents

Text of documents on the device, platform independent. The platform engines are in
[Vafadar.Documents.Maui](../Vafadar.Documents.Maui/README.md).

| Type | Purpose |
|---|---|
| `LayoutWord` | A word with its box (top, bottom, left), from an OCR engine or a PDF text layer |
| `TextLayout.Rows` | Joins words into rows in reading order; rows in Arabic script (Persian) are read right to left |
| `TextLayout.PagesToRead` | Which pages of a long document are read: the first ones and always the last (totals) |
| `PdfText` | The text layer of a PDF (Syncfusion PDF): `IsPdf`, `PageCount`, `Extract`. Scanned, damaged or password-protected files return `null` |

```csharp
if (PdfText.IsPdf(contentType, bytes) && PdfText.Extract(bytes, maxPages: 4) is { } text)
{
    // A digital PDF: exact text without OCR.
}
```

Persian text stored as presentation forms is normalised (NFKC) to ordinary letters. Interpreting the text – amounts,
dates, names – belongs to the app (for Zanance: `ReceiptParser`).
