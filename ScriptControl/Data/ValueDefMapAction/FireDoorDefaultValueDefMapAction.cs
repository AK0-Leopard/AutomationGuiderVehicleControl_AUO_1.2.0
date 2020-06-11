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
    public class FireDoorDefaultValueDefMapAction : IValueDefMapAction
    {
        public const string DEVICE_NAME_MTL = "EQ";
        Logger logger = NLog.LogManager.GetCurrentClassLogger();
        AUNIT unit = null;
        ASEGMENT segment = null;
        protected SCApplication scApp = null;
        protected BCFApplication bcfApp = null;
        public FireDoorDefaultValueDefMapAction()
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
            this.unit = baseEQ as AUNIT;

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
                        //initFireReport();
                        initFireCrossSignal();
                        initFireDoorData();
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

        private void initFireDoorData()
        {
            var function = scApp.getFunBaseObj<FireDoorData>(unit.UNIT_ID) as FireDoorData;
            try
            {
                //1.建立各個Function物件
                function.Read(bcfApp, unit.EqptObjectCate, unit.UNIT_ID);
                //2.read log
                function.Timestamp = DateTime.Now;
                LogManager.GetLogger("com.mirle.ibg3k0.sc.Common.LogHelper").Info(function.ToString());
                //LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(EQStatusReport), Device: DEVICE_NAME_MTL,
                //    XID: eqpt.EQPT_ID, Data: function.ToString());

                //3.logical (include db save)
                unit.fireDoorOpen = function.FIRE_DOOR_OPEN;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
            }
            finally
            {
                scApp.putFunBaseObj<FireDoorData>(function);
            }
        }

        private void initFireCrossSignal()
        {
            try
            {
                sendFireDoorCrossSignal(false);//程式開啟時先填false
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
            }
            finally
            {

            }
        }

        //private void FireReportChange(object sender, ValueChangedEventArgs e)
        //{
        //    var function = scApp.getFunBaseObj<FireReport>(unit.EQPT_ID) as FireReport;
        //    try
        //    {
        //        //1.建立各個Function物件
        //        function.Read(bcfApp, unit.EqptObjectCate, unit.EQPT_ID);
        //        //2.read log
        //        function.Timestamp = DateTime.Now;
        //        LogManager.GetLogger("com.mirle.ibg3k0.sc.Common.LogHelper").Info(function.ToString());

        //        //3.logical (include db save)
        //        unit.fireReport = function.FIRE_REPORT;

        //        if (unit.fireReport)//火災發生
        //        {
        //            //Ban該防火門所在segment
        //            sendFireDoorCrossSignal(true);
        //        }
        //        else//火災終止
        //        {
        //            //解Ban該防火門所在segment
        //            sendFireDoorCrossSignal(false);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.Error(ex, "Exception");
        //    }
        //    finally
        //    {
        //        scApp.putFunBaseObj<FireReport>(function);
        //    }
        //}


        public void sendFireDoorCrossSignal(bool signal)
        {
            var function =
                scApp.getFunBaseObj<FireDoorCross>(unit.UNIT_ID) as FireDoorCross;
            try
            {
                //1.建立各個Function物件
                function.FIRE_DOOR_CROSS = !signal;//寫入ADAM時訊號要相反，才能符合客戶規範
                function.Write(bcfApp, unit.EqptObjectCate, unit.UNIT_ID);
                //2.write log
                function.Timestamp = DateTime.Now;
                LogManager.GetLogger("com.mirle.ibg3k0.sc.Common.LogHelper").Info(function.ToString());

                //3.logical (include db save)
                unit.fireDoorCrossingSignal = signal;
                if (!unit.fireDoorCrossingSignal)//因為火災報警當下可能因為Cross訊號所以無法Ban路徑，所以這邊要再做檢查。
                {
                    AEQPT firedoorinfo = scApp.getEQObjCacheManager().getEquipmentByEQPTID(unit.EQPT_ID);
                    if (firedoorinfo.fireReport)
                    {
                        banFireDoorRoute();
                        //檢查是不是所有Cross訊號都關了
                        bool result = true;
                        foreach (AUNIT unit in firedoorinfo.UnitList)
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
                            firedoorinfo.fireDoorCancelAbortCommand(scApp);
                        }
                        else
                        {
                            //do nothing
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
                scApp.putFunBaseObj<FireDoorCross>(function);
            }
        }

        public void banFireDoorRoute()
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
        private void FireDoorDataChange(object sender, ValueChangedEventArgs e)
        {
            var function = scApp.getFunBaseObj<FireDoorData>(unit.UNIT_ID) as FireDoorData;
            try
            {
                //1.建立各個Function物件
                function.Read(bcfApp, unit.EqptObjectCate, unit.UNIT_ID);
                //2.read log
                function.Timestamp = DateTime.Now;
                LogManager.GetLogger("com.mirle.ibg3k0.sc.Common.LogHelper").Info(function.ToString());
                //LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(EQStatusReport), Device: DEVICE_NAME_MTL,
                //    XID: eqpt.EQPT_ID, Data: function.ToString());

                //3.logical (include db save)
                unit.fireDoorOpen = function.FIRE_DOOR_OPEN;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
            }
            finally
            {
                scApp.putFunBaseObj<FireDoorData>(function);
            }
        }

        string event_id = string.Empty;
        /// <summary>
        /// Does the initialize.
        /// </summary>
        public virtual void doInit()
        {
            try
            {
                //ValueRead FireReport_vr = null;
                //if (bcfApp.tryGetReadValueEventstring(unit.EqptObjectCate, unit.EQPT_ID, "FIRE_REPORT", out FireReport_vr))
                //{
                //    FireReport_vr.afterValueChange += (_sender, _e) => FireReportChange(_sender, _e);
                //}
                ValueRead FireDoorOpen_vr = null;
                if (bcfApp.tryGetReadValueEventstring(SCAppConstants.EQPT_OBJECT_CATE_UNIT, unit.UNIT_ID, "FIRE_DOOR_OPEN", out FireDoorOpen_vr))
                {
                    FireDoorOpen_vr.afterValueChange += (_sender, _e) => FireDoorDataChange(_sender, _e);
                }
                ValueRead FireDoorCloseGrant_vr = null;
                if (bcfApp.tryGetReadValueEventstring(SCAppConstants.EQPT_OBJECT_CATE_UNIT, unit.UNIT_ID, "FIRE_DOOR_CLOSE_GRANT", out FireDoorCloseGrant_vr))
                {
                    FireDoorCloseGrant_vr.afterValueChange += (_sender, _e) => FireDoorDataChange(_sender, _e);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exection:");
            }

        }

    }
}
