// ***********************************************************************
// Assembly         : ScriptControl
// Author           : 
// Created          : 03-31-2016
//
// Last Modified By : 
// Last Modified On : 03-24-2016
// ***********************************************************************
// <copyright file="SECSConst.cs" company="">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using com.mirle.ibg3k0.stc.Common.SECS;

namespace com.mirle.ibg3k0.sc.Data.SECS.SouthInnolux
{
    /// <summary>
    /// Class SECSConst.
    /// </summary>
    public class SECSConst
    {
        #region Sample Flag
        /// <summary>
        /// The smplfla g_ selected
        /// </summary>
        public static readonly string SMPLFLAG_Selected = "Y";
        /// <summary>
        /// The smplfla g_ not_ selected
        /// </summary>
        public static readonly string SMPLFLAG_Not_Selected = "N";
        #endregion Sample Flag

        #region Host Control Mode
        public static readonly string HostCrtMode_EQ_Off_line = "1";
        public static readonly string HostCrtMode_Going_Online = "2";
        public static readonly string HostCrtMode_Host_Online = "3";
        public static readonly string HostCrtMode_On_Line_Local = "4";
        public static readonly string HostCrtMode_On_Line_Remote = "5";

        public static readonly string[] HOST_CRT_MODE =
        {
            HostCrtMode_EQ_Off_line,
            HostCrtMode_Going_Online,
            HostCrtMode_Host_Online,
            HostCrtMode_On_Line_Local,
            HostCrtMode_On_Line_Remote
        };
        #endregion Host Control Mode




        #region Vehicle State
        public static readonly string VHSTATE_NotRelated = "0";
        public static readonly string VHSTATE_Removed = "1";
        public static readonly string VHSTATE_NotAssigned = "2";
        public static readonly string VHSTATE_Enroute = "3";
        public static readonly string VHSTATE_Parked = "4";
        public static readonly string VHSTATE_Acquiring = "5";
        public static readonly string VHSTATE_Depositiong = "6";
        #endregion Vehicle State

        #region TRSMODE
        /// <summary>
        /// The trsmod e_ automatic
        /// </summary>
        public static readonly string TRSMODE_AUTO = "1";
        /// <summary>
        /// The trsmod e_ manual
        /// </summary>
        public static readonly string TRSMODE_Manual = "2";
        #endregion TRSMODE

        #region PPTYPE
        /// <summary>
        /// The pptyp e_ equipment
        /// </summary>
        public static readonly string PPTYPE_Equipment = "E";
        /// <summary>
        /// The pptyp e_ unit
        /// </summary>
        public static readonly string PPTYPE_Unit = "U";
        /// <summary>
        /// The pptyp e_ sub_ unit
        /// </summary>
        public static readonly string PPTYPE_Sub_Unit = "S";
        #endregion PPTYPE

        #region ONLACK
        public static readonly string ONLACK_Accepted = "0";
        public static readonly string ONLACK_Not_Accepted = "1";
        public static readonly string ONLACK_Equipment_Already_On_Line = "2";
        #endregion ONLACK

        #region SFCD
        /// <summary>
        /// The SFC d_ module_ status_ request
        /// </summary>
        public static readonly string SFCD_Module_Status_Request = "1";
        /// <summary>
        /// The SFC d_ port_ status_ request
        /// </summary>
        public static readonly string SFCD_Port_Status_Request = "2";
        /// <summary>
        /// The SFC d_ reticle_ status_ request
        /// </summary>
        public static readonly string SFCD_Reticle_Status_Request = "3";
        /// <summary>
        /// The SFC d_ unit_ status_ request
        /// </summary>
        public static readonly string SFCD_Unit_Status_Request = "4";
        /// <summary>
        /// The SFC d_ sub_ unit_ status_ request
        /// </summary>
        public static readonly string SFCD_Sub_Unit_Status_Request = "5";
        /// <summary>
        /// The SFC d_ mask_ status_ request
        /// </summary>
        public static readonly string SFCD_Mask_Status_Request = "6";
        /// <summary>
        /// The SFC d_ material_ status_ request
        /// </summary>
        public static readonly string SFCD_Material_Status_Request = "7";
        /// <summary>
        /// The SFC d_ sorter_ job_ list_ request
        /// </summary>
        public static readonly string SFCD_Sorter_Job_List_Request = "8";
        /// <summary>
        /// The SFC d_ crate_ port_ status_ request
        /// </summary>
        public static readonly string SFCD_Crate_Port_Status_Request = "9";
        /// <summary>
        /// The SFC d_ finish
        /// </summary>
        public static readonly string SFCD_Finish = "F";
        #endregion SFCD

        #region CIACK
        /// <summary>
        /// The ciac k_ accepted
        /// </summary>
        public static readonly string CIACK_Accepted = "0";
        /// <summary>
        /// The ciac k_ busy
        /// </summary>
        public static readonly string CIACK_Busy = "1";
        /// <summary>
        /// The ciac k_ csti d_is_ invalid
        /// </summary>
        public static readonly string CIACK_CSTID_is_Invalid = "2";
        /// <summary>
        /// The ciac k_ ppi d_is_ invalid
        /// </summary>
        public static readonly string CIACK_PPID_is_Invalid = "3";
        /// <summary>
        /// The ciac k_ slo t_ information_ mismatch
        /// </summary>
        public static readonly string CIACK_SLOT_Information_Mismatch = "4";
        /// <summary>
        /// The ciac k_ already_ received
        /// </summary>
        public static readonly string CIACK_Already_Received = "5";
        /// <summary>
        /// The ciac k_ pai r_ lo t_ mismatch
        /// </summary>
        public static readonly string CIACK_PAIR_LOT_Mismatch = "6";
        /// <summary>
        /// The ciac k_ pro d_ i d_ invalid
        /// </summary>
        public static readonly string CIACK_PROD_ID_Invalid = "7";
        /// <summary>
        /// The ciac k_ glass_ type_ invalid
        /// </summary>
        public static readonly string CIACK_Glass_Type_Invalid = "8";
        /// <summary>
        /// The ciac k_ other_ error
        /// </summary>
        public static readonly string CIACK_Other_Error = "9";
        #endregion CIACK

        #region RCMD
        public const string RCMD_Abort = "ABORT";
        public const string RCMD_Cancel = "CANCEL";
        public const string RCMD_Pause = "PAUSE";
        public const string RCMD_Resume = "RESUME";
        public const string RCMD_PriorityUpdate = "PRIORITYUPDATE";
        public const string RCMD_Install = "INSTALL";
        public const string RCMD_Remove = "REMOVE";
        public const string RCMD_TransferEXt = "TRANSFEREXT";
        public const string RCMD_Rename = "RENAME";
        public const string RCMD_StageDelete = "STAGEDELETE";
        #endregion RCMD

        #region CPNAME
        public const string CPNAME_CommandID = "COMMANDID";
        public const string CPNAME_SourcePort = "SOURCEPORT";
        public const string CPNAME_DestPort = "DESTPORT";
        #endregion CPNAME


        #region HCACK
        public static readonly string HCACK_Confirm_Executed = "0";
        public static readonly string HCACK_Command_Not_Exist = "1";
        public static readonly string HCACK_Cannot_Perform_Now = "2";
        public static readonly string HCACK_Param_Invalid = "3";
        public static readonly string HCACK_Confirm = "4";
        public static readonly string HCACK_Rejected = "5";
        public static readonly string HCACK_Obj_Not_Exist = "6";
        #endregion HCACK
        #region CEPACK
        public static readonly string CEPACK_No_Error = "0";
        public static readonly string CEPACK_CPNAME_Does_Not_Exist = "1";
        public static readonly string CEPACK_Incorrect_Value_In_CEPVAL = "2";
        public static readonly string CEPACK_Incorrect_Format_In_CEPVAL = "3";
        public static readonly string CEPACK_CPNAME_is_not_Valid = "4";
        #endregion HCACK

        #region CMD Result Code
        public const string CMD_Result_Successful = "0";
        public const string CMD_Result_AGV_Offline = "1";
        public const string CMD_Result_DisableUnload = "2";
        public const string CMD_Result_DisableLoad = "3";
        public const string CMD_Result_BCRError = "4";
        public const string CMD_Result_CarrierIDUnmach = "5";
        public const string CMD_Result_UnloadTimeout = "6";
        public const string CMD_Result_UnloadError = "7";
        public const string CMD_Result_LoadTimeout = "8";
        public const string CMD_Result_LoadError = "9";
        public const string CMD_Result_Unsuccessful = "100";
        public static string convert2MCS(AVEHICLE.VehicleState vehicleState, ACMD_MCS cmd_mcs, ProtocolFormat.OHTMessage.CompleteStatus tran_cmp_status)
        {
            switch (tran_cmp_status)
            {
                case ProtocolFormat.OHTMessage.CompleteStatus.CmpStatusMove:
                case ProtocolFormat.OHTMessage.CompleteStatus.CmpStatusLoad:
                case ProtocolFormat.OHTMessage.CompleteStatus.CmpStatusUnload:
                case ProtocolFormat.OHTMessage.CompleteStatus.CmpStatusLoadunload:
                case ProtocolFormat.OHTMessage.CompleteStatus.CmpStatusHome:
                case ProtocolFormat.OHTMessage.CompleteStatus.CmpStatusOverride:
                case ProtocolFormat.OHTMessage.CompleteStatus.CmpStatusCstIdrenmae:
                case ProtocolFormat.OHTMessage.CompleteStatus.CmpStatusMtlhome:
                case ProtocolFormat.OHTMessage.CompleteStatus.CmpStatusMoveToCharger:
                case ProtocolFormat.OHTMessage.CompleteStatus.CmpStatusSystemOut:
                case ProtocolFormat.OHTMessage.CompleteStatus.CmpStatusSystemIn:
                case ProtocolFormat.OHTMessage.CompleteStatus.CmpStatusTechingMove:
                    return CMD_Result_Successful;
                case ProtocolFormat.OHTMessage.CompleteStatus.CmpStatusCancel:
                case ProtocolFormat.OHTMessage.CompleteStatus.CmpStatusAbort:
                case ProtocolFormat.OHTMessage.CompleteStatus.CmpStatusVehicleAbort:
                case ProtocolFormat.OHTMessage.CompleteStatus.CmpStatusLongTimeInaction:
                case ProtocolFormat.OHTMessage.CompleteStatus.CmpStatusForceFinishByOp:
                    return CMD_Result_Unsuccessful;
                case ProtocolFormat.OHTMessage.CompleteStatus.CmpStatusIdreadFailed:
                    return CMD_Result_BCRError;
                case ProtocolFormat.OHTMessage.CompleteStatus.CmpStatusIdmisMatch:
                    return CMD_Result_CarrierIDUnmach;
                case ProtocolFormat.OHTMessage.CompleteStatus.CmpStatusInterlockError:
                    if (cmd_mcs == null)
                    {
                        return CMD_Result_Unsuccessful;
                    }
                    else
                    {
                        if(cmd_mcs.TRANSFERSTATE < E_TRAN_STATUS.Transferring)
                        {
                            return CMD_Result_UnloadError;
                        }
                        else
                        {
                            return CMD_Result_LoadError;
                        }
                        //if (cmd_mcs.isLoading)
                        //{
                        //    return CMD_Result_UnloadError;
                        //}
                        //else if (cmd_mcs.isUnloading)
                        //{
                        //    return CMD_Result_LoadError;
                        //}
                        //else
                        //{
                        //    return CMD_Result_Unsuccessful;
                        //}
                    }
                default:
                    throw new Exception("參數錯誤"); //TODO 要帶入正確的Exception。
            }
        }
        #endregion CMD Result Code
        #region ACK
        /// <summary>
        /// The ac k_ accepted
        /// </summary>
        public static readonly string ACK_Accepted = "0";
        /// <summary>
        /// The ac k_ not_ accepted
        /// </summary>
        public static readonly string ACK_Not_Accepted = "1";
        #endregion ACK

        public class AlarmStatus
        {
            public static string convert2MCS(ProtocolFormat.OHTMessage.ErrorStatus alarm_status, E_ALARM_LVL alarm_lvl)
            {
                switch (alarm_status)
                {
                    case ProtocolFormat.OHTMessage.ErrorStatus.ErrSet:
                        if (alarm_lvl == E_ALARM_LVL.Error)
                            return ALCD_Alarm_Set_Heavy;
                        else
                            return ALCD_Alarm_Set_Light;
                    case ProtocolFormat.OHTMessage.ErrorStatus.ErrReset:
                        if (alarm_lvl == E_ALARM_LVL.Error)
                            return ALCD_Alarm_Clear_Heavy;
                        else
                            return ALCD_Alarm_Clear_Light;
                    default:
                        return "";
                }
            }
        }


        public class ALID
        {
            public static string convert2MCS(E_ALARM_LVL alarm_lvl)
            {
                switch (alarm_lvl)
                {
                    case E_ALARM_LVL.Error:
                        return ALID_Vehicle_Heavy_Error;
                    case E_ALARM_LVL.Warn:
                        return ALID_Vehicle_Light_Error;
                    case E_ALARM_LVL.None:
                        return ALID_Vehicle_Light_Error;
                    default:
                        return "";
                }
            }
        }

        public class ALTX
        {
            public static string convert2MCS(E_ALARM_LVL alarm_lvl)
            {
                switch (alarm_lvl)
                {
                    case E_ALARM_LVL.Error:
                        return ALTX_Vehicle_Heavy_Error;
                    case E_ALARM_LVL.Warn:
                        return ALTX_Vehicle_Light_Error;
                    case E_ALARM_LVL.None:
                        return ALTX_Vehicle_Light_Error;
                    default:
                        return "";
                }
            }
        }
        #region CEID
        /// <summary>
        /// 用來代表所有的CEID（於Enable、Disable All CEID時會使用到）。
        /// </summary>
        //public const string CEID_ALL_CEID = "000";
        //CEID Control Related Events
        public const string CEID_Equipment_OFF_LINE = "001";
        public const string CEID_Control_Status_Local = "002";
        public const string CEID_Control_Status_Remote = "003";
        //SC Transition Events
        public const string CEID_Alarm_Cleared = "051";
        public const string CEID_Alarm_Set = "052";
        public const string CEID_TSC_Auto_Completed = "053";
        public const string CEID_TSC_Auto_Initiated = "054";
        public const string CEID_TSC_Pause_Completed = "055";
        public const string CEID_TSC_Paused = "056";
        public const string CEID_TSC_Pause_Initiated = "057";
        //Transfer Command Status Transition Events
        public const string CEID_Transfer_Abort_Completed = "101";
        public const string CEID_Transfer_Abort_Failed = "102";
        public const string CEID_Transfer_Abort_Initiated = "103";
        public const string CEID_Transfer_Cancel_Completed = "104";
        public const string CEID_Transfer_Cancel_Failed = "105";
        public const string CEID_Transfer_Cancel_Initiated = "106";
        public const string CEID_Transfer_Completed = "107";
        public const string CEID_Transfer_Initiated = "108";
        public const string CEID_Transfer_Pause = "109";
        public const string CEID_Transfer_Resumed = "110";
        public const string CEID_Transferring = "111";
        //Carrier Status Transition Events (CEID 151 ~ 152)
        public const string CEID_Carrier_Installed = "151";
        public const string CEID_Carrier_Removed = "152";
        //Vehicle Status Transition Events 
        public const string CEID_Vehicle_Arrived = "201";
        public const string CEID_Vehicle_Acquire_Started = "202";
        public const string CEID_Vehicle_Acquire_Completed = "203";
        public const string CEID_Vehicle_Assigned = "204";
        public const string CEID_Vehicle_Departed = "205";
        public const string CEID_Vehicle_Deposit_Started = "206";
        public const string CEID_Vehicle_Deposit_Completed = "207";
        public const string CEID_Vehicle_Installed = "208";
        public const string CEID_Vehicle_Removed = "209";
        public const string CEID_Vehicle_Unassigned = "210";
        //Supplier Definitions Events 
        public const string CEID_Operator_Initiated_Action = "601";
        public const string CEID_Port_Event_State_Changed = "602";
        public const string CEID_Vehicle_Charge_Request = "603";
        public const string CEID_Vehicle_Charge_Started = "604";
        public const string CEID_Vehicle_Charge_Completed = "605";
        public const string CEID_Unit_Error_Cleared = "901";
        public const string CEID_Unit_Error_Set = "902";

        //CEID Remark End
        #region CEID Array

        #region VID
        public const string VID_AlarmID = "1";
        public const string VID_Establish_Communications_Timeout = "2";
        public const string VID_Alarm_Enabled = "3";
        public const string VID_Alarm_Set = "4";
        public const string VID_Clock = "5";
        public const string VID_Control_State = "6";
        public const string VID_Events_Enabled = "7";
        public const string VID_Active_Carriers = "51";
        public const string VID_Active_Transfers = "52";
        public const string VID_Active_Vehicles = "53";
        public const string VID_Carrier_ID = "54";
        public const string VID_CarrierInfo = "55";
        public const string VID_Carrier_Loc = "56";
        public const string VID_Command_Name = "57";
        public const string VID_Command_ID = "58";
        public const string VID_Command_Info = "59";
        public const string VID_Command_Type = "60";
        public const string VID_Destination_Port = "61";
        public const string VID_Enhanced_Carriers = "62";
        public const string VID_Enhanced_Transfers = "63";
        public const string VID_EqpName = "64";
        public const string VID_InstallTime = "64";
        public const string VID_Priority = "65";
        public const string VID_Replace = "66";
        public const string VID_Result_Code = "67";
        public const string VID_Source_Port = "68";
        public const string VID_Transfer_Command = "69";
        public const string VID_Transfer_Info = "70";
        public const string VID_Transfer_Port = "71";
        public const string VID_Transfer_Port_List = "72";
        public const string VID_Transfer_State = "72";
        public const string VID_TCS_State = "73";
        public const string VID_Vehicle_ID = "74";
        public const string VID_Vehicle_Info = "75";
        public const string VID_Vehicle_State = "76";
        public const string VID_Transfer_Complete_Info = "301";
        public const string VID_Port_ID = "302";
        public const string VID_Port_Evt_State = "303";
        public const string VID_Port_Event_State = "304";
        public const string VID_Registered_Ports = "305";
        public const string VID_Near_Stocker_Port = "310";
        public const string VID_Current_Node = "311";
        public const string VID_Alarm_Text = "901";
        public const string VID_Charger_ID = "902";
        public const string VID_Error_Code = "903";
        public const string VID_Unit_ID = "904";
        public const string VID_Error_Text = "905";
        #region Using Synchronize 
        //public const string VID_AlarmsSet = "4";
        //public const string VID_ControlState = "6";
        //public const string VID_Enhanced_Carriers = "51";
        //public const string VID_SCState = "73";
        //public const string VID_Current_Port_States = "118";
        //public const string VID_Current_EQ_Port_Status = "350";
        //public const string VID_EQ_Port_Info = "351";
        //public const string VID_EQ_Req_Status = "352";
        //public const string VID_UnitAlarm = "360";
        #endregion Using Synchronize 
        public static Dictionary<string, string> VID_Dictionary = new Dictionary<string, string>()
        {
            {VID_AlarmID,"AlarmID" },
            {VID_Establish_Communications_Timeout,"Establish_Communications_Timeout" },
            {VID_Alarm_Enabled,"Alarm_Enabled" },
            {VID_Alarm_Set,"Alarm_Set" },
            {VID_Clock,"Clock" },
            {VID_Control_State,"Control_State" },
            {VID_Events_Enabled,"Events_Enabled" },
            {VID_Active_Carriers,"Active_Carriers" },
            {VID_Active_Transfers,"Active_Transfers" },
            {VID_Active_Vehicles,"Active_Vehicles" },
            {VID_Carrier_ID,"Carrier_ID" },
            {VID_CarrierInfo,"CarrierInfo" },
            {VID_Carrier_Loc,"Carrier_Loc" },
            {VID_Command_Name,"Command_Name" },
            {VID_Command_ID,"Command_ID" },
            {VID_Command_Info,"Command_Info" },
            {VID_Command_Type,"Command_Type" },
            {VID_Destination_Port,"Destination_Port" },
            {VID_Enhanced_Carriers,"Enhanced_Carriers" },
            {VID_Enhanced_Transfers,"Enhanced_Transfers" },
            {VID_InstallTime,"InstallTime" },
            {VID_Priority,"Priority" },
            {VID_Replace,"Replace" },
            {VID_Result_Code,"Result_Code" },
            {VID_Source_Port,"Source_Port" },
            {VID_Transfer_Command,"Transfer_Command" },
            {VID_Transfer_Info,"Transfer_Info" },
            {VID_Transfer_Port,"Transfer_Port" },
            {VID_Transfer_State,"Transfer_State" },
            {VID_TCS_State,"TCS_State" },
            {VID_Vehicle_ID,"Vehicle_ID" },
            {VID_Vehicle_Info,"Vehicle_Info" },
            {VID_Vehicle_State,"Vehicle_State" },
            {VID_Transfer_Complete_Info,"Transfer_Complete_Info" },
            {VID_Port_ID,"Port_ID" },
            {VID_Port_Evt_State,"Port_Evt_State" },
            {VID_Port_Event_State,"Port_Event_State" },
            {VID_Registered_Ports,"Registered_Ports" },
            {VID_Near_Stocker_Port,"Near_Stocker_Port" },
            {VID_Current_Node,"Current_Node" },
            {VID_Alarm_Text,"Alarm_Text" },
            {VID_Charger_ID,"Charger_ID" },
            {VID_Error_Code,"Error_Code" },
            {VID_Unit_ID,"Unit_ID" }
        };


        #endregion VID



        public static readonly string[] CEID_ARRAY =
        {
             CEID_Equipment_OFF_LINE,
             CEID_Control_Status_Local,
             CEID_Control_Status_Remote,
             CEID_Alarm_Cleared ,
             CEID_Alarm_Set,
             CEID_TSC_Auto_Completed ,
             CEID_TSC_Auto_Initiated,
             CEID_TSC_Pause_Completed,
             CEID_TSC_Paused,
             CEID_TSC_Pause_Initiated,
             CEID_Transfer_Abort_Completed,
             CEID_Transfer_Abort_Failed,
             CEID_Transfer_Abort_Initiated,
             CEID_Transfer_Cancel_Completed,
             CEID_Transfer_Cancel_Failed,
             CEID_Transfer_Cancel_Initiated,
             CEID_Transfer_Completed ,
             CEID_Transfer_Initiated,
             CEID_Transfer_Pause,
             CEID_Transfer_Resumed ,
             CEID_Transferring,
             CEID_Carrier_Installed ,
             CEID_Carrier_Removed,
             CEID_Vehicle_Arrived,
             CEID_Vehicle_Acquire_Started,
             CEID_Vehicle_Acquire_Completed,
             CEID_Vehicle_Assigned,
             CEID_Vehicle_Departed,
             CEID_Vehicle_Deposit_Started,
             CEID_Vehicle_Deposit_Completed,
             CEID_Vehicle_Installed,
             CEID_Vehicle_Removed ,
             CEID_Vehicle_Unassigned,
             CEID_Operator_Initiated_Action,
             CEID_Port_Event_State_Changed,
             CEID_Vehicle_Charge_Request,
             CEID_Vehicle_Charge_Started ,
             CEID_Vehicle_Charge_Completed ,
             CEID_Unit_Error_Cleared,
             CEID_Unit_Error_Set
        };
        public static Dictionary<string, string> CEID_Dictionary = new Dictionary<string, string>()
        {
            {CEID_Equipment_OFF_LINE,"Equipment_OFF_LINE" },
            {CEID_Control_Status_Local,"Equipment_OFF_LINE" },
            {CEID_Control_Status_Remote,"Control_Status_Remote" },
            {CEID_Alarm_Cleared,"Alarm_Cleared" },
            {CEID_Alarm_Set,"Alarm_Set" },
            {CEID_TSC_Auto_Completed,"TSC_Auto_Completed" },
            {CEID_TSC_Auto_Initiated,"TSC_Auto_Initiated" },
            {CEID_TSC_Pause_Completed,"TSC_Pause_Completed" },
            {CEID_TSC_Paused,"TSC_Paused" },
            {CEID_TSC_Pause_Initiated,"TSC_Pause_Initiated" },
            {CEID_Transfer_Abort_Completed,"Transfer_Abort_Completed" },
            {CEID_Transfer_Abort_Failed,"Transfer_Abort_Failed" },
            {CEID_Transfer_Abort_Initiated,"Transfer_Abort_Initiated" },
            {CEID_Transfer_Cancel_Completed,"Transfer_Cancel_Completed" },
            {CEID_Transfer_Cancel_Failed,"Transfer_Cancel_Failed" },
            {CEID_Transfer_Cancel_Initiated,"Transfer_Cancel_Initiated" },
            {CEID_Transfer_Completed,"Transfer_Completed" },
            {CEID_Transfer_Initiated,"Transfer_Initiated" },
            {CEID_Transfer_Pause,"Transfer_Pause" },
            {CEID_Transfer_Resumed,"Transfer_Resumed" },
            {CEID_Transferring,"Transferring" },
            {CEID_Carrier_Installed,"Carrier_Installed" },
            {CEID_Carrier_Removed,"Carrier_Removed" },
            {CEID_Vehicle_Arrived,"Vehicle_Arrived" },
            {CEID_Vehicle_Acquire_Started,"Vehicle_Acquire_Started" },
            {CEID_Vehicle_Acquire_Completed,"Vehicle_Acquire_Completed" },
            {CEID_Vehicle_Assigned,"Vehicle_Assigned" },
            {CEID_Vehicle_Departed,"Vehicle_Departed" },
            {CEID_Vehicle_Deposit_Started,"Vehicle_Deposit_Started" },
            {CEID_Vehicle_Deposit_Completed,"Vehicle_Deposit_Completed" },
            {CEID_Vehicle_Installed,"Vehicle_Installed" },
            {CEID_Vehicle_Removed,"Vehicle_Removed" },
            {CEID_Vehicle_Unassigned,"Vehicle_Unassigned" },
            {CEID_Operator_Initiated_Action,"Operator_Initiated_Action" },
            {CEID_Port_Event_State_Changed,"Port_Event_State_Changed" },
            {CEID_Vehicle_Charge_Request,"Vehicle_Charge_Request" },
            {CEID_Vehicle_Charge_Started,"Vehicle_Charge_Started" },
            {CEID_Vehicle_Charge_Completed,"Vehicle_Charge_Completed" },
            {CEID_Unit_Error_Cleared,"Unit_Error_Cleared" },
            {CEID_Unit_Error_Set,"Unit_Error_Set" }
        };
        #endregion CEID Array
        #endregion CEID

        #region ACKC6
        /// <summary>
        /// The ack C6_ accepted
        /// </summary>
        public static readonly string ACKC6_Accepted = "0";
        /// <summary>
        /// The ack C6_ not accepted
        /// </summary>
        public static readonly string ACKC6_NotAccepted = "1";
        #endregion ACKC6

        #region TIACK
        /// <summary>
        /// The tiac k_ accepted
        /// </summary>
        public static readonly string TIACK_Accepted = "0";
        /// <summary>
        /// The tiac k_ error_not_done
        /// </summary>
        public static readonly string TIACK_Error_not_done = "1";
        #endregion TIACK

        #region OFLACK
        /// <summary>
        /// The oflac k_ accepted
        /// </summary>
        public static readonly string OFLACK_Accepted = "0";
        /// <summary>
        /// The oflac k_ not_ accepted
        /// </summary>
        public static readonly string OFLACK_Not_Accepted = "1";
        #endregion OFLACK

        #region EAC
        /// <summary>
        /// The ea c_ accept
        /// </summary>
        public static readonly string EAC_Accept = "0";
        /// <summary>
        /// The ea c_ denied_ at_ least_one_constant_does_not_exist
        /// </summary>
        public static readonly string EAC_Denied_At_Least_one_constant_does_not_exist = "1";
        /// <summary>
        /// The ea c_ denied_ busy
        /// </summary>
        public static readonly string EAC_Denied_Busy = "2";
        /// <summary>
        /// The ea c_ denied_ at_least_one_constant_out_of_range
        /// </summary>
        public static readonly string EAC_Denied_At_least_one_constant_out_of_range = "3";
        /// <summary>
        /// The ea c_ other_equipment_specific_error
        /// </summary>
        public static readonly string EAC_Other_equipment_specific_error = "4";
        #endregion EAC

        #region TIAACK
        /// <summary>
        /// The tiaac k_ everything_correct
        /// </summary>
        public static readonly string TIAACK_Everything_correct = "0";
        /// <summary>
        /// The tiaac k_ too_many_ svi ds
        /// </summary>
        public static readonly string TIAACK_Too_many_SVIDs = "1";
        /// <summary>
        /// The tiaac k_ no_more_traces_allowed
        /// </summary>
        public static readonly string TIAACK_No_more_traces_allowed = "2";
        /// <summary>
        /// The tiaac k_ invalid_period
        /// </summary>
        public static readonly string TIAACK_Invalid_period = "3";
        /// <summary>
        /// The tiaac k_ equipment_specified_error
        /// </summary>
        public static readonly string TIAACK_Equipment_specified_error = "4";
        #endregion TIAACK

        #region ERACK
        /// <summary>
        /// The erac k_ accepted
        /// </summary>
        public static readonly string ERACK_Accepted = "0";
        /// <summary>
        /// The erac k_ denied_ at_least_one_ cei d_dose_not_exist
        /// </summary>
        public static readonly string ERACK_Denied_At_least_one_CEID_dose_not_exist = "1";
        /// <summary>
        /// The erac k_ other_ errors
        /// </summary>
        public static readonly string ERACK_Other_Errors = "2";
        #endregion ERACK

        #region ACKC5
        /// <summary>
        /// The ack C5_ accepted
        /// </summary>
        public static readonly string ACKC5_Accepted = "0";
        /// <summary>
        /// The ack C5_ not_ accepted
        /// </summary>
        public static readonly string ACKC5_Not_Accepted = "1";
        #endregion ACKC5

        #region ACKC7
        /// <summary>
        /// The ack C7_ accepted
        /// </summary>
        public static readonly string ACKC7_Accepted = "0";
        /// <summary>
        /// The ack C7_ not_ accepted
        /// </summary>
        public static readonly string ACKC7_Not_Accepted = "1";
        /// <summary>
        /// The ack C7_ unit_ i d_is_not_exist
        /// </summary>
        public static readonly string ACKC7_Unit_ID_is_not_exist = "2";
        /// <summary>
        /// The ack C7_ pptyp e_is_not_match
        /// </summary>
        public static readonly string ACKC7_PPTYPE_is_not_match = "3";
        /// <summary>
        /// The ack C7_ ppi d_is_not_match
        /// </summary>
        public static readonly string ACKC7_PPID_is_not_match = "4";
        #endregion ACKC7

        #region ACKC10
        /// <summary>
        /// The ack C10_ accepted
        /// </summary>
        public static readonly string ACKC10_Accepted = "0";
        /// <summary>
        /// The ack C10_ not_ accepted
        /// </summary>
        public static readonly string ACKC10_Not_Accepted = "1";
        #endregion ACKC10

        #region CEED
        /// <summary>
        /// The cee d_ enable
        /// </summary>
        public static readonly string CEED_Enable = "0";
        /// <summary>
        /// The cee d_ disable
        /// </summary>
        public static readonly string CEED_Disable = "1";
        #endregion CEED

        #region ALED
        /// <summary>
        /// The ale d_ enable
        /// </summary>
        public static readonly string ALED_Enable = "1";
        /// <summary>
        /// The ale d_ disable
        /// </summary>
        public static readonly string ALED_Disable = "128";
        #endregion ALED


        #region PPCINFO
        /// <summary>
        /// A new PPID is created and registered
        /// </summary>
        public static readonly string PPCINFO_Created = "1";
        /// <summary>
        /// Some parameters of a PPID are modified
        /// </summary>
        public static readonly string PPCINFO_Modified = "2";
        /// <summary>
        /// Any PPID is deleted
        /// </summary>
        public static readonly string PPCINFO_Deleted = "3";
        /// <summary>
        /// Equipment sets up any PPID which different from current PPID
        /// </summary>
        public static readonly string PPCINFO_Changed = "4";
        #endregion PPCINFO

        #region ALST
        /// <summary>
        /// The als t_ set
        /// </summary>
        public static readonly string ALST_SET = "1";
        /// <summary>
        /// The als t_ clear
        /// </summary>
        public static readonly string ALST_CLEAR = "2";
        #endregion ALST

        #region ALCD
        /// <summary>
        /// The alc d_ light_ alarm
        /// </summary>
        public static readonly string ALCD_Alarm_Set_Light = "86";
        public static readonly string ALCD_Alarm_Set_Heavy = "87";
        /// <summary>
        /// The alc d_ serious_ alarm
        /// </summary>
        public static readonly string ALCD_Alarm_Clear_Light = "6";
        public static readonly string ALCD_Alarm_Clear_Heavy = "7";
        #endregion ALCD

        #region ALTX
        public static readonly string ALTX_Vehicle_Heavy_Error = "Vehicle Heavy Error";
        public static readonly string ALTX_Vehicle_Middle_Error = "Vehicle Middle Error";
        public static readonly string ALTX_Vehicle_Light_Error = "Vehicle Light Error";
        public static readonly string ALTX_AGVC_Error = "AGVC Error";
        public static readonly string ALTX_Charger_Error = "Charger Error";

        #endregion ALTX

        #region ALID
        public static readonly string ALID_Vehicle_Heavy_Error = "1";
        public static readonly string ALID_Vehicle_Middle_Error = "2";
        public static readonly string ALID_Vehicle_Light_Error = "3";
        public static readonly string ALID_AGVC_Error = "10";
        public static readonly string ALID_Charger_Error = "12";

        #endregion ALID
        #region SCACK
        /// <summary>
        /// The scac k_ accepted
        /// </summary>
        public static readonly string SCACK_Accepted = "0";
        /// <summary>
        /// The scac k_ busy
        /// </summary>
        public static readonly string SCACK_Busy = "1";
        /// <summary>
        /// The scac k_ csti d_is_ invalid
        /// </summary>
        public static readonly string SCACK_CSTID_is_Invalid = "2";
        /// <summary>
        /// The scac k_ already_ received
        /// </summary>
        public static readonly string SCACK_Already_Received = "3";
        /// <summary>
        /// The scac k_ slo t_ information_ mismatch
        /// </summary>
        public static readonly string SCACK_SLOT_Information_Mismatch = "4";
        /// <summary>
        /// The scac k_ net yet_ prepared_ for_ this_ sorter_ job
        /// </summary>
        public static readonly string SCACK_NetYet_Prepared_For_This_Sorter_Job = "5";
        #endregion


        #region CPACK
        public static readonly string CPACK_No_Error = "0";
        public static readonly string CPACK_Name_Not_Exist = "1";
        public static readonly string CPACK_Invalid_Value = "2";
        public static readonly string CPACK_Invalid_Format = "3";
        public static readonly string CPACK_Other_Error = "4";
        #endregion

        #region SCSTATE
        public static readonly string SCSTATE_Init = "1";
        public static readonly string SCSTATE_Paused = "2";
        public static readonly string SCSTATE_Auto = "3";
        public static readonly string SCSTATE_Pausing = "4";
        #endregion SCSTATE

        #region TRANSFERSTATE
        public static readonly string TRANSFERSTATE_Queued = "1";
        public static readonly string TRANSFERSTATE_Transsfring = "2";
        public static readonly string TRANSFERSTATE_Canceling = "4";
        #endregion TRANSFERSTATE

        #region Carrier ID Read Status
        public const string IDREADSTATUS_Successful = "0";
        public const string IDREADSTATUS_Failed = "1";
        public const string IDREADSTATUS_Duplicate = "2";
        public const string IDREADSTATUS_Mismatch = "3";
        #endregion Carrier ID Read Status
        //#region PPTYPE
        //public static readonly string PPTYPE_Equipment = "E";
        //public static readonly string PPTYPE_Unit = "U";
        //public static readonly string PPTYPE_SubUnit = "S";
        //#endregion PPTYPE

        /// <summary>
        /// Checks the data value.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        /// <returns>com.mirle.ibg3k0.stc.Common.SECS.SECSAgent.SECS_STREAM_FUNCTION_CHECK_RESULT.</returns>
        public static com.mirle.ibg3k0.stc.Common.SECS.SECSAgent.SECS_STREAM_FUNCTION_CHECK_RESULT checkDataValue(
            string name, string value)
        {
            com.mirle.ibg3k0.stc.Common.SECS.SECSAgent.SECS_STREAM_FUNCTION_CHECK_RESULT result =
                SECSAgent.SECS_STREAM_FUNCTION_CHECK_RESULT.Recognize;

            //if (name.Trim().Equals("CRST"))
            //{
            //    //SECSConst.CRST
            //    if (!SECSConst.CRST.Contains(value.Trim()))
            //    {
            //        return SECSAgent.SECS_STREAM_FUNCTION_CHECK_RESULT.Illegal_Data_Value_Format;
            //    }
            //}

            return result;
        }

        /// <summary>
        /// The stream identifier array
        /// </summary>
        public static readonly int[] StreamIDArray = { 1, 2, 5, 6, 7, 9, 10 };
        /// <summary>
        /// The function identifier array
        /// </summary>
        public static readonly int[] FunctionIDArray =
        {
            0, 1, 2, 3, 4, 5, 6, 7, 9,
            11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
            23, 24, 25, 26, 29,
            30, 31, 32, 33, 34, 35, 36, 37, 38,
            41, 42, 49, 50,
            53, 54 ,
            103, 104, 105, 106, 107, 108, 109, 110, 110, 112,
            203, 204
        };

        public static Dictionary<string, List<string>> DicCEIDAndRPTID { get; private set; }
        public static Dictionary<string, List<ARPTID>> DicRPTIDAndVID { get; private set; }

        public static void setDicCEIDAndRPTID(Dictionary<string, List<string>> _dic)
        {
            DicCEIDAndRPTID = _dic;
        }
        public static void setDicRPTIDAndVID(Dictionary<string, List<ARPTID>> _dic)
        {
            DicRPTIDAndVID = _dic;
        }
        /// <summary>
        /// Checks the type of the sf.
        /// </summary>
        /// <param name="S">The s.</param>
        /// <param name="F">The f.</param>
        /// <returns>com.mirle.ibg3k0.stc.Common.SECS.SECSAgent.SECS_STREAM_FUNCTION_CHECK_RESULT.</returns>
        public static com.mirle.ibg3k0.stc.Common.SECS.SECSAgent.SECS_STREAM_FUNCTION_CHECK_RESULT checkSFType(int S, int F)
        {
            com.mirle.ibg3k0.stc.Common.SECS.SECSAgent.SECS_STREAM_FUNCTION_CHECK_RESULT result =
                SECSAgent.SECS_STREAM_FUNCTION_CHECK_RESULT.Recognize;
            string streamFunction = string.Format("S{0}F{1}", S, F);

            if (!StreamIDArray.Contains(S))
            {
                result = SECSAgent.SECS_STREAM_FUNCTION_CHECK_RESULT.Unrecognized_Stream_Type;
            }
            else if (!FunctionIDArray.Contains(F))
            {
                result = SECSAgent.SECS_STREAM_FUNCTION_CHECK_RESULT.Unrecognized_Function_Type;
            }
            else
            {
                Type type = Type.GetType("com.mirle.ibg3k0.sc.Data.SECS." + streamFunction);
                Type typeBase = Type.GetType("com.mirle.ibg3k0.stc.Data.SecsData." + streamFunction);
                if (type == null && typeBase == null && F != 0)
                {
                    result = SECSAgent.SECS_STREAM_FUNCTION_CHECK_RESULT.Unrecognized_Stream_Type;
                }
            }
            return result;
        }



    }
}
