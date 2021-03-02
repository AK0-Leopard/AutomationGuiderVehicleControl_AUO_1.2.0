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
    class ColorLightSignalGreenBlue : PLC_FunBase
    {
        public DateTime Timestamp;

        [PLCElement(ValueName = "COLOR_LIGHT_GREEN_SIGNAL")]
        public bool greenSignal;
        [PLCElement(ValueName = "COLOR_LIGHT_BLUE_SIGNAL")]
        public bool blueSignal;

        public override string ToString()
        {
            //string sJson = Newtonsoft.Json.JsonConvert.SerializeObject(this, JsHelper.jsBooleanConverter, JsHelper.jsTimeConverter);
            string sJson = Newtonsoft.Json.JsonConvert.SerializeObject(this, JsHelper.jsTimeConverter);
            sJson = sJson.Replace(nameof(Timestamp), "@timestamp");
            return sJson;
        }
    }
}
