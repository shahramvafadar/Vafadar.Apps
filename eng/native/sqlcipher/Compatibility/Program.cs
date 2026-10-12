// Independent Windows caller; the original schema algorithm is shared with the separate Android probe.
using Vafadar.SqlCipher.Compatibility;

if (args.Length != 1) throw new ArgumentException("Provide an owned evidence folder for the independent native check.");
if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "eng", "native", "sqlcipher", "inputs.lock.json")))
    throw new ArgumentException("Run from the repository root.");
var expectedRoot = Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "native-sqlcipher");
Console.WriteLine(await SchemaCompatibility.RunAsync(args[0], expectedRoot));
