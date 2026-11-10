namespace Vafadar.Zanance.App.Presentation;

/// <summary>Preserves a read-only Editor's native scrolling inside an enclosing page scroll view.</summary>
public static class ReadOnlyTextScroll
{
    /// <summary>Identifies the opt-in gesture behavior without replacing the original Editor or its style.</summary>
    public static readonly BindableProperty IsEnabledProperty = BindableProperty.CreateAttached(
        "IsEnabled", typeof(bool), typeof(ReadOnlyTextScroll), false, propertyChanged: OnEnabledChanged);

    /// <summary>Gets whether this Editor retains its own read-only scrolling gesture.</summary>
    public static bool GetIsEnabled(BindableObject value) => (bool)value.GetValue(IsEnabledProperty);

    /// <summary>Sets whether this Editor retains its own read-only scrolling gesture.</summary>
    public static void SetIsEnabled(BindableObject value, bool enabled) => value.SetValue(IsEnabledProperty, enabled);

    /// <summary>Attaches only the platform behavior; bindings, editing policy and styling remain original.</summary>
    private static void OnEnabledChanged(BindableObject value, object oldValue, object newValue)
    {
#if ANDROID
        if (value is not Editor editor) return;
        if ((bool)newValue)
        {
            editor.HandlerChanged += OnHandlerChanged;
            editor.HandlerChanging += OnHandlerChanging;
            Attach(editor);
        }
        else
        {
            editor.HandlerChanged -= OnHandlerChanged;
            editor.HandlerChanging -= OnHandlerChanging;
            Detach(editor);
        }
#endif
    }

#if ANDROID
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Editor, Subscription> Subscriptions = new();
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Android.Widget.EditText, TouchState> Gestures = new();

    /// <summary>Connects to a replacement native handler without retaining its page or model.</summary>
    private static void OnHandlerChanged(object? sender, EventArgs args)
    {
        if (sender is Editor editor && GetIsEnabled(editor)) Attach(editor);
    }

    /// <summary>Releases only this Editor's native observation before its handler changes.</summary>
    private static void OnHandlerChanging(object? sender, HandlerChangingEventArgs args)
    {
        if (sender is Editor editor) Detach(editor);
    }

    /// <summary>Registers static native callbacks, preserving all original focus and movement settings.</summary>
    private static void Attach(Editor editor)
    {
        if (editor.Handler?.PlatformView is not Android.Widget.EditText current) return;
        var subscription = Subscriptions.GetValue(editor, static _ => new Subscription());
        if (ReferenceEquals(subscription.Native, current)) return;
        Detach(editor);
        subscription.Native = current;
        subscription.OriginalBreakStrategy = current.BreakStrategy;
        subscription.OwnsBreakStrategy = editor.IsReadOnly && current.BreakStrategy != Android.Text.BreakStrategy.HighQuality;
        if (subscription.OwnsBreakStrategy)
        {
            // D-997: the retained Simple-layout snapshots exceed their content clip.
            // Native paragraph breaking keeps their glyph bounds inside the same styled viewport.
            current.BreakStrategy = Android.Text.BreakStrategy.HighQuality;
        }
        Gestures.Add(current, new TouchState());
        current.Touch += OnNativeTouch;
    }

    /// <summary>Removes this subscription and releases an unfinished owned drag.</summary>
    private static void Detach(Editor editor)
    {
        if (!Subscriptions.TryGetValue(editor, out var subscription) || subscription.Native is not { } native) return;
        if (Gestures.TryGetValue(native, out var gesture) && gesture.Owns)
            native.Parent?.RequestDisallowInterceptTouchEvent(false);
        native.Touch -= OnNativeTouch;
        Gestures.Remove(native);
        if (subscription.OwnsBreakStrategy && native.BreakStrategy == Android.Text.BreakStrategy.HighQuality)
            native.BreakStrategy = subscription.OriginalBreakStrategy;
        subscription.OwnsBreakStrategy = false;
        subscription.Native = null;
    }

    /// <summary>Lets Android scroll text and hands the drag back when that text reaches its boundary.</summary>
    private static void OnNativeTouch(object? sender, Android.Views.View.TouchEventArgs args)
    {
        // D-993: the enclosing ScrollView otherwise intercepts a nonfocusable
        // read-only Editor's drag. Do not enable editing, consume events or move text here.
        args.Handled = false;
        if (sender is not Android.Widget.EditText view || args.Event is not { } motion ||
            !Gestures.TryGetValue(view, out var gesture)) return;

        switch (motion.ActionMasked)
        {
            case Android.Views.MotionEventActions.Down:
                gesture.LastY = motion.GetY();
                gesture.Owns = !view.Focusable && !view.IsCursorVisible &&
                    (view.CanScrollVertically(-1) || view.CanScrollVertically(1));
                if (gesture.Owns) view.Parent?.RequestDisallowInterceptTouchEvent(true);
                break;
            case Android.Views.MotionEventActions.Move when gesture.Owns:
                var delta = motion.GetY() - gesture.LastY;
                gesture.LastY = motion.GetY();
                if (Math.Abs(delta) > 0.5f && !view.CanScrollVertically(delta > 0 ? -1 : 1))
                {
                    gesture.Owns = false;
                    view.Parent?.RequestDisallowInterceptTouchEvent(false);
                }
                break;
            case Android.Views.MotionEventActions.Up:
            case Android.Views.MotionEventActions.Cancel:
                gesture.Owns = false;
                view.Parent?.RequestDisallowInterceptTouchEvent(false);
                break;
        }
    }

    /// <summary>Stores the native observation under a weak Editor key.</summary>
    private sealed class Subscription
    {
        /// <summary>Gets or sets the currently observed native view.</summary>
        internal Android.Widget.EditText? Native { get; set; }

        /// <summary>Gets or sets the native policy captured before this opt-in changed it.</summary>
        internal Android.Text.BreakStrategy OriginalBreakStrategy { get; set; }

        /// <summary>Gets or sets whether this subscription still owns a policy change to restore.</summary>
        internal bool OwnsBreakStrategy { get; set; }
    }

    /// <summary>Stores pointer state without retaining a view, page or model.</summary>
    private sealed class TouchState
    {
        /// <summary>Gets or sets the previous pointer position within this viewport.</summary>
        internal float LastY { get; set; }

        /// <summary>Gets or sets whether the text can still consume this drag.</summary>
        internal bool Owns { get; set; }
    }
#endif
}
