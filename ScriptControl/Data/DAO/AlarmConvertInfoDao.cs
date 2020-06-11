// ***********************************************************************
// Assembly         : ScriptControl
// Author           : 
// Created          : 03-31-2016
//
// Last Modified By : 
// Last Modified On : 03-24-2016
// ***********************************************************************
// <copyright file="AlarmConvertInfoDao.cs" company="">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using com.mirle.ibg3k0.bcf.Data;
using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Data.VO;
using NLog;
using com.mirle.ibg3k0.bcf.Common;

namespace com.mirle.ibg3k0.sc.Data.DAO
{
    /// <summary>
    /// Class AlarmMapDao.
    /// </summary>
    /// <seealso cref="com.mirle.ibg3k0.bcf.Data.DaoBase" />
    public class AlarmConvertInfoDao : DaoBase
    {
        /// <summary>
        /// The logger
        /// </summary>
        private static Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Loads the alarm maps by eq real identifier.
        /// </summary>
        /// <param name="object_id">The eqpt_real_id.</param>
        /// <returns>List&lt;AlarmMap&gt;.</returns>
        public List<AlarmConvertInfo> loadAlarmConvertInfos()
        {
            try
            {
                DataTable dt = SCApplication.getInstance().OHxCConfig.Tables["ALARMCONVERTINFO"];
                var query = from c in dt.AsEnumerable()
                            select new AlarmConvertInfo
                            {
                                ALID = c.Field<string>("ALID"),
                                ALCD = c.Field<string>("ALCD"),
                                ALED = c.Field<string>("ALED"),
                                ALTX = c.Field<string>("ALTX"),
                                CEIDSet = c.Field<string>("CEIDSet"),
                                CEIDClear = c.Field<string>("CEIDClear"),
                                AlarmLevel = c.Field<string>("AlarmLevel"),
                                AGVAlarmCode = c.Field<string>("AGVAlarmCode")
                                //ALARM_LVL = convertALARM_LVL2Enum(c.Field<string>("AGVAlarmLevel"))
                            };
                return query.ToList();
            }
            catch (Exception ex)
            {
                logger.Warn(ex);
                throw;
            }
        }
     
        /// <summary>
        /// Gets the alarm map.
        /// </summary>
        /// <param name="object_id">The eqpt_real_id.</param>
        /// <param name="alarm_id">The alarm_id.</param>
        /// <returns>AlarmMap.</returns>
        public AlarmConvertInfo getAlarmConvertInfo(string alarm_id)
        {
            try
            {
                DataTable dt = SCApplication.getInstance().OHxCConfig.Tables["ALARMCONVERTINFO"];
                var query = from c in dt.AsEnumerable()
                            where c.Field<string>("AGVAlarmCode").Trim() == alarm_id.Trim()
                            select new AlarmConvertInfo
                            {
                                ALID = c.Field<string>("ALID"),
                                ALCD = c.Field<string>("ALCD"),
                                ALED = c.Field<string>("ALED"),
                                ALTX = c.Field<string>("ALTX"),
                                CEIDSet = c.Field<string>("CEIDSet"),
                                CEIDClear = c.Field<string>("CEIDClear"),
                                AlarmLevel = c.Field<string>("AlarmLevel"),
                                AGVAlarmCode = c.Field<string>("AGVAlarmCode")
                                //ALARM_LVL = convertALARM_LVL2Enum(c.Field<string>("AGVAlarmLevel"))
                            };
                return query.SingleOrDefault();
            }
            catch (Exception ex)
            {
                logger.Warn(ex);
                throw;
            }
        }

        /// <summary>
        /// 將文字的1(Warn)、2(Error)轉換成enum:E_ALARM_LVL
        /// </summary>
        /// <param name="s_lvl"></param>
        /// <returns></returns>
        private E_ALARM_LVL convertALARM_LVL2Enum(string s_lvl)
        {
            int i_lvl = int.Parse(s_lvl);
            return (E_ALARM_LVL)i_lvl;
        }


    }
}
