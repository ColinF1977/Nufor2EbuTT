using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NuforRx
{
    class Program
    {
        static bool connected = false;

        static void Main(string[] args)
        {
            Configuration cfg = new Configuration();
            NuforMessageParser parser = new NuforMessageParser("127.0.0.1", cfg.ListenPort);
            //EBUTTWriteMessageToFile messageConsumer = new EBUTTWriteMessageToFile(@".\Ouput");
            EBUTTWriteMessageToTCP messageConsumer = new EBUTTWriteMessageToTCP(cfg.IPAddress, cfg.SendPort);

            NuforToEBUTTConverter ebuttTX = new NuforToEBUTTConverter(cfg.SequenceName, parser, messageConsumer);

            parser.Start();

            Console.WriteLine("Listening for Nufor messages on port: " + cfg.ListenPort);
            Console.WriteLine("Type Quit to exit");

            string message = Console.ReadLine();

            while(message.ToUpper().StartsWith("QUIT") == false)
            {
                message = Console.ReadLine();
            }

            parser.Stop();

            while (connected)
                Thread.Sleep(1000);

            Console.WriteLine("\nHit enter to continue...");
            Console.Read();

        }


        private static void Parser_OnMessage(object sender, OnMessageEventArgs e)
        {
            Console.WriteLine("Message Received >>>>>");
            Console.WriteLine(e.Message.ToString());
            Console.WriteLine("End of Message   >>>>>");
        }

        private static void Parser_Disconnected(object sender, EventArgs e)
        {
            connected = false;
            Console.WriteLine(">> DISCONNECTED <<");
        }

        private static void Parser_Connected(object sender, EventArgs e)
        {
            connected = true;
            Console.WriteLine(">> CONNECTED <<");

        }
    }
}
