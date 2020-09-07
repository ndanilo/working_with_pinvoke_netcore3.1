using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text;

namespace KeyIntegration_Connection
{
    internal class CustomAssemblyLoadContext : AssemblyLoadContext
    {
        public IntPtr LoadUnmanagedLibrary(string absolutePath)
        {
            return LoadUnmanagedDll(absolutePath);
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, ThrowOnUnmappableChar = true, PreserveSig = true)]
        public static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);
    }
}
