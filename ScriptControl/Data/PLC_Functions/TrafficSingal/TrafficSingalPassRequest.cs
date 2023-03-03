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
    class TrafficSingalPassRequest : PLC_FunBase
    {
        public DateTime Timestamp;

        [PLCElement(ValueName = "TRAFFIC_SINGNAL_MIRLE_AGVC_ASK_PASS")]
        public bool RequestPass;

        public override string ToString()
        {
            string sJson = Newtonsoft.Json.JsonConvert.SerializeObject(this, JsHelper.jsTimeConverter);
            sJson = sJson.Replace(nameof(Timestamp), "@timestamp");
            return sJson;
        }
    }
}
