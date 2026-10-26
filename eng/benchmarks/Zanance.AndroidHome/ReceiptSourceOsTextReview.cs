using Android.App;
using Android.Views;

namespace Vafadar.Zanance.App;

/// <summary>Checks original owned receipt dialogs at actual large OS text, including native scroll reachability.</summary>
internal static class ReceiptSourceOsTextReview
{
    /// <summary>Retains only copied native geometry/text metadata so a failed scene need not be replayed.</summary>
    internal static object? LastEvidence { get; private set; }

    /// <summary>Requires an explicitly requested real200% configuration; never changes the display itself.</summary>
    internal static float RequireScale(Activity activity)
    {
        var scale = activity.Resources?.Configuration?.FontScale ?? 0;
        if (activity.PackageName != "pro.vafadar.zanance.receiptsourcereview" || Math.Abs(scale - 2f) > .001f)
            throw new InvalidDataException("Original independent source dialog at actual OS200% required.");
        return scale;
    }

    /// <summary>Finds real native text, scrolling only its own original action list when the row is not realized.</summary>
    internal static async Task<Android.Widget.TextView> FindAsync(Activity activity, string expected, CancellationToken ct)
    {
        RequireScale(activity);
        LastEvidence = new { Stage = "find-native-caption", ExpectedText = expected, ActualOSFontScale = 2f };
        var selectionRequested = false;
        for (var attempt = 0; attempt < 100; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            if (Dialog(activity) is { } dialog)
            {
                var target = Walk(dialog).OfType<Android.Widget.TextView>()
                    .FirstOrDefault(view => view.IsShown && view.Text == expected && view.Layout is not null);
                if (target is not null) return target;
                if (!selectionRequested)
                    foreach (var list in Walk(dialog).OfType<Android.Widget.ListView>())
                    {
                        if (list.Adapter is not { Count: > 0 and <= 8 } adapter) continue;
                        for (var position = 0; position < adapter.Count; position++)
                        {
                            using var item = adapter.GetItem(position);
                            if (item?.ToString() != expected) continue;
                            // Real native list scrolling exposes the requested row; it does not choose a source.
                            list.SetSelection(position);
                            selectionRequested = true;
                            break;
                        }
                    }
            }
            await Task.Delay(40, ct);
        }
        throw new InvalidDataException("Original large-text dialog caption was not realized.");
    }

    /// <summary>Requires complete text and its actual original44dp action surface, with no clipping behind another window.</summary>
    internal static async Task<object> ActionAsync(Activity activity, string expected, CancellationToken ct)
    {
        var text = await FindAsync(activity, expected, ct);
        Android.Views.View surface = text;
        if (text is not Android.Widget.Button)
            for (Android.Views.IViewParent? parent = text.Parent; parent is not null; parent = parent.Parent)
            {
                if (parent is Android.Widget.ListView list)
                {
                    var position = list.GetPositionForView(text);
                    if (position < 0) throw new InvalidDataException("Original action row identity unavailable.");
                    using var rowVisible = new Android.Graphics.Rect();
                    if (!surface.GetGlobalVisibleRect(rowVisible) || rowVisible.Height() + 1 < surface.Height)
                    {
                        list.SetSelection(position);
                        await Task.Delay(120, ct);
                        text = await FindAsync(activity, expected, ct);
                        surface = text;
                        for (Android.Views.IViewParent? current = text.Parent; current is Android.Views.View ancestor; current = current.Parent)
                        {
                            if (ancestor is Android.Widget.ListView) break;
                            surface = ancestor;
                        }
                    }
                    break;
                }
                if (parent is Android.Views.View view) surface = view;
            }
        var geometry = CompleteText(text);
        using var visible = new Android.Graphics.Rect();
        var density = surface.Resources?.DisplayMetrics?.Density ?? 0;
        var shown = surface.GetGlobalVisibleRect(visible);
        var result = new { Stage = "native-action", Text = geometry, SurfaceWidthDp = density > 0 ? surface.Width / density : 0,
            SurfaceHeightDp = density > 0 ? surface.Height / density : 0, Shown = shown,
            VisibleWidth = visible.Width(), VisibleHeight = visible.Height(), OriginalNativeAction = true };
        LastEvidence = result;
        if (Dialog(activity) is null || !surface.IsShown || !surface.Enabled || density <= 0
            || !shown || visible.Width() + 1 < surface.Width
            || visible.Height() + 1 < surface.Height || surface.Width / density < 44 || surface.Height / density < 44)
            throw new InvalidDataException("Original complete large-text action target is not visible/reachable.");
        return result;
    }

    /// <summary>Checks all original text allocation and the actual last line after native scrolling, without changing the source.</summary>
    internal static async Task<object> BodyAsync(Activity activity, string expected, CancellationToken ct)
    {
        var text = await FindAsync(activity, expected, ct);
        var geometry = CompleteText(text);
        var scrolled = false;
        for (Android.Views.IViewParent? parent = text.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent is Android.Widget.ScrollView scroll)
            {
                scrolled = scroll.CanScrollVertically(1) || scroll.CanScrollVertically(-1);
                scroll.FullScroll(FocusSearchDirection.Down);
                for (var attempt = 0; attempt < 100 && scroll.CanScrollVertically(1); attempt++)
                    await Task.Delay(20, ct);
                LastEvidence = new { Stage = "native-body-scroll", Text = geometry, scroll.ScrollY,
                    ViewportHeight = scroll.Height, CanScrollFurther = scroll.CanScrollVertically(1) };
                if (scroll.CanScrollVertically(1)) throw new InvalidDataException("Original help example cannot reach its final line.");
                break;
            }
            if (parent is AndroidX.Core.Widget.NestedScrollView nested)
            {
                scrolled = nested.CanScrollVertically(1) || nested.CanScrollVertically(-1);
                nested.FullScroll((int)FocusSearchDirection.Down);
                for (var attempt = 0; attempt < 100 && nested.CanScrollVertically(1); attempt++)
                    await Task.Delay(20, ct);
                LastEvidence = new { Stage = "native-nested-body-scroll", Text = geometry, nested.ScrollY,
                    ViewportHeight = nested.Height, CanScrollFurther = nested.CanScrollVertically(1) };
                if (nested.CanScrollVertically(1)) throw new InvalidDataException("Original nested help example is not reachable.");
                break;
            }
        }
        using var visible = new Android.Graphics.Rect();
        using var origin = new Android.Graphics.Point();
        // The native visible rectangle and its offset share the same root coordinates. Screen
        // coordinates additionally include the dialog/status-bar origin and cannot be mixed here.
        var shown = text.GetGlobalVisibleRect(visible, origin);
        var layout = text.Layout ?? throw new InvalidDataException("Original help layout was lost.");
        var last = layout.LineCount - 1;
        var top = origin.Y + text.TotalPaddingTop + layout.GetLineTop(last);
        var bottom = origin.Y + text.TotalPaddingTop + layout.GetLineBottom(last);
        var result = new { Stage = "native-body-final-line", Text = geometry, NativeScrollRequired = scrolled,
            FinalLineTop = top, FinalLineBottom = bottom, VisibleTop = visible.Top, VisibleBottom = visible.Bottom,
            SameNativeRootRectangleAndOffset = true,
            Shown = shown, FinalOriginalLineFullyReachable = shown && top + 1 >= visible.Top && bottom <= visible.Bottom + 1 };
        LastEvidence = result;
        if (Dialog(activity) is null || !shown
            || top + 1 < visible.Top || bottom > visible.Bottom + 1)
            throw new InvalidDataException("Original final help line is not fully visible after native scrolling.");
        return result;
    }

    /// <summary>Measures actual rendered text rather than assuming that200% means linear pixel scaling.</summary>
    private static object CompleteText(Android.Widget.TextView text)
    {
        var layout = text.Layout ?? throw new InvalidDataException("Original native text layout unavailable.");
        var value = text.Text;
        var result = new { Stage = "native-text-allocation", Text = value, Lines = layout.LineCount, text.Width, text.Height,
            LayoutHeight = layout.Height, text.TotalPaddingTop, text.TotalPaddingBottom,
            LastLineEnd = layout.LineCount > 0 ? layout.GetLineEnd(layout.LineCount - 1) : 0,
            Ellipsis = Enumerable.Range(0, layout.LineCount).Any(line => layout.GetEllipsisCount(line) != 0),
            ActualTextSizePx = text.TextSize, ActualDensity = text.Resources?.DisplayMetrics?.Density,
            ActualConfigurationFontScale = text.Resources?.Configuration?.FontScale,
            Direction = text.LayoutDirection.ToString(), Color = text.CurrentTextColor };
        LastEvidence = result;
        if (value is not { Length: > 0 } || layout.LineCount == 0
            || layout.GetLineEnd(layout.LineCount - 1) != value.Length
            || Enumerable.Range(0, layout.LineCount).Any(line => layout.GetEllipsisCount(line) != 0
                || layout.GetLineLeft(line) < -1 || layout.GetLineRight(line) > layout.Width + 1)
            || text.Height + 1 < layout.Height + text.TotalPaddingTop + text.TotalPaddingBottom)
            throw new InvalidDataException("Original large-text caption allocation is clipped or ellipsized.");
        return result;
    }

    /// <summary>Returns only the current owned dialog, excluding the application root and all other windows.</summary>
    private static Android.Views.View? Dialog(Activity activity)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(29))
            throw new InvalidDataException("Original native window inspection requires API29 or later.");
        return Android.Views.Inspectors.WindowInspector.GlobalWindowViews
            .SingleOrDefault(view => view.Context?.PackageName == activity.PackageName && view.IsShown && view.HasWindowFocus
                && view != activity.Window?.DecorView && view.LayoutParameters is WindowManagerLayoutParams);
    }

    /// <summary>Enumerates only the original realized native descendants.</summary>
    private static IEnumerable<Android.Views.View> Walk(Android.Views.View view)
    {
        yield return view;
        if (view is not ViewGroup group) yield break;
        for (var index = 0; index < group.ChildCount; index++)
            if (group.GetChildAt(index) is { } child)
                foreach (var nested in Walk(child)) yield return nested;
    }
}
