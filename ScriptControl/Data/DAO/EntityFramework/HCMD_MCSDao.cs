using com.mirle.ibg3k0.sc.Data.SECS;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.mirle.ibg3k0.sc.Data.DAO.EntityFramework
{
    public class HCMD_MCSDao
    {
        public void AddByBatch(DBConnection_EF con, List<HCMD_MCS> cmd_ohtcs)
        {
            con.HCMD_MCS.AddRange(cmd_ohtcs);
            con.SaveChanges();
        }
        public HCMD_MCS getLastCmdByCSTID(DBConnection_EF con, string cstID, string byPassCmdID)
        {
            var query = from cmd in con.HCMD_MCS
                        where cmd.CARRIER_ID.Trim() == cstID.Trim() &&
                              cmd.CMD_ID.Trim() != byPassCmdID.Trim()
                        orderby cmd.CMD_INSER_TIME descending
                        select cmd;
            return query.FirstOrDefault();
        }

        public List<ObjectRelay.HCMD_MCSObjToShow> loadLast24Hours(DBConnection_EF con)
        {
            DateTime query_time = DateTime.Now.AddHours(-24);
            var query = from cmd in con.HCMD_MCS
                        where cmd.CMD_INSER_TIME > query_time
                        orderby cmd.CMD_INSER_TIME descending
                        select new ObjectRelay.HCMD_MCSObjToShow() { cmd_mcs = cmd };
            return query.ToList();
        }
        public List<HCMD_MCS> loadByInsertTimeEndTime(DBConnection_EF con, DateTime insertTime, DateTime finishTime)
        {
            var query = from cmd in con.HCMD_MCS
                        where cmd.CMD_START_TIME > insertTime && (cmd.CMD_FINISH_TIME != null && cmd.CMD_FINISH_TIME < finishTime)
                        orderby cmd.CMD_START_TIME descending
                        select cmd;
            return query.ToList();
        }

    }

}
