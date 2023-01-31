using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TimeTreeShared;
using TopoTimeShared;

namespace TopoTime
{
    public partial class SubtaxaForm : Form
    {
        public SubtaxaForm()
        {
            InitializeComponent();
        }

        public void setList(List<TopoTimeNode> leafList, List<MyTuple<int, string>> subspeciesList)
        {
            listBox1.DataSource = leafList;
            listBox2.DataSource = subspeciesList;
            label2.Text = "Subtaxa List (" + leafList.Count + " taxa)";
        }

        public void setList(List<TopoTimeNode> leafList, IEnumerable<TopoTimeNode> namedNodes, Dictionary<TopoTimeNode, int> floatingNodes)
        {
            IEnumerable<TopoTimeNode> storedNodes = Enumerable.Empty<TopoTimeNode>();
            if (namedNodes != null)
                storedNodes = storedNodes.Concat(namedNodes);

            if (floatingNodes != null)
                storedNodes = storedNodes.Concat(floatingNodes.Keys);

            listBox1.DataSource = leafList;
            listBox2.DataSource = storedNodes.ToList();
            label2.Text = "Subtaxa List (" + leafList.Count + " taxa)";
        }

        private void Form4_FormClosing(object sender, FormClosingEventArgs e)
        {
            Hide();
        }

        private void listBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.C && e.Modifiers == Keys.Control)
            {
                try
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (object row in listBox1.Items)
                    {
                        sb.Append(row.ToString());
                        sb.AppendLine();
                    }
                    sb.Remove(sb.Length - 1, 1); // Just to avoid copying last empty row
                    Clipboard.SetData(System.Windows.Forms.DataFormats.Text, sb.ToString());
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
    }
}
