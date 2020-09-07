using KeyIntegration_Connection.Orbita;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text;

namespace KeyIntegration_Connection
{
    class Program
    {
        static void Main(string[] args)
        {
            OrbitaIntegration();

            Console.ReadKey();
        }

        private static void SagaIntegration()
        {
            var port = 2323;
            var hostname = "127.0.0.1";

            using (var client = new TcpClient(hostname, port))
            using (var stream = client.GetStream())
            {
                var responseData = string.Empty;
                var dataToRead = new byte[256];
                var totalBytes = 0;

                //read - connection stablished
                totalBytes = stream.Read(dataToRead, 0, dataToRead.Length);
                responseData = System.Text.Encoding.ASCII.GetString(dataToRead, 0, totalBytes);
                Console.WriteLine("Received: {0}", responseData);

                var messageCheckin = "CIN|107|20200825|1642|20200826|1200|1|02|N|YouCheckin|";
                var messageCheckout = "COUT|105|20200825 1550|";

                var message = messageCheckin;
                var data = System.Text.Encoding.ASCII.GetBytes(message);

                stream.Write(data, 0, data.Length);
                Console.WriteLine("Sent: {0}", message);

                stream.Flush();
                //checkout doesnt receive messages
                totalBytes = stream.Read(dataToRead, 0, dataToRead.Length);

                responseData = System.Text.Encoding.ASCII.GetString(dataToRead, 0, totalBytes);
                Console.WriteLine("Received: {0}", responseData);

                //success: 0|Success
            }
        }

        delegate int InitDelegate(long Obj, long Obj2);
        delegate int AddDelegate(int x, int y);
        delegate short DvConnectDelegate(short beep);
        delegate int PassDelegate(ref IntPtr o1, ref IntPtr o2, ref IntPtr o3);

        private static void OrbitaIntegration()
        {
            var assemblyLoader = new CustomAssemblyLoadContext();
            var folder = Path.Combine("lib", "orbita");

            var dir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            #region Build_To_x64

            //var dcrf32LibPath = Path.Combine(dir, folder, "dcrf32.dll");
            //var clockLibPath = Path.Combine(dir, folder, "CLock.dll");
            //var sampleLibPath = Path.Combine(dir, folder, "DLLSample.dll");

            //var o1 = NativeMethods.LoadLibraryEx(sampleLibPath, IntPtr.Zero, 0);
            //var o2 = NativeMethods.LoadLibraryEx(clockLibPath, IntPtr.Zero, 0);

            //NativeLibrary.Free(o1); 
            //NativeLibrary.Free(o2); 

            //var f = NativeMethods.GetProcAddress(o1, "PassParameter");
            //var pass_parameter = (PassDelegate)Marshal.GetDelegateForFunctionPointer(f, typeof(PassDelegate));

            //var p1 = "paramter1";
            //var p2 = "baramter2";
            //var p3 = "paramter3";

            //var t1 = Marshal.StringToHGlobalAnsi(p1);
            //var t2 = Marshal.StringToHGlobalAnsi(p2);
            //var t3 = Marshal.StringToHGlobalAnsi(p3);

            //var k2 = pass_parameter(ref t1, ref t2, ref t3);

            //var r1 = Marshal.PtrToStringAnsi(t1);
            //var r2 = Marshal.PtrToStringAnsi(t2);
            //var r3 = Marshal.PtrToStringAnsi(t3);

            #endregion

            var x = OrbitaPmsEncoder.Connect(1);

            string auth1;
            var m = OrbitaPmsEncoder.GetAuthCode(out auth1);

            var auth = "962314";
            var building = "01"; //01 first building, 02 next, etc
            var room = "106";
            var commdoors = "00";
            var arrival = "2020-09-03 12:42:00";
            var departure = "2020-09-04 12:00:00";
            var z = OrbitaPmsEncoder.WriteCard(auth, building, room, commdoors, arrival, departure);

            byte[] cardno = new byte[256], building2 = new byte[256], room2 = new byte[256], commdoors2 = new byte[256], arrival2 = new byte[256], departure2 = new byte[256];
            var z2 = OrbitaPmsEncoder.ReadCard(auth, cardno, building2, room2, commdoors2, arrival2, departure2);

            var ret_cardno = Encoding.ASCII.GetString(cardno).Trim('\0');
            var ret_building = Encoding.ASCII.GetString(building2).Trim('\0');
            var ret_room = Encoding.ASCII.GetString(room2).Trim('\0');
            var ret_commdoors = Encoding.ASCII.GetString(commdoors2).Trim('\0');
            var ret_arrival = Encoding.ASCII.GetString(arrival2).Trim('\0');
            var ret_departure = Encoding.ASCII.GetString(departure2).Trim('\0');

            var p = OrbitaPmsEncoder.Disconnect();

            Console.WriteLine("finished");
        }
    }
}
