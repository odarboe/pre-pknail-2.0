namespace PKNailForRevit
{
    partial class SurveyLineDlg
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
            this.LocationLbl = new System.Windows.Forms.Label();
            this.LengthLbl = new System.Windows.Forms.Label();
            this.CornerAngleMeasureBtn = new System.Windows.Forms.Button();
            this.CornerAngleCB = new System.Windows.Forms.ComboBox();
            this.CornerAngleLbl = new System.Windows.Forms.Label();
            this.CancelBtn = new System.Windows.Forms.Button();
            this.OKBtn = new System.Windows.Forms.Button();
            this.CenterChkBx = new System.Windows.Forms.CheckBox();
            this.TypeCB = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.LocationDC = new PKNailForRevit.DistanceControl();
            this.LengthDC = new PKNailForRevit.DistanceControl();
            this.SuspendLayout();
            // 
            // LocationLbl
            // 
            this.LocationLbl.AutoSize = true;
            this.LocationLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LocationLbl.Location = new System.Drawing.Point(6, 8);
            this.LocationLbl.Margin = new System.Windows.Forms.Padding(0);
            this.LocationLbl.Name = "LocationLbl";
            this.LocationLbl.Size = new System.Drawing.Size(67, 16);
            this.LocationLbl.TabIndex = 0;
            this.LocationLbl.Text = "Location";
            // 
            // LengthLbl
            // 
            this.LengthLbl.AutoSize = true;
            this.LengthLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LengthLbl.Location = new System.Drawing.Point(6, 133);
            this.LengthLbl.Margin = new System.Windows.Forms.Padding(0);
            this.LengthLbl.Name = "LengthLbl";
            this.LengthLbl.Size = new System.Drawing.Size(54, 16);
            this.LengthLbl.TabIndex = 6;
            this.LengthLbl.Text = "Length";
            // 
            // CornerAngleMeasureBtn
            // 
            this.CornerAngleMeasureBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CornerAngleMeasureBtn.Location = new System.Drawing.Point(110, 91);
            this.CornerAngleMeasureBtn.Name = "CornerAngleMeasureBtn";
            this.CornerAngleMeasureBtn.Size = new System.Drawing.Size(80, 28);
            this.CornerAngleMeasureBtn.TabIndex = 5;
            this.CornerAngleMeasureBtn.Text = "Measure...";
            this.CornerAngleMeasureBtn.UseVisualStyleBackColor = true;
            this.CornerAngleMeasureBtn.Click += new System.EventHandler(this.CornerAngleMeasureBtn_Click);
            // 
            // CornerAngleCB
            // 
            this.CornerAngleCB.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CornerAngleCB.FormattingEnabled = true;
            this.CornerAngleCB.Location = new System.Drawing.Point(9, 94);
            this.CornerAngleCB.Margin = new System.Windows.Forms.Padding(8, 4, 8, 14);
            this.CornerAngleCB.Name = "CornerAngleCB";
            this.CornerAngleCB.Size = new System.Drawing.Size(90, 24);
            this.CornerAngleCB.TabIndex = 4;
            this.CornerAngleCB.SelectedIndexChanged += new System.EventHandler(this.CornerAngleCB_SelectedIndexChanged);
            // 
            // CornerAngleLbl
            // 
            this.CornerAngleLbl.AutoSize = true;
            this.CornerAngleLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CornerAngleLbl.Location = new System.Drawing.Point(6, 74);
            this.CornerAngleLbl.Margin = new System.Windows.Forms.Padding(0);
            this.CornerAngleLbl.Name = "CornerAngleLbl";
            this.CornerAngleLbl.Size = new System.Drawing.Size(98, 16);
            this.CornerAngleLbl.TabIndex = 3;
            this.CornerAngleLbl.Text = "Corner Angle";
            // 
            // CancelBtn
            // 
            this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CancelBtn.Location = new System.Drawing.Point(103, 263);
            this.CancelBtn.Name = "CancelBtn";
            this.CancelBtn.Size = new System.Drawing.Size(75, 28);
            this.CancelBtn.TabIndex = 11;
            this.CancelBtn.Text = "Cancel";
            this.CancelBtn.UseVisualStyleBackColor = true;
            // 
            // OKBtn
            // 
            this.OKBtn.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.OKBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.OKBtn.Location = new System.Drawing.Point(22, 263);
            this.OKBtn.Name = "OKBtn";
            this.OKBtn.Size = new System.Drawing.Size(75, 28);
            this.OKBtn.TabIndex = 10;
            this.OKBtn.Text = "OK";
            this.OKBtn.UseVisualStyleBackColor = true;
            this.OKBtn.Click += new System.EventHandler(this.OKBtn_Click);
            // 
            // CenterChkBx
            // 
            this.CenterChkBx.AutoSize = true;
            this.CenterChkBx.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CenterChkBx.Location = new System.Drawing.Point(124, 8);
            this.CenterChkBx.Margin = new System.Windows.Forms.Padding(8, 4, 8, 14);
            this.CenterChkBx.Name = "CenterChkBx";
            this.CenterChkBx.Size = new System.Drawing.Size(66, 20);
            this.CenterChkBx.TabIndex = 1;
            this.CenterChkBx.Text = "Center";
            this.CenterChkBx.UseVisualStyleBackColor = true;
            this.CenterChkBx.CheckedChanged += new System.EventHandler(this.CenterChkBx_CheckedChanged);
            // 
            // TypeCB
            // 
            this.TypeCB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.TypeCB.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TypeCB.FormattingEnabled = true;
            this.TypeCB.Location = new System.Drawing.Point(9, 221);
            this.TypeCB.Margin = new System.Windows.Forms.Padding(8, 4, 8, 14);
            this.TypeCB.Name = "TypeCB";
            this.TypeCB.Size = new System.Drawing.Size(174, 24);
            this.TypeCB.TabIndex = 9;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(6, 199);
            this.label1.Margin = new System.Windows.Forms.Padding(0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(43, 16);
            this.label1.TabIndex = 8;
            this.label1.Text = "Style";
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
            // LengthDC
            // 
            this.LengthDC.Location = new System.Drawing.Point(9, 154);
            this.LengthDC.Margin = new System.Windows.Forms.Padding(0);
            this.LengthDC.Name = "LengthDC";
            this.LengthDC.RoundingIncrement = distance1;
            this.LengthDC.Size = new System.Drawing.Size(183, 31);
            this.LengthDC.TabIndex = 7;
            // 
            // SurveyLineDlg
            // 
            this.AcceptButton = this.OKBtn;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(210)))));
            this.CancelButton = this.CancelBtn;
            this.ClientSize = new System.Drawing.Size(199, 306);
            this.Controls.Add(this.LengthDC);
            this.Controls.Add(this.LocationDC);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.TypeCB);
            this.Controls.Add(this.CenterChkBx);
            this.Controls.Add(this.CancelBtn);
            this.Controls.Add(this.OKBtn);
            this.Controls.Add(this.CornerAngleMeasureBtn);
            this.Controls.Add(this.CornerAngleCB);
            this.Controls.Add(this.CornerAngleLbl);
            this.Controls.Add(this.LengthLbl);
            this.Controls.Add(this.LocationLbl);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SurveyLineDlg";
            this.Text = "Capture Line";
            this.Load += new System.EventHandler(this.surveyLineDlg_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label LocationLbl;
        private System.Windows.Forms.Label LengthLbl;
        private System.Windows.Forms.Button CornerAngleMeasureBtn;
        private System.Windows.Forms.ComboBox CornerAngleCB;
        private System.Windows.Forms.Label CornerAngleLbl;
        private System.Windows.Forms.Button CancelBtn;
        private System.Windows.Forms.Button OKBtn;
        private System.Windows.Forms.CheckBox CenterChkBx;
        private System.Windows.Forms.ComboBox TypeCB;
        private System.Windows.Forms.Label label1;
        private DistanceControl LocationDC;
        private DistanceControl LengthDC;
    }
}