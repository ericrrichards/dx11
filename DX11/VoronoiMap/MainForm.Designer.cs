namespace VoronoiMap {
    sealed partial class MainForm {
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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.label2 = new System.Windows.Forms.Label();
            this.btnAnimate = new System.Windows.Forms.Button();
            this.btnStepVoronoi = new System.Windows.Forms.Button();
            this.btnStepTo = new System.Windows.Forms.Button();
            this.nudStepTo = new System.Windows.Forms.NumericUpDown();
            this.nudRelax = new System.Windows.Forms.NumericUpDown();
            this.btnInitialize = new System.Windows.Forms.Button();
            this.nudSeed = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.nudNumRegions = new System.Windows.Forms.NumericUpDown();
            this.chDebug = new System.Windows.Forms.CheckBox();
            this.btnRegen = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.chkShowSites = new System.Windows.Forms.CheckBox();
            this.chkShowVertices = new System.Windows.Forms.CheckBox();
            this.chkShowEdges = new System.Windows.Forms.CheckBox();
            this.cbCircles = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.chkBeachline = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.splitPanel)).BeginInit();
            this.splitPanel.Panel1.SuspendLayout();
            this.splitPanel.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudStepTo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudRelax)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudSeed)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudNumRegions)).BeginInit();
            this.SuspendLayout();
            // 
            // splitPanel
            // 
            this.splitPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitPanel.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitPanel.IsSplitterFixed = true;
            this.splitPanel.Location = new System.Drawing.Point(0, 0);
            this.splitPanel.Name = "splitPanel";
            // 
            // splitPanel.Panel1
            // 
            this.splitPanel.Panel1.BackColor = System.Drawing.SystemColors.Control;
            this.splitPanel.Panel1.Controls.Add(this.tableLayoutPanel1);
            this.splitPanel.Panel1MinSize = 250;
            // 
            // splitPanel.Panel2
            // 
            this.splitPanel.Panel2.Paint += new System.Windows.Forms.PaintEventHandler(this.splitContainer1_Panel2_Paint);
            this.splitPanel.Size = new System.Drawing.Size(842, 600);
            this.splitPanel.SplitterDistance = 250;
            this.splitPanel.SplitterWidth = 1;
            this.splitPanel.TabIndex = 0;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.btnAnimate, 1, 13);
            this.tableLayoutPanel1.Controls.Add(this.btnStepVoronoi, 0, 12);
            this.tableLayoutPanel1.Controls.Add(this.btnStepTo, 1, 12);
            this.tableLayoutPanel1.Controls.Add(this.nudStepTo, 1, 11);
            this.tableLayoutPanel1.Controls.Add(this.nudRelax, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.btnInitialize, 1, 10);
            this.tableLayoutPanel1.Controls.Add(this.nudSeed, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.nudNumRegions, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.chDebug, 1, 7);
            this.tableLayoutPanel1.Controls.Add(this.btnRegen, 1, 8);
            this.tableLayoutPanel1.Controls.Add(this.label4, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.chkShowSites, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.chkShowVertices, 1, 4);
            this.tableLayoutPanel1.Controls.Add(this.chkShowEdges, 1, 5);
            this.tableLayoutPanel1.Controls.Add(this.cbCircles, 1, 6);
            this.tableLayoutPanel1.Controls.Add(this.label5, 0, 6);
            this.tableLayoutPanel1.Controls.Add(this.label3, 0, 11);
            this.tableLayoutPanel1.Controls.Add(this.chkBeachline, 0, 10);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 14;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(250, 600);
            this.tableLayoutPanel1.TabIndex = 19;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label2.Location = new System.Drawing.Point(3, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(119, 26);
            this.label2.TabIndex = 13;
            this.label2.Text = "Seed:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnAnimate
            // 
            this.btnAnimate.Location = new System.Drawing.Point(128, 333);
            this.btnAnimate.Name = "btnAnimate";
            this.btnAnimate.Size = new System.Drawing.Size(119, 23);
            this.btnAnimate.TabIndex = 14;
            this.btnAnimate.Text = "Animate";
            this.btnAnimate.UseVisualStyleBackColor = true;
            this.btnAnimate.Click += new System.EventHandler(this.btnAnimate_Click);
            // 
            // btnStepVoronoi
            // 
            this.btnStepVoronoi.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStepVoronoi.Location = new System.Drawing.Point(3, 304);
            this.btnStepVoronoi.Name = "btnStepVoronoi";
            this.btnStepVoronoi.Size = new System.Drawing.Size(119, 23);
            this.btnStepVoronoi.TabIndex = 7;
            this.btnStepVoronoi.Text = "Step Graph";
            this.btnStepVoronoi.UseVisualStyleBackColor = true;
            this.btnStepVoronoi.Click += new System.EventHandler(this.btnStepVoronoi_Click);
            // 
            // btnStepTo
            // 
            this.btnStepTo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStepTo.Location = new System.Drawing.Point(128, 304);
            this.btnStepTo.Name = "btnStepTo";
            this.btnStepTo.Size = new System.Drawing.Size(119, 23);
            this.btnStepTo.TabIndex = 11;
            this.btnStepTo.Text = "Step to:";
            this.btnStepTo.UseVisualStyleBackColor = true;
            this.btnStepTo.Click += new System.EventHandler(this.btnStepTo_Click);
            // 
            // nudStepTo
            // 
            this.nudStepTo.Location = new System.Drawing.Point(128, 278);
            this.nudStepTo.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nudStepTo.Name = "nudStepTo";
            this.nudStepTo.Size = new System.Drawing.Size(119, 20);
            this.nudStepTo.TabIndex = 10;
            // 
            // nudRelax
            // 
            this.nudRelax.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nudRelax.Location = new System.Drawing.Point(128, 55);
            this.nudRelax.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.nudRelax.Name = "nudRelax";
            this.nudRelax.Size = new System.Drawing.Size(119, 20);
            this.nudRelax.TabIndex = 18;
            // 
            // btnInitialize
            // 
            this.btnInitialize.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnInitialize.Location = new System.Drawing.Point(128, 249);
            this.btnInitialize.Name = "btnInitialize";
            this.btnInitialize.Size = new System.Drawing.Size(119, 23);
            this.btnInitialize.TabIndex = 8;
            this.btnInitialize.Text = "Initialize";
            this.btnInitialize.UseVisualStyleBackColor = true;
            this.btnInitialize.Click += new System.EventHandler(this.btnInitialize_Click);
            // 
            // nudSeed
            // 
            this.nudSeed.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nudSeed.Location = new System.Drawing.Point(128, 3);
            this.nudSeed.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.nudSeed.Name = "nudSeed";
            this.nudSeed.Size = new System.Drawing.Size(119, 20);
            this.nudSeed.TabIndex = 6;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Location = new System.Drawing.Point(3, 26);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(119, 26);
            this.label1.TabIndex = 2;
            this.label1.Text = "Number of Regions:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // nudNumRegions
            // 
            this.nudNumRegions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nudNumRegions.Location = new System.Drawing.Point(128, 29);
            this.nudNumRegions.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nudNumRegions.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudNumRegions.Name = "nudNumRegions";
            this.nudNumRegions.Size = new System.Drawing.Size(119, 20);
            this.nudNumRegions.TabIndex = 1;
            this.nudNumRegions.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            // 
            // chDebug
            // 
            this.chDebug.AutoSize = true;
            this.chDebug.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chDebug.Location = new System.Drawing.Point(128, 177);
            this.chDebug.Name = "chDebug";
            this.chDebug.Size = new System.Drawing.Size(119, 17);
            this.chDebug.TabIndex = 12;
            this.chDebug.Text = "Debug?";
            this.chDebug.UseVisualStyleBackColor = true;
            // 
            // btnRegen
            // 
            this.btnRegen.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRegen.Location = new System.Drawing.Point(128, 200);
            this.btnRegen.Name = "btnRegen";
            this.btnRegen.Size = new System.Drawing.Size(119, 23);
            this.btnRegen.TabIndex = 0;
            this.btnRegen.Text = "Regenerate Graph";
            this.btnRegen.UseVisualStyleBackColor = true;
            this.btnRegen.Click += new System.EventHandler(this.btnRegen_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label4.Location = new System.Drawing.Point(3, 52);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(119, 26);
            this.label4.TabIndex = 17;
            this.label4.Text = "Relax:";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // chkShowSites
            // 
            this.chkShowSites.AutoSize = true;
            this.chkShowSites.Checked = true;
            this.chkShowSites.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkShowSites.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkShowSites.Location = new System.Drawing.Point(128, 81);
            this.chkShowSites.Name = "chkShowSites";
            this.chkShowSites.Size = new System.Drawing.Size(119, 17);
            this.chkShowSites.TabIndex = 3;
            this.chkShowSites.Text = "Show Sites?";
            this.chkShowSites.UseVisualStyleBackColor = true;
            this.chkShowSites.CheckedChanged += new System.EventHandler(this.chkShowEdges_CheckedChanged);
            // 
            // chkShowVertices
            // 
            this.chkShowVertices.AutoSize = true;
            this.chkShowVertices.Checked = true;
            this.chkShowVertices.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkShowVertices.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkShowVertices.Location = new System.Drawing.Point(128, 104);
            this.chkShowVertices.Name = "chkShowVertices";
            this.chkShowVertices.Size = new System.Drawing.Size(119, 17);
            this.chkShowVertices.TabIndex = 4;
            this.chkShowVertices.Text = "Show Vertices?";
            this.chkShowVertices.UseVisualStyleBackColor = true;
            this.chkShowVertices.CheckedChanged += new System.EventHandler(this.chkShowEdges_CheckedChanged);
            // 
            // chkShowEdges
            // 
            this.chkShowEdges.AutoSize = true;
            this.chkShowEdges.Checked = true;
            this.chkShowEdges.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkShowEdges.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkShowEdges.Location = new System.Drawing.Point(128, 127);
            this.chkShowEdges.Name = "chkShowEdges";
            this.chkShowEdges.Size = new System.Drawing.Size(119, 17);
            this.chkShowEdges.TabIndex = 5;
            this.chkShowEdges.Text = "Show Edges?";
            this.chkShowEdges.UseVisualStyleBackColor = true;
            this.chkShowEdges.CheckedChanged += new System.EventHandler(this.chkShowEdges_CheckedChanged);
            // 
            // cbCircles
            // 
            this.cbCircles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cbCircles.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbCircles.FormattingEnabled = true;
            this.cbCircles.Items.AddRange(new object[] {
            "None",
            "Circles",
            "Triangles"});
            this.cbCircles.Location = new System.Drawing.Point(128, 150);
            this.cbCircles.Name = "cbCircles";
            this.cbCircles.Size = new System.Drawing.Size(119, 21);
            this.cbCircles.TabIndex = 15;
            this.cbCircles.SelectedIndexChanged += new System.EventHandler(this.cbCircles_SelectedIndexChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label5.Location = new System.Drawing.Point(3, 147);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(119, 27);
            this.label5.TabIndex = 19;
            this.label5.Text = "Show Circle Events:";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label3.Location = new System.Drawing.Point(3, 275);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(119, 26);
            this.label3.TabIndex = 16;
            this.label3.Text = "Step:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // chkBeachline
            // 
            this.chkBeachline.AutoSize = true;
            this.chkBeachline.Checked = true;
            this.chkBeachline.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkBeachline.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkBeachline.Location = new System.Drawing.Point(3, 249);
            this.chkBeachline.Name = "chkBeachline";
            this.chkBeachline.Size = new System.Drawing.Size(119, 23);
            this.chkBeachline.TabIndex = 20;
            this.chkBeachline.Text = "Show Beachline?";
            this.chkBeachline.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(842, 600);
            this.Controls.Add(this.splitPanel);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "MainForm";
            this.Text = "Voronoi";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Resize += new System.EventHandler(this.MainForm_Resize);
            this.splitPanel.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitPanel)).EndInit();
            this.splitPanel.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudStepTo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudRelax)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudSeed)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudNumRegions)).EndInit();
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
        private System.Windows.Forms.Button btnStepVoronoi;
        private System.Windows.Forms.Button btnInitialize;
        private System.Windows.Forms.Button btnStepTo;
        private System.Windows.Forms.NumericUpDown nudStepTo;
        private System.Windows.Forms.CheckBox chDebug;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnAnimate;
        private System.Windows.Forms.ComboBox cbCircles;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown nudRelax;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.CheckBox chkBeachline;

    }
}