//*********************************************************************************
//      AlarmBLL.cs
//*********************************************************************************
// File Name: AlarmBLL.cs
// Description: 業務邏輯：Alarm
//
//(c) Copyright 2014, MIRLE Automation Corporation
//
// Date          Author         Request No.    Tag     Description
// ------------- -------------  -------------  ------  -----------------------------
//**********************************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using com.mirle.ibg3k0.bcf.Common;
using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.Data.DAO;
using com.mirle.ibg3k0.sc.Data.SECS;
using com.mirle.ibg3k0.sc.Data.VO;
using NLog;
using com.mirle.ibg3k0.bcf.App;
using com.mirle.ibg3k0.sc.Data;
using Newtonsoft.Json;

namespace com.mirle.ibg3k0.sc.BLL
{
    /// <summary>
    /// Class AlarmBLL.
    /// </summary>
    public class AlarmBLL
    {

        public const string VEHICLE_ALARM_HAPPEND = "00000";
        public const string VEHICLE_LONG_TIME_INACTION = "10000";
        public const string VEHICLE_CAN_NOT_SERVICE = "10001";
        public const string VEHICLE_LONG_TIME_INSTALLED_CARRIER = "10002";
        /// <summary>
        /// The sc application
        /// </summary>
        protected SCApplication scApp = null;
        /// <summary>
        /// The logger
        /// </summary>
        private static Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The alarm DAO
        /// </summary>
        protected AlarmDao alarmDao = null;
        /// <summary>
        /// The line DAO
        /// </summary>
        protected LineDao lineDao = null;
        /// <summary>
        /// The alarm RPT cond DAO
        /// </summary>
        protected AlarmRptCondDao alarmRptCondDao = null;
        /// <summary>
        /// The alarm map DAO
        /// </summary>
        protected AlarmMapDao alarmMapDao = null;
        protected AlarmConvertInfoDao alarmConvertInfoDao = null;
        protected MainAlarmDao mainAlarmDao = null;
        /// <summary>
        /// Initializes a new instance of the <see cref="AlarmBLL"/> class.
        /// </summary>
        public AlarmBLL()
        {

        }

        /// <summary>
        /// Starts the specified sc application.
        /// </summary>
        /// <param name="scApp">The sc application.</param>
        public void start(SCApplication scApp)
        {
            this.scApp = scApp;
            alarmDao = scApp.AlarmDao;
            lineDao = scApp.LineDao;
            alarmRptCondDao = scApp.AlarmRptCondDao;
            alarmMapDao = scApp.AlarmMapDao;
            mainAlarmDao = scApp.MainAlarmDao;
            alarmConvertInfoDao = scApp.AlarmConvertInfoDao;
        }

        #region Alarm Map
        //public AlarmMap getAlarmMap(string eqpt_real_id, string alarm_id)
        //{
        //    DBConnection conn = null;
        //    AlarmMap alarmMap = null;
        //    try
        //    {
        //        conn = scApp.getDBConnection();
        //        conn.BeginTransaction();

        //        alarmMap = alarmMapDao.getAlarmMap(conn, eqpt_real_id, alarm_id);
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.Warn("getAlarmMap Exception!", ex);
        //    }
        //    return alarmMap;
        //}
        public List<AlarmMap> loadAlarmMaps()
        {
            List<AlarmMap> alarmMaps = alarmMapDao.loadAlarmMaps();
            return alarmMaps;
        }
        #endregion Alarm Map

        #region AlarmConvertInfo
        public List<AlarmConvertInfo> loadAlarmConvertInfos()
        {
            List<AlarmConvertInfo> alarmConvertInfos = alarmConvertInfoDao.loadAlarmConvertInfos();
            return alarmConvertInfos;
        }
        public AlarmConvertInfo getAlarmConvertInfo(string alarm_id)
        {
            AlarmConvertInfo alarmConvertInfo = alarmConvertInfoDao.getAlarmConvertInfo(alarm_id);
            return alarmConvertInfo;
        }
        #endregion AlarmConvertInfo
        object lock_obj_alarm = new object();
        public virtual ALARM setAlarmReport(string nodeID, string eq_id, string error_code, string errorDesc)
        {
            lock (lock_obj_alarm)
            {
                if (IsAlarmExist(eq_id, error_code)) return null;
                //AlarmMap alarmMap = alarmMapDao.getAlarmMap(eq_id, error_code);
                AlarmMap alarmMap = alarmMapDao.getAlarmMap(nodeID, error_code);
                string strNow = BCFUtility.formatDateTime(DateTime.Now, SCAppConstants.TimestampFormat_19);
                ALARM alarm = new ALARM()
                {
                    EQPT_ID = eq_id,
                    RPT_DATE_TIME = DateTime.Now,
                    ALAM_CODE = error_code,
                    ALAM_LVL = alarmMap == null ? E_ALARM_LVL.None : alarmMap.ALARM_LVL,
                    ALAM_STAT = ProtocolFormat.OHTMessage.ErrorStatus.ErrSet,
                    ALAM_DESC = alarmMap == null ? errorDesc : alarmMap.ALARM_DESC,
                };
                if (SCUtility.isEmpty(alarm.ALAM_DESC))
                {
                    alarm.ALAM_DESC = $"Unknow error:{error_code}";
                }
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    alarmDao.insertAlarm(con, alarm);
                    CheckSetAlarm();
                }

                return alarm;
            }
        }

        public virtual void setAlarmReport2Redis(ALARM alarm)
        {
            if (alarm == null) return;
            string hash_field = $"{alarm.EQPT_ID}_{alarm.ALAM_CODE}";
            scApp.getRedisCacheManager().AddTransactionCondition(StackExchange.Redis.Condition.HashNotExists(SCAppConstants.REDIS_KEY_CURRENT_ALARM, hash_field));
            scApp.getRedisCacheManager().HashSetAsync(SCAppConstants.REDIS_KEY_CURRENT_ALARM, hash_field, JsonConvert.SerializeObject(alarm));
        }

        public List<ALARM> getCurrentAlarmsFromRedis()
        {
            List<ALARM> alarms = new List<ALARM>();
            var redis_values_alarms = scApp.getRedisCacheManager().HashValuesAsync(SCAppConstants.REDIS_KEY_CURRENT_ALARM).Result;
            foreach (string redis_value_alarm in redis_values_alarms)
            {
                ALARM alarm_obj = (ALARM)JsonConvert.DeserializeObject(redis_value_alarm, typeof(ALARM));
                alarms.Add(alarm_obj);
            }
            return alarms;
        }

        public List<ALARM> GetAlarms(DateTime startTime, DateTime endTime)
        {
            List<ALARM> alarm = null;
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                alarm = alarmDao.getAlarms(con, startTime, endTime);
            }
            return alarm;
        }

        public virtual List<ALARMRPTCOND> loadAllAlarmReport(bool is_enable)
        {
            List<ALARMRPTCOND> aLARMRPTCONDs = null;
            try
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    aLARMRPTCONDs = alarmRptCondDao.loadRptCond(con, is_enable);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
            }
            return aLARMRPTCONDs;
        }

        public List<ALARM> getCurrentAlarms()
        {
            List<ALARM> alarms = new List<ALARM>();
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                alarms = alarmDao.loadSetAlarm(con);
            }
            return alarms;
        }

        public List<ALARM> getCurrentSeriousAlarms()
        {
            List<ALARM> alarms = new List<ALARM>();
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                alarms = alarmDao.loadSetSeriousAlarm(con);
            }
            return alarms;
        }

        public virtual ALARM resetAlarmReport(string eq_id, string error_code)
        {
            lock (lock_obj_alarm)
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    ALARM alarm = alarmDao.getSetAlarm(con, eq_id, error_code);
                    if (alarm != null)
                    {
                        alarm.ALAM_STAT = ProtocolFormat.OHTMessage.ErrorStatus.ErrReset;
                        alarm.CLEAR_DATE_TIME = DateTime.Now;
                        alarmDao.updateAlarm(con, alarm);
                        CheckSetAlarm();
                    }
                    return alarm;
                }
            }
        }
        public List<ALARM> GetALARMsBySetTimeClearTime(DateTime set_time, DateTime clear_time)
        {
            List<ALARM> alarms = null;
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                alarms = alarmDao.loadAllAlarmByStartTimeEndTime(con, set_time, clear_time);
            }
            return alarms;
        }
        public virtual void resetAlarmReport2Redis(ALARM alarm)
        {
            if (alarm == null) return;
            string hash_field = $"{alarm.EQPT_ID.Trim()}_{alarm.ALAM_CODE.Trim()}";
            //scApp.getRedisCacheManager().AddTransactionCondition(StackExchange.Redis.Condition.HashExists(SCAppConstants.REDIS_KEY_CURRENT_ALARM, hash_field));
            scApp.getRedisCacheManager().HashDeleteAsync(SCAppConstants.REDIS_KEY_CURRENT_ALARM, hash_field);
        }


        public List<ALARM> resetAllAlarmReport(string eq_id)
        {
            List<ALARM> alarms = null;
            lock (lock_obj_alarm)
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    alarms = alarmDao.loadSetAlarm(con, eq_id);

                    if (alarms != null)
                    {
                        foreach (ALARM alarm in alarms.ToList())
                        {
                            if (!SCUtility.isMatche(alarm.ALAM_CODE, VEHICLE_ALARM_HAPPEND))
                            {
                                alarm.ALAM_STAT = ProtocolFormat.OHTMessage.ErrorStatus.ErrReset;
                                alarm.CLEAR_DATE_TIME = DateTime.Now;
                                alarmDao.updateAlarm(con, alarm);
                            }
                            else
                            {
                                alarms.Remove(alarm);
                            }
                        }
                        CheckSetAlarm();
                    }
                }
            }
            return alarms;
        }



        public void resetAllAlarmReport2Redis(string vh_id)
        {
            var current_all_alarm = scApp.getRedisCacheManager().HashKeys(SCAppConstants.REDIS_KEY_CURRENT_ALARM);
            var vh_all_alarm = current_all_alarm.Where(redisKey => ((string)redisKey).Contains(vh_id)).ToArray();
            scApp.getRedisCacheManager().HashDeleteAsync(SCAppConstants.REDIS_KEY_CURRENT_ALARM, vh_all_alarm);
        }
        private bool IsAlarmExist(string eq_id, string code)
        {
            bool isExist = false;
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                isExist = alarmDao.getSetAlarmCountByEQAndCode(con, eq_id, code) > 0;
            }
            return isExist;
        }
        public bool IsReportToHost(string code)
        {
            return true;
        }

        public AlarmMap GetAlarmMap(string objID, string errorCode)
        {
            AlarmMap alarmMap = alarmMapDao.getAlarmMap(objID, errorCode);
            return alarmMap;
        }

        public bool hasAlarmExist()
        {
            var redis_values_alarms = scApp.getRedisCacheManager().HashValuesAsync(SCAppConstants.REDIS_KEY_CURRENT_ALARM).Result;
            if (redis_values_alarms.Count() > 0)
            {
                return true;
            }
            return false;
        }

        public bool hasAlarmErrorExist()
        {
            int count = 0;

            lock (lock_obj_alarm)
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    count = alarmDao.GetSetAlarmErrorCount(con);
                }
            }
            return count != 0;
        }

        public bool hasAlarmWarningExist()
        {
            int count = 0;

            lock (lock_obj_alarm)
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    count = alarmDao.GetSetAlarmWaringCount(con);
                }
            }
            return count != 0;
        }

        public virtual bool hasAlarmExistByEQ(string eqid)
        {

            bool isExist = false;
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                isExist = alarmDao.getSetAlarmCountByEQ(con, eqid) > 0;
            }
            return isExist;
        }

        public virtual List<ALARMRPTCOND> loadAllAlarmReport()
        {
            List<ALARMRPTCOND> aLARMRPTCONDs = null;
            try
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    aLARMRPTCONDs = alarmRptCondDao.loadRptCond(con);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
            }
            return aLARMRPTCONDs;
        }


        public virtual bool enableAllAlarmReport(Boolean isEnable)
        {
            bool isSuccess = true;
            try
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    alarmRptCondDao.deleteAllRptCond(con);
                    List<AlarmMap> alarmMaps = loadAlarmMaps();
                    foreach(AlarmMap alarmMap in alarmMaps)
                    {
                        ALARMRPTCOND cond = new ALARMRPTCOND()
                        {
                            ALAM_CODE = alarmMap.ALARM_ID,
                            EQPT_ID = alarmMap.EQPT_REAL_ID,
                            ENABLE_FLG = (isEnable ? SCAppConstants.YES_FLAG : SCAppConstants.NO_FLAG)
                        };
                        alarmRptCondDao.insertRptCond(con, cond);
                    }

                }
            }
            catch (Exception ex)
            {
                isSuccess = true;
                logger.Error(ex, "Exception");
            }
            return isSuccess;
        }

        public bool enableAlarmReport(string alarm_id, Boolean isEnable)
        {
            bool isSuccess = true;
            try
            {
                string enable_flag = (isEnable ? SCAppConstants.YES_FLAG : SCAppConstants.NO_FLAG);

                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    ALARMRPTCOND cond = null;
                    cond = alarmRptCondDao.getRptCond(con, alarm_id);
                    if (cond != null)
                    {
                        cond.ENABLE_FLG = enable_flag;
                        alarmRptCondDao.insertRptCond(con, cond);
                    }
                    else
                    {
                        cond = new ALARMRPTCOND()
                        {
                            ALAM_CODE = alarm_id,
                            ENABLE_FLG = enable_flag
                        };
                        alarmRptCondDao.insertRptCond(con, cond);
                    }
                }
            }
            catch (Exception ex)
            {
                isSuccess = true;
                logger.Error(ex, "Exception");
            }
            return isSuccess;
        }

        public string onMainAlarm(string mAlarmCode, params object[] args)
        {
            MainAlarm mainAlarm = mainAlarmDao.getMainAlarmByCode(mAlarmCode);
            bool isAlarm = false;
            string msg = string.Empty;
            try
            {
                if (mainAlarm != null)
                {
                    isAlarm = mainAlarm.CODE.StartsWith("A");
                    msg = string.Format(mainAlarm.DESCRIPTION, args);
                    if (isAlarm)
                    {
                        msg = string.Format("[{0}]{2}", mainAlarm.CODE, Environment.NewLine, msg);
                        BCFApplication.onErrorMsg(msg);
                    }
                    else
                    {
                        msg = string.Format("[{0}]{2}", mainAlarm.CODE, Environment.NewLine, msg);
                        BCFApplication.onWarningMsg(msg);
                    }
                }
                else
                {
                    logger.Warn(string.Format("LFC alarm/warm happen, but no defin remark code:[{0}] !!!", mAlarmCode));
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception:");
            }
            return msg;
        }

        object lock_obj_alarm_happen = new object();
        public void CheckSetAlarm()
        {
            lock (lock_obj_alarm_happen)
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    ALINE line = scApp.getEQObjCacheManager().getLine();
                    List<ALARM> alarmLst = alarmDao.loadSetAlarm(con);

                    if (alarmLst != null && alarmLst.Count > 0)
                    {
                        line.IsAlarmHappened = true;
                    }
                    else
                    {
                        line.IsAlarmHappened = false;
                    }
                }
            }
        }

        public List<ALARM> loadClearedAlarmBefore6Months()
        {
            List<ALARM> alarms;
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                alarms = alarmDao.loadBefore6Months(con);
            }
            return alarms;
        }

        public void RemoveOldClearedAlarmByBatch(List<ALARM> alarms)
        {
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                alarmDao.RemoveByBatch(con, alarms);
            }
        }
    }
}
