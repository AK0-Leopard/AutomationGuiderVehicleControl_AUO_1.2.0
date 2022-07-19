//*********************************************************************************
//      TrafficLightValueDefMapAction.cs
//*********************************************************************************
// File Name: TrafficLightValueDefMapAction.cs
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
using com.mirle.ibg3k0.sc.Data.PLC_Functions.SouthInnolux;
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
    public class TrafficLightValueDefMapAction : IValueDefMapAction
    {
        public string DEVICE_NAME_TRAFFIC_LIGHT = "EQ";
        Logger logger = NLog.LogManager.GetCurrentClassLogger();
        AEQPT eqpt = null;
        protected SCApplication scApp = null;
        protected BCFApplication bcfApp = null;
        public TrafficLightValueDefMapAction()
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
                        DEVICE_NAME_TRAFFIC_LIGHT = eqpt.EQPT_ID;
                        sendTrafficLightSignal(false, true, false, false, false, true);
                        initTrafficLight();
                        //addVirtualVehicle();
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



        private void addVirtualVehicle()
        {
            scApp.ReserveBLL.TryAddVehicleOrUpdate(eqpt.EQPT_ID, "", 99999, 99999,0, 0,
        sensorDir: Mirle.Hlts.Utils.HltDirection.NorthSouth,
          forkDir: Mirle.Hlts.Utils.HltDirection.None);
        }
        private void initTrafficLight()
        {
            var function = scApp.getFunBaseObj<TrafficLightWorkTrigger>(eqpt.EQPT_ID) as TrafficLightWorkTrigger;
            try
            {
                //1.建立各個Function物件
                function.Read(bcfApp, eqpt.EqptObjectCate, eqpt.EQPT_ID);
                //2.read log
                function.Timestamp = DateTime.Now;
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(TrafficLightValueDefMapAction), Device: DEVICE_NAME_TRAFFIC_LIGHT,
                         Data: function.ToString(),
                         VehicleID: eqpt.EQPT_ID);

                //3.logical (include db save)
                if (function.workButtonSignal)
                {
                    if (scApp.LineService.passRequest)
                    {
                        scApp.LineService.passRequestCancel = true;
                    }
                    scApp.LineService.passRequest = true;
                    //eqpt.passRequest = true;
                }
                //sendTrafficLightWorkSignal(function.workButtonSignal);
                scApp.LineService.CheckTrafficLight();
                //
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
            }
            finally
            {
                scApp.putFunBaseObj<TrafficLightWorkTrigger>(function);
            }
        }

        private void WorkButtonSignalChange(object sender, ValueChangedEventArgs e)
        {
            var function = scApp.getFunBaseObj<TrafficLightWorkTrigger>(eqpt.EQPT_ID) as TrafficLightWorkTrigger;
            try
            {
                //1.建立各個Function物件
                function.Read(bcfApp, eqpt.EqptObjectCate, eqpt.EQPT_ID);
                //2.read log
                function.Timestamp = DateTime.Now;
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(TrafficLightValueDefMapAction), Device: DEVICE_NAME_TRAFFIC_LIGHT,
                         Data: function.ToString(),
                         VehicleID: eqpt.EQPT_ID);

                //3.logical (include db save)
                if (function.workButtonSignal)
                {
                    if (scApp.LineService.passRequest)
                    {
                        scApp.LineService.passRequestCancel = true;
                    }
                    scApp.LineService.passRequest = true;
                    //eqpt.passRequest = true;
                }
                //sendTrafficLightWorkSignal(function.workButtonSignal);
                scApp.LineService.CheckTrafficLight();

            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
            }
            finally
            {
                scApp.putFunBaseObj<TrafficLightWorkTrigger>(function);
            }
        }

        public void sendTrafficLightSignal(bool work_signal,bool red_signal, bool yellow_signal, bool green_signal, bool buzzer_signal, bool force_on_signal)
        {
            var function =
                scApp.getFunBaseObj<TrafficLightSignal>(eqpt.EQPT_ID) as TrafficLightSignal;
            try
            {

                //1.建立各個Function物件
                function.workSignal = work_signal;
                function.redSignal = red_signal;
                function.yellowSignal = yellow_signal;
                function.greenSignal = green_signal;
                function.buzzerSignal = buzzer_signal;
                function.forceOnSignal = force_on_signal;
                function.Write(bcfApp, eqpt.EqptObjectCate, eqpt.EQPT_ID);
                //2.紀錄發送資料的Log
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(TrafficLightValueDefMapAction), Device: DEVICE_NAME_TRAFFIC_LIGHT,
                         Data: function.ToString(),
                         VehicleID: eqpt.EQPT_ID);
                //3.logical (include db save)

            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
            }
            finally
            {
                scApp.putFunBaseObj<TrafficLightSignal>(function);
            }
        }


        public void sendTrafficLightWorkSignal(bool work_signal)
        {
            var function =
                scApp.getFunBaseObj<TrafficLightWorkSignal>(eqpt.EQPT_ID) as TrafficLightWorkSignal;
            try
            {

                //1.建立各個Function物件
                function.workSignal = work_signal;
                function.Write(bcfApp, eqpt.EqptObjectCate, eqpt.EQPT_ID);
                //2.紀錄發送資料的Log
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(TrafficLightValueDefMapAction), Device: DEVICE_NAME_TRAFFIC_LIGHT,
                         Data: function.ToString(),
                         VehicleID: eqpt.EQPT_ID);
                //3.logical (include db save)

            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
            }
            finally
            {
                scApp.putFunBaseObj<TrafficLightWorkSignal>(function);
            }
        }

        public void sendTrafficLightYellowSignal(bool yellow_signal)
        {
            var function =
                scApp.getFunBaseObj<TrafficLightYellowSignal>(eqpt.EQPT_ID) as TrafficLightYellowSignal;
            try
            {

                //1.建立各個Function物件
                function.yellowSignal = yellow_signal;
                function.Write(bcfApp, eqpt.EqptObjectCate, eqpt.EQPT_ID);
                //2.紀錄發送資料的Log
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(TrafficLightValueDefMapAction), Device: DEVICE_NAME_TRAFFIC_LIGHT,
                         Data: function.ToString(),
                         VehicleID: eqpt.EQPT_ID);
                //3.logical (include db save)

            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
            }
            finally
            {
                scApp.putFunBaseObj<TrafficLightYellowSignal>(function);
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
                ValueRead work_button_signal_vr = null;
                if (bcfApp.tryGetReadValueEventstring(eqpt.EqptObjectCate, eqpt.EQPT_ID, "TRAFFIC_LIGHT_WORK_BUTTON_SIGNAL", out work_button_signal_vr))
                {
                    work_button_signal_vr.afterValueChange += (_sender, _e) => WorkButtonSignalChange(_sender, _e);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exection:");
            }

        }

    }
}
