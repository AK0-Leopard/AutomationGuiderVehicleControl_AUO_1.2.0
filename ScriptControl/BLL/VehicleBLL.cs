using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.Data;
using com.mirle.ibg3k0.sc.Data.DAO;
using com.mirle.ibg3k0.sc.Data.SECS;
using com.mirle.ibg3k0.sc.Data.ValueDefMapAction;
using com.mirle.ibg3k0.sc.Data.VO;
using com.mirle.ibg3k0.sc.ProtocolFormat.OHTMessage;
using com.mirle.iibg3k0.ttc.Common;
using NLog;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace com.mirle.ibg3k0.sc.BLL
{
    public class VehicleBLL
    {
        public Cache cache { get; private set; }


        protected VehicleDao vehicleDAO = null;
        protected SCApplication scApp = null;
        private static Logger logger = LogManager.GetCurrentClassLogger();


        public VehicleBLL()
        {

        }
        public void start(SCApplication app)
        {
            scApp = app;
            vehicleDAO = scApp.VehicleDao;
            cache = new Cache(scApp.getEQObjCacheManager());

        }
        public void startMapAction()
        {

            List<AVEHICLE> lstVH = scApp.getEQObjCacheManager().getAllVehicle();
            //foreach (AVEHICLE vh in lstVH)
            //{
            //    if (!vh_update_lock_obj_pool.ContainsKey(vh.VEHICLE_ID))
            //    {
            //        vh_update_lock_obj_pool.Add(vh.VEHICLE_ID, vh.VEHICLE_ID);
            //    }
            //}
        }
        public bool addVehicle(AVEHICLE _vh)
        {
            bool isSuccess = true;

            using (DBConnection_EF con = new DBConnection_EF())
            {
                AVEHICLE vh = new AVEHICLE
                {
                    VEHICLE_ID = _vh.VEHICLE_ID,
                    ACC_SEC_DIST = 0,
                    MODE_STATUS = 0,
                    ACT_STATUS = 0,
                    OHTC_CMD = string.Empty,
                    BLOCK_PAUSE = 0,
                    CMD_PAUSE = 0,
                    OBS_PAUSE = 0,
                    HAS_CST = 0,
                    VEHICLE_ACC_DIST = 0,
                    MANT_ACC_DIST = 0,
                    GRIP_COUNT = 0,
                    GRIP_MANT_COUNT = 0
                };
                vehicleDAO.add(con, vh);
            }
            return isSuccess;
        }
        public bool addVehicle(string vh_id)
        {
            bool isSuccess = true;

            using (DBConnection_EF con = new DBConnection_EF())
            {
                AVEHICLE vh = new AVEHICLE
                {
                    VEHICLE_ID = vh_id
                };
                vehicleDAO.add(con, vh);
            }
            return isSuccess;
        }

        //public void doUpdateVheiclePosition(AVEHICLE vh, string current_adr_id, string current_sec_id, string last_adr_id, string last_sec_id, int sec_dis, EventType vhPassEvent)
        //{
        //    //if (updateVheiclePosition(vh.VEHICLE_ID, current_adr_id, current_sec_id, sec_dis, vhPassEvent))
        //    //{
        //    //    updateVheiclePosition_CacheManager(vh, current_adr_id, current_sec_id, sec_dis, vhPassEvent);
        //    //    return true;
        //    //}
        //    //else
        //    //{
        //    //    return false;
        //    //}
        //    updateVheiclePosition_CacheManager(vh, current_adr_id, current_sec_id, sec_dis, vhPassEvent);
        //    if (!SCUtility.isMatche(current_adr_id, last_adr_id) || !SCUtility.isMatche(current_sec_id, last_sec_id))
        //        updateVheiclePosition(vh.VEHICLE_ID, current_adr_id, current_sec_id, sec_dis, vhPassEvent);

        //}
        public bool updateVheiclePosition_CacheManager(AVEHICLE vh, string adr_id, string sec_id, string seg_id, double sce_dis, DriveDirction driveDirction, double xAxis, double yAxis, double dirctionAngle, double vehicleAngle)
        {
            vh.CUR_ADR_ID = adr_id;
            vh.CUR_SEC_ID = sec_id;
            vh.CUR_SEG_ID = seg_id;
            vh.ACC_SEC_DIST = sce_dis;
            vh.CurrentDriveDirction = driveDirction;

            vh.X_Axis = xAxis;
            vh.Y_Axis = yAxis;
            vh.DirctionAngle = dirctionAngle;
            vh.VehicleAngle = vehicleAngle;
            //var showObj = scApp.getEQObjCacheManager().CommonInfo.ObjectToShow_list.
            //    Where(o => o.VEHICLE_ID == vh.VEHICLE_ID).SingleOrDefault();
            //showObj.NotifyPropertyChanged(nameof(showObj.ACC_SEC_DIST2Show));
            vh.NotifyVhPositionChange();
            return true;
        }
        public Mirle.Hlts.Utils.HltResult updateVheiclePositionToReserveControlModule(BLL.ReserveBLL reserveBLL, AVEHICLE vh, string currentSectionID, double x_axis, double y_axis, double dirctionAngle, double vehicleAngle, double speed,
                                                                                      Mirle.Hlts.Utils.HltDirection sensorDir, Mirle.Hlts.Utils.HltDirection forkDir)
        {
            string vh_id = vh.VEHICLE_ID;
            string section_id = currentSectionID;
            return reserveBLL.TryAddVehicleOrUpdate(vh_id, section_id, x_axis, y_axis, (float)vehicleAngle, speed, sensorDir, forkDir);
        }


        public void updateVehicleActionStatus(AVEHICLE vh, EventType vhPassEvent)
        {
            vh.VhRecentTranEvent = vhPassEvent;
            vh.NotifyVhStatusChange();
        }
        public void updateVehicleBCRReadResult(AVEHICLE vh, BCRReadResult bcrReadResult)
        {
            vh.BCRReadResult = bcrReadResult;
        }
        //public bool updateVheiclePosition(string vh_id, string adr_id, string sec_id, int sce_dis, EventType vhPassEvent)
        public void updateVheiclePosition(string vh_id, string adr_id, string sec_id, double sce_dis, EventType vhPassEvent)
        {
            //SCUtility.LockWithTimeout(vh_update_lock_obj_pool[vh_id], SCAppConstants.LOCK_TIMEOUT_MS, () =>
            //{
            AVEHICLE vh = scApp.VehiclPool.GetObject();
            //DBConnection_EF conn = null;
            try
            {
                //conn = DBConnection_EF.GetContext();
                //conn.BeginTransaction();
                //using (DBConnection_EF con = new DBConnection_EF())
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    //vh = vehicleDAO.getByID(con, vh_id);
                    vh.VEHICLE_ID = vh_id;
                    con.AVEHICLE.Attach(vh);
                    //con.Entry(vh).State = EntityState.Modified;
                    vh.CUR_ADR_ID = adr_id;
                    vh.CUR_SEC_ID = sec_id;
                    vh.ACC_SEC_DIST = sce_dis;
                    con.Entry(vh).Property(p => p.CUR_ADR_ID).IsModified = true;
                    con.Entry(vh).Property(p => p.CUR_SEC_ID).IsModified = true;
                    con.Entry(vh).Property(p => p.ACC_SEC_DIST).IsModified = true;
                    //vh.LAST_REPORT_EVENT = vhPassEvent;
                    //vehicleDAO.update(con, vh);
                    vehicleDAO.doUpdate(scApp, con, vh);
                    //conn.Commit();
                    con.Entry(vh).State = EntityState.Detached;
                }
                //return true;
            }
            catch (Exception ex)
            {
                //if (conn != null) { try { conn.Rollback(); } catch (Exception ex_rollback) { logger.Error(ex_rollback, "Exception"); } }
                logger.Error(ex, "Exception");
                throw new Exception($"updateVheiclePosition,vh_id:{vh_id},adr:{adr_id},sec:{sec_id}", ex);
                //return false;
            }
            finally
            {
                scApp.VehiclPool.PutObject(vh);
                //if (conn != null) { try { conn.Close(); } catch (Exception ex_close) { logger.Error(ex_close, "Exception"); } }
            }
            //});
        }

        public void updateVheicleTravelInfo(string vh_id, string node_adr)
        {
            //lock (update_lock_obj)
            //{
            //SCUtility.LockWithTimeout(vh_update_lock_obj_pool[vh_id], SCAppConstants.LOCK_TIMEOUT_MS, () =>
            //{
            AVEHICLE vh = scApp.VehiclPool.GetObject();
            string preNodeAdr = string.Empty;
            try
            {
                //DBConnection_EF con = DBConnection_EF.GetContext();
                //using (DBConnection_EF con = new DBConnection_EF())
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    //vh = vehicleDAO.getByID(con, vh_id);
                    vh.VEHICLE_ID = vh_id;
                    con.AVEHICLE.Attach(vh);
                    preNodeAdr = vh.NODE_ADR;
                    //ASECTION sce = scApp.SectionDao.getByFromToAdr(con, preNodeAdr, node_adr);
                    ASECTION sce = scApp.SectionDao.getByFromToAdr(scApp.getCommObjCacheManager(), preNodeAdr, node_adr);
                    if (sce != null)
                    {
                        double secstion_dist = 0;
                        secstion_dist = sce.SEC_DIS;
                        vh.VEHICLE_ACC_DIST += (int)secstion_dist;
                        vh.MANT_ACC_DIST += (int)secstion_dist;
                        con.Entry(vh).Property(p => p.VEHICLE_ACC_DIST).IsModified = true;
                        con.Entry(vh).Property(p => p.MANT_ACC_DIST).IsModified = true;
                    }
                    vh.NODE_ADR = node_adr;
                    con.Entry(vh).Property(p => p.NODE_ADR).IsModified = true;
                    //bool isDetached = con.Entry(vh).State == EntityState.Modified;
                    //if (isDetached)
                    //vehicleDAO.update(con, vh);
                    vehicleDAO.doUpdate(scApp, con, vh);
                    con.Entry(vh).State = EntityState.Detached;
                }
                //return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception:");
                //return false;
            }
            finally
            {
                scApp.VehiclPool.PutObject(vh);
            }
            //});
            //}
        }
        //public bool doUpdateVehicleStatus(AVEHICLE vh,
        //                          VHModeStatus mode_status, VHActionStatus act_status,
        //                          VhStopSingle block_pause, VhStopSingle cmd_pause, VhStopSingle obs_pause,
        //                          int has_cst)
        //{
        //    if (updateVehicleStatus(vh.VEHICLE_ID,
        //                             mode_status, act_status,
        //                             block_pause, cmd_pause, obs_pause,
        //                             has_cst))
        //    {
        //        //updateVehicleStatus_CacheMangerExceptAct(vh,
        //        //                       mode_status,
        //        //                       block_pause, cmd_pause, obs_pause,
        //        //                       has_cst, cst_id);
        //        vh.NotifyVhStatusChange();
        //        return true;
        //    }
        //    return false;
        //}
        public bool doUpdateVehicleStatus(AVEHICLE vh,
                                string cstID, VHModeStatus mode_status, VHActionStatus act_status,
                                 VhStopSingle block_pause, VhStopSingle cmd_pause, VhStopSingle obs_pause, VhStopSingle hid_pause, VhStopSingle error_status, VhLoadCSTStatus load_cst_status,
                                 uint batteryCapacity)
        {
            if (updateVehicleStatus(vh.VEHICLE_ID,
                                   cstID, mode_status, act_status,
                                     block_pause, cmd_pause, obs_pause, hid_pause, error_status, load_cst_status,
                                     batteryCapacity))
            {
                //updateVehicleStatus_CacheMangerExceptAct(vh,
                //                       mode_status,
                //                       block_pause, cmd_pause, obs_pause,
                //                       has_cst, cst_id);
                vh.NotifyVhStatusChange();
                return true;
            }
            return false;
        }
        private bool updateVehicleStatus(string vh_id,
                          string cstID, VHModeStatus mode_status, VHActionStatus act_status,
                          VhStopSingle block_pause, VhStopSingle cmd_pause, VhStopSingle obs_pause, VhStopSingle hid_pause, VhStopSingle error_status, VhLoadCSTStatus load_cst_status,
                          uint batteryCapacity)
        {
            bool isSuccess = false;
            AVEHICLE vh = scApp.VehiclPool.GetObject();
            try
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    vh.VEHICLE_ID = vh_id;
                    con.AVEHICLE.Attach(vh);
                    vh.CST_ID = cstID;
                    vh.MODE_STATUS = mode_status;
                    vh.ACT_STATUS = act_status;
                    vh.BLOCK_PAUSE = block_pause;
                    vh.CMD_PAUSE = cmd_pause;
                    vh.OBS_PAUSE = obs_pause;
                    vh.HID_PAUSE = hid_pause;
                    vh.ERROR = error_status;
                    vh.HAS_CST = (int)load_cst_status;
                    vh.BATTERYCAPACITY = (int)batteryCapacity;
                    con.Entry(vh).Property(p => p.CST_ID).IsModified = true;
                    con.Entry(vh).Property(p => p.MODE_STATUS).IsModified = true;
                    con.Entry(vh).Property(p => p.ACT_STATUS).IsModified = true;
                    con.Entry(vh).Property(p => p.BLOCK_PAUSE).IsModified = true;
                    con.Entry(vh).Property(p => p.CMD_PAUSE).IsModified = true;
                    con.Entry(vh).Property(p => p.OBS_PAUSE).IsModified = true;
                    con.Entry(vh).Property(p => p.HID_PAUSE).IsModified = true;
                    con.Entry(vh).Property(p => p.ERROR).IsModified = true;
                    con.Entry(vh).Property(p => p.HAS_CST).IsModified = true;
                    con.Entry(vh).Property(p => p.BATTERYCAPACITY).IsModified = true;
                    vehicleDAO.doUpdate(scApp, con, vh);
                    con.Entry(vh).State = EntityState.Detached;
                    isSuccess = true;
                }
            }
            finally
            {
                scApp.VehiclPool.PutObject(vh);
            }
            return isSuccess;
        }

        public bool updataVehicleMode(string vh_id, VHModeStatus mode_status)
        {
            bool isSuccess = false;
            AVEHICLE vh = new AVEHICLE();
            try
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    //vh = con.AVEHICLE.Find(vh_id);
                    //if (vh == null)
                    //vh = scApp.VehiclPool.GetObject();
                    vh.VEHICLE_ID = vh_id;

                    con.AVEHICLE.Attach(vh);
                    vh.MODE_STATUS = mode_status;
                    con.Entry(vh).Property(p => p.MODE_STATUS).IsModified = true;

                    vehicleDAO.doUpdate(scApp, con, vh);
                    con.Entry(vh).State = EntityState.Detached;
                    isSuccess = true;
                }
            }
            finally
            {
                //scApp.VehiclPool.PutObject(vh);
            }
            return isSuccess;
        }

        public bool updateVehiclePauseStatus(string vh_id, bool? earthquake_pause = null, bool? safyte_pause = null, bool? obstruct_pause = null)
        {
            bool isSuccess = false;
            AVEHICLE vh = scApp.VehiclPool.GetObject();
            try
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    vh.VEHICLE_ID = vh_id;
                    con.AVEHICLE.Attach(vh);

                    if (earthquake_pause.HasValue)
                    {
                        vh.EARTHQUAKE_PAUSE = earthquake_pause.Value ? VhStopSingle.StopSingleOn : VhStopSingle.StopSingleOff;
                        con.Entry(vh).Property(p => p.EARTHQUAKE_PAUSE).IsModified = true;
                    }
                    if (safyte_pause.HasValue)
                    {
                        vh.SAFETY_DOOR_PAUSE = safyte_pause.Value ? VhStopSingle.StopSingleOn : VhStopSingle.StopSingleOff;
                        con.Entry(vh).Property(p => p.SAFETY_DOOR_PAUSE).IsModified = true;
                    }
                    if (obstruct_pause.HasValue)
                    {
                        vh.OHXC_BLOCK_PAUSE = obstruct_pause.Value ? VhStopSingle.StopSingleOn : VhStopSingle.StopSingleOff;
                        con.Entry(vh).Property(p => p.OHXC_BLOCK_PAUSE).IsModified = true;
                    }
                    if (obstruct_pause.HasValue)
                    {
                        vh.OHXC_BLOCK_PAUSE = obstruct_pause.Value ? VhStopSingle.StopSingleOn : VhStopSingle.StopSingleOff;
                        con.Entry(vh).Property(p => p.OHXC_BLOCK_PAUSE).IsModified = true;
                    }

                    vehicleDAO.doUpdate(scApp, con, vh);
                    con.Entry(vh).State = EntityState.Detached;
                    isSuccess = true;
                }
            }
            finally
            {
                scApp.VehiclPool.PutObject(vh);
            }
            return isSuccess;
        }

        public bool updateVehicleExcuteCMD(string vh_id, string cmd_id, string mcs_cmd_id)
        {
            bool isSuccess = false;
            //DBConnection_EF con = DBConnection_EF.GetContext();
            //SCUtility.LockWithTimeout(vh_update_lock_obj_pool[vh_id], SCAppConstants.LOCK_TIMEOUT_MS, () =>
            //{
            //AVEHICLE vh = scApp.VehiclPool.GetObject();
            AVEHICLE vh = new AVEHICLE();
            try
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    //vh = vehicleDAO.getByID(con, vh_id);
                    vh.VEHICLE_ID = vh_id;
                    con.AVEHICLE.Attach(vh);
                    vh.OHTC_CMD = cmd_id;
                    vh.MCS_CMD = mcs_cmd_id;
                    //vehicleDAO.update(con, vh);
                    con.Entry(vh).Property(p => p.OHTC_CMD).IsModified = true;
                    con.Entry(vh).Property(p => p.MCS_CMD).IsModified = true;
                    vehicleDAO.doUpdate(scApp, con, vh);
                    con.Entry(vh).State = EntityState.Detached;
                    isSuccess = true;
                }
            }
            finally
            {
                //scApp.VehiclPool.PutObject(vh);
            }
            //});
            return isSuccess;
        }

        public bool updataVehicleCSTID(string vh_id, string cst_id)
        {
            bool isSuccess = false;
            AVEHICLE vh = scApp.VehiclPool.GetObject();
            try
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    vh.VEHICLE_ID = vh_id;
                    con.AVEHICLE.Attach(vh);
                    vh.CST_ID = SCUtility.Trim(cst_id);
                    con.Entry(vh).Property(p => p.CST_ID).IsModified = true;
                    vehicleDAO.doUpdate(scApp, con, vh);
                    con.Entry(vh).State = EntityState.Detached;
                    isSuccess = true;
                }
            }
            finally
            {
                scApp.VehiclPool.PutObject(vh);
            }
            return isSuccess;
        }
        public bool updataVehicleLastFullyChargerTime(string vh_id)
        {
            bool isSuccess = false;
            //AVEHICLE vh = scApp.VehiclPool.GetObject();
            AVEHICLE vh = new AVEHICLE();
            try
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    vh.VEHICLE_ID = vh_id;
                    con.AVEHICLE.Attach(vh);
                    vh.LAST_FULLY_CHARGED_TIME = DateTime.Now;
                    con.Entry(vh).Property(p => p.LAST_FULLY_CHARGED_TIME).IsModified = true;
                    vehicleDAO.doUpdate(scApp, con, vh);
                    con.Entry(vh).State = EntityState.Detached;
                    isSuccess = true;
                }
            }
            finally
            {
                //scApp.VehiclPool.PutObject(vh);
            }
            return isSuccess;
        }



        public VHModeStatus DecideVhModeStatus(string vh_id, VHModeStatus vh_current_mode_status, uint batteryCapacity)
        {
            AVEHICLE eqpt = scApp.VehicleBLL.getVehicleByID(vh_id);
            VHModeStatus modeStat = default(VHModeStatus);
            if (vh_current_mode_status == VHModeStatus.AutoRemote)
            {
                if (eqpt.MODE_STATUS == VHModeStatus.AutoLocal ||
                    eqpt.MODE_STATUS == VHModeStatus.AutoCharging)
                {
                    modeStat = eqpt.MODE_STATUS;
                }
                else if (batteryCapacity < AVEHICLE.BATTERYLEVELVALUE_LOW)
                {
                    modeStat = VHModeStatus.AutoCharging;
                }
                else
                {
                    modeStat = vh_current_mode_status;
                }
            }
            else
            {
                modeStat = vh_current_mode_status;
            }
            return modeStat;
        }
        //object update_lock_obj = new object();
        //Dictionary<string, string> vh_update_lock_obj_pool = new Dictionary<string, string>();
        public bool updataVehicleInstall(string vh_id)
        {
            bool isSuccess = false;
            AVEHICLE vh = scApp.VehiclPool.GetObject();
            try
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    vh.VEHICLE_ID = vh_id;
                    con.AVEHICLE.Attach(vh);
                    vh.IS_INSTALLED = true;
                    vh.INSTALLED_TIME = DateTime.Now;
                    con.Entry(vh).Property(p => p.IS_INSTALLED).IsModified = true;
                    con.Entry(vh).Property(p => p.INSTALLED_TIME).IsModified = true;
                    vehicleDAO.doUpdate(scApp, con, vh);
                    con.Entry(vh).State = EntityState.Detached;
                    isSuccess = true;
                }
            }
            finally
            {
                scApp.VehiclPool.PutObject(vh);
            }
            return isSuccess;
        }
        public bool updataVehicleRemove(string vh_id)
        {
            bool isSuccess = false;
            AVEHICLE vh = scApp.VehiclPool.GetObject();
            try
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    vh.VEHICLE_ID = vh_id;
                    con.AVEHICLE.Attach(vh);
                    vh.IS_INSTALLED = false;
                    vh.REMOVED_TIME = DateTime.Now;
                    con.Entry(vh).Property(p => p.IS_INSTALLED).IsModified = true;
                    con.Entry(vh).Property(p => p.REMOVED_TIME).IsModified = true;
                    vehicleDAO.doUpdate(scApp, con, vh);
                    con.Entry(vh).State = EntityState.Detached;
                    isSuccess = true;
                }
            }
            finally
            {
                scApp.VehiclPool.PutObject(vh);
            }
            return isSuccess;
        }


        public bool setVhIsParkingOnWay(string vh_id, string adr_id)
        {
            bool isSuccess = false;
            //lock (update_lock_obj)
            //{
            //SCUtility.LockWithTimeout(vh_update_lock_obj_pool[vh_id], SCAppConstants.LOCK_TIMEOUT_MS, () =>
            //{
            //DBConnection_EF con = DBConnection_EF.GetContext();
            //using (DBConnection_EF con = new DBConnection_EF())
            //vh = vehicleDAO.getByID(con, vh_id);
            AVEHICLE vh = scApp.VehiclPool.GetObject();
            try
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    vh.VEHICLE_ID = vh_id;
                    con.AVEHICLE.Attach(vh);
                    vh.PARK_ADR_ID = adr_id;
                    vh.PARK_TIME = null;
                    vh.IS_PARKING = false;

                    vh.CYCLERUN_ID = string.Empty;
                    vh.IS_CYCLING = false;
                    vh.CYCLERUN_TIME = null;

                    con.Entry(vh).Property(p => p.PARK_ADR_ID).IsModified = true;
                    con.Entry(vh).Property(p => p.PARK_TIME).IsModified = true;
                    con.Entry(vh).Property(p => p.IS_PARKING).IsModified = true;
                    con.Entry(vh).Property(p => p.CYCLERUN_ID).IsModified = true;
                    con.Entry(vh).Property(p => p.IS_CYCLING).IsModified = true;
                    con.Entry(vh).Property(p => p.CYCLERUN_TIME).IsModified = true;

                    //vehicleDAO.update(con, vh);
                    vehicleDAO.doUpdate(scApp, con, vh);
                    con.Entry(vh).State = EntityState.Detached;
                    isSuccess = true;
                }
            }
            finally
            {
                scApp.VehiclPool.PutObject(vh);
            }
            //});
            //}
            return isSuccess;
        }
        public bool setVhIsInPark(string vh_id, string park_adr)
        {
            bool isSuccess = false;
            //lock (update_lock_obj)
            //{
            //SCUtility.LockWithTimeout(vh_update_lock_obj_pool[vh_id], SCAppConstants.LOCK_TIMEOUT_MS, () =>
            //{
            AVEHICLE vh = scApp.VehiclPool.GetObject();
            //DBConnection_EF con = DBConnection_EF.GetContext();
            //using (DBConnection_EF con = new DBConnection_EF())
            try
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    //vh = vehicleDAO.getByID(con, vh_id);
                    vh.VEHICLE_ID = vh_id;
                    con.AVEHICLE.Attach(vh);
                    vh.IS_PARKING = true;
                    vh.PARK_TIME = DateTime.Now;
                    vh.PARK_ADR_ID = park_adr;

                    vh.CYCLERUN_ID = string.Empty;
                    vh.IS_CYCLING = false;
                    vh.CYCLERUN_TIME = null;

                    con.Entry(vh).Property(p => p.IS_PARKING).IsModified = true;
                    con.Entry(vh).Property(p => p.PARK_TIME).IsModified = true;
                    con.Entry(vh).Property(p => p.PARK_ADR_ID).IsModified = true;
                    con.Entry(vh).Property(p => p.CYCLERUN_ID).IsModified = true;
                    con.Entry(vh).Property(p => p.IS_CYCLING).IsModified = true;
                    con.Entry(vh).Property(p => p.CYCLERUN_TIME).IsModified = true;

                    //vehicleDAO.update(con, vh);
                    vehicleDAO.doUpdate(scApp, con, vh);
                    con.Entry(vh).State = EntityState.Detached;
                    isSuccess = true;
                }
            }
            finally
            {
                scApp.VehiclPool.PutObject(vh);
            }
            //});
            //}
            return isSuccess;
        }
        public bool resetVhIsInPark(string vh_id)
        {
            bool isSuccess = false;
            //lock (update_lock_obj)
            //{
            //SCUtility.LockWithTimeout(vh_update_lock_obj_pool[vh_id], SCAppConstants.LOCK_TIMEOUT_MS, () =>
            //{
            AVEHICLE vh = scApp.VehiclPool.GetObject();
            //DBConnection_EF con = DBConnection_EF.GetContext();
            //using (DBConnection_EF con = new DBConnection_EF())
            try
            {
                using (DBConnection_EF con = DBConnection_EF.GetContext())
                {
                    //vh = vehicleDAO.getByID(con, vh_id);
                    vh.VEHICLE_ID = vh_id;
                    con.AVEHICLE.Attach(vh);
                    //將不再有PARK_ADR_ID 在VH TABLE
                    vh.PARK_ADR_ID = string.Empty;
                    vh.PARK_TIME = null;
                    vh.IS_PARKING = false;
                    //vehicleDAO.update(con, vh);
                    con.Entry(vh).Property(p => p.IS_PARKING).IsModified = true;
                    con.Entry(vh).Property(p => p.PARK_TIME).IsModified = true;
                    con.Entry(vh).Property(p => p.PARK_ADR_ID).IsModified = true;

                    vehicleDAO.doUpdate(scApp, con, vh);
                    con.Entry(vh).State = EntityState.Detached;
                    isSuccess = true;
                }
            }
            finally
            {
                scApp.VehiclPool.PutObject(vh);
            }
            //});
            return isSuccess;
            //}
        }
        public void setVhReserveSuccessOfSegment(string vhID, Google.Protobuf.Collections.RepeatedField<ReserveInfo> reserveInfos)
        {
            AVEHICLE vh = scApp.VehicleBLL.cache.getVehicle(vhID);
            var reserve_success_segment = reserveInfos.
                                         Select(info => info.ReserveSectionID).
                                         ToList();
            vh.CurrentReserveSegmentID = reserve_success_segment;
            vh.NotifyVhExcuteCMDStatusChange();
        }
        public void setVhReserveSuccessOfSegment(string vhID, List<string> reserveInfos)
        {
            AVEHICLE vh = scApp.VehicleBLL.cache.getVehicle(vhID);
            vh.CurrentReserveSegmentID = reserveInfos;
            vh.NotifyVhExcuteCMDStatusChange();
        }

        public void setVhReserveUnSuccessOfSegment(string vhID, Google.Protobuf.Collections.RepeatedField<ReserveInfo> reserveInfos)
        {
            AVEHICLE vh = scApp.VehicleBLL.cache.getVehicle(vhID);
            var reserve_unsuccess_segment = reserveInfos.
                                         Select(info => info.ReserveSectionID).
                                         ToList();
            vh.CurrentReserveSegmentID = vh.CurrentReserveSegmentID.
                                            Intersect(reserve_unsuccess_segment).
                                            ToList();
            vh.NotifyVhExcuteCMDStatusChange();
        }

        public bool setVhIsCycleRunOnWay(string vh_id, string entry_adr)
        {
            //lock (update_lock_obj)
            //{
            bool isSuccess = false;
            //SCUtility.LockWithTimeout(vh_update_lock_obj_pool[vh_id], SCAppConstants.LOCK_TIMEOUT_MS, () =>
            //{
            AVEHICLE vh = scApp.VehiclPool.GetObject();
            //using (DBConnection_EF con = new DBConnection_EF())
            try
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    //vh = vehicleDAO.getByID(con, vh_id);
                    vh.VEHICLE_ID = vh_id;
                    con.AVEHICLE.Attach(vh);
                    ACYCLEZONEMASTER masterTemp = scApp.CycleBLL.getCycleZoneMaterByEntryAdr(entry_adr);
                    if (masterTemp != null)
                    {
                        vh.CYCLERUN_ID = masterTemp.CYCLE_ZONE_ID;
                    }
                    else
                    {
                        throw new Exception(string.Format("Cycle Run master not exist,entry adr id:{0}"
                                                         , entry_adr));
                    }
                    vh.IS_CYCLING = false;
                    vh.CYCLERUN_TIME = null;
                    vh.PARK_ADR_ID = string.Empty;
                    vh.PARK_TIME = null;
                    vh.IS_PARKING = false;
                    //vehicleDAO.update(con, vh);
                    con.Entry(vh).Property(p => p.IS_CYCLING).IsModified = true;
                    con.Entry(vh).Property(p => p.CYCLERUN_TIME).IsModified = true;
                    con.Entry(vh).Property(p => p.PARK_ADR_ID).IsModified = true;
                    con.Entry(vh).Property(p => p.PARK_TIME).IsModified = true;
                    con.Entry(vh).Property(p => p.IS_PARKING).IsModified = true;

                    vehicleDAO.doUpdate(scApp, con, vh);
                    con.Entry(vh).State = EntityState.Detached;
                    isSuccess = true;
                }
            }
            finally
            {
                scApp.VehiclPool.PutObject(vh);
            }
            //});
            return isSuccess;
            //}
        }
        public bool setVhIsInCycleRun(string vh_id)
        {
            //lock (update_lock_obj)
            //{
            bool isSuccess = false;
            //SCUtility.LockWithTimeout(vh_update_lock_obj_pool[vh_id], SCAppConstants.LOCK_TIMEOUT_MS, () =>
            //{
            AVEHICLE vh = scApp.VehiclPool.GetObject();
            //DBConnection_EF con = DBConnection_EF.GetContext();
            //using (DBConnection_EF con = new DBConnection_EF())
            try
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    //vh = vehicleDAO.getByID(con, vh_id);
                    vh.VEHICLE_ID = vh_id;
                    con.AVEHICLE.Attach(vh);
                    vh.IS_CYCLING = true;
                    vh.CYCLERUN_TIME = DateTime.Now;
                    //將不再有PARK_ADR_ID
                    vh.PARK_ADR_ID = string.Empty;
                    vh.PARK_TIME = null;
                    vh.IS_PARKING = false;
                    //vehicleDAO.update(con, vh);
                    con.Entry(vh).Property(p => p.IS_CYCLING).IsModified = true;
                    con.Entry(vh).Property(p => p.CYCLERUN_TIME).IsModified = true;
                    con.Entry(vh).Property(p => p.PARK_ADR_ID).IsModified = true;
                    con.Entry(vh).Property(p => p.PARK_TIME).IsModified = true;
                    con.Entry(vh).Property(p => p.IS_PARKING).IsModified = true;


                    vehicleDAO.doUpdate(scApp, con, vh);
                    con.Entry(vh).State = EntityState.Detached;
                    isSuccess = true;
                }
            }
            finally
            {
                scApp.VehiclPool.PutObject(vh);
            }
            //resetVhIsInPark(vh_id);
            //});
            return isSuccess;
            //}
        }
        public bool resetVhIsCycleRun(string vh_id)
        {
            //lock (update_lock_obj)
            //{

            bool isSuccess = false;
            //SCUtility.LockWithTimeout(vh_update_lock_obj_pool[vh_id], SCAppConstants.LOCK_TIMEOUT_MS, () =>
            //{
            AVEHICLE vh = scApp.VehiclPool.GetObject();
            //using (DBConnection_EF con = new DBConnection_EF())
            try
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    //vh = vehicleDAO.getByID(con, vh_id);
                    vh.VEHICLE_ID = vh_id;
                    con.AVEHICLE.Attach(vh);
                    vh.CYCLERUN_ID = string.Empty;
                    vh.IS_CYCLING = false;
                    vh.CYCLERUN_TIME = null;
                    //vehicleDAO.update(con, vh);
                    con.Entry(vh).Property(p => p.CYCLERUN_ID).IsModified = true;
                    con.Entry(vh).Property(p => p.IS_CYCLING).IsModified = true;
                    con.Entry(vh).Property(p => p.CYCLERUN_TIME).IsModified = true;


                    vehicleDAO.doUpdate(scApp, con, vh);
                    con.Entry(vh).State = EntityState.Detached;
                    isSuccess = true;
                }
            }
            finally
            {
                scApp.VehiclPool.PutObject(vh);
            }
            //});
            return isSuccess;
            //}
        }

        public AVEHICLE getVehicleByIDFromDB(string vh_id, bool isAttached = false)
        {
            AVEHICLE vh = null;
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                vh = vehicleDAO.getByID(con, vh_id);
                if (vh != null && !isAttached)
                {
                    con.Entry(vh).State = EntityState.Detached;
                }
            }
            return vh;
        }

        public AVEHICLE getVehicleByID(string vh_id)
        {
            AVEHICLE vh = null;
            //DBConnection_EF con = DBConnection_EF.GetContext();
            //using (DBConnection_EF con = new DBConnection_EF())
            //using (DBConnection_EF con = DBConnection_EF.GetUContext())
            //{
            //    vh = vehicleDAO.getByID(con, vh_id);
            //    if (!isAttached)
            //    {
            //        con.Entry(vh).State = EntityState.Detached;
            //    }
            //}
            vh = vehicleDAO.getByID(vh_id);

            return vh;
        }

        public AVEHICLE getVehicleByExcuteMCS_CMD_ID(string mcs_cmd_id)
        {
            AVEHICLE vh = null;
            vh = vehicleDAO.getByMCS_CMD_ID(mcs_cmd_id);

            return vh;
        }

        public AVEHICLE getVehicleByCarrierID(string carrier_id)
        {
            AVEHICLE vh = null;
            vh = vehicleDAO.getByCarrierID(carrier_id);

            return vh;
        }


        public AVEHICLE findBestSuitableVhStepByStepFromAdr_New(string source, E_VH_TYPE vh_type, bool isCheckHasVhCarry = false)
        {
            AVEHICLE best_vh = null;

            //List<AVEHICLE> vhs = cache.loadAllVh();
            List<AVEHICLE> vhs = cache.loadAllVh().ToList();
            //1.過濾掉狀態不符的
            filterVh(ref vhs, vh_type);
            //2.尋找距離Source最近的車子
            int minimum_cost = int.MaxValue;
            foreach (var vh in vhs)
            {
                var result = scApp.GuideBLL.getGuideInfo(vh.CUR_ADR_ID, source);
                if (result.totalCost < minimum_cost)
                {
                    best_vh = vh;
                    minimum_cost = result.totalCost;
                }
            }
            return best_vh;
        }

        public virtual void filterVh(ref List<AVEHICLE> vhs, E_VH_TYPE vh_type,bool checkCst=true)
        {
            if (vh_type != E_VH_TYPE.None)
            {
                foreach (AVEHICLE vh in vhs.ToList())
                {
                    if (vh.VEHICLE_TYPE != E_VH_TYPE.None
                        && vh.VEHICLE_TYPE != vh_type)
                    {
                        vhs.Remove(vh);
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleBLL), Device: "OHxC",
                           Data: $"vh id:{vh.VEHICLE_ID} vh type:{vh.VEHICLE_TYPE}, vehicle type not match current find vh type:{vh_type}," +
                                 $"so filter it out",
                           VehicleID: vh.VEHICLE_ID,
                           CarrierID: vh.CST_ID);
                    }
                }
            }

            foreach (AVEHICLE vh in vhs.ToList())
            {
                if (!vh.isTcpIpConnect)
                {
                    vhs.Remove(vh);
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleBLL), Device: "OHxC",
                       Data: $"vh id:{vh.VEHICLE_ID} of tcp ip connection is :{vh.isTcpIpConnect}" +
                             $"so filter it out",
                       VehicleID: vh.VEHICLE_ID,
                       CarrierID: vh.CST_ID);
                }
            }

            foreach (AVEHICLE vh in vhs.ToList())
            {
                if (vh.IsError)
                {
                    vhs.Remove(vh);
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleBLL), Device: "OHxC",
                       Data: $"vh id:{vh.VEHICLE_ID} of error flag is :{vh.IsError}" +
                             $"so filter it out",
                       VehicleID: vh.VEHICLE_ID,
                       CarrierID: vh.CST_ID);
                }
            }
            foreach (AVEHICLE vh in vhs.ToList())
            {
                if (vh.BatteryLevel == BatteryLevel.Low)
                {
                    vhs.Remove(vh);
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleBLL), Device: "OHxC",
                       Data: $"vh id:{vh.VEHICLE_ID} of BatteryLevel:{vh.BatteryLevel} , BatteryCapacity:{vh.BatteryCapacity}," +
                             $"so filter it out",
                       VehicleID: vh.VEHICLE_ID,
                       CarrierID: vh.CST_ID);
                }
            }
            foreach (AVEHICLE vh in vhs.ToList())
            {
                //if (!SCUtility.isEmpty(vh.OHTC_CMD))
                if (!SCUtility.isEmpty(vh.MCS_CMD))
                {
                    vhs.Remove(vh);
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleBLL), Device: "OHxC",
                       Data: $"vh id:{vh.VEHICLE_ID} has excute mcs command:{vh.MCS_CMD.Trim()}," +
                             $"so filter it out",
                       VehicleID: vh.VEHICLE_ID,
                       CarrierID: vh.CST_ID);
                }
            }
            if (checkCst)
            {
                foreach (AVEHICLE vh in vhs.ToList())
                {
                    if (vh.HAS_CST == 1)
                    {
                        vhs.Remove(vh);
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleBLL), Device: "OHxC",
                           Data: $"vh id:{vh.VEHICLE_ID} has carry cst,carrier id:{SCUtility.Trim(vh.CST_ID, true)}," +
                                 $"so filter it out",
                           VehicleID: vh.VEHICLE_ID,
                           CarrierID: vh.CST_ID);
                    }
                }
            }

            foreach (AVEHICLE vh in vhs.ToList())
            {
                if (vh.MODE_STATUS != VHModeStatus.AutoRemote)
                {
                    vhs.Remove(vh);
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleBLL), Device: "OHxC",
                       Data: $"vh id:{vh.VEHICLE_ID} current mode status is {vh.MODE_STATUS}," +
                             $"so filter it out",
                       VehicleID: vh.VEHICLE_ID,
                       CarrierID: vh.CST_ID);
                }
            }
            foreach (AVEHICLE vh in vhs.ToList())
            {
                if (SCUtility.isEmpty(vh.CUR_ADR_ID))
                {
                    vhs.Remove(vh);
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleBLL), Device: "OHxC",
                       Data: $"vh id:{vh.VEHICLE_ID} current address is empty," +
                             $"so filter it out",
                       VehicleID: vh.VEHICLE_ID,
                       CarrierID: vh.CST_ID);
                }
            }
            foreach (AVEHICLE vh in vhs.ToList())
            {
                if (scApp.CMDBLL.isCMD_OHTCQueueByVh(vh.VEHICLE_ID))
                {
                    vhs.Remove(vh);
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleBLL), Device: "OHxC",
                       Data: $"vh id:{vh.VEHICLE_ID} has ohxc command in queue," +
                             $"so filter it out",
                       VehicleID: vh.VEHICLE_ID,
                       CarrierID: vh.CST_ID);
                }
            }
        }

        private List<AVEHICLE> ListVhByBeginAdr(ref List<string> to_adrs, ref HashSet<string> searchedSection, int eachSearchCount)
        {
            List<AVEHICLE> vhs = new List<AVEHICLE>();

            do
            {

                List<ASECTION> secs = scApp.MapBLL.loadSectionByToAdrs(to_adrs);
                //foreach (string adr in to_adrs)
                //{
                //    AADDRESS adr_obj = scApp.AddressesBLL.cache.GetAddress(adr);
                //    if (!adr_obj.IsSegment)
                //    {
                //        ASECTION sec_ojb = scApp.SectionBLL.cache.GetSection(adr_obj.SEC_ID);
                //        secs.Add(sec_ojb);
                //    }
                //}

                bool hasNotSearchedSec = false;
                to_adrs.Clear();
                foreach (ASECTION sec in secs)
                {
                    //if (!scApp.MapBLL.IsSegmentActive(sec.SEG_NUM))
                    //    continue;

                    if (!searchedSection.Contains(sec.SEC_ID.Trim()))
                    {
                        hasNotSearchedSec = true;
                        searchedSection.Add(sec.SEC_ID.Trim());
                        List<AVEHICLE> vhs_onSec = loadVehicleOnAutoRemoteBySEC_ID(sec.SEC_ID);
                        if (hasErrorVh(vhs_onSec))
                        {
                            if (secs.Last() == sec)
                            {
                                throw new BlockedByTheErrorVehicleException("Can't find the way to transfer.");
                            }
                            continue;
                        }

                        vhs.AddRange(loadVehicleOnAutoRemoteBySEC_ID(sec.SEC_ID));
                        to_adrs.Add(sec.REAL_FROM_ADR_ID);
                    }
                }
                if (!hasNotSearchedSec)
                    break;
                //if (adrs.Contains(source))
                //    break;
            } while (vhs.Count < eachSearchCount);
            return vhs;
        }

        public class BlockedByTheErrorVehicleException : Exception
        {
            public BlockedByTheErrorVehicleException(string msg) : base(msg)
            {

            }
        }

        private bool hasErrorVh(List<AVEHICLE> vhs)
        {
            return vhs.Where(vh => vh.IsError).Count() > 0;
        }

        public int getActVhCount()
        {
            int count = 0;
            //using (DBConnection_EF con = new DBConnection_EF())
            //{
            //    count = vehicleDAO.getActVhCount(con);
            //}
            count = vehicleDAO.getActVhCount();
            return count;
        }
        public int getActVhCount(E_VH_TYPE vh_type)
        {
            int count = 0;
            count = vehicleDAO.getActVhCount(vh_type);
            return count;
        }
        public int getIdleVhCount()
        {
            int count = 0;
            //using (DBConnection_EF con = new DBConnection_EF())
            //{
            //    count = vehicleDAO.getIdleVhCount(con);
            //}
            count = vehicleDAO.getIdleVhCount();

            return count;
        }
        public int getIdleVhCount(E_VH_TYPE vh_type)
        {
            int count = 0;
            //using (DBConnection_EF con = new DBConnection_EF())
            //{
            //    count = vehicleDAO.getIdleVhCount(con);
            //}
            count = vehicleDAO.getIdleVhCount(vh_type);

            return count;
        }

        public int getNoExcuteMcsCmdVhCount(E_VH_TYPE vh_type)
        {
            int count = 0;
            //using (DBConnection_EF con = new DBConnection_EF())
            //{
            //    count = vehicleDAO.getIdleVhCount(con);
            //}
            count = vehicleDAO.getNoExcuteMcsCmdVhCount(vh_type);

            return count;
        }

        public int getParkingVhCount()
        {
            int count = 0;
            //using (DBConnection_EF con = new DBConnection_EF())
            //{
            //    count = vehicleDAO.getParkingVhCount(con);
            //}
            count = vehicleDAO.getParkingVhCount();
            return count;
        }
        public int getCyclingVhCount()
        {
            int count = 0;
            //using (DBConnection_EF con = new DBConnection_EF())
            //{
            //    count = vehicleDAO.getCyclingVhCount(con);
            //}
            count = vehicleDAO.getCyclingVhCount();
            return count;
        }



        public bool hasVehicleOnSections(List<string> sections)
        {
            bool has_vh = false;
            List<AVEHICLE> vhs = scApp.getEQObjCacheManager().getAllVehicle();
            var query = from vh in vhs
                        where sections.Contains(vh.CUR_SEC_ID)
                        select vh;
            if (query != null && query.Count() > 0)
            {
                has_vh = true;
            }
            return has_vh;
        }

        public List<AVEHICLE> loadAllVehicle()
        {
            List<AVEHICLE> vhs = null;
            using (DBConnection_EF con = new DBConnection_EF())
            {
                vhs = vehicleDAO.loadAll(con);
            }
            return vhs;
        }
        public List<AVEHICLE> loadVehicleBySEC_ID(string sec_id)
        {
            List<AVEHICLE> vhs = null;
            vhs = vehicleDAO.loadBySEC_ID(sec_id);
            return vhs;
        }
        public List<AVEHICLE> loadVehicleOnAutoRemoteBySEC_ID(string sec_id)
        {
            List<AVEHICLE> vhs = null;
            vhs = vehicleDAO.loadOnAutoRemoteBySEC_ID(sec_id);
            return vhs;
        }

        public List<AVEHICLE> loadParkingVehicle()
        {
            List<AVEHICLE> vhs = null;
            //using (DBConnection_EF con = new DBConnection_EF())
            //{
            //    vhs = vehicleDAO.loadParkingVehicle(con);
            //}
            vhs = vehicleDAO.loadParkingVehicle();
            return vhs;
        }
        public List<AVEHICLE> loadByCycleZoneID(string cyc_zone_id)
        {
            List<AVEHICLE> vhs = null;
            //using (DBConnection_EF con = new DBConnection_EF())
            //{
            //    vhs = vehicleDAO.loadVhByCycleZoneID(con, cyc_zone_id);
            //}
            vhs = vehicleDAO.loadVhByCycleZoneID(cyc_zone_id);
            return vhs;
        }
        public List<AVEHICLE> loadFirstCyclingVhInEachCycleZone()
        {
            List<AVEHICLE> vhs = null;
            //using (DBConnection_EF con = new DBConnection_EF())
            //{
            //    vhs = vehicleDAO.loadFirstCyclingVhInEachCycleZone(con);
            //}
            vhs = vehicleDAO.loadFirstCyclingVhInEachCycleZone();
            return vhs;
        }


        public AVEHICLE getVhOnAddress(string adrID)
        {
            List<AVEHICLE> vhs = scApp.getEQObjCacheManager().getAllVehicle();
            return vhs.Where(vh => vh.ACT_STATUS == VHActionStatus.NoCommand &&
                                   vh.CUR_ADR_ID.Trim() == adrID.Trim()).
                       SingleOrDefault();
        }
        public bool hasVhOnAddress(string adrID)
        {
            List<AVEHICLE> vhs = scApp.getEQObjCacheManager().getAllVehicle();
            return vhs.Where(vh => vh.ACT_STATUS == VHActionStatus.NoCommand &&
                                   vh.CUR_ADR_ID.Trim() == adrID.Trim()).
                       Count() != 0;
        }
        public bool hasChargingVhOnAddress(string adrID)
        {
            List<AVEHICLE> vhs = scApp.getEQObjCacheManager().getAllVehicle();
            //return vhs.Where(vh => vh.MODE_STATUS == VHModeStatus.AutoCharging &&
            //                       vh.CUR_ADR_ID.Trim() == adrID.Trim()).
            //           Count() != 0;
            return vhs.Where(vh => vh.CUR_ADR_ID.Trim() == adrID.Trim()).
                       Count() != 0;
        }
        public bool hasVhGoingAdr(string adrID)
        {
            List<AVEHICLE> vhs = scApp.getEQObjCacheManager().getAllVehicle();
            return vhs.Where(vh => vh.ACT_STATUS == VHActionStatus.Commanding &&
                                   vh.CmdType == E_CMD_TYPE.Move_Charger &&
                                   vh.ToAdr.Trim() == adrID.Trim()).
                       Count() != 0;
        }
        public bool hasVhReserveParkAdr(string park_adr)
        {
            AVEHICLE vh = null;
            return hasVhReserveParkAdr(park_adr, out vh);
        }
        public bool hasVhReserveParkAdr(string park_adr, out AVEHICLE vh)
        {
            vh = null;
            //DBConnection_EF con = DBConnection_EF.GetContext();
            //using (DBConnection_EF con = new DBConnection_EF())
            //{
            //    vh = vehicleDAO.getOnWayParkingVhByParkAdr(con, park_adr);
            //}
            vh = vehicleDAO.getOnWayParkingVhByParkAdr(park_adr);
            return vh != null;
        }

        public List<AVEHICLE> loadAllErrorVehicle()
        {
            List<AVEHICLE> vhs = null;
            vhs = vehicleDAO.loadAllErrorVehicle();
            return vhs;
        }

        //private void FindParkZoneOrCycleRunZoneForDriveAway(AVEHICLE vh)
        //{
        //    lock (scApp.pack_lock_obj)
        //    {
        //        APACKZONEDETAIL packDetail = scApp.PackBLL.getPackDetailByAdr(vh.PACK_ADR_ID);
        //        if (packDetail.PRIO > SCAppConstants.FIRST_PARK_PRIO)
        //        {

        //            APACKZONEDETAIL nextPackDetail = scApp.PackBLL.getPackDetailByZoneIDAndPRIO
        //                (packDetail.PACK_ZONE_ID, packDetail.PRIO - 1);
        //            //bool isSuccess = scApp.CMDBLL.creatCommand_OHTC(vh.VEHICLE_ID
        //            //           , string.Empty
        //            //           , string.Empty
        //            //           , E_CMD_TYPE.Move_Pack
        //            //           , vh.CUR_ADR_ID
        //            //           , nextPackDetail.ADR_ID, 0, 0);
        //            //if (isSuccess)
        //            //{
        //            //    isSuccess = scApp.CMDBLL.generateCmd_OHTC_Details();
        //            //    return;
        //            //}
        //            scApp.CMDBLL.doSendTransferCommand(vh.VEHICLE_ID
        //                       , string.Empty
        //                       , string.Empty
        //                       , E_CMD_TYPE.Move_Pack
        //                       , vh.CUR_ADR_ID
        //                       , nextPackDetail.ADR_ID, 0, 0);
        //        }
        //        APACKZONEDETAIL bestPackDetail = null;
        //        APACKZONEMASTER pack_master = null;

        //        int readyComeToVhCountByCMD = 0;
        //        if (!scApp.CMDBLL.hasExcuteCMDFromToAdrIsParkInSpecifyPackZoneID
        //            (packDetail.PACK_ZONE_ID, out readyComeToVhCountByCMD))
        //        {
        //            pack_master = scApp.PackBLL.getPackZoneMasterByAdrID(vh.PACK_ADR_ID);
        //        }
        //        if (scApp.PackBLL.tryFindPackZone(vh, out bestPackDetail, pack_master))
        //        {
        //            bool isSuccess = scApp.CMDBLL.creatCommand_OHTC(vh.VEHICLE_ID
        //                                           , string.Empty
        //                                           , string.Empty
        //                                           , E_CMD_TYPE.Move_Pack
        //                                           , vh.CUR_ADR_ID
        //                                           , bestPackDetail.ADR_ID, 0, 0);
        //            if (isSuccess)
        //            {
        //                isSuccess = scApp.CMDBLL.generateCmd_OHTC_Details();
        //                if (isSuccess)
        //                {
        //                    scApp.PackBLL.tryAdjustTheVhPackingPositionByPackZoneAndPrio(bestPackDetail);
        //                }
        //            }
        //        }
        //        else
        //        {
        //            ACYCLEZONEMASTER bestCycleZone = null;
        //            if (scApp.CycleBLL.tryFindCycleZone(vh, out bestCycleZone))
        //            {
        //                //bool isSuccess = scApp.CMDBLL.creatCommand_OHTC(vh.VEHICLE_ID
        //                //                               , string.Empty
        //                //                               , string.Empty
        //                //                               , E_CMD_TYPE.Round
        //                //                               , vh.CUR_ADR_ID
        //                //                               , bestCycleZone.ENTRY_ADR_ID, 0, 0);
        //                //if (isSuccess)
        //                //{
        //                //    isSuccess = scApp.CMDBLL.generateCmd_OHTC_Details();
        //                //}
        //                scApp.CMDBLL.doSendTransferCommand(vh.VEHICLE_ID
        //                                               , string.Empty
        //                                               , string.Empty
        //                                               , E_CMD_TYPE.Round
        //                                               , vh.CUR_ADR_ID
        //                                               , bestCycleZone.ENTRY_ADR_ID, 0, 0);
        //            }
        //        }
        //    }
        //}
        #region DoSomeThing


        public void DoIdleVehicleHandle_InAction(VhLoadCSTStatus loadCSTStatus)
        {
            switch (loadCSTStatus)
            {
                case VhLoadCSTStatus.NotExist:
                    //1.Cancel Command
                    //2.回Home
                    break;
                case VhLoadCSTStatus.Exist:

                    break;
            }
        }

        public void doLoadArrivals(string eq_id, string current_adr_id, string current_sec_id)
        {
            scApp.CMDBLL.update_CMD_Detail_LoadStartTime(eq_id, current_adr_id, current_sec_id);
            //scApp.VIDBLL.upDateVIDPortID(eq_id, current_adr_id);
            //mcsDefaultMapAction.sendS6F11_common(SECSConst.CEID_Vehicle_Arrived, eq_id);

            AVEHICLE vh = scApp.VehicleBLL.getVehicleByID(eq_id);
            if (!SCUtility.isEmpty(vh.MCS_CMD))
            {
                scApp.SysExcuteQualityBLL.updateSysExecQity_ArrivalSourcePort(vh.MCS_CMD);
            }
            vh.VehicleArrive();
            //NetworkQualityTest(eq_id, current_adr_id, current_sec_id, 0);
        }
        public void doLoading(string eqID)
        {
            AVEHICLE vh = scApp.VehicleBLL.getVehicleByID(eqID);
            vh.VehicleAcquireStart();
            //mcsDefaultMapAction.sendS6F11_common(SECSConst.CEID_Vehicle_Acquire_Started, eq_id);
        }
        public void doLoadComplete(string eqID, string current_adr_id, string current_sec_id, string cst_id)
        {
            scApp.CMDBLL.update_CMD_Detail_LoadEndTime(eqID, current_adr_id, current_sec_id);
            //updataVehicleCSTID(eqID, cst_id);
            AVEHICLE vh = scApp.VehicleBLL.getVehicleByID(eqID);
            //沒有Vh夾取完成後，開始行走的事件，因此在夾取完成後，直接將狀態改成Enroute(Depart)
            vh.VehilceAcquireComplete();
            vh.VehicleDepart();

            //VIDCollection vids = null;
            //if (scApp.VIDBLL.tryGetVIOInfoVIDCollectionByEQID(eqID, out vids))
            //{
            //    string source_port = vids.VID_65_SourcePort.SOURCE_PORT.Trim();
            //    string cst_id_test = vids.VID_54_CarrierID.CARRIER_ID.Trim();
            //    if (source_port.Length < 9) return;

            //    string ToDevice = source_port.Substring(0, 8);

            //    logger.Trace($"Start Send remove To STK,ToDevice:{ToDevice},Dest Port:{source_port},CST ID:{cst_id_test}");
            //    Task.Run(() => scApp.webClientManager.postInfo2Stock($"{ToDevice}.mirle.com.tw", source_port, cst_id_test, "remove"));
            //    logger.Trace($"End Send remove To STK,ToDevice:{ToDevice},Dest Port:{source_port},CST ID:{cst_id_test}");
            //}
        }
        public void doUnloadArrivals(string eq_id, string current_adr_id, string current_sec_id)
        {
            scApp.CMDBLL.update_CMD_Detail_UnloadStartTime(eq_id, current_adr_id, current_sec_id);
            //scApp.VIDBLL.upDateVIDPortID(eq_id, current_adr_id);
            //mcsDefaultMapAction.sendS6F11_common(SECSConst.CEID_Vehicle_Arrived, eq_id);

            AVEHICLE vh = scApp.VehicleBLL.getVehicleByID(eq_id);
            vh.VehicleArrive();

            if (!SCUtility.isEmpty(vh.MCS_CMD))
            {
                scApp.SysExcuteQualityBLL.updateSysExecQity_ArrivalDestnPort(vh.MCS_CMD);
            }
            //NetworkQualityTest(eq_id, current_adr_id, current_sec_id, 0);
        }
        public void doUnloading(string eqID)
        {
            AVEHICLE vh = scApp.VehicleBLL.getVehicleByID(eqID);
            vh.VehicleDepositStart();
        }
        public void doUnloadComplete(string eqID)
        {
            //updataVehicleCSTID(eqID, "");
            //mcsDefaultMapAction.sendS6F11_common(SECSConst.CEID_Vehicle_Removed, eq_id);
            //mcsDefaultMapAction.sendS6F11_common(SECSConst.CEID_Vehicle_Deposit_Completed, eq_id);
            //AVEHICLE vh = scApp.VehicleBLL.getVehicleByID(eq_id);
            //if (!SCUtility.isEmpty(vh.MCS_CMD))
            //{
            //    scApp.SysExcuteQualityBLL.updateSysExecQity_CmdFinish(vh.MCS_CMD);
            //}
            AVEHICLE vh = scApp.VehicleBLL.getVehicleByID(eqID);
            vh.VehicleDepositComplete();

            //VIDCollection vids = null;
            //if (scApp.VIDBLL.tryGetVIOInfoVIDCollectionByEQID(eqID, out vids))
            //{
            //    string dest_port = vids.VID_60_DestinationPort.DESTINATION_PORT.Trim();
            //    string cst_id_test = vids.VID_54_CarrierID.CARRIER_ID.Trim();
            //    if (dest_port.Length < 9) return;
            //    string ToDevice = dest_port.Substring(0, 8);
            //    logger.Trace($"Start Send waitin To STK,ToDevice:{ToDevice},Dest Port:{dest_port},CST ID:{cst_id_test}");
            //    Task.Run(() => scApp.webClientManager.postInfo2Stock($"{ToDevice}.mirle.com.tw", dest_port, cst_id_test, "waitin"));
            //    logger.Trace($"End Send waitin To STK,ToDevice:{ToDevice},Dest Port:{dest_port},CST ID:{cst_id_test}");
            //}
        }
        //public void doTransferCommandFinish(Equipment eqpt)
        public virtual bool doTransferCommandFinish(string vh_id, string cmd_id, CompleteStatus completeStatus, int total_cmd_dis)
        {
            bool isSuccess = true;
            //1.
            try
            {
                //scApp.VehicleBLL.getAndProcPositionReportFromRedis(vh_id);
                //TODO 再Update沒有成功這件事情要分成兩個部分，1.沒有找到該筆Command (要回復VH)2.發生Exection(不回復VH)。
                AVEHICLE vh = scApp.VehicleBLL.getVehicleByID(vh_id);
                string mcs_cmd_id = vh.MCS_CMD;

                vh.WillPassSectionID = null;
                vh.FromPort = null;
                vh.ToPort = null;
                vh.startAdr = string.Empty;
                vh.FromAdr = string.Empty;
                vh.ToAdr = string.Empty;
                vh.procProgress_Percen = 0;
                vh.vh_CMD_Status = E_CMD_STATUS.NormalEnd;
                vh.VehicleUnassign();
                vh.Stop();
                E_CMD_STATUS ohtc_cmd_status = CompleteStatusToECmdStatus(completeStatus);

                //isSuccess &= scApp.CMDBLL.updateCommand_OHTC_StatusByCmdID(cmd_id, E_CMD_STATUS.NormalEnd);
                //isSuccess &= scApp.CMDBLL.updateCommand_OHTC_StatusByCmdID(cmd_id, ohtc_cmd_status);
                isSuccess &= scApp.CMDBLL.updateCommand_OHTC_StatusByCmdID(vh_id, cmd_id, ohtc_cmd_status);

                if (!SCUtility.isEmpty(mcs_cmd_id))
                {
                    E_TRAN_STATUS mcs_cmd_tran_status = CompleteStatusToETransferStatus(completeStatus);
                    //isSuccess &= scApp.SysExcuteQualityBLL.updateSysExecQity_CmdFinish(vh.MCS_CMD);
                    //isSuccess &= scApp.CMDBLL.updateCMD_MCS_TranStatus2Complete(vh.MCS_CMD);
                    ASYSEXCUTEQUALITY quality = null;
                    scApp.SysExcuteQualityBLL.updateSysExecQity_CmdFinish(mcs_cmd_id, ohtc_cmd_status, completeStatus, total_cmd_dis, out quality);
                    //scApp.CMDBLL.updateCMD_MCS_TranStatus2Complete(mcs_cmd_id, E_TRAN_STATUS.Complete);
                    scApp.CMDBLL.updateCMD_MCS_TranStatus2Complete(mcs_cmd_id, mcs_cmd_tran_status);
                    if (quality != null)
                    {
                        SCUtility.TrimAllParameter(quality);
                        LogManager.GetLogger("SysExcuteQuality").Info(quality.ToString());
                    }
                }
                //isSuccess &= scApp.CMDBLL.updateCommand_OHTC_StatusByVhID(vh_id, E_CMD_STATUS.NormalEnd);
                //updateVehicleExcuteCMD(vh_id, string.Empty, string.Empty);
                vh.NotifyVhExcuteCMDStatusChange();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exection:");
                isSuccess = false;
            }
            return isSuccess;
            //2.
            //bool isParkAdr = scApp.ParkBLL.isInPrckAddress(vh.CUR_ADR_ID, vh.VEHICLE_TYPE);
            //if (isParkAdr)
            //{
            //    scApp.VehicleBLL.setVhIsParkingOnWay(eq_id, vh.CUR_ADR_ID);
            //    scApp.ParkBLL.checkAndUpdateVhEntryParkingAdr(eq_id, vh.CUR_ADR_ID);
            //}
            ////bool needMoveToIdle = vh.VEHICLE_TYPE == E_VH_TYPE.Dirty
            ////                   || scApp.CMDBLL.getCMD_MCSisQueueCount() == 0;
            //else
            //{
            //    Task.Run(() =>
            //    {
            //        bool isCmdInQueue = scApp.CMDBLL.isCMD_OHTCQueueByVh(eq_id);
            //        if (!isCmdInQueue)
            //        {
            //            bool isParking = false;
            //            bool isCycleRun = false;
            //            isParking = scApp.ParkBLL.checkAndUpdateVhEntryParkingAdr(eq_id, vh.CUR_ADR_ID);
            //            if (!isParking)
            //            {
            //                isCycleRun = scApp.CycleBLL.checkAndUpdateVhEntryCycleRunAdr(eq_id, vh.CUR_ADR_ID);
            //                if (!isCycleRun)
            //                {
            //                    FindParkZoneOrCycleRunZoneNew(vh);
            //                }
            //            }
            //        }
            //    });
            //}
            //3.
            //mcsDefaultMapAction.sendS6F11_common(SECSConst.CEID_Vehicle_Unassigned, eq_id);
            //mcsDefaultMapAction.sendS6F11_common(SECSConst.CEID_Transfer_Completed, eq_id);
            //scApp.VIDBLL.initialVIDCommandInfo(eq_id);
        }
        private E_CMD_STATUS CompleteStatusToECmdStatus(CompleteStatus completeStatus)
        {
            switch (completeStatus)
            {
                case CompleteStatus.CmpStatusCancel:
                    return E_CMD_STATUS.CancelEndByOHTC;
                case CompleteStatus.CmpStatusAbort:
                    return E_CMD_STATUS.AbnormalEndByOHTC;
                case CompleteStatus.CmpStatusVehicleAbort:
                case CompleteStatus.CmpStatusIdmisMatch:
                case CompleteStatus.CmpStatusIdreadFailed:
                case CompleteStatus.CmpStatusInterlockError:
                case CompleteStatus.CmpStatusLongTimeInaction:
                    return E_CMD_STATUS.AbnormalEndByOHT;
                case CompleteStatus.CmpStatusForceFinishByOp:
                    return E_CMD_STATUS.AbnormalEndByOHTC;
                default:
                    return E_CMD_STATUS.NormalEnd;
            }
        }
        private E_TRAN_STATUS CompleteStatusToETransferStatus(CompleteStatus completeStatus)
        {
            switch (completeStatus)
            {
                case CompleteStatus.CmpStatusCancel:
                    return E_TRAN_STATUS.Canceled;
                case CompleteStatus.CmpStatusAbort:
                case CompleteStatus.CmpStatusVehicleAbort:
                case CompleteStatus.CmpStatusIdmisMatch:
                case CompleteStatus.CmpStatusIdreadFailed:
                case CompleteStatus.CmpStatusInterlockError:
                case CompleteStatus.CmpStatusLongTimeInaction:
                case CompleteStatus.CmpStatusForceFinishByOp:
                    return E_TRAN_STATUS.Aborted;
                default:
                    return E_TRAN_STATUS.Complete;
            }
        }


        public bool callVehicleToMove(AVEHICLE vh, string to_adr)
        {
            bool isSuccess = scApp.CMDBLL.doCreatTransferCommand(vh.VEHICLE_ID
                                           , string.Empty
                                           , string.Empty
                                           , E_CMD_TYPE.Move_Park
                                           , vh.CUR_ADR_ID
                                           , to_adr, 0, 0);
            return isSuccess;
        }


        public void doAdrArrivals(string eq_id, string current_adr_id, string current_sec_id)
        {
            NetworkQualityTest(eq_id, current_adr_id, current_sec_id, 0);
        }
        private void NetworkQualityTest(string eq_id, string current_adr_id, string current_sec_id, int acc_dist)
        {
            if (scApp.getBCFApplication().NetworkQualityTest)
            {
                //Task.Run(() => scApp.NetWorkQualityBLL.VhNetworkQualityTest(eq_id, current_adr_id, current_sec_id, acc_dist));
            }
        }


        public void noticeVhPass(string vh_id)
        {
            BLOCKZONEQUEUE usingBlockQueue = scApp.MapBLL.getUsingBlockZoneQueueByVhID(vh_id);
            if (usingBlockQueue != null)
            {
                noticeVhPass(usingBlockQueue);
            }
            else
            {
                string reason = string.Empty;
                scApp.VehicleService.PauseRequest(vh_id, PauseEvent.Continue, SCAppConstants.OHxCPauseType.Block);
                //AVEHICLE noticeCar = scApp.getEQObjCacheManager().getVehicletByVHID(vh_id);
                //noticeCar.sned_Str39(PauseEvent.Continue, PauseType.Block);
                // noticeCar.sned_Str31(string.Empty, ActiveType.Continue, string.Empty, new string[0], new string[0], string.Empty, string.Empty, out reason);
            }
        }
        public bool noticeVhPass(BLOCKZONEQUEUE blockZoneQueue)
        {
            //Equipment noticeCar = scApp.getEQObjCacheManager().getEquipmentByEQPTID(blockZoneQueue.CAR_ID.Trim());
            //AVEHICLE noticeCar = scApp.getEQObjCacheManager().getVehicletByVHID(blockZoneQueue.CAR_ID.Trim());
            string notice_vh_id = blockZoneQueue.CAR_ID.Trim();
            string reason = string.Empty;
            //bool isSuccess = noticeCar.sned_Str31
            //    (string.Empty, ActiveType.Continue, string.Empty, new string[0], new string[0], string.Empty, string.Empty, out reason);
            //bool isSuccess = noticeCar.sned_Str39(PauseEvent.Continue, PauseType.Block);
            bool isSuccess = scApp.VehicleService.PauseRequest(notice_vh_id, PauseEvent.Continue, SCAppConstants.OHxCPauseType.Block);
            if (isSuccess)
            {
                string req_block_id = blockZoneQueue.ENTRY_SEC_ID.Trim();
                if (scApp.MapBLL.IsBlockControlStatus
                      (notice_vh_id, SCAppConstants.BlockQueueState.Request))
                {
                    scApp.MapBLL.updateBlockZoneQueue_BlockTime(notice_vh_id, req_block_id);
                    scApp.MapBLL.ChangeBlockControlStatus_Blocking(notice_vh_id);
                }
                //scApp.MapBLL.updateBlockZoneQueue_BlockTime(noticeCar.VEHICLE_ID, blockZoneQueue.ENTRY_SEC_ID.Trim());
                //Console.WriteLine("Notice vh pass,ID:{0},ENTRY_SEC_ID:{1},BLOCK_TIME:{2}",
                //                   blockZoneQueue.CAR_ID.Trim(),
                //                   blockZoneQueue.ENTRY_SEC_ID.Trim(),
                //                   blockZoneQueue.REQ_TIME.ToString(SCAppConstants.DateTimeFormat_22));
            }
            else
            {
                //Console.WriteLine("Notice vh pass fail,ID:{0},ENTRY_SEC_ID:{1},BLOCK_TIME:{2}",
                //                   blockZoneQueue.CAR_ID.Trim(),
                //                   blockZoneQueue.ENTRY_SEC_ID.Trim(),
                //                   blockZoneQueue.REQ_TIME.ToString(SCAppConstants.DateTimeFormat_22));

            }
            return isSuccess;
        }




        #endregion




        TimeSpan POSITION_TIMEOUT = new TimeSpan(0, 5, 0);
        public void clearAndPublishPositionReportInfo2Redis(string vhID)
        {
            setAndPublishPositionReportInfo2Redis(vhID, string.Empty, string.Empty, 0);
        }
        public void setAndPublishPositionReportInfo2Redis(string vh_id, string sec_id, string adr_id, uint distance)
        {
            AVEHICLE vh = getVehicleByID(vh_id);
            ID_134_TRANS_EVENT_REP id_134_trans_event_rep = new ID_134_TRANS_EVENT_REP()
            {
                //CSTID = vh.CST_ID,
                CurrentAdrID = adr_id,
                CurrentSecID = sec_id,
                EventType = EventType.AdrPass,
                SecDistance = distance
            };
            setAndPublishPositionReportInfo2Redis(vh_id, id_134_trans_event_rep);
        }

        public void setAndPublishPositionReportInfo2Redis(string vh_id, ID_143_STATUS_RESPONSE report_obj)
        {
            AVEHICLE vh = getVehicleByID(vh_id);

            ID_134_TRANS_EVENT_REP id_134_trans_event_rep = new ID_134_TRANS_EVENT_REP()
            {
                //CSTID = vh.CST_ID == null ? "" : vh.CST_ID,
                CurrentAdrID = report_obj.CurrentAdrID,
                CurrentSecID = report_obj.CurrentSecID,
                EventType = vh.VhRecentTranEvent,
                SecDistance = report_obj.SecDistance,
                XAxis = report_obj.XAxis,
                YAxis = report_obj.YAxis,
                DirectionAngle = report_obj.DirectionAngle,
                VehicleAngle = report_obj.VehicleAngle
            };
            setAndPublishPositionReportInfo2Redis(vh_id, id_134_trans_event_rep);
        }

        public void setAndPublishPositionReportInfo2Redis(string vh_id, ID_144_STATUS_CHANGE_REP report_obj)
        {
            //AVEHICLE vh = getVehicleByID(vh_id);
            //ID_134_TRANS_EVENT_REP id_134_trans_event_rep = new ID_134_TRANS_EVENT_REP()
            //{
            //    //CSTID = vh.CST_ID == null ? "" : vh.CST_ID,
            //    CurrentAdrID = report_obj.CurrentAdrID,
            //    CurrentSecID = report_obj.CurrentSecID,
            //    EventType = vh.VhRecentTranEvent,
            //    SecDistance = report_obj.SecDistance,
            //    DrivingDirection = report_obj.DrivingDirection
            //};
            //setAndPublishPositionReportInfo2Redis(vh_id, id_134_trans_event_rep);
        }
        public void setAndPublishPositionReportInfo2Redis(string vh_id, ID_136_TRANS_EVENT_REP report_obj)
        {
            //AVEHICLE vh = getVehicleByID(vh_id);
            //ID_134_TRANS_EVENT_REP id_134_trans_event_rep = new ID_134_TRANS_EVENT_REP()
            //{
            //    //CSTID = report_obj.CSTID,
            //    CurrentAdrID = report_obj.CurrentAdrID,
            //    CurrentSecID = report_obj.CurrentSecID,
            //    EventType = report_obj.EventType,
            //    SecDistance = report_obj.SecDistance == 0 ? (uint)vh.ACC_SEC_DIST : report_obj.SecDistance,
            //    DrivingDirection = vh.CurrentDriveDirction
            //};
            //setAndPublishPositionReportInfo2Redis(vh_id, id_134_trans_event_rep);
        }


        public void setAndPublishPositionReportInfo2Redis(string vh_id, ID_172_RANGE_TEACHING_COMPLETE_REPORT report_obj, string sec_id)
        {
            //AVEHICLE vh = getVehicleByID(vh_id);
            //string from_adr = report_obj.FromAdr;
            //string to_adr = report_obj.ToAdr;
            //ID_134_TRANS_EVENT_REP id_134_trans_event_rep = new ID_134_TRANS_EVENT_REP()
            //{
            //    //CSTID = report_obj.CSTID,
            //    CurrentAdrID = report_obj.ToAdr,
            //    CurrentSecID = sec_id,
            //    EventType = vh.VhRecentTranEvent,
            //    SecDistance = report_obj.SecDistance == 0 ? (uint)vh.ACC_SEC_DIST : report_obj.SecDistance,
            //    DrivingDirection = vh.CurrentDriveDirction
            //};
            //setAndPublishPositionReportInfo2Redis(vh_id, id_134_trans_event_rep);
        }

        public void setAndPublishPositionReportInfo2Redis(string vh_id, ID_132_TRANS_COMPLETE_REPORT report_obj)
        {
            AVEHICLE vh = getVehicleByID(vh_id);
            string current_adr = report_obj.CurrentAdrID;
            ID_134_TRANS_EVENT_REP id_134_trans_event_rep = new ID_134_TRANS_EVENT_REP()
            {
                //CSTID = report_obj.CSTID,
                CurrentAdrID = report_obj.CurrentAdrID,
                CurrentSecID = report_obj.CurrentSecID,
                EventType = vh.VhRecentTranEvent,
                SecDistance = report_obj.SecDistance == 0 ? (uint)vh.ACC_SEC_DIST : report_obj.SecDistance,
                DrivingDirection = vh.CurrentDriveDirction,
                XAxis = report_obj.XAxis,
                YAxis = report_obj.YAxis,
                DirectionAngle = report_obj.DirectionAngle,
                VehicleAngle = report_obj.VehicleAngle
            };
            setAndPublishPositionReportInfo2Redis(vh_id, id_134_trans_event_rep);
        }

        public void setAndPublishPositionReportInfo2Redis(string vh_id, ID_152_AVOID_COMPLETE_REPORT report_obj)
        {
            AVEHICLE vh = getVehicleByID(vh_id);
            string current_adr = report_obj.CurrentAdrID;
            ID_134_TRANS_EVENT_REP id_134_trans_event_rep = new ID_134_TRANS_EVENT_REP()
            {
                //CSTID = report_obj.CSTID,
                CurrentAdrID = report_obj.CurrentAdrID,
                CurrentSecID = report_obj.CurrentSecID,
                EventType = vh.VhRecentTranEvent,
                SecDistance = report_obj.SecDistance == 0 ? (uint)vh.ACC_SEC_DIST : report_obj.SecDistance,
                DrivingDirection = vh.CurrentDriveDirction,
                XAxis = report_obj.XAxis,
                YAxis = report_obj.YAxis,
                DirectionAngle = report_obj.DirectionAngle,
                VehicleAngle = report_obj.VehicleAngle
            };
            setAndPublishPositionReportInfo2Redis(vh_id, id_134_trans_event_rep);
        }

        private void conterAddress2XYAxis()
        {
            throw new NotImplementedException();
        }

        public virtual void setAndPublishPositionReportInfo2Redis(string vh_id, ID_134_TRANS_EVENT_REP report_obj)
        {
            setPositionReportInfo2Redis(vh_id, report_obj);
            //PublishPositionReportInfo2Redis(vh_id, report_obj);
            doUpdateVheiclePositionAndCmdSchedule(vh_id, report_obj);
        }


        private void setPositionReportInfo2Redis(string vh_id, ID_134_TRANS_EVENT_REP report_obj)
        {
            string key_word_position = $"{SCAppConstants.REDIS_KEY_WORD_POSITION_REPORT}_{vh_id}";
            byte[] arrayByte = new byte[report_obj.CalculateSize()];
            report_obj.WriteTo(new Google.Protobuf.CodedOutputStream(arrayByte));
            scApp.getRedisCacheManager().Obj2ByteArraySetAsync(key_word_position, arrayByte, POSITION_TIMEOUT);
        }

        public void loadAllAndProcPositionReportFromRedis()
        {
            var listVh = scApp.getEQObjCacheManager().getAllVehicle();
            //foreach (AVEHICLE vh in listVh)
            //{
            //    scApp.VehicleBLL.getAndProcPositionReportFromRedis(vh.VEHICLE_ID);
            //    SpinWait.SpinUntil(() => false, 5);
            //}
        }

        const int ALLOWANCE_DISTANCE_mm = 10;
        private void doUpdateVheiclePositionAndCmdSchedule
            (string vhID, ID_134_TRANS_EVENT_REP report_obj)
        {
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vhID);

            string current_sec_id = SCUtility.isEmpty(report_obj.CurrentSecID) ? string.Empty : report_obj.CurrentSecID;
            string current_adr_id = SCUtility.isEmpty(report_obj.CurrentAdrID) ? string.Empty : report_obj.CurrentAdrID;
            double x_axis = report_obj.XAxis;
            double y_axis = report_obj.YAxis;
            double dir_angle = report_obj.DirectionAngle;
            double vh_angle = report_obj.VehicleAngle;
            double speed = report_obj.Speed;
            List<string> current_guide_address = vh.PredictAddresses?.ToList();
            DriveDirction drive_dirction = report_obj.DrivingDirection;
            //DriveDirction drive_dirction = getDrivingDirection(current_sec_id, current_guide_address);
            //DriveDirction drive_dirction = getDrivingDirection(vh, current_sec_id);
            speed = drive_dirction == DriveDirction.DriveDirForward ? speed : -speed;
            //如果這次上報的x、y 為0，則繼續拿上一次地來更新
            x_axis = x_axis == 0 ? vh.X_Axis : x_axis;
            y_axis = y_axis == 0 ? vh.Y_Axis : y_axis;

            if (SCUtility.isEmpty(current_adr_id))
            {
                ASECTION cur_sec_obj = scApp.SectionBLL.cache.GetSection(current_sec_id);
                current_adr_id = cur_sec_obj == null ? string.Empty : cur_sec_obj.FROM_ADR_ID;
            }

            ASECTION sec_obj = scApp.SectionBLL.cache.GetSection(current_sec_id);
            string current_seg_id = sec_obj == null ? string.Empty : sec_obj.SEG_NUM;
            string last_adr_id = vh.CUR_ADR_ID;
            string last_sec_id = vh.CUR_SEC_ID;
            string last_seg_id = vh.CUR_SEG_ID;

            uint sec_dis = report_obj.SecDistance;

            //DriveDirction drive_dirction = report_obj.DrivingDirection;

            lock (vh.PositionRefresh_Sync)
            {
                double distanceWithLastPosition = getDistance(x_axis, y_axis, vh.X_Axis, vh.Y_Axis);
                Console.WriteLine($"distance:{distanceWithLastPosition}");
                if (distanceWithLastPosition < ALLOWANCE_DISTANCE_mm)
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleBLL), Device: Service.VehicleService.DEVICE_NAME_AGV,
                       Data: $"The vehicles report position change, the distan less then:{ALLOWANCE_DISTANCE_mm}mm, by pass it.",
                       VehicleID: vh.VEHICLE_ID,
                       CarrierID: vh.CST_ID);
                    return;
                }
                updateVheiclePosition_CacheManager(vh, current_adr_id, current_sec_id, current_seg_id, sec_dis, drive_dirction, x_axis, y_axis, dir_angle, vh_angle);
                //if (!SCUtility.isMatche(current_adr_id, last_adr_id))
                {
                    var sensor_dir = decideReserveDirection(vh_angle);
                    //var update_result = updateVheiclePositionToReserveControlModule(scApp.ReserveBLL, vh, x_axis, y_axis, dir_angle, vh_angle, speed,
                    //                                                                Mirle.Hlts.Utils.HltDirection.NESW, Mirle.Hlts.Utils.HltDirection.None);
                    var update_result = updateVheiclePositionToReserveControlModule(scApp.ReserveBLL, vh, current_sec_id, x_axis, y_axis, dir_angle, vh_angle, speed,
                                                                                    sensor_dir, Mirle.Hlts.Utils.HltDirection.None);
                    if (!update_result.OK)
                    {
                        string message = $"The vehicles bumped, vh:{vh.VEHICLE_ID} with vh:{update_result.VehicleID}";
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleBLL), Device: Service.VehicleService.DEVICE_NAME_AGV,
                           Data: message,
                           Details: update_result.Description,
                           VehicleID: vh.VEHICLE_ID,
                           CarrierID: vh.CST_ID);

                        AVEHICLE will_bumped_vh = cache.getVehicle(update_result.VehicleID);

                        //2020/02/05 由於目前在行走R2000的時候，車子的角度還無法時時上報，因此先ByPass R2000的檢查
                        bool one_of_them_is_in_r2000 = scApp.ReserveBLL.IsR2000Section(vh.CUR_SEC_ID) ||
                                                       scApp.ReserveBLL.IsR2000Section(will_bumped_vh.CUR_SEC_ID);
                        //如果發生碰撞或踩入別人預約的不是虛擬車的話，則就要對兩台車下達EMS
                        if (!one_of_them_is_in_r2000 &&
                            !update_result.VehicleID.StartsWith(Service.VehicleService.VehicleVirtualSymbol))
                        {
                            //bcf.App.BCFApplication.onErrorMsg(message);
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleBLL), Device: Service.VehicleService.DEVICE_NAME_AGV,
                               Data: $"The vehicles bumped will happend. send ems to {vhID}",
                               VehicleID: vh.VEHICLE_ID,
                               CarrierID: vh.CST_ID);
                            //scApp.VehicleService.doAbortCommand(vh, vh.OHTC_CMD, CMDCancelType.CmdEms);
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleBLL), Device: Service.VehicleService.DEVICE_NAME_AGV,
                               Data: $"The vehicles bumped will happend. send ems to {update_result.VehicleID}",
                               VehicleID: vh.VEHICLE_ID,
                               CarrierID: vh.CST_ID);
                            //scApp.VehicleService.doAbortCommand(will_bumped_vh, will_bumped_vh.OHTC_CMD, CMDCancelType.CmdEms);
                        }
                    }
                }
                ALINE line = scApp.getEQObjCacheManager().getLine();
                if (line.ServiceMode == SCAppConstants.AppServiceMode.Active)
                {
                    if (!SCUtility.isMatche(last_sec_id, current_sec_id))
                    {
                        //TODO 要改成查一次CMD出來然後直接帶入CMD ID
                        if (!SCUtility.isEmpty(vh.OHTC_CMD))
                        {
                            scApp.CMDBLL.update_CMD_DetailEntryTime(vh.OHTC_CMD, current_adr_id, current_sec_id);
                            scApp.CMDBLL.update_CMD_DetailLeaveTime(vh.OHTC_CMD, last_adr_id, last_sec_id);
                            List<string> willPassSecID = null;
                            vh.procProgress_Percen = scApp.CMDBLL.getAndUpdateVhCMDProgress(vh.VEHICLE_ID, out willPassSecID);
                        }
                        vh.onLocationChange(current_sec_id, last_sec_id);
                    }

                    if (!SCUtility.isMatche(current_seg_id, last_seg_id))
                    {
                        vh.onSegmentChange(current_seg_id, last_seg_id);
                    }
                }
                //scApp.VehicleBLL.updateVheiclePosition_CacheManager(vh, current_adr_id, current_sec_id, current_seg_id, sec_dis, drive_dirction);
            }
        }

        private double getDistance(double x1, double y1, double x2, double y2)
        {
            double dx, dy;
            dx = x2 - x1;
            dy = y2 - y1;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private Mirle.Hlts.Utils.HltDirection decideReserveDirection(double vhAngle)
        {
            if (vhAngle == 90
                || vhAngle == -90
                || vhAngle == -270
                || vhAngle == 270)
                return Mirle.Hlts.Utils.HltDirection.NS;
            else
            {
                return Mirle.Hlts.Utils.HltDirection.EW;
            }
        }


        private DriveDirction getDrivingDirection(string currentSec, List<string> currentGuideAddress)
        {
            if (currentGuideAddress == null) return DriveDirction.DriveDirNone;
            ASECTION sec = scApp.SectionBLL.cache.GetSection(currentSec);
            string from_to_addresses = $"{SCUtility.Trim(sec.REAL_FROM_ADR_ID)},{SCUtility.Trim(sec.REAL_TO_ADR_ID)}";

            string current_guide_Addresses = string.Join(",", currentGuideAddress);

            return current_guide_Addresses.Contains(from_to_addresses) ?
                   DriveDirction.DriveDirForward : DriveDirction.DriveDirReverse;
        }
        private DriveDirction getDrivingDirection(AVEHICLE vh, string currentSecID)
        {
            string current_section = currentSecID;
            if (SCUtility.isEmpty(current_section)) return DriveDirction.DriveDirNone;

            ASECTION sec = scApp.SectionBLL.cache.GetSection(current_section);
            string from_to_addresses = $"{SCUtility.Trim(sec.REAL_FROM_ADR_ID)},{SCUtility.Trim(sec.REAL_TO_ADR_ID)}";

            if (vh.VhAvoidInfo != null)
            {
                List<string> current_avoid_guide_address = vh.VhAvoidInfo.GuideAddresses?.ToList();
                if (current_avoid_guide_address == null) return DriveDirction.DriveDirNone;
                string current_avoid_guide_Addresses = string.Join(",", current_avoid_guide_address);
                return current_avoid_guide_Addresses.Contains(from_to_addresses) ?
                       DriveDirction.DriveDirForward : DriveDirction.DriveDirReverse;
            }
            else
            {
                List<string> current_guide_address = vh.PredictAddresses?.ToList();
                if (current_guide_address == null) return DriveDirction.DriveDirNone;

                string current_guide_Addresses = string.Join(",", current_guide_address);

                return current_guide_Addresses.Contains(from_to_addresses) ?
                       DriveDirction.DriveDirForward : DriveDirction.DriveDirReverse;
            }
        }
        #region Vehicle Object Info

        /// <summary>
        /// 將AVEHICLE物件轉換成GBP的VEHICLE_INFO物件
        /// 要用來做物件的序列化所使用
        /// </summary>
        /// <param name="vh"></param>
        /// <returns></returns>
        public static byte[] Convert2GPB_VehicleInfo(AVEHICLE vh)
        {
            int vehicleType = (int)vh.VEHICLE_TYPE;
            int cmd_tpye = (int)vh.CmdType;
            int cmd_status = (int)vh.vh_CMD_Status;
            VEHICLE_INFO vh_gpp = new VEHICLE_INFO()
            {
                VEHICLEID = vh.VEHICLE_ID,
                IsTcpIpConnect = vh.isTcpIpConnect,
                VEHICLETYPE = (com.mirle.ibg3k0.sc.ProtocolFormat.OHTMessage.VehicleType)vehicleType,
                CURADRID = vh.CUR_ADR_ID == null ? string.Empty : vh.CUR_ADR_ID,
                CURSECID = vh.CUR_SEC_ID == null ? string.Empty : vh.CUR_SEC_ID,
                ACCSECDIST = vh.ACC_SEC_DIST,
                MODESTATUS = vh.MODE_STATUS,
                ACTSTATUS = vh.ACT_STATUS,
                MCSCMD = vh.MCS_CMD == null ? string.Empty : vh.MCS_CMD,
                OHTCCMD = vh.OHTC_CMD == null ? string.Empty : vh.OHTC_CMD,
                PAUSESTATUS = vh.PauseStatus,
                CMDPAUSE = vh.CMD_PAUSE,
                BLOCKPAUSE = vh.BLOCK_PAUSE,
                OBSPAUSE = vh.OBS_PAUSE,
                HIDPAUSE = vh.HIDStatus,
                SAFETYDOORPAUSE = vh.SAFETY_DOOR_PAUSE,
                EARTHQUAKEPAUSE = vh.EARTHQUAKE_PAUSE,
                RESERVEPAUSE = vh.RESERVE_PAUSE,
                ERROR = vh.ERROR,
                OBSDIST = vh.OBS_DIST,
                HASCST = vh.HAS_CST,
                CSTID = vh.CST_ID == null ? string.Empty : vh.CST_ID,
                VEHICLEACCDIST = vh.VEHICLE_ACC_DIST,
                MANTACCDIST = vh.MANT_ACC_DIST,
                GRIPCOUNT = vh.GRIP_COUNT,
                GRIPMANTCOUNT = vh.GRIP_MANT_COUNT,
                ISPARKING = vh.IS_PARKING,
                PARKADRID = vh.PARK_ADR_ID == null ? string.Empty : vh.PARK_ADR_ID,
                ISCYCLING = vh.IS_CYCLING,
                CYCLERUNID = vh.CYCLERUN_ID == null ? string.Empty : vh.CYCLERUN_ID,

                StartAdr = vh.startAdr == null ? string.Empty : vh.startAdr,
                FromAdr = vh.FromAdr == null ? string.Empty : vh.FromAdr,
                ToAdr = vh.ToAdr == null ? string.Empty : vh.ToAdr,
                Speed = vh.Speed,
                ObsVehicleID = vh.ObsVehicleID == null ? string.Empty : vh.ToAdr,
                CmdType = (com.mirle.ibg3k0.sc.ProtocolFormat.OHTMessage.CommandType)cmd_tpye,
                VhCMDStatus = (com.mirle.ibg3k0.sc.ProtocolFormat.OHTMessage.CommandStatus)cmd_status,
                VhRecentTranEvent = vh.VhRecentTranEvent,
                ProcProgressPercen = vh.procProgress_Percen,
                CurrentDriveDirction = vh.CurrentDriveDirction,
                BatteryCapacity = (uint)vh.BatteryCapacity,
                ChargeStatus = vh.ChargeStatus,
                BatteryTemperature = vh.BatteryTemperature
            };
            if (vh.PredictSections != null)
                vh_gpp.PredictPath.AddRange(vh.PredictSections);
            if (vh.WillPassSectionID != null)
                vh_gpp.WillPassSectionID.AddRange(vh.WillPassSectionID);
            if (vh.Alarms != null)
                vh_gpp.Alarms.AddRange(vh.Alarms);
            LogManager.GetLogger("VehicleHistoricalInfo").Trace(vh_gpp.ToString());

            byte[] arrayByte = new byte[vh_gpp.CalculateSize()];
            vh_gpp.WriteTo(new Google.Protobuf.CodedOutputStream(arrayByte));
            return arrayByte;
        }

        public static VEHICLE_INFO Convert2Object_VehicleInfo(byte[] raw_data)
        {
            return ToObject<VEHICLE_INFO>(raw_data);
        }
        private static T ToObject<T>(byte[] buf) where T : Google.Protobuf.IMessage<T>, new()
        {
            if (buf == null)
                return default(T);
            Google.Protobuf.MessageParser<T> parser = new Google.Protobuf.MessageParser<T>(() => new T());
            return parser.ParseFrom(buf);
        }
        #endregion Vehicle Object Info


        public class Cache
        {
            EQObjCacheManager eqObjCacheManager = null;
            public Cache(EQObjCacheManager eqObjCacheManager)
            {
                this.eqObjCacheManager = eqObjCacheManager;
            }

            public void ResetCanNotReserveInfo(string vhID)
            {
                var vh = eqObjCacheManager.getAllVehicle().Where(v => SCUtility.isMatche(v.VEHICLE_ID, vhID)).SingleOrDefault();
                vh.CanNotReserveInfo = null;
            }
            public void SetUnsuccessReserveInfo(string vhID, AVEHICLE.ReserveUnsuccessInfo reserveUnsuccessInfo)
            {
                var vh = eqObjCacheManager.getAllVehicle().Where(v => SCUtility.isMatche(v.VEHICLE_ID, vhID)).SingleOrDefault();
                vh.CanNotReserveInfo = reserveUnsuccessInfo;
            }
            public void SetReservePause(string vhID, VhStopSingle stopSingle)
            {
                var vh = eqObjCacheManager.getAllVehicle().Where(v => SCUtility.isMatche(v.VEHICLE_ID, vhID)).SingleOrDefault();
                vh.RESERVE_PAUSE = stopSingle;
                vh.NotifyVhStatusChange();
                //如果stop single是 off則重置計算override fail的次數
                if (stopSingle == VhStopSingle.StopSingleOff)
                {
                    vh.CurrentFailOverrideTimes = 0;
                }
            }


            public bool IsVehicleExistByRealID(string vhRealID)
            {
                var vhs = eqObjCacheManager.getAllVehicle();
                int count = vhs.Where(vh => SCUtility.isMatche(vh.Real_ID, vhRealID)).Count();
                return count != 0;
            }

            public AVEHICLE getVehicle(string vhID)
            {
                var vh = eqObjCacheManager.getVehicletByVHID(vhID);
                return vh;
            }
            public AVEHICLE getVehicleByRealID(string vhRealID)
            {
                var vh = eqObjCacheManager.getVehicletByRealID(vhRealID);
                return vh;
            }
            public AVEHICLE getVehicleByMCSCmdID(string mcsCmdID)
            {
                var vhs = eqObjCacheManager.getAllVehicle();
                AVEHICLE vh = vhs.Where(v => SCUtility.isMatche(v.MCS_CMD, mcsCmdID)).
                    FirstOrDefault();
                return vh;
            }
            public AVEHICLE getVehicleByCSTID(string cstID)
            {
                var vhs = eqObjCacheManager.getAllVehicle();
                AVEHICLE vh = vhs.Where(v => SCUtility.isMatche(v.CST_ID, cstID)).
                    FirstOrDefault();
                return vh;
            }


            public List<AVEHICLE> loadAllVh()
            {
                var vhs = eqObjCacheManager.getAllVehicle();
                return vhs;
            }
            public List<AVEHICLE> loadVhBySegmentID(string segID)
            {
                var vhs = eqObjCacheManager.getAllVehicle();
                return vhs.
                       Where(vh => vh.CUR_SEG_ID.Trim() == segID.Trim()).
                       ToList();
            }
            public List<AVEHICLE> loadVhBySectionIDs(List<string> segIDs)
            {
                var vhs = eqObjCacheManager.getAllVehicle();
                return vhs.
                       Where(vh => segIDs.Contains(SCUtility.Trim(vh.CUR_SEC_ID, true))).
                       ToList();
            }
            public List<AVEHICLE> loadVhByAddressIDs(List<string> adrIDs)
            {
                var vhs = eqObjCacheManager.getAllVehicle();
                return vhs.
                       Where(vh => adrIDs.Contains(SCUtility.Trim(vh.CUR_ADR_ID, true))).
                       ToList();
            }

            public UInt16 getVhCurrentModeInAutoRemoteCount()
            {
                var vhs = eqObjCacheManager.getAllVehicle();
                return (UInt16)vhs.
                       Where(vh => vh.MODE_STATUS == VHModeStatus.AutoRemote &&
                                   vh.isTcpIpConnect).
                       Count();
            }
            public UInt16 getVhCurrentModeInAutoLocalCount()
            {
                var vhs = eqObjCacheManager.getAllVehicle();
                return (UInt16)vhs.
                       Where(vh => vh.MODE_STATUS == VHModeStatus.AutoLocal ||
                                   vh.MODE_STATUS == VHModeStatus.AutoCharging).
                       Count();
            }
            public UInt16 getVhCurrentStatusInIdleCount()
            {
                var vhs = eqObjCacheManager.getAllVehicle();
                return (UInt16)vhs.
                       Where(vh => vh.MODE_STATUS == VHModeStatus.AutoRemote &&
                                   vh.ACT_STATUS == VHActionStatus.NoCommand &&
                                   vh.isTcpIpConnect &&
                                   !vh.IsError).
                       Count();
            }
            public UInt16 getVhCurrentStatusInIdleCount(CMDBLL cmdBll)
            {
                var vhs = eqObjCacheManager.getAllVehicle();
                return (UInt16)vhs.
                       Where(vh => vh.isTcpIpConnect &&
                                   vh.MODE_STATUS == VHModeStatus.AutoRemote &&
                                   vh.ACT_STATUS == VHActionStatus.NoCommand &&
                                   !vh.IsError &&
                                   !cmdBll.isCMD_OHTCExcuteByVh(vh.VEHICLE_ID)).
                       Count();
            }
            public UInt16 getVhCurrentStatusInErrorCount()
            {
                var vhs = eqObjCacheManager.getAllVehicle();
                return (UInt16)vhs.
                       Where(vh => vh.ERROR == VhStopSingle.StopSingleOn).
                       Count();
            }



        }



    }
}
