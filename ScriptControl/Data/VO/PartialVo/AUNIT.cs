using com.mirle.ibg3k0.bcf.App;
using com.mirle.ibg3k0.bcf.Common;
using com.mirle.ibg3k0.bcf.Data.ValueDefMapAction;
using com.mirle.ibg3k0.bcf.Data.VO;
using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Data.SECS;
using com.mirle.ibg3k0.sc.Data.ValueDefMapAction;
using com.mirle.ibg3k0.sc.Data.VO;
using com.mirle.ibg3k0.sc.Data.VO.Interface;
using com.mirle.ibg3k0.sc.ObjectRelay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.mirle.ibg3k0.sc
{
    public partial class AUNIT : BaseUnitObject
    {
        private AlarmHisList alarmHisList = new AlarmHisList();
        public event EventHandler<SCAppConstants.CouplerHPSafety> CouplerHPSafetyChaged;
        #region charger 
        //private string coupler1Address;
        //public virtual string Coupler1Address
        //{
        //    get { return coupler1Address; }
        //    set
        //    {
        //        if (coupler1Address != value)
        //        {
        //            coupler1Address = value;
        //            OnPropertyChanged(BCFUtility.getPropertyName(() => this.Coupler1Address));
        //        }
        //    }
        //}

        //private string coupler2Address;
        //public virtual string Coupler2Address
        //{
        //    get { return coupler2Address; }
        //    set
        //    {
        //        if (coupler2Address != value)
        //        {
        //            coupler2Address = value;
        //            OnPropertyChanged(BCFUtility.getPropertyName(() => this.Coupler2Address));
        //        }
        //    }
        //}

        //private string coupler3Address;
        //public virtual string Coupler3Address
        //{
        //    get { return coupler3Address; }
        //    set
        //    {
        //        if (coupler3Address != value)
        //        {
        //            coupler3Address = value;
        //            OnPropertyChanged(BCFUtility.getPropertyName(() => this.Coupler3Address));
        //        }
        //    }
        //}

        private int chargerAlive;
        public virtual int ChargerAlive
        {
            get { return chargerAlive; }
            set
            {
                if (chargerAlive != value)
                {
                    chargerAlive = value;
                    OnPropertyChanged(BCFUtility.getPropertyName(() => this.ChargerAlive));
                }
            }
        }

        private int chargerStatusIndex;
        public virtual int ChargerStatusIndex
        {
            get { return chargerStatusIndex; }
            set
            {
                if (chargerStatusIndex != value)
                {
                    chargerStatusIndex = value;
                    OnPropertyChanged(BCFUtility.getPropertyName(() => this.ChargerStatusIndex));
                }
            }
        }

        private int chargerCurrentParameterIndex;
        public virtual int ChargerCurrentParameterIndex
        {
            get { return chargerCurrentParameterIndex; }
            set
            {
                if (chargerCurrentParameterIndex != value)
                {
                    chargerCurrentParameterIndex = value;
                    OnPropertyChanged(BCFUtility.getPropertyName(() => this.ChargerCurrentParameterIndex));
                }
            }
        }


        private int couplerChargeInfoIndex;
        public virtual int CouplerChargeInfoIndex
        {
            get { return couplerChargeInfoIndex; }
            set
            {
                if (couplerChargeInfoIndex != value)
                {
                    couplerChargeInfoIndex = value;
                    OnPropertyChanged(BCFUtility.getPropertyName(() => this.CouplerChargeInfoIndex));
                }
            }
        }

        private int pIOIndex;
        public virtual int PIOIndex
        {
            get { return pIOIndex; }
            set
            {
                if (pIOIndex != value)
                {
                    pIOIndex = value;
                    OnPropertyChanged(BCFUtility.getPropertyName(() => this.PIOIndex));
                }
            }
        }


        private int currentSupplyStatusBlock;
        public virtual int CurrentSupplyStatusBlock
        {
            get { return currentSupplyStatusBlock; }
            set
            {
                OnPropertyChanged(BCFUtility.getPropertyName(() => this.CurrentSupplyStatusBlock));
            }
        }
        public float inputVoltage;
        public float chargeVoltage;
        public float chargeCurrent;
        public float chargePower;
        public float couplerChargeVoltage;
        public float couplerChargeCurrent;
        public UInt16 couplerID;

        public bool chargerReserve;
        public bool chargerConstantVoltageOutput;
        public bool chargerConstantCurrentOutput;
        public bool chargerHighInputVoltageProtection;
        public bool chargerLowInputVoltageProtection;
        public bool chargerHighOutputVoltageProtection;
        public bool chargerHighOutputCurrentProtection;
        public bool chargerOverheatProtection;
        public string chargerRS485Status;

        public SCAppConstants.CouplerStatus_SOUTH_INNOLUX coupler1Status_SOUTH_INNOLUX;
        public SCAppConstants.CouplerStatus_SOUTH_INNOLUX coupler2Status_SOUTH_INNOLUX;
        public SCAppConstants.CouplerStatus_SOUTH_INNOLUX coupler3Status_SOUTH_INNOLUX;

        public SCAppConstants.CouplerStatus_NORTH_INNOLUX coupler1Status_NORTH_INNOLUX;
        public SCAppConstants.CouplerStatus_NORTH_INNOLUX coupler2Status_NORTH_INNOLUX;
        public SCAppConstants.CouplerStatus_NORTH_INNOLUX coupler3Status_NORTH_INNOLUX;

        public SCAppConstants.CouplerStatus coupler1Status;
        public SCAppConstants.CouplerStatus coupler2Status;
        public SCAppConstants.CouplerStatus coupler3Status;

        public bool coupler1SegmentDisableByAGVC = false;
        public bool coupler2SegmentDisableByAGVC = false;
        public bool coupler3SegmentDisableByAGVC = false;

        public const int couplerHPNotSafeTimes = 5;
        public int coupler1HPNotSafeCount = 0;
        public int coupler2HPNotSafeCount = 0;
        public int coupler3HPNotSafeCount = 0;

        public SCAppConstants.CouplerStatus GetCoupler1Status(string bcID)
        {
            switch (bcID)
            {
                case "SOUTH_INNOLUX":
                    if (coupler1Status_SOUTH_INNOLUX == SCAppConstants.CouplerStatus_SOUTH_INNOLUX.None) return SCAppConstants.CouplerStatus.Disable;
                    else if (coupler1Status_SOUTH_INNOLUX == SCAppConstants.CouplerStatus_SOUTH_INNOLUX.Manual) return SCAppConstants.CouplerStatus.Disable;
                    else if (coupler1Status_SOUTH_INNOLUX == SCAppConstants.CouplerStatus_SOUTH_INNOLUX.Auto) return SCAppConstants.CouplerStatus.Enable;
                    else if (coupler1Status_SOUTH_INNOLUX == SCAppConstants.CouplerStatus_SOUTH_INNOLUX.Charging) return SCAppConstants.CouplerStatus.Charging;
                    else if (coupler1Status_SOUTH_INNOLUX == SCAppConstants.CouplerStatus_SOUTH_INNOLUX.Error) return SCAppConstants.CouplerStatus.Error;
                    else return SCAppConstants.CouplerStatus.Error;
                case "NORTH_INNOLUX":
                    if (coupler1Status_NORTH_INNOLUX == SCAppConstants.CouplerStatus_NORTH_INNOLUX.None) return SCAppConstants.CouplerStatus.Disable;
                    else if (coupler1Status_NORTH_INNOLUX == SCAppConstants.CouplerStatus_NORTH_INNOLUX.Manual) return SCAppConstants.CouplerStatus.Disable;
                    else if (coupler1Status_NORTH_INNOLUX == SCAppConstants.CouplerStatus_NORTH_INNOLUX.Auto) return SCAppConstants.CouplerStatus.Enable;
                    else if (coupler1Status_NORTH_INNOLUX == SCAppConstants.CouplerStatus_NORTH_INNOLUX.Charging) return SCAppConstants.CouplerStatus.Charging;
                    else if (coupler1Status_NORTH_INNOLUX == SCAppConstants.CouplerStatus_NORTH_INNOLUX.Error) return SCAppConstants.CouplerStatus.Error;
                    else return SCAppConstants.CouplerStatus.Error;
                default:
                    return coupler1Status;
            }
        }
        public SCAppConstants.CouplerStatus GetCoupler2Status(string bcID)
        {
            switch (bcID)
            {
                case "SOUTH_INNOLUX":
                    if (coupler2Status_SOUTH_INNOLUX == SCAppConstants.CouplerStatus_SOUTH_INNOLUX.None) return SCAppConstants.CouplerStatus.Disable;
                    else if (coupler2Status_SOUTH_INNOLUX == SCAppConstants.CouplerStatus_SOUTH_INNOLUX.Manual) return SCAppConstants.CouplerStatus.Disable;
                    else if (coupler2Status_SOUTH_INNOLUX == SCAppConstants.CouplerStatus_SOUTH_INNOLUX.Auto) return SCAppConstants.CouplerStatus.Enable;
                    else if (coupler2Status_SOUTH_INNOLUX == SCAppConstants.CouplerStatus_SOUTH_INNOLUX.Charging) return SCAppConstants.CouplerStatus.Charging;
                    else if (coupler2Status_SOUTH_INNOLUX == SCAppConstants.CouplerStatus_SOUTH_INNOLUX.Error) return SCAppConstants.CouplerStatus.Error;
                    else return SCAppConstants.CouplerStatus.Error;
                case "NORTH_INNOLUX":
                    if (coupler2Status_NORTH_INNOLUX == SCAppConstants.CouplerStatus_NORTH_INNOLUX.None) return SCAppConstants.CouplerStatus.Disable;
                    else if (coupler2Status_NORTH_INNOLUX == SCAppConstants.CouplerStatus_NORTH_INNOLUX.Manual) return SCAppConstants.CouplerStatus.Disable;
                    else if (coupler2Status_NORTH_INNOLUX == SCAppConstants.CouplerStatus_NORTH_INNOLUX.Auto) return SCAppConstants.CouplerStatus.Enable;
                    else if (coupler2Status_NORTH_INNOLUX == SCAppConstants.CouplerStatus_NORTH_INNOLUX.Charging) return SCAppConstants.CouplerStatus.Charging;
                    else if (coupler2Status_NORTH_INNOLUX == SCAppConstants.CouplerStatus_NORTH_INNOLUX.Error) return SCAppConstants.CouplerStatus.Error;
                    else return SCAppConstants.CouplerStatus.Error;
                default:
                    return coupler2Status;
            }
        }
        public SCAppConstants.CouplerStatus GetCoupler3Status(string bcID)
        {
            switch (bcID)
            {
                case "SOUTH_INNOLUX":
                    if (coupler3Status_SOUTH_INNOLUX == SCAppConstants.CouplerStatus_SOUTH_INNOLUX.None) return SCAppConstants.CouplerStatus.Disable;
                    else if (coupler3Status_SOUTH_INNOLUX == SCAppConstants.CouplerStatus_SOUTH_INNOLUX.Manual) return SCAppConstants.CouplerStatus.Disable;
                    else if (coupler3Status_SOUTH_INNOLUX == SCAppConstants.CouplerStatus_SOUTH_INNOLUX.Auto) return SCAppConstants.CouplerStatus.Enable;
                    else if (coupler3Status_SOUTH_INNOLUX == SCAppConstants.CouplerStatus_SOUTH_INNOLUX.Charging) return SCAppConstants.CouplerStatus.Charging;
                    else if (coupler3Status_SOUTH_INNOLUX == SCAppConstants.CouplerStatus_SOUTH_INNOLUX.Error) return SCAppConstants.CouplerStatus.Error;
                    else return SCAppConstants.CouplerStatus.Error;
                case "NORTH_INNOLUX":
                    if (coupler3Status_NORTH_INNOLUX == SCAppConstants.CouplerStatus_NORTH_INNOLUX.None) return SCAppConstants.CouplerStatus.Disable;
                    else if (coupler3Status_NORTH_INNOLUX == SCAppConstants.CouplerStatus_NORTH_INNOLUX.Manual) return SCAppConstants.CouplerStatus.Disable;
                    else if (coupler3Status_NORTH_INNOLUX == SCAppConstants.CouplerStatus_NORTH_INNOLUX.Auto) return SCAppConstants.CouplerStatus.Enable;
                    else if (coupler3Status_NORTH_INNOLUX == SCAppConstants.CouplerStatus_NORTH_INNOLUX.Charging) return SCAppConstants.CouplerStatus.Charging;
                    else if (coupler3Status_NORTH_INNOLUX == SCAppConstants.CouplerStatus_NORTH_INNOLUX.Error) return SCAppConstants.CouplerStatus.Error;
                    else return SCAppConstants.CouplerStatus.Error;
                default:
                    return coupler3Status;
            }
        }

        private SCAppConstants.CouplerHPSafety coupler1hpsafety;
        public SCAppConstants.CouplerHPSafety coupler1HPSafety
        {
            get { return coupler1hpsafety; }
            set
            {
                if (coupler1hpsafety != value)
                {
                    coupler1hpsafety = value;
                    onCouplerHPSafetyChaged(value);
                }
            }
        }
        private SCAppConstants.CouplerHPSafety coupler2hpsafety;
        public SCAppConstants.CouplerHPSafety coupler2HPSafety
        {
            get { return coupler2hpsafety; }
            set
            {
                if (coupler2hpsafety != value)
                {
                    coupler2hpsafety = value;
                    //onCouplerHPSafetyChaged();
                }
            }
        }
        private SCAppConstants.CouplerHPSafety coupler3hpsafety;
        public SCAppConstants.CouplerHPSafety coupler3HPSafety
        {
            get { return coupler3hpsafety; }
            set
            {
                if (coupler3hpsafety != value)
                {
                    coupler3hpsafety = value;
                    //onCouplerHPSafetyChaged();
                }
            }
        }



        public float chargerOutputVoltage;
        public float chargerOutputCurrent;
        public float chargerOverVoltage;
        public float chargerOverCurrent;

        public int chargerCouplerID;
        public DateTime chargerChargingStartTime;
        public DateTime chargerChargingEndTime;
        public float chargerInputAH;
        public string chargerChargingResult;
        public List<PIOInfo> PIOInfos = new List<PIOInfo>();


        public class PIOInfo
        {
            public int CouplerID;
            public DateTime Timestamp;
            public string signal1;
            public string signal2;
        }
        public void onCouplerHPSafetyChaged(SCAppConstants.CouplerHPSafety couplerHPSafety)
        {
            CouplerHPSafetyChaged?.Invoke(this, couplerHPSafety);
        }

        #endregion charger

        public AUNIT()
        {
            eqptObjectCate = SCAppConstants.EQPT_OBJECT_CATE_UNIT;
        }

        public override void doShareMemoryInit(BCFAppConstants.RUN_LEVEL runLevel)
        {
            foreach (IValueDefMapAction action in valueDefMapActionDic.Values)
            {
                action.doShareMemoryInit(runLevel);
            }
        }


        /// <summary>
        /// Gets the eqpt object key.
        /// </summary>
        /// <returns>System.String.</returns>
        public virtual string getEqptObjectKey()
        {
            return BCFUtility.generateEQObjectKey(eqptObjectCate, UNIT_ID);
        }

        #region FireReport
        //public bool fireReport { get; set; }
        public bool fireDoorOpen { get; set; }
        public bool fireDoorCloseGrant { get; set; }
        public bool fireDoorCrossingSignal { get; set; }

        private List<string> reserve_section_id_List = new List<string>();//在防火門所在區域Segment的Section被Reserve時，會把ID加到這個List。

        Object reserve_section_id_dic_lock = new object();
        public void section_reserved(string section_id)
        {
            lock (reserve_section_id_dic_lock)
            {
                if (!reserve_section_id_List.Contains(section_id))
                {
                    reserve_section_id_List.Add(section_id);
                }
                else
                {

                }
                if (reserve_section_id_List.Count == 1)//剛好被預約一個Section時，發送CrossSignal。
                {
                    FireDoorDefaultValueDefMapAction mapAction = getMapActionByIdentityKey(nameof(FireDoorDefaultValueDefMapAction)) as FireDoorDefaultValueDefMapAction;
                    mapAction.sendFireDoorCrossSignal(true);
                }
            }
        }
        public void section_unreserved(string section_id)
        {
            lock (reserve_section_id_dic_lock)
            {
                if (reserve_section_id_List.Contains(section_id))
                {
                    reserve_section_id_List.Remove(section_id);
                }
                else
                {

                }
                if (reserve_section_id_List.Count == 0)//沒有被預約Section時，滅掉CrossSignal。
                {
                    FireDoorDefaultValueDefMapAction mapAction = getMapActionByIdentityKey(nameof(FireDoorDefaultValueDefMapAction)) as FireDoorDefaultValueDefMapAction;
                    mapAction.sendFireDoorCrossSignal(false);
                }
            }
        }
        public bool isOKtoBanRoute()
        {
            return reserve_section_id_List.Count == 0 && !fireDoorCrossingSignal;
        }
        #endregion FireReport
    }

}
