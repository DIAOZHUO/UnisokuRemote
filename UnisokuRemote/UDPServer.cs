using System;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace UnisokuRemote
{
    public enum IPAddressEnum
    {
        Local=1,
        LAN,
        Public
    }


    public class ServerUtility
    {
        public static IPEndPoint GetIPEndPoint(IPAddressEnum iPAddressEnum, int port)
        {
            IPEndPoint endpoint = new IPEndPoint(IPAddress.None, port);
            switch (iPAddressEnum)
            {
                case IPAddressEnum.Public:
                    string url = "http://checkip.dyndns.org";
                    System.Net.WebRequest req = System.Net.WebRequest.Create(url);
                    System.Net.WebResponse resp = req.GetResponse();
                    System.IO.StreamReader sr = new System.IO.StreamReader(resp.GetResponseStream());
                    string response = sr.ReadToEnd().Trim();
                    string[] ipAddressWithText = response.Split(':');
                    string ipAddressWithHTMLEnd = ipAddressWithText[1].Substring(1);
                    string[] ipAddress = ipAddressWithHTMLEnd.Split('<');
                    endpoint.Address = IPAddress.Parse(ipAddress[0]);
                    break;
                case IPAddressEnum.LAN:
                    using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                    {
                        socket.Connect("8.8.8.8", 65530);
                        IPEndPoint _endPoint = socket.LocalEndPoint as IPEndPoint;
                        endpoint.Address = _endPoint.Address;
                    }
                    break;
                case IPAddressEnum.Local:
                    endpoint.Address = IPAddress.Loopback;
                    break;
            }
            return endpoint;
        }
    }


    public class UDPSocket
    {
        public delegate void UdpReceiveEventHandler<T>(T args);
        public event UdpReceiveEventHandler<string> OnUdpReceive;

        private Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private const int bufSize = 8 * 1024;
        private State state = new State();
        private EndPoint epFrom = new IPEndPoint(IPAddress.Any, 0) as EndPoint;
        private AsyncCallback recv = null;

        public class State
        {
            public byte[] buffer = new byte[bufSize];
        }

        public void Server(IPEndPoint endPoint)
        {
            
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _socket.EnableBroadcast = true;
            _socket.Bind(endPoint);
            Receive();
            
        }

        public void Client(string address, int port)
        {
            _socket.Connect(IPAddress.Parse(address), port);
            Receive();
        }

        public void Send(string text)
        {
            byte[] data = Encoding.UTF8.GetBytes(text);
            _socket.BeginSend(data, 0, data.Length, SocketFlags.None, (ar) =>
            {
                State so = (State)ar.AsyncState;
                int bytes = _socket.EndSend(ar);
                //Console.WriteLine("SEND: {0}, {1}", bytes, text);
            }, state);
        }

        public void Stop()
        {
            _socket.Close();
            _socket.Dispose();
        }


        private void Receive()
        {
            
            _socket.BeginReceiveFrom(state.buffer, 0, bufSize, SocketFlags.None, ref epFrom, recv = (ar) =>
            {
                try
                {
                    State so = (State)ar.AsyncState;
                    int bytes = _socket.EndReceiveFrom(ar, ref epFrom);
                    _socket.BeginReceiveFrom(so.buffer, 0, bufSize, SocketFlags.None, ref epFrom, recv, so);
                    var msg = Encoding.UTF8.GetString(so.buffer, 0, bytes);
                    var msgs = msg.Split('|');
                    Console.WriteLine("RECV: {0}: {1}, {2}", epFrom.ToString(), bytes, msg);


                    var dispatcher = System.Windows.Application.Current.Dispatcher;
                    dispatcher.Invoke(() =>
                    {
                        OnUdpReceive(msgs[0]);
                    });
                    
                    if (msgs.Length > 1)
                    {
                        var eP = CreateIPEndPoint(msgs[1]);
                        Console.WriteLine(eP.ToString());
                        Socket _send_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                        _send_socket.Connect(eP);
                        _send_socket.Send(Encoding.UTF8.GetBytes(UnisokuAutomation.Instance.ZSliderValue.ToString()));
                        _send_socket.Dispose();
                    }   
                }
                catch
                {
                    //Console.WriteLine(e.ToString());
                }

            }, state);
            
        }

        public static IPEndPoint CreateIPEndPoint(string endPoint)
        {
            string[] ep = endPoint.Split(':');
            if (ep.Length != 2) throw new FormatException("Invalid endpoint format");
            IPAddress ip;
            if (!IPAddress.TryParse(ep[0], out ip))
            {
                throw new FormatException("Invalid ip-adress");
            }
            int port;
            if (!int.TryParse(ep[1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out port))
            {
                throw new FormatException("Invalid port");
            }
            return new IPEndPoint(ip, port);
        }
    }


    }
