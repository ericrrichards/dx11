namespace Fortune.FromJS {
    partial class MainForm {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.splitPanel = new System.Windows.Forms.SplitContainer();
            this.chkShowEdges = new System.Windows.Forms.CheckBox();
            this.chkShowVertices = new System.Windows.Forms.CheckBox();
            this.chkShowSites = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.nudNumRegions = new System.Windows.Forms.NumericUpDown();
            this.btnRegen = new System.Windows.Forms.Button();
            this.nudSeed = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.splitPanel)).BeginInit();
            this.splitPanel.Panel1.SuspendLayout();
            this.splitPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudNumRegions)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudSeed)).BeginInit();
            this.SuspendLayout();
            // 
            // splitPanel
            // 
            this.splitPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitPanel.Location = new System.Drawing.Point(0, 0);
            this.splitPanel.Name = "splitPanel";
            this.splitPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitPanel.Panel1
            // 
            this.splitPanel.Panel1.BackColor = System.Drawing.SystemColors.Control;
            this.splitPanel.Panel1.Controls.Add(this.nudSeed);
            this.splitPanel.Panel1.Controls.Add(this.chkShowEdges);
            this.splitPanel.Panel1.Controls.Add(this.chkShowVertices);
            this.splitPanel.Panel1.Controls.Add(this.chkShowSites);
            this.splitPanel.Panel1.Controls.Add(this.label1);
            this.splitPanel.Panel1.Controls.Add(this.nudNumRegions);
            this.splitPanel.Panel1.Controls.Add(this.btnRegen);
            // 
            // splitPanel.Panel2
            // 
            this.splitPanel.Panel2.Paint += new System.Windows.Forms.PaintEventHandler(this.splitContainer1_Panel2_Paint);
            this.splitPanel.Size = new System.Drawing.Size(800, 600);
            this.splitPanel.SplitterDistance = 35;
            this.splitPanel.TabIndex = 0;
            // 
            // chkShowEdges
            // 
            this.chkShowEdges.AutoSize = true;
            this.chkShowEdges.Checked = true;
            this.chkShowEdges.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkShowEdges.Location = new System.Drawing.Point(442, 13);
            this.chkShowEdges.Name = "chkShowEdges";
            this.chkShowEdges.Size = new System.Drawing.Size(92, 17);
            this.chkShowEdges.TabIndex = 5;
            this.chkShowEdges.Text = "Show Edges?";
            this.chkShowEdges.UseVisualStyleBackColor = true;
            this.chkShowEdges.CheckedChanged += new System.EventHandler(this.chkShowEdges_CheckedChanged);
            // 
            // chkShowVertices
            // 
            this.chkShowVertices.AutoSize = true;
            this.chkShowVertices.Checked = true;
            this.chkShowVertices.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkShowVertices.Location = new System.Drawing.Point(336, 13);
            this.chkShowVertices.Name = "chkShowVertices";
            this.chkShowVertices.Size = new System.Drawing.Size(100, 17);
            this.chkShowVertices.TabIndex = 4;
            this.chkShowVertices.Text = "Show Vertices?";
            this.chkShowVertices.UseVisualStyleBackColor = true;
            this.chkShowVertices.CheckedChanged += new System.EventHandler(this.chkShowEdges_CheckedChanged);
            // 
            // chkShowSites
            // 
            this.chkShowSites.AutoSize = true;
            this.chkShowSites.Checked = true;
            this.chkShowSites.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkShowSites.Location = new System.Drawing.Point(245, 13);
            this.chkShowSites.Name = "chkShowSites";
            this.chkShowSites.Size = new System.Drawing.Size(85, 17);
            this.chkShowSites.TabIndex = 3;
            this.chkShowSites.Text = "Show Sites?";
            this.chkShowSites.UseVisualStyleBackColor = true;
            this.chkShowSites.CheckedChanged += new System.EventHandler(this.chkShowEdges_CheckedChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 14);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(101, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Number of Regions:";
            // 
            // nudNumRegions
            // 
            this.nudNumRegions.Location = new System.Drawing.Point(119, 12);
            this.nudNumRegions.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nudNumRegions.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudNumRegions.Name = "nudNumRegions";
            this.nudNumRegions.Size = new System.Drawing.Size(120, 20);
            this.nudNumRegions.TabIndex = 1;
            this.nudNumRegions.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            // 
            // btnRegen
            // 
            this.btnRegen.Location = new System.Drawing.Point(713, 9);
            this.btnRegen.Name = "btnRegen";
            this.btnRegen.Size = new System.Drawing.Size(75, 23);
            this.btnRegen.TabIndex = 0;
            this.btnRegen.Text = "Regenerate";
            this.btnRegen.UseVisualStyleBackColor = true;
            this.btnRegen.Click += new System.EventHandler(this.btnRegen_Click);
            // 
            // nudSeed
            // 
            this.nudSeed.Location = new System.Drawing.Point(540, 12);
            this.nudSeed.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.nudSeed.Name = "nudSeed";
            this.nudSeed.Size = new System.Drawing.Size(120, 20);
            this.nudSeed.TabIndex = 6;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Controls.Add(this.splitPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "MainForm";
            this.Text = "Voronoi";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.splitPanel.Panel1.ResumeLayout(false);
            this.splitPanel.Panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitPanel)).EndInit();
            this.splitPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.nudNumRegions)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudSeed)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitPanel;
        private System.Windows.Forms.CheckBox chkShowEdges;
        private System.Windows.Forms.CheckBox chkShowVertices;
        private System.Windows.Forms.CheckBox chkShowSites;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown nudNumRegions;
        private System.Windows.Forms.Button btnRegen;
        private System.Windows.Forms.NumericUpDown nudSeed;

    }
}