using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuforRx
{
    public class Configuration
    {
        public int ListenPort { get; set; }
        public int SendPort { get; set; }
        public string IPAddress { get; set; }

        public string SequenceName { get; set; }

        public int SequenceNumber { get; set; }

        public Configuration()
        {
            Read();
        }

        public void Read()
        {
            int listenPort = 0;
            string ipAdrress = "127.0.0.1";
            int sendPort = 0;
            string sequenceName = "Foo";
            int sequenceNumber = 0;

            Int32.TryParse(ConfigurationManager.AppSettings["listenport"], out listenPort);
            Int32.TryParse(ConfigurationManager.AppSettings["sendport"], out sendPort);
            ipAdrress = ConfigurationManager.AppSettings["ipaddress"];
            sequenceName = ConfigurationManager.AppSettings["sequenceName"];
            Int32.TryParse(ConfigurationManager.AppSettings["sequenceNumber"], out sequenceNumber);

            ListenPort = listenPort;
            SendPort = sendPort;
            IPAddress = ipAdrress;
            SequenceName = sequenceName;
            SequenceNumber = sequenceNumber;

        }

        public void Write()
        {
            //Create the object
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);


            WriteConfigSetting(config, "listenport", ListenPort);
            WriteConfigSetting(config, "sendport", SendPort);
            WriteConfigSetting(config, "ipaddress", IPAddress);
            WriteConfigSetting(config, "sequenceName", SequenceName);
            WriteConfigSetting(config, "sequenceNumber", SequenceNumber);
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        public void WriteConfigSetting(System.Configuration.Configuration config, string key, object value)
        {
            if (config.AppSettings.Settings[key] != null)
            {
                config.AppSettings.Settings[key].Value = value.ToString();
            }
            else
            {
                config.AppSettings.Settings.Add(key, value.ToString());
            }
        }


    }
}
