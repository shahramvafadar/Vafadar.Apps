#if HOUSEHOLD_MONEY_REVIEW
using System.Reflection;
using System.Text.Json;
using Vafadar.Zanance.App.Diagnostics;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Checks selected and actual popup text from the two changed original household pickers without accepting a choice.</summary>
    private static async Task ObserveHouseholdCaptionAndroidAsync(Page page, Picker picker, string stage, bool displayOnly,
        string folder, string comparison, Func<VisualElement, Task> scroll, List<object> observations, CancellationToken ct)
    {
        await scroll(picker);
        await PlanEditorNativeReview.WaitAsync(() => picker.Handler?.PlatformView is Android.Widget.EditText
            { IsShown: true, IsAttachedToWindow: true, Enabled: true, Width: > 0, Height: > 0 } && picker.SelectedIndex >= 0, ct);
        var index = picker.SelectedIndex; var expected = picker.Items[index];
        if (expected.Contains("\u00c2\u00b7", StringComparison.Ordinal) || !expected.Contains('\u00b7'))
            throw new ReviewCheckException("Original household separator is malformed or missing.");
        var native = (Android.Widget.EditText)picker.Handler!.PlatformView!;
        await scroll(picker); await Task.Delay(100, ct);
        using var visible = new Android.Graphics.Rect();
        if (!native.GetGlobalVisibleRect(visible) || visible.Width() + 1 < native.Width || visible.Height() + 1 < native.Height)
            throw new ReviewCheckException("Original selected household caption is outside the visible viewport.");
        if (native.Text != expected || native.Layout is not { } selectedLayout)
            throw new ReviewCheckException("Original selected household text differs.");
        MonetaryOsScope.RequireCompleteHeight(native, selectedLayout);
        var selected = new { native.Text, native.Width, native.Height, Lines = selectedLayout.LineCount };
        if (!native.PerformClick()) throw new ReviewCheckException("Original household picker did not open.");
        var field = typeof(Microsoft.Maui.Handlers.PickerHandler).GetField("_dialog", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field?.GetValue(picker.Handler) is not AndroidX.AppCompat.App.AlertDialog dialog || dialog.ListView is not { } list)
            throw new ReviewCheckException("Original household native dialog missing.");
        object popup;
        try
        {
            list.SetSelection(index);
            await PlanEditorNativeReview.WaitAsync(() => list.GetChildAt(index - list.FirstVisiblePosition) is Android.Widget.TextView
                { IsShown: true, Width: > 0, Height: > 0 }, ct);
            bool Ready() => list.GetChildAt(index - list.FirstVisiblePosition) is Android.Widget.TextView current
                && current.Text == expected && current.Layout is not null && dialog.Context?.PackageName == Package
                && dialog.Window?.DecorView.HasWindowFocus == true;
            if (!Ready())
            {
                var initial = list.GetChildAt(index - list.FirstVisiblePosition) as Android.Widget.TextView;
                LogReport("household-caption-popup-arranging", comparison, new { Stage = stage, Expected = expected,
                    NativeText = initial?.Text, Package = dialog.Context?.PackageName, PopupFocus = dialog.Window?.DecorView.HasWindowFocus,
                    LayoutReady = initial?.Layout is not null });
            }
            // Row attachment can precede native dialog focus/layout. Keep the same popup and strict predicate.
            await PlanEditorNativeReview.WaitAsync(Ready, ct);
            var row = (Android.Widget.TextView)list.GetChildAt(index - list.FirstVisiblePosition)!;
            var layout = row.Layout ?? throw new ReviewCheckException("Original household popup layout missing.");
            MonetaryOsScope.RequireCompleteHeight(row, layout);
            popup = new { row.Text, row.Width, row.Height, Lines = layout.LineCount };
            if (dialog.GetButton((int)Android.Content.DialogButtonType.Negative) is not { } cancel || !cancel.PerformClick())
                throw new ReviewCheckException("Original household picker Cancel unavailable.");
        }
        finally { if (dialog.IsShowing) dialog.Dismiss(); }
        if (!ReferenceEquals(Shell.Current.CurrentPage, page) || picker.SelectedIndex != index)
            throw new ReviewCheckException("Household caption observation changed original page or selection.");
        var observation = new { Stage = stage, ProcessId = Environment.ProcessId, Expected = expected, Selected = selected,
            Popup = popup, DisplayPacketOnly = displayOnly, FinancialSaves = 0 };
        await File.WriteAllTextAsync(Path.Combine(folder, stage + ".json"), JsonSerializer.Serialize(observation), ct);
        observations.Add(observation); LogReport("household-caption-observation", comparison, observation);
    }
}
#endif
