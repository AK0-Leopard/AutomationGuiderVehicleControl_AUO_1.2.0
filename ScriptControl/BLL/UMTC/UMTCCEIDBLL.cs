using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.Data;
using com.mirle.ibg3k0.sc.Data.DAO.EntityFramework;
using com.mirle.ibg3k0.sc.ProtocolFormat.OHTMessage;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace com.mirle.ibg3k0.sc.BLL
{
    public class UMTCCEIDBLL : CEIDBLL
    {
        //protected CEIDDao ceidDAO = null;
        //protected RPTIDDao rptidDAO = null;
        //protected SCApplication scApp = null;
        public UMTCCEIDBLL()
        {

        }
        public void start(SCApplication app)
        {
            scApp = app;
            ceidDAO = scApp.CEIDDao;
            rptidDAO = scApp.RPTIDDao;
        }


        public override bool buildCEIDAndReportID(Dictionary<string, string[]> reportItem)
        {
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                try
                {
                    foreach (var item in reportItem)
                    {
                        //if CEID is exists, remove them first
                        List<ACEID> existCEIDs = ceidDAO.getAllRPTIDByCEID(con, item.Key);
                        if (existCEIDs.Count > 0)
                        {
                            ceidDAO.RemoveByBatch(con, existCEIDs);
                        }

                        int order = 1;
                        foreach (var report_id in item.Value)
                        {
                            addCEID(con, item.Key, report_id, order++);
                        }
                    }

                }
                catch (Exception ex)
                {
                    return false;
                }
            }
            return true;
        }

        public bool buildCEIDsFromMCS(Data.SECS.S2F35.RPTITEM[] rPTITEMs)
        {
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                foreach (var item in rPTITEMs)
                {
                    int order = 1;
                    foreach (var rpt_item in item.RPTIDS)
                    {
                        addCEID(con, item.CEID, rpt_item, order++);
                    }
                }
            }
            return true;
        }


        public bool addCEID(DBConnection_EF con, string ceid, string rpt_id, int order)
        {
            bool isSuccess = true;
            ACEID aCEID = new ACEID()
            {
                CEID = ceid.Trim(),
                RPTID = rpt_id.Trim(),
                ORDER_NUM = order
            };
            ceidDAO.add(con, aCEID);
            return isSuccess;
        }

        public bool getCEID(string vh_id)
        {
            bool isSuccess = true;

            return isSuccess;
        }
        public void DeleteCEIDInfoByBatch()
        {
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                string sql = "DELETE FROM [ACEID] ";
                con.Database.ExecuteSqlCommand(sql);
            }
        }


        public Dictionary<string, List<string>> loadDicCEIDAndRPTID()
        {
            Dictionary<string, List<string>> dicCeidAndRptid = null;
            using (DBConnection_EF con = new DBConnection_EF())
            {
                dicCeidAndRptid = ceidDAO.loadAllCEIDByGroup(con);
            }
            return dicCeidAndRptid;
        }



        public Dictionary<string, List<ARPTID>> loadDicRPTIDAndVID()
        {
            Dictionary<string, List<ARPTID>> dicRptidAndVid = null;
            using (DBConnection_EF con = new DBConnection_EF())
            {
                dicRptidAndVid = rptidDAO.loadAllRPTIDByGroup(con);
            }
            return dicRptidAndVid;
        }

        public override bool buildReportIDAndVid(Dictionary<string, string[]> reportItems)
        {
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                try
                {
                    foreach (var item in reportItems)
                    {
                        //if RPTID is exists, remove them first
                        List<ARPTID> existRptIDs = rptidDAO.getAllVIDByRPTID(con, item.Key);
                        if (existRptIDs.Count > 0)
                        {
                            rptidDAO.RemoveByBatch(con, existRptIDs);
                        }

                        int order = 1;
                        foreach (var rpt_item in item.Value)
                        {
                            addRpt(con, item.Key, rpt_item, order++);
                        }
                    }
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
            return true;
        }
        public bool buildRptsFromMCS(Data.SECS.S2F33.RPTITEM[] rPTITEMs)
        {
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                foreach (var item in rPTITEMs)
                {
                    int order = 1;
                    foreach (var rpt_item in item.VIDS)
                    {
                        addRpt(con, item.REPTID, rpt_item, order++);
                    }
                }
            }
            return true;
        }



        public bool addRpt(DBConnection_EF con, string rpt_id, string vid, int order)
        {
            bool isSuccess = true;
            ARPTID rpt = new ARPTID()
            {
                RPTID = rpt_id.Trim(),
                VID = vid.Trim(),
                ORDER_NUM = order
            };
            rptidDAO.add(con, rpt);
            return isSuccess;
        }

        public void DeleteRptInfoByBatch()
        {
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                string sql = "DELETE FROM [ARPTID]";
                con.Database.ExecuteSqlCommand(sql);
            }
        }

        
    }
}
