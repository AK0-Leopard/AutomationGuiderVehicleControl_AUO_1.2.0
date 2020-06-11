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
        #region charger 


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

        public SCAppConstants.CouplerStatus coupler1Status;
        public SCAppConstants.CouplerStatus coupler2Status;
        public SCAppConstants.CouplerStatus coupler3Status;


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
                if(reserve_section_id_List.Count == 1)//剛好被預約一個Section時，發送CrossSignal。
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
