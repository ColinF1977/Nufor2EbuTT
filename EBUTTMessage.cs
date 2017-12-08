using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NuforRx
{
    public class EBUTTMessageBase
    {
        protected int _sequenceNumber;
        protected string _sequenceIdentifier;
        private string _template;
    
        public EBUTTMessageBase(string identifier, int sequenceNumber, string template)
        {
            _sequenceIdentifier = identifier;
            _sequenceNumber = sequenceNumber;
            _template = template;
        }


        public int SequenceNumber
        {
            get
            {
                return _sequenceNumber;
            }
        }

        public string SequenceIdentifier
        {
            get
            {
                return _sequenceIdentifier;
            }
        }

        protected string Template
        {
            get
            {
                return _template;
            }
        }
    }

    public class EBUTTMessageOffAir : EBUTTMessageBase
    {
        public EBUTTMessageOffAir(string identifier, int sequenceNumber, string template) : base(identifier, sequenceNumber, template)
        {

        }

        public override string ToString()
        {
            XElement doc = XElement.Load(Template);

            XNamespace ebuttp = "urn:ebu:tt:parameters";

            XAttribute seqId = doc.Attribute(ebuttp + "sequenceIdentifier");

            if (seqId != null)
            {
                seqId.Value = _sequenceIdentifier;
            }

            XAttribute seqNum = doc.Attribute(ebuttp + "sequenceNumber");

            if (seqNum != null)
            {
                seqNum.Value = _sequenceNumber.ToString();
            }

            return doc.ToString();


        }
       
    }

    public class EBUTTSubtitleMessage : EBUTTMessageBase
    {
        public TeletextAlign TextAlign { get; set; }
        public TeletextVerticalAlign VerticalAlign { get; set; }

     

        public EBUTTSubtitleMessage(string identifier, int sequenceNumber, string template) : base(identifier, sequenceNumber, template)
        {

        }

        public List<EBUTTSubtitleRow> Rows = new List<EBUTTSubtitleRow> { };

        public override string ToString()
        {
            XElement doc = XElement.Load(Template);

            XNamespace ebuttp = "urn:ebu:tt:parameters";

            XAttribute seqId = doc.Attribute(ebuttp + "sequenceIdentifier");

            if (seqId != null)
            {
                seqId.Value = _sequenceIdentifier;
            }

            XAttribute seqNum = doc.Attribute(ebuttp + "sequenceNumber");

            if (seqNum != null)
            {
                seqNum.Value = _sequenceNumber.ToString();
            }
            XNamespace ttml = "http://www.w3.org/ns/ttml";

            XNamespace tts = "http://www.w3.org/ns/ttml#styling";

            XElement div = doc.Descendants(ttml + "div").FirstOrDefault();


            XElement p = new XElement(ttml + "p", 
                new XAttribute( "style", GetStyle()),
                new XAttribute("region", GetRegion())
                );

            

            div.Add(p);

            foreach (EBUTTSubtitleRow r in Rows)
            {
                if (string.IsNullOrEmpty(r.Text))
                    continue;

                // Argh - dreaded line 26 formatting. Ignore the hard stuff
                if (r.RowNumber == 26)
                    continue;

                XElement xRow = new XElement(ttml + "span",
                    new XAttribute("style", r.GetStyle())
                    );
                xRow.Value = r.Text;
                p.Add(xRow);
            }

            return doc.ToString();


        }

        protected virtual string GetRegion()
        {
            string region = "";

            switch (VerticalAlign)
            {
                case TeletextVerticalAlign.Centre:
                    region = "verticalAlignCenter";
                    break;
                case TeletextVerticalAlign.Top:
                    region = "verticalAlignTop";
                    break;
                case TeletextVerticalAlign.Bottom:
                    region = "verticalAlignBottom";
                    break;

            }
            return region;
        }

        protected virtual string GetStyle()
        {
            string style = "";

            switch (TextAlign)
            {
                case TeletextAlign.Centre:
                    style = "textAlignCenter";
                    break;
                case TeletextAlign.Right:
                    style = "textAlignRight";
                    break;
                case TeletextAlign.Left:
                    style = "textAlignLeft";
                    break;
            }

            return style;
        }
    }

    public class ScreenXMLSubtitleMessage : EBUTTSubtitleMessage
    {
        public ScreenXMLSubtitleMessage(string template) : base(String.Empty, 0, template)
        {
           
        }

        public override string ToString()
        {
            XElement doc = XElement.Load(Template);

            XNamespace ttml = "http://www.w3.org/ns/ttml";


            XElement div = doc.Descendants(ttml + "div").FirstOrDefault();


            XElement p = new XElement(ttml + "p",
                new XAttribute("alignment", GetStyle())
                );



            div.Add(p);

            foreach (EBUTTSubtitleRow r in Rows)
            {
                if (string.IsNullOrEmpty(r.Text))
                    continue;

                // Argh - dreaded line 26 formatting. Ignore the hard stuff
                if (r.RowNumber == 26)
                    continue;

                XElement xRow = new XElement(ttml + "span");
                xRow.Value = r.Text;
                p.Add(xRow);
            }

            return doc.ToString();
        }

        protected override string GetStyle()
        {
            return "center";
        }


    }

    public class ScreenXMOffAirMessage : EBUTTMessageOffAir
    {
        public ScreenXMOffAirMessage(string template) : base(String.Empty, 0, template)
        {

        }

        public override string ToString()
        {
            System.IO.TextReader tr = System.IO.File.OpenText(Template);
            return tr.ReadToEnd();
        }

    }



        public class EBUTTSubtitleRow
    {
        public string Text;
        public int RowNumber;
        public TeletextColor Color;
        public bool DoubleHeight;

        public string GetStyle()
        {
            string style = Enum.GetName(typeof(TeletextColor), Color);

            if(DoubleHeight)
            {
                style += " doubleHeight";
            }

            return style;
        }
       

    }

 


    

    

    
}
