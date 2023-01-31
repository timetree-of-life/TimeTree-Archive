namespace TopoTime
{
    partial class CompareForm
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
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.btnAllSuspects = new System.Windows.Forms.Button();
            this.textBoxPubID = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.clmQueryStudy = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.clmOtherStudy = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.clmNodeText = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.clmQueryStudyTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.clmOtherStudyTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.clmRatio = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.clmStudyCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.clmQueryStudy,
            this.clmOtherStudy,
            this.clmNodeText,
            this.clmQueryStudyTime,
            this.clmOtherStudyTime,
            this.clmRatio,
            this.clmStudyCount});
            this.dataGridView1.Location = new System.Drawing.Point(13, 13);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.RowHeadersVisible = false;
            this.dataGridView1.Size = new System.Drawing.Size(799, 329);
            this.dataGridView1.TabIndex = 0;
            // 
            // btnAllSuspects
            // 
            this.btnAllSuspects.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAllSuspects.Location = new System.Drawing.Point(12, 381);
            this.btnAllSuspects.Name = "btnAllSuspects";
            this.btnAllSuspects.Size = new System.Drawing.Size(193, 23);
            this.btnAllSuspects.TabIndex = 10;
            this.btnAllSuspects.Text = "Compare at All Divergences";
            this.btnAllSuspects.UseVisualStyleBackColor = true;
            this.btnAllSuspects.Click += new System.EventHandler(this.btnAllSuspects_Click);
            // 
            // textBoxPubID
            // 
            this.textBoxPubID.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxPubID.Location = new System.Drawing.Point(144, 351);
            this.textBoxPubID.Name = "textBoxPubID";
            this.textBoxPubID.Size = new System.Drawing.Size(668, 20);
            this.textBoxPubID.TabIndex = 12;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 354);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(126, 13);
            this.label1.TabIndex = 13;
            this.label1.Text = "PubMed or Reference ID";
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.button1.Location = new System.Drawing.Point(211, 381);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(193, 23);
            this.button1.TabIndex = 14;
            this.button1.Text = "Compare Average Divergences";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // clmQueryStudy
            // 
            this.clmQueryStudy.DataPropertyName = "StudyA";
            this.clmQueryStudy.HeaderText = "Query";
            this.clmQueryStudy.Name = "clmQueryStudy";
            this.clmQueryStudy.ReadOnly = true;
            // 
            // clmOtherStudy
            // 
            this.clmOtherStudy.DataPropertyName = "StudyB";
            this.clmOtherStudy.HeaderText = "Other Study";
            this.clmOtherStudy.Name = "clmOtherStudy";
            this.clmOtherStudy.ReadOnly = true;
            // 
            // clmNodeText
            // 
            this.clmNodeText.DataPropertyName = "NodeText";
            this.clmNodeText.HeaderText = "Node Text";
            this.clmNodeText.Name = "clmNodeText";
            this.clmNodeText.ReadOnly = true;
            this.clmNodeText.Width = 150;
            // 
            // clmQueryStudyTime
            // 
            this.clmQueryStudyTime.DataPropertyName = "TimeA";
            this.clmQueryStudyTime.HeaderText = "Query Study Time";
            this.clmQueryStudyTime.Name = "clmQueryStudyTime";
            this.clmQueryStudyTime.ReadOnly = true;
            this.clmQueryStudyTime.Width = 120;
            // 
            // clmOtherStudyTime
            // 
            this.clmOtherStudyTime.DataPropertyName = "TimeB";
            this.clmOtherStudyTime.HeaderText = "Other Study Time";
            this.clmOtherStudyTime.Name = "clmOtherStudyTime";
            this.clmOtherStudyTime.ReadOnly = true;
            this.clmOtherStudyTime.Width = 120;
            // 
            // clmRatio
            // 
            this.clmRatio.DataPropertyName = "Ratio";
            this.clmRatio.HeaderText = "Ratio";
            this.clmRatio.Name = "clmRatio";
            this.clmRatio.ReadOnly = true;
            this.clmRatio.Width = 75;
            // 
            // clmStudyCount
            // 
            this.clmStudyCount.DataPropertyName = "StudyCount";
            this.clmStudyCount.HeaderText = "Studies on Divergence";
            this.clmStudyCount.Name = "clmStudyCount";
            this.clmStudyCount.ReadOnly = true;
            // 
            // CompareForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(824, 416);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBoxPubID);
            this.Controls.Add(this.btnAllSuspects);
            this.Controls.Add(this.dataGridView1);
            this.MinimumSize = new System.Drawing.Size(426, 27);
            this.Name = "CompareForm";
            this.Text = "Compare Studies";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button btnAllSuspects;
        private System.Windows.Forms.TextBox textBoxPubID;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.DataGridViewTextBoxColumn clmQueryStudy;
        private System.Windows.Forms.DataGridViewTextBoxColumn clmOtherStudy;
        private System.Windows.Forms.DataGridViewTextBoxColumn clmNodeText;
        private System.Windows.Forms.DataGridViewTextBoxColumn clmQueryStudyTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn clmOtherStudyTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn clmRatio;
        private System.Windows.Forms.DataGridViewTextBoxColumn clmStudyCount;
    }
}