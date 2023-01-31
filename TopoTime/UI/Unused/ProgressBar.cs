using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TopoTime
{
    public partial class ProgressBar : Form
    {
        private BackgroundWorker bgWorker;

        public ProgressBar(BackgroundWorker bgWorker)
        {
            InitializeComponent();
            this.bgWorker = bgWorker;
            this.bgWorker.ProgressChanged += new ProgressChangedEventHandler(progressChange);
            this.bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(closeWhenFinished);
            this.bgWorker.WorkerSupportsCancellation = true;
            this.bgWorker.WorkerReportsProgress = true;
        }

        public int ProgressPercentage
        {
            get { return progressBar1.Value; }
            set { progressBar1.Value = value; }
        }

        public string ProgressText
        {
            get { return lblProgressText.Text; }
            set { lblProgressText.Text = value; }
        }

        private void ProgressBar_Load(object sender, EventArgs e)
        {
            bgWorker.RunWorkerAsync();
        }

        private void progressChange(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
            lblProgressText.Text = e.UserState.ToString();
        }

        private void btnCancelJob_Click(object sender, EventArgs e)
        {
            bgWorker.CancelAsync();
        }

        private void closeWhenFinished(object sender, RunWorkerCompletedEventArgs e)
        {
            this.Close();
        }
    }
}
