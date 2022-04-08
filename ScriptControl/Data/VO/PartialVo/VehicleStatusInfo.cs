using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.mirle.ibg3k0.sc.Data.VO.PartialVo
{
    public class VehicleStatusInfo
    {
        enum ControlStatus
        {
            None,
            Local,
            Remote
        }
        enum VehicleStatus
        {
            Manual,
            Auto
        }
        enum CommandStatus
        {
            NoCommand,
            MCS,
            OHTC
        }
        enum VehicleState
        {
            Remove,
            Install
        }
        enum RepairStatus
        {
            None,
            Repair,
            Matain
        }
        enum VhErrorStatus
        {
            AlarmClean,
            AlarmSet,
            AlarmConfirm
        }
        enum ChargeStatus
        {
            NoCharge,
            Charging
        }



        public VehicleStatusInfo(AVEHICLE _vh)
        {
            vh = _vh;
        }
        AVEHICLE vh;
        bool IsConnected { get { return vh.isTcpIpConnect; } }
        ControlStatus controlStatus
        {
            get
            {
                switch (vh.MODE_STATUS)
                {
                    case ProtocolFormat.OHTMessage.VHModeStatus.AutoLocal:
                        return ControlStatus.Local;
                    case ProtocolFormat.OHTMessage.VHModeStatus.AutoRemote:
                        return ControlStatus.Remote;
                    default:
                        return ControlStatus.None;
                }
            }
        }
        //
        VehicleStatus vehicleStatus
        {
            get
            {
                switch (vh.MODE_STATUS)
                {
                    case ProtocolFormat.OHTMessage.VHModeStatus.AutoCharging:
                    case ProtocolFormat.OHTMessage.VHModeStatus.AutoLocal:
                        return VehicleStatus.Auto;
                    default:
                        return VehicleStatus.Manual;
                }
            }
        }

        CommandStatus commandStatus
        {
            get
            {
                if (vh.ACT_STATUS == ProtocolFormat.OHTMessage.VHActionStatus.Commanding)
                {
                    if (sc.Common.SCUtility.isEmpty(vh.MCS_CMD))
                    {
                        return CommandStatus.OHTC;
                    }
                    else
                    {
                        return CommandStatus.MCS;
                    }
                }
                else
                {
                    return CommandStatus.NoCommand;
                }
            }
        }


        VehicleState vehicleState
        {
            get
            {
                if (vh.IS_INSTALLED)
                    return VehicleState.Install;
                else
                    return VehicleState.Remove;
            }
        }
        RepairStatus repairStatus
        {
            get
            {
                return RepairStatus.Matain;
            }
        }

        //Error Status
        VhErrorStatus errorStatus
        {
            get
            {
                switch (vh.errorState)
                {
                    case AVEHICLE.VehicleErrorState.AlarmConfirm:
                        return VhErrorStatus.AlarmConfirm;
                    case AVEHICLE.VehicleErrorState.AlarmHappending:
                        return VhErrorStatus.AlarmSet;
                    case AVEHICLE.VehicleErrorState.NoAlarm:
                        return VhErrorStatus.AlarmClean;
                    default:
                        return VhErrorStatus.AlarmClean;
                }

            }
        }

        ChargeStatus chargeStatus
        {
            get
            {
                switch (vh.ChargeStatus)
                {
                    case ProtocolFormat.OHTMessage.VhChargeStatus.ChargeStatusCharging:
                        return ChargeStatus.Charging;
                    default:
                        return ChargeStatus.NoCharge;
                }
            }
        }
        bool IsLongCharging
        {
            get
            {
                switch (vh.ChargeStatus)
                {
                    case ProtocolFormat.OHTMessage.VhChargeStatus.ChargeStatusCharging:
                        if (vh.IsNeedToLongCharge())
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    default:
                        return false;
                }
            }
        }
        bool IsCSTInstall
        {
            get
            {
                if (vh.HAS_CST == 1)
                {
                    return true;
                }
                else
                    return false;
            }
        }


        StringBuilder sb = new StringBuilder();
        /// <summary>
        //<column name="Time" layout="${message}" />
        //<column name="VehicleID" layout="" />
        //<column name="IsConnected" layout="" />
        //<column name="controlStatus" layout="" />
        //<column name="vehicleStatus" layout="" />
        //<column name="commandStatus" layout="" />
        //<column name="vehicleState" layout="" />
        //<column name="repairStatus" layout="" />
        //<column name="errorStatus" layout="" />
        //<column name="chargeStatus" layout="" />
        //<column name="IsLongCharging" layout="" />
        //<column name="IsCSTInstall" layout="" />
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            sb.Clear();
            sb.Append(DateTime.Now.ToString(App.SCAppConstants.DateTimeFormat_19));
            sb.Append(vh.VEHICLE_ID).Append(",");
            sb.Append(IsConnected).Append(",");
            sb.Append(controlStatus).Append(",");
            sb.Append(vehicleStatus).Append(",");
            sb.Append(commandStatus).Append(",");
            sb.Append(vehicleState).Append(",");
            sb.Append(repairStatus).Append(",");
            sb.Append(errorStatus).Append(",");
            sb.Append(chargeStatus).Append(",");
            sb.Append(IsLongCharging).Append(",");
            sb.Append(IsCSTInstall);
            string record_message = sb.ToString();
            return record_message;
        }

    }
}
