using com.mirle.ibg3k0.sc.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.mirle.ibg3k0.sc.Data.PLC_Functions
{
    class AGVCToChargerCouplerEnableDisable : PLC_FunBase
    {
        [PLCElement(ValueName = "AGVC_TO_CHARGER_COUPLER_ID")]
        public uint CouplerID;
        [PLCElement(ValueName = "AGVC_TO_CHARGER_COUPLER_ENABLE_DISABLE")]
        public uint Enable;
        [PLCElement(ValueName = "AGVC_TO_CHARGER_COUPLER_ENABLE_DISABLE_INDEX")]
        public uint Index;
    }


}
