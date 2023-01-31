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
    public partial class UserInputForm : Form
    {
        public double newValue;

        public UserInputForm(string initialValue, string promptText = "Enter the new value:")
        {
            InitializeComponent();
            textBox1.Text = initialValue;
            label1.Text = promptText;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (Double.TryParse(textBox1.Text, out newValue))
                buttonOK.Enabled = true;
            else
                buttonOK.Enabled = false;
        }

        private void frmEditHeight_FormClosing(object sender, FormClosingEventArgs e)
        {
            
        }
    }
}
