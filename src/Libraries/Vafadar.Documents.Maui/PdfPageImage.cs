namespace Vafadar.Documents.Maui;

/// <summary>One bounded encoded PDF page and the actual document count; the caller owns the transient image bytes.</summary>
public sealed record PdfPageImage(int PageCount, byte[] Data);
