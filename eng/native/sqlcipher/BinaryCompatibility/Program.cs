using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using Vafadar.Backup;
using Vafadar.Data;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.CompiledModel;
using Vafadar.SqlCipher.BinaryCompatibility;

if (!OperatingSystem.IsWindows() || args.Length != 2 || args[0] is not ("create" or "reopen"))
    throw new ArgumentException("Provide a Windows independent create/reopen fixture.");
var root = Path.GetFullPath(args[1]);
var ownedRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "native-sqlcipher", "binary-host"));
if (!root.StartsWith(ownedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
    throw new IOException("Binary compatibility requires its independent fictitious root.");
var create = args[0] == "create";
if (create) { if (Directory.Exists(root)) throw new IOException("Use a fresh binary fixture."); Directory.CreateDirectory(root); }
await BinaryPersistenceCompatibility.RunAsync(root, create, folder => OperatingSystem.IsWindows() ? new WindowsGenerationKeyStore(folder, () => { }) : throw new PlatformNotSupportedException(), folder => OperatingSystem.IsWindows() ? new WindowsGenerationFileDurability(folder) : throw new PlatformNotSupportedException());
