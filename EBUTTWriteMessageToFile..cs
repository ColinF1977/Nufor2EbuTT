using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuforRx
{
    public class EBUTTWriteMessageToFile : IEBUTTMessageConsumer
    {
        private string _folder;

        public EBUTTWriteMessageToFile(string folder)
        {
            if(System.IO.Directory.Exists(folder) == false)
            {
                System.IO.Directory.CreateDirectory(folder);
            }

            _folder = folder;
        }
        public void EbuttTX_OnMessage(object sender, EBUTTOnMessageArgs e)
        {
            Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>");
            Console.WriteLine(e.Message.ToString());
            Console.WriteLine("<<<<<<<<<<<<<<<<<<<<<");

            string filename = e.SequenceIdentifier + "-" + e.SequenceNumber + ".xml";
            string fullPath = System.IO.Path.Combine(_folder, filename);
            using (System.IO.TextWriter tw = System.IO.File.CreateText(fullPath))
            {

                tw.WriteLine(e.Message);
                tw.Flush();
                tw.Close();
            }

        }
    }
}
