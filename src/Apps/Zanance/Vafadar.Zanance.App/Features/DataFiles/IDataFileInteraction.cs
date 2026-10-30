namespace Vafadar.Zanance.App.Features.DataFiles;

/// <summary>The original picked CSV name/content, obtained only through the platform picker.</summary>
public sealed record PickedDataFile(string Name, string Text);

/// <summary>Explicit native picker, authentication and cache/share effects; application import logic stays independent.</summary>
public interface IDataFileInteraction
{
    /// <summary>Reads the complete BOM-aware CSV within technical budgets; picker cancellation keeps the existing review.</summary>
    Task<PickedDataFile?> PickCsvAsync(string title);
    /// <summary>Retains the original picker result only while its original native owner remains current.</summary>
    async Task<PickedDataFile?> PickCsvAsync(string title, Action validateOwner, CancellationToken ct)
    { ct.ThrowIfCancellationRequested(); validateOwner(); var result = await PickCsvAsync(title); ct.ThrowIfCancellationRequested(); validateOwner(); return result; }
    /// <summary>Reads a bounded original bank CSV in its explicitly chosen strict encoding.</summary>
    Task<PickedDataFile?> PickBankCsvAsync(string title, Core.DataFiles.BankCsvEncoding encoding) => PickCsvAsync(title);
    /// <summary>Preserves original strict bank decoding under the original cancellable native owner.</summary>
    async Task<PickedDataFile?> PickBankCsvAsync(string title, Core.DataFiles.BankCsvEncoding encoding, Action validateOwner, CancellationToken ct)
    { ct.ThrowIfCancellationRequested(); validateOwner(); var result = await PickBankCsvAsync(title, encoding); ct.ThrowIfCancellationRequested(); validateOwner(); return result; }
    /// <summary>Requests the existing device/app-lock confirmation before an unencrypted export leaves the app.</summary>
    Task<bool> ConfirmExportAsync(string reason);
    /// <summary>Writes the original neutral-named CSV with UTF-8 BOM to app cache, then uses the native share surface.</summary>
    Task ShareCsvAsync(string fileName, string title, string content);
    /// <summary>Offers only an original owner-validated export; production also validates after asynchronous preparation.</summary>
    async Task ShareCsvAsync(string fileName, string title, string content, Action validateOwner, CancellationToken ct)
    { ct.ThrowIfCancellationRequested(); validateOwner(); await ShareCsvAsync(fileName, title, content); }
}
