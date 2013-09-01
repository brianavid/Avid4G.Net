using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace Avid.Desktop
{
    class SkyLocator
    {
        readonly IPAddress multicastAddress = IPAddress.Parse("239.255.255.250");
        const int multicastPort = 1900;
        const int unicastPort = 1901;
        const int searchTimeOutSeconds = 30;

        const string messageHeader = "M-SEARCH * HTTP/1.1";
        const string messageHost = "HOST: 239.255.255.250:1900";
        const string messageMan = "MAN: \"ssdp:discover\"";
        const string messageMx = "MX: 20";
        const string messageSt = "ST: ssdp:all";

        readonly byte[] broadcastMessage = Encoding.UTF8.GetBytes(
            string.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{0}",
                          "\r\n",
                          messageHeader,
                          messageHost,
                          messageMan,
                          messageMx,
                          messageSt));

        HashSet<string> Devices = new HashSet<string>();

        IEnumerable<string> GetSkyLocations(string localIpAddress)
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                IPAddress ipAddress = IPAddress.Parse(localIpAddress);
                socket.Bind(new IPEndPoint(/*IPAddress.Any*/ipAddress, unicastPort));
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(multicastAddress, IPAddress.Any));

                var thd = new Thread(() => GetSocketResponse(socket));
                thd.Start();

                socket.SendTo(broadcastMessage, 0, broadcastMessage.Length, SocketFlags.None, new IPEndPoint(multicastAddress, multicastPort));

                for (int i = 0; i < searchTimeOutSeconds; i++)
                {
                    Thread.Sleep(1000);
                    lock (Devices)
                    {
                        if (Devices.Count == 2)
                        {
                            break;
                        }
                    }
                }

                socket.Close();
                return Devices;
            }
        }

        string GetLocation(string str)
        {
            if (str.StartsWith("HTTP/1.1 200 OK"))
            {
                var reader = new StringReader(str);
                var lines = new List<string>();
                for (; ; )
                {
                    var line = reader.ReadLine();
                    if (line == null) break;
                    if (line != "") lines.Add(line);
                }

                var server = lines.Where(lin => lin.ToLower().StartsWith("server:")).First();
                if (server.Contains(" SKY "))
                {
                    var location = lines.Where(lin => lin.ToLower().StartsWith("location:")).First();
                    return location.Substring("location:".Length).Trim();
                }
            }

            return "";
        }

        public void GetSocketResponse(Socket socket)
        {
            try
            {
                while (true)
                {
                    var response = new byte[8000];
                    EndPoint ep = new IPEndPoint(IPAddress.Any, multicastPort);
                    socket.ReceiveFrom(response, ref ep);
                    var str = Encoding.UTF8.GetString(response);

                    var location = GetLocation(str);
                    if (!string.IsNullOrEmpty(location))
                    {
                        lock (Devices)
                        {
                            Devices.Add(location);
                        }
                    }
                }
            }
            catch
            {
                //TODO handle exception for when connection closes
            }

        }

        public static Dictionary<string, string> GetSkyServices(string localIpAddress)
        {
            SkyLocator locator = new SkyLocator();
            IEnumerable<string> locations = locator.GetSkyLocations(localIpAddress);

            Dictionary<string, string> services = new Dictionary<string, string>();

            foreach (string location in locations)
            {
                int lastSlash = location.LastIndexOf("/");
                string host = location.Substring(0, lastSlash);

                HttpWebRequest request = WebRequest.Create(location) as HttpWebRequest;
                request.UserAgent = "SKY_skyplus";
                request.Accept = "text/xml";

                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        var doc = XDocument.Load(responseStream);
                        var nsRoot = doc.Root.GetDefaultNamespace();
                        foreach (var service in doc.Descendants(nsRoot + "service"))
                        {
                            services[service.Element(nsRoot + "serviceType").Value] = host + service.Element(nsRoot + "controlURL").Value;
                        }
                    }
                }
            }

            using (RegistryKey key = Registry.LocalMachine.CreateSubKey("Software").CreateSubKey("Avid").CreateSubKey("Sky"))
            {
                foreach (string serviceType in services.Keys)
                {
                    key.SetValue(serviceType, services[serviceType]);
                }
            }
            return services;

        }
    }
}
