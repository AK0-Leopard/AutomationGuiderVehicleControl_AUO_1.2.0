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
using com.mirle.ibg3k0.sc.Data.PLC_Functions.NorthInnolux;
using KingAOP;
using NLog;
using System;
using System.Dynamic;
using System.Linq.Expressions;
using System.Linq;
namespace com.mirle.ibg3k0.sc.Data.ValueDefMapAction.NorthInnolux
{
    public class SubChargerValueDefMapAction : com.mirle.ibg3k0.sc.Data.ValueDefMapAction.SubChargerValueDefMapAction
    {
        public override void ChargerStatusReport(object sender, ValueChangedEventArgs args)
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
                unit.coupler1Status_SOUTH_INNOLUX = function.Coupler1Status;
                unit.coupler2Status_SOUTH_INNOLUX = function.Coupler2Status;
                unit.coupler3Status_SOUTH_INNOLUX = function.Coupler3Status;
                unit.coupler1HPSafety = function.Coupler1Position;
                unit.coupler2HPSafety = function.Coupler2Position;
                unit.coupler3HPSafety = function.Coupler3Position;

                unit.ChargerStatusIndex = function.Index;
                //if (function.Coupler1Position == SCAppConstants.CouplerHPSafety.NonSafety && !unit.coupler1SegmentDisableByAGVC)
                //{
                //    CouplerAddress coupler_address = scApp.AddressesBLL.cache.GetCouplerAddress(unit.UNIT_ID, CouplerNum.NumberOne);
                //    unit.coupler1SegmentDisableByAGVC = couplerSafetyHandler(SCAppConstants.CouplerHPSafety.NonSafety, coupler_address);
                //}
                //else if (function.Coupler1Position == SCAppConstants.CouplerHPSafety.Safety && unit.coupler1SegmentDisableByAGVC)
                //{
                //    CouplerAddress coupler_address = scApp.AddressesBLL.cache.GetCouplerAddress(unit.UNIT_ID, CouplerNum.NumberOne);
                //    unit.coupler1SegmentDisableByAGVC = couplerSafetyHandler(SCAppConstants.CouplerHPSafety.Safety, coupler_address);
                //}

                //if (function.Coupler2Position == SCAppConstants.CouplerHPSafety.NonSafety && !unit.coupler2SegmentDisableByAGVC)
                //{
                //    CouplerAddress coupler_address = scApp.AddressesBLL.cache.GetCouplerAddress(unit.UNIT_ID, CouplerNum.NumberTwo);
                //    unit.coupler2SegmentDisableByAGVC = couplerSafetyHandler(SCAppConstants.CouplerHPSafety.NonSafety, coupler_address);
                //}
                //else if (function.Coupler2Position == SCAppConstants.CouplerHPSafety.Safety && unit.coupler2SegmentDisableByAGVC)
                //{
                //    CouplerAddress coupler_address = scApp.AddressesBLL.cache.GetCouplerAddress(unit.UNIT_ID, CouplerNum.NumberTwo);
                //    unit.coupler2SegmentDisableByAGVC = couplerSafetyHandler(SCAppConstants.CouplerHPSafety.Safety, coupler_address);
                //}

                //if (function.Coupler3Position == SCAppConstants.CouplerHPSafety.NonSafety && !unit.coupler3SegmentDisableByAGVC)
                //{
                //    CouplerAddress coupler_address = scApp.AddressesBLL.cache.GetCouplerAddress(unit.UNIT_ID, CouplerNum.NumberThree);
                //    unit.coupler3SegmentDisableByAGVC = couplerSafetyHandler(SCAppConstants.CouplerHPSafety.NonSafety, coupler_address);
                //}
                //else if (function.Coupler3Position == SCAppConstants.CouplerHPSafety.Safety && unit.coupler3SegmentDisableByAGVC)
                //{
                //    CouplerAddress coupler_address = scApp.AddressesBLL.cache.GetCouplerAddress(unit.UNIT_ID, CouplerNum.NumberThree);
                //    unit.coupler3SegmentDisableByAGVC = couplerSafetyHandler(SCAppConstants.CouplerHPSafety.Safety, coupler_address);
                //}

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

        public bool couplerSafetyHandler(SCAppConstants.CouplerHPSafety safety, CouplerAddress coupler_address,CouplerNum couplerNum)
        {
            bool result = false;
            if (coupler_address != null)
            {
                if (safety == SCAppConstants.CouplerHPSafety.Safety)
                {
                    if(couplerNum == CouplerNum.NumberOne)
                    {
                        unit.coupler1HPNotSafeCount = 0;
                        if (!unit.coupler1SegmentDisableByAGVC)
                        {
                            return false;
                        }
                    }
                    else if (couplerNum == CouplerNum.NumberTwo)
                    {
                        unit.coupler2HPNotSafeCount = 0;
                        if (!unit.coupler2SegmentDisableByAGVC)
                        {
                            return false;
                        }
                    }
                    else if (couplerNum == CouplerNum.NumberThree)
                    {
                        unit.coupler3HPNotSafeCount = 0;
                        if (!unit.coupler3SegmentDisableByAGVC)
                        {
                            return false;
                        }
                    }

                    scApp.ReserveBLL.RemoveVehicle(unit.UNIT_ID + coupler_address.CouplerNum + "_v_car");
                    foreach (string seg_id in coupler_address.TrafficControlSegment)
                    {
                        ASEGMENT selectSeg = scApp.SegmentBLL.cache.GetSegment(seg_id);
                        if (selectSeg.STATUS == E_SEG_STATUS.Closed)
                        {
                            var ban_result = scApp.VehicleService.doEnableDisableSegment(selectSeg.SEG_ID.Trim(), E_PORT_STATUS.InService);
                            if (ban_result.isSuccess)
                            {
                                selectSeg.DISABLE_TIME = ban_result.segment.DISABLE_TIME;
                                selectSeg.STATUS = ban_result.segment.STATUS;
                            }
                        }
                    }
                }
                else if (coupler_address.hasVh(scApp.VehicleBLL))
                {
                    if (couplerNum == CouplerNum.NumberOne)
                    {
                        unit.coupler1HPNotSafeCount = 0;
                    }
                    else if (couplerNum == CouplerNum.NumberTwo)
                    {
                        unit.coupler2HPNotSafeCount = 0;
                    }
                    else if (couplerNum == CouplerNum.NumberThree)
                    {
                        unit.coupler3HPNotSafeCount = 0;
                    }
                }
                else
                {
                    if (couplerNum == CouplerNum.NumberOne)
                    {
                        unit.coupler1HPNotSafeCount ++;
                        if(unit.coupler1HPNotSafeCount< AUNIT.couplerHPNotSafeTimes)
                        {
                            return false;
                        }
                    }
                    else if (couplerNum == CouplerNum.NumberTwo)
                    {
                        unit.coupler2HPNotSafeCount ++;
                        if (unit.coupler2HPNotSafeCount < AUNIT.couplerHPNotSafeTimes)
                        {
                            return false;
                        }
                    }
                    else if (couplerNum == CouplerNum.NumberThree)
                    {
                        unit.coupler3HPNotSafeCount ++;
                        if (unit.coupler3HPNotSafeCount < AUNIT.couplerHPNotSafeTimes)
                        {
                            return false;
                        }
                    }

                    string msg = $"The coupler position Address{coupler_address.ADR_ID} is not safe ,begin coupler safe handle process.";
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(SubChargerValueDefMapAction), Device: Service.VehicleService.DEVICE_NAME_AGV,
                       Data: msg);
                    bcf.App.BCFApplication.onErrorMsg(msg);
                    foreach (string seg_id in coupler_address.TrafficControlSegment)
                    {
                        ASEGMENT selectSeg = scApp.SegmentBLL.cache.GetSegment(seg_id);
                        if (selectSeg.STATUS == E_SEG_STATUS.Active && !selectSeg.PRE_DISABLE_FLAG)
                        {
                            var ban_result = scApp.VehicleService.doEnableDisableSegment(selectSeg.SEG_ID.Trim(), E_PORT_STATUS.OutOfService);
                            if (ban_result.isSuccess)
                            {
                                selectSeg.DISABLE_TIME = ban_result.segment.DISABLE_TIME;
                                selectSeg.STATUS = ban_result.segment.STATUS;
                            }
                        }
                    }
                    var update_result = scApp.ReserveBLL.TryAddVehicleOrUpdate(unit.UNIT_ID + coupler_address.CouplerNum + "_v_car", coupler_address.ADR_ID, 90, Mirle.Hlts.Utils.HltDirection.NS);

                    if (!update_result.OK)
                    {
                        string message = $"The coupler position Address{coupler_address.ADR_ID} is not safe, vh:{update_result.VehicleID} need to stop";
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(SubChargerValueDefMapAction), Device: Service.VehicleService.DEVICE_NAME_AGV,
                           Data: message,
                           Details: update_result.Description,
                           VehicleID: update_result.VehicleID);

                        AVEHICLE will_bumped_vh = scApp.VehicleBLL.cache.getVehicle(update_result.VehicleID);

                        //如果發生碰撞或踩入別人預約的不是虛擬車的話，則就要對兩台車下達EMS
                        if (!update_result.VehicleID.StartsWith(Service.VehicleService.VehicleVirtualSymbol))
                        {
                            //string cmd_id = will_bumped_vh.OHTC_CMD;
                            //if (!string.IsNullOrWhiteSpace(cmd_id))
                            //{
                            //    ACMD_OHTC cmd = scApp.CMDBLL.GetCMD_OHTCByID(cmd_id);
                            //    string cmdSourceAdr = cmd.SOURCE;
                            //    if (!string.IsNullOrWhiteSpace(cmdSourceAdr))
                            //    {
                            //        if(SCUtility.isMatche( cmd_id, coupler_address.))
                            //    }
                            //}

                            
                            bcf.App.BCFApplication.onErrorMsg(message);
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(SubChargerValueDefMapAction), Device: Service.VehicleService.DEVICE_NAME_AGV,
                               Data: $"The vehicles bumped coupler will happend. send pause to {update_result.VehicleID}",
                               VehicleID: update_result.VehicleID);
                            scApp.VehicleService.doAbortCommand(will_bumped_vh, will_bumped_vh.OHTC_CMD, ProtocolFormat.OHTMessage.CMDCancelType.CmdEms);
                            //doAbortCommand(assign_vh, ohtc_cmd_id, actType); ;
                            //scApp.VehicleService.PauseRequest(will_bumped_vh.VEHICLE_ID, ProtocolFormat.OHTMessage.PauseEvent.Pause, SCAppConstants.OHxCPauseType.Safty);
                        }
                    }

                    result = true;
                }
            }

            return result;
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
