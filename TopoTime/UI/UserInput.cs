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
    public partial class UserInput : Form
    {
        public string userInputString;

        public UserInput()
        {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            userInputString = textBox1.Text;
        }
    }
}
