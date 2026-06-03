namespace PKNailForRevit
{
    partial class NumLvlDlg
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
            this.OKBtn = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.YesRB = new System.Windows.Forms.RadioButton();
            this.NoRB = new System.Windows.Forms.RadioButton();
            this.NumLvlsTB = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // OKBtn
            // 
            this.OKBtn.Location = new System.Drawing.Point(99, 161);
            this.OKBtn.Name = "OKBtn";
            this.OKBtn.Size = new System.Drawing.Size(75, 23);
            this.OKBtn.TabIndex = 6;
            this.OKBtn.Text = "OK";
            this.OKBtn.UseVisualStyleBackColor = true;
            this.OKBtn.Click += new System.EventHandler(this.OKBtn_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(36, 33);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(201, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "How many levels/floors will be surveyed?";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(79, 54);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(34, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "levels";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(36, 99);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(195, 13);
            this.label3.TabIndex = 3;
            this.label3.Text = "Does the building contain drop ceilings?";
            // 
            // YesRB
            // 
            this.YesRB.AutoSize = true;
            this.YesRB.Location = new System.Drawing.Point(39, 115);
            this.YesRB.Name = "YesRB";
            this.YesRB.Size = new System.Drawing.Size(43, 17);
            this.YesRB.TabIndex = 4;
            this.YesRB.TabStop = true;
            this.YesRB.Text = "Yes";
            this.YesRB.UseVisualStyleBackColor = true;
            // 
            // NoRB
            // 
            this.NoRB.AutoSize = true;
            this.NoRB.Location = new System.Drawing.Point(93, 115);
            this.NoRB.Name = "NoRB";
            this.NoRB.Size = new System.Drawing.Size(39, 17);
            this.NoRB.TabIndex = 5;
            this.NoRB.TabStop = true;
            this.NoRB.Text = "No";
            this.NoRB.UseVisualStyleBackColor = true;
            // 
            // NumLvlsTB
            // 
            this.NumLvlsTB.Location = new System.Drawing.Point(39, 51);
            this.NumLvlsTB.Name = "NumLvlsTB";
            this.NumLvlsTB.Size = new System.Drawing.Size(34, 20);
            this.NumLvlsTB.TabIndex = 1;
            // 
            // NumLvlDlg
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CausesValidation = false;
            this.ClientSize = new System.Drawing.Size(273, 196);
            this.Controls.Add(this.NumLvlsTB);
            this.Controls.Add(this.NoRB);
            this.Controls.Add(this.YesRB);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.OKBtn);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "NumLvlDlg";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "Levels";
            this.Load += new System.EventHandler(this.NumLevelsDlg_Load);
            this.Shown += new System.EventHandler(this.NumLevelsDlg_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button OKBtn;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.RadioButton YesRB;
        private System.Windows.Forms.RadioButton NoRB;
        private System.Windows.Forms.TextBox NumLvlsTB;
    }
}