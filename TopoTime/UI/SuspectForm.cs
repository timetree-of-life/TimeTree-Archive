using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using TopoTimeShared;

namespace TopoTime
{
    public partial class SuspectForm : Form
    {
        private List<TopoTimeNode> suspectList;
        private bool DisplayNamedNodesOnly { get; set; }
        public SuspectForm()
        {
            InitializeComponent();
            dgvSuspectList.AutoGenerateColumns = false;
        }

        public SuspectForm(List<Tuple<string, string>> columnList)
        {
            InitializeComponent();
            dgvSuspectList.AutoGenerateColumns = false;
            dgvSuspectList.Columns.Clear();

            foreach (Tuple<string, string> pair in columnList)
            {
                dgvSuspectList.Columns.Add("clmAuto" + pair.Item1, pair.Item2);
            }
            
        }

        public void setList(List<TopoTimeNode> suspectList)
        {
            this.suspectList = suspectList;
            dgvSuspectList.DataSource = this.suspectList;

            toolStripLabel1.Text = suspectList.Count + " listed nodes.";
        }

        private void dgvSuspectList_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            TopoTimeNode selectedNode = (TopoTimeNode)dgvSuspectList.Rows[e.RowIndex].DataBoundItem;
            selectedNode.EnsureVisible();
            selectedNode.TreeView.SelectedNode = selectedNode;
        }

        private void btnAllSuspects_Click(object sender, EventArgs e)
        {
            List<TopoTimeNode> suspects = suspectList.Where(arg => (!DisplayNamedNodesOnly || arg.TaxonID > 0)).ToList();

            dgvSuspectList.DataSource = suspects;
            toolStripLabel1.Text = suspectList.Count + " listed nodes.";
        }

        private void btnRearrangedNodes_Click(object sender, EventArgs e)
        {
            List<TopoTimeNode> suspects = suspectList.FindAll(
                delegate(TopoTimeNode arg)
                {
                    return arg.rearranged && (!DisplayNamedNodesOnly || arg.TaxonID > 0);
                });

            dgvSuspectList.DataSource = suspects;
            toolStripLabel1.Text = suspects.Count + " listed nodes.";
        }

        private void btnUnusualDivergence_Click(object sender, EventArgs e)
        {
            List<TopoTimeNode> suspects = suspectList.FindAll(
                delegate(TopoTimeNode arg)
                {
                    return (arg.DivergenceRatio >= 5) && (!DisplayNamedNodesOnly || arg.TaxonID > 0);
                });

            dgvSuspectList.DataSource = suspects;
            toolStripLabel1.Text = suspects.Count + " listed nodes.";
        }

        private void btnAmbiguousPartitions_Click(object sender, EventArgs e)
        {
            // for whatever reason, the C# parser does NOT stop processing the Boolean statement after finding arg.PartitionData is null
            // so this is being left as is
            // ¯\_(ツ)_/¯
            List<TopoTimeNode> suspects = suspectList.FindAll(
                delegate(TopoTimeNode arg)
                {
                    return arg.PartitionData != null && (!DisplayNamedNodesOnly || arg.TaxonID > 0)/* && arg.PartitionData.FavorAB == arg.PartitionData.FavorAC && arg.PartitionData.FavorAC >= arg.PartitionData.FavorBC ||
                            arg.PartitionData.FavorAB == arg.PartitionData.FavorBC && arg.PartitionData.FavorBC >= arg.PartitionData.FavorAC*/;
                });

            dgvSuspectList.DataSource = suspects;
            toolStripLabel1.Text = suspects.Count + " listed nodes.";
        }

        private void btnUnsupportedPartitions_Click(object sender, EventArgs e)
        {
            List<TopoTimeNode> suspects = suspectList.FindAll(
                delegate(TopoTimeNode arg)
                {
                    return (arg.PartitionData != null && (arg.PartitionData.FavorAB < arg.PartitionData.FavorAC || arg.PartitionData.FavorAB < arg.PartitionData.FavorBC)) && (!DisplayNamedNodesOnly || arg.TaxonID > 0);
                });

            dgvSuspectList.DataSource = suspects;
            toolStripLabel1.Text = suspects.Count + " listed nodes.";
        }

        private void dgvSuspectList_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            foreach (DataGridViewRow row in ((DataGridView)sender).Rows)
            {
                row.HeaderCell.Value = String.Format("{0}", row.Index + 1);
            }
        }

        private void btnNegativeBranch_Click(object sender, EventArgs e)
        {
            List<TopoTimeNode> suspects = suspectList.FindAll(
                delegate(TopoTimeNode arg)
                {
                    return (arg.LevelsFromNegativeBranch > -1) && (!DisplayNamedNodesOnly || arg.TaxonID > 0);
                });

            dgvSuspectList.DataSource = suspects;
            toolStripLabel1.Text = suspects.Count + " listed nodes.";
        }

        private void btnZeroHeightParents_Click(object sender, EventArgs e)
        {
            List<TopoTimeNode> suspects = suspectList.FindAll(
                delegate(TopoTimeNode arg)
                {
                    return (arg.Nodes.Count > 0 && arg.getNodeHeight(true) == 0) && (!DisplayNamedNodesOnly || arg.TaxonID > 0);
                });

            dgvSuspectList.DataSource = suspects;
            toolStripLabel1.Text = suspects.Count + " listed nodes.";
        }

        private void chkBoxNamedNodes_CheckedChanged(object sender, EventArgs e)
        {
            DisplayNamedNodesOnly = chkBoxNamedNodes.Checked;
        }
    }
}
