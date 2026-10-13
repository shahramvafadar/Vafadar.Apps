// Independent Windows caller; the original schema algorithm is shared with the separate Android probe.
using Vafadar.SqlCipher.Compatibility;

if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "eng", "native", "sqlcipher", "inputs.lock.json")))
    throw new ArgumentException("Run from the repository root.");

if (args.Length == 2 && args[0] is "--create-key-proof" or "--reopen-key-proof")
{
    if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Windows DPAPI proof requires Windows.");
    await WindowsKeyCompatibility.RunAsync(args[1], args[0] == "--create-key-proof");
    return;
}

if (args.Length != 1) throw new ArgumentException("Provide an owned evidence folder for the independent native check.");
var expectedRoot = Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "native-sqlcipher");
Console.WriteLine(await SchemaCompatibility.RunAsync(args[0], expectedRoot));
