using com.mirle.ibg3k0.sc;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.ObjectRelay;
using com.mirle.ibg3k0.sc.ProtocolFormat.OHTMessage;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace com.mirle.ibg3k0.bc.winform.UI
{
    public partial class TransferCommandQureyListForm : Form
    {
        BCMainForm mainform;
        BindingSource cmsMCS_bindingSource = new BindingSource();
        List<CMD_MCSObjToShow> cmdMCSshowList = null;
        int selection_index = -1;
        public TransferCommandQureyListForm(BCMainForm _mainForm)
        {
            InitializeComponent();
            dgv_TransferCommand.AutoGenerateColumns = false;
            mainform = _mainForm;

            dgv_TransferCommand.DataSource = cmsMCS_bindingSource;
        }

        private void updateTransferCommand()
        {
            var mcs_commands = mainform.BCApp.SCApplication.CMDBLL.loadACMD_MCSIsUnfinished();
            cmdMCSshowList = mcs_commands.
                Select(mcs_cmd => new CMD_MCSObjToShow(mainform.BCApp.SCApplication.PortStationBLL, mcs_cmd)).
                ToList();
            cmsMCS_bindingSource.DataSource = cmdMCSshowList;
            dgv_TransferCommand.Refresh();
        }

        private void btn_refresh_Click(object sender, EventArgs e)
        {
            selection_index = -1;
            updateTransferCommand();
        }

        private async void btn_cancel_abort_Click(object sender, EventArgs e)
        {
            try
            {
                if (selection_index == -1) return;
                btn_cancel_abort.Enabled = false;
                var mcs_cmd = cmdMCSshowList[selection_index];
                CMDCancelType cnacel_type = default(CMDCancelType);
                if (mcs_cmd.TRANSFERSTATE < sc.E_TRAN_STATUS.Transferring)
                {
                    cnacel_type = CMDCancelType.CmdCancel;
                }
                else if (mcs_cmd.TRANSFERSTATE < sc.E_TRAN_STATUS.Canceling)
                {
                    cnacel_type = CMDCancelType.CmdAbort;
                }
                else
                {
                    MessageBox.Show($"Command ID:{mcs_cmd.CMD_ID.Trim()} can't excute cancel / abort,\r\ncurrent state:{mcs_cmd.TRANSFERSTATE}", "Cancel / Abort command fail.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                await Task.Run(() => mainform.BCApp.SCApplication.VehicleService.doCancelOrAbortCommandByMCSCmdID(mcs_cmd.CMD_ID, cnacel_type));
                updateTransferCommand();
            }
            catch { }
            finally
            {
                btn_cancel_abort.Enabled = true;
            }
        }

        private void dgv_TransferCommand_SelectionChanged(object sender, EventArgs e)
        {
            if (dgv_TransferCommand.SelectedRows.Count > 0)
                selection_index = dgv_TransferCommand.SelectedRows[0].Index;
        }

        private async void btn_force_finish_Click(object sender, EventArgs e)
        {
            try
            {
                if (selection_index == -1) return;
                btn_force_finish.Enabled = false;
                var mcs_cmd = cmdMCSshowList[selection_index];

                AVEHICLE excute_cmd_of_vh = mainform.BCApp.SCApplication.VehicleBLL.cache.getVehicleByMCSCmdID(mcs_cmd.CMD_ID);
                //if (excute_cmd_of_vh != null)
                //{
                //    MessageBox.Show($"Command ID:{mcs_cmd.CMD_ID.Trim()} can't excute force finish,\r\nvh id:{excute_cmd_of_vh.VEHICLE_ID} in excute.", "Force finish fail.",
                //                     MessageBoxButtons.OK,
                //                     MessageBoxIcon.Warning);
                //    return;
                //}

                await Task.Run(() =>
                {
                    try
                    {
                        if (excute_cmd_of_vh != null)
                        {
                            mainform.BCApp.SCApplication.VehicleBLL.doTransferCommandFinish(excute_cmd_of_vh.VEHICLE_ID, excute_cmd_of_vh.OHTC_CMD, CompleteStatus.CmpStatusForceFinishByOp, 0);
                            mainform.BCApp.SCApplication.VIDBLL.initialVIDCommandInfo(excute_cmd_of_vh.VEHICLE_ID);
                        }
                        mainform.BCApp.SCApplication.CMDBLL.updateCMD_MCS_TranStatus2Complete(mcs_cmd.CMD_ID, E_TRAN_STATUS.Canceled);
                        mainform.BCApp.SCApplication.ReportBLL.newReportTransferCommandFinish(mcs_cmd.cmd_mcs, excute_cmd_of_vh, sc.Data.SECS.AGVC.SECSConst.CMD_Result_Unsuccessful, null);
                    }
                    catch { }
                }
                );
                updateTransferCommand();
            }
            catch { }
            finally
            {
                btn_force_finish.Enabled = true;
            }
        }

        private void TransferCommandQureyListForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            mainform.removeForm(this.Name);
        }
    }
}
