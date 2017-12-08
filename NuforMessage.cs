using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuforRx
{
    public enum NuforMessageType
    {
        Ack,
        Nack,
        OnAir,
        OffAir,
        ChannelSelect,
        InitString,
        Subtitle

    }
    public class NuforMessageBase
    {
        public NuforMessageType Type { get; set; }

        public virtual  bool ParseByte(byte b)
        {
            return true;
        }

        public override string ToString()
        {
            return string.Format(">>> {0} <<<<<", Enum.GetName(typeof(NuforMessageType), Type));
        }


    }

    public class NuforMessageChannelSelect : NuforMessageBase
    {
        public short Channel { get; private set;  }
        public override bool ParseByte(byte b)
        {
            Channel = (short)b;

            return true;
        }


        public override string ToString()
        {
            return string.Format(">>> {0} {1} <<<<<", Enum.GetName(typeof(NuforMessageType), Type), Channel);
        }

    }

    public class NuforInitString : NuforMessageBase
    {
        private byte[] _pageBytes = new byte[4];

        private int _index = 0;

        public override bool ParseByte(byte b)
        {
            if (_index < 4)
            {
                _pageBytes[_index++] = b;
            }

            return _index == 3;
        }

        public NuforInitString() : base()
        {
            for(int i = 0; i < 4; i++)
            {
                _pageBytes[i] = 0xff;
            }
        }

        public int GetPageNumber()
        {
            int b0 = NuforMessageParser.ReverseHammingTable[_pageBytes[0]];
            int b1 = NuforMessageParser.ReverseHammingTable[_pageBytes[1]];
            int b2 = NuforMessageParser.ReverseHammingTable[_pageBytes[2]];
            int b3 = NuforMessageParser.ReverseHammingTable[_pageBytes[3]];
            int x = b1 * 100 + b2 * 10 + b3;
            return x;
        }


        public override string ToString()
        {
            return string.Format(">>> {0} {1} <<<<<", Enum.GetName(typeof(NuforMessageType), Type), GetPageNumber());
        }

    }

 


  

    public class SubtitleRow
    {
        public string Text = string.Empty;
        public int RowNumber;
        public TeletextSize Size;
        public TeletextColor Colour;
        public bool Flash;

        public override string ToString()
        {
            return string.Format($"{RowNumber}\t{Text}") + Environment.NewLine;
        }
    }

    public class NuforMessageSubtitle : NuforMessageBase
    {
        private StringBuilder _subtitle = new StringBuilder();
        private int _rowBytes = 0;
        private byte[] _rowNumber = new byte[2];
        private byte[] _initBytes = new byte[2];
        public int NumberOfLines { get; set; }

        public List<SubtitleRow> SubtitleRows = new List<SubtitleRow> { };

        SubtitleRow _thisRow = new SubtitleRow();


      


        enum SubtitleParseState
        {
            WaitRowNumber,
            WaitData
        };

        SubtitleParseState _parseState = SubtitleParseState.WaitRowNumber;

        public override string ToString()
        {
            string ret = string.Empty;

            foreach (SubtitleRow r in SubtitleRows)
            {
                ret += r.ToString();
            }

            return ret;

        }

        public int SubtitleSize { get; set; }

        private int _index = 0;

        public override bool ParseByte(byte b)
        {

            switch (_parseState)
            {
                case SubtitleParseState.WaitRowNumber:
                    _rowNumber[_rowBytes++] = b;
                    if (_rowBytes == 2)
                    {
                        byte topByte = NuforMessageParser.ReverseHammingTable[_rowNumber[0]];
                        byte bottomByte = NuforMessageParser.ReverseHammingTable[_rowNumber[1]];
                        _thisRow.RowNumber = (int)((topByte << 4) + bottomByte);
                        _parseState = SubtitleParseState.WaitData;
                        _rowBytes = 0;
                    }
                    break;
                case SubtitleParseState.WaitData:
                    {
                        _rowBytes++;
                        _index++;
                        char c = (char)(b & 0x7f);

                        string toAppend;
                        switch ((int)c)
                        {
                            case 0x00: // Black
                                this._thisRow.Colour = TeletextColor.Black;
                                break;
                            case 0x01: // Red
                                this._thisRow.Colour = TeletextColor.Red;
                                break;
                            case 0x02: // Green
                                this._thisRow.Colour = TeletextColor.Green;
                                break;
                            case 0x03: // Yellow
                                this._thisRow.Colour = TeletextColor.Yellow;
                                break;
                            case 0x04: // Blue
                                this._thisRow.Colour = TeletextColor.Blue;
                                break;
                            case 0x05: // Magenta
                                this._thisRow.Colour = TeletextColor.Magenta;
                                break;
                            case 0x06: // Cyan
                                this._thisRow.Colour = TeletextColor.Cyan;
                                break;
                            case 0x07: // White
                                this._thisRow.Colour = TeletextColor.White;
                                break;
                            case 0x08: // Flash
                                this._thisRow.Flash = true;
                                break;
                            case 0x09: // Steady
                                this._thisRow.Flash = false;
                                break;
                            case 0x0a: // StartBox
                                //toAppend = "<EndBox>";
                                break;
                            case 0x0b: // EndBox
                                //toAppend = "<StartBox>";
                                break;
                            case 0x0c: // Normal Size
                                this._thisRow.Size = TeletextSize.NormalSize;
                                break;
                            case 0x0d: // Double Height
                                this._thisRow.Size = TeletextSize.DoubleHeight;
                                break;
                            case 0x0e: // Double Width
                                this._thisRow.Size = TeletextSize.DoubleWidth;
                                break;
                            case 0x0f: // Double Size
                                this._thisRow.Size = TeletextSize.DoubleSize;
                                break;
                            case 0x1c:
                            case 0x1d:
                                Console.Write(".");
                                break;
                            default:
                                toAppend = c.ToString();
                                _subtitle.Append(toAppend);
                                break;



                        }


                        if (_rowBytes == 40)
                        {

                            _thisRow.Text = _subtitle.ToString();
                            SubtitleRows.Add(_thisRow);

                            _thisRow = new SubtitleRow();
                            _subtitle = new StringBuilder();
                            _rowBytes = 0;
                            _parseState = SubtitleParseState.WaitRowNumber;

                        }
                    }
                    break;
            }

            return _index == SubtitleSize ? true : false; ;
        }
    }

      

}
