namespace PKNailForRevit
{
    partial class SettingsRFKeypadMapCtl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.PagesLB = new System.Windows.Forms.ListBox();
            this.NewBtn = new System.Windows.Forms.Button();
            this.DeleteBtn = new System.Windows.Forms.Button();
            this.panel6 = new System.Windows.Forms.Panel();
            this.panel5 = new System.Windows.Forms.Panel();
            this.panel4 = new System.Windows.Forms.Panel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.UpLeftTB = new System.Windows.Forms.ComboBox();
            this.UpTB = new System.Windows.Forms.ComboBox();
            this.UpRightTB = new System.Windows.Forms.ComboBox();
            this.DownRightTB = new System.Windows.Forms.ComboBox();
            this.DownTB = new System.Windows.Forms.ComboBox();
            this.DownLeftTB = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // PagesLB
            // 
            this.PagesLB.FormattingEnabled = true;
            this.PagesLB.Location = new System.Drawing.Point(4, 4);
            this.PagesLB.Name = "PagesLB";
            this.PagesLB.Size = new System.Drawing.Size(120, 108);
            this.PagesLB.TabIndex = 8;
            this.PagesLB.SelectedIndexChanged += new System.EventHandler(this.PagesLB_SelectedIndexChanged);
            // 
            // NewBtn
            // 
            this.NewBtn.Location = new System.Drawing.Point(131, 4);
            this.NewBtn.Name = "NewBtn";
            this.NewBtn.Size = new System.Drawing.Size(75, 23);
            this.NewBtn.TabIndex = 9;
            this.NewBtn.Text = "New";
            this.NewBtn.UseVisualStyleBackColor = true;
            this.NewBtn.Click += new System.EventHandler(this.NewBtn_Click);
            // 
            // DeleteBtn
            // 
            this.DeleteBtn.Location = new System.Drawing.Point(131, 34);
            this.DeleteBtn.Name = "DeleteBtn";
            this.DeleteBtn.Size = new System.Drawing.Size(75, 23);
            this.DeleteBtn.TabIndex = 10;
            this.DeleteBtn.Text = "Delete";
            this.DeleteBtn.UseVisualStyleBackColor = true;
            this.DeleteBtn.Click += new System.EventHandler(this.DeleteBtn_Click);
            // 
            // panel6
            // 
            this.panel6.BackgroundImage = global::PKNailForRevit.Properties.Resources.cursorkeydownright;
            this.panel6.Location = new System.Drawing.Point(239, 296);
            this.panel6.Name = "panel6";
            this.panel6.Size = new System.Drawing.Size(32, 32);
            this.panel6.TabIndex = 5;
            // 
            // panel5
            // 
            this.panel5.BackgroundImage = global::PKNailForRevit.Properties.Resources.cursorkeydown;
            this.panel5.Location = new System.Drawing.Point(134, 296);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(32, 32);
            this.panel5.TabIndex = 3;
            // 
            // panel4
            // 
            this.panel4.BackgroundImage = global::PKNailForRevit.Properties.Resources.cursorkeydownleft;
            this.panel4.Location = new System.Drawing.Point(29, 296);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(32, 32);
            this.panel4.TabIndex = 7;
            // 
            // panel3
            // 
            this.panel3.BackgroundImage = global::PKNailForRevit.Properties.Resources.cursorkeyupleft;
            this.panel3.Location = new System.Drawing.Point(29, 161);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(32, 32);
            this.panel3.TabIndex = 5;
            // 
            // panel2
            // 
            this.panel2.BackgroundImage = global::PKNailForRevit.Properties.Resources.cursorkeyupright;
            this.panel2.Location = new System.Drawing.Point(239, 161);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(32, 32);
            this.panel2.TabIndex = 3;
            // 
            // panel1
            // 
            this.panel1.BackgroundImage = global::PKNailForRevit.Properties.Resources.cursorkeyup;
            this.panel1.Location = new System.Drawing.Point(134, 161);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(32, 32);
            this.panel1.TabIndex = 1;
            // 
            // UpLeftTB
            // 
            this.UpLeftTB.FormattingEnabled = true;
            this.UpLeftTB.Location = new System.Drawing.Point(0, 199);
            this.UpLeftTB.Name = "UpLeftTB";
            this.UpLeftTB.Size = new System.Drawing.Size(92, 21);
            this.UpLeftTB.TabIndex = 11;
            // 
            // UpTB
            // 
            this.UpTB.FormattingEnabled = true;
            this.UpTB.Location = new System.Drawing.Point(104, 199);
            this.UpTB.Name = "UpTB";
            this.UpTB.Size = new System.Drawing.Size(92, 21);
            this.UpTB.TabIndex = 12;
            // 
            // UpRightTB
            // 
            this.UpRightTB.FormattingEnabled = true;
            this.UpRightTB.Location = new System.Drawing.Point(208, 199);
            this.UpRightTB.Name = "UpRightTB";
            this.UpRightTB.Size = new System.Drawing.Size(92, 21);
            this.UpRightTB.TabIndex = 13;
            // 
            // DownRightTB
            // 
            this.DownRightTB.FormattingEnabled = true;
            this.DownRightTB.Location = new System.Drawing.Point(208, 334);
            this.DownRightTB.Name = "DownRightTB";
            this.DownRightTB.Size = new System.Drawing.Size(92, 21);
            this.DownRightTB.TabIndex = 16;
            // 
            // DownTB
            // 
            this.DownTB.FormattingEnabled = true;
            this.DownTB.Location = new System.Drawing.Point(104, 334);
            this.DownTB.Name = "DownTB";
            this.DownTB.Size = new System.Drawing.Size(92, 21);
            this.DownTB.TabIndex = 15;
            // 
            // DownLeftTB
            // 
            this.DownLeftTB.FormattingEnabled = true;
            this.DownLeftTB.Location = new System.Drawing.Point(0, 334);
            this.DownLeftTB.Name = "DownLeftTB";
            this.DownLeftTB.Size = new System.Drawing.Size(92, 21);
            this.DownLeftTB.TabIndex = 14;
            // 
            // SettingsRFKeypadMapCtl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.DownRightTB);
            this.Controls.Add(this.DownTB);
            this.Controls.Add(this.DownLeftTB);
            this.Controls.Add(this.UpRightTB);
            this.Controls.Add(this.UpTB);
            this.Controls.Add(this.UpLeftTB);
            this.Controls.Add(this.DeleteBtn);
            this.Controls.Add(this.NewBtn);
            this.Controls.Add(this.PagesLB);
            this.Controls.Add(this.panel6);
            this.Controls.Add(this.panel5);
            this.Controls.Add(this.panel4);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Name = "SettingsRFKeypadMapCtl";
            this.Size = new System.Drawing.Size(300, 360);
            this.Load += new System.EventHandler(this.SettingsRFKeypadMapCtl_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.Panel panel6;
        private System.Windows.Forms.ListBox PagesLB;
        private System.Windows.Forms.Button NewBtn;
        private System.Windows.Forms.Button DeleteBtn;
        private System.Windows.Forms.ComboBox UpLeftTB;
        private System.Windows.Forms.ComboBox UpTB;
        private System.Windows.Forms.ComboBox UpRightTB;
        private System.Windows.Forms.ComboBox DownRightTB;
        private System.Windows.Forms.ComboBox DownTB;
        private System.Windows.Forms.ComboBox DownLeftTB;
    }
}
