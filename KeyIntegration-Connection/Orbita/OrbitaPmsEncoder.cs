using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace KeyIntegration_Connection.Orbita
{
    public class OrbitaPmsEncoder
    {
        private const string clockLibPath = @"lib\orbita\CLock.dll";

        [DllImport(clockLibPath, EntryPoint = "dv_connect")]
        public static extern short Connect(short beep);

        [DllImport(clockLibPath, EntryPoint = "dv_disconnect")]
        public static extern short Disconnect();

        [DllImport(clockLibPath, EntryPoint = "dv_check_card")]
        public static extern short CheckCard();

        [DllImport(clockLibPath, EntryPoint = "dv_write_card", CharSet = CharSet.Ansi)]
        public static extern short WriteCard(string auth, string building, string room, string commdoors, string arrival, string departure);

        [DllImport(clockLibPath, EntryPoint = "dv_read_card", CharSet = CharSet.Ansi)]
        public static extern short ReadCard(string auth,
            byte[] cardno,
            byte[] building, 
            byte[] room, 
            byte[] commdoors, 
            byte[] arrival, 
            byte[] departure);

        [DllImport(clockLibPath, EntryPoint = "dv_get_auth_code", CharSet = CharSet.Ansi)]
        public static extern short GetAuthCode([MarshalAs(UnmanagedType.LPStr), Out()] out string auth);
    }
}
