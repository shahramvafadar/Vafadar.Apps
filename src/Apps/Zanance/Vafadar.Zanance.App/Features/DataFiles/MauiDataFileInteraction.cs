using System.Text;
using Vafadar.Zanance.App.Security;

namespace Vafadar.Zanance.App.Features.DataFiles;

/// <summary>Preserves the existing in-context picker, app-lock confirmation and native unencrypted CSV share.</summary>
internal sealed class MauiDataFileInteraction(AppLockService appLock) : IDataFileInteraction
{
    /// <inheritdoc />
    public async Task<PickedDataFile?> PickCsvAsync(string title)
    {
        var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = title });
        if (file is null) return null;
        await using var stream = await file.OpenReadAsync();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return new(file.FileName, await reader.ReadToEndAsync());
    }
    /// <inheritdoc />
    public Task<bool> ConfirmExportAsync(string reason) => appLock.ConfirmAsync(reason);
    /// <inheritdoc />
    public async Task ShareCsvAsync(string fileName, string title, string content)
    {
        if (Path.GetFileName(fileName) != fileName || !fileName.StartsWith("zanance-", StringComparison.Ordinal)
            || !fileName.EndsWith(".csv", StringComparison.Ordinal)) throw new ArgumentException("A neutral owned CSV file name is required.", nameof(fileName));
        var path = Path.Combine(FileSystem.CacheDirectory, fileName);
        await File.WriteAllTextAsync(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        await Share.Default.RequestAsync(new ShareFileRequest { Title = title, File = new ShareFile(path, "text/csv") });
    }
}
