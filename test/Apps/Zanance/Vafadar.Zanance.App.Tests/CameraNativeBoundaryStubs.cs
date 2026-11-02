// Native boundaries only: the original Android CameraCapture and bounded file reader execute unchanged in these tests.
namespace Android.App
{
    /// <summary>Values returned at the original Android activity boundary.</summary>
    internal enum Result { Canceled = 0, Ok = -1 }
}
namespace Android.Content
{
    /// <summary>Original temporary file grants, without an operating-system permission operation.</summary>
    [Flags] internal enum ActivityFlags { GrantWriteUriPermission = 1, GrantReadUriPermission = 2 }
    /// <summary>Captures only the original output file passed to the native launch.</summary>
    internal sealed class Intent(string action)
    {
        /// <summary>Gets the original intent action.</summary>
        internal string Action { get; } = action;
        /// <summary>Gets the native output object supplied by the original adapter.</summary>
        internal object? Output { get; private set; }
        /// <summary>Retains the original output argument for the controlled native launch.</summary>
        internal void PutExtra(string name, object value) => Output = value;
        /// <summary>Accepts the original temporary-grant flags without changing system permissions.</summary>
        internal void AddFlags(ActivityFlags flags) { }
    }
    /// <summary>Represents a native camera launch refusal.</summary>
    internal sealed class ActivityNotFoundException : Exception { }
}
namespace Android.Content.PM
{
    /// <summary>Admits the camera feature without constructing any Android service.</summary>
    internal sealed class PackageManager
    {
        /// <summary>Names the original optional camera feature.</summary>
        internal const string FeatureCameraAny = "camera.any";
        /// <summary>Admits only the feature boundary needed by these host tests.</summary>
        internal bool HasSystemFeature(string feature) => true;
    }
}
namespace Android.Provider
{
    /// <summary>Names used by the original capture intent.</summary>
    internal static class MediaStore
    {
        /// <summary>Names the original camera action and output argument.</summary>
        internal const string ActionImageCapture = "android.media.action.IMAGE_CAPTURE", ExtraOutput = "output";
    }
}
namespace Android.Runtime
{
    /// <summary>Retains the original provider registration as inert host metadata.</summary>
    [AttributeUsage(AttributeTargets.Class)] internal sealed class RegisterAttribute(string name) : Attribute
    {
        /// <summary>Gets the unchanged provider registration name.</summary>
        internal string Name { get; } = name;
    }
}
namespace Java.IO
{
    /// <summary>Maps only each test's fresh camera output onto its real temporary filesystem.</summary>
    internal sealed class File
    {
        /// <summary>Gets the real path inside this test-owned temporary directory.</summary>
        internal string AbsolutePath { get; }
        /// <summary>Wraps the fresh test-owned cache directory.</summary>
        internal File(string path) => AbsolutePath = path;
        /// <summary>Wraps the original adapter-generated output name beneath its owned cache.</summary>
        internal File(File parent, string child) => AbsolutePath = Path.Combine(parent.AbsolutePath, child);
        /// <summary>Creates the original camera subdirectory on the test filesystem.</summary>
        internal bool Mkdirs() { Directory.CreateDirectory(AbsolutePath); return true; }
        /// <summary>Reports whether the original camera output exists.</summary>
        internal bool Exists() => System.IO.File.Exists(AbsolutePath);
        /// <summary>Reads the real camera output length for the original encoded-size guard.</summary>
        internal long Length() => new FileInfo(AbsolutePath).Length;
        /// <summary>Deletes only the exact test-owned output selected by the original adapter.</summary>
        internal bool Delete() { System.IO.File.Delete(AbsolutePath); return true; }
    }
}
namespace AndroidX.Core.Content
{
    /// <summary>Passes only the original test-owned output through the intent boundary.</summary>
    internal class FileProvider
    {
        /// <summary>Passes the exact original output through an inert URI boundary.</summary>
        internal static object GetUriForFile(Vafadar.Zanance.App.CameraBoundaryActivity activity,
            string authority, Java.IO.File file) => file;
    }
}
namespace Vafadar.Zanance.App
{
    /// <summary>Current native activity boundary used only by the linked original camera adapter.</summary>
    internal static class Platform
    {
        /// <summary>Gets or sets the current native activity boundary for the serialized cohort.</summary>
        internal static CameraBoundaryActivity? CurrentActivity { get; set; }
        /// <summary>Gets the inert application feature-check boundary.</summary>
        internal static CameraBoundaryContext AppContext { get; } = new();
    }
    /// <summary>Provides only the original feature check.</summary>
    internal sealed class CameraBoundaryContext
    {
        /// <summary>Gets the original feature-check substitute without native services.</summary>
        internal Android.Content.PM.PackageManager PackageManager { get; } = new();
    }
    /// <summary>Records native launches and holds their callbacks while real original file logic runs.</summary>
    internal sealed class CameraBoundaryActivity(string root)
    {
        /// <summary>Gets the unique real cache directory owned by this test.</summary>
        internal Java.IO.File CacheDir { get; } = new(root);
        /// <summary>Gets the fixed independent test identity.</summary>
        internal string PackageName => "pro.vafadar.zanance.cameraownershiptests";
        /// <summary>Gets the number of original native launch attempts.</summary>
        internal int Launches { get; private set; }
        /// <summary>Gets the request code that owns the last native result.</summary>
        internal int LastRequest { get; private set; }
        /// <summary>Gets the exact output passed to the last native launch.</summary>
        internal Java.IO.File? LastOutput { get; private set; }
        /// <summary>Gets or sets whether the native boundary refuses the launch.</summary>
        internal bool ThrowOnLaunch { get; set; }
        /// <summary>Records the original native launch and optionally preserves its actual refusal.</summary>
        internal void StartActivityForResult(Android.Content.Intent intent, int request)
        {
            Launches++; LastRequest = request; LastOutput = (Java.IO.File?)intent.Output;
            if (ThrowOnLaunch) throw new Android.Content.ActivityNotFoundException();
        }
    }
}
