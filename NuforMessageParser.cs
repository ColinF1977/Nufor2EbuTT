using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NuforRx
{
    public enum ParseState
    {
        WaitCode,
        WaitSize,
        WaitBytes
    };


    public class NuforMessageParser
    {
        #region Message Codes
        public const byte NUFOR_ACK = 0x06;
        public const byte NUFOR_NACK = 0x15;
        public const byte NUFOR_ONAIR = 0x10;
        public const byte NUFOR_OFFAIR = 0x18;
        public const byte NUFOR_CHANNELSELECT = 0x1b;
        public const byte NUFOR_INITSTRING = 0x0E;
        public const byte NUFOR_SUBTITLE = 0x0F;
        #endregion

        #region Hamming Table
        public static byte[] HammingTable = { 0x15, 0x02, 0x49, 0x5e, 0x64, 0x73, 0x38, 0x2f, 0xd0, 0xC7, 0x8c, 0x9b, 0xa1, 0xb6, 0xfd, 0xea };

        public static byte[] ReverseHammingTable = new byte[256];
        #endregion

        #region Public Members
        public bool IsSD04 { get; set; }

        public Queue<NuforMessageBase> MessageQueue = new Queue<NuforMessageBase> { };
        #endregion

        #region Private Members
        private ParseState _state;
        private int _bytesToRead = 0;
        private NuforMessageBase _currentMessage = new NuforMessageBase();
        private TcpClient _tcpClient;
        private TcpListener _server;
        private string _ipAddress;
        private int _port;
        private System.Threading.Thread _ListenThread;
        private bool _terminate = false;

        #endregion

        #region events
        public event EventHandler Connected;
        public event EventHandler Disconnected;
        public event EventHandler<OnMessageEventArgs> OnMessage;
        #endregion

        #region Initialisation
        public NuforMessageParser(string ipAdress, int port) 
        {
            InitReverseHamming();
            IsSD04 = false;
            _ipAddress = ipAdress;
            _port = port;
        }

        private void InitReverseHamming()
        {
            int j, k;

            for(int i = 0; i < 256; i++)
            {
                ReverseHammingTable[i] = 0xff;
            }

            for(int i = 0; i < 16; i++)
            {
                ReverseHammingTable[HammingTable[i]] = (byte)i;

                for(j = 0, k = 1; j < 8; j++, k <<= 1)
                {
                    ReverseHammingTable[HammingTable[i] ^ k] = (byte)i;
                }
            }
        }
        public void Stop()
        {
            _terminate = true;
            _ListenThread.Abort();
        }

        public void Start()
        {
            _ListenThread = new System.Threading.Thread(new System.Threading.ThreadStart(Listen));
            _ListenThread.Start();
        }

        public void Listen()
        {
            try
            {
                IPAddress ip = IPAddress.Parse(_ipAddress);
                _server = new TcpListener(ip, _port);

                // Start listening for client requests.
                _server.Start();

              

                // Enter the listening loop.
                while (!_terminate)
                {
                    // Perform a blocking call to accept requests.
                    // You could also user server.AcceptSocket() here.
                    _tcpClient = _server.AcceptTcpClient();

                    if(this.Connected != null)
                    {
                        this.Connected(this, null);
                    }

                    // Get a stream object for reading and writing
                    NetworkStream stream = _tcpClient.GetStream();


                    int i;
                    //stream.ReadTimeout = 500;

                    while(!_terminate)
                    {
                        // Buffer for reading data
                        Byte[] bytes = new Byte[256];

                        try
                        {
                            // Loop to receive all the data sent by the client.
                            i = stream.Read(bytes, 0, bytes.Length);
                        }
                        catch(System.IO.IOException)
                        {
                            // Timeout occurred
                            continue;
                        }

                        if(i > 0 && !_terminate)
                        {
                            if (ProcessMessage(bytes, bytes.Length))
                            {
                                Console.WriteLine("Parser has read {0} messages", MessageQueue.Count);

                                while (MessageQueue.Count > 0)
                                {
                                    NuforMessageBase mb = MessageQueue.Dequeue();

                                    if (this.OnMessage != null)
                                    {
                                        this.OnMessage(this, new OnMessageEventArgs { Message = mb });
                                    }

                                }
                            }
                        }

                    }

                    Console.WriteLine("Closing Connection...");
                    // Shutdown and end connection
                    _tcpClient.Close();
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
                // Stop listening for new clients.
                _server.Stop();
                if (this.Disconnected != null)
                {
                    this.Disconnected(this, null);
                }
            }

        }
        #endregion

        #region Message Passing
        public bool ProcessMessage(byte[] data, int length)
        {

            for (int i = 0; i < length; i++)
            {
                ParseChar(data[i]);
            }

            return MessageQueue.Count > 0;
        }


        private bool ParseChar(byte b)
        {
            bool messageComplete = false;

            b &= 0x7f;


            switch (_state)
            {
                case ParseState.WaitCode:
                    messageComplete = ParseWaitCode(b);
                    break;
                case ParseState.WaitSize:
                    messageComplete = ParseSize(b);
                    break;
                case ParseState.WaitBytes:
                    messageComplete = _currentMessage.ParseByte(b);
                    break;

            }

            if(messageComplete)
            {
                if(_currentMessage != null)
                {
                    MessageQueue.Enqueue(_currentMessage);
                    _state = ParseState.WaitCode;
                }

            }

            return true;

        }

        private bool ParseWaitCode(byte b)
        {
            bool messageComplete = false;

            switch(b)
            {
                case NUFOR_ACK:
                    Console.WriteLine(">> NUFOR_ACK");
                    NuforMessageBase ack = new NuforMessageBase() { Type = NuforMessageType.Ack  };
                    _currentMessage = ack;
                    messageComplete = true;
                    break;
                case NUFOR_NACK:
                    Console.WriteLine(">> NUFOR_NACK");
                    NuforMessageBase nack = new NuforMessageBase() { Type = NuforMessageType.Nack };
                    _currentMessage = nack;
                    messageComplete = true;
                    break;
                case NUFOR_OFFAIR:
                    Console.WriteLine(">> NUFOR_OFFAIR");
                    NuforMessageBase offair = new NuforMessageBase() { Type = NuforMessageType.OffAir };
                    _currentMessage = offair;
                    messageComplete = true;
                    break;
                case NUFOR_ONAIR:
                    Console.WriteLine(">> NUFOR_ONAIR");
                    NuforMessageBase onair = new NuforMessageBase() { Type = NuforMessageType.OnAir };
                    _currentMessage = onair;
                    messageComplete = true;
                    break;
                case NUFOR_CHANNELSELECT:
                    Console.WriteLine(">> NUFOR_CHANNELSELECT");
                    NuforMessageChannelSelect pageNum = new NuforMessageChannelSelect() { Type = NuforMessageType.ChannelSelect };
                    _currentMessage = pageNum;
                    _bytesToRead = 1;
                    _state = ParseState.WaitBytes;
                    break;
                case NUFOR_SUBTITLE:
                    Console.WriteLine(">> NUFOR_SUBTITLE");
                    NuforMessageSubtitle subtitle = new NuforMessageSubtitle() { Type = NuforMessageType.Subtitle };
                    _currentMessage = subtitle;
                    _bytesToRead = 1;
                    _state = ParseState.WaitSize;
                    break;
                case NUFOR_INITSTRING:
                    Console.WriteLine(">> INIT STRING ");
                    NuforInitString initString = new NuforInitString() { Type = NuforMessageType.InitString };
                    _currentMessage = initString;
                    _bytesToRead = 4;
                    _state = ParseState.WaitBytes;
                    break;
                default:
                    //Console.WriteLine(">> unknown command {0}", b);
                    messageComplete = true;
                    _currentMessage = null;
                    break;


            }
            return messageComplete;
        }

        private bool ParseSize(byte b)
        {
            int numberOfLines = 0;

            if(IsSD04)
            {
                numberOfLines = b & 0x0f;

            }
            else
            {
                numberOfLines = (int)ReverseHammingTable[b];
                numberOfLines &= 0x07;
            }

            if(numberOfLines == 0xFF)
            {
                Console.WriteLine("Bad Data");

            }

            NuforMessageSubtitle sub = _currentMessage as NuforMessageSubtitle;
            sub.NumberOfLines = numberOfLines;

            if (sub == null)
                throw new Exception("Parsing error!");

            numberOfLines = numberOfLines & 0x07;

            _bytesToRead = numberOfLines * 42;
            sub.SubtitleSize = _bytesToRead;
            _state = ParseState.WaitBytes;
            return false;

        }
        #endregion
    }

    public class OnMessageEventArgs : EventArgs
    {
        public NuforMessageBase Message { get; set;  }
    }

    
}
