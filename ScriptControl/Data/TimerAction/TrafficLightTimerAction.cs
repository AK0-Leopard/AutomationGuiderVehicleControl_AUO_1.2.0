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
                        scApp.LineService.CheckTrafficLight();
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
