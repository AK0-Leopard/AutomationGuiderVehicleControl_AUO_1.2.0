using com.mirle.ibg3k0.bcf.App;
using com.mirle.ibg3k0.bcf.Common;
using com.mirle.ibg3k0.bcf.Data.ValueDefMapAction;
using com.mirle.ibg3k0.bcf.Data.VO;
using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.Data.SECS;
using com.mirle.ibg3k0.sc.Data.VO;
using com.mirle.ibg3k0.sc.Data.VO.Interface;
using com.mirle.ibg3k0.sc.ObjectRelay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.mirle.ibg3k0.sc
{
    public partial class APORTSTATION : BaseEQObject
    {
        public string CST_ID { get; set; }
        public string EQPT_ID { get; set; }
        public override void doShareMemoryInit(BCFAppConstants.RUN_LEVEL runLevel)
        {
            foreach (IValueDefMapAction action in valueDefMapActionDic.Values)
            {
                action.doShareMemoryInit(runLevel);
            }
        }

        public string GroupID
        {
            get
            {
                string[] temp = PORT_ID.Contains(":") ? PORT_ID.Split(':') : null;
                if (temp != null)
                {
                    return temp[0];
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public override string ToString()
        {
            return $"{PORT_ID} ({ADR_ID})";
        }

    }

}
