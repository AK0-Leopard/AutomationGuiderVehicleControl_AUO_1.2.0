using com.mirle.ibg3k0.sc.App;
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
    class AGVCToCharger_DateTimeSync : PLC_FunBase
    {
        [JsonIgnore]
        [PLCElement(ValueName = "AGVC_TO_CHARGER_DATE_TIME_SYNC_COMMAND_YEAR")]
        public uint Year;
        [JsonIgnore]
        [PLCElement(ValueName = "AGVC_TO_CHARGER_DATE_TIME_SYNC_COMMAND_MONTH")]
        public uint Month;
        [JsonIgnore]
        [PLCElement(ValueName = "AGVC_TO_CHARGER_DATE_TIME_SYNC_COMMAND_DAY")]
        public uint Day;
        [JsonIgnore]
        [PLCElement(ValueName = "AGVC_TO_CHARGER_DATE_TIME_SYNC_COMMAND_HOUR")]
        public uint Hour;
        [JsonIgnore]
        [PLCElement(ValueName = "AGVC_TO_CHARGER_DATE_TIME_SYNC_COMMAND_MINUTE")]
        public uint Min;
        [JsonIgnore]
        [PLCElement(ValueName = "AGVC_TO_CHARGER_DATE_TIME_SYNC_COMMAND_SECOND")]
        public uint Sec;
        [PLCElement(ValueName = "AGVC_TO_CHARGER_DATE_TIME_SYNC_COMMAND_INDEX", IsIndexProp = true)]
        public uint Index;


        public DateTime DateTime
        {
            get
            {
                string year = $"{Year.ToString("X")}";
                string minth = $"{Month.ToString("X")}".PadLeft(2, '0');
                string day = $"{Day.ToString("X")}".PadLeft(2, '0');
                string hour = $"{Hour.ToString("X")}".PadLeft(2, '0');
                string minute = $"{Min.ToString("X")}".PadLeft(2, '0');
                string second = $"{Sec.ToString("X")}".PadLeft(2, '0');
                string dateTime = $"{year}{minth}{day}{hour}{minute}{second}";
                DateTime parseDateTime = default(DateTime);
                DateTime.TryParseExact(dateTime, SCAppConstants.TimestampFormat_14, CultureInfo.InvariantCulture, DateTimeStyles.None, out parseDateTime);
                return parseDateTime;
            }
        }
        public override string ToString()
        {
            string sJson = Newtonsoft.Json.JsonConvert.SerializeObject(this, JsHelper.jsBooleanConverter, JsHelper.jsTimeConverter);
            return sJson;
        }
    }


}
