using System;
using Windows.Win32;
using Windows.Win32.System.Threading;

namespace ConPtyTermEmulatorLib
{
    /// <summary>
    /// Represents an instance of a process.
    /// </summary>
    internal sealed class Process : IDisposable
    {
        public Process(STARTUPINFOEXW startupInfo, PROCESS_INFORMATION processInfo)
        {
            StartupInfo = startupInfo;
            ProcessInfo = processInfo;
        }

        public STARTUPINFOEXW StartupInfo { get; }
        public PROCESS_INFORMATION ProcessInfo { get; }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects).
                }

                // dispose unmanaged state

                // Free the attribute list
                if (StartupInfo.lpAttributeList != default)
                {
                    PInvoke.DeleteProcThreadAttributeList(StartupInfo.lpAttributeList);
                }

                // Close process and thread handles
                if (ProcessInfo.hProcess != IntPtr.Zero)
                {
                    PInvoke.CloseHandle(ProcessInfo.hProcess);
                }
                if (ProcessInfo.hThread != IntPtr.Zero)
                {
                    PInvoke.CloseHandle(ProcessInfo.hThread);
                }

                disposedValue = true;
            }
        }

        ~Process()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
