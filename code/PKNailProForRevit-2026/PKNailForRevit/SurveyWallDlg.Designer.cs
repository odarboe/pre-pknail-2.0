namespace PKNailForRevit
{
    partial class SurveyWallDlg
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
            PKNailForRevit.Distance distance1 = new PKNailForRevit.Distance();
            PKNailForRevit.Distance distance2 = new PKNailForRevit.Distance();
            PKNailForRevit.Distance distance3 = new PKNailForRevit.Distance();
            this.HeightLbl = new System.Windows.Forms.Label();
            this.CornerAngleLbl = new System.Windows.Forms.Label();
            this.LengthLbl = new System.Windows.Forms.Label();
            this.OKBtn = new System.Windows.Forms.Button();
            this.CancelBtn = new System.Windows.Forms.Button();
            this.CornerAngleCB = new System.Windows.Forms.ComboBox();
            this.WidthLbl = new System.Windows.Forms.Label();
            this.UnknownWidthCheck = new System.Windows.Forms.CheckBox();
            this.CloseCheck = new System.Windows.Forms.CheckBox();
            this.CornerAngleMeasureBtn = new System.Windows.Forms.Button();
            this.ConnectedLevelCB = new System.Windows.Forms.ComboBox();
            this.WidthCB = new System.Windows.Forms.ComboBox();
            this.LevelOffsetLbl = new System.Windows.Forms.Label();
            this.LengthDC = new PKNailForRevit.DistanceControl();
            this.WidthDC = new PKNailForRevit.DistanceControl();
            this.HeightDC = new PKNailForRevit.DistanceControl();
            this.LevelOffsetDC = new PKNailForRevit.DistanceControl();
            this.SuspendLayout();
            // 
            // HeightLbl
            // 
            this.HeightLbl.AutoSize = true;
            this.HeightLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.HeightLbl.Location = new System.Drawing.Point(5, 252);
            this.HeightLbl.Margin = new System.Windows.Forms.Padding(0);
            this.HeightLbl.Name = "HeightLbl";
            this.HeightLbl.Size = new System.Drawing.Size(53, 16);
            this.HeightLbl.TabIndex = 10;
            this.HeightLbl.Text = "Height";
            // 
            // CornerAngleLbl
            // 
            this.CornerAngleLbl.AutoSize = true;
            this.CornerAngleLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CornerAngleLbl.Location = new System.Drawing.Point(5, 9);
            this.CornerAngleLbl.Margin = new System.Windows.Forms.Padding(0);
            this.CornerAngleLbl.Name = "CornerAngleLbl";
            this.CornerAngleLbl.Size = new System.Drawing.Size(98, 16);
            this.CornerAngleLbl.TabIndex = 0;
            this.CornerAngleLbl.Text = "Corner Angle";
            // 
            // LengthLbl
            // 
            this.LengthLbl.AutoSize = true;
            this.LengthLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LengthLbl.Location = new System.Drawing.Point(5, 84);
            this.LengthLbl.Margin = new System.Windows.Forms.Padding(0);
            this.LengthLbl.Name = "LengthLbl";
            this.LengthLbl.Size = new System.Drawing.Size(54, 16);
            this.LengthLbl.TabIndex = 4;
            this.LengthLbl.Text = "Length";
            // 
            // OKBtn
            // 
            this.OKBtn.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.OKBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.OKBtn.Location = new System.Drawing.Point(21, 419);
            this.OKBtn.Name = "OKBtn";
            this.OKBtn.Size = new System.Drawing.Size(75, 28);
            this.OKBtn.TabIndex = 15;
            this.OKBtn.Text = "OK";
            this.OKBtn.UseVisualStyleBackColor = true;
            this.OKBtn.Click += new System.EventHandler(this.OKBtn_Click);
            // 
            // CancelBtn
            // 
            this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CancelBtn.Location = new System.Drawing.Point(102, 419);
            this.CancelBtn.Name = "CancelBtn";
            this.CancelBtn.Size = new System.Drawing.Size(75, 28);
            this.CancelBtn.TabIndex = 16;
            this.CancelBtn.Text = "Cancel";
            this.CancelBtn.UseVisualStyleBackColor = true;
            // 
            // CornerAngleCB
            // 
            this.CornerAngleCB.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CornerAngleCB.FormattingEnabled = true;
            this.CornerAngleCB.Location = new System.Drawing.Point(8, 28);
            this.CornerAngleCB.Margin = new System.Windows.Forms.Padding(8, 4, 8, 14);
            this.CornerAngleCB.Name = "CornerAngleCB";
            this.CornerAngleCB.Size = new System.Drawing.Size(90, 24);
            this.CornerAngleCB.TabIndex = 1;
            this.CornerAngleCB.SelectedIndexChanged += new System.EventHandler(this.CornerAngleCB_SelectedIndexChanged);
            this.CornerAngleCB.TextUpdate += new System.EventHandler(this.CornerAngleCB_TextUpdate);
            this.CornerAngleCB.Validating += new System.ComponentModel.CancelEventHandler(this.CornerAngleCB_Validating);
            // 
            // WidthLbl
            // 
            this.WidthLbl.AutoSize = true;
            this.WidthLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.WidthLbl.Location = new System.Drawing.Point(5, 152);
            this.WidthLbl.Margin = new System.Windows.Forms.Padding(0);
            this.WidthLbl.Name = "WidthLbl";
            this.WidthLbl.Size = new System.Drawing.Size(47, 16);
            this.WidthLbl.TabIndex = 6;
            this.WidthLbl.Text = "Width";
            // 
            // UnknownWidthCheck
            // 
            this.UnknownWidthCheck.AutoSize = true;
            this.UnknownWidthCheck.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.UnknownWidthCheck.Location = new System.Drawing.Point(111, 151);
            this.UnknownWidthCheck.Name = "UnknownWidthCheck";
            this.UnknownWidthCheck.Size = new System.Drawing.Size(82, 20);
            this.UnknownWidthCheck.TabIndex = 7;
            this.UnknownWidthCheck.Text = "Unknown";
            this.UnknownWidthCheck.UseVisualStyleBackColor = true;
            this.UnknownWidthCheck.CheckedChanged += new System.EventHandler(this.UnknownWidthCheck_CheckedChanged);
            // 
            // CloseCheck
            // 
            this.CloseCheck.AutoSize = true;
            this.CloseCheck.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CloseCheck.Location = new System.Drawing.Point(111, 58);
            this.CloseCheck.Name = "CloseCheck";
            this.CloseCheck.Size = new System.Drawing.Size(62, 20);
            this.CloseCheck.TabIndex = 3;
            this.CloseCheck.Text = "Close";
            this.CloseCheck.UseVisualStyleBackColor = true;
            this.CloseCheck.CheckedChanged += new System.EventHandler(this.CloseCheck_CheckedChanged);
            // 
            // CornerAngleMeasureBtn
            // 
            this.CornerAngleMeasureBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CornerAngleMeasureBtn.Location = new System.Drawing.Point(111, 25);
            this.CornerAngleMeasureBtn.Name = "CornerAngleMeasureBtn";
            this.CornerAngleMeasureBtn.Size = new System.Drawing.Size(80, 28);
            this.CornerAngleMeasureBtn.TabIndex = 2;
            this.CornerAngleMeasureBtn.Text = "Measure...";
            this.CornerAngleMeasureBtn.UseVisualStyleBackColor = true;
            this.CornerAngleMeasureBtn.Click += new System.EventHandler(this.CornerAngleMeasureBtn_Click);
            // 
            // ConnectedLevelCB
            // 
            this.ConnectedLevelCB.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ConnectedLevelCB.FormattingEnabled = true;
            this.ConnectedLevelCB.Location = new System.Drawing.Point(8, 271);
            this.ConnectedLevelCB.Margin = new System.Windows.Forms.Padding(8, 4, 8, 14);
            this.ConnectedLevelCB.Name = "ConnectedLevelCB";
            this.ConnectedLevelCB.Size = new System.Drawing.Size(183, 24);
            this.ConnectedLevelCB.TabIndex = 11;
            this.ConnectedLevelCB.SelectedIndexChanged += new System.EventHandler(this.ConnectedLevelCB_SelectedIndexChanged);
            // 
            // WidthCB
            // 
            this.WidthCB.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.WidthCB.FormattingEnabled = true;
            this.WidthCB.Location = new System.Drawing.Point(8, 171);
            this.WidthCB.Margin = new System.Windows.Forms.Padding(8, 4, 8, 14);
            this.WidthCB.Name = "WidthCB";
            this.WidthCB.Size = new System.Drawing.Size(90, 24);
            this.WidthCB.TabIndex = 8;
            this.WidthCB.SelectedIndexChanged += new System.EventHandler(this.WidthCB_SelectedIndexChanged);
            // 
            // LevelOffsetLbl
            // 
            this.LevelOffsetLbl.AutoSize = true;
            this.LevelOffsetLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LevelOffsetLbl.Location = new System.Drawing.Point(5, 351);
            this.LevelOffsetLbl.Margin = new System.Windows.Forms.Padding(0);
            this.LevelOffsetLbl.Name = "LevelOffsetLbl";
            this.LevelOffsetLbl.Size = new System.Drawing.Size(90, 16);
            this.LevelOffsetLbl.TabIndex = 13;
            this.LevelOffsetLbl.Text = "Level Offset";
            // 
            // LengthDC
            // 
            this.LengthDC.Location = new System.Drawing.Point(8, 104);
            this.LengthDC.Margin = new System.Windows.Forms.Padding(0);
            this.LengthDC.Name = "LengthDC";
            this.LengthDC.RoundingIncrement = distance1;
            this.LengthDC.Size = new System.Drawing.Size(183, 31);
            this.LengthDC.TabIndex = 5;
            // 
            // WidthDC
            // 
            this.WidthDC.Location = new System.Drawing.Point(8, 205);
            this.WidthDC.Margin = new System.Windows.Forms.Padding(0);
            this.WidthDC.Name = "WidthDC";
            this.WidthDC.RoundingIncrement = distance2;
            this.WidthDC.Size = new System.Drawing.Size(183, 31);
            this.WidthDC.TabIndex = 9;
            // 
            // HeightDC
            // 
            this.HeightDC.Location = new System.Drawing.Point(8, 306);
            this.HeightDC.Margin = new System.Windows.Forms.Padding(0);
            this.HeightDC.Name = "HeightDC";
            this.HeightDC.RoundingIncrement = distance3;
            this.HeightDC.Size = new System.Drawing.Size(183, 31);
            this.HeightDC.TabIndex = 12;
            // 
            // LevelOffsetDC
            // 
            this.LevelOffsetDC.Location = new System.Drawing.Point(8, 371);
            this.LevelOffsetDC.Margin = new System.Windows.Forms.Padding(0);
            this.LevelOffsetDC.Name = "LevelOffsetDC";
            this.LevelOffsetDC.RoundingIncrement = distance3;
            this.LevelOffsetDC.Size = new System.Drawing.Size(183, 31);
            this.LevelOffsetDC.TabIndex = 14;
            // 
            // SurveyWallDlg
            // 
            this.AcceptButton = this.OKBtn;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(255)))), ((int)(((byte)(220)))));
            this.CancelButton = this.CancelBtn;
            this.ClientSize = new System.Drawing.Size(199, 461);
            this.Controls.Add(this.LevelOffsetDC);
            this.Controls.Add(this.HeightDC);
            this.Controls.Add(this.WidthDC);
            this.Controls.Add(this.LengthDC);
            this.Controls.Add(this.LevelOffsetLbl);
            this.Controls.Add(this.WidthCB);
            this.Controls.Add(this.ConnectedLevelCB);
            this.Controls.Add(this.CornerAngleMeasureBtn);
            this.Controls.Add(this.CloseCheck);
            this.Controls.Add(this.UnknownWidthCheck);
            this.Controls.Add(this.WidthLbl);
            this.Controls.Add(this.CornerAngleCB);
            this.Controls.Add(this.CancelBtn);
            this.Controls.Add(this.OKBtn);
            this.Controls.Add(this.HeightLbl);
            this.Controls.Add(this.CornerAngleLbl);
            this.Controls.Add(this.LengthLbl);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SurveyWallDlg";
            this.Padding = new System.Windows.Forms.Padding(8);
            this.Text = "Capture Wall";
            this.Load += new System.EventHandler(this.surveyWallDlg_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label HeightLbl;
        private System.Windows.Forms.Label CornerAngleLbl;
        private System.Windows.Forms.Label LengthLbl;
        private System.Windows.Forms.Button OKBtn;
        private System.Windows.Forms.Button CancelBtn;
        private System.Windows.Forms.ComboBox CornerAngleCB;
        private System.Windows.Forms.Label WidthLbl;
        private System.Windows.Forms.CheckBox UnknownWidthCheck;
        private System.Windows.Forms.CheckBox CloseCheck;
        private System.Windows.Forms.Button CornerAngleMeasureBtn;
        private System.Windows.Forms.ComboBox ConnectedLevelCB;
        private System.Windows.Forms.ComboBox WidthCB;
        private System.Windows.Forms.Label LevelOffsetLbl;
        private DistanceControl LengthDC;
        private DistanceControl WidthDC;
        private DistanceControl HeightDC;
        private DistanceControl LevelOffsetDC;

    }
}