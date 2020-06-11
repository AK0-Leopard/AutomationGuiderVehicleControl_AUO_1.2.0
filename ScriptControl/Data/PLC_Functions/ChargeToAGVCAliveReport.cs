using com.mirle.ibg3k0.sc.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using com.mirle.ibg3k0.sc.App;
namespace com.mirle.ibg3k0.sc.Data.PLC_Functions
{
    class ChargeToAGVCAliveReport : PLC_FunBase
    {
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_ALIVE_INDEX")]
        public UInt16 Alive;
    }

}
