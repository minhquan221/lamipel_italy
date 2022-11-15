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
    class Loader
    {
        //private static string Private = "MIICWwIBAAKBgQC9OVHoo711U0EIE8CQJ4or6fcfUK9MlLrZk2i797xpTSaQOgrcrJ9KhWIg51f0GGafEDYlXI+EJG+C3DjNnUDOlEWU/t9hg2SsYhDGx9qO6lFyYIyYWbqsAEllpQxdgb95kNLN1dVKNxowDaltwg4YPx0ApbtVD+vIrxomyumtxwIDAQABAoGAUb0vCsWgk8vw7aJKrnrGDw40OAzMjNI6nL42oev0MbCoFelcw9K1xKU1rG0C62iW++Mu4JjJThXHLcofp4JkeZhdD4VjD2rPw4gF2+/dDy6lfzlnKVyMaE6ehXAOqxbEHj3p4QwCXpIECD3/vx7SzqM3r1qykmrAQBB+FE8e41ECQQD8LtdD8XqgOo9XWDBTR4TjQmwu73ltntvKQtdSwvArMkREEgppuXJtsP8oHPDNaZL3uJAfKR5rpNbXwWmNUb7dAkEAwBaGcFbqwkGfGLTmDVVbtEsa90Q5RoUiq+rZNUt6fclCzzPVJacrESHp7dBe8lixbOFw9M2GFErl5ed2bNJq8wJAN4iZHv0uY9qBdlqVScGWIfWenTdfJiS1gfT2NeN3wAtxvQu7/0w3RrCTf+HfpVx6YNLj6fjEGjBSn3CYLeaygQJAFgI+sx5yVYGZTgZkGYD/OeAlHdG2Ukss1s/YxU95oHHv8re4o1DNEXB2UiY+yru40IXgIFxwEWEniDkGC5/sWwJAcvlxeUaZhhJMBWfyEZqdmBSSK1HIwp8Y54YgeV5hf7Zq1CyeoG7CWjJZfZFF8APTCrjsLVL35LwPDV+RIvzFdg==";
        private static string Private = "MIICXQIBAAKBgQDOrspdFLtuXafh2gdRMBdXdKgOgXO+LjGqqeBaJ3Klc6X8B+1T9bOOxspeiLrnEKxGekpz4IZcjUvMo3QCEFpULbIC1CDQSy7NeZK8Aw+wT2b2cDKNI/FmNdMjnAngq/9sAu0W+wXeIAQL5mVsXrNwBFheNCwF/xizUb7kNuQnRwIDAQABAoGBAJMc5v2KHD9aenzP7BVl3bdqWZS5n+DmkIOhlweUvI3321WhbtQsNzqRVMolGKQPwhAIS6ZfUkPebu2iCpjNevzWy9hOXDUr4wbgf6yLZBac0iTJkLEFJ9ByIab5DGxY3yFtd++osSwc8WtVGGXIheG/PPb0bIn5YnS5mkbkjGPBAkEA/l9GKhJhuTm1ANfj+yWVbpfb6a0I0ZrA5jmHkcUZWSuyybtimWrQ6kwIWr/XjH1OX6YkHW3rzHU88ysvLurtVQJBANABY53IV8io6IKHOtcibAGk/dYWjyI3RKAQL6wImfx+fqGWsqpkipBkgiB1XBfSaiS5qA8VkQLvbOhwUYJFIisCQBjZMb92YDl526ajcE/eUgSlrQPctVnUZjeXMxkYSeueCy8NsQoPlyzsFHzY9LNyoi9RyYFedYRZh4SxmUBaDqkCQQC42VMux/kDMOAYB5dJ0qRLPdX66Odgj98jo97Jsoct9TgR3/t9I4ZkVX2y3LjvzqVXZ8orRY4uHpoZYp989nUrAkBxdEa2Udg/OytJ0bpkjWN406CogMwMFwCdnhqmVjccgrfYMNqlUFlgfQZTA0CPXixbCN2DOO8CrM8Uv5bpnI3F";
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
        public static void Setup()
        {
            Thread t = new Thread(() =>
            {
                try
                {
                    var privateKey = Private;
                    var macAddr = GetMacAddress();
                    var resultgetIp = APIHelper.CallAPIString("common/getip", null, new List<string[]> { new string[] { "PrivateKeyAuthen", privateKey } });
                    string IpAddr = resultgetIp;
                    var MachineName = Environment.MachineName;
                    string encodePrivate = JoseHelper.Current.Sign(JsonConvert.SerializeObject(new
                    {
                        macAddress = macAddr,
                        IPInternet = IpAddr,
                        MachineName = MachineName,
                        PrivateKey = privateKey
                    }), privateKey);
                    var resultAuthen = APIHelper.CallAPIString("common/authencomputer", new
                    {
                        privateKey = privateKey
                    }, new List<string[]> { new string[] { "Signature", encodePrivate } });
                    var result = resultAuthen;
                    File.WriteAllText(ConfigurationManager.AppSettings["PathTrace"] + "log_result_" + DateTime.Now.ToString("yyyyMMdd_HHmmssttt"), DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + " - IP: " + IpAddr + " - MAC: " + macAddr + " - MachineName: " + MachineName + " - " + result);
                }
                catch (Exception ex)
                {
                    File.WriteAllText(ConfigurationManager.AppSettings["PathTraceError"] + "log_error_" + DateTime.Now.ToString("yyyyMMdd_HHmmssttt"), DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + " - " + ex.ToString());
                }
            });
            t.Start();
        }
    }
}
