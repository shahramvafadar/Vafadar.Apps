using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Vafadar.Build
{
    /// <summary>Owns one draft process on a temporary desktop that is never switched to the input display.</summary>
    public sealed class StoreDraftProcess : IDisposable
    {
        private IntPtr desktop;

        /// <summary>Gets the exact original process, retaining a handle for completion and exit-code observation.</summary>
        public Process Process { get; private set; }

        /// <summary>Gets the separate native desktop assigned before the application can create a window.</summary>
        public string DesktopName { get; private set; }

        /// <summary>Reads only the input desktop's name for isolation evidence; never switches or captures it.</summary>
        public static string ReadInputDesktopName()
        {
            IntPtr input = OpenInputDesktop(0, false, 1);
            if (input == IntPtr.Zero) return "unavailable";
            try
            {
                var name = new StringBuilder(256);
                uint needed;
                return GetUserObjectInformationW(input, 2, name, 512, out needed) ? name.ToString() : "unavailable";
            }
            finally { CloseDesktop(input); }
        }

        private StoreDraftProcess(IntPtr desktop, Process process, string name)
        {
            this.desktop = desktop;
            Process = process;
            DesktopName = name;
        }

        /// <summary>Starts the verified application directly, without a shell, on a fresh non-input desktop.</summary>
        public static StoreDraftProcess Start(string executable, string directory, string run)
        {
            Guid parsed;
            if (!Guid.TryParseExact(run, "N", out parsed)) throw new ArgumentException("Invalid draft run.");
            string name = "ZananceStore_" + run;
            // Deliberately omit DESKTOP_SWITCHDESKTOP (0x100); no switching API is imported.
            IntPtr desktop = CreateDesktopW(name, null, IntPtr.Zero, 0, 0x00cf, IntPtr.Zero);
            if (desktop == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
            var startup = new StartupInfo();
            startup.Size = Marshal.SizeOf(typeof(StartupInfo));
            startup.Desktop = "WinSta0\\" + name;
            startup.Flags = 0x81; // STARTF_USESHOWWINDOW and STARTF_FORCEOFFFEEDBACK.
            startup.ShowWindow = 0;
            ProcessInformation information;
            if (!CreateProcessW(executable, new StringBuilder("\"" + executable + "\""), IntPtr.Zero,
                IntPtr.Zero, false, 0, IntPtr.Zero, directory, ref startup, out information))
            {
                int error = Marshal.GetLastWin32Error();
                CloseDesktop(desktop);
                throw new Win32Exception(error);
            }
            try
            {
                var process = System.Diagnostics.Process.GetProcessById((int)information.ProcessId);
                // Open its retained managed handle before releasing CreateProcess's original native handles.
                if (process.Handle == IntPtr.Zero) throw new InvalidOperationException("Original draft process handle unavailable.");
                return new StoreDraftProcess(desktop, process, name);
            }
            catch { CloseDesktop(desktop); throw; }
            finally { CloseHandle(information.Thread); CloseHandle(information.Process); }
        }

        /// <summary>Releases this observer's desktop reference; never terminates a live process after a timeout.</summary>
        public void Dispose()
        {
            if (desktop != IntPtr.Zero) { CloseDesktop(desktop); desktop = IntPtr.Zero; }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct StartupInfo
        {
            public int Size;
            public string Reserved, Desktop, Title;
            public uint X, Y, XSize, YSize, XChars, YChars, Fill, Flags;
            public ushort ShowWindow, ReservedBytes;
            public IntPtr ReservedPointer, Input, Output, Error;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ProcessInformation
        {
            public IntPtr Process, Thread;
            public uint ProcessId, ThreadId;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateDesktopW(string name, string device, IntPtr mode, uint flags, uint access, IntPtr security);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseDesktop(IntPtr desktop);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr OpenInputDesktop(uint flags, bool inherit, uint access);
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool GetUserObjectInformationW(IntPtr desktop, int index, StringBuilder name, uint length, out uint needed);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CreateProcessW(string application, StringBuilder command, IntPtr processSecurity,
            IntPtr threadSecurity, bool inheritHandles, uint flags, IntPtr environment, string directory,
            ref StartupInfo startup, out ProcessInformation information);
        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr handle);
    }
}
