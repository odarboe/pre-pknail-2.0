namespace PKNailForRevit
{
    partial class MeasureCornerAngleDlg
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
            this.RefDCLbl = new System.Windows.Forms.Label();
            this.SurveyDCLbl = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.RefChooseCheck = new System.Windows.Forms.CheckBox();
            this.SurveyChooseCheck = new System.Windows.Forms.CheckBox();
            this.CancelBtn = new System.Windows.Forms.Button();
            this.OKBtn = new System.Windows.Forms.Button();
            this.RefDC = new PKNailForRevit.DistanceControl();
            this.SurveyDC = new PKNailForRevit.DistanceControl();
            this.DiagDC = new PKNailForRevit.DistanceControl();
            this.SuspendLayout();
            // 
            // RefDCLbl
            // 
            this.RefDCLbl.AutoSize = true;
            this.RefDCLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.RefDCLbl.Location = new System.Drawing.Point(5, 14);
            this.RefDCLbl.Margin = new System.Windows.Forms.Padding(0);
            this.RefDCLbl.Name = "RefDCLbl";
            this.RefDCLbl.Size = new System.Drawing.Size(186, 16);
            this.RefDCLbl.TabIndex = 0;
            this.RefDCLbl.Text = "Length on Reference Wall";
            // 
            // SurveyDCLbl
            // 
            this.SurveyDCLbl.AutoSize = true;
            this.SurveyDCLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SurveyDCLbl.Location = new System.Drawing.Point(5, 80);
            this.SurveyDCLbl.Margin = new System.Windows.Forms.Padding(0);
            this.SurveyDCLbl.Name = "SurveyDCLbl";
            this.SurveyDCLbl.Size = new System.Drawing.Size(162, 16);
            this.SurveyDCLbl.TabIndex = 3;
            this.SurveyDCLbl.Text = "Length on Survey Wall";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(5, 146);
            this.label3.Margin = new System.Windows.Forms.Padding(0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(138, 16);
            this.label3.TabIndex = 6;
            this.label3.Text = "Length of Diagonal";
            // 
            // RefChooseCheck
            // 
            this.RefChooseCheck.AutoSize = true;
            this.RefChooseCheck.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.RefChooseCheck.Location = new System.Drawing.Point(199, 13);
            this.RefChooseCheck.Margin = new System.Windows.Forms.Padding(0);
            this.RefChooseCheck.Name = "RefChooseCheck";
            this.RefChooseCheck.Size = new System.Drawing.Size(80, 20);
            this.RefChooseCheck.TabIndex = 1;
            this.RefChooseCheck.Text = "Measure";
            this.RefChooseCheck.UseVisualStyleBackColor = true;
            this.RefChooseCheck.CheckedChanged += new System.EventHandler(this.RefChooseCheck_CheckedChanged);
            // 
            // SurveyChooseCheck
            // 
            this.SurveyChooseCheck.AutoSize = true;
            this.SurveyChooseCheck.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SurveyChooseCheck.Location = new System.Drawing.Point(199, 79);
            this.SurveyChooseCheck.Margin = new System.Windows.Forms.Padding(0);
            this.SurveyChooseCheck.Name = "SurveyChooseCheck";
            this.SurveyChooseCheck.Size = new System.Drawing.Size(80, 20);
            this.SurveyChooseCheck.TabIndex = 4;
            this.SurveyChooseCheck.Text = "Measure";
            this.SurveyChooseCheck.UseVisualStyleBackColor = true;
            this.SurveyChooseCheck.CheckedChanged += new System.EventHandler(this.SurveyChooseCheck_CheckedChanged);
            // 
            // CancelBtn
            // 
            this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CancelBtn.Location = new System.Drawing.Point(140, 215);
            this.CancelBtn.Name = "CancelBtn";
            this.CancelBtn.Size = new System.Drawing.Size(75, 28);
            this.CancelBtn.TabIndex = 9;
            this.CancelBtn.Text = "Cancel";
            this.CancelBtn.UseVisualStyleBackColor = true;
            this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
            // 
            // OKBtn
            // 
            this.OKBtn.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.OKBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.OKBtn.Location = new System.Drawing.Point(59, 215);
            this.OKBtn.Name = "OKBtn";
            this.OKBtn.Size = new System.Drawing.Size(75, 28);
            this.OKBtn.TabIndex = 8;
            this.OKBtn.Text = "OK";
            this.OKBtn.UseVisualStyleBackColor = true;
            this.OKBtn.Click += new System.EventHandler(this.OKBtn_Click);
            // 
            // RefDC
            // 
            this.RefDC.Location = new System.Drawing.Point(8, 34);
            this.RefDC.Margin = new System.Windows.Forms.Padding(0);
            this.RefDC.Name = "RefDC";
            this.RefDC.RoundingIncrement = distance1;
            this.RefDC.Size = new System.Drawing.Size(183, 31);
            this.RefDC.TabIndex = 2;
            // 
            // SurveyDC
            // 
            this.SurveyDC.Location = new System.Drawing.Point(8, 100);
            this.SurveyDC.Margin = new System.Windows.Forms.Padding(0);
            this.SurveyDC.Name = "SurveyDC";
            this.SurveyDC.RoundingIncrement = distance1;
            this.SurveyDC.Size = new System.Drawing.Size(183, 31);
            this.SurveyDC.TabIndex = 5;
            // 
            // DiagDC
            // 
            this.DiagDC.Location = new System.Drawing.Point(8, 166);
            this.DiagDC.Margin = new System.Windows.Forms.Padding(0);
            this.DiagDC.Name = "DiagDC";
            this.DiagDC.RoundingIncrement = distance1;
            this.DiagDC.Size = new System.Drawing.Size(183, 31);
            this.DiagDC.TabIndex = 7;
            // 
            // MeasureCornerAngleDlg
            // 
            this.AcceptButton = this.OKBtn;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CancelBtn;
            this.ClientSize = new System.Drawing.Size(278, 257);
            this.Controls.Add(this.DiagDC);
            this.Controls.Add(this.SurveyDC);
            this.Controls.Add(this.RefDC);
            this.Controls.Add(this.CancelBtn);
            this.Controls.Add(this.OKBtn);
            this.Controls.Add(this.SurveyChooseCheck);
            this.Controls.Add(this.RefChooseCheck);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.SurveyDCLbl);
            this.Controls.Add(this.RefDCLbl);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MeasureCornerAngleDlg";
            this.Padding = new System.Windows.Forms.Padding(0, 8, 0, 8);
            this.Text = "Measure Corner Angle";
            this.Load += new System.EventHandler(this.MeasureCornerAngleDlg_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label RefDCLbl;
        private System.Windows.Forms.Label SurveyDCLbl;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox RefChooseCheck;
        private System.Windows.Forms.CheckBox SurveyChooseCheck;
        private System.Windows.Forms.Button CancelBtn;
        private System.Windows.Forms.Button OKBtn;
        private DistanceControl RefDC;
        private DistanceControl SurveyDC;
        private DistanceControl DiagDC;
    }
}