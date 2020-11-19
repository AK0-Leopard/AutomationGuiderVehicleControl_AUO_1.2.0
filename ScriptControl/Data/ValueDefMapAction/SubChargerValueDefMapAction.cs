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
using System.Linq;
namespace com.mirle.ibg3k0.sc.Data.ValueDefMapAction
{
    public class SubChargerValueDefMapAction : IValueDefMapAction
    {
        public const string DEVICE_NAME_CHARGER = "CHARGER";
        protected Logger logger = NLog.LogManager.GetCurrentClassLogger();
        protected AUNIT unit = null;
        ALINE line;
        protected SCApplication scApp = null;
        protected BCFApplication bcfApp = null;

        public SubChargerValueDefMapAction()
            : base()
        {
            scApp = SCApplication.getInstance();
            bcfApp = scApp.getBCFApplication();
            line = scApp.getEQObjCacheManager().getLine();
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
            ChargerAliveReport(null, null);
            ChargerCurrentStatus(null, null);
            ChargerStatusReport(null, null);
            ConstantReport(null, null);
            ChangingInfoReport(null, null);
            ChargerPIOHandshake(null, null);
        }


        uint ForceStopCharging_index = 0;
        public virtual void AGVCToChargerForceStopCharging()
        {
            AGVCToChargerForceStopCharging send_function =
                scApp.getFunBaseObj<AGVCToChargerForceStopCharging>(unit.UNIT_ID) as AGVCToChargerForceStopCharging;
            try
            {
                //1.建立各個Function物件
                if (ForceStopCharging_index > 9999)
                { ForceStopCharging_index = 0; }
                send_function.Index = ++ForceStopCharging_index;
                //2.紀錄發送資料的Log
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(SubChargerValueDefMapAction), Device: DEVICE_NAME_CHARGER,
                         Data: send_function.ToString(),
                         VehicleID: unit.UNIT_ID);
                //3.發送訊息
                send_function.Write(bcfApp, unit.EqptObjectCate, unit.UNIT_ID);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
            }
            finally
            {
                scApp.putFunBaseObj<AGVCToChargerForceStopCharging>(send_function);
            }
        }

        uint message_index = 0;
        public virtual void AGVCToChargerCouplerEnable(uint couplerID, bool isEnable)
        {
            AGVCToChargerCouplerEnableDisable send_function =
                scApp.getFunBaseObj<AGVCToChargerCouplerEnableDisable>(unit.UNIT_ID) as AGVCToChargerCouplerEnableDisable;
            try
            {
                //1.建立各個Function物件
                send_function.CouplerID = couplerID;
                send_function.Enable = (uint)(isEnable ? 1 : 0);
                if (message_index > 9999)
                { message_index = 0; }
                send_function.Index = ++message_index;
                //2.紀錄發送資料的Log
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(SubChargerValueDefMapAction), Device: DEVICE_NAME_CHARGER,
                         Data: send_function.ToString(),
                         VehicleID: unit.UNIT_ID);
                //3.發送訊息
                send_function.Write(bcfApp, unit.EqptObjectCate, unit.UNIT_ID);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
            }
            finally
            {
                scApp.putFunBaseObj<AGVCToChargerCouplerEnableDisable>(send_function);
            }
        }

        public virtual void ChargerAliveReport(object sender, ValueChangedEventArgs args)
        {
            var function =
                scApp.getFunBaseObj<ChargeToAGVCAliveReport>(unit.UNIT_ID) as ChargeToAGVCAliveReport;
            try
            {
                //1.建立各個Function物件
                function.Read(bcfApp, unit.EqptObjectCate, unit.UNIT_ID);
                //2.read log
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(SubChargerValueDefMapAction), Device: DEVICE_NAME_CHARGER,
                    XID: unit.UNIT_ID, Data: function.ToString());
                unit.ChargerAlive = function.Alive;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
            }
            finally
            {
                scApp.putFunBaseObj<ChargeToAGVCAliveReport>(function);
            }
        }
        public virtual void ChargerStatusReport(object sender, ValueChangedEventArgs args)
        {
            var function =
                scApp.getFunBaseObj<ChargeToAGVCStatusReport>(unit.UNIT_ID) as ChargeToAGVCStatusReport;
            try
            {
                //1.建立各個Function物件
                function.Read(bcfApp, unit.EqptObjectCate, unit.UNIT_ID);
                //2.read log
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(SubChargerValueDefMapAction), Device: DEVICE_NAME_CHARGER,
                    XID: unit.UNIT_ID, Data: function.ToString());

                unit.chargerReserve = function.Reserve;
                unit.chargerConstantVoltageOutput = function.ConstantVoltageOutput;
                unit.chargerConstantCurrentOutput = function.ConstantCurrentOutput;
                unit.chargerHighInputVoltageProtection = function.HighInputVoltageProtection;
                unit.chargerLowInputVoltageProtection = function.LowInputVoltageProtection;
                unit.chargerHighOutputVoltageProtection = function.HighOutputVoltageProtection;
                unit.chargerHighOutputCurrentProtection = function.HighOutputCurrentProtection;
                unit.chargerOverheatProtection = function.OverheatProtection;
                unit.chargerRS485Status = function.RS485Status.ToString();
                unit.coupler1Status = function.Coupler1Status;
                unit.coupler2Status = function.Coupler2Status;
                unit.coupler3Status = function.Coupler3Status;
                unit.coupler1HPSafety = function.Coupler1Position;
                unit.coupler2HPSafety = function.Coupler2Position;
                unit.coupler3HPSafety = function.Coupler3Position;

                unit.ChargerStatusIndex = function.Index;

                //3.logical (include db save)
                //eqpt.Inline_Mode = function.InlineMode;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
            }
            finally
            {
                scApp.putFunBaseObj<ChargeToAGVCStatusReport>(function);
            }
        }

        //public virtual void ChargerCurrentStatus()
        //{
        //    var function =
        //        scApp.getFunBaseObj<ChargeToAGVCCurrentStatus>(unit.UNIT_ID) as ChargeToAGVCCurrentStatus;
        //    try
        //    {
        //        //1.建立各個Function物件
        //        function.Read(bcfApp, unit.EqptObjectCate, unit.UNIT_ID);
        //        //2.read log
        //        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(SubChargerValueDefMapAction), Device: DEVICE_NAME_CHARGER,
        //         Data: function.ToString(),
        //        VehicleID: unit.UNIT_ID);
        //        //3.logical (include db save)
        //        unit.inputVoltage = function.InputVoltage;
        //        unit.chargeVoltage = function.ChargingVoltage;
        //        unit.chargeCurrent = function.ChargingCurrent;
        //        unit.chargePower = function.ChargingPower;
        //        unit.couplerChargeVoltage = function.CouplerChargingVoltage;
        //        unit.couplerChargeCurrent = function.CouplerChargingCurrent;
        //        unit.couplerID = function.CouplerID;
        //        unit.CurrentSupplyStatusBlock = function.BlockValue;
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.Error(ex, "Exception");
        //    }
        //    finally
        //    {
        //        scApp.putFunBaseObj<ChargeToAGVCCurrentStatus>(function);
        //    }
        //}
        public virtual void ChargerCurrentStatus(object sender, ValueChangedEventArgs args)
        {
            var function =
                scApp.getFunBaseObj<ChargeToAGVCCurrentStatus>(unit.UNIT_ID) as ChargeToAGVCCurrentStatus;
            try
            {
                //1.建立各個Function物件
                function.Read(bcfApp, unit.EqptObjectCate, unit.UNIT_ID);
                //2.read log
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(SubChargerValueDefMapAction), Device: DEVICE_NAME_CHARGER,
                 Data: function.ToString(),
                VehicleID: unit.UNIT_ID);
                //3.logical (include db save)
                unit.inputVoltage = function.InputVoltage;
                unit.chargeVoltage = function.ChargingVoltage;
                unit.chargeCurrent = function.ChargingCurrent;
                unit.chargePower = function.ChargingPower;
                unit.couplerChargeVoltage = function.CouplerChargingVoltage;
                unit.couplerChargeCurrent = function.CouplerChargingCurrent;
                unit.couplerID = function.CouplerID;
                unit.CurrentSupplyStatusBlock = function.BlockValue;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
            }
            finally
            {
                scApp.putFunBaseObj<ChargeToAGVCCurrentStatus>(function);
            }
        }

        public virtual void ConstantReport(object sender, ValueChangedEventArgs args)
        {
            var function =
                scApp.getFunBaseObj<ChargeToAGVCConstantReport>(unit.UNIT_ID) as ChargeToAGVCConstantReport;
            try
            {
                //1.建立各個Function物件
                function.Read(bcfApp, unit.EqptObjectCate, unit.UNIT_ID);
                //2.read log
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(SubChargerValueDefMapAction), Device: DEVICE_NAME_CHARGER,
                 Data: function.ToString(),
                VehicleID: unit.UNIT_ID);

                unit.chargerOutputVoltage = function.OutputVoltage;
                unit.chargerOutputCurrent = function.OutputCurrent;
                unit.chargerOverVoltage = function.OverVoltage;
                unit.chargerOverCurrent = function.OverCurrent;
                unit.ChargerCurrentParameterIndex = function.Index;

                //3.logical (include db save)
                //eqpt.Inline_Mode = function.InlineMode;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
            }
            finally
            {
                scApp.putFunBaseObj<ChargeToAGVCConstantReport>(function);
            }
        }

        public virtual void ChangingInfoReport(object sender, ValueChangedEventArgs args)
        {
            var function =
                scApp.getFunBaseObj<CouplerChargingReport>(unit.UNIT_ID) as CouplerChargingReport;
            try
            {
                //1.建立各個Function物件
                function.Read(bcfApp, unit.EqptObjectCate, unit.UNIT_ID);
                function.LineID = line.LINE_ID;
                function.ChargerID = unit.UNIT_ID;
                function.Timestamp = DateTime.Now;

                //2.read log
                //LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(SubChargerValueDefMapAction), Device: DEVICE_NAME_CHARGER,
                // Data: function.ToString(),
                LogManager.GetLogger("ChargingReport").Info(function.ToString());

                unit.chargerCouplerID = function.CouplerID;
                unit.chargerChargingStartTime = function.ChargingStartTime;
                unit.chargerChargingEndTime = function.ChargingEndTime;
                unit.chargerInputAH = function.InputAH;
                unit.chargerChargingResult = function.ChargingResult.ToString();
                unit.CouplerChargeInfoIndex = function.Index;


                //DateTime startTime = function.ChargingStartTime;
                //DateTime endTime = function.ChargingEndTime;
                //3.logical (include db save)
                //eqpt.Inline_Mode = function.InlineMode;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
            }
            finally
            {
                scApp.putFunBaseObj<CouplerChargingReport>(function);
            }
        }

        object plc_cst_interface_lock_obj = new object();
        DateTime LastHandshakeTime = DateTime.Now;
        public virtual void ChargerPIOHandshake(object sender, ValueChangedEventArgs args)
        {
            lock (plc_cst_interface_lock_obj)
            {
                var function =
                    scApp.getFunBaseObj<ChargerInterface>(unit.UNIT_ID) as ChargerInterface;
                try
                {
                    //1.建立各個Function物件
                    function.Read(bcfApp, unit.EqptObjectCate, unit.UNIT_ID, 15);
                    function.Details = function.RawDetails.
                                       Where(detail => detail.Timestamp > LastHandshakeTime).
                                       OrderBy(detail => detail.Timestamp).
                                       ToList();
                    if (function.Details.Count() > 0)
                        LastHandshakeTime = function.Details.Last().Timestamp;
                    //2.read log
                    foreach (var detail in function.Details)
                    {
                        LogManager.GetLogger("RecodeVehicleCSTInterface").Info(detail.ToString());
                    }
                    unit.PIOInfos.Clear();
                    foreach (ChargerInterface.ChargerInterfaceDetail item in function.RawDetails)
                    {
                        AUNIT.PIOInfo pIOInfo = new AUNIT.PIOInfo()
                        {
                            CouplerID = item.CouplerID,
                            Timestamp = item.Timestamp,
                            signal1 = item.SChargerSigna1,
                            signal2 = item.SChargerSigna2,
                        };
                        unit.PIOInfos.Add(pIOInfo);
                    }
                    unit.PIOIndex = function.Index;


                    //3.logical (include db save)
                    //eqpt.Inline_Mode = function.InlineMode;
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Exception");
                }
                finally
                {
                    scApp.putFunBaseObj<ChargerInterface>(function);
                }
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

                if (bcfApp.tryGetReadValueEventstring(unit.EqptObjectCate, unit.UNIT_ID, "CHARGERX_TO_AGVC_ALIVE_INDEX", out vr))
                {
                    vr.afterValueChange += (_sender, _e) => ChargerAliveReport(_sender, _e);
                }
                if (bcfApp.tryGetReadValueEventstring(unit.EqptObjectCate, unit.UNIT_ID, "CHARGERX_TO_AGVC_CHARGER_CURRENT_CHARGING_STATUS_REPORT_BLOCK", out vr))
                {
                    vr.afterValueChange += (_sender, _e) => ChargerCurrentStatus(_sender, _e);
                }
                if (bcfApp.tryGetReadValueEventstring(unit.EqptObjectCate, unit.UNIT_ID, "CHARGERX_TO_AGVC_CHARGER_STATUS_REPORT_INDEX", out vr))
                {
                    vr.afterValueChange += (_sender, _e) => ChargerStatusReport(_sender, _e);
                }
                if (bcfApp.tryGetReadValueEventstring(unit.EqptObjectCate, unit.UNIT_ID, "CHARGERX_TO_AGVC_CHARGER_CURRENT_CONSTANT_REPORT_INDEX", out vr))
                {
                    vr.afterValueChange += (_sender, _e) => ConstantReport(_sender, _e);
                }
                if (bcfApp.tryGetReadValueEventstring(unit.EqptObjectCate, unit.UNIT_ID, "CHARGERX_TO_AGVC_COUPLER_CHANGING_INFO_REPORT_INDEX", out vr))
                {
                    vr.afterValueChange += (_sender, _e) => ChangingInfoReport(_sender, _e);
                }
                if (bcfApp.tryGetReadValueEventstring(unit.EqptObjectCate, unit.UNIT_ID, "CHARGERX_TO_AGVC_PIO_HANDSHAKE_REPORT_INDEX", out vr))
                {
                    vr.afterValueChange += (_sender, _e) => ChargerPIOHandshake(_sender, _e);
                }

            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exection:");
            }

        }


    }
}
