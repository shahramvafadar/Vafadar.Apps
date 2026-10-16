namespace Vafadar.Zanance.App.Features.DataFiles;

/// <summary>The original picked CSV name/content, obtained only through the platform picker.</summary>
public sealed record PickedDataFile(string Name, string Text);

/// <summary>Explicit native picker, authentication and cache/share effects; application import logic stays independent.</summary>
public interface IDataFileInteraction
{
    /// <summary>Reads the selected UTF-8 CSV; cancellation returns null without discarding an existing review.</summary>
    Task<PickedDataFile?> PickCsvAsync(string title);
    /// <summary>Reads a bounded original bank CSV in its explicitly chosen strict encoding.</summary>
    Task<PickedDataFile?> PickBankCsvAsync(string title, Core.DataFiles.BankCsvEncoding encoding) => PickCsvAsync(title);
    /// <summary>Requests the existing device/app-lock confirmation before an unencrypted export leaves the app.</summary>
    Task<bool> ConfirmExportAsync(string reason);
    /// <summary>Writes the original neutral-named CSV with UTF-8 BOM to app cache, then uses the native share surface.</summary>
    Task ShareCsvAsync(string fileName, string title, string content);
}
