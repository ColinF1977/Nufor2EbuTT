using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuforRx
{
    public interface IEBUTTMessageConsumer
    {
        void EbuttTX_OnMessage(object sender, EBUTTOnMessageArgs e);

    }
}
