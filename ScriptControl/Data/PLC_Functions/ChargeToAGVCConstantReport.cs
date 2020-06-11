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
    class ChargeToAGVCConstantReport : PLC_FunBase
    {
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_CHARGER_CURRENT_CONSTANT_REPORT_INDEX")]
        public UInt16 Index;
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_CHARGER_CURRENT_CONSTANT_REPORT_OUTPUT_VOLTAGE")]
        public float OutputVoltage;
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_CHARGER_CURRENT_CONSTANT_REPORT_OUTPUT_CURRENT")]
        public float OutputCurrent;
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_CHARGER_CURRENT_CONSTANT_REPORT_OVERVOLTAGE")]
        public float OverVoltage;
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_CHARGER_CURRENT_CONSTANT_REPORT_OVERCURRENT")]
        public float OverCurrent;
    }
}
