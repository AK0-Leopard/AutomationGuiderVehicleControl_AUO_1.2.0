using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.Data;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace com.mirle.ibg3k0.sc.Scheduler
{
    public class DBManatainScheduler : IJob
    {
        NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        SCApplication scApp = SCApplication.getInstance();
        public void Execute(IJobExecutionContext context)
        {
            MoveACMD_MCSToHCMD_MCS();
            SpinWait.SpinUntil(() => false, 5000);
            MoveACMD_OHTCToHCMD_OHTC();
            SpinWait.SpinUntil(() => false, 5000);
            DeleteOldHCMD_MCS();
            SpinWait.SpinUntil(() => false, 5000);
            DeleteOldHCMD_OHTC();
            //SpinWait.SpinUntil(() => false, 5000);
            //DeleteOldAlarm();
        }

        private void MoveACMD_MCSToHCMD_MCS()
        {
            try
            {
                var finish_cmd_mcs_list = scApp.CMDBLL.loadFinishCMD_MCS();
                if (finish_cmd_mcs_list != null && finish_cmd_mcs_list.Count > 0)
                {
                    using (TransactionScope tx = SCUtility.getTransactionScope())
                    {
                        using (DBConnection_EF con = DBConnection_EF.GetUContext())
                        {
                            scApp.CMDBLL.remoteCMD_MCSByBatch(finish_cmd_mcs_list);
                            List<HCMD_MCS> hcmd_mcs_list = finish_cmd_mcs_list.Select(cmd => cmd.ToHCMD_MCS()).ToList();
                            scApp.CMDBLL.CreatHCMD_MCSs(hcmd_mcs_list);
                            tx.Complete();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
            }
        }
        private void MoveACMD_OHTCToHCMD_OHTC()
        {
            try
            {
                var finish_cmd_ohtc_list = scApp.CMDBLL.loadFinishCMD_OHTC();
                if (finish_cmd_ohtc_list != null && finish_cmd_ohtc_list.Count > 0)
                {
                    using (TransactionScope tx = SCUtility.getTransactionScope())
                    {
                        using (DBConnection_EF con = DBConnection_EF.GetUContext())
                        {
                            scApp.CMDBLL.remoteCMD_OHTCByBatch(finish_cmd_ohtc_list);
                            List<HCMD_OHTC> hcmd_ohtc_list = finish_cmd_ohtc_list.Select(cmd => cmd.ToHCMD_OHTC()).ToList();
                            scApp.CMDBLL.CreatHCMD_OHTCs(hcmd_ohtc_list);
                            tx.Complete();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
            }
        }
        private void DeleteOldHCMD_MCS()
        {
            var hcmd_mcs_list = scApp.CMDBLL.loadHCMD_MCSBefore6Months();
            if (hcmd_mcs_list != null && hcmd_mcs_list.Count > 0)
            {
                scApp.CMDBLL.RemoveHCMD_MCSByBatch(hcmd_mcs_list); ;
            }
        }
        private void DeleteOldHCMD_OHTC()
        {
            var hcmd_ohtc_list = scApp.CMDBLL.loadHCMD_OHTCBefore6Months();
            if (hcmd_ohtc_list != null && hcmd_ohtc_list.Count > 0)
            {
                scApp.CMDBLL.RemoveHCMD_OHTCByBatch(hcmd_ohtc_list); ;
            }
        }
        private void DeleteOldAlarm()
        {
            var alarms = scApp.AlarmBLL.loadClearedAlarmBefore6Months();
            if (alarms != null && alarms.Count > 0)
            {
                scApp.AlarmBLL.RemoveOldClearedAlarmByBatch(alarms); ;
            }
        }
    }

}
