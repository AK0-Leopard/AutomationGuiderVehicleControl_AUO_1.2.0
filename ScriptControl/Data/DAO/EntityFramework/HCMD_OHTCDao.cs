using com.mirle.ibg3k0.sc.Data.SECS;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.mirle.ibg3k0.sc.Data.DAO.EntityFramework
{
    public class HCMD_OHTCDao
    {
        public void AddByBatch(DBConnection_EF con, List<HCMD_OHTC> cmd_ohtcs)
        {
            con.HCMD_OHTC.AddRange(cmd_ohtcs);
            con.SaveChanges();
        }


        public List<HCMD_OHTC> loadByInsertTimeEndTime(DBConnection_EF con, DateTime insertTime, DateTime finishTime)
        {
            var query = from cmd in con.HCMD_OHTC
                        where cmd.CMD_START_TIME > insertTime && (cmd.CMD_END_TIME != null && cmd.CMD_END_TIME < finishTime)
                        orderby cmd.CMD_START_TIME descending
                        select cmd;
            return query.ToList();
        }

        public HCMD_OHTC geCmdByCMDID(DBConnection_EF con, string cmd_id)
        {
            var query = from cmd in con.HCMD_OHTC
                        where cmd.CMD_ID.Trim() == cmd_id.Trim() 
                        select cmd;
            return query.FirstOrDefault();
        }
    }

}
