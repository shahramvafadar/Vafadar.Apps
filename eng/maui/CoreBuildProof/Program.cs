using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

// Validate the newly compiled public framework only. Application images and live data are never inputs.
using var profile = JsonDocument.Parse(File.ReadAllText(args[0]));
var expected = profile.RootElement;
var sourceRoot = Path.GetFullPath(args[3]);
using var original = JsonDocument.Parse(File.ReadAllText(args[4]));
var allowedChanges = expected.GetProperty("patch_files").EnumerateArray()
    .ToDictionary(x => x.GetProperty("path").GetString()!, x => x.GetProperty("after").GetString()!, StringComparer.Ordinal);
using var stream = File.OpenRead(args[1]);
using var image = new PEReader(stream);
var metadata = image.GetMetadataReader();
var assembly = metadata.GetAssemblyDefinition();
if (metadata.GetString(assembly.Name) != "Microsoft.Maui" || !assembly.PublicKey.IsNil ||
    assembly.Version.ToString() != expected.GetProperty("assembly_version").GetString())
    throw new InvalidOperationException("Private Core public assembly identity changed.");
var version = FileVersionInfo.GetVersionInfo(args[1]);
if (version.FileVersion != expected.GetProperty("file_version").GetString() ||
    version.ProductVersion != expected.GetProperty("informational_version").GetString())
    throw new InvalidOperationException("Private Core version stamp is missing or ambiguous.");
using var symbols = File.OpenRead(args[2]);
using var provider = MetadataReaderProvider.FromPortablePdbStream(symbols);
var pdb = provider.GetMetadataReader();
var options = new Dictionary<string, string>(StringComparer.Ordinal);
foreach (var handle in pdb.CustomDebugInformation)
{
    var item = pdb.GetCustomDebugInformation(handle);
    if (item.Parent.Kind != HandleKind.ModuleDefinition ||
        pdb.GetGuid(item.Kind) != new Guid("B5FEEC05-8CD0-4A83-96DA-466284BB4BD8"))
        continue;
    var bytes = pdb.GetBlobBytes(item.Value);
    if (bytes.Length > 32768)
        throw new InvalidOperationException("Compiler-options bound exceeded.");
    var fields = Encoding.UTF8.GetString(bytes).Split('\0');
    if (fields[^1].Length != 0 || (fields.Length - 1) % 2 != 0)
        throw new InvalidOperationException("Compiler-options encoding refused.");
    for (var i = 0; i < fields.Length - 1; i += 2)
        options.Add(fields[i], fields[i + 1]);
}
foreach (var item in expected.GetProperty("expected_compile_options").EnumerateObject())
{
    if (!options.TryGetValue(item.Name, out var actual) || actual != item.Value.GetString())
        throw new InvalidOperationException("Private Core compiler profile changed: " + item.Name);
}
if (!options.TryGetValue("runtime-version", out var runtime) ||
    !runtime.StartsWith(expected.GetProperty("host_runtime").GetString()! + "-", StringComparison.Ordinal))
    throw new InvalidOperationException("Private Core build runtime changed.");
var handwritten = 0;
foreach (var handle in pdb.Documents)
{
    var document = pdb.GetDocument(handle);
    var path = pdb.GetString(document.Name).Replace('\\', '/');
    var offset = path.IndexOf("/src/Core/src/", StringComparison.Ordinal);
    if (offset < 0)
        continue;
    var relative = path[(offset + 1)..];
    if (!original.RootElement.TryGetProperty(relative, out var originalHash))
        throw new InvalidOperationException("Core source document was not in the verified source archive.");
    if (pdb.GetGuid(document.HashAlgorithm) != new Guid("8829d00f-11b8-4213-878b-770e8597ac16"))
        throw new InvalidOperationException("Source-document hash algorithm changed.");
    var file = Path.GetFullPath(Path.Combine(sourceRoot, relative));
    var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    if (!file.StartsWith(sourceRoot + Path.DirectorySeparatorChar, comparison))
        throw new InvalidOperationException("Source document escaped the verified source root.");
    var actualHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file)));
    var expectedHash = allowedChanges.GetValueOrDefault(relative, originalHash.GetString()!);
    if (!actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase) ||
        actualHash != Convert.ToHexString(pdb.GetBlobBytes(document.Hash)))
        throw new InvalidOperationException("Core source bytes changed before or during compilation.");
    handwritten++;
}
if (handwritten != expected.GetProperty("expected_handwritten_core_documents").GetInt32() ||
    pdb.Documents.Count != expected.GetProperty("expected_core_documents").GetInt32())
    throw new InvalidOperationException("Private Core source-document coverage changed.");
File.WriteAllText(args[5], JsonSerializer.Serialize(new
{
    CoreSHA256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[1]))),
    PdbSHA256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[2]))),
    HandwrittenCoreDocuments = handwritten,
    TotalDocuments = pdb.Documents.Count,
    AssemblyVersion = assembly.Version.ToString(),
    version.FileVersion,
    version.ProductVersion,
    CompilerProfileExact = true,
    AllCompiledHandwrittenSourceBytesBound = true,
    NoLibraryExecuted = true
}, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"Private Core source/identity proof passed for {handwritten} handwritten documents.");
