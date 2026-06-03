namespace PKNailForRevit
{
    partial class SurveyHostedObjectDlg
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
            this.CenterChkBx = new System.Windows.Forms.CheckBox();
            this.TypeCB = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.CancelBtn = new System.Windows.Forms.Button();
            this.OKBtn = new System.Windows.Forms.Button();
            this.SillHeightLbl = new System.Windows.Forms.Label();
            this.HeightLbl = new System.Windows.Forms.Label();
            this.WidthLbl = new System.Windows.Forms.Label();
            this.DepthLbl = new System.Windows.Forms.Label();
            this.LocationDC = new PKNailForRevit.DistanceControl();
            this.WidthDC = new PKNailForRevit.DistanceControl();
            this.HeightDC = new PKNailForRevit.DistanceControl();
            this.SillHeightDC = new PKNailForRevit.DistanceControl();
            this.DepthDC = new PKNailForRevit.DistanceControl();
            this.SuspendLayout();
            // 
            // CenterChkBx
            // 
            this.CenterChkBx.AutoSize = true;
            this.CenterChkBx.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CenterChkBx.Location = new System.Drawing.Point(126, 8);
            this.CenterChkBx.Margin = new System.Windows.Forms.Padding(8, 4, 8, 14);
            this.CenterChkBx.Name = "CenterChkBx";
            this.CenterChkBx.Size = new System.Drawing.Size(66, 20);
            this.CenterChkBx.TabIndex = 1;
            this.CenterChkBx.Text = "Center";
            this.CenterChkBx.UseVisualStyleBackColor = true;
            // 
            // TypeCB
            // 
            this.TypeCB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.TypeCB.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TypeCB.FormattingEnabled = true;
            this.TypeCB.Location = new System.Drawing.Point(9, 93);
            this.TypeCB.Margin = new System.Windows.Forms.Padding(8, 4, 8, 14);
            this.TypeCB.Name = "TypeCB";
            this.TypeCB.Size = new System.Drawing.Size(174, 24);
            this.TypeCB.TabIndex = 4;
            // 
            // label6
            // 
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(6, 73);
            this.label6.Margin = new System.Windows.Forms.Padding(0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(59, 16);
            this.label6.TabIndex = 3;
            this.label6.Text = "Family";
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(6, 9);
            this.label1.Margin = new System.Windows.Forms.Padding(0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(72, 16);
            this.label1.TabIndex = 0;
            this.label1.Text = "Location";
            // 
            // CancelBtn
            // 
            this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CancelBtn.Location = new System.Drawing.Point(103, 396);
            this.CancelBtn.Name = "CancelBtn";
            this.CancelBtn.Size = new System.Drawing.Size(75, 28);
            this.CancelBtn.TabIndex = 14;
            this.CancelBtn.Text = "Cancel";
            this.CancelBtn.UseVisualStyleBackColor = true;
            // 
            // OKBtn
            // 
            this.OKBtn.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.OKBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.OKBtn.Location = new System.Drawing.Point(21, 396);
            this.OKBtn.Name = "OKBtn";
            this.OKBtn.Size = new System.Drawing.Size(75, 28);
            this.OKBtn.TabIndex = 13;
            this.OKBtn.Text = "OK";
            this.OKBtn.UseVisualStyleBackColor = true;
            // 
            // SillHeightLbl
            // 
            this.SillHeightLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SillHeightLbl.Location = new System.Drawing.Point(6, 262);
            this.SillHeightLbl.Margin = new System.Windows.Forms.Padding(0);
            this.SillHeightLbl.Name = "SillHeightLbl";
            this.SillHeightLbl.Size = new System.Drawing.Size(154, 16);
            this.SillHeightLbl.TabIndex = 9;
            this.SillHeightLbl.Text = "Sill Height/Elevation";
            // 
            // HeightLbl
            // 
            this.HeightLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.HeightLbl.Location = new System.Drawing.Point(6, 197);
            this.HeightLbl.Margin = new System.Windows.Forms.Padding(0);
            this.HeightLbl.Name = "HeightLbl";
            this.HeightLbl.Size = new System.Drawing.Size(58, 16);
            this.HeightLbl.TabIndex = 7;
            this.HeightLbl.Text = "Height";
            // 
            // WidthLbl
            // 
            this.WidthLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.WidthLbl.Location = new System.Drawing.Point(6, 131);
            this.WidthLbl.Margin = new System.Windows.Forms.Padding(0);
            this.WidthLbl.Name = "WidthLbl";
            this.WidthLbl.Size = new System.Drawing.Size(52, 16);
            this.WidthLbl.TabIndex = 5;
            this.WidthLbl.Text = "Width";
            // 
            // DepthLbl
            // 
            this.DepthLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.DepthLbl.Location = new System.Drawing.Point(6, 327);
            this.DepthLbl.Margin = new System.Windows.Forms.Padding(0);
            this.DepthLbl.Name = "DepthLbl";
            this.DepthLbl.Size = new System.Drawing.Size(54, 16);
            this.DepthLbl.TabIndex = 11;
            this.DepthLbl.Text = "Depth";
            // 
            // LocationDC
            // 
            this.LocationDC.Location = new System.Drawing.Point(9, 29);
            this.LocationDC.Margin = new System.Windows.Forms.Padding(0);
            this.LocationDC.Name = "LocationDC";
            this.LocationDC.RoundingIncrement = distance1;
            this.LocationDC.Size = new System.Drawing.Size(183, 31);
            this.LocationDC.TabIndex = 2;
            // 
            // WidthDC
            // 
            this.WidthDC.Location = new System.Drawing.Point(9, 151);
            this.WidthDC.Margin = new System.Windows.Forms.Padding(0);
            this.WidthDC.Name = "WidthDC";
            this.WidthDC.RoundingIncrement = distance1;
            this.WidthDC.Size = new System.Drawing.Size(183, 31);
            this.WidthDC.TabIndex = 6;
            // 
            // HeightDC
            // 
            this.HeightDC.Location = new System.Drawing.Point(9, 217);
            this.HeightDC.Margin = new System.Windows.Forms.Padding(0);
            this.HeightDC.Name = "HeightDC";
            this.HeightDC.RoundingIncrement = distance1;
            this.HeightDC.Size = new System.Drawing.Size(183, 31);
            this.HeightDC.TabIndex = 8;
            // 
            // SillHeightDC
            // 
            this.SillHeightDC.Location = new System.Drawing.Point(9, 282);
            this.SillHeightDC.Margin = new System.Windows.Forms.Padding(0);
            this.SillHeightDC.Name = "SillHeightDC";
            this.SillHeightDC.RoundingIncrement = distance1;
            this.SillHeightDC.Size = new System.Drawing.Size(183, 31);
            this.SillHeightDC.TabIndex = 10;
            // 
            // DepthDC
            // 
            this.DepthDC.Location = new System.Drawing.Point(9, 347);
            this.DepthDC.Margin = new System.Windows.Forms.Padding(0);
            this.DepthDC.Name = "DepthDC";
            this.DepthDC.RoundingIncrement = distance1;
            this.DepthDC.Size = new System.Drawing.Size(183, 31);
            this.DepthDC.TabIndex = 12;
            // 
            // SurveyHostedObjectDlg
            // 
            this.AcceptButton = this.OKBtn;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(215)))), ((int)(((byte)(160)))));
            this.CancelButton = this.CancelBtn;
            this.ClientSize = new System.Drawing.Size(199, 437);
            this.Controls.Add(this.DepthDC);
            this.Controls.Add(this.SillHeightDC);
            this.Controls.Add(this.HeightDC);
            this.Controls.Add(this.WidthDC);
            this.Controls.Add(this.LocationDC);
            this.Controls.Add(this.DepthLbl);
            this.Controls.Add(this.SillHeightLbl);
            this.Controls.Add(this.HeightLbl);
            this.Controls.Add(this.WidthLbl);
            this.Controls.Add(this.CancelBtn);
            this.Controls.Add(this.OKBtn);
            this.Controls.Add(this.CenterChkBx);
            this.Controls.Add(this.TypeCB);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SurveyHostedObjectDlg";
            this.Text = "Capture Wall-Hosted Object";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox CenterChkBx;
        private System.Windows.Forms.ComboBox TypeCB;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button CancelBtn;
        private System.Windows.Forms.Button OKBtn;
        private System.Windows.Forms.Label SillHeightLbl;
        private System.Windows.Forms.Label HeightLbl;
        private System.Windows.Forms.Label WidthLbl;
        private System.Windows.Forms.Label DepthLbl;
        private DistanceControl LocationDC;
        private DistanceControl WidthDC;
        private DistanceControl HeightDC;
        private DistanceControl SillHeightDC;
        private DistanceControl DepthDC;
    }
}