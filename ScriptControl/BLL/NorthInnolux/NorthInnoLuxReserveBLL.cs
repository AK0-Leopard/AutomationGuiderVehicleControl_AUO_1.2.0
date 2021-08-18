using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Common;
using Mirle.Hlts.Utils;
using System;
using System.Text;
using System.Linq;
using com.mirle.ibg3k0.sc.ProtocolFormat.OHTMessage;
using Google.Protobuf.Collections;
using NLog;
using System.Collections.Generic;

namespace com.mirle.ibg3k0.sc.BLL
{
    public class NorthInnoLuxReserveBLL:ReserveBLL
    {
        NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private Mirle.Hlts.ReserveSection.Map.ViewModels.HltMapViewModel mapAPI { get; set; }

        private EventHandler reserveStatusChange;
        private object _reserveStatusChangeEventLock = new object();
        public override event EventHandler ReserveStatusChange
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

        public NorthInnoLuxReserveBLL()
        {
        }
        public override void start(SCApplication _app)
        {
            mapAPI = _app.getReserveSectionAPI();
        }

        public override bool DrawAllReserveSectionInfo()
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

        public override System.Windows.Media.Imaging.BitmapSource GetCurrentReserveInfoMap()
        {
            return mapAPI.MapBitmapSource;
        }

        public override (double x, double y, bool isTR50) GetHltMapAddress(string adrID)
        {
            var adr_obj = mapAPI.GetAddressObjectByID(adrID);
            return (adr_obj.X, adr_obj.Y, adr_obj.IsTR50);
        }

        public HltResult TryAddVehicleOrUpdate(string vhID, double vehicleX, double vehicleY, float vehicleAngle, HltDirection sensorDir = HltDirection.None, HltDirection forkDir = HltDirection.None)
        {

            //HltResult result = mapAPI.TryAddVehicleOrUpdate(vhID, vehicleX, vehicleY, vehicleAngle, sensorDir, forkDir);
            var hlt_vh = new HltVehicle(vhID, vehicleX, vehicleY, vehicleAngle, sensorDirection: sensorDir, forkDirection: forkDir);
            HltResult result = mapAPI.TryAddOrUpdateVehicle(hlt_vh);
            onReserveStatusChange();

            return result;
        }
        public HltResult TryAddVehicleOrUpdate(string vhID, string adrID)
        {
            var adr_obj = mapAPI.GetAddressObjectByID(adrID);
            var hlt_vh = new HltVehicle(vhID, adr_obj.X, adr_obj.Y, 0, sensorDirection: Mirle.Hlts.Utils.HltDirection.NESW);
            //HltResult result = mapAPI.TryAddVehicleOrUpdate(vhID, adr_obj.X, adr_obj.Y, 0, vehicleSensorDirection: Mirle.Hlts.Utils.HltDirection.NESW);
            HltResult result = mapAPI.TryAddOrUpdateVehicle(hlt_vh);
            onReserveStatusChange();

            return result;
        }

        public override HltResult TryAddVehicleOrUpdateResetSensorForkDir(string vhID)
        {
            var hltvh = mapAPI.HltVehicles.Where(vh => SCUtility.isMatche(vh.ID, vhID)).SingleOrDefault();
            var clone_hltvh = hltvh.DeepClone();
            clone_hltvh.SensorDirection = HltDirection.None;
            clone_hltvh.ForkDirection = HltDirection.None;
            HltResult result = mapAPI.TryAddOrUpdateVehicle(clone_hltvh);
            return result;
        }

        public override HltResult TryAddVehicleOrUpdate(string vhID, string currentSectionID, double vehicleX, double vehicleY, float vehicleAngle, double speedMmPerSecond,
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
        public override HltResult TryAddVehicleOrUpdate(string vhID, string adrID, float angle = 0, Mirle.Hlts.Utils.HltDirection direction = HltDirection.NESW)
        {
            var adr_obj = mapAPI.GetAddressObjectByID(adrID);
            var hlt_vh = new HltVehicle(vhID, adr_obj.X, adr_obj.Y, angle, sensorDirection: direction);
            //HltResult result = mapAPI.TryAddVehicleOrUpdate(vhID, adr_obj.X, adr_obj.Y, 0, vehicleSensorDirection: Mirle.Hlts.Utils.HltDirection.NESW);
            HltResult result = mapAPI.TryAddOrUpdateVehicle(hlt_vh);
            onReserveStatusChange();

            return result;
        }





        public override void RemoveManyReservedSectionsByVIDSID(string vhID, string sectionID)
        {
            //int sec_id = 0;
            //int.TryParse(sectionID, out sec_id);
            string sec_id = SCUtility.Trim(sectionID);
            mapAPI.RemoveManyReservedSectionsByVIDSID(vhID, sec_id);
            onReserveStatusChange();
        }

        public override void RemoveVehicle(string vhID)
        {
            var vh = mapAPI.GetVehicleObjectByID(vhID);
            if (vh != null)
            {
                mapAPI.RemoveVehicle(vh);
            }
        }

        public override string GetCurrentReserveSection()
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
        public override HltVehicle GetHltVehicle(string vhID)
        {
            return mapAPI.GetVehicleObjectByID(vhID);
        }

        //public HltResult TryAddReservedSection(string vhID, string sectionID, HltDirection sensorDir = HltDirection.NESW, HltDirection forkDir = HltDirection.NESW, bool isAsk = false)
        //{
        //    //int sec_id = 0;
        //    //int.TryParse(sectionID, out sec_id);
        //    string sec_id = SCUtility.Trim(sectionID);

        //    HltResult result = mapAPI.TryAddReservedSection(vhID, sec_id, sensorDir, forkDir, isAsk);
        //    onReserveStatusChange();

        //    return result;
        //}

        public override HltResult RemoveAllReservedSectionsBySectionID(string sectionID)
        {
            //int sec_id = 0;
            //int.TryParse(sectionID, out sec_id);
            string sec_id = SCUtility.Trim(sectionID);
            HltResult result = mapAPI.RemoveAllReservedSectionsBySectionID(sec_id);
            onReserveStatusChange();
            return result;

        }

        public override void RemoveAllReservedSectionsByVehicleID(string vhID)
        {
            mapAPI.RemoveAllReservedSectionsByVehicleID(vhID);
            onReserveStatusChange();
        }
        public override void RemoveAllReservedSections()
        {

            mapAPI.RemoveAllReservedSections();
            onReserveStatusChange();
        }

        public ReserveCheckResult TryAddReservedSectionNew(string vhID, string sectionID, HltDirection sensorDir = HltDirection.None, HltDirection forkDir = HltDirection.None, bool isAsk = false)
        {
            //int sec_id = 0;
            //int.TryParse(sectionID, out sec_id);
            string sec_id = SCUtility.Trim(sectionID);

            ReserveCheckResult result = mapAPI.TryAddReservedSection(vhID, sec_id, sensorDir, forkDir, isAsk).ToReserveCheckResult();
            onReserveStatusChange();

            return result;
        }


        public override HltResult TryAddReservedSection(string vhID, string sectionID, HltDirection sensorDir = HltDirection.NESW, HltDirection forkDir = HltDirection.None, bool isAsk = false)
        {
            //int sec_id = 0;
            //int.TryParse(sectionID, out sec_id);
            string sec_id = SCUtility.Trim(sectionID);

            HltResult result = mapAPI.TryAddReservedSection(vhID, sec_id, sensorDir, forkDir, isAsk);
            onReserveStatusChange();

            return result;
        }

        private (bool isSuccess, string reservedVhID) IsReserveBlockSuccess(SCApplication scApp, string vhID, string reserveSectionID)
        {
            AVEHICLE vh = scApp.VehicleBLL.cache.getVehicle(vhID);
            string vh_id = vh.VEHICLE_ID;
            string cur_sec_id = SCUtility.Trim(vh.CUR_SEC_ID, true);
            var block_control_check_result = scApp.getCommObjCacheManager().IsBlockControlSection(reserveSectionID);
            if (block_control_check_result.isBlockControlSec)
            {
                var current_vh_section_is_in_req_block_control_check_result =
                    scApp.getCommObjCacheManager().IsBlockControlSection(block_control_check_result.enhanceInfo.BlockID, cur_sec_id);
                if (current_vh_section_is_in_req_block_control_check_result.isBlockControlSec)
                {
                    return (true, "");
                }

                List<string> reserve_enhance_sections = block_control_check_result.enhanceInfo.EnhanceControlSections.ToList();
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(ReserveBLL), Device: "AGV",
                   Data: $"reserve section:{reserveSectionID} is reserve enhance section, group:{string.Join(",", reserve_enhance_sections)}",
                   VehicleID: vh_id);


                foreach (var enhance_section in reserve_enhance_sections)
                {
                    var check_one_direct_result = TryAddReservedSectionNew(vh_id, enhance_section,
                                                        sensorDir: HltDirection.None,
                                                        isAsk: true);
                    if (!check_one_direct_result.OK)
                    {
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(ReserveBLL), Device: "AGV",
                           Data: $"vh:{vh_id} Try add reserve section:{reserveSectionID} , it is reserve enhance section" +
                                 $"try to reserve section:{reserveSectionID} fail,result:{check_one_direct_result}.",
                           VehicleID: vh_id);
                        return (false, check_one_direct_result.VehicleID);
                    }
                }
            }
            return (true, "");
        }

        public override (bool isSuccess, string reservedVhID, string reservedFailSection, RepeatedField<ReserveInfo> reserveSuccessInfos) IsMultiReserveSuccess
        (SCApplication scApp, string vhID, RepeatedField<ReserveInfo> reserveInfos, bool isAsk = false)
        {
            try
            {

                    if (DebugParameter.isForcedPassReserve)
                    {
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(ReserveBLL), Device: "AGV",
                           Data: "test flag: Force pass reserve is open, will driect reply to vh pass",
                           VehicleID: vhID);
                        return (true, string.Empty, string.Empty, reserveInfos);
                    }
                

                //強制拒絕Reserve的要求
                if (DebugParameter.isForcedRejectReserve)
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(ReserveBLL), Device: "AGV",
                       Data: "test flag: Force reject reserve is open, will driect reply to vh can't pass",
                       VehicleID: vhID);
                    return (false, string.Empty, string.Empty, null);
                }

                if (reserveInfos == null || reserveInfos.Count == 0) return (false, string.Empty, string.Empty, null);

                var reserve_success_section = new RepeatedField<ReserveInfo>();
                bool has_success = false;
                string final_blocked_vh_id = string.Empty;
                string reserve_fail_section = "";
                bool isFirst = true;

                ReserveCheckResult result = default(ReserveCheckResult);
                foreach (var reserve_info in reserveInfos)
                {
                    string reserve_section_id = reserve_info.ReserveSectionID;

                    var reserve_enhance_check_result = IsReserveBlockSuccess(scApp, vhID, reserve_section_id);
                    //var reserve_enhance_check_result = IsReserveBlockSuccessNew(vh, reserve_section_id, drive_dirction);
                    if (!reserve_enhance_check_result.isSuccess)
                    {
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(ReserveBLL), Device: "AGV",
                           Data: $"vh:{vhID} Try add reserve enhance section:{reserve_section_id} fail. reserved vh id:{reserve_enhance_check_result.reservedVhID}",
                           VehicleID: vhID);
                        has_success |= false;
                        final_blocked_vh_id = reserve_enhance_check_result.reservedVhID;
                        reserve_fail_section = reserve_section_id;

                        break;
                    }

                    Mirle.Hlts.Utils.HltDirection hltDirection = Mirle.Hlts.Utils.HltDirection.NS;
                    //Mirle.Hlts.Utils.HltDirection hltDirection = decideReserveDirection(vh, reserve_section_id);
                    //AVEHICLE vh = scApp.VehicleBLL.cache.getVehicle(vhID);
                    //Mirle.Hlts.Utils.HltDirection hltDirection = scApp.ReserveBLL.DecideReserveDirection(scApp.SectionBLL, vh, reserve_section_id);
                    //LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(ReserveBLL), Device: "AGV",
                    //   Data: $"vh:{vhID} Try add(Only ask) reserve section:{reserve_section_id} ,hlt dir:{hltDirection}...",
                    //   VehicleID: vhID);isFirst
                    result = TryAddReservedSectionNew(vhID, reserve_section_id,
                                                   sensorDir: hltDirection,
                                                   isAsk: isAsk);
                    bool already_pass = false;
                    if (!result.OK)
                    {
                        if (isFirst)
                        {
                            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vhID);
                            if(vh!= null)
                            {
                                if (SCUtility.isMatche(vh.CUR_SEC_ID, reserve_section_id))
                                {
                                    if (reserveInfos.Count >= 2)
                                    {
                                        ASECTION sec1 = scApp.MapBLL.getSectiontByID(reserveInfos[0].ReserveSectionID);
                                        ASECTION sec2 = scApp.MapBLL.getSectiontByID(reserveInfos[1].ReserveSectionID);
                                        if (sec1 != null && sec2 != null)
                                        {
                                            if ((SCUtility.isMatche(sec1.FROM_ADR_ID, vh.CUR_ADR_ID) || SCUtility.isMatche(sec1.TO_ADR_ID, vh.CUR_ADR_ID)) &&
                                                (SCUtility.isMatche(sec2.FROM_ADR_ID, vh.CUR_ADR_ID) || SCUtility.isMatche(sec2.TO_ADR_ID, vh.CUR_ADR_ID)))
                                            {
                                                already_pass = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    isFirst = false;

                    if (result.OK|| already_pass)
                    {
                        reserve_success_section.Add(reserve_info);
                        has_success |= true;
                    }
                    else
                    {
                        has_success |= false;
                        final_blocked_vh_id = result.VehicleID;
                        reserve_fail_section = reserve_section_id;
                        break;
                    }
                }

                return (has_success, final_blocked_vh_id, reserve_fail_section, reserve_success_section);
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(ReserveBLL), Device: "AGV",
                   Data: ex,
                   Details: $"process function:{nameof(IsMultiReserveSuccess)} Exception");
                return (false, string.Empty, string.Empty, null);
            }
        }

        public override HltDirection DecideReserveDirection(SectionBLL sectionBLL, AVEHICLE reserveVh, string reserveSectionID)
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
                    return Mirle.Hlts.Utils.HltDirection.NS;
                }
                else
                {
                    return Mirle.Hlts.Utils.HltDirection.NESW;
                }
            }
        }

        public override bool IsR2000Address(string adrID)
        {
            var hlt_r2000_section_objs = mapAPI.HltMapSections.Where(sec => SCUtility.isMatche(sec.Type, HtlSectionType.R2000.ToString())).ToList();
            bool is_r2000_address = hlt_r2000_section_objs.Where(sec => SCUtility.isMatche(sec.StartAddressID, adrID) || SCUtility.isMatche(sec.EndAddressID, adrID))
                                                          .Count() > 0;
            return is_r2000_address;
        }
        public override bool IsR2000Section(string sectionID)
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
    public class ReserveCheckResult
    {
        public static ReserveCheckResult Empty() { return new ReserveCheckResult(); }
        public ReserveCheckResult() { }
        public ReserveCheckResult(bool ok, string vehicleID, string sectionID, string description)
        {
            OK = ok;
            VehicleID = vehicleID;
            SectionID = sectionID;
            Description = description;
        }

        public virtual bool OK { get; } = true;
        public virtual string VehicleID { get; } = "";
        public virtual string SectionID { get; } = "";
        public virtual string Description { get; } = "";
    }
}
public static class HltMapViewModelExtension
{
    public static com.mirle.ibg3k0.sc.BLL.ReserveCheckResult ToReserveCheckResult(this HltResult result)
    {
        return new com.mirle.ibg3k0.sc.BLL.ReserveCheckResult(result.OK, result.VehicleID, result.SectionID, result.Description);
    }
}