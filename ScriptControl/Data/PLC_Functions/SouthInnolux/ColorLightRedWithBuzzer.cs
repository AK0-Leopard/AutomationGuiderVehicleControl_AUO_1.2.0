using com.mirle.ibg3k0.sc.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using com.mirle.ibg3k0.sc.App;
namespace com.mirle.ibg3k0.sc.Data.PLC_Functions.SouthInnolux
{
    class ColorLightRedWithBuzzer : PLC_FunBase
    {
        public DateTime Timestamp;

        [PLCElement(ValueName = "COLOR_LIGHT_RED_SIGNAL")]
        public bool redSignal;
        [PLCElement(ValueName = "COLOR_LIGHT_BUZZER_SIGNAL")]
        public bool buzzerSignal;

        public override string ToString()
        {
            //string sJson = Newtonsoft.Json.JsonConvert.SerializeObject(this, JsHelper.jsBooleanConverter, JsHelper.jsTimeConverter);
            string sJson = Newtonsoft.Json.JsonConvert.SerializeObject(this, JsHelper.jsTimeConverter);
            sJson = sJson.Replace(nameof(Timestamp), "@timestamp");
            return sJson;
        }
    }
}
