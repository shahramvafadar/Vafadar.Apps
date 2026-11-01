using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using NativeDialog = AndroidX.AppCompat.App.AlertDialog;

namespace Vafadar.Zanance.App.Security;

/// <summary>Protects the original pinned SDK picker dialog without replacing its UI or selection (D-728).</summary>
internal sealed class ProtectedPickerHandler : PickerHandler
{
    private static readonly FieldInfo OriginalDialog = FindOriginalDialog();

    /// <summary>Admits the pinned SDK contract before connecting any native picker input.</summary>
    public ProtectedPickerHandler() => _ = OriginalDialog;

    [DynamicDependency("_dialog", typeof(PickerHandler))]
    private static FieldInfo FindOriginalDialog()
    {
        // MAUI 10.0.120 has no public picker-dialog creation hook or native-root
        // registration. Retain/read only this source-reviewed managed reference;
        // never reflect Android framework globals or replace the stock backend.
        var field = typeof(PickerHandler).GetField("_dialog",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        if (field?.FieldType != typeof(NativeDialog))
            throw new NotSupportedException("The pinned Android picker dialog contract is unavailable.");
        return field;
    }

    /// <inheritdoc />
    protected override void ConnectHandler(MauiPicker platformView)
    {
        base.ConnectHandler(platformView);
        // The original click creates/shows the SDK dialog synchronously. Observe it
        // afterward in the same native dispatch, before the drawing traversal.
        platformView.Click += OnOriginalClick;
    }

    /// <inheritdoc />
    protected override void DisconnectHandler(MauiPicker platformView)
    {
        platformView.Click -= OnOriginalClick;
        base.DisconnectHandler(platformView);
    }

    private void OnOriginalClick(object? sender, EventArgs args)
    {
        if (OriginalDialog.GetValue(this) is not NativeDialog { IsShowing: true } dialog) return;
        if (dialog.Window?.DecorView is not { } root)
            throw new InvalidOperationException("The original Android picker window is unavailable.");
        AndroidPageScreenProtection.TrackNativeRoot(root);
    }
}
