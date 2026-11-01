using Microsoft.Maui.Handlers;

namespace Vafadar.Zanance.App.Security;

/// <summary>Includes the stock native time dialog in the existing screenshot policy (D-730).</summary>
internal sealed class ProtectedTimePickerHandler : TimePickerHandler
{
    /// <inheritdoc />
    protected override Android.App.TimePickerDialog CreateTimePickerDialog(int hour, int minute)
    {
        // Retain the SDK's original format, callback, selection, cancellation and
        // orientation recreation. Its public factory needs no private-field access.
        var dialog = base.CreateTimePickerDialog(hour, minute);
        var window = dialog.Window ?? throw new InvalidOperationException("The original Android time dialog window is unavailable.");
        AndroidPageScreenProtection.TrackNativeWindow(window);
        return dialog;
    }
}
