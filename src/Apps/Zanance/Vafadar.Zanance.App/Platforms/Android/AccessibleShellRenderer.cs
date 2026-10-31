using System.ComponentModel;
using Vafadar.Zanance.App.Presentation;
using NativeToolbar = AndroidX.AppCompat.Widget.Toolbar;
using AndroidX.DrawerLayout.Widget;
using AndroidX.Fragment.App;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;
using Vafadar.Localization;

namespace Vafadar.Zanance.App;

/// <summary>Keeps the native back arrow and navigation handling, with an app-language accessible description.</summary>
internal sealed class AccessibleShellRenderer : ShellRenderer
{
    private readonly List<WeakReference<Fragment>> _ownedItemFragments = [];
    private bool _navigationRetired;

    /// <summary>Creates the platform renderer through the existing Shell handler factory.</summary>
    public AccessibleShellRenderer() { }

    /// <inheritdoc />
    protected override IShellItemRenderer CreateShellItemRenderer(ShellItem shellItem)
    {
        var item = base.CreateShellItemRenderer(shellItem);
        _ownedItemFragments.Add(new WeakReference<Fragment>(item.Fragment));
        item.Destroyed += OnOwnedItemDestroyed;
        return item;
    }

    private void OnOwnedItemDestroyed(object? sender, EventArgs args)
    {
        if (sender is IShellItemRenderer item) item.Destroyed -= OnOwnedItemDestroyed;
        _ownedItemFragments.RemoveAll(reference => !reference.TryGetTarget(out var fragment) || ReferenceEquals(fragment, sender));
    }

    /// <summary>Removes only this retired root's fragments; Android still owns their destruction and disposal.</summary>
    internal void RetireNavigation()
    {
        if (_navigationRetired) return;
        _navigationRetired = true;
        // D-360: ShellRenderer disconnects its current item but does not remove that root
        // fragment from the Activity manager. Detached containers otherwise accumulate whole
        // navigation trees. Never enumerate/remove another renderer's active fragments.
        foreach (var reference in _ownedItemFragments)
        {
            if (!reference.TryGetTarget(out var fragment) || !((Java.Interop.IJavaPeerable)fragment).PeerReference.IsValid
                || fragment.IsRemoving) continue;
            var manager = fragment.ParentFragmentManager;
            if (manager is null || manager.IsDestroyed) continue;
            var transaction = manager.BeginTransaction()
                ?? throw new InvalidOperationException("Android did not create a retired-root fragment transaction.");
            // An initial add can still be queued when another display update replaces this
            // root. Match the SDK's reordered initial add, so its add/remove can be collapsed
            // before Android tries to create a view in the now-detached old container.
            transaction.SetReorderingAllowed(true);
            transaction.Remove(fragment);
            transaction.CommitAllowingStateLoss();
        }
        _ownedItemFragments.Clear();
    }

    /// <inheritdoc />
    protected override IShellToolbarTracker CreateTrackerForToolbar(NativeToolbar toolbar)
    {
        var context = (IShellContext)this;
        return new AccessibleToolbarTracker(context, toolbar, context.CurrentDrawerLayout);
    }

    /// <summary>Translates only the native back description while retaining platform icons and commands.</summary>
    private sealed class AccessibleToolbarTracker : ShellToolbarTracker
    {
        private NativeToolbar? _nativeToolbar;
        private bool _retired;
        private AndroidToolbarLayoutObserver<AccessibleToolbarTracker>? _layoutListener;
        private int _ordinaryToolbarHeight;
        private int _requestedToolbarHeight;
        private int _titleVerticalChrome = -1;

        /// <summary>Observes translations and native title geometry for this toolbar's lifetime.</summary>
        public AccessibleToolbarTracker(IShellContext context, NativeToolbar toolbar, DrawerLayout drawerLayout)
            : base(context, toolbar, drawerLayout)
        {
            _nativeToolbar = toolbar;
            if (toolbar.ViewTreeObserver is { IsAlive: true } observer)
            {
                _layoutListener = new AndroidToolbarLayoutObserver<AccessibleToolbarTracker>(this, static tracker => tracker.UpdateTitleGeometry());
                _layoutListener.Register(toolbar);
            }
            Translator.Instance.PropertyChanged += OnTranslationsChanged;
        }

        /// <inheritdoc />
        protected override void UpdateToolbarIconAccessibilityText(NativeToolbar toolbar, Shell shell)
        {
            base.UpdateToolbarIconAccessibilityText(toolbar, shell);
            UpdateBackDescription();
        }

        /// <summary>Lets the native title retain its scaled font without clipping taller script metrics.</summary>
        private void UpdateTitleGeometry()
        {
            if (_retired || _nativeToolbar is not { Height: > 0 } toolbar
                || toolbar.Visibility != Android.Views.ViewStates.Visible || string.IsNullOrEmpty(toolbar.Title)) return;
            Android.Widget.TextView? title = null;
            for (var index = 0; index < toolbar.ChildCount; index++)
                if (toolbar.GetChildAt(index) is Android.Widget.TextView candidate && candidate.Text == toolbar.Title)
                { title = candidate; break; }
            if (title?.Layout is not { LineCount: > 0 } layout || title.Height <= 0
                || toolbar.LayoutParameters is not { } parameters) return;
            if (_titleVerticalChrome < 0)
            {
                _ordinaryToolbarHeight = toolbar.Height;
                _titleVerticalChrome = Math.Max(0, toolbar.Height - title.Height);
            }
            // D-449: AppCompat measures a scaled Persian title against the fixed action-bar
            // height. Grow only the owned native toolbar; keep its arrow, title font and handlers.
            var required = layout.GetLineBottom(layout.LineCount - 1)
                + title.CompoundPaddingTop + title.CompoundPaddingBottom + _titleVerticalChrome;
            var height = Math.Max(_requestedToolbarHeight, Math.Max(_ordinaryToolbarHeight, required));
            // Never fight a framework remeasurement or shrink inside its global-layout pass.
            // A fresh toolbar owns a fresh baseline; this toolbar only grows for taller text.
            // D-691: AppCompat can replace the retained toolbar's layout parameters on a width
            // change. A previous request is not proof that its current measured title still fits.
            if (height <= toolbar.Height
                || (height == _requestedToolbarHeight && parameters.Height == height)) return;
            _requestedToolbarHeight = height;
            parameters.Height = height;
            toolbar.LayoutParameters = parameters;
        }

        private void OnTranslationsChanged(object? sender, PropertyChangedEventArgs e) =>
            MainThread.BeginInvokeOnMainThread(UpdateBackDescription);

        private void UpdateBackDescription()
        {
            // D-83: a retained Android toolbar's resource context can keep the previous per-app locale.
            // Use the live translation without replacing the arrow; ignore a disconnected/retired Shell.
            if (!_retired && _nativeToolbar is { } toolbar && ShellContext?.Shell?.Handler is not null && CanNavigateBack)
            { toolbar.NavigationContentDescription = Translator.Instance["Common_Back"]; }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing && !_retired)
            {
                _retired = true;
                Translator.Instance.PropertyChanged -= OnTranslationsChanged;
                // D-485: the observer follows attachment; retirement removes its exact current registration.
                if (_layoutListener is { } listener && _nativeToolbar is { } toolbar)
                    listener.Retire(toolbar);
                _layoutListener = null;
                _nativeToolbar = null;
            }
            base.Dispose(disposing);
        }
    }
}
