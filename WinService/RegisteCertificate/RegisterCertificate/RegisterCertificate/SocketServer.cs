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
    public class SocketServer
    {
        public static string data = null;

        public static void StartServer()
        {
            byte[] bytes = new Byte[1024];
            try
            {
                IPAddress ipAddress = IPAddress.Parse("127.0.0.1");

                //Console.WriteLine("Starting TCP listener...");

                TcpListener listener = new TcpListener(ipAddress, 8080);

                listener.Start();
                while (true)
                {
                    Socket client = listener.AcceptSocket();
                    data = null;
                    //Console.WriteLine("Connection accepted.");

                    var childSocketThread = new Thread(() =>
                    {
                        bytes = new byte[100];
                        int size = client.Receive(bytes);

                        int bytesRec = client.Receive(bytes);
                        data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                        var objectReceived = JsonConvert.DeserializeObject<SocketData>(data);
                        bool result = false;
                        switch (objectReceived.type)
                        {
                            case "authen":
                                var privateKey = ConfigurationManager.AppSettings["privateKey"];
                                if (objectReceived.privateKey == privateKey)
                                {
                                    result = true;
                                }
                                break;
                            case "reconnect":
                                break;
                            case "":
                                break;
                            default:
                                break;
                        }
                        if (result)
                        {
                            client.Send(Encoding.ASCII.GetBytes("true"));
                        }
                        client.Close();
                    });
                    childSocketThread.Start();
                }

                listener.Stop();
            }
            catch (Exception ex)
            {
                File.WriteAllText(ConfigurationManager.AppSettings["PathTraceError"] + "log_error_" + DateTime.Now.ToString("yyyyMMdd_HHmmssttt"), DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + " - " + ex.ToString());
            }
        }


    }
}
