namespace PKNailForRevit
{
    partial class ConvertUnknownWidthWallsDlg
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
            this.CancelBtn = new System.Windows.Forms.Button();
            this.OKBtn = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.SelectedRB = new System.Windows.Forms.RadioButton();
            this.AllCurLvlRB = new System.Windows.Forms.RadioButton();
            this.AllProjectRB = new System.Windows.Forms.RadioButton();
            this.SuspendLayout();
            // 
            // CancelBtn
            // 
            this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CancelBtn.Location = new System.Drawing.Point(145, 170);
            this.CancelBtn.Name = "CancelBtn";
            this.CancelBtn.Size = new System.Drawing.Size(75, 28);
            this.CancelBtn.TabIndex = 5;
            this.CancelBtn.Text = "Cancel";
            this.CancelBtn.UseVisualStyleBackColor = true;
            // 
            // OKBtn
            // 
            this.OKBtn.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.OKBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.OKBtn.Location = new System.Drawing.Point(64, 170);
            this.OKBtn.Name = "OKBtn";
            this.OKBtn.Size = new System.Drawing.Size(75, 28);
            this.OKBtn.TabIndex = 4;
            this.OKBtn.Text = "OK";
            this.OKBtn.UseVisualStyleBackColor = true;
            this.OKBtn.Click += new System.EventHandler(this.OKBtn_Click);
            // 
            // textBox1
            // 
            this.textBox1.BackColor = System.Drawing.SystemColors.Control;
            this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox1.Location = new System.Drawing.Point(19, 13);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(246, 49);
            this.textBox1.TabIndex = 0;
            this.textBox1.TabStop = false;
            this.textBox1.Text = "Which unknown-width wall pairs would you like to convert to regular walls?";
            // 
            // SelectedRB
            // 
            this.SelectedRB.AutoSize = true;
            this.SelectedRB.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SelectedRB.Location = new System.Drawing.Point(49, 63);
            this.SelectedRB.Name = "SelectedRB";
            this.SelectedRB.Size = new System.Drawing.Size(88, 20);
            this.SelectedRB.TabIndex = 1;
            this.SelectedRB.TabStop = true;
            this.SelectedRB.Text = "Selected";
            this.SelectedRB.UseVisualStyleBackColor = true;
            // 
            // AllCurLvlRB
            // 
            this.AllCurLvlRB.AutoSize = true;
            this.AllCurLvlRB.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.AllCurLvlRB.Location = new System.Drawing.Point(49, 93);
            this.AllCurLvlRB.Name = "AllCurLvlRB";
            this.AllCurLvlRB.Size = new System.Drawing.Size(162, 20);
            this.AllCurLvlRB.TabIndex = 2;
            this.AllCurLvlRB.TabStop = true;
            this.AllCurLvlRB.Text = "All On Current Level";
            this.AllCurLvlRB.UseVisualStyleBackColor = true;
            // 
            // AllProjectRB
            // 
            this.AllProjectRB.AutoSize = true;
            this.AllProjectRB.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.AllProjectRB.Location = new System.Drawing.Point(49, 123);
            this.AllProjectRB.Name = "AllProjectRB";
            this.AllProjectRB.Size = new System.Drawing.Size(113, 20);
            this.AllProjectRB.TabIndex = 3;
            this.AllProjectRB.TabStop = true;
            this.AllProjectRB.Text = "All In Project";
            this.AllProjectRB.UseVisualStyleBackColor = true;
            // 
            // ConvertUnknownWidthWallsDlg
            // 
            this.AcceptButton = this.OKBtn;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CancelBtn;
            this.ClientSize = new System.Drawing.Size(284, 212);
            this.Controls.Add(this.AllProjectRB);
            this.Controls.Add(this.AllCurLvlRB);
            this.Controls.Add(this.SelectedRB);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.CancelBtn);
            this.Controls.Add(this.OKBtn);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ConvertUnknownWidthWallsDlg";
            this.Text = "Convert Unknown Width Walls";
            this.Load += new System.EventHandler(this.ConvertUnknownWidthWallsDlg_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button CancelBtn;
        private System.Windows.Forms.Button OKBtn;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.RadioButton SelectedRB;
        private System.Windows.Forms.RadioButton AllCurLvlRB;
        private System.Windows.Forms.RadioButton AllProjectRB;
    }
}