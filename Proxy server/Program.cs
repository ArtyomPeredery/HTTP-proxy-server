using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml;

namespace Proxy
{
    class Proxy : IDisposable
    {       
        private int Port;
        public bool Exit = false;        
        public int portInt = 80; 
        private const int backlog = (int)SocketOptionName.MaxConnections;
        private const int sendPort = 80;

        public Proxy(int proxyPort)
        {
            this.Port = proxyPort;
        }

        public void Dispose()
        {
            Exit = true;
        }

        public void StartProxy()
        {
            using (Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))//конструкция для вызова Dispose
            {
                IPEndPoint listenIP = new IPEndPoint(IPAddress.Loopback, Port); // loopback = 127.0.0.1 or simple localhost
                listenSocket.Bind(listenIP);
                listenSocket.Listen(backlog);
                while (true)
                {
                    if (listenSocket.Poll(0, SelectMode.SelectRead))
                    {
                        StartClientReceive(listenSocket.Accept());
                    }
                    if (Exit) break;
                }
            }
        }

        private void SendErrorPage(Socket socket, string host)
        {
            string htmlBody = "<html><body><h1>Blocked</h1><br><h2 style = \" color: red\">" + host + " is blocked</h2></body></html>";

            byte[] errorBodyBytes = Encoding.ASCII.GetBytes(htmlBody);
            socket.Send(errorBodyBytes, errorBodyBytes.Length, SocketFlags.None);
        }

        private void StartClientReceive(Socket socket)
        {
            Thread listenThread = new Thread(() => { ProcessSocket(socket); })
            {
                IsBackground = true
            };
            listenThread.Start();
        }

        private void ProcessSocket(Socket requestSocket)
        {
            using (requestSocket)
            {
                if (requestSocket.Connected)
                {
                        bool IsHttps = false;
                        GetMessageFromSocket(requestSocket, out byte[] httpByteArray);
                        string[] httpFields = SplitHttpToArray(httpByteArray);
                        string hostField = httpFields.FirstOrDefault(x => x.Contains("Host"));
                        if (hostField == null) return;
                        int UsedPort = 0;
                        string[] hostFields = hostField.Split(' ');
                        string host = hostFields[1];                                         
                        bool isClosed = IsBlocked(host);
                    try
                    {
                        if (host.IndexOf(':') != -1)
                        {
                            string[] trueports = host.Split(':');
                            portInt = int.Parse(trueports[1]);
                            IsHttps = true;
                        }

                        if (isClosed)
                        {
                            SendErrorPage(requestSocket, host);
                            Console.WriteLine(" {0} in black list.", host);
                            return;
                        }
                        else
                        {
                            if (IsHttps)
                            {
                                UsedPort = portInt;
                            }
                            else
                            {
                                UsedPort = sendPort;
                            }

                            Console.WriteLine("request to " + hostField +"|| port: " + UsedPort);
                            IPHostEntry ipHostEntry = Dns.GetHostEntry(host);
                           
                            
                            IPEndPoint ipEndPoint = new IPEndPoint(ipHostEntry.AddressList[0], UsedPort);
                            using (Socket replySocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                            {
                                replySocket.Connect(ipEndPoint);
                                if (replySocket.Send(httpByteArray, httpByteArray.Length, SocketFlags.None) != httpByteArray.Length)
                                {
                                    Console.WriteLine("Can''t connect ");
                                }
                                else
                                {
                                    GetMessageFromSocket(replySocket, out byte[] httpResponse);
                                    requestSocket.Send(httpResponse, httpResponse.Length, SocketFlags.None);
                                    httpFields = SplitHttpToArray(httpResponse);

                                    string[] responseCode = httpFields[0].Split(' ');
                                    if (responseCode == null) return;
                                    Console.WriteLine("{0} answer code: {1}",hostField, responseCode[1]);
                                }
                            }
                        }
                        requestSocket.Close();
                    }
                    catch(Exception)
                    {
                        Console.WriteLine("Данный порт не поддерживается(порт: {0})", portInt);
                    }
                  

                }
            }
        }

        private static void GetMessageFromSocket(Socket socket, out byte[] requestBytes)
        {
            byte[] buf = new byte[socket.ReceiveBufferSize];
            int recv = 0;
            using (MemoryStream requestMemoryStream = new MemoryStream())
            {
                while (socket.Poll(999999, SelectMode.SelectRead) && (recv = socket.Receive(buf, socket.ReceiveBufferSize, SocketFlags.None)) > 0)
                {
                    requestMemoryStream.Write(buf, 0, recv);
                }
                requestBytes = requestMemoryStream.ToArray();
            }
        }

        private string[] SplitHttpToArray(byte[] HttpHeading)
        {            
           string strHttp = Encoding.ASCII.GetString(HttpHeading);
            string[] resArray = strHttp.Trim().Split(new char[] { '\r', '\n' });
            return resArray;
        }

        private bool IsBlocked(string host)
        {

           List<string> blacklist = XmlSettingsParser.GetBlockedWebsites();
           
            foreach (var key in blacklist)
            {
                if (host.Equals(key))
                {
                    return true;
                }
            }
            return false;
        }       
    }

    public static class XmlSettingsParser
    {
        public static List<string> GetBlockedWebsites()
        {
            var settingsFile = new XmlDocument();
            settingsFile.Load(Environment.CurrentDirectory + Path.DirectorySeparatorChar + "Settings.xml");

            XmlNodeList blockedWebsites = settingsFile.SelectNodes("/Settings/BlockedWebsites/Website");

            var returnList = new List<string>();

            for (int i = 0; i < blockedWebsites.Count; ++i)
            {
                returnList.Add(blockedWebsites[i].FirstChild.Value);
            }

            return returnList;
        }
    }


    class Program
    {
        private const int port = 55555;
        static void Main(string[] args)
        {
            using (Proxy proxy = new Proxy(port))
            {
                proxy.StartProxy();
            }
        }
    }
}
