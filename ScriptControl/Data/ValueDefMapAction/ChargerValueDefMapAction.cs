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
using KingAOP;
using NLog;
using System;
using System.Dynamic;
using System.Linq.Expressions;

namespace com.mirle.ibg3k0.sc.Data.ValueDefMapAction
{
    public class ChargerValueDefMapAction : IValueDefMapAction
    {
        public const string DEVICE_NAME_MC = "Master Charger";
        Logger logger = NLog.LogManager.GetCurrentClassLogger();
        AEQPT eqpt = null;
        protected SCApplication scApp = null;
        protected BCFApplication bcfApp = null;

        public ChargerValueDefMapAction()
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
                        initRead();
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


        public virtual void initRead()
        {
            var recevie_function =
                scApp.getFunBaseObj<MCToAGVCAbnormalReport>(eqpt.EQPT_ID) as MCToAGVCAbnormalReport;
            try
            {
                recevie_function.Read(bcfApp, eqpt.EqptObjectCate, eqpt.EQPT_ID);
                eqpt.abnormalReportCode01 = recevie_function.ErrorCode_1;
                eqpt.abnormalReportCode02 = recevie_function.ErrorCode_2;
                eqpt.abnormalReportCode03 = recevie_function.ErrorCode_3;
                eqpt.abnormalReportCode04 = recevie_function.ErrorCode_4;
                eqpt.abnormalReportCode05 = recevie_function.ErrorCode_5;
                eqpt.AbnormalReportIndex = recevie_function.index;

            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
            }
            finally
            {
                scApp.putFunBaseObj<MCToAGVCAbnormalReport>(recevie_function);
            }
        }

        object dateTimeSyneObj = new object();
        uint dateTimeIndex = 0;
        public virtual void DateTimeSyncCommand(DateTime dateTime)
        {

            AGVCToCharger_DateTimeSync send_function =
               scApp.getFunBaseObj<AGVCToCharger_DateTimeSync>(eqpt.EQPT_ID) as AGVCToCharger_DateTimeSync;
            try
            {
                lock (dateTimeSyneObj)
                {
                    //1.準備要發送的資料
                    send_function.Year = Convert.ToUInt16(dateTime.Year.ToString(), 16);
                    send_function.Month = Convert.ToUInt16(dateTime.Month.ToString(), 16);
                    send_function.Day = Convert.ToUInt16(dateTime.Day.ToString(), 16);
                    send_function.Hour = Convert.ToUInt16(dateTime.Hour.ToString(), 16);
                    send_function.Min = Convert.ToUInt16(dateTime.Minute.ToString(), 16);
                    send_function.Sec = Convert.ToUInt16(dateTime.Second.ToString(), 16);
                    if (dateTimeIndex >= 9999)
                        dateTimeIndex = 0;
                    send_function.Index = ++dateTimeIndex;
                    //2.紀錄發送資料的Log
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(ChargerValueDefMapAction), Device: DEVICE_NAME_MC,
                             Data: send_function.ToString(),
                             VehicleID: eqpt.EQPT_ID);
                    //3.發送訊息
                    send_function.Write(bcfApp, eqpt.EqptObjectCate, eqpt.EQPT_ID);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
            }
            finally
            {
                scApp.putFunBaseObj<AGVCToCharger_DateTimeSync>(send_function);
            }
        }

        uint message_index = 0;
        public virtual void AGVCToChargerAllCouplerEnable(bool isEnable)
        {
            AGVCToChargerALLCouplerEnableDiable send_function =
                scApp.getFunBaseObj<AGVCToChargerALLCouplerEnableDiable>(eqpt.EQPT_ID) as AGVCToChargerALLCouplerEnableDiable;
            try
            {
                //1.建立各個Function物件
                send_function.Enable = (uint)(isEnable ? 1 : 0);
                if (message_index > 9999)
                { message_index = 0; }
                send_function.Index = ++message_index;
                //2.紀錄發送資料的Log
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(ChargerValueDefMapAction), Device: DEVICE_NAME_MC,
                         Data: send_function.ToString(),
                         VehicleID: eqpt.EQPT_ID);
                //3.發送訊息
                send_function.Write(bcfApp, eqpt.EqptObjectCate, eqpt.EQPT_ID);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
            }
            finally
            {
                scApp.putFunBaseObj<AGVCToChargerALLCouplerEnableDiable>(send_function);
            }
        }

        UInt16 car_realtime_info = 0;
        public virtual void AGVCToMChargerAllChargerChargingFinish()
        {
            AGVCToMChargerAllChargerChargingFinish send_function =
                scApp.getFunBaseObj<AGVCToMChargerAllChargerChargingFinish>(eqpt.EQPT_ID) as AGVCToMChargerAllChargerChargingFinish;
            try
            {
                //1.建立各個Function物件
                if (car_realtime_info > 9999)
                { car_realtime_info = 0; }
                send_function.Index = ++car_realtime_info;
                //2.紀錄發送資料的Log
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(ChargerValueDefMapAction), Device: DEVICE_NAME_MC,
                         Data: send_function.ToString(),
                         VehicleID: eqpt.EQPT_ID);
                //3.發送訊息
                send_function.Write(bcfApp, eqpt.EqptObjectCate, eqpt.EQPT_ID);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
            }
            finally
            {
                scApp.putFunBaseObj<AGVCToMChargerAllChargerChargingFinish>(send_function);
            }
        }

        UInt16 alarm_reset_index = 0;
        public virtual void AGVCToMChargerAlarmReset()
        {
            AGVCToMChargerAlarmReset send_function =
                scApp.getFunBaseObj<AGVCToMChargerAlarmReset>(eqpt.EQPT_ID) as AGVCToMChargerAlarmReset;
            try
            {
                //1.建立各個Function物件
                if (alarm_reset_index > 9999)
                { alarm_reset_index = 0; }
                send_function.Index = ++alarm_reset_index;
                //2.紀錄發送資料的Log
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(ChargerValueDefMapAction), Device: DEVICE_NAME_MC,
                         Data: send_function.ToString(),
                         VehicleID: eqpt.EQPT_ID);
                //3.發送訊息
                send_function.Write(bcfApp, eqpt.EqptObjectCate, eqpt.EQPT_ID);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
            }
            finally
            {
                scApp.putFunBaseObj<AGVCToMChargerAlarmReset>(send_function);
            }
        }







        public virtual void MasterChargerAbnormalChargingReport(object sender, ValueChangedEventArgs args)
        {
            var recevie_function =
                scApp.getFunBaseObj<MCToAGVCAbnormalReport>(eqpt.EQPT_ID) as MCToAGVCAbnormalReport;
            try
            {
                recevie_function.Read(bcfApp, eqpt.EqptObjectCate, eqpt.EQPT_ID);
                eqpt.abnormalReportCode01 = recevie_function.ErrorCode_1;
                eqpt.abnormalReportCode02 = recevie_function.ErrorCode_2;
                eqpt.abnormalReportCode03 = recevie_function.ErrorCode_3;
                eqpt.abnormalReportCode04 = recevie_function.ErrorCode_4;
                eqpt.abnormalReportCode05 = recevie_function.ErrorCode_5;
                eqpt.AbnormalReportIndex = recevie_function.index;

                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(ChargerValueDefMapAction), Device: DEVICE_NAME_MC,
                         Data: recevie_function.ToString(),
                         VehicleID: eqpt.EQPT_ID);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
            }
            finally
            {
                scApp.putFunBaseObj<MCToAGVCAbnormalReport>(recevie_function);
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
                ValueRead vr = null;
                if (bcfApp.tryGetReadValueEventstring(eqpt.EqptObjectCate, eqpt.EQPT_ID, "MCHARGER_TO_AGVC_ABNORMAL_CHARGING_REPORT_INDEX", out vr))
                {
                    vr.afterValueChange += (_sender, _e) => MasterChargerAbnormalChargingReport(_sender, _e);
                }

            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exection:");
            }

        }


    }
}
