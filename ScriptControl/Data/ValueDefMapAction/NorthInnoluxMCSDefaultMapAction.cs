//*********************************************************************************
//      MESDefaultMapAction.cs
//*********************************************************************************
// File Name: MESDefaultMapAction.cs
// Description: 與EAP通訊的劇本
//
//(c) Copyright 2014, MIRLE Automation Corporation
//
// Date          Author         Request No.    Tag     Description
// ------------- -------------  -------------  ------  -----------------------------
// 2019/07/16    Mark Chou      N/A            M0.01   修正回覆S1F4 SVID305會發生Exception的問題
// 2019/08/26    Kevin Wei      N/A            M0.02   修正原本在只要有From、To命令還是在Wating的狀態時，
//                                                     此時MCS若下達一筆命令則會拒絕，改成只要是From相同，就會拒絕。
// 2020/10/05    Mark Chou      N/A            M0.03   北群創收到MCS S2F49不檢查是否有相同的Load Port命令
//**********************************************************************************
using com.mirle.ibg3k0.bcf.App;
using com.mirle.ibg3k0.bcf.Controller;
using com.mirle.ibg3k0.bcf.Data.TimerAction;
using com.mirle.ibg3k0.bcf.Data.ValueDefMapAction;
using com.mirle.ibg3k0.bcf.Data.VO;
using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.BLL;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux;
using com.mirle.ibg3k0.sc.Data.SECSDriver;
using com.mirle.ibg3k0.sc.Data.VO;
using com.mirle.ibg3k0.stc.Common;
using com.mirle.ibg3k0.stc.Data.SecsData;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Transactions;

namespace com.mirle.ibg3k0.sc.Data.ValueDefMapAction
{
    public class NorthInnoluxMCSDefaultMapAction : IBSEMDriver, IValueDefMapAction
    {
        const string DEVICE_NAME_MCS = "MCS";
        const string CALL_CONTEXT_KEY_WORD_SERVICE_ID_MCS = "MCS Service";

        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static Logger GlassTrnLogger = LogManager.GetLogger("GlassTransferRpt_EAP");
        protected static Logger logger_MapActionLog = LogManager.GetLogger("MapActioLog");
        private ReportBLL reportBLL = null;


        public virtual string getIdentityKey()
        {
            return this.GetType().Name;
        }

        public virtual void setContext(BaseEQObject baseEQ)
        {
            this.line = baseEQ as ALINE;
        }
        public virtual void unRegisterEvent()
        {

        }
        public virtual void doShareMemoryInit(BCFAppConstants.RUN_LEVEL runLevel)
        {
            try
            {
                switch (runLevel)
                {
                    case BCFAppConstants.RUN_LEVEL.ZERO:
                        List<ALARMRPTCOND> aLARMRPTCONDs = scApp.AlarmBLL.loadAllAlarmReport();
                        if (aLARMRPTCONDs == null|| aLARMRPTCONDs.Count == 0)
                        {
                            scApp.AlarmBLL.enableAllAlarmReport(true);
                        }
                        List<AEVENTRPTCOND> aEVENTRPTCONDs = scApp.EventBLL.LoadAllCEID();
                        if (aEVENTRPTCONDs == null || aEVENTRPTCONDs.Count == 0)
                        {
                            scApp.EventBLL.enableAllEventReport(true);
                        }
                        SECSConst.setDicCEIDAndRPTID(scApp.CEIDBLL.loadDicCEIDAndRPTID());
                        SECSConst.setDicRPTIDAndVID(scApp.CEIDBLL.loadDicRPTIDAndVID());
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
        #region Receive 
        protected override void S1F1ReceiveAreYouThere(object sender, SECSEventArgs e)
        {
            try
            {
                S1F1 s1f1 = ((S1F1)e.secsHandler.Parse<S1F1>(e));
                SCUtility.secsActionRecordMsg(scApp, true, s1f1);
                SCUtility.actionRecordMsg(scApp, s1f1.StreamFunction, line.Real_ID,
                        "Receive Are You There From MES.", "");
                if (!isProcess(s1f1)) { return; }

                S1F2 s1f2 = new S1F2()
                {
                    SECSAgentName = scApp.EAPSecsAgentName,
                    SystemByte = s1f1.SystemByte,
                    MDLN = "AGVC",
                    SOFTREV = "11.02"
                };
                SCUtility.secsActionRecordMsg(scApp, false, s1f2);
                TrxSECS.ReturnCode rtnCode = ISECSControl.replySECS(bcfApp, s1f2);
                SCUtility.actionRecordMsg(scApp, s1f1.StreamFunction, line.Real_ID,
                        "Reply Are You There To MES.", rtnCode.ToString());
                if (rtnCode != TrxSECS.ReturnCode.Normal)
                {
                    logger.Warn("Reply EAP S1F2 Error:{0}", rtnCode);
                }
                line.CommunicationIntervalWithMCS.Restart();
            }
            catch (Exception ex)
            {
                logger.Error("MESDefaultMapAction has Error[Line Name:{0}],[Error method:{1}],[Error Message:{2}",
                    line.LINE_ID, "S1F1_Receive_AreYouThere", ex.ToString());
            }
        }



        protected override void S1F3ReceiveSelectedEquipmentStatusRequest(object sender, SECSEventArgs e)
        {
            try
            {
                S1F3 s1f3 = ((S1F3)e.secsHandler.Parse<S1F3>(e));
                SCUtility.secsActionRecordMsg(scApp, true, s1f3);
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                              Data: s1f3);

                int count = s1f3.SVID.Count();
                S1F4 s1f4 = new S1F4();
                s1f4.SECSAgentName = scApp.EAPSecsAgentName;
                s1f4.SystemByte = s1f3.SystemByte;
                s1f4.SV = new SXFY[count];
                for (int i = 0; i < count; i++)
                {
                    if (s1f3.SVID[i] == SECSConst.VID_ActiveCarriers)
                    {
                        //line.EnhancedVehiclesChecked = true;
                        s1f4.SV[i] = buildActiveCarriersVIDItem();
                    }
                    else if (s1f3.SVID[i] == SECSConst.VID_ActiveTransfers)
                    {
                        //line.EnhancedVehiclesChecked = true;
                        s1f4.SV[i] = buildActiveTransfersVIDItem();
                    }
                    else if (s1f3.SVID[i] == SECSConst.VID_ActiveVehicles)
                    {
                        line.EnhancedVehiclesChecked = true;
                        s1f4.SV[i] = buildActiveVehiclesVIDItem();
                    }
                    else if (s1f3.SVID[i] == SECSConst.VID_AlarmEnabled)
                    {
                        //line.CurrentPortStateChecked = true;
                        s1f4.SV[i] = buildEnabledAlarmVIDItem();
                    }
                    else if (s1f3.SVID[i] == SECSConst.VID_AlarmSet)
                    {
                        //line.CurrentPortStateChecked = true;
                        s1f4.SV[i] = buildSettedAlarmVIDItem();
                    }
                    else if (s1f3.SVID[i] == SECSConst.VID_Clock)
                    {
                        //line.CurrentPortStateChecked = true;
                        s1f4.SV[i] = buildClockVIDItem();
                    }
                    


                    else if (s1f3.SVID[i] == SECSConst.VID_ControlState)
                    {
                        line.CurrentStateChecked = true;
                        s1f4.SV[i] = buildControlStateVIDItem();
                    }

                    else if (s1f3.SVID[i] == SECSConst.VID_CurrentPortStatus)
                    {
                        //line.CurrentPortStateChecked = true;
                        s1f4.SV[i] = buildCurrentPortStatusVIDItem();
                    }
                    else if (s1f3.SVID[i] == SECSConst.VID_CurrentLoadStatus)
                    {
                        //line.CurrentPortStateChecked = true;
                        s1f4.SV[i] = buildCurrentLoadStatusVIDItem();
                    }

                    //else if (s1f3.SVID[i] == SECSConst.VID_IOInfo)
                    //{
                    //    //line.CurrentPortStateChecked = true;
                    //    s1f4.SV[i] = buildCurrentLoadStatusVIDItem();
                    //}
                    else if (s1f3.SVID[i] == SECSConst.VID_EventEnabled)
                    {
                        //line.CurrentPortStateChecked = true;
                        s1f4.SV[i] = buildEnabledEventVIDItem();
                    }
                    else if (s1f3.SVID[i] == SECSConst.VID_EventEnabled)
                    {
                        //line.CurrentPortStateChecked = true;
                        s1f4.SV[i] = buildEnabledEventVIDItem();
                    }
                    else if (s1f3.SVID[i] == SECSConst.VID_PriviousControlState)
                    {
                        //line.CurrentPortStateChecked = true;
                        s1f4.SV[i] = buildPriviousControlStateVIDItem();
                    }
                    else if (s1f3.SVID[i] == SECSConst.VID_PriviousTSCState)
                    {
                        //line.CurrentPortStateChecked = true;
                        s1f4.SV[i] = buildPriviousTSCStateVIDItem();
                    }
                    else if (s1f3.SVID[i] == SECSConst.VID_TSAvail)
                    {
                        //line.CurrentPortStateChecked = true;
                        s1f4.SV[i] = buildTCSAvailItem();
                    }
                    else if (s1f3.SVID[i] == SECSConst.VID_TSCState)
                    {
                        line.TSCStateChecked = true;
                        s1f4.SV[i] = buildTCSStateVIDItem();
                    }
                    else if (s1f3.SVID[i] == SECSConst.VID_TSAvailability)
                    {
                        //line.TSCStateChecked = true;
                        s1f4.SV[i] = buildTCSStateAvailabilityVIDItem();
                    }
                    //else if (s1f3.SVID[i] == SECSConst.VID_Enhanced_Carriers)
                    //{
                    //    line.EnhancedCarriersChecked = true;
                    //    s1f4.SV[i] = buildEnhancedCarriersVIDItem();
                    //}
                    //else if (s1f3.SVID[i] == SECSConst.VID_Enhanced_Transfers)
                    //{
                    //    line.EnhancedTransfersChecked = true;
                    //    s1f4.SV[i] = buildEnhancedTransfersVIDItem();
                    //}


                    else
                    {
                        s1f4.SV[i] = new SXFY();
                    }
                }
                TrxSECS.ReturnCode rtnCode = ISECSControl.replySECS(bcfApp, s1f4);
                SCUtility.secsActionRecordMsg(scApp, false, s1f4);
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                              Data: s1f4);
            }
            catch (Exception ex)
            {
                logger.Error("AUOMCSDefaultMapAction has Error[Line Name:{0}],[Error method:{1}],[Error Message:{2}",
                    line.LINE_ID, "S1F3_Receive_Eqpt_Stat_Req", ex.ToString());
            }
        }
        #region Build VIDItem
        private S6F11.RPTINFO.RPTITEM.VIDITEM_06 buildControlStateVIDItem()
        {
            string control_state = SCAppConstants.LineHostControlState.convert2MES(line.Host_Control_State);
            S6F11.RPTINFO.RPTITEM.VIDITEM_06 viditem_06 = new S6F11.RPTINFO.RPTITEM.VIDITEM_06()
            {
                CONTROLSTATE = control_state
            };
            return viditem_06;
        }
        private S6F11.RPTINFO.RPTITEM.VIDITEM_51 buildActiveCarriersVIDItem()
        {
            List<AVEHICLE> has_carry_vhs = scApp.getEQObjCacheManager().getAllVehicle().Where(vh => vh.HAS_CST == 1).ToList();
            int carry_vhs_count = has_carry_vhs.Count;
            S6F11.RPTINFO.RPTITEM.VIDITEM_51 viditem_51 = new S6F11.RPTINFO.RPTITEM.VIDITEM_51();
            viditem_51.CARRIER_INFO = new S6F11.RPTINFO.RPTITEM.VIDITEM_55[carry_vhs_count];
            for (int j = 0; j < carry_vhs_count; j++)
            {
                viditem_51.CARRIER_INFO[j] = new S6F11.RPTINFO.RPTITEM.VIDITEM_55()
                {
                    CARRIER_ID = has_carry_vhs[j].CST_ID.Trim(),
                    VEHICLE_ID = has_carry_vhs[j].VEHICLE_ID.Trim(),
                    CARRIER_LOC = has_carry_vhs[j].VEHICLE_ID.Trim()+ "-01"//北群創的CARRIER_LOC一律都在車上的Crane
                };
            }
            return viditem_51;
        }

        public override bool S6F11SendTSAvailChanged()
        {
            try
            {
                VIDCollection Vids = new VIDCollection();
                Vids.VID_61_EqpName.EQP_NAME = line.LINE_ID;
                Vids.VID_201_TSAvail.TS_AVAIL = ((int)line.SCStats).ToString() == "2" ? "0" : "1";
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_TS_Avail_Changed, Vids);
                scApp.ReportBLL.insertMCSReport(mcs_queue);
                S6F11SendMessage(mcs_queue);
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
            }
            return true;
        }
        private S6F11.RPTINFO.RPTITEM.VIDITEM_52 buildActiveTransfersVIDItem()
        {
            List<ACMD_MCS> mcs_cmds = scApp.CMDBLL.loadACMD_MCSIsUnfinished();
            int cmd_count = mcs_cmds.Count;



            S6F11.RPTINFO.RPTITEM.VIDITEM_52 viditem_52 = new S6F11.RPTINFO.RPTITEM.VIDITEM_52();
            viditem_52.TRANSFER_COMMANDs = new S6F11.RPTINFO.RPTITEM.VIDITEM_66[cmd_count];
            for (int j = 0; j < cmd_count; j++)
            {
                viditem_52.TRANSFER_COMMANDs[j] = new S6F11.RPTINFO.RPTITEM.VIDITEM_66()
                {
                    COMMAND_INFO = new S6F11.RPTINFO.RPTITEM.VIDITEM_59()
                    {
                        COMMAND_ID = mcs_cmds[j].CMD_ID,
                        PRIORITY = mcs_cmds[j].PRIORITY.ToString(),
                        REPLACE = "0"
                    },
                    TRANSFER_INFOs = new S6F11.RPTINFO.RPTITEM.VIDITEM_67[1]
                };
                viditem_52.TRANSFER_COMMANDs[j].TRANSFER_INFOs[0] = new S6F11.RPTINFO.RPTITEM.VIDITEM_67()
                {
                    CARRIER_INFOs = new S6F11.RPTINFO.RPTITEM.VIDITEM_67.CARRIER_INFO[1]
                };
                viditem_52.TRANSFER_COMMANDs[j].TRANSFER_INFOs[0].CARRIER_INFOs[0] = new S6F11.RPTINFO.RPTITEM.VIDITEM_67.CARRIER_INFO()
                {
                    CARRIER_ID = new S6F11.RPTINFO.RPTITEM.VIDITEM_54()
                    {
                        CARRIER_ID = mcs_cmds[j].CARRIER_ID
                    },
                    SOURCE_PORT = new S6F11.RPTINFO.RPTITEM.VIDITEM_65()
                    {
                        SOURCE_PORT = mcs_cmds[j].HOSTSOURCE
                    },
                    DEST_PORT = new S6F11.RPTINFO.RPTITEM.VIDITEM_60()
                    {
                        DEST_PORT = mcs_cmds[j].HOSTDESTINATION
                    }
                };
            }
            return viditem_52;
        }
        private S6F11.RPTINFO.RPTITEM.VIDITEM_53 buildActiveVehiclesVIDItem()
        {
            List<AVEHICLE> vhs = scApp.getEQObjCacheManager().getAllVehicle();
            int vhs_count = vhs.Count;
            S6F11.RPTINFO.RPTITEM.VIDITEM_53 viditem_53 = new S6F11.RPTINFO.RPTITEM.VIDITEM_53();
            viditem_53.VEHICLEINFO = new S6F11.RPTINFO.RPTITEM.VIDITEM_71[vhs_count];
            for (int j = 0; j < vhs_count; j++)
            {
                viditem_53.VEHICLEINFO[j] = new S6F11.RPTINFO.RPTITEM.VIDITEM_71()
                {
                    VEHICLE_ID  = vhs[j].Real_ID,
                    VEHICLE_STATE = ((int)vhs[j].State).ToString()
                };
            }
            return viditem_53;
        }

        private S6F11.RPTINFO.RPTITEM.VIDITEM_03 buildEnabledAlarmVIDItem()
        {
            List<ALARMRPTCOND> aLARMRPTCONDs = scApp.AlarmBLL.loadAllAlarmReport(true);
            S6F11.RPTINFO.RPTITEM.VIDITEM_03 viditem_03 = new S6F11.RPTINFO.RPTITEM.VIDITEM_03();
            viditem_03.ALIDs = new string[aLARMRPTCONDs.Count];
            int index = -1;
            foreach (ALARMRPTCOND aLARMRPTCOND in aLARMRPTCONDs)
            {
                index++;
                viditem_03.ALIDs[index] = aLARMRPTCOND.ALAM_CODE;
            }
            return viditem_03;
        }

        private S6F11.RPTINFO.RPTITEM.VIDITEM_04 buildSettedAlarmVIDItem()
        {
            List<ALARM> alarms = scApp.AlarmBLL.getCurrentAlarms();
            S6F11.RPTINFO.RPTITEM.VIDITEM_04 viditem_04 = new S6F11.RPTINFO.RPTITEM.VIDITEM_04();
            viditem_04.ALIDs = new string[alarms.Count];
            int index = -1;
            foreach (ALARM alarm in alarms)
            {
                index++;
                viditem_04.ALIDs[index] = alarm.ALAM_CODE;
            }
            return viditem_04;
        }
        private S6F11.RPTINFO.RPTITEM.VIDITEM_05 buildClockVIDItem()
        {
            //string sc_state = SCAppConstants.LineSCState.convert2MES(line.SCStats);
            string time = DateTime.Now.ToString(SCAppConstants.TimestampFormat_14).PadRight(16, '0');
            S6F11.RPTINFO.RPTITEM.VIDITEM_05 viditem_5 = new S6F11.RPTINFO.RPTITEM.VIDITEM_05()
            {
                CLOCK = time
            };

            return viditem_5;
        }

        private S6F11.RPTINFO.RPTITEM.VIDITEM_07 buildEnabledEventVIDItem()
        {
            List<AEVENTRPTCOND> CEIDs =  scApp.EventBLL.LoadEnabledCEID();
            S6F11.RPTINFO.RPTITEM.VIDITEM_07 viditem_07 = new S6F11.RPTINFO.RPTITEM.VIDITEM_07();
            viditem_07.CEIDs = new string[CEIDs.Count];
            int index = -1;
            foreach(AEVENTRPTCOND ceid in CEIDs)
            {
                index++;
                viditem_07.CEIDs[index] = ceid.CEID;
            }
            return viditem_07;
        }                                                                                               
        private S6F11.RPTINFO.RPTITEM.VIDITEM_1210 buildPriviousControlStateVIDItem()
        {
            string control_state = SCAppConstants.LineHostControlState.convert2MES(line.Privious_Host_Control_State);
            S6F11.RPTINFO.RPTITEM.VIDITEM_1210 viditem_1210 = new S6F11.RPTINFO.RPTITEM.VIDITEM_1210();
            viditem_1210.PREVIOUS_CONTROL_STATE = control_state;
            return viditem_1210;
        }
        private S6F11.RPTINFO.RPTITEM.VIDITEM_1240 buildPriviousTSCStateVIDItem()
        {
            string tsc_state = ((int)line.PriviousSCStats).ToString();
            S6F11.RPTINFO.RPTITEM.VIDITEM_1240 viditem_1240 = new S6F11.RPTINFO.RPTITEM.VIDITEM_1240();
            viditem_1240.PREVIOUS_TSC_STATE = tsc_state;
            return viditem_1240;
        }
        private S6F11.RPTINFO.RPTITEM.VIDITEM_73 buildTCSStateVIDItem()
        {
            string tsc_state = ((int)line.TSC_state_machine.State).ToString();
            S6F11.RPTINFO.RPTITEM.VIDITEM_73 viditem_73 = new S6F11.RPTINFO.RPTITEM.VIDITEM_73()
            {
                TSC_STATE = tsc_state
            };

            return viditem_73;
        }
        private S6F11.RPTINFO.RPTITEM.VIDITEM_1230 buildTCSStateAvailabilityVIDItem()
        {
            string alarmhappen = !line.IsAlarmHappened ? "0" : "1";
            S6F11.RPTINFO.RPTITEM.VIDITEM_1230 viditem_1230 = new S6F11.RPTINFO.RPTITEM.VIDITEM_1230()
            {
                TSC_STATE_AVAILABILITY = alarmhappen
            };
            return viditem_1230;
        }
        private S6F11.RPTINFO.RPTITEM.VIDITEM_201 buildTCSAvailItem()
        {
            List<AVEHICLE> vhs = scApp.getEQObjCacheManager().getAllVehicle();
            bool isAvail = false;
            foreach(AVEHICLE vh in vhs)
            {
                if(vh.IS_INSTALLED&&(vh.MODE_STATUS == ProtocolFormat.OHTMessage.VHModeStatus.AutoRemote|| vh.MODE_STATUS == ProtocolFormat.OHTMessage.VHModeStatus.AutoLocal
                    ||vh.MODE_STATUS == ProtocolFormat.OHTMessage.VHModeStatus.AutoCharging) && !vh.IsError)
                {
                    isAvail = true;
                    break;
                }
            }
            S6F11.RPTINFO.RPTITEM.VIDITEM_201 viditem_201 = new S6F11.RPTINFO.RPTITEM.VIDITEM_201();
            viditem_201.TS_AVAIL = isAvail ? "0" : "1";
            return viditem_201;
        }
        //private S6F11.RPTINFO.RPTITEM.VIDITEM_62 buildEnhancedCarriersVIDItem()
        //{
        //    List<AVEHICLE> has_carry_vhs = scApp.getEQObjCacheManager().getAllVehicle().Where(vh => vh.HAS_CST == 1).ToList();
        //    int carry_vhs_count = has_carry_vhs.Count;
        //    S6F11.RPTINFO.RPTITEM.VIDITEM_62 viditem_62 = new S6F11.RPTINFO.RPTITEM.VIDITEM_62();
        //    viditem_62.ENHANCED_CARRIER_INFOs = new S6F11.RPTINFO.RPTITEM.VIDITEM_62.ENHANCED_CARRIER_INFO[carry_vhs_count];

        //    for (int k = 0; k < carry_vhs_count; k++)
        //    {
        //        AVEHICLE has_carray_vh = has_carry_vhs[k];
        //        AVIDINFO vid_info = scApp.VIDBLL.getVIDInfo(has_carray_vh.VEHICLE_ID);
        //        viditem_62.ENHANCED_CARRIER_INFOs[k] = new S6F11.RPTINFO.RPTITEM.VIDITEM_62.ENHANCED_CARRIER_INFO();
        //        viditem_62.ENHANCED_CARRIER_INFOs[k].CARRIER_ID_OBJ = new S6F11.RPTINFO.RPTITEM.VIDITEM_54()
        //        {
        //            CARRIER_ID = vid_info.CARRIER_ID.Trim()
        //        };
        //        viditem_62.ENHANCED_CARRIER_INFOs[k].VEHICLE_ID_OBJ = new S6F11.RPTINFO.RPTITEM.VIDITEM_74()
        //        {
        //            VEHICLE_ID = has_carray_vh.Real_ID
        //        };
        //        viditem_62.ENHANCED_CARRIER_INFOs[k].CARRIER_LOC_OBJ = new S6F11.RPTINFO.RPTITEM.VIDITEM_56()
        //        {
        //            CARRIER_LOC = vid_info.CARRIER_LOC
        //        };
        //        viditem_62.ENHANCED_CARRIER_INFOs[k].INSTALL_TIME_OBJ = new S6F11.RPTINFO.RPTITEM.VIDITEM_64_2()
        //        {
        //            INSTALL_TIME = vid_info.CARRIER_INSTALLED_TIME?.ToString(SCAppConstants.TimestampFormat_16)
        //        };
        //    }
        //    return viditem_62;
        //}

        //private S6F11.RPTINFO.RPTITEM.VIDITEM_63 buildEnhancedTransfersVIDItem()
        //{
        //    List<ACMD_MCS> mcs_cmds = scApp.CMDBLL.loadACMD_MCSIsUnfinished();
        //    int cmd_count = mcs_cmds.Count;
        //    S6F11.RPTINFO.RPTITEM.VIDITEM_63 viditem_63 = new S6F11.RPTINFO.RPTITEM.VIDITEM_63();
        //    viditem_63.ENHANCED_CARRIER_INFOs = new S6F11.RPTINFO.RPTITEM.VIDITEM_63.ENHANCED_TRANSFER_COMMAND[cmd_count];

        //    for (int k = 0; k < cmd_count; k++)
        //    {
        //        ACMD_MCS mcs_cmd = mcs_cmds[k];
        //        string transfer_state = SCAppConstants.TransferState.convert2MES(mcs_cmd.TRANSFERSTATE);
        //        viditem_63.ENHANCED_CARRIER_INFOs[k] = new S6F11.RPTINFO.RPTITEM.VIDITEM_63.ENHANCED_TRANSFER_COMMAND();
        //        viditem_63.ENHANCED_CARRIER_INFOs[k].COMMAND_INFO_OBJ = new S6F11.RPTINFO.RPTITEM.VIDITEM_59()
        //        {
        //            COMMAND_ID = mcs_cmd.CMD_ID,
        //            PRIORITY = mcs_cmd.PRIORITY.ToString(),
        //            REPLACE = mcs_cmd.REPLACE.ToString()//不知道Replace是什麼 , For Kevin Wei to Confirm
        //        };
        //        viditem_63.ENHANCED_CARRIER_INFOs[k].TRANSFER_STATE = new S6F11.RPTINFO.RPTITEM.VIDITEM_72_2()
        //        {
        //            TRANSFER_STATE = transfer_state
        //        };
        //        viditem_63.ENHANCED_CARRIER_INFOs[k].TRANSFER_INFOS = new S6F11.RPTINFO.RPTITEM.VIDITEM_70[1];//每一個TransferCMD只會有單一的Transfer Info嗎?  For Kevin Wei to Confirm
        //        viditem_63.ENHANCED_CARRIER_INFOs[k].TRANSFER_INFOS[0] = new S6F11.RPTINFO.RPTITEM.VIDITEM_70();
        //        viditem_63.ENHANCED_CARRIER_INFOs[k].TRANSFER_INFOS[0].CARRIER_ID = new S6F11.RPTINFO.RPTITEM.VIDITEM_54
        //        {
        //            CARRIER_ID = mcs_cmd.CARRIER_ID
        //        };
        //        viditem_63.ENHANCED_CARRIER_INFOs[k].TRANSFER_INFOS[0].SOURCE_PORT = new S6F11.RPTINFO.RPTITEM.VIDITEM_68
        //        {
        //            SOURCE_PORT = mcs_cmd.HOSTSOURCE
        //        };
        //        viditem_63.ENHANCED_CARRIER_INFOs[k].TRANSFER_INFOS[0].DEST_PORT = new S6F11.RPTINFO.RPTITEM.VIDITEM_61
        //        {
        //            DEST_PORT = mcs_cmd.HOSTDESTINATION
        //        };
        //    }
        //    return viditem_63;
        //}



        private S6F11.RPTINFO.RPTITEM.VIDITEM_74 buildCurrentPortStatusVIDItem()
        {
            //List<APORTSTATION> port_station = scApp.MapBLL.loadAllPort();
            List<APORTSTATION> port_station = scApp.getEQObjCacheManager().getAllPortStation();
            int port_count = port_station.Count;
            int eq_port_count = 0;
            for (int j = 0; j < port_count; j++)
            {
                AEQPT eqpt = scApp.getEQObjCacheManager().getEquipmentByEQPTID(port_station[j].EQPT_ID);
                if (eqpt.Type != SCAppConstants.EqptType.Equipment) continue;
                eq_port_count++;
            }
            S6F11.RPTINFO.RPTITEM.VIDITEM_74 viditem_74 = new S6F11.RPTINFO.RPTITEM.VIDITEM_74();
            viditem_74.PORT_INFO = new S6F11.RPTINFO.RPTITEM.VIDITEM_313[eq_port_count];
            int eq_port_index = -1;//M0.01
            for (int j = 0; j < port_count; j++)
            {
                AEQPT eqpt = scApp.getEQObjCacheManager().getEquipmentByEQPTID(port_station[j].EQPT_ID);
                if (eqpt.Type != SCAppConstants.EqptType.Equipment) continue;
                eq_port_index++;//M0.01
                viditem_74.PORT_INFO[eq_port_index] = new S6F11.RPTINFO.RPTITEM.VIDITEM_313();
                viditem_74.PORT_INFO[eq_port_index].PORT_ID = port_station[j].PORT_ID;
                string portTransferState = ((int)port_station[j].PORT_SERVICE_STATUS).ToString();
                if(portTransferState == "0")
                {
                    portTransferState = "1";
                }
                viditem_74.PORT_INFO[eq_port_index].PORT_TRANSFER_STATE = portTransferState;
            }
            return viditem_74;
        }

        private S6F11.RPTINFO.RPTITEM.VIDITEM_75 buildCurrentLoadStatusVIDItem()
        {
            //List<APORTSTATION> port_station = scApp.MapBLL.loadAllPort();
            List<APORTSTATION> port_station = scApp.getEQObjCacheManager().getAllPortStation();
            int port_count = port_station.Count;
            int eq_port_count = 0;
            for (int j = 0; j < port_count; j++)
            {
                AEQPT eqpt = scApp.getEQObjCacheManager().getEquipmentByEQPTID(port_station[j].EQPT_ID);
                if (eqpt.Type != SCAppConstants.EqptType.Equipment) continue;
                eq_port_count++;
            }
            S6F11.RPTINFO.RPTITEM.VIDITEM_75 viditem_75 = new S6F11.RPTINFO.RPTITEM.VIDITEM_75();
            viditem_75.PORT_LOAD_INFO = new S6F11.RPTINFO.RPTITEM.VIDITEM_315[eq_port_count];
            int eq_port_index = -1;//M0.01
            for (int j = 0; j < port_count; j++)
            {
                AEQPT eqpt = scApp.getEQObjCacheManager().getEquipmentByEQPTID(port_station[j].EQPT_ID);
                if (eqpt.Type != SCAppConstants.EqptType.Equipment) continue;
                eq_port_index++;//M0.01
                viditem_75.PORT_LOAD_INFO[eq_port_index] = new S6F11.RPTINFO.RPTITEM.VIDITEM_315();
                viditem_75.PORT_LOAD_INFO[eq_port_index].PORT_ID = port_station[j].PORT_ID;
                string portLoadState = SECSConst.NorthInnoluxPortLoadStatus.portLoadStatusConvert(port_station[j].PORT_STATUS);
                viditem_75.PORT_LOAD_INFO[eq_port_index].PORT_LOAD_STATE = portLoadState;
            }
            return viditem_75;
        }

        #endregion Build VIDItem
        protected override void S1F13ReceiveEstablishCommunicationRequest(object sender, SECSEventArgs e)
        {
            try
            {
                S1F13_Empty s1f13 = ((S1F13_Empty)e.secsHandler.Parse<S1F13_Empty>(e));
                SCUtility.secsActionRecordMsg(scApp, true, s1f13);
                SCUtility.actionRecordMsg(scApp, s1f13.StreamFunction, line.Real_ID,
                        "Receive Establish Communication From MES.", "");
                //if (!isProcessEAP(s1f13)) { return; }
                S1F14 s1f14 = new S1F14();
                s1f14.SECSAgentName = scApp.EAPSecsAgentName;
                s1f14.SystemByte = s1f13.SystemByte;
                s1f14.COMMACK = "0";
                s1f14.VERSION_INFO = new string[2]
                { "AGVC",
                  "11.02" };

                SCUtility.secsActionRecordMsg(scApp, false, s1f14);
                TrxSECS.ReturnCode rtnCode = ISECSControl.replySECS(bcfApp, s1f14);
                SCUtility.actionRecordMsg(scApp, s1f13.StreamFunction, line.Real_ID,
                        "Reply Establish Communication To MES.", rtnCode.ToString());
                if (rtnCode != TrxSECS.ReturnCode.Normal)
                {
                    logger.Warn("Reply EAP S1F14 Error:{0}", rtnCode);
                }
                logger.Debug("s1f13Receive ok!");
                line.EstablishComm = true;
            }
            catch (Exception ex)
            {
                logger.Error("MESDefaultMapAction has Error[Line Name:{0}],[Error method:{1}],[Error Message:{2}",
                    line.LINE_ID, "s1f13_Receive_EstablishCommunication", ex.ToString());
            }
        }
        protected virtual void S1F15ReceiveRequestOffLine(object sender, SECSEventArgs e)
        {
            try
            {
                string msg = string.Empty;
                S1F15 s1f15 = ((S1F15)e.secsHandler.Parse<S1F15>(e));
                SCUtility.secsActionRecordMsg(scApp, true, s1f15);

                if (!isProcess(s1f15)) { return; }

                S1F16 s1f16 = new S1F16();
                s1f16.SystemByte = s1f15.SystemByte;
                s1f16.SECSAgentName = scApp.EAPSecsAgentName;

                if (line.Host_Control_State == SCAppConstants.LineHostControlState.HostControlState.Host_Offline)
                {
                    S1F0 sxf0 = new S1F0()
                    {
                        SECSAgentName = scApp.EAPSecsAgentName,
                        StreamFunction = s1f15.getAbortFunctionName(),
                        SystemByte = s1f15.SystemByte
                    };
                    SCUtility.secsActionRecordMsg(scApp, false, sxf0);
                    TrxSECS.ReturnCode _rtnCode = ISECSControl.replySECS(bcfApp, sxf0);
                    SCUtility.actionRecordMsg(scApp, sxf0.StreamFunction, line.Real_ID,
                                "Reply Abort To MES.", _rtnCode.ToString());
                    return;
                }
                //檢查狀態是否允許連線
                else
                {
                    s1f16.OFLACK = SECSConst.OFLACK_Accepted;
                }

                TrxSECS.ReturnCode rtnCode = ISECSControl.replySECS(bcfApp, s1f16);
                SCUtility.secsActionRecordMsg(scApp, false, s1f16);
                if (rtnCode != TrxSECS.ReturnCode.Normal)
                {
                    logger.Warn("Reply EQPT S1F18 Error:{0}", rtnCode);
                }

                if (SCUtility.isMatche(s1f16.OFLACK, SECSConst.OFLACK_Accepted))
                {
                    if (DebugParameter.UseHostOffline)
                    {
                        line.Host_Control_State = SCAppConstants.LineHostControlState.HostControlState.Host_Offline;
                        S6F11SendEquiptmentOffLine();
                    }
                    else
                    {
                        line.Host_Control_State = SCAppConstants.LineHostControlState.HostControlState.EQ_Off_line;
                        S6F11SendEquiptmentOffLine();
                    }
                }
                else
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                logger.Error("MESDefaultMapAction has Error[Line Name:{0}],[Error method:{1}],[Error Message:{2}", line.LINE_ID, "S1F15ReceiveRequestOffLine", ex.ToString());
            }
        }
        protected override Boolean isProcess(SXFY sxfy)
        {
            Boolean isProcess = false;
            string streamFunction = sxfy.StreamFunction;
            if (line.Host_Control_State == SCAppConstants.LineHostControlState.HostControlState.EQ_Off_line)
            {
                if (sxfy is S1F17)
                {
                    isProcess = true;
                }
                else if (sxfy is S2F41)
                {
                    string rcmd = (sxfy as S2F41).RCMD;
                }
                else
                {
                    isProcess = false;
                }
            }
            else
            {
                isProcess = true;
            }
            if (!isProcess)
            {
                S1F0 sxf0 = new S1F0()
                {
                    SECSAgentName = scApp.EAPSecsAgentName,
                    StreamFunction = sxfy.getAbortFunctionName(),
                    SystemByte = sxfy.SystemByte
                };
                SCUtility.secsActionRecordMsg(scApp, false, sxf0);
                TrxSECS.ReturnCode rtnCode = ISECSControl.replySECS(bcfApp, sxf0);
                SCUtility.actionRecordMsg(scApp, sxf0.StreamFunction, line.Real_ID,
                            "Reply Abort To MES.", rtnCode.ToString());
            }
            return isProcess;
        }
        protected override void S1F17ReceiveRequestOnLine(object sender, SECSEventArgs e)
        {
            try
            {
                string msg = string.Empty;
                S1F17 s1f17 = ((S1F17)e.secsHandler.Parse<S1F17>(e));
                SCUtility.secsActionRecordMsg(scApp, true, s1f17);

                if (!isProcess(s1f17)) { return; }

                S1F18 s1f18 = new S1F18();
                s1f18.SystemByte = s1f17.SystemByte;
                s1f18.SECSAgentName = scApp.EAPSecsAgentName;


                //檢查狀態是否允許連線
                if (line.Host_Control_State == SCAppConstants.LineHostControlState.HostControlState.On_Line_Remote)
                {
                    s1f18.ONLACK = SECSConst.ONLACK_Equipment_Already_On_Line;
                    msg = "OHS is online remote ready!!"; //A0.05
                }
                else if (DebugParameter.UseHostOffline)
                {
                    if(line.Host_Control_State == SCAppConstants.LineHostControlState.HostControlState.Host_Offline)
                    {
                        s1f18.ONLACK = SECSConst.ONLACK_Not_Accepted;
                    }
                    else
                    {
                        s1f18.ONLACK = SECSConst.ONLACK_Accepted;
                    }
                }
                else
                {
                    s1f18.ONLACK = SECSConst.ONLACK_Accepted;
                }

                TrxSECS.ReturnCode rtnCode = ISECSControl.replySECS(bcfApp, s1f18);
                SCUtility.secsActionRecordMsg(scApp, false, s1f18);
                if (rtnCode != TrxSECS.ReturnCode.Normal)
                {
                    logger.Warn("Reply EQPT S1F18 Error:{0}", rtnCode);
                }

                if (SCUtility.isMatche(s1f18.ONLACK, SECSConst.ONLACK_Accepted))
                {
                    line.Host_Control_State = SCAppConstants.LineHostControlState.HostControlState.On_Line_Remote;
                    S6F11SendControlStateRemote();
                }
                else
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                logger.Error("MESDefaultMapAction has Error[Line Name:{0}],[Error method:{1}],[Error Message:{2}", line.LINE_ID, "S1F17_Receive_OnlineRequest", ex.ToString());
            }
        }
        protected override void S2F31ReceiveDateTimeSetReq(object sender, SECSEventArgs e)
        {
            try
            {
                S2F31 s2f31 = ((S2F31)e.secsHandler.Parse<S2F31>(e));

                SCUtility.secsActionRecordMsg(scApp, true, s2f31);
                SCUtility.actionRecordMsg(scApp, s2f31.StreamFunction, line.Real_ID,
                        "Receive Date Time Set Request From MES.", "");
                if (!isProcess(s2f31)) { return; }

                S2F32 s2f32 = new S2F32();
                s2f32.SECSAgentName = scApp.EAPSecsAgentName;
                s2f32.SystemByte = s2f31.SystemByte;
                s2f32.TIACK = SECSConst.TIACK_Accepted;

                string timeStr = s2f31.TIME;
                DateTime mesDateTime = DateTime.Now;
                try
                {
                    mesDateTime = DateTime.ParseExact(timeStr.Trim(), SCAppConstants.TimestampFormat_16, CultureInfo.CurrentCulture);
                }
                catch (Exception dtEx)
                {
                    s2f32.TIACK = SECSConst.TIACK_Error_not_done;
                }

                SCUtility.secsActionRecordMsg(scApp, false, s2f32);
                ISECSControl.replySECS(bcfApp, s2f32);

                if (!DebugParameter.DisableSyncTime)
                {
                    SCUtility.updateSystemTime(mesDateTime);
                }

                //TODO 與設備同步
            }
            catch (Exception ex)
            {
                logger.Error("MESDefaultMapAction has Error[Line Name:{0}],[Error method:{1}],[Error Message:{2}",
                    line.LINE_ID, "S2F31_Receive_Date_Time_Set_Req", ex.ToString());
            }
        }

        protected virtual void S2F17ReceiveDateTimeReq(object sender, SECSEventArgs e)
        {
            try
            {
                S2F17 s2f17 = ((S2F17)e.secsHandler.Parse<S2F17>(e));

                SCUtility.secsActionRecordMsg(scApp, true, s2f17);
                SCUtility.actionRecordMsg(scApp, s2f17.StreamFunction, line.Real_ID,
                        "Receive Date Time Request From MES.", "");
                if (!isProcess(s2f17)) { return; }

                S2F18 s2f18 = new S2F18();
                s2f18.SECSAgentName = scApp.EAPSecsAgentName;
                s2f18.SystemByte = s2f17.SystemByte;
                s2f18.TIME = DateTime.Now.ToString(SCAppConstants.TimestampFormat_14).PadRight(16, '0');

                SCUtility.secsActionRecordMsg(scApp, false, s2f18);
                ISECSControl.replySECS(bcfApp, s2f18);


            }
            catch (Exception ex)
            {
                logger.Error("MESDefaultMapAction has Error[Line Name:{0}],[Error method:{1}],[Error Message:{2}",
                    line.LINE_ID, "S2F17ReceiveDateTimeReq", ex.ToString());
            }
        }
        /// <summary>
        /// 接收從MES送來的Enable / Disable Alarm Report請求。
        /// 會檢查是否與BC目前得知的Alarm ID相符。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void S5F3_Receive_Enable_Disable_Alarm_Rpt_Req(object sender, SECSEventArgs e)
        {
            try
            {
                string ackResult = SECSConst.ACKC5_Accepted;

                S5F3 s5f3 = ((S5F3)e.secsHandler.Parse<S5F3>(e));

                SCUtility.secsActionRecordMsg(scApp, true, s5f3);
                SCUtility.actionRecordMsg(scApp, s5f3.StreamFunction, line.Real_ID,
                        "Receive Enable or Disable Alarm Report Request From MES.", "");
                if (!isProcess(s5f3)) { return; }
                Boolean isEnable = SCUtility.isMatche(s5f3.ALED, SECSConst.ALED_Enable);
                List<AlarmMap> alarmMapList = scApp.AlarmBLL.loadAlarmMaps();
                Boolean hasFoundAlarmID = false;
                string alarm_id = s5f3.ALID;
                if (SCUtility.isEmpty(alarm_id))
                {
                    ackResult = SECSConst.ACKC5_Not_Accepted;
                }
                hasFoundAlarmID = false;
                for (int jx = 0; jx < alarmMapList.Count; ++jx)
                {
                    AlarmMap alarmMap = alarmMapList[jx];
                    if (SCUtility.isMatche(alarmMap.ALARM_ID, alarm_id))
                    {
                        hasFoundAlarmID = true;
                        break;
                    }
                }
                if (!hasFoundAlarmID)
                {
                    ackResult = SECSConst.ACKC5_Not_Accepted;
                }

                if (SCUtility.isMatche(ackResult, SECSConst.ACKC5_Accepted))
                {

                    string _alarm_id = s5f3.ALID;
                    scApp.AlarmBLL.enableAlarmReport(_alarm_id, isEnable);

                }


                S5F4 s5f4 = new S5F4()
                {
                    SECSAgentName = scApp.EAPSecsAgentName,
                    SystemByte = s5f3.SystemByte,
                    ACKC5 = ackResult
                };
                SCUtility.secsActionRecordMsg(scApp, false, s5f4);
                TrxSECS.ReturnCode rtnCode = ISECSControl.replySECS(bcfApp, s5f4);
                SCUtility.actionRecordMsg(scApp, s5f4.StreamFunction, line.LINE_ID,
                        "Reply Enable or Disable Alarm Report Request To MES.", rtnCode.ToString());
            }
            catch (Exception ex)
            {
                logger.Error("MESDefaultMapAction has Error[Line Name:{0}],[Error method:{1}],[Error Message:{2}",
                     line.LINE_ID, "S5F3_Receive_Enable_Disable_Alarm_Rpt_Req", ex.ToString());
            }
        }


















        protected override void S2F33ReceiveDefineReport(object sender, SECSEventArgs e)
        {
            try
            {
                S2F33 s2f33 = ((S2F33)e.secsHandler.Parse<S2F33>(e));
                SCUtility.secsActionRecordMsg(scApp, true, s2f33);
                if (!isProcess(s2f33)) { return; }

                S2F34 s2f34 = null;
                s2f34 = new S2F34();
                s2f34.SystemByte = s2f33.SystemByte;
                s2f34.SECSAgentName = scApp.EAPSecsAgentName;
                s2f34.DRACK = "0";


                TrxSECS.ReturnCode rtnCode = ISECSControl.replySECS(bcfApp, s2f34);
                SCUtility.secsActionRecordMsg(scApp, false, s2f34);


                if (rtnCode != TrxSECS.ReturnCode.Normal)
                {
                    logger.Warn("Reply EQPT S2F18 Error:{0}", rtnCode);
                }

                scApp.CEIDBLL.DeleteRptInfoByBatch();

                if (s2f33.RPTITEMS != null && s2f33.RPTITEMS.Length > 0)
                    scApp.CEIDBLL.buildReportIDAndVid(s2f33.ToDictionary());



                SECSConst.setDicRPTIDAndVID(scApp.CEIDBLL.loadDicRPTIDAndVID());

            }
            catch (Exception ex)
            {
                logger.Error("MESDefaultMapAction has Error[Line Name:{0}],[Error method:{1}],[Error Message:{2}", line.LINE_ID, "S2F17_Receive_Date_Time_Req", ex.ToString());
            }
        }
        protected override void S2F35ReceiveLinkEventReport(object sender, SECSEventArgs e)
        {
            try
            {
                S2F35 s2f35 = ((S2F35)e.secsHandler.Parse<S2F35>(e));
                SCUtility.secsActionRecordMsg(scApp, true, s2f35);
                if (!isProcess(s2f35)) { return; }


                S2F36 s2f36 = null;
                s2f36 = new S2F36();
                s2f36.SystemByte = s2f35.SystemByte;
                s2f36.SECSAgentName = scApp.EAPSecsAgentName;
                s2f36.LRACK = "0";

                TrxSECS.ReturnCode rtnCode = ISECSControl.replySECS(bcfApp, s2f36);
                SCUtility.secsActionRecordMsg(scApp, false, s2f36);
                if (rtnCode != TrxSECS.ReturnCode.Normal)
                {
                    logger.Warn("Reply EQPT S2F18 Error:{0}", rtnCode);
                }

                scApp.CEIDBLL.DeleteCEIDInfoByBatch();

                if (s2f35.RPTITEMS != null && s2f35.RPTITEMS.Length > 0)
                    scApp.CEIDBLL.buildCEIDAndReportID(s2f35.ToDictionary());

                SECSConst.setDicCEIDAndRPTID(scApp.CEIDBLL.loadDicCEIDAndRPTID());

            }
            catch (Exception ex)
            {
                logger.Error("MESDefaultMapAction has Error[Line Name:{0}],[Error method:{1}],[Error Message:{2}", line.LINE_ID, "S2F17_Receive_Date_Time_Req", ex.ToString());
            }
        }
        protected override void S2F37ReceiveEnableDisableEventReport(object sender, SECSEventArgs e)
        {
            try
            {
                S2F37 s2f37 = ((S2F37)e.secsHandler.Parse<S2F37>(e));
                SCUtility.secsActionRecordMsg(scApp, true, s2f37);
                if (!isProcess(s2f37)) { return; }
                Boolean isValid = true;
                //Boolean isEnable = SCUtility.isMatche(s2f37.CEED, SECSConst.CEED_Enable);
                Boolean isEnable = s2f37.CEED[0]==1?true:false;
                int cnt = s2f37.CEIDS.Length;
                if (cnt == 0)
                {
                    isValid &= scApp.EventBLL.enableAllEventReport(isEnable);
                }
                else
                {
                    //Check Data
                    for (int ix = 0; ix < cnt; ++ix)
                    {
                        string ceid = s2f37.CEIDS[ix];
                        Boolean isContain = SECSConst.CEID_ARRAY.Contains(ceid.Trim());
                        if (!isContain)
                        {
                            isValid = false;
                            break;
                        }
                    }
                    if (isValid)
                    {
                        for (int ix = 0; ix < cnt; ++ix)
                        {
                            string ceid = s2f37.CEIDS[ix];
                            isValid &= scApp.EventBLL.enableEventReport(ceid, isEnable);
                        }
                    }
                }

                S2F38 s2f18 = null;
                s2f18 = new S2F38()
                {
                    SystemByte = s2f37.SystemByte,
                    SECSAgentName = scApp.EAPSecsAgentName,
                    ERACK = isValid ? SECSConst.ERACK_Accepted : SECSConst.ERACK_Denied_At_least_one_CEID_dose_not_exist
                };

                TrxSECS.ReturnCode rtnCode = ISECSControl.replySECS(bcfApp, s2f18);
                SCUtility.secsActionRecordMsg(scApp, false, s2f18);
                if (rtnCode != TrxSECS.ReturnCode.Normal)
                {
                    logger.Warn("Reply EQPT S2F18 Error:{0}", rtnCode);
                }
            }
            catch (Exception ex)
            {
                logger.Error("MESDefaultMapAction has Error[Line Name:{0}],[Error method:{1}],[Error Message:{2}", line.LINE_ID, "S2F17_Receive_Date_Time_Req", ex.ToString());
            }
        }


        protected override void S2F49ReceiveEnhancedRemoteCommandExtension(object sender, SECSEventArgs e)
        {
            try
            {
                if (scApp.getEQObjCacheManager().getLine().ServerPreStop)
                    return;
                string errorMsg = string.Empty;
                //S2F49 s2f49_transfer = ((S2F49)e.secsHandler.Parse<S2F49>(e));
                S2F49_TRANSFER s2f49_transfer = ((S2F49_TRANSFER)e.secsHandler.Parse<S2F49_TRANSFER>(e));
                switch (s2f49_transfer.RCMD)
                {
                    case "TRANSFER":
                        //S2F49_TRANSFER s2f49_transfer = ((S2F49_TRANSFER)e.secsHandler.Parse<S2F49_TRANSFER>(e));
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                           Data: s2f49_transfer);
                        SCUtility.secsActionRecordMsg(scApp, true, s2f49_transfer);
                        //SCUtility.RecodeReportInfo(s2f49_transfer);
                        //if (!isProcessEAP(s2f49)) { return; }

                        S2F50 s2f50 = new S2F50();
                        s2f50.SystemByte = s2f49_transfer.SystemByte;
                        s2f50.SECSAgentName = scApp.EAPSecsAgentName;
                        s2f50.HCACK = SECSConst.HCACK_Confirm;
                        string cmdID = s2f49_transfer.COMMS[0].COMMAINFOVALUE.CPVAL1.CPVAL;
                        string priority = s2f49_transfer.COMMS[0].COMMAINFOVALUE.CPVAL2.CPVAL;
                        string replace = s2f49_transfer.COMMS[0].COMMAINFOVALUE.CPVAL3.CPVAL;

                        string rtnStr = "";
                        //檢查CST Size及Glass Data

                        //S2F49.TRANSFERINFO tranferinfo = (S2F49.TRANSFERINFO)s2f49_transfer.REPITEMS[1];

                        //string cmdID = s2f49.REPITEMS.COMMINFO.COMMAINFO.COMMANDIDINFO.CommandID;
                        string cstID = s2f49_transfer.COMMS[1].COMMAINFOVALUE.CPVAL1.CPVAL;
                        string source = s2f49_transfer.COMMS[1].COMMAINFOVALUE.CPVAL2.CPVAL;
                        string dest = s2f49_transfer.COMMS[1].COMMAINFOVALUE.CPVAL3.CPVAL;
                        //string cstID = tranferinfo.Info.CarrierID.CPVAL;
                        //string source = tranferinfo.Info.SourcePort.CPVAL;
                        //string dest = tranferinfo.Info.DestPort.CPVAL;
                        //檢查搬送命令


                        s2f50.HCACK = doCheckMCSCommand(s2f49_transfer, ref s2f50, out rtnStr);

                        using (TransactionScope tx = SCUtility.getTransactionScope())
                        {
                            using (DBConnection_EF con = DBConnection_EF.GetUContext())
                            {
                                bool isCreatScuess = true;
                                if (s2f50.HCACK != SECSConst.HCACK_Rejected&& s2f50.HCACK != SECSConst.HCACK_Command_ID_Duplication && s2f50.HCACK != SECSConst.HCACK_Carrier_ID_Duplication)
                                    isCreatScuess &= scApp.CMDBLL.doCreatMCSCommand(cmdID, priority, replace, cstID, source, dest, s2f50.HCACK);
                                if (s2f50.HCACK == SECSConst.HCACK_Confirm)
                                    isCreatScuess &= scApp.SysExcuteQualityBLL.creatSysExcuteQuality(cmdID, cstID, source, dest);
                                if (isCreatScuess)
                                {
                                    TrxSECS.ReturnCode rtnCode = ISECSControl.replySECS(bcfApp, s2f50);
                                    SCUtility.secsActionRecordMsg(scApp, false, s2f50);
                                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                                       Data: s2f50);
                                    if (rtnCode != TrxSECS.ReturnCode.Normal)
                                    {
                                        logger.Warn("Reply EQPT S2F50) Error:{0}", rtnCode);
                                        isCreatScuess = false;
                                    }
                                    //SCUtility.RecodeReportInfo(s2f50, cmdID, rtnCode.ToString());
                                }
                                if (isCreatScuess)
                                {
                                    tx.Complete();
                                }
                                else
                                {
                                    return;
                                }
                            }
                        }
                        //}
                        if (s2f50.HCACK == SECSConst.HCACK_Confirm ||
                            s2f50.HCACK == SECSConst.HCACK_Confirm_Executed)
                        {
                            //scApp.CMDBLL.checkMCSTransferCommand();
                            scApp.CMDBLL.checkMCSTransferCommand_New();
                        }
                        else
                        {
                            //BCFApplication.onWarningMsg(rtnStr);
                            string xid = DateTime.Now.ToString(SCAppConstants.TimestampFormat_19);
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: string.Empty,
                                          Data: rtnStr,
                                          XID: xid);
                            BCFApplication.onWarningMsg(this, new bcf.Common.LogEventArgs(rtnStr, xid));
                        }
                        break;
                    //case "STAGE":
                    //    S2F49_STAGE s2f49_stage = ((S2F49_STAGE)e.secsHandler.Parse<S2F49_STAGE>(e));

                    //    S2F50 s2f50_stage = new S2F50();
                    //    s2f50_stage.SystemByte = s2f49_stage.SystemByte;
                    //    s2f50_stage.SECSAgentName = scApp.EAPSecsAgentName;
                    //    s2f50_stage.HCACK = SECSConst.HCACK_Confirm;

                    //    string source_port_id = s2f49_stage.REPITEMS.TRANSFERINFO.CPVALUE.SOURCEPORT_CP.CPVAL_ASCII;
                    //    TrxSECS.ReturnCode rtnCode_stage = ISECSControl.replySECS(bcfApp, s2f50_stage);
                    //    SCUtility.secsActionRecordMsg(scApp, false, s2f50_stage);

                    //    //TODO Stage
                    //    //將收下來的Stage命令先放到Redis上
                    //    //等待Timer發現後會將此命令取下來並下命令給車子去執行
                    //    //(此處將再考慮是要透過Timer或是開Thread來監控這件事)

                    //    var port = scApp.MapBLL.getPortByPortID(source_port_id);
                    //    AVEHICLE vh_test = scApp.VehicleBLL.findBestSuitableVhStepByStepFromAdr_New(port.ADR_ID, port.LD_VH_TYPE);
                    //    scApp.VehicleBLL.callVehicleToMove(vh_test, port.ADR_ID);
                    //    break;
                }
                line.CommunicationIntervalWithMCS.Restart();

            }
            catch (Exception ex)
            {
                logger.Error("MESDefaultMapAction has Error[Line Name:{0}],[Error method:{1}],[Error Message:{2}", line.LINE_ID, "S2F49_Receive_Remote_Command", ex);
            }
        }
        protected override void S5F3ReceiveEnableDisableAlarm(object sender, SECSEventArgs e)
        {
            try
            {
                bool isSuccess = true;
                S5F3 s5f3 = ((S5F3)e.secsHandler.Parse<S5F3>(e));
                SCUtility.secsActionRecordMsg(scApp, true, s5f3);
                if (!isProcess(s5f3)) { return; }
                Boolean isEnable = SCUtility.isMatche(s5f3.ALED, SECSConst.ALED_Enable);
                string alarm_code = s5f3.ALID;


                isSuccess = scApp.AlarmBLL.enableAlarmReport(alarm_code, isEnable);

                S5F4 s5f4 = null;
                s5f4 = new S5F4();
                s5f4.SystemByte = s5f3.SystemByte;
                s5f4.SECSAgentName = scApp.EAPSecsAgentName;
                s5f4.ACKC5 = isSuccess ? SECSConst.ACKC5_Accepted : SECSConst.ACKC5_Not_Accepted;

                TrxSECS.ReturnCode rtnCode = ISECSControl.replySECS(bcfApp, s5f4);
                SCUtility.secsActionRecordMsg(scApp, false, s5f4);
                if (rtnCode != TrxSECS.ReturnCode.Normal)
                {
                    logger.Warn("Reply EQPT S2F18 Error:{0}", rtnCode);
                }
            }
            catch (Exception ex)
            {
                logger.Error("MESDefaultMapAction has Error[Line Name:{0}],[Error method:{1}],[Error Message:{2}", line.LINE_ID, "S2F17_Receive_Date_Time_Req", ex.ToString());
            }
        }
        public string doCheckMCSCommand(string command_id, string Priority, string carrier_id, string HostSource, string HostDestination, out string check_result)
        {
            check_result = string.Empty;
            string checkcode = SECSConst.HCACK_Confirm;
            bool isSuccess = true;
            int ipriority = 0;
            string from_adr = string.Empty;
            string to_adr = string.Empty;
            E_VH_TYPE vh_type = E_VH_TYPE.None;
            if (isSuccess)
            {
                var cmd_obj = scApp.CMDBLL.getCMD_MCSByID(command_id);
                if (cmd_obj != null)
                {
                    check_result = $"Command id:{command_id} already exist.";
                    checkcode = SECSConst.HCACK_Confirm_Executed;
                }
            }

            return checkcode;
        }

        public string doCheckMCSCommand(S2F49_TRANSFER s2F49_TRANSFER, ref S2F50 s2F50, out string check_result)
        {
            check_result = string.Empty;
            string checkcode = SECSConst.HCACK_Confirm;
            bool isSuccess = true;
            string vehicleID = "";


            //string command_id = s2F49_TRANSFER.REPITEMS.COMMAND_INFO.CommandID.CPVAL;
            //string priority = s2F49_TRANSFER.REPITEMS.COMMAND_INFO.Priority.CPVAL;
            //string replace = s2F49_TRANSFER.REPITEMS.COMMAND_INFO.Replace.CPVAL;
            //string carrier_id = s2F49_TRANSFER.REPITEMS.TRANSFERINFOS[0].CarrierID.CPVAL;
            //string source_port_or_vh_id = s2F49_TRANSFER.REPITEMS.TRANSFERINFOS[0].SourcePort.CPVAL;
            //string dest_port = s2F49_TRANSFER.REPITEMS.TRANSFERINFOS[0].DestPort.CPVAL;

            string command_id = s2F49_TRANSFER.COMMS[0].COMMAINFOVALUE.CPVAL1.CPVAL;
            string priority = s2F49_TRANSFER.COMMS[0].COMMAINFOVALUE.CPVAL2.CPVAL;
            string replace = s2F49_TRANSFER.COMMS[0].COMMAINFOVALUE.CPVAL3.CPVAL;
            string carrier_id = s2F49_TRANSFER.COMMS[1].COMMAINFOVALUE.CPVAL1.CPVAL;
            string source_port_or_vh_id = s2F49_TRANSFER.COMMS[1].COMMAINFOVALUE.CPVAL2.CPVAL;
            string dest_port = s2F49_TRANSFER.COMMS[1].COMMAINFOVALUE.CPVAL3.CPVAL;

            //確認命令是否已經執行中
            if (isSuccess)
            {
                var cmd_obj = scApp.CMDBLL.getCMD_MCSByID(command_id);
                if (cmd_obj != null)
                {
                    check_result = $"MCS command id:{command_id} already exist.";
                    return SECSConst.HCACK_Command_ID_Duplication;
                }
            }
            //確認參數是否正確
            //isSuccess &= checkCommandID(comminfo_check_result, s2F49_TRANSFER.REPITEMS.COMMAND_INFO.CommandID.CPNAME, command_id);
            //isSuccess &= checkPriorityID(comminfo_check_result, s2F49_TRANSFER.REPITEMS.COMMAND_INFO.Priority.CPNAME, priority);
            //isSuccess &= checkReplace(comminfo_check_result, s2F49_TRANSFER.REPITEMS.COMMAND_INFO.Replace.CPNAME, replace);

            //isSuccess &= checkCarierID(traninfo_check_result, s2F49_TRANSFER.REPITEMS.TRANSFERINFOS[0].CarrierID.CPNAME, carrier_id);
            //isSuccess &= checkPortID(traninfo_check_result, s2F49_TRANSFER.REPITEMS.TRANSFERINFOS[0].SourcePort.CPNAME, source_port_or_vh_id);
            //isSuccess &= checkPortID(traninfo_check_result, s2F49_TRANSFER.REPITEMS.TRANSFERINFOS[0].DestPort.CPNAME, dest_port);
            isSuccess &= checkCommandID(s2F49_TRANSFER.COMMS[0].COMMAINFOVALUE.CPVAL1.CPNAME, command_id);
            isSuccess &= checkPriorityID(s2F49_TRANSFER.COMMS[0].COMMAINFOVALUE.CPVAL2.CPNAME, priority);
            isSuccess &= checkReplace(s2F49_TRANSFER.COMMS[0].COMMAINFOVALUE.CPVAL3.CPNAME, replace);

            isSuccess &= checkCarierID(s2F49_TRANSFER.COMMS[1].COMMAINFOVALUE.CPVAL1.CPNAME, carrier_id);
            isSuccess &= checkPortID(s2F49_TRANSFER.COMMS[1].COMMAINFOVALUE.CPVAL2.CPNAME, source_port_or_vh_id);
            isSuccess &= checkPortID(s2F49_TRANSFER.COMMS[1].COMMAINFOVALUE.CPVAL3.CPNAME, dest_port);

            List<SXFY> cep_items = new List<SXFY>();
            for(int i =0;i< s2F49_TRANSFER.COMMS.Length; i++)
            {
                if (i == 0)
                {
                    S2F50.COMMANDINFO comm_info_cepack = new S2F50.COMMANDINFO()
                    {
                        CPNAME = s2F49_TRANSFER.COMMS[i].COMMANDINFONAME,
                        Info = new S2F50.COMMANDINFO.INFO()
                        {
                            CommandID = new S2F50.COMMANDINFO.INFO.COMMANDID()
                            {
                                CPNAME = s2F49_TRANSFER.COMMS[i].COMMAINFOVALUE.CPVAL1.CPNAME,
                                CPVAL = s2F49_TRANSFER.COMMS[i].COMMAINFOVALUE.CPVAL1.CPVAL
                            },
                            Priority = new S2F50.COMMANDINFO.INFO.PRIORITY()
                            {
                                CPNAME = s2F49_TRANSFER.COMMS[i].COMMAINFOVALUE.CPVAL2.CPNAME,
                                CPVAL = s2F49_TRANSFER.COMMS[i].COMMAINFOVALUE.CPVAL2.CPVAL
                            },
                            Replace = new S2F50.COMMANDINFO.INFO.REPLACE()
                            {
                                CPNAME = s2F49_TRANSFER.COMMS[i].COMMAINFOVALUE.CPVAL3.CPNAME,
                                CPVAL = s2F49_TRANSFER.COMMS[i].COMMAINFOVALUE.CPVAL3.CPVAL
                            }
                        }
                    };
                    cep_items.Add(comm_info_cepack);
                }
                else
                {
                    S2F50.TRANSFERINFO tran_info_cepack = new S2F50.TRANSFERINFO()
                    {
                        CPNAME = s2F49_TRANSFER.COMMS[i].COMMANDINFONAME,
                        Info = new S2F50.TRANSFERINFO.INFO()
                        {
                            CarrierID = new S2F50.TRANSFERINFO.INFO.CARRIERID()
                            {
                                CPNAME = s2F49_TRANSFER.COMMS[i].COMMAINFOVALUE.CPVAL1.CPNAME,
                                CPVAL = s2F49_TRANSFER.COMMS[i].COMMAINFOVALUE.CPVAL1.CPVAL
                            },
                            SourcePort = new S2F50.TRANSFERINFO.INFO.SOURCEPORT()
                            {
                                CPNAME = s2F49_TRANSFER.COMMS[i].COMMAINFOVALUE.CPVAL2.CPNAME,
                                CPVAL = s2F49_TRANSFER.COMMS[i].COMMAINFOVALUE.CPVAL2.CPVAL
                            },
                            DestPort = new S2F50.TRANSFERINFO.INFO.DESTPORT()
                            {
                                CPNAME = s2F49_TRANSFER.COMMS[i].COMMAINFOVALUE.CPVAL3.CPNAME,
                                CPVAL = s2F49_TRANSFER.COMMS[i].COMMAINFOVALUE.CPVAL3.CPVAL
                            }
                        }
                    };
                    cep_items.Add(tran_info_cepack);
                }
            }
            s2F50.CEPCOLLECT = cep_items.ToArray();

            if (!isSuccess)
            {
                check_result = $"MCS command id:{command_id} has parameter invalid";
                return SECSConst.HCACK_Param_Invalid;
            }

            //確認是否有同一顆正在搬送的CST ID
            if (isSuccess)
            {
                var cmd_obj = scApp.CMDBLL.getExcuteCMD_MCSByCarrierID(carrier_id);
                if (cmd_obj != null)
                {
                    check_result = $"MCS command id:{command_id} of carrier id:{carrier_id} already excute by command id:{cmd_obj.CMD_ID.Trim()}";
                    return SECSConst.HCACK_Carrier_ID_Duplication;
                }
            }

            //確認是否有在相同Load Port的Transfer Command且該命令狀態還沒有變成Transferring(代表還在Port上還沒搬走)
            //M0.03 start
            //if (isSuccess)
            //{
            //    //M0.02 var cmd_obj = scApp.CMDBLL.getWatingCMDByFromTo(source_port_or_vh_id, dest_port);
            //    var cmd_obj = scApp.CMDBLL.getWatingCMD_MCSByFrom(source_port_or_vh_id);//M0.02 
            //    if (cmd_obj != null)
            //    {
            //        check_result = $"MCS command id:{command_id} is same as orther mcs command id {cmd_obj.CMD_ID.Trim()} of load port.";//M0.02 
            //        //M0.02 check_result = $"MCS command id:{command_id} of transfer load port is same command id:{cmd_obj.CMD_ID.Trim()}";
            //        return SECSConst.HCACK_Rejected;
            //    }
            //}
            //M0.03 end
            //確認 Port是否存在
            bool source_is_a_port = scApp.PortStationBLL.OperateCatch.IsExist(source_port_or_vh_id);
            if (source_is_a_port)
            {
                isSuccess = true;
            }
            //如果不是PortID的話，則可能是VehicleID
            else
            {
                if (scApp.BC_ID == "NORTH_INNOLUX")
                {
                    vehicleID = source_port_or_vh_id.Replace("-01", "");
                    isSuccess = scApp.VehicleBLL.cache.IsVehicleExistByRealID(vehicleID);

                }
                else
                {
                    vehicleID = source_port_or_vh_id;
                    isSuccess = scApp.VehicleBLL.cache.IsVehicleExistByRealID(source_port_or_vh_id);

                }
            }
            if (!isSuccess)
            {
                check_result = $"MCS command id:{command_id} - source Port:{source_port_or_vh_id} not exist.{Environment.NewLine}please confirm the port name";
                return SECSConst.HCACK_Obj_Not_Exist;
            }

            isSuccess = scApp.PortStationBLL.OperateCatch.IsExist(dest_port);
            if (!isSuccess)
            {
                check_result = $"MCS command id:{command_id} - destination Port:{dest_port} not exist.{Environment.NewLine}please confirm the port name";
                return SECSConst.HCACK_Obj_Not_Exist;
            }

            //如果Source是個Port才需要檢查
            if (source_is_a_port)
            {
                ////確認是否有車子來可以搬送
                //AVEHICLE vh = scApp.VehicleBLL.findBestSuitableVhStepByStepFromAdr(source_port_or_vh_id, E_VH_TYPE.None, isCheckHasVhCarry: true);
                //isSuccess = vh != null;
                //if (!isSuccess)
                //{
                //    check_result = $"No vehicle can reach mcs command id:{command_id} - source port:{source_port_or_vh_id}.{Environment.NewLine}please check the road traffic status.";
                //    return SECSConst.HCACK_Cannot_Perform_Now;
                //}
                ////確認路徑是否可以行走
                APORTSTATION source_port_station = scApp.PortStationBLL.OperateCatch.getPortStation(source_port_or_vh_id);
                APORTSTATION dest_port_station = scApp.PortStationBLL.OperateCatch.getPortStation(dest_port);
                isSuccess = scApp.GuideBLL.IsRoadWalkable(source_port_station.ADR_ID, dest_port_station.ADR_ID);
                if (!isSuccess)
                {
                    check_result = $"MCS command id:{command_id} ,source port:{source_port_or_vh_id} to destination port:{dest_port} no path to go{Environment.NewLine}," +
                        $"please check the road traffic status.";
                    return SECSConst.HCACK_Cannot_Perform_Now;
                }
            }
            //如果不是Port(則為指定車號)，要檢查是否從該車位置可以到達放貨地點
            else
            {
                AVEHICLE carry_vh = scApp.VehicleBLL.cache.getVehicleByRealID(vehicleID);
                APORTSTATION dest_port_station = scApp.PortStationBLL.OperateCatch.getPortStation(dest_port);
                isSuccess = scApp.GuideBLL.IsRoadWalkable(carry_vh.CUR_ADR_ID, dest_port_station.ADR_ID);
                if (!isSuccess)
                {
                    check_result = $"MCS command id:{command_id} ,vh:{source_port_or_vh_id} current address:{carry_vh.CUR_ADR_ID} to destination port:{dest_port}:{dest_port_station.ADR_ID} no path to go{Environment.NewLine}," +
                        $"please check the road traffic status.";
                    return SECSConst.HCACK_Cannot_Perform_Now;
                }
            }

            return SECSConst.HCACK_Confirm;
        }

        private bool checkCommandID(string name, string value)
        {
            bool is_success = !SCUtility.isEmpty(value);
            return is_success;
        }
        private bool checkPriorityID(string name, string value)
        {
            int i_priority = 0;
            bool is_success = int.TryParse(value, out i_priority);
            return is_success;
        }
        private bool checkReplace(string name, string value)
        {
            bool is_success = true;
            int i_replace = 0;
            if (SCUtility.isEmpty(value))
            {
                is_success = true;
            }
            else
            {
                is_success = int.TryParse(value, out i_replace);
            }
            return is_success;
        }

        private bool checkCarierID(string name, string value)
        {
            bool is_success = !SCUtility.isEmpty(value);
            return is_success;
        }
        private bool checkPortID(string name, string value)
        {
            bool is_success = !SCUtility.isEmpty(value);
            return is_success;
        }


        protected override void S2F41ReceiveHostCommand(object sender, SECSEventArgs e)
        {
            try
            {
                S2F41 s2f41 = ((S2F41)e.secsHandler.Parse<S2F41>(e));
                SCUtility.secsActionRecordMsg(scApp, true, s2f41);
                //if (!isProcessEAP(s2f37)) { return; }
                string mcs_cmd_id = string.Empty;
                bool needToResume = false;
                bool needToPause = false;
                bool canCancelCmd = false;
                bool canAbortCmd = false;
                bool canRenameCarrier = false;
                string new_carrier_id = "";
                string old_carrier_id = "";
                string cancel_abort_cmd_id = string.Empty;
                if(SCUtility.isMatche( s2f41.RCMD, SECSConst.RCMD_Resume))
                {
                    S2F42 s2f42 = null;
                    s2f42 = new S2F42();
                    s2f42.SystemByte = s2f41.SystemByte;
                    s2f42.SECSAgentName = scApp.EAPSecsAgentName;
                    if (line.TSC_state_machine.State == ALINE.TSCState.PAUSED || line.TSC_state_machine.State == ALINE.TSCState.PAUSING)
                    {
                        s2f42.HCACK = SECSConst.HCACK_Confirm_Executed;
                        needToResume = true;
                    }
                    else
                    {
                        s2f42.HCACK = SECSConst.HCACK_Cannot_Perform_Now;
                        needToResume = false;
                    }
                    TrxSECS.ReturnCode rtnCode = ISECSControl.replySECS(bcfApp, s2f42);
                    SCUtility.secsActionRecordMsg(scApp, false, s2f42);
                    if (rtnCode != TrxSECS.ReturnCode.Normal)
                    {
                        logger.Warn("Reply EQPT S2F18 Error:{0}", rtnCode);
                    }
                }
                else if (SCUtility.isMatche(s2f41.RCMD, SECSConst.RCMD_Pause))
                {
                    S2F42 s2f42 = null;
                    s2f42 = new S2F42();
                    s2f42.SystemByte = s2f41.SystemByte;
                    s2f42.SECSAgentName = scApp.EAPSecsAgentName;
                    if (line.TSC_state_machine.State == ALINE.TSCState.AUTO)
                    {
                        s2f42.HCACK = SECSConst.HCACK_Confirm_Executed;
                        needToPause = true;
                    }
                    else
                    {
                        s2f42.HCACK = SECSConst.HCACK_Cannot_Perform_Now;
                        needToResume = false;
                    }
                    TrxSECS.ReturnCode rtnCode = ISECSControl.replySECS(bcfApp, s2f42);
                    SCUtility.secsActionRecordMsg(scApp, false, s2f42);
                    if (rtnCode != TrxSECS.ReturnCode.Normal)
                    {
                        logger.Warn("Reply EQPT S2F18 Error:{0}", rtnCode);
                    }
                }
                else if (SCUtility.isMatche(s2f41.RCMD, SECSConst.RCMD_Rename))
                {
                    S2F41_RENAME s2f41_rename = ((S2F41_RENAME)e.secsHandler.Parse<S2F41_RENAME>(e));
                    var rename_check_result = checkHostCommandRename(s2f41_rename);
                    canRenameCarrier = rename_check_result.isOK;
                    S2F42_RENAME s2f42 = null;
                    s2f42 = new S2F42_RENAME();
                    s2f42.SystemByte = s2f41.SystemByte;
                    s2f42.SECSAgentName = scApp.EAPSecsAgentName;
                    s2f42.HCACK = rename_check_result.checkResult;
                    s2f42.ITEM = new S2F42_RENAME.RPYITEM();
                    s2f42.ITEM.CMDINFO = new S2F42_RENAME.RPYITEM.COMMNADINFO();
                    s2f42.ITEM.CMDINFO.CPNAME = "COMMANDID";
                    s2f42.ITEM.CMDINFO.CPVAL = s2f41_rename.ITEM.CMDITEM.CPVAL;
                    s2f42.ITEM.PRECAINFO = new S2F42_RENAME.RPYITEM.CARRIERINFO();
                    s2f42.ITEM.PRECAINFO.CPNAME = "PRECARRIERID";
                    s2f42.ITEM.PRECAINFO.CPVAL = s2f41_rename.ITEM.PRECAITEM.CPVAL;
                    s2f42.ITEM.NEWCAINFO = new S2F42_RENAME.RPYITEM.CARRIERINFO();
                    s2f42.ITEM.NEWCAINFO.CPNAME = "NEWCARRIERID";
                    s2f42.ITEM.NEWCAINFO.CPVAL = s2f41_rename.ITEM.NEWCAITEM.CPVAL;
                    s2f42.ITEM.CLINFO = new S2F42_RENAME.RPYITEM.CARRIERLOCINFO();
                    s2f42.ITEM.CLINFO.CPNAME = "CARRIERLOC";
                    s2f42.ITEM.CLINFO.CPVAL = s2f41_rename.ITEM.CLITEM.CPVAL;
                    mcs_cmd_id = s2f41_rename.ITEM.CMDITEM.CPVAL;
                    new_carrier_id = s2f41_rename.ITEM.NEWCAITEM.CPVAL;
                    old_carrier_id = s2f41_rename.ITEM.PRECAITEM.CPVAL;
                    bool result = false;
                    if (canRenameCarrier)
                    {
                        if (old_carrier_id == null || string.IsNullOrWhiteSpace(old_carrier_id))
                        {
                            old_carrier_id = "ERROR";
                        }
                        result = scApp.VehicleService.DoCarrierIDRenameRequset(s2f41_rename.ITEM.CMDITEM.CPVAL, new_carrier_id, old_carrier_id);
                        if (!result)
                        {
                            s2f42.HCACK = SECSConst.HCACK_Cannot_Perform_Now;
                        }
                    }
                    TrxSECS.ReturnCode rtnCode = ISECSControl.replySECS(bcfApp, s2f42);
                    SCUtility.secsActionRecordMsg(scApp, false, s2f42);
                    if (rtnCode != TrxSECS.ReturnCode.Normal)
                    {
                        logger.Warn("Reply EQPT S2F18 Error:{0}", rtnCode);
                    }
                    if (result)
                    {
                        S64F1SendDestinationChangeRequest(mcs_cmd_id, new_carrier_id);
                    }
                }
                else if (SCUtility.isMatche(s2f41.RCMD, SECSConst.RCMD_Abort))
                {
                    S2F41_CANCEL_ABORT_TRANSFER s2f41_transfer_abort = ((S2F41_CANCEL_ABORT_TRANSFER)e.secsHandler.Parse<S2F41_CANCEL_ABORT_TRANSFER>(e));
                    var abort_check_result = checkHostCommandAbort(s2f41_transfer_abort);
                    canAbortCmd = abort_check_result.isOK;
                    S2F42 s2f42 = null;
                    s2f42 = new S2F42();
                    s2f42.SystemByte = s2f41.SystemByte;
                    s2f42.SECSAgentName = scApp.EAPSecsAgentName;
                    s2f42.HCACK = abort_check_result.checkResult;
                    s2f42.ITEM = new S2F42.RPYITEM();
                    s2f42.ITEM.CPNAME = "COMMANDID";
                    s2f42.ITEM.CPACK = "0";
                    cancel_abort_cmd_id = abort_check_result.cmdID;
                    TrxSECS.ReturnCode rtnCode = ISECSControl.replySECS(bcfApp, s2f42);
                    SCUtility.secsActionRecordMsg(scApp, false, s2f42);
                    if (rtnCode != TrxSECS.ReturnCode.Normal)
                    {
                        logger.Warn("Reply EQPT S2F18 Error:{0}", rtnCode);
                    }
                }
                else if (SCUtility.isMatche(s2f41.RCMD, SECSConst.RCMD_Cancel))
                {
                    S2F41_CANCEL_ABORT_TRANSFER s2f41_transfer_cancel = ((S2F41_CANCEL_ABORT_TRANSFER)e.secsHandler.Parse<S2F41_CANCEL_ABORT_TRANSFER>(e));
                    var cancel_check_result = checkHostCommandCancel(s2f41_transfer_cancel);
                    canCancelCmd = cancel_check_result.isOK;
                    S2F42 s2f42 = null;
                    s2f42 = new S2F42();
                    s2f42.SystemByte = s2f41.SystemByte;
                    s2f42.SECSAgentName = scApp.EAPSecsAgentName;
                    s2f42.HCACK = cancel_check_result.checkResult;
                    s2f42.ITEM = new S2F42.RPYITEM();
                    s2f42.ITEM.CPNAME = "COMMANDID";
                    s2f42.ITEM.CPACK = "0";
                    cancel_abort_cmd_id = cancel_check_result.cmdID;
                    TrxSECS.ReturnCode rtnCode = ISECSControl.replySECS(bcfApp, s2f42);
                    SCUtility.secsActionRecordMsg(scApp, false, s2f42);
                    if (rtnCode != TrxSECS.ReturnCode.Normal)
                    {
                        logger.Warn("Reply EQPT S2F18 Error:{0}", rtnCode);
                    }
                }
                else if (SCUtility.isMatche(s2f41.RCMD, SECSConst.RCMD_CarrierCancel))
                {
                    S2F41_CARRIER_CANCEL_ABORT_TRANSFER s2f41_carrier_transfer_cancel = ((S2F41_CARRIER_CANCEL_ABORT_TRANSFER)e.secsHandler.Parse<S2F41_CARRIER_CANCEL_ABORT_TRANSFER>(e));
                    var carrier_cancel_check_result = checkHostCommandAbort(s2f41_carrier_transfer_cancel);
                    canCancelCmd = carrier_cancel_check_result.isOK;
                    S2F42_CARRIER_CANCEL_ABORT s2f42 = null;
                    s2f42 = new S2F42_CARRIER_CANCEL_ABORT();
                    s2f42.SystemByte = s2f41.SystemByte;
                    s2f42.SECSAgentName = scApp.EAPSecsAgentName;
                    s2f42.HCACK = carrier_cancel_check_result.checkResult;
                    s2f42.ITEM = new S2F42_CARRIER_CANCEL_ABORT.RPYITEM();
                    s2f42.ITEM.CMDINFO = new S2F42_CARRIER_CANCEL_ABORT.RPYITEM.COMMNADINFO();
                    s2f42.ITEM.CMDINFO.CPNAME = "COMMANDID";
                    s2f42.ITEM.CMDINFO.CPACK = "0";
                    s2f42.ITEM.CAINFO = new S2F42_CARRIER_CANCEL_ABORT.RPYITEM.CARRIERINFO();
                    s2f42.ITEM.CAINFO.CPNAME = "CARRIERID";
                    s2f42.ITEM.CAINFO.CPACK = "0";
                    cancel_abort_cmd_id = carrier_cancel_check_result.cmdID;
                    TrxSECS.ReturnCode rtnCode = ISECSControl.replySECS(bcfApp, s2f42);
                    SCUtility.secsActionRecordMsg(scApp, false, s2f42);
                    if (rtnCode != TrxSECS.ReturnCode.Normal)
                    {
                        logger.Warn("Reply EQPT S2F18 Error:{0}", rtnCode);
                    }
                }
                else if (SCUtility.isMatche(s2f41.RCMD, SECSConst.RCMD_CarrierAbort))
                {
                    S2F41_CARRIER_CANCEL_ABORT_TRANSFER s2f41_carrier_transfer_abort = ((S2F41_CARRIER_CANCEL_ABORT_TRANSFER)e.secsHandler.Parse<S2F41_CARRIER_CANCEL_ABORT_TRANSFER>(e));
                    var carrier_cancel_check_result = checkHostCommandCancel(s2f41_carrier_transfer_abort);
                    canCancelCmd = carrier_cancel_check_result.isOK;
                    S2F42_CARRIER_CANCEL_ABORT s2f42 = null;
                    s2f42 = new S2F42_CARRIER_CANCEL_ABORT();
                    s2f42.SystemByte = s2f41.SystemByte;
                    s2f42.SECSAgentName = scApp.EAPSecsAgentName;
                    s2f42.HCACK = carrier_cancel_check_result.checkResult;
                    s2f42.ITEM = new S2F42_CARRIER_CANCEL_ABORT.RPYITEM();
                    s2f42.ITEM.CMDINFO = new S2F42_CARRIER_CANCEL_ABORT.RPYITEM.COMMNADINFO();
                    s2f42.ITEM.CMDINFO.CPNAME = "COMMANDID";
                    s2f42.ITEM.CMDINFO.CPACK = "0";
                    s2f42.ITEM.CAINFO = new S2F42_CARRIER_CANCEL_ABORT.RPYITEM.CARRIERINFO();
                    s2f42.ITEM.CAINFO.CPNAME = "CARRIERID";
                    s2f42.ITEM.CAINFO.CPACK = "0";
                    cancel_abort_cmd_id = carrier_cancel_check_result.cmdID;
                    TrxSECS.ReturnCode rtnCode = ISECSControl.replySECS(bcfApp, s2f42);
                    SCUtility.secsActionRecordMsg(scApp, false, s2f42);
                    if (rtnCode != TrxSECS.ReturnCode.Normal)
                    {
                        logger.Warn("Reply EQPT S2F18 Error:{0}", rtnCode);
                    }
                }



                if (needToResume)
                {
                    line.ResumeToAuto(reportBLL);
                }
                if (needToPause)
                {
                    //line.RequestToPause(reportBLL);
                    scApp.LineService.TSCStateToPause();
                }
                if (canCancelCmd)
                {
                    scApp.VehicleService.doCancelOrAbortCommandByMCSCmdID(cancel_abort_cmd_id, ProtocolFormat.OHTMessage.CMDCancelType.CmdCancel);
                }
                if (canAbortCmd)
                {
                    scApp.VehicleService.doCancelOrAbortCommandByMCSCmdID(cancel_abort_cmd_id, ProtocolFormat.OHTMessage.CMDCancelType.CmdAbort);

                }

                
            }
            catch (Exception ex)
            {
                logger.Error("MESDefaultMapAction has Error[Line Name:{0}],[Error method:{1}],[Error Message:{2}", line.LINE_ID, "S2F17_Receive_Date_Time_Req", ex.ToString());
            }
        }
        private (bool isOK, string checkResult, string cmdID) checkHostCommandRename(S2F41_RENAME s2F41)
        {
            bool is_ok = true;
            string check_result = SECSConst.HCACK_Confirm;
            //bool is_ok = false;//目前無法實現Rename功能
            //string check_result = SECSConst.HCACK_Rejected;//目前無法實現Rename功能
            string command_id = string.Empty;
            var command_id_item = s2F41.ITEM.CMDITEM;
            if (command_id_item != null)
            {
                command_id = command_id_item.CPVAL;
                ACMD_MCS cmd_mcs = scApp.CMDBLL.getCMD_MCSByID(command_id);
                if (cmd_mcs == null)
                {
                    check_result = SECSConst.HCACK_Obj_Not_Exist;
                    is_ok = false;
                }
            }
            else
            {
                check_result = SECSConst.HCACK_Param_Invalid;
                is_ok = false;
            }
            return (is_ok, check_result, command_id);
        }
        private (bool isOK, string checkResult, string cmdID) checkHostCommandAbort(S2F41_CANCEL_ABORT_TRANSFER s2F41)
        {
            bool is_ok = true;
            string check_result = SECSConst.HCACK_Confirm;
            string command_id = string.Empty;
            var command_id_item = s2F41.ITEM;
            if (command_id_item != null)
            {
                command_id = command_id_item.CPVAL;
                ACMD_MCS cmd_mcs = scApp.CMDBLL.getCMD_MCSByID(command_id);
                if (cmd_mcs == null)
                {
                    check_result = SECSConst.HCACK_Obj_Not_Exist;
                    is_ok = false;
                }
            }
            else
            {
                check_result = SECSConst.HCACK_Param_Invalid;
                is_ok = false;
            }
            return (is_ok, check_result, command_id);
        }
        private (bool isOK, string checkResult, string cmdID) checkHostCommandAbort(S2F41_CARRIER_CANCEL_ABORT_TRANSFER s2F41)
        {
            bool is_ok = true;
            string check_result = SECSConst.HCACK_Confirm;
            string command_id = string.Empty;
            var command_id_item = s2F41.CMDITEM;
            if (command_id_item != null)
            {
                command_id = command_id_item.CPVAL;
                ACMD_MCS cmd_mcs = scApp.CMDBLL.getCMD_MCSByID(command_id);
                if (cmd_mcs == null)
                {
                    check_result = SECSConst.HCACK_Obj_Not_Exist;
                    is_ok = false;
                }
            }
            else
            {
                check_result = SECSConst.HCACK_Param_Invalid;
                is_ok = false;
            }
            return (is_ok, check_result, command_id);
        }
        private (bool isOK, string checkResult, string cmdID) checkHostCommandCancel(S2F41_CANCEL_ABORT_TRANSFER s2F41)
        {
            bool is_ok = true;
            string check_result = SECSConst.HCACK_Confirm;
            string command_id = string.Empty;
            var command_id_item = s2F41.ITEM;
            if (command_id_item != null)
            {
                command_id = command_id_item.CPVAL;
                ACMD_MCS cmd_mcs = scApp.CMDBLL.getCMD_MCSByID(command_id);
                if (cmd_mcs == null)
                {
                    check_result = SECSConst.HCACK_Obj_Not_Exist;
                    is_ok = false;
                }
            }
            else
            {
                check_result = SECSConst.HCACK_Param_Invalid;
                is_ok = false;
            }
            return (is_ok, check_result, command_id);
        }

        private (bool isOK, string checkResult, string cmdID) checkHostCommandCancel(S2F41_CARRIER_CANCEL_ABORT_TRANSFER s2F41)
        {
            bool is_ok = true;
            string check_result = SECSConst.HCACK_Confirm;
            string command_id = string.Empty;
            var command_id_item = s2F41.CMDITEM;
            if (command_id_item != null)
            {
                command_id = command_id_item.CPVAL;
                ACMD_MCS cmd_mcs = scApp.CMDBLL.getCMD_MCSByID(command_id);
                if (cmd_mcs == null)
                {
                    check_result = SECSConst.HCACK_Obj_Not_Exist;
                    is_ok = false;
                }
            }
            else
            {
                check_result = SECSConst.HCACK_Param_Invalid;
                is_ok = false;
            }
            return (is_ok, check_result, command_id);
        }
        protected override void S5F5ReceiveListAlarmRequest(object sender, SECSEventArgs e)
        {
            try
            {
                S5F5 s5f5 = ((S5F5)e.secsHandler.Parse<S5F5>(e));
                SCUtility.secsActionRecordMsg(scApp, true, s5f5);
                if (!isProcess(s5f5)) { return; }
                string alarm_codes = s5f5.ALID;

                List<AlarmMap> alarm_maps = scApp.AlarmBLL.loadAlarmMaps();
                string[] alarm_ids = scApp.AlarmBLL.getCurrentAlarmsFromRedis().Select(alarm => alarm.ALAM_CODE).ToArray();
                S5F6 s5f6 = null;
                s5f6 = new S5F6();
                s5f6.SystemByte = s5f5.SystemByte;
                s5f6.SECSAgentName = scApp.EAPSecsAgentName;
                s5f6.ALIDS = new S5F6.ALID_1[1];
                s5f6.ALIDS[0] = new S5F6.ALID_1();
                //if (alarm_codes.Length == 0)//填所有Alarm資料到S5F6
                //{
                //    s5f6.ALIDS = new S5F6.ALID_1[alarm_maps.Count];
                //    for (int i = 0; i < alarm_maps.Count; i++)
                //    {
                //        s5f6.ALIDS[i] = new S5F6.ALID_1();
                //        bool is_set = alarm_ids.Contains(alarm_maps[i].ALARM_ID);
                //        s5f6.ALIDS[i].ALCD = is_set ? "1" : "0";
                //        s5f6.ALIDS[i].ALID = alarm_maps[i].ALARM_ID;
                //        s5f6.ALIDS[i].ALTX = alarm_maps[i].ALARM_DESC;
                //    }
                //}
                //else
                //{
                //    s5f6.ALIDS = new S5F6.ALID_1[alarm_codes.Length];
                //    for (int i = 0; i < alarm_codes.Length; i++)//填S5F6資料
                //    {
                //        s5f6.ALIDS[i] = new S5F6.ALID_1();
                //        if (string.IsNullOrEmpty(alarm_codes[i]))
                //        {
                //            continue; //alarm_code空白不用填值
                //        }
                //        else
                //        {
                //            foreach (AlarmMap a in alarm_maps)
                //            {
                //                if (SCUtility.isMatche(a.ALARM_ID, alarm_codes[i]))
                //                {
                //                    bool is_set = alarm_ids.Contains(a.ALARM_ID);
                //                    s5f6.ALIDS[i].ALCD = is_set ? "1" : "0";
                //                    s5f6.ALIDS[i].ALID = alarm_codes[i];
                //                    s5f6.ALIDS[i].ALTX = a.ALARM_DESC;
                //                    break;
                //                }
                //                else
                //                {
                //                    continue;
                //                }
                //            }
                //        }
                //    }
                //}


                TrxSECS.ReturnCode rtnCode = ISECSControl.replySECS(bcfApp, s5f6);
                SCUtility.secsActionRecordMsg(scApp, false, s5f6);
                if (rtnCode != TrxSECS.ReturnCode.Normal)
                {
                    logger.Warn("Reply EQPT S5F6 Error:{0}", rtnCode);
                }
            }
            catch (Exception ex)
            {
                logger.Error("MESDefaultMapAction has Error[Line Name:{0}],[Error method:{1}],[Error Message:{2}", line.LINE_ID, "S5F5ReceiveListAlarmRequest", ex.ToString());
            }
        }
        protected virtual void S6F15ReceiveEventReportRequest(object sender, SECSEventArgs e)
        {
            try
            {
                S6F15 s6f15 = ((S6F15)e.secsHandler.Parse<S6F15>(e));
                SCUtility.secsActionRecordMsg(scApp, true, s6f15);
                if (!isProcess(s6f15)) { return; }
                string ceid = s6f15.CEID;

                List<AlarmMap> alarm_maps = scApp.AlarmBLL.loadAlarmMaps();
                string[] alarm_ids = scApp.AlarmBLL.getCurrentAlarmsFromRedis().Select(alarm => alarm.ALAM_CODE).ToArray();
                S6F16 s6F16 = new S6F16();
                s6F16.SystemByte = s6f15.SystemByte;
                s6F16.SECSAgentName = scApp.EAPSecsAgentName;
                s6F16.CEID = s6f15.CEID;
                //if (alarm_codes.Length == 0)//填所有Alarm資料到S5F6
                //{
                //    s5f6.ALIDS = new S5F6.ALID_1[alarm_maps.Count];
                //    for (int i = 0; i < alarm_maps.Count; i++)
                //    {
                //        s5f6.ALIDS[i] = new S5F6.ALID_1();
                //        bool is_set = alarm_ids.Contains(alarm_maps[i].ALARM_ID);
                //        s5f6.ALIDS[i].ALCD = is_set ? "1" : "0";
                //        s5f6.ALIDS[i].ALID = alarm_maps[i].ALARM_ID;
                //        s5f6.ALIDS[i].ALTX = alarm_maps[i].ALARM_DESC;
                //    }
                //}
                //else
                //{
                //    s5f6.ALIDS = new S5F6.ALID_1[alarm_codes.Length];
                //    for (int i = 0; i < alarm_codes.Length; i++)//填S5F6資料
                //    {
                //        s5f6.ALIDS[i] = new S5F6.ALID_1();
                //        if (string.IsNullOrEmpty(alarm_codes[i]))
                //        {
                //            continue; //alarm_code空白不用填值
                //        }
                //        else
                //        {
                //            foreach (AlarmMap a in alarm_maps)
                //            {
                //                if (SCUtility.isMatche(a.ALARM_ID, alarm_codes[i]))
                //                {
                //                    bool is_set = alarm_ids.Contains(a.ALARM_ID);
                //                    s5f6.ALIDS[i].ALCD = is_set ? "1" : "0";
                //                    s5f6.ALIDS[i].ALID = alarm_codes[i];
                //                    s5f6.ALIDS[i].ALTX = a.ALARM_DESC;
                //                    break;
                //                }
                //                else
                //                {
                //                    continue;
                //                }
                //            }
                //        }
                //    }
                //}


                TrxSECS.ReturnCode rtnCode = ISECSControl.replySECS(bcfApp, s6F16);
                SCUtility.secsActionRecordMsg(scApp, false, s6F16);
                if (rtnCode != TrxSECS.ReturnCode.Normal)
                {
                    logger.Warn("Reply EQPT S5F6 Error:{0}", rtnCode);
                }
            }
            catch (Exception ex)
            {
                logger.Error("MESDefaultMapAction has Error[Line Name:{0}],[Error method:{1}],[Error Message:{2}", line.LINE_ID, "S5F5ReceiveListAlarmRequest", ex.ToString());
            }
        }

        

        #endregion Receive 

        #region Send
        public override bool S1F1SendAreYouThere()
        {
            try
            {
                S1F1 s1f1 = new S1F1()
                {
                    SECSAgentName = scApp.EAPSecsAgentName
                };
                S1F2 s1f2 = null;
                string rtnMsg = string.Empty;
                SXFY abortSecs = null;
                //SCUtility.secsActionRecordMsg(scApp, false, s1f1);
                TrxSECS.ReturnCode rtnCode = ISECSControl.sendRecv<S1F2>(bcfApp, s1f1, out s1f2, out abortSecs, out rtnMsg, null);
                SCUtility.actionRecordMsg(scApp, s1f1.StreamFunction, line.Real_ID,
                                "Send Are You There To MES.", rtnCode.ToString());
                if (rtnCode == TrxSECS.ReturnCode.Normal)
                {
                    //SCUtility.secsActionRecordMsg(scApp, false, s1f2);
                    return true;
                }
                else if (rtnCode == TrxSECS.ReturnCode.Abort)
                {
                    SCUtility.secsActionRecordMsg(scApp, false, abortSecs);
                    if (DebugParameter.UseHostOffline)
                    {
                        scApp.LineBLL.updateHostControlState(SCAppConstants.LineHostControlState.HostControlState.Host_Offline);
                    }
                    else
                    {
                        scApp.LineBLL.updateHostControlState(SCAppConstants.LineHostControlState.HostControlState.EQ_Off_line);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exection:");
            }
            finally
            {
                line.CommunicationIntervalWithMCS.Restart();
            }
            return false;
        }
        public override bool S2F17SendDateAndTimeRequest()
        {
            try
            {
                S2F17 s2f17 = new S2F17();
                s2f17.SECSAgentName = scApp.EAPSecsAgentName;

                S2F18 s2f18 = null;
                string rtnMsg = string.Empty;
                SXFY abortSecs = null;
                SCUtility.secsActionRecordMsg(scApp, false, s2f17);

                TrxSECS.ReturnCode rtnCode = ISECSControl.sendRecv<S2F18>(bcfApp, s2f17, out s2f18, out abortSecs, out rtnMsg, null);
                SCUtility.actionRecordMsg(scApp, s2f17.StreamFunction, line.Real_ID, "Date Time Request.", rtnCode.ToString());

                if (rtnCode == TrxSECS.ReturnCode.Normal)
                {
                    SCUtility.secsActionRecordMsg(scApp, true, s2f18);
                    string timeStr = s2f18.TIME;
                    DateTime mesDateTime = DateTime.Now;
                    try
                    {
                        mesDateTime = DateTime.ParseExact(timeStr.Trim(), SCAppConstants.TimestampFormat_16, CultureInfo.CurrentCulture);
                    }
                    catch (Exception dtEx)
                    {
                        logger.Error(dtEx, String.Format("Receive Date Time Set Request From MES. Format Error![Date Time:{0}]",
                            timeStr));
                    }

                    if (!DebugParameter.DisableSyncTime)
                    {
                        SCUtility.updateSystemTime(mesDateTime);
                    }
                    //todo 跟其他設備同步
                    return true;
                }
                else
                    logger.Warn("Send Date Time Request[S2F17] Error!");
            }
            catch (Exception ex)
            {
                logger.Error("MESDefaultMapAction has Error[Line Name:{0}],[Error method:{1}],[Error Message:{2}", line.LINE_ID, "sendS2F17_DateTimeReq", ex.ToString());
            }

            return false;
        }

        public override bool S5F1SendAlarmReport(string alcd, string alid, string altx)
        {
            try
            {
                S5F1 s5f1 = new S5F1()
                {
                    SECSAgentName = scApp.EAPSecsAgentName,
                    ALCD = alcd,
                    ALID = alid,
                    ALTX = altx
                };
                S5F2 s5f2 = null;
                SXFY abortSecs = null;
                String rtnMsg = string.Empty;
                if (isSend())
                {
                    TrxSECS.ReturnCode rtnCode = ISECSControl.sendRecv<S5F2>(bcfApp, s5f1, out s5f2,
                        out abortSecs, out rtnMsg, null);
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(GEMDriver), Device: DEVICE_NAME_MCS,
                       Data: s5f1);
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(GEMDriver), Device: DEVICE_NAME_MCS,
                       Data: s5f2);
                    SCUtility.actionRecordMsg(scApp, s5f1.StreamFunction, line.Real_ID,
                        "Send Alarm Report.", rtnCode.ToString());
                    if (rtnCode != TrxSECS.ReturnCode.Normal)
                    {
                        logger.Warn("Send Alarm Report[S5F1] Error![rtnCode={0}]", rtnCode);
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception:");
                return false;
            }
        }
        public override bool S6F11SendUnitAlarmSet(string eq_id,string cmd_id, string alid, string altx, string error_code, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            try
            {
                VIDCollection Vids = new VIDCollection();
                Vids.VID_05_Clock.CLOCK = DateTime.Now.ToString(SCAppConstants.TimestampFormat_14).PadRight(16, '0');

                Vids.VID_01_AlarmID.ALID = alid;
                Vids.VID_70_VehicleID.VEHICLE_ID = eq_id;
                Vids.VID_58_CommandID.COMMAND_ID = cmd_id;
                Vids.VID_1060_AlarmText.ALARM_TEXT = altx;
                Vids.VID_1070_AlarmLoc.ALARM_LOC = eq_id;
                Vids.VID_1001_AlarmLevel.ALARM_LEVEL = error_code;
                Vids.VID_1002_FlagForAlarmReport.FLAG_FOR_ALARM_REPORT = "1";
                Vids.VID_1003_Classification.CLASSIFICATION = "1";

                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Alarm_Set, Vids);
                scApp.ReportBLL.insertMCSReport(mcs_queue);
                if (reportQueues == null)
                {
                    S6F11SendMessage(mcs_queue);
                }
                else
                {
                    reportQueues.Add(mcs_queue);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
            }
            return true;
        }

        public override bool S6F11SendUnitAlarmCleared(string eq_id, string alid, string altx, string error_code, string desc, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            //try
            //{
            //    VIDCollection Vids = new VIDCollection();
            //    Vids.VID_01_AlarmID.ALID = alid;
            //    Vids.VID_901_AlarmText.ALARM_TEXT = altx;
            //    //Vids.VID_904_UnitID.UNIT_ID = unitID;
            //    Vids.VID_903_ErrorCode.ERROR_CODE = error_code;
            //    AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Unit_Error_Cleared, Vids);
            //    scApp.ReportBLL.insertMCSReport(mcs_queue);
            //    if (reportQueues == null)
            //    {
            //        S6F11SendMessage(mcs_queue);
            //    }
            //    else
            //    {
            //        reportQueues.Add(mcs_queue);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
            //       Data: ex);
            //}
            return false;
        }
        public bool S6F11SendAlarmEvent(string eq_id, string ceid, string alid, string cmd_id, string altx, string alarmLvl, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            try
            {
                VIDCollection Vids = new VIDCollection();
                Vids.VID_05_Clock.CLOCK = DateTime.Now.ToString(SCAppConstants.TimestampFormat_14).PadRight(16, '0');
                Vids.VID_01_AlarmID.ALID = alid;
                Vids.VID_70_VehicleID.VEHICLE_ID = eq_id;
                Vids.VID_58_CommandID.COMMAND_ID = cmd_id;
                Vids.VID_1060_AlarmText.ALARM_TEXT = altx;
                Vids.VID_1070_AlarmLoc.ALARM_LOC = eq_id;
                Vids.VID_1001_AlarmLevel.ALARM_LEVEL = alarmLvl;
                Vids.VID_1002_FlagForAlarmReport.FLAG_FOR_ALARM_REPORT = "0";
                Vids.VID_1003_Classification.CLASSIFICATION = "0";
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(ceid, Vids);
                scApp.ReportBLL.insertMCSReport(mcs_queue);
                if (reportQueues == null)
                {
                    S6F11SendMessage(mcs_queue);
                }
                else
                {
                    reportQueues.Add(mcs_queue);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
                return false;
            }
        }
        public bool S64F1SendDestinationChangeRequest(string cmd_id, string carrier_id)
        {
            try
            {
                S64F1 s64f1 = new S64F1()
                {
                    SECSAgentName = scApp.EAPSecsAgentName,
                    CLOCK = DateTime.Now.ToString(SCAppConstants.TimestampFormat_14).PadRight(16, '0'),
                    COMMANDID = cmd_id,
                    CARRIERID = carrier_id
                };
                S64F2 s64f2 = null;
                SXFY abortSecs = null;
                String rtnMsg = string.Empty;
                if (isSend())
                {
                    TrxSECS.ReturnCode rtnCode = ISECSControl.sendRecv<S64F2>(bcfApp, s64f1, out s64f2,
                        out abortSecs, out rtnMsg, null);
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(GEMDriver), Device: DEVICE_NAME_MCS,
                       Data: s64f1);
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(GEMDriver), Device: DEVICE_NAME_MCS,
                       Data: s64f2);
                    SCUtility.actionRecordMsg(scApp, s64f1.StreamFunction, line.Real_ID,
                        "Send Destination Change Request.", rtnCode.ToString());
                    if (rtnCode != TrxSECS.ReturnCode.Normal)
                    {
                        logger.Warn("Send Destination Change Request[S64F1] Error![rtnCode={0}]", rtnCode);
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception:");
                return false;
            }
        }
        public override bool S6F11SendEquiptmentOffLine()
        {
            try
            {
                VIDCollection Vids = new VIDCollection();
                Vids.VID_05_Clock.CLOCK = DateTime.Now.ToString(SCAppConstants.TimestampFormat_14).PadRight(16, '0');
                Vids.VID_1210_PriviousControlState.PREVIOUS_CONTROL_STATE = SCAppConstants.LineHostControlState.convert2MES(line.Privious_Host_Control_State);
                Vids.VID_06_ControlState.CONTROLSTATE = SCAppConstants.LineHostControlState.convert2MES(line.Host_Control_State);
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Equipment_OFF_LINE, Vids);
                scApp.ReportBLL.insertMCSReport(mcs_queue);
                S6F11SendMessage(mcs_queue);
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
            }
            return true;
        }
        public override bool S6F11SendControlStateLocal()
        {
            try
            {
                VIDCollection Vids = new VIDCollection();
                Vids.VID_05_Clock.CLOCK = DateTime.Now.ToString(SCAppConstants.TimestampFormat_14).PadRight(16, '0');
                Vids.VID_1210_PriviousControlState.PREVIOUS_CONTROL_STATE = SCAppConstants.LineHostControlState.convert2MES(line.Privious_Host_Control_State);
                Vids.VID_06_ControlState.CONTROLSTATE = SCAppConstants.LineHostControlState.convert2MES(line.Host_Control_State);
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Control_Status_Local, Vids);
                scApp.ReportBLL.insertMCSReport(mcs_queue);
                S6F11SendMessage(mcs_queue);
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
            }
            return true;
        }
        public override bool S6F11SendControlStateRemote()
        {
            try
            {
                VIDCollection Vids = new VIDCollection();
                Vids.VID_05_Clock.CLOCK = DateTime.Now.ToString(SCAppConstants.TimestampFormat_14).PadRight(16, '0');
                Vids.VID_1210_PriviousControlState.PREVIOUS_CONTROL_STATE = SCAppConstants.LineHostControlState.convert2MES(line.Privious_Host_Control_State);
                Vids.VID_06_ControlState.CONTROLSTATE = SCAppConstants.LineHostControlState.convert2MES(line.Host_Control_State);
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Control_Status_Remote, Vids);
                scApp.ReportBLL.insertMCSReport(mcs_queue);
                S6F11SendMessage(mcs_queue);
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
            }
            return true;
        }

        public override bool S6F11SendTSCAutoInitiated()
        {
            try
            {
                VIDCollection Vids = new VIDCollection();
                Vids.VID_05_Clock.CLOCK = DateTime.Now.ToString(SCAppConstants.TimestampFormat_14).PadRight(16, '0');
                Vids.VID_1240_PriviousTSCState.PREVIOUS_TSC_STATE = ((int)line.PriviousSCStats).ToString();
                Vids.VID_73_TSCState.TSC_STATE = ((int)line.SCStats).ToString();
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_TSC_Auto_Initiated, Vids);
                scApp.ReportBLL.insertMCSReport(mcs_queue);
                S6F11SendMessage(mcs_queue);
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
            }
            return true;
        }

        public override bool S6F11SendTSCPaused()
        {
            try
            {
                VIDCollection Vids = new VIDCollection();
                Vids.VID_05_Clock.CLOCK = DateTime.Now.ToString(SCAppConstants.TimestampFormat_14).PadRight(16, '0');
                Vids.VID_1240_PriviousTSCState.PREVIOUS_TSC_STATE = ((int)line.PriviousSCStats).ToString();
                Vids.VID_73_TSCState.TSC_STATE = ((int)line.SCStats).ToString();
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_TSC_Paused, Vids);
                scApp.ReportBLL.insertMCSReport(mcs_queue);
                S6F11SendMessage(mcs_queue);
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
            }
            return true;
        }

        public override bool S6F11SendTSCAutoCompleted()
        {
            try
            {
                VIDCollection Vids = new VIDCollection();
                Vids.VID_05_Clock.CLOCK = DateTime.Now.ToString(SCAppConstants.TimestampFormat_14).PadRight(16, '0');
                Vids.VID_1240_PriviousTSCState.PREVIOUS_TSC_STATE = ((int)line.PriviousSCStats).ToString();
                Vids.VID_73_TSCState.TSC_STATE = ((int)line.SCStats).ToString();
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_TSC_Auto_Completed, Vids);
                scApp.ReportBLL.insertMCSReport(mcs_queue);
                S6F11SendMessage(mcs_queue);
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
            }
            return true;
        }
        public override bool S6F11SendTSCAutoCompleted(List<AMCSREPORTQUEUE> reportQueues = null)
        {
            try
            {
                VIDCollection Vids = new VIDCollection();
                Vids.VID_05_Clock.CLOCK = DateTime.Now.ToString(SCAppConstants.TimestampFormat_14).PadRight(16, '0');
                Vids.VID_1240_PriviousTSCState.PREVIOUS_TSC_STATE = ((int)line.PriviousSCStats).ToString();
                Vids.VID_73_TSCState.TSC_STATE = ((int)line.SCStats).ToString();


                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_TSC_Auto_Completed, Vids);
                if (reportQueues == null)
                {
                    S6F11SendMessage(mcs_queue);
                }
                else
                {
                    reportQueues.Add(mcs_queue);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
            }
            return true;
        }
        public override bool S6F11SendTSCPauseInitiated()
        {
            try
            {
                VIDCollection Vids = new VIDCollection();
                Vids.VID_05_Clock.CLOCK = DateTime.Now.ToString(SCAppConstants.TimestampFormat_14).PadRight(16, '0');
                Vids.VID_1240_PriviousTSCState.PREVIOUS_TSC_STATE = ((int)line.PriviousSCStats).ToString();
                Vids.VID_73_TSCState.TSC_STATE = ((int)line.SCStats).ToString();
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_TSC_Pause_Initiated, Vids);
                scApp.ReportBLL.insertMCSReport(mcs_queue);
                S6F11SendMessage(mcs_queue);
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
            }
            return true;
        }

        public override bool S6F11SendTSCPauseCompleted()
        {
            try
            {
                VIDCollection Vids = new VIDCollection();
                Vids.VID_05_Clock.CLOCK = DateTime.Now.ToString(SCAppConstants.TimestampFormat_14).PadRight(16, '0');
                Vids.VID_1240_PriviousTSCState.PREVIOUS_TSC_STATE = ((int)line.PriviousSCStats).ToString();
                Vids.VID_73_TSCState.TSC_STATE = ((int)line.SCStats).ToString();
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_TSC_Pause_Completed, Vids);
                scApp.ReportBLL.insertMCSReport(mcs_queue);
                S6F11SendMessage(mcs_queue);
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
            }
            return true;
        }
        public override bool S6F11SendTSCPauseCompleted(List<AMCSREPORTQUEUE> reportQueues = null)
        {
            try
            {
                VIDCollection Vids = new VIDCollection();
                Vids.VID_05_Clock.CLOCK = DateTime.Now.ToString(SCAppConstants.TimestampFormat_14).PadRight(16, '0');
                Vids.VID_1240_PriviousTSCState.PREVIOUS_TSC_STATE = ((int)line.PriviousSCStats).ToString();
                Vids.VID_73_TSCState.TSC_STATE = ((int)line.SCStats).ToString();
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_TSC_Pause_Completed, Vids);
                if (reportQueues == null)
                {
                    S6F11SendMessage(mcs_queue);
                }
                else
                {
                    reportQueues.Add(mcs_queue);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
            }
            return true;
        }
        public override bool S6F11SendAlarmCleared(string vhid)
        {
            try
            {
                AVIDINFO vid_info = scApp.VIDBLL.getVIDInfo(vhid);
                VIDCollection vid_collection = AVIDINFO2VIDCollection(vid_info);
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Alarm_Cleared, vid_collection);
                scApp.ReportBLL.insertMCSReport(mcs_queue);
                S6F11SendMessage(mcs_queue);
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
            }
            return true;
        }
        public override bool S6F11SendAlarmCleared()
        {
            try
            {
                VIDCollection Vids = new VIDCollection();
                Vids.VID_05_Clock.CLOCK = DateTime.Now.ToString(SCAppConstants.TimestampFormat_14).PadRight(16, '0');
                Vids.VID_1230_TSAvailability.TSC_STATE_AVAILABILITY = "0";
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Alarm_Cleared, Vids);
                scApp.ReportBLL.insertMCSReport(mcs_queue);
                S6F11SendMessage(mcs_queue);
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
            }
            return true;
        }
        public override bool S6F11SendAlarmSet(string vhid)
        {
            try
            {
                AVIDINFO vid_info = scApp.VIDBLL.getVIDInfo(vhid);
                VIDCollection vid_collection = AVIDINFO2VIDCollection(vid_info);
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Alarm_Set, vid_collection);
                scApp.ReportBLL.insertMCSReport(mcs_queue);
                S6F11SendMessage(mcs_queue);
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
            }
            return true;
        }
        public override bool S6F11SendAlarmSet()
        {
            try
            {
                VIDCollection Vids = new VIDCollection();
                Vids.VID_05_Clock.CLOCK = DateTime.Now.ToString(SCAppConstants.TimestampFormat_14).PadRight(16, '0');
                Vids.VID_1230_TSAvailability.TSC_STATE_AVAILABILITY = "1";
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Alarm_Set, Vids);
                scApp.ReportBLL.insertMCSReport(mcs_queue);
                S6F11SendMessage(mcs_queue);
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
            }
            return true;
        }

        //public bool sendS6F11_PortEventState(string port_id, string port_event_state)
        //{
        //    try
        //    {
        //        VIDCollection Vids = new VIDCollection();
        //        //Vids.VID_64_EqpName.EQP_NAME = SCApplication.ServerName;

        //        Vids.VID_304_PortEventState.PESTATE = new S6F11.RPTINFO.RPTITEM.VIDITEM_304.PORTEVENTSTATE();
        //        Vids.VID_304_PortEventState.PESTATE.PORT_ID = new S6F11.RPTINFO.RPTITEM.VIDITEM_311()
        //        {
        //            PORT_ID = port_id
        //        };
        //        Vids.VID_304_PortEventState.PESTATE.PORT_EVT_STATE = new S6F11.RPTINFO.RPTITEM.VIDITEM_303()
        //        {
        //            PORT_EVT_STATE = port_event_state
        //        };
        //        AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Port_Event_State_Changed, Vids);
        //        scApp.ReportBLL.insertMCSReport(mcs_queue);
        //        S6F11SendMessage(mcs_queue);
        //    }
        //    catch (Exception ex)
        //    {
        //        LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
        //           Data: ex);
        //    }
        //    return true;
        //}
        public override bool S6F11SendTransferInitial(string cmdID, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            try
            {
                //ACMD_MCS mcs_cmd = scApp.CMDBLL.getCMD_MCSByID(cmdID);

                VIDCollection Vids = new VIDCollection();
                Vids.VID_05_Clock.CLOCK = DateTime.Now.ToString(SCAppConstants.TimestampFormat_14).PadRight(16, '0');
                Vids.VID_58_CommandID.COMMAND_ID = cmdID.Trim();
                //Vids.VID_69_TransferCommand.COMMAND_INFO = new S6F11.RPTINFO.RPTITEM.VIDITEM_59()
                //{
                //    COMMAND_ID = mcs_cmd.CMD_ID,
                //    PRIORITY = mcs_cmd.PRIORITY.ToString(),
                //    REPLACE = mcs_cmd.REPLACE.ToString() // todo kevin 要填入正確的資料
                //};
                //S6F11.RPTINFO.RPTITEM.VIDITEM_70 vIDITEM_70 = new S6F11.RPTINFO.RPTITEM.VIDITEM_70();
                //vIDITEM_70.CARRIER_ID.CARRIER_ID = mcs_cmd.CARRIER_ID;
                //vIDITEM_70.SOURCE_PORT.SOURCE_PORT = mcs_cmd.HOSTSOURCE;
                //vIDITEM_70.DEST_PORT.DEST_PORT = mcs_cmd.HOSTDESTINATION;
                //Vids.VID_69_TransferCommand.TRANSFER_INFO = vIDITEM_70;
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Transfer_Initiated, Vids);
                if (reportQueues == null)
                {
                    S6F11SendMessage(mcs_queue);
                }
                else
                {
                    reportQueues.Add(mcs_queue);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
            }
            return true;
        }




        public override bool S6F11SendTSAvailChanged(List<AMCSREPORTQUEUE> reportQueues = null)
        {
            try
            {
                VIDCollection Vids = new VIDCollection();
                Vids.VID_61_EqpName.EQP_NAME = line.LINE_ID;
                Vids.VID_201_TSAvail.TS_AVAIL = ((int)line.SCStats).ToString()=="2"?"1":"0";
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_TS_Avail_Changed, Vids);
                if (reportQueues == null)
                {
                    S6F11SendMessage(mcs_queue);
                }
                else
                {
                    reportQueues.Add(mcs_queue);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
            }
            return true;
        }


        public override bool S6F11SendTransferring(string vhID, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            try
            {
                if (!isSend()) return true;
                AVIDINFO vid_info = scApp.VIDBLL.getVIDInfo(vhID);
                VIDCollection vid_collection = AVIDINFO2VIDCollection(vid_info);
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Transferring, vid_collection);
                if (reportQueues == null)
                {
                    S6F11SendMessage(mcs_queue);
                }
                else
                {
                    reportQueues.Add(mcs_queue);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
                return false;
            }
        }
        public override bool S6F11SendVehicleArrived(string vhID, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            try
            {
                if (!isSend()) return true;
                AVIDINFO vid_info = scApp.VIDBLL.getVIDInfo(vhID);
                VIDCollection vid_collection = AVIDINFO2VIDCollection(vid_info);
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Vehicle_Arrived, vid_collection);
                if (reportQueues == null)
                {
                    S6F11SendMessage(mcs_queue);
                }
                else
                {
                    reportQueues.Add(mcs_queue);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
                return false;
            }
        }

        public override bool S6F11SendVehicleAcquireStarted(string vhID, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            try
            {
                if (!isSend()) return true;
                AVIDINFO vid_info = scApp.VIDBLL.getVIDInfo(vhID);
                VIDCollection vid_collection = AVIDINFO2VIDCollection(vid_info);
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Vehicle_Acquire_Started, vid_collection);
                if (reportQueues == null)
                {
                    S6F11SendMessage(mcs_queue);
                }
                else
                {
                    reportQueues.Add(mcs_queue);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
                return false;
            }
        }

        public override bool S6F11SendVehicleAcquireCompleted(string vhID, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            try
            {
                if (!isSend()) return true;
                AVIDINFO vid_info = scApp.VIDBLL.getVIDInfo(vhID);
                VIDCollection vid_collection = AVIDINFO2VIDCollection(vid_info);
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Vehicle_Acquire_Completed, vid_collection);
                if (reportQueues == null)
                {
                    S6F11SendMessage(mcs_queue);
                }
                else
                {
                    reportQueues.Add(mcs_queue);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
                return false;
            }
        }
        public override bool S6F11SendVehicleAcquireFailed(string vhID, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            try
            {
                if (!isSend()) return true;
                AVIDINFO vid_info = scApp.VIDBLL.getVIDInfo(vhID);
                VIDCollection vid_collection = AVIDINFO2VIDCollection(vid_info);
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Vehicle_Acquire_Failed, vid_collection);
                if (reportQueues == null)
                {
                    S6F11SendMessage(mcs_queue);
                }
                else
                {
                    reportQueues.Add(mcs_queue);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
                return false;
            }
        }

        
        public override bool S6F11SendVehicleAssigned(string vhID, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            try
            {
                if (!isSend()) return true;
                AVIDINFO vid_info = scApp.VIDBLL.getVIDInfo(vhID);
                VIDCollection vid_collection = AVIDINFO2VIDCollection(vid_info);
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Vehicle_Assigned, vid_collection);
                if (reportQueues == null)
                {
                    S6F11SendMessage(mcs_queue);
                }
                else
                {
                    reportQueues.Add(mcs_queue);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
                return false;
            }
        }

        public override bool S6F11SendVehicleDeparted(string vhID, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            try
            {
                if (!isSend()) return true;
                AVIDINFO vid_info = scApp.VIDBLL.getVIDInfo(vhID);
                VIDCollection vid_collection = AVIDINFO2VIDCollection(vid_info);
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Vehicle_Departed, vid_collection);
                if (reportQueues == null)
                {
                    S6F11SendMessage(mcs_queue);
                }
                else
                {
                    reportQueues.Add(mcs_queue);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
                return false;
            }
        }

        public override bool S6F11SendVehicleDepositStarted(string vhID, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            try
            {
                if (!isSend()) return true;
                AVIDINFO vid_info = scApp.VIDBLL.getVIDInfo(vhID);
                VIDCollection vid_collection = AVIDINFO2VIDCollection(vid_info);
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Vehicle_Deposit_Started, vid_collection);
                if (reportQueues == null)
                {
                    S6F11SendMessage(mcs_queue);
                }
                else
                {
                    reportQueues.Add(mcs_queue);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
                return false;
            }
        }

        public override bool S6F11SendVehicleDepositCompleted(string vhID, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            try
            {
                if (!isSend()) return true;
                AVIDINFO vid_info = scApp.VIDBLL.getVIDInfo(vhID);
                VIDCollection vid_collection = AVIDINFO2VIDCollection(vid_info);
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Vehicle_Deposit_Completed, vid_collection);
                if (reportQueues == null)
                {
                    S6F11SendMessage(mcs_queue);
                }
                else
                {
                    reportQueues.Add(mcs_queue);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
                return false;
            }
        }

        public override bool S6F11SendVehicleDepositFailed(string vhID, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            try
            {
                if (!isSend()) return true;
                AVIDINFO vid_info = scApp.VIDBLL.getVIDInfo(vhID);
                VIDCollection vid_collection = AVIDINFO2VIDCollection(vid_info);
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Vehicle_Deposit_Failed, vid_collection);
                if (reportQueues == null)
                {
                    S6F11SendMessage(mcs_queue);
                }
                else
                {
                    reportQueues.Add(mcs_queue);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
                return false;
            }
        }
        public override bool S6F11SendCarrierInstalled(string vhID, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            try
            {
                if (!isSend()) return true;
                AVIDINFO vid_info = scApp.VIDBLL.getVIDInfo(vhID);
                VIDCollection vid_collection = AVIDINFO2VIDCollection(vid_info);
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Carrier_Installed, vid_collection);
                if (reportQueues == null)
                {
                    S6F11SendMessage(mcs_queue);
                }
                else
                {
                    reportQueues.Add(mcs_queue);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
                return false;
            }
        }


        public override bool S6F11SendCarrierInstalledWithIDRead(string vhID, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            try
            {
                if (!isSend()) return true;
                AVIDINFO vid_info = scApp.VIDBLL.getVIDInfo(vhID);
                VIDCollection vid_collection = AVIDINFO2VIDCollection(vid_info);
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Carrier_Installed_With_IDReadError, vid_collection);
                if (reportQueues == null)
                {
                    S6F11SendMessage(mcs_queue);
                }
                else
                {
                    reportQueues.Add(mcs_queue);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
                return false;
            }
        }
        public override bool S6F11SendCarrierRemoved(string vhID, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            try
            {
                if (!isSend()) return true;
                AVIDINFO vid_info = scApp.VIDBLL.getVIDInfo(vhID);
                VIDCollection vid_collection = AVIDINFO2VIDCollection(vid_info);
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Carrier_Removed, vid_collection);
                if (reportQueues == null)
                {
                    S6F11SendMessage(mcs_queue);
                }
                else
                {
                    reportQueues.Add(mcs_queue);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
                return false;
            }
        }

        public override bool S6F11SendVehicleUnassinged(string vhID, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            try
            {
                if (!isSend()) return true;
                AVIDINFO vid_info = scApp.VIDBLL.getVIDInfo(vhID);
                VIDCollection vid_collection = AVIDINFO2VIDCollection(vid_info);
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Vehicle_Unassigned, vid_collection);
                if (reportQueues == null)
                {
                    S6F11SendMessage(mcs_queue);
                }
                else
                {
                    reportQueues.Add(mcs_queue);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
                return false;
            }
        }

        public override bool S6F11SendTransferCompleted(string vhID, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            try
            {
                if (!isSend()) return true;
                AVIDINFO vid_info = scApp.VIDBLL.getVIDInfo(vhID);
                VIDCollection vid_collection = AVIDINFO2VIDCollection(vid_info);
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Transfer_Completed, vid_collection);
                if (reportQueues == null)
                {
                    S6F11SendMessage(mcs_queue);
                }
                else
                {
                    reportQueues.Add(mcs_queue);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
                return false;
            }
        }

        public override bool S6F11SendTransferCompleted(ACMD_MCS CMD_MCS, AVEHICLE vh, string resultCode, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            try
            {
                if (!isSend()) return true;

                VIDCollection vid_collection = new VIDCollection();
                vid_collection.VID_05_Clock.CLOCK = DateTime.Now.ToString(SCAppConstants.TimestampFormat_14).PadRight(16, '0');
                //VID_59_CommandInfo
                vid_collection.VID_59_CommandInfo.COMMAND_ID = CMD_MCS.CMD_ID;
                vid_collection.VID_59_CommandInfo.PRIORITY = CMD_MCS.PRIORITY.ToString();
                vid_collection.VID_59_CommandInfo.REPLACE = CMD_MCS.REPLACE.ToString();
                //VID_67_TransferInfo
                vid_collection.VID_67_TransferInfo.CARRIER_INFOs[0].CARRIER_ID.CARRIER_ID = CMD_MCS.CARRIER_ID;
                vid_collection.VID_67_TransferInfo.CARRIER_INFOs[0].SOURCE_PORT.SOURCE_PORT = CMD_MCS.HOSTSOURCE;
                vid_collection.VID_67_TransferInfo.CARRIER_INFOs[0].DEST_PORT.DEST_PORT = CMD_MCS.HOSTDESTINATION;
                //vid_collection.VID_301_TransferCompleteInfo.TRANSFER_COMPLETE_INFOs[0].CARRIER_LOC_OBJ.CARRIER_LOC = "";
                string carrier_loc = "";

                if (CMD_MCS.TRANSFERSTATE >= E_TRAN_STATUS.Transferring)
                {
                    AVEHICLE carry_vh = scApp.VehicleBLL.cache.getVehicleByCSTID(CMD_MCS.CARRIER_ID);
                    if (carry_vh != null)
                        carrier_loc = carry_vh.Real_ID;
                }
                else
                {
                    carrier_loc = CMD_MCS.HOSTSOURCE;
                }
                //string carrier_loc = CMD_MCS.TRANSFERSTATE >= E_TRAN_STATUS.Transferring ?
                //                          vh == null ? "" : vh.Real_ID :
                //                          CMD_MCS.HOSTSOURCE;
                vid_collection.VID_56_CarrierLoc.CARRIER_LOC = carrier_loc;

                //VID_63_ResultCode
                vid_collection.VID_64_ResultCode.RESULT_CODE = resultCode;

                //VID_310_NearStockerPort
                //vid_collection.VID_310_NearStockerPort.NEAR_STOCKER_PORT = string.Empty;//不知道NEAR_STOCKER_PORT要填什麼 , For Kevin Wei to Confirm


                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Transfer_Completed, vid_collection);
                if (reportQueues == null)
                {
                    S6F11SendMessage(mcs_queue);
                }
                else
                {
                    reportQueues.Add(mcs_queue);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
                return false;
            }
        }

        public override bool S6F11SendTransferAbortCompleted(string vhID, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            try
            {
                if (!isSend()) return true;
                AVIDINFO vid_info = scApp.VIDBLL.getVIDInfo(vhID);
                VIDCollection vid_collection = AVIDINFO2VIDCollection(vid_info);
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Transfer_Abort_Completed, vid_collection);
                if (reportQueues == null)
                {
                    S6F11SendMessage(mcs_queue);
                }
                else
                {
                    reportQueues.Add(mcs_queue);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
                return false;
            }
        }

        public override bool S6F11SendTransferCancelCompleted(string vhID, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            try
            {
                if (!isSend()) return true;
                AVIDINFO vid_info = scApp.VIDBLL.getVIDInfo(vhID);
                VIDCollection vid_collection = AVIDINFO2VIDCollection(vid_info);
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Transfer_Cancel_Completed, vid_collection);
                if (reportQueues == null)
                {
                    S6F11SendMessage(mcs_queue);
                }
                else
                {
                    reportQueues.Add(mcs_queue);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
                return false;
            }
        }

        public override bool S6F11PortEventStateChanged(string vhID, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            try
            {
                if (!isSend()) return true;
                AVIDINFO vid_info = scApp.VIDBLL.getVIDInfo(vhID);
                VIDCollection vid_collection = AVIDINFO2VIDCollection(vid_info);
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Transfer_Abort_Completed, vid_collection);
                if (reportQueues == null)
                {
                    S6F11SendMessage(mcs_queue);
                }
                else
                {
                    reportQueues.Add(mcs_queue);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
                return false;
            }
        }

        //public bool S6F11SendUnitAlarmCleared(string unitID, string alarmID, string alarmTest, List<AMCSREPORTQUEUE> reportQueues = null)
        //{
        //    try
        //    {
        //        VIDCollection Vids = new VIDCollection();
        //        Vids.VID_01_AlarmID.ALID = alarmID;
        //        Vids.VID_1060_AlarmText.ALARM_TEXT = alarmTest;
        //        Vids.VID_904_UnitID.UNIT_ID = unitID;
        //        Vids.VID_903_ErrorCode.ERROR_CODE = alarmID;
        //        AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Unit_Error_Cleared, Vids);
        //        scApp.ReportBLL.insertMCSReport(mcs_queue);
        //        if (reportQueues == null)
        //        {
        //            S6F11SendMessage(mcs_queue);
        //        }
        //        else
        //        {
        //            reportQueues.Add(mcs_queue);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
        //           Data: ex);
        //    }
        //    return true;
        //}

        //public bool S6F11SendUnitAlarmSet(string unitID, string alarmID, string alarmTest, List<AMCSREPORTQUEUE> reportQueues = null)
        //{
        //    try
        //    {
        //        VIDCollection Vids = new VIDCollection();
        //        Vids.VID_01_AlarmID.ALID = alarmID;
        //        Vids.VID_1060_AlarmText.ALARM_TEXT = alarmTest;
        //        Vids.VID_904_UnitID.UNIT_ID = unitID;
        //        Vids.VID_903_ErrorCode.ERROR_CODE = alarmID;
        //        AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Unit_Error_Set, Vids);
        //        scApp.ReportBLL.insertMCSReport(mcs_queue);
        //        if (reportQueues == null)
        //        {
        //            S6F11SendMessage(mcs_queue);
        //        }
        //        else
        //        {
        //            reportQueues.Add(mcs_queue);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
        //           Data: ex);
        //    }
        //    return true;
        //}
        public override bool S6F11SendOperatorInitiatedAction(string vhID, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            //try
            //{
            //    AVIDINFO vid_info = scApp.VIDBLL.getVIDInfo(vhID);
            //    VIDCollection vid_collection = AVIDINFO2VIDCollection(vid_info);
            //    AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Operator_Initiated_Action, vid_collection);
            //    scApp.ReportBLL.insertMCSReport(mcs_queue);
            //    if (reportQueues == null)
            //    {
            //        S6F11SendMessage(mcs_queue);
            //    }
            //    else
            //    {
            //        reportQueues.Add(mcs_queue);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
            //       Data: ex);
            //}
            return false;
        }
        public override bool S6F11SendVehicleChargeRequest(List<AMCSREPORTQUEUE> reportQueues = null)
        {
            //try
            //{
            //    VIDCollection Vids = new VIDCollection();
            //    AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Vehicle_Charge_Request, Vids);
            //    scApp.ReportBLL.insertMCSReport(mcs_queue);
            //    if (reportQueues == null)
            //    {
            //        S6F11SendMessage(mcs_queue);
            //    }
            //    else
            //    {
            //        reportQueues.Add(mcs_queue);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
            //       Data: ex);
            //}
            return false;
        }
        public override bool S6F11SendVehicleChargeStarted(List<AMCSREPORTQUEUE> reportQueues = null)
        {
            //try
            //{
            //    VIDCollection Vids = new VIDCollection();
            //    AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Vehicle_Charge_Started, Vids);
            //    scApp.ReportBLL.insertMCSReport(mcs_queue);
            //    if (reportQueues == null)
            //    {
            //        S6F11SendMessage(mcs_queue);
            //    }
            //    else
            //    {
            //        reportQueues.Add(mcs_queue);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
            //       Data: ex);
            //}
            return false;

        }
        public override bool S6F11SendVehicleChargeCompleted(List<AMCSREPORTQUEUE> reportQueues = null)
        {
            //try
            //{
            //    VIDCollection Vids = new VIDCollection();
            //    AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Vehicle_Charge_Completed, Vids);
            //    scApp.ReportBLL.insertMCSReport(mcs_queue);
            //    if (reportQueues == null)
            //    {
            //        S6F11SendMessage(mcs_queue);
            //    }
            //    else
            //    {
            //        reportQueues.Add(mcs_queue);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
            //       Data: ex);
            //}
            return false;
        }

        public override AMCSREPORTQUEUE S6F11BulibMessage(string ceid, object vidCollection)
        {
            try
            {
                VIDCollection Vids = vidCollection as VIDCollection;
                string ceidOfname = string.Empty;
                SECSConst.CEID_Dictionary.TryGetValue(ceid, out ceidOfname);
                string ceid_name = $"CEID:[{ceidOfname}({ceid})]";
                S6F11 s6f11 = new S6F11()
                {
                    SECSAgentName = scApp.EAPSecsAgentName,
                    DATAID = "0",
                    CEID = ceid,
                    StreamFunctionName = ceid_name
                };
                string tempceid = ceid;
                if (ceid.StartsWith("0"))
                {
                    tempceid = ceid.TrimStart('0');
                }
                List<string> RPTIDs = SECSConst.DicCEIDAndRPTID[tempceid];
                s6f11.INFO.ITEM = new S6F11.RPTINFO.RPTITEM[RPTIDs.Count];

                for (int i = 0; i < RPTIDs.Count; i++)
                {
                    string rpt_id = RPTIDs[i];
                    s6f11.INFO.ITEM[i] = new S6F11.RPTINFO.RPTITEM();
                    List<ARPTID> AVIDs = SECSConst.DicRPTIDAndVID[rpt_id];
                    List<string> VIDs = AVIDs.OrderBy(avid => avid.ORDER_NUM).Select(avid => avid.VID.Trim()).ToList();
                    s6f11.INFO.ITEM[i].RPTID = rpt_id;
                    s6f11.INFO.ITEM[i].VIDITEM = new SXFY[AVIDs.Count];
                    for (int j = 0; j < AVIDs.Count; j++)
                    {
                        string vid = VIDs[j];
                        SXFY vid_item = null;
                        switch (vid)
                        {
                            case SECSConst.VID_ActiveCarriers:
                                vid_item = Vids.VID_51_ActiveCarriers;
                                break;
                            case SECSConst.VID_ActiveTransfers:
                                vid_item = Vids.VID_52_ActiveTransfers;
                                break;
                            case SECSConst.VID_ActiveVehicles:
                                vid_item = Vids.VID_53_ActiveVehicles;
                                break;
                            case SECSConst.VID_AlarmID:
                                vid_item = Vids.VID_01_AlarmID;
                                break;
                            case SECSConst.VID_AlarmText:
                                vid_item = Vids.VID_1060_AlarmText;
                                break;
                            case SECSConst.VID_AlarmLoc:
                                vid_item = Vids.VID_1070_AlarmLoc;
                                break;
                            case SECSConst.VID_AlarmEnabled:
                                vid_item = Vids.VID_03_AlarmEnabled;
                                break;
                            case SECSConst.VID_AlarmSet:
                                vid_item = Vids.VID_04_AlarmSet;
                                break;
                            case SECSConst.VID_AllVehiclePassCountInfo:
                                vid_item = Vids.VID_310_AllVehiclePassCountInfo;
                                break;
                            case SECSConst.VID_AssignMode:
                                vid_item = Vids.VID_302_AssignMode;
                                break;
                            case SECSConst.VID_CarrierID:
                                vid_item = Vids.VID_54_CarrierID;
                                break;
                            case SECSConst.VID_CarrierInfo:
                                vid_item = Vids.VID_55_CarrierInfo;
                                break;
                            case SECSConst.VID_CarrierLoc:
                                vid_item = Vids.VID_56_CarrierLoc;
                                break;
                            case SECSConst.VID_Clock:
                                vid_item = Vids.VID_05_Clock;
                                break;
                            case SECSConst.VID_CommandName:
                                vid_item = Vids.VID_57_CommandName;
                                break;
                            case SECSConst.VID_CommandID:
                                vid_item = Vids.VID_58_CommandID;
                                break;
                            case SECSConst.VID_CommandInfo:
                                vid_item = Vids.VID_59_CommandInfo;
                                break;
                            case SECSConst.VID_ControlState:
                                vid_item = Vids.VID_06_ControlState;
                                break;
                            case SECSConst.VID_CurrentPortStatus:
                                vid_item = Vids.VID_74_CurrentPortStatus;
                                break;
                            case SECSConst.VID_CurrentLoadStatus:
                                vid_item = Vids.VID_75_CurrentLoadStatus;
                                break;
                            case SECSConst.VID_DestPort:
                                vid_item = Vids.VID_60_DestPort;
                                break;
                            case SECSConst.VID_EmptyVehicleCount:
                                vid_item = Vids.VID_304_EmptyVehicleCount;
                                break;
                            case SECSConst.VID_FrontEmptyVehicleCount:
                                vid_item = Vids.VID_305_FrontEmptyVehicleCount;
                                break;
                            case SECSConst.VID_FullVehicleCount:
                                vid_item = Vids.VID_307_FullVehicleCount;
                                break;
                            case SECSConst.VID_IDResultCode:
                                vid_item = Vids.VID_318_IDResultCode;
                                break;
                            case SECSConst.VID_IOInfo:
                                vid_item = Vids.VID_320_IOInfo;
                                break;
                            case SECSConst.VID_IOData:
                                vid_item = Vids.VID_321_IOData;
                                break;
                            case SECSConst.VID_IOUnitID:
                                vid_item = Vids.VID_322_IOUnitID;
                                break;
                            case SECSConst.VID_IOUnitState:
                                vid_item = Vids.VID_323_IOUnitState;
                                break;
                            case SECSConst.VID_PortID:
                                vid_item = Vids.VID_311_PortID;
                                break;
                            case SECSConst.VID_PortState:
                                vid_item = Vids.VID_312_PortState;
                                break;
                            case SECSConst.VID_PortInfo:
                                vid_item = Vids.VID_313_PortInfo;
                                break;
                            case SECSConst.VID_PortTransferState:
                                vid_item = Vids.VID_314_PortTransferState;
                                break;
                            case SECSConst.VID_PortLoadInfo:
                                vid_item = Vids.VID_315_PortLoadInfo;
                                break;
                            case SECSConst.VID_PortLoadState:
                                vid_item = Vids.VID_316_PortLoadState;
                                break;
                            case SECSConst.VID_Priority:
                                vid_item = Vids.VID_62_Priority;
                                break;
                            case SECSConst.VID_PriviousControlState:
                                vid_item = Vids.VID_1210_PriviousControlState;
                                break;
                            case SECSConst.VID_PriviousTSCState:
                                vid_item = Vids.VID_1240_PriviousTSCState;
                                break;
                            case SECSConst.VID_ReadCarrierID:
                                vid_item = Vids.VID_76_ReadCarrierID;
                                break;
                            case SECSConst.VID_ReadIDInfo:
                                vid_item = Vids.VID_317_ReadIDInfo;
                                break;
                            case SECSConst.VID_RearEmptyVehicleCount:
                                vid_item = Vids.VID_306_RearEmptyVehicleCount;
                                break;
                            case SECSConst.VID_Replace:
                                vid_item = Vids.VID_63_Replace;
                                break;
                            case SECSConst.VID_Result_Code:
                                vid_item = Vids.VID_64_ResultCode;
                                break;
                            case SECSConst.VID_SCUName:
                                vid_item = Vids.VID_303_SCUName;
                                break;
                            case SECSConst.VID_SourcePort:
                                vid_item = Vids.VID_65_SourcePort;
                                break;
                            case SECSConst.VID_TransferCommand:
                                vid_item = Vids.VID_66_TransferCommand;
                                break;
                            case SECSConst.VID_TransferInfo:
                                vid_item = Vids.VID_67_TransferInfo;
                                break;
                            case SECSConst.VID_TransferPort:
                                vid_item = Vids.VID_68_TransferPort;
                                break;
                            case SECSConst.VID_TransferPortList:
                                vid_item = Vids.VID_69_TransferPortList;
                                break;
                            case SECSConst.VID_TSAvail:
                                vid_item = Vids.VID_201_TSAvail;
                                break;
                            case SECSConst.VID_TSCState:
                                vid_item = Vids.VID_73_TSCState;
                                break;
                            case SECSConst.VID_TSAvailability:
                                vid_item = Vids.VID_1230_TSAvailability;
                                break;
                            case SECSConst.VID_VehicleID:
                                vid_item = Vids.VID_70_VehicleID;
                                break;
                            case SECSConst.VID_VehicleInfo:
                                vid_item = Vids.VID_71_VehicleInfo;
                                break;
                            case SECSConst.VID_VehicleState:
                                vid_item = Vids.VID_72_VehicleState;
                                break;
                            case SECSConst.VID_VehicleAuto:
                                vid_item = Vids.VID_1650_VehicleAuto;
                                break;
                            case SECSConst.VID_VehiclePassCountInfo:
                                vid_item = Vids.VID_308_VehiclePassCountInfo;
                                break;
                            case SECSConst.VID_VehiclePassTime:
                                vid_item = Vids.VID_309_VehiclePassTime;
                                break;
                            case SECSConst.VID_AlarmLevel:
                                vid_item = Vids.VID_1001_AlarmLevel;
                                break;
                            case SECSConst.VID_FlagForAlarmReport:
                                vid_item = Vids.VID_1002_FlagForAlarmReport;
                                break;
                            case SECSConst.VID_Classification:
                                vid_item = Vids.VID_1003_Classification;
                                break;
                            case SECSConst.VID_CommandType:
                                vid_item = Vids.VID_380_CommandType;
                                break;
                            case SECSConst.VID_EnrollActiveVehicles:
                                vid_item = Vids.VID_402_EnrollActiveVehicles;
                                break;
                            case SECSConst.VID_EnrollVehicleInfo:
                                vid_item = Vids.VID_401_EnrollVehicleInfo;
                                break;
                            case SECSConst.VID_VehicleForkStatus:
                                vid_item = Vids.VID_403_VehicleForkStatus;
                                break;
                            case SECSConst.VID_VehicleDistance:
                                vid_item = Vids.VID_404_VehicleDistance;
                                break;
                            case SECSConst.VID_EnrollActiveCarrier:
                                vid_item = Vids.VID_405_EnrollActiveCarrier;
                                break;
                            case SECSConst.VID_EnrollVehicleCarrier:
                                vid_item = Vids.VID_406_EnrollVehicleCarrier;
                                break;
                            case SECSConst.VID_VehiclePort:
                                vid_item = Vids.VID_407_VehiclePort;
                                break;
                            case SECSConst.VID_MainStatus:
                                vid_item = Vids.VID_1660_MainStatus;
                                break;
                            case SECSConst.VID_CaplerStatus:
                                vid_item = Vids.VID_1670_CaplerStatus;
                                break;
                            case SECSConst.VID_CaplerStatus1:
                                vid_item = Vids.VID_1671_CaplerStatus1;
                                break;
                            case SECSConst.VID_CaplerStatus2:
                                vid_item = Vids.VID_1672_CaplerStatus2;
                                break;
                            case SECSConst.VID_VehicleTXStatus:
                                vid_item = Vids.VID_1700_VehicleTXStatus;
                                break;
                            case SECSConst.VID_LoadStatus:
                                vid_item = Vids.VID_1710_LoadStatus;
                                break;
                            case SECSConst.VID_EqpName:
                                vid_item = Vids.VID_61_EqpName;
                                break;
                            case SECSConst.VID_EstablishCommunicationTimeout:
                                vid_item = Vids.VID_2_EstablishCommunicationTimeout;
                                break;
                            case SECSConst.VID_InitialCommunicationState:
                                vid_item = Vids.VID_1010_InitialCommunicationState;
                                break;
                            case SECSConst.VID_InitialControlState:
                                vid_item = Vids.VID_1020_InitialControlState;
                                break;
                            case SECSConst.VID_SoftRevision:
                                vid_item = Vids.VID_1040_SoftRevision;
                                break;
                            case SECSConst.VID_TimeFormat:
                                vid_item = Vids.VID_1050_TimeFormat;
                                break;
                            case SECSConst.VID_ModelName:
                                vid_item = Vids.VID_1030_ModelName;
                                break;

                        }
                        s6f11.INFO.ITEM[i].VIDITEM[j] = vid_item;
                    }
                }

                return BuildMCSReport
                (s6f11,
                  Vids.VID_58_CommandID.COMMAND_ID
                , Vids.VH_ID
                , Vids.VID_311_PortID.PORT_ID);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
                return null;
            }
        }
        private AMCSREPORTQUEUE BuildMCSReport(S6F11 sxfy, string cmd_id, string vh_id, string port_id)
        {
            byte[] byteArray = SCUtility.ToByteArray(sxfy);
            DateTime reportTime = DateTime.Now;
            AMCSREPORTQUEUE queue = new AMCSREPORTQUEUE()
            {
                SERIALIZED_SXFY = byteArray,
                INTER_TIME = reportTime,
                REPORT_TIME = reportTime,
                STREAMFUNCTION_NAME = string.Concat(sxfy.StreamFunction, '-', sxfy.StreamFunctionName),
                STREAMFUNCTION_CEID = sxfy.CEID,
                MCS_CMD_ID = cmd_id,
                VEHICLE_ID = vh_id,
                PORT_ID = port_id
            };
            return queue;
        }

        public override bool S6F11SendMessage(AMCSREPORTQUEUE queue)
        {
            try
            {

                LogHelper.setCallContextKey_ServiceID(CALL_CONTEXT_KEY_WORD_SERVICE_ID_MCS);

                S6F11 s6f11 = (S6F11)SCUtility.ToObject(queue.SERIALIZED_SXFY);

                S6F12 s6f12 = null;
                SXFY abortSecs = null;
                String rtnMsg = string.Empty;

                if (!isSend()) return true;


                //SCUtility.RecodeReportInfo(vh_id, mcs_cmd_id, s6f11, s6f11.CEID);
                SCUtility.secsActionRecordMsg(scApp, false, s6f11);
                TrxSECS.ReturnCode rtnCode = ISECSControl.sendRecv<S6F12>(bcfApp, s6f11, out s6f12,
                    out abortSecs, out rtnMsg, null);
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: s6f11,
                   VehicleID: queue.VEHICLE_ID,
                   XID: queue.MCS_CMD_ID);
                SCUtility.secsActionRecordMsg(scApp, false, s6f12);
                SCUtility.actionRecordMsg(scApp, s6f11.StreamFunction, line.Real_ID,
                            "sendS6F11_common.", rtnCode.ToString());
                //SCUtility.RecodeReportInfo(vh_id, mcs_cmd_id, s6f12, s6f11.CEID, rtnCode.ToString());
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: s6f12,
                   VehicleID: queue.VEHICLE_ID,
                   XID: queue.MCS_CMD_ID);

                if (rtnCode != TrxSECS.ReturnCode.Normal)
                {
                    logger_MapActionLog.Warn("Send Transfer Initiated[S6F11] Error![rtnCode={0}]", rtnCode);
                    return false;
                }
                line.CommunicationIntervalWithMCS.Restart();

                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception:");
                return false;
            }
        }

        #endregion Send

        #region Connected / Disconnection
        protected void secsConnected(object sender, SECSEventArgs e)
        {
            if (line.Secs_Link_Stat == SCAppConstants.LinkStatus.LinkOK) return;
            Dictionary<string, CommuncationInfo> dicCommunactionInfo =
                scApp.getEQObjCacheManager().CommonInfo.dicCommunactionInfo;
            if (dicCommunactionInfo.ContainsKey("MCS"))
            {
                dicCommunactionInfo["MCS"].IsConnectinoSuccess = true;
            }
            line.Secs_Link_Stat = SCAppConstants.LinkStatus.LinkOK;
            isOnlineWithMcs = true;
            line.connInfoUpdate_Connection();
            SCUtility.RecodeConnectionInfo
                ("MCS",
                SCAppConstants.RecodeConnectionInfo_Type.Connection.ToString(),
                line.StopWatch_mcsDisconnectionTime.Elapsed.TotalSeconds);

            ITimerAction timer = scApp.getBCFApplication().getTimerAction("SECSHeartBeat");
            if (timer != null && !timer.IsStarted)
            {
                timer.start();
            }

            initialWithMCS();
        }

        private void initialWithMCS()
        {
            S1F13SendEstablishCommunicationRequest();
            if (line.Host_Control_State != SCAppConstants.LineHostControlState.HostControlState.EQ_Off_line)
            {
                //S6F11SendEquiptmentOffLine();
                scApp.LineService.OfflineWithHost();
                scApp.LineService.OnlineRemoteWithHost();
            }
            //S1F1
            //S1F1SendAreYouThere();
            //S2F17
            //S2F17SendDateAndTimeRequest();
            //...
        }
        public override bool S1F13SendEstablishCommunicationRequest()
        {
            try
            {
                S1F13 s1f13 = new S1F13();
                s1f13.SECSAgentName = scApp.EAPSecsAgentName;
                //s1f13.MDLN = scApp.getEQObjCacheManager().getLine().LINE_ID.Trim();
                s1f13.MDLN = "AGVC";
                //s1f13.SOFTREV = SCApplication.getMessageString("SYSTEM_VERSION");
                s1f13.SOFTREV = "11.02";

                S1F14 s1f14 = null;
                string rtnMsg = string.Empty;
                SXFY abortSecs = null;
                SCUtility.secsActionRecordMsg(scApp, false, s1f13);

                TrxSECS.ReturnCode rtnCode = ISECSControl.sendRecv<S1F14>(bcfApp, s1f13, out s1f14, out abortSecs, out rtnMsg, null);
                SCUtility.actionRecordMsg(scApp, s1f13.StreamFunction, line.Real_ID, "Establish Communication.", rtnCode.ToString());

                if (rtnCode == TrxSECS.ReturnCode.Normal)
                {
                    SCUtility.secsActionRecordMsg(scApp, true, s1f14);
                    line.EstablishComm = true;
                    return true;
                }
                else
                {
                    line.EstablishComm = false;
                    logger.Warn("Send Establish Communication[S1F13] Error!");
                }
            }
            catch (Exception ex)
            {
                logger.Error("MESDefaultMapAction has Error[Line Name:{0}],[Error method:{1}],[Error Message:{2}", line.LINE_ID, " sendS1F13_Establish_Comm", ex.ToString());
            }
            return false;
        }
        protected void secsDisconnected(object sender, SECSEventArgs e)
        {
            if (line.Secs_Link_Stat == SCAppConstants.LinkStatus.LinkFail) return;
            //not implement
            Dictionary<string, CommuncationInfo> dicCommunactionInfo =
                scApp.getEQObjCacheManager().CommonInfo.dicCommunactionInfo;
            if (dicCommunactionInfo.ContainsKey("MCS"))
            {
                dicCommunactionInfo["MCS"].IsConnectinoSuccess = false;
            }
            isOnlineWithMcs = false;
            line.Secs_Link_Stat = SCAppConstants.LinkStatus.LinkFail;
            scApp.LineBLL.updateHostControlState(SCAppConstants.LineHostControlState.HostControlState.EQ_Off_line);
            scApp.LineService.TSCStateToNone();

            line.connInfoUpdate_Disconnection();

            SCUtility.RecodeConnectionInfo
                ("MCS",
                SCAppConstants.RecodeConnectionInfo_Type.Disconnection.ToString(),
                line.StopWatch_mcsConnectionTime.Elapsed.TotalSeconds);
        }
        #endregion Connected / Disconnection

        #region VID Info
        private VIDCollection AVIDINFO2VIDCollection(AVIDINFO vid_info)
        {
            if (vid_info == null)
                return null;
            //string carrier_loc = string.Empty;
            //string port_id = string.Empty;
            //scApp.MapBLL.getPortID(vid_info.CARRIER_LOC, out carrier_loc);
            //scApp.MapBLL.getPortID(vid_info.PORT_ID, out port_id);

            VIDCollection vid_collection = new VIDCollection();
            vid_collection.VH_ID = vid_info.EQ_ID;


            AVEHICLE vh = scApp.VehicleBLL.getVehicleByID(vid_info.EQ_ID);
            //VID_01_AlarmID
            vid_collection.VID_01_AlarmID.ALID = vid_info.ALARM_ID;

            vid_collection.VID_05_Clock.CLOCK = DateTime.Now.ToString(SCAppConstants.TimestampFormat_14).PadRight(16, '0');

            //VID_54_CarrierID
            //vid_collection.VID_54_CarrierID.CARRIER_ID = vid_info.CARRIER_ID;
            vid_collection.VID_54_CarrierID.CARRIER_ID = vid_info.MCS_CARRIER_ID;

            //VID_55_CarrierInfo
            vid_collection.VID_55_CarrierInfo.CARRIER_ID = vid_info.CARRIER_ID;
            vid_collection.VID_55_CarrierInfo.VEHICLE_ID = vh.Real_ID;
            if (vh.HAS_CST==1)
            {
                vid_collection.VID_55_CarrierInfo.CARRIER_LOC = vh.Real_ID + "-01";//北群創的CARRIER_LOC一律都在車上的Crane
            }
            else
            {
                vid_collection.VID_55_CarrierInfo.CARRIER_LOC = vid_info.SOURCEPORT;
            }

            //VID_56_CARRIER_LOC
            if (vh.HAS_CST == 1)
            {
                vid_collection.VID_56_CarrierLoc.CARRIER_LOC = vh.Real_ID + "-01";//北群創的CARRIER_LOC一律都在車上的Crane
            }
            else
            {
                vid_collection.VID_56_CarrierLoc.CARRIER_LOC = vid_info.SOURCEPORT;
            }

            //VID_58_CommandID
            vid_collection.VID_58_CommandID.COMMAND_ID = vid_info.COMMAND_ID;

            //VID_59_CommandInfo
            vid_collection.VID_59_CommandInfo.COMMAND_ID = vid_info.COMMAND_ID;
            vid_collection.VID_59_CommandInfo.PRIORITY = vid_info.PRIORITY.ToString();
            vid_collection.VID_59_CommandInfo.REPLACE = vid_info.REPLACE.ToString();

            //VID_380_CommandType
            vid_collection.VID_380_CommandType.COMMAND_TYPE = vid_info.COMMAND_TYPE;

            //VID_60_DestPort
            vid_collection.VID_60_DestPort.DEST_PORT = vid_info.DESTPORT;

            //VID_62_Priority
            vid_collection.VID_62_Priority.PRIORITY = vid_info.PRIORITY.ToString();

            //VID_63_ResultCode
            vid_collection.VID_64_ResultCode.RESULT_CODE = vid_info.RESULT_CODE.ToString();

            //VID_65_SourcePort
            vid_collection.VID_65_SourcePort.SOURCE_PORT = vid_info.SOURCEPORT;

            //VID_66_TransferCommand
            vid_collection.VID_66_TransferCommand.COMMAND_INFO.COMMAND_ID = vid_info.COMMAND_ID;
            vid_collection.VID_66_TransferCommand.COMMAND_INFO.PRIORITY = vid_info.PRIORITY.ToString();
            vid_collection.VID_66_TransferCommand.COMMAND_INFO.REPLACE = vid_info.REPLACE.ToString();
            vid_collection.VID_66_TransferCommand.TRANSFER_INFOs[0].CARRIER_INFOs[0].CARRIER_ID.CARRIER_ID = vid_info.MCS_CARRIER_ID;
            vid_collection.VID_66_TransferCommand.TRANSFER_INFOs[0].CARRIER_INFOs[0].SOURCE_PORT.SOURCE_PORT = vid_info.SOURCEPORT;
            vid_collection.VID_66_TransferCommand.TRANSFER_INFOs[0].CARRIER_INFOs[0].DEST_PORT.DEST_PORT = vid_info.DESTPORT;

            //VID_67_TransferCommand
            vid_collection.VID_67_TransferInfo.CARRIER_INFOs[0].CARRIER_ID.CARRIER_ID = vid_info.MCS_CARRIER_ID;
            vid_collection.VID_67_TransferInfo.CARRIER_INFOs[0].SOURCE_PORT.SOURCE_PORT = vid_info.SOURCEPORT;
            vid_collection.VID_67_TransferInfo.CARRIER_INFOs[0].DEST_PORT.DEST_PORT = vid_info.DESTPORT;

            //VID_68_TransferPort
            vid_collection.VID_68_TransferPort.TRANSFER_PORT = vid_info.PORT_ID;//不確定Transfer Port要填什麼 , For Kevin Wei to Confirm

            //VID_69_TransferPortList
            vid_collection.VID_69_TransferPortList.TRANSFER_PORTs[0].TRANSFER_PORT = vid_info.PORT_ID;//不確定Transfer Port要填什麼 , For Kevin Wei to Confirm

            //VID_70_VehicleID
            vid_collection.VID_70_VehicleID.VEHICLE_ID = vh.Real_ID;
            vid_collection.VID_317_ReadIDInfo.ID_RESULT_CODE = SECSConst.NorthInnoluxBarcodeReadReultMap( vh.BCRReadResult);
            //vid_collection.VID_317_ReadIDInfo.READ_CARRRIER_ID = vh.CST_ID;
            vid_collection.VID_317_ReadIDInfo.READ_CARRRIER_ID = vid_info.CARRIER_ID;
            //VID_901_AlarmText
            vid_collection.VID_1060_AlarmText.ALARM_TEXT = vid_info.ALARM_TEXT;

            return vid_collection;
        }
        #endregion VID Info

        public virtual void doInit()
        {
            string eapSecsAgentName = scApp.EAPSecsAgentName;
            reportBLL = scApp.ReportBLL;

            ISECSControl.addSECSReceivedHandler(bcfApp, eapSecsAgentName, "S1F1", S1F1ReceiveAreYouThere);
            ISECSControl.addSECSReceivedHandler(bcfApp, eapSecsAgentName, "S1F3", S1F3ReceiveSelectedEquipmentStatusRequest);
            ISECSControl.addSECSReceivedHandler(bcfApp, eapSecsAgentName, "S1F13", S1F13ReceiveEstablishCommunicationRequest);
            ISECSControl.addSECSReceivedHandler(bcfApp, eapSecsAgentName, "S1F17", S1F17ReceiveRequestOnLine);
            ISECSControl.addSECSReceivedHandler(bcfApp, eapSecsAgentName, "S1F15", S1F15ReceiveRequestOffLine);
            ISECSControl.addSECSReceivedHandler(bcfApp, eapSecsAgentName, "S2F17", S2F17ReceiveDateTimeReq);
            ISECSControl.addSECSReceivedHandler(bcfApp, eapSecsAgentName, "S2F31", S2F31ReceiveDateTimeSetReq);
            ISECSControl.addSECSReceivedHandler(bcfApp, eapSecsAgentName, "S2F33", S2F33ReceiveDefineReport);
            ISECSControl.addSECSReceivedHandler(bcfApp, eapSecsAgentName, "S2F35", S2F35ReceiveLinkEventReport);
            ISECSControl.addSECSReceivedHandler(bcfApp, eapSecsAgentName, "S2F37", S2F37ReceiveEnableDisableEventReport);
            ISECSControl.addSECSReceivedHandler(bcfApp, eapSecsAgentName, "S2F41", S2F41ReceiveHostCommand);
            ISECSControl.addSECSReceivedHandler(bcfApp, eapSecsAgentName, "S2F49", S2F49ReceiveEnhancedRemoteCommandExtension);

            ISECSControl.addSECSReceivedHandler(bcfApp, eapSecsAgentName, "S5F3", S5F3ReceiveEnableDisableAlarm);
            ISECSControl.addSECSReceivedHandler(bcfApp, eapSecsAgentName, "S5F5", S5F5ReceiveListAlarmRequest);
            ISECSControl.addSECSReceivedHandler(bcfApp, eapSecsAgentName, "S6F15", S6F15ReceiveEventReportRequest);

            ISECSControl.addSECSConnectedHandler(bcfApp, eapSecsAgentName, secsConnected);
            ISECSControl.addSECSDisconnectedHandler(bcfApp, eapSecsAgentName, secsDisconnected);


        }
        public override bool S6F11SendTransferAbortFailed(string cmdID, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            try
            {
                if (!isSend()) return true;
                AVIDINFO vid_info = scApp.VIDBLL.getVIDInfoByMCSCmdID(cmdID);
                VIDCollection vid_collection = AVIDINFO2VIDCollection(vid_info);
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Transfer_Abort_Failed, vid_collection);
                if (reportQueues == null)
                {
                    S6F11SendMessage(mcs_queue);
                }
                else
                {
                    reportQueues.Add(mcs_queue);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
                return false;
            }
        }

        public override bool S6F11SendTransferAbortInitial(string cmdID, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            try
            {
                if (!isSend()) return true;
                AVIDINFO vid_info = scApp.VIDBLL.getVIDInfoByMCSCmdID(cmdID);
                VIDCollection vid_collection = AVIDINFO2VIDCollection(vid_info);
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Transfer_Abort_Initiated, vid_collection);
                if (reportQueues == null)
                {
                    S6F11SendMessage(mcs_queue);
                }
                else
                {
                    reportQueues.Add(mcs_queue);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
                return false;
            }
        }



        public override bool S6F11SendTransferCancelFailed(string cmdID, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            try
            {
                if (!isSend()) return true;
                AVIDINFO vid_info = scApp.VIDBLL.getVIDInfoByMCSCmdID(cmdID);
                VIDCollection vid_collection = AVIDINFO2VIDCollection(vid_info);
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Transfer_Cancel_Failed, vid_collection);
                if (reportQueues == null)
                {
                    S6F11SendMessage(mcs_queue);
                }
                else
                {
                    reportQueues.Add(mcs_queue);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
                return false;
            }
        }

        public override bool S6F11SendTransferCancelInitial(string cmdID, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            try
            {
                if (!isSend()) return true;
                AVIDINFO vid_info = scApp.VIDBLL.getVIDInfoByMCSCmdID(cmdID);
                VIDCollection vid_collection = AVIDINFO2VIDCollection(vid_info);
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Transfer_Cancel_Initiated, vid_collection);
                if (reportQueues == null)
                {
                    S6F11SendMessage(mcs_queue);
                }
                else
                {
                    reportQueues.Add(mcs_queue);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
                return false;
            }
        }
        public override bool S6F11SendTransferCancelInitial(ACMD_MCS cmd, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            try
            {
                if (!isSend()) return true;
                VIDCollection vid_collection = new VIDCollection();
                vid_collection.VID_05_Clock.CLOCK = DateTime.Now.ToString(SCAppConstants.TimestampFormat_14).PadRight(16, '0');
                vid_collection.VID_58_CommandID.COMMAND_ID = cmd.CMD_ID;

                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Transfer_Cancel_Initiated, vid_collection);
                if (reportQueues == null)
                {
                    S6F11SendMessage(mcs_queue);
                }
                else
                {
                    reportQueues.Add(mcs_queue);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
                return false;
            }
        }

        public override bool S6F11SendTransferPaused(string cmdID, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            try
            {
                if (!isSend()) return true;
                AVIDINFO vid_info = scApp.VIDBLL.getVIDInfoByMCSCmdID(cmdID);
                VIDCollection vid_collection = AVIDINFO2VIDCollection(vid_info);
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Transfer_Pause, vid_collection);
                if (reportQueues == null)
                {
                    S6F11SendMessage(mcs_queue);
                }
                else
                {
                    reportQueues.Add(mcs_queue);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
                return false;
            }
        }

        public override bool S6F11SendTransferResumed(string cmdID, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            try
            {
                if (!isSend()) return true;
                AVIDINFO vid_info = scApp.VIDBLL.getVIDInfoByMCSCmdID(cmdID);
                VIDCollection vid_collection = AVIDINFO2VIDCollection(vid_info);
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Transfer_Resumed, vid_collection);
                if (reportQueues == null)
                {
                    S6F11SendMessage(mcs_queue);
                }
                else
                {
                    reportQueues.Add(mcs_queue);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
                return false;
            }
        }

        public override bool S6F11SendTransferCancelCompleted(ACMD_MCS cmd, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            try
            {
                if (!isSend()) return true;
                VIDCollection vid_collection = new VIDCollection();
                vid_collection.VID_05_Clock.CLOCK = DateTime.Now.ToString(SCAppConstants.TimestampFormat_14).PadRight(16, '0');
                vid_collection.VID_58_CommandID.COMMAND_ID = cmd.CMD_ID;
                string carrier_loc = "";
                if (cmd.TRANSFERSTATE >= E_TRAN_STATUS.Transferring)
                {
                    AVEHICLE carry_vh = scApp.VehicleBLL.cache.getVehicleByCSTID(cmd.CARRIER_ID);
                    if (carry_vh != null)
                        carrier_loc = carry_vh.Real_ID;
                }
                else
                {
                    carrier_loc = cmd.HOSTSOURCE;
                }
                vid_collection.VID_56_CarrierLoc.CARRIER_LOC = carrier_loc;
                vid_collection.VID_64_ResultCode.RESULT_CODE = SECSConst.CMD_Result_Successful;

                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Transfer_Cancel_Completed, vid_collection);
                if (reportQueues == null)
                {
                    S6F11SendMessage(mcs_queue);
                }
                else
                {
                    reportQueues.Add(mcs_queue);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AUOMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
                return false;
            }
        }

        public override bool S6F11SendVehicleInstalled(string vhID, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            if (!isSend()) return true;
            VIDCollection vid_collection = new VIDCollection();
            vid_collection.VID_05_Clock.CLOCK = DateTime.Now.ToString(SCAppConstants.TimestampFormat_14).PadRight(16, '0');
            vid_collection.VID_70_VehicleID.VEHICLE_ID = vhID;
            AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Vehicle_Installed, vid_collection);
            if (reportQueues == null)
            {
                S6F11SendMessage(mcs_queue);
            }
            else
            {
                reportQueues.Add(mcs_queue);
            }
            return true;
        }

        public override bool S6F11SendVehicleRemoved(string vhID, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            if (!isSend()) return true;
            VIDCollection vid_collection = new VIDCollection();
            vid_collection.VID_05_Clock.CLOCK = DateTime.Now.ToString(SCAppConstants.TimestampFormat_14).PadRight(16, '0');
            vid_collection.VID_70_VehicleID.VEHICLE_ID = vhID;
            AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Vehicle_Removed, vid_collection);
            if (reportQueues == null)
            {
                S6F11SendMessage(mcs_queue);
            }
            else
            {
                reportQueues.Add(mcs_queue);
            }
            return true;
        }
    }
}
