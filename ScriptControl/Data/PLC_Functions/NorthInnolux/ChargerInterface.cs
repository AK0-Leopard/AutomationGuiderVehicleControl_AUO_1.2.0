using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using com.mirle.ibg3k0.bcf.App;
using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Common;
using Newtonsoft.Json;

namespace com.mirle.ibg3k0.sc.Data.PLC_Functions.NorthInnolux
{
    public class ChargerInterface : PLC_FunBase
    {
        [PLCElement(ValueName = "CHARGERX_TO_AGVC_PIO_HANDSHAKE_REPORT_INDEX")]
        public UInt16 Index;
        public List<ChargerInterfaceDetail> RawDetails = new List<ChargerInterfaceDetail>();
        public List<ChargerInterfaceDetail> Details = new List<ChargerInterfaceDetail>();

        public override string ToString()
        {
            string sJson = Newtonsoft.Json.JsonConvert.SerializeObject(this);
            return sJson;
        }

        public new void Read(BCFApplication bcfApp, string eqObjIDCate, string eq_id, int item_count)
        {
            Details.Clear();
            RawDetails.Clear();
            for (int i = 1; i <= item_count; i++)
            {
                ChargerInterfaceDetail interfaceDetail = new ChargerInterfaceDetail();
                interfaceDetail.Index = $"Recode{nameof(ChargerInterface)}";
                interfaceDetail.EQ_ID = this.EQ_ID;
                interfaceDetail.Read(bcfApp, eqObjIDCate, eq_id, i);
                RawDetails.Add(interfaceDetail);
            }
            base.Read(bcfApp, eqObjIDCate, eq_id);
        }

        public class ChargerInterfaceDetail : PLC_FunBase
        {
            [PLCElement(ValueName = "CHARGERX_TO_AGVC_PIO_HANDSHAKE_REPORT_COUPLER_")]
            public UInt16 CouplerID;
            [JsonIgnore]
            [PLCElement(ValueName = "CHARGERX_TO_AGVC_PIO_HANDSHAKE_REPORT_YEAR_")]
            public UInt16 Year;
            [JsonIgnore]
            [PLCElement(ValueName = "CHARGERX_TO_AGVC_PIO_HANDSHAKE_REPORT_MONTH_")]
            public UInt16 Month;
            [JsonIgnore]
            [PLCElement(ValueName = "CHARGERX_TO_AGVC_PIO_HANDSHAKE_REPORT_DAY_")]
            public UInt16 Day;
            [JsonIgnore]
            [PLCElement(ValueName = "CHARGERX_TO_AGVC_PIO_HANDSHAKE_REPORT_HOUR_")]
            public UInt16 Hour;
            [JsonIgnore]
            [PLCElement(ValueName = "CHARGERX_TO_AGVC_PIO_HANDSHAKE_REPORT_MINUTE_")]
            public UInt16 Minute;
            [JsonIgnore]
            [PLCElement(ValueName = "CHARGERX_TO_AGVC_PIO_HANDSHAKE_REPORT_SECOND_")]
            public UInt16 Second;
            [JsonIgnore]
            [PLCElement(ValueName = "CHARGERX_TO_AGVC_PIO_HANDSHAKE_REPORT_MILLISECOND_")]
            public UInt16 Millisecond;

            public DateTime Timestamp
            {
                get
                {
                    string year = $"20{Year.ToString("X")}";
                    string minth = $"{Month.ToString("X")}".PadLeft(2,'0');
                    string day = $"{Day.ToString("X")}".PadLeft(2, '0');
                    string hour = $"{Hour.ToString("X")}".PadLeft(2, '0');
                    string minute = $"{Minute.ToString("X")}".PadLeft(2, '0');
                    string second = $"{Second.ToString("X")}".PadLeft(2, '0');
                    string millisecion = $"{Millisecond.ToString("X")}".PadLeft(3, '0');
                    string dateTime = $"{year}{minth}{day}{hour}{minute}{second}{millisecion}";
                    DateTime parseDateTime = default(DateTime);
                    DateTime.TryParseExact(dateTime, SCAppConstants.TimestampFormat_17, CultureInfo.InvariantCulture, DateTimeStyles.None, out parseDateTime);
                    return parseDateTime;
                }
            }

            [JsonIgnore]
            [PLCElement(ValueName = "CHARGERX_TO_AGVC_PIO_HANDSHAKE_REPORT_SIGNAL1_")]
            public Boolean[] ChargerSigna1;
            [JsonIgnore]
            [PLCElement(ValueName = "CHARGERX_TO_AGVC_PIO_HANDSHAKE_REPORT_SIGNAL2_")]
            public Boolean[] ChargerSigna2;
            public string SChargerSigna1 { get { return string.Join(",", ChargerSigna1); } }
            public string SChargerSigna2 { get { return string.Join(",", ChargerSigna2); } }
            [JsonIgnore]
            public override string EQ_ID { get => base.EQ_ID; set => base.EQ_ID = value; }
            public string ChargeID { get => base.EQ_ID; }

            public override string ToString()
            {
                string sJson = Newtonsoft.Json.JsonConvert.SerializeObject(this, JsHelper.jsBooleanConverter, JsHelper.jsTimeConverter);
                sJson = sJson.Replace(nameof(Timestamp), "@timestamp");
                return sJson;
            }

        }
    }
}
