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
    public partial class ResolveConstraints : Form
    {
        public double maxHeight;
        public double minHeight;

        public ResolveConstraints()
        {
            InitializeComponent();
        }

        private void btnResolve_Click(object sender, EventArgs e)
        {
            if (!double.TryParse(txtBoxMaxHeight.Text, out maxHeight))
                maxHeight = double.PositiveInfinity;

            if (!double.TryParse(txtBoxMinHeight.Text, out minHeight))
                minHeight = 0;
        }
    }
}
