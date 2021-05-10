using com.mirle.ibg3k0.bcf.App;
using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.Data;
using com.mirle.ibg3k0.sc.Data.DAO;
using com.mirle.ibg3k0.sc.Data.DAO.EntityFramework;
using com.mirle.ibg3k0.sc.Data.SECS;
using com.mirle.ibg3k0.sc.Data.ValueDefMapAction;
using com.mirle.ibg3k0.sc.Data.VO;
using com.mirle.ibg3k0.sc.ProtocolFormat.OHTMessage;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace com.mirle.ibg3k0.sc.BLL
{
    public class CMDBLL
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        protected CMD_OHTCDao cmd_ohtcDAO = null;
        protected CMD_OHTC_DetailDao cmd_ohtc_detailDAO = null;
        protected CMD_MCSDao cmd_mcsDao = null;
        protected VCMD_MCSDao vcmd_mcsDao = null;
        protected HCMD_MCSDao hcmd_mcsDao = null;
        protected HCMD_OHTCDao hcmd_ohtcDao = null;
        protected TestTranTaskDao testTranTaskDao = null;
        protected ReturnCodeMapDao return_code_mapDao = null;

        protected static Logger logger_VhRouteLog = LogManager.GetLogger("VhRoute");
        protected string[] ByPassSegment = null;
        protected ParkZoneTypeDao parkZoneTypeDao = null;
        protected SCApplication scApp = null;
        public CMDBLL()
        {

        }
        public void start(SCApplication app)
        {
            scApp = app;
            cmd_ohtcDAO = scApp.CMD_OHTCDao;
            cmd_ohtc_detailDAO = scApp.CMD_OHT_DetailDao;
            cmd_mcsDao = scApp.CMD_MCSDao;
            vcmd_mcsDao = scApp.VCMD_MCSDao;
            hcmd_mcsDao = scApp.HCMD_MCSDao;
            hcmd_ohtcDao = scApp.HCMD_OHTCDao;
            parkZoneTypeDao = scApp.ParkZoneTypeDao;
            testTranTaskDao = scApp.TestTranTaskDao;
            return_code_mapDao = scApp.ReturnCodeMapDao;
        }



        #region CMD_MCS
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
                var cmd_obj = getCMD_MCSByID(command_id);
                if (cmd_obj != null)
                {
                    check_result = $"Command id:{command_id} already exist.";
                    checkcode = SECSConst.HCACK_Confirm_Executed;
                }
            }

            return checkcode;
        }

        public virtual bool doCreatMCSCommand(string command_id, string Priority, string replace, string carrier_id, string HostSource, string HostDestination, string checkcode)
        {
            try
            {
                bool isSuccess = true;
                int ipriority = 0;
                if (!int.TryParse(Priority, out ipriority))
                {
                    logger.Warn("command id :{0} of priority parse fail. priority valus:{1}"
                                , command_id
                                , Priority);
                }
                int ireplace = 0;
                if (!int.TryParse(replace, out ireplace))
                {
                    logger.Warn("command id :{0} of priority parse fail. priority valus:{1}"
                                , command_id
                                , ireplace);
                }


                //ACMD_MCS mcs_com = creatCommand_MCS(command_id, ipriority, carrier_id, HostSource, HostDestination, checkcode);
                creatCommand_MCS(command_id, ipriority, ireplace, carrier_id, HostSource, HostDestination, checkcode);
                //if (mcs_com != null)
                //{
                //    isSuccess = true;
                //    scApp.SysExcuteQualityBLL.creatSysExcuteQuality(mcs_com);
                //    //mcsDefaultMapAction.sendS6F11_TranInit(command_id);
                //    scApp.ReportBLL.doReportTransferInitial(command_id);
                //    checkMCS_TransferCommand();
                //}
                return isSuccess;
            }
            catch(Exception ex)
            {
                logger.Error(ex, "Exection:");
                return false;
            }


        }
        public bool updateCMD_MCS_CmdState2BCRFail(string cmd_id)
        {
            bool isSuccess = true;
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                ACMD_MCS cmd = cmd_mcsDao.getByID(con, cmd_id);
                cmd.COMMANDSTATE = SCAppConstants.TaskCmdStatus.BCRReadFail;
                cmd_mcsDao.update(con, cmd);
            }
            return isSuccess;
        }
        public virtual ACMD_MCS creatCommand_MCS(string command_id, int Priority, int replace, string carrier_id, string HostSource, string HostDestination, string checkcode)
        {
            int port_priority = 0;
            if (!SCUtility.isEmpty(HostSource))
            {
                APORTSTATION source_portStation = scApp.getEQObjCacheManager().getPortStation(HostSource);

                if (source_portStation == null)
                {
                    logger.Warn($"MCS cmd of hostsource port[{HostSource} not exist.]");
                }
                else
                {
                    port_priority = source_portStation.PRIORITY;
                }
            }
            E_TRAN_STATUS transfer_status = E_TRAN_STATUS.Queue;
            if (SCUtility.isMatche(checkcode, com.mirle.ibg3k0.sc.Data.SECS.AGVC.SECSConst.HCACK_Confirm) ||
                SCUtility.isMatche(checkcode, com.mirle.ibg3k0.sc.Data.SECS.AGVC.SECSConst.HCACK_Confirm_Executed))
            {
                //Not thing...
            }
            else
            {
                transfer_status = E_TRAN_STATUS.Reject;
            }
            ACMD_MCS cmd = new ACMD_MCS()
            {
                CARRIER_ID = carrier_id,
                CMD_ID = command_id,
                TRANSFERSTATE = transfer_status,
                COMMANDSTATE = SCAppConstants.TaskCmdStatus.Queue,
                HOSTSOURCE = HostSource,
                HOSTDESTINATION = HostDestination,
                PRIORITY = Priority,
                CHECKCODE = checkcode,
                PAUSEFLAG = "0",
                CMD_INSER_TIME = DateTime.Now,
                TIME_PRIORITY = 0,
                PORT_PRIORITY = port_priority,
                PRIORITY_SUM = Priority + port_priority,
                REPLACE = replace
            };
            if (creatCommand_MCS(cmd))
            {
                return cmd;
            }
            else
            {
                return null;
            }
        }
        public bool creatCommand_MCS(ACMD_MCS cmd_mcs)
        {
            bool isSuccess = true;
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                cmd_mcsDao.add(con, cmd_mcs);
                con.Entry(cmd_mcs).State = EntityState.Detached;
            }
            return isSuccess;
        }

        public virtual bool updateCMD_MCS_TranStatus(string cmd_id, E_TRAN_STATUS status)
        {
            bool isSuccess = true;
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                ACMD_MCS cmd = cmd_mcsDao.getByID(con, cmd_id);
                cmd.TRANSFERSTATE = status;
                cmd_mcsDao.update(con, cmd);
            }
            return isSuccess;
        }

        public bool updateCMD_MCS_TranStatus2Initial(string cmd_id)
        {
            bool isSuccess = true;
            //using (DBConnection_EF con = new DBConnection_EF())
            try
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    ACMD_MCS cmd = cmd_mcsDao.getByID(con, cmd_id);
                    cmd.TRANSFERSTATE = E_TRAN_STATUS.Initial;
                    cmd.CMD_START_TIME = DateTime.Now;
                    cmd_mcsDao.update(con, cmd);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exection:");
                isSuccess = false;
            }
            return isSuccess;
        }

        public bool updateCMD_MCS_TranStatus2PreInitial(string cmd_id)
        {
            bool isSuccess = true;
            //using (DBConnection_EF con = new DBConnection_EF())
            try
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    ACMD_MCS cmd = cmd_mcsDao.getByID(con, cmd_id);
                    cmd.TRANSFERSTATE = E_TRAN_STATUS.PreInitial;
                    cmd_mcsDao.update(con, cmd);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exection:");
                isSuccess = false;
            }
            return isSuccess;
        }



        public bool updateCMD_MCS_TranStatus2Transferring(string cmd_id)
        {
            bool isSuccess = true;
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                ACMD_MCS cmd = cmd_mcsDao.getByID(con, cmd_id);
                cmd.TRANSFERSTATE = E_TRAN_STATUS.Transferring;
                cmd_mcsDao.update(con, cmd);
            }
            return isSuccess;
        }
        public bool updateCMD_MCS_TranStatus2Canceling(string cmd_id)
        {
            bool isSuccess = true;
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                ACMD_MCS cmd = cmd_mcsDao.getByID(con, cmd_id);
                cmd.TRANSFERSTATE = E_TRAN_STATUS.Canceling;
                cmd_mcsDao.update(con, cmd);
            }
            return isSuccess;
        }
        public virtual bool updateCMD_MCS_TranStatus2Canceled(string cmd_id)
        {
            bool isSuccess = true;
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                ACMD_MCS cmd = cmd_mcsDao.getByID(con, cmd_id);
                cmd.TRANSFERSTATE = E_TRAN_STATUS.Canceled;
                cmd.CMD_FINISH_TIME = DateTime.Now;
                cmd_mcsDao.update(con, cmd);
            }

            return isSuccess;
        }


        public bool updateCMD_MCS_TranStatus2Aborting(string cmd_id)
        {
            bool isSuccess = true;
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                ACMD_MCS cmd = cmd_mcsDao.getByID(con, cmd_id);
                cmd.TRANSFERSTATE = E_TRAN_STATUS.Aborting;
                cmd_mcsDao.update(con, cmd);
            }
            return isSuccess;
        }


        public bool updateCMD_MCS_TranStatus2Queue(string cmd_id)
        {
            bool isSuccess = true;
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                ACMD_MCS cmd = cmd_mcsDao.getByID(con, cmd_id);
                cmd.TRANSFERSTATE = E_TRAN_STATUS.Queue;
                cmd_mcsDao.update(con, cmd);
            }
            return isSuccess;
        }
        public virtual bool updateCMD_MCS_TranStatus2Complete(string cmd_id, E_TRAN_STATUS tran_status)
        {
            bool isSuccess = true;
            //DBConnection_EF con = DBConnection_EF.GetContext();
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                ACMD_MCS cmd = cmd_mcsDao.getByID(con, cmd_id);
                if (cmd != null)
                {
                    cmd.TRANSFERSTATE = tran_status;
                    cmd.CMD_FINISH_TIME = DateTime.Now;
                    cmd_mcsDao.update(con, cmd);
                }
                else
                {
                    //isSuccess = false;
                }
            }
            return isSuccess;
        }

        public bool updateCMD_MCS_Priority(ACMD_MCS mcs_cmd, int priority)
        {
            bool isSuccess = true;
            //using (DBConnection_EF con = new DBConnection_EF())
            try
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    con.ACMD_MCS.Attach(mcs_cmd);
                    mcs_cmd.PRIORITY = priority;
                    cmd_mcsDao.update(con, mcs_cmd);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exection:");
                isSuccess = false;
            }
            return isSuccess;
        }

        public virtual bool updateCMD_MCS_CarrierID(string cmd_id, string carrier_id)
        {
            bool isSuccess = true;
            try
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    ACMD_MCS cmd = cmd_mcsDao.getByID(con, cmd_id);
                    if (cmd != null)
                    {
                        cmd.CARRIER_ID = carrier_id;
                        cmd_mcsDao.update(con, cmd);
                    }
                    else
                    {
                        //isSuccess = false;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exection:");
                isSuccess = false;
            }
            return isSuccess;
        }
        public bool updateCMD_MCS_CheckCode(string cmd_id, string checkCode)
        {
            bool isSuccess = true;
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                ACMD_MCS cmd = cmd_mcsDao.getByID(con, cmd_id);
                if (cmd != null)
                {
                    cmd.CHECKCODE = checkCode;
                    cmd_mcsDao.update(con, cmd);
                }
                else
                {
                    //isSuccess = false;
                }
            }
            return isSuccess;

        }

        public bool updateCMD_MCS_TimePriority(ACMD_MCS mcs_cmd, int time_priority)
        {
            bool isSuccess = true;
            //using (DBConnection_EF con = new DBConnection_EF())
            try
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    con.ACMD_MCS.Attach(mcs_cmd);
                    mcs_cmd.TIME_PRIORITY = time_priority;
                    cmd_mcsDao.update(con, mcs_cmd);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exection:");
                isSuccess = false;
            }
            return isSuccess;
        }

        public bool updateCMD_MCS_PrioritySUM(ACMD_MCS mcs_cmd, int priority_sum)
        {
            bool isSuccess = true;
            //using (DBConnection_EF con = new DBConnection_EF())
            try
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    con.ACMD_MCS.Attach(mcs_cmd);
                    mcs_cmd.PRIORITY_SUM = priority_sum;
                    cmd_mcsDao.update(con, mcs_cmd);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exection:");
                isSuccess = false;
            }
            return isSuccess;
        }


        public void remoteCMD_MCSByBatch(List<ACMD_MCS> mcs_cmds)
        {
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {

                cmd_mcsDao.RemoteByBatch(con, mcs_cmds);
            }
        }



        public ACMD_MCS getCMD_MCSByID(string cmd_id)
        {
            ACMD_MCS cmd_mcs = null;
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                cmd_mcs = cmd_mcsDao.getByID(con, cmd_id);
            }
            return cmd_mcs;
        }

        public VACMD_MCS getVCMD_MCSByID(string cmd_id)
        {
            VACMD_MCS cmd_mcs = null;
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                cmd_mcs = vcmd_mcsDao.getVCMDByID(con, cmd_id);
            }
            return cmd_mcs;
        }

        public ACMD_MCS getExcuteCMD_MCSByCarrierID(string carrierID)
        {
            ACMD_MCS cmd_mcs = null;
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                cmd_mcs = cmd_mcsDao.getExcuteCMDByCSTID(con, carrierID);
            }
            return cmd_mcs;
        }

        public ACMD_MCS getWatingCMDByFromTo(string hostSource, string hostDestination)
        {
            ACMD_MCS cmd_mcs = null;
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                cmd_mcs = cmd_mcsDao.getWatingCMDByFromTo(con, hostSource, hostDestination);
            }
            return cmd_mcs;
        }
        public ACMD_MCS getWatingCMD_MCSByFrom(string hostSource)
        {
            ACMD_MCS cmd_mcs = null;
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                cmd_mcs = cmd_mcsDao.getWatingCMDByFrom(con, hostSource);
            }
            return cmd_mcs;
        }

        public ACMD_MCS getLastFinishCmd_MCSByCSTID(string cstID, string byPassCmdID)
        {
            ACMD_MCS cmd_mcs = null;
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                cmd_mcs = cmd_mcsDao.getLastFinishCmdByCSTID(con, cstID, byPassCmdID);
            }
            return cmd_mcs;
        }

        public bool HasCMD_MCSInQueue()
        {
            return getCMD_MCSIsQueueCount() > 0;
        }
        public int getCMD_MCSIsQueueCount()
        {
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                return cmd_mcsDao.getCMD_MCSIsQueueCount(con);
            }
        }


        public int getCMD_MCSIsRunningCount()
        {
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                return cmd_mcsDao.getCMD_MCSIsExcuteCount(con);
            }
        }

        public int getCMD_MCSIsRunningCount(DateTime befor_time)
        {
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                return cmd_mcsDao.getCMD_MCSIsExcuteCount(con, befor_time);
            }
        }
        public int getCMD_MCSIsUnfinishedCount(List<string> port_ids)
        {
            if (port_ids == null) return 0;
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                return cmd_mcsDao.getCMD_MCSIsUnfinishedCount(con, port_ids);
            }
        }
        public int getCMD_MCSIsUnfinishedCountByCarrierID(string carrier_id)
        {
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                return cmd_mcsDao.getCMD_MCSIsUnfinishedCountByCarrierID(con, carrier_id);
            }
        }
        public List<ACMD_MCS> loadACMD_MCSIsUnfinished()
        {
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                return cmd_mcsDao.loadACMD_MCSIsUnfinished(con);
            }
        }

        public List<ACMD_MCS> loadFinishCMD_MCS()
        {
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                return cmd_mcsDao.loadFinishCMD_MCS(con);
            }
        }
        public List<VACMD_MCS> loadVACMD_MCSIsUnfinished()
        {
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                return vcmd_mcsDao.loadVACMD_MCSIsUnfinished(con);
            }
        }

        public List<VACMD_MCS> loadAllVACMD_MCS()
        {
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                return vcmd_mcsDao.loadAllVACMD(con);
            }
        }

        public List<ACMD_MCS> loadACMD_MCSIsUnfinished(DBConnection_EF con)
        {
            var query = from cmd in con.ACMD_MCS.AsNoTracking()
                        where cmd.TRANSFERSTATE >= E_TRAN_STATUS.Queue && cmd.TRANSFERSTATE <= E_TRAN_STATUS.Aborting
                        && cmd.CHECKCODE.Trim() == SECSConst.HCACK_Confirm
                        select cmd;

            return query.ToList();
        }

        public List<ACMD_MCS> loadMCS_Command_Queue()
        {
            List<ACMD_MCS> ACMD_MCSs = list();
            return ACMD_MCSs;
        }

        public List<ACMD_MCS> loadMCS_Command_Executing()
        {
            List<ACMD_MCS> ACMD_MCSs = null;
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                ACMD_MCSs = cmd_mcsDao.loadACMD_MCSIsExecuting(con);
            }
            return ACMD_MCSs;
        }

        private List<ACMD_MCS> list()
        {
            List<ACMD_MCS> ACMD_MCSs = null;
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                ACMD_MCSs = cmd_mcsDao.loadACMD_MCSIsQueue(con);
            }
            return ACMD_MCSs;
        }

        private List<ACMD_MCS> Sort(List<ACMD_MCS> list_cmd_mcs)
        {
            list_cmd_mcs = list_cmd_mcs.OrderBy(cmd => cmd.CMD_INSER_TIME).ToList();
            return list_cmd_mcs;

        }


        public int getCMD_MCSInserCountLastHour(int hours)
        {
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                return cmd_mcsDao.getCMD_MCSInserCountLastHour(con, hours);
            }
        }
        public int getCMD_MCSFinishCountLastHour(int hours)
        {
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                return cmd_mcsDao.getCMD_MCSFinishCountLastHours(con, hours);
            }
        }

        public bool assignCommnadToVehicle(string mcs_id, string vh_id, out string result)
        {
            try
            {
                ACMD_MCS ACMD_MCS = scApp.CMDBLL.getCMD_MCSByID(mcs_id);
                if (ACMD_MCS != null)
                {
                    bool check_result = true;
                    result = "OK";
                    //ACMD_MCS excute_cmd = ACMD_MCSs[0];
                    string hostsource = ACMD_MCS.HOSTSOURCE;
                    string hostdest = ACMD_MCS.HOSTDESTINATION;
                    string from_adr = string.Empty;
                    string to_adr = string.Empty;
                    AVEHICLE vh = null;
                    E_VH_TYPE vh_type = E_VH_TYPE.None;
                    E_CMD_TYPE cmd_type = default(E_CMD_TYPE);

                    //確認 source 是否為Port
                    bool source_is_a_port = scApp.PortStationBLL.OperateCatch.IsExist(hostsource);
                    if (source_is_a_port)
                    {
                        scApp.MapBLL.getAddressID(hostsource, out from_adr, out vh_type);
                        vh = scApp.VehicleBLL.cache.getVehicle(vh_id);
                        cmd_type = E_CMD_TYPE.LoadUnload;
                    }
                    else
                    {
                        result = "Source must be a port.";
                        return false;
                    }
                    scApp.MapBLL.getAddressID(hostdest, out to_adr);
                    if (vh != null)
                    {
                        if (vh.ACT_STATUS != VHActionStatus.Commanding)
                        {
                            bool btemp = AssignMCSCommand2Vehicle(ACMD_MCS, cmd_type, vh);
                            if (!btemp)
                            {
                                result = "Assign command to vehicle failed.";
                                return false;
                            }
                        }
                        else
                        {
                            result = "Vehicle already have command.";
                            return false;

                        }

                    }
                    else
                    {
                        result = $"Can not find vehicle:{vh_id}.";
                        return false;
                    }
                    return true;
                }
                else
                {
                    result = $"Can not find command:{mcs_id}.";
                    return false;
                }
            }
            finally
            {
                System.Threading.Interlocked.Exchange(ref syncTranCmdPoint, 0);
            }
        }

        public bool commandShift(string mcs_id, string vh_id, out string result)
        {
            try
            {
                //1. Cancel命令
                CMDCancelType cnacel_type = default(CMDCancelType);
                cnacel_type = CMDCancelType.CmdCancel;
                bool btemp = scApp.VehicleService.doCancelCommandByMCSCmdIDWithNoReport(mcs_id, cnacel_type);
                if (btemp)
                {
                    result = "OK";
                }
                else
                {
                    result = $"Transfer command:[{mcs_id}] cancel failed.";
                }
                //2. Unassign Vehicle
                //3. 分派命令給新車(不能報command initial)

                ACMD_MCS ACMD_MCS = scApp.CMDBLL.getCMD_MCSByID(mcs_id);
                if (ACMD_MCS != null)
                {
                    bool check_result = true;
                    result = "OK";
                    //ACMD_MCS excute_cmd = ACMD_MCSs[0];
                    string hostsource = ACMD_MCS.HOSTSOURCE;
                    string hostdest = ACMD_MCS.HOSTDESTINATION;
                    string from_adr = string.Empty;
                    string to_adr = string.Empty;
                    AVEHICLE vh = null;
                    E_VH_TYPE vh_type = E_VH_TYPE.None;
                    E_CMD_TYPE cmd_type = default(E_CMD_TYPE);

                    //確認 source 是否為Port
                    bool source_is_a_port = scApp.PortStationBLL.OperateCatch.IsExist(hostsource);
                    if (source_is_a_port)
                    {
                        scApp.MapBLL.getAddressID(hostsource, out from_adr, out vh_type);
                        vh = scApp.VehicleBLL.cache.getVehicle(vh_id);
                        cmd_type = E_CMD_TYPE.LoadUnload;
                    }
                    else
                    {
                        result = "Source must be a port.";
                        return false;
                    }
                    scApp.MapBLL.getAddressID(hostdest, out to_adr);
                    if (vh != null)
                    {
                        if (vh.ACT_STATUS != VHActionStatus.Commanding)
                        {
                            bool temp = AssignMCSCommand2Vehicle(ACMD_MCS, cmd_type, vh);
                            if (!temp)
                            {
                                result = "Assign command to vehicle failed.";
                                return false;
                            }
                        }
                        else
                        {
                            result = "Vehicle already have command.";
                            return false;

                        }

                    }
                    else
                    {
                        result = $"Can not find vehicle:{vh_id}.";
                        return false;
                    }
                    return true;
                }
                else
                {
                    result = $"Can not find command:{mcs_id}.";
                    return false;
                }
            }
            finally
            {
                System.Threading.Interlocked.Exchange(ref syncTranCmdPoint, 0);
            }
        }


        const int HIGHT_PRIORITY_VALUE = 99;
        private long syncTranCmdPoint = 0;
        public void checkMCSTransferCommand()
        {
            if (System.Threading.Interlocked.Exchange(ref syncTranCmdPoint, 1) == 0)
            {
                try
                {
                    if (scApp.getEQObjCacheManager().getLine().ServiceMode
                        != SCAppConstants.AppServiceMode.Active)
                        return;

                    //if (scApp.getEQObjCacheManager().getLine().SCStats != ALINE.TSCState.AUTO)
                    //    return;
                    //if (DebugParameter.CanAutoRandomGeneratesCommand || scApp.getEQObjCacheManager().getLine().SCStats == ALINE.TSCState.AUTO)

                    if (DebugParameter.CanAutoRandomGeneratesCommand || (scApp.getEQObjCacheManager().getLine().SCStats == ALINE.TSCState.AUTO && scApp.getEQObjCacheManager().getLine().MCSCommandAutoAssign))
                    {
                        List<ACMD_MCS> ACMD_MCSs = scApp.CMDBLL.loadMCS_Command_Queue();
                        bool has_hight_priority_command = ACMD_MCSs.Where(cmd => cmd.PRIORITY_SUM >= HIGHT_PRIORITY_VALUE).Count() > 0;

                        if (ACMD_MCSs != null && ACMD_MCSs.Count > 0)
                        {
                            if (!has_hight_priority_command)
                            {
                                foreach (ACMD_MCS waitting_excute_mcs_cmd in ACMD_MCSs)
                                {
                                    string hostsource = waitting_excute_mcs_cmd.HOSTSOURCE;
                                    bool source_is_a_port = scApp.PortStationBLL.OperateCatch.IsExist(hostsource);
                                    if (source_is_a_port)
                                    {
                                        AVEHICLE bestSuitableVh = null;
                                        var find_result = tryFindOnTheWayVehicle(hostsource);
                                        if (find_result.isFind)
                                        {
                                            bestSuitableVh = find_result.onTheWayVh;
                                            if (AssignMCSCommand2Vehicle(waitting_excute_mcs_cmd, E_CMD_TYPE.LoadUnload, bestSuitableVh))
                                            {
                                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(CMDBLL), Device: string.Empty,
                                                              Data: $"Success excute on the way command, command id:{waitting_excute_mcs_cmd.CMD_ID.Trim()}" +
                                                                    $"vh target port:{bestSuitableVh.ToPort?.PORT_ID},command source port:{waitting_excute_mcs_cmd.HOSTSOURCE?.Trim()}",
                                                              XID: waitting_excute_mcs_cmd.CMD_ID);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }

                            foreach (ACMD_MCS waitting_excute_mcs_cmd in ACMD_MCSs)
                            {
                                string check_result = "";
                                //ACMD_MCS excute_cmd = ACMD_MCSs[0];
                                string hostsource = waitting_excute_mcs_cmd.HOSTSOURCE;
                                string hostdest = waitting_excute_mcs_cmd.HOSTDESTINATION;
                                string from_adr = string.Empty;
                                string to_adr = string.Empty;
                                AVEHICLE bestSuitableVh = null;
                                E_VH_TYPE vh_type = E_VH_TYPE.None;
                                E_CMD_TYPE cmd_type = default(E_CMD_TYPE);

                                //      bool sourceIsVh = scApp.getEQObjCacheManager().getVehicletByVHID(hostsource) != null;

                                //確認 source 是否為Port
                                bool source_is_a_port = scApp.PortStationBLL.OperateCatch.IsExist(hostsource);
                                if (source_is_a_port)
                                {
                                    scApp.MapBLL.getAddressID(hostsource, out from_adr, out vh_type);
                                    bestSuitableVh = scApp.VehicleBLL.findBestSuitableVhStepByStepFromAdr_New(from_adr, vh_type);
                                    cmd_type = E_CMD_TYPE.LoadUnload;
                                }
                                else
                                {
                                    bestSuitableVh = scApp.VehicleBLL.cache.getVehicleByRealID(hostsource);
                                    cmd_type = E_CMD_TYPE.Unload;
                                }


                                scApp.MapBLL.getAddressID(hostdest, out to_adr);
                                string vehicleId = string.Empty;
                                if (bestSuitableVh != null)
                                    vehicleId = bestSuitableVh.VEHICLE_ID.Trim();
                                else
                                {
                                    //int AccumulateTime_minute = 1;
                                    int AccumulateTime_minute = 2;
                                    int current_time_priority = (DateTime.Now - waitting_excute_mcs_cmd.CMD_INSER_TIME).Minutes / AccumulateTime_minute;
                                    if (current_time_priority != waitting_excute_mcs_cmd.TIME_PRIORITY)
                                    {
                                        int change_priority = current_time_priority - waitting_excute_mcs_cmd.TIME_PRIORITY;
                                        updateCMD_MCS_TimePriority(waitting_excute_mcs_cmd, current_time_priority);
                                        updateCMD_MCS_PrioritySUM(waitting_excute_mcs_cmd, waitting_excute_mcs_cmd.PRIORITY_SUM + change_priority);
                                    }
                                    continue;
                                }
                                AssignMCSCommand2Vehicle(waitting_excute_mcs_cmd, cmd_type, bestSuitableVh);
                            }
                        }
                    }
                }
                finally
                {
                    System.Threading.Interlocked.Exchange(ref syncTranCmdPoint, 0);
                }
            }
        }

        const string WTO_GROUP_NAME = "AAWTO400";
        public virtual void checkMCSTransferCommand_New()
        {
            if (System.Threading.Interlocked.Exchange(ref syncTranCmdPoint, 1) == 0)
            {
                try
                {
                    if (scApp.getEQObjCacheManager().getLine().ServiceMode
                        != SCAppConstants.AppServiceMode.Active)
                        return;

                    //if (scApp.getEQObjCacheManager().getLine().SCStats != ALINE.TSCState.AUTO)
                    //    return;
                    //if (DebugParameter.CanAutoRandomGeneratesCommand || scApp.getEQObjCacheManager().getLine().SCStats == ALINE.TSCState.AUTO)

                    if (DebugParameter.CanAutoRandomGeneratesCommand || (scApp.getEQObjCacheManager().getLine().SCStats == ALINE.TSCState.AUTO && scApp.getEQObjCacheManager().getLine().MCSCommandAutoAssign))
                    {
                        int idle_vh_count = scApp.VehicleBLL.cache.getVhCurrentStatusInIdleCount(scApp.CMDBLL);
                        if (idle_vh_count > 0)
                        {
                            List<ACMD_MCS> ACMD_MCSs = scApp.CMDBLL.loadMCS_Command_Queue();
                            checkOnlyOneExcuteWTOCommand(ref ACMD_MCSs);
                            //List<ACMD_MCS> wto_command = null;
                            List<ACMD_MCS> port_priority_max_command = null;
                            if (ACMD_MCSs != null && ACMD_MCSs.Count > 0)
                            {
                                //string WTO_GROUP_NAME = "AAWTO400";
                                ////先找是否有CST要到從WTO出來的，如果有找看看有沒有要去WTO的貨，有的話就先把它搬過去，
                                ////如果找不到要去WTO的Command就直接去把它搬出來
                                //List<ACMD_MCS> from_wto_command = ACMD_MCSs.
                                //    Where(cmd => SCUtility.isMatche(cmd.SOURCE_GROUP_NAME, WTO_GROUP_NAME)).
                                //    ToList();
                                //if (from_wto_command != null && from_wto_command.Count > 0)
                                //{
                                //    List<ACMD_MCS> to_wto_command = ACMD_MCSs.
                                //    Where(cmd => SCUtility.isMatche(cmd.DESTINATION_GROUP_NAME, WTO_GROUP_NAME)).
                                //    ToList();
                                //    if (to_wto_command != null && to_wto_command.Count > 0)
                                //    {
                                //        wto_command = to_wto_command;
                                //    }
                                //    else
                                //    {
                                //        wto_command = from_wto_command;
                                //    }
                                //}
                                port_priority_max_command = new List<ACMD_MCS>();
                                foreach (ACMD_MCS cmd in ACMD_MCSs)
                                {
                                    APORTSTATION source_port = scApp.getEQObjCacheManager().getPortStation(cmd.HOSTSOURCE);
                                    APORTSTATION destination_port = scApp.getEQObjCacheManager().getPortStation(cmd.HOSTDESTINATION);
                                    if (source_port != null && source_port.PRIORITY >= SCAppConstants.PortMaxPriority)
                                    {
                                        if (destination_port != null)
                                        {
                                            if (source_port.PRIORITY >= destination_port.PRIORITY)
                                            {
                                                cmd.PORT_PRIORITY = source_port.PRIORITY;
                                            }
                                            else
                                            {
                                                cmd.PORT_PRIORITY = destination_port.PRIORITY;
                                            }
                                        }
                                        else
                                        {
                                            cmd.PORT_PRIORITY = source_port.PRIORITY;
                                        }
                                        port_priority_max_command.Add(cmd);
                                        continue;
                                    }
                                    if (destination_port != null && destination_port.PRIORITY >= SCAppConstants.PortMaxPriority)
                                    {
                                        if (source_port != null)
                                        {
                                            if (destination_port.PRIORITY >= source_port.PRIORITY)
                                            {
                                                cmd.PORT_PRIORITY = destination_port.PRIORITY;
                                            }
                                            else
                                            {
                                                cmd.PORT_PRIORITY = source_port.PRIORITY;
                                            }
                                        }
                                        else
                                        {
                                            cmd.PORT_PRIORITY = destination_port.PRIORITY;
                                        }
                                        port_priority_max_command.Add(cmd);
                                        continue;
                                    }
                                }
                                if (port_priority_max_command.Count == 0)
                                {
                                    port_priority_max_command = null;
                                }
                                else
                                {
                                    port_priority_max_command = port_priority_max_command.OrderByDescending(cmd => cmd.PORT_PRIORITY).ToList();
                                }

                                List<ACMD_MCS> timeout_command = null;
                                //bool has_hight_priority_command = ACMD_MCSs.Where(cmd => cmd.PRIORITY_SUM >= HIGHT_PRIORITY_VALUE).Count() > 0;
                                //var hight_priority_command = ACMD_MCSs.Where(cmd => cmd.PRIORITY_SUM >= HIGHT_PRIORITY_VALUE).ToList();
                                //if (wto_command == null && SystemParameter.CSTMaxWaitTime != 0)
                                if (port_priority_max_command == null && SystemParameter.CSTMaxWaitTime != 0)
                                {
                                    timeout_command = ACMD_MCSs.
                                                      Where(cmd => DateTime.Now >= cmd.CMD_INSER_TIME.AddMinutes(SystemParameter.CSTMaxWaitTime)).
                                                      ToList();
                                    //if (timeout_command != null && timeout_command.Count > 0)
                                    //{
                                    //    string PRIORITY_EQ_NAME = "AAWTO400";
                                    //    List<ACMD_MCS> wto_first = null;
                                    //    //1.先找Desc port有沒有WTO的，如果有的話這樣有機會搬過去 就可以順途搬回來
                                    //    wto_first = timeout_command.Where(cmd => cmd.HOSTDESTINATION.Trim().StartsWith(PRIORITY_EQ_NAME)).ToList();
                                    //    //2.沒有的話再找Source port有沒有WTO的
                                    //    if (wto_first == null || wto_first.Count == 0)
                                    //    {
                                    //        wto_first = timeout_command.Where(cmd => cmd.HOSTSOURCE.Trim().StartsWith(PRIORITY_EQ_NAME)).ToList();
                                    //    }
                                    //    //3.如果有wto是在Time out list中，則就優先找出來做
                                    //    if (wto_first != null && wto_first.Count > 0)
                                    //    {
                                    //        timeout_command = wto_first;
                                    //    }
                                    //    //如果都沒有Timeout的List都沒有WTO的話，就不要走Timeout流程
                                    //    else
                                    //    {
                                    //        timeout_command = null;
                                    //    }
                                    //}
                                }
                                //LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(CMDBLL), Device: string.Empty,
                                //              Data: $"Has hight priority :{hight_priority_command != null}");

                                //if (hight_priority_command == null)
                                //{

                                //List<ACMD_MCS> search_nearest_mcs_cmd = timeout_command == null || timeout_command.Count == 0 ?
                                //                                        ACMD_MCSs : timeout_command;
                                List<ACMD_MCS> search_nearest_mcs_cmd = null;
                                //if (wto_command != null && wto_command.Count > 0)
                                //{
                                //    search_nearest_mcs_cmd = wto_command;
                                //}
                                if (port_priority_max_command != null && port_priority_max_command.Count > 0)
                                {
                                    search_nearest_mcs_cmd = port_priority_max_command;
                                }
                                else if (timeout_command != null && timeout_command.Count > 0)
                                {
                                    search_nearest_mcs_cmd = timeout_command;
                                }
                                else
                                {
                                    search_nearest_mcs_cmd = ACMD_MCSs;
                                }

                                AVEHICLE nearest_vh = null;
                                ACMD_MCS nearest_cmd_mcs = null;
                                List<AVEHICLE> vhs = scApp.VehicleBLL.cache.loadAllVh().ToList();
                                scApp.VehicleBLL.filterVh(ref vhs, E_VH_TYPE.None);
                                (nearest_vh, nearest_cmd_mcs) = FindNearestVhAndCommand(vhs, search_nearest_mcs_cmd);
                                if (nearest_vh != null && nearest_cmd_mcs != null)
                                {
                                    if (AssignMCSCommand2Vehicle(nearest_cmd_mcs, E_CMD_TYPE.LoadUnload, nearest_vh))
                                    {
                                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(CMDBLL), Device: string.Empty,
                                                      Data: $"Success find nearest mcs command for vh:{nearest_vh.VEHICLE_ID}, command id:{nearest_cmd_mcs.CMD_ID.Trim()}" +
                                                            $"vh current address:{nearest_vh.CUR_ADR_ID},command source port:{nearest_cmd_mcs.HOSTSOURCE?.Trim()}",
                                                      XID: nearest_cmd_mcs.CMD_ID);
                                        return;
                                    }
                                }
                                //foreach (ACMD_MCS waitting_excute_mcs_cmd in ACMD_MCSs)
                                //{
                                //    string hostsource = waitting_excute_mcs_cmd.HOSTSOURCE;
                                //    bool source_is_a_port = scApp.PortStationBLL.OperateCatch.IsExist(hostsource);
                                //    if (source_is_a_port)
                                //    {
                                //        AVEHICLE bestSuitableVh = null;
                                //        var find_result = tryFindOnTheWayVehicle(hostsource);
                                //        if (find_result.isFind)
                                //        {
                                //            bestSuitableVh = find_result.onTheWayVh;
                                //            if (AssignMCSCommand2Vehicle(waitting_excute_mcs_cmd, E_CMD_TYPE.LoadUnload, bestSuitableVh))
                                //            {
                                //                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(CMDBLL), Device: string.Empty,
                                //                              Data: $"Success excute on the way command, command id:{waitting_excute_mcs_cmd.CMD_ID.Trim()}" +
                                //                                    $"vh target port:{bestSuitableVh.ToPort?.PORT_ID},command source port:{waitting_excute_mcs_cmd.HOSTSOURCE?.Trim()}",
                                //                              XID: waitting_excute_mcs_cmd.CMD_ID);
                                //                return;
                                //            }
                                //        }
                                //    }
                                //}
                                //}
                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(CMDBLL), Device: string.Empty,
                                              Data: $"Start process normal command search...");

                                foreach (ACMD_MCS first_waitting_excute_mcs_cmd in ACMD_MCSs)
                                {
                                    string check_result = "";
                                    //ACMD_MCS excute_cmd = ACMD_MCSs[0];
                                    string hostsource = first_waitting_excute_mcs_cmd.HOSTSOURCE;
                                    string hostdest = first_waitting_excute_mcs_cmd.HOSTDESTINATION;
                                    string from_adr = string.Empty;
                                    string to_adr = string.Empty;
                                    AVEHICLE bestSuitableVh = null;
                                    E_VH_TYPE vh_type = E_VH_TYPE.None;
                                    E_CMD_TYPE cmd_type = default(E_CMD_TYPE);

                                    //      bool sourceIsVh = scApp.getEQObjCacheManager().getVehicletByVHID(hostsource) != null;

                                    //確認 source 是否為Port
                                    bool source_is_a_port = scApp.PortStationBLL.OperateCatch.IsExist(hostsource);
                                    if (source_is_a_port)
                                    {
                                        scApp.MapBLL.getAddressID(hostsource, out from_adr, out vh_type);
                                        bestSuitableVh = scApp.VehicleBLL.findBestSuitableVhStepByStepFromAdr_New(from_adr, vh_type);
                                        cmd_type = E_CMD_TYPE.LoadUnload;
                                    }
                                    else
                                    {
                                        bestSuitableVh = scApp.VehicleBLL.cache.getVehicleByRealID(hostsource);
                                        if (bestSuitableVh.MODE_STATUS != VHModeStatus.AutoRemote ||
                                            bestSuitableVh.BatteryLevel == BatteryLevel.Low)
                                        {
                                            //LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(CMDBLL), Device: string.Empty,
                                            //              Data: $"MCS command:{nearest_cmd_mcs.CMD_ID} specify of vehicle:{hostsource} not ready," +
                                            //                    $"mode status:{bestSuitableVh.MODE_STATUS},battery level:{bestSuitableVh.BatteryLevel}",
                                            //              XID: nearest_cmd_mcs.CMD_ID);
                                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(CMDBLL), Device: string.Empty,
                                                          Data: $"MCS command:{first_waitting_excute_mcs_cmd.CMD_ID} specify of vehicle:{hostsource} not ready," +
                                                                $"mode status:{bestSuitableVh.MODE_STATUS},battery level:{bestSuitableVh.BatteryLevel}",
                                                          XID: first_waitting_excute_mcs_cmd.CMD_ID);
                                            continue;
                                        }
                                        cmd_type = E_CMD_TYPE.Unload;
                                    }


                                    scApp.MapBLL.getAddressID(hostdest, out to_adr);
                                    //string vehicleId = string.Empty;
                                    if (bestSuitableVh != null)
                                    {
                                        //vehicleId = bestSuitableVh.VEHICLE_ID.Trim();
                                        if (AssignMCSCommand2Vehicle(first_waitting_excute_mcs_cmd, cmd_type, bestSuitableVh))
                                        {
                                            //return;
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        //int AccumulateTime_minute = 1;
                                        //int AccumulateTime_minute = 2;
                                        //int current_time_priority = (DateTime.Now - first_waitting_excute_mcs_cmd.CMD_INSER_TIME).Minutes / AccumulateTime_minute;
                                        //if (current_time_priority != first_waitting_excute_mcs_cmd.TIME_PRIORITY)
                                        //{
                                        //    int change_priority = current_time_priority - first_waitting_excute_mcs_cmd.TIME_PRIORITY;
                                        //    updateCMD_MCS_TimePriority(first_waitting_excute_mcs_cmd, current_time_priority);
                                        //    updateCMD_MCS_PrioritySUM(first_waitting_excute_mcs_cmd, first_waitting_excute_mcs_cmd.PRIORITY_SUM + change_priority);
                                        //}
                                        //continue;
                                    }
                                }
                            }


                            foreach (ACMD_MCS waitting_excute_mcs_cmd in ACMD_MCSs)
                            {
                                int AccumulateTime_minute = 1;
                                //int AccumulateTime_minute = 2;
                                int current_time_priority = (int)((DateTime.Now - waitting_excute_mcs_cmd.CMD_INSER_TIME).TotalMinutes / AccumulateTime_minute);
                                if (current_time_priority != waitting_excute_mcs_cmd.TIME_PRIORITY)
                                {
                                    int change_priority = current_time_priority - waitting_excute_mcs_cmd.TIME_PRIORITY;
                                    updateCMD_MCS_TimePriority(waitting_excute_mcs_cmd, current_time_priority);
                                    updateCMD_MCS_PrioritySUM(waitting_excute_mcs_cmd, waitting_excute_mcs_cmd.PRIORITY_SUM + change_priority);
                                }
                            }
                        }
                    }
                }
                finally
                {
                    System.Threading.Interlocked.Exchange(ref syncTranCmdPoint, 0);
                }
            }
        }

        public virtual bool createWaitingRetryOHTCCmd(string vhID, string mcs_cmd_ID)
        {
            return false;
        }

        private void checkOnlyOneExcuteWTOCommand(ref List<ACMD_MCS> InQueueACMD_MCSs)
        {
            if (InQueueACMD_MCSs != null && InQueueACMD_MCSs.Count > 0)
            {
                bool has_wto_command_in_queue = InQueueACMD_MCSs.Where(mcs_cmd => mcs_cmd.HOSTSOURCE.Contains(WTO_GROUP_NAME) ||
                                                                                  mcs_cmd.HOSTDESTINATION.Contains(WTO_GROUP_NAME)).Count() != 0;
                if (has_wto_command_in_queue)
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(CMDBLL), Device: string.Empty,
                                  Data: $"Has wto command in queue, start check has orther wto command excute...");

                    bool has_excute_wto_commnad = scApp.CMDBLL.hasCMD_MCSExcuteByFromToPort(WTO_GROUP_NAME);
                    if (has_excute_wto_commnad)
                    {
                        foreach (var mcs_cmd in InQueueACMD_MCSs.ToList())
                        {
                            if (mcs_cmd.HOSTSOURCE.Contains(WTO_GROUP_NAME) ||
                                mcs_cmd.HOSTDESTINATION.Contains(WTO_GROUP_NAME))
                            {
                                InQueueACMD_MCSs.Remove(mcs_cmd);
                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(CMDBLL), Device: string.Empty,
                                              Data: $"Has orther wto command excute, remove it:{SCUtility.Trim(mcs_cmd.CMD_ID)}");
                            }
                        }
                    }
                    else
                    {
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(CMDBLL), Device: string.Empty,
                                      Data: $"Has wto command in queue, no wto command excute.");
                    }
                }
            }
        }

        private (AVEHICLE nearestVh, ACMD_MCS nearestCmdMcs) FindNearestVhAndCommand(List<AVEHICLE> vhs, List<ACMD_MCS> ACMD_MCSs)
        {
            AVEHICLE nearest_vh = null;
            ACMD_MCS nearest_cmd_mcs = null;
            double minimum_cost = double.MaxValue;
            try
            {
                foreach (var vh in vhs)
                {
                    foreach (var mcs_cmd in ACMD_MCSs)
                    {
                        string hostsource = mcs_cmd.HOSTSOURCE;
                        string from_adr = string.Empty;
                        bool source_is_a_port = scApp.PortStationBLL.OperateCatch.IsExist(hostsource);
                        if (!source_is_a_port) continue;

                        E_VH_TYPE vh_type = E_VH_TYPE.None;
                        scApp.MapBLL.getAddressID(hostsource, out from_adr, out vh_type);

                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(CMDBLL), Device: string.Empty,
                                      Data: $"Start calculation distance, command id:{mcs_cmd.CMD_ID.Trim()} command source port:{mcs_cmd.HOSTSOURCE?.Trim()}," +
                                            $"vh:{vh.VEHICLE_ID} current adr:{vh.CUR_ADR_ID},from adr:{from_adr} ...",
                                      XID: mcs_cmd.CMD_ID);
                        var result = scApp.GuideBLL.getGuideInfo(vh.CUR_ADR_ID, from_adr);
                        //double total_section_distance = result.guideSectionIds != null && result.guideSectionIds.Count > 0 ?
                        //                                scApp.SectionBLL.cache.GetSectionsDistance(result.guideSectionIds) : 0;
                        double total_section_distance = 0;
                        if (result.isSuccess)
                        {
                            total_section_distance = result.guideSectionIds != null && result.guideSectionIds.Count > 0 ?
                                                            scApp.SectionBLL.cache.GetSectionsDistance(result.guideSectionIds) : 0;
                        }
                        else
                        {
                            total_section_distance = 99999999;
                        }
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(CMDBLL), Device: string.Empty,
                                      Data: $"command id:{mcs_cmd.CMD_ID.Trim()} command source port:{mcs_cmd.HOSTSOURCE?.Trim()}," +
                                            $"vh:{vh.VEHICLE_ID} current adr:{vh.CUR_ADR_ID},from adr:{from_adr} distance:{total_section_distance}",
                                      XID: mcs_cmd.CMD_ID);
                        if (total_section_distance < minimum_cost)
                        {
                            nearest_cmd_mcs = mcs_cmd;
                            nearest_vh = vh;
                            minimum_cost = total_section_distance;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                nearest_vh = null;
                nearest_cmd_mcs = null;
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(CMDBLL), Device: string.Empty,
                   Data: ex);
            }
            return (nearest_vh, nearest_cmd_mcs);
        }

        private (bool isFind, AVEHICLE onTheWayVh) tryFindOnTheWayVehicle(string cmdSourcePort)
        {
            APORTSTATION cmd_source_port = scApp.PortStationBLL.OperateCatch.getPortStation(cmdSourcePort);
            AEQPT eqpt = scApp.getEQObjCacheManager().getEquipmentByEQPTID(cmd_source_port.EQPT_ID);
            if (eqpt.Type != SCAppConstants.EqptType.Stock) return (false, null);

            List<AVEHICLE> vhs = scApp.VehicleBLL.cache.loadAllVh();
            foreach (AVEHICLE vh in vhs)
            {
                if (vh.ACT_STATUS == VHActionStatus.Commanding)
                {
                    if (vh.ToPort != null)
                    {
                        if (SCUtility.isMatche(cmd_source_port.GroupID, vh.ToPort.GroupID))
                        {
                            return (true, vh);
                        }
                    }
                }
            }
            return (false, null);
        }


        private bool AssignMCSCommand2Vehicle(ACMD_MCS waittingExcuteMcsCmd, E_CMD_TYPE cmdType, AVEHICLE bestSuitableVh)
        {
            string hostsource = waittingExcuteMcsCmd.HOSTSOURCE;
            string hostdest = waittingExcuteMcsCmd.HOSTDESTINATION;
            var source_port = scApp.PortStationBLL.OperateCatch.getPortStation(hostsource);
            var dest_port = scApp.PortStationBLL.OperateCatch.getPortStation(hostdest);
            string from_adr = source_port == null ? string.Empty : source_port.ADR_ID;
            string to_adr = dest_port == null ? string.Empty : dest_port.ADR_ID;

            return AssignMCSCommand2Vehicle(waittingExcuteMcsCmd, from_adr, to_adr, cmdType, bestSuitableVh);
        }
        private bool AssignMCSCommand2Vehicle(ACMD_MCS waittingExcuteMcsCmd, string fromAdr, string toAdr, E_CMD_TYPE cmdType, AVEHICLE bestSuitableVh)
        {
            bool isSuccess = true;
            string best_suitable_vh_id = bestSuitableVh.VEHICLE_ID;
            string mcs_cmd_id = waittingExcuteMcsCmd.CMD_ID;
            string carrier_id = waittingExcuteMcsCmd.CARRIER_ID;
            List<AMCSREPORTQUEUE> reportqueues = new List<AMCSREPORTQUEUE>();
            using (TransactionScope tx = SCUtility.getTransactionScope())
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {

                    isSuccess &= scApp.CMDBLL.doCreatTransferCommand(best_suitable_vh_id, mcs_cmd_id, carrier_id,
                                        cmdType,
                                        fromAdr,
                                        toAdr, 0, 0);
                    if (isSuccess)
                    {
                        //isSuccess &= scApp.CMDBLL.updateCMD_MCS_TranStatus2Initial(waitting_excute_mcs_cmd.CMD_ID);
                        //isSuccess &= scApp.ReportBLL.newReportTransferInitial(waitting_excute_mcs_cmd.CMD_ID, reportqueues);
                        isSuccess &= scApp.CMDBLL.updateCMD_MCS_TranStatus2PreInitial(waittingExcuteMcsCmd.CMD_ID);
                    }
                    if (isSuccess && !SCUtility.isEmpty(bestSuitableVh.OHTC_CMD))
                    {
                        //AVEHICLE VhCatchObj = scApp.getEQObjCacheManager().getVehicletByVHID(bestSuitableVh.VEHICLE_ID);
                        //isSuccess = bestSuitableVh.sned_Str37(bestSuitableVh.OHTC_CMD, CMDCancelType.CmdCancel);
                    }
                    scApp.ReportBLL.insertMCSReport(reportqueues);
                    if (isSuccess)
                    {
                        tx.Complete();
                    }
                    else
                    {
                        return isSuccess;
                    }
                    //else
                    //{
                    //    continue;
                    //}
                }
                //bool isSuccess = scApp.CMDBLL.creatCommand_OHTC(vehicleId, excute_cmd.CMD_ID, excute_cmd.CARRIER_ID,
                //                    E_CMD_TYPE.LoadUnload,
                //                    fromadr,
                //                    toAdr, 0, 0,
                //                    out cmd);
                //if (isSuccess)
                //{
                //    updateCMD_MCS_TranStatus2Initial(excute_cmd.CMD_ID);
                //    //scApp.CMDBLL.updateCMD_MCS_TranStatus2Initial(excute_cmd.CMD_ID);
                //    Task.Run(() => { scApp.CMDBLL.generateCmd_OHTC_Details(); });
                //}

            }
            scApp.ReportBLL.newSendMCSMessage(reportqueues);
            checkOHxC_TransferCommand();
            return isSuccess;
        }

        public List<TranTask> loadTranTasks()
        {
            return testTranTaskDao.loadTransferTasks_ACycle(scApp.TranCmdPeriodicDataSet.Tables[0]);
        }

        public Dictionary<int, List<TranTask>> loadTranTaskSchedule_24Hour()
        {
            List<TranTask> allTranTaskType = testTranTaskDao.loadTransferTasks_24Hour(scApp.TranCmdPeriodicDataSet.Tables[0]);
            Dictionary<int, List<TranTask>> dicTranTaskSchedule = new Dictionary<int, List<TranTask>>();
            var query = from tranTask in allTranTaskType
                        group tranTask by tranTask.Sec;

            dicTranTaskSchedule = query.OrderBy(item => item.Key).ToDictionary(item => item.Key, item => item.ToList());

            return dicTranTaskSchedule;
        }
        public Dictionary<string, List<TranTask>> loadTranTaskSchedule_Clear_Dirty()
        {
            List<TranTask> allTranTaskType = testTranTaskDao.loadTransferTasks_24Hour(scApp.TranCmdPeriodicDataSet.Tables[1]);
            Dictionary<string, List<TranTask>> dicTranTaskSchedule = new Dictionary<string, List<TranTask>>();
            var query = from tranTask in allTranTaskType
                        group tranTask by tranTask.CarType;

            dicTranTaskSchedule = query.OrderBy(item => item.Key).ToDictionary(item => item.Key, item => item.ToList());

            return dicTranTaskSchedule;
        }
        public E_TRAN_STATUS CompleteStatusToETransferStatus(CompleteStatus completeStatus)
        {
            switch (completeStatus)
            {
                case CompleteStatus.CmpStatusCancel:
                    return E_TRAN_STATUS.Canceled;
                case CompleteStatus.CmpStatusAbort:
                case CompleteStatus.CmpStatusVehicleAbort:
                case CompleteStatus.CmpStatusIdmisMatch:
                case CompleteStatus.CmpStatusIdreadFailed:
                case CompleteStatus.CmpStatusInterlockError:
                case CompleteStatus.CmpStatusLongTimeInaction:
                case CompleteStatus.CmpStatusForceFinishByOp:
                    return E_TRAN_STATUS.Aborted;
                default:
                    return E_TRAN_STATUS.Complete;
            }
        }

        public bool hasCMD_MCSExcuteByFromToPort(string containsPortID)
        {
            int count = 0;
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                count = cmd_mcsDao.getCMD_MCSExcutingCountByFromToPort(con, containsPortID);
            }
            return count != 0;
        }

        #endregion CMD_MCS

        #region CMD_OHTC
        public const string CALL_CONTEXT_KEY_WORD_OHTC_CMD_CHECK_RESULT = "OHTC_CMD_CHECK_RESULT";
        public class OHTCCommandCheckResult
        {
            public OHTCCommandCheckResult()
            {
                Num = DateTime.Now.ToString(SCAppConstants.TimestampFormat_19);
                IsSuccess = true;
            }
            public string Num { get; private set; }
            public bool IsSuccess = false;
            public StringBuilder Result = new StringBuilder();
            public override string ToString()
            {
                string message = "Alarm No.:" + Num + Environment.NewLine + Environment.NewLine + Result.ToString();
                return message;
            }
        }

        public bool creatVehicleMoveCommand(string vhID, string destinationAdr)
        {
            return doCreatTransferCommand(vhID, cmd_type: E_CMD_TYPE.Move, destination: destinationAdr);
        }
        public bool doCreatTransferCommand(string vh_id, string cmd_id_mcs = "", string carrier_id = "", E_CMD_TYPE cmd_type = E_CMD_TYPE.Move,
                                   string source = "", string destination = "", int priority = 0, int estimated_time = 0, SCAppConstants.GenOHxCCommandType gen_cmd_type = SCAppConstants.GenOHxCCommandType.Auto)
        {
            ACMD_OHTC cmd_obj = null;

            return doCreatTransferCommand_New(vh_id, out cmd_obj, cmd_id_mcs, carrier_id, cmd_type,
                                    source, destination, priority, estimated_time,
                                    gen_cmd_type);
        }
        public bool doCreatTransferCommand_New(string vh_id, out ACMD_OHTC cmd_obj, string cmd_id_mcs = "", string carrier_id = "", E_CMD_TYPE cmd_type = E_CMD_TYPE.Move,
                                    string source = "", string destination = "", int priority = 0, int estimated_time = 0, SCAppConstants.GenOHxCCommandType gen_cmd_type = SCAppConstants.GenOHxCCommandType.Auto)
        {
            OHTCCommandCheckResult check_result = getOrSetCallContext<OHTCCommandCheckResult>(CALL_CONTEXT_KEY_WORD_OHTC_CMD_CHECK_RESULT);
            cmd_obj = null;
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vh_id);
            string vh_current_adr = vh.CUR_ADR_ID;
            //不是MCS Cmd，要檢查檢查有沒有在執行中的，有則不能Creat

            if (SCUtility.isEmpty(vh_id) || vh == null)
            {
                check_result.Result.AppendLine($" vh id is empty");
                check_result.IsSuccess = false;
            }

            if (!vh.isTcpIpConnect)
            {
                check_result.Result.AppendLine($" vh:{vh_id} no connection");
                check_result.Result.AppendLine($" please check IPC.");
                check_result.IsSuccess = false;
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(CMDBLL), Device: string.Empty,
                              Data: check_result.Result.ToString(),
                              XID: check_result.Num);
                return check_result.IsSuccess;
            }

            if (vh.MODE_STATUS == VHModeStatus.Manual)
            {
                check_result.Result.AppendLine($" vh:{vh_id} is manual");
                check_result.Result.AppendLine($" please change to auto mode.");
                check_result.IsSuccess = false;
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(CMDBLL), Device: string.Empty,
                              Data: check_result.Result.ToString(),
                              XID: check_result.Num);
                return check_result.IsSuccess;
            }

            if (SCUtility.isEmpty(vh_current_adr))
            {
                check_result.Result.AppendLine($" vh:{vh_id} current address is empty");
                check_result.Result.AppendLine($" please excute home command.");
                check_result.Result.AppendLine();
                check_result.IsSuccess = false;
            }
            else
            {
                string result = "";
                if (!IsCommandWalkable(vh_id, cmd_type, vh_current_adr, source, destination, out result))
                {
                    check_result.Result.AppendLine(result);
                    check_result.Result.AppendLine($" please check the path status.");
                    check_result.Result.AppendLine();
                    check_result.IsSuccess = false;
                }
            }
            //如果該筆Command是MCS Cmd，只需要檢查有沒有已經在Queue中的，有則不能Creat
            if (!SCUtility.isEmpty(cmd_id_mcs))
            {
                if (isCMD_OHTCQueueByVh(vh_id))
                {
                    check_result.Result.AppendLine($" vh:{vh_id} comanmd id mcs:{cmd_id_mcs}, has command in queue.");
                    check_result.IsSuccess = false;
                }
            }
            else
            {
                if (isCMD_OHTCExcuteByVh(vh_id))
                {
                    check_result.Result.AppendLine($" vh:{vh_id} has command in excute.");
                    check_result.IsSuccess = false;
                }
            }

            if (check_result.IsSuccess)
            {
                check_result.IsSuccess = creatCommand_OHTC
                                         (vh_id, cmd_id_mcs, carrier_id, cmd_type, source, destination, priority, estimated_time, gen_cmd_type, out cmd_obj);

                if (!check_result.IsSuccess)
                {
                    check_result.Result.AppendLine($" vh:{vh_id} creat command to db unsuccess.");
                }
            }


            if (!check_result.IsSuccess)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(CMDBLL), Device: string.Empty,
                              Data: check_result.Result.ToString(),
                              XID: check_result.Num);
            }
            setCallContext(CALL_CONTEXT_KEY_WORD_OHTC_CMD_CHECK_RESULT, check_result);
            return check_result.IsSuccess;
        }

        public static T getCallContext<T>(string key)
        {
            object obj = System.Runtime.Remoting.Messaging.CallContext.GetData(key);
            if (obj == null)
            {
                return default(T);
            }
            return (T)obj;
        }
        public static T getOrSetCallContext<T>(string key)
        {
            //object obj = System.Runtime.Remoting.Messaging.CallContext.GetData(key);
            //if (obj == null)
            //{
            //    obj = Activator.CreateInstance(typeof(T));
            //    System.Runtime.Remoting.Messaging.CallContext.SetData(key, obj);
            //}
            object obj = Activator.CreateInstance(typeof(T));
            System.Runtime.Remoting.Messaging.CallContext.SetData(key, obj);
            return (T)obj;
        }
        public static void setCallContext<T>(string key, T obj)
        {
            if (obj != null)
            {
                System.Runtime.Remoting.Messaging.CallContext.SetData(key, obj);
            }
        }




        private bool IsCommandWalkable(string vh_id, E_CMD_TYPE cmd_type, string vh_current_adr, string source, string destination, out string result)
        {
            bool is_walk_able = true;
            switch (cmd_type)
            {
                case E_CMD_TYPE.Move:
                case E_CMD_TYPE.Unload:
                case E_CMD_TYPE.Move_Park:
                case E_CMD_TYPE.Move_Charger:
                    if (!scApp.GuideBLL.IsRoadWalkable(vh_current_adr, destination))
                    {
                        result = $" vh:{vh_id} current address:[{vh_current_adr}] to destination address:[{destination}] no traffic allowed";
                        is_walk_able = false;
                    }
                    else
                    {
                        result = "";
                    }
                    break;
                case E_CMD_TYPE.Load:
                    if (!scApp.GuideBLL.IsRoadWalkable(vh_current_adr, source))
                    {
                        result = $" vh:{vh_id} current address:[{vh_current_adr}] to destination address:[{source}] no traffic allowed";
                        is_walk_able = false;
                    }
                    else
                    {
                        result = "";
                    }
                    break;
                case E_CMD_TYPE.LoadUnload:
                    if (!scApp.GuideBLL.IsRoadWalkable(vh_current_adr, source))
                    {
                        result = $" vh:{vh_id} current address:{vh_current_adr} to source address:{source} no traffic allowed";
                        is_walk_able = false;
                    }
                    else if (!scApp.GuideBLL.IsRoadWalkable(source, destination))
                    {
                        result = $" vh:{vh_id} source address:{source} to destination address:{destination} no traffic allowed";
                        is_walk_able = false;
                    }
                    else
                    {
                        result = "";
                    }
                    break;
                default:
                    result = $"Incorrect of command type:{cmd_type}";
                    is_walk_able = false;
                    break;
            }

            return is_walk_able;
        }



        private bool creatCommand_OHTC(string vh_id, string cmd_id_mcs, string carrier_id, E_CMD_TYPE cmd_type,
                                              string source, string destination, int priority, int estimated_time, SCAppConstants.GenOHxCCommandType gen_cmd_type, out ACMD_OHTC cmd_ohtc)
        {
            try
            {
                cmd_ohtc = buildCommand_OHTC(vh_id, cmd_id_mcs, carrier_id, cmd_type, source, destination, priority, estimated_time, gen_cmd_type);

                creatCommand_OHTC(cmd_ohtc);
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(CMDBLL), Device: "AGVC",
                   Data: ex,
                   VehicleID: vh_id,
                   CarrierID: carrier_id);
                cmd_ohtc = null;
                return false;
            }
            return true;
        }



        private void creatCommand_OHTC(ACMD_OHTC cmd)
        {
            bool isSuccess = true;
            //DBConnection_EF con = DBConnection_EF.GetContext();
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                //同步修改至台車資料
                //AVEHICLE vh = scApp.VehicleDao.getByID(con, cmd.VH_ID);
                //if (vh != null)
                //    vh.OHTC_CMD = cmd.CMD_ID;
                cmd_ohtcDAO.add(con, cmd);
            }
        }

        private ACMD_OHTC buildCommand_OHTC(string vh_id, string cmd_id_mcs, string carrier_id, E_CMD_TYPE cmd_type,
                                            string source, string destination, int priority, int estimated_time,
                                            SCAppConstants.GenOHxCCommandType gen_cmd_type, bool is_generate_cmd_id = true)
        {
            string _source = string.Empty;
            string commandID = string.Empty;
            if (is_generate_cmd_id)
            {
                commandID = scApp.SequenceBLL.getCommandID(gen_cmd_type);
            }
            if (cmd_type == E_CMD_TYPE.LoadUnload
                || cmd_type == E_CMD_TYPE.Load)
            {
                _source = source;
            }
            ACMD_OHTC cmd = new ACMD_OHTC
            {
                CMD_ID = commandID,
                VH_ID = vh_id,
                CARRIER_ID = carrier_id,
                CMD_ID_MCS = cmd_id_mcs,
                CMD_TPYE = cmd_type,
                SOURCE = _source,
                DESTINATION = destination,
                PRIORITY = priority,
                //CMD_START_TIME = DateTime.Now,
                CMD_STAUS = E_CMD_STATUS.Queue,
                CMD_PROGRESS = 0,
                ESTIMATED_TIME = estimated_time,
                ESTIMATED_EXCESS_TIME = estimated_time
            };
            return cmd;
        }

        /// <summary>
        /// 根據Command ID更新OHTC的Command狀態
        /// </summary>
        /// <param name="cmd_id"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public bool updateCommand_OHTC_StatusByCmdID(string vhID, string cmd_id, E_CMD_STATUS status)
        {
            bool isSuccess = false;
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                ACMD_OHTC cmd = cmd_ohtcDAO.getByID(con, cmd_id);
                if (cmd != null)
                {
                    if (status == E_CMD_STATUS.Execution)
                    {
                        cmd.CMD_START_TIME = DateTime.Now;
                    }
                    else if (status >= E_CMD_STATUS.NormalEnd)
                    {
                        cmd.CMD_END_TIME = DateTime.Now;
                        cmd_ohtc_detailDAO.DeleteByBatch(con, cmd.CMD_ID);
                    }
                    cmd.CMD_STAUS = status;
                    cmd_ohtcDAO.Update(con, cmd);

                    if (status >= E_CMD_STATUS.NormalEnd)
                    {
                        //scApp.VehicleBLL.updateVehicleExcuteCMD(cmd.VH_ID, string.Empty, string.Empty);                    
                        scApp.VehicleBLL.updateVehicleExcuteCMD(vhID, string.Empty, string.Empty);
                    }

                }
                isSuccess = true;
            }
            return isSuccess;
        }
        public virtual bool updateCommand_OHTC_CarrierID(string cmd_id, string new_carrier_id)
        {
            bool isSuccess = false;
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                ACMD_OHTC cmd = cmd_ohtcDAO.getByID(con, cmd_id);
                if (cmd != null)
                {
                    cmd.CARRIER_ID = new_carrier_id;
                    cmd_ohtcDAO.Update(con, cmd);
                }
                isSuccess = true;
            }
            return isSuccess;
        }
        public bool updateCMD_OHxC_Status2ReadyToReWirte(string cmd_id, out ACMD_OHTC cmd_ohtc)
        {
            bool isSuccess = false;
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                cmd_ohtc = cmd_ohtcDAO.getByID(con, cmd_id);
                //if (cmd != null)
                //{
                //    cmd_ohtc = cmd;
                //    //cmd.CMD_STAUS = E_CMD_STATUS.Queue;
                //    //cmd.CMD_TPYE = E_CMD_TYPE.Override;
                //    //cmd.CMD_TPYE = E_CMD_TYPE.
                //}
                //else
                isSuccess = true;
            }
            return isSuccess;
        }


        public List<ACMD_OHTC> loadCMD_OHTCMDStatusIsQueue()
        {
            List<ACMD_OHTC> acmd_ohtcs = null;
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                acmd_ohtcs = cmd_ohtcDAO.loadAllQueue_Auto(con);
            }
            return acmd_ohtcs;
        }
        public List<ACMD_OHTC> loadFinishCMD_OHTC()
        {
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                return cmd_ohtcDAO.loadFinishCMD_OHT(con);
            }
        }
        public void remoteCMD_OHTCByBatch(List<ACMD_OHTC> cmds)
        {
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                cmd_ohtcDAO.RemoteByBatch(con, cmds);
            }
        }
        public ACMD_OHTC getCMD_OHTCByStatusSending()
        {
            ACMD_OHTC cmd_ohtc = null;
            using (DBConnection_EF con = new DBConnection_EF())
            {
                cmd_ohtc = cmd_ohtcDAO.getCMD_OHTCByStatusSending(con);
            }
            return cmd_ohtc;
        }
        public ACMD_OHTC getCMD_OHTCByVehicleID(string vh_id)
        {
            ACMD_OHTC cmd_ohtc = null;
            using (DBConnection_EF con = new DBConnection_EF())
            {
                cmd_ohtc = cmd_ohtcDAO.getCMD_OHTCByVehicleID(con, vh_id);
            }
            return cmd_ohtc;
        }
        public ACMD_OHTC getExcuteCMD_OHTCByCmdID(string cmd_id)
        {
            ACMD_OHTC cmd_ohtc = null;
            using (DBConnection_EF con = new DBConnection_EF())
            {
                cmd_ohtc = cmd_ohtcDAO.getExcuteCMD_OHTCByCmdID(con, cmd_id);
            }
            return cmd_ohtc;
        }

        public ACMD_OHTC GetCMD_OHTCByID(string cmdID)
        {
            ACMD_OHTC cmd_ohtc = null;
            using (DBConnection_EF con = new DBConnection_EF())
            {
                cmd_ohtc = cmd_ohtcDAO.getByID(con, cmdID);
            }
            return cmd_ohtc;
        }
        public bool isCMD_OHTCQueueByVh(string vh_id)
        {
            int count = 0;

            //DBConnection_EF con = DBConnection_EF.GetContext();
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                count = cmd_ohtcDAO.getVhQueueCMDConut(con, vh_id);
            }
            return count != 0;
        }

        public bool isCMD_OHTCExcuteByVh(string vh_id)
        {
            int count = 0;
            //DBConnection_EF con = DBConnection_EF.GetContext();
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                count = cmd_ohtcDAO.getVhExcuteCMDConut(con, vh_id);
            }
            return count != 0;
        }

        public bool hasExcuteCMDFromToAdrIsParkInSpecifyParkZoneID(string park_zone_id, out int ready_come_to_count)
        {
            ready_come_to_count = 0;
            bool hasCarComeTo = false;
            List<APARKZONEDETAIL> park_zone_detail = null;
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                park_zone_detail = scApp.ParkZoneDetailDao.loadByParkZoneID(con, park_zone_id);
                if (park_zone_detail != null && park_zone_detail.Count > 0)
                {
                    foreach (APARKZONEDETAIL detail in park_zone_detail)
                    {
                        int cmd_ohtc_count = cmd_ohtcDAO.getExecuteByFromAdrIsParkAdr(con, detail.ADR_ID);
                        if (cmd_ohtc_count > 0)
                        {
                            ready_come_to_count++;
                            hasCarComeTo = true;
                            continue;
                        }
                        cmd_ohtc_count = cmd_ohtcDAO.getExecuteByToAdrIsParkAdr(con, detail.ADR_ID);
                        if (cmd_ohtc_count > 0)
                        {
                            ready_come_to_count++;
                            hasCarComeTo = true;
                            continue;
                        }
                    }
                }
            }
            return hasCarComeTo;
        }


        public bool hasExcuteCMDWantToAdr(string adr_id)
        {
            int count = 0;
            using (DBConnection_EF con = new DBConnection_EF())
            {
                count = cmd_ohtcDAO.getExecuteByToAdr(con, adr_id);
            }
            return count != 0;

        }
        public bool hasExcuteCMDWantToParkAdr(string adr_id)
        {
            int count = 0;
            using (DBConnection_EF con = new DBConnection_EF())
            {
                count = cmd_ohtcDAO.getExecuteByToAdrIsPark(con, adr_id);
            }
            return count != 0;
        }



        public bool forceUpdataCmdStatus2FnishByVhID(string vh_id)
        {
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                List<ACMD_OHTC> cmds = cmd_ohtcDAO.loadUnfinishCmd(con, vh_id);
                if (cmds != null && cmds.Count > 0)
                {
                    foreach (ACMD_OHTC cmd in cmds)
                    {
                        //updateCommand_OHTC_StatusByCmdID(cmd.CMD_ID, E_CMD_STATUS.AbnormalEndByOHTC);
                        updateCommand_OHTC_StatusByCmdID(vh_id, cmd.CMD_ID, E_CMD_STATUS.AbnormalEndByOHTC);
                        if (!SCUtility.isEmpty(cmd.CMD_ID_MCS))
                        {
                            scApp.CMDBLL.updateCMD_MCS_TranStatus2Complete(cmd.CMD_ID_MCS, E_TRAN_STATUS.Aborted);
                        }
                    }
                    cmd_ohtcDAO.Update(con, cmds);
                }
            }
            return true;
        }

        public bool isCMCD_OHTCFinish(string cmdID)
        {
            ACMD_OHTC cmd_ohtc = null;
            using (DBConnection_EF con = new DBConnection_EF())
            {
                cmd_ohtc = cmd_ohtcDAO.getByID(con, cmdID);
            }
            return cmd_ohtc != null &&
                   cmd_ohtc.CMD_STAUS >= E_CMD_STATUS.NormalEnd;
        }


        //public bool FourceResetVhCmd()
        //{
        //    int count = 0;
        //    using (DBConnection_EF con = new DBConnection_EF())
        //    {
        //        count = cmd_ohtcDAO.getExecuteByToAdrIsPark(con, adr_id);
        //    }
        //    return count != 0;

        //}

        private long ohxc_cmd_SyncPoint = 0;
        public virtual void checkOHxC_TransferCommand()
        {
            if (System.Threading.Interlocked.Exchange(ref ohxc_cmd_SyncPoint, 1) == 0)
            {
                try
                {
                    if (scApp.getEQObjCacheManager().getLine().ServiceMode
                        != SCAppConstants.AppServiceMode.Active)
                        return;
                    List<ACMD_OHTC> CMD_OHTC_Queues = scApp.CMDBLL.loadCMD_OHTCMDStatusIsQueue();
                    if (CMD_OHTC_Queues == null || CMD_OHTC_Queues.Count == 0)
                        return;
                    foreach (ACMD_OHTC cmd in CMD_OHTC_Queues)
                    {

                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(CMDBLL), Device: string.Empty,
                           Data: $"Start process ohxc of command ,id:{SCUtility.Trim(cmd.CMD_ID)},vh id:{SCUtility.Trim(cmd.VH_ID)},from:{SCUtility.Trim(cmd.SOURCE)},to:{SCUtility.Trim(cmd.DESTINATION)}");

                        string vehicle_id = cmd.VH_ID.Trim();
                        AVEHICLE assignVH = scApp.VehicleBLL.getVehicleByID(vehicle_id);
                        if (cmd.CMD_TPYE != E_CMD_TYPE.Override)
                        {
                            if (!assignVH.isTcpIpConnect || !SCUtility.isEmpty(assignVH.OHTC_CMD))
                            {
                                continue;
                            }

                            scApp.VehicleService.doSendCommandToVh(assignVH, cmd);//定時檢查發送
                        }
                        //else
                        //{
                        //    if (!assignVH.isTcpIpConnect)
                        //    {
                        //        continue;
                        //    }
                        //    scApp.VehicleService.doSendOverrideCommandToVh(assignVH, cmd);
                        //}
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Exection:");
                }
                finally
                {
                    System.Threading.Interlocked.Exchange(ref ohxc_cmd_SyncPoint, 0);
                }
            }
        }

        public bool TransferCommandCheck(ActiveType activeType, string[] passSections, string[] cycleSections, string fromAdr, string toAdr, out string reason)
        {
            reason = "";
            if (activeType == ActiveType.Home || activeType == ActiveType.Mtlhome)
            {
                return true;
            }
            if (activeType != ActiveType.Load && activeType != ActiveType.Unload && (passSections == null || passSections.Count() == 0))
            {
                reason = "Pass section is empty !";
                return false;
            }

            bool isOK = true;
            switch (activeType)
            {
                case ActiveType.Load:
                    if (SCUtility.isEmpty(fromAdr))
                    {
                        isOK = false;
                        reason = $"Transfer type[{activeType},from adr is empty!]";
                    }
                    break;
                case ActiveType.Unload:
                    if (SCUtility.isEmpty(toAdr))
                    {
                        isOK = false;
                        reason = $"Transfer type[{activeType},from adr is empty!]";
                    }
                    break;
                case ActiveType.Loadunload:
                    if (SCUtility.isEmpty(fromAdr))
                    {
                        isOK = false;
                        reason = $"Transfer type[{activeType},from adr is empty!]";
                    }
                    else if (SCUtility.isEmpty(toAdr))
                    {
                        isOK = false;
                        reason = $"Transfer type[{activeType},toAdr adr is empty!]";
                    }
                    break;
                    //case ActiveType.Round:
                    //    if (cycleSections == null || cycleSections.Count() == 0)
                    //    {
                    //        isOK = false;
                    //        reason = $"Transfer type[{activeType},cycleSections is empty!]";
                    //    }
                    //    break;
            }

            return isOK;
        }

        public E_CMD_STATUS CompleteStatusToECmdStatus(CompleteStatus completeStatus)
        {
            switch (completeStatus)
            {
                case CompleteStatus.CmpStatusCancel:
                    return E_CMD_STATUS.CancelEndByOHTC;
                case CompleteStatus.CmpStatusAbort:
                    return E_CMD_STATUS.AbnormalEndByOHTC;
                case CompleteStatus.CmpStatusVehicleAbort:
                case CompleteStatus.CmpStatusIdmisMatch:
                case CompleteStatus.CmpStatusIdreadFailed:
                case CompleteStatus.CmpStatusInterlockError:
                case CompleteStatus.CmpStatusLongTimeInaction:
                    return E_CMD_STATUS.AbnormalEndByOHT;
                case CompleteStatus.CmpStatusForceFinishByOp:
                    return E_CMD_STATUS.AbnormalEndByOHTC;
                default:
                    return E_CMD_STATUS.NormalEnd;
            }
        }

        #endregion CMD_OHTC

        #region CMD_OHTC_DETAIL




        public string[] findBestFitRoute(string vh_crt_sec, string[] AllRouteInfo, string targetAdr)
        {
            string[] FitRouteSec = null;
            //try
            //{
            List<string> crtByPassSeg = ByPassSegment.ToList();
            filterByPassSec_VhAlreadyOnSec(vh_crt_sec, crtByPassSeg);
            filterByPassSec_TargetAdrOnSec(targetAdr, crtByPassSeg);
            string[] AllRoute = AllRouteInfo[1].Split(';');
            List<KeyValuePair<string[], double>> routeDetailAndDistance = PaserRoute2SectionsAndDistance(AllRoute);
            //if (scApp.getEQObjCacheManager().getLine().SegmentPreDisableExcuting)
            //{
            //    List<string> nonActiveSeg = scApp.MapBLL.loadNonActiveSegmentNum();
            //filterByPassSec_VhAlreadyOnSec(vh_crt_sec, nonActiveSeg);
            //filterByPassSec_TargetAdrOnSec(targetAdr, nonActiveSeg);
            foreach (var routeDetial in routeDetailAndDistance.ToList())
            {
                List<ASECTION> lstSec = scApp.MapBLL.loadSectionBySecIDs(routeDetial.Key.ToList());
                if (scApp.getEQObjCacheManager().getLine().SegmentPreDisableExcuting)
                {
                    List<string> nonActiveSeg = scApp.MapBLL.loadNonActiveSegmentNum();
                    string[] secOfSegments = lstSec.Select(s => s.SEG_NUM).Distinct().ToArray();
                    bool isIncludePassSeg = secOfSegments.Where(seg => nonActiveSeg.Contains(seg)).Count() != 0;
                    if (isIncludePassSeg)
                    {
                        routeDetailAndDistance.Remove(routeDetial);
                    }
                }
            }
            foreach (var routeDetial in routeDetailAndDistance.ToList())
            {
                List<ASECTION> lstSec = scApp.MapBLL.loadSectionBySecIDs(routeDetial.Key.ToList());
                List<AVEHICLE> vhs = scApp.VehicleBLL.loadAllErrorVehicle();
                foreach (AVEHICLE vh in vhs)
                {
                    bool IsErrorVhOnPassSection = lstSec.Where(sec => sec.SEC_ID.Trim() == vh.CUR_SEC_ID.Trim()).Count() > 0;
                    if (IsErrorVhOnPassSection)
                    {
                        routeDetailAndDistance.Remove(routeDetial);
                        if (routeDetailAndDistance.Count == 0)
                        {
                            throw new VehicleBLL.BlockedByTheErrorVehicleException
                                ($"Can't find the way to transfer.Because block by error vehicle [{vh.VEHICLE_ID}] on sec [{vh.CUR_SEC_ID}]");
                        }
                    }
                }
            }
            //}

            if (routeDetailAndDistance.Count == 0)
            {
                return null;
            }

            foreach (var routeDetial in routeDetailAndDistance)
            {
                List<ASECTION> lstSec = scApp.MapBLL.loadSectionBySecIDs(routeDetial.Key.ToList());
                string[] secOfSegments = lstSec.Select(s => s.SEG_NUM).Distinct().ToArray();
                bool isIncludePassSeg = secOfSegments.Where(seg => crtByPassSeg.Contains(seg)).Count() != 0;
                if (isIncludePassSeg)
                {
                    continue;
                }
                else
                {
                    FitRouteSec = routeDetial.Key;
                    break;
                }
            }
            if (FitRouteSec == null)
            {
                routeDetailAndDistance = routeDetailAndDistance.OrderBy(o => o.Value).ToList();
                FitRouteSec = routeDetailAndDistance.First().Key;
            }
            //}
            //catch (Exception ex)
            //{
            //    logger_VhRouteLog.Error(ex, "Exception");
            //}
            return FitRouteSec;
        }

        private void filterByPassSec_TargetAdrOnSec(string targetAdr, List<string> crtByPassSeg)
        {
            if (SCUtility.isEmpty(targetAdr)) return;
            List<ASECTION> adrOfSecs = scApp.MapBLL.loadSectionsByFromOrToAdr(targetAdr);
            string[] adrSecOfSegments = adrOfSecs.Select(s => s.SEG_NUM).Distinct().ToArray();
            if (adrSecOfSegments != null && adrSecOfSegments.Count() > 0)
            {
                foreach (string seg in adrSecOfSegments)
                {
                    if (crtByPassSeg.Contains(seg))
                    {
                        crtByPassSeg.Remove(seg);
                    }
                }
            }
        }

        private void filterByPassSec_VhAlreadyOnSec(string vh_crt_sec, List<string> crtByPassSeg)
        {
            ASECTION vh_current_sec = scApp.MapBLL.getSectiontByID(vh_crt_sec);
            if (vh_current_sec != null)
            {
                if (crtByPassSeg.Contains(vh_current_sec.SEG_NUM))
                {
                    crtByPassSeg.Remove(vh_current_sec.SEG_NUM);
                }
            }
        }

        private List<KeyValuePair<string[], double>> PaserRoute2SectionsAndDistance(string[] AllRoute)
        {
            List<KeyValuePair<string[], double>> routeDetailAndDistance = new List<KeyValuePair<string[], double>>();
            foreach (string routeDetial in AllRoute)
            {
                string route = routeDetial.Split('=')[0];
                string[] routeSection = route.Split(',');
                string distance = routeDetial.Split('=')[1];
                double idistance = double.MaxValue;
                if (!double.TryParse(distance, out idistance))
                {
                    logger.Warn($"fun:{nameof(PaserRoute2SectionsAndDistance)},parse distance fail.Route:{route},distance:{distance}");
                }
                routeDetailAndDistance.Add(new KeyValuePair<string[], double>(routeSection, idistance));
            }
            return routeDetailAndDistance;
        }

        //public bool tryGenerateCommandDetails(ACMD_OHTC acmd_ohtc, out string[] guideSections, out string[] guideAddresses)
        //{
        //    AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(acmd_ohtc.VH_ID);
        //    string start_section = vh.CUR_SEC_ID;
        //    string source_address = acmd_ohtc.SOURCE;
        //    string address_dest = acmd_ohtc.DESTINATION;
        //    switch (acmd_ohtc.CMD_TPYE)
        //    {
        //        case E_CMD_TYPE.LoadUnload:
        //            //FindGuideRouteByStartFromTo(start_section, source_address, address_dest,
        //            //                            out guideSections, out guideAddresses);
        //            return true;
        //        default:
        //            throw new Exception();
        //    }
        //}

        //private void FindGuideRouteByStartFromTo(string start_section, string source_address, string destination,
        //                                         out string[] guideSections, out string[] guideAddresses)
        //{
        //    string[] start2source_secs = null;
        //    string[] start2source_adrs = null;
        //    string[] source2dist_secs = null;
        //    string[] source2dist_adrs = null;
        //    int.TryParse(start_section, out int i_crt_sec_id);
        //    int.TryParse(source_address, out int i_source_adr);
        //    int.TryParse(destination, out int i_destination_adr);
        //    var all_routes_start2source = scApp.NewRouteGuide.getFromToRoutesSectionToAdr(i_crt_sec_id, i_source_adr);
        //    var best_route_start2source = all_routes_start2source.First();
        //    string last_route_final_section = best_route_start2source.sections.Last().section_id;
        //    int.TryParse(last_route_final_section, out int i_fianl_section);
        //    var all_routes_source2destination = scApp.NewRouteGuide.getFromToRoutesSectionToAdr(i_fianl_section, i_source_adr);
        //    var best_route_source2destination = all_routes_source2destination.First();

        //    start2source_secs = GetGuideSections(best_route_start2source.sections);
        //    start2source_adrs = GetGuideAddresses(best_route_start2source.sections, source_address);

        //    string last_route_final_section = start2source_secs.Last();
        //    var all_routes_source2destination = scApp.NewRouteGuide.getFromToRoutesSectionToAdr(i_fianl_section, i_source_adr);
        //    var best_route_source2destination = all_routes_source2destination.First();
        //    source2dist_secs = GetGuideSections(best_route_source2destination.sections).Skip(1).ToArray();//由於是用SectionToAddress去尋找，所以會包含原本的，因此要過濾掉。
        //    source2dist_adrs = GetGuideAddresses(best_route_source2destination.sections, source_address);

        //    guideSections = start2source_secs.Union(source2dist_secs).ToArray();
        //    guideAddresses = start2source_adrs.Union(source2dist_adrs).ToArray();
        //}

        private string[] GetGuideSections(List<RouteKit.Section> sections)
        {
            return sections.Select(sec => sec.section_id).ToArray();
        }
        private string[] GetGuideAddresses(List<RouteKit.Section> sections, string lastAddress)
        {
            List<string> addresses = new List<string>();
            //跳過第一個Section(VH自己所在的Section)不使用，
            //拿接下來要經過的Section組合出Guide Addresses。
            var filter_sections = sections.Skip(1);
            var first_section = filter_sections.First();
            string first_address = first_section.direct == 1 ?
                first_section.address_1.ToString() : first_section.address_2.ToString();
            addresses.Add(first_address);
            addresses.AddRange(filter_sections.Select(section_address_selector).ToList());
            //移掉最後一個Address加入最後要到達的Address即可
            addresses.Add(lastAddress);
            return addresses.ToArray();
        }

        string section_address_selector(RouteKit.Section sec)
        {
            if (sec.direct == 1)
            {
                return sec.address_2.ToString("0000");
            }
            else if (sec.direct == 2)
            {
                return sec.address_1.ToString("0000");
            }
            else
            {
                throw new Exception();
            }
        }

        //private static void setVhExcuteCmdToShow(ACMD_OHTC acmd_ohtc, AVEHICLE vehicle, Equipment eqpt, string[] min_route_seq)
        public void setVhExcuteCmdToShow(ACMD_OHTC acmd_ohtc, AVEHICLE vehicle, List<string> min_segment_seq, string[] min_section_seq, string[] willPassAddressIDs,
                                         List<string> guideSectionStartToLoad, List<string> guideSectionToDestination)
        {
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(acmd_ohtc.VH_ID);
            vh.MCS_CMD = acmd_ohtc.CMD_ID_MCS;
            vh.OHTC_CMD = acmd_ohtc.CMD_ID;
            vh.startAdr = vehicle.CUR_ADR_ID;
            vh.FromAdr = acmd_ohtc.SOURCE;
            vh.ToAdr = acmd_ohtc.DESTINATION;
            //APORTSTATION from_port = scApp.PortStationBLL.OperateCatch.getPortStationByAdrID(acmd_ohtc.SOURCE);
            //if (from_port != null)
            //{
            //    vh.FromPort = from_port;
            //}
            //APORTSTATION to_port = scApp.PortStationBLL.OperateCatch.getPortStationByAdrID(acmd_ohtc.DESTINATION);
            //if (from_port != null)
            //{
            //    vh.ToPort = to_port;
            //}
            vh.CmdType = acmd_ohtc.CMD_TPYE;

            vh.PredictSegment = min_segment_seq;
            vh.PredictSections = min_section_seq;
            vh.PredictSectionsStartToLoad = guideSectionStartToLoad;
            vh.PredictSectionsToDesination = guideSectionToDestination;

            vh.PredictAddresses = willPassAddressIDs;
            vh.WillPassAddressID = willPassAddressIDs?.ToList();
            if (guideSectionStartToLoad != null && guideSectionStartToLoad.Count > 0)
            {
                setWillPassSectionInfo(acmd_ohtc.VH_ID, guideSectionStartToLoad);
            }
            else
            {
                setWillPassSectionInfo(acmd_ohtc.VH_ID, guideSectionToDestination);
            }


            vh.vh_CMD_Status = E_CMD_STATUS.Execution;
            vh.Action();
            vh.NotifyVhExcuteCMDStatusChange();
            //_vhCatchObject.VID_Collection.VID_58_CommandID.COMMAND_ID = acmd_ohtc.CMD_ID_MCS;
        }
        public void setWillPassSectionInfo(string vhID, List<string> willPassSection)
        {
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vhID);
            vh.WillPassSectionID = willPassSection;
        }
        public void removeAlreadyPassedSection(string vhID, string sectionID)
        {
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vhID);
            if (vh.WillPassSectionID != null && vh.WillPassSectionID.Count > 0)
                vh.WillPassSectionID.Remove(sectionID);
        }
        public void removeAllWillPassSection(string vhID)
        {
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vhID);
            if (vh != null)
                vh.WillPassSectionID = null;
        }

        public ActiveType convertECmdType2ActiveType(E_CMD_TYPE cmdType)
        {
            ActiveType activeType;
            switch (cmdType)
            {
                case E_CMD_TYPE.Move:
                case E_CMD_TYPE.Move_Park:
                    activeType = ActiveType.Move;
                    break;
                case E_CMD_TYPE.Load:
                    activeType = ActiveType.Load;
                    break;
                case E_CMD_TYPE.Unload:
                    activeType = ActiveType.Unload;
                    break;
                case E_CMD_TYPE.LoadUnload:
                    activeType = ActiveType.Loadunload;
                    break;
                case E_CMD_TYPE.Teaching:
                    activeType = ActiveType.Home;
                    break;
                case E_CMD_TYPE.MTLHome:
                    activeType = ActiveType.Mtlhome;
                    break;
                case E_CMD_TYPE.Move_Charger:
                    activeType = ActiveType.Movetocharger;
                    break;
                case E_CMD_TYPE.Override:
                    activeType = ActiveType.Override;
                    break;
                case E_CMD_TYPE.Move_Teaching:
                    activeType = ActiveType.Techingmove;
                    break;
                default:
                    throw new Exception(string.Format("OHT Command type:{0} , not in the definition"
                                                     , cmdType.ToString()));
            }
            return activeType;
        }


        private bool needVh2FromAddressOfGuide(ACMD_OHTC acmd_ohtc, AVEHICLE vehicle)
        {
            bool is_need = true;
            string cmd_source_adr = acmd_ohtc.SOURCE;
            string vh_current_adr = vehicle.CUR_ADR_ID;
            string vh_current_sec = vehicle.CUR_SEC_ID;
            double vh_sec_dist = vehicle.ACC_SEC_DIST;
            if (SCUtility.isEmpty(cmd_source_adr))
            {
                is_need = false;
            }
            if (is_need && SCUtility.isMatche(cmd_source_adr, vh_current_adr))
            {
                var last_and_next_sections = scApp.MapBLL.loadSectionsByFromOrToAdr(vh_current_adr);
                foreach (ASECTION sec in last_and_next_sections)
                {
                    //如果車子在該段Section的開頭時，就可以不用給他From到Source的Sec
                    if (SCUtility.isMatche(sec.FROM_ADR_ID, vh_current_adr))
                    {
                        if (SCUtility.isMatche(sec.SEC_ID, vh_current_sec))
                        {
                            if (vh_sec_dist == 0)
                                is_need = false;
                        }
                    }
                }
            }
            return is_need;
        }
        //private bool isVhInSectionStartingPoint(AVEHICLE vh)
        //{

        //}

        public bool creatCmd_OHTC_Details(string cmd_id, List<string> sec_ids)
        {
            bool isSuccess = false;
            ASECTION section = null;
            try
            {
                //List<ASECTION> lstSce = scApp.MapBLL.loadSectionBySecIDs(sec_ids);
                for (int i = 0; i < sec_ids.Count; i++)
                {
                    section = scApp.MapBLL.getSectiontByID(sec_ids[i]);
                    creatCommand_OHTC_Detail(cmd_id, i + 1, section.FROM_ADR_ID, section.SEC_ID, section.SEG_NUM, 0);
                }
                isSuccess = true;
            }
            catch (Exception ex)
            {
                logger_VhRouteLog.Error(ex, "Exception");
                throw ex;
            }
            return isSuccess;
        }

        public bool creatCommand_OHTC_Detail(string cmd_id, int seq_no, string add_id,
                                      string sec_id, string seg_num, int estimated_time)
        {
            bool isSuccess = false;
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                ACMD_OHTC_DETAIL cmd = new ACMD_OHTC_DETAIL
                {
                    CMD_ID = cmd_id,
                    SEQ_NO = seq_no,
                    ADD_ID = add_id,
                    SEC_ID = sec_id,
                    SEG_NUM = seg_num,
                    ESTIMATED_TIME = estimated_time
                };
                cmd_ohtc_detailDAO.add(con, cmd);
            }
            return isSuccess;
        }

        public void CeratCmdDerails(string cmdID, string[] secIDs)
        {
            //using (DBConnection_EF con = new DBConnection_EF())
            int start_seq_no = 0;
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                ACMD_OHTC_DETAIL last_cmd_detail = cmd_ohtc_detailDAO.getLastByID(con, cmdID);
                if (last_cmd_detail != null)
                {
                    start_seq_no = last_cmd_detail.SEQ_NO;
                }
            }
            List<ACMD_OHTC_DETAIL> cmd_details = new List<ACMD_OHTC_DETAIL>();
            foreach (string sec_id in secIDs)
            {
                ASECTION section = scApp.MapBLL.getSectiontByID(sec_id);
                ACMD_OHTC_DETAIL cmd_detail = new ACMD_OHTC_DETAIL()
                {
                    CMD_ID = cmdID,
                    SEQ_NO = ++start_seq_no,
                    ADD_ID = section.FROM_ADR_ID,
                    SEC_ID = section.SEC_ID,
                    SEG_NUM = section.SEG_NUM,
                    ESTIMATED_TIME = 0
                };
                cmd_details.Add(cmd_detail);
            }
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                cmd_ohtc_detailDAO.AddByBatch(con, cmd_details);
            }
        }

        public bool update_CMD_DetailEntryTime(string cmd_id,
                                               string add_id,
                                               string sec_id)
        {
            bool isSuccess = false;
            //DBConnection_EF con = DBConnection_EF.GetContext();
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                //ACMD_OHTC cmd_oht = cmd_ohtcDAO.getExecuteByVhID(con, vh_id);
                ACMD_OHTC_DETAIL cmd_detail = cmd_ohtc_detailDAO.
                    getByCmdIDSECIDAndEntryTimeEmpty(con, cmd_id, sec_id);
                if (cmd_detail != null)
                {
                    DateTime nowTime = DateTime.Now;
                    cmd_detail.ADD_ENTRY_TIME = nowTime;
                    cmd_detail.SEC_ENTRY_TIME = nowTime;
                    cmd_ohtc_detailDAO.Update(con, cmd_detail);
                    isSuccess = true;
                }
            }
            return isSuccess;
        }
        public bool update_CMD_DetailLeaveTime(string cmd_id,
                                              string add_id,
                                              string sec_id)
        {
            bool isSuccess = false;
            //DBConnection_EF con = DBConnection_EF.GetContext();
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                //ACMD_OHTC cmd_oht = cmd_ohtcDAO.getExecuteByVhID(con, vh_id);
                //if (cmd_oht != null)
                //{
                ACMD_OHTC_DETAIL cmd_detail = cmd_ohtc_detailDAO.
                    getByCmdIDSECIDAndLeaveTimeEmpty(con, cmd_id, sec_id);
                if (cmd_detail == null)
                {
                    return false;
                }
                DateTime nowTime = DateTime.Now;
                cmd_detail.SEC_LEAVE_TIME = nowTime;

                cmd_ohtc_detailDAO.Update(con, cmd_detail);
                cmd_ohtc_detailDAO.UpdateIsPassFlag(con, cmd_detail.CMD_ID, cmd_detail.SEQ_NO);
                isSuccess = true;
                //}
            }
            return isSuccess;
        }

        public bool update_CMD_Detail_LoadStartTime(string vh_id,
                                                   string add_id,
                                                   string sec_id)
        {
            bool isSuccess = true;
            //DBConnection_EF con = DBConnection_EF.GetContext();
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                //ACMD_OHTC cmd_oht = cmd_ohtcDAO.getExecuteByVhID(con, vh_id);
                AVEHICLE vh = scApp.VehicleBLL.getVehicleByID(vh_id);
                if (!SCUtility.isEmpty(vh.OHTC_CMD))
                {
                    //ACMD_OHTC_DETAL cmd_detal = cmd_ohtc_detalDAO.getByCmdIDAndAdrID(con, cmd_oht.CMD_ID, add_id);
                    //ACMD_OHTC_DETAIL cmd_detail = cmd_ohtc_detailDAO.getByCmdIDAndSecID(con, cmd_oht.CMD_ID, sec_id);
                    ACMD_OHTC_DETAIL cmd_detail = cmd_ohtc_detailDAO.getByCmdIDAndSecID(con, vh.OHTC_CMD, sec_id);
                    if (cmd_detail == null)
                        return false;
                    DateTime nowTime = DateTime.Now;
                    cmd_detail.LOAD_START_TIME = nowTime;
                    cmd_ohtc_detailDAO.Update(con, cmd_detail);
                }
                else
                {
                    isSuccess = false;
                }
            }
            return isSuccess;
        }
        public bool update_CMD_Detail_LoadEndTime(string vh_id,
                                         string add_id,
                                         string sec_id)
        {
            bool isSuccess = true;
            //DBConnection_EF con = DBConnection_EF.GetContext();
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                //ACMD_OHTC cmd_oht = cmd_ohtcDAO.getExecuteByVhID(con, vh_id);
                AVEHICLE vh = scApp.VehicleBLL.getVehicleByID(vh_id);
                if (!SCUtility.isEmpty(vh.OHTC_CMD))
                {
                    //ACMD_OHTC_DETAL cmd_detal = cmd_ohtc_detalDAO.getByCmdIDAndAdrID(con, cmd_oht.CMD_ID, add_id);
                    //ACMD_OHTC_DETAIL cmd_detail = cmd_ohtc_detailDAO.getByCmdIDAndSecID(con, cmd_oht.CMD_ID, sec_id);
                    ACMD_OHTC_DETAIL cmd_detail = cmd_ohtc_detailDAO.getByCmdIDAndSecID(con, vh.OHTC_CMD, sec_id);
                    if (cmd_detail == null)
                        return false;
                    DateTime nowTime = DateTime.Now;
                    cmd_detail.LOAD_END_TIME = nowTime;
                    cmd_ohtc_detailDAO.Update(con, cmd_detail);
                    //}
                }
                else
                {
                    isSuccess = false;
                }
            }
            return isSuccess;
        }
        public bool update_CMD_Detail_UnloadStartTime(string vh_id,
                                       string add_id,
                                       string sec_id)
        {
            bool isSuccess = true;
            //DBConnection_EF con = DBConnection_EF.GetContext();
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                //ACMD_OHTC cmd_oht = cmd_ohtcDAO.getExecuteByVhID(con, vh_id);
                AVEHICLE vh = scApp.VehicleBLL.getVehicleByID(vh_id);
                if (!SCUtility.isEmpty(vh.OHTC_CMD))
                {
                    ACMD_OHTC_DETAIL cmd_detail = cmd_ohtc_detailDAO.getByCmdIDAndSecID(con, vh.OHTC_CMD, sec_id);
                    if (cmd_detail == null)
                        return false;
                    DateTime nowTime = DateTime.Now;
                    cmd_detail.UNLOAD_START_TIME = nowTime;
                    cmd_ohtc_detailDAO.Update(con, cmd_detail);
                }
                else
                {
                    isSuccess = false;
                }
            }
            return isSuccess;
        }


        public bool update_CMD_Detail_UnloadEndTime(string vh_id)
        {
            bool isSuccess = true;
            //DBConnection_EF con = DBConnection_EF.GetContext();
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                //ACMD_OHTC cmd_oht = cmd_ohtcDAO.getExecuteByVhID(con, vh_id);
                AVEHICLE vh = scApp.VehicleBLL.getVehicleByID(vh_id);
                if (!SCUtility.isEmpty(vh.OHTC_CMD))
                {
                    ACMD_OHTC_DETAIL cmd_detail = cmd_ohtc_detailDAO.getLastByID(con, vh.OHTC_CMD);
                    if (cmd_detail != null)
                    {
                        cmd_detail.UNLOAD_END_TIME = DateTime.Now;
                        cmd_ohtc_detailDAO.Update(con, cmd_detail);
                    }
                    else
                    {
                        isSuccess = false;
                    }
                }
                else
                {
                    isSuccess = false;
                }
            }
            return isSuccess;
        }

        //public bool update_CMD_Detail_2AbnormalFinsh(string cmd_id, List<string> sec_ids)
        //{
        //    bool isSuccess = false;
        //    using (DBConnection_EF con = DBConnection_EF.GetUContext())
        //    {
        //        foreach (string sec_id in sec_ids)
        //        {
        //            ACMD_OHTC_DETAIL cmd_detail = new ACMD_OHTC_DETAIL();
        //            cmd_detail.CMD_ID = cmd_id;
        //            con.ACMD_OHTC_DETAIL.Attach(cmd_detail);
        //            cmd_detail.SEC_ID = sec_id;
        //            cmd_detail.SEC_ENTRY_TIME = DateTime.MaxValue;
        //            cmd_detail.SEC_LEAVE_TIME = DateTime.MaxValue;
        //            cmd_detail.ADD_ID = "";
        //            cmd_detail.SEG_NUM = "";

        //            //con.Entry(cmd_detail).Property(p => p.CMD_ID).IsModified = true;
        //            //con.Entry(cmd_detail).Property(p => p.SEC_ID).IsModified = true;
        //            con.Entry(cmd_detail).Property(p => p.SEC_ENTRY_TIME).IsModified = true;
        //            con.Entry(cmd_detail).Property(p => p.SEC_LEAVE_TIME).IsModified = true;
        //            con.Entry(cmd_detail).Property(p => p.ADD_ID).IsModified = false;
        //            con.Entry(cmd_detail).Property(p => p.SEG_NUM).IsModified = false;
        //            cmd_ohtc_detailDAO.Update(con, cmd_detail);
        //            con.Entry(cmd_detail).State = EntityState.Detached;
        //        }
        //        isSuccess = true;
        //    }
        //    return isSuccess;
        //}
        public bool update_CMD_Detail_2AbnormalFinsh(string cmd_id, List<string> sec_ids)
        {
            bool isSuccess = false;
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                foreach (string sec_id in sec_ids)
                {
                    ACMD_OHTC_DETAIL cmd_detail = cmd_ohtc_detailDAO.getByCmdIDSECIDAndEntryTimeEmpty(con, cmd_id, sec_id);
                    if (cmd_detail != null)
                    {
                        cmd_detail.SEC_ENTRY_TIME = DateTime.MaxValue;
                        cmd_detail.SEC_LEAVE_TIME = DateTime.MaxValue;
                        cmd_detail.IS_PASS = true;

                        cmd_ohtc_detailDAO.Update(con, cmd_detail);
                    }
                }
                isSuccess = true;
            }
            return isSuccess;
        }
        public int getAndUpdateVhCMDProgress(string vh_id, out List<string> willPassSecID)
        {
            int procProgress_percen = 0;
            willPassSecID = null;
            //DBConnection_EF con = DBConnection_EF.GetContext();
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                AVEHICLE vh = scApp.VehicleBLL.getVehicleByID(vh_id);
                if (!SCUtility.isEmpty(vh.OHTC_CMD))
                {
                    ACMD_OHTC cmd_oht = cmd_ohtcDAO.getByID(con, vh.OHTC_CMD);
                    if (cmd_oht == null) return 0;
                    double totalDetailCount = 0;
                    double procDetailCount = 0;
                    //List<ACMD_OHTC_DETAIL> lstcmd_detail = cmd_ohtc_detailDAO.loadAllByCmdID(con, cmd_oht.CMD_ID);
                    //totalDetalCount = lstcmd_detail.Count();
                    //procDetalCount = lstcmd_detail.Where(cmd => cmd.ADD_ENTRY_TIME != null).Count();
                    totalDetailCount = cmd_ohtc_detailDAO.getAllDetailCountByCmdID(con, cmd_oht.CMD_ID);
                    procDetailCount = cmd_ohtc_detailDAO.getAllPassDetailCountByCmdID(con, cmd_oht.CMD_ID);
                    willPassSecID = cmd_ohtc_detailDAO.loadAllNonPassDetailSecIDByCmdID(con, cmd_oht.CMD_ID);
                    procProgress_percen = (int)((procDetailCount / totalDetailCount) * 100);
                    cmd_oht.CMD_PROGRESS = procProgress_percen;
                    cmd_ohtcDAO.Update(con, cmd_oht);
                }
            }
            return procProgress_percen;
        }

        public bool HasCmdWillPassSegment(string segment_num, out List<string> will_pass_cmd_id)
        {
            bool hasCmd = false;
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                will_pass_cmd_id = cmd_ohtc_detailDAO.loadAllWillPassDetailCmdID(con, segment_num);
            }
            hasCmd = will_pass_cmd_id != null && will_pass_cmd_id.Count > 0;
            return hasCmd;
        }

        public string[] loadPassSectionByCMDID(string cmd_id)
        {
            string[] sections = null;
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                sections = cmd_ohtc_detailDAO.loadAllSecIDByCmdID(con, cmd_id);
            }
            return sections;
        }

        #endregion CMD_OHTC_DETAIL

        #region Return Code Map
        public ReturnCodeMap getReturnCodeMap(string eq_id, string return_code)
        {
            return return_code_mapDao.getReturnCodeMap(scApp, eq_id, return_code);
        }
        #endregion Return Code Map


        #region HCMD_MCS
        public void CreatHCMD_MCS(HCMD_MCS hcmdMCS)
        {
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                hcmd_mcsDao.Add(con, hcmdMCS);
            }
        }
        public void CreatHCMD_MCSs(List<HCMD_MCS> HCMD_MCS)
        {
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                hcmd_mcsDao.AddByBatch(con, HCMD_MCS);
            }
        }
        public HCMD_MCS getLastFinishHCmd_MCSByCSTID(string cstID, string byPassCmdID)
        {
            HCMD_MCS cmd_mcs = null;
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                cmd_mcs = hcmd_mcsDao.getLastCmdByCSTID(con, cstID, byPassCmdID);
            }
            return cmd_mcs;
        }

        public List<ObjectRelay.HCMD_MCSObjToShow> loadHCMD_MCSs()
        {
            List<ObjectRelay.HCMD_MCSObjToShow> hcmds = null;
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                hcmds = hcmd_mcsDao.loadLast24Hours(con);
            }
            return hcmds;
        }
        #endregion HCMD_MCS
        #region HCMD_OHTC
        public void CreatHCMD_OHTCs(List<HCMD_OHTC> HCMD_OHTC)
        {
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                hcmd_ohtcDao.AddByBatch(con, HCMD_OHTC);
            }
        }

        public HCMD_OHTC getHCmd_OHTCByCMDID(string cmd_id)
        {
            HCMD_OHTC cmd_ohtc = null;
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                cmd_ohtc = hcmd_ohtcDao.geCmdByCMDID(con, cmd_id);
            }
            return cmd_ohtc;
        }

        public List<HCMD_OHTC> GetHisOHTCCMDsBySetTimeClearTime(DateTime insertTime, DateTime endTime)
        {
            List<HCMD_OHTC> cmds = null;
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                cmds = hcmd_ohtcDao.loadByInsertTimeEndTime(con, insertTime, endTime);
            }
            return cmds;
        }

        #endregion HCMD_OHTC


        public void MoveACMD_MCSToHCMD_MCS(string mcsCmdID)
        {
            try
            {
                ACMD_MCS cmd_mcs = null;
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    cmd_mcs = cmd_mcsDao.getByID(con, mcsCmdID);
                    if (cmd_mcs == null)
                    {
                        return;
                    }
                    cmd_mcsDao.Remove(con, cmd_mcs);
                    var hcmd_mcs = cmd_mcs.ToHCMD_MCS();
                    hcmd_mcsDao.Add(con, hcmd_mcs);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
            }
        }


    }
}
