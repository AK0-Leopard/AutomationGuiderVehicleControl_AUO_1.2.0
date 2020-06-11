using com.mirle.ibg3k0.sc.Data.SECS;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.mirle.ibg3k0.sc.Data.DAO.EntityFramework
{
    public class CMD_MCSDao
    {
        public void add(DBConnection_EF con, ACMD_MCS rail)
        {
            con.ACMD_MCS.Add(rail);
            con.SaveChanges();
        }
        public void RemoteByBatch(DBConnection_EF con, List<ACMD_MCS> cmd_mcss)
        {
            cmd_mcss.ForEach(entity => con.Entry(entity).State = EntityState.Deleted);
            con.ACMD_MCS.RemoveRange(cmd_mcss);
            con.SaveChanges();
        }


        public void update(DBConnection_EF con, ACMD_MCS cmd)
        {
            con.SaveChanges();
        }

        public ACMD_MCS getByID(DBConnection_EF con, String cmd_id)
        {
            var query = from cmd in con.ACMD_MCS
                        where cmd.CMD_ID.Trim() == cmd_id.Trim()
                        select cmd;
            return query.SingleOrDefault();
        }


        public ACMD_MCS getExcuteCMDByCSTID(DBConnection_EF con, String carrierID)
        {
            var query = from cmd in con.ACMD_MCS
                        where (cmd.TRANSFERSTATE >= E_TRAN_STATUS.Queue && cmd.TRANSFERSTATE <= E_TRAN_STATUS.Aborting) &&
                               cmd.CARRIER_ID.Trim() == carrierID.Trim()
                        select cmd;
            return query.FirstOrDefault();
        }

        public IQueryable getQueryAllSQL(DBConnection_EF con)
        {
            var query = from cmd_mcs in con.ACMD_MCS
                        select cmd_mcs;
            return query;
        }

        /// <summary>
        /// 透過hostSource與hostDestination取得尚未搬走(狀態還在Transferring之前)的CMD_MCS
        /// </summary>
        /// <param name="con"></param>
        /// <param name="hostSource"></param>
        /// <param name="hostDestination"></param>
        /// <returns></returns>
        public ACMD_MCS getWatingCMDByFromTo(DBConnection_EF con, string hostSource, string hostDestination)
        {
            var query = from cmd in con.ACMD_MCS
                        where (cmd.TRANSFERSTATE >= E_TRAN_STATUS.Queue && cmd.TRANSFERSTATE < E_TRAN_STATUS.Transferring) &&
                               cmd.HOSTSOURCE.Trim() == hostSource.Trim() &&
                               cmd.HOSTDESTINATION.Trim() == hostDestination.Trim()
                        select cmd;
            return query.FirstOrDefault();
        }
        public ACMD_MCS getWatingCMDByFrom(DBConnection_EF con, string hostSource)
        {
            var query = from cmd in con.ACMD_MCS
                        where (cmd.TRANSFERSTATE >= E_TRAN_STATUS.Queue && cmd.TRANSFERSTATE < E_TRAN_STATUS.Transferring) &&
                               cmd.HOSTSOURCE.Trim() == hostSource.Trim()
                        select cmd;
            return query.FirstOrDefault();
        }
        public ACMD_MCS getLastFinishCmdByCSTID(DBConnection_EF con, string cstID, string byPassCMDID)
        {
            var query = from cmd in con.ACMD_MCS
                        where cmd.CARRIER_ID.Trim() == cstID.Trim() &&
                              cmd.CMD_ID.Trim() != byPassCMDID.Trim() &&
                              cmd.TRANSFERSTATE >= E_TRAN_STATUS.Canceled
                        orderby cmd.CMD_INSER_TIME descending
                        select cmd;
            return query.FirstOrDefault();
        }
        public List<ACMD_MCS> loadACMD_MCSIsQueue(DBConnection_EF con)
        {
            var query = from cmd in con.ACMD_MCS.AsNoTracking()
                        where (cmd.TRANSFERSTATE == E_TRAN_STATUS.Queue || cmd.TRANSFERSTATE == E_TRAN_STATUS.RouteChanging)
                        && (cmd.CHECKCODE.Trim() == SECSConst.HCACK_Confirm || cmd.CHECKCODE.Trim() == SECSConst.HCACK_Confirm_Executed)
                        orderby cmd.PRIORITY_SUM descending, cmd.CMD_INSER_TIME
                        select cmd;

            return query.ToList();
        }

        public List<ACMD_MCS> loadACMD_MCSIsUnfinished(DBConnection_EF con)
        {
            var query = from cmd in con.ACMD_MCS.AsNoTracking()
                        where cmd.TRANSFERSTATE >= E_TRAN_STATUS.Queue && cmd.TRANSFERSTATE <= E_TRAN_STATUS.Aborting
                        && cmd.CHECKCODE.Trim() == SECSConst.HCACK_Confirm
                        select cmd;

            return query.ToList();
        }

        public List<ACMD_MCS> loadACMD_MCSIsExecuting(DBConnection_EF con)
        {
            var query = from cmd in con.ACMD_MCS.AsNoTracking()
                        where cmd.TRANSFERSTATE > E_TRAN_STATUS.Queue && cmd.TRANSFERSTATE <= E_TRAN_STATUS.Transferring
                        && cmd.CHECKCODE.Trim() == SECSConst.HCACK_Confirm
                        select cmd;
            return query.ToList();
        }

        public List<ACMD_MCS> loadFinishCMD_MCS(DBConnection_EF con)
        {
            var query = from cmd in con.ACMD_MCS
                        where cmd.TRANSFERSTATE >= E_TRAN_STATUS.Canceled
                        select cmd;
            return query.ToList();
        }



        public int getCMD_MCSMaxPrioritySum(DBConnection_EF con)
        {
            var query = from cmd in con.ACMD_MCS
                        where cmd.TRANSFERSTATE == E_TRAN_STATUS.Queue
                        orderby cmd.PRIORITY_SUM descending
                        select cmd.PRIORITY_SUM;
            List<int> prorityList = query.ToList();
            if (prorityList.Count == 0)
            {
                return 0;
            }
            else
            {
                return prorityList[0];
            }
        }
        public int getCMD_MCSMinPrioritySum(DBConnection_EF con)
        {
            var query = from cmd in con.ACMD_MCS
                        where cmd.TRANSFERSTATE == E_TRAN_STATUS.Queue
                        orderby cmd.PRIORITY_SUM ascending
                        select cmd.PRIORITY_SUM;
            List<int> prorityList = query.ToList();
            if (prorityList.Count == 0)
            {
                return 0;
            }
            else
            {
                return prorityList[0];
            }
        }

        public int getCMD_MCSIsQueueCount(DBConnection_EF con)
        {
            var query = from cmd in con.ACMD_MCS
                        where cmd.TRANSFERSTATE == E_TRAN_STATUS.Queue
                        select cmd;
            return query.Count();
        }
        public int getCMD_MCSTotalCount(DBConnection_EF con)
        {
            var query = from cmd in con.ACMD_MCS
                        select cmd;
            return query.Count();
        }

        public int getCMD_MCSIsExcuteCount(DBConnection_EF con)
        {
            var query = from cmd in con.ACMD_MCS
                        where cmd.TRANSFERSTATE > E_TRAN_STATUS.Queue
                        && cmd.TRANSFERSTATE < E_TRAN_STATUS.Canceled
                        select cmd;
            return query.Count();
        }
        public int getCMD_MCSIsExcuteCount(DBConnection_EF con, DateTime defore_time)
        {
            var query = from cmd in con.ACMD_MCS
                        where cmd.TRANSFERSTATE > E_TRAN_STATUS.Queue
                        && cmd.TRANSFERSTATE < E_TRAN_STATUS.Canceled
                        && cmd.CMD_INSER_TIME < defore_time
                        select cmd;
            return query.Count();
        }
        public List<string> loadIsExcuteCMD_MCS_ID(DBConnection_EF con, DateTime defore_time)
        {
            var query = from cmd in con.ACMD_MCS
                        where cmd.TRANSFERSTATE > E_TRAN_STATUS.Queue
                        && cmd.TRANSFERSTATE < E_TRAN_STATUS.Canceled
                        && cmd.CMD_INSER_TIME < defore_time
                        select cmd.CMD_ID;
            return query.ToList();
        }
        public int getCMD_MCSIsUnfinishedCount(DBConnection_EF con, List<string> port_ids)
        {
            var query = from cmd in con.ACMD_MCS
                        where port_ids.Contains(cmd.HOSTSOURCE.Trim()) &&
                        cmd.TRANSFERSTATE >= E_TRAN_STATUS.Queue
                        && cmd.TRANSFERSTATE < E_TRAN_STATUS.Canceled
                        select cmd;
            return query.Count();
        }

        public int getCMD_MCSInserCountLastHour(DBConnection_EF con, int hours)
        {
            DateTime nowTime = DateTime.Now;
            DateTime lastTime = nowTime.AddHours(-hours);

            var query = from cmd in con.ACMD_MCS
                        where cmd.CMD_INSER_TIME < nowTime &&
                        cmd.CMD_INSER_TIME > lastTime
                        select cmd;
            return query.Count();
        }

        public int getCMD_MCSFinishCountLastHours(DBConnection_EF con, int hours)
        {
            DateTime nowTime = DateTime.Now;
            DateTime lastTime = nowTime.AddHours(-hours);
            var query = from cmd in con.ACMD_MCS
                        where cmd.CMD_FINISH_TIME < nowTime &&
                        cmd.CMD_FINISH_TIME > lastTime
                        select cmd;
            return query.Count();
        }

        public int getCMD_MCSIsUnfinishedCountByCarrierID(DBConnection_EF con, string carrier_id)
        {
            var query = from cmd in con.ACMD_MCS
                        where cmd.CARRIER_ID.Trim() == carrier_id.Trim() &&
                        cmd.TRANSFERSTATE >= E_TRAN_STATUS.Queue
                        && cmd.TRANSFERSTATE < E_TRAN_STATUS.Canceled
                        select cmd;
            return query.Count();
        }
        public List<ACMD_MCS> loadByInsertTimeEndTime(DBConnection_EF con, DateTime insertTime, DateTime finishTime)
        {
            var query = from cmd in con.ACMD_MCS
                        where cmd.CMD_START_TIME > insertTime && (cmd.CMD_FINISH_TIME != null && cmd.CMD_FINISH_TIME < finishTime)
                        orderby cmd.CMD_START_TIME descending
                        select cmd;
            return query.ToList();
        }

        public int getCMD_MCSExcutingCountByFromToPort(DBConnection_EF con, string containsPortID)
        {
            DateTime nowTime = DateTime.Now;
            var query = from cmd in con.ACMD_MCS
                        where (cmd.HOSTSOURCE.Contains(containsPortID) || cmd.HOSTDESTINATION.Contains(containsPortID)) &&
                              (cmd.TRANSFERSTATE >= E_TRAN_STATUS.Initial && cmd.TRANSFERSTATE <= E_TRAN_STATUS.Canceled)
                        select cmd;
            return query.Count();
        }


    }

}
