namespace Vafadar.Zanance.App.Presentation;

/// <summary>Culture-independent comparison of unsaved form values, including dates outside the display calendar.</summary>
internal static class DraftFingerprint
{
    /// <summary>Captures the form's primitive values without parsing them in the display culture.</summary>
    public static string Create(params object?[] values) =>
        string.Join('|', values.Select(value => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)));
}
