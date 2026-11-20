using System.Security.Cryptography;
using Vafadar.Zanance.App.Security;

namespace Vafadar.Zanance.App.Features.DataFiles;

/// <summary>Preserves the existing in-context picker, app-lock confirmation and native unencrypted CSV share.</summary>
internal sealed class MauiDataFileInteraction(AppLockService appLock) : IDataFileInteraction
{
    /// <inheritdoc />
    public Task<PickedDataFile?> PickCsvAsync(string title) => PickCsvAsync(title, static () => { }, CancellationToken.None);
    /// <inheritdoc />
    public Task<PickedDataFile?> PickCsvAsync(string title, Action validateOwner, CancellationToken ct) =>
        PickAsync(title, (source, token) => Core.DataFiles.CsvInput.ReadTextAsync(source, cancellationToken: token), validateOwner, ct);
    /// <inheritdoc />
    public Task<bool> ConfirmExportAsync(string reason) => appLock.ConfirmAsync(reason);
    /// <inheritdoc />
    public Task<PickedDataFile?> PickBankCsvAsync(string title, Core.DataFiles.BankCsvEncoding encoding) =>
        PickBankCsvAsync(title, encoding, static () => { }, CancellationToken.None);
    /// <inheritdoc />
    public Task<PickedDataFile?> PickBankCsvAsync(string title, Core.DataFiles.BankCsvEncoding encoding, Action validateOwner, CancellationToken ct) =>
        PickAsync(title, (source, token) => ReadBankCsvAsync(source, encoding, validateOwner, token), validateOwner, ct);

    private static async Task<PickedDataFile?> PickAsync(string title, Func<Stream, CancellationToken, Task<string>> read,
        Action validateOwner, CancellationToken ct)
    {
        void Current() { ct.ThrowIfCancellationRequested(); validateOwner(); }
        async Task<PickedDataFile> ReadAsync(Stream input, string name, CancellationToken readToken)
        {
            Current();
            var text = await read(input, readToken);
            readToken.ThrowIfCancellationRequested();
            Current();
            return new(name, text);
        }

        Current();
        // D-385: small provider reads and decoding can finish synchronously.
#if ANDROID
        PickedDataFile? picked = null;
        // An ordinary bank export can have generic MIME metadata; only constrain virtual conversions.
        await AndroidDocumentPicker.UseAsync(title, null, async (input, name, _, lifetime) =>
        {
            picked = await Task.Run(() => ReadAsync(input, name, lifetime), lifetime);
        }, Current, virtualMimeTypes: ["text/csv", "text/comma-separated-values"], ct: ct);
        Current();
        return picked;
#else
        var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = title });
        Current();
        if (file is null) return null;
        return await Task.Run(async () =>
        {
            await using var input = await file.OpenReadAsync();
            return await ReadAsync(input, file.FileName, ct);
        }, ct);
#endif
    }

    private static async Task<string> ReadBankCsvAsync(Stream source, Core.DataFiles.BankCsvEncoding encoding,
        Action validateOwner, CancellationToken ct)
    {
        var bytes = await BoundedPrivateFileReader.ReadAsync(source, Core.DataFiles.StarMoneyCsv.MaximumBytes, ct,
            static () => new FormatException("The bank file exceeds its safe bound."), validateOwner);
        try
        {
            ct.ThrowIfCancellationRequested();
            validateOwner();
            return Core.DataFiles.StarMoneyCsv.Decode(bytes, encoding);
        }
        finally { CryptographicOperations.ZeroMemory(bytes); }
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
