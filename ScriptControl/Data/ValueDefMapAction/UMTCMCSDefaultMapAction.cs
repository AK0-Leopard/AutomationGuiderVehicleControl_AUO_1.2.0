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
//**********************************************************************************
using com.mirle.ibg3k0.bcf.App;
using com.mirle.ibg3k0.bcf.Controller;
using com.mirle.ibg3k0.bcf.Data.ValueDefMapAction;
using com.mirle.ibg3k0.bcf.Data.VO;
using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.BLL;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.Data.SECS.UMTC;
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
    public class UMTCMCSDefaultMapAction : IBSEMDriver, IValueDefMapAction
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
                    if (line.Host_Control_State == SCAppConstants.LineHostControlState.HostControlState.Host_Offline)
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

        protected override void S2F33ReceiveDefineReport(object sender, SECSEventArgs e)
        {
            try
            {
                S2F33 s2f33 = ((S2F33)e.secsHandler.Parse<S2F33>(e));
                SCUtility.secsActionRecordMsg(scApp, true, s2f33);
                if (!isProcess(s2f33)) { return; }

                bool isSuccess = false;
                if (s2f33.RPTITEMS != null && s2f33.RPTITEMS.Length > 0)
                    isSuccess = scApp.CEIDBLL.buildReportIDAndVid(s2f33.ToDictionary());

                S2F34 s2f34 = null;
                s2f34 = new S2F34();
                s2f34.SystemByte = s2f33.SystemByte;
                s2f34.SECSAgentName = scApp.EAPSecsAgentName;
                if (!isSuccess)
                {
                    s2f34.DRACK = "4";
                }
                else
                {
                    s2f34.DRACK = "0";
                }

                TrxSECS.ReturnCode rtnCode = ISECSControl.replySECS(bcfApp, s2f34);
                SCUtility.secsActionRecordMsg(scApp, false, s2f34);


                if (rtnCode != TrxSECS.ReturnCode.Normal)
                {
                    logger.Warn("Reply EQPT S2F34 Error:{0}", rtnCode);
                }

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


                bool isSuccess = false;
                if (s2f35.RPTITEMS != null && s2f35.RPTITEMS.Length > 0)
                    isSuccess = scApp.CEIDBLL.buildCEIDAndReportID(s2f35.ToDictionary());

                S2F36 s2f36 = null;
                s2f36 = new S2F36();
                s2f36.SystemByte = s2f35.SystemByte;
                s2f36.SECSAgentName = scApp.EAPSecsAgentName;
                if (!isSuccess)
                {
                    s2f36.LRACK = "6";
                }
                else
                {
                    s2f36.LRACK = "0";
                }

                TrxSECS.ReturnCode rtnCode = ISECSControl.replySECS(bcfApp, s2f36);
                SCUtility.secsActionRecordMsg(scApp, false, s2f36);
                if (rtnCode != TrxSECS.ReturnCode.Normal)
                {
                    logger.Warn("Reply EQPT S2F36 Error:{0}", rtnCode);
                }

                SECSConst.setDicCEIDAndRPTID(scApp.CEIDBLL.loadDicCEIDAndRPTID());
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
                S2F49 s2f49 = ((S2F49)e.secsHandler.Parse<S2F49>(e));

                switch (s2f49.RCMD)
                {
                    case "TRANSFER":
                        S2F49_TRANSFER s2f49_transfer = ((S2F49_TRANSFER)e.secsHandler.Parse<S2F49_TRANSFER>(e));
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                           Data: s2f49_transfer);
                        SCUtility.secsActionRecordMsg(scApp, true, s2f49_transfer);
                        //SCUtility.RecodeReportInfo(s2f49_transfer);
                        //if (!isProcessEAP(s2f49)) { return; }

                        S2F50 s2f50 = new S2F50();
                        s2f50.SystemByte = s2f49_transfer.SystemByte;
                        s2f50.SECSAgentName = scApp.EAPSecsAgentName;
                        s2f50.HCACK = SECSConst.HCACK_Confirm;

                        string cmdID = s2f49_transfer.REPITEMS.COMMINFO.COMMAINFOVALUE.COMMANDID.CPVAL;
                        string priority = s2f49_transfer.REPITEMS.COMMINFO.COMMAINFOVALUE.PRIORITY.CPVAL;
                        string replace = s2f49_transfer.REPITEMS.COMMINFO.COMMAINFOVALUE.REPLACE.CPVAL;

                        string rtnStr = "";
                        //檢查CST Size及Glass Data


                        //string cmdID = s2f49.REPITEMS.COMMINFO.COMMAINFO.COMMANDIDINFO.CommandID;
                        string cstID = s2f49_transfer.REPITEMS.TRANINFO.TRANSFERINFOVALUE.CARRIERIDINFO.CPVAL;
                        string source = s2f49_transfer.REPITEMS.TRANINFO.TRANSFERINFOVALUE.SOUINFO.CPVAL;

                        string dest = s2f49_transfer.REPITEMS.TRANINFO.TRANSFERINFOVALUE.DESTINFO.CPVAL;

                        //檢查搬送命令

                        //s2f50.HCACK = doCheckMCSCommand(cmdID, priority, cstID, source, dest, out rtnStr);
                        s2f50.HCACK = doCheckMCSCommand(s2f49_transfer, ref s2f50, out rtnStr);
                        //s2f50.HCACK = SECSConst.HCACK_Confirm;
                        //if (s2f50.HCACK == SECSConst.HCACK_Confirm)
                        //{
                        using (TransactionScope tx = SCUtility.getTransactionScope())
                        {
                            using (DBConnection_EF con = DBConnection_EF.GetUContext())
                            {
                                bool isCreatScuess = true;
                                if (s2f50.HCACK != SECSConst.HCACK_Rejected)
                                    isCreatScuess &= scApp.CMDBLL.doCreatMCSCommand(cmdID, priority, replace, cstID, source, dest, s2f50.HCACK);
                                if (s2f50.HCACK == SECSConst.HCACK_Confirm)
                                    isCreatScuess &= scApp.SysExcuteQualityBLL.creatSysExcuteQuality(cmdID, cstID, source, dest);
                                if (isCreatScuess)
                                {
                                    TrxSECS.ReturnCode rtnCode = ISECSControl.replySECS(bcfApp, s2f50);
                                    SCUtility.secsActionRecordMsg(scApp, false, s2f50);
                                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
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
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: string.Empty,
                                          Data: rtnStr,
                                          XID: xid);
                            BCFApplication.onWarningMsg(this, new bcf.Common.LogEventArgs(rtnStr, xid));
                        }
                        break;
                    case "STAGE":
                        S2F49_STAGE s2f49_stage = ((S2F49_STAGE)e.secsHandler.Parse<S2F49_STAGE>(e));

                        S2F50 s2f50_stage = new S2F50();
                        s2f50_stage.SystemByte = s2f49_stage.SystemByte;
                        s2f50_stage.SECSAgentName = scApp.EAPSecsAgentName;
                        s2f50_stage.HCACK = SECSConst.HCACK_Confirm;

                        string source_port_id = s2f49_stage.REPITEMS.TRANSFERINFO.CPVALUE.SOURCEPORT_CP.CPVAL_ASCII;
                        TrxSECS.ReturnCode rtnCode_stage = ISECSControl.replySECS(bcfApp, s2f50_stage);
                        SCUtility.secsActionRecordMsg(scApp, false, s2f50_stage);

                        //TODO Stage
                        //將收下來的Stage命令先放到Redis上
                        //等待Timer發現後會將此命令取下來並下命令給車子去執行
                        //(此處將再考慮是要透過Timer或是開Thread來監控這件事)

                        var port = scApp.MapBLL.getPortByPortID(source_port_id);
                        AVEHICLE vh_test = scApp.VehicleBLL.findBestSuitableVhStepByStepFromAdr_New(port.ADR_ID, port.LD_VH_TYPE);
                        scApp.VehicleBLL.callVehicleToMove(vh_test, port.ADR_ID);
                        break;
                }
                line.CommunicationIntervalWithMCS.Restart();

            }
            catch (Exception ex)
            {
                logger.Error("MESDefaultMapAction has Error[Line Name:{0}],[Error method:{1}],[Error Message:{2}", line.LINE_ID, "S2F49_Receive_Remote_Command", ex);
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
            List<S2F50.CP_U1> comminfo_check_result = new List<S2F50.CP_U1>();
            List<S2F50.CP_U1> traninfo_check_result = new List<S2F50.CP_U1>();
            string command_id = s2F49_TRANSFER.REPITEMS.COMMINFO.COMMAINFOVALUE.COMMANDID.CPVAL;
            string priority = s2F49_TRANSFER.REPITEMS.COMMINFO.COMMAINFOVALUE.PRIORITY.CPVAL;
            string replace = s2F49_TRANSFER.REPITEMS.COMMINFO.COMMAINFOVALUE.REPLACE.CPVAL;

            string carrier_id = s2F49_TRANSFER.REPITEMS.TRANINFO.TRANSFERINFOVALUE.CARRIERIDINFO.CPVAL;
            string source_port_or_vh_id = s2F49_TRANSFER.REPITEMS.TRANINFO.TRANSFERINFOVALUE.SOUINFO.CPVAL;
            string dest_port = s2F49_TRANSFER.REPITEMS.TRANINFO.TRANSFERINFOVALUE.DESTINFO.CPVAL;

            //確認命令是否已經執行中
            if (isSuccess)
            {
                var cmd_obj = scApp.CMDBLL.getCMD_MCSByID(command_id);
                if (cmd_obj != null)
                {
                    check_result = $"MCS command id:{command_id} already exist.";
                    return SECSConst.HCACK_Rejected;
                }
            }
            //確認參數是否正確
            isSuccess &= checkCommandID(comminfo_check_result, s2F49_TRANSFER.REPITEMS.COMMINFO.COMMAINFOVALUE.COMMANDID.CPNAME, command_id);
            isSuccess &= checkPriorityID(comminfo_check_result, s2F49_TRANSFER.REPITEMS.COMMINFO.COMMAINFOVALUE.PRIORITY.CPNAME, priority);
            isSuccess &= checkReplace(comminfo_check_result, s2F49_TRANSFER.REPITEMS.COMMINFO.COMMAINFOVALUE.REPLACE.CPNAME, replace);

            isSuccess &= checkCarierID(traninfo_check_result, s2F49_TRANSFER.REPITEMS.TRANINFO.TRANSFERINFOVALUE.CARRIERIDINFO.CPNAME, carrier_id);
            isSuccess &= checkPortID(traninfo_check_result, s2F49_TRANSFER.REPITEMS.TRANINFO.TRANSFERINFOVALUE.SOUINFO.CPNAME, source_port_or_vh_id);
            isSuccess &= checkPortID(traninfo_check_result, s2F49_TRANSFER.REPITEMS.TRANINFO.TRANSFERINFOVALUE.DESTINFO.CPNAME, dest_port);

            List<SXFY> cep_items = new List<SXFY>();
            if (comminfo_check_result.Count > 0)
            {
                S2F50.CEPITEM comm_info_cepack = new S2F50.CEPITEM()
                {
                    NAME = s2F49_TRANSFER.REPITEMS.COMMINFO.COMMANDINFONAME,
                    CPINFO = comminfo_check_result.ToArray()
                };
                cep_items.Add(comm_info_cepack);
            }
            if (traninfo_check_result.Count > 0)
            {
                S2F50.CEPITEMS transfer_info_cepack = new S2F50.CEPITEMS();
                transfer_info_cepack.CEPINFO = new S2F50.CEPITEM[1];
                transfer_info_cepack.CEPINFO[0] = new S2F50.CEPITEM();
                transfer_info_cepack.CEPINFO[0].NAME = s2F49_TRANSFER.REPITEMS.TRANINFO.TRANSFERINFONAME;
                transfer_info_cepack.CEPINFO[0].CPINFO = traninfo_check_result.ToArray();
                cep_items.Add(transfer_info_cepack);
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
                    return SECSConst.HCACK_Rejected;
                }
            }

            //確認是否有在相同Load Port的Transfer Command且該命令狀態還沒有變成Transferring(代表還在Port上還沒搬走)
            if (isSuccess)
            {
                //M0.02 var cmd_obj = scApp.CMDBLL.getWatingCMDByFromTo(source_port_or_vh_id, dest_port);
                var cmd_obj = scApp.CMDBLL.getWatingCMD_MCSByFrom(source_port_or_vh_id);//M0.02 
                if (cmd_obj != null)
                {
                    check_result = $"MCS command id:{command_id} is same as orther mcs command id {cmd_obj.CMD_ID.Trim()} of load port.";//M0.02 
                    //M0.02 check_result = $"MCS command id:{command_id} of transfer load port is same command id:{cmd_obj.CMD_ID.Trim()}";
                    return SECSConst.HCACK_Rejected;
                }
            }

            //確認 Port是否存在
            bool source_is_a_port = scApp.PortStationBLL.OperateCatch.IsExist(source_port_or_vh_id);
            if (source_is_a_port)
            {
                isSuccess = true;
            }
            //如果不是PortID的話，則可能是VehicleID
            else
            {
                isSuccess = scApp.VehicleBLL.cache.IsVehicleExistByRealID(source_port_or_vh_id);
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
                AVEHICLE carry_vh = scApp.VehicleBLL.cache.getVehicleByRealID(source_port_or_vh_id);
                if (carry_vh.HAS_CST == 0)
                {
                    check_result = $"MCS command id:{command_id} ,vh:{source_port_or_vh_id} current no cst in vh, can't not excute unload command.[HAS_CST:{carry_vh.HAS_CST}]";
                    return SECSConst.HCACK_Command_Not_Exist;
                }
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

        private bool checkCommandID(List<S2F50.CP_U1> comminfo_check_result, string name, string value)
        {
            bool is_success = !SCUtility.isEmpty(value);
            string cepack = is_success ? SECSConst.CEPACK_No_Error : SECSConst.CEPACK_Incorrect_Value_In_CEPVAL;
            if (!is_success)
            {
                S2F50.CP_U1 info = new S2F50.CP_U1()
                {
                    CPNAME = name,
                    CEPACK = cepack
                };
                comminfo_check_result.Add(info);
            }
            return is_success;
        }
        private bool checkPriorityID(List<S2F50.CP_U1> comminfo_check_result, string name, string value)
        {
            int i_priority = 0;
            bool is_success = int.TryParse(value, out i_priority);
            string cepack = is_success ?
                             SECSConst.CEPACK_No_Error : SECSConst.CEPACK_Incorrect_Value_In_CEPVAL;
            if (!is_success)
            {
                S2F50.CP_U1 info = new S2F50.CP_U1()
                {
                    CPNAME = name,
                    CEPACK = cepack
                };
                comminfo_check_result.Add(info);
            }
            return is_success;
        }
        private bool checkReplace(List<S2F50.CP_U1> comminfo_check_result, string name, string value)
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
                string cepack = is_success ?
                                 SECSConst.CEPACK_No_Error : SECSConst.CEPACK_Incorrect_Value_In_CEPVAL;
                if (!is_success)
                {
                    S2F50.CP_U1 info = new S2F50.CP_U1()
                    {
                        CPNAME = name,
                        CEPACK = cepack
                    };
                    comminfo_check_result.Add(info);
                }
            }
            return is_success;
        }

        private bool checkCarierID(List<S2F50.CP_U1> trnasferinfo_check_result, string name, string value)
        {
            bool is_success = !SCUtility.isEmpty(value);
            string cepack = is_success ?
                            SECSConst.CEPACK_No_Error : SECSConst.CEPACK_Incorrect_Value_In_CEPVAL;
            if (!is_success)
            {
                S2F50.CP_U1 info = new S2F50.CP_U1()
                {
                    CPNAME = name,
                    CEPACK = cepack
                };
                trnasferinfo_check_result.Add(info);
            }
            return is_success;
        }
        private bool checkPortID(List<S2F50.CP_U1> trnasferinfo_check_result, string name, string value)
        {
            bool is_success = !SCUtility.isEmpty(value);
            string cepack = is_success ?
                            SECSConst.CEPACK_No_Error : SECSConst.CEPACK_Incorrect_Value_In_CEPVAL;
            if (!is_success)
            {
                S2F50.CP_U1 info = new S2F50.CP_U1()
                {
                    CPNAME = name,
                    CEPACK = cepack
                };
                trnasferinfo_check_result.Add(info);
            }
            return is_success;
        }


        protected override void S2F41ReceiveHostCommand(object sender, SECSEventArgs e)
        {
            try
            {
                S2F41 s2f41 = ((S2F41)e.secsHandler.Parse<S2F41>(e));
                SCUtility.secsActionRecordMsg(scApp, true, s2f41);
                //if (!isProcessEAP(s2f37)) { return; }

                S2F42 s2f42 = null;
                s2f42 = new S2F42();
                s2f42.SystemByte = s2f41.SystemByte;
                s2f42.SECSAgentName = scApp.EAPSecsAgentName;
                string mcs_cmd_id = string.Empty;
                bool needToResume = false;
                bool needToPause = false;
                bool canCancelCmd = false;
                bool canAbortCmd = false;
                string cancel_abort_cmd_id = string.Empty;
                switch (s2f41.RCMD)
                {
                    case SECSConst.RCMD_Resume:
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
                        break;
                    case SECSConst.RCMD_Pause:
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
                        break;
                    case SECSConst.RCMD_Abort:
                        var abort_check_result = checkHostCommandAbort(s2f41);
                        canAbortCmd = abort_check_result.isOK;
                        s2f42.HCACK = abort_check_result.checkResult;
                        cancel_abort_cmd_id = abort_check_result.cmdID;
                        break;
                    case SECSConst.RCMD_Cancel:
                        var cancel_check_result = checkHostCommandCancel(s2f41);
                        canCancelCmd = cancel_check_result.isOK;
                        s2f42.HCACK = cancel_check_result.checkResult;
                        cancel_abort_cmd_id = cancel_check_result.cmdID;
                        break;
                }
                TrxSECS.ReturnCode rtnCode = ISECSControl.replySECS(bcfApp, s2f42);
                SCUtility.secsActionRecordMsg(scApp, false, s2f42);
                if (rtnCode != TrxSECS.ReturnCode.Normal)
                {
                    logger.Warn("Reply EQPT S2F18 Error:{0}", rtnCode);
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

        private (bool isOK, string checkResult, string cmdID) checkHostCommandAbort(S2F41 s2F41)
        {
            bool is_ok = true;
            string check_result = SECSConst.HCACK_Confirm;
            string command_id = string.Empty;
            var command_id_item = s2F41.REPITEMS.Where(item => SCUtility.isMatche(item.CPNAME, SECSConst.CPNAME_CommandID)).FirstOrDefault();
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
        private (bool isOK, string checkResult, string cmdID) checkHostCommandCancel(S2F41 s2F41)
        {
            bool is_ok = true;
            string check_result = SECSConst.HCACK_Confirm;
            string command_id = string.Empty;
            var command_id_item = s2F41.REPITEMS.Where(item => SCUtility.isMatche(item.CPNAME, SECSConst.CPNAME_CommandID)).FirstOrDefault();
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

        protected override void S1F3ReceiveSelectedEquipmentStatusRequest(object sender, SECSEventArgs e)
        {
            try
            {
                S1F3 s1f3 = ((S1F3)e.secsHandler.Parse<S1F3>(e));
                SCUtility.secsActionRecordMsg(scApp, true, s1f3);
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                              Data: s1f3);

                int count = s1f3.SVID.Count();
                S1F4 s1f4 = new S1F4();
                s1f4.SECSAgentName = scApp.EAPSecsAgentName;
                s1f4.SystemByte = s1f3.SystemByte;
                s1f4.SV = new SXFY[count];
                for (int i = 0; i < count; i++)
                {
                    if (s1f3.SVID[i] == SECSConst.VID_Control_State)
                    {
                        line.CurrentStateChecked = true;
                        s1f4.SV[i] = buildControlStateVIDItem();
                    }
                    else if (s1f3.SVID[i] == SECSConst.VID_Active_Vehicles)
                    {
                        line.EnhancedVehiclesChecked = true;
                        s1f4.SV[i] = buildActiveVehiclesVIDItem();
                    }
                    else if (s1f3.SVID[i] == SECSConst.VID_Enhanced_Carriers)
                    {
                        line.EnhancedCarriersChecked = true;
                        s1f4.SV[i] = buildEnhancedCarriersVIDItem();
                    }
                    else if (s1f3.SVID[i] == SECSConst.VID_Enhanced_Transfers)
                    {
                        line.EnhancedTransfersChecked = true;
                        s1f4.SV[i] = buildEnhancedTransfersVIDItem();
                    }
                    else if (s1f3.SVID[i] == SECSConst.VID_TCS_State)
                    {
                        line.TSCStateChecked = true;
                        s1f4.SV[i] = buildTCSStateVIDItem();
                    }
                    else if (s1f3.SVID[i] == SECSConst.VID_Registered_Ports)
                    {
                        line.CurrentPortStateChecked = true;
                        s1f4.SV[i] = buildRegisteredPortsVIDItem();
                    }
                    else
                    {
                        s1f4.SV[i] = new SXFY();
                    }
                }
                TrxSECS.ReturnCode rtnCode = ISECSControl.replySECS(bcfApp, s1f4);
                SCUtility.secsActionRecordMsg(scApp, false, s1f4);
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                              Data: s1f4);
            }
            catch (Exception ex)
            {
                logger.Error("UMTCMCSDefaultMapAction has Error[Line Name:{0}],[Error method:{1}],[Error Message:{2}",
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

        private S6F11.RPTINFO.RPTITEM.VIDITEM_53 buildActiveVehiclesVIDItem()
        {
            List<AVEHICLE> vhs = scApp.getEQObjCacheManager().getAllVehicle();
            int vhs_count = vhs.Count;
            S6F11.RPTINFO.RPTITEM.VIDITEM_53 viditem_53 = new S6F11.RPTINFO.RPTITEM.VIDITEM_53();
            viditem_53.VEHICLEINFO = new S6F11.RPTINFO.RPTITEM.VIDITEM_75[vhs_count];
            for (int j = 0; j < vhs_count; j++)
            {
                viditem_53.VEHICLEINFO[j] = new S6F11.RPTINFO.RPTITEM.VIDITEM_75()
                {
                    VHINFO = new S6F11.RPTINFO.RPTITEM.VIDITEM_75.VEHICLEINFO()
                    {
                        VEHICLE_ID = vhs[j].Real_ID,
                        VEHICLE_STATE = ((int)vhs[j].State).ToString()//目前都填 Not Assigned, For Kevin Wei to Confirm
                    }
                };
            }
            return viditem_53;
        }

        private S6F11.RPTINFO.RPTITEM.VIDITEM_62 buildEnhancedCarriersVIDItem()
        {
            List<AVEHICLE> has_carry_vhs = scApp.getEQObjCacheManager().getAllVehicle().Where(vh => vh.HAS_CST == 1).ToList();
            int carry_vhs_count = has_carry_vhs.Count;
            S6F11.RPTINFO.RPTITEM.VIDITEM_62 viditem_62 = new S6F11.RPTINFO.RPTITEM.VIDITEM_62();
            viditem_62.ENHANCED_CARRIER_INFOs = new S6F11.RPTINFO.RPTITEM.VIDITEM_62.ENHANCED_CARRIER_INFO[carry_vhs_count];

            for (int k = 0; k < carry_vhs_count; k++)
            {
                AVEHICLE has_carray_vh = has_carry_vhs[k];
                AVIDINFO vid_info = scApp.VIDBLL.getVIDInfo(has_carray_vh.VEHICLE_ID);
                viditem_62.ENHANCED_CARRIER_INFOs[k] = new S6F11.RPTINFO.RPTITEM.VIDITEM_62.ENHANCED_CARRIER_INFO();
                viditem_62.ENHANCED_CARRIER_INFOs[k].CARRIER_ID_OBJ = new S6F11.RPTINFO.RPTITEM.VIDITEM_54()
                {
                    CARRIER_ID = vid_info.CARRIER_ID.Trim()
                };
                viditem_62.ENHANCED_CARRIER_INFOs[k].VEHICLE_ID_OBJ = new S6F11.RPTINFO.RPTITEM.VIDITEM_74()
                {
                    VEHICLE_ID = has_carray_vh.Real_ID
                };
                viditem_62.ENHANCED_CARRIER_INFOs[k].CARRIER_LOC_OBJ = new S6F11.RPTINFO.RPTITEM.VIDITEM_56()
                {
                    CARRIER_LOC = vid_info.CARRIER_LOC
                };
                viditem_62.ENHANCED_CARRIER_INFOs[k].INSTALL_TIME_OBJ = new S6F11.RPTINFO.RPTITEM.VIDITEM_64_2()
                {
                    INSTALL_TIME = vid_info.CARRIER_INSTALLED_TIME?.ToString(SCAppConstants.TimestampFormat_16)
                };
            }
            return viditem_62;
        }

        private S6F11.RPTINFO.RPTITEM.VIDITEM_63 buildEnhancedTransfersVIDItem()
        {
            List<ACMD_MCS> mcs_cmds = scApp.CMDBLL.loadACMD_MCSIsUnfinished();
            int cmd_count = mcs_cmds.Count;
            S6F11.RPTINFO.RPTITEM.VIDITEM_63 viditem_63 = new S6F11.RPTINFO.RPTITEM.VIDITEM_63();
            viditem_63.ENHANCED_CARRIER_INFOs = new S6F11.RPTINFO.RPTITEM.VIDITEM_63.ENHANCED_TRANSFER_COMMAND[cmd_count];

            for (int k = 0; k < cmd_count; k++)
            {
                ACMD_MCS mcs_cmd = mcs_cmds[k];
                string transfer_state = SCAppConstants.TransferState.convert2MES(mcs_cmd.TRANSFERSTATE);
                viditem_63.ENHANCED_CARRIER_INFOs[k] = new S6F11.RPTINFO.RPTITEM.VIDITEM_63.ENHANCED_TRANSFER_COMMAND();
                viditem_63.ENHANCED_CARRIER_INFOs[k].COMMAND_INFO_OBJ = new S6F11.RPTINFO.RPTITEM.VIDITEM_59()
                {
                    COMMAND_ID = mcs_cmd.CMD_ID,
                    PRIORITY = mcs_cmd.PRIORITY.ToString(),
                    REPLACE = mcs_cmd.REPLACE.ToString()//不知道Replace是什麼 , For Kevin Wei to Confirm
                };
                viditem_63.ENHANCED_CARRIER_INFOs[k].TRANSFER_STATE = new S6F11.RPTINFO.RPTITEM.VIDITEM_72_2()
                {
                    TRANSFER_STATE = transfer_state
                };
                viditem_63.ENHANCED_CARRIER_INFOs[k].TRANSFER_INFOS = new S6F11.RPTINFO.RPTITEM.VIDITEM_70[1];//每一個TransferCMD只會有單一的Transfer Info嗎?  For Kevin Wei to Confirm
                viditem_63.ENHANCED_CARRIER_INFOs[k].TRANSFER_INFOS[0] = new S6F11.RPTINFO.RPTITEM.VIDITEM_70();
                viditem_63.ENHANCED_CARRIER_INFOs[k].TRANSFER_INFOS[0].CARRIER_ID = new S6F11.RPTINFO.RPTITEM.VIDITEM_54
                {
                    CARRIER_ID = mcs_cmd.CARRIER_ID
                };
                viditem_63.ENHANCED_CARRIER_INFOs[k].TRANSFER_INFOS[0].SOURCE_PORT = new S6F11.RPTINFO.RPTITEM.VIDITEM_68
                {
                    SOURCE_PORT = mcs_cmd.HOSTSOURCE
                };
                viditem_63.ENHANCED_CARRIER_INFOs[k].TRANSFER_INFOS[0].DEST_PORT = new S6F11.RPTINFO.RPTITEM.VIDITEM_61
                {
                    DEST_PORT = mcs_cmd.HOSTDESTINATION
                };
            }
            return viditem_63;
        }

        private S6F11.RPTINFO.RPTITEM.VIDITEM_73 buildTCSStateVIDItem()
        {
            //string sc_state = SCAppConstants.LineSCState.convert2MES(line.SCStats);
            string tsc_state = ((int)line.TSC_state_machine.State).ToString();
            S6F11.RPTINFO.RPTITEM.VIDITEM_73 viditem_73 = new S6F11.RPTINFO.RPTITEM.VIDITEM_73()
            {
                TSC_STATE = tsc_state
            };

            return viditem_73;
        }

        private S6F11.RPTINFO.RPTITEM.VIDITEM_305 buildRegisteredPortsVIDItem()
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
            S6F11.RPTINFO.RPTITEM.VIDITEM_305 viditem_305 = new S6F11.RPTINFO.RPTITEM.VIDITEM_305();
            viditem_305.PORT_EVENT_STATEs = new S6F11.RPTINFO.RPTITEM.VIDITEM_304[eq_port_count];
            int eq_port_index = -1;//M0.01
            for (int j = 0; j < port_count; j++)
            {
                AEQPT eqpt = scApp.getEQObjCacheManager().getEquipmentByEQPTID(port_station[j].EQPT_ID);
                if (eqpt.Type != SCAppConstants.EqptType.Equipment) continue;
                eq_port_index++;//M0.01

                //M0.01viditem_305.PORT_EVENT_STATEs[eq_port_index] = new S6F11.RPTINFO.RPTITEM.VIDITEM_304();
                //M0.01viditem_305.PORT_EVENT_STATEs[eq_port_index].PESTATE = new S6F11.RPTINFO.RPTITEM.VIDITEM_304.PORTEVENTSTATE();
                //M0.01viditem_305.PORT_EVENT_STATEs[eq_port_index].PESTATE.PORT_ID = new S6F11.RPTINFO.RPTITEM.VIDITEM_302()
                viditem_305.PORT_EVENT_STATEs[eq_port_index] = new S6F11.RPTINFO.RPTITEM.VIDITEM_304();//M0.01
                viditem_305.PORT_EVENT_STATEs[eq_port_index].PESTATE = new S6F11.RPTINFO.RPTITEM.VIDITEM_304.PORTEVENTSTATE();//M0.01
                viditem_305.PORT_EVENT_STATEs[eq_port_index].PESTATE.PORT_ID = new S6F11.RPTINFO.RPTITEM.VIDITEM_302()//M0.01
                {
                    PORT_ID = port_station[j].PORT_ID
                };
                string port_evt_state = ((int)port_station[j].PORT_STATUS).ToString();
                //if (port_station[j].PORT_SERVICE_STATUS == ProtocolFormat.OHTMessage.PortStationServiceStatus.OutOfService)
                //{
                //    port_evt_state = ((int)ProtocolFormat.OHTMessage.PortStationStatus.Disabled).ToString();
                //}
                //else
                //{
                //    if (eqpt.EQ_Down)
                //    {
                //        port_evt_state = ((int)ProtocolFormat.OHTMessage.PortStationStatus.Down).ToString();
                //    }
                //    else
                //    {
                //        port_evt_state = ((int)port_station[j].PORT_STATUS).ToString();
                //    }
                //}
                //port_evt_state = ((int)ProtocolFormat.OHTMessage.PortStationStatus.Wait).ToString();
                //M0.01viditem_305.PORT_EVENT_STATEs[eq_port_index].PESTATE.PORT_EVT_STATE = new S6F11.RPTINFO.RPTITEM.VIDITEM_303()
                viditem_305.PORT_EVENT_STATEs[eq_port_index].PESTATE.PORT_EVT_STATE = new S6F11.RPTINFO.RPTITEM.VIDITEM_303()//M0.01
                {
                    //PORT_EVT_STATE = ((int)port_station[j].PORT_EVENT_STATE).ToString()

                    PORT_EVT_STATE = port_evt_state
                };
            }
            return viditem_305;
        }

        #endregion Build VIDItem
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
        #endregion Receive 

        #region Send
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
        public override bool S6F11SendUnitAlarmSet(string eq_id, string alid, string altx, string error_code, string desc, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            try
            {
                VIDCollection Vids = new VIDCollection();
                Vids.VID_01_AlarmID.ALID = alid;
                Vids.VID_901_AlarmText.ALARM_TEXT = altx;
                //Vids.VID_904_UnitID.UNIT_ID = unitID;
                Vids.VID_903_ErrorCode.ERROR_CODE = error_code;
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Unit_Error_Set, Vids);
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
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
            }
            return true;
        }

        public override bool S6F11SendEquiptmentOffLine()   //TODO: no RPTID
        {
            try
            {
                VIDCollection Vids = new VIDCollection();
                Vids.VID_06_ControlState.CONTROLSTATE = SCAppConstants.LineHostControlState.convert2MES(line.Host_Control_State);
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Equipment_OFF_LINE, Vids);
                scApp.ReportBLL.insertMCSReport(mcs_queue);
                S6F11SendMessage(mcs_queue);
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
            }
            return true;
        }
        public override bool S6F11SendControlStateLocal()   //TODO: no RPTID
        {
            try
            {
                VIDCollection Vids = new VIDCollection();
                Vids.VID_06_ControlState.CONTROLSTATE = SCAppConstants.LineHostControlState.convert2MES(line.Host_Control_State);
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Control_Status_Local, Vids);
                scApp.ReportBLL.insertMCSReport(mcs_queue);
                S6F11SendMessage(mcs_queue);
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
            }
            return true;
        }
        public override bool S6F11SendControlStateRemote()  //TODO: no RPTID
        {
            try
            {
                VIDCollection Vids = new VIDCollection();
                Vids.VID_06_ControlState.CONTROLSTATE = SCAppConstants.LineHostControlState.convert2MES(line.Host_Control_State);
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Control_Status_Remote, Vids);
                scApp.ReportBLL.insertMCSReport(mcs_queue);
                S6F11SendMessage(mcs_queue);
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
            }
            return true;
        }

        public override bool S6F11SendTSCAutoInitiated()    //TODO: no RPTID
        {
            try
            {
                VIDCollection Vids = new VIDCollection();
                Vids.VID_73_TSCState.TSC_STATE = ((int)line.SCStats).ToString();
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_TSC_Auto_Initiated, Vids);
                scApp.ReportBLL.insertMCSReport(mcs_queue);
                S6F11SendMessage(mcs_queue);
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
            }
            return true;
        }

        public override bool S6F11SendTSCPaused()   //TODO: no RPTID
        {
            try
            {
                VIDCollection Vids = new VIDCollection();
                Vids.VID_73_TSCState.TSC_STATE = ((int)line.SCStats).ToString();
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_TSC_Paused, Vids);
                scApp.ReportBLL.insertMCSReport(mcs_queue);
                S6F11SendMessage(mcs_queue);
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
            }
            return true;
        }

        public override bool S6F11SendTSCAutoCompleted()    //TODO: no RPTID
        {
            try
            {
                VIDCollection Vids = new VIDCollection();
                Vids.VID_73_TSCState.TSC_STATE = ((int)line.SCStats).ToString();
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_TSC_Auto_Completed, Vids);
                scApp.ReportBLL.insertMCSReport(mcs_queue);
                S6F11SendMessage(mcs_queue);
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
            }
            return true;
        }

        public override bool S6F11SendTSCPauseInitiated()   //TODO: no RPTID
        {
            try
            {
                VIDCollection Vids = new VIDCollection();
                Vids.VID_73_TSCState.TSC_STATE = ((int)line.SCStats).ToString();
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_TSC_Pause_Initiated, Vids);
                scApp.ReportBLL.insertMCSReport(mcs_queue);
                S6F11SendMessage(mcs_queue);
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
            }
            return true;
        }

        public override bool S6F11SendTSCPauseCompleted()   //TODO: no RPTID
        {
            try
            {
                VIDCollection Vids = new VIDCollection();
                Vids.VID_73_TSCState.TSC_STATE = ((int)line.SCStats).ToString();
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_TSC_Pause_Completed, Vids);
                scApp.ReportBLL.insertMCSReport(mcs_queue);
                S6F11SendMessage(mcs_queue);
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
            }
            return true;
        }

        public override bool S6F11SendAlarmCleared()    //TODO: no RPTID
        {
            try
            {
                VIDCollection Vids = new VIDCollection();
                Vids.VID_64_EqpName.EQP_NAME = line.LINE_ID;
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Alarm_Cleared, Vids);
                scApp.ReportBLL.insertMCSReport(mcs_queue);
                S6F11SendMessage(mcs_queue);
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
            }
            return true;
        }
        public override bool S6F11SendAlarmCleared(string vhid) //TODO: no RPTID
        {
            try
            {
                AVIDINFO vid_info = scApp.VIDBLL.getVIDInfo(vhid);
                VIDCollection vid_collection = AVIDINFO2VIDCollection(vid_info);
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Alarm_Cleared, vid_info);
                scApp.ReportBLL.insertMCSReport(mcs_queue);
                S6F11SendMessage(mcs_queue);
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
            }
            return true;
        }
        public override bool S6F11SendAlarmSet()    //TODO: no RPTID
        {
            try
            {
                VIDCollection Vids = new VIDCollection();
                Vids.VID_64_EqpName.EQP_NAME = line.LINE_ID;
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Alarm_Set, Vids);
                scApp.ReportBLL.insertMCSReport(mcs_queue);
                S6F11SendMessage(mcs_queue);
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
            }
            return true;
        }

        public override bool S6F11SendAlarmSet(string vhid) //TODO: no RPTID
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
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
            }
            return true;
        }
        public bool sendS6F11_PortEventState(string port_id, string port_event_state)
        {
            try
            {
                VIDCollection Vids = new VIDCollection();
                //Vids.VID_64_EqpName.EQP_NAME = SCApplication.ServerName;

                Vids.VID_304_PortEventState.PESTATE = new S6F11.RPTINFO.RPTITEM.VIDITEM_304.PORTEVENTSTATE();
                Vids.VID_304_PortEventState.PESTATE.PORT_ID = new S6F11.RPTINFO.RPTITEM.VIDITEM_302()
                {
                    PORT_ID = port_id
                };
                Vids.VID_304_PortEventState.PESTATE.PORT_EVT_STATE = new S6F11.RPTINFO.RPTITEM.VIDITEM_303()
                {
                    PORT_EVT_STATE = port_event_state
                };
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Port_Event_State_Changed, Vids);
                scApp.ReportBLL.insertMCSReport(mcs_queue);
                S6F11SendMessage(mcs_queue);
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
            }
            return true;
        }
        public override bool S6F11SendTSAvailChanged()
        {
            try
            {
                //VIDCollection Vids = new VIDCollection();
                //Vids.VID_61_EqpName.EQP_NAME = line.LINE_ID;
                //Vids.VID_201_TSAvail.TS_AVAIL = ((int)line.SCStats).ToString() == "2" ? "0" : "1";
                //AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_TS_Avail_Changed, Vids);
                //scApp.ReportBLL.insertMCSReport(mcs_queue);
                //S6F11SendMessage(mcs_queue);
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
            }
            return true;
        }
        public override bool S6F11SendTransferInitial(string cmdID, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            try
            {
                ACMD_MCS mcs_cmd = scApp.CMDBLL.getCMD_MCSByID(cmdID);

                VIDCollection Vids = new VIDCollection();
                Vids.VID_69_TransferCommand.COMMAND_INFO = new S6F11.RPTINFO.RPTITEM.VIDITEM_59()
                {
                    COMMAND_ID = mcs_cmd.CMD_ID,
                    PRIORITY = mcs_cmd.PRIORITY.ToString(),
                    REPLACE = mcs_cmd.REPLACE.ToString() // todo kevin 要填入正確的資料
                };
                S6F11.RPTINFO.RPTITEM.VIDITEM_70 vIDITEM_70 = new S6F11.RPTINFO.RPTITEM.VIDITEM_70();
                vIDITEM_70.CARRIER_ID.CARRIER_ID = mcs_cmd.CARRIER_ID;
                vIDITEM_70.SOURCE_PORT.SOURCE_PORT = mcs_cmd.HOSTSOURCE;
                vIDITEM_70.DEST_PORT.DEST_PORT = mcs_cmd.HOSTDESTINATION;
                Vids.VID_69_TransferCommand.TRANSFER_INFO = vIDITEM_70;
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
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
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
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
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
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
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
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
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
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
                return false;
            }
        }
        public override bool S6F11SendVehicleAcquireFailed(string vhID, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            //try
            //{
            //    if (!isSend()) return true;
            //    AVIDINFO vid_info = scApp.VIDBLL.getVIDInfo(vhID);
            //    VIDCollection vid_collection = AVIDINFO2VIDCollection(vid_info);
            //    AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Vehicle_Acquire_Failed, vid_collection);
            //    if (reportQueues == null)
            //    {
            //        S6F11SendMessage(mcs_queue);
            //    }
            //    else
            //    {
            //        reportQueues.Add(mcs_queue);
            //    }
            //    return true;
            //}
            //catch (Exception ex)
            //{
            //    LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
            //       Data: ex);
            //    return false;
            //}
            return false;
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
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
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
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
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
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
                return false;
            }
        }
        public override bool S6F11SendTSCPauseCompleted(List<AMCSREPORTQUEUE> reportQueues = null)  //TODO: no RPTID
        {
            try
            {
                VIDCollection Vids = new VIDCollection();
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
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
            }
            return true;
        }
        public override bool S6F11SendVehicleDepositFailed(string vhID, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            return false;
        }
        public override bool S6F11SendTSCAutoCompleted(List<AMCSREPORTQUEUE> reportQueues = null)   //TODO: no RPTID
        {
            try
            {
                VIDCollection Vids = new VIDCollection();
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
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
            }
            return true;
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
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
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
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
                return false;
            }
        }

        public override bool S6F11SendTSAvailChanged(List<AMCSREPORTQUEUE> reportQueues = null)
        {
            try
            {
                //VIDCollection Vids = new VIDCollection();
                //Vids.VID_61_EqpName.EQP_NAME = line.LINE_ID;
                //Vids.VID_201_TSAvail.TS_AVAIL = ((int)line.SCStats).ToString() == "2" ? "0" : "1";
                //AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_TS_Avail_Changed, Vids);
                //if (reportQueues == null)
                //{
                //    S6F11SendMessage(mcs_queue);
                //}
                //else
                //{
                //    reportQueues.Add(mcs_queue);
                //}
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
            }
            return true;
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
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
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
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
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
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
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
                //VID_59_CommandInfo
                vid_collection.VID_59_CommandInfo.COMMAND_ID = CMD_MCS.CMD_ID;
                vid_collection.VID_59_CommandInfo.PRIORITY = CMD_MCS.PRIORITY.ToString();
                vid_collection.VID_59_CommandInfo.REPLACE = CMD_MCS.REPLACE.ToString();

                vid_collection.VID_301_TransferCompleteInfo.TRANSFER_COMPLETE_INFOs[0].TRANSFER_INFO_OBJ.CARRIER_ID.CARRIER_ID = CMD_MCS.CARRIER_ID;
                vid_collection.VID_301_TransferCompleteInfo.TRANSFER_COMPLETE_INFOs[0].TRANSFER_INFO_OBJ.SOURCE_PORT.SOURCE_PORT = CMD_MCS.HOSTSOURCE;
                vid_collection.VID_301_TransferCompleteInfo.TRANSFER_COMPLETE_INFOs[0].TRANSFER_INFO_OBJ.DEST_PORT.DEST_PORT = CMD_MCS.HOSTDESTINATION;
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
                    bool is_source_vh = scApp.VehicleBLL.cache.getVehicleByRealID(CMD_MCS.HOSTSOURCE) != null;
                    if (is_source_vh)
                    {
                        carrier_loc = tryFindCarrierLastLocation(CMD_MCS, vh);
                    }
                    else
                    {
                        carrier_loc = CMD_MCS.HOSTSOURCE;
                    }
                }
                //string carrier_loc = CMD_MCS.TRANSFERSTATE >= E_TRAN_STATUS.Transferring ?
                //                          vh == null ? "" : vh.Real_ID :
                //                          CMD_MCS.HOSTSOURCE;
                vid_collection.VID_301_TransferCompleteInfo.TRANSFER_COMPLETE_INFOs[0].CARRIER_LOC_OBJ.CARRIER_LOC = carrier_loc;

                //VID_67_ResultCode
                vid_collection.VID_67_ResultCode.RESULT_CODE = resultCode;

                //VID_310_NearStockerPort
                vid_collection.VID_310_NearStockerPort.NEAR_STOCKER_PORT = string.Empty;//不知道NEAR_STOCKER_PORT要填什麼 , For Kevin Wei to Confirm


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
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
                return false;
            }
        }

        private string tryFindCarrierLastLocation(ACMD_MCS CMD_MCS, AVEHICLE vh)
        {
            string carrier_loc;
            //如果該筆命令的Source 就是VH的話(即為要讓車子執行Unload)，
            //要去查找該CST他的上一筆命令的終點是在哪邊，
            //把他當作本次的命令結束時的carrier loc的最後位置。
            //如果最後還是找不到，就直接填入在車上。
            ACMD_MCS last_finish_cmd_mcs = scApp.CMDBLL.getLastFinishCmd_MCSByCSTID(CMD_MCS.CARRIER_ID, CMD_MCS.CMD_ID);
            if (last_finish_cmd_mcs != null)
            {
                carrier_loc = SCUtility.Trim(last_finish_cmd_mcs.HOSTDESTINATION, true);
            }
            else
            {
                HCMD_MCS last_finish_hcmd_mcd = scApp.CMDBLL.getLastFinishHCmd_MCSByCSTID(CMD_MCS.CARRIER_ID, CMD_MCS.CMD_ID);
                if (last_finish_hcmd_mcd != null)
                {
                    carrier_loc = SCUtility.Trim(last_finish_hcmd_mcd.HOSTDESTINATION, true);
                }
                else
                {
                    carrier_loc = vh.Real_ID;
                }
            }
            return carrier_loc;
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
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
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
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
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
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
                return false;
            }
        }

        public bool S6F11SendUnitAlarmCleared(string unitID, string alarmID, string alarmTest, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            try
            {
                VIDCollection Vids = new VIDCollection();
                Vids.VID_01_AlarmID.ALID = alarmID;
                Vids.VID_901_AlarmText.ALARM_TEXT = alarmTest;
                Vids.VID_904_UnitID.UNIT_ID = unitID;
                Vids.VID_903_ErrorCode.ERROR_CODE = alarmID;
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Unit_Error_Cleared, Vids);
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
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
            }
            return true;
        }

        public bool S6F11SendUnitAlarmSet(string unitID, string alarmID, string alarmTest, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            try
            {
                VIDCollection Vids = new VIDCollection();
                Vids.VID_01_AlarmID.ALID = alarmID;
                Vids.VID_901_AlarmText.ALARM_TEXT = alarmTest;
                Vids.VID_904_UnitID.UNIT_ID = unitID;
                Vids.VID_903_ErrorCode.ERROR_CODE = alarmID;
                AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Unit_Error_Set, Vids);
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
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
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
            //    LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
            //       Data: ex);
            //}
            return false;
        }
        public bool S6F11SendAlarmEvent(string eq_id, string ceid, string alid, string cmd_id, string altx, string alarmLvl, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            //try
            //{
            //    VIDCollection Vids = new VIDCollection();
            //    Vids.VID_01_AlarmID.ALID = alid;
            //    Vids.VID_901_AlarmText.ALARM_TEXT = altx;
            //    //Vids.VID_904_UnitID.UNIT_ID = unitID;
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
            //    LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
            //       Data: ex);
            //}
            return false;
        }
        public override bool S6F11SendOperatorInitiatedAction(ACMD_MCS mcsCmd, List<AMCSREPORTQUEUE> reportQueues = null)
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
            //    LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
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
            //    LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
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
            //    LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
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
            //    LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
            //       Data: ex);
            //}
            return false;
        }
        public override AMCSREPORTQUEUE S6F11BulibMessage(string ceid, object vidCollection, List<string> rptids = null)
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
                    CEID = ceid,
                    StreamFunctionName = ceid_name
                };
                List<string> RPTIDs = SECSConst.DicCEIDAndRPTID[ceid];
                s6f11.INFO.ITEM = new S6F11.RPTINFO.RPTITEM[RPTIDs.Count];

                for (int i = 0; i < RPTIDs.Count; i++)
                {
                    string rpt_id = RPTIDs[i];
                    s6f11.INFO.ITEM[i] = new S6F11.RPTINFO.RPTITEM();
                    List<string> VIDs;
                    if (rpt_id.Trim() != "0")
                    {
                        List<ARPTID> AVIDs = SECSConst.DicRPTIDAndVID[rpt_id];
                        VIDs = AVIDs.OrderBy(avid => avid.ORDER_NUM).Select(avid => avid.VID.Trim()).ToList();
                    }
                    else
                    {
                        //TODO: need to hard code
                        VIDs = GetVIDWithoutRPTID(ceid);
                    }

                    s6f11.INFO.ITEM[i].RPTID = rpt_id.Trim() != "0" ? rpt_id : String.Empty;
                    s6f11.INFO.ITEM[i].VIDITEM = new SXFY[VIDs.Count];
                    for (int j = 0; j < VIDs.Count; j++)
                    {
                        string vid = VIDs[j];
                        SXFY vid_item = null;
                        switch (vid)
                        {
                            case SECSConst.VID_AlarmID:
                                vid_item = Vids.VID_01_AlarmID;
                                break;
                            case SECSConst.VID_Establish_Communications_Timeout:
                                vid_item = Vids.VID_02_EstablishCommunicationsTimeout;
                                break;
                            case SECSConst.VID_Alarm_Enabled:
                                vid_item = Vids.VID_03_AlarmEnabled;
                                break;
                            case SECSConst.VID_Alarm_Set:
                                vid_item = Vids.VID_04_AlarmSet;
                                break;
                            case SECSConst.VID_Clock:
                                vid_item = Vids.VID_05_Clock;
                                break;
                            case SECSConst.VID_Control_State:
                                vid_item = Vids.VID_06_ControlState;
                                break;
                            case SECSConst.VID_Events_Enabled:
                                vid_item = Vids.VID_07_EventEnabled;
                                break;
                            case SECSConst.VID_Active_Carriers:
                                vid_item = Vids.VID_51_ActiveCarriers;
                                break;
                            case SECSConst.VID_Active_Transfers:
                                vid_item = Vids.VID_52_ActiveTransfers;
                                break;
                            case SECSConst.VID_Active_Vehicles:
                                vid_item = Vids.VID_53_ActiveVehicles;
                                break;
                            case SECSConst.VID_Carrier_ID:
                                vid_item = Vids.VID_54_CarrierID;
                                break;
                            case SECSConst.VID_CarrierInfo:
                                vid_item = Vids.VID_55_CarrierInfo;
                                break;
                            case SECSConst.VID_Carrier_Loc:
                                vid_item = Vids.VID_56_CarrierLoc;
                                break;
                            case SECSConst.VID_Command_Name:
                                vid_item = Vids.VID_57_CommandName;
                                break;
                            case SECSConst.VID_Command_ID:
                                vid_item = Vids.VID_58_CommandID;
                                break;
                            case SECSConst.VID_Command_Info:
                                vid_item = Vids.VID_59_CommandInfo;
                                break;
                            case SECSConst.VID_Command_Type:
                                vid_item = Vids.VID_60_CommandType;
                                break;
                            case SECSConst.VID_Destination_Port:
                                vid_item = Vids.VID_61_DestPort;
                                break;
                            case SECSConst.VID_Enhanced_Carriers:
                                vid_item = Vids.VID_62_EnhancedCarriers;
                                break;
                            case SECSConst.VID_Enhanced_Transfers:
                                vid_item = Vids.VID_63_EnhancedTransfers;
                                break;
                            case SECSConst.VID_EqpName:
                                vid_item = Vids.VID_64_EqpName;
                                break;
                            case SECSConst.VID_Priority:
                                vid_item = Vids.VID_65_Priority;
                                break;
                            case SECSConst.VID_Replace:
                                vid_item = Vids.VID_66_Replace;
                                break;
                            case SECSConst.VID_Result_Code:
                                vid_item = Vids.VID_67_ResultCode;
                                break;
                            case SECSConst.VID_Source_Port:
                                vid_item = Vids.VID_68_SourcePort;
                                break;
                            case SECSConst.VID_Transfer_Command:
                                vid_item = Vids.VID_69_TransferCommand;
                                break;
                            case SECSConst.VID_Transfer_Info:
                                vid_item = Vids.VID_70_TransferInfo;
                                break;
                            case SECSConst.VID_Transfer_Port:
                                vid_item = Vids.VID_71_TransferPort;
                                break;
                            case SECSConst.VID_Transfer_Port_List:
                                vid_item = Vids.VID_72_TransferPortList;
                                break;
                            case SECSConst.VID_TCS_State:
                                vid_item = Vids.VID_73_TSCState;
                                break;
                            case SECSConst.VID_Vehicle_ID:
                                vid_item = Vids.VID_74_VehicleID;
                                break;
                            case SECSConst.VID_Vehicle_Info:
                                vid_item = Vids.VID_75_VehicleInfo;
                                break;
                            case SECSConst.VID_Vehicle_State:
                                vid_item = Vids.VID_76_VehicleState;
                                break;
                            case SECSConst.VID_Transfer_Complete_Info:
                                vid_item = Vids.VID_301_TransferCompleteInfo;
                                break;
                            case SECSConst.VID_Port_ID:
                                vid_item = Vids.VID_302_PortID;
                                break;
                            case SECSConst.VID_Port_Evt_State:
                                vid_item = Vids.VID_303_PortEvtState;
                                break;
                            case SECSConst.VID_Port_Event_State:
                                vid_item = Vids.VID_304_PortEventState;
                                break;
                            case SECSConst.VID_Registered_Ports:
                                vid_item = Vids.VID_305_RegisteredPorts;
                                break;
                            case SECSConst.VID_Near_Stocker_Port:
                                vid_item = Vids.VID_310_NearStockerPort;
                                break;
                            case SECSConst.VID_Current_Node:
                                vid_item = Vids.VID_311_CurrentNode;
                                break;
                            case SECSConst.VID_Alarm_Text:
                                vid_item = Vids.VID_901_AlarmText;
                                break;
                            case SECSConst.VID_Charger_ID:
                                vid_item = Vids.VID_902_ChargerID;
                                break;
                            case SECSConst.VID_Error_Code:
                                vid_item = Vids.VID_903_ErrorCode;
                                break;
                            case SECSConst.VID_Unit_ID:
                                vid_item = Vids.VID_904_UnitID;
                                break;

                        }
                        s6f11.INFO.ITEM[i].VIDITEM[j] = vid_item;
                    }
                }

                return BuildMCSReport
                (s6f11,
                  Vids.VID_58_CommandID.COMMAND_ID
                , Vids.VH_ID
                , Vids.VID_302_PortID.PORT_ID);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
                return null;
            }
        }

        private List<string> GetVIDWithoutRPTID(string ceid)
        {
            List<string> VIDs = new List<string>();
            switch (ceid)
            {
                case SECSConst.CEID_Equipment_OFF_LINE:
                case SECSConst.CEID_Control_Status_Local:
                case SECSConst.CEID_Control_Status_Remote:
                    VIDs.Add(SECSConst.VID_Control_State);
                    break;
                case SECSConst.CEID_Alarm_Cleared:
                case SECSConst.CEID_Alarm_Set:
                    VIDs.Add(SECSConst.VID_Alarm_Set);
                    break;
                case SECSConst.CEID_TSC_Auto_Completed:
                case SECSConst.CEID_TSC_Auto_Initiated:
                case SECSConst.CEID_TSC_Pause_Completed:
                case SECSConst.CEID_TSC_Paused:
                case SECSConst.CEID_TSC_Pause_Initiated:
                    VIDs.Add(SECSConst.VID_TCS_State);
                    break;
                default:
                    break;
            }

            return VIDs;
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
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: s6f11,
                   VehicleID: queue.VEHICLE_ID,
                   XID: queue.MCS_CMD_ID);
                SCUtility.secsActionRecordMsg(scApp, false, s6f12);
                SCUtility.actionRecordMsg(scApp, s6f11.StreamFunction, line.Real_ID,
                            "sendS6F11_common.", rtnCode.ToString());
                //SCUtility.RecodeReportInfo(vh_id, mcs_cmd_id, s6f12, s6f11.CEID, rtnCode.ToString());
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
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

            //VID_54_CarrierID
            //vid_collection.VID_54_CarrierID.CARRIER_ID = vid_info.CARRIER_ID;
            vid_collection.VID_54_CarrierID.CARRIER_ID = vid_info.MCS_CARRIER_ID;

            //VID_55_CarrierInfo
            vid_collection.VID_55_CarrierInfo.CARRIER_ID = vid_info.CARRIER_ID;
            vid_collection.VID_55_CarrierInfo.VEHICLE_ID = vh.Real_ID;
            vid_collection.VID_55_CarrierInfo.CARRIER_LOC = vid_info.CARRIER_LOC;

            //VID_58_CommandID
            vid_collection.VID_58_CommandID.COMMAND_ID = vid_info.COMMAND_ID;

            //VID_59_CommandInfo
            vid_collection.VID_59_CommandInfo.COMMAND_ID = vid_info.COMMAND_ID;
            vid_collection.VID_59_CommandInfo.PRIORITY = vid_info.PRIORITY.ToString();
            vid_collection.VID_59_CommandInfo.REPLACE = vid_info.REPLACE.ToString();

            //VID_60_CommandType
            vid_collection.VID_60_CommandType.COMMAND_TYPE = vid_info.COMMAND_TYPE;

            //VID_61_DestPort
            vid_collection.VID_61_DestPort.DEST_PORT = vid_info.DESTPORT;

            //VID_65_Priority
            vid_collection.VID_65_Priority.PRIORITY = vid_info.PRIORITY.ToString();

            //VID_67_ResultCode
            vid_collection.VID_67_ResultCode.RESULT_CODE = vid_info.RESULT_CODE.ToString();

            //VID_68_SourcePort
            vid_collection.VID_68_SourcePort.SOURCE_PORT = vid_info.SOURCEPORT;

            //VID_69_TransferCommand
            vid_collection.VID_69_TransferCommand.COMMAND_INFO.COMMAND_ID = vid_info.COMMAND_ID;
            vid_collection.VID_69_TransferCommand.COMMAND_INFO.PRIORITY = vid_info.PRIORITY.ToString();
            vid_collection.VID_69_TransferCommand.COMMAND_INFO.REPLACE = vid_info.REPLACE.ToString();
            //vid_collection.VID_69_TransferCommand.TRANSFER_INFOs[0].CARRIER_ID.CARRIER_ID = vid_info.CARRIER_ID;
            vid_collection.VID_69_TransferCommand.TRANSFER_INFO.CARRIER_ID.CARRIER_ID = vid_info.MCS_CARRIER_ID;
            vid_collection.VID_69_TransferCommand.TRANSFER_INFO.SOURCE_PORT.SOURCE_PORT = vid_info.SOURCEPORT;
            vid_collection.VID_69_TransferCommand.TRANSFER_INFO.DEST_PORT.DEST_PORT = vid_info.DESTPORT;

            //VID_71_TransferPort
            vid_collection.VID_71_TransferPort.TRANSFER_PORT = vid_info.PORT_ID;//不確定Transfer Port要填什麼 , For Kevin Wei to Confirm

            //VID_72_TransferPortList
            vid_collection.VID_72_TransferPortList.TRANSFER_PORT_LIST[0].TRANSFER_PORT = vid_info.PORT_ID;//不確定Transfer Port要填什麼 , For Kevin Wei to Confirm

            //VID_74_VehicleID
            vid_collection.VID_74_VehicleID.VEHICLE_ID = vh.Real_ID;

            //VID_301_TransferCompleteInfo
            //vid_collection.VID_301_TransferCompleteInfo.TRANSFER_COMPLETE_INFOs[0].TRANSFER_INFO_OBJ.CARRIER_ID.CARRIER_ID = vid_info.CARRIER_ID;
            vid_collection.VID_301_TransferCompleteInfo.TRANSFER_COMPLETE_INFOs[0].TRANSFER_INFO_OBJ.CARRIER_ID.CARRIER_ID = vid_info.MCS_CARRIER_ID;
            vid_collection.VID_301_TransferCompleteInfo.TRANSFER_COMPLETE_INFOs[0].TRANSFER_INFO_OBJ.SOURCE_PORT.SOURCE_PORT = vid_info.SOURCEPORT;
            vid_collection.VID_301_TransferCompleteInfo.TRANSFER_COMPLETE_INFOs[0].TRANSFER_INFO_OBJ.DEST_PORT.DEST_PORT = vid_info.DESTPORT;
            vid_collection.VID_301_TransferCompleteInfo.TRANSFER_COMPLETE_INFOs[0].CARRIER_LOC_OBJ.CARRIER_LOC = vid_info.CARRIER_LOC;

            //VID_304_PortEventState
            vid_collection.VID_304_PortEventState.PESTATE.PORT_ID.PORT_ID = vid_info.PORT_ID;
            vid_collection.VID_304_PortEventState.PESTATE.PORT_EVT_STATE.PORT_EVT_STATE = string.Empty;//不知道Port Event State要填什麼 , For Kevin Wei to Confirm

            //VID_310_NearStockerPort
            vid_collection.VID_310_NearStockerPort.NEAR_STOCKER_PORT = string.Empty;//不知道NEAR_STOCKER_PORT要填什麼 , For Kevin Wei to Confirm

            //VID_311_NearStockerPort
            vid_collection.VID_311_CurrentNode.CURRENT_NODE = string.Empty;//不知道Current Node要填什麼 , For Kevin Wei to Confirm

            //VID_901_AlarmText
            vid_collection.VID_901_AlarmText.ALARM_TEXT = vid_info.ALARM_TEXT;

            //VID_902_ChargerID
            vid_collection.VID_902_ChargerID.CHANGER_ID = string.Empty;//不知道Charger ID要填什麼 , For Kevin Wei to Confirm

            //VID_903_ErrorCode
            vid_collection.VID_903_ErrorCode.ERROR_CODE = string.Empty;//不知道Error Code要填什麼 , For Kevin Wei to Confirm

            //VID_904_ErrorCode
            vid_collection.VID_904_UnitID.UNIT_ID = vid_info.UNIT_ID;


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
            ISECSControl.addSECSReceivedHandler(bcfApp, eapSecsAgentName, "S1F15", S1F15ReceiveRequestOffLine);
            ISECSControl.addSECSReceivedHandler(bcfApp, eapSecsAgentName, "S1F17", S1F17ReceiveRequestOnLine);

            ISECSControl.addSECSReceivedHandler(bcfApp, eapSecsAgentName, "S2F31", S2F31ReceiveDateTimeSetReq);
            ISECSControl.addSECSReceivedHandler(bcfApp, eapSecsAgentName, "S2F33", S2F33ReceiveDefineReport);
            ISECSControl.addSECSReceivedHandler(bcfApp, eapSecsAgentName, "S2F35", S2F35ReceiveLinkEventReport);
            ISECSControl.addSECSReceivedHandler(bcfApp, eapSecsAgentName, "S2F37", S2F37ReceiveEnableDisableEventReport);
            ISECSControl.addSECSReceivedHandler(bcfApp, eapSecsAgentName, "S2F41", S2F41ReceiveHostCommand);
            ISECSControl.addSECSReceivedHandler(bcfApp, eapSecsAgentName, "S2F49", S2F49ReceiveEnhancedRemoteCommandExtension);

            ISECSControl.addSECSReceivedHandler(bcfApp, eapSecsAgentName, "S5F3", S5F3ReceiveEnableDisableAlarm);
            ISECSControl.addSECSReceivedHandler(bcfApp, eapSecsAgentName, "S5F5", S5F5ReceiveListAlarmRequest);

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
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
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
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
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
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
                return false;
            }
        }
        public override bool S6F11SendTransferAbortCompleted(ACMD_MCS CMD_MCS, AVEHICLE vh, string resultCode, List<AMCSREPORTQUEUE> reportQueues = null, string _carrier_loc = null)
        {
            try
            {
                return false;
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
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
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
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

                vid_collection.VID_69_TransferCommand.COMMAND_INFO.COMMAND_ID = cmd.CMD_ID;
                vid_collection.VID_69_TransferCommand.COMMAND_INFO.PRIORITY = cmd.PRIORITY.ToString();
                vid_collection.VID_69_TransferCommand.COMMAND_INFO.REPLACE = cmd.REPLACE.ToString();
                vid_collection.VID_69_TransferCommand.TRANSFER_INFO.CARRIER_ID.CARRIER_ID = cmd.CARRIER_ID;
                vid_collection.VID_69_TransferCommand.TRANSFER_INFO.SOURCE_PORT.SOURCE_PORT = cmd.HOSTSOURCE;
                vid_collection.VID_69_TransferCommand.TRANSFER_INFO.DEST_PORT.DEST_PORT = cmd.HOSTDESTINATION;


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
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
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
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
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
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
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

                vid_collection.VID_69_TransferCommand.COMMAND_INFO.COMMAND_ID = cmd.CMD_ID;
                vid_collection.VID_69_TransferCommand.COMMAND_INFO.PRIORITY = cmd.PRIORITY.ToString();
                vid_collection.VID_69_TransferCommand.COMMAND_INFO.REPLACE = cmd.REPLACE.ToString();
                vid_collection.VID_69_TransferCommand.TRANSFER_INFO.CARRIER_ID.CARRIER_ID = cmd.CARRIER_ID;
                vid_collection.VID_69_TransferCommand.TRANSFER_INFO.SOURCE_PORT.SOURCE_PORT = cmd.HOSTSOURCE;
                vid_collection.VID_69_TransferCommand.TRANSFER_INFO.DEST_PORT.DEST_PORT = cmd.HOSTDESTINATION;


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
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
                   Data: ex);
                return false;
            }
        }

        public override bool S6F11SendVehicleInstalled(string vhID, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            if (!isSend()) return true;
            VIDCollection vid_collection = new VIDCollection();

            vid_collection.VID_74_VehicleID.VEHICLE_ID = vhID;

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

            vid_collection.VID_74_VehicleID.VEHICLE_ID = vhID;

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
        public override bool S6F11SendCarrierInstalledWithIDRead(string vhID, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            //try
            //{
            //    if (!isSend()) return true;
            //    AVIDINFO vid_info = scApp.VIDBLL.getVIDInfo(vhID);
            //    VIDCollection vid_collection = AVIDINFO2VIDCollection(vid_info);
            //    AMCSREPORTQUEUE mcs_queue = S6F11BulibMessage(SECSConst.CEID_Carrier_Installed_With_IDReadError, vid_collection);
            //    if (reportQueues == null)
            //    {
            //        S6F11SendMessage(mcs_queue);
            //    }
            //    else
            //    {
            //        reportQueues.Add(mcs_queue);
            //    }
            //    return true;
            //}
            //catch (Exception ex)
            //{
            //    LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(UMTCMCSDefaultMapAction), Device: DEVICE_NAME_MCS,
            //       Data: ex);
            return false;
            //}
        }

        public override bool S6F11SendCarrierRemoved(string vhID,string carrierID, string cmdID, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            throw new NotImplementedException();
        }

        public override bool S6F11SendCarrierInstalled(string vhID, string carrierID, string carrierLoc, string cmdID, List<AMCSREPORTQUEUE> reportQueues = null)
        {
            throw new NotImplementedException();
        }
    }
}
