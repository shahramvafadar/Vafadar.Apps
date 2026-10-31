using Vafadar.Data.PortableBudgetChecks;

if (!OperatingSystem.IsWindows() || args.Length != 1)
    throw new ArgumentException("Provide one new owned Windows result directory.");
var root = Path.GetFullPath(args[0]);
if (Directory.Exists(root) || File.Exists(root)) throw new IOException("Preserve existing evidence; use a new result directory.");
Directory.CreateDirectory(root);
SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlcipher());
await PortableBounds.RunAsync(root);
