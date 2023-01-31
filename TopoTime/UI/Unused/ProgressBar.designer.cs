namespace TopoTime
{
    partial class ProgressBar
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.btnCancelJob = new System.Windows.Forms.Button();
            this.lblProgressText = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(12, 12);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(390, 23);
            this.progressBar1.TabIndex = 0;
            // 
            // btnCancelJob
            // 
            this.btnCancelJob.Location = new System.Drawing.Point(12, 85);
            this.btnCancelJob.Name = "btnCancelJob";
            this.btnCancelJob.Size = new System.Drawing.Size(75, 23);
            this.btnCancelJob.TabIndex = 1;
            this.btnCancelJob.Text = "Cancel";
            this.btnCancelJob.UseVisualStyleBackColor = true;
            this.btnCancelJob.Click += new System.EventHandler(this.btnCancelJob_Click);
            // 
            // lblProgressText
            // 
            this.lblProgressText.AutoSize = true;
            this.lblProgressText.Location = new System.Drawing.Point(13, 42);
            this.lblProgressText.Name = "lblProgressText";
            this.lblProgressText.Size = new System.Drawing.Size(0, 13);
            this.lblProgressText.TabIndex = 2;
            // 
            // ProgressBar
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(412, 113);
            this.ControlBox = false;
            this.Controls.Add(this.lblProgressText);
            this.Controls.Add(this.btnCancelJob);
            this.Controls.Add(this.progressBar1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ProgressBar";
            this.ShowIcon = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.TopMost = true;
            this.Load += new System.EventHandler(this.ProgressBar_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Button btnCancelJob;
        private System.Windows.Forms.Label lblProgressText;
    }
}