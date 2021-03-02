using com.mirle.ibg3k0.bcf.Common;
using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.BLL;
using com.mirle.ibg3k0.sc.Data.VO;
using com.mirle.ibg3k0.sc.ProtocolFormat.OHTMessage;
using com.mirle.ibg3k0.Utility.ul.Data.VO;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static com.mirle.ibg3k0.sc.AVEHICLE;

namespace com.mirle.ibg3k0.sc.Service
{
    public class SouthInnoLuxLineService:LineService
    {
        NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        //private SCApplication scApp = null;
        //private ReportBLL reportBLL = null;
        //private LineBLL lineBLL = null;
        //private ALINE line = null;
        public SouthInnoLuxLineService()
        {

        }
        public override void start(SCApplication _app)
        {
            scApp = _app;
            reportBLL = _app.ReportBLL;
            lineBLL = _app.LineBLL;
            line = scApp.getEQObjCacheManager().getLine();

            line.addEventHandler(nameof(LineService), nameof(line.Currnet_Park_Type), PublishLineInfo);
            line.addEventHandler(nameof(LineService), nameof(line.Currnet_Cycle_Type), PublishLineInfo);
            line.addEventHandler(nameof(LineService), nameof(line.Secs_Link_Stat), PublishLineInfo);
            line.addEventHandler(nameof(LineService), nameof(line.Redis_Link_Stat), PublishLineInfo);
            line.addEventHandler(nameof(LineService), nameof(line.DetectionSystemExist), PublishLineInfo);
            line.addEventHandler(nameof(LineService), nameof(line.IsEarthquakeHappend), PublishLineInfo);
            line.addEventHandler(nameof(LineService), nameof(line.IsAlarmHappened), PublishLineInfo);

            //line.addEventHandler(nameof(LineService), nameof(line.CurrntVehicleModeAutoRemoteCount), PublishLineInfo);
            //line.addEventHandler(nameof(LineService), nameof(line.CurrntVehicleModeAutoLoaclCount), PublishLineInfo);
            //line.addEventHandler(nameof(LineService), nameof(line.CurrntVehicleStatusIdelCount), PublishLineInfo);
            //line.addEventHandler(nameof(LineService), nameof(line.CurrntVehicleStatusErrorCount), PublishLineInfo);
            //line.addEventHandler(nameof(LineService), nameof(line.CurrntCSTStatueTransferCount), PublishLineInfo);
            //line.addEventHandler(nameof(LineService), nameof(line.CurrntCSTStatueWaitingCount), PublishLineInfo);
            //line.addEventHandler(nameof(LineService), nameof(line.CurrntHostCommandTransferStatueAssignedCount), PublishLineInfo);
            //line.addEventHandler(nameof(LineService), nameof(line.CurrntHostCommandTransferStatueWaitingCounr), PublishLineInfo);
            line.LineStatusChange += Line_LineStatusChange;

            addVirtualVehicle();
            line.LongTimeNoCommuncation += Line_LongTimeNoCommuncation;
            line.TimerActionStart();
            //Section 的事務處理
            List<ASECTION> sections = scApp.SectionBLL.cache.GetSections();
            foreach (ASECTION section in sections)
            {
                section.VehicleLeave += SectionVehicleLeave;
            }
            List<AADDRESS> addresses = scApp.AddressesBLL.cache.GetAddresses();
            foreach (AADDRESS address in addresses)
            {
                address.VehicleRelease += AddressVehicleRelease;
            }

            var commonInfo = scApp.getEQObjCacheManager().CommonInfo;
            commonInfo.addEventHandler(nameof(LineService), BCFUtility.getPropertyName(() => commonInfo.MPCTipMsgList),
             PublishTipMessageInfo);
        }
        private void Line_LineStatusChange(object sender, EventArgs e)
        {
            PublishLineInfo(sender, null);
        }

        private void Line_LongTimeNoCommuncation(object sender, EventArgs e)
        {
            reportBLL.AskAreYouThere();
        }

        public void startHostCommunication()
        {
            scApp.getBCFApplication().getSECSAgent(scApp.EAPSecsAgentName).refreshConnection();
        }

        public void stopHostCommunication()
        {
            scApp.getBCFApplication().getSECSAgent(scApp.EAPSecsAgentName).stop();
            line.Secs_Link_Stat = SCAppConstants.LinkStatus.LinkFail;
            line.connInfoUpdate_Disconnection();
        }

        public void RefreshCurrentVehicleReserveStatus()
        {
            try
            {
                scApp.AddressesBLL.redis.ForceAllAddressRelease();
                RegisterAddressReserveAgain();
                ForceAllBlockToRelease();
                scApp.TrafficControlBLL.redis.ForceAllTrafficControlRelease();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception:");
            }
        }

        private void ForceAllBlockToRelease()
        {
            try
            {
                var all_block = scApp.BlockControlBLL.getAllCurrentBlockID();
                foreach (var block_ in all_block)
                {
                    scApp.BlockControlBLL.updateBlockZoneQueue_ForceRelease(block_.CAR_ID, block_.ENTRY_SEC_ID);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception:");
            }
        }

        private void RegisterAddressReserveAgain()
        {
            var vhs = scApp.VehicleBLL.cache.loadAllVh();
            foreach (AVEHICLE vh in vhs)
            {
                //if (!vh.isTcpIpConnect || vh.MODE_STATUS != ProtocolFormat.OHTMessage.VHModeStatus.AutoRemote)
                if (!vh.isTcpIpConnect)
                    continue;
                string vh_id = vh.VEHICLE_ID;
                //var current_segment = scApp.SegmentBLL.cache.GetSegment(vh.CUR_SEG_ID);
                var current_section = scApp.SectionBLL.cache.GetSection(vh.CUR_SEC_ID);

                //if (current_segment != null)
                if (current_section != null)
                {
                    foreach (string adr in current_section.NodeAddress)
                    //foreach (string adr in current_segment.NodeAddress)
                    {
                        scApp.AddressesBLL.redis.setVehicleInReserveList(vh_id, adr, vh.CUR_SEC_ID);
                    }
                    //scApp.VehicleBLL.setVhReserveSuccessOfSegment(vh.VEHICLE_ID, new List<string> { current_segment.SEG_ID });
                }
            }
        }

        private void SectionVehicleLeave(object sender, string vhID)
        {
            ASECTION sec = sender as ASECTION;
            sec.ReleaseSectionReservation(vhID);
        }
        private void AddressVehicleRelease(object sender, string vhID)
        {
            AADDRESS adr = sender as AADDRESS;
            scApp.AddressesBLL.redis.setReleaseAddressInfo(vhID, adr.ADR_ID);
            scApp.AddressesBLL.redis.AddressRelease(vhID, adr.ADR_ID);
        }

        private void PublishLineInfo(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                ALINE line = sender as ALINE;
                if (sender == null) return;
                byte[] line_serialize = BLL.LineBLL.Convert2GPB_LineInfo(line);
                scApp.getNatsManager().PublishAsync
                    (SCAppConstants.NATS_SUBJECT_LINE_INFO, line_serialize);


                //TODO 要改用GPP傳送
                //var line_Serialize = ZeroFormatter.ZeroFormatterSerializer.Serialize(line);
                //scApp.getNatsManager().PublishAsync
                //    (string.Format(SCAppConstants.NATS_SUBJECT_LINE_INFO), line_Serialize);
            }
            catch (Exception ex)
            {
            }
        }



        private void PublishTipMessageInfo(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                Data.VO.CommonInfo commonInfo = sender as Data.VO.CommonInfo;
                if (sender == null) return;

                byte[] line_serialize = BLL.LineBLL.Convert2GPB_TipMsgIngo(commonInfo.MPCTipMsgList);
                scApp.getNatsManager().PublishAsync
                    (SCAppConstants.NATS_SUBJECT_TIP_MESSAGE_INFO, line_serialize);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception:");
            }
        }
        //public void PublishSystemLog(LogObj obj)
        //{
        //    try
        //    {
        //        if (obj == null) return;

        //        byte[] line_serialize = BLL.LineBLL.Convert2GPB_SystemLog(obj);
        //        scApp.getNatsManager().PublishAsync
        //            (SCAppConstants.NATS_SUBJECT_SYSTEM_LOG, line_serialize);
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.Error(ex, "Exception:");
        //    }
        //}


        public override void OnlineRemoteWithHost()
        {
            bool isSuccess = true;
            isSuccess = isSuccess && reportBLL.AskAreYouThere();
            isSuccess = isSuccess && lineBLL.updateHostControlState(SCAppConstants.LineHostControlState.HostControlState.On_Line_Remote);
            isSuccess = isSuccess && reportBLL.ReportControlStateRemote();
            isSuccess = isSuccess && TSCStateToPause();
            isSuccess = isSuccess && reportBLL.AskDateAndTimeRequest();
            //todo fire TSC to pause

        }
        public override void OnlineLocalWithHostOp()
        {
            bool isSuccess = true;
            isSuccess = isSuccess && reportBLL.AskAreYouThere();
            isSuccess = isSuccess && lineBLL.updateHostControlState(SCAppConstants.LineHostControlState.HostControlState.On_Line_Local);
            isSuccess = isSuccess && reportBLL.ReportControlStateLocal();
            isSuccess = isSuccess && TSCStateToPause();
            isSuccess = isSuccess && reportBLL.AskDateAndTimeRequest();
            //todo fire TSC to pause
        }

        public void OfflineWithHostByOp()
        {
            bool isSuccess = true;
            isSuccess = isSuccess && lineBLL.updateHostControlState(SCAppConstants.LineHostControlState.HostControlState.EQ_Off_line);
            isSuccess = isSuccess && reportBLL.ReportEquiptmentOffLine();
            isSuccess = isSuccess && TSCStateToNone();

        }
        public void OfflineWithHost()
        {
            bool isSuccess = true;
            isSuccess = isSuccess && lineBLL.updateHostControlState(SCAppConstants.LineHostControlState.HostControlState.EQ_Off_line);
            isSuccess = isSuccess && reportBLL.ReportEquiptmentOffLine();
            isSuccess = isSuccess && TSCStateToNone();
        }

        public bool canOnlineWithHost()
        {
            bool can_online = true;
            ////1檢查目前沒有Remove的Vhhicle，是否都已連線
            //List<AVEHICLE> vhs = scApp.getEQObjCacheManager().getAllVehicle();
            //List<AVEHICLE> need_check_vhs = vhs.Where(vh => vh.State != VehicleState.REMOVED).ToList();

            //can_not_online = need_check_vhs.Where(vh => !vh.isTcpIpConnect).Count() > 0;
            return can_online;
        }

        public bool TSCStateToPause()
        {
            bool isSuccess = true;
            ALINE.TSCStateMachine tsc_sm = line.TSC_state_machine;
            if (tsc_sm.State == ALINE.TSCState.NONE)
            {
                isSuccess = isSuccess && line.AGVCInitialComplete(reportBLL);
                isSuccess = isSuccess && line.StartUpSuccessed(reportBLL);
            }
            else if (tsc_sm.State == ALINE.TSCState.TSC_INIT)
            {
                isSuccess = isSuccess && line.StartUpSuccessed(reportBLL);
            }
            else if (tsc_sm.State == ALINE.TSCState.AUTO)
            {
                isSuccess = isSuccess && line.RequestToPause(reportBLL);
                int in_excute_cmd_count = scApp.CMDBLL.getCMD_MCSIsRunningCount();
                if (in_excute_cmd_count == 0)
                {
                    isSuccess = isSuccess && line.PauseCompleted(reportBLL);
                }
            }
            else if (tsc_sm.State == ALINE.TSCState.PAUSING)
            {
                isSuccess = isSuccess && line.PauseCompleted(reportBLL);
            }
            else if (tsc_sm.State == ALINE.TSCState.PAUSED)
            {
                //do nothing
            }
            else
            {
                //do nothing
            }
            return isSuccess;
        }
        public bool TSCStateToNone()
        {
            bool isSuccess = true;
            isSuccess = isSuccess && line.ChangeToOffline();
            return isSuccess;
        }


        public void ProcessHostCommandResume()
        {
            //todo fire TSC to auto
        }
        object publishSystemMsgLock = new object();
        public void PublishSystemMsgInfo(Object systemLog)
        {
            lock (publishSystemMsgLock)
            {
                try
                {
                    SYSTEMPROCESS_INFO logObj = systemLog as SYSTEMPROCESS_INFO;

                    byte[] systemMsg_Serialize = BLL.LineBLL.Convert2GPB_SystemMsgInfo(logObj);

                    if (systemMsg_Serialize != null)
                    {
                        scApp.getNatsManager().PublishAsync
                            (SCAppConstants.NATS_SUBJECT_SYSTEM_LOG, systemMsg_Serialize);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Exception:");
                }
            }
        }

        object publishHostMsgLock = new object();
        public void PublishHostMsgInfo(Object secsLog)
        {
            lock (publishHostMsgLock)
            {
                try
                {
                    LogTitle_SECS logSECS = secsLog as LogTitle_SECS;

                    byte[] systemMsg_Serialize = BLL.LineBLL.Convert2GPB_SECSMsgInfo(logSECS);

                    if (systemMsg_Serialize != null)
                    {
                        scApp.getNatsManager().PublishAsync
                            (SCAppConstants.NATS_SUBJECT_SECS_LOG, systemMsg_Serialize);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Exception:");
                }
            }
        }

        object publishEQMsgLock = new object();
        public void PublishEQMsgInfo(Object tcpLog)
        {
            lock (publishEQMsgLock)
            {
                try
                {
                    dynamic logEntry = tcpLog as JObject;

                    byte[] tcpMsg_Serialize = BLL.LineBLL.Convert2GPB_TcpMsgInfo(logEntry);

                    if (tcpMsg_Serialize != null)
                    {
                        scApp.getNatsManager().PublishAsync
                            (SCAppConstants.NATS_SUBJECT_TCPIP_LOG, tcpMsg_Serialize);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Exception:");
                }
            }
        }
    }
}
