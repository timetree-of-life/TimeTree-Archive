using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TopoTimeShared;

namespace TopoTime
{
    public partial class DebugForm : Form
    {
        public object returnValue;

        public DebugForm(Object data, string title = "Debug")
        {
            InitializeComponent();
            this.Text = title;
            dataGridView1.DataSource = data;
            returnValue = null;
        }

        public DebugForm(Object data, int selectIndex, bool multiSelect = false)
        {
            InitializeComponent();
            dataGridView1.DataSource = data;
            returnValue = null;
            dataGridView1.MultiSelect = multiSelect;
            if (dataGridView1.Rows.Count > 0)
                dataGridView1.Rows[selectIndex].Selected = true;
        }

        private void dataGridView1_CellEnter(object sender, DataGridViewCellEventArgs e)
        {
            for (int i = 0; i < dataGridView1.Rows[e.RowIndex].Cells.Count; i++)
            {
                if (dataGridView1.Rows[e.RowIndex].Cells[i].Value == null)
                    continue;

                Type type = dataGridView1.Rows[e.RowIndex].Cells[i].Value.GetType();
                if (type == typeof(TopoTimeNode))
                {
                    TopoTimeNode selectedNode = (TopoTimeNode)dataGridView1.Rows[e.RowIndex].Cells[i].Value;

                    selectedNode.EnsureVisible();
                    selectedNode.TreeView.SelectedNode = selectedNode;
                    selectedNode.TreeView.Select();
                }
            }
        }

        private void dataGridView1_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            for (int i = 0; i < dataGridView1.Rows[e.RowIndex].Cells.Count; i++)
            {
                if (dataGridView1.Rows[e.RowIndex].Cells[i].Value == null)
                    continue;

                Type type = dataGridView1.Rows[e.RowIndex].Cells[i].Value.GetType();
                if (type == typeof(TopoTimeNode))
                {
                    TopoTimeNode selectedNode = (TopoTimeNode)dataGridView1.Rows[e.RowIndex].Cells[i].Value;
                    selectedNode.EnsureVisible();
                    selectedNode.TreeView.SelectedNode = selectedNode;
                    break;
                }
            }            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            returnValue = dataGridView1.SelectedRows[0].Index;
        }
    }
}
