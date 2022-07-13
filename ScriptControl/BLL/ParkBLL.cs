using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.Data;
using com.mirle.ibg3k0.sc.Data.DAO;
using com.mirle.ibg3k0.sc.Data.VO;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.mirle.ibg3k0.sc.BLL
{
    public class ParkBLL
    {
        protected ParkZoneTypeDao parkZoneTypeDao = null;
        protected ParkZoneMasterDao parkZoneMasterDao = null;
        protected ParkZoneDetailDao parkZoneDetailDao = null;
        protected Logger logger_ParkBllLog = LogManager.GetLogger("ParkBLL");
        private static Logger logger = LogManager.GetCurrentClassLogger();

        protected SCApplication scApp = null;
        public ParkBLL()
        {

        }
        public void start(SCApplication app)
        {
            scApp = app;
            parkZoneTypeDao = scApp.ParkZoneTypeDao;
            parkZoneMasterDao = scApp.ParkZoneMasterDao;
            parkZoneDetailDao = scApp.ParkZoneDetailDao;
        }

        public List<string> loadAllParkZoneType()
        {
            List<string> parkZoneTypes = null;
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                parkZoneTypes = parkZoneTypeDao.loadAll(con).Select(zone_type => zone_type.PARK_TYPE_ID.Trim()).ToList();
            }

            return parkZoneTypes;
        }

        public void doParkZoneTypeChange(string park_zone_type_id)
        {
            bool isSuccess = false;
            string original_park_zone_type_id = string.Empty;
            string new_park_zone_type_id = string.Empty;
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                APARKZONETYPE using_park_zone_type = parkZoneTypeDao.getUsingParkType(con);
                APARKZONETYPE changed_park_zone_type = parkZoneTypeDao.getByID(con, park_zone_type_id);
                if (using_park_zone_type != null && changed_park_zone_type != null)
                {
                    using_park_zone_type.IS_DEFAULT = 0;
                    changed_park_zone_type.IS_DEFAULT = 1;
                    parkZoneTypeDao.upadate(con);
                    original_park_zone_type_id = using_park_zone_type.PARK_TYPE_ID;
                    new_park_zone_type_id = changed_park_zone_type.PARK_TYPE_ID;
                    isSuccess = true;
                }
            }
            if (isSuccess)
            {
                using (System.Transactions.TransactionScope tx = SCUtility.getTransactionScope())
                {
                    using (DBConnection_EF con = DBConnection_EF.GetUContext())
                    {
                        var original_parkzonedetails = parkZoneDetailDao.loadAllParkAdrByParkTypeID(con, original_park_zone_type_id);
                        var new_parkzonedetails = parkZoneDetailDao.loadAllParkAdrByParkTypeID(con, new_park_zone_type_id);
                        if (original_parkzonedetails != null && new_parkzonedetails != null)
                        {
                            List<string> original_allParkAdr_id = original_parkzonedetails.Select(detail => detail.ADR_ID.Trim()).ToList();
                            List<string> new_allParkAdr_id = new_parkzonedetails.Select(detail => detail.ADR_ID.Trim()).ToList();
                            List<string> unUseParkAdr = original_allParkAdr_id.Except(new_allParkAdr_id).ToList();
                            List<AVEHICLE> vhs = scApp.VehicleDao.loadParkVehicleByParkAdrID(unUseParkAdr);
                            foreach (AVEHICLE vh in vhs)
                            {
                                scApp.ParkBLL.resetParkAdr(vh.PARK_ADR_ID);
                                scApp.VehicleBLL.resetVhIsInPark(vh.VEHICLE_ID);
                            }
                        }
                    }
                }
                setCurrentParkType();
            }

        }

        public Boolean setCurrentParkType()
        {
            bool isSuccess = false;
            APARKZONETYPE parkZoneType = null;
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                parkZoneType = parkZoneTypeDao.getUsingParkType(con);
            }

            if (parkZoneType != null)
            {
                scApp.getEQObjCacheManager().getLine().
                    Currnet_Park_Type = parkZoneType.PARK_TYPE_ID.Trim();
            }
            return isSuccess;
        }
        public APARKZONEDETAIL getParkDetailByAdr(string adr)
        {
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                return parkZoneDetailDao.getByAdrID(con, adr);
            }
        }
        public APARKZONEDETAIL getParkDetailByZoneIDAndPRIO(string zone_id, int prio)
        {
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                return parkZoneDetailDao.getByZoneIDAndPRIO(con, zone_id, prio);
            }
        }
        public APARKZONEDETAIL getParkDetailByParkZoneIDPrioAscAndCanParkingAdr(string parkZoneID)
        {
            using (DBConnection_EF con = new DBConnection_EF())
            {
                return parkZoneDetailDao.getByParkZoneIDPrioAscAndCanParkingAdr(con, parkZoneID);
            }
        }
        public APARKZONEDETAIL getParkDetailByParkZoneIDPrioDes(string parkZoneID)
        {
            using (DBConnection_EF con = new DBConnection_EF())
            {
                return parkZoneDetailDao.getByParkZoneIDPrioDes(con, parkZoneID);
            }
        }
        public APARKZONEMASTER getParkZoneMasterByParkZoneID(string park_zone_id)
        {
            APARKZONEMASTER park_master = null;
            using (DBConnection_EF con = new DBConnection_EF())
            {
                park_master = parkZoneMasterDao.getByID(con, park_zone_id);
            }
            return park_master;
        }

        public APARKZONEMASTER getParkZoneMasterByAdrID(string adr_id)
        {
            APARKZONEMASTER park_master = null;
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                park_master = parkZoneMasterDao.getByParkingAdr(con, adr_id);
            }
            return park_master;
        }

        public enum FindParkResult
        {
            Success,
            HasParkZoneNoFindRoute,
            NoParkZone,
            OrtherError
        }
      

        public bool EnableParkZoneMaster(string park_zone_id, bool enable)
        {
            bool iiSuccess = false;
            APARKZONEMASTER park_master = null;
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                park_master = parkZoneMasterDao.getByID(con, park_zone_id);
                if (park_master != null)
                {
                    park_master.IS_ACTIVE = enable;
                    parkZoneMasterDao.upadate(con, park_master);
                    iiSuccess = true;
                }
            }
            return iiSuccess;
        }


        /// <summary>
        /// //找出目前所有Park Adr停車的 跟 欲前往的方便得知目前為何沒有停車位
        /// </summary>
        /// <param name="con"></param>
        private void ParkAdrInfoTarce()
        {
            //DBConnection_EF con = DBConnection_EF.GetContext();
            List<APARKZONEDETAIL> lstAllParkDetail = null;
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                lstAllParkDetail = parkZoneDetailDao.loadAllParkAdrByParkTypeID(con,
                    scApp.getEQObjCacheManager().getLine().Currnet_Park_Type);
            }
            if (lstAllParkDetail == null)
                return;
            foreach (APARKZONEDETAIL detail in lstAllParkDetail)
            {
                string park_adr = detail.ADR_ID;
                string parkingVhID = detail.CAR_ID ?? string.Empty;
                string onWayVhID = string.Empty;
                AVEHICLE onWayVh = null;
                if (scApp.VehicleBLL.hasVhReserveParkAdr(park_adr, out onWayVh))
                {
                    onWayVhID = onWayVh.VEHICLE_ID;
                }
                string park_warn_info =
                    string.Format("Park adr:[{0}] ,Parking vh id:[{1}] ,On way vh id:[{2}]"
                                    , park_adr
                                    , parkingVhID
                                    , onWayVhID);
                logger_ParkBllLog.Info(park_warn_info);
            }
        }

        public List<APARKZONEMASTER> loadByParkTypeIDAndHasParkSpaceByCount(String _park_type_id, E_VH_TYPE vh_type)
        {
            List<APARKZONEMASTER> rtnParkZoneMaster = new List<APARKZONEMASTER>();
            //DBConnection_EF con = DBConnection_EF.GetContext();
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                List<APARKZONEMASTER> lstParkZoneMasterTemp = parkZoneMasterDao.loadByParkTypeID(con
                         , scApp.getEQObjCacheManager().getLine().Currnet_Park_Type, vh_type);
                foreach (APARKZONEMASTER master in lstParkZoneMasterTemp)
                {

                    int parkCount = parkZoneDetailDao.getCountByParkZoneIDAndVhOnAdrIncludeOnWay(con, master.PARK_ZONE_ID);
                    //int readyComeToVhCountByCMD = 0;
                    //if (scApp.CMDBLL.hasExcuteCMDFromToAdrIsParkInSpecifyPackZoneID
                    //    (master.PACK_ZONE_ID, out readyComeToVhCountByCMD))
                    //{
                    //    parkCount = parkCount + readyComeToVhCountByCMD;
                    //}
                    if (parkCount < master.TOTAL_BORDER)
                        rtnParkZoneMaster.Add(master);
                }
            }
            return rtnParkZoneMaster;
        }

        public List<APARKZONEMASTER> loadByParkTypeIDAndHasParkSpaceByCount(String _park_type_id)
        {
            List<APARKZONEMASTER> rtnParkZoneMaster = new List<APARKZONEMASTER>();
            //DBConnection_EF con = DBConnection_EF.GetContext();
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                List<APARKZONEMASTER> lstParkZoneMasterTemp = parkZoneMasterDao.loadByParkTypeID(con
                         , scApp.getEQObjCacheManager().getLine().Currnet_Park_Type);
                foreach (APARKZONEMASTER master in lstParkZoneMasterTemp)
                {

                    int parkCount = parkZoneDetailDao.getCountByParkZoneIDAndVhOnAdrIncludeOnWay(con, master.PARK_ZONE_ID);
                    //int readyComeToVhCountByCMD = 0;
                    //if (scApp.CMDBLL.hasExcuteCMDFromToAdrIsParkInSpecifyPackZoneID
                    //    (master.PACK_ZONE_ID, out readyComeToVhCountByCMD))
                    //{
                    //    parkCount = parkCount + readyComeToVhCountByCMD;
                    //}
                    if (parkCount < master.TOTAL_BORDER)
                        rtnParkZoneMaster.Add(master);
                }
            }
            return rtnParkZoneMaster;
        }


        public APARKZONEDETAIL findFitParkZoneDetailInParkMater(string zone_master_id)
        {
            //DBConnection_EF con = DBConnection_EF.GetContext();
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                APARKZONEMASTER parkZoneMaster = parkZoneMasterDao.getByID(con, zone_master_id);
                return findFitParkZoneDetailInParkMater(parkZoneMaster);
            }
        }

        private APARKZONEDETAIL findFitParkZoneDetailInParkMater(APARKZONEMASTER zone_master_temp)
        {
            //DBConnection_EF con = DBConnection_EF.GetContext();
            APARKZONEDETAIL bestParkDetailTemp = null;
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {

                switch (zone_master_temp.PARK_TYPE)
                {
                    case E_PARK_TYPE.OrderByAsc:
                        //bestPackDetailTemp = packZoneDetailDao.getByPackZoneIDPrioAscAndCanPackingAdr
                        bestParkDetailTemp = parkZoneDetailDao.getByParkZoneIDPrioDes
                            (con, zone_master_temp.PARK_ZONE_ID);
                        break;
                    case E_PARK_TYPE.OrderByDes:
                        bestParkDetailTemp = parkZoneDetailDao.getByParkZoneIDPrioDes
                            (con, zone_master_temp.PARK_ZONE_ID);
                        break;
                }
            }
            return bestParkDetailTemp;
        }


        public void updateVhEntryParkingAdr(string vh_id, APARKZONEDETAIL parkZoneDetail)
        {
            ALINE line = scApp.getEQObjCacheManager().getLine();
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                updateParkAdrEmpty(vh_id);
                con.APARKZONEDETAIL.Attach(parkZoneDetail);
                parkZoneDetail.CAR_ID = vh_id;
                con.Entry(parkZoneDetail).Property(p => p.CAR_ID).IsModified = true;
                parkZoneDetailDao.update(con, parkZoneDetail);
            }
        }

        //TODO 要改成直接用SQL下達Command
        private void updateParkAdrEmpty(string vh_id)
        {
            //DBConnection_EF con = DBConnection_EF.GetContext();
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                List<APARKZONEDETAIL> lstParkDetail = parkZoneDetailDao.loadAllParkDetailByVeID(con, vh_id);
                if (lstParkDetail != null && lstParkDetail.Count > 0)
                {
                    foreach (APARKZONEDETAIL detail in lstParkDetail)
                    {
                        detail.CAR_ID = string.Empty;
                        parkZoneDetailDao.update(con, detail);
                    }
                }
            }
        }


        public void resetParkAdr(string park_adr)
        {
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                APARKZONEDETAIL parkZoneDetail = parkZoneDetailDao.getByAdrID(con, park_adr);
                if (parkZoneDetail != null)
                {
                    parkZoneDetail.CAR_ID = string.Empty;
                    parkZoneDetailDao.update(con, parkZoneDetail);
                }
            }
        }



        public bool checkParkZoneLowerBorder(out List<APARKZONEMASTER> park_zone_masters)
        {
            bool isEnough = true;
            park_zone_masters = new List<APARKZONEMASTER>();
            List<APARKZONEMASTER> lstMaster = null;
            using (DBConnection_EF con = new DBConnection_EF())
            {
                lstMaster = parkZoneMasterDao.loadByParkTypeID(con
                    , scApp.getEQObjCacheManager().getLine().Currnet_Park_Type);
                if (lstMaster != null)
                {
                    foreach (APARKZONEMASTER master in lstMaster)
                    {
                        //List<APARKZONEDETAIL> lstVhOnAdr = parkZoneDetailDao.
                        //    loadByParkZoneIDAndVhOnAdrIncludeOnWay(con, master.PARK_ZONE_ID);
                        int parkCount = parkZoneDetailDao.
                         getCountByParkZoneIDAndVhOnAdrIncludeOnWay(con, master.PARK_ZONE_ID);
                        if (parkCount < master.LOWER_BORDER)
                        {
                            park_zone_masters.Add(master);
                            isEnough = false;
                            //break;
                        }
                    }
                }
            }
            return isEnough;
        }

        public APARKZONEDETAIL getParkAddress(string adr, E_VH_TYPE park_type)
        {
            APARKZONEDETAIL park_detail = null;
            //DBConnection_EF con = DBConnection_EF.GetContext();
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                park_detail = parkZoneDetailDao.getParkAdrCountByParkTypeAndAdr
                    (con,
                     scApp.getEQObjCacheManager().getLine().Currnet_Park_Type,
                     adr,
                     park_type);
            }
            return park_detail;
        }

        


    }
}
