using System.ComponentModel;
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

        /// <summary>Observes translations only for this toolbar's lifetime.</summary>
        public AccessibleToolbarTracker(IShellContext context, NativeToolbar toolbar, DrawerLayout drawerLayout)
            : base(context, toolbar, drawerLayout)
        {
            _nativeToolbar = toolbar;
            Translator.Instance.PropertyChanged += OnTranslationsChanged;
        }

        /// <inheritdoc />
        protected override void UpdateToolbarIconAccessibilityText(NativeToolbar toolbar, Shell shell)
        {
            base.UpdateToolbarIconAccessibilityText(toolbar, shell);
            UpdateBackDescription();
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
                _nativeToolbar = null;
            }
            base.Dispose(disposing);
        }
    }
}
