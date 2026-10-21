using Android.Text;
using Android.Text.Method;

namespace Vafadar.Maui.Controls;

/// <summary>Retains raw numeric input for application validation while preserving the native numeric keyboard hint.</summary>
internal static class AndroidNumericInput
{
    /// <summary>Removes character filtering only from ordinary numeric entries, retaining secure and nonnumeric input.</summary>
    internal static void PreserveRawText(Android.Widget.EditText native, Microsoft.Maui.IEntry entry)
    {
        var inputType = native.InputType;
        if ((inputType & InputTypes.MaskClass) != InputTypes.ClassNumber) return;
        if (entry.IsReadOnly)
        {
            // D-440: later Android numeric keyboard mappings can restore focus after MAUI maps IsReadOnly.
            // Preserve MAUI's explicit read-only focus/cursor state as well as excluding character changes.
            native.FocusableInTouchMode = false;
            native.Focusable = false;
            native.SetCursorVisible(false);
            return;
        }
        if (entry.IsPassword || (inputType & InputTypes.MaskVariation) == InputTypes.NumberVariationPassword)
        {
            return;
        }

        // D-440: DigitsKeyListener can discard a comma before TextChanged, turning 1,25 into 125.
        // Validation owns separators, signs and invalid drafts. SetRawInputType retains the numeric IME
        // hint without installing another filtering listener; existing length filters and masking stay intact.
        native.KeyListener = TextKeyListener.GetInstance(false, TextKeyListener.Capitalize.None);
        native.SetRawInputType(inputType);
    }
}
