using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Common;
using Mirle.Hlts.Utils;
using System;
using System.Text;
using System.Linq;
using com.mirle.ibg3k0.sc.ProtocolFormat.OHTMessage;
using Google.Protobuf.Collections;
using Common.Logging;

namespace com.mirle.ibg3k0.sc.BLL
{
    public class ReserveBLL
    {
        NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private Mirle.Hlts.ReserveSection.Map.ViewModels.HltMapViewModel mapAPI { get; set; }

        private EventHandler reserveStatusChange;
        private object _reserveStatusChangeEventLock = new object();
        public virtual event EventHandler ReserveStatusChange
        {
            add
            {
                lock (_reserveStatusChangeEventLock)
                {
                    reserveStatusChange -= value;
                    reserveStatusChange += value;
                }
            }
            remove
            {
                lock (_reserveStatusChangeEventLock)
                {
                    reserveStatusChange -= value;
                }
            }
        }

        private void onReserveStatusChange()
        {
            reserveStatusChange?.Invoke(this, EventArgs.Empty);
        }

        public ReserveBLL()
        {
        }
        public virtual void start(SCApplication _app)
        {
            mapAPI = _app.getReserveSectionAPI();
        }

        public virtual bool DrawAllReserveSectionInfo()
        {
            bool is_success = false;
            try
            {
                mapAPI.RedrawBitmap(false);
                mapAPI.DrawOverlapRR();
                mapAPI.RefreshBitmap();
                mapAPI.ClearOverlapRR();
                is_success = true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
                is_success = false;
            }
            return is_success;
        }

        public virtual (bool isSuccess, string reservedVhID, string reservedFailSection, RepeatedField<ReserveInfo> reserveSuccessInfos) IsMultiReserveSuccess
        (SCApplication scApp, string vhID, RepeatedField<ReserveInfo> reserveInfos, bool isAsk = false)
        {
            return (false, string.Empty, string.Empty, null);
        }

        public virtual System.Windows.Media.Imaging.BitmapSource GetCurrentReserveInfoMap()
        {
            return mapAPI.MapBitmapSource;
        }

        public virtual (double x, double y, bool isTR50) GetHltMapAddress(string adrID)
        {
            var adr_obj = mapAPI.GetAddressObjectByID(adrID);
            return (adr_obj.X, adr_obj.Y, adr_obj.IsTR50);
        }
        public virtual HltResult TryAddVehicleOrUpdateResetSensorForkDir(string vhID)
        {
            var hltvh = mapAPI.HltVehicles.Where(vh => SCUtility.isMatche(vh.ID, vhID)).SingleOrDefault();
            var clone_hltvh = hltvh.DeepClone();
            clone_hltvh.SensorDirection = HltDirection.None;
            clone_hltvh.ForkDirection = HltDirection.None;
            HltResult result = mapAPI.TryAddOrUpdateVehicle(clone_hltvh);
            return result;
        }
        public virtual HltResult TryAddVehicleOrUpdate(string vhID, string currentSectionID, double vehicleX, double vehicleY, float vehicleAngle, double speedMmPerSecond,
                                           HltDirection sensorDir, HltDirection forkDir)
        {
            LogHelper.Log(logger: logger, LogLevel: NLog.LogLevel.Debug, Class: nameof(ReserveBLL), Device: "AGV",
               Data: $"add vh in reserve system: vh:{vhID},x:{vehicleX},y:{vehicleY},angle:{vehicleAngle},speedMmPerSecond:{speedMmPerSecond},sensorDir:{sensorDir},forkDir:{forkDir}",
               VehicleID: vhID);
            //HltResult result = mapAPI.TryAddVehicleOrUpdate(vhID, vehicleX, vehicleY, vehicleAngle, sensorDir, forkDir);
            var hlt_vh = new HltVehicle(vhID, vehicleX, vehicleY, vehicleAngle, speedMmPerSecond, sensorDirection: sensorDir, forkDirection: forkDir);
            HltResult result = mapAPI.TryAddOrUpdateVehicle(hlt_vh);
            //mapAPI.KeepRestSection(hlt_vh, currentSectionID);
            onReserveStatusChange();

            return result;
        }
        public virtual HltResult TryAddVehicleOrUpdate(string vhID, string adrID, float angle = 0, Mirle.Hlts.Utils.HltDirection direction = HltDirection.NESW)
        {
            var adr_obj = mapAPI.GetAddressObjectByID(adrID);
            var hlt_vh = new HltVehicle(vhID, adr_obj.X, adr_obj.Y, angle, sensorDirection: direction);
            //HltResult result = mapAPI.TryAddVehicleOrUpdate(vhID, adr_obj.X, adr_obj.Y, 0, vehicleSensorDirection: Mirle.Hlts.Utils.HltDirection.NESW);
            HltResult result = mapAPI.TryAddOrUpdateVehicle(hlt_vh);
            onReserveStatusChange();

            return result;
        }

        public virtual void RemoveManyReservedSectionsByVIDSID(string vhID, string sectionID)
        {
            //int sec_id = 0;
            //int.TryParse(sectionID, out sec_id);
            string sec_id = SCUtility.Trim(sectionID);
            mapAPI.RemoveManyReservedSectionsByVIDSID(vhID, sec_id);
            LogHelper.Log(logger: logger, LogLevel: NLog.LogLevel.Debug, Class: nameof(ReserveBLL), Device: "AGV",
                Data: $"RemoveManyReservedSectionsByVIDSID. vh:{vhID},section ID:{sec_id}",
                VehicleID: vhID);
            onReserveStatusChange();
        }

        public virtual void RemoveVehicle(string vhID)
        {
            var vh = mapAPI.GetVehicleObjectByID(vhID);
            if (vh != null)
            {
                mapAPI.RemoveVehicle(vh);
            }
        }

        public virtual string GetCurrentReserveSection()
        {
            StringBuilder sb = new StringBuilder();
            var current_reserve_sections = mapAPI.HltReservedSections;
            sb.AppendLine("Current reserve section");
            foreach (var reserve_section in current_reserve_sections)
            {
                sb.AppendLine($"section id:{reserve_section.RSMapSectionID} vh id:{reserve_section.RSVehicleID}");
            }
            return sb.ToString();
        }
        public virtual HltVehicle GetHltVehicle(string vhID)
        {
            return mapAPI.GetVehicleObjectByID(vhID);
        }

        public virtual HltResult TryAddReservedSection(string vhID, string sectionID, HltDirection sensorDir = HltDirection.NESW, HltDirection forkDir = HltDirection.None, bool isAsk = false)
        {
            //int sec_id = 0;
            //int.TryParse(sectionID, out sec_id);
            string sec_id = SCUtility.Trim(sectionID);

            HltResult result = mapAPI.TryAddReservedSection(vhID, sec_id, sensorDir, forkDir, isAsk);
            onReserveStatusChange();

            return result;
        }

        public virtual HltResult RemoveAllReservedSectionsBySectionID(string sectionID)
        {
            //int sec_id = 0;
            //int.TryParse(sectionID, out sec_id);
            string sec_id = SCUtility.Trim(sectionID);
            HltResult result = mapAPI.RemoveAllReservedSectionsBySectionID(sec_id);
            onReserveStatusChange();
            return result;

        }

        public virtual void RemoveAllReservedSectionsByVehicleID(string vhID)
        {
            mapAPI.RemoveAllReservedSectionsByVehicleID(vhID);
            onReserveStatusChange();
        }
        public virtual void RemoveAllReservedSections()
        {
            mapAPI.RemoveAllReservedSections();
            onReserveStatusChange();
        }
        public virtual HltDirection DecideReserveDirection(SectionBLL sectionBLL, AVEHICLE reserveVh, string reserveSectionID)
        {
            //先取得目前vh所在的current adr，如果這次要求的Reserve Sec是該Current address連接的其中一點時
            //就不用增加Secsor預約的範圍，預防發生車子預約不到本身路段的問題
            string cur_adr = reserveVh.CUR_ADR_ID;
            var related_sections_id = sectionBLL.cache.GetSectionsByAddress(cur_adr).Select(sec => sec.SEC_ID.Trim()).ToList();
            if (related_sections_id.Contains(reserveSectionID))
            {
                return Mirle.Hlts.Utils.HltDirection.None;
            }
            else
            {
                //在R2000的路段上，預約方向要帶入
                if (IsR2000Section(reserveSectionID))
                {
                    return Mirle.Hlts.Utils.HltDirection.NorthSouth;
                }
                else
                {
                    return Mirle.Hlts.Utils.HltDirection.NESW;
                }
            }
        }


        public virtual bool IsR2000Address(string adrID)
        {
            var hlt_r2000_section_objs = mapAPI.HltMapSections.Where(sec => SCUtility.isMatche(sec.Type, HtlSectionType.R2000.ToString())).ToList();
            bool is_r2000_address = hlt_r2000_section_objs.Where(sec => SCUtility.isMatche(sec.StartAddressID, adrID) || SCUtility.isMatche(sec.EndAddressID, adrID))
                                                          .Count() > 0;
            return is_r2000_address;
        }
        public virtual bool IsR2000Section(string sectionID)
        {
            var hlt_section_obj = mapAPI.HltMapSections.Where(sec => SCUtility.isMatche(sec.ID, sectionID)).FirstOrDefault();
            return SCUtility.isMatche(hlt_section_obj.Type, HtlSectionType.R2000.ToString());
        }

        enum HtlSectionType
        {
            Horizontal,
            Vertical,
            R2000
        }
    }

    public class ReserveBLLForByPass : ReserveBLL
    {
        public override string GetCurrentReserveSection()
        {
            return "";
        }
        public override HltVehicle GetHltVehicle(string vhID)
        {
            return new HltVehicle();
        }
        public override (double x, double y, bool isTR50) GetHltMapAddress(string adrID)
        {
            return (0, 0, false);
        }
        public override void RemoveAllReservedSections()
        {
            //not thing...
        }
        public override HltResult RemoveAllReservedSectionsBySectionID(string sectionID)
        {
            return new HltResult(true, "By Pass Reserve");
        }
        public override void RemoveAllReservedSectionsByVehicleID(string vhID)
        {
            //not thing...
        }
        public override void RemoveManyReservedSectionsByVIDSID(string vhID, string sectionID)
        {
            //not thing...
        }
        public override void RemoveVehicle(string vhID)
        {
            //not thing...
        }
        public override HltResult TryAddReservedSection(string vhID, string sectionID, HltDirection sensorDir = HltDirection.NESW, HltDirection forkDir = HltDirection.None, bool isAsk = false)
        {
            return new HltResult(true, "By Pass Reserve");
        }
        public override HltResult TryAddVehicleOrUpdate(string vhID, string adrID, float angle = 0, Mirle.Hlts.Utils.HltDirection direction = HltDirection.NESW)
        {
            return new HltResult(true, "By Pass Reserve");
        }
        public override HltResult TryAddVehicleOrUpdate(string vhID, string currentSectionID, double vehicleX, double vehicleY, float vehicleAngle, double speedMmPerSecond, HltDirection sensorDir, HltDirection forkDir)
        {
            return new HltResult(true, "By Pass Reserve");
        }
        public override HltResult TryAddVehicleOrUpdateResetSensorForkDir(string vhID)
        {
            return new HltResult(true, "By Pass Reserve");
        }
        public override HltDirection DecideReserveDirection(SectionBLL sectionBLL, AVEHICLE reserveVh, string reserveSectionID)
        {
            return HltDirection.None;
        }

    }
}