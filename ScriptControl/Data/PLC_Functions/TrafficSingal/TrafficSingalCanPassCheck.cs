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
    class TrafficSingalCanPassCheck : PLC_FunBase
    {
        public DateTime Timestamp;

        [PLCElement(ValueName = "TRAFFIC_SINGNAL_AUO_AGVC_ACK_PASS")]
        public bool CanPass;

        public override string ToString()
        {
            string sJson = Newtonsoft.Json.JsonConvert.SerializeObject(this, JsHelper.jsTimeConverter);
            sJson = sJson.Replace(nameof(Timestamp), "@timestamp");
            return sJson;
        }
    }
}
