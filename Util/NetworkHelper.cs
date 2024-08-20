using System.Net.NetworkInformation;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Net;

namespace SJPCORE.Util
{
    public class NetworkHelper
    {
        public static string GetLocalIPv4()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        if (!IPAddress.IsLoopback(ip))
                        {
                            return ip.ToString();
                        }
                    }
                }
                throw new Exception("No intranet adapters with an IPv4 address in the system!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: '{0}'", ex);
                return "";
            }
        }

        public static string GetLocalIPv6()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        if (!IPAddress.IsLoopback(ip))
                        {
                            return ip.ToString();
                        }
                    }
                }
                throw new Exception("No intranet adapters with an IPv6 address in the system!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: '{0}'", ex);
                return "";
            }
        }

        //get dns name from ip
        public static string GetDnsNameFromIp(string ip)
        {
            try
            {
                IPHostEntry hostEntry = Dns.GetHostEntry(ip);
                return hostEntry.HostName;
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: '{0}'", ex);
                return "";
            }
        }

        public static string GetMacAddress()
        {
            try
            {
                var macAddr =
                     (
                           from nic in NetworkInterface.GetAllNetworkInterfaces()
                           where nic.OperationalStatus == OperationalStatus.Up
                           select nic.GetPhysicalAddress().ToString()
                     ).FirstOrDefault();
                return macAddr;
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: '{0}'", ex);
                return "";
            }
        }

        public static string IpToUrl(string ipAddress)
        {
            try
            {
                IPAddress address = IPAddress.Parse(ipAddress);
                string hostName = "something." + address.ToString().Replace(".", "-") + ".local";
                return hostName;
            }
            catch (FormatException)
            {
                return "Invalid IP address format.";
            }
        }

        public static string UrlToIp(string url)
        {
            try
            {
                string[] parts = url.Split('.');
                string ip = parts[1].Replace("-", ".");
                return ip;
            }
            catch (FormatException)
            {
                return "Invalid URL format.";
            }
        }

        public static string GetLocalHostName()
        {
            try
            {
                return Dns.GetHostName();
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: '{0}'", ex);
                return "";
            }
        }

        public static string GetLocalDomainName()
        {
            try
            {
                return Dns.GetHostEntry(Dns.GetHostName()).HostName;
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: '{0}'", ex);
                return "";
            }
        }

        public static string GetLocalDomainNameFromIp(string ip)
        {
            try
            {
                return Dns.GetHostEntry(ip).HostName;
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: '{0}'", ex);
                return "";
            }
        }

        public static string GetSoundfileUrl()
        {
            try
            {
                string url = "http://" + GetLocalIPv4() + ":8080/soundfile/";
                return url;
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: '{0}'", ex);
                return "";
            }
        }


    }
}
