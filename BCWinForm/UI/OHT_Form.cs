using com.mirle.ibg3k0.sc;
using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Data;
using com.mirle.ibg3k0.sc.Data.ValueDefMapAction;
using com.mirle.ibg3k0.sc.Data.VO;
using com.mirle.ibg3k0.sc.ProtocolFormat.OHTMessage;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.Entity;
using com.mirle.ibg3k0.bcf.Common;
using com.mirle.ibg3k0.sc.ObjectRelay;
using com.mirle.ibg3k0.bc.winform.App;
using com.mirle.ibg3k0.Utility.ul.Data.VO;
using com.mirle.ibg3k0.sc.Common;
using System.Threading;
using NLog;
using com.mirle.ibg3k0.bc.winform.Common;

namespace com.mirle.ibg3k0.bc.winform.UI
{
    public partial class OHT_Form : Form
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        BCMainForm mainform = null;
        SCApplication scApp = null;
        //BindingList<VehicleObjToShow> ObjectToShow_list = new BindingList<VehicleObjToShow>();
        BindingSource bindingSource = new BindingSource();
        BindingSource cmsMCS_bindingSource = new BindingSource();
        List<CMD_MCSObjToShow> cmd_mcs_obj_to_show = null;

        string[] allAdr_ID = null;
        string[] allPortID = null;

        List<ALARM> aLARMs = new List<ALARM>();

        public OHT_Form(BCMainForm _form)
        {
            InitializeComponent();
            mainform = _form;
            scApp = mainform.BCApp.SCApplication;
            uctl_Map.start(_form, this);
            //ui_Vehicle1.start("OHT001");
            initialComBox();
            initialDataGreadView();
            // utilityLog_SECS.start<LogTitle_SECS>();
            //dgv_vhStatus.AutoGenerateColumns = false;
            bindingSource.DataSource = scApp.getEQObjCacheManager().CommonInfo.ObjectToShow_list;
            dgv_vhStatus.DataSource = bindingSource;
            scApp.getEQObjCacheManager().CommonInfo.ObjectToShow_list.Clear();

            uctl_Map.BackColor = Color.FromArgb(29, 36, 60);
            dgv_TransferCommand.AutoGenerateColumns = false;


            double distance_scale = 1000;
            //using (DBConnection_EF context = new DBConnection_EF())
            //{
            //lstSection = context.AVEHICLE.ToList();
            //context.AVEHICLE.Load();
            foreach (var vh in scApp.getEQObjCacheManager().getAllVehicle())
            {
                VehicleObjToShow vhShowObj = new VehicleObjToShow(vh, distance_scale);

                scApp.getEQObjCacheManager().CommonInfo.ObjectToShow_list.Add(vhShowObj);
            }
            //}
            timer_TimedUpdates.Enabled = true;
            adjustmentDataGridViewWeight();

            initialEvent();

        }

        private void initialEvent()
        {
            ALINE line = scApp.getEQObjCacheManager().getLine();

            line.addEventHandler(this.Name
           , BCFUtility.getPropertyName(() => line.ServiceMode)
           , (s1, e1) =>
           {
               Adapter.Invoke((obj) =>
               {
                   switch (line.ServiceMode)
                   {
                       case SCAppConstants.AppServiceMode.None:
                           lbl_isMaster.BackColor = Color.Gray;
                           break;
                       case SCAppConstants.AppServiceMode.Active:
                           lbl_isMaster.BackColor = Color.Green;
                           break;
                       case SCAppConstants.AppServiceMode.Standby:
                           lbl_isMaster.BackColor = Color.Yellow;
                           break;
                   }
               }, null);
           });
            line.addEventHandler(this.Name
           , BCFUtility.getPropertyName(() => line.Secs_Link_Stat)
                , (s1, e1) =>
                {
                    lbl_hostconnAndMode.BackColor =
                    line.Secs_Link_Stat == SCAppConstants.LinkStatus.LinkOK ? Color.Green : Color.Gray;
                }
                );
            line.addEventHandler(this.Name
           , BCFUtility.getPropertyName(() => line.Host_Control_State)
                , (s1, e1) =>
                {
                    SetHostControlState(line.Host_Control_State);
                }
                );
            //line.addEventHandler(this.Name
            //, BCFUtility.getPropertyName(() => line.TSCStateMachine)
            //     , (s1, e1) =>
            //     {
            //         SetSCState(line.Host_Control_State);
            //     }
            //     );
            line.addEventHandler(this.Name
           , BCFUtility.getPropertyName(() => line.Redis_Link_Stat)
                , (s1, e1) =>
                {
                    lbl_RediStat.BackColor =
                    line.Redis_Link_Stat == SCAppConstants.LinkStatus.LinkOK ? Color.Green : Color.Gray;
                }
                );
            line.addEventHandler(this.Name
            , BCFUtility.getPropertyName(() => line.IsEarthquakeHappend)
                , (s1, e1) =>
                {
                    lbl_earthqualeHappend.BackColor =
                    line.IsEarthquakeHappend ? Color.Red : Color.Gray;
                    //if (line.IsEarthquakeHappend)
                    //{
                    //    aLARMs[0].ALAM_CODE = "AE001";
                    //    aLARMs[0].ALAM_DESC = "An earthquake has occurred !!!";
                    //    aLARMs[0].ALAM_LVL = "Alarm";
                    //    aLARMs[0].EQPT_ID = "MCP";
                    //    aLARMs[0].RPT_DATE_TIME = DateTime.Now.ToString(SCAppConstants.DateTimeFormat_19);
                    //}
                    //else
                    //{
                    //    aLARMs[0].ALAM_CODE = "";
                    //    aLARMs[0].ALAM_DESC = "";
                    //    aLARMs[0].ALAM_LVL = "";
                    //    aLARMs[0].EQPT_ID = "";
                    //    aLARMs[0].RPT_DATE_TIME = "";
                    //}
                    //Adapter.Invoke((obj) =>
                    //{
                    //    if (line.IsEarthquakeHappend)
                    //        tbcList.SelectedIndex = 4;
                    //    dgv_Alarm.Refresh();
                    //}, null);
                }
                );
            line.addEventHandler(this.Name
                , BCFUtility.getPropertyName(() => line.DetectionSystemExist)
                    , (s1, e1) =>
                    {
                        lbl_detectionSystemExist.BackColor =
                        line.DetectionSystemExist == SCAppConstants.ExistStatus.Exist ? Color.Green : Color.Gray;
                    }
                    );
            scApp.getNatsManager().Subscriber(SCAppConstants.NATS_SUBJECT_CURRENT_ALARM, SetCurrentAlarm);

            sc.App.SystemParameter.AutoOverrideChange += SystemParameter_AutoOverrideChange;
        }


        private void SetCurrentAlarm(object sender, EventArgs e)
        {
            List<ALARM> alarms = scApp.AlarmBLL.getCurrentAlarmsFromRedis();
            Adapter.Invoke((obj) =>
            {
                dgv_Alarm.DataSource = alarms;
            }, null);
        }

        private void SetHostControlState(SCAppConstants.LineHostControlState.HostControlState controlState)
        {
            Adapter.Invoke((obj) =>
            {
                lbl_hostconnAndMode.Text = controlState.GetDisplayName();
            }, null);
        }

        private void SetSCState(ALINE line)
        {
            Adapter.Invoke((obj) =>
            {
                lbl_SCState.Text = line.SCStats.GetDisplayName();
            }, null);
        }

        private void adjustmentDataGridViewWeight()
        {
            dgv_vhStatus.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            foreach (DataGridViewColumn col in dgv_vhStatus.Columns)
            {
                switch (col.Name)
                {
                    case "MCS_CMD":
                        col.FillWeight = 1200;
                        break;
                    case "OHTC_CMD":
                        col.FillWeight = 1200;
                        break;
                    case "ACT_STATUS":
                        col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                        col.FillWeight = 1500;
                        break;
                    case "PACK_TIME":
                        col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                        col.FillWeight = 2400;
                        break;
                    case "CYCLERUN_TIME":
                        col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                        col.FillWeight = 2400;
                        break;
                    case "OBS_DIST2Show":
                    case "VEHICLE_ACC_DIST2Show":
                    case "ACC_SEC_DIST2Show":
                        col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                        break;
                }
            }
        }

        void Local_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            foreach (INotifyPropertyChanged item in e.NewItems)
                item.PropertyChanged += item_PropertyChanged;
        }

        void item_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {

        }

        private void initialComBox()
        {
            List<AADDRESS> allAddress_obj = scApp.MapBLL.loadAllAddress();
            allAdr_ID = allAddress_obj.Select(adr => adr.ADR_ID).ToArray();
            //string[] adr_port1_ID = allAddress_obj.
            //                        Where(adr => adr.PORT1_ID != null && adr.PORT1_ID != string.Empty).
            //                        Select(adr => adr.PORT1_ID).ToArray();
            //string[] adr_port2_ID = allAddress_obj.
            //            Where(adr => adr.PORT2_ID != null && adr.PORT2_ID != string.Empty).
            //            Select(adr => adr.PORT2_ID).ToArray();
            //List<string> portIDTemp = new List<string>();
            //portIDTemp.AddRange(adr_port1_ID);    
            //portIDTemp.AddRange(adr_port2_ID);
            //portIDTemp.OrderBy(id => id);
            allPortID = scApp.MapBLL.loadAllPort().Select(s => s.PORT_ID).ToArray();
            //allPortID = allAddress_obj.Where(adr=>adr.
            BCUtility.setComboboxDataSource(cmb_toAddress, allAdr_ID);
            BCUtility.setComboboxDataSource(cmb_fromAddress, allAdr_ID.ToArray());
            //cmb_fromAddress.DataSource = allAdr_ID.ToArray();
            //cmb_fromAddress.AutoCompleteCustomSource.AddRange(allAdr_ID);
            //cmb_fromAddress.AutoCompleteMode = AutoCompleteMode.Suggest;
            //cmb_fromAddress.AutoCompleteSource = AutoCompleteSource.ListItems;


            string[] allCycleRunZone = scApp.CycleBLL.loadCycleRunMasterByCurrentCycleTypeID
                (scApp.getEQObjCacheManager().getLine().Currnet_Cycle_Type).Select(master => master.ENTRY_ADR_ID).ToArray();
            cmb_cycRunZone.DataSource = allCycleRunZone;


            string[] allSec = scApp.MapBLL.loadAllSectionID().ToArray();
            cmb_fromSection.DataSource = allSec;
            cmb_fromSection.AutoCompleteCustomSource.AddRange(allSec);
            cmb_fromSection.AutoCompleteMode = AutoCompleteMode.Suggest;
            cmb_fromSection.AutoCompleteSource = AutoCompleteSource.ListItems;


            List<string> lstVh = new List<string>();
            lstVh.Add(string.Empty);
            lstVh.AddRange(scApp.VehicleBLL.loadAllVehicle().Select(vh => vh.VEHICLE_ID).ToList());
            string[] allVh = lstVh.ToArray();
            cmb_Vehicle.DataSource = allVh;
            cmb_Vehicle.AutoCompleteCustomSource.AddRange(allVh);
            cmb_Vehicle.AutoCompleteMode = AutoCompleteMode.Suggest;
            cmb_Vehicle.AutoCompleteSource = AutoCompleteSource.ListItems;

            cbm_Action.DataSource = Enum.GetValues(typeof(E_CMD_TYPE)).Cast<E_CMD_TYPE>()
                                                  .Where(e => e != E_CMD_TYPE.Continue &&
                                                              e != E_CMD_TYPE.Move_Park).ToList();


            List<string> park_zone_type = scApp.ParkBLL.loadAllParkZoneType();
            cb_parkZoneType.DataSource = park_zone_type;

        }


        private void initialDataGreadView()
        {
            aLARMs.Add(new ALARM());
            dgv_Alarm.AutoGenerateColumns = false;
            dgv_Alarm.DataSource = aLARMs;
        }


        private void btn_start_Click(object sender, EventArgs e)
        {
            E_CMD_TYPE cmd_type;
            Enum.TryParse<E_CMD_TYPE>(cbm_Action.SelectedValue.ToString(), out cmd_type);
            switch (cmd_type)
            {
                case E_CMD_TYPE.Move:
                case E_CMD_TYPE.Move_Charger:
                    excuteMoveCommand(cmd_type);
                    break;
                case E_CMD_TYPE.Round:
                    excuteCycleRunCommand();
                    break;
                case E_CMD_TYPE.LoadUnload:
                    excuteLoadUnloadCommand();
                    break;
                case E_CMD_TYPE.Teaching:
                    excuteTeachingCommand();
                    break;
                case E_CMD_TYPE.Home:
                    excuteHomeCommand();
                    break;
                case E_CMD_TYPE.MTLHome:
                    excuteMTLHomeCommand();
                    break;
                case E_CMD_TYPE.Load:
                    excuteLoadCommand();
                    break;
                case E_CMD_TYPE.Unload:
                    excuteUnloadCommand();
                    break;
            }
        }

        private async void excuteLoadUnloadCommand()
        {
            string fromSection = cmb_fromSection.Text;
            ASECTION asection = scApp.MapBLL.getSectiontByID(fromSection);

            string hostsource = cmb_fromAddress.Text;
            string hostdest = cmb_toAddress.Text;
            string from_adr = string.Empty;
            string to_adr = string.Empty;
            string cst_id = txt_cst_id.Text;
            E_VH_TYPE vh_type = E_VH_TYPE.None;
            scApp.MapBLL.getAddressID(hostsource, out from_adr, out vh_type);
            scApp.MapBLL.getAddressID(hostdest, out to_adr);
            string vehicleId = string.Empty;
            if (BCFUtility.isEmpty(cmb_Vehicle.Text))
            {
                //AVEHICLE firstVh = scApp.VehicleBLL.findBestSuitableVhByFromAdr(fromadr);
                AVEHICLE firstVh = scApp.VehicleBLL.findBestSuitableVhStepByStepFromAdr_New(from_adr, vh_type);
                if (firstVh != null)
                    vehicleId = firstVh.VEHICLE_ID.Trim();
            }
            else
            {
                vehicleId = cmb_Vehicle.Text;
            }

            if (BCFUtility.isEmpty(vehicleId))
            {
                MessageBox.Show("No find idle vehile.");
                return;
            }
            sc.BLL.CMDBLL.OHTCCommandCheckResult check_result_info = null;
            await Task.Run(() =>
            {
                scApp.CMDBLL.doCreatTransferCommand(vehicleId, string.Empty, cst_id,
                                                E_CMD_TYPE.LoadUnload,
                                                from_adr,
                                                to_adr, 0, 0);
                check_result_info = sc.BLL.CMDBLL.getCallContext<sc.BLL.CMDBLL.OHTCCommandCheckResult>
                   (sc.BLL.CMDBLL.CALL_CONTEXT_KEY_WORD_OHTC_CMD_CHECK_RESULT);
            });
            if (check_result_info != null && !check_result_info.IsSuccess)
            {
                MessageBox.Show(check_result_info.ToString(), "Command create fail.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                // bcf.App.BCFApplication.onWarningMsg(this, new LogEventArgs("Command create fail.", check_result_info.Num));
            }

        }

        private void excuteCycleRunCommand()
        {
            string toAdr = string.Empty;
            string vehicleId = string.Empty;

            vehicleId = cmb_Vehicle.Text;
            toAdr = cmb_cycRunZone.Text;
            if (BCFUtility.isEmpty(vehicleId))
            {
                MessageBox.Show("No find vehile.");
                return;
            }

            //scApp.CMDBLL.creatCommand_OHTC(vehicleId, string.Empty, string.Empty,
            //                                E_CMD_TYPE.Round,
            //                                string.Empty,
            //                                toAdr, 0, 0);
            //Task.Run(() => { scApp.CMDBLL.generateCmd_OHTC_Details(); });
            Task.Run(() =>
            scApp.CMDBLL.doCreatTransferCommand(vehicleId, string.Empty, string.Empty,
                                            E_CMD_TYPE.Round,
                                            string.Empty,
                                            toAdr, 0, 0));
        }

        private async void excuteMoveCommand(E_CMD_TYPE cmd_type)
        {
            string toAdr = string.Empty;
            string vehicleId = string.Empty;

            vehicleId = cmb_Vehicle.Text;
            toAdr = cmb_toAddress.Text;
            if (BCFUtility.isEmpty(vehicleId))
            {
                MessageBox.Show("No find vehile.");
                return;
            }

            sc.BLL.CMDBLL.OHTCCommandCheckResult check_result_info = null;
            await Task.Run(() =>
            {
                scApp.CMDBLL.doCreatTransferCommand(vehicleId, string.Empty, string.Empty,
                                              cmd_type,
                                              string.Empty,
                                              toAdr, 0, 0);
                check_result_info = sc.BLL.CMDBLL.getCallContext<sc.BLL.CMDBLL.OHTCCommandCheckResult>
                                   (sc.BLL.CMDBLL.CALL_CONTEXT_KEY_WORD_OHTC_CMD_CHECK_RESULT);
            });
            if (check_result_info != null && !check_result_info.IsSuccess)
            {
                MessageBox.Show(check_result_info.ToString(), "Command create fail.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //bcf.App.BCFApplication.onWarningMsg(this, new LogEventArgs("Command create fail.", check_result_info.Num));
            }
        }

        private async void excuteLoadCommand()
        {
            string fromAdr = string.Empty;
            string vehicleId = string.Empty;
            string cmd_id = string.Empty;
            string cst_id = txt_cst_id.Text;
            vehicleId = cmb_Vehicle.Text;
            fromAdr = cmb_fromAddress.Text;
            cmd_id = scApp.SequenceBLL.getCommandID(SCAppConstants.GenOHxCCommandType.Manual);
            if (BCFUtility.isEmpty(vehicleId))
            {
                MessageBox.Show("No find vehile.");
                return;
            }
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vehicleId);

            sc.BLL.CMDBLL.OHTCCommandCheckResult check_result_info = null;
            await Task.Run(() =>
            {
                //if (SCUtility.isMatche(vh.CUR_ADR_ID, fromAdr))
                //{
                //    scApp.VehicleService.TransferRequset(vehicleId, cmd_id, ActiveType.Load, "CA0070", new string[0], new string[0], new string[0], new string[0], new string[0], new string[0], fromAdr, "");
                //}
                //else
                {
                    scApp.CMDBLL.doCreatTransferCommand(vehicleId, string.Empty, cst_id,
                                                    E_CMD_TYPE.Load,
                                                    fromAdr,
                                                    string.Empty, 0, 0);
                }
                check_result_info = sc.BLL.CMDBLL.getCallContext<sc.BLL.CMDBLL.OHTCCommandCheckResult>
                   (sc.BLL.CMDBLL.CALL_CONTEXT_KEY_WORD_OHTC_CMD_CHECK_RESULT);
            }
             );
            if (check_result_info != null && !check_result_info.IsSuccess)
            {
                MessageBox.Show(check_result_info.ToString(), "Command create fail.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //bcf.App.BCFApplication.onWarningMsg(this, new LogEventArgs("Command create fail.", check_result_info.Num));
            }
        }

        private async void excuteUnloadCommand()
        {
            string toAdr = string.Empty;
            string vehicleId = string.Empty;
            string cmd_id = string.Empty;
            vehicleId = cmb_Vehicle.Text;
            toAdr = cmb_toAddress.Text;
            if (BCFUtility.isEmpty(vehicleId))
            {
                MessageBox.Show("No find vehile.");
                return;
            }
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vehicleId);
            cmd_id = scApp.SequenceBLL.getCommandID(SCAppConstants.GenOHxCCommandType.Manual);
            string cst_id = SCUtility.Trim(vh.CST_ID);
            sc.BLL.CMDBLL.OHTCCommandCheckResult check_result_info = null;
            await Task.Run(() =>
            {

                //if (SCUtility.isMatche(vh.CUR_ADR_ID, toAdr))
                //{
                //    scApp.VehicleService.TransferRequset(vehicleId, cmd_id, ActiveType.Unload, "CST02", new string[0], new string[0], new string[0], new string[0], new string[0], new string[0], "", toAdr);
                //}
                //else
                {

                    scApp.CMDBLL.doCreatTransferCommand(vehicleId, string.Empty, cst_id,
                                                    E_CMD_TYPE.Unload,
                                                    string.Empty,
                                                    toAdr, 0, 0);
                }
                check_result_info = sc.BLL.CMDBLL.getCallContext<sc.BLL.CMDBLL.OHTCCommandCheckResult>
                     (sc.BLL.CMDBLL.CALL_CONTEXT_KEY_WORD_OHTC_CMD_CHECK_RESULT);
            });
            if (check_result_info != null && !check_result_info.IsSuccess)
            {
                MessageBox.Show(check_result_info.ToString(), "Command create fail.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //bcf.App.BCFApplication.onWarningMsg(this, new LogEventArgs("Command create fail.", check_result_info.Num));
            }
        }




        private void excuteTeachingCommand()
        {
            string vh_id = cmb_Vehicle.Text.Trim();
            string from_adr = cmb_fromAddress.Text;
            string to_adr = cmb_toAddress.Text;

            Task.Run(() => { scApp.VehicleService.TeachingRequest(vh_id, from_adr, to_adr); });

        }
        private void excuteHomeCommand()
        {
            string toAdr = string.Empty;
            string vehicleId = string.Empty;

            vehicleId = cmb_Vehicle.Text;
            toAdr = cmb_toAddress.Text;
            if (BCFUtility.isEmpty(vehicleId))
            {
                MessageBox.Show("No find vehile.");
                return;
            }

            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vehicleId);

            Task.Run(() =>
            scApp.VehicleService.TransferRequset(vehicleId, scApp.SequenceBLL.getCommandID(SCAppConstants.GenOHxCCommandType.Manual), ActiveType.Home, "", new string[0], new string[0], new string[0], new string[0], new string[0], new string[0], "", ""));
            //vh.sned_Str31(scApp.SequenceBLL.getCommandID_Manual(), ActiveType.Home, "", new string[0], new string[0], "", ""));
        }

        private void excuteMTLHomeCommand()
        {
            string toAdr = string.Empty;
            string vehicleId = string.Empty;

            vehicleId = cmb_Vehicle.Text;
            toAdr = cmb_toAddress.Text;
            if (BCFUtility.isEmpty(vehicleId))
            {
                MessageBox.Show("No find vehile.");
                return;
            }

            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vehicleId);

            Task.Run(() =>
            scApp.VehicleService.TransferRequset(vehicleId, scApp.SequenceBLL.getCommandID(SCAppConstants.GenOHxCCommandType.Manual), ActiveType.Mtlhome, "", new string[0], new string[0], new string[0], new string[0], new string[0], new string[0], "", ""));
            //vh.sned_Str31(scApp.SequenceBLL.getCommandID_Manual(), ActiveType.Home, "", new string[0], new string[0], "", ""));
        }
        //private string getAddressID(string adr_port_id)
        //{
        //    E_VH_TYPE vh_type = E_VH_TYPE.None;
        //    return getAddressID(adr_port_id, out vh_type);
        //}
        //private string getAddressID(string adr_port_id, out E_VH_TYPE vh_type)
        //{
        //    string Adr_ID = string.Empty;
        //    if (Raid_PortNameType_AdrID.Checked)
        //    {
        //        Adr_ID = adr_port_id;
        //        vh_type = E_VH_TYPE.None;
        //    }
        //    else if (Raid_PortNameType_PortID.Checked)
        //    {
        //        APORT port = scApp.MapBLL.getPortByPortID(adr_port_id);

        //        if (port != null)
        //        {
        //            Adr_ID = port.ADR_ID;
        //            vh_type = port.ULD_VH_TYPE;
        //        }
        //        else
        //        {
        //            vh_type = E_VH_TYPE.None;
        //        }
        //    }
        //    else
        //    {
        //        Adr_ID = adr_port_id;
        //        vh_type = E_VH_TYPE.None;
        //    }
        //    return Adr_ID;
        //}


        private void timer_TimedUpdates_Tick(object sender, EventArgs e)
        {
            //using (DBConnection_EF context = new DBConnection_EF())
            //{
            //    dgv_vhStatus.DataSource = context.AVEHICLE.ToList();
            //}
            //if (currentSelectIndex != -1)
            //    dgv_vhStatus.Rows[currentSelectIndex].Selected = true;
            dgv_vhStatus.Refresh();
            var line = mainform.BCApp.SCApplication.getEQObjCacheManager().getLine();
            SetSCState(line);
            SetHostControlState(line.Host_Control_State);

            updateTransferCommand();

        }
        private void updateTransferCommand()
        {
            List<ACMD_MCS> cmd_mcs_lst = scApp.CMDBLL.loadACMD_MCSIsUnfinished();

            //cmd_mcs_obj_to_show = mainform.BCApp.SCApplication.CMDBLL.loadACMD_MCSIsUnfinishedObjToShow();
            cmd_mcs_obj_to_show = cmd_mcs_lst.
                Select(cmd_mcs => new CMD_MCSObjToShow(scApp.VehicleBLL, scApp.PortStationBLL, cmd_mcs)).
                ToList();
            cmsMCS_bindingSource.DataSource = cmd_mcs_obj_to_show;
            dgv_TransferCommand.Refresh();
        }

        int currentSelectIndex = -1;
        //Equipment InObservationVh = null;
        AVEHICLE InObservationVh = null;
        string predictPathHandler = "predictPathHandler";
        private void dgv_vhStatus_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                currentSelectIndex = e.RowIndex;
                string vh_id = dgv_vhStatus.Rows[currentSelectIndex].Cells[0].Value as string;

                setMonitorVehicle(vh_id);
            }
        }

        public void setMonitorVehicle(string vh_id)
        {
            lock (predictPathHandler)
            {
                if (InObservationVh != null)
                    InObservationVh.removeEventHandler(predictPathHandler);

                resetSpecifyRail();
                resetSpecifyAdr();
                if (!BCFUtility.isEmpty(vh_id))
                {
                    InObservationVh = scApp.getEQObjCacheManager().getVehicletByVHID(vh_id);

                    changePredictPathByInObservation();

                    InObservationVh.addEventHandler(predictPathHandler
                                        , BCFUtility.getPropertyName(() => InObservationVh.VhExcuteCMDStatusChangeEvent)
                                        , (s1, e1) => { changePredictPathByInObservation(); });
                    InObservationVh.addEventHandler(predictPathHandler
                                        , BCFUtility.getPropertyName(() => InObservationVh.VhStatusChangeEvent)
                                        , (s1, e1) => { changePredictPathByInObservation(); });

                    cmb_Vehicle.Text = vh_id;
                    VehicleObjToShow veicleObjShow = scApp.getEQObjCacheManager().CommonInfo.ObjectToShow_list.Where(o => o.VEHICLE_ID == vh_id).FirstOrDefault();
                    if (veicleObjShow != null)
                    {
                        int selectIndex = scApp.getEQObjCacheManager().CommonInfo.ObjectToShow_list.IndexOf(veicleObjShow);
                        if (selectIndex >= 0)
                        {
                            if ((dgv_vhStatus.SelectedRows.Count > 0 && dgv_vhStatus.SelectedRows[0].Index != selectIndex) ||
                                dgv_vhStatus.SelectedRows.Count == 0)
                            {
                                dgv_vhStatus.Rows[selectIndex].Selected = true;
                                dgv_vhStatus.FirstDisplayedScrollingRowIndex = selectIndex;
                            }
                        }
                    }
                }
                else
                {
                    if (dgv_vhStatus.SelectedRows.Count > 0)
                        dgv_vhStatus.SelectedRows[0].Selected = false;
                    cmb_Vehicle.Text = string.Empty;

                }
            }
        }

        bool setCombBoxFlag = false;
        public void setAdrCombobox(string adr_id)
        {
            if (setCombBoxFlag)
            {
                cmb_fromAddress.Text = adr_id;
                setCombBoxFlag = false;
            }
            else
            {
                cmb_toAddress.Text = adr_id;
                setCombBoxFlag = true;
            }
        }


        public void entryMonitorMode()
        {
            setMonitorVehicle(string.Empty);
            Dictionary<string, double> dicSecIDAndAvgPingTime = scApp.NetWorkQualityBLL.loadGroupBySecIDAndAvgPingTime();
            foreach (KeyValuePair<string, double> keyValue in dicSecIDAndAvgPingTime)
            {
                if (keyValue.Value > 100)
                {
                    uctl_Map.changeSpecifyRailColor(keyValue.Key, Color.Red);
                }
                else if (keyValue.Value > 60)
                {
                    uctl_Map.changeSpecifyRailColor(keyValue.Key, Color.Orange);
                }
                else
                {
                    uctl_Map.changeSpecifyRailColor(keyValue.Key, Color.LightGreen);
                }
            }
            uctl_Map.DisplaySectionLables(true);
            preSelectionSec = dicSecIDAndAvgPingTime.Keys.ToArray();
        }
        public void LeaveMonitorMode()
        {
            resetSpecifyRail();
            uctl_Map.DisplaySectionLables(false);
        }

        public async void entryMonitorMode_SectionThroughTimesAsync()
        {
            setMonitorVehicle(string.Empty);
            Dictionary<string, int> dicSecIDAndThroughTimes = null;
            await Task.Run(() => dicSecIDAndThroughTimes = scApp.MapBLL.loadGroupBySecAndThroughTimes());
            //string color_Rad = System.Drawing.ColorTranslator.FromHtml("#FF0000");
            //string color_Y = System.Drawing.ColorTranslator.FromHtml("#FF0000");
#pragma warning disable CS0219 // 已指派變數 'totalHour'，但是從未使用過它的值。
            int totalHour = 24;
#pragma warning restore CS0219 // 已指派變數 'totalHour'，但是從未使用過它的值。
            ck_montor_vh.Checked = false;
            uctl_Map.entryMonitorMode();
            foreach (KeyValuePair<string, int> keyValue in dicSecIDAndThroughTimes)
            {
                //int2colorTranfer(keyValue.Value);

                double perHourPassTimes = keyValue.Value;
                Color changeColor = Color.Empty;
                if (perHourPassTimes > BCAppConstants.SEC_THROUGH_TIMES_LV9)
                {
                    changeColor = BCAppConstants.SEC_THROUGH_COLOR_LV10;
                }
                else if (perHourPassTimes > BCAppConstants.SEC_THROUGH_TIMES_LV8)
                {
                    changeColor = BCAppConstants.SEC_THROUGH_COLOR_LV9;
                }
                else if (perHourPassTimes > BCAppConstants.SEC_THROUGH_TIMES_LV7)
                {
                    changeColor = BCAppConstants.SEC_THROUGH_COLOR_LV8;
                }
                else if (perHourPassTimes > BCAppConstants.SEC_THROUGH_TIMES_LV6)
                {
                    changeColor = BCAppConstants.SEC_THROUGH_COLOR_LV7;
                }
                else if (perHourPassTimes > BCAppConstants.SEC_THROUGH_TIMES_LV5)
                {
                    changeColor = BCAppConstants.SEC_THROUGH_COLOR_LV6;
                }
                else if (perHourPassTimes > BCAppConstants.SEC_THROUGH_TIMES_LV4)
                {
                    changeColor = BCAppConstants.SEC_THROUGH_COLOR_LV5;
                }
                else if (perHourPassTimes > BCAppConstants.SEC_THROUGH_TIMES_LV3)
                {
                    changeColor = BCAppConstants.SEC_THROUGH_COLOR_LV4;
                }
                else if (perHourPassTimes > BCAppConstants.SEC_THROUGH_TIMES_LV2)
                {
                    changeColor = BCAppConstants.SEC_THROUGH_COLOR_LV3;
                }
                else if (perHourPassTimes > BCAppConstants.SEC_THROUGH_TIMES_LV1)
                {
                    changeColor = BCAppConstants.SEC_THROUGH_COLOR_LV2;
                }
                else
                {
                    changeColor = BCAppConstants.SEC_THROUGH_COLOR_LV1;
                }
                uctl_Map.changeSpecifyRailColor(keyValue.Key, changeColor);


                //if (perHourPassTimes > 30)
                //{
                //    uctl_Map1.changeSpecifyRailColor(keyValue.Key, Color.Red);
                //}
                //else if (perHourPassTimes > 11)
                //{
                //    uctl_Map1.changeSpecifyRailColor(keyValue.Key, Color.Yellow);
                //}
                //else
                //{
                //    Color color_Green = System.Drawing.ColorTranslator.FromHtml("#00FF00");
                //    uctl_Map1.changeSpecifyRailColor(keyValue.Key, color_Green);
                //}
            }
            //uctl_Map1.DisplaySectionLables(true);
            preSelectionSec = dicSecIDAndThroughTimes.Keys.ToArray();
        }
        private string int2colorTranfer(int passTimes)
        {
            double Denominator = 5000;
            double percentage = (passTimes / Denominator) * 100;
            double radPercen = percentage;
            double greenPercen = 100 - percentage;
            double radColorScale = 255 * (radPercen / 100);
            double greenColorScale = 255 * (greenPercen / 100);
            string sRadColorScale = Convert.ToString((int)radColorScale, 16);
            string sGreenColorScale = Convert.ToString((int)greenColorScale, 16).ToUpper();
            string colorCoding = string.Format("#{0}{1}00", sRadColorScale, sGreenColorScale);
            return colorCoding;
        }

        public void LeaveMonitorMode_SectionThroughTimes()
        {
            ck_montor_vh.Checked = true;
            uctl_Map.LeaveMonitorMode();
            resetSpecifyRail();
            //uctl_Map1.DisplaySectionLables(false);
        }

        public void entrySegmentSetMode(EventHandler eventHandler)
        {
            setMonitorVehicle(string.Empty);
            uctl_Map.RegistRailSelectedEvent(eventHandler);
        }
        public void LeaveSegmentSetMode(EventHandler eventHandler)
        {
            uctl_Map.RemoveRailSelectedEvent(eventHandler);
        }
        public void SetSpecifySegmentSelected(string seg_num, Color set_color)
        {
            uctl_Map.changeSpecifyRailColorBySegNum(seg_num, set_color);
        }
        public void ResetSpecifySegmentSelected(string seg_num)
        {
            uctl_Map.resetRailColor(seg_num);
        }

        public void ResetAllSegment()
        {
            uctl_Map.resetAllRailColor();
        }


        private void changePredictPathByInObservation()
        {
            //resetSpecifyRail();
            //resetSpecifyAdr();

            if (InObservationVh.ACT_STATUS == VHActionStatus.CycleRun)
            {
                resetSpecifyRail();
                resetSpecifyAdr();
                //setSpecifyRail(InObservationVh.CyclingPath);
            }
            else
            {
                if (InObservationVh.vh_CMD_Status < E_CMD_STATUS.NormalEnd)
                {
                    setSpecifyRail(InObservationVh.PredictSections);
                    setSpecifyAdr();
                }
                else
                {
                    resetSpecifyRail();
                    resetSpecifyAdr();
                }
            }
        }

        string reqSelectionStartAdr = string.Empty;
        string reqSelectionFromAdr = string.Empty;
        string reqSelectionToAdr = string.Empty;
        private void setSpecifyAdr()
        {
            if (!BCFUtility.isEmpty(InObservationVh.startAdr))
            {
                uctl_Map.changeSpecifyAddressColor
                    (InObservationVh.startAdr, BCAppConstants.CLR_MAP_ADDRESS_START);
                reqSelectionStartAdr = InObservationVh.startAdr;
            }
            if (!BCFUtility.isEmpty(InObservationVh.FromAdr))
            {
                uctl_Map.changeSpecifyAddressColor
                    (InObservationVh.FromAdr, BCAppConstants.CLR_MAP_ADDRESS_FROM);
                reqSelectionFromAdr = InObservationVh.FromAdr;

            }
            if (!BCFUtility.isEmpty(InObservationVh.ToAdr))
            {
                uctl_Map.changeSpecifyAddressColor
                    (InObservationVh.ToAdr, BCAppConstants.CLR_MAP_ADDRESS_TO);
                reqSelectionToAdr = InObservationVh.ToAdr;
            }
        }
        private void resetSpecifyAdr()
        {
            if (!BCFUtility.isEmpty(reqSelectionStartAdr))
            {
                uctl_Map.changeSpecifyAddressColor
                    (reqSelectionStartAdr, BCAppConstants.CLR_MAP_ADDRESS_DUFAULT);
                reqSelectionStartAdr = string.Empty;
            }
            if (!BCFUtility.isEmpty(reqSelectionFromAdr))
            {
                uctl_Map.changeSpecifyAddressColor
                    (reqSelectionFromAdr, BCAppConstants.CLR_MAP_ADDRESS_DUFAULT);
                reqSelectionFromAdr = string.Empty;

            }
            if (!BCFUtility.isEmpty(reqSelectionToAdr))
            {
                uctl_Map.changeSpecifyAddressColor
                    (reqSelectionToAdr, BCAppConstants.CLR_MAP_ADDRESS_DUFAULT);
                reqSelectionToAdr = string.Empty;
            }
        }

        string[] preSelectionSec = null;
        public void setSpecifyRail(string[] spacifyPath)
        {
            if (spacifyPath == null)
                return;
            if (isMatche(preSelectionSec, spacifyPath))
                return;
            else
            {
                resetSpecifyRail();
                resetSpecifyAdr();
            }
            preSelectionSec = spacifyPath;
            uctl_Map.changeSpecifyRailColor(spacifyPath);
        }
        private void resetSpecifyRail()
        {
            if (preSelectionSec != null)
                uctl_Map.resetRailColor(preSelectionSec);
            preSelectionSec = null;
        }
        private bool isMatche(string[] list1, string[] list2)
        {
            if (list1 == null || list2 == null) return false;
            if (list1.Count() != list2.Count()) return false;
            //List1 跟 List2取交集，如果交集的數量與List1(或Lsit2)一樣，代表兩個的List是一樣的
            var intersectedList = list1.Intersect(list2);
            if (intersectedList.Count() == list1.Count()) return true;
            return false;
        }


        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;  // Turn on WS_EX_COMPOSITED
                return cp;
            }
        }

        private void dgv_vhStatus_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            logger.Error(e.Exception, "Exception");
            //todo error catch
        }

        private void btn_continuous_Click(object sender, EventArgs e)
        {
            //Equipment noticeCar = scApp.getEQObjCacheManager().getEquipmentByEQPTID(cmb_Vehicle.Text.Trim());
            string vh_id = cmb_Vehicle.Text.Trim();
            Task.Run(() =>
            {
                AVEHICLE noticeCar = scApp.getEQObjCacheManager().getVehicletByVHID(vh_id);

                if (noticeCar.IsPause)
                {
                    scApp.VehicleService.PauseRequest(vh_id, PauseEvent.Continue, SCAppConstants.OHxCPauseType.Normal);
                    //noticeCar.sned_Str39(PauseEvent.Continue, PauseType.OhxC);
                }
                else
                {
                    scApp.VehicleBLL.noticeVhPass(vh_id);
                }
            });
        }

        private void btn_pause_Click(object sender, EventArgs e)
        {
            //Equipment noticeCar = scApp.getEQObjCacheManager().getEquipmentByEQPTID(cmb_Vehicle.Text.Trim());
            //AVEHICLE noticeCar = scApp.getEQObjCacheManager().getVehicletByVHID(cmb_Vehicle.Text.Trim());
            string notice_vh_id = cmb_Vehicle.Text.Trim();
            Task.Run(() =>
            {
                //if (noticeCar.sned_Str39(PauseEvent.Pause, PauseType.OhxC))
                if (scApp.VehicleService.PauseRequest(notice_vh_id, PauseEvent.Pause, SCAppConstants.OHxCPauseType.Normal))
                {

                }
            });
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            mainform.isAutoOpenTip = cb_autoTip.Checked;
        }

        private void cbm_Action_SelectedIndexChanged(object sender, EventArgs e)
        {
            cmb_fromAddress.Enabled = false;
            cmb_toAddress.Visible = false;
            cmb_cycRunZone.Visible = false;
            btn_start.Enabled = false;
            dgv_TaskCommand.Enabled = false;
            lbl_destinationName.Text = "To Address";
            E_CMD_TYPE cmd_type;
            Enum.TryParse<E_CMD_TYPE>(cbm_Action.SelectedValue.ToString(), out cmd_type);
            switch (cmd_type)
            {
                case E_CMD_TYPE.Move:
                case E_CMD_TYPE.Move_Charger:
                    cmb_toAddress.Visible = true;
                    btn_start.Enabled = true;
                    dgv_TaskCommand.Enabled = true;
                    break;
                case E_CMD_TYPE.Round:
                    cmb_cycRunZone.Visible = true;
                    btn_start.Enabled = true;
                    lbl_destinationName.Text = "Round Entry Adr.";
                    break;
                case E_CMD_TYPE.LoadUnload:
                    cmb_fromAddress.Enabled = true;
                    cmb_toAddress.Visible = true;
                    btn_start.Enabled = true;
                    dgv_TaskCommand.Enabled = true;
                    break;
                case E_CMD_TYPE.Teaching:
                    cmb_fromAddress.Enabled = true;
                    cmb_toAddress.Visible = true;
                    btn_start.Enabled = true;
                    break;
                case E_CMD_TYPE.Home:
                case E_CMD_TYPE.MTLHome:
                    btn_start.Enabled = true;
                    break;
                case E_CMD_TYPE.Load:
                    cmb_fromAddress.Enabled = true;
                    btn_start.Enabled = true;
                    break;
                case E_CMD_TYPE.Unload:
                    cmb_toAddress.Visible = true;
                    btn_start.Enabled = true;
                    break;
            }
        }

        private void Raid_PortNameType_CheckedChanged(object sender, EventArgs e)
        {
            string source_name = string.Empty;
            string destination_name = string.Empty;
            if (Raid_PortNameType_AdrID.Checked)
            {
                source_name = "From Address";
                destination_name = "To Address";
                BCUtility.setComboboxDataSource(cmb_toAddress, allAdr_ID);
                BCUtility.setComboboxDataSource(cmb_fromAddress, allAdr_ID.ToArray());
            }
            else if (Raid_PortNameType_PortID.Checked)
            {
                source_name = "From Port";
                destination_name = "To Port";
                BCUtility.setComboboxDataSource(cmb_toAddress, allPortID);
                BCUtility.setComboboxDataSource(cmb_fromAddress, allPortID.ToArray());
            }
            lbl_sourceName.Text = source_name;
            lbl_destinationName.Text = destination_name;
        }

        private void ck_montor_vh_CheckedChanged(object sender, EventArgs e)
        {
            if (ck_montor_vh.Checked)
            {
                uctl_Map.trunOnMonitorAllVhStatus();
            }
            else
            {
                uctl_Map.trunOffMonitorAllVhStatus();
            }
        }

        private void cb_sectionThroughTimes_Click(object sender, EventArgs e)
        {
            if (cb_sectionThroughTimes.Checked)
            {
                entryMonitorMode_SectionThroughTimesAsync();
            }
            else
            {
                LeaveMonitorMode_SectionThroughTimes();
            }

        }


        private void OHT_Form_Load(object sender, EventArgs e)
        {
            ck_montor_vh.Checked = true;
            dgv_TransferCommand.DataSource = cmsMCS_bindingSource;
        }


        private void cb_sectionThroughTimes_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void btn_parkZoneTypeChange_Click(object sender, EventArgs e)
        {
            string selected_park_zone_type = cb_parkZoneType.SelectedItem as string;
            if (selected_park_zone_type == null) return;
            scApp.ParkBLL.doParkZoneTypeChange(selected_park_zone_type);
        }


        private void uctl_Map_Load(object sender, EventArgs e)
        {

        }

        private void cmb_fromSection_SelectedIndexChanged(object sender, EventArgs e)
        {
            //if (!SCUtility.isEmpty(cmb_Vehicle.Text))
            //    scApp.getEQObjCacheManager().getVehicletByVHID(cmb_Vehicle.Text).CUR_SEC_ID = cmb_fromSection.Text;
        }

        private async void btn_Avoid_Click(object sender, EventArgs e)
        {
            string vehicle_id = cmb_Vehicle.Text;
            string avoid_adr = cmb_toAddress.Text;
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vehicle_id);
            if (vh == null)
            {
                MessageBox.Show("No find vehile.");
                return;
            }
            if (BCFUtility.isEmpty(avoid_adr))
            {
                MessageBox.Show("No find avoid address.");
                return;
            }
            (bool is_success, string result, List<string> guide_address_ids) resule = default((bool is_success, string result, List<string> guide_address_ids));
            await Task.Run(() =>
            {
                resule = scApp.VehicleService.AvoidRequest(vehicle_id, avoid_adr);
            });
            if (resule.is_success)
            {
                MessageBox.Show("Avoid success", "Avoid success.", MessageBoxButtons.OK, MessageBoxIcon.None);
            }
            else
            {
                MessageBox.Show(resule.result, "Avoid fail.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void checkBox1_CheckedChanged_1(object sender, EventArgs e)
        {
        }

        private void cb_autoOverride_Click(object sender, EventArgs e)
        {
            com.mirle.ibg3k0.sc.App.SystemParameter.setAutoOverride(cb_autoOverride.Checked);
        }

        private void SystemParameter_AutoOverrideChange(object sender, bool e)
        {
            Adapter.Invoke((obj) =>
            {
                cb_autoOverride.Checked = e;
                btn_Avoid.Enabled = !e;
            }, null);
        }

    }
}