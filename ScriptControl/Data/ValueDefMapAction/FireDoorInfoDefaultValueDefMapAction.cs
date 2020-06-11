//*********************************************************************************
//      MTLValueDefMapAction.cs
//*********************************************************************************
// File Name: MTLValueDefMapAction.cs
// Description: 
//
//(c) Copyright 2018, MIRLE Automation Corporation
//
// Date          Author         Request No.    Tag     Description
// ------------- -------------  -------------  ------  -----------------------------
//**********************************************************************************
using com.mirle.ibg3k0.bcf.App;
using com.mirle.ibg3k0.bcf.Common;
using com.mirle.ibg3k0.bcf.Common.MPLC;
using com.mirle.ibg3k0.bcf.Controller;
using com.mirle.ibg3k0.bcf.Data.ValueDefMapAction;
using com.mirle.ibg3k0.bcf.Data.VO;
using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.Data.PLC_Functions;
using com.mirle.ibg3k0.sc.ProtocolFormat.OHTMessage;
using KingAOP;
using NLog;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;
using System.Text;

namespace com.mirle.ibg3k0.sc.Data.ValueDefMapAction
{
    public class FireDoorInfoDefaultValueDefMapAction : IValueDefMapAction
    {
        public const string DEVICE_NAME_MTL = "EQ";
        Logger logger = NLog.LogManager.GetCurrentClassLogger();
        AEQPT eqpt = null;
        protected SCApplication scApp = null;
        protected BCFApplication bcfApp = null;
        public FireDoorInfoDefaultValueDefMapAction()
            : base()
        {
            scApp = SCApplication.getInstance();
            bcfApp = scApp.getBCFApplication();
        }
        public DynamicMetaObject GetMetaObject(Expression parameter)
        {
            return new AspectWeaver(parameter, this);
        }

        public virtual string getIdentityKey()
        {
            return this.GetType().Name;
        }
        public virtual void setContext(BaseEQObject baseEQ)
        {
            this.eqpt = baseEQ as AEQPT;

        }
        public virtual void unRegisterEvent()
        {
            //not implement
        }
        public virtual void doShareMemoryInit(BCFAppConstants.RUN_LEVEL runLevel)
        {
            try
            {
                switch (runLevel)
                {
                    case BCFAppConstants.RUN_LEVEL.ZERO:
                        initFireReport();
                        //initFireDoorData();
                        break;
                    case BCFAppConstants.RUN_LEVEL.ONE:
                        break;
                    case BCFAppConstants.RUN_LEVEL.TWO:
                        break;
                    case BCFAppConstants.RUN_LEVEL.NINE:
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exection:");
            }
        }

        private void initFireReport()
        {
            var function = scApp.getFunBaseObj<FireReport>(eqpt.EQPT_ID) as FireReport;
            try
            {
                //1.建立各個Function物件
                function.Read(bcfApp, eqpt.EqptObjectCate, eqpt.EQPT_ID);
                //2.read log
                function.Timestamp = DateTime.Now;
                LogManager.GetLogger("com.mirle.ibg3k0.sc.Common.LogHelper").Info(function.ToString());

                //3.logical (include db save)
                eqpt.fireReport = !function.FIRE_REPORT || !function.TEST_FIRE_REPORT;//讀入訊號時要反過來看，方能符合客戶規範
                if (eqpt.fireReport)//火災發生
                {
                    foreach (AUNIT unit in eqpt.UnitList)
                    {
                        if (unit.isOKtoBanRoute())//該區域沒有被預約，直接ban路徑
                        {
                            foreach (ASEGMENT seg in scApp.getCommObjCacheManager().getFireDoorSegment(unit.UNIT_ID))
                            {
                                var ban_result = scApp.VehicleService.doEnableDisableSegment(seg.SEG_ID, E_PORT_STATUS.OutOfService);//Ban 路徑
                                if (ban_result.isSuccess)
                                {
                                    seg.DISABLE_TIME = ban_result.segment.DISABLE_TIME;
                                    seg.STATUS = ban_result.segment.STATUS;
                                }
                            }
                        }
                        else
                        {
                            //do nothing
                        }
                    }
                    bool result = true;
                    foreach (AUNIT unit in eqpt.UnitList)
                    {
                        foreach (ASEGMENT seg in scApp.getCommObjCacheManager().getFireDoorSegment(unit.UNIT_ID))
                        {
                            if (seg.STATUS == E_SEG_STATUS.Active)
                            {
                                result = false;
                            }
                        }
                    }
                    if (result)//所有門的路段都被Ban了
                    {
                        eqpt.fireDoorCancelAbortCommand(scApp);
                    }
                    else
                    {
                        //do nothing
                    }
                }
                else//火災終止
                {
                    //解Ban該防火門所在segment
                    foreach (AUNIT unit in eqpt.UnitList)
                    {
                        foreach (ASEGMENT seg in scApp.getCommObjCacheManager().getFireDoorSegment(unit.UNIT_ID))
                        {
                            var ban_result = scApp.VehicleService.doEnableDisableSegment(seg.SEG_ID, E_PORT_STATUS.InService);//Ban 路徑
                            if (ban_result.isSuccess)
                            {
                                seg.DISABLE_TIME = ban_result.segment.DISABLE_TIME;
                                seg.STATUS = ban_result.segment.STATUS;
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
            }
            finally
            {
                scApp.putFunBaseObj<FireReport>(function);
            }
        }


        //private void initFireDoorData()
        //{
        //    var function = scApp.getFunBaseObj<FireDoorData>(eqpt.EQPT_ID) as FireDoorData;
        //    try
        //    {
        //        //1.建立各個Function物件
        //        function.Read(bcfApp, eqpt.EqptObjectCate, eqpt.EQPT_ID);
        //        //2.read log
        //        function.Timestamp = DateTime.Now;
        //        LogManager.GetLogger("com.mirle.ibg3k0.sc.Common.LogHelper").Info(function.ToString());
        //        //LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(EQStatusReport), Device: DEVICE_NAME_MTL,
        //        //    XID: eqpt.EQPT_ID, Data: function.ToString());

        //        //3.logical (include db save)
        //        eqpt.fireDoorOpen = function.FIRE_DOOR_OPEN;
        //        eqpt.fireDoorCloseGrant = function.FIRE_DOOR_CLOSE_GRANT;
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.Error(ex, "Exception");
        //    }
        //    finally
        //    {
        //        scApp.putFunBaseObj<FireDoorData>(function);
        //    }
        //}
        private void FireReportChange(object sender, ValueChangedEventArgs e)
        {
            var function = scApp.getFunBaseObj<FireReport>(eqpt.EQPT_ID) as FireReport;
            try
            {
                //1.建立各個Function物件
                function.Read(bcfApp, eqpt.EqptObjectCate, eqpt.EQPT_ID);
                //2.read log
                function.Timestamp = DateTime.Now;
                LogManager.GetLogger("com.mirle.ibg3k0.sc.Common.LogHelper").Info(function.ToString());

                //3.logical (include db save)
                eqpt.fireReport = !function.FIRE_REPORT || !function.TEST_FIRE_REPORT;

                if (eqpt.fireReport)//火災發生
                {
                    foreach (AUNIT unit in eqpt.UnitList)
                    {
                        if (unit.isOKtoBanRoute())//該區域沒有被預約，直接ban路徑
                        {
                            foreach (ASEGMENT seg in scApp.getCommObjCacheManager().getFireDoorSegment(unit.UNIT_ID))
                            {
                                var ban_result = scApp.VehicleService.doEnableDisableSegment(seg.SEG_ID, E_PORT_STATUS.OutOfService);//Ban 路徑
                                if (ban_result.isSuccess)
                                {
                                    seg.DISABLE_TIME = ban_result.segment.DISABLE_TIME;
                                    seg.STATUS = ban_result.segment.STATUS;
                                }
                            }
                        }
                        else
                        {
                            //do nothing
                        }
                    }
                    bool result = true;
                    foreach (AUNIT unit in eqpt.UnitList)
                    {
                        foreach (ASEGMENT seg in scApp.getCommObjCacheManager().getFireDoorSegment(unit.UNIT_ID))
                        {
                            if (seg.STATUS == E_SEG_STATUS.Active)
                            {
                                result = false;
                            }
                        }
                    }
                    if (result)//所有門的路段都被Ban了
                    {
                        eqpt.fireDoorCancelAbortCommand(scApp);
                    }
                    else
                    {
                        //do nothing
                    }
                }
                else//火災終止
                {
                    //解Ban該防火門所在segment
                    foreach (AUNIT unit in eqpt.UnitList)
                    {
                        foreach (ASEGMENT seg in scApp.getCommObjCacheManager().getFireDoorSegment(unit.UNIT_ID))
                        {
                            var ban_result = scApp.VehicleService.doEnableDisableSegment(seg.SEG_ID, E_PORT_STATUS.InService);//Ban 路徑
                            if (ban_result.isSuccess)
                            {
                                seg.DISABLE_TIME = ban_result.segment.DISABLE_TIME;
                                seg.STATUS = ban_result.segment.STATUS;
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
            }
            finally
            {
                scApp.putFunBaseObj<FireReport>(function);
            }
        }


        //public void sendFireDoorCrossSignal(bool signal)
        //{
        //    var function =
        //        scApp.getFunBaseObj<FireDoorCross>(eqpt.EQPT_ID) as FireDoorCross;
        //    try
        //    {
        //        //1.建立各個Function物件
        //        function.FIRE_DOOR_CROSS = signal;
        //        function.Write(bcfApp, eqpt.EqptObjectCate, eqpt.EQPT_ID);
        //        //2.write log
        //        function.Timestamp = DateTime.Now;
        //        LogManager.GetLogger("com.mirle.ibg3k0.sc.Common.LogHelper").Info(function.ToString());

        //        //3.logical (include db save)
        //        eqpt.fireDoorCrossingSignal = signal;
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.Error(ex, "Exception");
        //    }
        //    finally
        //    {
        //        scApp.putFunBaseObj<FireDoorCross>(function);
        //    }
        //}

        //private void FireDoorDataChange(object sender, ValueChangedEventArgs e)
        //{
        //    var function = scApp.getFunBaseObj<FireDoorData>(eqpt.EQPT_ID) as FireDoorData;
        //    try
        //    {
        //        //1.建立各個Function物件
        //        function.Read(bcfApp, eqpt.EqptObjectCate, eqpt.EQPT_ID);
        //        //2.read log
        //        function.Timestamp = DateTime.Now;
        //        LogManager.GetLogger("com.mirle.ibg3k0.sc.Common.LogHelper").Info(function.ToString());
        //        //LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(EQStatusReport), Device: DEVICE_NAME_MTL,
        //        //    XID: eqpt.EQPT_ID, Data: function.ToString());

        //        //3.logical (include db save)
        //        eqpt.fireDoorOpen = function.FIRE_DOOR_OPEN;
        //        eqpt.fireDoorCloseGrant = function.FIRE_DOOR_CLOSE_GRANT;
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.Error(ex, "Exception");
        //    }
        //    finally
        //    {
        //        scApp.putFunBaseObj<FireDoorData>(function);
        //    }
        //}

        string event_id = string.Empty;
        /// <summary>
        /// Does the initialize.
        /// </summary>
        public virtual void doInit()
        {
            try
            {
                ValueRead FireReport_vr = null;
                if (bcfApp.tryGetReadValueEventstring(eqpt.EqptObjectCate, eqpt.EQPT_ID, "FIRE_REPORT", out FireReport_vr))
                {
                    FireReport_vr.afterValueChange += (_sender, _e) => FireReportChange(_sender, _e);
                }
                ValueRead TestFireReport_vr = null;
                if (bcfApp.tryGetReadValueEventstring(eqpt.EqptObjectCate, eqpt.EQPT_ID, "TEST_FIRE_REPORT", out TestFireReport_vr))
                {
                    TestFireReport_vr.afterValueChange += (_sender, _e) => FireReportChange(_sender, _e);
                }
                //ValueRead FireDoorOpen_vr = null;
                //if (bcfApp.tryGetReadValueEventstring(SCAppConstants.EQPT_OBJECT_CATE_UNIT, eqpt.EQPT_ID, "FIRE_DOOR_OPEN", out FireDoorOpen_vr))
                //{
                //    FireDoorOpen_vr.afterValueChange += (_sender, _e) => FireDoorDataChange(_sender, _e);
                //}
                //ValueRead FireDoorCloseGrant_vr = null;
                //if (bcfApp.tryGetReadValueEventstring(SCAppConstants.EQPT_OBJECT_CATE_UNIT, eqpt.EQPT_ID, "FIRE_DOOR_CLOSE_GRANT", out FireDoorCloseGrant_vr))
                //{
                //    FireDoorCloseGrant_vr.afterValueChange += (_sender, _e) => FireDoorDataChange(_sender, _e);
                //}
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exection:");
            }

        }

    }
}
