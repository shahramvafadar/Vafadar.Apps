using Android.App;
using Android.Runtime;

namespace Vafadar.Zanance.App;

/// <summary>Retains original initialization and observes real SDK tap payloads before native Activity delivery.</summary>
[Application]
public sealed class MainApplication(IntPtr handle, JniHandleOwnership ownership) : MauiApplication(handle, ownership)
{
    /// <inheritdoc />
    protected override MauiApp CreateMauiApp()
    {
        var app = MauiProgram.CreateMauiApp();
        // Subscription only: never prepare/open a database or read key material during Application creation.
        NativeGoalProfileScopeReview.ObserveNativeTap(app.Services);
        return app;
    }
}
