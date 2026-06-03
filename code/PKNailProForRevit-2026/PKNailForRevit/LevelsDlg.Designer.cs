namespace PKNailForRevit
{
    partial class LevelsDlg
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
            this.components = new System.ComponentModel.Container();
            PKNailForRevit.Distance distance1 = new PKNailForRevit.Distance();
            this.Copy2Lbl = new System.Windows.Forms.Label();
            this.Copy1Lbl = new System.Windows.Forms.Label();
            this.CopyCB = new System.Windows.Forms.CheckBox();
            this.LockCB = new System.Windows.Forms.CheckBox();
            this.Floor2DCNoneCB = new System.Windows.Forms.CheckBox();
            this.MoveDownBtn = new System.Windows.Forms.Button();
            this.MoveUpBtn = new System.Windows.Forms.Button();
            this.LevelsLB = new System.Windows.Forms.ListBox();
            this.Floor2FloorLbl = new System.Windows.Forms.Label();
            this.Floor2CeilingLbl = new System.Windows.Forms.Label();
            this.Floor2DropCeilingLbl = new System.Windows.Forms.Label();
            this.LevelElevLbl = new System.Windows.Forms.Label();
            this.LevelRefCB = new System.Windows.Forms.CheckBox();
            this.LevelDiagramPanel = new System.Windows.Forms.Panel();
            this.CloseBtn = new System.Windows.Forms.Button();
            this.UndoBtn = new System.Windows.Forms.Button();
            this.WindowHeightBtn = new System.Windows.Forms.Button();
            this.F2FEP = new System.Windows.Forms.ErrorProvider(this.components);
            this.F2CEP = new System.Windows.Forms.ErrorProvider(this.components);
            this.F2DCEP = new System.Windows.Forms.ErrorProvider(this.components);
            this.Floor2FloorDC = new PKNailForRevit.DistanceControl();
            this.Floor2CeilingDC = new PKNailForRevit.DistanceControl();
            this.Floor2DropCeilingDC = new PKNailForRevit.DistanceControl();
            this.LevelElevDC = new PKNailForRevit.DistanceControl();
            ((System.ComponentModel.ISupportInitialize)(this.F2FEP)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.F2CEP)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.F2DCEP)).BeginInit();
            this.SuspendLayout();
            // 
            // Copy2Lbl
            // 
            this.Copy2Lbl.AutoSize = true;
            this.Copy2Lbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Copy2Lbl.Location = new System.Drawing.Point(50, 290);
            this.Copy2Lbl.Name = "Copy2Lbl";
            this.Copy2Lbl.Size = new System.Drawing.Size(97, 16);
            this.Copy2Lbl.TabIndex = 17;
            this.Copy2Lbl.Text = "Measurements";
            // 
            // Copy1Lbl
            // 
            this.Copy1Lbl.AutoSize = true;
            this.Copy1Lbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Copy1Lbl.Location = new System.Drawing.Point(50, 273);
            this.Copy1Lbl.Name = "Copy1Lbl";
            this.Copy1Lbl.Size = new System.Drawing.Size(40, 16);
            this.Copy1Lbl.TabIndex = 16;
            this.Copy1Lbl.Text = "Copy";
            // 
            // CopyCB
            // 
            this.CopyCB.AutoSize = true;
            this.CopyCB.Location = new System.Drawing.Point(33, 273);
            this.CopyCB.Name = "CopyCB";
            this.CopyCB.Size = new System.Drawing.Size(15, 14);
            this.CopyCB.TabIndex = 15;
            this.CopyCB.UseCompatibleTextRendering = true;
            this.CopyCB.UseVisualStyleBackColor = true;
            // 
            // LockCB
            // 
            this.LockCB.AutoSize = true;
            this.LockCB.Enabled = false;
            this.LockCB.Location = new System.Drawing.Point(297, 348);
            this.LockCB.Name = "LockCB";
            this.LockCB.Size = new System.Drawing.Size(50, 17);
            this.LockCB.TabIndex = 11;
            this.LockCB.Text = "Lock";
            this.LockCB.UseVisualStyleBackColor = true;
            // 
            // Floor2DCNoneCB
            // 
            this.Floor2DCNoneCB.AutoSize = true;
            this.Floor2DCNoneCB.Location = new System.Drawing.Point(398, 137);
            this.Floor2DCNoneCB.Name = "Floor2DCNoneCB";
            this.Floor2DCNoneCB.Size = new System.Drawing.Size(52, 17);
            this.Floor2DCNoneCB.TabIndex = 5;
            this.Floor2DCNoneCB.Text = "None";
            this.Floor2DCNoneCB.UseVisualStyleBackColor = true;
            this.Floor2DCNoneCB.CheckedChanged += new System.EventHandler(this.Floor2DCNoneCB_CheckedChanged);
            // 
            // MoveDownBtn
            // 
            this.MoveDownBtn.Location = new System.Drawing.Point(33, 222);
            this.MoveDownBtn.Name = "MoveDownBtn";
            this.MoveDownBtn.Size = new System.Drawing.Size(72, 40);
            this.MoveDownBtn.TabIndex = 14;
            this.MoveDownBtn.UseVisualStyleBackColor = true;
            this.MoveDownBtn.Click += new System.EventHandler(this.MoveDownBtn_Click);
            this.MoveDownBtn.Paint += new System.Windows.Forms.PaintEventHandler(this.MoveDownBtn_Paint);
            // 
            // MoveUpBtn
            // 
            this.MoveUpBtn.Location = new System.Drawing.Point(33, 166);
            this.MoveUpBtn.Name = "MoveUpBtn";
            this.MoveUpBtn.Size = new System.Drawing.Size(72, 40);
            this.MoveUpBtn.TabIndex = 13;
            this.MoveUpBtn.UseVisualStyleBackColor = true;
            this.MoveUpBtn.Click += new System.EventHandler(this.MoveUpBtn_Click);
            this.MoveUpBtn.Paint += new System.Windows.Forms.PaintEventHandler(this.MoveUpBtn_Paint);
            // 
            // LevelsLB
            // 
            this.LevelsLB.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.LevelsLB.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LevelsLB.FormattingEnabled = true;
            this.LevelsLB.ItemHeight = 17;
            this.LevelsLB.Location = new System.Drawing.Point(12, 12);
            this.LevelsLB.Name = "LevelsLB";
            this.LevelsLB.Size = new System.Drawing.Size(115, 89);
            this.LevelsLB.TabIndex = 12;
            this.LevelsLB.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.LevelsLB_DrawItem);
            this.LevelsLB.SelectedIndexChanged += new System.EventHandler(this.LevelsLB_SelectedIndexChanged);
            this.LevelsLB.DoubleClick += new System.EventHandler(this.LevelsLB_DoubleClick);
            // 
            // Floor2FloorLbl
            // 
            this.Floor2FloorLbl.AutoSize = true;
            this.Floor2FloorLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Floor2FloorLbl.Location = new System.Drawing.Point(239, 12);
            this.Floor2FloorLbl.Name = "Floor2FloorLbl";
            this.Floor2FloorLbl.Size = new System.Drawing.Size(101, 16);
            this.Floor2FloorLbl.TabIndex = 0;
            this.Floor2FloorLbl.Text = "Floor to Floor";
            // 
            // Floor2CeilingLbl
            // 
            this.Floor2CeilingLbl.AutoSize = true;
            this.Floor2CeilingLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Floor2CeilingLbl.Location = new System.Drawing.Point(239, 68);
            this.Floor2CeilingLbl.Name = "Floor2CeilingLbl";
            this.Floor2CeilingLbl.Size = new System.Drawing.Size(113, 16);
            this.Floor2CeilingLbl.TabIndex = 2;
            this.Floor2CeilingLbl.Text = "Floor to Ceiling";
            // 
            // Floor2DropCeilingLbl
            // 
            this.Floor2DropCeilingLbl.AutoSize = true;
            this.Floor2DropCeilingLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Floor2DropCeilingLbl.Location = new System.Drawing.Point(239, 136);
            this.Floor2DropCeilingLbl.Name = "Floor2DropCeilingLbl";
            this.Floor2DropCeilingLbl.Size = new System.Drawing.Size(151, 16);
            this.Floor2DropCeilingLbl.TabIndex = 4;
            this.Floor2DropCeilingLbl.Text = "Floor to Drop Ceiling";
            // 
            // LevelElevLbl
            // 
            this.LevelElevLbl.AutoSize = true;
            this.LevelElevLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LevelElevLbl.Location = new System.Drawing.Point(239, 290);
            this.LevelElevLbl.Name = "LevelElevLbl";
            this.LevelElevLbl.Size = new System.Drawing.Size(115, 16);
            this.LevelElevLbl.TabIndex = 8;
            this.LevelElevLbl.Text = "Level Elevation";
            // 
            // LevelRefCB
            // 
            this.LevelRefCB.AutoSize = true;
            this.LevelRefCB.Location = new System.Drawing.Point(242, 348);
            this.LevelRefCB.Name = "LevelRefCB";
            this.LevelRefCB.Size = new System.Drawing.Size(49, 17);
            this.LevelRefCB.TabIndex = 10;
            this.LevelRefCB.Text = "Boss";
            this.LevelRefCB.UseVisualStyleBackColor = true;
            this.LevelRefCB.CheckedChanged += new System.EventHandler(this.LevelRefCB_CheckedChanged);
            // 
            // LevelDiagramPanel
            // 
            this.LevelDiagramPanel.Location = new System.Drawing.Point(153, 45);
            this.LevelDiagramPanel.Name = "LevelDiagramPanel";
            this.LevelDiagramPanel.Size = new System.Drawing.Size(80, 285);
            this.LevelDiagramPanel.TabIndex = 18;
            this.LevelDiagramPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.LevelDiagramPanel_Paint);
            // 
            // CloseBtn
            // 
            this.CloseBtn.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.CloseBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CloseBtn.Location = new System.Drawing.Point(350, 389);
            this.CloseBtn.Margin = new System.Windows.Forms.Padding(4);
            this.CloseBtn.Name = "CloseBtn";
            this.CloseBtn.Size = new System.Drawing.Size(75, 28);
            this.CloseBtn.TabIndex = 19;
            this.CloseBtn.Text = "Done";
            this.CloseBtn.UseVisualStyleBackColor = true;
            // 
            // UndoBtn
            // 
            this.UndoBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.UndoBtn.Location = new System.Drawing.Point(267, 389);
            this.UndoBtn.Margin = new System.Windows.Forms.Padding(4);
            this.UndoBtn.Name = "UndoBtn";
            this.UndoBtn.Size = new System.Drawing.Size(75, 28);
            this.UndoBtn.TabIndex = 20;
            this.UndoBtn.Text = "Undo";
            this.UndoBtn.UseVisualStyleBackColor = true;
            this.UndoBtn.Click += new System.EventHandler(this.UndoBtn_Click);
            // 
            // WindowHeightBtn
            // 
            this.WindowHeightBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.WindowHeightBtn.Location = new System.Drawing.Point(270, 209);
            this.WindowHeightBtn.Name = "WindowHeightBtn";
            this.WindowHeightBtn.Size = new System.Drawing.Size(122, 42);
            this.WindowHeightBtn.TabIndex = 7;
            this.WindowHeightBtn.Text = "Typical Window Height/Sill Height";
            this.WindowHeightBtn.UseVisualStyleBackColor = true;
            this.WindowHeightBtn.Click += new System.EventHandler(this.WindowHeightBtn_Click);
            // 
            // F2FEP
            // 
            this.F2FEP.ContainerControl = this;
            // 
            // F2CEP
            // 
            this.F2CEP.ContainerControl = this;
            // 
            // F2DCEP
            // 
            this.F2DCEP.ContainerControl = this;
            // 
            // Floor2FloorDC
            // 
            this.Floor2FloorDC.Location = new System.Drawing.Point(242, 31);
            this.Floor2FloorDC.Margin = new System.Windows.Forms.Padding(0);
            this.Floor2FloorDC.Name = "Floor2FloorDC";
            this.Floor2FloorDC.RoundingIncrement = distance1;
            this.Floor2FloorDC.Size = new System.Drawing.Size(183, 31);
            this.Floor2FloorDC.TabIndex = 21;
            this.Floor2FloorDC.Validating += new System.ComponentModel.CancelEventHandler(this.Floor2FloorDC_Validating);
            this.Floor2FloorDC.Validated += new System.EventHandler(this.Floor2FloorDC_Validated);
            // 
            // Floor2CeilingDC
            // 
            this.Floor2CeilingDC.Location = new System.Drawing.Point(242, 88);
            this.Floor2CeilingDC.Margin = new System.Windows.Forms.Padding(0);
            this.Floor2CeilingDC.Name = "Floor2CeilingDC";
            this.Floor2CeilingDC.RoundingIncrement = distance1;
            this.Floor2CeilingDC.Size = new System.Drawing.Size(183, 31);
            this.Floor2CeilingDC.TabIndex = 22;
            this.Floor2CeilingDC.Validating += new System.ComponentModel.CancelEventHandler(this.Floor2CeilingDC_Validating);
            this.Floor2CeilingDC.Validated += new System.EventHandler(this.Floor2CeilingDC_Validated);
            // 
            // Floor2DropCeilingDC
            // 
            this.Floor2DropCeilingDC.Location = new System.Drawing.Point(242, 156);
            this.Floor2DropCeilingDC.Margin = new System.Windows.Forms.Padding(0);
            this.Floor2DropCeilingDC.Name = "Floor2DropCeilingDC";
            this.Floor2DropCeilingDC.RoundingIncrement = distance1;
            this.Floor2DropCeilingDC.Size = new System.Drawing.Size(183, 31);
            this.Floor2DropCeilingDC.TabIndex = 23;
            this.Floor2DropCeilingDC.Validating += new System.ComponentModel.CancelEventHandler(this.Floor2DropCeilingDC_Validating);
            this.Floor2DropCeilingDC.Validated += new System.EventHandler(this.Floor2DropCeilingDC_Validated);
            // 
            // LevelElevDC
            // 
            this.LevelElevDC.Location = new System.Drawing.Point(242, 310);
            this.LevelElevDC.Margin = new System.Windows.Forms.Padding(0);
            this.LevelElevDC.Name = "LevelElevDC";
            this.LevelElevDC.RoundingIncrement = distance1;
            this.LevelElevDC.Size = new System.Drawing.Size(183, 31);
            this.LevelElevDC.TabIndex = 24;
            this.LevelElevDC.Validated += new System.EventHandler(this.LevelElevDC_Validated);
            // 
            // LevelsDlg
            // 
            this.AcceptButton = this.CloseBtn;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CloseBtn;
            this.ClientSize = new System.Drawing.Size(449, 430);
            this.Controls.Add(this.LevelElevDC);
            this.Controls.Add(this.Floor2DropCeilingDC);
            this.Controls.Add(this.Floor2CeilingDC);
            this.Controls.Add(this.Floor2FloorDC);
            this.Controls.Add(this.WindowHeightBtn);
            this.Controls.Add(this.UndoBtn);
            this.Controls.Add(this.CloseBtn);
            this.Controls.Add(this.Copy2Lbl);
            this.Controls.Add(this.Copy1Lbl);
            this.Controls.Add(this.CopyCB);
            this.Controls.Add(this.LockCB);
            this.Controls.Add(this.Floor2DCNoneCB);
            this.Controls.Add(this.MoveDownBtn);
            this.Controls.Add(this.MoveUpBtn);
            this.Controls.Add(this.LevelsLB);
            this.Controls.Add(this.Floor2FloorLbl);
            this.Controls.Add(this.Floor2CeilingLbl);
            this.Controls.Add(this.Floor2DropCeilingLbl);
            this.Controls.Add(this.LevelElevLbl);
            this.Controls.Add(this.LevelRefCB);
            this.Controls.Add(this.LevelDiagramPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "LevelsDlg";
            this.Text = "Survey Levels";
            this.Load += new System.EventHandler(this.LevelsDlg_Load);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.LevelsDlg_Paint);
            ((System.ComponentModel.ISupportInitialize)(this.F2FEP)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.F2CEP)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.F2DCEP)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label Copy2Lbl;
        private System.Windows.Forms.Label Copy1Lbl;
        private System.Windows.Forms.CheckBox CopyCB;
        private System.Windows.Forms.CheckBox LockCB;
        private System.Windows.Forms.CheckBox Floor2DCNoneCB;
        private System.Windows.Forms.Button MoveDownBtn;
        private System.Windows.Forms.Button MoveUpBtn;
        private System.Windows.Forms.ListBox LevelsLB;
        private System.Windows.Forms.Label Floor2FloorLbl;
        private System.Windows.Forms.Label Floor2CeilingLbl;
        private System.Windows.Forms.Label Floor2DropCeilingLbl;
        private System.Windows.Forms.Label LevelElevLbl;
        private System.Windows.Forms.CheckBox LevelRefCB;
        private System.Windows.Forms.Panel LevelDiagramPanel;
        private System.Windows.Forms.Button CloseBtn;
        private System.Windows.Forms.Button UndoBtn;
        private System.Windows.Forms.Button WindowHeightBtn;
        private System.Windows.Forms.ErrorProvider F2FEP;
        private System.Windows.Forms.ErrorProvider F2CEP;
        private System.Windows.Forms.ErrorProvider F2DCEP;
        private DistanceControl Floor2FloorDC;
        private DistanceControl Floor2CeilingDC;
        private DistanceControl Floor2DropCeilingDC;
        private DistanceControl LevelElevDC;
    }
}