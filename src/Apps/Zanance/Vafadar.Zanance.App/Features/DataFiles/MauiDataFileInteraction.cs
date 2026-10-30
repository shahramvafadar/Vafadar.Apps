using Vafadar.Zanance.App.Security;

namespace Vafadar.Zanance.App.Features.DataFiles;

/// <summary>Preserves the existing in-context picker, app-lock confirmation and native unencrypted CSV share.</summary>
internal sealed class MauiDataFileInteraction(AppLockService appLock) : IDataFileInteraction
{
    /// <inheritdoc />
    public Task<PickedDataFile?> PickCsvAsync(string title) => PickCsvAsync(title, static () => { }, CancellationToken.None);
    /// <inheritdoc />
    public async Task<PickedDataFile?> PickCsvAsync(string title, Action validateOwner, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested(); validateOwner();
        var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = title });
        ct.ThrowIfCancellationRequested(); validateOwner();
        if (file is null) return null;
        // D-385: providers may finish reads synchronously; keep stream opening/decoding off the window thread.
        return await Task.Run(async () =>
        {
            await using var stream = await file.OpenReadAsync();
            ct.ThrowIfCancellationRequested(); validateOwner();
            var content = await Core.DataFiles.CsvInput.ReadTextAsync(stream, cancellationToken: ct);
            ct.ThrowIfCancellationRequested(); validateOwner(); return new PickedDataFile(file.FileName, content);
        }, ct);
    }
    /// <inheritdoc />
    public Task<bool> ConfirmExportAsync(string reason) => appLock.ConfirmAsync(reason);
    /// <inheritdoc />
    public Task<PickedDataFile?> PickBankCsvAsync(string title, Core.DataFiles.BankCsvEncoding encoding) =>
        PickBankCsvAsync(title, encoding, static () => { }, CancellationToken.None);
    /// <inheritdoc />
    public async Task<PickedDataFile?> PickBankCsvAsync(string title, Core.DataFiles.BankCsvEncoding encoding, Action validateOwner, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested(); validateOwner();
        var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = title });
        ct.ThrowIfCancellationRequested(); validateOwner();
        if (file is null) return null;
        await using var source = await file.OpenReadAsync(); using var buffer = new MemoryStream();
        var bytes = new byte[65536]; int count;
        while ((count = await source.ReadAsync(bytes, ct)) != 0)
        {
            validateOwner();
            if (buffer.Length + count > Core.DataFiles.StarMoneyCsv.MaximumBytes) throw new FormatException("The bank file exceeds its safe bound.");
            await buffer.WriteAsync(bytes.AsMemory(0, count), ct);
        }
        // Strict decoding can be synchronous even when the platform stream completed asynchronously.
        var content = buffer.ToArray();
        var text = await Task.Run(() => Core.DataFiles.StarMoneyCsv.Decode(content, encoding), ct);
        ct.ThrowIfCancellationRequested(); validateOwner(); return new(file.FileName, text);
    }
    /// <inheritdoc />
    public Task ShareCsvAsync(string fileName, string title, string content) =>
        ShareCsvAsync(fileName, title, content, static () => { }, CancellationToken.None);
    /// <inheritdoc />
    public async Task ShareCsvAsync(string fileName, string title, string content, Action validateOwner, CancellationToken ct)
    {
        void Current()
        { ct.ThrowIfCancellationRequested(); validateOwner(); if (appLock.IsLocked) throw new InvalidOperationException("The original CSV export is covered."); }
        var prepared = await CsvSharePreparation.CreateAsync(FileSystem.CacheDirectory, fileName, content, Current, ct);
        try
        {
            Current(); prepared.MarkOffered();
            await Share.Default.RequestAsync(new ShareFileRequest { Title = title, File = new ShareFile(prepared.Path, "text/csv") });
        }
        finally { prepared.DiscardUnoffered(); }
    }
}
