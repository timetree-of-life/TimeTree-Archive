namespace TopoTime
{
    partial class SuspectForm
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            this.dgvSuspectList = new System.Windows.Forms.DataGridView();
            this.btnRearrangedNodes = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.btnAllSuspects = new System.Windows.Forms.Button();
            this.btnUnusualDivergence = new System.Windows.Forms.Button();
            this.btnAmbiguousPartitions = new System.Windows.Forms.Button();
            this.btnUnsupportedPartitions = new System.Windows.Forms.Button();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.btnNegativeBranch = new System.Windows.Forms.Button();
            this.btnZeroHeightParents = new System.Windows.Forms.Button();
            this.chkBoxNamedNodes = new System.Windows.Forms.CheckBox();
            this.clmFileName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.clmRearranged = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.clmAB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.clmAC = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.clmBC = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.clmStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.clmNearestNegativeBranch = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.clmDivergenceLarge = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.clmDivergenceSmall = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.clmHeightDiff = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.clmDivCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.clmTimeList = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.clmPreadjustedTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.clmPostAdjusted = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSuspectList)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgvSuspectList
            // 
            this.dgvSuspectList.AllowUserToAddRows = false;
            this.dgvSuspectList.AllowUserToDeleteRows = false;
            this.dgvSuspectList.AllowUserToResizeRows = false;
            this.dgvSuspectList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvSuspectList.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvSuspectList.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvSuspectList.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.clmFileName,
            this.clmRearranged,
            this.clmAB,
            this.clmAC,
            this.clmBC,
            this.clmStatus,
            this.clmNearestNegativeBranch,
            this.clmDivergenceLarge,
            this.clmDivergenceSmall,
            this.clmHeightDiff,
            this.clmDivCount,
            this.clmTimeList,
            this.clmPreadjustedTime,
            this.clmPostAdjusted});
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvSuspectList.DefaultCellStyle = dataGridViewCellStyle2;
            this.dgvSuspectList.Location = new System.Drawing.Point(12, 12);
            this.dgvSuspectList.Name = "dgvSuspectList";
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvSuspectList.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
            this.dgvSuspectList.RowHeadersWidth = 65;
            this.dgvSuspectList.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvSuspectList.Size = new System.Drawing.Size(1074, 331);
            this.dgvSuspectList.TabIndex = 5;
            this.dgvSuspectList.DataBindingComplete += new System.Windows.Forms.DataGridViewBindingCompleteEventHandler(this.dgvSuspectList_DataBindingComplete);
            this.dgvSuspectList.RowEnter += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvSuspectList_RowEnter);
            // 
            // btnRearrangedNodes
            // 
            this.btnRearrangedNodes.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnRearrangedNodes.Location = new System.Drawing.Point(589, 349);
            this.btnRearrangedNodes.Name = "btnRearrangedNodes";
            this.btnRearrangedNodes.Size = new System.Drawing.Size(120, 23);
            this.btnRearrangedNodes.TabIndex = 6;
            this.btnRearrangedNodes.Text = "Rearranged Nodes";
            this.btnRearrangedNodes.UseVisualStyleBackColor = true;
            this.btnRearrangedNodes.Click += new System.EventHandler(this.btnRearrangedNodes_Click);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(14, 356);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(32, 13);
            this.label1.TabIndex = 7;
            this.label1.Text = "Filter:";
            // 
            // btnAllSuspects
            // 
            this.btnAllSuspects.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAllSuspects.Location = new System.Drawing.Point(52, 349);
            this.btnAllSuspects.Name = "btnAllSuspects";
            this.btnAllSuspects.Size = new System.Drawing.Size(107, 23);
            this.btnAllSuspects.TabIndex = 8;
            this.btnAllSuspects.Text = "All Suspect Nodes";
            this.btnAllSuspects.UseVisualStyleBackColor = true;
            this.btnAllSuspects.Click += new System.EventHandler(this.btnAllSuspects_Click);
            // 
            // btnUnusualDivergence
            // 
            this.btnUnusualDivergence.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnUnusualDivergence.Location = new System.Drawing.Point(165, 349);
            this.btnUnusualDivergence.Name = "btnUnusualDivergence";
            this.btnUnusualDivergence.Size = new System.Drawing.Size(152, 23);
            this.btnUnusualDivergence.TabIndex = 9;
            this.btnUnusualDivergence.Text = "Unusual Divergence Ratios";
            this.btnUnusualDivergence.UseVisualStyleBackColor = true;
            this.btnUnusualDivergence.Click += new System.EventHandler(this.btnUnusualDivergence_Click);
            // 
            // btnAmbiguousPartitions
            // 
            this.btnAmbiguousPartitions.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAmbiguousPartitions.Location = new System.Drawing.Point(461, 349);
            this.btnAmbiguousPartitions.Name = "btnAmbiguousPartitions";
            this.btnAmbiguousPartitions.Size = new System.Drawing.Size(122, 23);
            this.btnAmbiguousPartitions.TabIndex = 10;
            this.btnAmbiguousPartitions.Text = "Ambiguous Partitions";
            this.btnAmbiguousPartitions.UseVisualStyleBackColor = true;
            this.btnAmbiguousPartitions.Click += new System.EventHandler(this.btnAmbiguousPartitions_Click);
            // 
            // btnUnsupportedPartitions
            // 
            this.btnUnsupportedPartitions.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnUnsupportedPartitions.Location = new System.Drawing.Point(323, 349);
            this.btnUnsupportedPartitions.Name = "btnUnsupportedPartitions";
            this.btnUnsupportedPartitions.Size = new System.Drawing.Size(132, 23);
            this.btnUnsupportedPartitions.TabIndex = 11;
            this.btnUnsupportedPartitions.Text = "Unsupported Partitions";
            this.btnUnsupportedPartitions.UseVisualStyleBackColor = true;
            this.btnUnsupportedPartitions.Click += new System.EventHandler(this.btnUnsupportedPartitions_Click);
            // 
            // toolStrip1
            // 
            this.toolStrip1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripLabel1});
            this.toolStrip1.Location = new System.Drawing.Point(0, 378);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1098, 25);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(0, 22);
            // 
            // btnNegativeBranch
            // 
            this.btnNegativeBranch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnNegativeBranch.Location = new System.Drawing.Point(715, 349);
            this.btnNegativeBranch.Name = "btnNegativeBranch";
            this.btnNegativeBranch.Size = new System.Drawing.Size(117, 23);
            this.btnNegativeBranch.TabIndex = 12;
            this.btnNegativeBranch.Text = "Negative Branches";
            this.btnNegativeBranch.UseVisualStyleBackColor = true;
            this.btnNegativeBranch.Click += new System.EventHandler(this.btnNegativeBranch_Click);
            // 
            // btnZeroHeightParents
            // 
            this.btnZeroHeightParents.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnZeroHeightParents.Location = new System.Drawing.Point(838, 349);
            this.btnZeroHeightParents.Name = "btnZeroHeightParents";
            this.btnZeroHeightParents.Size = new System.Drawing.Size(117, 23);
            this.btnZeroHeightParents.TabIndex = 13;
            this.btnZeroHeightParents.Text = "Zero-Height Parents";
            this.btnZeroHeightParents.UseVisualStyleBackColor = true;
            this.btnZeroHeightParents.Click += new System.EventHandler(this.btnZeroHeightParents_Click);
            // 
            // chkBoxNamedNodes
            // 
            this.chkBoxNamedNodes.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkBoxNamedNodes.AutoSize = true;
            this.chkBoxNamedNodes.Location = new System.Drawing.Point(962, 353);
            this.chkBoxNamedNodes.Name = "chkBoxNamedNodes";
            this.chkBoxNamedNodes.Size = new System.Drawing.Size(118, 17);
            this.chkBoxNamedNodes.TabIndex = 14;
            this.chkBoxNamedNodes.Text = "Named Nodes Only";
            this.chkBoxNamedNodes.UseVisualStyleBackColor = true;
            this.chkBoxNamedNodes.CheckedChanged += new System.EventHandler(this.chkBoxNamedNodes_CheckedChanged);
            // 
            // clmFileName
            // 
            this.clmFileName.DataPropertyName = "Text";
            this.clmFileName.HeaderText = "Name";
            this.clmFileName.Name = "clmFileName";
            this.clmFileName.ReadOnly = true;
            this.clmFileName.Width = 150;
            // 
            // clmRearranged
            // 
            this.clmRearranged.DataPropertyName = "Rearranged";
            this.clmRearranged.HeaderText = "Rearranged";
            this.clmRearranged.Name = "clmRearranged";
            this.clmRearranged.ReadOnly = true;
            // 
            // clmAB
            // 
            this.clmAB.DataPropertyName = "SupportAB";
            this.clmAB.HeaderText = "AB";
            this.clmAB.Name = "clmAB";
            this.clmAB.ReadOnly = true;
            this.clmAB.Width = 50;
            // 
            // clmAC
            // 
            this.clmAC.DataPropertyName = "SupportAC";
            this.clmAC.HeaderText = "AC";
            this.clmAC.Name = "clmAC";
            this.clmAC.ReadOnly = true;
            this.clmAC.Width = 50;
            // 
            // clmBC
            // 
            this.clmBC.DataPropertyName = "SupportBC";
            this.clmBC.HeaderText = "BC";
            this.clmBC.Name = "clmBC";
            this.clmBC.ReadOnly = true;
            this.clmBC.Width = 50;
            // 
            // clmStatus
            // 
            this.clmStatus.DataPropertyName = "DivergenceRatio";
            this.clmStatus.HeaderText = "Max\\Min Divergence Ratio";
            this.clmStatus.Name = "clmStatus";
            this.clmStatus.ReadOnly = true;
            this.clmStatus.Width = 200;
            // 
            // clmNearestNegativeBranch
            // 
            this.clmNearestNegativeBranch.DataPropertyName = "LevelsFromNegativeBranch";
            this.clmNearestNegativeBranch.HeaderText = "Levels from Nearest Negative Branch";
            this.clmNearestNegativeBranch.Name = "clmNearestNegativeBranch";
            this.clmNearestNegativeBranch.ReadOnly = true;
            this.clmNearestNegativeBranch.Width = 250;
            // 
            // clmDivergenceLarge
            // 
            this.clmDivergenceLarge.DataPropertyName = "DivergenceLargest";
            this.clmDivergenceLarge.HeaderText = "Large Study";
            this.clmDivergenceLarge.Name = "clmDivergenceLarge";
            this.clmDivergenceLarge.ReadOnly = true;
            // 
            // clmDivergenceSmall
            // 
            this.clmDivergenceSmall.DataPropertyName = "DivergenceSmallest";
            this.clmDivergenceSmall.HeaderText = "Small Study";
            this.clmDivergenceSmall.Name = "clmDivergenceSmall";
            this.clmDivergenceSmall.ReadOnly = true;
            // 
            // clmHeightDiff
            // 
            this.clmHeightDiff.DataPropertyName = "HeightDiff";
            this.clmHeightDiff.HeaderText = "Height Difference";
            this.clmHeightDiff.Name = "clmHeightDiff";
            this.clmHeightDiff.ReadOnly = true;
            this.clmHeightDiff.Width = 150;
            // 
            // clmDivCount
            // 
            this.clmDivCount.DataPropertyName = "DivergenceCount";
            this.clmDivCount.HeaderText = "Study Count";
            this.clmDivCount.Name = "clmDivCount";
            this.clmDivCount.ReadOnly = true;
            // 
            // clmTimeList
            // 
            this.clmTimeList.DataPropertyName = "DivergenceList";
            this.clmTimeList.HeaderText = "Times on Study";
            this.clmTimeList.Name = "clmTimeList";
            this.clmTimeList.ReadOnly = true;
            // 
            // clmPreadjustedTime
            // 
            this.clmPreadjustedTime.DataPropertyName = "PreAdjustedHeight";
            this.clmPreadjustedTime.HeaderText = "Pre-Adjusted Time";
            this.clmPreadjustedTime.Name = "clmPreadjustedTime";
            // 
            // clmPostAdjusted
            // 
            this.clmPostAdjusted.DataPropertyName = "StoredAdjustedHeight";
            this.clmPostAdjusted.HeaderText = "Post-Adjusted Time";
            this.clmPostAdjusted.Name = "clmPostAdjusted";
            // 
            // SuspectForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1098, 403);
            this.Controls.Add(this.chkBoxNamedNodes);
            this.Controls.Add(this.btnZeroHeightParents);
            this.Controls.Add(this.btnNegativeBranch);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.btnUnsupportedPartitions);
            this.Controls.Add(this.btnAmbiguousPartitions);
            this.Controls.Add(this.btnUnusualDivergence);
            this.Controls.Add(this.btnAllSuspects);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnRearrangedNodes);
            this.Controls.Add(this.dgvSuspectList);
            this.Name = "SuspectForm";
            this.Text = "Suspect Nodes";
            ((System.ComponentModel.ISupportInitialize)(this.dgvSuspectList)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dgvSuspectList;
        private System.Windows.Forms.Button btnRearrangedNodes;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnAllSuspects;
        private System.Windows.Forms.Button btnUnusualDivergence;
        private System.Windows.Forms.Button btnAmbiguousPartitions;
        private System.Windows.Forms.Button btnUnsupportedPartitions;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.Button btnNegativeBranch;
        private System.Windows.Forms.Button btnZeroHeightParents;
        private System.Windows.Forms.CheckBox chkBoxNamedNodes;
        private System.Windows.Forms.DataGridViewTextBoxColumn clmFileName;
        private System.Windows.Forms.DataGridViewTextBoxColumn clmRearranged;
        private System.Windows.Forms.DataGridViewTextBoxColumn clmAB;
        private System.Windows.Forms.DataGridViewTextBoxColumn clmAC;
        private System.Windows.Forms.DataGridViewTextBoxColumn clmBC;
        private System.Windows.Forms.DataGridViewTextBoxColumn clmStatus;
        private System.Windows.Forms.DataGridViewTextBoxColumn clmNearestNegativeBranch;
        private System.Windows.Forms.DataGridViewTextBoxColumn clmDivergenceLarge;
        private System.Windows.Forms.DataGridViewTextBoxColumn clmDivergenceSmall;
        private System.Windows.Forms.DataGridViewTextBoxColumn clmHeightDiff;
        private System.Windows.Forms.DataGridViewTextBoxColumn clmDivCount;
        private System.Windows.Forms.DataGridViewTextBoxColumn clmTimeList;
        private System.Windows.Forms.DataGridViewTextBoxColumn clmPreadjustedTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn clmPostAdjusted;
    }
}