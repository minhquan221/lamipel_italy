using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RegisterCertificate
{
    public class SocketClient
    {
        public static void StartClient()
        {
            // Data buffer for incoming data.
            byte[] bytes = new byte[1024];

            // Connect to a remote device.
            try
            {
                // Establish the remote endpoint for the socket.
                
                IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress ipAddress = IPAddress.Parse("210.211.113.131");
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, 1337);

                // Create a TCP/IP  socket.
                Socket sender = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect the socket to the remote endpoint. Catch any errors.
                try
                {
                    sender.Connect(remoteEP);

                    //Console.WriteLine("Socket connected to {0}", sender.RemoteEndPoint.ToString());

                    var privateKey = ConfigurationManager.AppSettings["privateKey"];
                    var macAddr = GetMacAddress();
                    var IpAddr = APIHelper.CallAPI("common/getip", null, new List<string[]> { new string[] { "PrivateKeyAuthen", privateKey } });
                    var MachineName = Environment.MachineName;
                    string encodePrivate = JoseHelper.Current.Sign(JsonConvert.SerializeObject(new
                    {
                        macAddress = macAddr,
                        IPInternet = IpAddr,
                        MachineName = MachineName,
                        PrivateKey = privateKey
                    }), privateKey);
                    string authenData = JsonConvert.SerializeObject(new {
                        type = "authen",
                        encode = encodePrivate,
                        privateKey = privateKey
                    });
                    // Encode the data string into a byte array.
                    byte[] msg = Encoding.ASCII.GetBytes(authenData);

                    // Send the data through the socket.
                    int bytesSent = sender.Send(msg);

                    // Receive the response from the remote device.
                    int bytesRec = sender.Receive(bytes);

                    Console.WriteLine("Echoed test = {0}", Encoding.ASCII.GetString(bytes, 0, bytesRec));

                    // Release the socket.
                    //sender.Shutdown(SocketShutdown.Both);
                    //sender.Close();

                }
                catch (ArgumentNullException ane)
                {
                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                }
                catch (SocketException se)
                {
                    Console.WriteLine("SocketException : {0}", se.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static string GetMacAddress()
        {
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapterConfiguration where IPEnabled=true");
                IEnumerable<ManagementObject> objects = searcher.Get().Cast<ManagementObject>();
                string mac = (from o in objects orderby o["IPConnectionMetric"] select o["MACAddress"].ToString()).FirstOrDefault();
                return mac;
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }
    }
}
