using System.Diagnostics;
using NativeView = Android.Views.View;

namespace Vafadar.Zanance.App.Security;

/// <summary>Includes original SDK dialog roots in the existing screenshot policy (D-727).</summary>
internal static class AndroidDialogScreenProtection
{
    private static readonly RegistrationObserver Observer = new();
    private static IDisposable? _listeners;

    /// <summary>Subscribes before page creation to the pinned SDK's versioned native-root registration.</summary>
    internal static void Register() => _listeners ??= DiagnosticListener.AllListeners.Subscribe(Observer);

    /// <summary>Reads only the versioned native-view envelope, without reflecting private SDK state.</summary>
    private sealed class RegistrationObserver : IObserver<DiagnosticListener>,
        IObserver<KeyValuePair<string, object?>>
    {
        private const string ListenerName = "Microsoft.Maui.NativeElements";
        private readonly object _gate = new();
        private IDisposable? _subscription;

        /// <inheritdoc />
        public void OnNext(DiagnosticListener listener)
        {
            if (listener.Name != ListenerName) return;
            lock (_gate) _subscription ??= listener.Subscribe(this,
                name => name == ListenerName + ".Registered.v1");
        }

        /// <inheritdoc />
        public void OnNext(KeyValuePair<string, object?> value)
        {
            // MAUI 10.0.120 registers its original dialog decor synchronously after Show,
            // before the native drawing traversal. Keep that source-backed versioned
            // contract under native qualification when upgrading the pinned SDK; do not
            // replace alert rendering, consent, result tasks or native cancellation.
            if (value.Key != ListenerName + ".Registered.v1" ||
                value.Value is not object?[] { Length: 5 } parts || parts[0] is not int version || version != 1 ||
                parts[1] is not Page || parts[2] is not NativeView root || parts[3] is not string role ||
                role != "Dialog") return;
            if (MainThread.IsMainThread) AndroidPageScreenProtection.TrackNativeRoot(root);
            else MainThread.BeginInvokeOnMainThread(() => AndroidPageScreenProtection.TrackNativeRoot(root));
        }

        /// <inheritdoc />
        public void OnCompleted() { }

        /// <inheritdoc />
        public void OnError(Exception error) { }
    }
}
