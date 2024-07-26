using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Web;
using System.Windows;

namespace V2boardApi.Tools
{
    public static class Network
    {
        public static string ScanPort(string Address)
        {
            string hostname = Address;
             int portno = 3306;
            try
            {
                IPAddress ipa = (IPAddress)Dns.GetHostAddresses(hostname)[0];
                try
                {
                    System.Net.Sockets.Socket sock =
                            new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork,
                                                          System.Net.Sockets.SocketType.Stream,
                                                          System.Net.Sockets.ProtocolType.Tcp
                                                         );
                    sock.Connect(ipa, portno);
                    PingHost(hostname, portno);

                    if (sock.Connected == true) // Port is in use and connection is successful
                        return "پورت مقصد از سمت فایروال مجاز شده است";
                    sock.Close();
                    return "پورت مقصد از سمت فایروال غیرمجاز است";
                }
                catch (System.Net.Sockets.SocketException ex)
                {
                    if (ex.ErrorCode == 10061) // Port is unused and could not establish connection 
                        return "پورت مقصد فعال است اما توسط aaPanel فعال نیست";
                    else
                        return "آیپی وارد شده در شبکه وجود ندارد";
                }
            }
            catch(Exception ex)
            {
                return "قالب آیپی نسخه 4 صحیح نیست";
            }
        }

        public static bool PingHost(string hostUri, int portNumber)
        {
            try
            {
                using (var client = new TcpClient(hostUri, portNumber))
                    
                    return client.Connected;
            }
            catch (SocketException ex)
            {
                
                return false;
            }
        }
    }
}