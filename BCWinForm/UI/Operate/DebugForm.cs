using com.mirle.ibg3k0.bc.winform.App;
using com.mirle.ibg3k0.bc.winform.Common;
using com.mirle.ibg3k0.bcf.App;
using com.mirle.ibg3k0.bcf.Common;
using com.mirle.ibg3k0.sc;
using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Data;
using com.mirle.ibg3k0.sc.Data.SECS.AGVC;
using com.mirle.ibg3k0.sc.Data.ValueDefMapAction;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static com.mirle.ibg3k0.sc.App.SCAppConstants;

namespace com.mirle.ibg3k0.bc.winform.UI
{
    public partial class DebugForm : Form
    {

        BCMainForm mainForm;
        BCApplication bcApp;
        List<RadioButton> radioButtons = new List<RadioButton>();
#pragma warning disable CS0414 // 已指派欄位 'DebugForm.blocked_queues'，但從未使用過其值。
        List<BLOCKZONEQUEUE> blocked_queues = null;
#pragma warning restore CS0414 // 已指派欄位 'DebugForm.blocked_queues'，但從未使用過其值。
        protected AUOMCSDefaultMapAction mcsMapAction = null;
        APORTSTATION P11 = null;
        APORTSTATION P12 = null;
        APORTSTATION P13 = null;
        APORTSTATION P14 = null;
        List<AECDATAMAP> ECDATAMAPs = null;
        AEQPT mCharger = null;
        public DebugForm(BCMainForm _mainForm)
        {
            InitializeComponent();
            mainForm = _mainForm;
            bcApp = mainForm.BCApp;

            cb_StartGenAntoCmd.Checked = DebugParameter.CanAutoRandomGeneratesCommand;
            cb_FroceBlockPass.Checked = DebugParameter.isForcedPassBlockControl;
            cb_FroceBlockPass.Checked = DebugParameter.isForcedRejectBlockControl;
            numer_num_of_avoid_seg.Value = DebugParameter.NumberOfAvoidanceSegment;
            checkBox_host_offline.Checked = DebugParameter.UseHostOffline;
            cb_advanceDriveAway.Checked = DebugParameter.AdvanceDriveAway;
            cb_passCouplerHPSafetySingnal.Checked = DebugParameter.isPassCouplerHPSafetySignal;

            List<string> lstVh = new List<string>();
            lstVh.Add(string.Empty);
            lstVh.AddRange(bcApp.SCApplication.VehicleBLL.loadAllVehicle().Select(vh => vh.VEHICLE_ID).ToList());
            string[] allVh = lstVh.ToArray();
            BCUtility.setComboboxDataSource(cmb_tcpipctr_Vehicle, allVh);
            BCUtility.setComboboxDataSource(cmb_mcsReportTestVHID, allVh);

            List<AADDRESS> allAddress_obj = bcApp.SCApplication.MapBLL.loadAllAddress();
            string[] allAdr_ID = allAddress_obj.Select(adr => adr.ADR_ID).ToArray();
            BCUtility.setComboboxDataSource(cmb_teach_from_adr, allAdr_ID);
            BCUtility.setComboboxDataSource(cmb_teach_to_adr, allAdr_ID.ToArray());

            List<ASECTION> sections = bcApp.SCApplication.SectionBLL.cache.GetSections();
            string[] allSec_ID = sections.Select(sec => sec.SEC_ID).ToArray();
            BCUtility.setComboboxDataSource(cmb_reserve_section, allSec_ID.ToArray());
            BCUtility.setComboboxDataSource(cmb_reserve_section1, allSec_ID.ToArray());
            BCUtility.setComboboxDataSource(cmb_reserve_section2, allSec_ID.ToArray());

            cb_OperMode.DataSource = Enum.GetValues(typeof(sc.ProtocolFormat.OHTMessage.OperatingVHMode));
            cb_PwrMode.DataSource = Enum.GetValues(typeof(sc.ProtocolFormat.OHTMessage.OperatingPowerMode));
            cmb_pauseEvent.DataSource = Enum.GetValues(typeof(sc.ProtocolFormat.OHTMessage.PauseEvent));
            cmb_pauseType.DataSource = Enum.GetValues(typeof(OHxCPauseType));
            cb_Abort_Type.DataSource = Enum.GetValues(typeof(sc.ProtocolFormat.OHTMessage.CMDCancelType));


            mcsMapAction = SCApplication.getInstance().getEQObjCacheManager().getLine().getMapActionByIdentityKey(typeof(AUOMCSDefaultMapAction).Name) as AUOMCSDefaultMapAction;

            cb_Cache_data_Name.Items.Add("");
            cb_Cache_data_Name.Items.Add("ASECTION");
            dgv_cache_object_data.AutoGenerateColumns = false;
            dgv_cache_object_data_portstation.AutoGenerateColumns = false;
            P11 = bcApp.SCApplication.getEQObjCacheManager().getPortStation("AASTK250P01");
            P12 = bcApp.SCApplication.getEQObjCacheManager().getPortStation("AASTK250P02");
            P13 = bcApp.SCApplication.getEQObjCacheManager().getPortStation("CASTK010P01");
            P14 = bcApp.SCApplication.getEQObjCacheManager().getPortStation("CASTK010P02");

            if (P11?.PORT_SERVICE_STATUS == sc.ProtocolFormat.OHTMessage.PortStationServiceStatus.InService)
            {
                comboBox_port11.SelectedIndex = 0;
            }
            else
            {
                comboBox_port11.SelectedIndex = 1;
            }
            if (P12?.PORT_SERVICE_STATUS == sc.ProtocolFormat.OHTMessage.PortStationServiceStatus.InService)
            {
                comboBox_port12.SelectedIndex = 0;
            }
            else
            {
                comboBox_port12.SelectedIndex = 1;
            }
            if (P13?.PORT_SERVICE_STATUS == sc.ProtocolFormat.OHTMessage.PortStationServiceStatus.InService)
            {
                comboBox_port13.SelectedIndex = 0;
            }
            else
            {
                comboBox_port13.SelectedIndex = 1;
            }
            if (P14?.PORT_SERVICE_STATUS == sc.ProtocolFormat.OHTMessage.PortStationServiceStatus.InService)
            {
                comboBox_port14.SelectedIndex = 0;
            }
            else
            {
                comboBox_port14.SelectedIndex = 1;
            }

            num_BatteryLowBoundaryValue.Value = AVEHICLE.BATTERYLEVELVALUE_LOW;
            //num_BatteryMiddleBoundaryValue.Value = AVEHICLE.BATTERYLEVELVALUE_MIDDLE;
            num_BatteryHighBoundaryValue.Value = AVEHICLE.BATTERYLEVELVALUE_HIGH;


            List<APORTSTATION> all_port_station = bcApp.SCApplication.PortStationBLL.OperateCatch.getAllPortStation();
            dgv_cache_object_data_portstation.DataSource = all_port_station;

            tabControl1.TabPages.RemoveAt(1);

            ECDATAMAPs = bcApp.SCApplication.LineBLL.loadECDataList(bcApp.SCApplication.getEQObjCacheManager().getLine().LINE_ID);
            foreach (AECDATAMAP item in ECDATAMAPs)
            {
                if (BCFUtility.isMatche(item.ECID, SCAppConstants.ECID_EQPNAME))
                {
                    textBox_eqpname.Text = item.ECV;
                }
                else if (BCFUtility.isMatche(item.ECID, SCAppConstants.ECID_ESTABLISH_COMMUNICATION_TIMEOUT))
                {
                    textBox_EstablishCommunicationTimeout.Text = item.ECV;
                }
                else if (BCFUtility.isMatche(item.ECID, SCAppConstants.ECID_INITIAL_COMMUNICATION_STATE))
                {
                    textBox_InitialCommunicationState.Text = item.ECV;
                }
                else if (BCFUtility.isMatche(item.ECID, SCAppConstants.ECID_INITIAL_CONTROL_STATE))
                {
                    textBox_InitialControlState.Text = item.ECV;
                }
                else if (BCFUtility.isMatche(item.ECID, SCAppConstants.ECID_SOFT_REVISION))
                {
                    textBox_SoftRevision.Text = item.ECV;
                }
                else if (BCFUtility.isMatche(item.ECID, SCAppConstants.ECID_TIME_FORMAT))
                {
                    textBox_TIMEFORMAT.Text = item.ECV;
                }
                else if (BCFUtility.isMatche(item.ECID, SCAppConstants.ECID_MDLN))
                {
                    textBox_ModelName.Text = item.ECV;
                }
                else
                {
                    continue;
                }
            }
            mCharger = bcApp.SCApplication.getEQObjCacheManager().getEquipmentByEQPTID("MCharger");
            if (mCharger != null)
            {
                foreach (AUNIT unit in mCharger.UnitList)
                {
                    cb_Charger.Items.Add(unit.UNIT_ID);
                    cb_ChargerID.Items.Add(unit.UNIT_ID);
                }
            }
            for (int i = 0; i < 15; i++)
            {
                cb_PIOCoupler.Items.Add(i + 1);
            }
            initChargerValue();
            registerEvent();
        }

        //private void initHandlerEvent()
        //{
        //    try
        //    {

        //    }
        //    catch (Exception ex)
        //    {
        //        logger.Error(ex, "Exception");
        //    }
        //}


        private void agvcAliveChange(AEQPT eqpt)
        {


            Adapter.Invoke(new SendOrPostCallback((o1) =>
            {
                tb_agvcAlive.Text = eqpt.AGVCAliveIndex.ToString();
            }), null);

        }

        private void MChargerAbnormalIndexChange(AEQPT eqpt)
        {
            Adapter.Invoke(new SendOrPostCallback((o1) =>
            {
                tb_ErrorReportIndex.Text = eqpt.AbnormalReportIndex.ToString();
                tb_ErrorReportCode1.Text = eqpt.abnormalReportCode01.ToString();
                tb_ErrorReportCode2.Text = eqpt.abnormalReportCode02.ToString();
                tb_ErrorReportCode3.Text = eqpt.abnormalReportCode03.ToString();
                tb_ErrorReportCode4.Text = eqpt.abnormalReportCode04.ToString();
                tb_ErrorReportCode5.Text = eqpt.abnormalReportCode05.ToString();
            }), null);
        }

        private void ChargerAliveChange(AUNIT unit)
        {
            Adapter.Invoke(new SendOrPostCallback((o1) =>
            {
                if (unit.UNIT_ID != cb_ChargerID.Text) return;
                tb_ChargerAlive.Text = unit.ChargerAlive.ToString();
            }), null);
        }


        private void CurrentSupplyStatusBlockChange(AUNIT unit)
        {
            Adapter.Invoke(new SendOrPostCallback((o1) =>
            {
                if (unit.UNIT_ID != cb_ChargerID.Text) return;
                tb_inputVoltage.Text = unit.inputVoltage.ToString();
                tb_chargeVoltage.Text = unit.chargeVoltage.ToString();
                textBox_chargeCurrent.Text = unit.chargeCurrent.ToString();
                textBox_chargePower.Text = unit.chargePower.ToString();
                tb_couplerChargeVoltage.Text = unit.couplerChargeVoltage.ToString();
                tb_couplerChargeCurrent.Text = unit.couplerChargeCurrent.ToString();
                textBox_couplerID.Text = unit.couplerID.ToString();
            }), null);
        }

        private void ChargerStatusIndexChange(AUNIT unit)
        {
            Adapter.Invoke(new SendOrPostCallback((o1) =>
            {
                if (unit.UNIT_ID != cb_ChargerID.Text) return;
                tb_ChargerStatusReportIndex.Text = unit.ChargerStatusIndex.ToString();
                tb_Reserve.Text = unit.chargerReserve.ToString();
                tb_ConstantVoOutput.Text = unit.chargerConstantVoltageOutput.ToString();
                tb_ContantCurrentOutput.Text = unit.chargerConstantCurrentOutput.ToString();
                tb_HighInputVoProtect.Text = unit.chargerHighInputVoltageProtection.ToString();
                tb_LowInputVoProtect.Text = unit.chargerLowInputVoltageProtection.ToString();
                tb_HighOutputVoProtect.Text = unit.chargerHighOutputVoltageProtection.ToString();
                tb_HighOutputCurrentProtect.Text = unit.chargerHighOutputCurrentProtection.ToString();
                tb_OverheatProtect.Text = unit.chargerOverheatProtection.ToString();
                tb_RS485Status.Text = unit.chargerRS485Status.ToString();
                //tb_CouplerStatus1.Text = unit.coupler1Status.ToString();
                //tb_CouplerStatus2.Text = unit.coupler2Status.ToString();
                //tb_CouplerStatus3.Text = unit.coupler3Status.ToString();
                setCouplerStatus(unit);
                tb_Coupler1HPSafety.Text = unit.coupler1HPSafety.ToString();
                tb_Coupler2HPSafety.Text = unit.coupler2HPSafety.ToString();
                tb_Coupler3HPSafety.Text = unit.coupler3HPSafety.ToString();

            }), null);
        }

        private void ChargerCurrentParameterindexChange(AUNIT unit)
        {
            Adapter.Invoke(new SendOrPostCallback((o1) =>
            {
                if (unit.UNIT_ID != cb_ChargerID.Text) return;
                tb_ChargerCurrentParameterSettingIndex.Text = unit.ChargerCurrentParameterIndex.ToString();
                tb_OutputVo.Text = unit.chargerOutputVoltage.ToString();
                tb_OutputCurrent.Text = unit.chargerOutputCurrent.ToString();
                tb_OverloadVo.Text = unit.chargerOverVoltage.ToString();
                tb_OverloadCurrent.Text = unit.chargerOverCurrent.ToString();
            }), null);
        }


        private void CouplerChargeInfoIndexChange(AUNIT unit)
        {
            Adapter.Invoke(new SendOrPostCallback((o1) =>
            {
                if (unit.UNIT_ID != cb_ChargerID.Text) return;
                tb_CouplerChargeInfoReportIndex.Text = unit.CouplerChargeInfoIndex.ToString();
                tb_CouplerID.Text = unit.chargerCouplerID.ToString();
                tb_ChargeStartTime.Text = unit.chargerChargingStartTime.ToString("yyyy/MM/dd hh:mm:ss");
                tb_ChargeEndTime.Text = unit.chargerChargingEndTime.ToString("yyyy/MM/dd hh:mm:ss");
                tb_InputAh.Text = unit.chargerInputAH.ToString();
                tb_ChargeResult.Text = unit.chargerChargingResult.ToString();
            }), null);
        }

        private void PIOIndexChange(AUNIT unit)
        {
            Adapter.Invoke(new SendOrPostCallback((o1) =>
            {
                tb_PIOIndex.Text = unit.PIOIndex.ToString();
                int x;
                bool result = int.TryParse(cb_PIOCoupler.Text, out x);
                if (result)
                {
                    if (unit.PIOInfos.Count >= x)
                    {
                        tb_PIOCouplerID.Text = unit.PIOInfos[x - 1].CouplerID.ToString();
                        tb_PIOHandshakeTime.Text = unit.PIOInfos[x - 1].Timestamp.ToString("yyyy/MM/dd hh:mm:ss");
                        tb_PIOSignal1.Text = unit.PIOInfos[x - 1].signal1.Replace(",", "").Replace("True", "1").Replace("False", "0");
                        tb_PIOSignal2.Text = unit.PIOInfos[x - 1].signal2.Replace(",", "").Replace("True", "1").Replace("False", "0");
                    }
                }
            }), null);
        }

        private void cb_ChargerID_SelectedIndexChanged(object sender, EventArgs e)
        {
            AUNIT unit = bcApp.SCApplication.getEQObjCacheManager().getUnitByUnitID(cb_ChargerID.Text);
            if (unit != null)
            {
                tb_ChargerAlive.Text = unit.ChargerAlive.ToString();
                tb_inputVoltage.Text = unit.inputVoltage.ToString();
                tb_chargeVoltage.Text = unit.chargeVoltage.ToString();
                textBox_chargeCurrent.Text = unit.chargeCurrent.ToString();
                textBox_chargePower.Text = unit.chargePower.ToString();
                tb_couplerChargeVoltage.Text = unit.couplerChargeVoltage.ToString();
                tb_couplerChargeCurrent.Text = unit.couplerChargeCurrent.ToString();
                textBox_couplerID.Text = unit.couplerID.ToString();

                tb_ChargerStatusReportIndex.Text = unit.ChargerStatusIndex.ToString();
                tb_Reserve.Text = unit.chargerReserve.ToString();
                tb_ConstantVoOutput.Text = unit.chargerConstantVoltageOutput.ToString();
                tb_ContantCurrentOutput.Text = unit.chargerConstantCurrentOutput.ToString();
                tb_HighInputVoProtect.Text = unit.chargerHighInputVoltageProtection.ToString();
                tb_LowInputVoProtect.Text = unit.chargerLowInputVoltageProtection.ToString();
                tb_HighOutputVoProtect.Text = unit.chargerHighOutputVoltageProtection.ToString();
                tb_HighOutputCurrentProtect.Text = unit.chargerHighOutputCurrentProtection.ToString();
                tb_OverheatProtect.Text = unit.chargerOverheatProtection.ToString();
                tb_RS485Status.Text = unit.chargerRS485Status.ToString();

                setCouplerStatus(unit);

                tb_Coupler1HPSafety.Text = unit.coupler1HPSafety.ToString();
                tb_Coupler2HPSafety.Text = unit.coupler2HPSafety.ToString();
                tb_Coupler3HPSafety.Text = unit.coupler3HPSafety.ToString();

                tb_ChargerCurrentParameterSettingIndex.Text = unit.ChargerCurrentParameterIndex.ToString();
                tb_OutputVo.Text = unit.chargerOutputVoltage.ToString();
                tb_OutputCurrent.Text = unit.chargerOutputCurrent.ToString();
                tb_OverloadVo.Text = unit.chargerOverVoltage.ToString();
                tb_OverloadCurrent.Text = unit.chargerOverCurrent.ToString();

                tb_CouplerChargeInfoReportIndex.Text = unit.CouplerChargeInfoIndex.ToString();
                tb_CouplerID.Text = unit.chargerCouplerID.ToString();
                tb_ChargeStartTime.Text = unit.chargerChargingStartTime.ToString("yyyy/MM/dd hh:mm:ss");
                tb_ChargeEndTime.Text = unit.chargerChargingEndTime.ToString("yyyy/MM/dd hh:mm:ss");
                tb_InputAh.Text = unit.chargerInputAH.ToString();
                tb_ChargeResult.Text = unit.chargerChargingResult;

                tb_PIOIndex.Text = unit.PIOIndex.ToString();
                int x;
                bool result = int.TryParse(cb_PIOCoupler.Text, out x);
                if (result)
                {
                    if (unit.PIOInfos.Count >= x)
                    {
                        tb_PIOCouplerID.Text = unit.PIOInfos[x - 1].CouplerID.ToString();
                        tb_PIOHandshakeTime.Text = unit.PIOInfos[x - 1].Timestamp.ToString("yyyy/MM/dd hh:mm:ss");
                        tb_PIOSignal1.Text = unit.PIOInfos[x - 1].signal1.Replace(",", "").Replace("True", "1").Replace("False", "0");
                        tb_PIOSignal2.Text = unit.PIOInfos[x - 1].signal2.Replace(",", "").Replace("True", "1").Replace("False", "0");
                    }
                }
            }

        }

        private void setCouplerStatus(AUNIT unit)
        {
            switch (bcApp.SCApplication.BC_ID)
            {
                case "SOUTH_INNOLUX":
                    tb_CouplerStatus1.Text = unit.coupler1Status_SOUTH_INNOLUX.ToString();
                    tb_CouplerStatus2.Text = unit.coupler2Status_SOUTH_INNOLUX.ToString();
                    tb_CouplerStatus3.Text = unit.coupler3Status_SOUTH_INNOLUX.ToString();
                    break;
                default:
                    tb_CouplerStatus1.Text = unit.coupler1Status.ToString();
                    tb_CouplerStatus2.Text = unit.coupler2Status.ToString();
                    tb_CouplerStatus3.Text = unit.coupler3Status.ToString();
                    break;
            }
        }

        private void cb_PIOCoupler_SelectedIndexChanged(object sender, EventArgs e)
        {
            AUNIT unit = bcApp.SCApplication.getEQObjCacheManager().getUnitByUnitID(cb_ChargerID.Text);
            if (unit != null)
            {
                tb_PIOIndex.Text = unit.PIOIndex.ToString();
                int x;
                bool result = int.TryParse(cb_PIOCoupler.Text, out x);
                if (result)
                {
                    if (unit.PIOInfos.Count >= x)
                    {
                        tb_PIOCouplerID.Text = unit.PIOInfos[x - 1].CouplerID.ToString();
                        tb_PIOHandshakeTime.Text = unit.PIOInfos[x - 1].Timestamp.ToString("yyyy/MM/dd hh:mm:ss");
                        tb_PIOSignal1.Text = unit.PIOInfos[x - 1].signal1.Replace(",", "").Replace("True", "1").Replace("False", "0");
                        tb_PIOSignal2.Text = unit.PIOInfos[x - 1].signal2.Replace(",", "").Replace("True", "1").Replace("False", "0");
                    }
                }
            }
        }


        private void initChargerValue()
        {
            if (mCharger != null)
            {
                agvcAliveChange(mCharger);
                MChargerAbnormalIndexChange(mCharger);
                foreach (AUNIT unit in mCharger.UnitList)
                {
                    ChargerAliveChange(unit);
                    CurrentSupplyStatusBlockChange(unit);
                    ChargerStatusIndexChange(unit);
                    ChargerCurrentParameterindexChange(unit);
                    CouplerChargeInfoIndexChange(unit);
                    PIOIndexChange(unit);
                }
            }
        }
        private void registerEvent()
        {
            string Handler = this.Name;
            if (mCharger != null)
            {
                mCharger.addEventHandler(Handler, BCFUtility.getPropertyName(() => mCharger.AGVCAliveIndex),
                    (s1, e1) => agvcAliveChange(mCharger));

                mCharger.addEventHandler(Handler, BCFUtility.getPropertyName(() => mCharger.AbnormalReportIndex),
                    (s1, e1) => MChargerAbnormalIndexChange(mCharger));

                foreach (AUNIT unit in mCharger.UnitList)
                {
                    unit.addEventHandler(Handler, BCFUtility.getPropertyName(() => unit.ChargerAlive),
                        (s1, e1) => ChargerAliveChange(unit));
                    unit.addEventHandler(Handler, BCFUtility.getPropertyName(() => unit.CurrentSupplyStatusBlock),
                        (s1, e1) => CurrentSupplyStatusBlockChange(unit));
                    unit.addEventHandler(Handler, BCFUtility.getPropertyName(() => unit.ChargerStatusIndex),
                        (s1, e1) => ChargerStatusIndexChange(unit));
                    unit.addEventHandler(Handler, BCFUtility.getPropertyName(() => unit.ChargerCurrentParameterIndex),
                        (s1, e1) => ChargerCurrentParameterindexChange(unit));
                    unit.addEventHandler(Handler, BCFUtility.getPropertyName(() => unit.CouplerChargeInfoIndex),
                        (s1, e1) => CouplerChargeInfoIndexChange(unit));
                    unit.addEventHandler(Handler, BCFUtility.getPropertyName(() => unit.PIOIndex),
                        (s1, e1) => PIOIndexChange(unit));

                }
            }
        }
        private void unregisterEvent()
        {
            if (mCharger != null)
            {
                mCharger.RemoveAllEvents();

                foreach (AUNIT unit in mCharger.UnitList)
                {
                    unit.RemoveAllEvents();
                }
            }
        }

        private void DebugForm_Load(object sender, EventArgs e)
        {
            DebugParameter.IsDebugMode = true;
            timer1.Start();
        }

        private void DebugForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            TrunOffAllVhPLCControl();
            DebugParameter.IsDebugMode = false;
            DebugParameter.IsCycleRun = false;
            timer1.Stop();
            unregisterEvent();
            mainForm.removeForm(typeof(DebugForm).Name);
        }



        private void cb_FroceBlockPass_CheckedChanged(object sender, EventArgs e)
        {
            DebugParameter.isForcedPassBlockControl = cb_FroceBlockPass.Checked;
        }


        AVEHICLE noticeCar = null;
        string vh_id = null;
        private void cmb_Vehicle_SelectedIndexChanged(object sender, EventArgs e)
        {
            vh_id = cmb_tcpipctr_Vehicle.Text.Trim();

            noticeCar = bcApp.SCApplication.getEQObjCacheManager().getVehicletByVHID(vh_id);
            lbl_id_37_cmdID_value.Text = noticeCar?.OHTC_CMD;
            lbl_install_status.Text = noticeCar?.IS_INSTALLED.ToString();
        }

        private void uctl_Btn1_Click(object sender, EventArgs e)
        {
            asyExecuteAction(bcApp.SCApplication.VehicleService.HostBasicVersionReport);
            //asyExecuteAction(noticeCar.sned_S1);
        }
        private void uctl_SendFun11_Click(object sender, EventArgs e)
        {
            asyExecuteAction(bcApp.SCApplication.VehicleService.BasicInfoReport);
            //asyExecuteAction(noticeCar.sned_S11);
        }
        private void uctl_SendFun13_Click(object sender, EventArgs e)
        {
            asyExecuteAction(bcApp.SCApplication.VehicleService.TavellingDataReport);
            //asyExecuteAction(noticeCar.sned_S13);
        }
        private void uctl_SendFun15_Click(object sender, EventArgs e)
        {
        }
        private void uctl_SendFun17_Click(object sender, EventArgs e)
        {
            asyExecuteAction(bcApp.SCApplication.VehicleService.AddressDataReport);
            //asyExecuteAction(noticeCar.sned_S17);
        }

        private void uctl_SendFun19_Click(object sender, EventArgs e)
        {
            asyExecuteAction(bcApp.SCApplication.VehicleService.ScaleDataReport);
            //asyExecuteAction(noticeCar.sned_S19);
        }

        private void uctl_SendFun21_Click(object sender, EventArgs e)
        {
            asyExecuteAction(bcApp.SCApplication.VehicleService.ControlDataReport);
            //asyExecuteAction(noticeCar.sned_S21);
        }

        private void uctl_SendFun23_Click(object sender, EventArgs e)
        {
            asyExecuteAction(bcApp.SCApplication.VehicleService.GuideDataReport);
            //asyExecuteAction(noticeCar.sned_S23);
        }

        private void asyExecuteAction(Func<string, bool> act)
        {
            Task.Run(() =>
            {
                act(vh_id);
            });
        }

        private void uctl_SendAllFun_Click(object sender, EventArgs e)
        {
            asyExecuteAction(bcApp.SCApplication.VehicleService.doDataSysc);
            //asyExecuteAction(noticeCar.sned_ALL);
        }

        private void uctl_Send_Fun_71_Click(object sender, EventArgs e)
        {
            string from_adr = cmb_teach_from_adr.Text;
            string to_adr = cmb_teach_to_adr.Text;
            Task.Run(() =>
            {
                bcApp.SCApplication.VehicleService.TeachingRequest(vh_id, from_adr, to_adr);
                //noticeCar.send_Str71(from_adr, to_adr);
            });
        }

        private void button1_Click(object sender, EventArgs e)
        {
            asyExecuteAction(bcApp.SCApplication.VehicleService.IndividualUploadRequest);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            asyExecuteAction(bcApp.SCApplication.VehicleService.IndividualChangeRequest);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            sc.ProtocolFormat.OHTMessage.OperatingVHMode operatiogMode;
            Enum.TryParse(cb_OperMode.SelectedValue.ToString(), out operatiogMode);

            Task.Run(() =>
            {
                bcApp.SCApplication.VehicleService.ModeChangeRequest(vh_id, operatiogMode);
            });
        }

        private void button4_Click(object sender, EventArgs e)
        {
            sc.ProtocolFormat.OHTMessage.OperatingPowerMode operatiogPowerMode;
            Enum.TryParse(cb_PwrMode.SelectedValue.ToString(), out operatiogPowerMode);

            Task.Run(() =>
            {
                bcApp.SCApplication.VehicleService.PowerOperatorRequest(vh_id, operatiogPowerMode);
            });
        }

        private void button5_Click(object sender, EventArgs e)
        {
            asyExecuteAction(bcApp.SCApplication.VehicleService.AlarmResetRequest);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                bool isSuccess = bcApp.SCApplication.CMDBLL.forceUpdataCmdStatus2FnishByVhID(vh_id);
                if (isSuccess)
                {
                    var vh = bcApp.SCApplication.VehicleBLL.getVehicleByID(vh_id);
                    vh.VehicleUnassign();
                    vh.NotifyVhExcuteCMDStatusChange();
                }

            });
        }

        private void button7_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                //bcApp.SCApplication.VehicleService.forceResetVHStatus(vh_id);
                bcApp.SCApplication.VehicleService.VehicleStatusRequest(vh_id, true);
            });
        }

        private void cb_StartGenAntoCmd_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            DebugParameter.CanAutoRandomGeneratesCommand = cb.Checked;

            if (!cb.Checked)
            {
                Task.Run(() =>
                {
                    var mcs_cmds = bcApp.SCApplication.CMDBLL.loadMCS_Command_Queue();
                    foreach (var cmd in mcs_cmds)
                        bcApp.SCApplication.CMDBLL.updateCMD_MCS_TranStatus2Complete(cmd.CMD_ID, E_TRAN_STATUS.Canceled);
                });
            }
        }

        private void btn_forceReleaseALLBlock_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                bcApp.SCApplication.VehicleService.forceReleaseBlockControl();
            });
        }

        private void btn_ForceReleaseBlock_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                bcApp.SCApplication.VehicleService.forceReleaseBlockControl(vh_id);
            });
        }

        private void btn_pause_Click(object sender, EventArgs e)
        {
            sc.ProtocolFormat.OHTMessage.PauseEvent pauseEvent;
            OHxCPauseType pauseType;
            Enum.TryParse(cmb_pauseEvent.SelectedValue.ToString(), out pauseEvent);
            Enum.TryParse(cmb_pauseType.SelectedValue.ToString(), out pauseType);
            Task.Run(() =>
            {
                bcApp.SCApplication.VehicleService.PauseRequest(vh_id, pauseEvent, pauseType);
            });

        }




        private void label17_Click(object sender, EventArgs e)
        {

        }



        private void radio_bitX_Click(object sender, EventArgs e)
        {
            (sender as RadioButton).Checked = !(sender as RadioButton).Checked;
        }



        private void TrunOffAllVhPLCControl()
        {
            var vhs = bcApp.SCApplication.getEQObjCacheManager().getAllVehicle();

            foreach (var vh in vhs)
            {
                vh.PLC_Control_TrunOff();
            }
        }

        private void cb_FroceBlockReject_CheckedChanged(object sender, EventArgs e)
        {
            DebugParameter.isForcedRejectBlockControl = cb_FroceBlockReject.Checked;
        }

        private void button8_Click(object sender, EventArgs e)
        {
        }

        private void button9_Click(object sender, EventArgs e)
        {
            sc.ProtocolFormat.OHTMessage.CMDCancelType type;
            Enum.TryParse(cb_Abort_Type.SelectedValue.ToString(), out type);

            Task.Run(() =>
            {
                noticeCar.sned_Str37(noticeCar.OHTC_CMD, type);
            });

        }

        private void button10_Click(object sender, EventArgs e)
        {

        }

        private void button11_Click(object sender, EventArgs e)
        {
        }

        private void btn_blocked_sec_refresh_Click(object sender, EventArgs e)
        {
        }


        private void btn_portInServeice_Click(object sender, EventArgs e)
        {
            string port_id = cb_PortID.Text;
            Task.Run(() =>
            {
            });
        }

        private void btn_portOutOfServeice_Click(object sender, EventArgs e)
        {
            string port_id = cb_PortID.Text;
            Task.Run(() =>
            {
            });

        }

        private void ck_test_carrierinterface_CheckedChanged(object sender, EventArgs e)
        {
            DebugParameter.isTestCarrierInterfaceError = ck_test_carrierinterface_error.Checked;
        }

        private void button8_Click_1(object sender, EventArgs e)
        {
        }



        private void ck_autoTech_Click(object sender, EventArgs e)
        {

        }

        private void btn_reset_teach_result_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                List<ASECTION> sections = bcApp.SCApplication.MapBLL.loadAllSection();
                foreach (var sec in sections)
                {
                    if (bcApp.SCApplication.MapBLL.resetSecTechingTime(sec.SEC_ID))
                    {
                        sec.LAST_TECH_TIME = null;
                    }

                }
            });
        }

        private void btn_cmd_override_test_Click(object sender, EventArgs e)
        {
            string vh_id = cmb_tcpipctr_Vehicle.Text;
            Task.Run(() =>
            {
                bcApp.SCApplication.VehicleService.VhicleChangeThePath(vh_id);
            });
        }

        private void uctl_SendFun2_Click(object sender, EventArgs e)
        {
            //asyExecuteAction(bcApp.SCApplication.VehicleService.BasicInfoVersionReport);
            asyExecuteAction(bcApp.SCApplication.VehicleService.HostBasicVersionReport);


        }

        private void cb_Cache_data_Name_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selected_name = (sender as ComboBox).SelectedItem as string;
            if (selected_name == "ASECTION")
            {
                RefreshSectionInfo();
            }
        }

        private void RefreshSectionInfo()
        {
            List<ASECTION> sections = bcApp.SCApplication.SectionBLL.cache.GetSections();
            dgv_cache_object_data.DataSource = sections;
        }

        private void dgv_cache_object_data_EditModeChanged(object sender, EventArgs e)
        {

        }


        #region MTL Test


        private void btn_mtl_dateTimeSync_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                var mtl_mapaction = bcApp.SCApplication.getEQObjCacheManager().getEquipmentByEQPTID("MCharger").
                    getMapActionByIdentityKey(nameof(com.mirle.ibg3k0.sc.Data.ValueDefMapAction.ChargerValueDefMapAction)) as
                    com.mirle.ibg3k0.sc.Data.ValueDefMapAction.ChargerValueDefMapAction;
                DateTime dateTime = DateTime.Now;
                //DateTime dateTime = DateTime.Now.AddMilliseconds(int.Parse(numericUpDownForDateTimeAddMilliseconds.Value.ToString()));
                mtl_mapaction.DateTimeSyncCommand(dateTime);
            }
            );
        }
        #endregion MTL Test

        private void btn_mtl_message_download_Click(object sender, EventArgs e)
        {
            bool is_enable = cb_all_coupler_enable.Checked;
            Task.Run(() =>
            {
                var mtl_mapaction = bcApp.SCApplication.getEQObjCacheManager().getEquipmentByEQPTID("MCharger").
                    getMapActionByIdentityKey(nameof(com.mirle.ibg3k0.sc.Data.ValueDefMapAction.ChargerValueDefMapAction)) as
                    com.mirle.ibg3k0.sc.Data.ValueDefMapAction.ChargerValueDefMapAction;
                mtl_mapaction.AGVCToChargerAllCouplerEnable(is_enable);
            }
            );
        }

        private void btn_mtl_vh_realtime_info_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                var mtl_mapaction = bcApp.SCApplication.getEQObjCacheManager().getEquipmentByEQPTID("MCharger").
                    getMapActionByIdentityKey(nameof(com.mirle.ibg3k0.sc.Data.ValueDefMapAction.ChargerValueDefMapAction)) as
                    com.mirle.ibg3k0.sc.Data.ValueDefMapAction.ChargerValueDefMapAction;
                mtl_mapaction.AGVCToMChargerAllChargerChargingFinish();
            }
            );
        }

        private void btn_mtl_car_out_notify_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                var mtl_mapaction = bcApp.SCApplication.getEQObjCacheManager().getEquipmentByEQPTID("MCharger").
                    getMapActionByIdentityKey(nameof(com.mirle.ibg3k0.sc.Data.ValueDefMapAction.ChargerValueDefMapAction)) as
                    com.mirle.ibg3k0.sc.Data.ValueDefMapAction.ChargerValueDefMapAction;
                mtl_mapaction.AGVCToMChargerAlarmReset();
            }
            );
        }

        private void btn_func71_Click(object sender, EventArgs e)
        {
            string[] guide_sections = txt_func51_guideSec.Text.Split(',').Select(sec => sec.PadLeft(4, '0')).ToArray();
            string[] guide_addresses = txt_func51_guideAdr.Text.Split(',').Select(adr => adr.PadLeft(4, '0')).ToArray();
            Task.Run(() =>
            {
                bcApp.SCApplication.VehicleService.AvoidRequest(vh_id, "", guide_sections, guide_addresses);
            });
        }

        private void btn_coulper_enable_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(cb_Charger.Text))
            {
                MessageBox.Show("Please select charger.");
                return;
            }
            if (string.IsNullOrWhiteSpace(txt_charger_coupler_id.Text))
            {
                MessageBox.Show("Please enter coupler ID.");
                return;
            }

            string charger_id = cb_Charger.Text.Trim();
            string coupler_id = txt_charger_coupler_id.Text.Trim();
            if (!uint.TryParse(coupler_id, out uint i_coupler_id))
            {
                MessageBox.Show("Coupler ID could only be numbers.");
                return;
            }
            //uint.TryParse(coupler_id, out uint i_coupler_id);
            bool is_enable = cb_coupler_enable.Checked;
            Task.Run(() =>
            {
                var mtl_mapaction = bcApp.SCApplication.getEQObjCacheManager().getUnit("MCharger", charger_id).
                getMapActionByIdentityKey(nameof(com.mirle.ibg3k0.sc.Data.ValueDefMapAction.SubChargerValueDefMapAction)) as
                com.mirle.ibg3k0.sc.Data.ValueDefMapAction.SubChargerValueDefMapAction;
                mtl_mapaction.AGVCToChargerCouplerEnable(i_coupler_id, is_enable);
            });
        }

        private void btn_ReserveTest_Click(object sender, EventArgs e)
        {
            string sec_id = cmb_reserve_section.Text;
            string sec_id_1 = cmb_reserve_section1.Text;
            string sec_id_2 = cmb_reserve_section2.Text;
            sc.ProtocolFormat.OHTMessage.DriveDirction driveDirction = radio_Forward.Checked ?
                sc.ProtocolFormat.OHTMessage.DriveDirction.DriveDirForward :
                sc.ProtocolFormat.OHTMessage.DriveDirction.DriveDirReverse;
            Task.Run(() =>
            {
                Google.Protobuf.Collections.RepeatedField<sc.ProtocolFormat.OHTMessage.ReserveInfo> reserves = new Google.Protobuf.Collections.RepeatedField<sc.ProtocolFormat.OHTMessage.ReserveInfo>();
                if (!sc.Common.SCUtility.isEmpty(sec_id))
                    reserves.Add(new sc.ProtocolFormat.OHTMessage.ReserveInfo()
                    {
                        ReserveSectionID = sec_id,
                        DriveDirction = driveDirction
                    });
                if (!sc.Common.SCUtility.isEmpty(sec_id_1))
                    reserves.Add(new sc.ProtocolFormat.OHTMessage.ReserveInfo()
                    {
                        ReserveSectionID = sec_id_1,
                        DriveDirction = driveDirction
                    });
                if (!sc.Common.SCUtility.isEmpty(sec_id_2))
                    reserves.Add(new sc.ProtocolFormat.OHTMessage.ReserveInfo()
                    {
                        ReserveSectionID = sec_id_2,
                        DriveDirction = driveDirction
                    });
                bcApp.SCApplication.VehicleService.IsReserveSuccessTest(noticeCar.VEHICLE_ID, reserves);
            });

        }

        private void button7_Click_1(object sender, EventArgs e)
        {
            dgv_cache_object_data.Refresh();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            dgv_cache_object_data.Refresh();
        }

        private void btn_reserve_clear_Click(object sender, EventArgs e)
        {
            string sec_id = cmb_reserve_section.Text;
            ASECTION section = bcApp.SCApplication.SectionBLL.cache.GetSection(sec_id);
            sc.ProtocolFormat.OHTMessage.DriveDirction driveDirction = radio_Forward.Checked ?
            sc.ProtocolFormat.OHTMessage.DriveDirction.DriveDirForward :
            sc.ProtocolFormat.OHTMessage.DriveDirction.DriveDirReverse;
            section.ClaerSectionReservation(driveDirction);

        }


        private void num_134_test_dis_ValueChanged(object sender, EventArgs e)
        {
            string vh_id = cmb_tcpipctr_Vehicle.Text;
            string sec_id = txt_134_test_section_id.Text;
            string drive = txt_134_test_section_id.Text;
            uint distance = (uint)num_134_test_dis.Value;
            sc.ProtocolFormat.OHTMessage.DriveDirction driveDirction = rad_134_test_f.Checked ?
                sc.ProtocolFormat.OHTMessage.DriveDirction.DriveDirForward : sc.ProtocolFormat.OHTMessage.DriveDirction.DriveDirReverse;
            var section_obj = mainForm.BCApp.SCApplication.SectionBLL.cache.GetSection(sec_id);
            sc.ProtocolFormat.OHTMessage.ID_134_TRANS_EVENT_REP id_134_trans_event_rep = new sc.ProtocolFormat.OHTMessage.ID_134_TRANS_EVENT_REP()
            {
                CurrentAdrID = section_obj == null ? "" : section_obj.TO_ADR_ID,
                CurrentSecID = sec_id,
                EventType = sc.ProtocolFormat.OHTMessage.EventType.AdrPass,
                SecDistance = distance,
                DrivingDirection = driveDirction
            };

            Task.Run(() =>
            {
                mainForm.BCApp.SCApplication.VehicleBLL.setAndPublishPositionReportInfo2Redis(vh_id, id_134_trans_event_rep);
            });
        }

        private void btn_Charger1ForceFinish_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(cb_Charger.Text))
            {
                MessageBox.Show("Please select charger.");
            }
            string charger_id = cb_Charger.Text.Trim();
            Task.Run(() =>
            {
                var mtl_mapaction = bcApp.SCApplication.getEQObjCacheManager().getUnit("MCharger", charger_id)
                .getMapActionByIdentityKey(nameof(com.mirle.ibg3k0.sc.Data.ValueDefMapAction.SubChargerValueDefMapAction)) as
                    com.mirle.ibg3k0.sc.Data.ValueDefMapAction.SubChargerValueDefMapAction;
                mtl_mapaction.AGVCToChargerForceStopCharging();
            }
            );
        }




        private void ck_CycleRunTest_CheckedChanged(object sender, EventArgs e)
        {
            DebugParameter.IsCycleRun = ck_CycleRunTest.Checked;
        }

        //private void btn_CouplerStatusRefresh_Click(object sender, EventArgs e)
        //{
        //    refreshCouplerStatusUI();
        //}

        //private void refreshCouplerStatusUI()
        //{
        //AUNIT unit1 = bcApp.SCApplication.getEQObjCacheManager().getUnit("MCharger", "Charger1");
        //AUNIT unit2 = bcApp.SCApplication.getEQObjCacheManager().getUnit("MCharger", "Charger2");
        //AUNIT unit3 = bcApp.SCApplication.getEQObjCacheManager().getUnit("MCharger", "Charger3");
        //AUNIT unit4 = bcApp.SCApplication.getEQObjCacheManager().getUnit("MCharger", "Charger4");

        //textBox_C11_Status.Text = unit1.coupler1Status.ToString();
        //textBox_C12_Status.Text = unit1.coupler2Status.ToString();
        //textBox_C13_Status.Text = unit1.coupler3Status.ToString();
        //textBox_C21_Status.Text = unit2.coupler1Status.ToString();
        //textBox_C22_Status.Text = unit2.coupler2Status.ToString();
        //textBox_C23_Status.Text = unit2.coupler3Status.ToString();
        //textBox_C31_Status.Text = unit3.coupler1Status.ToString();
        //textBox_C32_Status.Text = unit3.coupler2Status.ToString();
        //textBox_C33_Status.Text = unit3.coupler3Status.ToString();
        //textBox_C41_Status.Text = unit4.coupler1Status.ToString();
        //textBox_C42_Status.Text = unit4.coupler2Status.ToString();
        //textBox_C43_Status.Text = unit4.coupler3Status.ToString();
        //}

        private void btn_online_Click(object sender, EventArgs e)
        {
            SCApplication scApp = SCApplication.getInstance();
            scApp.LineService.OnlineRemoteWithHost();
        }

        private void btnPortEnableSet_Click(object sender, EventArgs e)
        {
            AEQPT eqpt = bcApp.SCApplication.getEQObjCacheManager().getEquipmentByEQPTID("EQ1");
            if (comboBox_port11.SelectedIndex == 0)
            {
                if (P11.PORT_SERVICE_STATUS != sc.ProtocolFormat.OHTMessage.PortStationServiceStatus.InService)
                {
                    P11.PORT_SERVICE_STATUS = sc.ProtocolFormat.OHTMessage.PortStationServiceStatus.InService;
                    if (eqpt.EQ_Down)
                    {
                        mcsMapAction.sendS6F11_PortEventState(P11.PORT_ID, ((int)sc.ProtocolFormat.OHTMessage.PortStationStatus.Down).ToString());
                    }
                    else
                    {
                        mcsMapAction.sendS6F11_PortEventState(P11.PORT_ID, ((int)P11.PORT_STATUS).ToString());
                    }
                }
            }
            else
            {
                if (P11.PORT_SERVICE_STATUS != sc.ProtocolFormat.OHTMessage.PortStationServiceStatus.OutOfService)
                {
                    P11.PORT_SERVICE_STATUS = sc.ProtocolFormat.OHTMessage.PortStationServiceStatus.OutOfService;
                    mcsMapAction.sendS6F11_PortEventState(P11.PORT_ID, ((int)sc.ProtocolFormat.OHTMessage.PortStationStatus.Disabled).ToString());
                }
            }
            if (comboBox_port12.SelectedIndex == 0)
            {
                if (P12.PORT_SERVICE_STATUS != sc.ProtocolFormat.OHTMessage.PortStationServiceStatus.InService)
                {
                    P12.PORT_SERVICE_STATUS = sc.ProtocolFormat.OHTMessage.PortStationServiceStatus.InService;
                    if (eqpt.EQ_Down)
                    {
                        mcsMapAction.sendS6F11_PortEventState(P12.PORT_ID, ((int)sc.ProtocolFormat.OHTMessage.PortStationStatus.Down).ToString());
                    }
                    else
                    {
                        mcsMapAction.sendS6F11_PortEventState(P12.PORT_ID, ((int)P12.PORT_STATUS).ToString());
                    }
                }
            }
            else
            {
                if (P12.PORT_SERVICE_STATUS != sc.ProtocolFormat.OHTMessage.PortStationServiceStatus.OutOfService)
                {
                    P12.PORT_SERVICE_STATUS = sc.ProtocolFormat.OHTMessage.PortStationServiceStatus.OutOfService;
                    mcsMapAction.sendS6F11_PortEventState(P12.PORT_ID, ((int)sc.ProtocolFormat.OHTMessage.PortStationStatus.Disabled).ToString());
                }
            }
            if (comboBox_port13.SelectedIndex == 0)
            {
                if (P13.PORT_SERVICE_STATUS != sc.ProtocolFormat.OHTMessage.PortStationServiceStatus.InService)
                {
                    P13.PORT_SERVICE_STATUS = sc.ProtocolFormat.OHTMessage.PortStationServiceStatus.InService;
                    if (eqpt.EQ_Down)
                    {
                        mcsMapAction.sendS6F11_PortEventState(P13.PORT_ID, ((int)sc.ProtocolFormat.OHTMessage.PortStationStatus.Down).ToString());
                    }
                    else
                    {
                        mcsMapAction.sendS6F11_PortEventState(P13.PORT_ID, ((int)P13.PORT_STATUS).ToString());
                    }
                }
            }
            else
            {
                if (P13.PORT_SERVICE_STATUS != sc.ProtocolFormat.OHTMessage.PortStationServiceStatus.OutOfService)
                {
                    P13.PORT_SERVICE_STATUS = sc.ProtocolFormat.OHTMessage.PortStationServiceStatus.OutOfService;
                    mcsMapAction.sendS6F11_PortEventState(P13.PORT_ID, ((int)sc.ProtocolFormat.OHTMessage.PortStationStatus.Disabled).ToString());
                }
            }
            if (comboBox_port14.SelectedIndex == 0)
            {
                if (P14.PORT_SERVICE_STATUS != sc.ProtocolFormat.OHTMessage.PortStationServiceStatus.InService)
                {
                    P14.PORT_SERVICE_STATUS = sc.ProtocolFormat.OHTMessage.PortStationServiceStatus.InService;
                    if (eqpt.EQ_Down)
                    {
                        mcsMapAction.sendS6F11_PortEventState(P14.PORT_ID, ((int)sc.ProtocolFormat.OHTMessage.PortStationStatus.Down).ToString());
                    }
                    else
                    {
                        mcsMapAction.sendS6F11_PortEventState(P14.PORT_ID, ((int)P14.PORT_STATUS).ToString());
                    }
                }
            }
            else
            {
                if (P14.PORT_SERVICE_STATUS != sc.ProtocolFormat.OHTMessage.PortStationServiceStatus.OutOfService)
                {
                    P14.PORT_SERVICE_STATUS = sc.ProtocolFormat.OHTMessage.PortStationServiceStatus.OutOfService;
                    mcsMapAction.sendS6F11_PortEventState(P14.PORT_ID, ((int)sc.ProtocolFormat.OHTMessage.PortStationStatus.Disabled).ToString());
                }
            }
        }

        private void num_batteryCapacity_ValueChanged(object sender, EventArgs e)
        {
            DebugParameter.BatteryCapacity = (uint)num_batteryCapacity.Value;
        }

        private void btn_auto_remote_Click(object sender, EventArgs e)
        {
            asyExecuteAction(bcApp.SCApplication.VehicleService.changeVhStatusToAutoRemote);

        }

        private async void button10_Click_1(object sender, EventArgs e)
        {
            try
            {
                button10.Enabled = false;
                await Task.Run(() =>
                {
                    CreatTranCmd("AASTK250P01", "CASTK010P01");
                    CreatTranCmd("CASTK010P01", "AASTK250P01");
                    CreatTranCmd("AASTK250P01", "CASTK010P01");
                    CreatTranCmd("CASTK010P01", "AASTK250P01");
                    CreatTranCmd("AASTK250P01", "CASTK010P01");
                    CreatTranCmd("CASTK010P01", "AASTK250P01");
                });
            }
            catch { }
            finally
            {
                button10.Enabled = true;
            }
        }
        public void CreatTranCmd(string source_port, string destn_port)
        {

            string cmdID = DateTime.Now.ToString("yyyyMMddHHmmssfffff");
            bcApp.SCApplication.CMDBLL.doCreatMCSCommand(cmdID, "0", "0", "CST05", source_port, destn_port, SECSConst.HCACK_Confirm);
            bcApp.SCApplication.SysExcuteQualityBLL.creatSysExcuteQuality(cmdID, "CST05", source_port, destn_port);

        }

        private void num_BatteryLowBoundaryValue_ValueChanged(object sender, EventArgs e)
        {
            AVEHICLE.SettingBatteryLevelLowBoundary((UInt16)num_BatteryLowBoundaryValue.Value);

        }

        private void num_BatteryHighBoundaryValue_ValueChanged(object sender, EventArgs e)
        {
            AVEHICLE.SettingBatteryLevelHighBoundary((UInt16)num_BatteryHighBoundaryValue.Value);
        }

        private void btn_refresf_portsation_info_Click(object sender, EventArgs e)
        {
            dgv_cache_object_data_portstation.Refresh();

        }

        private void num_cycle_run_interval_time_ValueChanged(object sender, EventArgs e)
        {
            DebugParameter.CycleRunIntervalTime = (int)num_cycle_run_interval_time.Value;
        }

        private void cb_reserve_reject_CheckedChanged(object sender, EventArgs e)
        {
            DebugParameter.isForcedRejectReserve = cb_reserve_reject.Checked;
        }

        private void button11_Click_1(object sender, EventArgs e)
        {
        }

        private async void btn_release_block_Click(object sender, EventArgs e)
        {
            try
            {
                btn_release_block.Enabled = false;
                await Task.Run(() => bcApp.SCApplication.LineService.RefreshCurrentVehicleReserveStatus());
                BCFApplication.onInfoMsg("Fource release current vehicle reserve status success");
            }
#pragma warning disable CS0168 // 已宣告變數 'ex'，但從未使用過它。
            catch (Exception ex)
#pragma warning restore CS0168 // 已宣告變數 'ex'，但從未使用過它。
            {

            }
            finally
            {
                btn_release_block.Enabled = true;
            }
        }

        private void numer_num_of_avoid_seg_ValueChanged(object sender, EventArgs e)
        {
            DebugParameter.NumberOfAvoidanceSegment = (int)numer_num_of_avoid_seg.Value;
        }

        private void cb_reserve_pass_CheckedChanged(object sender, EventArgs e)
        {
            DebugParameter.isForcedPassReserve = cb_reserve_pass.Checked;
        }

        private void btn_loadArrivals_Click(object sender, EventArgs e)
        {
            var report_event = sc.ProtocolFormat.OHTMessage.EventType.LoadArrivals;
            McsReportEventTest(report_event);
        }

        private void McsReportEventTest(sc.ProtocolFormat.OHTMessage.EventType report_event,
            sc.ProtocolFormat.OHTMessage.BCRReadResult bCRReadResult = sc.ProtocolFormat.OHTMessage.BCRReadResult.BcrNormal)
        {
            string cst_id = txt_mcsReportTestCstID.Text;
            AVEHICLE test_report_vh = bcApp.SCApplication.VehicleBLL.cache.getVehicle(cmb_mcsReportTestVHID.Text);
            var id_136 = new sc.ProtocolFormat.OHTMessage.ID_136_TRANS_EVENT_REP()
            {
                EventType = report_event,
                CSTID = cst_id,
                BCRReadResult = bCRReadResult
            };
            var bcfApp = bcApp.SCApplication.getBCFApplication();
            Task.Run(() => bcApp.SCApplication.VehicleService.TranEventReport(bcfApp, test_report_vh, id_136, 0));
        }

        private void btn_vhloading_Click(object sender, EventArgs e)
        {
            var report_event = sc.ProtocolFormat.OHTMessage.EventType.Vhloading;
            McsReportEventTest(report_event);
        }

        private void btn_loadComplete_Click(object sender, EventArgs e)
        {
            var report_event = sc.ProtocolFormat.OHTMessage.EventType.LoadComplete;
            McsReportEventTest(report_event);
        }

        private void btn_unloadArrivals_Click(object sender, EventArgs e)
        {
            var report_event = sc.ProtocolFormat.OHTMessage.EventType.UnloadArrivals;
            McsReportEventTest(report_event);
        }

        private void btn_vhunloading_Click(object sender, EventArgs e)
        {
            var report_event = sc.ProtocolFormat.OHTMessage.EventType.Vhunloading;
            McsReportEventTest(report_event);
        }

        private void btn_unloadComplete_Click(object sender, EventArgs e)
        {
            var report_event = sc.ProtocolFormat.OHTMessage.EventType.UnloadComplete;
            McsReportEventTest(report_event);
        }




        private void btn_id_bcr_read_Click(object sender, EventArgs e)
        {
            var report_event = sc.ProtocolFormat.OHTMessage.EventType.Bcrread;
            McsReportEventTest(report_event);
        }

        private void btn_bcrReadMismatch_Click(object sender, EventArgs e)
        {
            var report_event = sc.ProtocolFormat.OHTMessage.EventType.Bcrread;
            McsReportEventTest(report_event, sc.ProtocolFormat.OHTMessage.BCRReadResult.BcrMisMatch);

            //var completeStatus = sc.ProtocolFormat.OHTMessage.CompleteStatus.CmpStatusIdmisMatch;
            //McsCommandCompleteTest(completeStatus);
        }

        private void btn_bcrReadError_Click(object sender, EventArgs e)
        {
            var report_event = sc.ProtocolFormat.OHTMessage.EventType.Bcrread;
            McsReportEventTest(report_event, sc.ProtocolFormat.OHTMessage.BCRReadResult.BcrReadFail);
            //var completeStatus = sc.ProtocolFormat.OHTMessage.CompleteStatus.CmpStatusIdreadFailed;
            //McsCommandCompleteTest(completeStatus);
        }

        private void btn_cancel_cmp_Click(object sender, EventArgs e)
        {

        }
        private void btn_cancelFail_Click(object sender, EventArgs e)
        {

        }

        private void btn_interlockError_Click(object sender, EventArgs e)
        {
            var completeStatus = sc.ProtocolFormat.OHTMessage.CompleteStatus.CmpStatusInterlockError;

            McsCommandCompleteTest(completeStatus);
        }

        private void McsCommandCompleteTest(sc.ProtocolFormat.OHTMessage.CompleteStatus completeStatus)
        {
            string cmd_id = txt_mcsReportTestCmdID.Text;
            string cst_id = txt_mcsReportTestCstID.Text;
            AVEHICLE test_report_vh = bcApp.SCApplication.VehicleBLL.cache.getVehicle(cmb_mcsReportTestVHID.Text);
            var id_132 = new sc.ProtocolFormat.OHTMessage.ID_132_TRANS_COMPLETE_REPORT()
            {
                CmdID = cmd_id,
                CSTID = cst_id,
                CmpStatus = completeStatus
            };
            var bcfApp = bcApp.SCApplication.getBCFApplication();
            Task.Run(() => bcApp.SCApplication.VehicleService.CommandCompleteReport("", bcfApp, test_report_vh, id_132, 0));
        }

        private void btn_alarmtSet_Click(object sender, EventArgs e)
        {
            string error_code = "100001";
            var error_status = sc.ProtocolFormat.OHTMessage.ErrorStatus.ErrSet;
            AVEHICLE test_report_vh = bcApp.SCApplication.VehicleBLL.cache.getVehicle(cmb_mcsReportTestVHID.Text);
            Task.Run(() => bcApp.SCApplication.VehicleService.ProcessAlarmReport(test_report_vh, error_code, error_status, ""));
        }

        private void btn_alarmClear_Click(object sender, EventArgs e)
        {
            string error_code = "100001";
            var error_status = sc.ProtocolFormat.OHTMessage.ErrorStatus.ErrReset;
            AVEHICLE test_report_vh = bcApp.SCApplication.VehicleBLL.cache.getVehicle(cmb_mcsReportTestVHID.Text);
            Task.Run(() => bcApp.SCApplication.VehicleService.ProcessAlarmReport(test_report_vh, error_code, error_status, ""));
        }

        private void btn_cmpIdMismatch_Click(object sender, EventArgs e)
        {
            var completeStatus = sc.ProtocolFormat.OHTMessage.CompleteStatus.CmpStatusIdmisMatch;

            McsCommandCompleteTest(completeStatus);
        }

        private void btn_idReadError_Click(object sender, EventArgs e)
        {
            var completeStatus = sc.ProtocolFormat.OHTMessage.CompleteStatus.CmpStatusIdreadFailed;

            McsCommandCompleteTest(completeStatus);
        }

        private void btn_cmp_vh_abort_Click(object sender, EventArgs e)
        {
            var completeStatus = sc.ProtocolFormat.OHTMessage.CompleteStatus.CmpStatusVehicleAbort;

            McsCommandCompleteTest(completeStatus);
        }

        private void ch_reserve_stop_CheckedChanged(object sender, EventArgs e)
        {
            bcApp.SCApplication.VehicleService.ReserveStopTest(vh_id, ch_reserve_stop.Checked);
        }

        private void btn_auto_charge_Click(object sender, EventArgs e)
        {
            asyExecuteAction(bcApp.SCApplication.VehicleService.changeVhStatusToAutoCharging);
        }

        private void btn_auto_local_Click(object sender, EventArgs e)
        {
            asyExecuteAction(bcApp.SCApplication.VehicleService.changeVhStatusToAutoLocal);
        }

        private async void btn_changeToRemove_Click(object sender, EventArgs e)
        {
            try
            {
                if (!noticeCar.IS_INSTALLED)
                {
                    MessageBox.Show($"{vh_id} is removed ready!");
                    return;
                }
                btn_changeToRemove.Enabled = false;
                (bool isSuccess, string result) check_result = default((bool isSuccess, string result));
                await Task.Run(() => check_result = bcApp.SCApplication.VehicleService.Remove(vh_id));
                if (check_result.isSuccess)
                {
                    MessageBox.Show($"{vh_id} remove ok");
                }
                else
                {
                    MessageBox.Show($"{vh_id} remove fail.{Environment.NewLine}" +
                                    $"result:{check_result.result}");
                }
                lbl_install_status.Text = noticeCar?.IS_INSTALLED.ToString();
            }
            finally
            {
                btn_changeToRemove.Enabled = true;
            }
        }

        private async void btn_changeToInstall_Click(object sender, EventArgs e)
        {
            try
            {
                if (noticeCar.IS_INSTALLED)
                {
                    MessageBox.Show($"{vh_id} is install ready!");
                    return;
                }

                btn_changeToInstall.Enabled = false;
                (bool isSuccess, string result) check_result = default((bool isSuccess, string result));
                await Task.Run(() => check_result = bcApp.SCApplication.VehicleService.Install(vh_id));
                if (check_result.isSuccess)
                {
                    MessageBox.Show($"{vh_id} install ok");
                }
                else
                {
                    MessageBox.Show($"{vh_id} install fail.{Environment.NewLine}" +
                                    $"result:{check_result.result}");
                }
                lbl_install_status.Text = noticeCar?.IS_INSTALLED.ToString();
            }
            finally
            {
                btn_changeToInstall.Enabled = true;
            }
        }




        private void btn_set_adr_Click(object sender, EventArgs e)
        {

        }

        private void button_set_ECID_Click(object sender, EventArgs e)
        {
            string rtnMsg;
            for (int i = 0; i < ECDATAMAPs.Count; i++)
            {
                if (BCFUtility.isMatche(ECDATAMAPs[i].ECID, SCAppConstants.ECID_EQPNAME))
                {
                    ECDATAMAPs[i].ECV = textBox_eqpname.Text;
                }
                else if (BCFUtility.isMatche(ECDATAMAPs[i].ECID, SCAppConstants.ECID_ESTABLISH_COMMUNICATION_TIMEOUT))
                {
                    ECDATAMAPs[i].ECV = textBox_EstablishCommunicationTimeout.Text;
                }
                else if (BCFUtility.isMatche(ECDATAMAPs[i].ECID, SCAppConstants.ECID_INITIAL_COMMUNICATION_STATE))
                {
                    ECDATAMAPs[i].ECV = textBox_InitialCommunicationState.Text;
                }
                else if (BCFUtility.isMatche(ECDATAMAPs[i].ECID, SCAppConstants.ECID_INITIAL_CONTROL_STATE))
                {
                    ECDATAMAPs[i].ECV = textBox_InitialControlState.Text;
                }
                else if (BCFUtility.isMatche(ECDATAMAPs[i].ECID, SCAppConstants.ECID_SOFT_REVISION))
                {
                    ECDATAMAPs[i].ECV = textBox_SoftRevision.Text;
                }
                else if (BCFUtility.isMatche(ECDATAMAPs[i].ECID, SCAppConstants.ECID_TIME_FORMAT))
                {
                    ECDATAMAPs[i].ECV = textBox_TIMEFORMAT.Text;
                }
                else if (BCFUtility.isMatche(ECDATAMAPs[i].ECID, SCAppConstants.ECID_MDLN))
                {
                    ECDATAMAPs[i].ECV = textBox_ModelName.Text;
                }
                else
                {
                    continue;
                }
            }
            bcApp.SCApplication.LineBLL.updateECData(ECDATAMAPs, out rtnMsg);
        }

        private void checkBox_host_offline_CheckedChanged(object sender, EventArgs e)
        {
            DebugParameter.UseHostOffline = checkBox_host_offline.Checked;
        }

        private void btn_reposition_Click(object sender, EventArgs e)
        {
            var report_event = sc.ProtocolFormat.OHTMessage.EventType.RePosition;
            McsReportEventTest(report_event);
        }

        private void cb_advanceDriveAway_CheckedChanged(object sender, EventArgs e)
        {
            DebugParameter.AdvanceDriveAway = cb_advanceDriveAway.Checked;
        }

        private void cb_passCouplerHPSafetySingnal_CheckedChanged(object sender, EventArgs e)
        {
            DebugParameter.isPassCouplerHPSafetySignal = cb_passCouplerHPSafetySingnal.Checked;
        }


        private void btnLighthouse_Click(object sender, EventArgs e)
        {
            var Lighthouse = bcApp.SCApplication.getEQObjCacheManager().getEquipmentByEQPTID("ColorLight");
            if (sender == btn_lighthouse_red_set)
            {
                Lighthouse.setColorLightRedWithBuzzer(true, true);
            }
            else if (sender == btn_lighthouse_green_set)
            {
                Lighthouse.setColorLightGreen(true);
            }
            else if (sender == btn_lighthouse_blue_set)
            {
                Lighthouse.setColorLightBlue(true);
            }
            else if (sender == btn_lighthouse_orange_set)
            {
                Lighthouse.setColorLightYellow(true);
            }
            else if (sender == btn_lighthouse_red_reset)
            {
                Lighthouse.setColorLightRedWithBuzzer(false, false);
            }
            else if (sender == btn_lighthouse_green_reset)
            {
                Lighthouse.setColorLightGreen(false);
            }
            else if (sender == btn_lighthouse_blue_reset)
            {
                Lighthouse.setColorLightBlue(false);
            }
            else if (sender == btn_lighthouse_orange_reset)
            {
                Lighthouse.setColorLightYellow(false);
            }
        }
    }
}
