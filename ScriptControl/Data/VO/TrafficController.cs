using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.Data.StateMachine;
using com.mirle.ibg3k0.sc.Data.ValueDefMapAction;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.mirle.ibg3k0.sc.Data.VO
{
    public class TrafficControl : AEQPT
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
        public bool IsCanPass { get; set; }

        //向Server發出通過的請求
        public void RequestPass(bool isAsk)
        {
            getExcuteMapAction().SendTrafficSignalMirlePassAsk(isAsk);
        }
        private TrafficSingalDefaultValueDefMapAction getExcuteMapAction()
        {
            TrafficSingalDefaultValueDefMapAction mapAction;
            mapAction = this.getMapActionByIdentityKey(typeof(TrafficSingalDefaultValueDefMapAction).Name) as TrafficSingalDefaultValueDefMapAction;
            return mapAction;
        }

    }

}
