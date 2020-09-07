using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace KeyIntegration_Connection.Orbita
{
    public class OrbitaDcrf32
    {
        private const string dcrl32LibPath = @"lib\orbita\dcrf32.dll";

        [DllImport(dcrl32LibPath, EntryPoint = "dc_init")]
        public static extern IntPtr Init(short p1, int p2);

        [DllImport(dcrl32LibPath, EntryPoint = "dc_init_485")]
        public static extern void Init485(short p1);

        [DllImport(dcrl32LibPath, EntryPoint = "dc_beep")]
        public static extern IntPtr Beep(IntPtr ptr, int p2);
    }
}
