using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Security;
using Windows.Win32.System.Threading;

namespace ConPtyTermEmulatorLib
{
    /// <summary>
    /// Support for starting and configuring processes.
    /// </summary>
    public static class ProcessFactory
    {
        public class WrappedProcess : IDisposable
        {
            internal WrappedProcess(Process process) { this._process = process; }
            internal Process _process;
            public int Pid => (int)_process.ProcessInfo.dwProcessId;
            public System.Diagnostics.Process Process => _Process ??= System.Diagnostics.Process.GetProcessById(Pid);
            private System.Diagnostics.Process _Process;
            private bool IsDisposed;

            protected virtual void Dispose(bool disposing)
            {
                if (!IsDisposed)
                {
                    if (disposing)
                    {
                        _process.Dispose();
                    }
                    IsDisposed = true;
                }
            }

            public void Dispose()
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }
        /// <summary>
        /// Start and configure a process. The return value represents the process and should be disposed.
        /// </summary>
        public static WrappedProcess Start(string command, nuint attributes, PseudoConsole console, string workingDirectory)
        {
            var startupInfo = ConfigureProcessThread(console.Handle, attributes);
            var processInfo = RunProcess(ref startupInfo, command, workingDirectory);
            return new(new Process(startupInfo, processInfo));
        }

        unsafe private static STARTUPINFOEXW ConfigureProcessThread(PseudoConsole.ConPtyClosePseudoConsoleSafeHandle hPC, nuint attributes)
        {
            // this method implements the behavior described in https://docs.microsoft.com/en-us/windows/console/creating-a-pseudoconsole-session#preparing-for-creation-of-the-child-process

            nuint lpSize = 0;
            var success = PInvoke.InitializeProcThreadAttributeList(
                lpAttributeList: default,
                dwAttributeCount: 1,
                dwFlags: 0,
                lpSize: &lpSize
            );
            if (success || lpSize == 0) // we're not expecting `success` here, we just want to get the calculated lpSize
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not calculate the number of bytes for the attribute list.");
            }

            var startupInfo = new STARTUPINFOEXW();
            startupInfo.StartupInfo.cb = (uint)Marshal.SizeOf<STARTUPINFOEXW>();
            startupInfo.lpAttributeList = new LPPROC_THREAD_ATTRIBUTE_LIST((void*)Marshal.AllocHGlobal((int)lpSize));

            success = PInvoke.InitializeProcThreadAttributeList(
                lpAttributeList: startupInfo.lpAttributeList,
                dwAttributeCount: 1,
                dwFlags: 0,
                lpSize: &lpSize
            );
            if (!success)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not set up attribute list.");
            }

            success = PInvoke.UpdateProcThreadAttribute(
                lpAttributeList: startupInfo.lpAttributeList,
                dwFlags: 0,
                attributes,
                (void*)hPC.DangerousGetHandle(),
                (nuint)IntPtr.Size,
                null,
                (nuint*)null
            );
            if (!success)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not set pseudoconsole thread attribute.");
            }

            return startupInfo;
        }

        unsafe private static PROCESS_INFORMATION RunProcess(ref STARTUPINFOEXW sInfoEx, string commandLine, string workingDirectory)
        {
            uint securityAttributeSize = (uint)Marshal.SizeOf<SECURITY_ATTRIBUTES>();
            var pSec = new SECURITY_ATTRIBUTES { nLength = securityAttributeSize };
            var tSec = new SECURITY_ATTRIBUTES { nLength = securityAttributeSize };
            var info = sInfoEx;
            Span<char> spanChar = (commandLine + '\0').ToCharArray();

            var success = PInvoke.CreateProcess(
                null, // lpApplicationName
                ref spanChar, // lpCommandLine
                pSec, // lpProcessAttributes
                tSec, // lpThreadAttributes
                false, // bInheritHandles
                PROCESS_CREATION_FLAGS.EXTENDED_STARTUPINFO_PRESENT, // dwCreationFlags
                null, // lpEnvironment
                workingDirectory, // lpCurrentDirectory
                info.StartupInfo, // lpStartupInfo
                out var pInfo // lpProcessInformation
            );

            if (!success)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not create process.");

            return pInfo;

        }
    }
}
