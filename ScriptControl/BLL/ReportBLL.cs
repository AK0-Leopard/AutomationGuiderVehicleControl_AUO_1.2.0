// ***********************************************************************
// Assembly         : ScriptControl
// Author           : 
// Created          : 03-31-2016
//
// Last Modified By : 
// Last Modified On : 03-24-2016
// ***********************************************************************
// <copyright file="ReportBLL.cs" company="">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.Data;
using com.mirle.ibg3k0.sc.Data.DAO;
using com.mirle.ibg3k0.sc.Data.SECSDriver;
using com.mirle.ibg3k0.sc.Data.ValueDefMapAction;
using com.mirle.ibg3k0.sc.Data.VO;
using com.mirle.ibg3k0.sc.ProtocolFormat.OHTMessage;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace com.mirle.ibg3k0.sc.BLL
{
    /// <summary>
    /// Class ReportBLL.
    /// </summary>
    public class ReportBLL
    {
        /// <summary>
        /// The sc application
        /// </summary>
        protected SCApplication scApp = null;
        /// <summary>
        /// The logger
        /// </summary>
        private static Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The trace set DAO
        /// </summary>
        protected TraceSetDao traceSetDao = null;
        protected MCSReportQueueDao mcsReportQueueDao = null;
        protected DataCollectionDao dataCollectionDao = null;
        protected AlarmRptCondDao alarmRptCondDao = null;
        protected IBSEMDriver iBSEMDriver = null;
        /// <summary>
        /// Initializes a new instance of the <see cref="ReportBLL"/> class.
        /// </summary>
        public ReportBLL()
        {

        }

        /// <summary>
        /// Starts the specified sc application.
        /// </summary>
        /// <param name="scApp">The sc application.</param>
        public void start(SCApplication scApp)
        {
            this.scApp = scApp;
            traceSetDao = scApp.TraceSetDao;
            mcsReportQueueDao = scApp.MCSReportQueueDao;
            dataCollectionDao = scApp.DataCollectionDao;
            alarmRptCondDao = scApp.AlarmRptCondDao;
        }
        public void startMapAction(IBSEMDriver iBSEMDriver)
        {
            //var auo_mcsDefaultMapAction = scApp.getEQObjCacheManager().
            //      getLine().getMapActionByIdentityKey(typeof(AUOMCSDefaultMapAction).Name) as AUOMCSDefaultMapAction;
            //iBSEMDriver = auo_mcsDefaultMapAction;
            this.iBSEMDriver = iBSEMDriver;
        }

        /// <summary>
        /// Updates the trace set.
        /// </summary>
        /// <param name="trace_id">The trace_id.</param>
        /// <param name="smp_period">The smp_period.</param>
        /// <param name="total_smp_cnt">The total_smp_cnt.</param>
        /// <param name="svidList">The svid list.</param>
        public void updateTraceSet(string trace_id, string smp_period, int total_smp_cnt, List<string> svidList)
        {
            ATRACESET traceSet = new ATRACESET()
            {
                TRACE_ID = trace_id,
                SMP_PERIOD = smp_period,
                TOTAL_SMP_CNT = total_smp_cnt,
                TraceItemList = new List<ATRACEITEM>()
            };
            traceSet.calcNextSmpTime();
            List<ATRACEITEM> traceItems = new List<ATRACEITEM>();
            foreach (string svid in svidList)
            {
                ATRACEITEM tItem = new ATRACEITEM();
                tItem.TRACE_ID = trace_id;
                tItem.SVID = svid;
                traceItems.Add(tItem);
            }
            updateTraceSet(traceSet, traceItems);
        }

        /// <summary>
        /// Updates the trace set.
        /// </summary>
        /// <param name="traceSet">The trace set.</param>
        /// <param name="traceItems">The trace items.</param>
        public void updateTraceSet(ATRACESET traceSet, List<ATRACEITEM> traceItems)
        {
            DBConnection_EF conn = null;
            try
            {
                conn = DBConnection_EF.GetContext();
                conn.BeginTransaction();
                ATRACESET sv_traceSet = null;
                sv_traceSet = traceSetDao.getTraceSet(conn, true, traceSet.TRACE_ID);
                if (sv_traceSet != null)
                {
                    sv_traceSet.SMP_PERIOD = traceSet.SMP_PERIOD;
                    sv_traceSet.TOTAL_SMP_CNT = traceSet.TOTAL_SMP_CNT;
                    sv_traceSet.SMP_CNT = 0;            //重新開始
                    sv_traceSet.calcNextSmpTime();
                    traceSetDao.updateTraceSet(conn, sv_traceSet);
                }
                else
                {
                    sv_traceSet = traceSet;
                    sv_traceSet.NX_SMP_TIME = DateTime.Now;
                    sv_traceSet.SMP_TIME = DateTime.Now;
                    traceSetDao.insertTraceSet(conn, traceSet);
                }

                deleteTraceItem(traceSet.TRACE_ID);
                foreach (ATRACEITEM item in traceItems)
                {
                    traceSetDao.insertTraceItem(conn, item);
                }
                conn.Commit();
            }
            catch (Exception ex)
            {
                if (conn != null) { try { conn.Rollback(); } catch (Exception ex_rollback) { logger.Error(ex_rollback, "Exception"); } }
                logger.Error(ex, "Exception:");
            }
            finally
            {
                if (conn != null) { try { conn.Close(); } catch (Exception ex_close) { logger.Error(ex_close, "Exception:"); } }
            }
        }

        /// <summary>
        /// Deletes the trace item.
        /// </summary>
        /// <param name="trace_id">The trace_id.</param>
        public void deleteTraceItem(string trace_id)
        {
            DBConnection_EF conn = null;
            try
            {
                conn = DBConnection_EF.GetContext();
                conn.BeginTransaction();
                traceSetDao.deleteTraceItem(conn, trace_id);
                conn.Commit();
            }
            catch (Exception ex)
            {
                if (conn != null) { try { conn.Rollback(); } catch (Exception ex_rollback) { logger.Error(ex_rollback, "Exception"); } }
                logger.Error(ex, "Exception:");
            }
            finally
            {
                if (conn != null) { try { conn.Close(); } catch (Exception ex_close) { logger.Error(ex_close, "Exception:"); } }
            }
        }

        /// <summary>
        /// Updates the trace set.
        /// </summary>
        /// <param name="traceSet">The trace set.</param>
        public void updateTraceSet(ATRACESET traceSet)
        {

            DBConnection_EF conn = null;
            try
            {
                conn = DBConnection_EF.GetContext();
                conn.BeginTransaction();
                traceSetDao.updateTraceSet(conn, traceSet);
                conn.Commit();
            }
            catch (Exception ex)
            {
                if (conn != null) { try { conn.Rollback(); } catch (Exception ex_rollback) { logger.Error(ex_rollback, "Exception"); } }
                logger.Error(ex, "Exception:");
            }
            finally
            {
                if (conn != null) { try { conn.Close(); } catch (Exception ex_close) { logger.Error(ex_close, "Exception:"); } }
            }
        }

        /// <summary>
        /// Loads the active trace set data.
        /// </summary>
        /// <returns>List&lt;TraceSet&gt;.</returns>
        public List<ATRACESET> loadActiveTraceSetData()
        {
            List<ATRACESET> traceSetList = new List<ATRACESET>();
            DBConnection_EF conn = null;
            try
            {
                conn = DBConnection_EF.GetContext();

                traceSetList = traceSetDao.loadActiveTraceSet(conn);
                foreach (ATRACESET set in traceSetList)
                {
                    string trace_id = set.TRACE_ID;
                    List<ATRACEITEM> itemList = traceSetDao.loadTraceItem(conn, trace_id);
                    set.TraceItemList = itemList;
                }
            }
            catch (Exception ex)
            {
                if (conn != null) { try { conn.Rollback(); } catch (Exception ex_rollback) { logger.Error(ex_rollback, "Exception"); } }
                logger.Error(ex, "Exception:");
            }
            finally
            {
                if (conn != null) { try { conn.Close(); } catch (Exception ex_close) { logger.Error(ex_close, "Exception:"); } }
            }
            return traceSetList;
        }

        /// <summary>
        /// Loads the report trace set data.
        /// </summary>
        /// <returns>List&lt;TraceSet&gt;.</returns>
        public List<ATRACESET> loadReportTraceSetData()
        {
            List<ATRACESET> traceSetList = new List<ATRACESET>();
            DBConnection_EF conn = null;
            try
            {
                conn = DBConnection_EF.GetContext();

                List<ATRACESET> tmpTraceSetList = traceSetDao.loadActiveTraceSet(conn);
                DateTime reportNowDate = DateTime.Now;
                //
                foreach (ATRACESET traceSet in tmpTraceSetList)
                {
                    if (traceSet.NX_SMP_TIME.CompareTo(reportNowDate) <= 0 && traceSet.SMP_TIME.CompareTo(reportNowDate) < 0)
                    {
                        traceSetList.Add(traceSet);
                    }
                }
                //
                foreach (ATRACESET set in traceSetList)
                {
                    string trace_id = set.TRACE_ID;
                    List<ATRACEITEM> itemList = traceSetDao.loadTraceItem(conn, trace_id);
                    set.TraceItemList = itemList;
                }
            }
            catch (Exception ex)
            {
                if (conn != null) { try { conn.Rollback(); } catch (Exception ex_rollback) { logger.Error(ex_rollback, "Exception"); } }
                logger.Error(ex, "Exception:");
            }
            finally
            {
                if (conn != null) { try { conn.Close(); } catch (Exception ex_close) { logger.Error(ex_close, "Exception:"); } }
            }
            return traceSetList;
        }

        #region MCS SXFY Report
        public bool AskAreYouThere()
        {
            return iBSEMDriver.S1F1SendAreYouThere();
        }
        public bool AskDateAndTimeRequest()
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && iBSEMDriver.S2F17SendDateAndTimeRequest();
            return isSuccsess;
        }

        public bool ReportEquiptmentOffLine()
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendEquiptmentOffLine();
            return isSuccsess;
        }

        public bool ReportControlStateRemote()
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendControlStateRemote();
            return isSuccsess;
        }

        public bool ReportControlStateLocal()
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendControlStateLocal();
            return isSuccsess;
        }

        public bool ReportTSCAutoInitiated()
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendTSCAutoInitiated();
            return isSuccsess;
        }
        public bool ReportTSCPaused()
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendTSCPaused();
            return isSuccsess;
        }
        public bool ReportTSCAutoCompleted()
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendTSCAutoCompleted();
            return isSuccsess;
        }
        public virtual bool ReportTSCAutoCompleted(List<AMCSREPORTQUEUE> reportqueues)
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendTSCAutoCompleted(reportqueues);
            return isSuccsess;
        }
        public bool ReportTSCPauseInitiated()
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendTSCPauseInitiated();
            return isSuccsess;
        }
        public bool ReportTSCPauseCompleted()
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendTSCPauseCompleted();
            return isSuccsess;
        }
        public virtual bool ReportTSAvailChanged()
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendTSAvailChanged();
            return isSuccsess;
        }

        public virtual bool ReportTSCPauseCompleted(List<AMCSREPORTQUEUE> reportqueues)
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendTSCPauseCompleted(reportqueues);
            return isSuccsess;
        }

        public virtual bool ReportAlarmSet()
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendAlarmSet();
            return isSuccsess;
        }

        public virtual bool ReportAlarmSet(string vhid)
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendAlarmSet(vhid);
            return isSuccsess;
        }
        public virtual bool ReportAlarmCleared()
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendAlarmCleared();
            return isSuccsess;
        }

        public virtual bool ReportAlarmCleared(string vhid)
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendAlarmCleared(vhid);
            return isSuccsess;
        }

        public bool newReportTransferInitial(string cmdID, List<AMCSREPORTQUEUE> reportqueues)
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendTransferInitial(cmdID, reportqueues);
            return isSuccsess;
        }
        public bool newReportBeginTransfer(string vhID, List<AMCSREPORTQUEUE> reportqueues)
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendVehicleAssigned(vhID, reportqueues);
            //isSuccsess = isSuccsess && iBSEMDriver.S6F11SendTransferring(vhID, reportqueues);
            return isSuccsess;
        }
        public bool newReportLoadArrivals(string vhID, List<AMCSREPORTQUEUE> reportqueues)
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendVehicleArrived(vhID, reportqueues);
            return isSuccsess;
        }
        public virtual bool newReportLoadComplete(string vhID, BCRReadResult bcrReadResult, List<AMCSREPORTQUEUE> reportqueues)
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendVehicleAcquireCompleted(vhID, reportqueues);
            if (bcrReadResult == BCRReadResult.BcrNormal)
                isSuccsess = isSuccsess && iBSEMDriver.S6F11SendVehicleDeparted(vhID, reportqueues);
            return isSuccsess;
        }

        public virtual bool newReportLoadFailed(string vhID, List<AMCSREPORTQUEUE> reportqueues)
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendVehicleAcquireFailed(vhID, reportqueues);
            return isSuccsess;
        }
        public bool newReportCarrierIDReadReport(string vhID, List<AMCSREPORTQUEUE> reportqueues)
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendCarrierInstalled(vhID, reportqueues);
            return isSuccsess;
        }


        public bool newReportUnloadArrivals(string vhID, List<AMCSREPORTQUEUE> reportqueues)
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendVehicleArrived(vhID, reportqueues);
            return isSuccsess;
        }
        public virtual bool newReportUnloadComplete(string vhID, List<AMCSREPORTQUEUE> reportqueues)
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendCarrierRemoved(vhID, reportqueues);
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendVehicleDepositCompleted(vhID, reportqueues);
            return isSuccsess;
        }
        public virtual bool newReportUnloadFailed(string vhID, List<AMCSREPORTQUEUE> reportqueues)
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendVehicleDepositFailed(vhID, reportqueues);
            return isSuccsess;
        }
        public bool newReportLoading(string vhID, List<AMCSREPORTQUEUE> reportqueues)
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendTransferring(vhID, reportqueues);
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendVehicleAcquireStarted(vhID, reportqueues);
            return isSuccsess;
        }
        public bool newReportUnloading(string vhID, List<AMCSREPORTQUEUE> reportqueues)
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendVehicleDepositStarted(vhID, reportqueues);
            return isSuccsess;
        }
        public bool newReportTransferCommandFinish(string vhID, List<AMCSREPORTQUEUE> reportqueues)
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendVehicleUnassinged(vhID, reportqueues);
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendTransferCompleted(vhID, reportqueues);
            return isSuccsess;
        }
        public bool newReportTransferCommandFinish(ACMD_MCS CMD_MCS, AVEHICLE vh, string resultCode, List<AMCSREPORTQUEUE> reportqueues)
        {
            bool isSuccsess = true;
            if (vh != null)
            {
                isSuccsess = isSuccsess && iBSEMDriver.S6F11SendVehicleUnassinged(vh.VEHICLE_ID, reportqueues);
            }
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendTransferCompleted(CMD_MCS, vh, resultCode, reportqueues);
            return isSuccsess;
        }
        public bool newReportTransferCancelCompleted(string vhID, List<AMCSREPORTQUEUE> reportqueues)
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendVehicleUnassinged(vhID, reportqueues);
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendTransferCancelCompleted(vhID, reportqueues);
            return isSuccsess;
        }
        public bool newReportTransferCancelCompleted(ACMD_MCS cmd, List<AMCSREPORTQUEUE> reportqueues)
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendTransferCancelCompleted(cmd, reportqueues);
            return isSuccsess;
        }

        public bool newReportTransferCancelInitial(ACMD_MCS cmd, List<AMCSREPORTQUEUE> reportqueues)
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendTransferCancelInitial(cmd, reportqueues);
            return isSuccsess;
        }
        public bool newReportTransferCancelFailed(string cmdID, List<AMCSREPORTQUEUE> reportqueues)
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendTransferCancelFailed(cmdID, reportqueues);
            return isSuccsess;
        }
        public bool newReportTransferAbortInitial(string cmdID, List<AMCSREPORTQUEUE> reportqueues)
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendTransferAbortInitial(cmdID, reportqueues);
            return isSuccsess;
        }
        public bool newReportTransferAbortFailed(string cmdID, List<AMCSREPORTQUEUE> reportqueues)
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendTransferAbortFailed(cmdID, reportqueues);
            return isSuccsess;
        }
        public bool newReportTransferCommandAbortFinish(string vhID, List<AMCSREPORTQUEUE> reportqueues)
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendVehicleUnassinged(vhID, reportqueues);
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendTransferAbortCompleted(vhID, reportqueues);
            return isSuccsess;
        }



        public bool newReportUnitAlarmSet(string unitID, string alarmID, string alarmTest, List<AMCSREPORTQUEUE> reportqueues)
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && (iBSEMDriver as AUOMCSDefaultMapAction).S6F11SendUnitAlarmSet(unitID, alarmID, alarmTest, reportqueues);
            return isSuccsess;
        }
        public bool newReportUnitAlarmClear(string unitID, string alarmID, string alarmTest, List<AMCSREPORTQUEUE> reportqueues)
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && (iBSEMDriver as AUOMCSDefaultMapAction).S6F11SendUnitAlarmCleared(unitID, alarmID, alarmTest, reportqueues);
            return isSuccsess;
        }
        public bool newReportVehicleInstalled(string vhID, List<AMCSREPORTQUEUE> reportqueues)
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendVehicleInstalled(vhID, reportqueues);
            return isSuccsess;
        }
        public bool newReportVehicleRemoved(string vhID, List<AMCSREPORTQUEUE> reportqueues)
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && iBSEMDriver.S6F11SendVehicleRemoved(vhID, reportqueues);
            return isSuccsess;
        }


        public bool newReportPortInServeice(string portID, string portStatus, List<AMCSREPORTQUEUE> reportqueues)
        {
            bool isSuccsess = true;
            //not implementation...
            return isSuccsess;
        }

        public void newSendMCSMessage(List<AMCSREPORTQUEUE> reportqueues)
        {
            foreach (AMCSREPORTQUEUE queue in reportqueues)
                iBSEMDriver.S6F11SendMessage(queue);
        }

        public virtual bool ReportAlarmHappend(ErrorStatus alarm_status, string error_code, string desc)
        {
            string alcd = SCAppConstants.AlarmStatus.convert2MCS(alarm_status);
            string alid = error_code;
            string altx = desc;
            return iBSEMDriver.S5F1SendAlarmReport(alcd, alid, altx);
        }

        public virtual bool ReportAlarmHappend(ErrorStatus alarm_status, E_ALARM_LVL alarm_lvl, string error_code, string desc)
        {

            //string alcd = SECSConst.AlarmStatus.convert2MCS(alarm_status, alarm_lvl);
            //string alid = error_code;
            //string altx = desc;
            //return iBSEMDriver.S5F1SendAlarmReport(alcd, alid, altx);
            return false;
        }

        public virtual bool newReportUnitAlarmSet(string eq_id, ErrorStatus alarm_status, E_ALARM_LVL alarm_lvl, string error_code, string desc, List<AMCSREPORTQUEUE> reportqueues)
        {
            ////string alcd = SECSConst.AlarmStatus.convert2MCS(alarm_status, alarm_lvl);
            //string alid = SECSConst.ALID.convert2MCS(alarm_lvl);
            //string altx = SECSConst.ALTX.convert2MCS(alarm_lvl);
            //bool isSuccsess = true;
            //isSuccsess = isSuccsess && iBSEMDriver.S6F11SendUnitAlarmSet(alid, altx, error_code, desc, reportqueues);
            return false;
        }
        public virtual bool newReportUnitAlarmClear(string eq_id, ErrorStatus alarm_status, E_ALARM_LVL alarm_lvl, string error_code, string desc, List<AMCSREPORTQUEUE> reportqueues)
        {
            ////string alcd = SECSConst.AlarmStatus.convert2MCS(alarm_status, alarm_lvl);
            //string alid = SECSConst.ALID.convert2MCS(alarm_lvl);
            //string altx = SECSConst.ALTX.convert2MCS(alarm_lvl);
            //bool isSuccsess = true;
            //isSuccsess = isSuccsess && iBSEMDriver.S6F11SendUnitAlarmClear(alid, altx, error_code, desc, reportqueues);
            return false;
        }


        public virtual bool newReportAlarmEvent(string eq_id, string ceid, string alid, string cmd_id, string altx, string alarmLvl, List<AMCSREPORTQUEUE> reportQueues)
        {
            bool isSuccsess = true;
            isSuccsess = isSuccsess && (iBSEMDriver as AUOMCSDefaultMapAction).S6F11SendAlarmEvent(eq_id, ceid, alid, cmd_id, altx, alarmLvl, reportQueues);
            return isSuccsess;
        }



        public void insertMCSReport(List<AMCSREPORTQUEUE> mcsQueues)
        {
            //using (DBConnection_EF con = DBConnection_EF.GetUContext())
            //{
            //    mcsReportQueueDao.AddByBatch(con, mcsQueues);
            //}
        }

        public void insertMCSReport(AMCSREPORTQUEUE mcs_queue)
        {
            //lock (mcs_report_lock_obj)
            //{
            //SCUtility.LockWithTimeout(mcs_report_lock_obj, SCAppConstants.LOCK_TIMEOUT_MS,
            //    () =>
            //    {
            //        //DBConnection_EF con = DBConnection_EF.GetContext();
            //        //using (DBConnection_EF con = new DBConnection_EF())
            //        using (DBConnection_EF con = DBConnection_EF.GetUContext())
            //        {
            //            mcsReportQueueDao.add(con, mcs_queue);
            //        }
            //    });
            //}
        }
        object mcs_report_lock_obj = new object();

        public bool updateMCSReportTime2Empty(AMCSREPORTQUEUE ReportQueue)
        {
            bool isSuccess = false;
            //DBConnection_EF con = DBConnection_EF.GetContext();
            try
            {
                //using (DBConnection_EF con = new DBConnection_EF())
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    //con.BeginTransaction();
                    con.AMCSREPORTQUEUE.Attach(ReportQueue);
                    ReportQueue.REPORT_TIME = null;
                    con.Entry(ReportQueue).Property(p => p.REPORT_TIME).IsModified = true;
                    mcsReportQueueDao.Update(con, ReportQueue);
                    con.Entry(ReportQueue).State = System.Data.Entity.EntityState.Detached;

                    //con.Commit();
                }
                isSuccess = true;
            }
            catch (Exception ex)
            {
                //if (con != null) { try { con.Rollback(); } catch (Exception ex_rollback) { logger.Error(ex_rollback, "Exception"); } }
                logger.Error(ex, "Exception");
                return isSuccess;
            }
            finally
            {
                //if (con != null) { try { con.Close(); } catch (Exception ex_close) { logger.Error(ex_close, "Exception"); } }
            }
            return isSuccess;
        }


        public bool sendMCSMessage(AMCSREPORTQUEUE mcsMessageQueue)
        {
            return iBSEMDriver.S6F11SendMessage(mcsMessageQueue);
        }


        public List<AMCSREPORTQUEUE> loadNonReportEvent()
        {
            List<AMCSREPORTQUEUE> AMCSREPORTQUEUEs;
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                AMCSREPORTQUEUEs = mcsReportQueueDao.loadByNonReport(con);
            }

            return AMCSREPORTQUEUEs;
        }
        #endregion MCS SXFY Report


        #region Zabbix Report


        public Tuple<string, int> getZabbixServerIPAndPort()
        {
            DataCollectionSetting setting = dataCollectionDao.getDataCollectionFirstItem(scApp);
            string ip = setting.IP;
            var remoteipAdr = System.Net.Dns.GetHostAddresses(setting.IP);
            if (remoteipAdr != null && remoteipAdr.Count() != 0)
            {
                ip = remoteipAdr[0].ToString();
            }
            return new Tuple<string, int>(ip, setting.Port);
        }
        public string getZabbixHostName()
        {
            //DataCollectionSetting setting = dataCollectionDao.getDataCollectionFirstItem(scApp);
            //return setting.Method;
            return SCApplication.ServerName;
        }

        public bool IsReportZabbixInfo(string item_name)
        {
            DataCollectionSetting setting = dataCollectionDao.getDataCollectionItemByMethodAndItemName(scApp, item_name);
            if (setting == null)
                return false;
            return setting.IsReport;
        }
        public void ZabbixPush(string key, int value)
        {
            ZabbixPush(key, value.ToString());
        }
        //[Conditional("Release")]
        public void ZabbixPush(string key, string value)
        {
            try
            {
                string zabbix_host_name = getZabbixHostName();
                if (!IsReportZabbixInfo(key))
                    return;
                //var response1 = scApp.ZabbixService.Send(zabbix_host_name, key, value);
                //if (response1.Failed != 0)
                //{
                //    logger.Error($"Push zabbix fail,key:{key},value:{value},info:{response1.Info},responsel:{response1.Response}");
                //}
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception:");
            }
        }
        #endregion Zabbix Report





    }
}
