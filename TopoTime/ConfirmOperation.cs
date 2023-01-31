using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TopoTime
{
    public partial class ConfirmOperation : Form
    {
        public ConfirmOperation(string operationText, Object data)
        {
            InitializeComponent();
            btnConfirm.Text = btnConfirm.Text + operationText;
            dataGridView1.DataSource = data;
        }

        private void btnConfirm_Click(object sender, EventArgs e)
        {
            CSharpScriptEngine.Execute(textBox1.Text);
        }
    }
}
