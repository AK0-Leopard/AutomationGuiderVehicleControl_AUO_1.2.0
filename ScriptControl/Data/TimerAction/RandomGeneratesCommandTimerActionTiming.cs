// ***********************************************************************
// Assembly         : ScriptControl
// Author           : 
// Created          : 03-31-2016
//
// Last Modified By : 
// Last Modified On : 03-24-2016
// ***********************************************************************
// <copyright file="BCSystemStatusTimer.cs" company="">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using com.mirle.ibg3k0.bcf.Data.TimerAction;
using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.Data.DAO;
using com.mirle.ibg3k0.sc.Data.SECS;
using NLog;

namespace com.mirle.ibg3k0.sc.Data.TimerAction
{
    /// <summary>
    /// Class BCSystemStatusTimer.
    /// </summary>
    /// <seealso cref="com.mirle.ibg3k0.bcf.Data.TimerAction.ITimerAction" />
    public class RandomGeneratesCommandTimerActionTiming : ITimerAction
    {
        /// <summary>
        /// The logger
        /// </summary>
        private static Logger logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// The sc application
        /// </summary>
        protected SCApplication scApp = null;

        public Dictionary<int, List<TranTask>> dicTranTaskSchedule = null;
        public int MCS_TaskIndex = 0;//420


        /// <summary>
        /// Initializes a new instance of the <see cref="BCSystemStatusTimer"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="intervalMilliSec">The interval milli sec.</param>
        public RandomGeneratesCommandTimerActionTiming(string name, long intervalMilliSec)
            : base(name, intervalMilliSec)
        {

        }
        /// <summary>
        /// Initializes the start.
        /// </summary>
        public override void initStart()
        {
            scApp = SCApplication.getInstance();
            dicTranTaskSchedule = scApp.CMDBLL.loadTranTaskSchedule_24Hour();

        }
        /// <summary>
        /// Timer Action的執行動作
        /// </summary>
        /// <param name="obj">The object.</param>
        private long syncPoint = 0;
        public override void doProcess(object obj)
        {
            if (!DebugParameter.CanAutoRandomGeneratesCommand) return;
            if (System.Threading.Interlocked.Exchange(ref syncPoint, 1) == 0)
            {
                try
                {

                    TimingGeneratesCommands();
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Exception");
                }
                finally
                {
                    System.Threading.Interlocked.Exchange(ref syncPoint, 0);
                }
            }
        }

        private void TimingGeneratesCommands()
        {
            try
            {
                List<TranTask> lstTranTask = null;
                logger.Trace($"MCS Index:{MCS_TaskIndex}.");
                if (dicTranTaskSchedule.TryGetValue(MCS_TaskIndex, out lstTranTask))
                {
                    if (lstTranTask != null)//只做24小時
                    {
                        foreach (TranTask task in lstTranTask)
                        {
                            string cst_id = task.CSTID;
                            string priority = task.Priority;
                            string source = task.SourcePort;
                            string destination = task.DestinationPort;
                            Task.Run(() => sendTranCmd(cst_id, priority, source, destination));
                            SpinWait.SpinUntil(() => false, 100);
                        }
                    }
                }
                MCS_TaskIndex++;
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, "Exception");
            }

        }

        public void sendTranCmd(string carrier_id, string priority, string source_port, string destn_port)
        {
            string cmdID = DateTime.Now.ToString("yyyyMMddHHmmssfffff");
            logger.Trace($"Creat command,CMD ID:{cmdID}, CST ID:{carrier_id},Priority:{priority},Source:{source_port},Destination:{destn_port}.");

            scApp.CMDBLL.doCreatMCSCommand(cmdID, priority, "0", carrier_id, source_port, destn_port, SECSConst.HCACK_Confirm);
            scApp.SysExcuteQualityBLL.creatSysExcuteQuality(cmdID, carrier_id, source_port, destn_port);
            //SpinWait.SpinUntil(() => false, 5000);
            //scApp.CMDBLL.checkMCSTransferCommand();
            scApp.CMDBLL.checkMCSTransferCommand_New();
        }
    }

}

