using System.Runtime.Versioning;
using Vafadar.Data;

namespace Vafadar.SqlCipher.Compatibility;

/// <summary>Windows native effects for the shared exact-retirement runtime scenario.</summary>
[SupportedOSPlatform("windows")]
internal static class RetirementCompatibility
{
    /// <summary>Runs one fresh or cold phase against actual Windows keys and file effects.</summary>
    internal static Task<string> RunAsync(string folder, bool create) => SharedRetirementCompatibility.RunAsync(folder, create,
        path => new WindowsGenerationKeyStore(path, () => { }), path => new WindowsGenerationFileDurability(path),
        path =>
        {
            var files = new WindowsProfileRetirementFiles(new Dictionary<string, string> { ["database"] = path }, () => { });
            return new(files.CaptureAsync, files.RemoveAsync, files.VerifyRemovedAsync);
        });
}
