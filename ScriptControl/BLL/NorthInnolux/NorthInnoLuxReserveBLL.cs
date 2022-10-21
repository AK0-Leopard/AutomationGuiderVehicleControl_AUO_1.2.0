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
using System.Collections.Specialized;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace com.mirle.ibg3k0.sc.BLL
{
    public class NorthInnoLuxReserveBLL : ReserveBLL
    {
        NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private Mirle.Hlts.ReserveSection.Map.ViewModels.HltMapViewModel mapAPI { get; set; }
        private SCApplication scApp;

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
            scApp = _app;
            mapAPI = _app.getReserveSectionAPI();

            mapAPI.HltReservedSections.CollectionChanged += onCollecionChangedEvent;
            mapAPI.HltVehicles.CollectionChanged += onCollecionChangedEvent;
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
            var hlt_vh = new HltVehicle(vhID, vehicleX, vehicleY, vehicleAngle, speedMmPerSecond, sensorDirection: sensorDir, forkDirection: forkDir, currentSectionID: currentSectionID);
            HltResult result = mapAPI.TryAddOrUpdateVehicle(hlt_vh);
            mapAPI.KeepRestSection(hlt_vh);
            onReserveStatusChange();

            return result;
        }
        public override HltResult TryAddVehicleOrUpdate(string vhID, string adrID, float angle = 0, Mirle.Hlts.Utils.HltDirection direction = HltDirection.FRLR)
        {
            var adr_obj = mapAPI.GetAddressObjectByID(adrID);
            var hlt_vh = new HltVehicle(vhID, adr_obj.X, adr_obj.Y, angle, sensorDirection: direction);
            //HltResult result = mapAPI.TryAddVehicleOrUpdate(vhID, adr_obj.X, adr_obj.Y, 0, vehicleSensorDirection: Mirle.Hlts.Utils.HltDirection.NESW);
            HltResult result = mapAPI.TryAddOrUpdateVehicle(hlt_vh);
            onReserveStatusChange();

            return result;
        }

        public override HltResult TryAddVehicleOrUpdate(HltVehicle hltVehicle)
        {
            HltResult result = mapAPI.TryAddOrUpdateVehicle(hltVehicle);
            onReserveStatusChange();

            return result;
        }

        public override void RemoveManyReservedSectionsByVIDSID(string vhID, string sectionID)
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
        public override (bool isExist, HltMapSection section) GetHltMapSections(string secID)
        {
            var sec_obj = mapAPI.HltMapSections.Where(sec => SCUtility.isMatche(sec.ID, secID)).FirstOrDefault();
            return (sec_obj != null, sec_obj);
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
            var block_control_check_result = scApp.getCommObjCacheManager().IsBlockControlSection(reserveSectionID);
            if (block_control_check_result.isBlockControlSec)
            {
                List<string> reserve_enhance_sections = block_control_check_result.enhanceInfo.EnhanceControlSections.ToList();
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(ReserveBLL), Device: "AGV",
                   Data: $"reserve section:{reserveSectionID} is reserve enhance section, group:{string.Join(",", reserve_enhance_sections)}",
                   VehicleID: vh_id);
                //用來判斷是否車子是否有在Block裡面，決定是否要直接讓車子通過
                bool can_direct_pass = true;
                var related_sec = scApp.SectionBLL.cache.GetSectionsByAddress(vh.CUR_ADR_ID);
                foreach (var sec in related_sec)
                {
                    var current_vh_section_is_in_req_block_control_check_result =
                        scApp.getCommObjCacheManager().IsBlockControlSection(block_control_check_result.enhanceInfo.BlockID, sec.SEC_ID);
                    if (!current_vh_section_is_in_req_block_control_check_result.isBlockControlSec)
                    {
                        can_direct_pass = false;
                        break;
                    }
                }
                if (can_direct_pass)
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(ReserveBLL), Device: "AGV",
                        Data: $"reserve section:{reserveSectionID} is reserve enhance section, vh:{vh.VEHICLE_ID} is in here, direct pass.",
                        VehicleID: vh_id);
                    return (true, "");
                }

                foreach (var enhance_section in reserve_enhance_sections)
                {
                    var check_one_direct_result = scApp.ReserveBLL.TryAddReservedSection(vh_id, enhance_section,
                        sensorDir: HltDirection.Forward,
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

                //2022.7.12 北群創不允許部分放行，不是全程都可以預約就要算成全部不過
                var reserve_success_section = new RepeatedField<ReserveInfo>();
                bool has_success = true;
                string final_blocked_vh_id = string.Empty;
                string reserve_fail_section = "";
                bool isFirst = true;

                HltVehicle hltvh = mapAPI.HltVehicles.Where(v => SCUtility.isMatche(v.ID, vhID)).SingleOrDefault();
                double originalSpeed = hltvh.SpeedMmPerSecond;
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
                        has_success &= false;
                        final_blocked_vh_id = reserve_enhance_check_result.reservedVhID;
                        reserve_fail_section = reserve_section_id;

                        break;
                    }

                    //2021.08.26 Hsinyu Chang: 預約每個section之前，都要先更新AGV車的行進方向(forward/reverse)
                    double newSpeed = hltvh.SpeedMmPerSecond;
                    newSpeed = (reserve_info.DriveDirction == DriveDirction.DriveDirReverse) ? -Math.Abs(newSpeed) : Math.Abs(newSpeed);
                    hltvh.SpeedMmPerSecond = newSpeed;
                    mapAPI.TryAddOrUpdateVehicle(hltvh);
                    Mirle.Hlts.Utils.HltDirection hltDirection = Mirle.Hlts.Utils.HltDirection.Forward;
                    //Mirle.Hlts.Utils.HltDirection hltDirection = decideReserveDirection(vh, reserve_section_id);
                    //AVEHICLE vh = scApp.VehicleBLL.cache.getVehicle(vhID);
                    //Mirle.Hlts.Utils.HltDirection hltDirection = scApp.ReserveBLL.DecideReserveDirection(scApp.SectionBLL, vh, reserve_section_id);
                    result = TryAddReservedSectionNew(vhID, reserve_section_id,
                                                   sensorDir: hltDirection,
                                                   isAsk: isAsk);
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(ReserveBLL), Device: "AGV",
                        Data: $"vh:{vhID} Try add(Only ask:{isAsk}) reserve section:{reserve_section_id},hlt dir:{hltDirection},result:{result.Description}...",
                        VehicleID: vhID);

                    bool already_pass = false;
                    if (!result.OK)
                    {
                        AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vhID);

                        if (vh != null && checkIsForcePassFirstSectionReserve(vh, reserve_section_id))
                        {
                            already_pass = true;
                        }
                        else if (vh != null && isFirst)
                        {
                            if (!vh.IsOnAdr && SCUtility.isMatche(vh.CUR_SEC_ID, reserve_section_id))
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

                    isFirst = false;

                    if (result.OK || already_pass)
                    {
                        reserve_success_section.Add(reserve_info);
                        has_success &= true;
                    }
                    else
                    {
                        has_success &= false;
                        final_blocked_vh_id = result.VehicleID;
                        reserve_fail_section = reserve_section_id;
                        //中途失敗，放掉所有這次已預約成功的路段
                        foreach (var giveupSection in reserve_success_section)
                        {
                            mapAPI.RemoveManyReservedSectionsByVIDSID(hltvh.ID, giveupSection.ReserveSectionID);
                        }
                        break;
                    }
                }
                hltvh.SpeedMmPerSecond = originalSpeed;
                mapAPI.TryAddOrUpdateVehicle(hltvh);

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

        private bool checkIsForcePassFirstSectionReserve(AVEHICLE vh, string reserveSectionID)
        {
            //LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(ReserveBLL), Device: "AGV",
            //    Data: $"vh:{vh.VEHICLE_ID}的起步section:{vh.FirstPredictSection}",
            //    VehicleID: vh.VEHICLE_ID);
            //if (SCUtility.isMatche(vh.FirstPredictSection, reserveSectionID))
            //{
            //    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(ReserveBLL), Device: "AGV",
            //       Data: $"由於 vh:{vh.VEHICLE_ID} 位於GuideSection的起步section:{vh.FirstPredictSection}，因此強制放行.",
            //       VehicleID: vh.VEHICLE_ID);
            //    return true;
            //}
            //return false;
            try
            {
                var vh_guide_info = vh.tryGetCurrentGuideSection();
                if (!vh_guide_info.hasInfo)
                    return false;
                List<string> current_guide_section = vh_guide_info.currentGuideSection.ToList();
                if (current_guide_section.Count < 2)
                {
                    return false;
                }
                string first_section = current_guide_section.FirstOrDefault();
                if (!SCUtility.isMatche(first_section, reserveSectionID))
                {
                    return false;
                }
                string secend_section = current_guide_section[1];
                var get_cross_address_result = tryFindCrossAddressID(first_section, secend_section);
                if (!get_cross_address_result.hasFind)
                {
                    return false;
                }
                string current_adr_id = vh.CUR_ADR_ID;
                if (SCUtility.isMatche(get_cross_address_result.adrID, current_adr_id))
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(NorthInnoLuxReserveBLL), Device: "AGV",
                       Data: $"由於 vh:{vh.VEHICLE_ID} 位於address:{vh.CUR_ADR_ID}為 Guide Section的第一段:{first_section}於第二段:{secend_section}的交接口，因此強制放行.",
                       VehicleID: vh.VEHICLE_ID);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
                return false;
            }
        }
        private (bool hasFind, string adrID) tryFindCrossAddressID(string secID1, string secID2)
        {
            ASECTION sec1 = scApp.SectionBLL.cache.GetSection(secID1);
            ASECTION sec2 = scApp.SectionBLL.cache.GetSection(secID2);
            if (sec1 == null || sec2 == null)
            {
                return (false, "");
            }
            List<string> address_temp = new List<string>();
            address_temp.Add(sec1.FROM_ADR_ID);
            address_temp.Add(sec1.TO_ADR_ID);
            if (address_temp.Contains(sec2.FROM_ADR_ID))
                return (true, sec2.FROM_ADR_ID);
            if (address_temp.Contains(sec2.TO_ADR_ID))
                return (true, sec2.TO_ADR_ID);
            return (false, "");
        }

        public override HltDirection DecideReserveDirection(SectionBLL sectionBLL, AVEHICLE reserveVh, string reserveSectionID)
        {
            ////先取得目前vh所在的current adr，如果這次要求的Reserve Sec是該Current address連接的其中一點時
            ////就不用增加Secsor預約的範圍，預防發生車子預約不到本身路段的問題
            //string cur_adr = reserveVh.CUR_ADR_ID;
            //var related_sections_id = sectionBLL.cache.GetSectionsByAddress(cur_adr).Select(sec => sec.SEC_ID.Trim()).ToList();
            //if (related_sections_id.Contains(reserveSectionID))
            //{
            //    return Mirle.Hlts.Utils.HltDirection.None;
            //}
            //else
            //{
            //    //在R2000的路段上，預約方向要帶入
            //    if (IsR2000Section(reserveSectionID))
            //    {
            //        return Mirle.Hlts.Utils.HltDirection.NorthSouth;
            //    }
            //    else
            //    {
            //        return Mirle.Hlts.Utils.HltDirection.NESW;
            //    }
            //}
            return HltDirection.Forward;
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


        class ReservedVehicleData
        {
            //public string VehicleID { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double Angle { get; set; }
            public List<string> Sections { get; set; }
        }

        protected void onCollecionChangedEvent(object sender, NotifyCollectionChangedEventArgs e)
        {
            Dictionary<string, ReservedVehicleData> reservedSections = new Dictionary<string, ReservedVehicleData>();
            foreach (var vehicle in mapAPI.HltVehicles)
            {
                var vehicleData = new ReservedVehicleData()
                {
                    X = vehicle.X,
                    Y = vehicle.Y,
                    Angle = vehicle.Angle,
                    Sections = new List<string>()
                };
                reservedSections.Add(vehicle.ID, vehicleData);
            }
            foreach (HltReservedSection result in mapAPI.HltReservedSections)
            {
                reservedSections[result.RSVehicleID].Sections.Add(result.RSMapSectionID);
            }

            Task.Run(() =>
            {
                JObject logEntry = new JObject();
                logEntry.Add("ReportTime", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz", CultureInfo.InvariantCulture));
                foreach (var reserveData in reservedSections)
                {
                    JObject logData = new JObject();
                    logData.Add("VehicleX", reserveData.Value.X);
                    logData.Add("VehicleY", reserveData.Value.Y);
                    logData.Add("VehicleTheta", reserveData.Value.Angle);
                    logData.Add("ReservedSection", new JArray(reserveData.Value.Sections.ToArray()));
                    logEntry.Add($"{reserveData.Key}", logData);
                }
                logEntry.Add("Index", "ReserveSectionInfo");
                var json = logEntry.ToString(Newtonsoft.Json.Formatting.None);
                LogManager.GetLogger("ReserveSectionInfo").Info(json);
            });
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