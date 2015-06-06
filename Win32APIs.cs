using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace OpenNUI.Unity.Library
{
    static class Win32APIs
    {
        //For SharedMemory
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenFileMapping(MemoryMappedFileRights dwDesiredAccess,
                                              bool bInheritHandle,
                                              string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject,
                                            MemoryMappedFileRights dwDesiredAccess,
                                            long dwFileOffsetHigh,
                                            long dwFileOffsetLow,
                                            int dwNumberOfBytesToMap);
        [DllImport("Kernel32.dll")]
        public static extern bool UnmapViewOfFile(IntPtr map);

        [DllImport("kernel32.dll")]
        public static extern int CloseHandle(IntPtr hObject);

        [DllImport("msvcrt.dll", SetLastError = false)]
        public static extern IntPtr memcpy(IntPtr dest, IntPtr src, int count);

        //for IPC
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern SafeFileHandle CreateFile(
           String pipeName,
           uint dwDesiredAccess,
           uint dwShareMode,
           IntPtr lpSecurityAttributes,
           uint dwCreationDisposition,
           uint dwFlagsAndAttributes,
           IntPtr hTemplate);
    }
}
