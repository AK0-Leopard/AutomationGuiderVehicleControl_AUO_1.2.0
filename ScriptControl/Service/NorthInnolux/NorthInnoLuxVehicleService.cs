using com.mirle.ibg3k0.bcf.App;
using com.mirle.ibg3k0.bcf.Common;
using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.BLL;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.Data;
using com.mirle.ibg3k0.sc.Data.PLC_Functions;
using com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux;
using com.mirle.ibg3k0.sc.Data.VO;
using com.mirle.ibg3k0.sc.Module;
using com.mirle.ibg3k0.sc.ProtocolFormat.NorthInnolux.Agvmessage;
using com.mirle.ibg3k0.sc.ProtocolFormat.OHTMessage;
using com.mirle.ibg3k0.sc.RouteKit;
using Google.Protobuf.Collections;
using KingAOP;
using Mirle.Hlts.Utils;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using static com.mirle.ibg3k0.sc.App.SCAppConstants;

namespace com.mirle.ibg3k0.sc.Service
{

    public class NorthInnoLuxVehicleService : VehicleService
    {
        public DynamicMetaObject GetMetaObject(Expression parameter)
        {
            return new AspectWeaver(parameter, this);
        }
        public const string DEVICE_NAME_AGV = "AGV";
        Logger logger = LogManager.GetCurrentClassLogger();
        //SCApplication scApp = null;

        public event EventHandler<DeadLockEventArgs> DeadLockProcessFail;
        private Dictionary<string, string> WaitingRetryMCSCMDList { get; set; } = new Dictionary<string, string>();
        private object WaitingRetryMCSCMDListLock = new object();
        public override void addCMDToWaitingRetryMCSCMDList(string vh_id, string cmd)
        {
            lock (WaitingRetryMCSCMDListLock)
            {
                try
                {
                    if (!WaitingRetryMCSCMDList.ContainsKey(vh_id))
                    {
                        WaitingRetryMCSCMDList.Add(vh_id, cmd);
                        //Chris Add Log 20210927
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                        Data: $"Print WaitingRetryMCSCMDList CMD ID:{cmd} ",
                        VehicleID: vh_id);
                    }
                }
                catch(Exception ex)
                {
                    logger.Error(ex, "Exception:");
                }

            }
        }
        public override void removeCMDToWaitingRetryMCSCMDList(string vh_id)
        {
            lock (WaitingRetryMCSCMDListLock)
            {
                try
                {
                    WaitingRetryMCSCMDList.Remove(vh_id);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Exception:");
                }
            }
        }
        public override void CreateCMDFromWaitingRetryMCSCMDList()
        {

            lock (WaitingRetryMCSCMDListLock)
            {
                try
                {
                    if (WaitingRetryMCSCMDList.Count > 0)
                    {
                        List<string> keyList = WaitingRetryMCSCMDList.Keys.ToList();
                        foreach (string k in keyList)
                        {
                            bool isSuccess = scApp.CMDBLL.createWaitingRetryOHTCCmd(k, WaitingRetryMCSCMDList[k]);
                            if (isSuccess)
                            {
                                WaitingRetryMCSCMDList.Remove(k);
                            }
                        }
                    }
                }
                catch(Exception ex)
                {
                    logger.Error(ex, "Exception:");
                }

            }

        }
        public override bool IsVehicleWaitingRetryMCSCMD(string vh_id)
        {
            if (WaitingRetryMCSCMDList.ContainsKey(vh_id.Trim()))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public NorthInnoLuxVehicleService()
        {

        }
        public void Start(SCApplication app)
        {
            scApp = app;

            scApp.VehicleBLL.loadAllAndProcPositionReportFromRedis();
            //SubscriptionPositionChangeEvent();

            List<AVEHICLE> vhs = scApp.getEQObjCacheManager().getAllVehicle();

            foreach (var vh in vhs)
            {
                vh.addEventHandler(nameof(VehicleService), nameof(vh.isTcpIpConnect), PublishVhInfo);
                vh.addEventHandler(nameof(VehicleService), vh.VhPositionChangeEvent, PublishVhInfo);
                vh.addEventHandler(nameof(VehicleService), vh.VhExcuteCMDStatusChangeEvent, PublishVhInfo);
                vh.addEventHandler(nameof(VehicleService), vh.VhStatusChangeEvent, PublishVhInfo);
                vh.addEventHandler(nameof(VehicleService), vh.VhStatusChangeEvent, PublishVhInfo);
                vh.LocationChange += Vh_LocationChange;
                vh.SegmentChange += Vh_SegementChange;
                vh.TimerActionStart();
                vh.LongTimeNoCommuncation += Vh_LongTimeNoCommuncation;
                vh.LongTimeInaction += Vh_LongTimeInaction;
                vh.LongTimeDisconnection += Vh_LongTimeDisconnection;
                vh.ModeStatusChange += Vh_ModeStatusChange;
                vh.LongTimeCarrierInstalled += Vh_LongTimeCarrierInstalled;
                vh.NoCommandLongTimeCarrierInstalled += Vh_NoCommandLongTimeCarrierInstalled;
            }
        }
        private long syncPoint_LongTimeCarrierInstalled = 0;
        private void Vh_LongTimeCarrierInstalled(object sender, string carrierID)
        {
            AVEHICLE vh = sender as AVEHICLE;
            if (vh == null) return;
            if (System.Threading.Interlocked.Exchange(ref syncPoint_LongTimeCarrierInstalled, 1) == 0)
            {

                try
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Data: $"Process vehicle long time inaction, cmd id:{carrierID}",
                       VehicleID: vh.VEHICLE_ID,
                       CarrierID: vh.CST_ID);

                    //當發生
                    //doCommandFigish(vh.VEHICLE_ID, cmdID, CompleteStatus.CmpStatusLongTimeInaction, 0);
                    //要再上報Alamr Rerport給MCS
                    ProcessAlarmReport(null, AlarmBLL.VEHICLE_LONG_TIME_INSTALLED_CARRIER, ErrorStatus.ErrSet, $"vehicle long time installed carrier, carrier id:{carrierID}");

                    BCFApplication.onWarningMsg($"Vehicle:{vh.VEHICLE_ID} long time installed carrier, carrier id:{carrierID}");
                }
                catch (Exception ex)
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Data: ex,
                       VehicleID: vh.VEHICLE_ID,
                       CarrierID: vh.CST_ID);
                }
                finally
                {
                    System.Threading.Interlocked.Exchange(ref syncPoint_LongTimeCarrierInstalled, 0);
                }
            }
        }
        private long syncPoint_NoCmdLongTimeCarrierInstalled = 0;
        private void Vh_NoCommandLongTimeCarrierInstalled(object sender, string carrierID)
        {
            AVEHICLE vh = sender as AVEHICLE;
            if (vh == null) return;
            if (System.Threading.Interlocked.Exchange(ref syncPoint_NoCmdLongTimeCarrierInstalled, 1) == 0)
            {

                try
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Data: $"Process carrier long time inaction, carrier id:{carrierID}",
                       VehicleID: vh.VEHICLE_ID,
                       CarrierID: vh.CST_ID);

                    ProcessAlarmReport(null, AlarmBLL.VEHICLE_LONG_TIME_INSTALLED_CARRIER, ErrorStatus.ErrSet, $"vehicle long time installed carrier without command, carrier id:{carrierID}");

                    BCFApplication.onWarningMsg($"Vehicle:{vh.VEHICLE_ID} long time installed carrier without command, carrier id:{carrierID}");
                }
                catch (Exception ex)
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Data: ex,
                       VehicleID: vh.VEHICLE_ID,
                       CarrierID: vh.CST_ID);
                }
                finally
                {
                    System.Threading.Interlocked.Exchange(ref syncPoint_NoCmdLongTimeCarrierInstalled, 0);
                }
            }
        }
        private void Vh_ModeStatusChange(object sender, VHModeStatus e)
        {
            AVEHICLE vh = sender as AVEHICLE;
            if (vh == null) return;
            try
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: $"Process vehicle mode change ,change to mode status:{e}",
                   VehicleID: vh.VEHICLE_ID,
                   CarrierID: vh.CST_ID);

                //如果他是變成manual mode的話，則需要報告無法服務的Alarm給 MCS
                if (e == VHModeStatus.AutoCharging ||
                    e == VHModeStatus.AutoLocal ||
                    e == VHModeStatus.AutoRemote)
                {
                    //北群創不需要報這個 
                    ProcessAlarmReport(vh, AlarmBLL.VEHICLE_CAN_NOT_SERVICE, ErrorStatus.ErrReset, $"vehicle cannot service");
                }
                else
                {
                    //北群創不需要報這個 
                    if (vh.IS_INSTALLED) ProcessAlarmReport(vh, AlarmBLL.VEHICLE_CAN_NOT_SERVICE, ErrorStatus.ErrSet, $"vehicle cannot service");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: ex,
                   VehicleID: vh.VEHICLE_ID,
                   CarrierID: vh.CST_ID);
            }
        }

        private void Vh_LongTimeDisconnection(object sender, EventArgs e)
        {
            AVEHICLE vh = sender as AVEHICLE;
            if (vh == null) return;
            try
            {
                vh.stopVehicleTimer();
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: $"Process vehicle long time disconnection",
                   VehicleID: vh.VEHICLE_ID,
                   CarrierID: vh.CST_ID);

                //要再上報Alamr Rerport給MCS
                //北群創不需要報這個 
                if (vh.IS_INSTALLED) ProcessAlarmReport(vh, AlarmBLL.VEHICLE_CAN_NOT_SERVICE, ErrorStatus.ErrSet, $"vehicle cannot service");
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: ex,
                   VehicleID: vh.VEHICLE_ID,
                   CarrierID: vh.CST_ID);
            }
        }

        public void onDeadLockProcessFail(AVEHICLE vehicle1, AVEHICLE vehicle2)
        {
            SystemParameter.setAutoOverride(false);
            DeadLockProcessFail?.Invoke(this, new DeadLockEventArgs(vehicle1, vehicle2));
        }


        private long syncPoint_ProcLongTimeInaction = 0;
        private void Vh_LongTimeInaction(object sender, string cmdID)
        {
            AVEHICLE vh = sender as AVEHICLE;
            if (vh == null) return;
            if (System.Threading.Interlocked.Exchange(ref syncPoint_ProcLongTimeInaction, 1) == 0)
            {

                try
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Data: $"Process vehicle long time inaction, cmd id:{cmdID}",
                       VehicleID: vh.VEHICLE_ID,
                       CarrierID: vh.CST_ID);

                    //當發生命令執行過久之後要將該筆命令改成Abormal end，如果該筆命令是MCS的Command則需要將命令上報給MCS作為結束
                    doCommandFigish(vh.VEHICLE_ID, cmdID, CompleteStatus.CmpStatusLongTimeInaction, 0);
                    //要再上報Alamr Rerport給MCS
                    //北群創不需要報這個 ProcessAlarmReport(vh, AlarmBLL.VEHICLE_LONG_TIME_INACTION, ErrorStatus.ErrSet, $"vehicle long time inaction, cmd id:{cmdID}");

                    BCFApplication.onWarningMsg($"vehicle:{vh.VEHICLE_ID} long time inaction, cmd id:{cmdID}");
                }
                catch (Exception ex)
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Data: ex,
                       VehicleID: vh.VEHICLE_ID,
                       CarrierID: vh.CST_ID);
                }
                finally
                {
                    System.Threading.Interlocked.Exchange(ref syncPoint_ProcLongTimeInaction, 0);
                }
            }
        }

        private void Vh_LongTimeNoCommuncation(object sender, EventArgs e)
        {
            AVEHICLE vh = sender as AVEHICLE;
            if (vh == null) return;
            //當發生很久沒有通訊的時候，就會發送143去進行狀態的詢問，確保Control還與Vehicle連線著
            bool is_success = VehicleStatusRequest(vh.VEHICLE_ID);
            if (!is_success)
            {

            }
        }

        private void Vh_LocationChange(object sender, LocationChangeEventArgs e)
        {
            AVEHICLE vh = sender as AVEHICLE;
            ASECTION leave_section = scApp.SectionBLL.cache.GetSection(e.LeaveSection);
            ASECTION entry_section = scApp.SectionBLL.cache.GetSection(e.EntrySection);
            entry_section?.Entry(vh.VEHICLE_ID);
            leave_section?.Leave(vh.VEHICLE_ID);

            vh.removeAttentionReserveSection(leave_section);
            //在一進入已經預約的路段後，就將該預約權釋放。用途是這樣不會去擋到上一段預約的路徑
            //這樣在垂直路段上，會有撞車的疑慮
            //if (entry_section != null)
            //{
            //    scApp.ReserveBLL.RemoveManyReservedSectionsByVIDSID(vh.VEHICLE_ID, entry_section.SEC_ID);
            //    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
            //       Data: $"vh:{vh.VEHICLE_ID} entry section {entry_section.SEC_ID},remove reserved.",
            //       VehicleID: vh.VEHICLE_ID);
            //}
            if (leave_section != null)
            {
                scApp.ReserveBLL.RemoveManyReservedSectionsByVIDSID(vh.VEHICLE_ID, leave_section.SEC_ID);
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: $"vh:{vh.VEHICLE_ID} leave section {entry_section.SEC_ID},remove reserved.",
                   VehicleID: vh.VEHICLE_ID);
            }
        }

        private void Vh_SegementChange(object sender, SegmentChangeEventArgs e)
        {
            AVEHICLE vh = sender as AVEHICLE;
            ASEGMENT leave_section = scApp.SegmentBLL.cache.GetSegment(e.LeaveSegment);
            ASEGMENT entry_section = scApp.SegmentBLL.cache.GetSegment(e.EntrySegment);
            //if (leave_section != null && entry_section != null)
            //{
            //    AADDRESS release_adr = FindReleaseAddress(leave_section, entry_section);
            //    release_adr?.Release(vh.VEHICLE_ID);
            //}
        }

        private AADDRESS FindReleaseAddress(ASECTION leave_section, ASECTION entry_section)
        {
            string leave_sec_from_adr = leave_section.FROM_ADR_ID;
            string leave_sec_to_adr = leave_section.TO_ADR_ID;
            string entry_sec_from_adr = entry_section.FROM_ADR_ID;
            string entry_sec_to_adr = entry_section.TO_ADR_ID;

            AADDRESS release_adr = null;
            if (SCUtility.isMatche(leave_sec_from_adr, entry_sec_from_adr) ||
                SCUtility.isMatche(leave_sec_from_adr, entry_sec_to_adr))
            {
                release_adr = scApp.AddressesBLL.cache.GetAddress(leave_sec_to_adr);
            }
            else if (SCUtility.isMatche(leave_sec_to_adr, entry_sec_from_adr) ||
                    SCUtility.isMatche(leave_sec_to_adr, entry_sec_to_adr))
            {
                release_adr = scApp.AddressesBLL.cache.GetAddress(leave_sec_from_adr);
            }

            return release_adr;
        }

        private AADDRESS FindReleaseAddress(ASEGMENT leave_segemnt, ASEGMENT entry_segment)
        {
            string leave_seg_from_adr = leave_segemnt.RealFromAddress;
            string leave_seg_to_adr = leave_segemnt.RealToAddress;
            string entry_seg_from_adr = entry_segment.RealFromAddress;
            string entry_seg_to_adr = entry_segment.RealToAddress;

            AADDRESS release_adr = null;
            if (SCUtility.isMatche(leave_seg_from_adr, entry_seg_from_adr) ||
                SCUtility.isMatche(leave_seg_from_adr, entry_seg_to_adr))
            {
                release_adr = scApp.AddressesBLL.cache.GetAddress(leave_seg_to_adr);
            }
            else if (SCUtility.isMatche(leave_seg_to_adr, entry_seg_from_adr) ||
                    SCUtility.isMatche(leave_seg_to_adr, entry_seg_to_adr))
            {
                release_adr = scApp.AddressesBLL.cache.GetAddress(leave_seg_from_adr);
            }

            return release_adr;
        }


        private void PublishVhInfo(object sender, PropertyChangedEventArgs e)
        {
            //Task.Run(() =>
            //{
            try
            {
                // AVEHICLE vh = sender as AVEHICLE;
                string vh_id = e.PropertyValue as string;
                AVEHICLE vh = scApp.VehicleBLL.getVehicleByID(vh_id);
                if (sender == null) return;
                byte[] vh_Serialize = BLL.VehicleBLL.Convert2GPB_VehicleInfo(vh);
                RecoderVehicleObjInfoLog(vh_id, vh_Serialize);
                //var vh_Serialize = ZeroFormatter.ZeroFormatterSerializer.Serialize(vh);
                //RecoderTESTLog(vh_Serialize, target_log_TEST_ZeroFormatter);

                Task.Run(() => scApp.FlexsimCommandDao.setVhStatusToFlexsimDB(vh_id, vh.CUR_ADR_ID, vh.ACC_SEC_DIST, vh.VhRecentTranEvent, vh.CST_ID,
                                                               vh.MODE_STATUS, vh.ACT_STATUS, vh.OBS_PAUSE, vh.BLOCK_PAUSE, vh.CMD_PAUSE,
                                                               vh.HID_PAUSE, vh.ERROR, vh.EARTHQUAKE_PAUSE, vh.SAFETY_DOOR_PAUSE));

                scApp.getNatsManager().PublishAsync
                    (string.Format(SCAppConstants.NATS_SUBJECT_VH_INFO_0, vh.VEHICLE_ID.Trim()), vh_Serialize);

                scApp.getRedisCacheManager().ListSetByIndexAsync
                    (SCAppConstants.REDIS_LIST_KEY_VEHICLES, vh.VEHICLE_ID, vh.ToString());
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception:");
            }
            //});
        }

        private static void RecoderVehicleObjInfoLog(string vh_id, byte[] arrayByte)
        {
            string compressStr = SCUtility.CompressArrayByte(arrayByte);
            dynamic logEntry = new JObject();
            logEntry.RPT_TIME = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz", CultureInfo.InvariantCulture);
            logEntry.OBJECT_ID = vh_id;
            logEntry.RAWDATA = compressStr;
            logEntry.Index = "ObjectHistoricalInfo";
            var json = logEntry.ToString(Newtonsoft.Json.Formatting.None);
            json = json.Replace("RPT_TIME", "@timestamp");
            LogManager.GetLogger("ObjectHistoricalInfo").Info(json);
        }

        #region Send Message To Vehicle
        public bool HostBasicVersionReport(string vh_id)
        {
            bool isSuccess = false;
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vh_id);
            DateTime crtTime = DateTime.Now;
            ID_101_HOST_BASIC_INFO_VERSION_RESPONSE receive_gpp = null;
            ID_1_HOST_BASIC_INFO_VERSION_REP sned_gpp = new ID_1_HOST_BASIC_INFO_VERSION_REP()
            {
                DataDateTimeYear = "2018",
                DataDateTimeMonth = "10",
                DataDateTimeDay = "25",
                DataDateTimeHour = "15",
                DataDateTimeMinute = "22",
                DataDateTimeSecond = "50",
                CurrentTimeYear = crtTime.Year.ToString(),
                CurrentTimeMonth = crtTime.Month.ToString(),
                CurrentTimeDay = crtTime.Day.ToString(),
                CurrentTimeHour = crtTime.Hour.ToString(),
                CurrentTimeMinute = crtTime.Minute.ToString(),
                CurrentTimeSecond = crtTime.Second.ToString()
            };
            isSuccess = vh.send_Str1(sned_gpp, out receive_gpp);
            isSuccess = isSuccess && receive_gpp.ReplyCode == 0;
            return isSuccess;
        }
        public bool BasicInfoReport(string vh_id)
        {
            bool isSuccess = false;
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vh_id);
            DateTime crtTime = DateTime.Now;
            ID_111_BASIC_INFO_RESPONSE receive_gpp = null;
            int travel_base_data_count = 1;
            int section_data_count = 0;
            int address_data_coune = 0;
            int scale_base_data_count = 1;
            int control_data_count = 1;
            int guide_base_data_count = 1;
            section_data_count = scApp.DataSyncBLL.getCount_ReleaseVSections();
            address_data_coune = scApp.MapBLL.getCount_AddressCount();
            ID_11_BASIC_INFO_REP sned_gpp = new ID_11_BASIC_INFO_REP()
            {
                TravelBasicDataCount = travel_base_data_count,
                SectionDataCount = section_data_count,
                AddressDataCount = address_data_coune,
                ScaleDataCount = scale_base_data_count,
                ContrlDataCount = control_data_count,
                GuideDataCount = guide_base_data_count
            };
            isSuccess = vh.sned_S11(sned_gpp, out receive_gpp);
            isSuccess = isSuccess && receive_gpp.ReplyCode == 0;
            return isSuccess;
        }
        public bool TavellingDataReport(string vh_id)
        {
            bool isSuccess = false;
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vh_id);
            DateTime crtTime = DateTime.Now;
            AVEHICLE_CONTROL_100 data = scApp.DataSyncBLL.getReleaseVehicleControlData_100(vh_id);

            ID_113_TAVELLING_DATA_RESPONSE receive_gpp = null;
            ID_13_TAVELLING_DATA_REP sned_gpp = new ID_13_TAVELLING_DATA_REP()
            {
                Resolution = (UInt32)data.TRAVEL_RESOLUTION,
                StartStopSpd = (UInt32)data.TRAVEL_START_STOP_SPEED,
                MaxSpeed = (UInt32)data.TRAVEL_MAX_SPD,
                AccelTime = (UInt32)data.TRAVEL_ACCEL_DECCEL_TIME,
                SCurveRate = (UInt16)data.TRAVEL_S_CURVE_RATE,
                OriginDir = (UInt16)data.TRAVEL_HOME_DIR,
                OriginSpd = (UInt32)data.TRAVEL_HOME_SPD,
                BeaemSpd = (UInt32)data.TRAVEL_KEEP_DIS_SPD,
                ManualHSpd = (UInt32)data.TRAVEL_MANUAL_HIGH_SPD,
                ManualLSpd = (UInt32)data.TRAVEL_MANUAL_LOW_SPD,
                TeachingSpd = (UInt32)data.TRAVEL_TEACHING_SPD,
                RotateDir = (UInt16)data.TRAVEL_TRAVEL_DIR,
                EncoderPole = (UInt16)data.TRAVEL_ENCODER_POLARITY,
                PositionCompensation = 0, //TODO 要填入正確的資料
                //FLimit = (UInt16)data.TRAVEL_F_DIR_LIMIT, //TODO 要填入正確的資料
                //RLimit = (UInt16)data.TRAVEL_R_DIR_LIMIT,
                KeepDistFar = (UInt32)data.TRAVEL_OBS_DETECT_LONG,
                KeepDistNear = (UInt32)data.TRAVEL_OBS_DETECT_SHORT,
            };
            isSuccess = vh.sned_S13(sned_gpp, out receive_gpp);
            isSuccess = isSuccess && receive_gpp.ReplyCode == 0;
            return isSuccess;
        }
        public bool AddressDataReport(string vh_id)
        {
            bool isSuccess = false;
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vh_id);
            List<AADDRESS_DATA> adrs = scApp.DataSyncBLL.loadReleaseADDRESS_DATAs(vh_id);

            string rtnMsg = string.Empty;
            ID_17_ADDRESS_DATA_REP send_gpp = new ID_17_ADDRESS_DATA_REP();
            ID_117_ADDRESS_DATA_RESPONSE receive_gpp = null;
            foreach (AADDRESS_DATA adr in adrs)
            {
                var block_master = scApp.MapBLL.loadBZMByAdrID(adr.ADR_ID.Trim());
                var adrInfo = new ID_17_ADDRESS_DATA_REP.Types.Address()
                {
                    Addr = adr.ADR_ID,
                    Resolution = adr.RESOLUTION,
                    Loaction = adr.LOACTION,
                    BlockRelease = (block_master != null && block_master.Count > 0) ? 1 : 0
                };
                send_gpp.Addresss.Add(adrInfo);
            }
            isSuccess = vh.sned_S17(send_gpp, out receive_gpp);
            // isSuccess = isSuccess && receive_gpp.ReplyCode == 0;
            return isSuccess;
        }
        public bool ScaleDataReport(string vh_id)
        {
            bool isSuccess = false;
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vh_id);
            SCALE_BASE_DATA data = scApp.DataSyncBLL.getReleaseSCALE_BASE_DATA();

            ID_119_SCALE_DATA_RESPONSE receive_gpp = null;
            ID_19_SCALE_DATA_REP sned_gpp = new ID_19_SCALE_DATA_REP()
            {
                Resolution = (UInt32)data.RESOLUTION,
                InposArea = (UInt32)data.INPOSITION_AREA,
                InposStability = (UInt32)data.INPOSITION_STABLE_TIME,
                ScalePulse = (UInt32)data.TOTAL_SCALE_PULSE,
                ScaleOffset = (UInt32)data.SCALE_OFFSET,
                ScaleReset = (UInt32)data.SCALE_RESE_DIST,
                ReadDir = (UInt16)data.READ_DIR

            };
            isSuccess = vh.sned_S19(sned_gpp, out receive_gpp);
            isSuccess = isSuccess && receive_gpp.ReplyCode == 0;
            return isSuccess;
        }
        public bool ControlDataReport(string vh_id)
        {
            bool isSuccess = false;
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vh_id);

            CONTROL_DATA data = scApp.DataSyncBLL.getReleaseCONTROL_DATA();
            string rtnMsg = string.Empty;
            ID_121_CONTROL_DATA_RESPONSE receive_gpp;
            ID_21_CONTROL_DATA_REP sned_gpp = new ID_21_CONTROL_DATA_REP()
            {
                TimeoutT1 = (UInt32)data.T1,
                TimeoutT2 = (UInt32)data.T2,
                TimeoutT3 = (UInt32)data.T3,
                TimeoutT4 = (UInt32)data.T4,
                TimeoutT5 = (UInt32)data.T5,
                TimeoutT6 = (UInt32)data.T6,
                TimeoutT7 = (UInt32)data.T7,
                TimeoutT8 = (UInt32)data.T8,
                TimeoutBlock = (UInt32)data.BLOCK_REQ_TIME_OUT
            };
            isSuccess = vh.sned_S21(sned_gpp, out receive_gpp);
            isSuccess = isSuccess && receive_gpp.ReplyCode == 0;
            return isSuccess;
        }
        public bool GuideDataReport(string vh_id)
        {
            bool isSuccess = false;
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vh_id);
            AVEHICLE_CONTROL_100 data = scApp.DataSyncBLL.getReleaseVehicleControlData_100(vh_id);
            ID_123_GUIDE_DATA_RESPONSE receive_gpp;
            ID_23_GUIDE_DATA_REP sned_gpp = new ID_23_GUIDE_DATA_REP()
            {
                StartStopSpd = (UInt32)data.GUIDE_START_STOP_SPEED,
                MaxSpeed = (UInt32)data.GUIDE_MAX_SPD,
                AccelTime = (UInt32)data.GUIDE_ACCEL_DECCEL_TIME,
                SCurveRate = (UInt16)data.GUIDE_S_CURVE_RATE,
                NormalSpd = (UInt32)data.GUIDE_RUN_SPD,
                ManualHSpd = (UInt32)data.GUIDE_MANUAL_HIGH_SPD,
                ManualLSpd = (UInt32)data.GUIDE_MANUAL_LOW_SPD,
                LFLockPos = (UInt32)data.GUIDE_LF_LOCK_POSITION,
                LBLockPos = (UInt32)data.GUIDE_LB_LOCK_POSITION,
                RFLockPos = (UInt32)data.GUIDE_RF_LOCK_POSITION,
                RBLockPos = (UInt32)data.GUIDE_RB_LOCK_POSITION,
                ChangeStabilityTime = (UInt32)data.GUIDE_CHG_STABLE_TIME,
            };
            isSuccess = vh.sned_S23(sned_gpp, out receive_gpp);
            isSuccess = isSuccess && receive_gpp.ReplyCode == 0;
            return isSuccess;
        }
        public bool doDataSysc(string vh_id)
        {
            bool isSyscCmp = false;
            DateTime ohtDataVersion = new DateTime(2017, 03, 27, 10, 30, 00);
            if (BasicInfoReport(vh_id) &&
                TavellingDataReport(vh_id) &&
                AddressDataReport(vh_id) &&
                ScaleDataReport(vh_id) &&
                ControlDataReport(vh_id) &&
                GuideDataReport(vh_id))
            {
                isSyscCmp = true;
            }
            return isSyscCmp;
        }

        public bool IndividualUploadRequest(string vh_id)
        {
            bool isSuccess = false;
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vh_id);
            ID_161_INDIVIDUAL_UPLOAD_RESPONSE receive_gpp;
            ID_61_INDIVIDUAL_UPLOAD_REQ sned_gpp = new ID_61_INDIVIDUAL_UPLOAD_REQ()
            {

            };
            isSuccess = vh.sned_S61(sned_gpp, out receive_gpp);
            //TODO Set info 2 DB
            if (isSuccess)
            {

            }
            return isSuccess;
        }
        public bool IndividualChangeRequest(string vh_id)
        {
            bool isSuccess = false;
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vh_id);
            ID_163_INDIVIDUAL_CHANGE_RESPONSE receive_gpp;
            ID_63_INDIVIDUAL_CHANGE_REQ sned_gpp = new ID_63_INDIVIDUAL_CHANGE_REQ()
            {
                OffsetGuideFL = 1,
                OffsetGuideRL = 2,
                OffsetGuideFR = 3,
                OffsetGuideRR = 4
            };
            isSuccess = vh.sned_S63(sned_gpp, out receive_gpp);
            return isSuccess;
        }

        /// <summary>
        /// 與Vehicle進行資料同步。(通常使用剛與Vehicle連線時)
        /// </summary>
        /// <param name="vh_id"></param>
        public override void VehicleInfoSynchronize(string vh_id)
        {
            /*與Vehicle進行狀態同步*/
            VehicleStatusRequest(vh_id, true);
            /*要求Vehicle進行Alarm的Reset，如果成功後會將OHxC上針對該Vh的Alarm清除*/
            if (AlarmResetRequest(vh_id))
            {
                scApp.AlarmBLL.resetAllAlarmReport(vh_id);
                scApp.AlarmBLL.resetAllAlarmReport2Redis(vh_id);
            }
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vh_id);
            //if (vh.MODE_STATUS == VHModeStatus.Manual &&
            //    !SCUtility.isEmpty(vh.CUR_ADR_ID))
            //{
            //    ModeChangeRequest(vh_id, OperatingVHMode.OperatingAuto);
            //}
        }
        public override bool VehicleStatusRequest(string vh_id, bool isSync = false)
        {
            bool isSuccess = false;
            string reason = string.Empty;
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vh_id);
            ID_143_STATUS_RESPONSE receive_gpp;
            ID_43_STATUS_REQUEST send_gpp = new ID_43_STATUS_REQUEST()
            {
                SystemTime = DateTime.Now.ToString(SCAppConstants.TimestampFormat_16)
            };
            SCUtility.RecodeReportInfo(vh.VEHICLE_ID, 0, send_gpp);
            isSuccess = vh.send_S43(send_gpp, out receive_gpp);
            SCUtility.RecodeReportInfo(vh.VEHICLE_ID, 0, receive_gpp, isSuccess.ToString());
            //if (isSync && isSuccess)
            if (isSuccess)
            {
                string cst_id = receive_gpp.CSTID;
                uint batteryCapacity = receive_gpp.BatteryCapacity;
                VHModeStatus modeStat = scApp.VehicleBLL.DecideVhModeStatus(vh.VEHICLE_ID, receive_gpp.ModeStatus, batteryCapacity);
                VHActionStatus actionStat = receive_gpp.ActionStatus;
                VhPowerStatus powerStat = receive_gpp.PowerStatus;
                //string cstID = recive_str.CSTID;
                VhStopSingle obstacleStat = receive_gpp.ObstacleStatus;
                VhStopSingle blockingStat = receive_gpp.BlockingStatus;
                VhStopSingle pauseStat = receive_gpp.PauseStatus;
                VhStopSingle errorStat = receive_gpp.ErrorStatus;
                VhLoadCSTStatus loadCSTStatus = receive_gpp.HasCST;
                //VhGuideStatus leftGuideStat = recive_str.LeftGuideLockStatus;
                //VhGuideStatus rightGuideStat = recive_str.RightGuideLockStatus;


                int obstacleDIST = receive_gpp.ObstDistance;
                string obstacleVhID = receive_gpp.ObstVehicleID;

                string current_adr = receive_gpp.CurrentAdrID;
                scApp.VehicleBLL.setAndPublishPositionReportInfo2Redis(vh.VEHICLE_ID, receive_gpp);
                //  scApp.VehicleBLL.getAndProcPositionReportFromRedis(vh.VEHICLE_ID);

                //checkCurrentCmdStatusWithVhActionStat(vh.VEHICLE_ID, actionStat);

                if (modeStat != vh.MODE_STATUS)
                {
                    vh.onModeStatusChange(modeStat);
                }
                if (loadCSTStatus == VhLoadCSTStatus.Exist)
                {
                    vh.CarrierInstall();
                }
                else
                {
                    vh.CarrierRemove();
                    ProcessAlarmReport(null, AlarmBLL.VEHICLE_LONG_TIME_INSTALLED_CARRIER, ErrorStatus.ErrReset, $"vehicle long time installed carrier.");
                }
                if (!scApp.VehicleBLL.doUpdateVehicleStatus(vh,
                                      cst_id, modeStat, actionStat,
                                       blockingStat, pauseStat, obstacleStat, VhStopSingle.StopSingleOff, errorStat, loadCSTStatus,
                                       batteryCapacity))
                {
                    isSuccess = false;
                }
            }
            return isSuccess;
        }

        public bool ModeChangeRequest(string vh_id, OperatingVHMode mode)
        {
            bool isSuccess = false;
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vh_id);
            ID_141_MODE_CHANGE_RESPONSE receive_gpp;
            ID_41_MODE_CHANGE_REQ sned_gpp = new ID_41_MODE_CHANGE_REQ()
            {
                OperatingVHMode = mode
            };
            isSuccess = vh.sned_S41(sned_gpp, out receive_gpp);
            return isSuccess;
        }

        public bool PowerOperatorRequest(string vh_id, OperatingPowerMode mode)
        {
            bool isSuccess = false;
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vh_id);
            ID_145_POWER_OPE_RESPONSE receive_gpp;
            ID_45_POWER_OPE_REQ sned_gpp = new ID_45_POWER_OPE_REQ()
            {
                OperatingPowerMode = mode
            };
            isSuccess = vh.sned_S45(sned_gpp, out receive_gpp);
            return isSuccess;
        }
        public bool AlarmResetRequest(string vh_id)
        {
            bool isSuccess = false;
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vh_id);
            ID_191_ALARM_RESET_RESPONSE receive_gpp;
            ID_91_ALARM_RESET_REQUEST sned_gpp = new ID_91_ALARM_RESET_REQUEST()
            {

            };
            isSuccess = vh.sned_S91(sned_gpp, out receive_gpp);
            if (isSuccess)
            {
                isSuccess = receive_gpp?.ReplyCode == 0;
            }
            return isSuccess;
        }

        public void urgentPauseRequest(string vhID)
        {
            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
               Data: $"Initial excute urgent stop ,vh:{vhID}!!!",
               VehicleID: vhID);
            AVEHICLE vh = scApp.VehicleBLL.cache.getVehicle(vhID);
            if (vh != null)
            {
                if (System.Threading.Interlocked.Exchange(ref vh.syncUrgentPausePoint, 1) == 0)
                {
                    try
                    {

                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                           Data: $"Start excute urgent stop,vh:{vhID} , current section:{vh.CUR_SEC_ID}," +
                                 $"current address:{vh.CUR_ADR_ID},current excute command:{SCUtility.Trim(vh.OHTC_CMD, true)}!!!",
                           VehicleID: vhID,
                           CarrierID: vh.CST_ID);
                        do
                        {
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                               Data: $"Sent urgent stop,vh:{vhID}...",
                               VehicleID: vhID,
                               CarrierID: vh.CST_ID);
                            PauseRequest(vhID, PauseEvent.Pause, OHxCPauseType.Normal);
                            SpinWait.SpinUntil(() => IsStop(vh), 1000);
                        }
                        while (!IsStop(vh));

                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                           Data: $"Finish excute urgent stop,vh:{vhID} , current section:{vh.CUR_SEC_ID}," +
                                 $"current address:{vh.CUR_ADR_ID},current excute command:{SCUtility.Trim(vh.OHTC_CMD, true)}!!!",
                           VehicleID: vhID,
                           CarrierID: vh.CST_ID);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                           Data: ex,
                           VehicleID: vhID);
                    }
                    finally
                    {
                        System.Threading.Interlocked.Exchange(ref vh.syncUrgentPausePoint, 0);
                    }
                }
            }
            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
               Data: $"End urgent stop ,vh:{vhID}!!!",
               VehicleID: vhID);
        }
        private bool IsStop(AVEHICLE vh)
        {
            return vh.IsPause || vh.ACT_STATUS != VHActionStatus.Commanding || vh.MODE_STATUS == VHModeStatus.Manual;
        }

        public bool PauseRequest(string vh_id, PauseEvent pause_event, OHxCPauseType ohxc_pause_type)
        {
            bool isSuccess = false;
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vh_id);
            PauseType pauseType = convert2PauseType(ohxc_pause_type);
            ID_139_PAUSE_RESPONSE receive_gpp;
            ID_39_PAUSE_REQUEST send_gpp = new ID_39_PAUSE_REQUEST()
            {
                PauseType = pauseType,
                EventType = pause_event
            };
            SCUtility.RecodeReportInfo(vh.VEHICLE_ID, 0, send_gpp);
            isSuccess = vh.sned_Str39(send_gpp, out receive_gpp);
            SCUtility.RecodeReportInfo(vh.VEHICLE_ID, 0, receive_gpp, isSuccess.ToString());
            return isSuccess;
        }
        public bool OHxCPauseRequest(string vh_id, PauseEvent pause_event, OHxCPauseType ohxc_pause_type)
        {
            bool isSuccess = false;
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vh_id);
            using (TransactionScope tx = SCUtility.getTransactionScope())
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {

                    switch (ohxc_pause_type)
                    {
                        case OHxCPauseType.Earthquake:
                            scApp.VehicleBLL.updateVehiclePauseStatus
                                (vh_id, earthquake_pause: pause_event == PauseEvent.Pause);
                            break;
                        case OHxCPauseType.Obstacle:
                            scApp.VehicleBLL.updateVehiclePauseStatus
                                (vh_id, obstruct_pause: pause_event == PauseEvent.Pause);
                            break;
                        case OHxCPauseType.Safty:
                            scApp.VehicleBLL.updateVehiclePauseStatus
                                (vh_id, safyte_pause: pause_event == PauseEvent.Pause);
                            break;
                    }
                    PauseType pauseType = convert2PauseType(ohxc_pause_type);
                    ID_139_PAUSE_RESPONSE receive_gpp;
                    ID_39_PAUSE_REQUEST send_gpp = new ID_39_PAUSE_REQUEST()
                    {
                        PauseType = pauseType,
                        EventType = pause_event
                    };
                    SCUtility.RecodeReportInfo(vh.VEHICLE_ID, 0, send_gpp);
                    isSuccess = vh.sned_Str39(send_gpp, out receive_gpp);
                    SCUtility.RecodeReportInfo(vh.VEHICLE_ID, 0, receive_gpp, isSuccess.ToString());

                    if (isSuccess)
                    {
                        tx.Complete();
                        vh.NotifyVhStatusChange();
                    }
                }
            }
            return isSuccess;
        }
        private PauseType convert2PauseType(OHxCPauseType ohxc_pauseType)
        {
            switch (ohxc_pauseType)
            {
                case OHxCPauseType.Normal:
                case OHxCPauseType.Obstacle:
                    return PauseType.OhxC;
                case OHxCPauseType.Block:
                    return PauseType.Block;
                case OHxCPauseType.Earthquake:
                    return PauseType.EarthQuake;
                //case OHxCPauseType.Obstruct:
                //    return PauseType.;
                case OHxCPauseType.Safty:
                    return PauseType.Safety;
                case OHxCPauseType.ALL:
                    return PauseType.All;
                default:
                    throw new AggregateException($"enum arg not exist!value: {ohxc_pauseType}");
            }
        }

        public override bool doSendCommandToVh(AVEHICLE assignVH, ACMD_OHTC cmd)
        {
            bool isSuccess = false;
            string cmd_id = cmd.CMD_ID;
            string carrier_id = cmd.CARRIER_ID;
            string vh_id = cmd.VH_ID;
            string source_adr = cmd.SOURCE;
            string dest_adr = cmd.DESTINATION;
            string vh_current_address = assignVH.CUR_ADR_ID;
            string vh_current_section = assignVH.CUR_SEC_ID;
            string start_section = assignVH.CUR_SEC_ID;
            ActiveType active_type = scApp.CMDBLL.convertECmdType2ActiveType(cmd.CMD_TPYE);
            try
            {
                List<string> guide_start_to_from_segment_ids = null;
                List<string> guide_start_to_from_section_ids = null;
                List<string> guide_start_to_from_address_ids = null;
                List<string> guide_to_dest_segment_ids = null;
                List<string> guide_to_dest_section_ids = null;
                List<string> guide_to_dest_address_ids = null;
                int total_cost = 0;
                //1.取得行走路徑的詳細資料
                (isSuccess, total_cost,
                 guide_start_to_from_segment_ids,
                 guide_start_to_from_section_ids,
                 guide_start_to_from_address_ids,
                 guide_to_dest_segment_ids,
                 guide_to_dest_section_ids,
                 guide_to_dest_address_ids)
                //= FindGuideInfo(vh_current_address, source_adr, dest_adr, active_type, has_carry, need_by_pass_adr_ids);
                = FindGuideInfo(vh_current_address, source_adr, dest_adr, active_type);

                if (!isSuccess)
                {
                    //AbnormalProcess(cmd);
                    AbnormalProcess(vh_id, cmd);
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleService), Device: "OHxC",
                       Data: $"Planning path failed.cmd id:{cmd.CMD_ID},vh id:{cmd.VH_ID},carrier id:{cmd.CARRIER_ID},mcs cmd id:{cmd.CMD_ID_MCS}," +
                             $"command type:{cmd.CMD_TPYE},source:{cmd.SOURCE},dest:{cmd.DESTINATION}");
                    return false;
                }

                bool start_section_is_same = true;
                if (guide_start_to_from_section_ids != null && guide_start_to_from_section_ids.Count > 0)
                {
                    if (!assignVH.IsOnAdr)
                    {
                        start_section_is_same = SCUtility.isMatche(guide_start_to_from_section_ids[0], start_section);
                        if (!start_section_is_same)
                        {
                            guide_start_to_from_section_ids.Insert(0, start_section);
                            ASECTION new_start_section = scApp.SectionBLL.cache.GetSection(start_section);
                            if (SCUtility.isMatche(guide_start_to_from_address_ids[0], new_start_section.FROM_ADR_ID))
                            {
                                guide_start_to_from_address_ids.Insert(0, new_start_section.TO_ADR_ID);
                            }
                            else
                            {
                                guide_start_to_from_address_ids.Insert(0, new_start_section.FROM_ADR_ID);
                            }
                            //if (!SCUtility.isMatche(guide_start_to_from_segment_ids[0], new_start_section.SEG_NUM))
                            //{
                            //    guide_start_to_from_segment_ids.Insert(0, new_start_section.SEG_NUM);
                            //}
                        }
                    }
                }
                else if (guide_to_dest_section_ids != null && guide_to_dest_section_ids.Count > 0)
                {
                    if (!assignVH.IsOnAdr)
                    {
                        start_section_is_same = SCUtility.isMatche(guide_to_dest_section_ids[0], start_section);
                        if (!start_section_is_same)
                        {
                            guide_to_dest_section_ids.Insert(0, start_section);
                            ASECTION new_start_section = scApp.SectionBLL.cache.GetSection(start_section);
                            if (SCUtility.isMatche(guide_to_dest_address_ids[0], new_start_section.FROM_ADR_ID))
                            {
                                guide_to_dest_address_ids.Insert(0, new_start_section.TO_ADR_ID);
                            }
                            else
                            {
                                guide_to_dest_address_ids.Insert(0, new_start_section.FROM_ADR_ID);
                            }
                            //if (!SCUtility.isMatche(guide_to_dest_segment_ids[0], new_start_section.SEG_NUM))
                            //{
                            //    guide_to_dest_segment_ids.Insert(0, new_start_section.SEG_NUM);
                            //}
                        }
                    }
                }



                //2.建立Cmd Details
                List<string> guide_section_ids = new List<string>();
                List<string> guide_segment_ids = new List<string>();
                List<string> guide_addresses_ids = new List<string>();

                if (guide_start_to_from_section_ids == null && guide_to_dest_section_ids == null)
                {
                    //Not thing....
                }
                else
                {
                    if (guide_start_to_from_section_ids != null)
                        guide_section_ids.AddRange(guide_start_to_from_section_ids);
                    if (guide_to_dest_section_ids != null)
                        guide_section_ids.AddRange(guide_to_dest_section_ids);
                    scApp.CMDBLL.CeratCmdDerails(cmd_id, guide_section_ids.ToArray());
                }
                if (guide_start_to_from_segment_ids == null && guide_to_dest_segment_ids == null)
                {
                    //Not thing....
                }
                else
                {
                    if (guide_start_to_from_segment_ids != null)
                        guide_segment_ids.AddRange(guide_start_to_from_segment_ids);
                    if (guide_to_dest_segment_ids != null)
                        guide_segment_ids.AddRange(guide_to_dest_segment_ids);
                }
                if (guide_start_to_from_address_ids == null && guide_to_dest_address_ids == null)
                {
                    //Not thing....
                }
                else
                {
                    if (guide_start_to_from_address_ids != null)
                        guide_addresses_ids.AddRange(guide_start_to_from_address_ids);
                    if (guide_to_dest_address_ids != null)
                        guide_addresses_ids.AddRange(guide_to_dest_address_ids);
                }

                //3.發送命令給VH
                SCUtility.TrimAllParameter(cmd);
                //scApp.CMDBLL.updateCommand_OHTC_StatusByCmdID(cmd.CMD_ID, E_CMD_STATUS.Sending);
                scApp.CMDBLL.updateCommand_OHTC_StatusByCmdID(vh_id, cmd.CMD_ID, E_CMD_STATUS.Sending);
                if (!string.IsNullOrEmpty(cmd.CMD_ID_MCS))
                {
                    if(!cmd.CMD_ID.StartsWith(((int)GenOHxCCommandType.Retry).ToString()))
                    {
                        isSuccess &= scApp.ReportBLL.newReportTransferInitial(cmd.CMD_ID_MCS, null);
                    }
                }
                isSuccess = ProcSendTransferCommandToVh(cmd, assignVH, active_type,
                 guide_start_to_from_segment_ids?.ToArray(), guide_start_to_from_section_ids?.ToArray(), guide_start_to_from_address_ids?.ToArray(),
                 guide_to_dest_segment_ids?.ToArray(), guide_to_dest_section_ids?.ToArray(), guide_to_dest_address_ids?.ToArray());
                //4.更新命令狀態(HOST CMD)
                if (isSuccess)
                {
                    if (!SCUtility.isEmpty(cmd.CMD_ID_MCS))
                    {
                        //在設備確定接收該筆命令，把它從PreInitial改成Initial狀態並上報給MCS
                        isSuccess &= scApp.CMDBLL.updateCMD_MCS_TranStatus2Initial(cmd.CMD_ID_MCS);
                        //TODO 在進行命令的改派後SysExecQity的資料要重新判斷一下要怎樣計算
                        //scApp.SysExcuteQualityBLL.updateSysExecQity_PassSecInfo(cmd.CMD_ID_MCS, assignVH.VEHICLE_ID, assignVH.CUR_SEC_ID,
                        //                        guide_to_dest_section_ids?.ToArray(), guide_to_dest_address_ids?.ToArray());
                        scApp.SysExcuteQualityBLL.updateSysExecQity_PassSecInfo(cmd.CMD_ID_MCS, assignVH.VEHICLE_ID, assignVH.CUR_SEC_ID,
                                                guide_start_to_from_section_ids?.ToArray(), guide_to_dest_section_ids?.ToArray());
                    }

                    assignVH.VehicleAssign();
                    scApp.CMDBLL.setVhExcuteCmdToShow(cmd, assignVH, guide_segment_ids, guide_section_ids?.ToArray(), guide_addresses_ids?.ToArray(),
                                                      guide_start_to_from_section_ids, guide_to_dest_section_ids);

                    assignVH.sw_speed.Restart();
                }
                else
                {
                    //AbnormalProcess(cmd);
                    AbnormalProcess(vh_id, cmd);
                    BCFApplication.onWarningMsg($"doSendCommandToVh fail.vh:{vh_id}, cmd id:{cmd_id},from:{source_adr},to:{dest_adr},active type:{active_type}." +
                            $"vh current adr:{vh_current_address},start section:{start_section}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: ex,
                   Details: $"doSendCommandToVh fail.vh:{vh_id}, cmd id:{cmd_id},from:{source_adr},to:{dest_adr},active type:{active_type}." +
                            $"vh current adr:{vh_current_address},start section:{start_section}",
                   VehicleID: vh_id,
                   XID: cmd_id,
                   CarrierID: carrier_id);
                //AbnormalProcess(cmd);
                AbnormalProcess(vh_id, cmd);
            }
            return isSuccess;
        }
        private (bool isSuccess, int total_code,
            List<string> guide_start_to_from_segment_ids, List<string> guide_start_to_from_section_ids, List<string> guide_start_to_from_address_ids,
            List<string> guide_to_dest_segment_ids, List<string> guide_to_dest_section_ids, List<string> guide_to_dest_address_ids)
            //FindGuideInfo(string vh_current_address, string source_adr, string dest_adr, ActiveType active_type, bool has_carray = false, List<string> byPassAddressIDs = null)
            FindGuideInfo(string vh_current_address, string source_adr, string dest_adr, ActiveType active_type, bool has_carray = false, List<string> byPassSectionIDs = null)
        {
            bool isSuccess = false;
            List<string> guide_start_to_from_segment_ids = null;
            List<string> guide_start_to_from_section_ids = null;
            List<string> guide_start_to_from_address_ids = null;
            List<string> guide_to_dest_segment_ids = null;
            List<string> guide_to_dest_section_ids = null;
            List<string> guide_to_dest_address_ids = null;
            int total_cost = 0;
            //多增加過濾掉故障車路線的邏輯
            List<string> all_by_pass_section_ids = loadAllByPassSection(byPassSectionIDs);

            //1.取得行走路徑的詳細資料
            switch (active_type)
            {
                case ActiveType.Loadunload:
                    if (has_carray)
                    {
                        if (!SCUtility.isMatche(vh_current_address, dest_adr))
                        {
                            (isSuccess, guide_to_dest_segment_ids, guide_to_dest_section_ids, guide_to_dest_address_ids, total_cost)
                                = scApp.GuideBLL.getGuideInfo(vh_current_address, dest_adr, byPassSectionIDs);
                        }
                    }
                    else
                    {
                        if (!SCUtility.isMatche(vh_current_address, source_adr))
                        {
                            (isSuccess, guide_start_to_from_segment_ids, guide_start_to_from_section_ids, guide_start_to_from_address_ids, total_cost)
                                = scApp.GuideBLL.getGuideInfo(vh_current_address, source_adr, byPassSectionIDs);
                        }
                        else
                        {
                            isSuccess = true;//如果相同 代表是在同一個點上
                        }
                        if (isSuccess && !SCUtility.isMatche(source_adr, dest_adr))
                        {
                            (isSuccess, guide_to_dest_segment_ids, guide_to_dest_section_ids, guide_to_dest_address_ids, total_cost)
                                = scApp.GuideBLL.getGuideInfo(source_adr, dest_adr, null);
                        }
                    }
                    break;
                case ActiveType.Load:
                    if (!SCUtility.isMatche(vh_current_address, source_adr))
                    {
                        (isSuccess, guide_start_to_from_segment_ids, guide_start_to_from_section_ids, guide_start_to_from_address_ids, total_cost)
                            = scApp.GuideBLL.getGuideInfo(vh_current_address, source_adr, byPassSectionIDs);
                    }
                    else
                    {
                        isSuccess = true; //如果相同 代表是在同一個點上
                    }
                    break;
                case ActiveType.Unload:
                    if (!SCUtility.isMatche(vh_current_address, dest_adr))
                    {
                        (isSuccess, guide_to_dest_segment_ids, guide_to_dest_section_ids, guide_to_dest_address_ids, total_cost)
                            = scApp.GuideBLL.getGuideInfo(vh_current_address, dest_adr, byPassSectionIDs);
                    }
                    else
                    {
                        isSuccess = true;//如果相同 代表是在同一個點上
                    }
                    break;
                case ActiveType.Move:
                case ActiveType.Movetocharger:
                    if (!SCUtility.isMatche(vh_current_address, dest_adr))
                    {
                        (isSuccess, guide_to_dest_segment_ids, guide_to_dest_section_ids, guide_to_dest_address_ids, total_cost)
                            = scApp.GuideBLL.getGuideInfo(vh_current_address, dest_adr, byPassSectionIDs);
                    }
                    else
                    {
                        isSuccess = false;
                    }
                    break;
            }
            return (isSuccess, total_cost,
                    guide_start_to_from_segment_ids, guide_start_to_from_section_ids, guide_start_to_from_address_ids,
                    guide_to_dest_segment_ids, guide_to_dest_section_ids, guide_to_dest_address_ids);
        }

        private List<string> loadAllByPassSection(List<string> originalByPassSection)
        {
            List<string> by_pass_section_ids = new List<string>();
            if (originalByPassSection != null && originalByPassSection.Count > 0)
                by_pass_section_ids.AddRange(originalByPassSection);
            List<string> blocked_section_ids = loadBlockedSectionIDs(scApp.VehicleBLL, scApp.UnitBLL);
            if (blocked_section_ids != null && blocked_section_ids.Count > 0)
                by_pass_section_ids.AddRange(blocked_section_ids);
            return by_pass_section_ids;
        }

        private List<string> loadBlockedSectionIDs(VehicleBLL vehicleBLL, UnitBLL unitBLL)
        {
            List<string> blocked_section_ids = new List<string>();
            try
            {
                var alarm_vhs = vehicleBLL.cache.loadAlarmVhs();

                foreach (var vh in alarm_vhs)
                {
                    if (vh.IsOnAdr)
                    {
                        var sectionIDs = scApp.SectionBLL.cache.GetSectionsByAddress(vh.CUR_ADR_ID).Select(s => s.SEC_ID);
                        blocked_section_ids.AddRange(sectionIDs);
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(GuideBLL), Device: "OHx",
                            Data: $"vh:{vh.VEHICLE_ID} error at address:{vh.CUR_ADR_ID}, bypass sections connected to this address when finding guide route");
                    }
                    else
                    {
                        string current_sec_id = vh.CUR_SEC_ID;
                        if (SCUtility.isEmpty(current_sec_id))
                        {
                            continue;
                        }
                        blocked_section_ids.Add(current_sec_id);
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(GuideBLL), Device: "OHx",
                            Data: $"vh:{vh.VEHICLE_ID} error at section:{current_sec_id} by pass this section ,when find guide route");
                    }
                }
                return blocked_section_ids;
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, "Exception:");
                return blocked_section_ids;
            }
        }

        private void AbnormalProcess(string vhID, ACMD_OHTC cmd)
        {
            if (!SCUtility.isEmpty(cmd.CMD_ID_MCS))
            {
                scApp.CMDBLL.updateCMD_MCS_TranStatus2Queue(cmd.CMD_ID_MCS);
            }
            //scApp.CMDBLL.updateCommand_OHTC_StatusByCmdID(cmd.CMD_ID, E_CMD_STATUS.AbnormalEndByOHT);
            scApp.CMDBLL.updateCommand_OHTC_StatusByCmdID(vhID, cmd.CMD_ID, E_CMD_STATUS.AbnormalEndByOHT);
        }



        List<string> SpecifyOverrideFixedByPassSection_AUO_CAAGV100 = new List<string>() { "0321", "0352", "0361" };
        /// <summary>
        /// 用來預防再發生override，更改路徑成功時，要重新計算預約權時被人家搶走原有的路權
        /// </summary>
        object reserve_lock = new object();
        //public bool doSendOverrideCommandToVh(AVEHICLE assignVH, ACMD_OHTC cmd, string byPassAdr)
        public override bool trydoOverrideCommandToVh(AVEHICLE assignVH, ACMD_OHTC cmd, string byPassSection, bool isAvoidComplete = false)
        {
            bool isSuccess = false;
            try
            {
                string vh_id = cmd.VH_ID;
                string cmd_id = cmd.CMD_ID;
                string vh_current_address = assignVH.CUR_ADR_ID;
                string vh_current_section = assignVH.CUR_SEC_ID;
                string source_adr = cmd.SOURCE;
                string dest_adr = cmd.DESTINATION;
                bool has_carry = assignVH.HAS_CST == 1;
                ActiveType original_active_type = scApp.CMDBLL.convertECmdType2ActiveType(cmd.CMD_TPYE);
                ActiveType active_type = scApp.CMDBLL.convertECmdType2ActiveType(cmd.CMD_TPYE);
                //List<string> need_by_pass_adr_ids = new List<string>() { byPassAdr };
                //List<string> need_by_pass_sec_ids = new List<string>() { byPassSection };
                List<string> need_by_pass_sec_ids = new List<string>();
                if (!SCUtility.isEmpty(byPassSection))
                {
                    need_by_pass_sec_ids.Add(byPassSection);
                }
                List<string> guide_start_to_from_segment_ids = null;
                List<string> guide_start_to_from_section_ids = null;
                List<string> guide_start_to_from_address_ids = null;
                List<string> guide_to_dest_segment_ids = null;
                List<string> guide_to_dest_section_ids = null;
                List<string> guide_to_dest_address_ids = null;
                int total_cost = 0;
                SCUtility.TrimAllParameter(cmd);
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: $"start override vh:{assignVH.VEHICLE_ID} vh current address:{assignVH.CUR_ADR_ID} ,command :{cmd.CMD_ID} sourc:{cmd.SOURCE} destination:{cmd.DESTINATION}",
                   VehicleID: assignVH.VEHICLE_ID,
                   CarrierID: assignVH.CST_ID);
                int current_find_count = 0;
                int max_find_count = 10;
                bool is_need_check_reserve_status = !isAvoidComplete;
                do
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                        Data: $"Finding override path cycle count: {current_find_count}, vh current address:{assignVH.CUR_ADR_ID} ," +
                        $" bypass section list: {string.Join(",", need_by_pass_sec_ids)}.",
                        VehicleID: assignVH.VEHICLE_ID,
                        CarrierID: assignVH.CST_ID);

                    (isSuccess, total_cost,
                     guide_start_to_from_segment_ids,
                     guide_start_to_from_section_ids,
                     guide_start_to_from_address_ids,
                     guide_to_dest_segment_ids,
                     guide_to_dest_section_ids,
                     guide_to_dest_address_ids)
                    //= FindGuideInfo(vh_current_address, source_adr, dest_adr, active_type, has_carry, need_by_pass_adr_ids);
                    = FindGuideInfo(vh_current_address, source_adr, dest_adr, active_type, has_carry, need_by_pass_sec_ids);
                    //如果有找到路徑則確認一下段是否可以預約的到
                    if (isSuccess)
                    {
                        //確認下一段Section，是否可以預約成功
                        string next_walk_section = "";
                        string next_walk_address = "";
                        List<string> newSectionIDs = new List<string>();
                        List<string> newAddressIDs = new List<string>();
                        if (guide_start_to_from_section_ids != null && guide_start_to_from_section_ids.Count > 0)
                        {
                            next_walk_section = guide_start_to_from_section_ids[0];
                            next_walk_address = guide_start_to_from_address_ids[0];
                            newSectionIDs = guide_start_to_from_section_ids;
                            newAddressIDs = guide_start_to_from_address_ids;
                        }
                        else if (guide_to_dest_section_ids != null && guide_to_dest_section_ids.Count > 0)
                        {
                            next_walk_section = guide_to_dest_section_ids[0];
                            next_walk_address = guide_to_dest_address_ids[0];
                            newSectionIDs = guide_to_dest_section_ids;
                            newAddressIDs = guide_to_dest_address_ids;
                        }

                        if (isSuccess && is_need_check_reserve_status)
                        {
                            for (int i = 0; i < newSectionIDs.Count; i++)
                            {
                                var reserve_result = askReserveSuccess(assignVH.VEHICLE_ID, newSectionIDs[i], newAddressIDs[i]);
                                if (reserve_result.isSuccess)
                                {
                                    isSuccess = true;
                                }
                                else
                                {
                                    isSuccess = false;
                                    //need_by_pass_adr_ids.Add(reserve_result.reserveUnsuccessInfo.ReservedAdrID);
                                    need_by_pass_sec_ids.Add(newSectionIDs[i]);
                                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                       Data: $"find the override path ,but section:{newSectionIDs[i]} is reserved for vh:{reserve_result.reservedVhID}" +
                                             $"add to need by pass sec ids",
                                       VehicleID: assignVH.VEHICLE_ID,
                                       CarrierID: assignVH.CST_ID);
                                    break;
                                }
                            }

                            ////4.在準備送出前，如果是因Avoid完成所下的Over ride，要判斷原本block section是否已經可以預約到了，事才可以下給車子
                            ////if (isSuccess && isAvoidComplete)
                            ////if (isSuccess && is_need_check_reserve_status)
                            ////{
                            ////先判斷，新的路徑是否會在經過之前Blocked的Section
                            //if (assignVH.VhAvoidInfo != null)
                            //{
                            //    bool is_pass_before_blocked_section = true;
                            //    if (guide_start_to_from_section_ids != null)
                            //    {
                            //        is_pass_before_blocked_section &= guide_start_to_from_section_ids.Contains(assignVH.VhAvoidInfo.BlockedSectionID);
                            //    }
                            //    if (guide_to_dest_section_ids != null)
                            //    {
                            //        is_pass_before_blocked_section &= guide_to_dest_section_ids.Contains(assignVH.VhAvoidInfo.BlockedSectionID);
                            //    }
                            //    if (is_pass_before_blocked_section)
                            //    {
                            //        //如果有則要嘗試去預約，如果等了20秒還是沒有釋放出來則嘗試別條路徑
                            //        string before_block_section_id = assignVH.VhAvoidInfo.BlockedSectionID;
                            //        //if (!SpinWait.SpinUntil(() => scApp.ReserveBLL.TryAddReservedSection(vh_id, before_block_section_id, isAsk: true).OK, 20000))
                            //        //if (!SpinWait.SpinUntil(() => checkWillPassSectionIsClear(vh_id, before_block_section_id), 20000))
                            //        //2022.8.26 當下不過就加入排除列表了，不再等待20秒
                            //        if (!scApp.ReserveBLL.TryAddReservedSection(vh_id, before_block_section_id, isAsk: true).OK)
                            //        {
                            //            isSuccess = false;
                            //            //need_by_pass_sec_ids.Add(next_walk_section);
                            //            need_by_pass_sec_ids.Add(before_block_section_id);
                            //            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                            //               Data: $"block section id:{before_block_section_id} not release, by pass section:{before_block_section_id} find next path.current by pass section:{string.Join(",", need_by_pass_sec_ids)}",
                            //               VehicleID: assignVH.VEHICLE_ID,
                            //               CarrierID: assignVH.CST_ID);
                            //        }
                            //    }
                            //}
                        }
                        //4.在準備送出前，如果是因Avoid完成所下的Over ride，要判斷原本block section是否已經可以預約到了，事才可以下給車子
                        else if (isSuccess && isAvoidComplete)
                        {
                            //先判斷，新的路徑是否會在經過之前Blocked的Section
                            if (assignVH.VhAvoidInfo != null)
                            {
                                bool is_pass_before_blocked_section = true;
                                if (guide_start_to_from_section_ids != null)
                                {
                                    is_pass_before_blocked_section &= guide_start_to_from_section_ids.Contains(assignVH.VhAvoidInfo.BlockedSectionID);
                                }
                                if (guide_to_dest_section_ids != null)
                                {
                                    is_pass_before_blocked_section &= guide_to_dest_section_ids.Contains(assignVH.VhAvoidInfo.BlockedSectionID);
                                }
                                if (is_pass_before_blocked_section)
                                {
                                    //如果有則要嘗試去預約，如果等了20秒還是沒有釋放出來則嘗試別條路徑
                                    string before_block_section_id = assignVH.VhAvoidInfo.BlockedSectionID;
                                    if (!SpinWait.SpinUntil(() => checkWillPassSectionIsClear(vh_id, before_block_section_id), 20000))
                                    {
                                        isSuccess = false;
                                        //need_by_pass_sec_ids.Add(next_walk_section);
                                        need_by_pass_sec_ids.Add(before_block_section_id);
                                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                           Data: $"wait more than 20 seconds,before block section id:{before_block_section_id} not release, by pass section:{before_block_section_id} find next path.current by pass section:{string.Join(",", need_by_pass_sec_ids)}",
                                           VehicleID: assignVH.VEHICLE_ID,
                                           CarrierID: assignVH.CST_ID);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        ////如果在找不到路的時候，就把原本By pass的路徑給打開，然後再找一次
                        ////該次就不檢查原本預約不到的路是否已經可以過了，即使不能過也再下一次走看看
                        //if (need_by_pass_sec_ids != null && need_by_pass_sec_ids.Count > 0)
                        if (isAvoidComplete)
                        {
                            isSuccess = false;
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                Data: $"override path fail vh:{assignVH.VEHICLE_ID} of command id:{cmd.CMD_ID} vh current address:{assignVH.CUR_ADR_ID} ," +
                                $" by pass section:{string.Join(",", need_by_pass_sec_ids)},clear all by pass section and then continue find override path.",
                                VehicleID: assignVH.VEHICLE_ID,
                                CarrierID: assignVH.CST_ID);

                            need_by_pass_sec_ids.Clear();
                            //is_need_check_reserve_status = false;
                        }
                        else
                        {
                            //如果找不到路徑，則就直接跳出搜尋的Loop
                            isSuccess = false;
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                               Data: $"override path fail vh:{assignVH.VEHICLE_ID} of command id:{cmd.CMD_ID} vh current address:{assignVH.CUR_ADR_ID} ," +
                               $" by pass section{string.Join(",", need_by_pass_sec_ids)}",
                               VehicleID: assignVH.VEHICLE_ID,
                               CarrierID: assignVH.CST_ID);
                            break;
                        }
                    }
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Data: $"find the override path result:{isSuccess} vh:{assignVH.VEHICLE_ID} vh current address:{assignVH.CUR_ADR_ID} ," +
                       $". by pass section:{string.Join(",", need_by_pass_sec_ids)}",
                       VehicleID: assignVH.VEHICLE_ID,
                       CarrierID: assignVH.CST_ID);
                }
                while (!isSuccess && current_find_count++ < max_find_count);

                if (isSuccess)
                {
                    bool start_section_is_same = true;
                    if (guide_start_to_from_section_ids != null && guide_start_to_from_section_ids.Count > 0)
                    {
                        if (!assignVH.IsOnAdr)
                        {
                            start_section_is_same = SCUtility.isMatche(guide_start_to_from_section_ids[0], vh_current_section);
                            if (!start_section_is_same)
                            {
                                guide_start_to_from_section_ids.Insert(0, vh_current_section);
                                ASECTION new_start_section = scApp.SectionBLL.cache.GetSection(vh_current_section);
                                if (SCUtility.isMatche(guide_start_to_from_address_ids[0], new_start_section.FROM_ADR_ID))
                                {
                                    guide_start_to_from_address_ids.Insert(0, new_start_section.TO_ADR_ID);
                                }
                                else
                                {
                                    guide_start_to_from_address_ids.Insert(0, new_start_section.FROM_ADR_ID);
                                }
                                //if (!SCUtility.isMatche(guide_start_to_from_segment_ids[0], new_start_section.SEG_NUM))
                                //{
                                //    guide_start_to_from_segment_ids.Insert(0, new_start_section.SEG_NUM);
                                //}
                            }
                        }
                    }
                    else if (guide_to_dest_section_ids != null && guide_to_dest_section_ids.Count > 0)
                    {
                        if (!assignVH.IsOnAdr)
                        {
                            start_section_is_same = SCUtility.isMatche(guide_to_dest_section_ids[0], vh_current_section);
                            if (!start_section_is_same)
                            {
                                guide_to_dest_section_ids.Insert(0, vh_current_section);
                                ASECTION new_start_section = scApp.SectionBLL.cache.GetSection(vh_current_section);
                                if (SCUtility.isMatche(guide_to_dest_address_ids[0], new_start_section.FROM_ADR_ID))
                                {
                                    guide_to_dest_address_ids.Insert(0, new_start_section.TO_ADR_ID);
                                }
                                else
                                {
                                    guide_to_dest_address_ids.Insert(0, new_start_section.FROM_ADR_ID);
                                }
                                //if (!SCUtility.isMatche(guide_to_dest_segment_ids[0], new_start_section.SEG_NUM))
                                //{
                                //    guide_to_dest_segment_ids.Insert(0, new_start_section.SEG_NUM);
                                //}
                            }
                        }
                    }

                    //2.建立Cmd Details
                    List<string> guide_section_ids = new List<string>();
                    List<string> guide_segment_ids = new List<string>();

                    if (guide_start_to_from_section_ids == null && guide_to_dest_section_ids == null)
                    {
                        //Not thing....
                    }
                    else
                    {
                        if (guide_start_to_from_section_ids != null)
                            guide_section_ids.AddRange(guide_start_to_from_section_ids);
                        if (guide_to_dest_section_ids != null)
                            guide_section_ids.AddRange(guide_to_dest_section_ids);
                        scApp.CMDBLL.CeratCmdDerails(cmd_id, guide_section_ids.ToArray());
                    }
                    if (guide_start_to_from_segment_ids == null && guide_to_dest_segment_ids == null)
                    {
                        //Not thing....
                    }
                    else
                    {
                        if (guide_start_to_from_segment_ids != null)
                            guide_segment_ids.AddRange(guide_start_to_from_segment_ids);
                        if (guide_to_dest_segment_ids != null)
                            guide_segment_ids.AddRange(guide_to_dest_segment_ids);
                    }

                    //3.發送命令給VH
                    //在發送Override之前要先判斷目前的產生的路徑是否跟車子正在執行的路徑相同，如果還是一樣的話代表產生了一筆相同的路徑
                    //就不能再發送override了。
                    //2019 12 30 - 換成新的方式後，Override 可能是同一條路徑
                    //if (assignVH.PredictSections != null && assignVH.PredictSections.Count() > 0)
                    //{
                    //    List<string> assign_vh_current_predict_sections = assignVH.PredictSections.ToList();

                    //    bool is_match = isMatche(assign_vh_current_predict_sections, guide_section_ids);
                    //    //if (assign_vh_current_predict_sections.Except(guide_section_ids).Count() == 0)
                    //    if (is_match)
                    //    {
                    //        LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                    //           Data: $"find the override path :{string.Join(",", guide_section_ids)},but same with current vh predict path:{string.Join(",", assign_vh_current_predict_sections)} ," +
                    //           $"can't send override to vh" +
                    //           $"vh current address:{assignVH.CUR_ADR_ID} ," +
                    //           $". by pass address:{string.Join(",", need_by_pass_sec_ids)}",
                    //           VehicleID: assignVH.VEHICLE_ID,
                    //           CarrierID: assignVH.CST_ID);
                    //        return false;
                    //    }
                    //}
                    scApp.CMDBLL.updateCommand_OHTC_StatusByCmdID(vh_id, cmd.CMD_ID, E_CMD_STATUS.Sending);
                    isSuccess = ProcSendTransferCommandToVh(cmd, assignVH, ActiveType.Override,
                     guide_start_to_from_segment_ids?.ToArray(), guide_start_to_from_section_ids?.ToArray(), guide_start_to_from_address_ids?.ToArray(),
                     guide_to_dest_segment_ids?.ToArray(), guide_to_dest_section_ids?.ToArray(), guide_to_dest_address_ids?.ToArray(),
                     original_active_type);
                    //4.更新命令狀態(HOST CMD)
                    if (isSuccess)
                    {
                        scApp.ReserveBLL.RemoveAllReservedSectionsByVehicleID(assignVH.VEHICLE_ID);
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                           Data: $"override success remove vh:{vh_id} all reserved section.",
                           VehicleID: assignVH.VEHICLE_ID,
                           CarrierID: assignVH.CST_ID);

                        //在移除掉該VH所預約的路徑後，要再加入自己本身所在的Section，避免之後其他車子預約走
                        scApp.ReserveBLL.TryAddReservedSection(vh_id, vh_current_section);
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                           Data: $"override success append vh:{vh_id} current section:{vh_current_section}.",
                           VehicleID: assignVH.VEHICLE_ID,
                           CarrierID: assignVH.CST_ID);

                        if (!SCUtility.isEmpty(cmd.CMD_ID_MCS))
                        {
                            //TODO 在進行命令的改派後SysExecQity的資料要重新判斷一下要怎樣計算
                            //scApp.SysExcuteQualityBLL.updateSysExecQity_PassSecInfo(cmd.CMD_ID_MCS, assignVH.VEHICLE_ID, assignVH.CUR_SEC_ID,
                            //                        guide_to_dest_section_ids?.ToArray(), guide_to_dest_address_ids?.ToArray());
                        }


                        scApp.CMDBLL.setVhExcuteCmdToShow(cmd, assignVH, guide_segment_ids, guide_section_ids?.ToArray(), guide_start_to_from_address_ids?.ToArray(),
                                                          guide_start_to_from_section_ids, guide_to_dest_section_ids);


                        assignVH.sw_speed.Restart();
                    }
                    else
                    {
                        BCFApplication.onWarningMsg($"doSendOverrideCommandToVh fail.vh:{vh_id}, cmd id:{cmd_id},from:{source_adr},to:{dest_adr},active type:{active_type}." +
                                $"vh current adr:{vh_current_address},start section:{vh_current_section}");
                    }
                }
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: $"find the override path result:{isSuccess}, cmd id:{cmd_id},from:{source_adr},to:{dest_adr},active type:{active_type}." +
                         $"vh current adr:{vh_current_address},start section:{vh_current_section}." +
                         $" by pass section:{string.Join(",", need_by_pass_sec_ids)}",
                   VehicleID: assignVH.VEHICLE_ID,
                   CarrierID: assignVH.CST_ID);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exection:");
                isSuccess = false;
            }
            return isSuccess;
        }
        private bool checkWillPassSectionIsClear(string vhID, string beforeBlockSectionID)
        {
            SpinWait.SpinUntil(() => false, 1000);
            return scApp.ReserveBLL.TryAddReservedSection(vhID, beforeBlockSectionID, isAsk: true).OK;
        }

        public override bool trydoAvoidCommandToVh(AVEHICLE avoidVh, AVEHICLE passVh)
        {
            var find_avoid_result = findNotConflictSectionAndAvoidAddressNew(passVh, avoidVh, true);
            string blocked_section = avoidVh.CanNotReserveInfo.ReservedSectionID;
            string blocked_vh_id = avoidVh.CanNotReserveInfo.ReservedVhID;
            if (find_avoid_result.isFind)
            {
                avoidVh.VhAvoidInfo = null;
                //找到避車點之後，就可以下ID:51，開始讓vh進行避車了
                var avoid_request_result = AvoidRequest(avoidVh.VEHICLE_ID, find_avoid_result.avoidAdr,
                                                        needbyPassSectionID: blocked_section);
                if (avoid_request_result.is_success)
                {
                    avoidVh.VhAvoidInfo = new AVEHICLE.AvoidInfo(blocked_section, blocked_vh_id, avoid_request_result.guide_address_ids);
                }
                return avoid_request_result.is_success;
            }
            else
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: $"No find the can avoid address. avoid vh:{avoidVh.VEHICLE_ID} current adr:{avoidVh.CUR_ADR_ID}," +
                         $"will pass vh:{passVh.VEHICLE_ID} current adr:{passVh.CUR_ADR_ID}",
                   VehicleID: avoidVh.VEHICLE_ID,
                   CarrierID: avoidVh.CST_ID);
                return false;
            }
        }

        public override bool trydoAvoidCommandToVh(AVEHICLE avoidVh, List<string> pathAddr, List<string> pathSec)
        {
            avoidVh.VhAvoidInfo = null;
            //找到避車點之後，就可以下ID:51，開始讓vh進行避車了
            var avoid_request_result = AvoidRequest(avoidVh.VEHICLE_ID, pathAddr.LastOrDefault(), pathSec.ToArray(), pathAddr.ToArray());
            if (avoid_request_result)
            {
                avoidVh.VhAvoidInfo = new AVEHICLE.AvoidInfo("", "", pathAddr);

                scApp.ReserveBLL.RemoveAllReservedSectionsByVehicleID(avoidVh.VEHICLE_ID);
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: $"Avoid success remove vh:{avoidVh.VEHICLE_ID} all reserved section.",
                   VehicleID: avoidVh.VEHICLE_ID,
                   CarrierID: avoidVh.CST_ID);
                //在移除掉該VH所預約的路徑後，要再加入自己本身所在的Section，避免之後其他車子預約走
                scApp.ReserveBLL.TryAddReservedSection(avoidVh.VEHICLE_ID, avoidVh.CUR_SEC_ID);
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: $"Avoid success append vh:{avoidVh.VEHICLE_ID} current section:{avoidVh.CUR_SEC_ID}.",
                   VehicleID: avoidVh.VEHICLE_ID,
                   CarrierID: avoidVh.CST_ID);

                //2022.7.28 避車時同步更新路徑cache(WillPassSectionInfo & RedundantSections)
                avoidVh.RedundantSections.Clear();
                scApp.CMDBLL.setWillPassSectionInfo(avoidVh.VEHICLE_ID, pathSec);
            }
            return avoid_request_result;

        }

        private bool isMatche(List<string> list1, List<string> list2)
        {
            if (list1 == null || list2 == null) return false;
            if (list1.Count() != list2.Count()) return false;
            //List1 跟 List2取交集，如果交集的數量與List1(或Lsit2)一樣，代表兩個的List是一樣的
            var intersectedList = list1.Intersect(list2);
            if (intersectedList.Count() == list1.Count()) return true;
            return false;
        }

        public override bool doCancelOrAbortCommandByMCSCmdID(string cancel_abort_mcs_cmd_id, CMDCancelType actType)
        {
            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(NorthInnoLuxVehicleService), Device: DEVICE_NAME_AGV,
               Details: $"Enter doCancelOrAbortCommandByMCSCmdID method,mcs cmdid:[{cancel_abort_mcs_cmd_id}] cancel type:[{actType}].",
               XID: cancel_abort_mcs_cmd_id);
            ACMD_MCS mcs_cmd = scApp.CMDBLL.getCMD_MCSByID(cancel_abort_mcs_cmd_id);
            if (mcs_cmd == null)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(NorthInnoLuxVehicleService), Device: DEVICE_NAME_AGV,
                   Details: $"want to cancel/abort mcs cmd:{cancel_abort_mcs_cmd_id},but cmd not exist.",
                   XID: cancel_abort_mcs_cmd_id);
                return false;
            }
            bool is_success = true;
            switch (actType)
            {
                case CMDCancelType.CmdCancel:
                    scApp.ReportBLL.newReportTransferCancelInitial(mcs_cmd, null);

                    if (mcs_cmd.TRANSFERSTATE == E_TRAN_STATUS.Queue)
                    {
                        scApp.CMDBLL.updateCMD_MCS_TranStatus2Canceled(cancel_abort_mcs_cmd_id);
                        scApp.ReportBLL.newReportTransferCancelCompleted(mcs_cmd, null);
                    }
                    else if (mcs_cmd.TRANSFERSTATE > E_TRAN_STATUS.Queue) //北群創只要命令開始執行就不能Cancel了
                    {
                        scApp.ReportBLL.newReportTransferCancelFailed(mcs_cmd.CMD_ID, null);
                    }
                    break;
                case CMDCancelType.CmdAbort:
                    scApp.ReportBLL.newReportTransferAbortInitial(cancel_abort_mcs_cmd_id, null);
                    if (mcs_cmd.TRANSFERSTATE >= E_TRAN_STATUS.Queue && mcs_cmd.TRANSFERSTATE < E_TRAN_STATUS.Initial)
                    {
                        scApp.ReportBLL.newReportTransferAbortFailed(cancel_abort_mcs_cmd_id, null);
                    }
                    else if (mcs_cmd.TRANSFERSTATE >= E_TRAN_STATUS.Initial)
                    {
                        if (mcs_cmd.COMMANDSTATE == TaskCmdStatus.BCRReadFail)
                        {
                            List<AMCSREPORTQUEUE> reportQueue = new List<AMCSREPORTQUEUE>();
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(NorthInnoLuxVehicleService), Device: DEVICE_NAME_AGV,
                               Details: $" doCancelOrAbortCommandByMCSCmdID Enter BCRReadFail senario,mcs cmdid:[{cancel_abort_mcs_cmd_id}]].",
                               XID: cancel_abort_mcs_cmd_id);
                            AVEHICLE assign_vh = scApp.VehicleBLL.getVehicleByExcuteMCS_CMD_ID(cancel_abort_mcs_cmd_id);
                            scApp.CMDBLL.updateCMD_MCS_TranStatus2Aborting(cancel_abort_mcs_cmd_id);
                            is_success = scApp.ReportBLL.newReportTransferCommandAbortFinish(mcs_cmd, assign_vh,"8", reportQueue);
                            if (is_success)
                            {
                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(NorthInnoLuxVehicleService), Device: DEVICE_NAME_AGV,
                               Details: $" doCancelOrAbortCommandByMCSCmdID end BCRReadFail senario success,mcs cmdid:[{cancel_abort_mcs_cmd_id}].",
                               XID: cancel_abort_mcs_cmd_id);


                            }
                            else
                            {
                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(NorthInnoLuxVehicleService), Device: DEVICE_NAME_AGV,
                                Details: $" doCancelOrAbortCommandByMCSCmdID end BCRReadFail senario fail,mcs cmdid:[{cancel_abort_mcs_cmd_id}].",
                                XID: cancel_abort_mcs_cmd_id);
                            }

                            using (TransactionScope tx = SCUtility.getTransactionScope())
                            {
                                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                                {
                                    bool isSuccess = true;
                                    //isSuccess &= scApp.VehicleBLL.doTransferCommandFinish(vh.VEHICLE_ID, cmd_id, completeStatus, travel_dis);
                                    isSuccess &= scApp.VehicleBLL.doTransferCommandFinish(assign_vh.VEHICLE_ID, assign_vh.OHTC_CMD, CompleteStatus.CmpStatusIdreadFailed, 0);
                                    //isSuccess &= scApp.VIDBLL.initialVIDCommandInfo(vh.VEHICLE_ID);
                                    isSuccess &= scApp.VIDBLL.initialVIDCommandInfo(assign_vh.VEHICLE_ID);

                                    if (isSuccess)
                                    {
                                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(NorthInnoLuxVehicleService), Device: DEVICE_NAME_AGV,
                                        Details: $" doCancelOrAbortCommandByMCSCmdID BCRReadFail senario delete Cmd success,mcs cmdid:[{cancel_abort_mcs_cmd_id}].",
                                        XID: cancel_abort_mcs_cmd_id);

                                        tx.Complete();
                                    }
                                    else
                                    {
                                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(NorthInnoLuxVehicleService), Device: DEVICE_NAME_AGV,
                                Details: $" doCancelOrAbortCommandByMCSCmdID BCRReadFail senario delete Cmd fail,mcs cmdid:[{cancel_abort_mcs_cmd_id}].",
                                XID: cancel_abort_mcs_cmd_id);
                                    }
                                }
                            }
                            assign_vh.onCommandComplete(CompleteStatus.CmpStatusIdreadFailed);
                            //is_success &= scApp.ReportBLL.newReportTransferCommandAbortFinish(mcs_cmd, assign_vh, "8", null);
                            if (reportQueue != null && reportQueue.Count > 0)
                                scApp.ReportBLL.newSendMCSMessage(reportQueue);

                        }
                        else
                        {
                            is_success = scApp.VehicleService.cancleOrAbortCommandByMCSCmdID(cancel_abort_mcs_cmd_id, ProtocolFormat.OHTMessage.CMDCancelType.CmdAbort);
                            if (is_success)
                            {
                                scApp.CMDBLL.updateCMD_MCS_TranStatus2Aborting(cancel_abort_mcs_cmd_id);
                            }
                            else
                            {
                                scApp.ReportBLL.newReportTransferAbortFailed(cancel_abort_mcs_cmd_id, null);
                            }
                        }
                    }
                    break;
            }
            return is_success;
        }

        //取消給AGV的命令但不上報給MCS
        public bool doCancelCommandByMCSCmdIDWithNoReport(string cancel_abort_mcs_cmd_id, CMDCancelType actType)
        {
            ACMD_MCS mcs_cmd = scApp.CMDBLL.getCMD_MCSByID(cancel_abort_mcs_cmd_id);
            if (mcs_cmd == null)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Details: $"want to cancel/abort mcs cmd:{cancel_abort_mcs_cmd_id},but cmd not exist.",
                   XID: cancel_abort_mcs_cmd_id);
                return false;
            }
            bool is_success = true;
            switch (actType)
            {
                case CMDCancelType.CmdCancel:
                    //scApp.ReportBLL.newReportTransferCancelInitial(mcs_cmd, null);
                    if (mcs_cmd.TRANSFERSTATE == E_TRAN_STATUS.Queue)
                    {
                        return false;
                    }
                    else if (mcs_cmd.TRANSFERSTATE >= E_TRAN_STATUS.Queue && mcs_cmd.TRANSFERSTATE < E_TRAN_STATUS.Transferring)
                    {
                        AVEHICLE assign_vh = null;
                        assign_vh = scApp.VehicleBLL.getVehicleByExcuteMCS_CMD_ID(cancel_abort_mcs_cmd_id);
                        string ohtc_cmd_id = assign_vh.OHTC_CMD;
                        //assign_vh = scApp.VehicleBLL.getVehicleByExcuteMCS_CMD_ID(cancel_abort_mcs_cmd_id);
                        is_success = doAbortCommand(assign_vh, ohtc_cmd_id, actType);
                        return is_success;
                    }
                    else if (mcs_cmd.TRANSFERSTATE >= E_TRAN_STATUS.Transferring) //當狀態變為Transferring時，即代表已經是Load complete
                    {
                        return false;
                    }
                    break;
                case CMDCancelType.CmdAbort:
                    //do nothing
                    break;
            }
            return is_success;
        }
        public override bool cancleOrAbortCommandByMCSCmdID(string mcsCmdID, CMDCancelType actType)
        {
            bool isSuccess = true;
            AVEHICLE assign_vh = null;
            try
            {
                assign_vh = scApp.VehicleBLL.getVehicleByExcuteMCS_CMD_ID(mcsCmdID);
                string ohtc_cmd_id = assign_vh.OHTC_CMD;
                switch (actType)
                {
                    case CMDCancelType.CmdAbort:
                        if (assign_vh.VhRecentTranEvent == EventType.Vhunloading) return false;
                        scApp.CMDBLL.updateCMD_MCS_TranStatus2Aborting(mcsCmdID);
                        break;
                    case CMDCancelType.CmdCancel:
                        if (assign_vh.VhRecentTranEvent == EventType.Vhloading) return false;
                        scApp.CMDBLL.updateCMD_MCS_TranStatus2Canceling(mcsCmdID);
                        break;
                }
                //bool pause_success = false;
                bool is_success = true;
                //is_success = is_success && PauseRequest(assign_vh.VEHICLE_ID, PauseEvent.Pause, SCAppConstants.OHxCPauseType.Normal);
                //if (is_success)
                //{
                //    pause_success = true;
                //}
                is_success = is_success && FinishExecutionMCSCommand(assign_vh.VEHICLE_ID);
                //if (!is_success && pause_success)
                //{
                //    PauseRequest(assign_vh.VEHICLE_ID, PauseEvent.Continue, SCAppConstants.OHxCPauseType.Normal);
                //}
            }
            catch (Exception ex)
            {
                isSuccess = false;
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: ex,
                   VehicleID: assign_vh?.VEHICLE_ID,
                   CarrierID: assign_vh?.CST_ID,
                   Details: $"abort command fail mcs command id:{mcsCmdID}");
            }
            return isSuccess;
        }
        public bool doAbortCommand(AVEHICLE assign_vh, string cmd_id, CMDCancelType actType)
        {
            var abort_result = assign_vh.sned_Str37(cmd_id, actType);
            if (abort_result.isSendOK)
            {
                if (abort_result.replyCode == 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }


        private bool ProcSendTransferCommandToVh(ACMD_OHTC cmd, AVEHICLE assignVH, ActiveType activeType,
            string[] guideSegmentStartToLoad, string[] guideSectionsStartToLoad, string[] guideAddressesStartToLoad,
            string[] guideSegmentToDest, string[] guideSectionsToDest, string[] guideAddressesToDest,
            ActiveType originalAactiveType = ActiveType.Home)
        {
            bool isSuccess = true;
            string vh_id = assignVH.VEHICLE_ID;
            try
            {
                List<AMCSREPORTQUEUE> reportqueues = new List<AMCSREPORTQUEUE>();
                using (var tx = SCUtility.getTransactionScope())
                {
                    using (DBConnection_EF con = DBConnection_EF.GetUContext())
                    {
                        switch (cmd.CMD_TPYE)
                        {
                            case E_CMD_TYPE.Move_Park:
                                APARKZONEDETAIL aPARKZONEDETAIL = scApp.ParkBLL.getParkDetailByAdr(cmd.DESTINATION);
                                if (assignVH.IS_PARKING)
                                {
                                    scApp.ParkBLL.resetParkAdr(assignVH.PARK_ADR_ID);
                                }
                                scApp.VehicleBLL.setVhIsParkingOnWay(cmd.VH_ID, cmd.DESTINATION);
                                break;
                            default:
                                if (assignVH.IS_PARKING
                                    || !SCUtility.isEmpty(assignVH.PARK_ADR_ID))
                                {
                                    //改成找出該VH是停在哪個位置，並更新狀態
                                    scApp.ParkBLL.resetParkAdr(assignVH.PARK_ADR_ID);
                                    scApp.VehicleBLL.resetVhIsInPark(assignVH.VEHICLE_ID);
                                }
                                break;
                        }

                        //isSuccess &= scApp.CMDBLL.updateCommand_OHTC_StatusByCmdID(cmd.CMD_ID, E_CMD_STATUS.Execution);
                        isSuccess &= scApp.CMDBLL.updateCommand_OHTC_StatusByCmdID(vh_id, cmd.CMD_ID, E_CMD_STATUS.Execution);
                        if (activeType != ActiveType.Override)
                        {
                            isSuccess &= scApp.VehicleBLL.updateVehicleExcuteCMD(cmd.VH_ID, cmd.CMD_ID, cmd.CMD_ID_MCS);

                            if (!SCUtility.isEmpty(cmd.CMD_ID_MCS))
                            {
                                isSuccess &= scApp.VIDBLL.upDateVIDCommandInfo(cmd.VH_ID, cmd.CMD_ID_MCS);
                                if (!cmd.CMD_ID.StartsWith(((int)GenOHxCCommandType.Retry).ToString()))//retry命令不用發
                                {
                                    isSuccess &= scApp.ReportBLL.newReportBeginTransfer(assignVH.VEHICLE_ID, reportqueues);
                                    scApp.ReportBLL.insertMCSReport(reportqueues);
                                }
                            }
                        }

                        if (isSuccess)
                        {

                            isSuccess &= TransferRequset
                                (cmd.VH_ID, cmd.CMD_ID, activeType, cmd.CARRIER_ID,
                               guideSegmentStartToLoad, guideSectionsStartToLoad, guideAddressesStartToLoad,
                               guideSegmentToDest, guideSectionsToDest, guideAddressesToDest,
                                cmd.SOURCE, cmd.DESTINATION, originalAactiveType);
                            //isSuccess &= assignVH.sned_Str31(cmd.CMD_ID, activeType, cmd.CARRIER_ID, routeSections, cycleRunSections
                            //    , cmd.SOURCE, cmd.DESTINATION, out Reason);
                        }
                        if (isSuccess)
                        {
                            tx.Complete();
                        }
                        else
                        {
                            scApp.getEQObjCacheManager().restoreVhDataFromDB(assignVH);
                        }
                    }
                }

                if (isSuccess)
                {
                    scApp.ReportBLL.newSendMCSMessage(reportqueues);
                    Task.Run(() => scApp.FlexsimCommandDao.setCommandToFlexsimDB(cmd));
                }
                else
                {
                    scApp.getEQObjCacheManager().restoreVhDataFromDB(assignVH);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exection:");
                isSuccess = false;
                scApp.getEQObjCacheManager().restoreVhDataFromDB(assignVH);
            }
            return isSuccess;
        }
        public bool TransferRequset(string vh_id, string cmd_id, ActiveType activeType, string cst_id,
            string[] guideSegmentStartToLoad, string[] guideSectionsStartToLoad, string[] guideAddressesStartToLoad,
            string[] guideSegmentToDest, string[] guideSectionsToDest, string[] guideAddressesToDest,
            string fromAdr, string destAdr, ActiveType originalAactiveType = ActiveType.Home)
        {
            //TODO 要在加入Transfer Command的確認 scApp.CMDBLL.TransferCommandCheck(activeType,) 
            bool isSuccess = true;
            string reason = string.Empty;
            ID_131_TRANS_RESPONSE receive_gpp = null;
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vh_id);
            //if (isSuccess)
            //{
            ID_31_TRANS_REQUEST send_gpp = new ID_31_TRANS_REQUEST()
            {
                CmdID = cmd_id,
                ActType = activeType,
                CSTID = cst_id ?? string.Empty,
                LoadAdr = fromAdr,
                DestinationAdr = destAdr
            };
            if (guideSegmentStartToLoad != null)
                send_gpp.GuideSegmentsStartToLoad.AddRange(guideSegmentStartToLoad);
            if (guideSectionsStartToLoad != null)
                send_gpp.GuideSectionsStartToLoad.AddRange(guideSectionsStartToLoad);
            if (guideAddressesStartToLoad != null)
                send_gpp.GuideAddressesStartToLoad.AddRange(guideAddressesStartToLoad);
            if (guideSegmentToDest != null)
                send_gpp.GuideSegmentsToDestination.AddRange(guideSegmentToDest);
            if (guideSectionsToDest != null)
                send_gpp.GuideSectionsToDestination.AddRange(guideSectionsToDest);
            if (guideAddressesToDest != null)
                send_gpp.GuideAddressesToDestination.AddRange(guideAddressesToDest);
            SCUtility.RecodeReportInfo(vh.VEHICLE_ID, 0, send_gpp);
            isSuccess = vh.sned_Str31(send_gpp, out receive_gpp, out reason);
            SCUtility.RecodeReportInfo(vh.VEHICLE_ID, 0, receive_gpp, isSuccess.ToString());
            //}
            if (isSuccess)
            {
                int reply_code = receive_gpp.ReplyCode;
                if (reply_code != 0)
                {
                    isSuccess = false;
                    var return_code_map = scApp.CMDBLL.getReturnCodeMap(vh.NODE_ID, reply_code.ToString());
                    if (return_code_map != null)
                        reason = return_code_map.DESC;
                    bcf.App.BCFApplication.onWarningMsg(string.Format("發送命令失敗,VH ID:{0}, CMD ID:{1}, Reason:{2}",
                                                              vh_id,
                                                              cmd_id,
                                                              reason));
                }
                else
                {
                    vh.setVhGuideInfo(scApp.ReserveBLL, send_gpp, originalAactiveType);
                }
                vh.NotifyVhExcuteCMDStatusChange();
            }
            else
            {
                bcf.App.BCFApplication.onWarningMsg(string.Format("發送命令失敗,VH ID:{0}, CMD ID:{1}, Reason:{2}",
                                          vh_id,
                                          cmd_id,
                                          reason));
                VehicleStatusRequest(vh_id, true);
            }
            return isSuccess;
        }
        public override bool CarrierIDRenameRequset(string vh_id, string newCarrierID, string oldCarrierID)
        {
            bool isSuccess = true;

            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vh_id);
            ID_135_CST_ID_RENAME_RESPONSE receive_gpp;
            ID_35_CST_ID_RENAME_REQUEST send_gpp = new ID_35_CST_ID_RENAME_REQUEST()
            {
                OLDCSTID = oldCarrierID ?? string.Empty,
                NEWCSTID = newCarrierID ?? string.Empty,
            };
            SCUtility.RecodeReportInfo(vh.VEHICLE_ID, 0, send_gpp);
            isSuccess = vh.sned_Str35(send_gpp, out receive_gpp);
            SCUtility.RecodeReportInfo(vh.VEHICLE_ID, 0, receive_gpp, isSuccess.ToString());
            if (isSuccess)
            {
                using (TransactionScope tx = SCUtility.getTransactionScope())
                {
                    using (DBConnection_EF con = DBConnection_EF.GetUContext())
                    {
                        scApp.VIDBLL.upDateVIDMCSCarrierID(vh_id, newCarrierID);
                        scApp.VIDBLL.upDateVIDCarrierID(vh_id, newCarrierID);
                        scApp.VehicleBLL.updataVehicleCSTID(vh_id, newCarrierID);
                        tx.Complete();
                    }
                }
            }
            return isSuccess;
        }

        public bool DoCarrierIDRenameRequset(string cmd_id, string newCarrierID, string oldCarrierID)
        {
            ACMD_MCS mcs_cmd = scApp.CMDBLL.getCMD_MCSByID(cmd_id);
            if (mcs_cmd == null)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Details: $"want to cancel/abort mcs cmd:{cmd_id},but cmd not exist.",
                   XID: cmd_id);
                return false;
            }
            bool isSuccess = true;
            if (mcs_cmd.TRANSFERSTATE == E_TRAN_STATUS.Queue)
            {
                scApp.CMDBLL.updateCMD_MCS_CarrierID(cmd_id, newCarrierID);
                //scApp.CMDBLL.updateCMD_MCS_TranStatus2Canceled(cancel_abort_mcs_cmd_id);
                //scApp.ReportBLL.newReportTransferCancelCompleted(mcs_cmd, null);
            }
            else if (mcs_cmd.TRANSFERSTATE >= E_TRAN_STATUS.Queue)
            {
                AVEHICLE assign_vh = scApp.VehicleBLL.getVehicleByExcuteMCS_CMD_ID(cmd_id);
                isSuccess = CarrierIDRenameRequset(assign_vh.VEHICLE_ID, newCarrierID, oldCarrierID);
                if (isSuccess)
                {
                    scApp.CMDBLL.updateCMD_MCS_CarrierID(cmd_id, newCarrierID);
                    scApp.CMDBLL.updateCommand_OHTC_CarrierID(assign_vh.OHTC_CMD, newCarrierID);
                }
                else
                {
                    //do nothing
                }
            }
            return isSuccess;
        }


        public bool TeachingRequest(string vh_id, string from_adr, string to_adr)
        {
            bool isSuccess = false;
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vh_id);
            ID_171_RANGE_TEACHING_RESPONSE receive_gpp;
            ID_71_RANGE_TEACHING_REQUEST send_gpp = new ID_71_RANGE_TEACHING_REQUEST()
            {
                FromAdr = from_adr,
                ToAdr = to_adr
            };

            SCUtility.RecodeReportInfo(vh.VEHICLE_ID, 0, send_gpp);
            isSuccess = vh.send_Str71(send_gpp, out receive_gpp);
            SCUtility.RecodeReportInfo(vh.VEHICLE_ID, 0, receive_gpp, isSuccess.ToString());

            return isSuccess;
        }



        public (bool is_success, string result, List<string> guide_address_ids) AvoidRequest(string vh_id, string avoidAddress, string needbyPassSectionID = null)
        {
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vh_id);
            List<string> guide_segment_ids = null;
            List<string> guide_section_ids = null;
            List<string> guide_address_ids = null;
            int total_cost = 0;
            bool is_success = true;
            string result = "";
            string vh_current_address = SCUtility.Trim(vh.CUR_ADR_ID, true);
            try
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: $"Start vh:{vh_id} avoid script,avoid to address:{avoidAddress}...",
                   VehicleID: vh_id,
                   CarrierID: vh.CST_ID);

                if (SCUtility.isEmpty(vh.OHTC_CMD))
                {
                    is_success = false;
                    result = $"vh:{vh_id} not excute ohtc command.";
                }
                if (!vh.IsReservePause)
                {
                    is_success = false;
                    result = $"vh:{vh_id} current not in reserve pause.";
                }
                List<string> need_by_pass_sec_ids = new List<string>();
                if (!SCUtility.isEmpty(needbyPassSectionID))
                {
                    need_by_pass_sec_ids.Add(needbyPassSectionID);
                }

                if (is_success)
                {
                    int current_find_count = 0;
                    int max_find_count = 10;
                    do
                    {
                        //確認下一段Section，是否可以預約成功
                        string next_walk_section = "";
                        string next_walk_address = "";


                        //(is_success, guide_segment_ids, guide_section_ids, guide_address_ids, total_cost) =
                        //    scApp.GuideBLL.getGuideInfo_New2(vh_current_section, vh_current_address, avoidAddress);
                        (is_success, guide_segment_ids, guide_section_ids, guide_address_ids, total_cost) =
                                                    scApp.GuideBLL.getGuideInfo(vh_current_address, avoidAddress, need_by_pass_sec_ids);
                        next_walk_section = guide_section_ids[0];
                        next_walk_address = guide_address_ids[0];

                        if (is_success)
                        {

                            var reserve_result = askReserveSuccess(vh_id, next_walk_section, next_walk_address);
                            if (reserve_result.isSuccess)
                            {
                                is_success = true;
                            }
                            else
                            {
                                is_success = false;
                                need_by_pass_sec_ids.Add(next_walk_section);
                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                   Data: $"find the avoid path ,but section:{next_walk_section} is reserved for vh:{reserve_result.reservedVhID}" +
                                         $"add to need by pass sec ids,current by pass section:{string.Join(",", need_by_pass_sec_ids)}",
                                   VehicleID: vh.VEHICLE_ID,
                                   CarrierID: vh.CST_ID);
                            }
                        }
                        if (current_find_count++ > max_find_count)
                        {
                            is_success = false;
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                               Data: $"find the avoid path ,but over times:{max_find_count}",
                               VehicleID: vh.VEHICLE_ID,
                               CarrierID: vh.CST_ID);
                            break;
                        }
                    } while (!is_success);

                    string vh_current_section = SCUtility.Trim(vh.CUR_SEC_ID, true);
                    if (is_success)
                    {
                        //根據車子所在位置，補足路段
                        bool start_section_is_same = true;
                        if (guide_section_ids != null && guide_section_ids.Count > 0)
                        {
                            if (!vh.IsOnAdr)
                            {
                                start_section_is_same = SCUtility.isMatche(guide_section_ids[0], vh_current_section);
                                if (!start_section_is_same)
                                {
                                    guide_section_ids.Insert(0, vh_current_section);
                                    ASECTION new_start_section = scApp.SectionBLL.cache.GetSection(vh_current_section);
                                    if (SCUtility.isMatche(guide_address_ids[0], new_start_section.FROM_ADR_ID))
                                    {
                                        guide_address_ids.Insert(0, new_start_section.TO_ADR_ID);
                                    }
                                    else
                                    {
                                        guide_address_ids.Insert(0, new_start_section.FROM_ADR_ID);
                                    }
                                    //if (!SCUtility.isMatche(guide_segment_ids[0], new_start_section.SEG_NUM))
                                    //{
                                    //    guide_segment_ids.Insert(0, new_start_section.SEG_NUM);
                                    //}
                                }
                            }
                        }
                        vh.IsPrepareAvoid = true;
                        is_success = AvoidRequest(vh_id, avoidAddress, guide_section_ids.ToArray(), guide_address_ids.ToArray());
                        if (!is_success)
                        {
                            result = $"send avoid to vh fail.vh:{vh_id}, vh current adr:{vh_current_address} ,avoid address:{avoidAddress}.";
                        }
                        else
                        {
                            scApp.ReserveBLL.RemoveAllReservedSectionsByVehicleID(vh_id);
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                               Data: $"Avoid success remove vh:{vh_id} all reserved section.",
                               VehicleID: vh.VEHICLE_ID,
                               CarrierID: vh.CST_ID);
                            //在移除掉該VH所預約的路徑後，要再加入自己本身所在的Section，避免之後其他車子預約走
                            scApp.ReserveBLL.TryAddReservedSection(vh_id, vh_current_section);
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                               Data: $"Avoid success append vh:{vh_id} current section:{vh_current_section}.",
                               VehicleID: vh.VEHICLE_ID,
                               CarrierID: vh.CST_ID);

                            //2022.7.28 避車時同步更新路徑cache(WillPassSectionInfo & RedundantSections)
                            vh.RedundantSections.Clear();
                            scApp.CMDBLL.setWillPassSectionInfo(vh_id, guide_section_ids);
                        }
                    }
                    else
                    {
                        result = $"find avoid path fail.vh:{vh_id}, vh current adr:{vh_current_address} ,avoid address:{avoidAddress}.";
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: ex,
                   Details: $"AvoidRequest fail.vh:{vh_id}, vh current adr:{vh_current_address} ,avoid address:{avoidAddress}.",
                   VehicleID: vh_id);
            }
            finally
            {
                vh.IsPrepareAvoid = false;
            }
            return (is_success, result, guide_address_ids);
        }
        public bool AvoidRequest(string vh_id, string avoidAddress, string[] guideSection, string[] guideAddresses)
        {
            bool isSuccess = false;
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vh_id);
            ID_151_AVOID_RESPONSE receive_gpp;
            ID_51_AVOID_REQUEST send_gpp = new ID_51_AVOID_REQUEST();
            send_gpp.DestinationAdr = avoidAddress;
            send_gpp.GuideSections.AddRange(guideSection);
            send_gpp.GuideAddresses.AddRange(guideAddresses);

            SCUtility.RecodeReportInfo(vh.VEHICLE_ID, 0, send_gpp);
            isSuccess = vh.send_Str51(send_gpp, out receive_gpp);
            SCUtility.RecodeReportInfo(vh.VEHICLE_ID, 0, receive_gpp, isSuccess.ToString());
            if (isSuccess)
            {
                if (receive_gpp.ReplyCode == 0)
                {
                    vh.setVhGuideInfo(scApp.ReserveBLL, send_gpp);
                }
            }
            return isSuccess;
        }
        public (bool is_success, string result, List<string> guide_address_ids) AvoidRequest(string vh_id, string avoidAddress, List<string> needbyPassSectionID = null)
        {
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vh_id);
            List<string> guide_segment_ids = null;
            List<string> guide_section_ids = null;
            List<string> guide_address_ids = null;
            int total_cost = 0;
            bool is_success = true;
            string result = "";
            string vh_current_address = SCUtility.Trim(vh.CUR_ADR_ID, true);
            try
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: $"Start vh:{vh_id} avoid script,avoid to address:{avoidAddress}...",
                   VehicleID: vh_id,
                   CarrierID: vh.CST_ID);

                if (SCUtility.isEmpty(vh.OHTC_CMD))
                {
                    is_success = false;
                    result = $"vh:{vh_id} not excute ohtc command.";
                }
                if (!vh.IsReservePause)
                {
                    is_success = false;
                    result = $"vh:{vh_id} current not in reserve pause.";
                }
                List<string> need_by_pass_sec_ids = new List<string>();
                if (needbyPassSectionID != null)
                {
                    need_by_pass_sec_ids.AddRange(needbyPassSectionID);
                }

                if (is_success)
                {
                    int current_find_count = 0;
                    int max_find_count = 10;
                    do
                    {
                        //確認下一段Section，是否可以預約成功
                        string next_walk_section = "";
                        string next_walk_address = "";


                        //(is_success, guide_segment_ids, guide_section_ids, guide_address_ids, total_cost) =
                        //    scApp.GuideBLL.getGuideInfo_New2(vh_current_section, vh_current_address, avoidAddress);
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                            Data: $"find avoid path to {avoidAddress}, current by pass section:{string.Join(",", need_by_pass_sec_ids)}",
                            VehicleID: vh.VEHICLE_ID,
                            CarrierID: vh.CST_ID);

                        (is_success, guide_segment_ids, guide_section_ids, guide_address_ids, total_cost) =
                                                    scApp.GuideBLL.getGuideInfo(vh_current_address, avoidAddress, need_by_pass_sec_ids);

                        if (is_success)
                        {
                            next_walk_section = guide_section_ids[0];
                            next_walk_address = guide_address_ids[0];
                            var reserve_result = askReserveSuccess(vh_id, next_walk_section, next_walk_address);
                            if (reserve_result.isSuccess)
                            {
                                is_success = true;
                            }
                            else
                            {
                                is_success = false;
                                need_by_pass_sec_ids.Add(next_walk_section);
                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                   Data: $"find the avoid path ,but section:{next_walk_section} is reserved for vh:{reserve_result.reservedVhID}" +
                                         $"add to need by pass sec ids,current by pass section:{string.Join(",", need_by_pass_sec_ids)}",
                                   VehicleID: vh.VEHICLE_ID,
                                   CarrierID: vh.CST_ID);
                            }
                        }
                        else
                        {
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                Data: $"find avoid path to {avoidAddress}, but no avaliable path found.",
                                VehicleID: vh.VEHICLE_ID,
                                CarrierID: vh.CST_ID);
                        }
                        if (current_find_count++ > max_find_count)
                        {
                            is_success = false;
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                               Data: $"find the avoid path ,but over times:{max_find_count}",
                               VehicleID: vh.VEHICLE_ID,
                               CarrierID: vh.CST_ID);
                            break;
                        }
                    } while (!is_success);

                    string vh_current_section = SCUtility.Trim(vh.CUR_SEC_ID, true);
                    if (is_success)
                    {
                        //根據車子所在位置，補足路段
                        bool start_section_is_same = true;
                        if (guide_section_ids != null && guide_section_ids.Count > 0)
                        {
                            if (!vh.IsOnAdr)
                            {
                                start_section_is_same = SCUtility.isMatche(guide_section_ids[0], vh_current_section);
                                if (!start_section_is_same)
                                {
                                    guide_section_ids.Insert(0, vh_current_section);
                                    ASECTION new_start_section = scApp.SectionBLL.cache.GetSection(vh_current_section);
                                    if (SCUtility.isMatche(guide_address_ids[0], new_start_section.FROM_ADR_ID))
                                    {
                                        guide_address_ids.Insert(0, new_start_section.TO_ADR_ID);
                                    }
                                    else
                                    {
                                        guide_address_ids.Insert(0, new_start_section.FROM_ADR_ID);
                                    }
                                }
                            }
                        }
                        vh.IsPrepareAvoid = true;
                        is_success = AvoidRequest(vh_id, avoidAddress, guide_section_ids.ToArray(), guide_address_ids.ToArray());
                        if (!is_success)
                        {
                            result = $"send avoid to vh fail.vh:{vh_id}, vh current adr:{vh_current_address} ,avoid address:{avoidAddress}.";
                        }
                        else
                        {
                            scApp.ReserveBLL.RemoveAllReservedSectionsByVehicleID(vh_id);
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                               Data: $"Avoid success remove vh:{vh_id} all reserved section.",
                               VehicleID: vh.VEHICLE_ID,
                               CarrierID: vh.CST_ID);
                            //在移除掉該VH所預約的路徑後，要再加入自己本身所在的Section，避免之後其他車子預約走
                            scApp.ReserveBLL.TryAddReservedSection(vh_id, vh_current_section);
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                               Data: $"Avoid success append vh:{vh_id} current section:{vh_current_section}.",
                               VehicleID: vh.VEHICLE_ID,
                               CarrierID: vh.CST_ID);

                            //2022.7.28 避車時同步更新路徑cache(WillPassSectionInfo & RedundantSections)
                            vh.RedundantSections.Clear();
                            scApp.CMDBLL.setWillPassSectionInfo(vh_id, guide_section_ids);
                        }
                    }
                    else
                    {
                        result = $"find avoid path fail.vh:{vh_id}, vh current adr:{vh_current_address} ,avoid address:{avoidAddress}.";
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: ex,
                   Details: $"AvoidRequest fail.vh:{vh_id}, vh current adr:{vh_current_address} ,avoid address:{avoidAddress}.",
                   VehicleID: vh_id);
            }
            finally
            {
                vh.IsPrepareAvoid = false;
            }
            return (is_success, result, guide_address_ids);
        }
        #endregion Send Message To Vehicle

        #region Position Report
        [ClassAOPAspect]
        public void PositionReport(BCFApplication bcfApp, AVEHICLE vh, ID_134_TRANS_EVENT_REP receiveStr)
        {
            if (scApp.getEQObjCacheManager().getLine().ServerPreStop)
                return;
            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
               seq_num: 0, //由於Position Report的資料可能從很多地方來，例如143、144、PLC、136 因此在此先不考慮其seq_num
               Data: receiveStr,
               VehicleID: vh.VEHICLE_ID,
               CarrierID: vh.CST_ID);
            //SCUtility.RecodeReportInfo(eqpt.VEHICLE_ID, 0, recive_str);
            EventType eventType = receiveStr.EventType;
            string current_adr_id = SCUtility.isEmpty(receiveStr.CurrentAdrID) ? string.Empty : receiveStr.CurrentAdrID;
            string current_sec_id = SCUtility.isEmpty(receiveStr.CurrentSecID) ? string.Empty : receiveStr.CurrentSecID;
            ASECTION sec_obj = scApp.SectionBLL.cache.GetSection(current_sec_id);
            string current_seg_id = sec_obj == null ? string.Empty : sec_obj.SEG_NUM;
            string last_adr_id = vh.CUR_ADR_ID;
            string last_sec_id = vh.CUR_SEC_ID;
            uint sec_dis = receiveStr.SecDistance;
            switch (eventType)
            {
                case EventType.AdrPass:
                    lock (vh.BlockControl_SyncForRedis)
                    {
                        if (scApp.MapBLL.IsBlockControlStatus
                            (vh.VEHICLE_ID, SCAppConstants.BlockQueueState.Blocking))
                        {
                            BLOCKZONEQUEUE throuBlockQueue = null;
                            if (scApp.MapBLL.updateBlockZoneQueue_ThrouTime(vh.VEHICLE_ID, out throuBlockQueue))
                            {
                                scApp.MapBLL.ChangeBlockControlStatus_Through(vh.VEHICLE_ID);
                            }
                        }
                    }
                    break;
                case EventType.AdrOrMoveArrivals:
                    scApp.VehicleBLL.doAdrArrivals(vh.VEHICLE_ID, current_adr_id, current_sec_id);
                    break;
            }
        }
        #endregion Position Report
        #region Transfer Report
        [ClassAOPAspect]
        public override void TranEventReport(BCFApplication bcfApp, AVEHICLE eqpt, ID_136_TRANS_EVENT_REP recive_str, int seq_num)
        {
            if (scApp.getEQObjCacheManager().getLine().ServerPreStop)
                return;

            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
               seq_num: seq_num,
               Data: recive_str,
               VehicleID: eqpt.VEHICLE_ID,
               CarrierID: eqpt.CST_ID);
            SCUtility.RecodeReportInfo(eqpt.VEHICLE_ID, seq_num, recive_str);
            EventType eventType = recive_str.EventType;
            string current_adr_id = recive_str.CurrentAdrID;
            string current_sec_id = recive_str.CurrentSecID;
            string carrier_id = recive_str.CSTID;
            string last_adr_id = eqpt.CUR_ADR_ID;
            string last_sec_id = eqpt.CUR_SEC_ID;
            string req_block_id = recive_str.RequestBlockID;
            var reserveInfos = recive_str.ReserveInfos;
            BCRReadResult bCRReadResult = recive_str.BCRReadResult;
            scApp.VehicleBLL.updateVehicleActionStatus(eqpt, eventType);
            //replyTranEventReport(bcfApp, eventType, eqpt, seq_num);

            switch (eventType)
            {
                case EventType.ReserveReq:
                    //TranEventReportPathReserveReq(bcfApp, eqpt, seq_num, reserveInfos);
                    TranEventReportPathReserveReqNew(bcfApp, eqpt, seq_num, reserveInfos);
                    break;
                case EventType.LoadArrivals:
                case EventType.LoadComplete:
                case EventType.UnloadArrivals:
                case EventType.UnloadComplete:
                case EventType.AdrOrMoveArrivals:
                    TranEventReportArriveAndComplete(bcfApp, eqpt, seq_num, recive_str.EventType, recive_str.CurrentAdrID, recive_str.CurrentSecID, carrier_id);
                    break;
                case EventType.Vhloading:
                case EventType.Vhunloading:
                    TranEventReportLoadingUnloading(bcfApp, eqpt, seq_num, eventType);
                    break;
                case EventType.Bcrread:
                    //replyTranEventReport(bcfApp, eventType, eqpt, seq_num);
                    TransferReportBCRRead(bcfApp, eqpt, seq_num, eventType, carrier_id, bCRReadResult);
                    break;
                case EventType.RePosition:
                    TransferReportRePosition(bcfApp, eqpt, seq_num, eventType);
                    break;
                case EventType.Initial:
                    TransferReportInitial(bcfApp, eqpt, seq_num, eventType, bCRReadResult, carrier_id);
                    break;
                default:
                    replyTranEventReport(bcfApp, eventType, eqpt, seq_num);
                    break;
            }
        }
        private void TransferReportInitial(BCFApplication bcfApp, AVEHICLE vh, int seq_num, EventType eventType,
            BCRReadResult bCRReadResult, string cstID)
        {
            try
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: $"Process report {eventType}",
                   VehicleID: vh.VEHICLE_ID);

                cstID = cstID.Trim();
                scApp.ReserveBLL.RemoveAllReservedSectionsByVehicleID(vh.VEHICLE_ID);

                //帳料sync
                bool cstOnVehicle = !SCUtility.isEmpty(cstID);
                bool isBcrReadFail = (bCRReadResult == BCRReadResult.BcrReadFail);
                AVIDINFO vid_info = scApp.VIDBLL.getVIDInfo(vh.VEHICLE_ID);
                List<AMCSREPORTQUEUE> reportqueues = new List<AMCSREPORTQUEUE>();
                if (cstOnVehicle)
                {
                    if (isBcrReadFail)
                    {
                        if (!string.IsNullOrEmpty(vid_info.CARRIER_ID))
                        {
                            string new_carrier_id = $"NR-{vh.Real_ID.Trim()}-{DateTime.Now.ToString(SCAppConstants.TimestampFormat_16)}";
                            scApp.VIDBLL.upDateVIDCarrierID(vh.VEHICLE_ID, new_carrier_id);
                            scApp.ReportBLL.ReportCarrierInstalledOnly(vh.VEHICLE_ID, reportqueues);
                            scApp.ReportBLL.insertMCSReport(reportqueues);
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                Data: $"vh id:{vh.VEHICLE_ID} 非在執行命令中，身上有CST但CST ID為unknwon:{cstID}");
                        }
                        else
                        {
                            string new_carrier_id = $"NR-{vh.Real_ID.Trim()}-{DateTime.Now.ToString(SCAppConstants.TimestampFormat_16)}";
                            scApp.VIDBLL.upDateVIDCarrierID(vh.VEHICLE_ID, new_carrier_id);
                            scApp.ReportBLL.ReportCarrierInstalledOnly(vh.VEHICLE_ID, reportqueues);
                            scApp.ReportBLL.insertMCSReport(reportqueues);
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                Data: $"vh id:{vh.VEHICLE_ID} 非在執行命令中，身上有CST但CST ID為unknwon:{cstID}，故將其rename為unknown id:{new_carrier_id}");
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(vid_info.CARRIER_ID))
                        {
                            if (SCUtility.isMatche(vid_info.CARRIER_ID, cstID))
                            {
                                //do nothing...
                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                    Data: $"vh id:{vh.VEHICLE_ID} 非在執行命令中，身上有CST但CST ID為:{cstID}，與原本在身上的ID一致，故不進行調整");
                            }
                            else
                            {
                                scApp.VIDBLL.upDateVIDCarrierID(vh.VEHICLE_ID, cstID);
                                //TODO
                                scApp.ReportBLL.ReportCarrierInstalledOnly(vh.VEHICLE_ID, reportqueues, BCRReadResult.BcrMisMatch);
                                scApp.ReportBLL.insertMCSReport(reportqueues);
                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                    Data: $"vh id:{vh.VEHICLE_ID} 非在執行命令中，身上有CST但CST ID為:{cstID}，與資料庫的不一樣db cst id:{vid_info.CARRIER_ID},故將其進行對資料庫的rename");
                            }
                        }
                        else
                        {
                            scApp.VIDBLL.upDateVIDCarrierID(vh.VEHICLE_ID, cstID);
                            scApp.ReportBLL.ReportCarrierInstalledOnly(vh.VEHICLE_ID, reportqueues);
                            scApp.ReportBLL.insertMCSReport(reportqueues);

                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                Data: $"vh id:{vh.VEHICLE_ID} 非在執行命令中，身上有CST但CST ID為:{cstID}，但db中無該vh的帳料，進行建帳");
                        }
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(vid_info.CARRIER_ID))
                    {
                        scApp.VIDBLL.upDateVIDCarrierID(vh.VEHICLE_ID, string.Empty);
                        scApp.ReportBLL.ReportCarrierRemovedOnly(vh.VEHICLE_ID, reportqueues);
                        scApp.ReportBLL.insertMCSReport(reportqueues);
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                            Data: $"vh id:{vh.VEHICLE_ID} 非在執行命令中，身上無CST，但db中有該vh的帳料:{vid_info.CARRIER_ID}，進行刪除帳料");
                    }
                }
                if (reportqueues != null && reportqueues.Count > 0)
                    scApp.ReportBLL.newSendMCSMessage(reportqueues);

                replyTranEventReport(bcfApp, eventType, vh, seq_num);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
            }
        }
        protected void TransferReportRePosition(BCFApplication bcfApp, AVEHICLE eqpt, int seqNum,
                                         EventType eventType)
        {
            RepositionPath path = getRepositionInfo(eqpt);
            List<string> guide_addresses = new List<string>();
            List<string> guide_sections = new List<string>();
            string dest = string.Empty;
            if (path != null)
            {
                guide_addresses = path.guide_addresses;
                guide_sections = path.guide_sections;
                dest = path.destinationAdr;
            }
            ID_36_TRANS_EVENT_RESPONSE_EXTENSION iD_36_TRANS_EVENT_RESPONSE_EXTENSION = new com.mirle.ibg3k0.sc.ProtocolFormat.NorthInnolux.Agvmessage.ID_36_TRANS_EVENT_RESPONSE_EXTENSION()
            {
                DestinationAdr = dest
            };

            iD_36_TRANS_EVENT_RESPONSE_EXTENSION.GuideAddresses.AddRange(guide_addresses);
            iD_36_TRANS_EVENT_RESPONSE_EXTENSION.GuideSections.AddRange(guide_sections);
            replyTranEventReport(bcfApp, eventType, eqpt, seqNum, extensionMessage: iD_36_TRANS_EVENT_RESPONSE_EXTENSION);
            eqpt.VehicleReposition();   //2022.12.29
        }


        private RepositionPath getRepositionInfo(AVEHICLE eqpt)
        {
            if (eqpt != null)
            {
                string current_adr_id = eqpt.CUR_ADR_ID;

                List<RepositionPath> RepositionPathList = new List<RepositionPath>();
                RepositionPath firstRepositionPath = new RepositionPath();
                firstRepositionPath.destinationAdr = current_adr_id;
                firstRepositionPath.guide_addresses.Add(current_adr_id);
                RepositionPathList.Add(firstRepositionPath);

                RepositionPath currentBestRepositionPath = null;
                RepositionPath finalRepositionPath = null;

                bool is_find = false;

                while(!is_find)
                {
                    List<string> destAdrList = RepositionPathList.Select(p => p.destinationAdr).Distinct().ToList();
                    List<RepositionPath> newRepositionPathList = new List<RepositionPath>();
                    List<RepositionPath> HaveChanceRepositionPathList = new List<RepositionPath>();
                    foreach (string destAdr in destAdrList)
                    {
                        List<ASECTION> sections = scApp.SectionBLL.cache.GetSectionsByAddress(destAdr);
                        foreach (ASECTION sec in sections)
                        {
                            if (!sec.IsActive(scApp.SegmentBLL)) continue;

                            bool needsTurn = scApp.ReserveBLL.IsR2000Section(sec.SEC_ID);
                            double cost = sec.SEC_DIS;
                            foreach (RepositionPath repositionPath in
                                RepositionPathList.Where(p =>SCUtility.isMatche(p.destinationAdr,destAdr)).ToList())
                            {
                                if (repositionPath.guide_sections.Contains(sec.SEC_ID)) continue;
                                RepositionPath newRepositionPath = new RepositionPath();
                                newRepositionPath.destinationAdr = sec.GetOrtherEndPoint(repositionPath.destinationAdr);
                                newRepositionPath.needsTurn = repositionPath.needsTurn || needsTurn;
                                if (needsTurn)
                                {
                                    newRepositionPath.turnCount = repositionPath.turnCount + 1;
                                }
                                newRepositionPath.guide_sections = repositionPath.guide_sections.ToList();
                                newRepositionPath.guide_sections.Add(sec.SEC_ID);
                                newRepositionPath.guide_addresses = repositionPath.guide_addresses.ToList();
                                newRepositionPath.guide_addresses.Add(newRepositionPath.destinationAdr);
                                newRepositionPath.cost = repositionPath.cost + cost;

                                newRepositionPathList.Add(newRepositionPath);
                            }
                        }
                    }

                    RepositionPathList = newRepositionPathList.ToList();

                    if (RepositionPathList.Count == 0)
                    {
                        if (currentBestRepositionPath != null)
                        {
                            finalRepositionPath = currentBestRepositionPath;
                            is_find = true;
                        }
                        else
                        {
                            is_find = false;
                        }
                        break;
                    }

                    foreach (RepositionPath path in RepositionPathList)
                    {
                        if (isRepositionPathQualified(path, repositionDistance))
                        {
                            if(isBetterRepositionPath(currentBestRepositionPath, path))
                            {
                                currentBestRepositionPath = path;
                            }
                        }
                        else
                        {
                            if (isBetterRepositionPath(currentBestRepositionPath, path))
                            {
                                HaveChanceRepositionPathList.Add(path);
                            }
                        }
                    }

                    if (HaveChanceRepositionPathList.Count == 0)
                    {
                        if (currentBestRepositionPath != null)
                        {
                            finalRepositionPath = currentBestRepositionPath;
                            is_find = true;
                        }
                        else
                        {
                            is_find = false;
                        }
                        break;
                    }
                    else
                    {
                        RepositionPathList = HaveChanceRepositionPathList.ToList();
                        continue;
                    }
                }

                //根據車子所在位置，補足路段
                if (finalRepositionPath != null)
                {
                    bool start_section_is_same = true;
                    if (finalRepositionPath.guide_sections != null && finalRepositionPath.guide_sections.Count > 0)
                    {
                        if (!eqpt.IsOnAdr)
                        {
                            start_section_is_same = SCUtility.isMatche(finalRepositionPath.guide_sections[0], eqpt.CUR_SEC_ID);
                            if (!start_section_is_same)
                            {
                                finalRepositionPath.guide_sections.Insert(0, eqpt.CUR_SEC_ID);
                                ASECTION new_start_section = scApp.SectionBLL.cache.GetSection(eqpt.CUR_SEC_ID);
                                if (SCUtility.isMatche(finalRepositionPath.guide_addresses[0], new_start_section.FROM_ADR_ID))
                                {
                                    finalRepositionPath.guide_addresses.Insert(0, new_start_section.TO_ADR_ID);
                                }
                                else
                                {
                                    finalRepositionPath.guide_addresses.Insert(0, new_start_section.FROM_ADR_ID);
                                }
                                //if (!SCUtility.isMatche(guide_segment_ids[0], new_start_section.SEG_NUM))
                                //{
                                //    guide_segment_ids.Insert(0, new_start_section.SEG_NUM);
                                //}
                            }
                        }
                    }
                }

                return finalRepositionPath;
            }
            else
            {
                return null;
            }

        }

        bool isRepositionPathQualified(RepositionPath path ,int cost)
        {
            if(path.cost> cost)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        bool isBetterRepositionPath(RepositionPath currentBestPath, RepositionPath path)
        {
            if (currentBestPath == null) return true;
            if (currentBestPath.needsTurn)
            {
                if (!path.needsTurn)
                {
                    return true;
                }
            }
            else
            {
                if (path.needsTurn)
                {
                    return false;
                }
            }

            if (currentBestPath.turnCount > path.turnCount)
            {
                return true;
            }
            else if (currentBestPath.turnCount < path.turnCount)
            {
                return false;
            }

            AADDRESS destinationAddress = scApp.AddressesBLL.cache.GetAddress(path.destinationAdr);
            AADDRESS currentBestDestinationAddress = scApp.AddressesBLL.cache.GetAddress(currentBestPath.destinationAdr);
            if (destinationAddress.IsPort)
            {
                return false;
            }
            else if (currentBestDestinationAddress.IsPort)
            {
                return true;
            }

            if (currentBestPath.cost > path.cost)
            {
                return true;
            }
            return false;

        }

        class RepositionPath
        {
            public string destinationAdr;
            public bool needsTurn = false;
            public double cost = 0;
            public List<string> guide_sections = new List<string>();
            public List<string> guide_addresses = new List<string>();
            public int turnCount = 0;


        }

        protected override void TransferReportBCRRead(BCFApplication bcfApp, AVEHICLE eqpt, int seqNum,
                                           EventType eventType, string readCarrierID, BCRReadResult bCRReadResult)
        {
            scApp.VehicleBLL.updateVehicleBCRReadResult(eqpt, bCRReadResult);
            scApp.VIDBLL.upDateVIDCarrierLocInfo(eqpt.VEHICLE_ID, eqpt.Real_ID);
            switch (bCRReadResult)
            {
                case BCRReadResult.BcrMisMatch:
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Data: $"BCR miss match happend,start abort command id:{eqpt.OHTC_CMD?.Trim()}",
                       VehicleID: eqpt.VEHICLE_ID,
                       CarrierID: eqpt.CST_ID);

                    replyTranEventReport(bcfApp, eventType, eqpt, seqNum, cancelType: CMDCancelType.CmdCancelIdMismatch);
                    scApp.VIDBLL.upDateVIDCarrierID(eqpt.VEHICLE_ID, readCarrierID);

                    break;
                case BCRReadResult.BcrReadFail:
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Data: $"BCR read fail happend,start abort command id:{eqpt.OHTC_CMD?.Trim()}",
                       VehicleID: eqpt.VEHICLE_ID,
                       CarrierID: eqpt.CST_ID);

                    //AVIDINFO vid_info = scApp.VIDBLL.getVIDInfo(eqpt.VEHICLE_ID);
                    //string new_carrier_id =
                    //    $"NR-{eqpt.Real_ID.Trim()}-{vid_info.CARRIER_INSTALLED_TIME?.ToString(SCAppConstants.TimestampFormat_16)}";
                    replyTranEventReport(bcfApp, eventType, eqpt, seqNum, cancelType: CMDCancelType.CmdCancelIdReadFailed);
                    //scApp.VIDBLL.upDateVIDCarrierID(eqpt.VEHICLE_ID, new_carrier_id);

                    break;
                case BCRReadResult.BcrNormal:
                    replyTranEventReport(bcfApp, eventType, eqpt, seqNum);
                    break;
            }

            //2020/12/21 Hsinyu Chang: 北群創不需要報CEID 151，包含的資訊已經用2880取代了
            //List<AMCSREPORTQUEUE> reportqueues = new List<AMCSREPORTQUEUE>();
            //scApp.ReportBLL.newReportCarrierIDReadReport(eqpt.VEHICLE_ID, reportqueues);
            //scApp.ReportBLL.insertMCSReport(reportqueues);
            //scApp.ReportBLL.newSendMCSMessage(reportqueues);
        }


        private void TranEventReportLoadingUnloading(BCFApplication bcfApp, AVEHICLE eqpt, int seq_num, EventType eventType)
        {
            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
               Data: $"Process report {eventType}",
               VehicleID: eqpt.VEHICLE_ID,
               CarrierID: eqpt.CST_ID);

            if (!SCUtility.isEmpty(eqpt.MCS_CMD))
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: $"do report {eventType} to mcs.",
                   VehicleID: eqpt.VEHICLE_ID,
                   CarrierID: eqpt.CST_ID);
                List<AMCSREPORTQUEUE> reportqueues = new List<AMCSREPORTQUEUE>();
                using (TransactionScope tx = SCUtility.getTransactionScope())
                {
                    using (DBConnection_EF con = DBConnection_EF.GetUContext())
                    {
                        bool isSuccess = true;
                        switch (eventType)
                        {
                            case EventType.Vhloading:
                                //scApp.CMDBLL.updateCMD_MCS_TranStatus2Transferring(eqpt.MCS_CMD);
                                scApp.ReportBLL.newReportLoading(eqpt.VEHICLE_ID, reportqueues);
                                break;
                            case EventType.Vhunloading:
                                scApp.ReportBLL.newReportUnloading(eqpt.VEHICLE_ID, reportqueues);
                                break;
                        }
                        scApp.ReportBLL.insertMCSReport(reportqueues);
                        //isSuccess = scApp.ReportBLL.ReportLoadingUnloading(eqpt.VEHICLE_ID, eventType, out List<AMCSREPORTQUEUE> reportqueues);

                        if (isSuccess)
                        {
                            if (replyTranEventReport(bcfApp, eventType, eqpt, seq_num))
                            {
                                tx.Complete();
                                scApp.ReportBLL.newSendMCSMessage(reportqueues);
                            }
                        }
                    }
                }
            }
            else
            {
                replyTranEventReport(bcfApp, eventType, eqpt, seq_num);
            }
            switch (eventType)
            {
                case EventType.Vhloading:
                    scApp.VehicleBLL.doLoading(eqpt.VEHICLE_ID);
                    break;
                case EventType.Vhunloading:
                    scApp.VehicleBLL.doUnloading(eqpt.VEHICLE_ID);
                    scApp.MapBLL.getPortID(eqpt.CUR_ADR_ID, out string port_id);
                    scApp.PortBLL.OperateCatch.updatePortStationCSTExistStatus(port_id, eqpt.CST_ID);
                    break;
            }
        }

        private void TranEventReportPathReserveReqNew(BCFApplication bcfApp, AVEHICLE eqpt, int seqNum, RepeatedField<ReserveInfo> reserveInfos)
        {
            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
               Data: $"Process path reserve request,request path id:{reserveInfos.ToString()}",
               VehicleID: eqpt.VEHICLE_ID,
               CarrierID: eqpt.CST_ID);

            //2022.7.12 辨認為了反折過彎多要的路權
            List<string> crossSections = new List<string>();
            string afterCrossingSectionID = string.Empty;
            int i = 0, j = 0;
            while ((i < eqpt.WillPassSectionID.Count) && (eqpt.WillPassSectionID[i] != reserveInfos[j].ReserveSectionID)) i++;
            if (i < eqpt.WillPassSectionID.Count)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                    Data: $"WillPassSectionID: {string.Join(",", eqpt.WillPassSectionID)}, startIndex: {i}, startSection: {eqpt.WillPassSectionID[i]}",
                    VehicleID: eqpt.VEHICLE_ID,
                    CarrierID: eqpt.CST_ID);
            }
            while ((i < eqpt.WillPassSectionID.Count) && (j < reserveInfos.Count))
            {
                if (eqpt.WillPassSectionID[i].Equals(reserveInfos[j].ReserveSectionID))
                {
                    if (crossSections.Count > 0)
                    {
                        afterCrossingSectionID = reserveInfos[j].ReserveSectionID;
                        break;
                    }
                    else
                        i++;
                }
                else
                {
                    crossSections.Add(reserveInfos[j].ReserveSectionID);
                }
                j++;
            }
            if (j < reserveInfos.Count)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                    Data: $"WillPassSectionID: {string.Join(",", eqpt.WillPassSectionID)}, endIndex: {j}, endSection: {afterCrossingSectionID}",
                    VehicleID: eqpt.VEHICLE_ID,
                    CarrierID: eqpt.CST_ID);
            }
            AVEHICLE.RedundantSectionData redundantData = null;
            if (crossSections.Count > 0)
            {
                redundantData = new AVEHICLE.RedundantSectionData()
                {
                    RedundantSectionIds = crossSections,
                    WayOutSectionIdForCross = afterCrossingSectionID,
                };
            }

            lock (reserve_lock)
            {
                var ReserveResult = scApp.ReserveBLL.IsMultiReserveSuccess(scApp, eqpt.VEHICLE_ID, reserveInfos);
                //var ReserveResult = IsReserveSuccessNew(eqpt.VEHICLE_ID, reserveInfos);
                if (ReserveResult.isSuccess)
                {
                    scApp.VehicleBLL.cache.ResetCanNotReserveInfo(eqpt.VEHICLE_ID);//TODO Mark check
                    //防火門機制要檢查其影響區域有沒要被預約了。
                    //if (scApp.getCommObjCacheManager().isSectionAtFireDoorArea(ReserveResult.reservedSecID))
                    //{
                    //    //Task. scApp.getCommObjCacheManager().sectionReserveAtFireDoorArea(reserveInfo.Value);
                    //    Task.Run(() => scApp.getCommObjCacheManager().sectionReserveAtFireDoorArea(ReserveResult.reservedSecID));
                    //}
                    if (redundantData != null)
                        eqpt.RedundantSections.Add(redundantData);
                    if (eqpt.IsCrossing)
                    {
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                           Data: $"Redundant sections for crossing: {string.Join(",", crossSections)}, exit section: {afterCrossingSectionID}",
                           VehicleID: eqpt.VEHICLE_ID,
                           CarrierID: eqpt.CST_ID);
                    }
                }
                else
                {
                    string reserve_fail_section = reserveInfos[0].ReserveSectionID;
                    DriveDirction driveDirction = reserveInfos[0].DriveDirction;
                    ASECTION reserve_fail_sec_obj = scApp.SectionBLL.cache.GetSection(reserve_fail_section);
                    scApp.VehicleBLL.cache.SetUnsuccessReserveInfo(eqpt.VEHICLE_ID, new AVEHICLE.ReserveUnsuccessInfo(ReserveResult.reservedVhID, "", reserve_fail_section));
                    Task.Run(() => tryNotifyVhAvoid_New(eqpt.VEHICLE_ID, ReserveResult.reservedVhID));
                }
                //replyTranEventReport(bcfApp, EventType.ReserveReq, eqpt, seqNum, reserveSuccess: ReserveResult.isSuccess, reserveInfos: reserveInfos);
                replyTranEventReport(bcfApp, EventType.ReserveReq, eqpt, seqNum,
                     reserveSuccess: ReserveResult.isSuccess,
                     reserveInfos: ReserveResult.reserveSuccessInfos);
            }
        }

        private (bool isSuccess, string reservedVhID, string reservedSecID) askReserveSuccess(string vhID, string sectionID, string addressID)
        {
            RepeatedField<ReserveInfo> reserveInfos = new RepeatedField<ReserveInfo>();
            ASECTION current_section = scApp.SectionBLL.cache.GetSection(sectionID);
            DriveDirction driveDirction = SCUtility.isMatche(current_section.FROM_ADR_ID, addressID) ?
                DriveDirction.DriveDirForward : DriveDirction.DriveDirReverse;
            ReserveInfo info = new ReserveInfo()
            {
                //DriveDirction = DriveDirction.DriveDirForward,
                DriveDirction = driveDirction,
                ReserveSectionID = sectionID
            };
            reserveInfos.Add(info);
            return IsReserveSuccessNew(vhID, reserveInfos, isAsk: true);
        }
        public (bool isSuccess, string reservedVhID, string reservedSecID) IsReserveSuccessTest(string vhID, RepeatedField<ReserveInfo> reserveInfos)
        {
            return IsReserveSuccessNew(vhID, reserveInfos);
        }
        private (bool isSuccess, string reservedVhID, string reservedSecID) IsReserveSuccessNew(string vhID, RepeatedField<ReserveInfo> reserveInfos, bool isAsk = false)
        {
            try
            {
                if (DebugParameter.isForcedPassReserve)
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Data: "test flag: Force pass reserve is open, will driect reply to vh pass",
                       VehicleID: vhID);
                    return (true, string.Empty, string.Empty);
                }

                //強制拒絕Reserve的要求
                if (DebugParameter.isForcedRejectReserve)
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Data: "test flag: Force reject reserve is open, will driect reply to vh can't pass",
                       VehicleID: vhID);
                    return (false, string.Empty, string.Empty);
                }
                AVEHICLE vh = scApp.VehicleBLL.cache.getVehicle(vhID);
                if (vh.IsPrepareAvoid)
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Data: $"vh:{vhID} is prepare excute avoid action, will reject reserve request.",
                       VehicleID: vhID);
                    return (false, string.Empty, string.Empty);
                }
                if (reserveInfos == null || reserveInfos.Count == 0) return (false, string.Empty, string.Empty);
                string reserve_section_id = reserveInfos[0].ReserveSectionID;


                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: $"vh:{vhID} Try add reserve section:{reserve_section_id}...",
                   VehicleID: vhID);
                Mirle.Hlts.Utils.HltDirection hltDirection = decideReserveDirection(vh, reserve_section_id);

                var result = scApp.ReserveBLL.TryAddReservedSection(vhID, reserve_section_id,
                                                                    sensorDir: hltDirection,
                                                                    isAsk: isAsk);

                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: $"vh:{vhID} Try add reserve section:{reserve_section_id},result:{result.ToString()}",
                   VehicleID: vhID);
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: $"current reserve section:{scApp.ReserveBLL.GetCurrentReserveSection()}",
                   VehicleID: vhID);
                return (result.OK, result.VehicleID, reserve_section_id);
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: ex,
                   Details: $"process function:{nameof(IsReserveSuccessNew)} Exception");
                return (false, string.Empty, string.Empty);
            }
        }
        private Mirle.Hlts.Utils.HltDirection decideReserveDirection(AVEHICLE reserveVh, string reserveSectionID)
        {
            //先取得目前vh所在的current adr，如果這次要求的Reserve Sec是該Current address連接的其中一點時
            //就不用增加Secsor預約的範圍，預防發生車子預約不到本身路段的問題
            string cur_adr = reserveVh.CUR_ADR_ID;
            var related_sections_id = scApp.SectionBLL.cache.GetSectionsByAddress(cur_adr).Select(sec => sec.SEC_ID.Trim()).ToList();
            if (related_sections_id.Contains(reserveSectionID))
            {
                return Mirle.Hlts.Utils.HltDirection.None;
            }
            else
            {
                //在R2000的路段上，預約方向要帶入
                if (scApp.ReserveBLL.IsR2000Section(reserveSectionID))
                {
                    return Mirle.Hlts.Utils.HltDirection.NorthSouth;
                }
                else
                {
                    //if (SCUtility.isMatche(reserveSectionID, "0001") ||
                    //    SCUtility.isMatche(reserveSectionID, "0004") ||
                    //    SCUtility.isMatche(reserveSectionID, "0006"))
                    //    return Mirle.Hlts.Utils.HltDirection.EW;
                    //else
                    //    return Mirle.Hlts.Utils.HltDirection.NS;
                    return Mirle.Hlts.Utils.HltDirection.NESW;

                }
            }
        }


        private long syncPoint_NotifyVhAvoid = 0;

        enum CAN_NOT_AVOID_RESULT
        {
            Normal,
            VehicleInLongCharge,
            VehicleInError,
            BlockingForCouplerSafety,
            VehicleInLoadingUnloading,
            VehicleInObstacleStop,
            VehicleIsMoving, //Chris 移植交管邏輯 from AUO
            VehicleIsAutoLocal,
        }

        private (bool is_can, CAN_NOT_AVOID_RESULT result) canCreatAvoidCommand(AVEHICLE reservedVh)
        {
            if (reservedVh is null)
            {
                //reservedVh為保護充電站用的假車，繞過去
                return (false, CAN_NOT_AVOID_RESULT.BlockingForCouplerSafety);
            }
            else if (reservedVh.ACT_STATUS == VHActionStatus.NoCommand &&
                reservedVh.IsOnCharge(scApp.AddressesBLL) &&
                reservedVh.IsNeedToLongCharge())
            {
                return (false, CAN_NOT_AVOID_RESULT.VehicleInLongCharge);
            }
            else if (!reservedVh.isTcpIpConnect || reservedVh.IsError || reservedVh.MODE_STATUS == VHModeStatus.Manual)
            {
                return (false, CAN_NOT_AVOID_RESULT.VehicleInError);
            }
            else if (reservedVh.MODE_STATUS == VHModeStatus.AutoLocal)
            {
                return (false, CAN_NOT_AVOID_RESULT.VehicleIsAutoLocal);
            }
            else if (reservedVh.State == AVEHICLE.VehicleState.ACQUIRING || reservedVh.State == AVEHICLE.VehicleState.DEPOSITING)
            {
                //Loading/unloading
                return (false, CAN_NOT_AVOID_RESULT.VehicleInLoadingUnloading);
            }
            else if (reservedVh.ACT_STATUS == VHActionStatus.Commanding &&
                reservedVh.ObstacleStatus == VhStopSingle.StopSingleOff &&
                reservedVh.RESERVE_PAUSE == VhStopSingle.StopSingleOff &&
                reservedVh.BlockingStatus == VhStopSingle.StopSingleOff &&
                reservedVh.PauseStatus == VhStopSingle.StopSingleOff &&
                reservedVh.ERROR == VhStopSingle.StopSingleOff &&
                reservedVh.EARTHQUAKE_PAUSE == VhStopSingle.StopSingleOff &&
                reservedVh.SAFETY_DOOR_PAUSE == VhStopSingle.StopSingleOff)
            {
                return (false, CAN_NOT_AVOID_RESULT.VehicleIsMoving); //Chris 移植交管邏輯 from AUO : 判斷對方正在移動中先不進行override
            }
            else
            {
                bool is_can = reservedVh.isTcpIpConnect &&
                       (reservedVh.MODE_STATUS == VHModeStatus.AutoRemote || reservedVh.MODE_STATUS == VHModeStatus.AutoCharging) &&
                       reservedVh.ACT_STATUS == VHActionStatus.NoCommand &&
                       !scApp.CMDBLL.isCMD_OHTCUnfinishedByVh(reservedVh.VEHICLE_ID) /* &&
                       !scApp.CMDBLL.HasCMD_MCSInQueue() &&
                       canAvoidCheckByMCSCommand(reservedVh) */;
                return (is_can, CAN_NOT_AVOID_RESULT.Normal);
            }
        }

        private bool canAvoidCheckByMCSCommand(AVEHICLE checkVh)
        {
            if (checkVh is null)
                return true;
            if (!scApp.CMDBLL.HasCMD_MCSInQueue())
                return true;
            if (scApp.CMDBLL.HasUnloadCMD_MCSInQueue(checkVh.VEHICLE_ID))
                return true;
            return false;
        }

        private void tryNotifyVhAvoid_New(string requestVhID, string reservedVhID)
        {
            if (System.Threading.Interlocked.Exchange(ref syncPoint_NotifyVhAvoid, 1) == 0)
            {

                try
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(NorthInnoLuxVehicleService), Device: DEVICE_NAME_AGV,
                       Data: $"Try to notify vh avoid...,requestVh:{requestVhID} reservedVh:{reservedVhID}",
                       VehicleID: requestVhID);
                    AVEHICLE reserved_vh = scApp.VehicleBLL.cache.getVehicle(reservedVhID);
                    AVEHICLE request_vh = scApp.VehicleBLL.cache.getVehicle(requestVhID);

                    //先確認是否可以進行趕車的確認，如果當前Reserved的車子狀態是
                    //1.發出Error的
                    //2.正在進行長充電的
                    //則要將來要得車子進行路徑Override
                    var check_can_creat_avoid_command = canCreatAvoidCommand(reserved_vh);
                    //if (canCreatAvoidCommand(reserved_vh))
                    if (check_can_creat_avoid_command.is_can)
                    {
                        string reserved_vh_current_section = reserved_vh.CUR_SEC_ID;
                        string reserved_vh_current_address = reserved_vh.CUR_ADR_ID;

                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                           Data: $"start search section:{reserved_vh_current_section}, address:{reserved_vh_current_address}",
                           VehicleID: requestVhID);

                        var findResult = findNotConflictSectionAndAvoidAddressNew(request_vh, reserved_vh, false);
                        if (!findResult.isFind)
                        {
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                               Data: $"find not conflict section fail. reserved section:{reserved_vh_current_section}, address:{reserved_vh_current_address}",
                               VehicleID: requestVhID);
                            return;
                        }
                        else
                        {
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                               Data: $"find not conflict section:{findResult.notConflictSection.SEC_ID}.avoid address:{findResult.avoidAdr}",
                               VehicleID: requestVhID);
                        }

                        string avoid_address = findResult.avoidAdr;


                        if (!SCUtility.isEmpty(avoid_address))
                        {
                            bool is_success = scApp.CMDBLL.doCreatTransferCommand(reserved_vh.VEHICLE_ID, string.Empty, string.Empty,
                                                                E_CMD_TYPE.Move,
                                                                string.Empty,
                                                                avoid_address);
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                               Data: $"Try to notify vh avoid,requestVh:{requestVhID} reservedVh:{reservedVhID}, is success :{is_success}.",
                               VehicleID: requestVhID);
                        }
                        else
                        {
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                               Data: $"Try to notify vh avoid,requestVh:{requestVhID} reservedVh:{reservedVhID}, fail.",
                               VehicleID: requestVhID);
                        }
                    }
                    else
                    {
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                           Data: $"Try to notify vh avoid,requestVh:{requestVhID} reservedVh:{reservedVhID}," +
                                 $"but reservedVh:{reservedVhID} status not ready." +
                                 $" isTcpIpConnect:{reserved_vh?.isTcpIpConnect}" +
                                 $" MODE_STATUS:{reserved_vh?.MODE_STATUS}" +
                                 $" ACT_STATUS:{reserved_vh?.ACT_STATUS}" +
                                 $" result:{check_can_creat_avoid_command.result},"/* + 
                                 $" canAvoidCheckByMCSCommand: {canAvoidCheckByMCSCommand(reserved_vh)}"*/,
                           VehicleID: requestVhID);

                        switch (check_can_creat_avoid_command.result)
                        {
                            case CAN_NOT_AVOID_RESULT.VehicleInError:
                            case CAN_NOT_AVOID_RESULT.VehicleInLongCharge:
                            case CAN_NOT_AVOID_RESULT.BlockingForCouplerSafety:
                            case CAN_NOT_AVOID_RESULT.VehicleIsAutoLocal:
                                if (request_vh.IsReservePause)
                                {
                                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                       Data: $"Try to notify vh avoid fail,start over request of vh..., because reserved of status:{check_can_creat_avoid_command.result}," +
                                             $" requestVh:{requestVhID} reservedVh:{reservedVhID}.",
                                       VehicleID: requestVhID);

                                    ACMD_OHTC cmd_ohtc = scApp.CMDBLL.GetCMD_OHTCByID(request_vh.OHTC_CMD);
                                    if (cmd_ohtc != null && request_vh.CanNotReserveInfo != null)
                                    {
                                        bool is_override_success = scApp.VehicleService.trydoOverrideCommandToVh
                                            (request_vh, cmd_ohtc, request_vh.CanNotReserveInfo.ReservedSectionID);
                                        if (is_override_success)
                                        {
                                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                               Data: $"Try to notify vh avoid fail,but over request of vh is success," +
                                                     $" requestVh:{requestVhID} reservedVh:{reservedVhID}.",
                                               VehicleID: requestVhID);
                                            SpinWait.SpinUntil(() => false, 15000);
                                        }
                                    }
                                }
                                break;
                            case CAN_NOT_AVOID_RESULT.VehicleInLoadingUnloading:
                                //shirley: 等待對方完成LoadUnload再決定要不要避車。
                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                    Data: $"vh:{requestVhID} cannot ask for reserve section id:{request_vh.CanNotReserveInfo.ReservedSectionID}" +
                                          $"because blockingVh:{reservedVhID} status is {check_can_creat_avoid_command.result}." +
                                          $"waiting {reservedVhID} finished",
                                    VehicleID: requestVhID);
                                break;
                            case CAN_NOT_AVOID_RESULT.VehicleIsMoving:
                                //Chris 移植交管邏輯 from AUO : 對方正在移動，等待對方移動結束。
                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                    Data: $"vh:{requestVhID} of can't reserve section id:{request_vh.CanNotReserveInfo.ReservedSectionID}" +
                                          $"because reservedVh:{reservedVhID} status is {check_can_creat_avoid_command.result}." +
                                          $"waiting {reservedVhID} moving away",
                                    VehicleID: requestVhID);
                                break;
                            default:
                                if (request_vh.IsReservePause && reserved_vh.IsReservePause)
                                {
                                    //如果兩台車都已經Reserve Pause了，就不再透過這邊進行避車
                                    //而是透過Deadlock的Timer來解除。
                                }
                                else if (request_vh.IsReservePause)
                                {
                                    if (IsBlockEachOrther(reserved_vh, request_vh))
                                    {
                                        OvrerideByDriveOutFail(requestVhID, reservedVhID, check_can_creat_avoid_command.result);
                                    }
                                    else
                                    {
                                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                           Data: $"request vh:{requestVhID} with reserved vh:{reservedVhID} of can't reserve info not same,don't excute override",
                                           VehicleID: requestVhID);
                                    }
                                }
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Data: ex,
                       Details: $"excute tryNotifyVhAvoid has exception happend.requestVh:{requestVhID},reservedVh:{reservedVhID}");
                }
                finally
                {
                    System.Threading.Interlocked.Exchange(ref syncPoint_NotifyVhAvoid, 0);
                }
            }
        }
        private void OvrerideByDriveOutFail(string requestVhID, string reservedVhID, CAN_NOT_AVOID_RESULT canNotDriveOutResult)
        {
            AVEHICLE request_vh = scApp.VehicleBLL.cache.getVehicle(requestVhID);
            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
               Data: $"Try to notify vh avoid fail,start over request of vh..., because reserved of status:{canNotDriveOutResult}," +
                     $" requestVh:{requestVhID} reservedVh:{reservedVhID}.",
               VehicleID: requestVhID);

            ACMD_OHTC cmd_ohtc = scApp.CMDBLL.GetCMD_OHTCByID(request_vh.OHTC_CMD);
            if (cmd_ohtc != null && request_vh.CanNotReserveInfo != null)
            {
                bool is_override_success = scApp.VehicleService.trydoOverrideCommandToVh
                    (request_vh, cmd_ohtc, request_vh.CanNotReserveInfo.ReservedSectionID);
                if (is_override_success)
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Data: $"Try to notify vh avoid fail,but over request of vh is success," +
                             $" requestVh:{requestVhID} reservedVh:{reservedVhID}.",
                       VehicleID: requestVhID);
                    SpinWait.SpinUntil(() => false, 15000);
                }
            }
        }

        private (bool isFind, ASECTION notConflictSection, string entryAdr, string avoidAdr) findNotConflictSectionAndAvoidAddress(AVEHICLE requestVh, AVEHICLE reservedVh)
        {
            int calculation_count = 0;
            int max_calculation_count = 10;

            string reserved_vh_current_section_id = reservedVh.CUR_SEC_ID;
            ASECTION reserved_vh_current_section = scApp.SectionBLL.cache.GetSection(reserved_vh_current_section_id);

            ASECTION not_conflict_section = null;
            string avoid_address = null;
            string orther_end_point = "";

            List<(string next_address, ASECTION source_section)> next_search_infos =
                new List<(string next_address, ASECTION source_section)>() { (reserved_vh_current_section.FROM_ADR_ID, reserved_vh_current_section) };

            List<(string, ASECTION)> next_search_address_temp = new List<(string, ASECTION)>();

            do
            {
                foreach (var search_info in next_search_infos.ToArray())
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Data: $"start search address:{search_info.next_address}",
                       VehicleID: requestVh.VEHICLE_ID);
                    //next_search_address.Clear();
                    List<ASECTION> next_sections = scApp.SectionBLL.cache.GetSectionsByAddress(search_info.next_address);

                    //先把自己的Section移除
                    next_sections.Remove(search_info.source_section);
                    if (next_sections != null && next_sections.Count() > 0)
                    {
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                           Data: $"next search section:{string.Join(",", next_sections.Select(sec => sec.SEC_ID).ToArray())}",
                           VehicleID: requestVh.VEHICLE_ID);
                        //過濾掉已經Disable的Segment
                        next_sections = next_sections.Where(sec => sec.IsActive(scApp.SegmentBLL)).ToList();
                        if (next_sections != null && next_sections.Count() > 0)
                        {
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                               Data: $"next search section:{string.Join(",", next_sections.Select(sec => sec.SEC_ID).ToArray())} after filter not in active",
                               VehicleID: requestVh.VEHICLE_ID);
                        }
                        else
                        {
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                               Data: $"search result is empty after filter not in active ,search adr:{search_info.next_address}",
                               VehicleID: requestVh.VEHICLE_ID);
                        }
                    }
                    else
                    {
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                           Data: $"search result is empty,search adr:{search_info.next_address}",
                           VehicleID: requestVh.VEHICLE_ID);
                    }

                    foreach (ASECTION sec in next_sections)
                    {
                        //if (sec == search_info.source_section) continue;
                        orther_end_point = sec.GetOrtherEndPoint(search_info.next_address);
                        //要先嘗試去預約看看，確保該路徑是可以走的，則就不再以這條繼續往下找
                        var reserve_check_result = scApp.ReserveBLL.TryAddReservedSection(reservedVh.VEHICLE_ID, sec.SEC_ID, isAsk: true);
                        if (!reserve_check_result.OK)
                        {
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                               Data: $"sec id:{SCUtility.Trim(sec.SEC_ID)} try to reserve fail. find the orther end point address.",
                               VehicleID: requestVh.VEHICLE_ID);
                            continue;
                        }

                        if (requestVh.PredictSections.Contains(SCUtility.Trim(sec.SEC_ID)))
                        {
                            //next_search_address.Add(next_calculation_address);
                            next_search_address_temp.Add((orther_end_point, sec));
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                               Data: $"sec id:{SCUtility.Trim(sec.SEC_ID)} is request_vh of predict sections.by pass 1,address:{orther_end_point} to next",
                               VehicleID: requestVh.VEHICLE_ID);
                            continue;
                        }
                        //取得沒有相交的Section後，在確認是否該Orther end point是一個Port點，如果是的話就可以拿來作為一個避車點
                        AADDRESS orther_end_address = scApp.AddressesBLL.cache.GetAddress(orther_end_point);
                        if (!orther_end_address.canAvoidVhecle)
                        {
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                               Data: $"sec id:{SCUtility.Trim(sec.SEC_ID)} of orther end point:{orther_end_point} is not can avoid address, continue find next address",
                               VehicleID: requestVh.VEHICLE_ID);
                            next_search_address_temp.Add((orther_end_point, sec));
                            continue;
                        }
                        not_conflict_section = sec;
                        avoid_address = orther_end_point;
                        return (true, not_conflict_section, search_info.next_address, avoid_address);
                    }
                }
                next_search_infos = next_search_address_temp.ToList();
                next_search_address_temp.Clear();
                calculation_count++;
            } while (next_search_infos.Count() != 0 && calculation_count < max_calculation_count);



            return (false, null, null, null);
        }

        private (bool isFind, ASECTION notConflictSection, string entryAdr, string avoidAdr) findNotConflictSectionAndAvoidAddressNew(AVEHICLE requestVh, AVEHICLE findAvoidAdrOfVh, bool isDeadLock)
        {
            string req_vh_cur_adr = requestVh.CUR_ADR_ID;
            string find_avoid_vh_cur_adr = findAvoidAdrOfVh.CUR_ADR_ID;
            ASECTION req_vh_current_section = scApp.SectionBLL.cache.GetSection(requestVh.CUR_SEC_ID);
            ASECTION find_avoid_vh_current_section = scApp.SectionBLL.cache.GetSection(findAvoidAdrOfVh.CUR_SEC_ID);
            //先找出哪個Address是距離即將到來的車子比較遠，即反方向
            string first_search_adr;
            if (findAvoidAdrOfVh.IsOnAdr)
            {
                first_search_adr = find_avoid_vh_cur_adr;
                find_avoid_vh_current_section = scApp.SectionBLL.cache.GetSectionsByAddress(find_avoid_vh_cur_adr).FirstOrDefault();
            }
            else
                first_search_adr = findTheOppositeOfAddress(req_vh_cur_adr, find_avoid_vh_current_section);

            (string next_address, ASECTION source_section) first_search_section_infos = (first_search_adr, find_avoid_vh_current_section);
            var searchResult = tryFindAvoidAddressByOneWay(requestVh, findAvoidAdrOfVh, first_search_section_infos, false);
            if (!isDeadLock && !searchResult.isFind)
            {
                string second_search_adr = SCUtility.isMatche(first_search_adr, find_avoid_vh_current_section.FROM_ADR_ID) ?
                    find_avoid_vh_current_section.TO_ADR_ID : find_avoid_vh_current_section.FROM_ADR_ID;

                (string next_address, ASECTION source_section) second_search_section_infos = (second_search_adr, find_avoid_vh_current_section);
                searchResult = tryFindAvoidAddressByOneWay(requestVh, findAvoidAdrOfVh, second_search_section_infos, true);
            }
            return searchResult;
        }

        private string findTheOppositeOfAddress(string req_vh_cur_adr, ASECTION reserved_vh_current_section)
        {
            string opposite_address = "";
            int from_distance = 0;
            var from_adr_guide_result = scApp.GuideBLL.getGuideInfo(req_vh_cur_adr, reserved_vh_current_section.FROM_ADR_ID);
            if (from_adr_guide_result.isSuccess)
            {
                from_distance = from_adr_guide_result.totalCost;
            }
            int to_distance = 0;
            var to_adr_guide_result = scApp.GuideBLL.getGuideInfo(req_vh_cur_adr, reserved_vh_current_section.TO_ADR_ID);
            if (to_adr_guide_result.isSuccess)
            {
                to_distance = to_adr_guide_result.totalCost;
            }
            if (from_distance > to_distance)
            {
                opposite_address = reserved_vh_current_section.FROM_ADR_ID;
            }
            else
            {
                opposite_address = reserved_vh_current_section.TO_ADR_ID;
            }
            return opposite_address;
        }

        public const string VehicleVirtualSymbol = "virtual";
        private (bool isFind, ASECTION notConflictSection, string entryAdr, string avoidAdr) tryFindAvoidAddressByOneWay(AVEHICLE requestVh, AVEHICLE reservedVh, (string next_address, ASECTION source_section) startSearchInfo, bool isForceCrossing)
        {
            int calculation_count = 0;
            int max_calculation_count = 10;

            List<(string next_address, ASECTION source_section)> next_search_infos =
                new List<(string next_address, ASECTION source_section)>() { startSearchInfo };
            List<(string next_address, ASECTION source_section)> next_search_address_temp =
                new List<(string, ASECTION)>();

            ASECTION not_conflict_section = null;
            string avoid_address = null;
            string orther_end_point = "";
            //string virtual_vh_id = "";
            List<string> virtual_vh_ids = new List<string>();

            try
            {
                //在一開始的時候就先Set一台虛擬車在相同位置，防止找到鄰近的Address
                var hlt_vh_obj = scApp.ReserveBLL.GetHltVehicle(reservedVh.VEHICLE_ID);
                string virtual_vh_id = $"{VehicleVirtualSymbol}_{reservedVh.VEHICLE_ID}";
                HltVehicle virtualVh = new HltVehicle(hlt_vh_obj);
                virtualVh.ID = virtual_vh_id;
                virtualVh.SensorDirection = HltDirection.ForwardReverse;
                scApp.ReserveBLL.TryAddVehicleOrUpdate(virtualVh);
                virtual_vh_ids.Add(virtual_vh_id);
                do
                {
                    foreach (var search_info in next_search_infos.ToArray())
                    {
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                           Data: $"start search address:{search_info.next_address}",
                           VehicleID: requestVh.VEHICLE_ID);
                        //next_search_address.Clear();
                        List<ASECTION> next_sections = scApp.SectionBLL.cache.GetSectionsByAddress(search_info.next_address);

                        //先把自己的Section移除
                        next_sections.Remove(search_info.source_section);
                        if (next_sections != null && next_sections.Count() > 0)
                        {
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                               Data: $"next search section:{string.Join(",", next_sections.Select(sec => sec.SEC_ID).ToArray())}",
                               VehicleID: requestVh.VEHICLE_ID);
                            //過濾掉已經Disable的Segment
                            next_sections = next_sections.Where(sec => sec.IsActive(scApp.SegmentBLL)).ToList();
                            if (next_sections != null && next_sections.Count() > 0)
                            {
                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                   Data: $"next search section:{string.Join(",", next_sections.Select(sec => sec.SEC_ID).ToArray())} after filter not in active",
                                   VehicleID: requestVh.VEHICLE_ID);
                            }
                            else
                            {
                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                   Data: $"search result is empty after filter not in active ,search adr:{search_info.next_address}",
                                   VehicleID: requestVh.VEHICLE_ID);
                            }
                        }
                        else
                        {
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                               Data: $"search result is empty,search adr:{search_info.next_address}",
                               VehicleID: requestVh.VEHICLE_ID);
                            #region 2022.9.28 marked
                            //如果找到最後已經沒路的，就用這個端點去嘗試Add 一台車看看能不能成功，
                            //如果可以成功則代表這個點其實是可以閃避的
                            //string virtual_vh_section_id = $"{virtual_vh_id}_{search_info.next_address}";
                            //virtual_vh_ids.Add(virtual_vh_section_id);
                            //var check_result = scApp.ReserveBLL.TryAddVehicleOrUpdate(virtual_vh_section_id, search_info.next_address, 0);
                            //if (check_result.OK)
                            //    check_result = scApp.ReserveBLL.TryAddVehicleOrUpdate(virtual_vh_section_id, search_info.next_address, 90);
                            //if (check_result.OK)
                            //{
                            //    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                            //       Data: $"search result is empty,but finial adr:{search_info.next_address} it ok for avoid",
                            //       VehicleID: requestVh.VEHICLE_ID);

                            //    not_conflict_section = search_info.source_section;
                            //    avoid_address = search_info.next_address;
                            //    return (true, not_conflict_section, search_info.next_address, avoid_address);
                            //}
                            #endregion 2022.9.28 marked
                        }

                        //當找出兩段以上的Section時且他的Source為會與另一台vh前進路徑交錯的車，
                        //代表找到了叉路，因此要在入口加入一台虛擬車來幫助找避車路徑時確保不會卡住的點。
                        if (next_sections.Count >= 2 &&
                            hasCrossWithPredictSection(search_info.source_section.SEC_ID, requestVh.WillPassSectionID.ToArray()))
                        {
                            string virtual_vh_section_id = $"{virtual_vh_id}_{search_info.next_address}";
                            scApp.ReserveBLL.TryAddVehicleOrUpdate(virtual_vh_section_id, search_info.next_address);
                            virtual_vh_ids.Add(virtual_vh_section_id);
                            //scApp.ReserveBLL.ForceUpdateVehicle(virtual_vh_id, search_info.next_address);
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                               Data: $"Add virtual in reserve system vh:{virtual_vh_section_id} in address id:{search_info.next_address}",
                               VehicleID: reservedVh.VEHICLE_ID);
                        }
                        foreach (ASECTION sec in next_sections)
                        {
                            //if (sec == search_info.source_section) continue;
                            orther_end_point = sec.GetOrtherEndPoint(search_info.next_address);
                            //如果跟目前找停車位的車子同一個點位時，代表找回到了原點，因此要把它濾掉。
                            if (SCUtility.isMatche(reservedVh.CUR_ADR_ID, orther_end_point))
                            {
                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                   Data: $"sec id:{SCUtility.Trim(sec.SEC_ID)} of orther end point:{orther_end_point} same with vh current address pass this section",
                                   VehicleID: requestVh.VEHICLE_ID);
                                continue;
                            }
                            if (!isForceCrossing)
                            {
                                if (requestVh.PredictSections != null && requestVh.PredictSections.Count() > 0)
                                {
                                    if (requestVh.PredictSections.Contains(SCUtility.Trim(sec.SEC_ID)))
                                    {
                                        //next_search_address.Add(next_calculation_address);
                                        next_search_address_temp.Add((orther_end_point, sec));
                                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                           Data: $"sec id:{SCUtility.Trim(sec.SEC_ID)} is request_vh of predict sections.by pass it,continue find next address{orther_end_point}",
                                           VehicleID: requestVh.VEHICLE_ID);
                                        continue;
                                    }
                                }
                            }
                            //取得沒有相交的Section後，在確認是否該Orther end point是一個可以避車且不是R2000的任一端點，如果是的話就可以拿來作為一個避車點
                            AADDRESS orther_end_address = scApp.AddressesBLL.cache.GetAddress(orther_end_point);
                            //if (!orther_end_address.canAvoidVhecle)
                            if (!orther_end_address.canAvoidVhecle ||
                                scApp.ReserveBLL.IsR2000Address(orther_end_point))
                            {
                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                   Data: $"sec id:{SCUtility.Trim(sec.SEC_ID)} of orther end point:{orther_end_point} is not can avoid address, continue find next address{orther_end_point}",
                                   VehicleID: requestVh.VEHICLE_ID);
                                next_search_address_temp.Add((orther_end_point, sec));
                                continue;
                            }
                            //找到以後嘗試去預約看看，確保該路徑是可以走的，如果不行則就繼續往下找
                            var reserve_check_result = scApp.ReserveBLL.TryAddReservedSection(reservedVh.VEHICLE_ID, sec.SEC_ID, isAsk: true);
                            if (!reserve_check_result.OK)
                            {
                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                   Data: $"sec id:{SCUtility.Trim(sec.SEC_ID)} try to reserve fail,result:{reserve_check_result.Description}. continue find next address{orther_end_point}.",
                                   VehicleID: requestVh.VEHICLE_ID);
                                next_search_address_temp.Add((orther_end_point, sec));
                                continue;
                            }
                            else
                            {
                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                   Data: $"sec id:{SCUtility.Trim(sec.SEC_ID)} try to reserve success,result:{reserve_check_result.Description}.",
                                   VehicleID: requestVh.VEHICLE_ID);
                            }
                            not_conflict_section = sec;
                            avoid_address = orther_end_point;
                            return (true, not_conflict_section, search_info.next_address, avoid_address);
                        }
                    }
                    next_search_infos = next_search_address_temp.ToList();
                    next_search_address_temp.Clear();
                    calculation_count++;
                } while (next_search_infos.Count() != 0 && calculation_count < max_calculation_count);
            }
            finally
            {
                if (virtual_vh_ids != null && virtual_vh_ids.Count > 0)
                {
                    foreach (string virtual_vh_id in virtual_vh_ids)
                    {
                        scApp.ReserveBLL.RemoveVehicle(virtual_vh_id);
                        //scApp.ReserveBLL.ForceUpdateVehicle(virtual_vh_id, 0, 0, 0);
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                           Data: $"remove virtual in reserve system vh:{virtual_vh_id} ",
                           VehicleID: reservedVh.VEHICLE_ID);
                    }
                }
            }
            return (false, null, null, null);
        }

        private bool hasCrossWithPredictSection(string checkSection, string[] prediceSection)
        {
            if (prediceSection == null || prediceSection.Count() == 0) return false;
            if (SCUtility.isEmpty(checkSection)) return false;
            return prediceSection.Contains(SCUtility.Trim(checkSection));
            throw new NotImplementedException();
        }

        #region New 51 Avoid address chooser
        //2022.9.26 new
        private SpinLock avoidSpinLock = new SpinLock();
        public override bool tryDo51AvoidCommandToVhNew(AVEHICLE avoidVh, AVEHICLE passVh)
        {
            bool avoidLockTaken = false;
            try
            {
                avoidSpinLock.Enter(ref avoidLockTaken);

                var findAvoidResult = findNotConflictSectionAnd51AvoidAddress(passVh, avoidVh);
                string blocked_section = avoidVh.CanNotReserveInfo.ReservedSectionID;
                string blocked_vh_id = avoidVh.CanNotReserveInfo.ReservedVhID;
                if (findAvoidResult.isFind)
                {
                    avoidVh.VhAvoidInfo = null;
                    //找到避車點之後，就可以下ID:51，開始讓vh進行避車了
                    var avoid_request_result = AvoidRequest(avoidVh.VEHICLE_ID, findAvoidResult.avoidAdr,
                                                            needbyPassSectionID: findAvoidResult.needToBlockedSections);
                    if (avoid_request_result.is_success)
                    {
                        avoidVh.VhAvoidInfo = new AVEHICLE.AvoidInfo(blocked_section, blocked_vh_id, avoid_request_result.guide_address_ids);
                    }
                    return avoid_request_result.is_success;
                }
                else
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Data: $"No find the can avoid address. avoid vh:{avoidVh.VEHICLE_ID} current adr:{avoidVh.CUR_ADR_ID}," +
                             $"will pass vh:{passVh.VEHICLE_ID} current adr:{passVh.CUR_ADR_ID}",
                       VehicleID: avoidVh.VEHICLE_ID,
                       CarrierID: avoidVh.CST_ID);
                    return false;
                }
            }
            finally
            {
                if (avoidLockTaken)
                    avoidSpinLock.Exit();
            }
        }

        private (bool isFind, string avoidAdr, List<string> needToBlockedSections) findNotConflictSectionAnd51AvoidAddress(AVEHICLE requestVh, AVEHICLE findAvoidAdrOfVh)
        {
            string req_vh_cur_adr = requestVh.CUR_ADR_ID;
            string find_avoid_vh_cur_adr = findAvoidAdrOfVh.CUR_ADR_ID;
            ASECTION find_avoid_vh_current_section = scApp.SectionBLL.cache.GetSection(findAvoidAdrOfVh.CUR_SEC_ID);
            //先找出哪個Address是距離即將到來的車子比較遠，即反方向
            string first_search_adr;
            if (findAvoidAdrOfVh.IsOnAdr)
                first_search_adr = find_avoid_vh_cur_adr;
            else
                first_search_adr = findTheOppositeOfAddress(req_vh_cur_adr, find_avoid_vh_current_section);

            var searchResult = tryFind51AvoidAddress(requestVh, findAvoidAdrOfVh, first_search_adr);
            return searchResult;
        }

        private (bool isFind, string avoidAdr, List<string> needToBlockedSectionIds) tryFind51AvoidAddress(AVEHICLE requestVh, AVEHICLE reservedVh, string startAddress)
        {
            string avoid_address = null;
            List<string> virtual_vh_ids = new List<string>();
            List<string> needToBlockedSectionIds = new List<string>();
            //if (requestVh.PredictSections != null)
            //    needToBlockedSectionIds.AddRange(requestVh.PredictSections);

            try
            {
                #region 2022.9.28 marked
                //在一開始的時候就先Set一台虛擬車在相同位置，防止找到鄰近的Address
                //var hlt_vh_obj = scApp.ReserveBLL.GetHltVehicle(reservedVh.VEHICLE_ID);
                //string virtual_vh_id = $"{VehicleVirtualSymbol}_{reservedVh.VEHICLE_ID}";
                //HltVehicle virtualVh = new HltVehicle(hlt_vh_obj);
                //virtualVh.ID = virtual_vh_id;
                //virtualVh.SensorDirection = HltDirection.ForwardReverse;
                //scApp.ReserveBLL.TryAddVehicleOrUpdate(virtualVh);
                //virtual_vh_ids.Add(virtual_vh_id);
                #endregion 2022.9.28 marked

                Queue<(string addressId, string sectionId)> nextAddresses = new Queue<(string, string)>();
                var firstTestingSections = scApp.SectionBLL.cache.GetSectionsByAddress(startAddress)
                    .Where(sec => sec.IsActive(scApp.SegmentBLL));
                foreach (var sec in firstTestingSections)
                {
                    var otherAddress = sec.GetOrtherEndPoint(startAddress);
                    nextAddresses.Enqueue((otherAddress, sec.SEC_ID));
                }
                HashSet<string> visitedAddress = new HashSet<string>();
                visitedAddress.Add(startAddress);
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                    Data: $"requestVh will pass sections:{string.Join(",", requestVh.WillPassSectionID)}",
                    VehicleID: requestVh.VEHICLE_ID);
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                    Data: $"start search address:{startAddress}",
                    VehicleID: requestVh.VEHICLE_ID);

                while (nextAddresses.Count > 0)
                {
                    var testAddrSec = nextAddresses.Dequeue();
                    if (visitedAddress.Contains(testAddrSec.addressId))
                        continue;
                    visitedAddress.Add(testAddrSec.addressId);
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                        Data: $"next testing address:{testAddrSec}",
                        VehicleID: requestVh.VEHICLE_ID);

                    //如果找到和另一台車預定路線交錯的address，要在交錯的address加入一台虛擬車來幫助找避車路徑時確保不會卡住的點。(?)
                    //if (hasCrossWithPredictAddress(testAddrSec.addressId, requestVh.WillPassAddressID))
                    //{
                    //    string virtual_vh_section_id = $"{virtual_vh_id}_{testAddrSec.addressId}";
                    //    scApp.ReserveBLL.TryAddVehicleOrUpdate(virtual_vh_section_id, testAddrSec.addressId);
                    //    virtual_vh_ids.Add(virtual_vh_section_id);
                    //    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                    //       Data: $"Add virtual in reserve system vh:{virtual_vh_section_id} in address id:{testAddrSec}",
                    //       VehicleID: reservedVh.VEHICLE_ID);
                    //}

                    //fail case 1: 路徑重疊 => 往支線(下一個點)找
                    if (requestVh.WillPassSectionID != null && requestVh.WillPassSectionID.Count() > 0
                        && requestVh.WillPassSectionID.Contains(SCUtility.Trim(testAddrSec.sectionId)))
                    {
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                            Data: $"sec id:{SCUtility.Trim(testAddrSec.sectionId)} is request_vh of predict sections.by pass it,continue find next address.",
                            VehicleID: requestVh.VEHICLE_ID);
                        //needToBlockedSectionIds.Add(testAddrSec.sectionId);
                    }
                    else
                    {
                        //fail case 2: 路權預約不過 => 此路不通
                        var reserve_check_result = scApp.ReserveBLL.TryAddReservedSection(reservedVh.VEHICLE_ID, testAddrSec.sectionId, isAsk: true);
                        if (!reserve_check_result.OK)
                        {
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                Data: $"sec id:{SCUtility.Trim(testAddrSec.sectionId)} try to reserve fail, result:{reserve_check_result.Description}. continue find next address.",
                                VehicleID: requestVh.VEHICLE_ID);
                            needToBlockedSectionIds.Add(testAddrSec.sectionId);
                        }
                        //fail case 3: 踩到不能避車的點 => 往下一個點找
                        else if (!canDo51Avoid(testAddrSec.addressId))
                        {
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                Data: $"address id: {testAddrSec.addressId} is not can avoid address, continue to find next address.",
                                VehicleID: requestVh.VEHICLE_ID);
                        }
                        //前置檢查成功，檢查是否可以讓另一台車通過:
                        else
                        {
                            if (checkCanOtherVehiclePass(requestVh, reservedVh, testAddrSec.addressId, testAddrSec.sectionId))
                            {
                                avoid_address = testAddrSec.addressId;
                                break;
                            }
                        }
                    }
                    var nextSections = scApp.SectionBLL.cache.GetSectionsByAddress(testAddrSec.addressId)
                        .Where(sec => sec.IsActive(scApp.SegmentBLL))
                        .Where(sec => !needToBlockedSectionIds.Contains(sec.SEC_ID));
                    foreach (var sec in nextSections)
                    {
                        var nextAddress = sec.GetOrtherEndPoint(testAddrSec.addressId);
                        if (!visitedAddress.Contains(nextAddress))
                            nextAddresses.Enqueue((nextAddress, sec.SEC_ID));
                    }
                }

                if (virtual_vh_ids != null && virtual_vh_ids.Count > 0)
                {
                    foreach (string vhId in virtual_vh_ids)
                    {
                        scApp.ReserveBLL.RemoveVehicle(vhId);
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                           Data: $"remove virtual in reserve system vh:{vhId} ",
                           VehicleID: reservedVh.VEHICLE_ID);
                    }
                }
                if (!string.IsNullOrEmpty(avoid_address))
                {
                    return (true, avoid_address, needToBlockedSectionIds);
                }
                else
                    return (false, null, null);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception:");
                return (false, null, null);
            }
        }

        private bool canDo51Avoid(string address)
        {
            AADDRESS orther_end_address = scApp.AddressesBLL.cache.GetAddress(address);
            return orther_end_address.canAvoidVhecle && !scApp.ReserveBLL.IsR2000Address(address);
        }

        private bool hasCrossWithPredictAddress(string checkAddress, List<string> predictAddresses)
        {
            if (predictAddresses == null || predictAddresses.Count() == 0) return false;
            if (SCUtility.isEmpty(checkAddress)) return false;
            return predictAddresses.Contains(SCUtility.Trim(checkAddress));
        }

        //2022.9.26 需要再完善條件?
        private bool checkCanOtherVehiclePass(AVEHICLE requestVh, AVEHICLE needToAvoidVh, string addressId, string sectionId)
        {
            string needToAvoidVirtualVehicle = $"{VehicleVirtualSymbol}_Avoid";
            var hltVhOriginal = scApp.ReserveBLL.GetHltVehicle(needToAvoidVh.VEHICLE_ID);
            var resultStep1 = scApp.ReserveBLL.TryAddVehicleOrUpdate(needToAvoidVirtualVehicle, addressId, hltVhOriginal.Angle);
            if (!resultStep1.OK)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                    Data: $"can not reserve address id: {addressId}, continue to find next address.",
                    VehicleID: requestVh.VEHICLE_ID);
                return false;
            }
            else
            {
                bool resultStep2 = true;
                foreach (var checkSec in requestVh.WillPassSectionID)
                {
                    HltResult reserve_check_result = scApp.ReserveBLL.TryAddReservedSection(requestVh.VEHICLE_ID, checkSec, isAsk: true);
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                        Data: $"check reserve result after another vehicle move to {addressId}, section {checkSec} reserve result:{reserve_check_result.OK},blocked vh:{reserve_check_result.VehicleID}.",
                        VehicleID: requestVh.VEHICLE_ID);
                    if (!reserve_check_result.OK && reserve_check_result.VehicleID.Equals(needToAvoidVirtualVehicle))
                    {
                        resultStep2 = false;
                        break;
                    }
                }
                scApp.ReserveBLL.RemoveVehicle(needToAvoidVirtualVehicle);
                return resultStep2;
            }
        }
        #endregion New 51 Avoid address chooser

        #region 緊急避車(強制拉到最近的角落)
        public class ForceToAvoidCheckResult
        {
            public AVEHICLE Vehicle { get; set; }
            public string AvoidAddress { get; set; }
            public List<string> AvoidSecPath { get; set; }
            public List<string> AvoidAddrPath { get; set; }
        }
        public override List<ForceToAvoidCheckResult> ForceToAvoidToCorner(List<AVEHICLE> avoidVehicles, List<string> avoidAddressIDs)
        {
            try
            {
                var result = new List<ForceToAvoidCheckResult>();
                foreach (var checkVh in avoidVehicles)
                {
                    var res = new ForceToAvoidCheckResult() { Vehicle = checkVh };
                    var candidateAdrIds = new List<string>();
                    foreach (var adrId in avoidAddressIDs)
                    {
                        AADDRESS adrObj = scApp.AddressesBLL.cache.GetAddress(adrId);
                        if (adrObj is null) continue;
                        var loc = scApp.ReserveBLL.GetHltMapAddress(adrId);
                        if (checkSameX(loc.x, loc.y, checkVh.X_Axis, checkVh.Y_Axis))
                            candidateAdrIds.Add(adrId);
                    }
                    foreach (var adrId in candidateAdrIds)
                    {
                        var path = getSameXPath(checkVh.CUR_ADR_ID, adrId, checkVh.VEHICLE_ID);
                        if (path.pathSec.Count > 0)
                        {
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                Data: $"Prepare to drive vh:{checkVh.VEHICLE_ID} to {adrId}...",
                                VehicleID: checkVh.VEHICLE_ID,
                                CarrierID: checkVh.CST_ID);
                            res.AvoidSecPath = path.pathSec;
                            res.AvoidAddrPath = path.pathAddr;
                            res.AvoidAddress = adrId;
                            break;
                        }
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception:");
                return new List<ForceToAvoidCheckResult>();
            }
        }
        private bool checkSameX(double checkX, double checkY, double baseX, double baseY)
        {
            double tolerance = 0.175;   //0.175弧度約等於10 degrees
            try
            {
                double[] vectorBaseCheck = new double[2] { checkX - baseX, checkY - baseY };
                double[] vectorBase = new double[2] { 0, checkY - baseY };
                var lengthBaseCheck = Math.Sqrt(Math.Pow(checkX - baseX, 2) + Math.Pow(checkY - baseY, 2));
                var lengthBase = checkY - baseY;
                //var angle = Math.Acos(((vectorBaseCheck[0] * vectorBase[0]) + (vectorBaseCheck[1] * vectorBase[1])) / (lengthBaseCheck * lengthBase));
                var angle = Math.Acos(Math.Pow(checkY - baseY, 2) / (lengthBaseCheck * lengthBase));
                return angle < tolerance;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception:");
                return false;
            }
        }
        private (List<string> pathSec, List<string> pathAddr) getSameXPath(string startAddrID, string destAddrID, string vehicleID)
        {
            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                Data: $"Calculate path from {startAddrID} to {destAddrID}...",
                VehicleID: vehicleID,
                CarrierID: "");

            var resultSec = new List<string>();
            var resultAddr = new List<string>();
            var thisAddr = startAddrID;
            var startLoc = scApp.ReserveBLL.GetHltMapAddress(startAddrID);
            var destLoc = scApp.ReserveBLL.GetHltMapAddress(destAddrID);
            while (!thisAddr.Equals(destAddrID))
            {
                var nextPossibleSections = scApp.SectionBLL.cache.GetSectionsByAddress(thisAddr);
                var thisLoc = scApp.ReserveBLL.GetHltMapAddress(thisAddr);
                foreach (var sec in nextPossibleSections)
                {
                    var nextPossibleAddr = sec.GetOrtherEndPoint(thisAddr);
                    var nextPossibleLoc = scApp.ReserveBLL.GetHltMapAddress(nextPossibleAddr);
                    if (!checkSameX(startLoc.x, startLoc.y, nextPossibleLoc.x, nextPossibleLoc.y))
                        continue;
                    if (Math.Abs(nextPossibleLoc.y - destLoc.y) >= Math.Abs(thisLoc.y - destLoc.y))
                        continue;
                    if (Math.Abs(nextPossibleLoc.y - destLoc.y) < Math.Abs(thisLoc.y - destLoc.y))
                    {
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                            Data: $"Target:{destAddrID}, test next section {sec.SEC_ID}...",
                            VehicleID: vehicleID,
                            CarrierID: "");
                        var reserveResult = scApp.ReserveBLL.TryAddReservedSection(vehicleID, sec.SEC_ID, isAsk: true);
                        if (reserveResult.OK)
                        {
                            resultAddr.Add(thisAddr);
                            resultSec.Add(sec.SEC_ID);
                            thisAddr = nextPossibleAddr;
                            break;
                        }
                    }
                    return (new List<string>(), new List<string>());
                }
            }
            return (resultSec, resultAddr);
        }
        #endregion

        private void TranEventReportArriveAndComplete(BCFApplication bcfApp, AVEHICLE vh, int seqNum
                                                    , EventType eventType, string current_adr_id, string current_sec_id, string carrier_id)
        {
            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
               Data: $"Process report {eventType}",
               VehicleID: vh.VEHICLE_ID,
               CarrierID: vh.CST_ID);
            string port_id = "";
            switch (eventType)
            {
                case EventType.LoadArrivals:
                case EventType.UnloadArrivals:
                    //scApp.VIDBLL.upDateVIDPortID(eqpt.VEHICLE_ID, eqpt.CUR_ADR_ID);
                    scApp.CMDBLL.setWillPassSectionInfo(vh.VEHICLE_ID, vh.PredictSectionsToDesination);
                    scApp.VIDBLL.upDateVIDPortID(vh.VEHICLE_ID, eventType);
                    break;
                case EventType.LoadComplete:
                    if (!SCUtility.isEmpty(vh.MCS_CMD))
                        scApp.CMDBLL.updateCMD_MCS_TranStatus2Transferring(vh.MCS_CMD);
                    scApp.CMDBLL.setWillPassSectionInfo(vh.VEHICLE_ID, vh.PredictSectionsToDesination);
                    scApp.MapBLL.getPortID(vh.CUR_ADR_ID, out port_id);
                    scApp.PortBLL.OperateCatch.updatePortStationCSTExistStatus(port_id, string.Empty);
                    BCRReadResult bCRReadResult = BCRReadResult.BcrNormal;
                    AVIDINFO vid_info = scApp.VIDBLL.getVIDInfo(vh.VEHICLE_ID);
                    if (carrier_id.StartsWith("ERROR"))
                    {
                        string new_carrier_id =
                            $"NR-{vh.Real_ID.Trim()}-{vid_info.CARRIER_INSTALLED_TIME?.ToString(SCAppConstants.TimestampFormat_16)}";
                        scApp.VIDBLL.upDateVIDCarrierID(vh.VEHICLE_ID, new_carrier_id);//讀不到ID的話，更新為"NR-xxxx"
                        bCRReadResult = BCRReadResult.BcrReadFail;
                        scApp.CMDBLL.updateCMD_MCS_CmdState2BCRFail(vh.MCS_CMD);
                    }
                    //else if(!SCUtility.isMatche( carrier_id , vid_info.MCS_CARRIER_ID)) //Chris Add 修改手動命令會卡住136的問題
                    else if (!SCUtility.isMatche(carrier_id, vid_info.MCS_CARRIER_ID) && !SCUtility.isEmpty(vh.MCS_CMD))
                    {
                        scApp.VIDBLL.upDateVIDCarrierID(vh.VEHICLE_ID, carrier_id);
                        bCRReadResult = BCRReadResult.BcrMisMatch;
                        scApp.CMDBLL.updateCMD_MCS_CmdState2BCRFail(vh.MCS_CMD);
                    }
                    else
                    {
                        scApp.VIDBLL.upDateVIDCarrierID(vh.VEHICLE_ID, carrier_id);
                        bCRReadResult = BCRReadResult.BcrNormal;
                    }
                    scApp.VehicleBLL.updateVehicleBCRReadResult(vh, bCRReadResult);//因為已經要上報MCS Bcrcode Read Report，要先更新BCRReadResult
                    //scApp.PortBLL.OperateCatch.ClearAllPortStationCSTExistToEmpty();
                    break;
                case EventType.UnloadComplete:
                    //var port_station = scApp.MapBLL.getPortByAdrID(current_adr_id);//要考慮到一個Address會有多個Port的問題
                    //if (port_station != null)
                    //{
                    //    scApp.VIDBLL.upDateVIDCarrierLocInfo(eqpt.VEHICLE_ID, port_station.PORT_ID);
                    //}
                    scApp.VIDBLL.upDateVIDCarrierLocInfoUnloadComplete(vh.VEHICLE_ID);

                    //scApp.MapBLL.getPortID(eqpt.CUR_ADR_ID, out port_id);
                    //scApp.PortBLL.OperateCatch.updatePortStationCSTExistStatus(port_id, eqpt.CST_ID);

                    break;
            }


            List<AMCSREPORTQUEUE> reportqueues = new List<AMCSREPORTQUEUE>();
            using (TransactionScope tx = SCUtility.getTransactionScope())
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    if (!SCUtility.isEmpty(vh.MCS_CMD))
                    {
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                           Data: $"do report {eventType} to mcs.",
                           VehicleID: vh.VEHICLE_ID,
                           CarrierID: vh.CST_ID);
                        //bool isCreatReportInfoSuccess = scApp.ReportBLL.ReportArrivalOrComplete
                        //                                (eqpt.VEHICLE_ID, eventType, current_adr_id, current_sec_id, carrier_id, eqpt.MCS_CMD, reportqueues);
                        bool isCreatReportInfoSuccess = false;
                        switch (eventType)
                        {
                            case EventType.LoadArrivals:
                                isCreatReportInfoSuccess = scApp.ReportBLL.newReportLoadArrivals(vh.VEHICLE_ID, reportqueues);
                                break;
                            case EventType.LoadComplete:
                                isCreatReportInfoSuccess = scApp.ReportBLL.newReportLoadComplete(vh.VEHICLE_ID, vh.BCRReadResult, reportqueues);
                                break;
                            case EventType.UnloadArrivals:
                                isCreatReportInfoSuccess = scApp.ReportBLL.newReportUnloadArrivals(vh.VEHICLE_ID, reportqueues);
                                break;
                            case EventType.UnloadComplete:
                                isCreatReportInfoSuccess = scApp.ReportBLL.newReportUnloadComplete(vh.VEHICLE_ID, reportqueues);
                                break;
                            default:
                                break;
                        }
                        if (!isCreatReportInfoSuccess)
                        {
                            return;
                        }
                        scApp.ReportBLL.insertMCSReport(reportqueues);
                    }

                    Boolean resp_cmp = replyTranEventReport(bcfApp, eventType, vh, seqNum);

                    if (resp_cmp)
                    {
                        tx.Complete();
                    }
                    else
                    {
                        return;
                    }
                }
            }
            scApp.ReportBLL.newSendMCSMessage(reportqueues);
            switch (eventType)
            {
                case EventType.LoadArrivals:
                    scApp.VehicleBLL.doLoadArrivals(vh.VEHICLE_ID, current_adr_id, current_sec_id);
                    scApp.ReserveBLL.RemoveAllReservedSectionsByVehicleID(vh.VEHICLE_ID);
                    break;
                case EventType.LoadComplete:
                    scApp.VehicleBLL.doLoadComplete(vh.VEHICLE_ID, current_adr_id, current_sec_id, carrier_id);
                    break;
                case EventType.UnloadArrivals:
                    scApp.VehicleBLL.doUnloadArrivals(vh.VEHICLE_ID, current_adr_id, current_sec_id);
                    scApp.ReserveBLL.RemoveAllReservedSectionsByVehicleID(vh.VEHICLE_ID);
                    break;
                case EventType.UnloadComplete:
                    scApp.VehicleBLL.doUnloadComplete(vh.VEHICLE_ID);
                    break;
            }
        }
        private void TranEventReportBlockRelease(BCFApplication bcfApp, AVEHICLE eqpt, ID_136_TRANS_EVENT_REP recive_str, int seq_num)
        {
            string release_adr = recive_str.ReleaseBlockAdrID;
            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
               Data: $"Process block release,release address id:{release_adr}",
               VehicleID: eqpt.VEHICLE_ID,
               CarrierID: eqpt.CST_ID);
            try
            {

            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: ex,
                   VehicleID: eqpt.VEHICLE_ID,
                   CarrierID: eqpt.CST_ID);
                logger.Warn(ex, "Warn");
            }
            replyTranEventReport(bcfApp, recive_str.EventType, eqpt, seq_num);
        }

        private bool replyTranEventReport(BCFApplication bcfApp, EventType eventType, AVEHICLE eqpt, int seq_num, bool reserveSuccess = true, bool canBlockPass = true, bool canHIDPass = true,
                                  string renameCarrierID = "", CMDCancelType cancelType = CMDCancelType.CmdNone, RepeatedField<ReserveInfo> reserveInfos = null,
                                  Google.Protobuf.IMessage extensionMessage = null)
        {
            ID_36_TRANS_EVENT_RESPONSE send_str = new ID_36_TRANS_EVENT_RESPONSE
            {
                EventType = eventType,
                IsReserveSuccess = reserveSuccess ? ReserveResult.Success : ReserveResult.Unsuccess,
                IsBlockPass = canBlockPass ? PassType.Pass : PassType.Block,
                ReplyCode = 0,
                RenameCarrierID = renameCarrierID,
                ReplyActiveType = cancelType,

            };
            if (reserveInfos != null)
            {
                send_str.ReserveInfos.AddRange(reserveInfos);
            }
            if (extensionMessage == null)
            {
                extensionMessage = new com.mirle.ibg3k0.sc.ProtocolFormat.NorthInnolux.Agvmessage.ID_36_TRANS_EVENT_RESPONSE_EXTENSION();
            }

            send_str.ExtensionMessage = Google.Protobuf.WellKnownTypes.Any.Pack(extensionMessage);

            WrapperMessage wrapper = new WrapperMessage
            {
                SeqNum = seq_num,
                ImpTransEventResp = send_str
            };





            //在client端將其解封包
            ID_36_TRANS_EVENT_RESPONSE_EXTENSION extension_message = null;
            if (send_str.ExtensionMessage != null)
            {
                send_str.ExtensionMessage.TryUnpack(out extension_message);
            }

            Google.Protobuf.Reflection.TypeRegistry t = Google.Protobuf.Reflection.TypeRegistry.FromMessages(ID_36_TRANS_EVENT_RESPONSE_EXTENSION.Descriptor);
            //Boolean resp_cmp = ITcpIpControl.sendGoogleMsg(bcfApp, eqpt.TcpIpAgentName, wrapper, true);

            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
              seq_num: seq_num, Data: send_str,
              VehicleID: eqpt.VEHICLE_ID,
              CarrierID: eqpt.CST_ID);
            Boolean resp_cmp = eqpt.sendMessage(wrapper, true);
            SCUtility.RecodeReportInfo(eqpt.VEHICLE_ID, seq_num, send_str, resp_cmp.ToString(), t);
            return resp_cmp;
        }
        #endregion Transfer Report
        #region Status Report
        public void ReserveStopTest(string vhID, bool is_reserve_stop)
        {
            AVEHICLE vh = scApp.VehicleBLL.cache.getVehicle(vhID);
            scApp.VehicleBLL.cache.SetReservePause(vhID, is_reserve_stop ? VhStopSingle.StopSingleOn : VhStopSingle.StopSingleOff);
            //vh.NotifyVhStatusChange();
        }
        [ClassAOPAspect]
        public override void StatusReport(BCFApplication bcfApp, AVEHICLE eqpt, ID_144_STATUS_CHANGE_REP recive_str, int seq_num)
        {
            if (scApp.getEQObjCacheManager().getLine().ServerPreStop)
                return;
            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
               seq_num: seq_num,
               Data: recive_str,
               VehicleID: eqpt.VEHICLE_ID,
               CarrierID: eqpt.CST_ID);

            SCUtility.RecodeReportInfo(eqpt.VEHICLE_ID, seq_num, recive_str);

            uint batteryCapacity = recive_str.BatteryCapacity;
            VHModeStatus modeStat = scApp.VehicleBLL.DecideVhModeStatus(eqpt.VEHICLE_ID, recive_str.ModeStatus, batteryCapacity);
            VHActionStatus actionStat = recive_str.ActionStatus;
            VhPowerStatus powerStat = recive_str.PowerStatus;
            string cstID = recive_str.CSTID;
            VhStopSingle reserveStatus = recive_str.ReserveStatus;
            VhStopSingle obstacleStat = recive_str.ObstacleStatus;
            VhStopSingle blockingStat = recive_str.BlockingStatus;
            VhStopSingle pauseStat = recive_str.PauseStatus;
            VhStopSingle errorStat = recive_str.ErrorStatus;
            VhLoadCSTStatus loadCSTStatus = recive_str.HasCST;
            int obstacleDIST = recive_str.ObstDistance;
            string obstacleVhID = recive_str.ObstVehicleID;
            int steeringWheel = recive_str.SteeringWheel;
            bool hasdifferent = !SCUtility.isMatche(eqpt.CST_ID, cstID) ||
                                eqpt.MODE_STATUS != modeStat ||
                                eqpt.ACT_STATUS != actionStat ||
                                eqpt.ObstacleStatus != obstacleStat ||
                                eqpt.BlockingStatus != blockingStat ||
                                eqpt.PauseStatus != pauseStat ||
                                eqpt.ERROR != errorStat ||
                                eqpt.HAS_CST != (int)loadCSTStatus ||
                                eqpt.BatteryCapacity != batteryCapacity ||
                                eqpt.STEERINGWHEELANGLE != steeringWheel;

            if (loadCSTStatus == VhLoadCSTStatus.Exist)
            {
                eqpt.CarrierInstall();
            }
            else
            {
                eqpt.CarrierRemove();
                ProcessAlarmReport(null, AlarmBLL.VEHICLE_LONG_TIME_INSTALLED_CARRIER, ErrorStatus.ErrReset, $"vehicle long time installed carrier.");
            }

            if (modeStat != eqpt.MODE_STATUS)
            {
                eqpt.onModeStatusChange(modeStat);
            }

            //20190906 暫時不去判斷該條件
            //checkCurrentCmdStatusWithVhActionStat(eqpt.VEHICLE_ID, actionStat);
            if (eqpt.RESERVE_PAUSE != reserveStatus)
            {
                scApp.VehicleBLL.cache.SetReservePause(eqpt.VEHICLE_ID, reserveStatus);
            }
            if (hasdifferent && !scApp.VehicleBLL.doUpdateVehicleStatus(eqpt,
                                   cstID, modeStat, actionStat,
                                   blockingStat, pauseStat, obstacleStat, VhStopSingle.StopSingleOff, errorStat, loadCSTStatus,
                                   batteryCapacity))
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: $"update vhicle status fail!",
                   VehicleID: eqpt.VEHICLE_ID,
                   CarrierID: eqpt.CST_ID);
                return;
            }

            if (eqpt.ERROR != errorStat)
            {
                //todo 在error flag 有變化時，上報S5F1 alarm set/celar
            }

            //  reply_status_event_report(bcfApp, eqpt, seq_num);
        }



        private bool reply_status_event_report(BCFApplication bcfApp, AVEHICLE eqpt, int seq_num)
        {
            ID_44_STATUS_CHANGE_RESPONSE send_str = new ID_44_STATUS_CHANGE_RESPONSE
            {
                ReplyCode = 0
            };
            WrapperMessage wrapper = new WrapperMessage
            {
                SeqNum = seq_num,
                StatusChangeResp = send_str
            };

            //Boolean resp_cmp = ITcpIpControl.sendGoogleMsg(bcfApp, eqpt.TcpIpAgentName, wrapper, true);
            Boolean resp_cmp = eqpt.sendMessage(wrapper, true);
            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
              seq_num: seq_num, Data: send_str,
              VehicleID: eqpt.VEHICLE_ID,
              CarrierID: eqpt.CST_ID);
            SCUtility.RecodeReportInfo(eqpt.VEHICLE_ID, seq_num, send_str, resp_cmp.ToString());
            return resp_cmp;
        }
        #endregion Status Report
        #region Command Complete Report
        [ClassAOPAspect]
        public override void CommandCompleteReport(string tcpipAgentName, BCFApplication bcfApp, AVEHICLE vh, ID_132_TRANS_COMPLETE_REPORT recive_str, int seq_num)
        {
            if (scApp.getEQObjCacheManager().getLine().ServerPreStop)
                return;
            ID_32_TRANS_COMPLETE_RESPONSE send_str = null;
            SCUtility.RecodeReportInfo(vh.VEHICLE_ID, seq_num, recive_str);
            string finish_ohxc_cmd = vh.OHTC_CMD;
            string finish_mcs_cmd = vh.MCS_CMD;
            string cmd_id = recive_str.CmdID;
            int travel_dis = recive_str.CmdDistance;
            CompleteStatus completeStatus = recive_str.CmpStatus;
            string cur_sec_id = recive_str.CurrentSecID;
            string cur_adr_id = recive_str.CurrentAdrID;
            string cur_cst_id = recive_str.CSTID;
            string vh_id = vh.VEHICLE_ID.ToString();
            bool isSuccess = true;

            scApp.VIDBLL.upDateVIDResultCode(vh.VEHICLE_ID, vh.State, completeStatus);
            //send_str = new ID_32_TRANS_COMPLETE_RESPONSE
            //{
            //    ReplyCode = 0
            //};
            //WrapperMessage wrapper = new WrapperMessage
            //{
            //    SeqNum = seq_num,
            //    TranCmpResp = send_str
            //};

            //Boolean resp_cmp = ITcpIpControl.sendGoogleMsg(bcfApp, tcpipAgentName, wrapper, true);
            //Boolean resp_cmp = eqpt.sendMessage(wrapper, true);
            bool isAddCmdToWaitingRetryMCSCMDList = false;
            bool informMCSCMDFailed = false;
            List<AMCSREPORTQUEUE> reportqueues = new List<AMCSREPORTQUEUE>();
            if (!SCUtility.isEmpty(finish_mcs_cmd))
            {
                //List<AMCSREPORTQUEUE> reportqueues = new List<AMCSREPORTQUEUE>();
                using (TransactionScope tx = SCUtility.getTransactionScope())
                {
                    using (DBConnection_EF con = DBConnection_EF.GetUContext())
                    {
                        switch (completeStatus)
                        {
                            case CompleteStatus.CmpStatusCancel:
                                isSuccess = scApp.ReportBLL.newReportTransferCancelCompleted(vh.VEHICLE_ID, reportqueues);
                                vh.NeedToRetryAfterVehicleAbort = false;
                                vh.curCMDRetryCount = 0;
                                break;
                            case CompleteStatus.CmpStatusAbort:
                                isSuccess = scApp.ReportBLL.newReportTransferCommandAbortFinish(vh.VEHICLE_ID, reportqueues);
                                vh.NeedToRetryAfterVehicleAbort = false;
                                vh.curCMDRetryCount = 0;
                                break;
                            case CompleteStatus.CmpStatusLoad:
                            case CompleteStatus.CmpStatusUnload:
                            case CompleteStatus.CmpStatusLoadunload:
                                //case CompleteStatus.CmpStatusIdmisMatch:      //20210113 removed
                                //case CompleteStatus.CmpStatusIdreadFailed:    //20210113 removed
                                //case CompleteStatus.CmpStatusVehicleAbort:    //20201030 removed
                                isSuccess = scApp.ReportBLL.newReportTransferCommandFinish(vh.VEHICLE_ID, reportqueues);
                                vh.NeedToRetryAfterVehicleAbort = false;
                                vh.curCMDRetryCount = 0;
                                break;
                            case CompleteStatus.CmpStatusInterlockError:    //2021.10.15
                                vh.curCMDRetryCount = 0;
                                vh.NeedToRetryAfterVehicleAbort = false;
                                if (vh.HAS_CST == 1)
                                {
                                    isSuccess = scApp.ReportBLL.newReportUnloadFailed(vh.VEHICLE_ID, reportqueues);
                                    informMCSCMDFailed = true;
                                }
                                else
                                {
                                    isSuccess = scApp.ReportBLL.newReportLoadFailed(vh.VEHICLE_ID, reportqueues);
                                }
                                isSuccess = scApp.ReportBLL.newReportTransferCommandFinish(vh.VEHICLE_ID, reportqueues);
                                break;
                            case CompleteStatus.CmpStatusMove:
                            case CompleteStatus.CmpStatusHome:
                            case CompleteStatus.CmpStatusOverride:
                            case CompleteStatus.CmpStatusCstIdrenmae:
                            case CompleteStatus.CmpStatusMtlhome:
                            case CompleteStatus.CmpStatusMoveToCharger:
                            case CompleteStatus.CmpStatusSystemOut:
                            case CompleteStatus.CmpStatusSystemIn:
                            case CompleteStatus.CmpStatusTechingMove:
                            case CompleteStatus.CmpStatusIdmisMatch:      //20210113 added
                            case CompleteStatus.CmpStatusIdreadFailed:    //20210113 added
                                vh.NeedToRetryAfterVehicleAbort = false;
                                vh.curCMDRetryCount = 0;
                                break;
                            case CompleteStatus.CmpStatusVehicleAbort: //20201030 added
                                                                       //case CompleteStatus.CmpStatusInterlockError:
                                                                       //just add new ohtc command...
                                                                       //if( vh.curCMDRetryCount >= cmdRetryCount)
                                                                       //{
                                                                       //    vh.curCMDRetryCount = 0;
                                                                       //    isAddCmdToWaitingRetryMCSCMDList = false;
                                                                       //    if (vh.HAS_CST == 1)
                                                                       //    {
                                                                       //        informMCSCMDFailed = true;
                                                                       //    }
                                                                       //    else
                                                                       //    {
                                                                       //        vh.no_needs_to_retry = true;
                                                                       //        isSuccess = scApp.ReportBLL.newReportTransferCommandFinish(vh.VEHICLE_ID, reportqueues);
                                                                       //    }
                                                                       //}
                                                                       //else
                                                                       //{
                                                                       //    vh.curCMDRetryCount++;
                                                                       //    isAddCmdToWaitingRetryMCSCMDList = true;
                                                                       //}
                                if (cmdRetryCount >= 0 && vh.curCMDRetryCount < cmdRetryCount)
                                {
                                    vh.NeedToRetryAfterVehicleAbort = true;
                                    vh.curCMDRetryCount++;
                                    isAddCmdToWaitingRetryMCSCMDList = true;
                                }
                                else
                                {
                                    vh.curCMDRetryCount = 0;
                                    isAddCmdToWaitingRetryMCSCMDList = false;
                                    vh.NeedToRetryAfterVehicleAbort = false;
                                    isSuccess = scApp.ReportBLL.newReportTransferCommandFinish(vh.VEHICLE_ID, reportqueues);
                                }
                                //WaitingRetryMCSCMDList.Add(vh.VEHICLE_ID, finish_mcs_cmd);
                                break;
                            default:
                                logger.Info($"Proc func:CommandCompleteReport, but completeStatus:{completeStatus} notimplemented ");
                                vh.curCMDRetryCount = 0;
                                break;
                        }

                        scApp.ReportBLL.insertMCSReport(reportqueues);
                        if (isSuccess)
                        {
                            tx.Complete();
                        }
                        else
                        {
                            return;
                        }
                    }
                }
                //scApp.ReportBLL.newSendMCSMessage(reportqueues);
            }

            //tryReleaseReservedControl(vh_id, cur_sec_id);
            string start_adr = vh.startAdr;

            scApp.ReserveBLL.RemoveAllReservedSectionsByVehicleID(vh.VEHICLE_ID);
            vh.RedundantSections.Clear();
            vh.resetVhGuideInfo();

            if (completeStatus != CompleteStatus.CmpStatusIdmisMatch && completeStatus != CompleteStatus.CmpStatusIdreadFailed)
            {
                using (TransactionScope tx = SCUtility.getTransactionScope())
                {
                    using (DBConnection_EF con = DBConnection_EF.GetUContext())
                    {
                        //isSuccess &= scApp.VehicleBLL.doTransferCommandFinish(vh.VEHICLE_ID, cmd_id, completeStatus, travel_dis);
                        isSuccess &= scApp.VehicleBLL.doTransferCommandFinish(vh_id, cmd_id, completeStatus, travel_dis);
                        //isSuccess &= scApp.VIDBLL.initialVIDCommandInfo(vh.VEHICLE_ID);
                        isSuccess &= scApp.VIDBLL.initialVIDCommandInfo(vh_id);

                        if (isSuccess)
                        {
                            tx.Complete();
                        }
                        else
                        {
                            return;
                        }
                    }
                }
            }

            send_str = new ID_32_TRANS_COMPLETE_RESPONSE
            {
                ReplyCode = 0
            };
            WrapperMessage wrapper = new WrapperMessage
            {
                SeqNum = seq_num,
                TranCmpResp = send_str
            };

            Boolean resp_cmp = vh.sendMessage(wrapper, true);
            if (reportqueues != null && reportqueues.Count > 0)
                scApp.ReportBLL.newSendMCSMessage(reportqueues);

            SCUtility.RecodeReportInfo(vh.VEHICLE_ID, seq_num, send_str, finish_ohxc_cmd, finish_mcs_cmd, resp_cmp.ToString());
            vh.NotifyVhExcuteCMDStatusChange();
            vh.VhAvoidInfo = null;
            if (!SCUtility.isEmpty(cur_cst_id))
            {
                scApp.VIDBLL.upDateVIDCarrierID(vh.VEHICLE_ID, cur_cst_id);
                scApp.VIDBLL.upDateVIDCarrierLocInfo(vh.VEHICLE_ID, vh.Real_ID);
            }

            sendCommandCompleteEventToNats(vh.VEHICLE_ID, recive_str);

            if (DebugParameter.IsDebugMode && DebugParameter.IsCycleRun)
            {
                SpinWait.SpinUntil(() => false, DebugParameter.CycleRunIntervalTime);
                TestCycleRun(vh, cmd_id, start_adr);
            }
            if (completeStatus != CompleteStatus.CmpStatusIdmisMatch && completeStatus != CompleteStatus.CmpStatusIdreadFailed)
            {
                vh.onCommandComplete(completeStatus);
            }

            if (scApp.getEQObjCacheManager().getLine().SCStats == ALINE.TSCState.PAUSING)
            {
                List<ACMD_MCS> cmd_mcs_lst = scApp.CMDBLL.loadACMD_MCSIsUnfinished();
                if (cmd_mcs_lst.Count == 0)
                {
                    scApp.LineService.TSCStateToPause();
                }
            }
            if (isAddCmdToWaitingRetryMCSCMDList)
            {
                addCMDToWaitingRetryMCSCMDList(vh.VEHICLE_ID, finish_mcs_cmd);
            }
            if (informMCSCMDFailed)
            {
                if (vh.HAS_CST == 1)
                {
                    scApp.ReportBLL.RequestDestnationChange(finish_mcs_cmd, cur_cst_id);
                }
                else
                {
                    //do nothing
                }
                //
            }

        }

        private void tryReleaseReservedControl(string vhID, string curSecID)
        {
            try
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: "Begin try release not normally released reserved control... ",
                   VehicleID: vhID);
                //清空除了自己目前Section所屬的Address外的Reserved
                ASECTION current_section = scApp.SectionBLL.cache.GetSection(curSecID);
                if (current_section != null)
                    scApp.AddressesBLL.redis.AllAddressRelease(vhID, current_section.NodeAddress);

                //如果自己不是在交通管制路段上，則嘗試清空
                var block_control_section_check_result = scApp.getCommObjCacheManager().IsBlockControlSection(curSecID);
                if (!block_control_section_check_result.isBlockControlSec)
                {
                    scApp.BlockControlBLL.updateBlockZoneQueue_ForceRelease(vhID);
                }
                //如果自己不是在Block的路段上，則嘗試進行清空
                var traffic_control_section_check_result = scApp.TrafficControlBLL.cache.IsTrafficControlSection(curSecID);
                if (!traffic_control_section_check_result.isTrafficControlInfo)
                {
                    scApp.TrafficControlBLL.redis.TrafficControlRelease(vhID);
                }

                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: "End try release not normally released reserved control. ",
                   VehicleID: vhID);
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: ex,
                   VehicleID: vhID);
            }
        }

        private void ProcessVehicleAbort(string vhID, string mcsCmdID, List<AMCSREPORTQUEUE> reportqueues)
        {
            bool is_success = true;
            ACMD_MCS mcs_cmd = scApp.CMDBLL.getCMD_MCSByID(mcsCmdID);
            if (mcs_cmd.TRANSFERSTATE < E_TRAN_STATUS.Transferring)
            {

            }
            else
            {
                is_success = scApp.ReportBLL.newReportTransferCommandFinish(vhID, reportqueues);
            }
        }

        public bool doCommandFigish(string vhID, string cmdID, CompleteStatus completeStatus, int totalDistance)
        {
            AVEHICLE vh = scApp.VehicleBLL.cache.getVehicle(vhID);
            bool is_success = true;
            bool is_mcs_cmd = SCUtility.isEmpty(vh.MCS_CMD);
            if (!is_mcs_cmd)
            {
                is_success &= ReportTransferResult2MCS(vh, vh.MCS_CMD, completeStatus, totalDistance);
            }
            is_success &= FinishVehicleExcuteCommand(vh, completeStatus);
            return is_success;
        }

        private bool ReportTransferResult2MCS(AVEHICLE vh, string mcsCmdID, CompleteStatus completeStatus, int totalDistance)
        {
            bool isSuccess = false;
            scApp.VIDBLL.upDateVIDResultCode(vh.VEHICLE_ID, vh.State, completeStatus);
            List<AMCSREPORTQUEUE> reportqueues = new List<AMCSREPORTQUEUE>();
            using (TransactionScope tx = SCUtility.getTransactionScope())
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    switch (completeStatus)
                    {
                        case CompleteStatus.CmpStatusCancel:
                            isSuccess = scApp.ReportBLL.newReportTransferCancelCompleted(vh.VEHICLE_ID, reportqueues);
                            break;
                        case CompleteStatus.CmpStatusAbort:
                            isSuccess = scApp.ReportBLL.newReportTransferCommandAbortFinish(vh.VEHICLE_ID, reportqueues);
                            break;
                        case CompleteStatus.CmpStatusLoad:
                        case CompleteStatus.CmpStatusUnload:
                        case CompleteStatus.CmpStatusLoadunload:
                        case CompleteStatus.CmpStatusIdmisMatch:
                        case CompleteStatus.CmpStatusIdreadFailed:
                        case CompleteStatus.CmpStatusVehicleAbort:
                        case CompleteStatus.CmpStatusInterlockError:
                        case CompleteStatus.CmpStatusLongTimeInaction:
                        case CompleteStatus.CmpStatusForceFinishByOp:
                            isSuccess = scApp.ReportBLL.newReportTransferCommandFinish(vh.VEHICLE_ID, reportqueues);
                            break;
                        case CompleteStatus.CmpStatusMove:
                        case CompleteStatus.CmpStatusHome:
                        case CompleteStatus.CmpStatusOverride:
                        case CompleteStatus.CmpStatusCstIdrenmae:
                        case CompleteStatus.CmpStatusMtlhome:
                        case CompleteStatus.CmpStatusMoveToCharger:
                        case CompleteStatus.CmpStatusSystemOut:
                        case CompleteStatus.CmpStatusSystemIn:
                        case CompleteStatus.CmpStatusTechingMove:
                            //Nothing...
                            break;
                        default:
                            logger.Info($"Proc func:CommandCompleteReport, but completeStatus:{completeStatus} notimplemented ");
                            break;
                    }

                    scApp.ReportBLL.insertMCSReport(reportqueues);
                    if (isSuccess)
                    {
                        tx.Complete();
                    }
                }
            }
            scApp.ReportBLL.newSendMCSMessage(reportqueues);
            return isSuccess;
        }
        private bool FinishVehicleExcuteCommand(AVEHICLE vh, CompleteStatus completeStatus)
        {
            bool is_success = true;
            bool is_mcs_cmd = SCUtility.isEmpty(vh.MCS_CMD);
            string cmd_id = vh.OHTC_CMD;
            string vh_id = vh.VEHICLE_ID;
            using (TransactionScope tx = SCUtility.getTransactionScope())
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    is_success &= scApp.VehicleBLL.doTransferCommandFinish(vh_id, cmd_id, completeStatus, 0);
                    is_success &= scApp.VIDBLL.initialVIDCommandInfo(vh_id);
                    if (is_success)
                    {
                        tx.Complete();
                        vh.NotifyVhExcuteCMDStatusChange();
                    }
                }
            }

            //var finish_cmd_mcs_list = scApp.CMDBLL.loadFinishCMD_MCS();
            //if (finish_cmd_mcs_list != null && finish_cmd_mcs_list.Count > 0)
            //{
            //    using (TransactionScope tx = SCUtility.getTransactionScope())
            //    {
            //        using (DBConnection_EF con = DBConnection_EF.GetUContext())
            //        {
            //            scApp.CMDBLL.remoteCMD_MCSByBatch(finish_cmd_mcs_list);
            //            List<HCMD_MCS> hcmd_mcs_list = finish_cmd_mcs_list.Select(cmd => cmd.ToHCMD_MCS()).ToList();
            //            scApp.CMDBLL.CreatHCMD_MCSs(hcmd_mcs_list);

            //            tx.Complete();
            //        }
            //    }

            //}

            return is_success;
        }

        public void ForceCommandComplete(string vhID)
        {
            AVEHICLE vh = scApp.VehicleBLL.cache.getVehicle(vhID);

        }


        private void TestCycleRun(AVEHICLE vh, string cmd_id, string start_adr)
        {
            ACMD_OHTC cmd = scApp.CMDBLL.GetCMD_OHTCByID(cmd_id);
            if (cmd == null) return;
            if (!(cmd.CMD_TPYE == E_CMD_TYPE.LoadUnload || cmd.CMD_TPYE == E_CMD_TYPE.Move)) return;

            ACMD_OHTC cmd_obj = null;
            string result = string.Empty;
            bool isSuccess = false;
            string cst_id = cmd.CARRIER_ID?.Trim();
            string from_port_id = cmd.DESTINATION.Trim();
            string to_port_id = cmd.SOURCE.Trim();
            string from_adr = "";
            string to_adr = "";
            switch (cmd.CMD_TPYE)
            {
                case E_CMD_TYPE.LoadUnload:
                    scApp.MapBLL.getAddressID(from_port_id, out from_adr);
                    scApp.MapBLL.getAddressID(to_port_id, out to_adr);
                    break;
                case E_CMD_TYPE.Move:
                    to_adr = start_adr.Trim();
                    break;
            }
            isSuccess = scApp.CMDBLL.doCreatTransferCommand_New(cmd.VH_ID, out cmd_obj,
                                            carrier_id: cst_id,
                                            cmd_type: cmd.CMD_TPYE,
                                            source: from_adr,
                                            destination: to_adr,
                                            gen_cmd_type: SCAppConstants.GenOHxCCommandType.Manual);
            if (isSuccess)
            {
                isSuccess = scApp.VehicleService.doSendCommandToVh(vh, cmd_obj);
            }

        }

        private void sendCommandCompleteEventToNats(string vhID, ID_132_TRANS_COMPLETE_REPORT recive_str)
        {
            byte[] arrayByte = new byte[recive_str.CalculateSize()];
            recive_str.WriteTo(new Google.Protobuf.CodedOutputStream(arrayByte));
            scApp.getNatsManager().PublishAsync
                (string.Format(SCAppConstants.NATS_SUBJECT_VH_COMMAND_COMPLETE_0, vhID), arrayByte);
        }

        #endregion Command Complete Report
        #region Alarm

        [ClassAOPAspect]
        public override void AlarmReport(BCFApplication bcfApp, AVEHICLE eqpt, ID_194_ALARM_REPORT recive_str, int seq_num)
        {
            SCUtility.RecodeReportInfo(eqpt.VEHICLE_ID, seq_num, recive_str);
            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
              seq_num: seq_num, Data: recive_str,
              VehicleID: eqpt.VEHICLE_ID,
              CarrierID: eqpt.CST_ID);
            try
            {
                string node_id = eqpt.NODE_ID;
                string eq_id = eqpt.VEHICLE_ID;
                string err_code = recive_str.ErrCode;
                string err_desc = recive_str.ErrDescription;
                ErrorStatus status = recive_str.ErrStatus;
                ProcessAlarmReport(eqpt, err_code, status, err_desc);
                ID_94_ALARM_RESPONSE send_str = new ID_94_ALARM_RESPONSE
                {
                    ReplyCode = 0
                };
                WrapperMessage wrapper = new WrapperMessage
                {
                    SeqNum = seq_num,
                    AlarmResp = send_str
                };
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                  seq_num: seq_num, Data: send_str,
                  VehicleID: eqpt.VEHICLE_ID,
                  CarrierID: eqpt.CST_ID);

                Boolean resp_cmp = eqpt.sendMessage(wrapper, true);
                SCUtility.RecodeReportInfo(eqpt.VEHICLE_ID, seq_num, send_str, resp_cmp.ToString());
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: $"do reply alarm report ,{resp_cmp}",
                   VehicleID: eqpt.VEHICLE_ID,
                   CarrierID: eqpt.CST_ID);


            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception:");
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: ex,
                   VehicleID: eqpt.VEHICLE_ID,
                   CarrierID: eqpt.CST_ID);
            }
        }

        public override void ProcessAlarmReport(AVEHICLE eqpt, string err_code, ErrorStatus status, string errorDesc)
        {
            try
            {
                string node_id;
                string vh_id;
                if (eqpt == null)
                {
                    node_id = "AGVC";
                    vh_id = "AGVC";
                }
                else
                {
                    node_id = eqpt.NODE_ID;
                    vh_id = eqpt.VEHICLE_ID;
                }
                bool is_all_alarm_clear = SCUtility.isMatche(err_code, "0") && status == ErrorStatus.ErrReset;
                if (!is_all_alarm_clear)
                {
                    var alarm_map = scApp.AlarmBLL.GetAlarmMap(node_id, err_code);
                    if (alarm_map == null)
                    {
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(NorthInnoLuxVehicleService), Device: DEVICE_NAME_AGV,
                           Data: $"Process vehicle alarm report,but can not found alarm map. alarm code:{err_code},alarm status{status},error desc:{errorDesc}",
                           VehicleID: eqpt?.VEHICLE_ID ?? "",
                           CarrierID: eqpt?.CST_ID ?? "");
                        return;
                    }

                    //if (is_all_alarm_clear ||
                    //    (alarm_map != null && alarm_map.ALARM_LVL == E_ALARM_LVL.Error))
                    //{
                    //在設備上報Alarm時，如果是第一次上報(之前都沒有Alarm發生時，則要上報S6F11 CEID=52 Alarm Set)
                    //if (status == ErrorStatus.ErrSet &&
                    //    !scApp.AlarmBLL.hasAlarmExist())
                    //{
                    //    scApp.ReportBLL.ReportAlarmSet();
                    //}
                    if (status == ErrorStatus.ErrSet &&
                        !scApp.AlarmBLL.hasAlarmErrorExist() && alarm_map.ALARM_LVL == E_ALARM_LVL.Error)
                    {
                        scApp.ReportBLL.ReportAlarmSet();
                    }
                }
                List<ALARM> alarms = null;

                scApp.getRedisCacheManager().BeginTransaction();
                using (TransactionScope tx = SCUtility.getTransactionScope())
                {
                    using (DBConnection_EF con = DBConnection_EF.GetUContext())
                    {
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                           Data: $"Process vehicle alarm report.alarm code:{err_code},alarm status{status},error desc:{errorDesc}",
                           VehicleID: eqpt?.VEHICLE_ID ?? "",
                           CarrierID: eqpt?.CST_ID ?? "");
                        ALARM alarm = null;
                        if (is_all_alarm_clear)
                        {
                            alarms = scApp.AlarmBLL.resetAllAlarmReport(vh_id);
                            scApp.AlarmBLL.resetAllAlarmReport2Redis(vh_id);
                        }
                        else
                        {
                            switch (status)
                            {
                                case ErrorStatus.ErrSet:
                                    //將設備上報的Alarm填入資料庫。
                                    alarm = scApp.AlarmBLL.setAlarmReport(node_id, vh_id, err_code, errorDesc);
                                    //將其更新至Redis，保存目前所發生的Alarm
                                    scApp.AlarmBLL.setAlarmReport2Redis(alarm);
                                    alarms = new List<ALARM>() { alarm };
                                    break;
                                case ErrorStatus.ErrReset:
                                    //將設備上報的Alarm從資料庫刪除。
                                    alarm = scApp.AlarmBLL.resetAlarmReport(vh_id, err_code);
                                    //將其更新至Redis，保存目前所發生的Alarm
                                    scApp.AlarmBLL.resetAlarmReport2Redis(alarm);
                                    alarms = new List<ALARM>() { alarm };
                                    break;
                            }
                        }
                        tx.Complete();
                    }
                }
                scApp.getRedisCacheManager().ExecuteTransaction();
                //通知有Alarm的資訊改變。
                scApp.getNatsManager().PublishAsync(SCAppConstants.NATS_SUBJECT_CURRENT_ALARM, new byte[0]);

                foreach (ALARM report_alarm in alarms)
                {
                    if (report_alarm == null) continue;
                    if (report_alarm.ALAM_LVL != E_ALARM_LVL.Error) continue;
                    //需判斷Alarm是否存在如果有的話則需再判斷MCS是否有Disable該Alarm的上報
                    if (scApp.AlarmBLL.IsReportToHost(report_alarm.ALAM_CODE))
                    {
                        string alarm_code = report_alarm.ALAM_CODE;
                        //scApp.ReportBLL.ReportAlarmHappend(eqpt.VEHICLE_ID, alarm.ALAM_STAT, alarm.ALAM_CODE, alarm.ALAM_DESC, out reportqueues);
                        List<AMCSREPORTQUEUE> reportqueues = new List<AMCSREPORTQUEUE>();
                        AlarmConvertInfo alarmConvertInfo = scApp.AlarmBLL.getAlarmConvertInfo(alarm_code);
                        if (alarmConvertInfo != null)
                        {
                            if (report_alarm.ALAM_STAT == ErrorStatus.ErrSet)
                            {

                                scApp.ReportBLL.ReportAlarmHappend(report_alarm.ALAM_STAT, alarmConvertInfo.ALID, alarmConvertInfo.ALTX);
                                scApp.ReportBLL.newReportAlarmEvent(eqpt?.Real_ID ?? "AGVC", alarmConvertInfo.CEIDSet, alarmConvertInfo.ALID, eqpt?.MCS_CMD ?? "", alarmConvertInfo.ALTX, alarmConvertInfo.AlarmLevel, reportqueues);
                            }
                            else
                            {
                                scApp.ReportBLL.ReportAlarmHappend(report_alarm.ALAM_STAT, alarm_code, report_alarm.ALAM_DESC);
                                scApp.ReportBLL.newReportAlarmEvent(eqpt?.Real_ID ?? "AGVC", alarmConvertInfo.CEIDClear, alarmConvertInfo.ALID, eqpt?.MCS_CMD ?? "", alarmConvertInfo.ALTX, alarmConvertInfo.AlarmLevel, reportqueues);
                            }
                        }
                        else
                        {
                            BCFApplication.onErrorMsg($"Can not found AlarmConvertInfo,alarm code:{err_code}, AlarmConvertInfo not found");
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(NorthInnoLuxVehicleService), Device: DEVICE_NAME_AGV,
                           Data: $"can not found AlarmConvertInfo,alarm code:{err_code},alarm status{status}",
                           VehicleID: eqpt?.VEHICLE_ID ?? "",
                           CarrierID: eqpt?.CST_ID ?? "");
                        }

                        scApp.ReportBLL.newSendMCSMessage(reportqueues);

                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(NorthInnoLuxVehicleService), Device: DEVICE_NAME_AGV,
                           Data: $"do report alarm to mcs,alarm code:{err_code},alarm status{status}",
                           VehicleID: eqpt?.VEHICLE_ID ?? "",
                           CarrierID: eqpt?.CST_ID ?? "");
                    }
                }
                //在設備上報取消Alarm，如果已經沒有Alarm(Alarm都已經消除，則要上報S6F11 CEID=52 Alarm Clear)
                if (status == ErrorStatus.ErrReset &&
                    !scApp.AlarmBLL.hasAlarmErrorExist())
                {
                    scApp.ReportBLL.ReportAlarmCleared();
                }
                //}
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: ex,
                   VehicleID: eqpt?.VEHICLE_ID ?? "",
                   CarrierID: eqpt?.CST_ID ?? "");
            }
        }
        #endregion Alarm

        #region Range Teach
        [ClassAOPAspect]
        public void RangeTeachingCompleteReport(string tcpipAgentName, BCFApplication bcfApp, AVEHICLE eqpt, ID_172_RANGE_TEACHING_COMPLETE_REPORT recive_str, int seq_num)
        {
            ID_72_RANGE_TEACHING_COMPLETE_RESPONSE response = null;
            response = new ID_72_RANGE_TEACHING_COMPLETE_RESPONSE()
            {
                ReplyCode = 0
            };

            WrapperMessage wrapper = new WrapperMessage
            {
                SeqNum = seq_num,
                RangeTeachingCmpResp = response
            };
            Boolean resp_cmp = eqpt.sendMessage(wrapper, true);
            SCUtility.RecodeReportInfo(eqpt.VEHICLE_ID, seq_num, response, resp_cmp.ToString());
        }

        #endregion Range Teach
        #region Receive Message
        public void BasicInfoVersionReport(BCFApplication bcfApp, AVEHICLE eqpt, ID_102_BASIC_INFO_VERSION_REP recive_str, int seq_num)
        {
            ID_2_BASIC_INFO_VERSION_RESPONSE send_str = new ID_2_BASIC_INFO_VERSION_RESPONSE
            {
                ReplyCode = 0
            };
            WrapperMessage wrapper = new WrapperMessage
            {
                SeqNum = seq_num,
                BasicInfoVersionResp = send_str
            };
            Boolean resp_cmp = eqpt.sendMessage(wrapper, true);
            //SCUtility.RecodeReportInfo(eqpt.VEHICLE_ID, seqNum, send_str, resp_cmp.ToString());
        }
        public void IndividualDownloadRequest(BCFApplication bcfApp, AVEHICLE eqpt, ID_162_INDIVIDUAL_DOWNLOAD_REQ recive_str, int seq_num)
        {
            ID_62_INDIVIDUAL_DOWNLOAD_RESPONSE send_str = new ID_62_INDIVIDUAL_DOWNLOAD_RESPONSE
            {
                OffsetGuideFL = 1,
                OffsetGuideRL = 2,
                OffsetGuideFR = 3,
                OffsetGuideRR = 4
            };
            WrapperMessage wrapper = new WrapperMessage
            {
                SeqNum = seq_num,
                IndividualDownloadResp = send_str
            };
            Boolean resp_cmp = eqpt.sendMessage(wrapper, true);
            //SCUtility.RecodeReportInfo(eqpt.VEHICLE_ID, seqNum, send_str, resp_cmp.ToString());
        }
        public void AddressTeachReport(BCFApplication bcfApp, AVEHICLE eqpt, ID_174_ADDRESS_TEACH_REPORT recive_str, int seq_num)
        {
            try
            {
                string adr_id = recive_str.Addr;
                int resolution = recive_str.Position;

                //scApp.DataSyncBLL.updateAddressData(eqpt.VEHICLE_ID, adr_id, resolution);

                ID_74_ADDRESS_TEACH_RESPONSE send_str = new ID_74_ADDRESS_TEACH_RESPONSE
                {
                    ReplyCode = 0
                };
                WrapperMessage wrapper = new WrapperMessage
                {
                    SeqNum = seq_num,
                    AddressTeachResp = send_str
                };
                Boolean resp_cmp = eqpt.sendMessage(wrapper, true);
                //SCUtility.RecodeReportInfo(eqpt.VEHICLE_ID, seqNum, send_str, resp_cmp.ToString());
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception:");
            }
        }
        #endregion Receive Message
        #region Vehicle Change The Path
        public void VhicleChangeThePath(string vh_id)
        {
            //string ohxc_cmd_id = "";
            //try
            //{
            //    bool isSuccess = true;
            //    AVEHICLE need_change_path_vh = scApp.getEQObjCacheManager().getVehicletByVHID(vh_id);
            //    if (need_change_path_vh.VhRecentTranEvent == EventType.Vhloading ||
            //        need_change_path_vh.VhRecentTranEvent == EventType.Vhunloading)
            //        return;
            //    //1.先下暫停給該台VH
            //    isSuccess = PauseRequest(vh_id, PauseEvent.Pause, OHxCPauseType.OHxC);
            //    //2.送出31執行命令的Override
            //    //  a.取得執行中的命令
            //    //  b.重新將該命令改成Ready to rewrite
            //    ACMD_OHTC cmd_ohtc = null;
            //    using (TransactionScope tx = SCUtility.getTransactionScope())
            //    {
            //        using (DBConnection_EF con = DBConnection_EF.GetUContext())
            //        {
            //            isSuccess &= scApp.CMDBLL.updateCMD_OHxC_Status2ReadyToReWirte(need_change_path_vh.OHTC_CMD, out cmd_ohtc);
            //            isSuccess &= scApp.CMDBLL.update_CMD_Detail_2AbnormalFinsh(need_change_path_vh.OHTC_CMD, need_change_path_vh.WillPassSectionID);
            //            if (isSuccess)
            //                tx.Complete();
            //        }
            //    }
            //    ohxc_cmd_id = cmd_ohtc.CMD_ID.Trim();
            //    scApp.VehicleService.doSendOverrideCommandToVh(need_change_path_vh, cmd_ohtc);
            //}
            //catch (BLL.VehicleBLL.BlockedByTheErrorVehicleException blockedExecption)
            //{
            //    logger.Warn(blockedExecption, "BlockedByTheErrorVehicleException:");
            //    VehicleBlockedByTheErrorVehicle();
            //}
            //catch (Exception ex)
            //{
            //    logger.Error(ex, "Exception:");
            //}
        }
        #endregion Vehicle Change The Path
        #region Vh connection / disconnention
        [ClassAOPAspect]
        public override void Connection(BCFApplication bcfApp, AVEHICLE vh)
        {
            //scApp.getEQObjCacheManager().refreshVh(eqpt.VEHICLE_ID);
            vh.VhRecentTranEvent = EventType.AdrPass;
            vh.isTcpIpConnect = true;
            vh.startVehicleTimer();

            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
               Data: "Connection ! Begin synchronize with vehicle...",
               VehicleID: vh.VEHICLE_ID,
               CarrierID: vh.CST_ID);
            VehicleInfoSynchronize(vh.VEHICLE_ID);
            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
               Data: "Connection ! End synchronize with vehicle.",
               VehicleID: vh.VEHICLE_ID,
               CarrierID: vh.CST_ID);

            SCUtility.RecodeConnectionInfo
                (vh.VEHICLE_ID,
                SCAppConstants.RecodeConnectionInfo_Type.Connection.ToString(),
                vh.getDisconnectionIntervalTime(bcfApp));
        }
        [ClassAOPAspect]
        public void Disconnection(BCFApplication bcfApp, AVEHICLE vh)
        {
            vh.isTcpIpConnect = false;

            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
               Data: "Disconnection !",
               VehicleID: vh.VEHICLE_ID,
               CarrierID: vh.CST_ID);
            SCUtility.RecodeConnectionInfo
                (vh.VEHICLE_ID,
                SCAppConstants.RecodeConnectionInfo_Type.Disconnection.ToString(),
                vh.getConnectionIntervalTime(bcfApp));
        }
        #endregion Vh Connection / disconnention

        #region Vehicle Install/Remove
        public (bool isSuccess, string result) Install(string vhID)
        {
            try
            {
                AVEHICLE vh_vo = scApp.VehicleBLL.cache.getVehicle(vhID);
                if (!vh_vo.isTcpIpConnect)
                {
                    string message = $"vh:{vhID} current not connection, can't excute action:Install";
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Data: message,
                       VehicleID: vhID);
                    return (false, message);
                }
                if (vh_vo.IsOnAdr)
                {
                    AADDRESS currentAddress = scApp.AddressesBLL.cache.GetAddress(vh_vo.CUR_ADR_ID);
                    if (currentAddress is null)
                    {
                        string message = $"vh:{vhID} current address:{SCUtility.Trim(vh_vo.CUR_ADR_ID, true)} is not exist, can't excute action:Install";
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                           Data: message,
                           VehicleID: vhID);
                        return (false, message);
                    }
                    var addResult = scApp.ReserveBLL.TryAddVehicleOrUpdate(vh_vo.VEHICLE_ID, vh_vo.CUR_ADR_ID);
                    if (!addResult.OK)
                    {
                        string message = $"vh:{vhID} current address:{SCUtility.Trim(vh_vo.CUR_ADR_ID, true)} can't reserved," +
                            $" can't excute action:Install";
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                           Data: message,
                           VehicleID: vhID);
                        return (false, message);
                    }
                }
                else
                {
                    ASECTION current_section = scApp.SectionBLL.cache.GetSection(vh_vo.CUR_SEC_ID);
                    if (current_section == null)
                    {
                        string message = $"vh:{vhID} current section:{SCUtility.Trim(vh_vo.CUR_SEC_ID, true)} is not exist, can't excute action:Install";
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                           Data: message,
                           VehicleID: vhID);
                        return (false, message);
                    }

                    var ReserveResult = askReserveSuccess(vhID, vh_vo.CUR_SEC_ID, vh_vo.CUR_ADR_ID);
                    if (!ReserveResult.isSuccess)
                    {
                        string message = $"vh:{vhID} current section:{SCUtility.Trim(vh_vo.CUR_SEC_ID, true)} can't reserved," +
                                         $" can't excute action:Install";
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                           Data: message,
                           VehicleID: vhID);
                        return (false, message);
                    }
                }

                bool is_success = true;
                is_success = is_success && scApp.VehicleBLL.updataVehicleInstall(vhID);
                if (vh_vo.MODE_STATUS == VHModeStatus.Manual)
                {
                    //北群創不需要報這個 
                    ProcessAlarmReport(vh_vo, AlarmBLL.VEHICLE_CAN_NOT_SERVICE, ErrorStatus.ErrSet, $"vehicle cannot service");
                }
                List<AMCSREPORTQUEUE> reportqueues = new List<AMCSREPORTQUEUE>();
                is_success = is_success && scApp.ReportBLL.newReportVehicleInstalled(vh_vo.Real_ID, reportqueues);
                scApp.ReportBLL.newSendMCSMessage(reportqueues);
                return (true, "");
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: ex,
                   VehicleID: vhID);
                return (false, "");
            }
        }
        public (bool isSuccess, string result) Remove(string vhID)
        {
            try
            {
                //1.確認該VH 是否可以進行Remove
                //  a.是否為斷線狀態
                //2.將該台VH 更新成Remove狀態
                //3.將位置的資訊清空。(包含Reserve的路段、紅綠燈、Block)
                //4.上報給MCS
                AVEHICLE vh_vo = scApp.VehicleBLL.cache.getVehicle(vhID);

                if (vh_vo.isTcpIpConnect)
                {
                    string message = $"vh:{vhID} current is connection, can't excute action:remove";
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Data: message,
                       VehicleID: vhID);
                    return (false, message);
                }
                bool is_success = true;
                is_success = is_success && scApp.VehicleBLL.updataVehicleRemove(vhID);
                if (is_success)
                {
                    vh_vo.VechileRemove();
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Data: $"vh id:{vhID} remove success. start release reserved control...",
                       VehicleID: vhID);
                    scApp.ReserveBLL.RemoveAllReservedSectionsByVehicleID(vh_vo.VEHICLE_ID);
                    //北群創不需要報這個 
                    ProcessAlarmReport(vh_vo, AlarmBLL.VEHICLE_CAN_NOT_SERVICE, ErrorStatus.ErrReset, $"vehicle cannot service");
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Data: $"vh id:{vhID} remove success. end release reserved control.",
                       VehicleID: vhID);
                }
                List<AMCSREPORTQUEUE> reportqueues = new List<AMCSREPORTQUEUE>();
                is_success = is_success && scApp.ReportBLL.newReportVehicleRemoved(vh_vo.Real_ID, reportqueues);
                scApp.ReportBLL.newSendMCSMessage(reportqueues);
                return (true, "");
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: ex,
                   VehicleID: vhID);
                return (false, "");
            }
        }
        #endregion Vehicle Install/Remove

        #region Avoid Control
        const int MaxOverrideCountAfterAvoid = 5;
        const int IntervalBetweenOverrideAfterAvoid = 30;
        public void AvoidCompleteReport(BCFApplication bcfApp, AVEHICLE eqpt, ID_152_AVOID_COMPLETE_REPORT recive_str, int seq_num)
        {
            if (scApp.getEQObjCacheManager().getLine().ServerPreStop)
                return;
            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
               Data: $"Process Avoid complete report.vh current address:{eqpt.CUR_ADR_ID}, current section:{eqpt.CUR_SEC_ID}",
               VehicleID: eqpt.VEHICLE_ID,
               CarrierID: eqpt.CST_ID);

            ID_52_AVOID_COMPLETE_RESPONSE send_str = null;
            SCUtility.RecodeReportInfo(eqpt.VEHICLE_ID, seq_num, recive_str);
            send_str = new ID_52_AVOID_COMPLETE_RESPONSE
            {
                ReplyCode = 0
            };
            WrapperMessage wrapper = new WrapperMessage
            {
                SeqNum = seq_num,
                AvoidCompleteResp = send_str
            };

            //Boolean resp_cmp = ITcpIpControl.sendGoogleMsg(bcfApp, tcpipAgentName, wrapper, true);
            Boolean resp_cmp = eqpt.sendMessage(wrapper, true);

            SCUtility.RecodeReportInfo(eqpt.VEHICLE_ID, seq_num, send_str, resp_cmp.ToString());

            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
               Data: $"start override avoid complete of vh current address:{eqpt.CUR_ADR_ID}, current section:{eqpt.CUR_SEC_ID}",
               VehicleID: eqpt.VEHICLE_ID,
               CarrierID: eqpt.CST_ID);
            ACMD_OHTC cmd_ohtc = scApp.CMDBLL.GetCMD_OHTCByID(eqpt.OHTC_CMD);

            scApp.ReserveBLL.RemoveAllReservedSectionsByVehicleID(eqpt.VEHICLE_ID);
            eqpt.resetVhGuideInfo();

            //bool is_success = trydoOverrideCommandToVh(eqpt, cmd_ohtc, "", true);
            //if (is_success)
            //{
            //    eqpt.VhAvoidInfo = null;
            //}
            eqpt.VhAvoidInfo = null;
            bool is_success = false;
            int tryCount = 0;
            while (!is_success && tryCount < MaxOverrideCountAfterAvoid)
            {
                is_success = trydoOverrideCommandToVh(eqpt, cmd_ohtc, "", true);
                if (!is_success)
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                        Data: $"avoid complete but find override path fail, vh current address:{eqpt.CUR_ADR_ID}, current section:{eqpt.CUR_SEC_ID}, retry times:{tryCount}",
                        VehicleID: eqpt.VEHICLE_ID,
                        CarrierID: eqpt.CST_ID);
                    SpinWait.SpinUntil(() => false, IntervalBetweenOverrideAfterAvoid);
                }
                tryCount++;
            }

            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
               Data: $"end override avoid complete of vh current address:{eqpt.CUR_ADR_ID}, current section:{eqpt.CUR_SEC_ID} ,result:{is_success}",
               VehicleID: eqpt.VEHICLE_ID,
               CarrierID: eqpt.CST_ID);

        }
        #endregion Avoid Control
        #region Specially Control
        public bool changeVhStatusToAutoRemote(string vhID)
        {
            scApp.VehicleBLL.updataVehicleMode(vhID, VHModeStatus.AutoRemote);
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vhID);
            vh?.NotifyVhStatusChange();
            return true;
        }
        public bool changeVhStatusToAutoLocal(string vhID)
        {
            scApp.VehicleBLL.updataVehicleMode(vhID, VHModeStatus.AutoLocal);
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vhID);
            vh?.NotifyVhStatusChange();
            return true;
        }
        public bool changeVhStatusToAutoCharging(string vhID)
        {
            scApp.VehicleBLL.updataVehicleMode(vhID, VHModeStatus.AutoCharging);
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vhID);
            vh?.NotifyVhStatusChange();
            return true;
        }
        public void forceReleaseBlockControl(string vh_id = "")
        {
            List<BLOCKZONEQUEUE> queues = null;
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {

                if (SCUtility.isEmpty(vh_id))
                {
                    queues = scApp.MapBLL.loadAllNonReleaseBlockQueue();
                }
                else
                {
                    queues = scApp.MapBLL.loadNonReleaseBlockQueueByCarID(vh_id);
                }

                foreach (var queue in queues)
                {
                    scApp.MapBLL.updateBlockZoneQueue_AbnormalEnd(queue, SCAppConstants.BlockQueueState.Abnormal_Release_ForceRelease);
                    scApp.MapBLL.DeleteBlockControlKeyWordToRedis(queue.CAR_ID.Trim(), queue.ENTRY_SEC_ID);
                }
            }
        }
        public void reCheckBlockControl(BLOCKZONEQUEUE blockZoneQueue)
        {
            ABLOCKZONEMASTER blockmaster = scApp.MapBLL.getBlockZoneMasterByEntrySecID(blockZoneQueue.ENTRY_SEC_ID);
            if (blockmaster != null)
            {
                List<string> lstSecid = scApp.MapBLL.loadBlockZoneDetailSecIDsByEntrySecID(blockZoneQueue.ENTRY_SEC_ID);
                if (!scApp.VehicleBLL.hasVehicleOnSections(lstSecid))
                {
                    using (TransactionScope tx = SCUtility.getTransactionScope())
                    {
                        using (DBConnection_EF con = DBConnection_EF.GetUContext())
                        {
                            scApp.MapBLL.updateBlockZoneQueue_AbnormalEnd(blockZoneQueue, SCAppConstants.BlockQueueState.Abnormal_Release_ForceRelease);
                            scApp.MapBLL.DeleteBlockControlKeyWordToRedis(blockZoneQueue.CAR_ID, blockZoneQueue.ENTRY_SEC_ID);
                            tx.Complete();
                        }
                    }
                }
            }
        }
        public void PauseAllVehicleByOHxCPause()
        {
            List<AVEHICLE> vhs = scApp.getEQObjCacheManager().getAllVehicle();
            foreach (var vh in vhs)
            {
                PauseRequest(vh.VEHICLE_ID, PauseEvent.Pause, OHxCPauseType.Normal);
            }
        }
        public void ResumeAllVehicleByOhxCPause()
        {
            List<AVEHICLE> vhs = scApp.getEQObjCacheManager().getAllVehicle();
            foreach (var vh in vhs)
            {
                PauseRequest(vh.VEHICLE_ID, PauseEvent.Continue, OHxCPauseType.Normal);
            }
        }

        public void CarrierInstalledByManual(string vhID, string cstID)
        {
            AVEHICLE vh = scApp.VehicleBLL.cache.getVehicle(vhID);
            if (vh == null)
            {
                throw new NullReferenceException($"Vh:{vhID} is not exist.");
            }
            scApp.VIDBLL.upDateVIDCarrierID(vhID, cstID);
            scApp.VIDBLL.upDateVIDCarrierLocInfo(vhID, vh.Real_ID);
            scApp.VehicleBLL.updataVehicleCSTID(vhID, cstID);

        }
        public void CarrierRemovedByManual(string vhID)
        {
            AVEHICLE vh = scApp.VehicleBLL.cache.getVehicle(vhID);
            if (vh == null)
            {
                throw new NullReferenceException($"Vh:{vhID} is not exist.");
            }
            scApp.VIDBLL.upDateVIDCarrierID(vhID, string.Empty);
            scApp.VIDBLL.upDateVIDCarrierLocInfo(vhID, string.Empty);
            scApp.VehicleBLL.updataVehicleCSTID(vhID, string.Empty);
        }
        #endregion Specially Control

        #region RoadService
        public (bool isSuccess, ASEGMENT segment) doEnableDisableSegment(string segment_id, E_PORT_STATUS port_status)
        {
            ASEGMENT segment = null;
            try
            {
                //List<APORTSTATION> port_stations = scApp.MapBLL.loadAllPortBySegmentID(segment_id);

                using (TransactionScope tx = SCUtility.getTransactionScope())
                {
                    using (DBConnection_EF con = DBConnection_EF.GetUContext())
                    {

                        switch (port_status)
                        {
                            case E_PORT_STATUS.InService:
                                segment = scApp.GuideBLL.unbanRouteTwoDirect(segment_id);
                                scApp.SegmentBLL.cache.EnableSegment(segment_id);
                                break;
                            case E_PORT_STATUS.OutOfService:
                                segment = scApp.GuideBLL.banRouteTwoDirect(segment_id);
                                scApp.SegmentBLL.cache.DisableSegment(segment_id);
                                break;
                        }
                        //foreach (APORTSTATION port_station in port_stations)
                        //{
                        //    scApp.MapBLL.updatePortStatus(port_station.PORT_ID, port_status);
                        //}
                        tx.Complete();
                    }
                }
                //List<AMCSREPORTQUEUE> reportqueues = new List<AMCSREPORTQUEUE>();
                //foreach (APORTSTATION port_station in port_stations)
                //{
                //    switch (port_status)
                //    {
                //        case E_PORT_STATUS.InService:
                //            scApp.ReportBLL.ReportPortInServeice(port_station.PORT_ID, reportqueues);
                //            break;
                //        case E_PORT_STATUS.OutOfService:
                //            scApp.ReportBLL.ReportPortOutServeice(port_station.PORT_ID, reportqueues);
                //            break;
                //    }
                //}
                //scApp.ReportBLL.sendMCSMessageAsyn(reportqueues);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception:");
            }
            return (segment != null, segment);
        }
        #endregion RoadService
        #region TEST
        private void CarrierInterfaceSim_LoadComplete(AVEHICLE vh)
        {
            //vh.CatchPLCCSTInterfacelog();
            bool[] bools_01 = new bool[16];
            bool[] bools_02 = new bool[16];
            bool[] bools_03 = new bool[16];
            bool[] bools_04 = new bool[16];
            bool[] bools_05 = new bool[16];
            bool[] bools_06 = new bool[16];
            bool[] bools_07 = new bool[16];
            bool[] bools_08 = new bool[16];
            bool[] bools_09 = new bool[16];
            bool[] bools_10 = new bool[16];

            bools_01[3] = true;

            bools_02[03] = true; bools_02[08] = true; bools_02[12] = true; bools_02[14] = true;
            bools_02[15] = true;

            bools_03[3] = true; bools_03[8] = true; bools_03[10] = true; bools_03[12] = true;
            bools_03[14] = true; bools_03[15] = true;

            bools_04[3] = true; bools_04[4] = true; bools_04[8] = true; bools_04[10] = true;
            bools_04[12] = true; bools_04[14] = true; bools_04[15] = true;

            bools_05[3] = true; bools_05[4] = true; bools_05[8] = true; bools_05[10] = true;
            bools_05[11] = true; bools_05[12] = true; bools_05[14] = true; bools_05[15] = true;

            bools_06[3] = true; bools_06[4] = true; bools_06[5] = true; bools_06[8] = true;
            bools_06[10] = true; bools_06[11] = true; bools_06[12] = true; bools_06[14] = true;
            bools_06[15] = true;

            bools_07[3] = true; bools_07[4] = true; bools_07[5] = true; bools_07[10] = true;
            bools_07[11] = true; bools_07[12] = true; bools_07[14] = true; bools_07[15] = true;

            bools_08[3] = true; bools_08[6] = true; bools_08[10] = true; bools_08[11] = true;
            bools_08[12] = true; bools_08[14] = true; bools_08[15] = true;

            bools_09[3] = true; bools_09[6] = true; bools_09[10] = true; bools_09[12] = true;
            bools_09[14] = true; bools_09[15] = true;

            bools_10[3] = true;

            List<bool[]> lst_bools = new List<bool[]>()
            {
                bools_01,bools_02,bools_03,bools_04,bools_05,bools_06,bools_07,bools_08,bools_09,bools_10,
            };
            if (DebugParameter.isTestCarrierInterfaceError)
            {
                RandomSetCSTInterfaceBool(bools_03);
                RandomSetCSTInterfaceBool(bools_04);
                RandomSetCSTInterfaceBool(bools_05);
                RandomSetCSTInterfaceBool(bools_06);
                RandomSetCSTInterfaceBool(bools_07);
                RandomSetCSTInterfaceBool(bools_08);
                RandomSetCSTInterfaceBool(bools_09);
                //lst_bools[6][11] = false;
            }
            string port_id = "";
            scApp.MapBLL.getPortID(vh.CUR_ADR_ID, out port_id);

            scApp.PortBLL.OperateCatch.updatePortStationCSTExistStatus(port_id, string.Empty);

            CarrierInterface_LogOut(vh.VEHICLE_ID, port_id, lst_bools);
        }

        private static void RandomSetCSTInterfaceBool(bool[] bools_03)
        {
            Random rnd_Index = new Random(Guid.NewGuid().GetHashCode());
            int rnd_value_1 = rnd_Index.Next(bools_03.Length - 1);
            int rnd_value_2 = rnd_Index.Next(bools_03.Length - 1);
            int rnd_value_3 = rnd_Index.Next(bools_03.Length - 1);
            int rnd_value_4 = rnd_Index.Next(bools_03.Length - 1);
            int rnd_value_5 = rnd_Index.Next(bools_03.Length - 1);
            int rnd_value_6 = rnd_Index.Next(bools_03.Length - 1);
            bools_03[rnd_value_1] = true;
            bools_03[rnd_value_2] = true;
            bools_03[rnd_value_3] = true;
            bools_03[rnd_value_4] = true;
            bools_03[rnd_value_5] = true;
            bools_03[rnd_value_6] = true;
        }

        private void CarrierInterfaceSim_UnloadComplete(AVEHICLE vh, string carrier_id)
        {
            //vh.CatchPLCCSTInterfacelog();
            ChargerInterface vehicleCSTInterface = new ChargerInterface();
            bool[] bools_01 = new bool[16];
            bool[] bools_02 = new bool[16];
            bool[] bools_03 = new bool[16];
            bool[] bools_04 = new bool[16];
            bool[] bools_05 = new bool[16];
            bool[] bools_06 = new bool[16];
            bool[] bools_07 = new bool[16];
            bool[] bools_08 = new bool[16];
            bool[] bools_09 = new bool[16];
            bool[] bools_10 = new bool[16];

            bools_01[3] = true;

            bools_02[03] = true; bools_02[9] = true; bools_02[12] = true; bools_02[14] = true;
            bools_02[15] = true;

            bools_03[3] = true; bools_03[9] = true; bools_03[10] = true; bools_03[12] = true;
            bools_03[14] = true; bools_03[15] = true;

            bools_04[3] = true; bools_04[4] = true; bools_04[9] = true; bools_04[10] = true;
            bools_04[12] = true; bools_04[14] = true; bools_04[15] = true;

            bools_05[3] = true; bools_05[4] = true; bools_05[9] = true; bools_05[10] = true;
            bools_05[11] = true; bools_05[12] = true; bools_05[14] = true; bools_05[15] = true;

            bools_06[3] = true; bools_06[4] = true; bools_06[5] = true; bools_06[9] = true;
            bools_06[10] = true; bools_06[11] = true; bools_06[12] = true; bools_06[14] = true;
            bools_06[15] = true;

            bools_07[3] = true; bools_07[4] = true; bools_07[5] = true; bools_07[10] = true;
            bools_07[11] = true; bools_07[12] = true; bools_07[14] = true; bools_07[15] = true;

            bools_08[3] = true; bools_08[6] = true; bools_08[10] = true; bools_08[11] = true;
            bools_08[12] = true; bools_08[14] = true; bools_08[15] = true;

            bools_09[3] = true; bools_09[6] = true; bools_09[10] = true; bools_09[12] = true;
            bools_09[14] = true; bools_09[15] = true;

            bools_10[3] = true;
            List<bool[]> lst_bools = new List<bool[]>()
            {
                bools_01,bools_02,bools_03,bools_04,bools_05,bools_06,bools_07,bools_08,bools_09,bools_10,
            };
            if (DebugParameter.isTestCarrierInterfaceError)
            {
                RandomSetCSTInterfaceBool(bools_03);
                RandomSetCSTInterfaceBool(bools_04);
                RandomSetCSTInterfaceBool(bools_05);
                RandomSetCSTInterfaceBool(bools_06);
                RandomSetCSTInterfaceBool(bools_07);
                RandomSetCSTInterfaceBool(bools_08);
                RandomSetCSTInterfaceBool(bools_09);
            }
            string port_id = "";
            scApp.MapBLL.getPortID(vh.CUR_ADR_ID, out port_id);
            scApp.PortBLL.OperateCatch.updatePortStationCSTExistStatus(port_id, carrier_id);

            CarrierInterface_LogOut(vh.VEHICLE_ID, port_id, lst_bools);
        }

        private static void CarrierInterface_LogOut(string vh_id, string port_id, List<bool[]> lst_bools)
        {
            ChargerInterface vehicleCSTInterface = new ChargerInterface();
            foreach (var bools in lst_bools)
            {
                DateTime now_time = DateTime.Now;
                vehicleCSTInterface.Details.Add(new ChargerInterface.ChargerInterfaceDetail()
                {
                    EQ_ID = vh_id,
                    Index = $"Recode{nameof(ChargerInterface)}",
                    ChargerSigna2 = bools,
                    Year = (ushort)now_time.Year,
                    Month = (ushort)now_time.Month,
                    Day = (ushort)now_time.Day,
                    Hour = (ushort)now_time.Hour,
                    Minute = (ushort)now_time.Minute,
                    Second = (ushort)now_time.Second,
                    Millisecond = (ushort)now_time.Millisecond,
                });
                SpinWait.SpinUntil(() => false, 100);
            }
            foreach (var detail in vehicleCSTInterface.Details)
            {
                LogManager.GetLogger("RecodeVehicleCSTInterface").Info(detail.ToString());
            }
        }

        #endregion TEST
    }
}
