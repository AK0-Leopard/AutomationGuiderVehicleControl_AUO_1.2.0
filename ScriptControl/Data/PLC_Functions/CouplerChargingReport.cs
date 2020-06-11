using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.mirle.ibg3k0.sc.Data.PLC_Functions
{
    class CouplerChargingReport : PLC_FunBase
    {
        public DateTime Timestamp;


        public DateTime ChargingStartTime
        {
            get
            {
                string year = $"20{ChargingStartTimeYear.ToString("X")}";
                string minth = $"{ChargingStartTimeMonth.ToString("X")}".PadLeft(2, '0');
                string day = $"{ChargingStartTimeDay.ToString("X")}".PadLeft(2, '0');
                string hour = $"{ChargingStartTimeHour.ToString("X")}".PadLeft(2, '0');
                string minute = $"{ChargingStartTimeMinute.ToString("X")}".PadLeft(2, '0');
                string second = $"{ChargingStartTimeSecond.ToString("X")}".PadLeft(2, '0');
                string dateTime = $"{year}{minth}{day}{hour}{minute}{second}";
                DateTime parseDateTime = default(DateTime);
                DateTime.TryParseExact(dateTime, SCAppConstants.TimestampFormat_14, CultureInfo.InvariantCulture, DateTimeStyles.None, out parseDateTime);
                return parseDateTime;
            }
        }

        public DateTime ChargingEndTime
        {
            get
            {
                string year = $"20{ChargingEndTimeYear.ToString("X")}";
                string minth = $"{ChargingEndTimeMonth.ToString("X")}".PadLeft(2, '0');
                string day = $"{ChargingEndTimeDay.ToString("X")}".PadLeft(2, '0');
                string hour = $"{ChargingEndTimeHour.ToString("X")}".PadLeft(2, '0');
                string minute = $"{ChargingEndTimeMinute.ToString("X")}".PadLeft(2, '0');
                string second = $"{ChargingEndTimeSecond.ToString("X")}".PadLeft(2, '0');
                string dateTime = $"{year}{minth}{day}{hour}{minute}{second}";
                DateTime parseDateTime = default(DateTime);
                DateTime.TryParseExact(dateTime, SCAppConstants.TimestampFormat_14, CultureInfo.InvariantCulture, DateTimeStyles.None, out parseDateTime);
                return parseDateTime;
            }
        }
        public string LineID;
        public string ChargerID;

        
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_COUPLER_CHANGING_INFO_REPORT_INDEX")]
        public UInt16 Index;
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_COUPLER_CHANGING_INFO_REPORT_COUPLER_ID")]
        public UInt16 CouplerID;
        [JsonIgnore]
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_COUPLER_CHANGING_INFO_REPORT_CHARGING_START_TIME_YEAR")]
        public UInt16 ChargingStartTimeYear;
        [JsonIgnore]
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_COUPLER_CHANGING_INFO_REPORT_CHARGING_START_TIME_MONTH")]
        public UInt16 ChargingStartTimeMonth;
        [JsonIgnore]
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_COUPLER_CHANGING_INFO_REPORT_CHARGING_START_TIME_DAY")]
        public UInt16 ChargingStartTimeDay;
        [JsonIgnore]
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_COUPLER_CHANGING_INFO_REPORT_CHARGING_START_TIME_HOUR")]
        public UInt16 ChargingStartTimeHour;
        [JsonIgnore]
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_COUPLER_CHANGING_INFO_REPORT_CHARGING_START_TIME_MINUTE")]
        public UInt16 ChargingStartTimeMinute;
        [JsonIgnore]
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_COUPLER_CHANGING_INFO_REPORT_CHARGING_START_TIME_SECOND")]
        public UInt16 ChargingStartTimeSecond;
        [JsonIgnore]
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_COUPLER_CHANGING_INFO_REPORT_CHARGING_END_TIME_YEAR")]
        public UInt16 ChargingEndTimeYear;
        [JsonIgnore]
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_COUPLER_CHANGING_INFO_REPORT_CHARGING_END_TIME_MONTH")]
        public UInt16 ChargingEndTimeMonth;
        [JsonIgnore]
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_COUPLER_CHANGING_INFO_REPORT_CHARGING_END_TIME_DAY")]
        public UInt16 ChargingEndTimeDay;
        [JsonIgnore]
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_COUPLER_CHANGING_INFO_REPORT_CHARGING_END_TIME_HOUR")]
        public UInt16 ChargingEndTimeHour;
        [JsonIgnore]
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_COUPLER_CHANGING_INFO_REPORT_CHARGING_END_TIME_MINUTE")]
        public UInt16 ChargingEndTimeMinute;
        [JsonIgnore]
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_COUPLER_CHANGING_INFO_REPORT_CHARGING_END_TIME_SECOND")]
        public UInt16 ChargingEndTimeSecond;
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_COUPLER_CHANGING_INFO_REPORT_INPUT_AH")]
        public float InputAH;
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_COUPLER_CHANGING_INFO_REPORT_CHARGING_RESULT")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ChargingResultType ChargingResult;

        public enum ChargingResultType
        {
            Normal = 1,
            CouplerRequestEnd = 2,
            AGVCRequestEnd = 3
        }


        public override string ToString()
        {
            string sJson = Newtonsoft.Json.JsonConvert.SerializeObject(this, JsHelper.jsBooleanConverter, JsHelper.jsTimeConverter);
            sJson = sJson.Replace(nameof(Timestamp), "@timestamp");
            return sJson;
        }

    }
}
