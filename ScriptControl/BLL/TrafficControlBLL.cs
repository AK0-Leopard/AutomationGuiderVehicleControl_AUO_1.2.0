using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.Data;
using com.mirle.ibg3k0.sc.Data.DAO;
using com.mirle.ibg3k0.sc.Data.VO;
using com.mirle.ibg3k0.sc.ProtocolFormat.OHTMessage;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace com.mirle.ibg3k0.sc.BLL
{
    public class TrafficControlBLL
    {
        public static NLog.Logger TrafficControlLogger = NLog.LogManager.GetLogger("TrafficControl");
        static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        protected SCApplication scApp;
        public Database dataBase { get; private set; }
        public Cache cache { get; private set; }
        public Redis redis { get; private set; }
        public TrafficControlBLL()
        {
        }
        public void start(SCApplication _app)
        {
            scApp = _app;
            cache = new Cache(scApp.getEQObjCacheManager());
            redis = new Redis(scApp.getRedisCacheManager(), scApp.SectionBLL);
        }


        public class Database
        {

        }
        public class Cache
        {
            EQObjCacheManager EqObjCacheManager = null;
            public Cache(EQObjCacheManager eqObjCacheManager)
            {
                EqObjCacheManager = eqObjCacheManager;
            }
            //取得所有traddic controller的物件
            public IEnumerable<TrafficController> LoadAllTrafficController()
            {
                var eqs = EqObjCacheManager.getAllEquipment();
                var trafficControllers = eqs.Where(e => e is TrafficController).Select(e => e as TrafficController);
                return trafficControllers;
            }
            public TrafficController GetTrafficController()
            {
                var eqs = EqObjCacheManager.getAllEquipment();
                var trafficControllers = eqs.Where(e => e is TrafficController).FirstOrDefault() as TrafficController;
                return trafficControllers;
            }
            public (bool hasPass, TrafficController controller) tryGetPassTrafficControl(string sectionID)
            {

                var eqs = EqObjCacheManager.getAllEquipment();
                var trafficControllers = eqs.Where(e => e is TrafficController).Select(e => e as TrafficController);
                var pass_traffic_controller = trafficControllers.Where(t => t.IsInControlSection(SCUtility.Trim(sectionID))).FirstOrDefault();
                if (pass_traffic_controller != null)
                {
                    return (true, pass_traffic_controller);
                }
                else
                {
                    return (false, null);
                }
            }
        }
        public class Redis
        {
            TimeSpan timeOut_10min = new TimeSpan(0, 10, 0);

            RedisCacheManager redisCacheManager = null;
            SectionBLL sectionBLL = null;
            public Redis(RedisCacheManager _redisCacheManager, SectionBLL _sectionBLL)
            {
                redisCacheManager = _redisCacheManager;
                sectionBLL = _sectionBLL;
            }



            public (bool hasReserve, string reserveVh, string reserveByAddress) trafficControlHasBeenReserve(string trafficControlID)
            {
                string adr_reserve_info = string.Format(SCAppConstants.REDIS_KEY_TRAFFIC_CONTROL_INFO_0, trafficControlID.Trim());
                string reserve_vh_address = redisCacheManager.StringGet(adr_reserve_info);
                string[] reserve_vh_address_array = reserve_vh_address.Split('#');
                string reserve_vh = reserve_vh_address_array[0];
                string reserve_by_address = string.Empty;
                if (reserve_vh_address_array.Count() > 1)
                    reserve_by_address = reserve_vh_address_array[1];
                return (!SCUtility.isEmpty(reserve_vh), reserve_vh, reserve_by_address);
            }

            public bool setTrafficControlKey(string trafficControlID, string vhID, string adrID)
            {
                string traffic_control_key = string.Format(SCAppConstants.REDIS_KEY_TRAFFIC_CONTROL_INFO_0, trafficControlID.Trim());
                string traffic_control_value = $"{vhID}#{SCUtility.Trim(adrID, true)}";
                //redisCacheManager.stringSetAsync(adr_reserve_key, vhID);
                return redisCacheManager.stringSet(traffic_control_key, traffic_control_value);
            }


            public void TrafficControlRelease(string trafficControlID, string vhID)
            {
                string traffic_control_id = trafficControlID.Trim();
                string traffic_control_reserve_info = string.Format(SCAppConstants.REDIS_KEY_TRAFFIC_CONTROL_INFO_0, traffic_control_id);
                string reserve_vh_address_id = redisCacheManager.StringGet(traffic_control_reserve_info);
                string[] reserve_vh_section_array = reserve_vh_address_id.Split('#');
                string reserve_vh = reserve_vh_section_array[0];

                bool is_success = false;
                if (SCUtility.isMatche(vhID, reserve_vh))
                    is_success = redisCacheManager.KeyDelete(traffic_control_reserve_info);

                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(TrafficControlBLL), Device: "AGVC",
                   Details: $"Excute traffic control id:{traffic_control_id} release by vh id:{vhID},result:{is_success}");
            }

            public void TrafficControlRelease(string vhID)
            {
                string key_pattern = string.Format(SCAppConstants.REDIS_KEY_TRAFFIC_CONTROL_INFO_0, "*");
                var all_reserve_key = redisCacheManager.SearchKey(key_pattern).ToArray();
                if (all_reserve_key.Count() > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var reserve_key in all_reserve_key)
                    {
                        string value = redisCacheManager.StringGet(reserve_key, false);
                        string[] reserve_vh_section_array = value.Split('#');
                        string reserve_vh = reserve_vh_section_array[0];
                        if (SCUtility.isMatche(vhID, reserve_vh))
                        {
                            bool is_success = redisCacheManager.KeyDelete(reserve_key, false);
                            sb.Append($"key:{reserve_key},value:{value},is_success:{is_success}");
                            sb.AppendLine();
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(TrafficControlBLL), Device: "AGVC",
                               Details: $"Excute traffic control release:{sb.ToString()},by vh id:{vhID}",
                               VehicleID: vhID);
                        }
                    }
                }
            }

            public void ForceAllTrafficControlRelease()
            {
                string key_pattern = string.Format(SCAppConstants.REDIS_KEY_TRAFFIC_CONTROL_INFO_0, "*");
                var all_reserve_key = redisCacheManager.SearchKey(key_pattern).ToArray();
                string[] all_reserve_value_vh_id = null;
                if (all_reserve_key.Count() > 0)
                {
                    all_reserve_value_vh_id = redisCacheManager.StringGet(all_reserve_key).
                                                                Select(redis_value => redis_value.ToString()).
                                                                ToArray();
                    long count = redisCacheManager.KeyDelete(all_reserve_key);


                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < all_reserve_key.Count(); i++)
                    {
                        sb.Append($"key:{all_reserve_key[i]},value:{all_reserve_value_vh_id[i]}");
                        sb.Append(",");
                    }

                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(TrafficControlBLL), Device: "AGVC",
                       Details: $"Excute force traffic control release {sb.ToString()}");
                }
            }



            public void hasReserveTrafficControl(string vhID)
            {
                string key_pattern = string.Format(SCAppConstants.REDIS_KEY_TRAFFIC_CONTROL_INFO_0, "*");
                var all_reserve_key = redisCacheManager.SearchKey(key_pattern).ToArray();
                string[] all_reserve_value_vh_id = null;
                if (all_reserve_key.Count() > 0)
                {
                    all_reserve_value_vh_id = redisCacheManager.StringGet(all_reserve_key).
                                                                Select(redis_value => redis_value.ToString()).
                                                                ToArray();
                    foreach (string reserve_vh_address_value in all_reserve_value_vh_id)
                    {
                        string[] reserve_vh_address_array = reserve_vh_address_value.Split('#');
                        string reserve_vh = reserve_vh_address_array[0];
                    }

                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < all_reserve_key.Count(); i++)
                    {
                        sb.Append($"key:{all_reserve_key[i]},value:{all_reserve_value_vh_id[i]}");
                        sb.Append(",");
                    }

                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(TrafficControlBLL), Device: "AGVC",
                       Details: $"Excute force traffic control release {sb.ToString()}");
                }
            }



        }



    }
}
