using com.mirle.ibg3k0.bcf.Common;
using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.BLL;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using static com.mirle.ibg3k0.sc.ALINE;
using static com.mirle.ibg3k0.sc.AVEHICLE;

namespace com.mirle.ibg3k0.sc.Service
{
    public class SouthInnoLuxTransferService : TransferService
    {
        NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        //private SCApplication scApp = null;
        //private ReportBLL reportBLL = null;
        //private LineBLL lineBLL = null;
        //private ALINE line = null;
        public SouthInnoLuxTransferService()
        {

        }
        public void start(SCApplication _app)
        {
            scApp = _app;
            reportBLL = _app.ReportBLL;
            lineBLL = _app.LineBLL;
            line = scApp.getEQObjCacheManager().getLine();

            line.addEventHandler(nameof(ConnectionInfoService), nameof(line.MCSCommandAutoAssign), PublishTransferInfo);


            initPublish(line);
        }
        private void initPublish(ALINE line)
        {
            PublishTransferInfo(line, null);
            //PublishOnlineCheckInfo(line, null);
            //PublishPingCheckInfo(line, null);
        }

        private void PublishTransferInfo(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                ALINE line = sender as ALINE;
                if (sender == null) return;
                byte[] line_serialize = BLL.LineBLL.Convert2GPB_TransferInfo(line);
                scApp.getNatsManager().PublishAsync
                    (SCAppConstants.NATS_SUBJECT_TRANSFER, line_serialize);


                //TODO 要改用GPP傳送
                //var line_Serialize = ZeroFormatter.ZeroFormatterSerializer.Serialize(line);
                //scApp.getNatsManager().PublishAsync
                //    (string.Format(SCAppConstants.NATS_SUBJECT_LINE_INFO), line_Serialize);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception:");
            }
        }


        public bool doChangeLinkStatus(string linkStatus, out string result)
        {
            bool isSuccess = true;
            result = string.Empty;
            try
            {
                if (isSuccess)
                {
                    using (TransactionScope tx = SCUtility.getTransactionScope())
                    {
                        using (DBConnection_EF con = DBConnection_EF.GetUContext())
                        {
                            if (linkStatus == SCAppConstants.LinkStatus.LinkOK.ToString())
                            {
                                if (scApp.getEQObjCacheManager().getLine().Secs_Link_Stat == SCAppConstants.LinkStatus.LinkOK)
                                {
                                    result = "Selected already!";
                                }
                                else
                                {
                                    Task.Run(() => scApp.LineService.startHostCommunication());
                                    result = "OK";
                                }

                                tx.Complete();

                            }
                            else if (linkStatus == SCAppConstants.LinkStatus.LinkFail.ToString())
                            {
                                if (scApp.getEQObjCacheManager().getLine().Secs_Link_Stat == SCAppConstants.LinkStatus.LinkFail)
                                {
                                    result = "Not selected already!";
                                }
                                else
                                {
                                    Task.Run(() => scApp.LineService.stopHostCommunication());
                                    result = "OK";
                                }

                                tx.Complete();

                            }
                            else
                            {
                                result = linkStatus + " Not Defined";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                isSuccess = false;
                logger.Error(ex, "Execption:");
            }
            return isSuccess;
        }


        public bool doChangeHostMode(string host_mode, out string result)
        {
            bool isSuccess = true;
            result = string.Empty;
            try
            {
                if (isSuccess)
                {
                    using (TransactionScope tx = SCUtility.getTransactionScope())
                    {
                        using (DBConnection_EF con = DBConnection_EF.GetUContext())
                        {
                            if (host_mode == SCAppConstants.LineHostControlState.HostControlState.On_Line_Remote.ToString())
                            {
                                if (!scApp.LineService.canOnlineWithHost())
                                {
                                    //MessageBox.Show("Has vh not ready");
                                    //回報當無法連線
                                    result = "Has vh not ready";
                                }
                                else if (scApp.getEQObjCacheManager().getLine().Host_Control_State == SCAppConstants.LineHostControlState.HostControlState.On_Line_Remote)
                                {
                                    //MessageBox.Show("On line ready");
                                    result = "OnlineRemote ready";
                                }
                                else
                                {
                                    line.resetOnlieCheckItem();
                                    Task.Run(() => scApp.LineService.OnlineRemoteWithHost());
                                    result = "OK";
                                }
                                //isSuccess = scApp.PortStationBLL.OperateDB.updatePriority(portID, priority);
                                //if (isSuccess)
                                //{
                                tx.Complete();
                                //    scApp.PortStationBLL.OperateCatch.updatePriority(portID, priority);
                                //}
                            }
                            else if (host_mode == SCAppConstants.LineHostControlState.HostControlState.On_Line_Local.ToString())
                            {
                                if (!scApp.LineService.canOnlineWithHost())
                                {
                                    //MessageBox.Show("Has vh not ready");
                                    //回報當無法連線
                                    result = "Has vh not ready";
                                }
                                else if (scApp.getEQObjCacheManager().getLine().Host_Control_State == SCAppConstants.LineHostControlState.HostControlState.On_Line_Local)
                                {
                                    //MessageBox.Show("On line ready");
                                    result = "OnlineLocal ready";
                                }
                                else
                                {
                                    line.resetOnlieCheckItem();
                                    Task.Run(() => scApp.LineService.OnlineLocalWithHostOp());
                                    result = "OK";
                                }
                                //isSuccess = scApp.PortStationBLL.OperateDB.updatePriority(portID, priority);
                                //if (isSuccess)
                                //{
                                tx.Complete();
                                //    scApp.PortStationBLL.OperateCatch.updatePriority(portID, priority);
                                //}
                            }
                            else
                            {
                                if (scApp.getEQObjCacheManager().getLine().SCStats != TSCState.PAUSED)
                                {
                                    //MessageBox.Show("Please change tsc state to pause first.");
                                    result = "Please change tsc state to pause first.";
                                }
                                else if (scApp.getEQObjCacheManager().getLine().Host_Control_State == SCAppConstants.LineHostControlState.HostControlState.EQ_Off_line)
                                {
                                    //MessageBox.Show("Current is off line");
                                    result = "Current is off line";
                                }
                                else
                                {
                                    line.resetOnlieCheckItem();
                                    Task.Run(() => scApp.LineService.OfflineWithHostByOp());
                                    result = "OK";
                                }
                                //isSuccess = scApp.PortStationBLL.OperateDB.updatePriority(portID, priority);
                                //if (isSuccess)
                                //{
                                tx.Complete();
                                //    scApp.PortStationBLL.OperateCatch.updatePriority(portID, priority);
                                //}
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                isSuccess = false;
                logger.Error(ex, "Execption:");
            }
            return isSuccess;
        }

        public bool doChangeTSCstate(string tscstate, out string result)
        {
            bool isSuccess = true;
            result = string.Empty;
            try
            {
                if (isSuccess)
                {
                    using (TransactionScope tx = SCUtility.getTransactionScope())
                    {
                        using (DBConnection_EF con = DBConnection_EF.GetUContext())
                        {
                            if (tscstate == ALINE.TSCState.AUTO.ToString())
                            {
                                if (scApp.getEQObjCacheManager().getLine().SCStats == ALINE.TSCState.AUTO)
                                {
                                    result = "AUTO ready";
                                }
                                else
                                {
                                    Task.Run(() => scApp.getEQObjCacheManager().getLine().ResumeToAuto(scApp.ReportBLL));
                                    result = "OK";
                                }
                                //isSuccess = scApp.PortStationBLL.OperateDB.updatePriority(portID, priority);
                                //if (isSuccess)
                                //{
                                tx.Complete();
                                //    scApp.PortStationBLL.OperateCatch.updatePriority(portID, priority);
                                //}
                            }
                            else if (tscstate == ALINE.TSCState.PAUSED.ToString())
                            {
                                if (scApp.getEQObjCacheManager().getLine().SCStats == ALINE.TSCState.PAUSED)
                                {
                                    //MessageBox.Show("Has vh not ready");
                                    //回報當無法連線
                                    result = "PAUSED ready";
                                }
                                else
                                {
                                    Task.Run(() => scApp.LineService.TSCStateToPause());
                                    result = "OK";
                                }
                                //isSuccess = scApp.PortStationBLL.OperateDB.updatePriority(portID, priority);
                                //if (isSuccess)
                                //{
                                tx.Complete();
                                //    scApp.PortStationBLL.OperateCatch.updatePriority(portID, priority);
                                //}
                            }
                            else
                            {
                                result = tscstate + " Not Defined";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                isSuccess = false;
                logger.Error(ex, "Execption:");
            }
            return isSuccess;
        }
        static int ManualCommandSeqNum = 1;
        public override (bool isSuccess, string checkResult) tryToCreatManualMCSCommand(string source_port_or_vh_id, string dest_port, string carrier_id)
        {
            try
            {
                var check_result = doCheckManualMCSCommand(source_port_or_vh_id, dest_port, carrier_id);
                if (check_result.isSuccess)
                {
                    string cmd_id = $"MANUAL{ManualCommandSeqNum++.ToString("000000")}";
                    var creat_resule = scApp.CMDBLL.doCreatMCSCommandForManual(cmd_id, "10", "", carrier_id, source_port_or_vh_id, dest_port, "4");
                    check_result.isSuccess = creat_resule.isSuccess;
                    if (check_result.isSuccess)
                    {
                        scApp.ReportBLL.newReportS6F11SendOperatorInitiatedAction(creat_resule.mcsCmd, null);
                    }
                    else
                    {
                        check_result.checkResult = $"Source:{source_port_or_vh_id} dest:{dest_port} cst:{carrier_id} creat to db fail.";
                    }
                }
                return (check_result.isSuccess, check_result.checkResult);
            }
            catch (Exception ex)
            {
                return (false,ex.ToString());
            }
        }

        public override (bool isSuccess, string checkResult) doCheckManualMCSCommand(string source_port_or_vh_id, string dest_port, string carrier_id)
        {
            bool isSuccess = true;
            string check_result = "";

            //確認是否有同一顆正在搬送的CST ID
            if (isSuccess)
            {
                var cmd_obj = scApp.CMDBLL.getExcuteCMD_MCSByCarrierID(carrier_id);
                if (cmd_obj != null)
                {
                    check_result = $"Want to creat manual mcs cmd ,but carrier id:{carrier_id} already excute by command id:{cmd_obj.CMD_ID.Trim()}";
                    return (false, check_result);
                }
            }

            //確認是否有在相同Load Port的Transfer Command且該命令狀態還沒有變成Transferring(代表還在Port上還沒搬走)
            if (isSuccess)
            {
                //M0.02 var cmd_obj = scApp.CMDBLL.getWatingCMDByFromTo(source_port_or_vh_id, dest_port);
                var cmd_obj = scApp.CMDBLL.getWatingCMD_MCSByFrom(source_port_or_vh_id);//M0.02 
                if (cmd_obj != null)
                {
                    check_result = $"Want to creat manual mcs cmd ,but is same as orther mcs command id {cmd_obj.CMD_ID.Trim()} of load port.";//M0.02 
                    //M0.02 check_result = $"MCS command id:{command_id} of transfer load port is same command id:{cmd_obj.CMD_ID.Trim()}";
                    return (false, check_result);
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
                check_result = $"Want to creat manual mcs cmd ,but source Port:{source_port_or_vh_id} not exist.{Environment.NewLine}please confirm the port name";
                return (false, check_result);
            }

            isSuccess = scApp.PortStationBLL.OperateCatch.IsExist(dest_port);
            if (!isSuccess)
            {
                check_result = $"Want to creat manual mcs cmd ,but destination Port:{dest_port} not exist.{Environment.NewLine}please confirm the port name";
                return (false, check_result);
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
                    check_result = $"Want to creat manual mcs cmd ,but source port:{source_port_or_vh_id} to destination port:{dest_port} no path to go{Environment.NewLine}," +
                        $"please check the road traffic status.";
                    return (false, check_result);
                }
            }
            //如果不是Port(則為指定車號)，要檢查是否從該車位置可以到達放貨地點
            else
            {
                AVEHICLE carry_vh = scApp.VehicleBLL.cache.getVehicleByRealID(source_port_or_vh_id);
                APORTSTATION dest_port_station = scApp.PortStationBLL.OperateCatch.getPortStation(dest_port);
                isSuccess = scApp.GuideBLL.IsRoadWalkable(carry_vh.CUR_ADR_ID, dest_port_station.ADR_ID);
                if (!isSuccess)
                {
                    check_result = $"Want to creat manual mcs cmd ,but vh:{source_port_or_vh_id} current address:{carry_vh.CUR_ADR_ID} to destination port:{dest_port}:{dest_port_station.ADR_ID} no path to go{Environment.NewLine}," +
                        $"please check the road traffic status.";
                    return (false, check_result);
                }
            }

            return (true, check_result);
        }



    }
}
