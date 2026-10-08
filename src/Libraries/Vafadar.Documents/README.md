# Vafadar.Documents

Text of documents on the device, platform independent. The platform engines are in
[Vafadar.Documents.Maui](../Vafadar.Documents.Maui/README.md).

| Type | Purpose |
|---|---|
| `LayoutWord` | A word with optional right edge, page, source line/block, baseline/source angle and engine character confidence |
| `TextLayout.Reconstruct` | Returns `TextLayoutResult(Text, IsAmbiguous)`; preserves source lines, deskews supplied geometry, aligns columns and joins nearby decimal pieces |
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

D-64 metadata is additive: existing `LayoutWord(Top, Bottom, Left, Text)` construction and `Rows` still work. Source
line ids are scoped by page/block. Positive baseline angles slope down to the right; coordinates already made upright
must use Angle=0 and may retain the original rotation as SourceAngle. Engine confidence describes characters only.
Different pages never merge: rows within a page use LF; page boundaries use form feed (`\f`). Consumers needing
review guarantees should use `Reconstruct`, not discard `IsAmbiguous` via the compatibility `Rows` wrapper.

Full right bounds permit joining a nearby comma/dot and following decimals; separated quantity/price tokens are not
concatenated without a separator. RTL reverses text groups while retaining numeric fragments in their internal order.
Without angle/line metadata, suspicious staggered columns are explicitly uncertain. The algorithm does not invent an
angle from an ambiguous price column. PDF extraction retains page ids/full boxes; selected pages remain in order.
