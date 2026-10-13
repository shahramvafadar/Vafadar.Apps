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

if (args.Length == 2 && args[0] is "fresh-startup-create" or "fresh-startup-reopen" or "startup-create" or "startup-reopen" or "startup-resume")
{
    if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Windows startup requires Windows.");
    var owned = Path.GetFullPath(args[1]);
    var root = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "native-sqlcipher"));
    if (!owned.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        throw new ArgumentException("Startup proof must stay in independently owned artifacts.");
    SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlcipher());
    Func<string, Vafadar.Data.IDatabaseGenerationKeyStore> createKeys = path =>
        {
            if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
            return new Vafadar.Data.WindowsGenerationKeyStore(path, () => { });
        };
    Func<string, Vafadar.Data.IDatabaseGenerationFileDurability> createDurability = path =>
        {
            if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
            return new Vafadar.Data.WindowsGenerationFileDurability(path);
        };
    Console.WriteLine(args[0].StartsWith("fresh-", StringComparison.Ordinal)
        ? await FreshStartupCompatibility.RunAsync(owned, args[0] == "fresh-startup-create", createKeys, createDurability)
        : await ProtectedStartupCompatibility.RunAsync(owned, args[0] == "startup-create", createKeys, createDurability, args[0] == "startup-resume"));
    return;
}

if (args.Length == 2 && args[0].StartsWith("journal-", StringComparison.Ordinal))
{
    var owned = Path.GetFullPath(args[1]);
    var root = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "native-sqlcipher"));
    if (!owned.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        throw new ArgumentException("Journal proof must stay in independently owned artifacts.");
    Console.WriteLine(await JournalCompatibility.RunAsync(owned, args[0]));
    return;
}

if (args.Length != 1) throw new ArgumentException("Provide an owned evidence folder for the independent native check.");
var expectedRoot = Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "native-sqlcipher");
Console.WriteLine(await SchemaCompatibility.RunAsync(args[0], expectedRoot));
