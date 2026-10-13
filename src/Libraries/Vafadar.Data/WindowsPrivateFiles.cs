using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Vafadar.Data;

/// <summary>Owns native no-follow Windows handles in one captured local private directory.</summary>
/// <remarks>Checks actual handle attributes/names, refuses redirected ancestors and keeps the directory
/// non-deletable while its operation runs. This is not a defence against privileged or same-user code.</remarks>
[SupportedOSPlatform("windows")]
internal sealed class WindowsPrivateFiles : IDisposable
{
    private const uint Read = 0x80000000, Write = 0x40000000, OpenExisting = 3;
    private const uint NoFollow = 0x00200000, BackupSemantics = 0x02000000;
    private readonly string directory;
    private readonly SafeFileHandle root;

    /// <summary>Captures an existing local directory through a validated native handle.</summary>
    internal WindowsPrivateFiles(string directory)
    {
        if (!Path.IsPathFullyQualified(directory) || directory.StartsWith("\\\\", StringComparison.Ordinal))
            throw new IOException("Private storage requires a local absolute directory.");
        this.directory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
        for (var path = this.directory; path is not null; path = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(path)))
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Private storage ancestry is redirected.");
        root = CreateFileW(this.directory, Read | Write, 3, IntPtr.Zero, OpenExisting, NoFollow | BackupSemantics, IntPtr.Zero);
        try { RequireHandle(root, this.directory, isDirectory: true); }
        catch { root.Dispose(); throw; }
    }

    /// <summary>Opens one exact non-linked regular file without following a final reparse point.</summary>
    internal SafeFileHandle OpenFile(string path, bool writable)
    {
        RequireChild(path);
        var handle = CreateFileW(path, Read | (writable ? Write : 0), 1, IntPtr.Zero, OpenExisting, NoFollow, IntPtr.Zero);
        try { RequireHandle(handle, path, isDirectory: false); return handle; }
        catch { handle.Dispose(); throw; }
    }

    /// <summary>Opens the exact regular file with DELETE authority while denying concurrent write/rename sharing.</summary>
    internal SafeFileHandle OpenForDeletion(string path)
    {
        RequireChild(path);
        var handle = CreateFileW(path, Read | 0x00010000, 1, IntPtr.Zero, OpenExisting, NoFollow, IntPtr.Zero);
        try { RequireHandle(handle, path, isDirectory: false); return handle; }
        catch { handle.Dispose(); throw; }
    }

    /// <summary>Reads native volume/file identity from the already verified handle.</summary>
    internal static string FileIdentity(SafeFileHandle handle)
    {
        if (!GetFileInformationByHandle(handle, out var info)) throw Error("Private file identity unavailable.");
        return $"{info.Volume:x8}{info.IndexHigh:x8}{info.IndexLow:x8}";
    }

    /// <summary>Reads the captured directory's native volume/file identity.</summary>
    internal string DirectoryIdentity => FileIdentity(root);

    /// <summary>Marks this same verified handle for deletion on close; never reopens an unchecked filename.</summary>
    internal static void DeleteOnClose(SafeFileHandle handle)
    {
        // Win32 FILE_DISPOSITION_INFO contains one BOOLEAN, and its handle requires DELETE access.
        byte delete = 1;
        if (!SetFileInformationByHandle(handle, 4, ref delete, 1)) throw Error("Private handle deletion failed.");
    }

    /// <summary>Creates an immutable final entry with an owner-only protected ACL from its first byte.</summary>
    internal SafeFileHandle CreateNewFile(string path)
    {
        RequireChild(path);
        using var identity = WindowsIdentity.GetCurrent();
        var user = identity.User?.Value ?? throw new IOException("Current user identity unavailable.");
        if (!ConvertStringSecurityDescriptorToSecurityDescriptorW($"O:{user}D:P(A;;FA;;;{user})", 1, out var descriptor, out _))
            throw Error("Private file ACL creation failed.");
        var attributes = new SecurityAttributes { Length = Marshal.SizeOf<SecurityAttributes>(), Descriptor = descriptor };
        var pointer = Marshal.AllocHGlobal(attributes.Length);
        try
        {
            Marshal.StructureToPtr(attributes, pointer, false);
            var handle = CreateFileW(path, Read | Write, 0, pointer, 1, NoFollow | 0x80000000, IntPtr.Zero);
            try { RequireHandle(handle, path, isDirectory: false); return handle; }
            catch { handle.Dispose(); throw; }
        }
        finally { Marshal.FreeHGlobal(pointer); _ = LocalFree(descriptor); }
    }

    /// <summary>Flushes the actual directory handle; unsupported or denied effects fail instead of reporting success.</summary>
    internal void FlushDirectory()
    {
        RequireHandle(root, directory, isDirectory: true);
        if (!FlushFileBuffers(root)) throw Error("Private directory flush failed.");
    }

    /// <summary>Flushes an actual regular file handle, with errors propagated.</summary>
    internal static void FlushFile(SafeFileHandle handle)
    { if (!FlushFileBuffers(handle)) throw Error("Private file flush failed."); }

    /// <inheritdoc />
    public void Dispose() => root.Dispose();

    private void RequireChild(string path)
    {
        RequireHandle(root, directory, isDirectory: true);
        if (!Path.IsPathFullyQualified(path) || !string.Equals(Path.GetDirectoryName(Path.GetFullPath(path)), directory, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Private file belongs to another directory.");
    }

    private static void RequireHandle(SafeFileHandle handle, string path, bool isDirectory)
    {
        if (handle.IsInvalid || !GetFileInformationByHandle(handle, out var info)) throw Error("Private handle unavailable.");
        var attributes = (FileAttributes)info.Attributes;
        if ((attributes & FileAttributes.ReparsePoint) != 0 || ((attributes & FileAttributes.Directory) != 0) != isDirectory
            || (!isDirectory && info.Links != 1)) throw new IOException("Private handle is redirected or linked.");
        var name = new StringBuilder(32768);
        var length = GetFinalPathNameByHandleW(handle, name, (uint)name.Capacity, 0);
        if (length == 0 || length >= name.Capacity) throw Error("Private handle identity unavailable.");
        var actual = name.ToString();
        if (actual.StartsWith("\\\\?\\", StringComparison.Ordinal)) actual = actual[4..];
        if (!string.Equals(Path.TrimEndingDirectorySeparator(actual), Path.TrimEndingDirectorySeparator(Path.GetFullPath(path)), StringComparison.OrdinalIgnoreCase))
            throw new IOException("Private handle does not match its captured path.");
    }

    private static IOException Error(string message) => new(message, new Win32Exception(Marshal.GetLastWin32Error()));
    /// <summary>Exact DWORD/FILETIME layout of Win32 BY_HANDLE_FILE_INFORMATION; FILETIME consists of two DWORDs.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct FileInformation
    {
        /// <summary>Native attributes and timestamp words.</summary>
        internal uint Attributes, CreationLow, CreationHigh, AccessLow, AccessHigh, WriteLow, WriteHigh;
        /// <summary>Native volume, size, hard-link count and file-index words.</summary>
        internal uint Volume, SizeHigh, SizeLow, Links, IndexHigh, IndexLow;
    }
    /// <summary>Native SECURITY_ATTRIBUTES layout used only while creating the owner-restricted file.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct SecurityAttributes
    {
        /// <summary>Native structure length in bytes.</summary>
        internal int Length;
        /// <summary>Temporary native security descriptor, released with LocalFree.</summary>
        internal IntPtr Descriptor;
        /// <summary>Zero forbids handle inheritance by child processes.</summary>
        internal int Inherit;
    }
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFileW(string path, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFileInformationByHandle(SafeFileHandle handle, out FileInformation information);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetFinalPathNameByHandleW(SafeFileHandle handle, StringBuilder name, uint length, uint flags);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FlushFileBuffers(SafeFileHandle handle);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetFileInformationByHandle(SafeFileHandle handle, int informationClass, ref byte information, uint length);
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ConvertStringSecurityDescriptorToSecurityDescriptorW(string text, uint version, out IntPtr descriptor, out uint size);
    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr memory);
}
