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
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.Data.VO;
using com.mirle.ibg3k0.sc.Service;
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
        private long syncPoint = 0;
        public override void doProcess(object obj)
        {
            if (System.Threading.Interlocked.Exchange(ref syncPoint, 1) == 0)
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
                                TrafficControlBLL.TrafficControlLogger.Info($"有車輛正在要求 traffic control:{traffic_controller.EQPT_ID}的通行權...");
                                if (traffic_controller.IsOverAGVRequestTime())
                                {
                                    TrafficControlBLL.TrafficControlLogger.Info($"已間隔{TrafficController.MAX_AGV_REQUEST_INTRRVAL_TIME_MS} ms,AGV無再重複要求，取消通行需求");
                                    traffic_controller.CancelRequestForRightOfWay();
                                }
                                else
                                {
                                    TrafficControlBLL.TrafficControlLogger.Info($"嘗試發出通行需求...");
                                    traffic_controller.AGVCRequestForRightOfWay();
                                }
                                break;
                            case StateMachine.TrafficControlStateMachine.State.WaitReply:
                                //這邊要持續確認AGV是否還有通過的需求，如果沒有的話就可以直接cancel request
                                TrafficControlBLL.TrafficControlLogger.Info($"等待 Traffic Control回復通行許可...");
                                if (traffic_controller.IsReadyReplyPass)
                                {
                                    TrafficControlBLL.TrafficControlLogger.Info($"收到Traffic Control回復通行許可");

                                    traffic_controller.AGVCAcquireRightOfWay();
                                }
                                else
                                {
                                    if (traffic_controller.IsOverAGVRequestTime())
                                    {
                                        TrafficControlBLL.TrafficControlLogger.Info($"已間隔{TrafficController.MAX_AGV_REQUEST_INTRRVAL_TIME_MS} ms,AGV無再重複要求，取消通行需求");
                                        traffic_controller.CancelRequestForRightOfWay();
                                    }
                                }
                                break;
                            case StateMachine.TrafficControlStateMachine.State.AllowedEntry:
                                //持續確認AGV是否已經通過(確認Reserve圖資、Section的資料)，如果已通過或無需求，就呼叫Complete
                                if (traffic_controller.IsNoAGVWiilPass(scApp.CMDBLL, scApp.VehicleBLL, scApp.AddressesBLL))
                                {
                                    TrafficControlBLL.TrafficControlLogger.Info($"已無AGV無管制路段中,重置通行需求");
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
                finally
                {
                    System.Threading.Interlocked.Exchange(ref syncPoint, 0);
                }
            }
        }

    }
}
