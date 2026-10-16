using System.Security.Cryptography;
using Vafadar.Backup.Security;

namespace Vafadar.Zanance.App.Interaction;

/// <summary>Explicit native document effects for small password-encrypted kits; no readable cache file, persisted URI or new storage permission.</summary>
internal static class HouseholdRecoveryFiles
{
    /// <summary>Actual qualified adapter targets; iOS Save remains a separate native Mac implementation/acceptance gate.</summary>
    internal static bool Available => OperatingSystem.IsAndroid() || OperatingSystem.IsWindows();

    /// <summary>Reads at most one bounded user-selected file, without creating a draft or keeping a plaintext password/key.</summary>
    internal static async Task<byte[]?> PickAsync(string title, CancellationToken ct)
    {
        var selected = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = title });
        ct.ThrowIfCancellationRequested(); if (selected is null) return null;
        await using var stream = await selected.OpenReadAsync(); using var buffer = new MemoryStream();
        var piece = new byte[4096];
        try
        {
            while (true)
            {
                var read = await stream.ReadAsync(piece, ct); if (read == 0) break;
                if (buffer.Length > 32768 - read) throw new InvalidDataException("The selected recovery file exceeds its bound.");
                await buffer.WriteAsync(piece.AsMemory(0, read), ct);
            }
            if (!BackupEncryption.IsEncrypted(buffer.GetBuffer().AsSpan(0, checked((int)buffer.Length))))
                throw new InvalidDataException("An encrypted household recovery file is required.");
            return buffer.ToArray();
        }
        finally { CryptographicOperations.ZeroMemory(piece); CryptographicOperations.ZeroMemory(buffer.GetBuffer()); }
    }

    /// <summary>Uses a user-selected native destination and returns only after its actual encrypted write/close; never substitutes a Share dismissal for a successful Save.</summary>
    internal static async Task<bool> SaveAsync(string fileName, byte[] encrypted, CancellationToken ct)
    {
        if (Path.GetFileName(fileName) != fileName || !fileName.StartsWith("zanance-household-recovery-", StringComparison.Ordinal) ||
            !fileName.EndsWith(".zhr", StringComparison.Ordinal) || encrypted.Length is < 64 or > 32768 || !BackupEncryption.IsEncrypted(encrypted))
            throw new ArgumentException("A bounded encrypted household recovery export is required.");
        ct.ThrowIfCancellationRequested();
#if ANDROID
        return await AndroidHouseholdRecoverySave.SaveAsync(fileName, encrypted, ct);
#elif WINDOWS
        var window = Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView as Microsoft.UI.Xaml.Window
            ?? throw new InvalidOperationException("The current application window is unavailable.");
        var picker = new Windows.Storage.Pickers.FileSavePicker { SuggestedFileName = Path.GetFileNameWithoutExtension(fileName), DefaultFileExtension = ".zhr" };
        picker.FileTypeChoices.Add("Zanance", new List<string> { ".zhr" });
        WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(window));
        var file = await picker.PickSaveFileAsync(); ct.ThrowIfCancellationRequested(); if (file is null) return false;
        using (var native = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
        {
            native.Size = 0;
            using var stream = System.IO.WindowsRuntimeStreamExtensions.AsStreamForWrite(native);
            await stream.WriteAsync(encrypted, ct); await stream.FlushAsync(ct);
        }
        ct.ThrowIfCancellationRequested(); return true;
#else
        await Task.CompletedTask; throw new PlatformNotSupportedException("Native recovery-file Save is not yet available on this platform.");
#endif
    }
}
