using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuforRx
{
    public class NuforToScreenXMLConverter
    {
        private string _sequenceNameBase;
        private int _seqNum = 0;

       // public event EventHandler<EBUTTOnMessageArgs> OnMessage;

        NuforMessageParser _nuforClient;
        IEBUTTMessageConsumer _consumer;

        private EBUTTMessageBase _nextMessage = null;

        private string _offAirMessageTemplate = @"Messages\ScreenClearTemplate.xml";
        private string _onAirMessageTemplate = @"Messages\ScreenOnAirTemplate.xml";

        public NuforToScreenXMLConverter(string sequenceID, NuforMessageParser client, IEBUTTMessageConsumer consumer)
        {
            _sequenceNameBase = sequenceID;
            _nuforClient = client;
            _consumer = consumer;

            if (client != null)
            {
                _nuforClient.OnMessage += _nuforClient_OnMessage;
            }

        }

        public string OffAirMessageTemplate
        {
            get
            {
                return _offAirMessageTemplate;
            }
            set
            {
                if (value != null)
                {
                    _offAirMessageTemplate = value;
                }
            }
        }

        public string OnAirMessageTemplate
        {
            get
            {
                return _onAirMessageTemplate;
            }
            set
            {
                if (value != null)
                {
                    _onAirMessageTemplate = value;
                }
            }
        }



        private void _nuforClient_OnMessage(object sender, OnMessageEventArgs e)
        {
            switch (e.Message.Type)
            {
                case NuforMessageType.Subtitle:
                    SubtitleMessage(e.Message as NuforMessageSubtitle);
                    break;
                case NuforMessageType.OnAir:
                    OnAirMessage();
                    break;
                case NuforMessageType.OffAir:
                    OffAirMessage();
                    break;
                case NuforMessageType.Ack:
                default:
                    break;

            }
        }

        public void SubtitleMessage(NuforMessageSubtitle subtitle)
        {
            _nextMessage = ConvertFromNuForSubtitle(subtitle);
        }

        public EBUTTSubtitleMessage ConvertFromNuForSubtitle(NuforMessageSubtitle subtitle)
        {
            ScreenXMLSubtitleMessage message = new ScreenXMLSubtitleMessage(_onAirMessageTemplate);

            foreach (SubtitleRow row in subtitle.SubtitleRows)
            {

                if (string.IsNullOrEmpty(row.Text))
                    continue;

                EBUTTSubtitleRow ebutt = new EBUTTSubtitleRow()
                {
                    Color = row.Colour,
                    RowNumber = row.RowNumber,
                    Text = FormatText(row.Text),
                    DoubleHeight = row.Size == TeletextSize.DoubleHeight
                };

                message.Rows.Add(ebutt);
                message.TextAlign = GetTextAlign(row.Text);
                message.VerticalAlign = GetVerticalAlign(row.RowNumber);
            }

            return message;

        }

        public TeletextAlign GetTextAlign(string txt)
        {
            TeletextAlign ret = TeletextAlign.Left;

            if (txt.StartsWith(" "))
            {
                if (txt.EndsWith(" "))
                {
                    ret = TeletextAlign.Centre;
                }
                else
                {
                    ret = TeletextAlign.Right;
                }
            }

            return ret;
        }


        public string FormatText(string txt)
        {
            if (string.IsNullOrEmpty(txt))
                return string.Empty;

            string subText = txt.TrimStart(' ');
            subText = subText.TrimEnd(' ');
            return subText;
        }

        public TeletextVerticalAlign  GetVerticalAlign(int rowNumber)
        {
            TeletextVerticalAlign align = TeletextVerticalAlign.Bottom;

            if (rowNumber < 16)
            {
                if (rowNumber > 5)
                {
                    align = TeletextVerticalAlign.Centre;
                }
                else
                {
                    align = TeletextVerticalAlign.Top;
                }
            }

            return align;
        }


        private void OffAirMessage()
        {
            _nextMessage = new ScreenXMOffAirMessage(_offAirMessageTemplate);
            SendNextMessage();

        }

        private void OnAirMessage()
        {
            if(_nextMessage == null)
                _nextMessage = new ScreenXMOffAirMessage(_offAirMessageTemplate);

            SendNextMessage();
        }

        private void SendNextMessage()
        {
            if(_consumer != null)
            {
                _consumer.EbuttTX_OnMessage(this, new EBUTTOnMessageArgs(_nextMessage.SequenceIdentifier, _nextMessage.SequenceNumber) { Message = _nextMessage });
            }

            _nextMessage = null;
        }

        private int GetNextSequenceNumber()
        {
            return ++_seqNum;
        }

        private int SequenceNumber
        {
            get
            {
                return _seqNum;
            }
        }
    }

}
