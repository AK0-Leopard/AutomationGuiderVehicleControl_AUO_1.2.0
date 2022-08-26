using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using com.mirle.ibg3k0.sc.ProtocolFormat.OHTMessage;
using com.mirle.ibg3k0.sc.BLL;
using System.Collections;
using Newtonsoft.Json;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.App;

namespace com.mirle.ibg3k0.sc
{
    public partial class AADDRESS
    {
        private const int BIT_INDEX_CONTROL = 7;
        private const int BIT_INDEX_PORT = 2;
        private const int BIT_INDEX_COUPLER = 3;
        private const int BIT_INDEX_SECTION = 4;
        private const int BIT_INDEX_SEGMENT = 5;

        private const int ADR_TYPE_NUM_CAN_AVOID_ADR = 4;


        public Boolean[] AddressTypeFlags { get; set; }
        public string[] SegmentIDs { get; set; }

        public event EventHandler<string> VehicleRelease;
        //public AADDRESS()
        //{
        //    initialAddressType();
        //}
        public void initialAddressType()
        {
            string s_type = ADR_ID.Substring(0, 2);
            int.TryParse(s_type, out int type);
            BitArray b = new BitArray(new int[] { type });
            AddressTypeFlags = new bool[b.Count];
            b.CopyTo(AddressTypeFlags, 0);
        }
        public void initialSegmentID(SectionBLL sectionBLL)
        {
            var sections = sectionBLL.cache.GetSectionsByFromAddress(ADR_ID);
            SegmentIDs = sections.Select(sec => sec.SEG_NUM).Distinct().ToArray();
            if (SegmentIDs.Length == 0)
            {
                throw new Exception($"Adr id:{ADR_ID},no setting on section or segment");
            }
        }

        public void Release(string vhID)
        {
            OnAddressRelease(vhID);
        }

        private void OnAddressRelease(string vhID)
        {
            VehicleRelease?.Invoke(this, vhID);
        }


        [JsonIgnore]
        public bool IsCoupler
        { get { return AddressTypeFlags[BIT_INDEX_COUPLER]; } }
        //{ get { return false; } }
        //{ get { return ADR_ID == "24031"; } }
        //{ get { return ADR_ID == "10010"; } }
        //{ get { return ADR_ID == "24014"; }}
        //{ get { return ADR_ID == "10001"|| ADR_ID == "10007"; } }
        [JsonIgnore]
        public bool IsPort
        { get { return AddressTypeFlags[BIT_INDEX_PORT]; } }
        [JsonIgnore]
        public bool IsControl
        { get { return AddressTypeFlags[BIT_INDEX_CONTROL] || !SCUtility.isEmpty(SEC_ID); } }
        [JsonIgnore]
        public bool IsSegment
        {
            get
            {
                return AddressTypeFlags[BIT_INDEX_SEGMENT];
            }
        }


        [JsonIgnore]
        public bool IsSection
        {
            get
            {
                return AddressTypeFlags[BIT_INDEX_SECTION];
            }
        }
        
        public bool canAvoidVhecle
        {
            get
            {
                if (SCApplication.getInstance().BC_ID == "NORTH_INNOLUX_Test_Site") //暫時都return ok
                {
                    return true;
                    //如果是該廠的話先一律回復True，因為是台中的Demo Site
                }
                else
                {
                    //if (ADRTYPE != 4)
                    if (ADRTYPE != ADR_TYPE_NUM_CAN_AVOID_ADR)
                        return false;
                    else
                        return IsCoupler || IsPort;
                }
                //return IsCoupler || IsPort || (!IsSegment && IsSection);
            }
        }
    }

    public class CouplerAddress : AADDRESS, ICpuplerType, IComparable<CouplerAddress>
    {
        public string ChargerID { get; set; }
        public CouplerNum CouplerNum { get; set; }
        public bool IsEnable { get; set; }
        public int Priority { get; set; }
        public string[] TrafficControlSegment { get; set; }

        public bool hasVh(VehicleBLL vehicleBLL)
        {
            return vehicleBLL.hasVhOnAddress(ADR_ID);
        }
        public bool hasChargingVh(VehicleBLL vehicleBLL)
        {
            return vehicleBLL.hasChargingVhOnAddress(ADR_ID);
        }
        public bool hasVhGoing(VehicleBLL vehicleBLL)
        {
            return vehicleBLL.hasVhGoingAdr(ADR_ID);
        }

        public void setDistanceWithTargetAdr(GuideBLL guideBLL, string targetAdr)
        {
            if (!guideBLL.IsRoadWalkable(this.ADR_ID, targetAdr))
            {
                DistanceWithTargetAdr = int.MaxValue;
            }
            else
            {
                DistanceWithTargetAdr = guideBLL.getGuideInfo(this.ADR_ID, targetAdr).totalCost;
            }
        }

        public int DistanceWithTargetAdr { get; private set; } = 0;
        public int CompareTo(CouplerAddress other)
        {
            int result;
            if (this.Priority == other.Priority && this.DistanceWithTargetAdr == other.DistanceWithTargetAdr)
            {
                result = 0;
            }
            else
            {
                if (this.Priority > other.Priority)
                {
                    result = -1;
                }
                else if (this.Priority == other.Priority && this.DistanceWithTargetAdr < other.DistanceWithTargetAdr)
                {
                    result = -1;
                }
                else
                {
                    result = 1;
                }
            }

            return result;
        }
        public bool IsPreciseOnHere(AVEHICLE vh, BLL.ReserveBLL reserveBLL)
        {
            var adr_result = reserveBLL.GetHltMapAddress(ADR_ID);
            if (!adr_result.isExist) return true; //因為如果找不到對應的X、Y，就無法判斷是否是準確停在點上，因此保險作法就是回復true讓它當作已經是在點上
            double distance = getDistance(vh.X_Axis, vh.Y_Axis, adr_result.x, adr_result.y);
            if (distance > SystemParameter.MAX_ALLOW_DISTANCE_OFFSET_mm) return false;
            else return true;
        }

        private double getDistance(double x1, double y1, double x2, double y2)
        {
            double dx, dy;
            dx = x2 - x1;
            dy = y2 - y1;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }

    public class ReserveEnhanceAddress : AADDRESS, IReserveEnhance
    {
        public string[] EnhanceControlAddress { get; set; }
        public List<Data.VO.ReserveEnhanceInfo> infos { get; set; }
    }


    public class CouplerAndReserveEnhanceAddress : AADDRESS, ICpuplerType, IReserveEnhance
    {
        public string ChargerID { get; set; }
        public CouplerNum CouplerNum { get; set; }
        public bool IsEnable { get; set; }
        public int Priority { get; set; }
        public string[] TrafficControlSegment { get; set; }

        public bool hasVh(VehicleBLL vehicleBLL)
        {
            return vehicleBLL.hasVhOnAddress(ADR_ID);
        }
        public bool hasVhGoing(VehicleBLL vehicleBLL)
        {
            return vehicleBLL.hasVhGoingAdr(ADR_ID);
        }
        public string[] EnhanceControlAddress { get; set; }
        public List<Data.VO.ReserveEnhanceInfo> infos { get; set; }
    }

    public enum CouplerNum
    {
        NumberOne = 1,
        NumberTwo = 2,
        NumberThree = 3
    }

    public interface ICpuplerType
    {
        string ChargerID { get; set; }
        CouplerNum CouplerNum { get; set; }
        bool IsEnable { get; set; }
        int Priority { get; set; }
        string[] TrafficControlSegment { get; set; }
        bool hasVh(VehicleBLL vehicleBLL);
        bool hasVhGoing(VehicleBLL vehicleBLL);
    }

    public interface IReserveEnhance
    {
        //string WillPassSection { get; set; }
        string[] EnhanceControlAddress { get; set; }
        List<Data.VO.ReserveEnhanceInfo> infos { get; set; }
    }
}
