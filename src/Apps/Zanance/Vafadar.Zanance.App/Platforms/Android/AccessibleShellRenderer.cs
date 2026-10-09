using System.ComponentModel;
using NativeToolbar = AndroidX.AppCompat.Widget.Toolbar;
using AndroidX.DrawerLayout.Widget;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;
using Vafadar.Localization;

namespace Vafadar.Zanance.App;

/// <summary>Keeps the native back arrow and navigation handling, with an app-language accessible description.</summary>
internal sealed class AccessibleShellRenderer : ShellRenderer
{
    /// <summary>Creates the platform renderer through the existing Shell handler factory.</summary>
    public AccessibleShellRenderer() { }

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
