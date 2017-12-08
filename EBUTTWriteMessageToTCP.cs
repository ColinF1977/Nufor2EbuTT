using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NuforRx
{
    public class EBUTTWriteMessageToTCP : IEBUTTMessageConsumer
    {
        private string _ip;
        private int _port;
        private TcpClient _client;
        private NetworkStream _stream;

        public EBUTTWriteMessageToTCP(string ip, int port)
        {
            _ip = ip;
            _port = port;

            _client = new TcpClient();
            _client.Connect(ip, port);

            _stream = _client.GetStream(); 
            
        }
        public void EbuttTX_OnMessage(object sender, EBUTTOnMessageArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
            sb.Append(e.Message);

            string message = sb.ToString();

            Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>");
            Console.WriteLine(message);
            Console.WriteLine("<<<<<<<<<<<<<<<<<<<<<");


            
            byte[] data = System.Text.Encoding.Unicode.GetBytes(message);
            _stream.Write(data, 0, data.Length);

        }
    }
}
