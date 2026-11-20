using Android.App;
using Android.Runtime;

namespace Vafadar.Zanance.App;

[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    protected override MauiApp CreateMauiApp()
    {
        // Private portable backups are staged by MAUI under the restricted internal provider path.
        Microsoft.Maui.Storage.FileProvider.TemporaryLocation = Microsoft.Maui.Storage.FileProviderLocation.Internal;
        return MauiProgram.CreateMauiApp();
    }
}
