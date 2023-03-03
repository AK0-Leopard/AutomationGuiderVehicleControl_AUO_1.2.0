// ***********************************************************************
// Assembly         : ScriptControl
// Author           : 
// Created          : 03-31-2016
//
// Last Modified By : 
// Last Modified On : 03-24-2016
// ***********************************************************************
// <copyright file="TraceDataReportTimer.cs" company="">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using com.mirle.ibg3k0.bcf.Data.TimerAction;
using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.BLL;
using NLog;
using System;

namespace com.mirle.ibg3k0.sc.Data.TimerAction
{
    public class TrafficControlCheckTimerAction : ITimerAction
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private SCApplication scApp = null;
        public TrafficControlCheckTimerAction(string name, long intervalMilliSec)
            : base(name, intervalMilliSec)
        {

        }
        public override void initStart()
        {
            scApp = SCApplication.getInstance();
        }
        public override void doProcess(object obj)
        {
            try
            {
                var traffic_controllers = scApp.TrafficControlBLL.cache.LoadAllTrafficController();
                foreach (var traffic_controller in traffic_controllers)
                {
                    switch (traffic_controller.TrafficControlState)
                    {
                        case StateMachine.TrafficControlStateMachine.State.NotEntry:
                            //not thing...
                            break;
                        case StateMachine.TrafficControlStateMachine.State.AGVRequest:
                            traffic_controller.AGVCRequestForRightOfWay();
                            break;
                        case StateMachine.TrafficControlStateMachine.State.WaitReply:
                            //這邊要持續確認AGV是否還有通過的需求，如果沒有的話就可以直接cancel request
                            if (traffic_controller.IsReadyReplyPass)
                            {
                                traffic_controller.AGVCAcquireRightOfWay();
                            }
                            else
                            {
                                if (traffic_controller.IsOverAGVRequestTime())
                                {
                                    traffic_controller.CancelRequestForRightOfWay();
                                }
                            }
                            break;
                        case StateMachine.TrafficControlStateMachine.State.AllowedEntry:
                            //持續確認AGV是否已經通過(確認Reserve圖資、Section的資料)，如果已通過或無需求，就呼叫Complete
                            if (traffic_controller.IsNoAGVWiilPass(scApp.VehicleBLL, scApp.ReserveBLL))
                            {
                                traffic_controller.ReturnRightOfWay();
                            }
                            break;
                    }


                }


            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception:");
            }
        }

    }
}
