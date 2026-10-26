#if NATIVE_FAULT_PC_REVIEW
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Android.App;

namespace Vafadar.Zanance.App;

/// <summary>Optionally arms only the independent retained-source reader, never the shipping application.</summary>
internal static partial class NativeFaultPcObserver
{
    private static int requested;
    private static string? armedContext;

    /// <summary>Requires the explicit owned UID, read-only phase and original request identifiers before opening a private record.</summary>
    internal static bool TryArm(Activity activity)
    {
        if (activity.Intent?.GetBooleanExtra("review_native_fault_pc", false) != true) return true;
        var run = activity.Intent.GetStringExtra("review_run");
        var comparison = activity.Intent.GetStringExtra("review_pass");
        var pid = Android.OS.Process.MyPid();
        var uid = Android.OS.Process.MyUid();
        try
        {
            if (activity.PackageName != "pro.vafadar.zanance.androidhomereview"
                || activity.Intent.GetStringExtra("review_phase") != "home-current-stages"
                || activity.Intent.GetIntExtra("review_fault_uid", -1) != uid
                || run is null || comparison is null || !Identifier().IsMatch(run) || !Identifier().IsMatch(comparison))
                throw new InvalidDataException("Exact independent read-only signal request required.");
            var context = run + "-" + comparison;
            if (Interlocked.CompareExchange(ref requested, 1, 0) != 0)
                return armedContext == context; // Preserve an already armed process; never reopen/rearm an accepted request.
            var folder = Path.Combine(activity.FilesDir?.AbsolutePath
                ?? throw new InvalidDataException("Owned private files unavailable."), "native-fault-observations");
            Directory.CreateDirectory(folder);
            var name = context + "-" + pid.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".bin";
            var token = Convert.FromHexString(run);
            int code;
            try { code = OpenAndInstall(Path.Combine(folder, name), checked((uint)uid), token); }
            finally { CryptographicOperations.ZeroMemory(token); }
            Android.Util.Log.Info("ZananceFaultPc", JsonSerializer.Serialize(new
            {
                Run = run, Comparison = comparison, Pid = pid, Uid = uid, NativeArmCode = code,
                OnlyRelativeReviewedEngineLocations = true, NoDatabaseOrFinancialCommand = true,
                RequiresIndependentFatalEvidence = true
            }));
            if (code != 0) return false;
            armedContext = context;
            // A normal managed exception must remain recoverable with the original runtime handler in place.
            try { _ = OriginalLength(null!); }
            catch (NullReferenceException)
            {
                Android.Util.Log.Info("ZananceFaultPc", JsonSerializer.Serialize(new
                { Run = run, Comparison = comparison, Pid = pid, OriginalManagedNullReferenceRecovered = true,
                    NoClaimThatThisExceptionUsedAnOsSignal = true }));
                return true;
            }
            return false;
        }
        catch (Exception error)
        {
            Android.Util.Log.Info("ZananceFaultPc", JsonSerializer.Serialize(new
            { Run = run, Comparison = comparison, Pid = pid, Uid = uid, Refused = true, ErrorType = error.GetType().FullName }));
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int OriginalLength(string value) => value.Length;

    [DllImport("zanance_fault_pc", EntryPoint = "zanance_fault_pc_open_and_install", CallingConvention = CallingConvention.Cdecl)]
    private static extern int OpenAndInstall([MarshalAs(UnmanagedType.LPUTF8Str)] string path, uint uid,
        [In, MarshalAs(UnmanagedType.LPArray, SizeConst = 16)] byte[] token);

    [GeneratedRegex("^[0-9a-f]{32}$", RegexOptions.CultureInvariant)] private static partial Regex Identifier();
}
#endif
