//*********************************************************************************
//      ColorLightValueDefMapAction.cs
//*********************************************************************************
// File Name: FourColorLightValueDefMapAction.cs
// Description: 
//
//(c) Copyright 2018, MIRLE Automation Corporation
//
// Date          Author         Request No.    Tag     Description
// ------------- -------------  -------------  ------  -----------------------------
//**********************************************************************************
using com.mirle.ibg3k0.bcf.App;
using com.mirle.ibg3k0.bcf.Data.ValueDefMapAction;
using com.mirle.ibg3k0.bcf.Data.VO;
using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.Data.PLC_Functions;
using com.mirle.ibg3k0.sc.Data.PLC_Functions.SouthInnolux;
using com.mirle.ibg3k0.sc.Data.VO;
using KingAOP;
using NLog;
using System;
using System.Dynamic;
using System.Linq.Expressions;

namespace com.mirle.ibg3k0.sc.Data.ValueDefMapAction
{
    public class ColorLightValueDefMapAction : IValueDefMapAction
    {
        public const string DEVICE_NAME_COLORLIGHT = "COLOR_LIGHT";
        Logger logger = LogManager.GetCurrentClassLogger();
        AEQPT eqpt = null;
        protected SCApplication scApp = null;
        protected BCFApplication bcfApp = null;
        public ColorLightValueDefMapAction()
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
            try
            {
                this.eqpt = baseEQ as AEQPT;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exection:");
            }
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
                        initialValueWrite();
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

        //private void initialFilckTimerAction()
        //{
        //    RedLightFilckTimerAction = new FilckTimerAction(eqpt.EqptObjectCate, eqpt.EQPT_ID, "RED_LIGHT", this);
        //}

        private void initialValueWrite()
        {
            sendColorLightSignal(false, false, false, false, false, true);
        }

        public void sendColorLightSignal(bool red_signal, bool yellow_signal,bool green_signal,bool blue_signal, bool buzzer_signal, bool force_on_signal)
        {
            var function =
                scApp.getFunBaseObj<ColorLightSignal>(eqpt.EQPT_ID) as ColorLightSignal;
            try
            {

                //1.建立各個Function物件
                function.redSignal = red_signal;
                function.yellowSignal = yellow_signal;
                function.greenSignal = green_signal;
                function.blueSignal = blue_signal;
                function.buzzerSignal = buzzer_signal;
                function.forceOnSignal = force_on_signal;
                function.Write(bcfApp, eqpt.EqptObjectCate, eqpt.EQPT_ID);
                //2.紀錄發送資料的Log
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(ColorLightValueDefMapAction), Device: DEVICE_NAME_COLORLIGHT,
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
                scApp.putFunBaseObj<ColorLightSignal>(function);
            }
        }

        public void sendColorLightRedWithBuzzerSignal(bool buzzer_signal, bool light_signal)
        {
            var function =
                scApp.getFunBaseObj<ColorLightRedWithBuzzer>(eqpt.EQPT_ID) as ColorLightRedWithBuzzer;
            try
            {

                //1.建立各個Function物件
                function.buzzerSignal = buzzer_signal;
                function.redSignal = light_signal;
                function.Write(bcfApp, eqpt.EqptObjectCate, eqpt.EQPT_ID);
                //2.紀錄發送資料的Log
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(ColorLightValueDefMapAction), Device: DEVICE_NAME_COLORLIGHT,
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
                scApp.putFunBaseObj<ColorLightRedWithBuzzer>(function);
            }
        }

        public void sendColorLightYellowSignal(bool signal)
        {
            var function =
                scApp.getFunBaseObj<ColorLightYellow>(eqpt.EQPT_ID) as ColorLightYellow;
            try
            {
                //1.建立各個Function物件
                function.yellowSignal = signal;
                function.Write(bcfApp, eqpt.EqptObjectCate, eqpt.EQPT_ID);
                //2.紀錄發送資料的Log
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(ColorLightValueDefMapAction), Device: DEVICE_NAME_COLORLIGHT,
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
                scApp.putFunBaseObj<ColorLightYellow>(function);
            }
        }
        public void sendColorLightGreenSignal(bool signal)
        {
            var function =
                scApp.getFunBaseObj<ColorLightSignalGreen>(eqpt.EQPT_ID) as ColorLightSignalGreen;
            try
            {
                //1.建立各個Function物件
                function.greenSignal = signal;
                function.Write(bcfApp, eqpt.EqptObjectCate, eqpt.EQPT_ID);
                //2.紀錄發送資料的Log
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(ColorLightValueDefMapAction), Device: DEVICE_NAME_COLORLIGHT,
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
                scApp.putFunBaseObj<ColorLightSignalGreen>(function);
            }
        }

        public void sendColorLightBlueSignal(bool signal)
        {
            var function =
                scApp.getFunBaseObj<ColorLightSignalBlue>(eqpt.EQPT_ID) as ColorLightSignalBlue;
            try
            {
                //1.建立各個Function物件
                function.blueSignal = signal;
                function.Write(bcfApp, eqpt.EqptObjectCate, eqpt.EQPT_ID);
                //2.紀錄發送資料的Log
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(ColorLightValueDefMapAction), Device: DEVICE_NAME_COLORLIGHT,
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
                scApp.putFunBaseObj<ColorLightSignalBlue>(function);
            }
        }


        /// <summary>
        /// Does the initialize.
        /// </summary>
        public virtual void doInit()
        {
            try
            {
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exection:");
            }

        }

    }
}
