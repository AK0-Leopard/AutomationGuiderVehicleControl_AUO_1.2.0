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
    class MCToAGVCAbnormalReport : PLC_FunBase
    {
        [PLCElement(ValueName = "MCHARGER_TO_AGVC_ABNORMAL_CHARGING_ERRORCODE1")]
        public UInt16 ErrorCode_1;
        [PLCElement(ValueName = "MCHARGER_TO_AGVC_ABNORMAL_CHARGING_ERRORCODE2")]
        public UInt16 ErrorCode_2;
        [PLCElement(ValueName = "MCHARGER_TO_AGVC_ABNORMAL_CHARGING_ERRORCODE3")]
        public UInt16 ErrorCode_3;
        [PLCElement(ValueName = "MCHARGER_TO_AGVC_ABNORMAL_CHARGING_ERRORCODE4")]
        public UInt16 ErrorCode_4;
        [PLCElement(ValueName = "MCHARGER_TO_AGVC_ABNORMAL_CHARGING_ERRORCODE5")]
        public UInt16 ErrorCode_5;
        [PLCElement(ValueName = "MCHARGER_TO_AGVC_ABNORMAL_CHARGING_REPORT_INDEX")]
        public UInt16 index;
        public List<string> loadCurrentHappendAlarms()
        {
            var alarms = new List<UInt16>();
            if (ErrorCode_1 != 0)
                alarms.Add(ErrorCode_1);
            if (ErrorCode_2 != 0)
                alarms.Add(ErrorCode_2);
            if (ErrorCode_3 != 0)
                alarms.Add(ErrorCode_3);
            if (ErrorCode_4 != 0)
                alarms.Add(ErrorCode_4);
            if (ErrorCode_5 != 0)
                alarms.Add(ErrorCode_5);
            return alarms.Select(error_code => error_code.ToString()).ToList();
        }

    }

}
