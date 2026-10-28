namespace Vafadar.Zanance.App.Features.Reports;

/// <summary>A labelled amount, e.g. one line of an account movement.</summary>
public sealed record AmountLine(string Label, string Amount, bool IsTotal, bool IsNegative = false, bool IsPositive = false);
