using System.Runtime.CompilerServices;
using Microsoft.Maui;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace Vafadar.Zanance.App;

/// <summary>Refreshes only unchanged stock Entry and Editor padding after a retained density change.</summary>
internal static class AndroidEntryConfigurationPadding
{
    // The pinned Core keeps this layer type internal. Compare public type identity only; never access its members.
    private const string OwnedBackgroundTypeName = "Microsoft.Maui.Platform.MauiLayerDrawable";
    private static readonly ConditionalWeakTable<IElementHandler, PaddingState> States = new();
    private static int _registered;

    /// <summary>Captures stock defaults after the original background mapping without retaining a view or handler.</summary>
    internal static void Register()
    {
        // Controls installs its own mappings from these type initializers; attach only after those are ready.
        RuntimeHelpers.RunClassConstructor(typeof(Microsoft.Maui.Controls.Entry).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(Microsoft.Maui.Controls.Editor).TypeHandle);
        if (Interlocked.Exchange(ref _registered, 1) != 0)
        {
            return;
        }

        EntryHandler.Mapper.AppendToMapping(nameof(IView.Background), static (handler, entry) => Capture(handler, entry));
        EditorHandler.Mapper.AppendToMapping(nameof(IView.Background), static (handler, editor) => Capture(handler, editor));
    }

    /// <summary>Checks whether the same stock handler still owns its previously captured default padding.</summary>
    internal static bool NeedsRefresh(IElement element, IElementHandler handler) =>
        TryCurrent(element, handler, out _, out _);

    /// <summary>Applies current theme defaults after the caller remaps the background and rechecks root ownership.</summary>
    internal static void Refresh(IElement element, IElementHandler handler)
    {
        if (!TryCurrent(element, handler, out var view, out var state) || ReadDefault(view) is not { } padding)
        {
            return;
        }

        // D1190/D1199: background remapping preserves Entry/Editor pixels. Obtain current defaults
        // from the exact native constructor; scaling rounded old pixels cannot reconstruct resource dimensions.
        view.SetPadding(padding.Left, padding.Top, padding.Right, padding.Bottom);
        state.Padding = padding;
    }

    /// <summary>Records only the exact stock handler/native class and its original default absolute padding.</summary>
    private static void Capture(IElementHandler handler, IElement entry)
    {
        if (States.TryGetValue(handler, out var existing) && existing.Owner.TryGetTarget(out var owner) &&
            ReferenceEquals(owner, entry))
        {
            return;
        }

        States.Remove(handler);
        if (!IsStockHandler(entry, handler) || handler.PlatformView is not MauiAppCompatEditText view ||
            view.GetType() != typeof(MauiAppCompatEditText) || !IsStockBackground(view) ||
            !ReferenceEquals(handler.VirtualView, entry) || ReadDefault(view) is not { } defaults || Read(view) != defaults)
        {
            return;
        }

        States.Add(handler, new PaddingState(entry, defaults));
    }

    /// <summary>Abandons eligibility when application/native customization replaces captured defaults or ownership.</summary>
    private static bool TryCurrent(IElement element, IElementHandler handler, out MauiAppCompatEditText view, out PaddingState state)
    {
        view = null!;
        state = null!;
        if (!States.TryGetValue(handler, out var captured))
        {
            return false;
        }

        if (!ReferenceEquals(element.Handler, handler) || !ReferenceEquals(handler.VirtualView, element) ||
            !captured.Owner.TryGetTarget(out var owner) || !ReferenceEquals(owner, element) ||
            !IsStockHandler(element, handler) || handler.PlatformView is not MauiAppCompatEditText current ||
            current.GetType() != typeof(MauiAppCompatEditText) || !IsStockBackground(current) || Read(current) != captured.Padding)
        {
            States.Remove(handler);
            return false;
        }

        view = current;
        state = captured;
        return true;
    }

    /// <summary>Accepts only the exact stock text-input handler paired with its corresponding virtual contract.</summary>
    private static bool IsStockHandler(IElement element, IElementHandler handler) =>
        (element is IEntry && handler.GetType() == typeof(EntryHandler)) ||
        (element is IEditor && handler.GetType() == typeof(EditorHandler));

    /// <summary>Limits this repair to the original MAUI-managed background and absolute padding path.</summary>
    private static bool IsStockBackground(MauiAppCompatEditText view) =>
        !view.IsPaddingRelative && view.Background?.GetType() is { } type &&
        type.Assembly == typeof(MauiAppCompatEditText).Assembly && type.FullName == OwnedBackgroundTypeName;

    /// <summary>Reads the four existing native pixel values without changing control content or focus.</summary>
    private static Padding Read(MauiAppCompatEditText view) =>
        new(view.PaddingLeft, view.PaddingTop, view.PaddingRight, view.PaddingBottom);

    /// <summary>Reads a detached current-theme native default with the retained control's actual direction.</summary>
    private static Padding? ReadDefault(MauiAppCompatEditText view)
    {
        if (view.Context is not { } context)
        {
            return null;
        }

        using var reference = new MauiAppCompatEditText(context) { LayoutDirection = view.LayoutDirection };
        return reference.IsPaddingRelative ? null : Read(reference);
    }

    /// <summary>Contains only weak ownership and captured pixels; it does not root the handler, page or native view.</summary>
    private sealed class PaddingState(IElement owner, Padding padding)
    {
        /// <summary>Identifies the original virtual owner without extending its lifetime.</summary>
        internal WeakReference<IElement> Owner { get; } = new(owner);

        /// <summary>Tracks only values last captured or applied by this default-padding owner.</summary>
        internal Padding Padding { get; set; } = padding;
    }

    /// <summary>Immutable absolute pixel padding used for exact ownership comparisons.</summary>
    private readonly record struct Padding(int Left, int Top, int Right, int Bottom);
}
