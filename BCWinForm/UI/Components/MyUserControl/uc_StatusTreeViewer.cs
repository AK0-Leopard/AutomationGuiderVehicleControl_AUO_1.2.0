using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace com.mirle.ibg3k0.bc.winform.UI.Components.MyUserControl
{
    public partial class uc_StatusTreeViewer : UserControl
    {
        const string NODE_AGV_STATUS = "AGV";
        const string NODE_CHARGER_STATUS = "Charger";
        const string NODE_AGVC_STATUS = "AGVC";

        const int VH_STATUS_AUTO_LOCAL = 1;
        const int VH_STATUS_AUTO_REMOTE = 2;
        const int VH_STATUS_AUTO_MANUAL = 3;
        const int VH_STATUS_AUTO_POWERON = 4;
        const int VH_STATUS_AUTO_DISCONNECTION = 5;


        sc.App.SCApplication scApp = null;
        sc.ALINE line;
        public uc_StatusTreeViewer()
        {
            InitializeComponent();
        }
        public void start(sc.App.SCApplication _app)
        {
            scApp = _app;
            line = scApp.getEQObjCacheManager().getLine();
            ceratMainTreeNode();
        }
        private void ceratMainTreeNode()
        {

            treeView.Nodes.Clear();
            var vhs = scApp.VehicleBLL.cache.loadAllVh();
            TreeNode tn_agv_status = new TreeNode()
            {
                Name = NODE_AGV_STATUS,
                Text = $"{NODE_AGV_STATUS}:{vhs.Count}"
            };
            foreach (var vh in vhs)
            {
                string vh_id = vh.VEHICLE_ID;
                string vh_real_id = vh.Real_ID;
                string state = vh.State.ToString();
                TreeNode node = new TreeNode()
                {
                    Name = vh_id,
                    Text = $"{vh_real_id}:{state}",
                    Tag = vh,
                    ImageIndex = getVhStatus2ImageIndex(vh)
                };
                tn_agv_status.Nodes.Add(node);
            }
            var mCharger = scApp.getEQObjCacheManager().getEquipmentByEQPTID("MCharger");
            int charger_count = 0;
            if (mCharger != null)
            {
                foreach (sc.AUNIT unit in mCharger.UnitList)
                {
                    charger_count++;
                }
            }
            TreeNode tn_charger_status = new TreeNode()
            {
                Name = NODE_CHARGER_STATUS,
                Text = $"{NODE_CHARGER_STATUS}:{charger_count}"
            };
            foreach (var charger in mCharger.UnitList)
            {
                tn_charger_status.Nodes.Add(charger2TreeNodes(charger));
            }

            TreeNode tn_agvc_status = new TreeNode()
            {
                Name = NODE_AGVC_STATUS,
                Text = $"{NODE_AGVC_STATUS}:Control Mode"
            };
            tn_agvc_status.Nodes.Add(TSCState2TreeNode(line));
            tn_agvc_status.Nodes.Add(HostMode2TreeNode(line));
            tn_agvc_status.Nodes.Add(LinkStatus2TreeNode(line));

            treeView.Nodes.Add(tn_agv_status);
            treeView.Nodes.Add(tn_charger_status);
            treeView.Nodes.Add(tn_agvc_status);
        }
        const string COUPLER_1 = "Coupler1";
        const string COUPLER_2 = "Coupler2";
        const string COUPLER_3 = "Coupler3";
        private TreeNode charger2TreeNodes(sc.AUNIT unit)
        {
            TreeNode tNode = new TreeNode();
            int i = 0;
            string cherger_text = unit.UNIT_ID;
            //tNode = tNode.Nodes.Add(scenarioInfo.Name);
            tNode = tNode.Nodes.Add(cherger_text, cherger_text);
            tNode.Nodes.Add(COUPLER_1, $"{COUPLER_1}:{unit.coupler1Status_NORTH_INNOLUX}");
            tNode.Nodes.Add(COUPLER_2, $"{COUPLER_2}:{unit.coupler2Status_NORTH_INNOLUX}");
            tNode.Nodes.Add(COUPLER_3, $"{COUPLER_3}:{unit.coupler3Status_NORTH_INNOLUX}");
            tNode.Tag = unit;
            return tNode;
        }
        const string TSC_STATE = "TSCStats";
        private TreeNode TSCState2TreeNode(sc.ALINE line)
        {
            TreeNode tNode = new TreeNode();
            int i = 0;
            tNode = tNode.Nodes.Add(TSC_STATE, $"{TSC_STATE}:{line.SCStats}");
            return tNode;
        }
        const string HOST_MODE = "HostMode";
        private TreeNode HostMode2TreeNode(sc.ALINE line)
        {
            TreeNode tNode = new TreeNode();
            int i = 0;
            tNode = tNode.Nodes.Add(HOST_MODE, $"{HOST_MODE}:{line.Host_Control_State}");
            return tNode;
        }
        const string HOST_LINK_STATE = "LinkStatus";
        private TreeNode LinkStatus2TreeNode(sc.ALINE line)
        {
            TreeNode tNode = new TreeNode();
            int i = 0;
            tNode = tNode.Nodes.Add(HOST_LINK_STATE, $"{HOST_LINK_STATE}:{line.Secs_Link_Stat}");
            return tNode;
        }

        public void refresh()
        {
            var agv_node = treeView.Nodes[NODE_AGV_STATUS];
            foreach (var node_obj in agv_node.Nodes)
            {
                TreeNode node = node_obj as TreeNode;
                var vh = node.Tag as sc.AVEHICLE;
                string vh_real_id = vh.Real_ID;
                string state = vh.State.ToString();
                node.Text = $"{vh_real_id}:{state}";
                node.ImageIndex = getVhStatus2ImageIndex(vh);
            }

            var charger_node = treeView.Nodes[NODE_CHARGER_STATUS];
            foreach (var node_obj in charger_node.Nodes)
            {
                TreeNode node = node_obj as TreeNode;
                var charger = node.Tag as sc.AUNIT;
                node.Nodes[COUPLER_1].Text = $"{COUPLER_1}:{charger.coupler1Status_NORTH_INNOLUX}";
                node.Nodes[COUPLER_2].Text = $"{COUPLER_2}:{charger.coupler2Status_NORTH_INNOLUX}";
                node.Nodes[COUPLER_3].Text = $"{COUPLER_3}:{charger.coupler3Status_NORTH_INNOLUX}";
            }

            var agvc_status_node = treeView.Nodes[NODE_AGVC_STATUS];
            foreach (var node_obj in agvc_status_node.Nodes)
            {
                TreeNode node = node_obj as TreeNode;
                switch (node.Name)
                {
                    case TSC_STATE:
                        node.Text = $"{TSC_STATE}:{line.SCStats}";
                        break;
                    case HOST_MODE:
                        node.Text = $"{HOST_MODE}:{line.Host_Control_State}";
                        break;
                    case HOST_LINK_STATE:
                        node.Text = $"{HOST_LINK_STATE}:{line.Secs_Link_Stat}";
                        break;
                }
            }
        }
        private int getVhStatus2ImageIndex(sc.AVEHICLE vh)
        {
            if (!vh.isTcpIpConnect)
            {
                return VH_STATUS_AUTO_DISCONNECTION;
            }
            else
            {
                switch (vh.MODE_STATUS)
                {
                    case sc.ProtocolFormat.OHTMessage.VHModeStatus.AutoCharging:
                    case sc.ProtocolFormat.OHTMessage.VHModeStatus.AutoLocal:
                        return VH_STATUS_AUTO_LOCAL;
                    case sc.ProtocolFormat.OHTMessage.VHModeStatus.AutoRemote:
                        return VH_STATUS_AUTO_REMOTE;
                    case sc.ProtocolFormat.OHTMessage.VHModeStatus.Manual:
                        return VH_STATUS_AUTO_MANUAL;
                    default:
                        return VH_STATUS_AUTO_POWERON;
                }
            }
        }
    }
}
