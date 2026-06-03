namespace PKNailForRevit
{
    partial class SurveyDoorDlg
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
            this.CancelBtn = new System.Windows.Forms.Button();
            this.OKBtn = new System.Windows.Forms.Button();
            this.HeightLbl = new System.Windows.Forms.Label();
            this.WidthLbl = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.LateralOrientationCB = new System.Windows.Forms.ComboBox();
            this.LateralOrientationLbl = new System.Windows.Forms.Label();
            this.FacingOrientationLbl = new System.Windows.Forms.Label();
            this.FacingOrientationCB = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.TypeCB = new System.Windows.Forms.ComboBox();
            this.SillHeightLbl = new System.Windows.Forms.Label();
            this.CenterChkBx = new System.Windows.Forms.CheckBox();
            this.WidthDC = new PKNailForRevit.DistanceControl();
            this.HeightDC = new PKNailForRevit.DistanceControl();
            this.SillHeightDC = new PKNailForRevit.DistanceControl();
            this.LocationDC = new PKNailForRevit.DistanceControl();
            this.SuspendLayout();
            // 
            // CancelBtn
            // 
            this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CancelBtn.Location = new System.Drawing.Point(103, 442);
            this.CancelBtn.Name = "CancelBtn";
            this.CancelBtn.Size = new System.Drawing.Size(75, 28);
            this.CancelBtn.TabIndex = 16;
            this.CancelBtn.Text = "Cancel";
            this.CancelBtn.UseVisualStyleBackColor = true;
            // 
            // OKBtn
            // 
            this.OKBtn.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.OKBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.OKBtn.Location = new System.Drawing.Point(21, 442);
            this.OKBtn.Name = "OKBtn";
            this.OKBtn.Size = new System.Drawing.Size(75, 28);
            this.OKBtn.TabIndex = 15;
            this.OKBtn.Text = "OK";
            this.OKBtn.UseVisualStyleBackColor = true;
            // 
            // HeightLbl
            // 
            this.HeightLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.HeightLbl.Location = new System.Drawing.Point(6, 195);
            this.HeightLbl.Margin = new System.Windows.Forms.Padding(0);
            this.HeightLbl.Name = "HeightLbl";
            this.HeightLbl.Size = new System.Drawing.Size(58, 16);
            this.HeightLbl.TabIndex = 7;
            this.HeightLbl.Text = "Height";
            // 
            // WidthLbl
            // 
            this.WidthLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.WidthLbl.Location = new System.Drawing.Point(6, 129);
            this.WidthLbl.Margin = new System.Windows.Forms.Padding(0);
            this.WidthLbl.Name = "WidthLbl";
            this.WidthLbl.Size = new System.Drawing.Size(52, 16);
            this.WidthLbl.TabIndex = 5;
            this.WidthLbl.Text = "Width";
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(6, 8);
            this.label1.Margin = new System.Windows.Forms.Padding(0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(72, 16);
            this.label1.TabIndex = 0;
            this.label1.Text = "Location";
            // 
            // LateralOrientationCB
            // 
            this.LateralOrientationCB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.LateralOrientationCB.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LateralOrientationCB.FormattingEnabled = true;
            this.LateralOrientationCB.Location = new System.Drawing.Point(9, 344);
            this.LateralOrientationCB.Margin = new System.Windows.Forms.Padding(8, 4, 8, 14);
            this.LateralOrientationCB.Name = "LateralOrientationCB";
            this.LateralOrientationCB.Size = new System.Drawing.Size(110, 24);
            this.LateralOrientationCB.TabIndex = 12;
            // 
            // LateralOrientationLbl
            // 
            this.LateralOrientationLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LateralOrientationLbl.Location = new System.Drawing.Point(6, 324);
            this.LateralOrientationLbl.Margin = new System.Windows.Forms.Padding(0);
            this.LateralOrientationLbl.Name = "LateralOrientationLbl";
            this.LateralOrientationLbl.Size = new System.Drawing.Size(142, 16);
            this.LateralOrientationLbl.TabIndex = 11;
            this.LateralOrientationLbl.Text = "Hinge Side";
            // 
            // FacingOrientationLbl
            // 
            this.FacingOrientationLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FacingOrientationLbl.Location = new System.Drawing.Point(6, 381);
            this.FacingOrientationLbl.Margin = new System.Windows.Forms.Padding(0);
            this.FacingOrientationLbl.Name = "FacingOrientationLbl";
            this.FacingOrientationLbl.Size = new System.Drawing.Size(142, 16);
            this.FacingOrientationLbl.TabIndex = 13;
            this.FacingOrientationLbl.Text = "Swing Direction";
            // 
            // FacingOrientationCB
            // 
            this.FacingOrientationCB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.FacingOrientationCB.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FacingOrientationCB.FormattingEnabled = true;
            this.FacingOrientationCB.Location = new System.Drawing.Point(9, 401);
            this.FacingOrientationCB.Margin = new System.Windows.Forms.Padding(8, 4, 8, 14);
            this.FacingOrientationCB.Name = "FacingOrientationCB";
            this.FacingOrientationCB.Size = new System.Drawing.Size(110, 24);
            this.FacingOrientationCB.TabIndex = 14;
            // 
            // label6
            // 
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(6, 72);
            this.label6.Margin = new System.Windows.Forms.Padding(0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(55, 16);
            this.label6.TabIndex = 3;
            this.label6.Text = "Family";
            // 
            // TypeCB
            // 
            this.TypeCB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.TypeCB.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TypeCB.FormattingEnabled = true;
            this.TypeCB.Location = new System.Drawing.Point(9, 92);
            this.TypeCB.Margin = new System.Windows.Forms.Padding(8, 4, 8, 14);
            this.TypeCB.Name = "TypeCB";
            this.TypeCB.Size = new System.Drawing.Size(174, 24);
            this.TypeCB.TabIndex = 4;
            // 
            // SillHeightLbl
            // 
            this.SillHeightLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SillHeightLbl.Location = new System.Drawing.Point(6, 260);
            this.SillHeightLbl.Margin = new System.Windows.Forms.Padding(0);
            this.SillHeightLbl.Name = "SillHeightLbl";
            this.SillHeightLbl.Size = new System.Drawing.Size(84, 16);
            this.SillHeightLbl.TabIndex = 9;
            this.SillHeightLbl.Text = "Sill Height";
            // 
            // CenterChkBx
            // 
            this.CenterChkBx.AutoSize = true;
            this.CenterChkBx.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CenterChkBx.Location = new System.Drawing.Point(126, 7);
            this.CenterChkBx.Margin = new System.Windows.Forms.Padding(8, 4, 8, 14);
            this.CenterChkBx.Name = "CenterChkBx";
            this.CenterChkBx.Size = new System.Drawing.Size(66, 20);
            this.CenterChkBx.TabIndex = 1;
            this.CenterChkBx.Text = "Center";
            this.CenterChkBx.UseVisualStyleBackColor = true;
            // 
            // WidthDC
            // 
            this.WidthDC.Location = new System.Drawing.Point(9, 149);
            this.WidthDC.Margin = new System.Windows.Forms.Padding(0);
            this.WidthDC.Name = "WidthDC";
            this.WidthDC.RoundingIncrement = distance1;
            this.WidthDC.Size = new System.Drawing.Size(183, 31);
            this.WidthDC.TabIndex = 6;
            // 
            // HeightDC
            // 
            this.HeightDC.Location = new System.Drawing.Point(9, 215);
            this.HeightDC.Margin = new System.Windows.Forms.Padding(0);
            this.HeightDC.Name = "HeightDC";
            this.HeightDC.RoundingIncrement = distance1;
            this.HeightDC.Size = new System.Drawing.Size(183, 31);
            this.HeightDC.TabIndex = 8;
            // 
            // SillHeightDC
            // 
            this.SillHeightDC.Location = new System.Drawing.Point(9, 280);
            this.SillHeightDC.Margin = new System.Windows.Forms.Padding(0);
            this.SillHeightDC.Name = "SillHeightDC";
            this.SillHeightDC.RoundingIncrement = distance1;
            this.SillHeightDC.Size = new System.Drawing.Size(183, 31);
            this.SillHeightDC.TabIndex = 10;
            // 
            // LocationDC
            // 
            this.LocationDC.Location = new System.Drawing.Point(9, 28);
            this.LocationDC.Margin = new System.Windows.Forms.Padding(0);
            this.LocationDC.Name = "LocationDC";
            this.LocationDC.RoundingIncrement = distance2;
            this.LocationDC.Size = new System.Drawing.Size(183, 31);
            this.LocationDC.TabIndex = 2;
            // 
            // SurveyDoorDlg
            // 
            this.AcceptButton = this.OKBtn;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(255)))));
            this.CancelButton = this.CancelBtn;
            this.ClientSize = new System.Drawing.Size(199, 484);
            this.Controls.Add(this.LocationDC);
            this.Controls.Add(this.SillHeightDC);
            this.Controls.Add(this.HeightDC);
            this.Controls.Add(this.WidthDC);
            this.Controls.Add(this.CenterChkBx);
            this.Controls.Add(this.SillHeightLbl);
            this.Controls.Add(this.TypeCB);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.FacingOrientationCB);
            this.Controls.Add(this.FacingOrientationLbl);
            this.Controls.Add(this.LateralOrientationLbl);
            this.Controls.Add(this.LateralOrientationCB);
            this.Controls.Add(this.CancelBtn);
            this.Controls.Add(this.OKBtn);
            this.Controls.Add(this.HeightLbl);
            this.Controls.Add(this.WidthLbl);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SurveyDoorDlg";
            this.Text = "Capture Door";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button CancelBtn;
        private System.Windows.Forms.Button OKBtn;
        private System.Windows.Forms.Label HeightLbl;
        private System.Windows.Forms.Label WidthLbl;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox LateralOrientationCB;
        private System.Windows.Forms.Label LateralOrientationLbl;
        private System.Windows.Forms.Label FacingOrientationLbl;
        private System.Windows.Forms.ComboBox FacingOrientationCB;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox TypeCB;
        private System.Windows.Forms.Label SillHeightLbl;
        private System.Windows.Forms.CheckBox CenterChkBx;
        private DistanceControl WidthDC;
        private DistanceControl HeightDC;
        private DistanceControl SillHeightDC;
        private DistanceControl LocationDC;
    }
}