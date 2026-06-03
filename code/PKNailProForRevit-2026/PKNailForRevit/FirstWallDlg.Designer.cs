namespace PKNailForRevit
{
    partial class FirstWallDlg
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
            this.LabelTB = new System.Windows.Forms.TextBox();
            this.CancelBtn = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.OKBtn = new System.Windows.Forms.Button();
            this.PerspectiveCB = new System.Windows.Forms.ComboBox();
            this.DirectionCB = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // LabelTB
            // 
            this.LabelTB.BackColor = System.Drawing.SystemColors.Control;
            this.LabelTB.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.LabelTB.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LabelTB.Location = new System.Drawing.Point(16, 15);
            this.LabelTB.Margin = new System.Windows.Forms.Padding(8, 8, 8, 14);
            this.LabelTB.Multiline = true;
            this.LabelTB.Name = "LabelTB";
            this.LabelTB.Size = new System.Drawing.Size(271, 76);
            this.LabelTB.TabIndex = 0;
            this.LabelTB.TabStop = false;
            // 
            // CancelBtn
            // 
            this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CancelBtn.Location = new System.Drawing.Point(155, 226);
            this.CancelBtn.Name = "CancelBtn";
            this.CancelBtn.Size = new System.Drawing.Size(75, 28);
            this.CancelBtn.TabIndex = 6;
            this.CancelBtn.Text = "Cancel";
            this.CancelBtn.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(6, 113);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(188, 16);
            this.label2.TabIndex = 1;
            this.label2.Text = "Are you outside or inside?";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(6, 167);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(291, 16);
            this.label3.TabIndex = 3;
            this.label3.Text = "What compass direction are YOU facing?";
            // 
            // OKBtn
            // 
            this.OKBtn.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.OKBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.OKBtn.Location = new System.Drawing.Point(73, 226);
            this.OKBtn.Name = "OKBtn";
            this.OKBtn.Size = new System.Drawing.Size(75, 28);
            this.OKBtn.TabIndex = 5;
            this.OKBtn.Text = "OK";
            this.OKBtn.UseVisualStyleBackColor = true;
            this.OKBtn.Click += new System.EventHandler(this.OKBtn_Click);
            // 
            // PerspectiveCB
            // 
            this.PerspectiveCB.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PerspectiveCB.FormattingEnabled = true;
            this.PerspectiveCB.Location = new System.Drawing.Point(9, 132);
            this.PerspectiveCB.Name = "PerspectiveCB";
            this.PerspectiveCB.Size = new System.Drawing.Size(125, 24);
            this.PerspectiveCB.TabIndex = 2;
            // 
            // DirectionCB
            // 
            this.DirectionCB.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.DirectionCB.FormattingEnabled = true;
            this.DirectionCB.Location = new System.Drawing.Point(9, 186);
            this.DirectionCB.Name = "DirectionCB";
            this.DirectionCB.Size = new System.Drawing.Size(125, 24);
            this.DirectionCB.TabIndex = 4;
            // 
            // FirstWallDlg
            // 
            this.AcceptButton = this.OKBtn;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CancelBtn;
            this.ClientSize = new System.Drawing.Size(303, 268);
            this.Controls.Add(this.DirectionCB);
            this.Controls.Add(this.PerspectiveCB);
            this.Controls.Add(this.OKBtn);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.CancelBtn);
            this.Controls.Add(this.LabelTB);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FirstWallDlg";
            this.Text = "Orient First Wall";
            this.Load += new System.EventHandler(this.FirstWallDlg_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox LabelTB;
        private System.Windows.Forms.Button CancelBtn;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button OKBtn;
        private System.Windows.Forms.ComboBox PerspectiveCB;
        private System.Windows.Forms.ComboBox DirectionCB;

    }
}