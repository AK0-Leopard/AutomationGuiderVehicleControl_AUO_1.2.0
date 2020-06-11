//*********************************************************************************
//      AGVCAliveTimer.cs
//*********************************************************************************
// File Name: AGVCAliveTimer.cs
// Description: 
//
//(c) Copyright 2014, MIRLE Automation Corporation
//
// Date          Author         Request No.    Tag     Description
// ------------- -------------  -------------  ------  -----------------------------


//**********************************************************************************
using com.mirle.ibg3k0.bcf.App;
using com.mirle.ibg3k0.bcf.Controller;
using com.mirle.ibg3k0.bcf.Data.TimerAction;
using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.Data.ValueDefMapAction;
using com.mirle.ibg3k0.sc.Data.VO;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
namespace com.mirle.ibg3k0.sc.Data.TimerAction
{
    class AGVCAliveTimer : ITimerAction
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        protected SCApplication scApp = null;
        protected MPLCSMControl smControl;
        private AEQPT mCharger;
        public AGVCAliveTimer(string name, long intervalMilliSec)
            : base(name, intervalMilliSec)
        {

        }

        public override void initStart()
        {
            
            scApp = SCApplication.getInstance();
            //smControl = scApp.getBCFApplication().getMPLCSMControl("Charger") as MPLCSMControl;
        }
        private static bool switchFlag = true;
        public override void doProcess(object obj)
        {

            bool isWriteSucess = false;
            try
            {
                if (mCharger == null)
                {
                    mCharger = scApp.getEQObjCacheManager().getEquipmentByEQPTID("MCharger");
                }
                ValueWrite isAliveIndexVW = scApp.getBCFApplication().getWriteValueEvent(SCAppConstants.EQPT_OBJECT_CATE_EQPT, "MCharger", "AGVC_TO_CHARGER_ALIVE_INDEX");
                if (isAliveIndexVW == null) return;
                UInt16 isAliveIndex = (UInt16)isAliveIndexVW.getText();
                
                int x = isAliveIndex + 1;
                if (x > 9999) { x = 1; }
                isAliveIndexVW.setWriteValue((UInt16)x);

                if (isWriteSucess || switchFlag)
                {
                    isWriteSucess = false;
                    switchFlag = false;

                    //isWriteSucess = smControl.writeDeviceBlock(isAliveIndexVW);
                    isWriteSucess = ISMControl.writeDeviceBlock(scApp.getBCFApplication(), isAliveIndexVW);
                    switchFlag = true;
                    mCharger.AGVCAliveIndex = x;

                }
                else
                {
                    switchFlag = false;
                    isWriteSucess = false;
                }
            }
            catch (Exception e)
            {
                switchFlag = true;
                isWriteSucess = false;
            }
        }
    }
}