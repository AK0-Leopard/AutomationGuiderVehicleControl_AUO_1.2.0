using com.mirle.ibg3k0.bcf.App;
using com.mirle.ibg3k0.bcf.Common;
using com.mirle.ibg3k0.bcf.Data.ValueDefMapAction;
using com.mirle.ibg3k0.bcf.Data.VO;
using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Data.SECS;
using com.mirle.ibg3k0.sc.Data.ValueDefMapAction;
using com.mirle.ibg3k0.sc.Data.VO;
using com.mirle.ibg3k0.sc.Data.VO.Interface;
using com.mirle.ibg3k0.sc.ObjectRelay;
using com.mirle.ibg3k0.sc.ProtocolFormat.OHTMessage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.mirle.ibg3k0.sc
{
    public partial class AEQPT : BaseEQObject, IAlarmHisList, IVIDCollection
    {
        public AEQPT()
        {
            eqptObjectCate = SCAppConstants.EQPT_OBJECT_CATE_EQPT;
        }

        #region MCharger
        private int agvcAliveIndex;
        public virtual int AGVCAliveIndex
        {
            get { return agvcAliveIndex; }
            set
            {
                if (agvcAliveIndex != value)
                {
                    agvcAliveIndex = value;
                    OnPropertyChanged(BCFUtility.getPropertyName(() => this.AGVCAliveIndex));
                }
            }
        }
        private int abnormalReportIndex;
        public virtual int AbnormalReportIndex
        {
            get { return abnormalReportIndex; }
            set
            {
                if (abnormalReportIndex != value)
                {
                    abnormalReportIndex = value;
                    OnPropertyChanged(BCFUtility.getPropertyName(() => this.AbnormalReportIndex));
                }
            }
        }
        public virtual int abnormalReportCode01 { get; set; }
        public virtual int abnormalReportCode02 { get; set; }
        public virtual int abnormalReportCode03 { get; set; }
        public virtual int abnormalReportCode04 { get; set; }
        public virtual int abnormalReportCode05 { get; set; }
        #endregion MCharger
        public virtual SCAppConstants.EqptType Type { get; set; }

        private AlarmHisList alarmHisList = new AlarmHisList();
        public VIDCollection VID_Collection;

        public List<AUNIT> UnitList;

        public override void doShareMemoryInit(BCFAppConstants.RUN_LEVEL runLevel)
        {
            foreach (IValueDefMapAction action in valueDefMapActionDic.Values)
            {
                action.doShareMemoryInit(runLevel);
            }
            //對sub eqpt進行初始化
            List<AUNIT> subUnitList = SCApplication.getInstance().getEQObjCacheManager().getUnitListByEquipment(EQPT_ID);
            if (subUnitList != null)
            {
                foreach (AUNIT unit in subUnitList)
                {
                    unit.doShareMemoryInit(runLevel);
                }
            }
            List<APORT> subPortList = SCApplication.getInstance().getEQObjCacheManager().getPortListByEquipment(EQPT_ID);
            if (subPortList != null)
            {
                foreach (APORT port in subPortList)
                {
                    port.doShareMemoryInit(runLevel);
                }
            }
            List<ABUFFER> subBuffList = SCApplication.getInstance().getEQObjCacheManager().getBuffListByEquipment(EQPT_ID);
            if (subBuffList != null)
            {
                foreach (ABUFFER buff in subBuffList)
                {
                    buff.doShareMemoryInit(runLevel);
                }
            }
            List<APORTSTATION> portStationList = SCApplication.getInstance().getEQObjCacheManager().getPortStationByEquipment(EQPT_ID);
            if (portStationList != null)
            {
                foreach (APORTSTATION portStation in portStationList)
                {
                    portStation.doShareMemoryInit(runLevel);
                }
            }
        }

        public virtual void resetAlarmHis(List<ALARM> AlarmHisList)
        {
            alarmHisList.resetAlarmHis(AlarmHisList);
        }

        public VIDCollection getVIDCollection()
        {
            return VID_Collection;
        }

        public string Process_Data_Format { get; set; }             //A0.11
        private com.mirle.ibg3k0.sc.ConfigHandler.ProcessDataConfigHandler procDataConfigHandler;     //A0.11
        public com.mirle.ibg3k0.sc.ConfigHandler.ProcessDataConfigHandler getProcessDataConfigHandler()
        {
            if (BCFUtility.isEmpty(Process_Data_Format))
            {
                return null;
            }
            if (procDataConfigHandler == null)
            {
                procDataConfigHandler =
                    new com.mirle.ibg3k0.sc.ConfigHandler.ProcessDataConfigHandler(Process_Data_Format);
            }
            return procDataConfigHandler;
        }
        #region PortStation
        public bool EQ_Ready { get; set; }
        public bool EQ_Error { get; set; }
        public bool EQ_Down { get; set; }
        public bool EQ_Disable { get; set; }
        #endregion PortStation
        #region FireReport
        public bool fireReport { get; set; }
        //public bool fireDoorOpen { get; set; }
        //public bool fireDoorCloseGrant { get; set; }
        //public bool fireDoorCrossingSignal { get; set; }

        public void fireDoorCancelAbortCommand(SCApplication scApp)
        {
            //Cancel當前Queue的會找不到路徑的命令。
            List<ACMD_MCS> queue_mcs_cmds = scApp.CMDBLL.loadMCS_Command_Queue();
            foreach (ACMD_MCS cmd in queue_mcs_cmds)
            {
                bool isWalkable = true;
                bool source_is_a_port = scApp.PortStationBLL.OperateCatch.IsExist(cmd.HOSTSOURCE);
                if (source_is_a_port)
                {
                    APORTSTATION source_port_station = scApp.PortStationBLL.OperateCatch.getPortStation(cmd.HOSTSOURCE);
                    APORTSTATION dest_port_station = scApp.PortStationBLL.OperateCatch.getPortStation(cmd.HOSTDESTINATION);
                    isWalkable = scApp.GuideBLL.IsRoadWalkable(source_port_station.ADR_ID, dest_port_station.ADR_ID);
                }
                else
                {
                    AVEHICLE carry_vh = scApp.VehicleBLL.cache.getVehicleByRealID(cmd.HOSTSOURCE);
                    APORTSTATION dest_port_station = scApp.PortStationBLL.OperateCatch.getPortStation(cmd.HOSTDESTINATION);
                    isWalkable = scApp.GuideBLL.IsRoadWalkable(carry_vh.CUR_ADR_ID, dest_port_station.ADR_ID);
                }



                if (!isWalkable)
                {
                    //await Task.Run(() => mainform.BCApp.scApp.VehicleService.doCancelOrAbortCommandByMCSCmdID(mcs_cmd.CMD_ID, cnacel_type));
                    scApp.VehicleService.doCancelOrAbortCommandByMCSCmdID(cmd.CMD_ID, CMDCancelType.CmdCancel);
                }
            }
            //Abort已經執行，但是會經過防火門的命令。
            List<ACMD_MCS> executing_mcs_cmds = scApp.CMDBLL.loadMCS_Command_Executing();
            foreach (ACMD_MCS cmd in executing_mcs_cmds)
            {
                bool isWalkable = true;
                bool source_is_a_port = scApp.PortStationBLL.OperateCatch.IsExist(cmd.HOSTSOURCE);
                if (source_is_a_port)
                {
                    APORTSTATION source_port_station = scApp.PortStationBLL.OperateCatch.getPortStation(cmd.HOSTSOURCE);
                    APORTSTATION dest_port_station = scApp.PortStationBLL.OperateCatch.getPortStation(cmd.HOSTDESTINATION);
                    isWalkable = scApp.GuideBLL.IsRoadWalkable(source_port_station.ADR_ID, dest_port_station.ADR_ID);
                }
                else
                {
                    AVEHICLE carry_vh = scApp.VehicleBLL.cache.getVehicleByRealID(cmd.HOSTSOURCE);
                    APORTSTATION dest_port_station = scApp.PortStationBLL.OperateCatch.getPortStation(cmd.HOSTDESTINATION);
                    isWalkable = scApp.GuideBLL.IsRoadWalkable(carry_vh.CUR_ADR_ID, dest_port_station.ADR_ID);
                }

                if (!isWalkable)
                {
                    //await Task.Run(() => mainform.BCApp.scApp.VehicleService.doCancelOrAbortCommandByMCSCmdID(mcs_cmd.CMD_ID, cnacel_type));
                    CMDCancelType cnacel_type = default(CMDCancelType);
                    if (cmd.TRANSFERSTATE < sc.E_TRAN_STATUS.Transferring)
                    {
                        cnacel_type = CMDCancelType.CmdCancel;
                    }
                    else if (cmd.TRANSFERSTATE < sc.E_TRAN_STATUS.Canceling)
                    {
                        cnacel_type = CMDCancelType.CmdAbort;
                    }
                    else
                    {
                        continue;
                    }
                    scApp.VehicleService.doCancelOrAbortCommandByMCSCmdID(cmd.CMD_ID, cnacel_type);
                }
            }
        }
        #endregion FireReport

        #region TrafficLight
        //public bool passRequest { get; set; }
        //public bool passGranted { get; set; }
        //public DateTime? passGrantedTime { get; set; }
        object traffic_light_lock = new object();
        public void setTrafficLight(bool work_signal,bool red_signal, bool yellow_signal, bool green_signal, bool buzzer_signal, bool force_on_signal)
        {
            lock (traffic_light_lock)
            {
                TrafficLightValueDefMapAction mapAction =
                    getMapActionByIdentityKey(nameof(TrafficLightValueDefMapAction)) as TrafficLightValueDefMapAction;
                mapAction.sendTrafficLightSignal(work_signal, red_signal, yellow_signal, green_signal, buzzer_signal, force_on_signal);
            }
        }
        public void setTrafficWorkSignal(bool work_signal)
        {
            lock (traffic_light_lock)
            {
                TrafficLightValueDefMapAction mapAction =
                    getMapActionByIdentityKey(nameof(TrafficLightValueDefMapAction)) as TrafficLightValueDefMapAction;
                mapAction.sendTrafficLightWorkSignal(work_signal);
            }
        }
        public void setTrafficYellowSignal(bool yellow_signal)
        {
            lock (traffic_light_lock)
            {
                TrafficLightValueDefMapAction mapAction =
                    getMapActionByIdentityKey(nameof(TrafficLightValueDefMapAction)) as TrafficLightValueDefMapAction;
                mapAction.sendTrafficLightYellowSignal(yellow_signal);
            }
        }

        #endregion TrafficLight


        //Color Light
        #region Color Light


        object color_light_lock = new object();

        public void setColorLight( bool red_signal, bool yellow_signal, bool green_signal, bool blue_signal, bool buzzer_signal, bool force_on_signal)
        {
            lock (color_light_lock)
            {
                ColorLightValueDefMapAction mapAction =
                    getMapActionByIdentityKey(nameof(ColorLightValueDefMapAction)) as ColorLightValueDefMapAction;
                //mapAction.sendFourColorLightRedWithBuzzerSignal(buzzer_signal, light_signal);
                mapAction.sendColorLightSignal(red_signal, yellow_signal, green_signal, blue_signal, buzzer_signal, force_on_signal);
            }
        }

        public void setColorLightRedWithBuzzer(bool buzzer_signal, bool light_signal)
        {
            lock (color_light_lock)
            {
                ColorLightValueDefMapAction mapAction =
                    getMapActionByIdentityKey(nameof(ColorLightValueDefMapAction)) as ColorLightValueDefMapAction;
                //mapAction.sendFourColorLightRedWithBuzzerSignal(buzzer_signal, light_signal);
                mapAction.sendColorLightRedWithBuzzerSignal(buzzer_signal, light_signal);
            }
        }
        public void setColorLightYellow(bool signal)
        {
            lock (color_light_lock)
            {
                ColorLightValueDefMapAction mapAction =
                    getMapActionByIdentityKey(nameof(ColorLightValueDefMapAction)) as ColorLightValueDefMapAction;
                mapAction.sendColorLightYellowSignal(signal);
            }
        }
        public void setColorLightGreen(bool signal)
        {
            lock (color_light_lock)
            {
                ColorLightValueDefMapAction mapAction =
                    getMapActionByIdentityKey(nameof(ColorLightValueDefMapAction)) as ColorLightValueDefMapAction;
                mapAction.sendColorLightGreenSignal(signal);
            }
        }
        public void setColorLightBlue(bool signal)
        {
            lock (color_light_lock)
            {
                ColorLightValueDefMapAction mapAction =
                    getMapActionByIdentityKey(nameof(ColorLightValueDefMapAction)) as ColorLightValueDefMapAction;
                mapAction.sendColorLightBlueSignal(signal);
            }
        }
        #endregion Four Color Light
    }

}
