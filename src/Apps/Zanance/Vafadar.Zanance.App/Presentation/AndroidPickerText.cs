#if ANDROID
namespace Vafadar.Zanance.App.Presentation;

/// <summary>Retains complete native picker captions when Android accessibility text grows.</summary>
internal static class AndroidPickerText
{
    private static bool _registered;

    /// <summary>Allows the selected caption to wrap without replacing native selection or editing restrictions.</summary>
    internal static void Register()
    {
        if (_registered) { return; }
        _registered = true;
        Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("ZananceGrowingPickerText", (handler, _) =>
        {
            // D-355: the native read-only EditText otherwise keeps a single horizontally scrolling line.
            // Wrap the complete selected value at the inherited OS font size; keep the original dialog and KeyListener.
            var native = handler.PlatformView;
            native.SetSingleLine(false);
            native.SetMaxLines(int.MaxValue);
            native.SetHorizontallyScrolling(false);
            native.Ellipsize = null;
        });
    }
}
#endif
