//*********************************************************************************
//      TrafficLightTimerAction.cs
//*********************************************************************************
// File Name: MESDefaultMapAction.cs
// Description: Type 1 Function
//
//(c) Copyright 2014, MIRLE Automation Corporation
//
// Date          Author         Request No.    Tag     Description
// ------------- -------------  -------------  ------  -----------------------------
//**********************************************************************************
using com.mirle.ibg3k0.bcf.Controller;
using com.mirle.ibg3k0.bcf.Data.TimerAction;
using com.mirle.ibg3k0.sc.App;
using NLog;
using System;

namespace com.mirle.ibg3k0.sc.Data.TimerAction
{
    public class TrafficLightTimerAction : ITimerAction
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        protected SCApplication scApp = null;
        protected MPLCSMControl smControl;
        private DateTime? lastCheckTime = null;
        private DateTime? lastWorkFlashTime = null;

        private DateTime? lastYellowFlashTime = null;
        private bool pre_work_signal = false;
        private bool pre_yellow_signal = false;
        const int check_interval = 1000;
        const int work_flash_interval = 500;
        const int yellow_flash_interval = 500;

        public TrafficLightTimerAction(string name, long intervalMilliSec)
            : base(name, intervalMilliSec)
        {

        }

        public override void initStart()
        {
            scApp = SCApplication.getInstance();
        }

        private long wholeSyncPoint = 0;
        private long syncPoint = 0;
        public override void doProcess(object obj)
        {
            try
            {
                if (System.Threading.Interlocked.Exchange(ref wholeSyncPoint, 1) == 0)
                {
                    try
                    {
                        if(lastCheckTime == null|| lastCheckTime.Value.AddMilliseconds(check_interval)< DateTime.Now)
                        {
                            lastCheckTime = DateTime.Now;
                            scApp.LineService.CheckTrafficLight();
                        }

                        if (scApp.LineService.traffic_work_light_on && scApp.LineService.traffic_work_light_flash)
                        {
                            if (lastWorkFlashTime == null || lastWorkFlashTime.Value.AddMilliseconds(work_flash_interval) < DateTime.Now)
                            {
                                lastWorkFlashTime = DateTime.Now;
                                pre_work_signal = !pre_work_signal;
                                var trafficLight1 = scApp.getEQObjCacheManager().getEquipmentByEQPTID("TrafficLight1");
                                var trafficLight2 = scApp.getEQObjCacheManager().getEquipmentByEQPTID("TrafficLight2");
                                trafficLight1.setTrafficWorkSignal(pre_work_signal);
                                trafficLight2.setTrafficWorkSignal(pre_work_signal);
                            }
                        }
                        else
                        {
                            lastWorkFlashTime = null;
                            pre_work_signal = false;
                        }



                        if (scApp.LineService.traffic_yellow_light_on && scApp.LineService.traffic_yellow_light_flash)
                        {
                            if (lastYellowFlashTime == null || lastYellowFlashTime.Value.AddMilliseconds(yellow_flash_interval) < DateTime.Now)
                            {
                                lastYellowFlashTime = DateTime.Now;
                                pre_yellow_signal = !pre_yellow_signal;
                                var trafficLight1 = scApp.getEQObjCacheManager().getEquipmentByEQPTID("TrafficLight1");
                                var trafficLight2 = scApp.getEQObjCacheManager().getEquipmentByEQPTID("TrafficLight2");
                                trafficLight1.setTrafficYellowSignal(pre_work_signal);
                                trafficLight2.setTrafficYellowSignal(pre_work_signal);
                            }
                        }
                        else
                        {
                            lastYellowFlashTime = null;
                            pre_yellow_signal = false;
                        }

                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Exception");
                    }
                    finally
                    {
                        System.Threading.Interlocked.Exchange(ref wholeSyncPoint, 0);
                    }
                }

            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
            }
        }


    }
}
