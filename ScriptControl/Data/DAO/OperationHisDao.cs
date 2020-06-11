// ***********************************************************************
// Assembly         : ScriptControl
// Author           : 
// Created          : 03-31-2016
//
// Last Modified By : 
// Last Modified On : 03-24-2016
// ***********************************************************************
// <copyright file="OperationHisDao.cs" company="">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using com.mirle.ibg3k0.bcf.Common;
using com.mirle.ibg3k0.bcf.Data;
using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Data.VO;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.mirle.ibg3k0.sc.Data.DAO
{
    /// <summary>
    /// Class OperationHisDao.
    /// </summary>
    /// <seealso cref="com.mirle.ibg3k0.bcf.Data.DaoBase" />
    public class OperationHisDao : DaoBase
    {
        /// <summary>
        /// The logger
        /// </summary>
        private static Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Inserts the operation his.
        /// </summary>
        /// <param name="conn">The connection.</param>
        /// <param name="opHis">The op his.</param>
        public void insertOperationHis(DBConnection_EF conn, HOPERATION opHis)
        {
            try
            {
                conn.HOPERATION.Add(opHis);
                conn.SaveChanges();
            }
            catch (Exception ex)
            {
                logger.Warn(ex);
                throw;
            }
        }

        /// <summary>
        /// Loads the operation his.
        /// </summary>
        /// <param name="conn">The connection.</param>
        /// <returns>List&lt;OperationHis&gt;.</returns>
        public List<HOPERATION> loadOperationHis(DBConnection_EF conn)
        {
            List<HOPERATION> rtnList = new List<HOPERATION>();
            try
            {
                var query = from opHis in conn.HOPERATION
                            select opHis;
                rtnList = query.ToList();
            }
            catch (Exception ex)
            {
                logger.Warn(ex);
            }
            return rtnList;
        }

        public List<HOPERATION> loadOperationHisByTime(DBConnection_EF con, DateTime startTime, DateTime endTime)
        {
            var query = from operation in con.HOPERATION
                        where operation.INSERT_TIME > startTime && operation.INSERT_TIME < endTime
                        orderby operation.INSERT_TIME descending
                        select operation;
            return query.ToList();
        }
        public List<HOPERATION> loadOperationHisByTimeUserIDFormName(DBConnection_EF con, DateTime startTime, DateTime endTime,string user_id,string form_name)
        {
            var query = from operation in con.HOPERATION
                        where operation.INSERT_TIME > startTime && operation.INSERT_TIME < endTime
                        && operation.USER_ID.Trim() == user_id.Trim()&& operation.FORM_NAME.Trim() == form_name.Trim()
                        orderby operation.INSERT_TIME descending
                        select operation;
            return query.ToList();
        }
        public List<HOPERATION> loadOperationHisByTimeUserID(DBConnection_EF con, DateTime startTime, DateTime endTime, string user_id)
        {
            var query = from operation in con.HOPERATION
                        where operation.INSERT_TIME > startTime && operation.INSERT_TIME < endTime
                        && operation.USER_ID.Trim() == user_id.Trim()
                        orderby operation.INSERT_TIME descending
                        select operation;
            return query.ToList();
        }
        public List<HOPERATION> loadOperationHisByTimeFormName(DBConnection_EF con, DateTime startTime, DateTime endTime, string form_name)
        {
            var query = from operation in con.HOPERATION
                        where operation.INSERT_TIME > startTime && operation.INSERT_TIME < endTime
                        && operation.FORM_NAME.Trim() == form_name.Trim()
                        orderby operation.INSERT_TIME descending
                        select operation;
            return query.ToList();
        }
    }
}
