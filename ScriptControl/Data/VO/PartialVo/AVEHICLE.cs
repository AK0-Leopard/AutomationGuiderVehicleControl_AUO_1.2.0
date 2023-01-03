using com.mirle.ibg3k0.bcf.App;
using com.mirle.ibg3k0.bcf.Common;
using com.mirle.ibg3k0.bcf.Controller;
using com.mirle.ibg3k0.bcf.Data.TimerAction;
using com.mirle.ibg3k0.bcf.Data.ValueDefMapAction;
using com.mirle.ibg3k0.bcf.Data.VO;
using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.Data.PLC_Functions;
using com.mirle.ibg3k0.sc.Data.ValueDefMapAction;
using com.mirle.ibg3k0.sc.Data.VO;
using com.mirle.ibg3k0.sc.ObjectRelay;
using com.mirle.ibg3k0.sc.ProtocolFormat.OHTMessage;
using Google.Protobuf.Collections;
using Newtonsoft.Json;
using NLog;
using Stateless;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using static com.mirle.ibg3k0.sc.App.SCAppConstants;

namespace com.mirle.ibg3k0.sc
{
    public class GuideInfo
    {
        public class Section
        {
            public string ID;
            public DriveDirction Dir;
            public bool isPass { get; private set; }
            public void setIsPassFlag()
            {
                isPass = true;
            }
            public Section(string _id, DriveDirction _dir)
            {
                ID = _id;
                Dir = _dir;
                isPass = false;
            }
        }
        public GuideInfo(AVEHICLE _vh)
        {
            vh = _vh;
            startToLoadGuideAddresse = new List<string>();
            startToLoadGuideSection = new List<Section>();
            ToDesinationGuideAddresse = new List<string>();
            ToDesinationGuideSection = new List<Section>();
            AvoidGuideAddresse = new List<string>();
            AvoidGuideSection = new List<Section>();
        }
        public void setGuideSection(sc.BLL.ReserveBLL reserveBLL, ID_31_TRANS_REQUEST id_31, ActiveType originalAactiveType)
        {
            startToLoadGuideAddresse = id_31.GuideAddressesStartToLoad.ToList();
            List<string> startToLoadGuideSectionIDs = id_31.GuideSectionsStartToLoad.ToList();
            startToLoadGuideSection = convertGuideSectionIDToObject(reserveBLL, startToLoadGuideSectionIDs, startToLoadGuideAddresse);
            ToDesinationGuideAddresse = id_31.GuideAddressesToDestination.ToList();
            List<string> ToDesinationGuideSectionIDs = id_31.GuideSectionsToDestination.ToList();
            ToDesinationGuideSection = convertGuideSectionIDToObject(reserveBLL, ToDesinationGuideSectionIDs, ToDesinationGuideAddresse);
            isAvoiding = false;
            judgeIsMoveCommand(id_31, originalAactiveType);
        }

        private void judgeIsMoveCommand(ID_31_TRANS_REQUEST id_31, ActiveType originalAactiveType)
        {
            if (id_31.ActType == ActiveType.Override)
            {
                isMove = originalAactiveType == ActiveType.Move ||
                         originalAactiveType == ActiveType.Movetocharger;
            }
            else
            {
                isMove = id_31.ActType == ActiveType.Move ||
                         id_31.ActType == ActiveType.Movetocharger;
            }
        }

        public void setAvoidSection(sc.BLL.ReserveBLL reserveBLL, ID_51_AVOID_REQUEST id_51)
        {
            AvoidGuideAddresse = id_51.GuideAddresses.ToList();
            List<string> avoidGuideSectionIDs = id_51.GuideSections.ToList();
            AvoidGuideSection = convertGuideSectionIDToObject(reserveBLL, avoidGuideSectionIDs, AvoidGuideAddresse);
            isAvoiding = true;
        }
        private List<Section> convertGuideSectionIDToObject(sc.BLL.ReserveBLL reserveBLL, List<string> guideSectionIDs, List<string> guideAddresses)
        {
            string current_guide_Addresses = string.Join(",", guideAddresses);
            List<Section> GuideSections = new List<Section>();
            foreach (string sec_id in guideSectionIDs)
            {
                //var sec_obj = sectinoBLL.cache.GetSection(sec_id);
                var get_result = reserveBLL.GetHltMapSections(sec_id);
                if (!get_result.isExist)
                {
                    continue;
                }
                string from_to_addresses = $"{SCUtility.Trim(get_result.section.StartAddressID)},{SCUtility.Trim(get_result.section.EndAddressID)}";

                DriveDirction dir = current_guide_Addresses.Contains(from_to_addresses) ?
                                DriveDirction.DriveDirForward : DriveDirction.DriveDirReverse;
                Section sec = new Section(sec_id, dir);
                GuideSections.Add(sec);
            }
            return GuideSections;
        }

        public void setAvoidComplete()
        {
            isAvoiding = false;
        }
        public void resetGuideInfo()
        {
            startToLoadGuideAddresse = new List<string>();
            startToLoadGuideSection = new List<Section>();
            ToDesinationGuideAddresse = new List<string>();
            ToDesinationGuideSection = new List<Section>();
            AvoidGuideAddresse = new List<string>();
            AvoidGuideSection = new List<Section>();
            isMove = false;
            isAvoiding = false;
        }

        AVEHICLE vh;
        bool isAvoiding;
        bool isMove;
        List<string> startToLoadGuideAddresse;
        List<Section> startToLoadGuideSection;
        List<string> ToDesinationGuideAddresse;
        List<Section> ToDesinationGuideSection;
        List<string> AvoidGuideAddresse;
        List<Section> AvoidGuideSection;
        public (bool hasInfo, List<string> currentGuideAddresses) tryGetCurrentGuideAddresses()
        {
            if (isAvoiding)
            {
                if (AvoidGuideAddresse != null && AvoidGuideAddresse.Count > 0)
                    return (true, AvoidGuideAddresse.ToList());
                else
                    return (false, null);
            }
            else if (isMove)
            {
                if (ToDesinationGuideAddresse != null && ToDesinationGuideAddresse.Count > 0)
                    return (true, ToDesinationGuideAddresse.ToList());
                else
                    return (false, null);
            }
            else
            {
                if (vh.HAS_CST == 0)
                {
                    if (startToLoadGuideAddresse != null && startToLoadGuideAddresse.Count > 0)
                        return (true, startToLoadGuideAddresse.ToList());
                    else
                        return (false, null);
                }
                else
                {
                    if (ToDesinationGuideAddresse != null && ToDesinationGuideAddresse.Count > 0)
                        return (true, ToDesinationGuideAddresse.ToList());
                    else
                        return (false, null);
                }
            }
        }
        public (bool hasInfo, List<string> currentGuideSection) tryGetCurrentGuideSection()
        {
            var try_get_section_obj_result = tryGetCurrentGuideSectionObj();
            if (try_get_section_obj_result.hasInfo)
            {
                return (true, try_get_section_obj_result.currentGuideSection.Select(sec => sec.ID).ToList());
            }
            else
            {
                return (false, null);
            }

        }
        public (bool hasInfo, List<Section> currentGuideSection) tryGetCurrentGuideSectionObj()
        {
            if (isAvoiding)
            {
                if (AvoidGuideSection != null && AvoidGuideSection.Count > 0)
                    return (true, AvoidGuideSection.ToList());
                else
                    return (false, null);
            }
            else if (isMove)
            {
                if (ToDesinationGuideSection != null && ToDesinationGuideSection.Count > 0)
                    return (true, ToDesinationGuideSection.ToList());
                else
                    return (false, null);
            }
            else
            {
                if (vh.HAS_CST == 0)
                {
                    if (startToLoadGuideSection != null && startToLoadGuideSection.Count > 0)
                        return (true, startToLoadGuideSection.ToList());
                    else
                        return (false, null);
                }
                else
                {
                    if (ToDesinationGuideSection != null && ToDesinationGuideSection.Count > 0)
                        return (true, ToDesinationGuideSection.ToList());
                    else
                        return (false, null);
                }
            }
        }
        public (bool isExist, DriveDirction dir) tryGetWalkDirOnSection(string secID)
        {
            var try_get_current_guide_section_obj_result = tryGetCurrentGuideSectionObj();
            if (!try_get_current_guide_section_obj_result.hasInfo)
                return (false, DriveDirction.DriveDirNone);
            var sec_obj = try_get_current_guide_section_obj_result.currentGuideSection.
                Where(s => SCUtility.isMatche(s.ID, secID)).FirstOrDefault();
            if (sec_obj == null)
            {
                return (false, DriveDirction.DriveDirNone);
            }
            return (true, sec_obj.Dir);
        }
    }

    public class LocationChangeEventArgs : EventArgs
    {
        public string EntrySection;
        public string LeaveSection;
        public LocationChangeEventArgs(string entrySection, string leaveSection)
        {
            EntrySection = entrySection;
            LeaveSection = leaveSection;
        }
    }
    public class SegmentChangeEventArgs : EventArgs
    {
        public string EntrySegment;
        public string LeaveSegment;
        public SegmentChangeEventArgs(string entrySegment, string leaveSegment)
        {
            EntrySegment = entrySegment;
            LeaveSegment = leaveSegment;
        }
    }

    public partial class AVEHICLE : BaseEQObject
    {
        Logger logger = LogManager.GetCurrentClassLogger();
        public const string DEVICE_NAME_AGV = "AGV";
        //public static UInt16 BATTERYLEVELVALUE_FULL { get; private set; } = 98;
        public static UInt16 BATTERYLEVELVALUE_FULL { get; private set; } = 100;
        public static UInt16 BATTERYLEVELVALUE_HIGH { get; private set; } = 80;
        //public static UInt16 BATTERYLEVELVALUE_MIDDLE { get; private set; } = 50;
        //public static UInt16 BATTERYLEVELVALUE_LOW { get; private set; } = 10;
        public static UInt16 BATTERYLEVELVALUE_MIDDLE { get; private set; } = 65;
        //public static UInt16 BATTERYLEVELVALUE_LOW { get; private set; } = 50;
        public static UInt16 BATTERYLEVELVALUE_LOW { get; private set; } = 70;

        /// <summary>
        /// 在一次的Reserve要不到的過程中，最多可以Override失敗的次數
        /// </summary>
        public static UInt16 MAX_FAIL_OVERRIDE_TIMES_IN_ONE_CASE { get; private set; } = 3;
        /// <summary>
        /// 最大允許沒有通訊的時間
        /// </summary>
        public static UInt16 MAX_ALLOW_NO_COMMUNICATION_TIME_SECOND { get; private set; } = 60;
        /// <summary>
        /// 單筆命令，最大允許的搬送時間
        /// </summary>
        public static UInt16 MAX_ALLOW_ACTION_TIME_SECOND { get; private set; } = 1200;
        /// <summary>
        /// 最大允許斷線時間
        /// </summary>
        public static UInt16 MAX_ALLOW_NO_CONNECTION_TIME_SECOND { get; private set; } = 30;
        /// <summary>
        /// Carrier 最大可以Installed在車子的時間
        /// </summary>
        public static UInt16 MAX_ALLOW_CARRIER_INSTALLED_TIME_SECOND { get; private set; } = 1200;
        /// <summary>
        /// 無命令狀態下，Carrier 最大可以Installed在車子的時間
        /// </summary>
        public static UInt16 MAX_ALLOW_NO_COMMAND_CARRIER_INSTALLED_TIME_SECOND { get; private set; } = 30;

        public bool NeedToRetryAfterVehicleAbort = false;
        public bool is_in_retry_process = false;
        public int curCMDRetryCount = 0;
        public long syncUrgentPausePoint = 0;

        public event EventHandler<LocationChangeEventArgs> LocationChange;
        public event EventHandler<SegmentChangeEventArgs> SegmentChange;
        public event EventHandler<CompleteStatus> CommandComplete;
        public event EventHandler<int> BatteryCapacityChange;
        public event EventHandler<BatteryLevel> BatteryLevelChange;
        public event EventHandler LongTimeNoCommuncation;
        public event EventHandler<string> LongTimeInaction;
        public event EventHandler LongTimeDisconnection;
        public event EventHandler<VHModeStatus> ModeStatusChange;
        public event EventHandler<string> LongTimeCarrierInstalled;
        public event EventHandler<string> NoCommandLongTimeCarrierInstalled;

        VehicleTimerAction vehicleTimer = null;

        public VehicleStateMachine vhStateMachine;

        private Stopwatch CurrentCommandExcuteTime;
        private Stopwatch CarrierInstalledTime;
        private Stopwatch CarrierInstalledWithoutCmdTime;


        public void addAttentionReserveSection(ASECTION attentionSection)
        {
            if (attentionSection != null)
                attentionSection.VehicleEntry += AttentionSection_VehicleEntry;
        }
        public void removeAttentionReserveSection(ASECTION attentionSection)
        {
            if (attentionSection != null)
                attentionSection.VehicleEntry -= AttentionSection_VehicleEntry;
        }




        private void AttentionSection_VehicleEntry(object sender, string e)
        {
            try
            {
                //當被Reserve的Section有車子進來時，要判斷是否為自己(AGV)
                ASECTION enrty_section = sender as ASECTION;
                string entry_vh_id = e;
                if (SCUtility.isMatche(VEHICLE_ID, entry_vh_id))
                {
                    //Not thing...
                }
                else
                {
                    //如果不是，則要去Redis查一下是不是真的自己有預約該段Section
                    //如果是自己有預約的，則就要將自己以及進來的AGV下達暫停!
                    SCApplication scApp = SCApplication.getInstance();
                    List<string> reserved_of_section = scApp.AddressesBLL.redis.loadReserveSection(VEHICLE_ID);
                    if (reserved_of_section.Contains(SCUtility.Trim(enrty_section.SEC_ID, true)))
                    {
                        //要開始將自己跟誤闖進來的AGV下暫停直到暫停訊號 = True
                        System.Threading.Tasks.Task.Run(() => scApp.VehicleService.urgentPauseRequest(VEHICLE_ID));
                        System.Threading.Tasks.Task.Run(() => scApp.VehicleService.urgentPauseRequest(entry_vh_id));

                        string message = $"vh:{entry_vh_id} abnormal entry vh:{VEHICLE_ID} reserved section:{enrty_section.SEC_ID}," +
                                         $"start sent pause to AGV";
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AVEHICLE), Device: Service.VehicleService.DEVICE_NAME_AGV,
                           Data: message,
                           VehicleID: VEHICLE_ID);
                        BCFApplication.onErrorMsg(message);
                    }
                    else
                    {
                        //如果自己預約的已經沒有該段Section，則一定是沒有解註冊的路段，
                        //因此在這邊補上反註冊
                        enrty_section.VehicleEntry -= AttentionSection_VehicleEntry;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AVEHICLE), Device: Service.VehicleService.DEVICE_NAME_AGV,
                   Data: ex,
                   VehicleID: e);
            }
        }

        public void onCommandComplete(CompleteStatus cmpStatus)
        {
            CommandComplete?.Invoke(this, cmpStatus);
        }
        public void onLocationChange(string entrySection, string leaveSection)
        {
            LocationChange?.Invoke(this, new LocationChangeEventArgs(entrySection, leaveSection));
        }
        public void onSegmentChange(string entrySegemnt, string leaveSegment)
        {
            SegmentChange?.Invoke(this, new SegmentChangeEventArgs(entrySegemnt, leaveSegment));
        }
        public void onLongTimeNoCommuncation()
        {
            LongTimeNoCommuncation?.Invoke(this, EventArgs.Empty);
        }
        public void onLongTimeInaction(string cmdID)
        {
            LongTimeInaction?.Invoke(this, cmdID);
        }
        public void onLongTimeDisConnection()
        {
            LongTimeDisconnection?.Invoke(this, EventArgs.Empty);
        }
        public void onModeStatusChange(VHModeStatus modeStatus)
        {
            ModeStatusChange?.Invoke(this, modeStatus);
        }
        public void onCarrierLongTimeInstalledInVh(string carrierID)
        {
            LongTimeCarrierInstalled?.Invoke(this, carrierID);
        }
        public void onNoCommandCarrierLongTimeInstalledInVh(string carrierID)
        {
            NoCommandLongTimeCarrierInstalled?.Invoke(this, carrierID);
        }

        public AVEHICLE()
        {
            eqptObjectCate = SCAppConstants.EQPT_OBJECT_CATE_EQPT;
            PositionRefreshTimer.Restart();
            vhStateMachine = new VehicleStateMachine(() => State, (state) => State = state);
            vhStateMachine.OnTransitioned(TransitionedHandler);
            vhStateMachine.OnUnhandledTrigger(UnhandledTriggerHandler);

            CurrentCommandExcuteTime = new Stopwatch();
            CarrierInstalledTime = new Stopwatch();
            CarrierInstalledWithoutCmdTime = new Stopwatch();
            guideInfo = new GuideInfo(this);

        }
        public void TimerActionStart()
        {
            vehicleTimer = new VehicleTimerAction(this, "VehicleTimerAction", 1000);
            startVehicleTimer();
        }
        public void startVehicleTimer()
        {
            vehicleTimer.start();
        }
        public void stopVehicleTimer()
        {
            vehicleTimer.stop();
        }
        public override string ToString()
        {
            string json = JsonConvert.SerializeObject(this);
            return json;
        }
        /// <summary>
        /// 測試使用
        /// </summary>
        [JsonIgnore]
        public virtual Stopwatch sw_speed { get; set; } = new Stopwatch();
        [JsonIgnore]
        public virtual List<string> PredictSegment { get; set; }
        [JsonIgnore]
        public virtual string[] PredictSections { get; set; }
        public virtual List<string> PredictSectionsStartToLoad { get; set; }
        public virtual List<string> PredictSectionsToDesination { get; set; }

        [JsonIgnore]
        public virtual int PrePositionSeqNum { get; set; } = 0;
        [JsonIgnore]
        public virtual string[] PredictAddresses { get; set; }
        [JsonIgnore]
        public virtual string startAdr { get; set; } = string.Empty;
        [JsonIgnore]
        public virtual string FromAdr { get; set; } = string.Empty;
        [JsonIgnore]
        public virtual string ToAdr { get; set; } = string.Empty;
        [JsonIgnore]
        public virtual bool IsOnAdr { get { return SCUtility.isEmpty(CUR_SEC_ID); } }
        [JsonIgnore]
        public virtual APORTSTATION FromPort { get; set; }
        [JsonIgnore]
        public virtual APORTSTATION ToPort { get; set; }
        [JsonIgnore]
        public virtual DriveDirction CurrentDriveDirction { get; set; }
        [JsonIgnore]
        public virtual double Speed { get; set; }
        [JsonIgnore]
        public virtual string ObsVehicleID { get; set; }
        [JsonIgnore]
        public virtual int CurrentFailOverrideTimes { get; set; } = 0;
        [JsonIgnore]
        public virtual double X_Axis { get; set; }
        [JsonIgnore]
        public virtual double Y_Axis { get; set; }
        [JsonIgnore]
        public virtual double DirctionAngle { get; set; }
        [JsonIgnore]
        public virtual double VehicleAngle { get; set; }

        [JsonIgnore]
        public virtual List<string> Alarms { get; set; }
        [JsonIgnore]
        public virtual bool IsPrepareAvoid { get; set; }

        private BatteryLevel batterylevel = BatteryLevel.None;
        [JsonIgnore]
        public virtual BatteryLevel BatteryLevel
        {
            get { return batterylevel; }
            set
            {
                if (batterylevel != value)
                {
                    batterylevel = value;
                    Task.Run(() => BatteryLevelChange?.Invoke(this, batterylevel));
                }
            }
        }
        [JsonIgnore]
        //public virtual int BatteryCapacity
        //{
        //    get { return BATTERYCAPACITY; }
        //    set
        //    {
        //        BATTERYCAPACITY = value;
        //        if (BATTERYCAPACITY >= BATTERYLEVELVALUE_FULL) BatteryLevel = BatteryLevel.Full;
        //        else if (BATTERYCAPACITY > BATTERYLEVELVALUE_HIGH) BatteryLevel = BatteryLevel.High;
        //        else if (BATTERYCAPACITY >= BATTERYLEVELVALUE_MIDDLE) BatteryLevel = BatteryLevel.Middle;
        //        else if (BATTERYCAPACITY <= BATTERYLEVELVALUE_LOW) BatteryLevel = BatteryLevel.Low;
        //        else
        //        {
        //            //如果是介於BATTERYLEVELVALUE_LOW~BATTERYLEVELVALUE_MIDDLE 之間的話，就將他歸類於Middle的電位
        //            BatteryLevel = BatteryLevel.Middle;
        //        }
        //    }
        //}
        public virtual int BatteryCapacity
        {
            get { return BATTERYCAPACITY; }
            set
            {
                //if (BATTERYCAPACITY != value)
                {
                    BATTERYCAPACITY = value;
                    if (BATTERYCAPACITY >= BATTERYLEVELVALUE_FULL) BatteryLevel = BatteryLevel.Full;
                    else if (BATTERYCAPACITY > BATTERYLEVELVALUE_HIGH) BatteryLevel = BatteryLevel.High;
                    else if (BATTERYCAPACITY < BATTERYLEVELVALUE_LOW) BatteryLevel = BatteryLevel.Low;
                    else
                    {
                        //如果是介於BATTERYLEVELVALUE_LOW~BATTERYLEVELVALUE_MIDDLE 之間的話，就將他歸類於Middle的電位
                        BatteryLevel = BatteryLevel.Middle;
                    }
                    Task.Run(() => BatteryCapacityChange?.Invoke(this, BATTERYCAPACITY));
                }
            }
        }

        [JsonIgnore]
        public virtual VhChargeStatus ChargeStatus { get; set; }
        [JsonIgnore]
        public virtual int BatteryTemperature { get; set; }
        [JsonIgnore]
        public virtual ReserveUnsuccessInfo CanNotReserveInfo { get; set; }
        public class ReserveUnsuccessInfo
        {
            public ReserveUnsuccessInfo(string vhID, string adrID, string secID)
            {
                ReservedVhID = vhID;
                ReservedAdrID = SCUtility.Trim(adrID);
                ReservedSectionID = SCUtility.Trim(secID);
            }
            public string ReservedVhID { get; }
            public string ReservedAdrID { get; }
            public string ReservedSectionID { get; }
        }
        [JsonIgnore]
        public virtual AvoidInfo VhAvoidInfo { get; set; }
        public class AvoidInfo
        {
            public string BlockedSectionID { get; }
            public string BlockedVehicleID { get; }
            public List<string> GuideAddresses { get; }
            public AvoidInfo(string blockedSectionID, string blockedVehicleID, List<string> guideAddresses)
            {
                BlockedSectionID = SCUtility.Trim(blockedSectionID, true);
                BlockedVehicleID = SCUtility.Trim(blockedVehicleID, true);
                GuideAddresses = guideAddresses;
            }
        }
        public GuideInfo guideInfo { get; private set; }


        public com.mirle.ibg3k0.sc.ProtocolFormat.OHTMessage.VhStopSingle RESERVE_PAUSE { get; set; }
        [JsonIgnore]
        public virtual bool IsReservePause
        {
            get { return RESERVE_PAUSE == VhStopSingle.StopSingleOn; }
            set { }
        }

        public List<string> CurrentReserveSegmentID = new List<string>();

        //public static void SettingBatteryLevelMiddleBoundary(UInt16 boundaryValue)
        //{
        //    BATTERYLEVELVALUE_MIDDLE = boundaryValue;
        //}
        public static void SettingBatteryLevelHighBoundary(UInt16 boundaryValue)
        {
            BATTERYLEVELVALUE_HIGH = boundaryValue;
        }
        public static void SettingBatteryLevelLowBoundary(UInt16 boundaryValue)
        {
            BATTERYLEVELVALUE_LOW = boundaryValue;
        }

        [JsonIgnore]
        public virtual E_CMD_TYPE CmdType { get; set; } = default(E_CMD_TYPE);

        [JsonIgnore]
        public virtual E_CMD_STATUS vh_CMD_Status { get; set; }

        [JsonIgnore]
        [BaseElement(NonChangeFromOtherVO = true)]
        public object BlockControl_SyncForRedis = new object();

        [JsonIgnore]
        [BaseElement(NonChangeFromOtherVO = true)]
        public object PositionRefresh_Sync = new object();

        [JsonIgnore]
        [BaseElement(NonChangeFromOtherVO = true)]
        public Stopwatch PositionRefreshTimer = new Stopwatch();

        public int Pixel_Loaction_X = 0;
        public int Pixel_Loaction_Y = 0;

        private EventType vhRecentTranEcent = EventType.AdrPass;
        public virtual EventType VhRecentTranEvent
        {
            get { return vhRecentTranEcent; }
            set
            {
                vhRecentTranEcent = value;
                switch (value)
                {
                    case EventType.LoadComplete:
                    case EventType.UnloadComplete:
                    case EventType.LoadArrivals:
                    case EventType.UnloadArrivals:
                    case EventType.AdrOrMoveArrivals:
                    case EventType.Vhloading:
                    case EventType.Vhunloading:
                        sw_speed.Restart();
                        break;

                }
            }
        }

        public BCRReadResult BCRReadResult = BCRReadResult.BcrNormal;
        public VehicleState State = VehicleState.REMOVED;

        private string tcpip_msg_satae;
        [JsonIgnore]
        [BaseElement(NonChangeFromOtherVO = true)]
        public string TcpIp_Msg_State
        {
            get
            {
                return tcpip_msg_satae;
            }
            set
            {
                if (tcpip_msg_satae != value)
                {
                    tcpip_msg_satae = value;
                    OnPropertyChanged(BCFUtility.getPropertyName(() => this.TcpIp_Msg_State));
                }
            }
        }

        private VehicleInfoFromPLC status_info_plc;
        [JsonIgnore]
        [BaseElement(NonChangeFromOtherVO = true)]
        public VehicleInfoFromPLC Status_Info_PLC
        {
            get
            {
                return status_info_plc;
            }
            set
            {
                status_info_plc = value;
                OnPropertyChanged(BCFUtility.getPropertyName(() => this.Status_Info_PLC));
            }
        }

        public class RedundantSectionData
        {
            public List<string> RedundantSectionIds;
            public string WayOutSectionIdForCross;
        }
        public virtual List<string> WillPassSectionID { get; set; }
        public virtual List<string> WillPassAddressID { get; set; }
        //public virtual List<string> RedundantSectionIdForCross { get; set; }
        //public virtual string WayOutSectionIdForCross { get; set; }
        //public bool IsCrossing => !string.IsNullOrEmpty(WayOutSectionIdForCross);
        public virtual List<RedundantSectionData> RedundantSections { get; set; } = new List<RedundantSectionData>();
        public bool IsCrossing => RedundantSections != null && RedundantSections.Count > 0;

        private int procprogress_percen;
        [JsonIgnore]
        [BaseElement(NonChangeFromOtherVO = true)]
        public virtual int procProgress_Percen
        {
            get { return procprogress_percen; }
            set
            {
                if (procprogress_percen != value)
                {
                    procprogress_percen = value;
                    OnPropertyChanged(BCFUtility.getPropertyName(() => this.procProgress_Percen));
                }
            }
        }


        private bool istcpipconnect;
        [BaseElement(NonChangeFromOtherVO = true)]
        public virtual bool isTcpIpConnect
        {
            get { return istcpipconnect; }
            set
            {
                if (istcpipconnect != value)
                {
                    sw_speed.Restart();
                    istcpipconnect = value;
                    OnPropertyChanged(BCFUtility.getPropertyName(() => this.isTcpIpConnect), VEHICLE_ID);
                }
            }
        }

        [BaseElement(NonChangeFromOtherVO = true)]
        public virtual bool isAuto
        {
            get
            {
                return MODE_STATUS == VHModeStatus.AutoCharging ||
                       MODE_STATUS == VHModeStatus.AutoLocal ||
                       MODE_STATUS == VHModeStatus.AutoRemote;
            }
        }

        private bool isloadArrival;
        [BaseElement(NonChangeFromOtherVO = true)]
        public virtual bool isLoadArrival
        {
            get { return isloadArrival; }
            set
            {
                if (isloadArrival != value)
                {
                    isloadArrival = value;
                    OnPropertyChanged(BCFUtility.getPropertyName(() => this.isLoadArrival));
                }
            }
        }

        //public bool IsPresence;
        public Stopwatch watchObstacleTime = new Stopwatch();
        [JsonIgnore]
        [BaseElement(NonChangeFromOtherVO = true)]
        public VhStopSingle ObstacleStatus
        {
            get { return OBS_PAUSE; }
            set
            {
                if (OBS_PAUSE != value)
                {
                    OBS_PAUSE = value;
                    if (OBS_PAUSE == VhStopSingle.StopSingleOn)
                    {
                        watchObstacleTime.Restart();
                    }
                    else
                    {
                        sw_speed.Restart();
                        watchObstacleTime.Stop();
                    }
                    OnPropertyChanged(BCFUtility.getPropertyName(() => this.ObstacleStatus));
                }
            }
        }
        [JsonIgnore]
        public virtual bool IsObstacle
        {
            get { return OBS_PAUSE == VhStopSingle.StopSingleOn; }
            set { }
        }
        public Stopwatch watchBlockTime = new Stopwatch();
        [JsonIgnore]
        [BaseElement(NonChangeFromOtherVO = true)]
        public VhStopSingle BlockingStatus
        {
            get { return BLOCK_PAUSE; }
            set
            {
                if (BLOCK_PAUSE != value)
                {
                    BLOCK_PAUSE = value;
                    if (BLOCK_PAUSE == VhStopSingle.StopSingleOn)
                    {
                        watchBlockTime.Restart();
                    }
                    else
                    {
                        sw_speed.Restart();
                        watchBlockTime.Stop();
                    }
                    OnPropertyChanged(BCFUtility.getPropertyName(() => this.BlockingStatus));
                }
            }
        }
        [JsonIgnore]
        public virtual bool IsBlocking
        {
            get { return BLOCK_PAUSE == VhStopSingle.StopSingleOn; }
            set { }
        }
        public Stopwatch watchPauseTime = new Stopwatch();
        [JsonIgnore]
        [BaseElement(NonChangeFromOtherVO = true)]
        public VhStopSingle PauseStatus
        {
            get { return CMD_PAUSE; }
            set
            {
                if (CMD_PAUSE != value)
                {
                    CMD_PAUSE = value;
                    if (CMD_PAUSE == VhStopSingle.StopSingleOn)
                    {
                        watchPauseTime.Restart();
                    }
                    else
                    {
                        sw_speed.Restart();
                        watchPauseTime.Stop();
                    }
                    OnPropertyChanged(BCFUtility.getPropertyName(() => this.PauseStatus));
                }
            }
        }
        [JsonIgnore]
        public virtual bool IsPause
        {
            get { return CMD_PAUSE == VhStopSingle.StopSingleOn; }
            set { }
        }
        [JsonIgnore]
        public virtual bool IsError
        {
            get { return ERROR == VhStopSingle.StopSingleOn; }
            set { }
        }

        public Stopwatch watchHIDTime = new Stopwatch();
        [JsonIgnore]
        [BaseElement(NonChangeFromOtherVO = true)]
        public VhStopSingle HIDStatus
        {
            get { return HID_PAUSE; }
            set
            {
                if (HID_PAUSE != value)
                {
                    HID_PAUSE = value;
                    if (HID_PAUSE == VhStopSingle.StopSingleOn)
                    {
                        watchHIDTime.Restart();
                    }
                    else
                    {
                        sw_speed.Restart();
                        watchHIDTime.Stop();
                    }
                    OnPropertyChanged(BCFUtility.getPropertyName(() => this.HIDStatus));
                }
            }
        }
        [JsonIgnore]
        public virtual bool IsHIDPause
        {
            get { return HID_PAUSE == VhStopSingle.StopSingleOn; }
            set { }
        }
        public virtual string NODE_ID { get; set; }

        public bool IsOnCharge(BLL.AddressesBLL addressesBLL)
        {
            var address_obj = addressesBLL.cache.GetAddress(CUR_ADR_ID);
            return address_obj is CouplerAddress;
        }
        public bool IsNeedToLongCharge()
        {
            return MODE_STATUS == VHModeStatus.AutoCharging &&
                   LAST_FULLY_CHARGED_TIME.HasValue &&
                   DateTime.Now > LAST_FULLY_CHARGED_TIME?.AddMinutes(SystemParameter.TheLongestFullyChargedIntervalTime_Mim);
        }


        [JsonIgnore]
        public string VhExcuteCMDStatusChangeEvent = "VhExcuteCMDStatusChangeEvent";
        public void NotifyVhExcuteCMDStatusChange()
        {
            OnPropertyChanged(BCFUtility.getPropertyName(() => this.VhExcuteCMDStatusChangeEvent), VEHICLE_ID);
        }
        [JsonIgnore]
        public string VhStatusChangeEvent = "VhStatusChangeEvent";
        public void NotifyVhStatusChange()
        {
            OnPropertyChanged(BCFUtility.getPropertyName(() => this.VhStatusChangeEvent), VEHICLE_ID);
        }
        [JsonIgnore]
        public string VhPositionChangeEvent = "VhPositionChangeEvent";
        public void NotifyVhPositionChange()
        {
            OnPropertyChanged(BCFUtility.getPropertyName(() => this.VhPositionChangeEvent), VEHICLE_ID);
        }

        public void Action()
        {
            CurrentCommandExcuteTime.Restart();
            if (CarrierInstalledWithoutCmdTime.IsRunning)
                CarrierInstalledWithoutCmdTime.Reset();
        }
        public void Stop()
        {
            CurrentCommandExcuteTime.Reset();
            if (CarrierInstalledTime.IsRunning)
                CarrierInstalledWithoutCmdTime.Restart();
        }

        public void CarrierInstall()
        {
            if (!CarrierInstalledTime.IsRunning)
            {
                CarrierInstalledTime.Restart();
                if (!CurrentCommandExcuteTime.IsRunning)
                    CarrierInstalledWithoutCmdTime.Restart();
            }
        }

        public void CarrierRemove()
        {
            if (CarrierInstalledTime.IsRunning)
                CarrierInstalledTime.Reset();
            if (CarrierInstalledWithoutCmdTime.IsRunning)
                CarrierInstalledWithoutCmdTime.Reset();
        }

        public bool send_Str1(ID_1_HOST_BASIC_INFO_VERSION_REP sned_gpp, out ID_101_HOST_BASIC_INFO_VERSION_RESPONSE receive_gpp)
        {
            ValueDefMapActionBase mapAction = null;
            mapAction = getExcuteMapAction();
            return mapAction.sned_Str1(sned_gpp, out receive_gpp);
        }
        public bool sned_S11(ID_11_BASIC_INFO_REP sned_gpp, out ID_111_BASIC_INFO_RESPONSE receive_gpp)
        {
            ValueDefMapActionBase mapAction = null;
            mapAction = getExcuteMapAction();
            return mapAction.sned_Str11(sned_gpp, out receive_gpp);
        }
        public bool sned_S13(ID_13_TAVELLING_DATA_REP sned_gpp, out ID_113_TAVELLING_DATA_RESPONSE receive_gpp)
        {
            ValueDefMapActionBase mapAction = null;
            mapAction = getExcuteMapAction();
            return mapAction.sned_Str13(sned_gpp, out receive_gpp);
        }
        public bool sned_S15(ID_15_SECTION_DATA_REP send_gpp, out ID_115_SECTION_DATA_RESPONSE receive_gpp)
        {
            ValueDefMapActionBase mapAction = null;
            mapAction = getExcuteMapAction();
            return mapAction.sned_Str15(send_gpp, out receive_gpp);
        }
        public bool sned_S17(ID_17_ADDRESS_DATA_REP send_gpp, out ID_117_ADDRESS_DATA_RESPONSE receive_gpp)
        {
            ValueDefMapActionBase mapAction = null;
            mapAction = getExcuteMapAction();
            return mapAction.sned_Str17(send_gpp, out receive_gpp);
        }
        public bool sned_S19(ID_19_SCALE_DATA_REP send_gpp, out ID_119_SCALE_DATA_RESPONSE receive_gpp)
        {
            ValueDefMapActionBase mapAction = null;
            mapAction = getExcuteMapAction();
            return mapAction.sned_Str19(send_gpp, out receive_gpp);
        }

        public bool sned_S21(ID_21_CONTROL_DATA_REP send_gpp, out ID_121_CONTROL_DATA_RESPONSE receive_gpp)
        {
            ValueDefMapActionBase mapAction = null;
            mapAction = getExcuteMapAction();
            return mapAction.sned_Str21(send_gpp, out receive_gpp);
        }
        public bool sned_S23(ID_23_GUIDE_DATA_REP send_gpp, out ID_123_GUIDE_DATA_RESPONSE receive_gpp)
        {
            ValueDefMapActionBase mapAction = null;
            mapAction = getExcuteMapAction();
            return mapAction.sned_Str23(send_gpp, out receive_gpp);
        }


        public bool sned_S61(ID_61_INDIVIDUAL_UPLOAD_REQ send_gpp, out ID_161_INDIVIDUAL_UPLOAD_RESPONSE receive_gpp)
        {
            ValueDefMapActionBase mapAction = null;
            mapAction = getExcuteMapAction();
            return mapAction.sned_Str61(send_gpp, out receive_gpp);
        }
        public bool sned_S63(ID_63_INDIVIDUAL_CHANGE_REQ send_gpp, out ID_163_INDIVIDUAL_CHANGE_RESPONSE receive_gpp)
        {
            ValueDefMapActionBase mapAction = null;
            mapAction = getExcuteMapAction();
            return mapAction.sned_Str63(send_gpp, out receive_gpp);
        }
        public bool sned_S41(ID_41_MODE_CHANGE_REQ send_gpp, out ID_141_MODE_CHANGE_RESPONSE receive_gpp)
        {
            ValueDefMapActionBase mapAction = null;
            mapAction = getExcuteMapAction();
            return mapAction.send_Str41(send_gpp, out receive_gpp);
        }
        public bool send_S43(ID_43_STATUS_REQUEST send_gpp, out ID_143_STATUS_RESPONSE receive_gpp)
        {
            ValueDefMapActionBase mapAction = null;
            mapAction = getExcuteMapAction();
            return mapAction.send_Str43(send_gpp, out receive_gpp);
        }
        public bool sned_S45(ID_45_POWER_OPE_REQ send_gpp, out ID_145_POWER_OPE_RESPONSE receive_gpp)
        {
            ValueDefMapActionBase mapAction = null;
            mapAction = getExcuteMapAction();
            return mapAction.sned_Str45(send_gpp, out receive_gpp);
        }
        public bool sned_S91(ID_91_ALARM_RESET_REQUEST send_gpp, out ID_191_ALARM_RESET_RESPONSE receive_gpp)
        {
            ValueDefMapActionBase mapAction = null;
            mapAction = getExcuteMapAction();
            return mapAction.send_Str91(send_gpp, out receive_gpp);
        }


        public void registeredProcEvent()
        {
            getExcuteMapAction().RegisteredTcpIpProcEvent();
        }
        public void unRegisteredProcEvent()
        {
            getExcuteMapAction().UnRgisteredProcEvent();
        }

        public bool sned_Str31(ID_31_TRANS_REQUEST send_gpp, out ID_131_TRANS_RESPONSE receive_gpp, out string reason)
        {
            ValueDefMapActionBase mapAction = null;
            mapAction = getExcuteMapAction();
            return mapAction.send_Str31(send_gpp, out receive_gpp, out reason);
        }
        public bool sned_Str35(ID_35_CST_ID_RENAME_REQUEST send_gpp, out ID_135_CST_ID_RENAME_RESPONSE receive_gpp)
        {
            ValueDefMapActionBase mapAction = null;
            mapAction = getExcuteMapAction();
            return mapAction.send_Str35(send_gpp, out receive_gpp);
        }

        public (bool isSendOK, int replyCode) sned_Str37(string cmd_id, CMDCancelType actType)
        {
            ValueDefMapActionBase mapAction = null;
            mapAction = getExcuteMapAction();
            return mapAction.send_Str37(cmd_id, actType);
        }
        public bool sned_Str39(ID_39_PAUSE_REQUEST sned_gpp, out ID_139_PAUSE_RESPONSE receive_gpp)
        {
            ValueDefMapActionBase mapAction = null;
            mapAction = getExcuteMapAction();
            return mapAction.send_Str39(sned_gpp, out receive_gpp);
        }
        public void CatchPLCCSTInterfacelog()
        {
            ValueDefMapActionBase mapAction = null;
            mapAction = getExcuteMapAction();
            mapAction.doCatchPLCCSTInterfaceLog();
        }

        public bool send_Str71(ID_71_RANGE_TEACHING_REQUEST send_gpp, out ID_171_RANGE_TEACHING_RESPONSE receive_gpp)
        {
            ValueDefMapActionBase mapAction = null;
            mapAction = getExcuteMapAction();
            return mapAction.send_Str71(send_gpp, out receive_gpp);
        }
        public bool send_Str51(ID_51_AVOID_REQUEST send_gpp, out ID_151_AVOID_RESPONSE receive_gpp)
        {
            ValueDefMapActionBase mapAction = null;
            mapAction = getExcuteMapAction();
            return mapAction.send_Str51(send_gpp, out receive_gpp);
        }

        private ValueDefMapActionBase getExcuteMapAction()
        {
            ValueDefMapActionBase mapAction;
            mapAction = this.getMapActionByIdentityKey(typeof(EQTcpIpMapAction).Name) as EQTcpIpMapAction;
            return mapAction;
        }

        public bool sendMessage(WrapperMessage wrapper, bool isReply = false)
        {
            ValueDefMapActionBase mapAction = null;
            mapAction = getExcuteMapAction();
            return mapAction.sendMessage(wrapper, isReply);
        }


        #region PLC Control
        public void PLC_Control_TrunOn()
        {
            ValueDefMapActionBase mapAction = null;
            mapAction = getExcuteMapAction();
            isPLCInControl = true;
            mapAction.PLC_Control_TrunOn();
        }
        public void PLC_Control_TrunOff()
        {
            ValueDefMapActionBase mapAction = null;
            mapAction = getExcuteMapAction();
            isPLCInControl = false;
            VehicleControlItemForPLC = new Boolean[16];
            mapAction.PLC_Control_TrunOff();
        }

        public bool setVehicleControlItemForPLC(Boolean[] items)
        {
            ValueDefMapActionBase mapAction = null;
            mapAction = getExcuteMapAction();
            VehicleControlItemForPLC = items;
            return mapAction.setVehicleControlItemForPLC(items);
        }

        public bool isPLCInControl { get; private set; }
        public Boolean[] VehicleControlItemForPLC { get; private set; }
        #endregion PLC Control


        #region TcpIpAgentInfo
        int CommunicationInterval_ms = 15000;
        public void getAgentInfo(BCFApplication bcfApp,
            out bool IsCommunication, out bool IsConnections,
            out DateTime connTime, out TimeSpan accConnTime,
            out DateTime disConnTime, out TimeSpan accDisConnTime,
            out int disconnTimes, out int lostPackets)
        {
            Stopwatch fromLastCommTime = ITcpIpControl.StopWatch_FromTheLastCommTime(bcfApp, TcpIpAgentName);
            IsCommunication = fromLastCommTime.IsRunning ?
                fromLastCommTime.ElapsedMilliseconds < CommunicationInterval_ms : false;
            IsConnections = ITcpIpControl.IsConnection(bcfApp, TcpIpAgentName);
            connTime = ITcpIpControl.ConnectionTime(bcfApp, TcpIpAgentName);
            accConnTime = ITcpIpControl.StopWatch_ConnectionTime(bcfApp, TcpIpAgentName).Elapsed;
            disConnTime = ITcpIpControl.DisconnectionTime(bcfApp, TcpIpAgentName);
            accDisConnTime = ITcpIpControl.StopWatch_DisconnectionTime(bcfApp, TcpIpAgentName).Elapsed;
            disconnTimes = ITcpIpControl.DisconnectionTimes(bcfApp, TcpIpAgentName);
            lostPackets = ITcpIpControl.NumberOfPacketsLost(bcfApp, TcpIpAgentName);
        }

        internal string getIPAddress(BCFApplication bcfApp)
        {
            if (SCUtility.isEmpty(TcpIpAgentName))
            {
                return string.Empty;
            }
            return ITcpIpControl.getRemoteIPAddress(bcfApp, TcpIpAgentName);
        }

        internal double getFromTheLastCommTime(BCFApplication bcfApp)
        {
            return ITcpIpControl.StopWatch_FromTheLastCommTime(bcfApp, TcpIpAgentName).Elapsed.TotalSeconds;

        }
        internal double getConnectionIntervalTime(BCFApplication bcfApp)
        {
            return ITcpIpControl.StopWatch_ConnectionTime(bcfApp, TcpIpAgentName).Elapsed.TotalSeconds;
        }
        internal double getDisconnectionIntervalTime(BCFApplication bcfApp)
        {
            return ITcpIpControl.StopWatch_DisconnectionTime(bcfApp, TcpIpAgentName).Elapsed.TotalSeconds;
        }

        #endregion TcpIpAgentInfo

        public void OnBeforeUpdate()
        {
            VehicleObjToShow showObj = SCApplication.getInstance().getEQObjCacheManager().CommonInfo.ObjectToShow_list.
                Where(o => o.VEHICLE_ID == VEHICLE_ID).SingleOrDefault();
            if (showObj != null)
            {
                //showObj.cUR_ADR_ID = CUR_ADR_ID;
                //showObj.cUR_SEC_ID = CUR_SEC_ID;
                showObj.ACC_SEC_DIST = ACC_SEC_DIST;
                showObj.MODE_STATUS = MODE_STATUS;
                showObj.ACT_STATUS = ACT_STATUS;
                showObj.MCS_CMD = MCS_CMD;
                showObj.OHTC_CMD = OHTC_CMD;
                //showObj.bLOCK_PAUSE = BLOCK_PAUSE == ProtocolFormat.OHTMessage.VhStopSingle.StopSingleOn;
                //showObj.cMD_PAUSE = CMD_PAUSE == ProtocolFormat.OHTMessage.VhStopSingle.StopSingleOn;
                //showObj.oBS_PAUSE = OBS_PAUSE == ProtocolFormat.OHTMessage.VhStopSingle.StopSingleOn;
                showObj.BLOCK_PAUSE = BLOCK_PAUSE;
                showObj.CMD_PAUSE = CMD_PAUSE;
                showObj.OBS_PAUSE = OBS_PAUSE;
                showObj.OBS_DIST = OBS_DIST;
                //showObj.hAS_CST = HAS_CST;
                //showObj.cST_ID = CST_ID;
                showObj.UPD_TIME = UPD_TIME;
                showObj.VEHICLE_ACC_DIST = VEHICLE_ACC_DIST;
                //showObj.mANT_ACC_DIST = MANT_ACC_DIST;
                //showObj.mANT_DATE = MANT_DATE;
                //showObj.gRIP_COUNT = GRIP_COUNT;
                //showObj.gRIP_MANT_COUNT = GRIP_MANT_COUNT;
                //showObj.gRIP_MANT_DATE = GRIP_MANT_DATE;
                //showObj.nODE_ADR = NODE_ADR;
                showObj.IS_PARKING = IS_PARKING;
                showObj.PARK_TIME = PARK_TIME;
                //showObj.pACK_ADR_ID = PACK_ADR_ID;
                showObj.IS_CYCLING = IS_CYCLING;
                showObj.CYCLERUN_TIME = CYCLERUN_TIME;
                //showObj.cYCLERUN_ID = CYCLERUN_ID;
            }
        }

        public void OnBeforeInsert()
        {

        }
        public override void doShareMemoryInit(BCFAppConstants.RUN_LEVEL runLevel)
        {
            foreach (IValueDefMapAction action in valueDefMapActionDic.Values)
            {
                action.doShareMemoryInit(runLevel);
            }

        }

        /// <summary>
        /// 由於車子有
        /// 1.舵輪不能轉180度的限制
        /// 2.如果Port口是正對路沖的話，需要先往左或右橫移一下再進入(用來進行與Port的點為校正用)
        /// 因此將會透過該Function，進行Append或者Replace來達到該需求
        /// </summary>
        /// <param name="guideSections"></param>
        //public (List<string> guideSectionIds, List<string> guideAddressIds) CheckTurningAngleHasOver180(sc.BLL.SectionBLL sectionBLL, List<string> guideSections, List<string> guideAddresses)
        //{

        //}
        public (List<string> replacedSegmentIds, List<string> replacedSectionIds, List<string> replacedAddressIds)
            CheckTurningAngleHasOver180(sc.BLL.SegmentBLL segmentBLL, sc.BLL.GuideBLL guideBLL,
            List<string> guideSegment, List<string> guideSection, List<string> guideAddresses)
        {
            List<string> guideSegmentTemp = new List<string>(guideSegment);
            List<string> guideSectionTemp = new List<string>(guideSection);
            List<string> guideAddressesTemp = new List<string>(guideAddresses);
            int steeringWheelAngle = STEERINGWHEELANGLE;
            foreach (string seg_id in guideSegment)
            {
                ASEGMENT seg_obj = segmentBLL.cache.GetSegment(seg_id);

                if (steeringWheelAngle + getTurningAngle(steeringWheelAngle, seg_obj.SEG_RAIL_TYPE) >= 180 ||
                    steeringWheelAngle + getTurningAngle(steeringWheelAngle, seg_obj.SEG_RAIL_TYPE) <= -180)
                {

                    string seg_first_section_id = seg_obj.Sections.First().SEC_ID.Trim();
                    string seg_last_section_id = seg_obj.Sections.Last().SEC_ID.Trim();
                    int first_sec_index = guideSection.IndexOf(seg_first_section_id);
                    int last_sec_index = guideSection.IndexOf(seg_last_section_id);
                    if (first_sec_index < last_sec_index)
                    {
                        seg_first_section_id = guideSection[first_sec_index];
                        seg_last_section_id = guideSection[last_sec_index];
                    }
                    else
                    {
                        seg_first_section_id = guideSection[last_sec_index];
                        seg_last_section_id = guideSection[first_sec_index];
                    }

                    int adr1_index = guideAddresses.IndexOf(seg_obj.FROM_ADR_ID.Trim());
                    int adr2_index = guideAddresses.IndexOf(seg_obj.TO_ADR_ID.Trim());
                    string seg_start_adr_id = string.Empty;
                    string seg_end_adr_id = string.Empty;
                    if (adr1_index < adr2_index)
                    {
                        seg_start_adr_id = guideAddresses[adr1_index];
                        seg_end_adr_id = guideAddresses[adr2_index];
                    }
                    else
                    {
                        seg_start_adr_id = guideAddresses[adr2_index];
                        seg_end_adr_id = guideAddresses[adr1_index];
                    }


                    (List<string> replace_guide_segment_ids, List<string> replace_guide_section_ids, List<string> replace_guide_address_ids) =
                        guideBLL.getReplaceRoad(seg_start_adr_id, seg_end_adr_id, seg_id);
                    //todo 剩下替換的方式
                    ReplaceSegment(guideSegmentTemp, seg_id, replace_guide_segment_ids);
                    ReplaceSection(guideSectionTemp, seg_first_section_id, seg_last_section_id, replace_guide_section_ids);
                    ReplaceAddresses(guideAddressesTemp, seg_start_adr_id, seg_end_adr_id, replace_guide_address_ids);
                    foreach (string new_segment_id in replace_guide_segment_ids)
                    {
                        ASEGMENT new_seg_obj = segmentBLL.cache.GetSegment(new_segment_id);
                        steeringWheelAngle = steeringWheelAngle + getTurningAngle(steeringWheelAngle, new_seg_obj.SEG_RAIL_TYPE);
                    }
                }
                else
                {
                    steeringWheelAngle = steeringWheelAngle + getTurningAngle(steeringWheelAngle, seg_obj.SEG_RAIL_TYPE);
                }
            }
            return (guideSegmentTemp, guideSectionTemp, guideAddressesTemp);
        }

        private void ReplaceSegment(List<string> guideSegmentTemp, string oldSegmentID, List<string> newGuideSegmentIDs)
        {
            //1.先找出要替代的Section位置，並將要替代的道路放在後面，最後在將原本的進行移除。
            int start_segment_index = guideSegmentTemp.IndexOf(oldSegmentID);
            guideSegmentTemp.RemoveAt(start_segment_index);
            guideSegmentTemp.InsertRange(start_segment_index, newGuideSegmentIDs);
        }

        private void ReplaceSection(List<string> guideSectionTemp, string oldStartSectionID, string oldEndSectionIndex, List<string> newGuideSectionIDs)
        {
            //1.先找出要替代的Section位置，並將要替代的道路放在後面，最後在將原本的進行移除。
            int start_section_index = guideSectionTemp.IndexOf(oldStartSectionID);
            int end_section_index = guideSectionTemp.IndexOf(oldEndSectionIndex);
            guideSectionTemp.RemoveRange(start_section_index, end_section_index - start_section_index + 1);
            guideSectionTemp.InsertRange(start_section_index, newGuideSectionIDs);
        }

        private void ReplaceAddresses(List<string> guideAddressesTemp, string oldAdr1, string oldAdr2, List<string> newGuideAddressIDs)
        {
            //1.將原本的頭、尾兩個先去掉(因為原本的就會有)，再將新的Address加入新的路徑中
            int start_address_index = guideAddressesTemp.IndexOf(oldAdr1);
            int end_address_index = guideAddressesTemp.IndexOf(oldAdr2);
            guideAddressesTemp.RemoveRange(start_address_index, end_address_index - start_address_index + 1);
            guideAddressesTemp.InsertRange(start_address_index, newGuideAddressIDs);
        }

        private int getTurningAngle(int currentWheeAngle, SegmentRailType sectionType)
        {
            if (currentWheeAngle == 0)
            {
                switch (sectionType)
                {
                    case SegmentRailType.Curve0To90: return -90;
                    case SegmentRailType.Curve90To180: return 90;
                    case SegmentRailType.Curve180To270: return -90;
                    case SegmentRailType.Curve270To360: return 90;
                    default: return 0;
                }
            }
            else
            {
                switch (sectionType)
                {
                    case SegmentRailType.Curve0To90: return 90;
                    case SegmentRailType.Curve90To180: return -90;
                    case SegmentRailType.Curve180To270: return 90;
                    case SegmentRailType.Curve270To360: return -90;
                    default: return 0;
                }
            }
        }

        public APORTSTATION GetCurrentPortStation(BLL.PortStationBLL portStationBll)
        {
            return portStationBll.OperateCatch.getPortStationByAdrID(CUR_ADR_ID);
        }



        public void CheckSectionEndIsPortStation(string loadOrUnloadAdr, List<ASECTION> guideSections)
        {
            ASECTION last_section = guideSections.Last();
            if (SCUtility.isMatche(last_section.FROM_ADR_ID, loadOrUnloadAdr) &&
                last_section.ISBANEND_To2From)
            {

            }
            else if (SCUtility.isMatche(last_section.TO_ADR_ID, loadOrUnloadAdr) &&
                last_section.ISBANEND_From2To)
            {

            }
        }

        public List<string> GetCurrentGuideSection()
        {
            if (VhAvoidInfo != null)
                return VhAvoidInfo.GuideAddresses;
            else
            {
                if (HAS_CST == 0)
                {
                    if (PredictSectionsStartToLoad != null && PredictSectionsStartToLoad.Count > 0)
                        return PredictSectionsStartToLoad;
                    else
                        return null;
                }
                else
                {
                    if (PredictSectionsToDesination != null && PredictSectionsToDesination.Count > 0)
                        return PredictSectionsToDesination;
                    else
                        return null;
                }
            }
        }


        object guideInfoSetLock = new object();
        public void setVhAvoidComplete()
        {
            lock (guideInfoSetLock)
                guideInfo.setAvoidComplete();
        }
        public void resetVhGuideInfo()
        {
            lock (guideInfoSetLock)
                guideInfo.resetGuideInfo();
        }
        public void setVhGuideInfo(BLL.ReserveBLL reserveBLL, ID_31_TRANS_REQUEST id_31, ActiveType originalAactiveType)
        {
            lock (guideInfoSetLock)
                guideInfo.setGuideSection(reserveBLL, id_31, originalAactiveType);
        }
        public void setVhGuideInfo(BLL.ReserveBLL reserveBLL, ID_51_AVOID_REQUEST id_51)
        {
            lock (guideInfoSetLock)
                guideInfo.setAvoidSection(reserveBLL, id_51);
        }

        public (bool hasInfo, List<string> currentGuideAddresses) tryGetCurrentGuideAddresses()
        {
            return guideInfo.tryGetCurrentGuideAddresses();
        }
        public (bool hasInfo, List<string> currentGuideSection) tryGetCurrentGuideSection()
        {
            return guideInfo.tryGetCurrentGuideSection();
        }
        public (bool isExist, DriveDirction dir) tryGetWalkDirOnSection(string secID)
        {
            return guideInfo.tryGetWalkDirOnSection(secID);
        }


        public void initialParameter()
        {
            this.VEHICLE_ID = null;
            this.VEHICLE_TYPE = default(E_VH_TYPE);
            this.CUR_ADR_ID = null;
            this.CUR_SEC_ID = null;
            this.SEC_ENTRY_TIME = null;
            this.ACC_SEC_DIST = 0;
            this.MODE_STATUS = default(VHModeStatus);
            this.ACT_STATUS = default(VHActionStatus);
            this.MCS_CMD = null;
            this.OHTC_CMD = null;
            this.BLOCK_PAUSE = default(VhStopSingle);
            this.CMD_PAUSE = default(VhStopSingle);
            this.OBS_PAUSE = default(VhStopSingle);
            this.ERROR = default(VhStopSingle);
            this.OBS_DIST = 0;
            this.HAS_CST = 0;
            this.CST_ID = null;
            this.UPD_TIME = null;
            this.VEHICLE_ACC_DIST = 0;
            this.MANT_ACC_DIST = 0;
            this.MANT_DATE = null;
            this.GRIP_COUNT = 0;
            this.GRIP_MANT_COUNT = 0;
            this.GRIP_MANT_DATE = null;
            this.NODE_ADR = null;
            this.IS_PARKING = false;
            this.PARK_TIME = null;
            this.PARK_ADR_ID = null;
            this.IS_CYCLING = false;
            this.CYCLERUN_TIME = null;
            this.CYCLERUN_ID = null;
        }
        [JsonIgnore]
        public override string Version { get { return base.Version; } }
        [JsonIgnore]
        public override string EqptObjectCate { get { return base.EqptObjectCate; } }
        [JsonIgnore]
        [BaseElement(NonChangeFromOtherVO = true)]
        public override string SECSAgentName { get { return base.SECSAgentName; } set { base.SECSAgentName = value; } }
        [JsonIgnore]
        [BaseElement(NonChangeFromOtherVO = true)]
        public override string TcpIpAgentName { get { return base.TcpIpAgentName; } set { base.TcpIpAgentName = value; } }
        //
        // 摘要:
        //     真實的ID
        [JsonIgnore]
        [BaseElement(NonChangeFromOtherVO = true)]
        public override string Real_ID { get; set; }


        void TransitionedHandler(Stateless.StateMachine<VehicleState, VehicleTrigger>.Transition transition)
        {
            string Destination = transition.Destination.ToString();
            string Source = transition.Source.ToString();
            string Trigger = transition.Trigger.ToString();
            string IsReentry = transition.IsReentry.ToString();

            LogHelper.Log(logger: NLog.LogManager.GetCurrentClassLogger(), LogLevel: NLog.LogLevel.Debug, Class: nameof(AVEHICLE), Device: DEVICE_NAME_AGV,
                           Data: $"Vh:{VEHICLE_ID} message state,From:{Source} to:{Destination} by:{Trigger}.IsReentry:{IsReentry}",
                           VehicleID: VEHICLE_ID,
                           CarrierID: CST_ID);
        }

        void UnhandledTriggerHandler(VehicleState state, VehicleTrigger trigger)
        {
            string SourceState = state.ToString();
            string Trigger = trigger.ToString();

            LogHelper.Log(logger: NLog.LogManager.GetCurrentClassLogger(), LogLevel: NLog.LogLevel.Debug, Class: nameof(AVEHICLE), Device: DEVICE_NAME_AGV,
                           Data: $"Vh:{VEHICLE_ID} message state ,unhandled trigger happend ,source state:{SourceState} trigger:{Trigger}",
                           VehicleID: VEHICLE_ID,
                           CarrierID: CST_ID);
        }

        #region Vehicle state machine

        public class VehicleStateMachine : StateMachine<VehicleState, VehicleTrigger>
        {
            public VehicleStateMachine(Func<VehicleState> stateAccessor, Action<VehicleState> stateMutator)
                : base(stateAccessor, stateMutator)
            {
                VehicleStateMachineConfigInitial();
            }
            internal IEnumerable<VehicleTrigger> getPermittedTriggers()//回傳當前狀態可以進行的Trigger，且會檢查GaurdClause。
            {
                return this.PermittedTriggers;
            }


            internal VehicleState getCurrentState()//回傳當前的狀態
            {
                return this.State;
            }
            public List<string> getNextStateStrList()
            {
                List<string> nextStateStrList = new List<string>();
                foreach (VehicleTrigger item in this.PermittedTriggers)
                {
                    nextStateStrList.Add(item.ToString());
                }
                return nextStateStrList;
            }
            private void VehicleStateMachineConfigInitial()
            {
                this.Configure(VehicleState.NOT_ASSIGNED)
                    .PermitIf(VehicleTrigger.VehicleAssign, VehicleState.ASSIGNED, () => VehicleAssignGC())//guardClause為真才會執行狀態變化
                    .PermitIf(VehicleTrigger.VechileRemove, VehicleState.REMOVED, () => VechileRemoveGC());//guardClause為真才會執行狀態變化
                
                this.Configure(VehicleState.ASSIGNED).OnEntry(() => this.Fire(VehicleTrigger.VehicleAssign))
                    .PermitIf(VehicleTrigger.VehicleAssign, VehicleState.ENROUTE)
                    .PermitIf(VehicleTrigger.VehicleUnassign, VehicleState.NOT_ASSIGNED);
                
                this.Configure(VehicleState.ENROUTE).SubstateOf(VehicleState.ASSIGNED)
                    .PermitIf(VehicleTrigger.VehicleArrive, VehicleState.PARKED, () => VehicleArriveGC());//guardClause為真才會執行狀態變化
                                                                                                          //.PermitIf(VehicleTrigger.VehicleUnassign, VehicleState.NOT_ASSIGNED, () => VehicleUnassignGC());//guardClause為真才會執行狀態變化
                
                this.Configure(VehicleState.PARKED).SubstateOf(VehicleState.ASSIGNED)
                    .PermitIf(VehicleTrigger.VehicleDepart, VehicleState.ENROUTE, () => VehicleDepartGC())//guardClause為真才會執行狀態變化
                    .PermitIf(VehicleTrigger.VehicleAcquireStart, VehicleState.ACQUIRING, () => VehicleAcquireStartGC())//guardClause為真才會執行狀態變化
                    .PermitIf(VehicleTrigger.VehicleDepositStart, VehicleState.DEPOSITING, () => VehicleDepositStartGC());//guardClause為真才會執行狀態變化
                
                this.Configure(VehicleState.ACQUIRING).SubstateOf(VehicleState.ASSIGNED)
                    .PermitIf(VehicleTrigger.VehilceAcquireComplete, VehicleState.PARKED, () => VehilceAcquireCompleteGC())//guardClause為真才會執行狀態變化
                    .PermitIf(VehicleTrigger.VehicleDepositStart, VehicleState.DEPOSITING, () => VehicleDepositStartGC())//guardClause為真才會執行狀態變化
                    .PermitIf(VehicleTrigger.VehicleReposition, VehicleState.ENROUTE, () => VehicleRepositionGC());//guardClause為真才會執行狀態變化
                
                this.Configure(VehicleState.DEPOSITING).SubstateOf(VehicleState.ASSIGNED)
                    .PermitIf(VehicleTrigger.VehicleDepositComplete, VehicleState.PARKED, () => VehicleDepositCompleteGC())//guardClause為真才會執行狀態變化
                    .PermitIf(VehicleTrigger.VehicleAcquireStart, VehicleState.ACQUIRING, () => VehicleAcquireStartGC())//guardClause為真才會執行狀態變化
                    .PermitIf(VehicleTrigger.VehicleReposition, VehicleState.ENROUTE, () => VehicleRepositionGC());//guardClause為真才會執行狀態變化
                
                this.Configure(VehicleState.REMOVED)
                    .PermitIf(VehicleTrigger.VehicleInstall, VehicleState.NOT_ASSIGNED, () => VehicleInstallGC());//guardClause為真才會執行狀態變化

            }

            private bool VehicleArriveGC()
            {
                return true;
            }
            private bool VehicleUnassignGC()
            {
                return true;
            }
            private bool VehicleDepartGC()
            {
                return true;
            }
            private bool VehicleAcquireStartGC()
            {
                return true;
            }
            private bool VehicleDepositStartGC()
            {
                return true;
            }
            private bool VehilceAcquireCompleteGC()
            {
                return true;
            }
            private bool VehicleDepositCompleteGC()
            {
                return true;
            }
            private bool VehicleAssignGC()
            {
                return true;
            }
            private bool VechileRemoveGC()
            {
                return true;
            }
            private bool VehicleInstallGC()
            {
                return true;
            }
            private bool VehicleRepositionGC()
            {
                return true;
            }
        }

        public enum VehicleState //有哪些State
        {
            REMOVED = 1,
            NOT_ASSIGNED = 2,
            ENROUTE = 3,
            PARKED = 4,
            ACQUIRING = 5,
            DEPOSITING = 6,
            ASSIGNED = 99
        }

        public enum VehicleTrigger //有哪些Trigger
        {
            VehicleArrive,
            VehicleDepart,
            VehicleAcquireStart,
            VehilceAcquireComplete,
            VehicleDepositStart,
            VehicleDepositComplete,
            VehicleUnassign,
            VehicleAssign,
            VechileRemove,
            VehicleInstall,
            VehicleReposition,  //2022.12.29 for North Innolux
        }
        public bool VehicleArrive()
        {
            try
            {
                if (vhStateMachine.CanFire(VehicleTrigger.VehicleArrive))//檢查當前狀態能否進行這個Trigger
                {
                    vhStateMachine.Fire(VehicleTrigger.VehicleArrive);//進行Trigger

                    //可以在這邊做事情

                    return true;

                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool VehicleDepart()
        {
            try
            {
                if (vhStateMachine.CanFire(VehicleTrigger.VehicleDepart))//檢查當前狀態能否進行這個Trigger
                {
                    vhStateMachine.Fire(VehicleTrigger.VehicleDepart);//進行Trigger

                    //可以在這邊做事情

                    return true;

                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool VehicleAcquireStart()
        {
            try
            {
                if (vhStateMachine.CanFire(VehicleTrigger.VehicleAcquireStart))//檢查當前狀態能否進行這個Trigger
                {
                    vhStateMachine.Fire(VehicleTrigger.VehicleAcquireStart);//進行Trigger

                    //可以在這邊做事情

                    return true;

                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool VehilceAcquireComplete()
        {
            try
            {
                if (vhStateMachine.CanFire(VehicleTrigger.VehilceAcquireComplete))//檢查當前狀態能否進行這個Trigger
                {
                    vhStateMachine.Fire(VehicleTrigger.VehilceAcquireComplete);//進行Trigger

                    //可以在這邊做事情

                    return true;

                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool VehicleDepositStart()
        {
            try
            {
                if (vhStateMachine.CanFire(VehicleTrigger.VehicleDepositStart))//檢查當前狀態能否進行這個Trigger
                {
                    vhStateMachine.Fire(VehicleTrigger.VehicleDepositStart);//進行Trigger

                    //可以在這邊做事情

                    return true;

                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool VehicleDepositComplete()
        {
            try
            {
                if (vhStateMachine.CanFire(VehicleTrigger.VehicleDepositComplete))//檢查當前狀態能否進行這個Trigger
                {
                    vhStateMachine.Fire(VehicleTrigger.VehicleDepositComplete);//進行Trigger

                    //可以在這邊做事情

                    return true;

                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool VehicleUnassign()
        {
            try
            {
                if (vhStateMachine.CanFire(VehicleTrigger.VehicleUnassign))//檢查當前狀態能否進行這個Trigger
                {
                    vhStateMachine.Fire(VehicleTrigger.VehicleUnassign);//進行Trigger

                    //可以在這邊做事情

                    return true;

                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool VehicleAssign()
        {
            try
            {
                if (vhStateMachine.CanFire(VehicleTrigger.VehicleAssign))//檢查當前狀態能否進行這個Trigger
                {
                    vhStateMachine.Fire(VehicleTrigger.VehicleAssign);//進行Trigger

                    //可以在這邊做事情

                    return true;

                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool VechileRemove()
        {
            try
            {
                if (vhStateMachine.CanFire(VehicleTrigger.VechileRemove))//檢查當前狀態能否進行這個Trigger
                {
                    vhStateMachine.Fire(VehicleTrigger.VechileRemove);//進行Trigger

                    //可以在這邊做事情

                    return true;

                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool VehicleInstall()
        {
            try
            {
                if (vhStateMachine.CanFire(VehicleTrigger.VehicleInstall))//檢查當前狀態能否進行這個Trigger
                {
                    vhStateMachine.Fire(VehicleTrigger.VehicleInstall);//進行Trigger

                    //可以在這邊做事情

                    return true;

                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool VehicleReposition()
        {
            try
            {
                if (vhStateMachine.CanFire(VehicleTrigger.VehicleReposition))//檢查當前狀態能否進行這個Trigger
                {
                    vhStateMachine.Fire(VehicleTrigger.VehicleReposition);//進行Trigger
                    //可以在這邊做事情
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        #endregion Vehicle state machine


        public class VehicleTimerAction : ITimerAction
        {
            private static Logger logger = LogManager.GetCurrentClassLogger();
            AVEHICLE vh = null;
            SCApplication scApp = null;
            public VehicleTimerAction(AVEHICLE _vh, string name, long intervalMilliSec)
                : base(name, intervalMilliSec)
            {
                vh = _vh;
            }

            public override void initStart()
            {
                scApp = SCApplication.getInstance();
            }

            private long syncPoint = 0;
            public override void doProcess(object obj)
            {
                if (System.Threading.Interlocked.Exchange(ref syncPoint, 1) == 0)
                {
                    try
                    {
                        //檢查斷線時間是否已經大於容許的最大值
                        double disconnection_time = vh.getDisconnectionIntervalTime(scApp.getBCFApplication());
                        if (disconnection_time > AVEHICLE.MAX_ALLOW_NO_CONNECTION_TIME_SECOND)
                        {
                            vh.onLongTimeDisConnection();
                        }
                        if (!vh.isTcpIpConnect) return;
                        //1.檢查是否已經大於一定時間沒有進行通訊
                        double from_last_comm_time = vh.getFromTheLastCommTime(scApp.getBCFApplication());
                        if (from_last_comm_time > AVEHICLE.MAX_ALLOW_NO_COMMUNICATION_TIME_SECOND)
                        {
                            vh.onLongTimeNoCommuncation();
                        }
                        double action_time = vh.CurrentCommandExcuteTime.Elapsed.TotalSeconds;
                        if (action_time > AVEHICLE.MAX_ALLOW_ACTION_TIME_SECOND)
                        {
                            vh.onLongTimeInaction(vh.OHTC_CMD);
                        }
                        double carrier_installed_time = vh.CarrierInstalledTime.Elapsed.TotalSeconds;
                        if (carrier_installed_time > AVEHICLE.MAX_ALLOW_CARRIER_INSTALLED_TIME_SECOND)
                        {
                            vh.onCarrierLongTimeInstalledInVh(SCUtility.Trim(vh.CST_ID, true));
                        }
                        double carrierInstallTimeWithoutAction = vh.CarrierInstalledWithoutCmdTime.Elapsed.TotalSeconds;
                        if (carrierInstallTimeWithoutAction > AVEHICLE.MAX_ALLOW_NO_COMMAND_CARRIER_INSTALLED_TIME_SECOND)
                        {
                            vh.onCarrierLongTimeInstalledInVh(SCUtility.Trim(vh.CST_ID, true));
                        }
                        //
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(AVEHICLE), Device: "AGVC",
                           Data: ex,
                           VehicleID: vh.VEHICLE_ID,
                           CarrierID: vh.CST_ID);
                    }
                    finally
                    {
                        System.Threading.Interlocked.Exchange(ref syncPoint, 0);
                    }

                }
            }

        }
    }
}
