using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.Data.StateMachine;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.mirle.ibg3k0.sc.Data.VO
{
    public class TrafficControl
    {
        TrafficControlInfo TrafficControlInfo;
        TrafficControlStateMachine StateMachine;
        public TrafficControl(TrafficControlInfo trafficControlInfo)
        {
            TrafficControlInfo = trafficControlInfo;
            StateMachine = new TrafficControlStateMachine();
        }
        public string[] ControlSections
        {
            get { return TrafficControlInfo.ControlSections; }
        }
    }

}
