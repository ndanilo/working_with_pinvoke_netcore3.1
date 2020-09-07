using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace KeyIntegration_Connection
{
    internal static class NativeMethods
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr LoadLibraryEx([MarshalAs(UnmanagedType.LPWStr), In()] string fileName, IntPtr intPtr, UInt32 flags);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern Int32 FreeLibrary(IntPtr handle);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
        public static extern IntPtr GetProcAddress([In()] IntPtr dllPtr, [MarshalAs(UnmanagedType.LPStr), In()] string procName);
    }
}
