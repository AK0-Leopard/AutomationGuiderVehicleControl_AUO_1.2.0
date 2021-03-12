using com.mirle.ibg3k0.sc.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.mirle.ibg3k0.sc.Data.PLC_Functions.NorthInnolux
{
    class ChargeToAGVCCurrentStatus : PLC_FunBase
    {
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_CHARGER_CURRENT_CHARGING_STATUS_REPORT_INPUT_VOLTAGE")]
        public float InputVoltage;
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_CHARGER_CURRENT_CHARGING_STATUS_REPORT_CHARGING_VOLTAGE")]
        public float ChargingVoltage;
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_CHARGER_CURRENT_CHARGING_STATUS_REPORT_CHARGING_CURRENT")]
        public float ChargingCurrent;
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_CHARGER_CURRENT_CHARGING_STATUS_REPORT_CHARGING_POWER")]
        public float ChargingPower;
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_CHARGER_CURRENT_CHARGING_STATUS_REPORT_COUPLER_CHARGING_VOLTAGE")]
        public float CouplerChargingVoltage;
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_CHARGER_CURRENT_CHARGING_STATUS_REPORT_COUPLER_CHARGING_CURRENT")]
        public float CouplerChargingCurrent;
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_CHARGER_CURRENT_CHARGING_STATUS_REPORT_COUPLER_ID")]
        public UInt16 CouplerID;
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_CHARGER_CURRENT_CHARGING_STATUS_REPORT_BLOCK")]
        public UInt16 BlockValue;
        
    }
}
