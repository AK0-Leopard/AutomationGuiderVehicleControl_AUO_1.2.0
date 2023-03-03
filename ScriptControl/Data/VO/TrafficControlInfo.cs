using com.mirle.ibg3k0.sc.Common;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.mirle.ibg3k0.sc.Data.VO
{
    public class TrafficControlInfo
    {
        Logger logger = LogManager.GetCurrentClassLogger();

        public string ID;
        public List<ProtocolFormat.OHTMessage.ReserveInfo> EntrySectionInfos;
        public string[] ControlSections;
        public TrafficControlType TrafficControlType;
    }
    public enum TrafficControlType
    {
        BlockControl,
        WithOrtherAGVC
    }

}
