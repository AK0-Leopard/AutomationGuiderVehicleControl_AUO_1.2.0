using com.mirle.ibg3k0.sc.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using com.mirle.ibg3k0.sc.App;

namespace com.mirle.ibg3k0.sc.Data.PLC_Functions.NorthInnolux
{
    class ChargeToAGVCStatusReport : PLC_FunBase
    {
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_CHARGER_STATUS_REPORT_INDEX")]
        public UInt16 Index;
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_CHARGER_STATUS_REPORT_CHARGER_STATUS_RESERVE")]
        public bool Reserve;
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_CHARGER_STATUS_REPORT_CHARGER_STATUS_CONSTANT_VOLTAGE_OUTPUT")]
        public bool ConstantVoltageOutput;
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_CHARGER_STATUS_REPORT_CHARGER_STATUS_CONSTANT_CURRENT_OUTPUT")]
        public bool ConstantCurrentOutput;
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_CHARGER_STATUS_REPORT_CHARGER_STATUS_HIGH_INPUT_VOLTAGE_PROTECTION")]
        public bool HighInputVoltageProtection;
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_CHARGER_STATUS_REPORT_CHARGER_STATUS_LOW_INPUT_VOLTAGE_PROTECTION")]
        public bool LowInputVoltageProtection;
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_CHARGER_STATUS_REPORT_CHARGER_STATUS_HIGH_OUTPUT_VOLTAGE_PROTECTION")]
        public bool HighOutputVoltageProtection;
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_CHARGER_STATUS_REPORT_CHARGER_STATUS_HIGH_OUTPUT_CURRENT_PROTECTION")]
        public bool HighOutputCurrentProtection;
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_CHARGER_STATUS_REPORT_CHARGER_STATUS_OVERHEAT_PROTECTION")]
        public bool OverheatProtection;
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_CHARGER_STATUS_REPORT_RS485_STATUS")]
        public RS485StatusType RS485Status;
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_CHARGER_STATUS_REPORT_COUPLER1_STATUS")]
        public SCAppConstants.CpuplerStatus_SOUTH_INNOLUX Coupler1Status;
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_CHARGER_STATUS_REPORT_COUPLER2_STATUS")]
        public SCAppConstants.CpuplerStatus_SOUTH_INNOLUX Coupler2Status;
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_CHARGER_STATUS_REPORT_COUPLER3_STATUS")]
        public SCAppConstants.CpuplerStatus_SOUTH_INNOLUX Coupler3Status;
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_CHARGER_STATUS_REPORT_COUPLER1_POSITION")]
        public SCAppConstants.CouplerHPSafety Coupler1Position;
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_CHARGER_STATUS_REPORT_COUPLER2_POSITION")]
        public SCAppConstants.CouplerHPSafety Coupler2Position;
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_CHARGER_STATUS_REPORT_COUPLER3_POSITION")]
        public SCAppConstants.CouplerHPSafety Coupler3Position;


        public enum RS485StatusType
        {
            Normal = 0,
            CRCError = 2,
            DataLengthError = 3,
            CommandCodeError = 4,
            AddressError = 5,
            OutputVoltageOutRange = 6,
            OutputCurrentOutRange = 7,
            DeviceOutputWriteForbid = 8
        }
    }

}
