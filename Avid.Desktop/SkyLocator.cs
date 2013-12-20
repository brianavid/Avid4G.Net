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
        //  Data for a multicast Send to trigger responses from SSDP services on the local LAN
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

        /// <summary>
        /// The set of device addresses discovered
        /// </summary>
        HashSet<string> Devices = new HashSet<string>();

        /// <summary>
        /// Discover a Sky STB on the local LAN and populate a registry key with string values for the services it reports
        /// </summary>
        /// <param name="localIpAddress"></param>
        /// <returns></returns>
        public static Dictionary<string, string> GetSkyServices(
            string localIpAddress)
        {
            //  Instantiate a SkyLocator and use it to determine the available Sky STB device service locations
            //  We expect to find two of these
            SkyLocator locator = new SkyLocator();
            IEnumerable<string> locations = locator.GetSkyLocations(localIpAddress);

            //  A dictionary keyed by Sky service types for the service URLs
            //  We expect to find two of these
            Dictionary<string, string> services = new Dictionary<string, string>();

            //  For each Sky STB service location we find, request (synchronously) for the services its provides
            foreach (string location in locations)
            {
                int lastSlash = location.LastIndexOf("/");
                string host = location.Substring(0, lastSlash);

                //  Query synchronously the discovered service location URLs
                HttpWebRequest request = WebRequest.Create(location) as HttpWebRequest;
                request.UserAgent = "SKY_skyplus";
                request.Accept = "text/xml";

                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        //  The response will be a service description XML 
                        var doc = XDocument.Load(responseStream);
                        var nsRoot = doc.Root.GetDefaultNamespace();
                        //  The <service> elements are the ones we want.
                        //  For each of these, add a Directory mapping of <serviceType> to <controlURL>
                        foreach (var service in doc.Descendants(nsRoot + "service"))
                        {
                            services[service.Element(nsRoot + "serviceType").Value] = host + service.Element(nsRoot + "controlURL").Value;
                        }
                    }
                }
            }

            //  For all discovered services, add them to the registry to be used in another process context which is unable 
            //  to run this service discovery
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey("Software").CreateSubKey("Avid").CreateSubKey("Sky"))
            {
                foreach (string serviceType in services.Keys)
                {
                    key.SetValue(serviceType, services[serviceType]);
                }
            }
            return services;
        }

        /// <summary>
        /// Use a broadcast SSDP on the local LAN to discover Sky STBs and their service descriptions
        /// </summary>
        /// <param name="localIpAddress"></param>
        /// <returns></returns>
        IEnumerable<string> GetSkyLocations(
            string localIpAddress)
        {
            //  Use a broadcast UDP socket
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                //  Broadcast to the local LAN
                IPAddress ipAddress = IPAddress.Parse(localIpAddress);
                socket.Bind(new IPEndPoint(/*IPAddress.Any*/ipAddress, unicastPort));
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(multicastAddress, IPAddress.Any));

                //  Start a thread to receive socket responses to that broadcast
                var thd = new Thread(() => GetSocketResponse(socket));
                thd.Start();

                //  Send the broadcast out and start waiting for responses
                socket.SendTo(broadcastMessage, 0, broadcastMessage.Length, SocketFlags.None, new IPEndPoint(multicastAddress, multicastPort));

                //  Wait for a while, but stop when we have two responses, as these are the ones we want.
                //  This assumes that there is only one SDB on the LAN
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

                //  All done - return the ones we found
                socket.Close();
                return Devices;
            }
        }

        /// <summary>
        /// On a background thread, receive UDP packets from the SSDP response port
        /// </summary>
        /// <param name="socket"></param>
        void GetSocketResponse(Socket socket)
        {
            try
            {
                //  Keep going until the socket is closed on the main thread
                while (true)
                {
                    var response = new byte[8000];
                    EndPoint ep = new IPEndPoint(IPAddress.Any, multicastPort);

                    //  Read a response packet as a byte array and make it a string
                    socket.ReceiveFrom(response, ref ep);
                    var str = Encoding.UTF8.GetString(response);

                    //  Does it encode a service description from a Sky STB
                    var location = GetLocation(str);
                    if (!string.IsNullOrEmpty(location))
                    {
                        //  If so add it to our Devices. The main thread will stop when we have received two of them
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

        /// <summary>
        /// If the SSDP response is a service description from a Sky box return that service location
        /// </summary>
        /// <param name="str"></param>
        /// <returns>A Sky service location URL, or an empty string if the response is NOT a Sky service description</returns>
        string GetLocation(
            string str)
        {
            //  All OK?
            if (str.StartsWith("HTTP/1.1 200 OK"))
            {
                //  break the response into a collection of lines
                var reader = new StringReader(str);
                var lines = new List<string>();
                for (; ; )
                {
                    var line = reader.ReadLine();
                    if (line == null) break;
                    if (line != "") lines.Add(line);
                }

                //  Is there a server: line which contains " SKY ". If so, we determine this to be from a Sky STB
                var server = lines.Where(lin => lin.ToLower().StartsWith("server:")).First();
                if (server.Contains(" SKY "))
                {
                    //  Return  the value tagged with "location:", which is the SSDP encoding of the service URL
                    var location = lines.Where(lin => lin.ToLower().StartsWith("location:")).First();
                    return location.Substring("location:".Length).Trim();
                }
            }

            //  Not a Sky STB service location
            return "";
        }

    }
}
