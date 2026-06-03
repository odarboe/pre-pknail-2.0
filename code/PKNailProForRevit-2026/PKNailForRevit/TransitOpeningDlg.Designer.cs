namespace PKNailForRevit
{
    partial class TransitOpeningDlg
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
            this.DiagramPnl = new System.Windows.Forms.Panel();
            this.OKBtn = new System.Windows.Forms.Button();
            this.CancelBtn = new System.Windows.Forms.Button();
            this.CornerToOpeningLbl = new System.Windows.Forms.Label();
            this.WallExtentLbl = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.OpeningRefDC = new PKNailForRevit.DistanceControl();
            this.WallRefDC = new PKNailForRevit.DistanceControl();
            this.LevelOffsetDC = new PKNailForRevit.DistanceControl();
            this.SuspendLayout();
            // 
            // DiagramPnl
            // 
            this.DiagramPnl.Location = new System.Drawing.Point(12, 12);
            this.DiagramPnl.Name = "DiagramPnl";
            this.DiagramPnl.Size = new System.Drawing.Size(400, 70);
            this.DiagramPnl.TabIndex = 0;
            this.DiagramPnl.Paint += new System.Windows.Forms.PaintEventHandler(this.DiagramPnl_Paint);
            // 
            // OKBtn
            // 
            this.OKBtn.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.OKBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.OKBtn.Location = new System.Drawing.Point(134, 293);
            this.OKBtn.Name = "OKBtn";
            this.OKBtn.Size = new System.Drawing.Size(75, 28);
            this.OKBtn.TabIndex = 7;
            this.OKBtn.Text = "OK";
            this.OKBtn.UseVisualStyleBackColor = true;
            this.OKBtn.Click += new System.EventHandler(this.OKBtn_Click);
            // 
            // CancelBtn
            // 
            this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CancelBtn.Location = new System.Drawing.Point(215, 293);
            this.CancelBtn.Name = "CancelBtn";
            this.CancelBtn.Size = new System.Drawing.Size(75, 28);
            this.CancelBtn.TabIndex = 8;
            this.CancelBtn.Text = "Cancel";
            this.CancelBtn.UseVisualStyleBackColor = true;
            // 
            // CornerToOpeningLbl
            // 
            this.CornerToOpeningLbl.AutoSize = true;
            this.CornerToOpeningLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CornerToOpeningLbl.Location = new System.Drawing.Point(9, 95);
            this.CornerToOpeningLbl.Name = "CornerToOpeningLbl";
            this.CornerToOpeningLbl.Size = new System.Drawing.Size(152, 16);
            this.CornerToOpeningLbl.TabIndex = 1;
            this.CornerToOpeningLbl.Text = "CornerToOpeningLbl";
            // 
            // WallExtentLbl
            // 
            this.WallExtentLbl.AutoSize = true;
            this.WallExtentLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.WallExtentLbl.Location = new System.Drawing.Point(118, 158);
            this.WallExtentLbl.Name = "WallExtentLbl";
            this.WallExtentLbl.Size = new System.Drawing.Size(108, 16);
            this.WallExtentLbl.TabIndex = 3;
            this.WallExtentLbl.Text = "Length Overall";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(118, 222);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(90, 16);
            this.label1.TabIndex = 5;
            this.label1.Text = "Level Offset";
            // 
            // OpeningRefDC
            // 
            this.OpeningRefDC.Location = new System.Drawing.Point(12, 115);
            this.OpeningRefDC.Margin = new System.Windows.Forms.Padding(0);
            this.OpeningRefDC.Name = "OpeningRefDC";
            this.OpeningRefDC.RoundingIncrement = distance1;
            this.OpeningRefDC.Size = new System.Drawing.Size(183, 31);
            this.OpeningRefDC.TabIndex = 2;
            // 
            // WallRefDC
            // 
            this.WallRefDC.Location = new System.Drawing.Point(121, 178);
            this.WallRefDC.Margin = new System.Windows.Forms.Padding(0);
            this.WallRefDC.Name = "WallRefDC";
            this.WallRefDC.RoundingIncrement = distance1;
            this.WallRefDC.Size = new System.Drawing.Size(183, 31);
            this.WallRefDC.TabIndex = 4;
            // 
            // LevelOffsetDC
            // 
            this.LevelOffsetDC.Location = new System.Drawing.Point(121, 241);
            this.LevelOffsetDC.Margin = new System.Windows.Forms.Padding(0);
            this.LevelOffsetDC.Name = "LevelOffsetDC";
            this.LevelOffsetDC.RoundingIncrement = distance1;
            this.LevelOffsetDC.Size = new System.Drawing.Size(183, 31);
            this.LevelOffsetDC.TabIndex = 6;
            // 
            // TransitOpeningDlg
            // 
            this.AcceptButton = this.OKBtn;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CancelBtn;
            this.ClientSize = new System.Drawing.Size(425, 337);
            this.Controls.Add(this.LevelOffsetDC);
            this.Controls.Add(this.WallRefDC);
            this.Controls.Add(this.OpeningRefDC);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.WallExtentLbl);
            this.Controls.Add(this.CornerToOpeningLbl);
            this.Controls.Add(this.CancelBtn);
            this.Controls.Add(this.OKBtn);
            this.Controls.Add(this.DiagramPnl);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "TransitOpeningDlg";
            this.Text = "Capture Wall Relative To Opening";
            this.Load += new System.EventHandler(this.TransitOpeningDlg_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel DiagramPnl;
        private System.Windows.Forms.Button OKBtn;
        private System.Windows.Forms.Button CancelBtn;
        private System.Windows.Forms.Label CornerToOpeningLbl;
        private System.Windows.Forms.Label WallExtentLbl;
        private System.Windows.Forms.Label label1;
        private DistanceControl OpeningRefDC;
        private DistanceControl WallRefDC;
        private DistanceControl LevelOffsetDC;
    }
}