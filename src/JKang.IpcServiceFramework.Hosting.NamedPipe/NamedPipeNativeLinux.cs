using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace JKang.IpcServiceFramework.Hosting.NamedPipe
{
    internal static class NamedPipeNativeLinux
    {
        #region I/O Flags
            
        private const int O_ACCMODE = 0003;
        private const int O_RDONLY = 00;
        private const int O_WRONLY = 01;
        private const int O_RDWR = 02;
        private const int O_CREAT = 0100;
        private const int O_EXCL = 0200;
        private const int O_NOCTTY = 0400;
        private const int O_TRUNC = 01000;
        private const int O_APPEND = 02000;
        private const int O_NONBLOCK = 04000;
        private const int O_SYNC = 04010000;
        private const int O_ASYNC = 020000;
        private const int O_LARGEFILE = 0100000;
        private const int O_DIRECTORY = 0200000;
        private const int O_NOFOLLOW = 0400000;
        private const int O_CLOEXEC = 02000000;
        private const int O_DIRECT = 040000;
        private const int O_NOATIME = 01000000;
        private const int O_PATH = 010000000;
        private const int O_DSYNC = 010000;
        private const int O_TMPFILE = 020000000 | O_DIRECTORY;

        #endregion
        
        #region Pipe Methods

        private const string glibc = "libc";
            
        [DllImport(glibc, CharSet = CharSet.Unicode)]
        internal static extern int mkfifo(
            string pathname,
            uint mode);

        [DllImport(glibc, CharSet = CharSet.Unicode)]
        internal static extern int open(
            string pathname,
            int flags
        );

        [DllImport(glibc, CharSet = CharSet.Unicode)]
        private static extern string strerror(int errnum);

        /// <summary>
        /// Helper to create a Linux FIFO (NamedPipe) via native API.
        /// </summary>
        /// <param name="pipeName">Named pipe core name.</param>
        /// <param name="mode">The file creation mode (permissions). Refer to CHMOD(2) manpage. (ex: 0666 is rw-rw-rw-)</param>
        /// <returns>NamedPipeServerStream</returns>
        internal static NamedPipeServerStream CreateNamedPipe(
            string pipeName, 
            uint mode)
        {
            // https://github.com/dotnet/runtime/blob/4a81845e09718353f02d5d0896d418a4a6c593d8/src/libraries/System.IO.Pipes/src/System/IO/Pipes/PipeStream.Unix.cs#L67
            // This provides a full path to the location that pipes are created by the .NET library.
            string s_pipePrefix = Path.Combine(Path.GetTempPath(), "CoreFxPipe_");
            string fullPath = Path.IsPathRooted(pipeName) ? pipeName : s_pipePrefix + pipeName;
            
            // Create the named pipe with native methods
            if (mkfifo(fullPath, mode) == 0)
            {
                // Open the file returning a handle
                int handle = open(fullPath, O_RDWR | O_NONBLOCK | O_ASYNC);
                if (handle != -1)
                {
                    var pipeHandle = new SafePipeHandle(new IntPtr(handle), true);
                    try 
                    {
                        return new NamedPipeServerStream(PipeDirection.InOut, true, false, pipeHandle);
                    }
                    catch (Exception)
                    {
                        pipeHandle.Dispose();
                        throw;
                    }
                }
                else
                {
                    int errno_open = Marshal.GetLastWin32Error();
                    throw new IOException("Failed to open named pipe: " + strerror(errno_open));
                }
            }
            else
            {
                var errno_creat = Marshal.GetLastWin32Error();
                throw new IOException("Failed to create named pipe: " + strerror(errno_creat));
            }
        }

        #endregion
    }
}