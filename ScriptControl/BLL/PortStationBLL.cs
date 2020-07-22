using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.Data;
using com.mirle.ibg3k0.sc.Data.DAO;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.mirle.ibg3k0.sc.BLL
{
    public class PortStationBLL
    {
        public DB OperateDB { private set; get; }
        public Catch OperateCatch { private set; get; }
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public PortStationBLL()
        {
        }
        public void start(SCApplication _app)
        {
            OperateDB = new DB(_app.PortStationDao);
            OperateCatch = new Catch(_app.getEQObjCacheManager());
        }
        public class DB
        {
            PortStationDao portStationDao = null;
            public DB(PortStationDao _portStationDao)
            {
                portStationDao = _portStationDao;
            }
            public APORTSTATION get(string _id)
            {
                APORTSTATION rtnPortStation = null;
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    rtnPortStation = portStationDao.getByID(con, _id);
                }
                return rtnPortStation;
            }
            public bool add(APORTSTATION portStation)
            {
                try
                {
                    using (DBConnection_EF con = DBConnection_EF.GetUContext())
                    {
                        portStationDao.add(con, portStation);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Exception");
                    return false;
                }
                return true;
            }

            public bool updatePriority(string portID, int priority)
            {
                try
                {
                    APORTSTATION port_statino = new APORTSTATION();
                    port_statino.PORT_ID = portID;
                    using (DBConnection_EF con = DBConnection_EF.GetUContext())
                    {
                        con.APORTSTATION.Attach(port_statino);
                        port_statino.PRIORITY = priority;

                        con.Entry(port_statino).Property(p => p.PRIORITY).IsModified = true;

                        portStationDao.update(con, port_statino);
                        con.Entry(port_statino).State = EntityState.Detached;
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Exception");
                    return false;
                }
                return true;
            }

            public bool updateServiceStatus(string portID, ProtocolFormat.OHTMessage.PortStationServiceStatus status)
            {
                try
                {
                    APORTSTATION port_statino = new APORTSTATION();
                    port_statino.PORT_ID = portID;
                    using (DBConnection_EF con = DBConnection_EF.GetUContext())
                    {
                        con.APORTSTATION.Attach(port_statino);
                        port_statino.PORT_SERVICE_STATUS = status;

                        con.Entry(port_statino).Property(p => p.PORT_SERVICE_STATUS).IsModified = true;

                        portStationDao.update(con, port_statino);
                        con.Entry(port_statino).State = EntityState.Detached;
                    }
                }
                catch
                {
                    return false;
                }
                return true;
            }
            public bool updatePortStatus(string portID, ProtocolFormat.OHTMessage.PortStationStatus status)
            {
                try
                {
                    APORTSTATION port_statino = new APORTSTATION();
                    port_statino.PORT_ID = portID;
                    using (DBConnection_EF con = DBConnection_EF.GetUContext())
                    {
                        con.APORTSTATION.Attach(port_statino);
                        port_statino.PORT_STATUS = status;

                        con.Entry(port_statino).Property(p => p.PORT_STATUS).IsModified = true;

                        portStationDao.update(con, port_statino);
                        con.Entry(port_statino).State = EntityState.Detached;
                    }
                }
                catch
                {
                    return false;
                }
                return true;
            }
        }
        public class Catch
        {
            EQObjCacheManager CacheManager;
            public Catch(EQObjCacheManager _cache_manager)
            {
                CacheManager = _cache_manager;
            }
            public List<APORTSTATION> getAllPortStation()
            {
                return CacheManager.getALLPortStation();
            }
            public APORTSTATION getPortStation(string port_id)
            {
                APORTSTATION portTemp = CacheManager.getPortStation(port_id);
                return portTemp;
            }
            public APORTSTATION getPortStationByAdrID(string adrID)
            {
                if (SCUtility.isEmpty(adrID)) return null;
                APORTSTATION portTemp = CacheManager.getALLPortStation().Where(port_station => port_station.ADR_ID.Trim() == adrID.Trim()).
                                                                         SingleOrDefault();
                return portTemp;
            }
            public List<APORTSTATION> loadPortStationByGroupID(string groupID)
            {
                if (SCUtility.isEmpty(groupID)) return null;
                List<APORTSTATION> portsTemp = CacheManager.getALLPortStation().Where(port_station => SCUtility.isMatche(port_station.GROUP_ID, groupID)).
                                                                         ToList();
                return portsTemp;
            }

            public void updatePortStationCSTExistStatus(string port_id, string cst_id)
            {
                APORTSTATION port_station = CacheManager.getPortStation(port_id);
                if (port_station != null)
                {
                    port_station.CST_ID = cst_id;
                }
            }
            public bool IsExist(string portID)
            {
                APORTSTATION port_station = CacheManager.getPortStation(portID);
                return port_station != null;
            }


            public bool updatePriority(string portID, int priority)
            {
                try
                {
                    APORTSTATION port_station = CacheManager.getPortStation(portID);
                    port_station.PRIORITY = priority;
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Exception");
                    return false;
                }
                return true;
            }

            public bool updateServiceStatus(string portID, ProtocolFormat.OHTMessage.PortStationServiceStatus status)
            {
                try
                {
                    APORTSTATION port_station = CacheManager.getPortStation(portID);
                    port_station.PORT_SERVICE_STATUS = status;
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Exception");
                    return false;
                }
                return true;
            }
            public bool updatePortStatus(string portID, ProtocolFormat.OHTMessage.PortStationStatus status)
            {
                try
                {
                    APORTSTATION port_station = CacheManager.getPortStation(portID);
                    port_station.PORT_STATUS = status;
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Exception");
                    return false;
                }
                return true;
            }

        }
    }
}