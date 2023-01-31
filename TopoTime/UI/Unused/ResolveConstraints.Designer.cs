namespace TopoTime
{
    partial class ResolveConstraints
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
            this.txtBoxMaxHeight = new System.Windows.Forms.TextBox();
            this.txtBoxMinHeight = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.btnResolve = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // txtBoxMaxHeight
            // 
            this.txtBoxMaxHeight.Location = new System.Drawing.Point(12, 22);
            this.txtBoxMaxHeight.Name = "txtBoxMaxHeight";
            this.txtBoxMaxHeight.Size = new System.Drawing.Size(192, 20);
            this.txtBoxMaxHeight.TabIndex = 0;
            // 
            // txtBoxMinHeight
            // 
            this.txtBoxMinHeight.Location = new System.Drawing.Point(12, 64);
            this.txtBoxMinHeight.Name = "txtBoxMinHeight";
            this.txtBoxMinHeight.Size = new System.Drawing.Size(192, 20);
            this.txtBoxMinHeight.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(11, 48);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(114, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Minimum Node Height:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(11, 6);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(117, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Maximum Node Height:";
            // 
            // btnResolve
            // 
            this.btnResolve.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnResolve.Location = new System.Drawing.Point(12, 94);
            this.btnResolve.Name = "btnResolve";
            this.btnResolve.Size = new System.Drawing.Size(93, 23);
            this.btnResolve.TabIndex = 4;
            this.btnResolve.Text = "Resolve";
            this.btnResolve.UseVisualStyleBackColor = true;
            this.btnResolve.Click += new System.EventHandler(this.btnResolve_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(111, 94);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(93, 23);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // ResolveConstraints
            // 
            this.AcceptButton = this.btnResolve;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(211, 125);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnResolve);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtBoxMinHeight);
            this.Controls.Add(this.txtBoxMaxHeight);
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(219, 152);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(219, 152);
            this.Name = "ResolveConstraints";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "Resolution Constraints";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtBoxMaxHeight;
        private System.Windows.Forms.TextBox txtBoxMinHeight;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnResolve;
        private System.Windows.Forms.Button btnCancel;
    }
}