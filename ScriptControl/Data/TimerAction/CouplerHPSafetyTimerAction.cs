//*********************************************************************************
//      ZoneBlockCheck.cs
//*********************************************************************************
// File Name: ZoneBlockCheck.cs
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
using com.mirle.ibg3k0.sc.Data.VO;
using com.mirle.ibg3k0.sc.ProtocolFormat.OHTMessage;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.mirle.ibg3k0.sc.Data.TimerAction
{
    /// <summary>
    /// Class ZoneBlockCheck.
    /// </summary>
    /// <seealso cref="com.mirle.ibg3k0.bcf.Data.TimerAction.ITimerAction" />
    class CouplerHPSafetyTimerAction : ITimerAction
    {
        /// <summary>
        /// The logger
        /// </summary>
        private static Logger logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// The sc application
        /// </summary>
        protected SCApplication scApp = null;
        const string DEVICE_NAME = "AGVC";


        /// <summary>
        /// Initializes a new instance of the <see cref="DeadlockCheck"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="intervalMilliSec">The interval milli sec.</param>
        public CouplerHPSafetyTimerAction(string name, long intervalMilliSec)
            : base(name, intervalMilliSec)
        {

        }

        /// <summary>
        /// Initializes the start.
        /// </summary>
        public override void initStart()
        {
            //do nothing
            scApp = SCApplication.getInstance();

        }

        private long checkSyncPoint = 0;
        /// <summary>
        /// Timer Action的執行動作
        /// </summary>
        /// <param name="obj">The object.</param>
        public override void doProcess(object obj)
        {
            if (System.Threading.Interlocked.Exchange(ref checkSyncPoint, 1) == 0)
            {
                try
                {
                    List<AUNIT> unitList =  scApp.getEQObjCacheManager().getAllUnit();
                    foreach (AUNIT unit in unitList)
                    {
                        var mapAction = unit
                        .getMapActionByIdentityKey(nameof(com.mirle.ibg3k0.sc.Data.ValueDefMapAction.NorthInnolux.SubChargerValueDefMapAction)) as
                            com.mirle.ibg3k0.sc.Data.ValueDefMapAction.NorthInnolux.SubChargerValueDefMapAction;
                        if (mapAction != null)
                        {

                            if (unit.coupler1HPSafety == SCAppConstants.CouplerHPSafety.NonSafety && !unit.coupler1SegmentDisableByAGVC)
                            {
                                CouplerAddress coupler_address = scApp.AddressesBLL.cache.GetCouplerAddress(unit.UNIT_ID, CouplerNum.NumberOne);
                                if (coupler_address != null)
                                    unit.coupler1SegmentDisableByAGVC = mapAction.couplerSafetyHandler(SCAppConstants.CouplerHPSafety.NonSafety, coupler_address, CouplerNum.NumberOne);
                            }
                            else if (unit.coupler1HPSafety == SCAppConstants.CouplerHPSafety.Safety)
                            {
                                CouplerAddress coupler_address = scApp.AddressesBLL.cache.GetCouplerAddress(unit.UNIT_ID, CouplerNum.NumberOne);
                                if (coupler_address != null)
                                    unit.coupler1SegmentDisableByAGVC = mapAction.couplerSafetyHandler(SCAppConstants.CouplerHPSafety.Safety, coupler_address, CouplerNum.NumberOne);
                            }

                            if (unit.coupler2HPSafety == SCAppConstants.CouplerHPSafety.NonSafety && !unit.coupler2SegmentDisableByAGVC)
                            {
                                CouplerAddress coupler_address = scApp.AddressesBLL.cache.GetCouplerAddress(unit.UNIT_ID, CouplerNum.NumberTwo);
                                if (coupler_address != null)
                                    unit.coupler2SegmentDisableByAGVC = mapAction.couplerSafetyHandler(SCAppConstants.CouplerHPSafety.NonSafety, coupler_address, CouplerNum.NumberTwo);
                            }
                            else if (unit.coupler2HPSafety == SCAppConstants.CouplerHPSafety.Safety)
                            {
                                CouplerAddress coupler_address = scApp.AddressesBLL.cache.GetCouplerAddress(unit.UNIT_ID, CouplerNum.NumberTwo);
                                if (coupler_address != null)
                                    unit.coupler2SegmentDisableByAGVC = mapAction.couplerSafetyHandler(SCAppConstants.CouplerHPSafety.Safety, coupler_address, CouplerNum.NumberTwo);
                            }

                            if (unit.coupler3HPSafety == SCAppConstants.CouplerHPSafety.NonSafety && !unit.coupler3SegmentDisableByAGVC)
                            {
                                CouplerAddress coupler_address = scApp.AddressesBLL.cache.GetCouplerAddress(unit.UNIT_ID, CouplerNum.NumberThree);
                                if (coupler_address != null)
                                    unit.coupler3SegmentDisableByAGVC = mapAction.couplerSafetyHandler(SCAppConstants.CouplerHPSafety.NonSafety, coupler_address, CouplerNum.NumberThree);
                            }
                            else if (unit.coupler3HPSafety == SCAppConstants.CouplerHPSafety.Safety)
                            {
                                CouplerAddress coupler_address = scApp.AddressesBLL.cache.GetCouplerAddress(unit.UNIT_ID, CouplerNum.NumberThree);
                                if (coupler_address != null)
                                    unit.coupler3SegmentDisableByAGVC = mapAction.couplerSafetyHandler(SCAppConstants.CouplerHPSafety.Safety, coupler_address, CouplerNum.NumberThree);
                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(CouplerHPSafetyTimerAction), Device: DEVICE_NAME,
                       Data: ex);
                }
                finally
                {
                    System.Threading.Interlocked.Exchange(ref checkSyncPoint, 0);
                }
            }
        }


    }
}