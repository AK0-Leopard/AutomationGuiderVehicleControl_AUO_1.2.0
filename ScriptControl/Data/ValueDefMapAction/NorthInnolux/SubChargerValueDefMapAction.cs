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


    }
}
