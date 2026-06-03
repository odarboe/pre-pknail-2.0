namespace PKNailForRevit
{
    partial class SettingsAppCtl
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
            this.label5 = new System.Windows.Forms.Label();
            this.AutoSaveTB = new System.Windows.Forms.TextBox();
            this.COMPortTB = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.AutoDetectCOMPortChkBx = new System.Windows.Forms.CheckBox();
            this.UserNameTB = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.IsShowSettingsOnNewProjectChkBx = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(12, 149);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(156, 13);
            this.label5.TabIndex = 5;
            this.label5.Text = "Auto-save Interval (0 to disable)";
            // 
            // AutoSaveTB
            // 
            this.AutoSaveTB.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.AutoSaveTB.Location = new System.Drawing.Point(13, 165);
            this.AutoSaveTB.Name = "AutoSaveTB";
            this.AutoSaveTB.Size = new System.Drawing.Size(47, 22);
            this.AutoSaveTB.TabIndex = 6;
            this.AutoSaveTB.Validated += new System.EventHandler(this.AutoSaveTB_Validated);
            // 
            // COMPortTB
            // 
            this.COMPortTB.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.COMPortTB.Location = new System.Drawing.Point(13, 87);
            this.COMPortTB.Name = "COMPortTB";
            this.COMPortTB.Size = new System.Drawing.Size(100, 22);
            this.COMPortTB.TabIndex = 3;
            this.COMPortTB.Validated += new System.EventHandler(this.COMPortTB_Validated);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(12, 71);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(114, 13);
            this.label6.TabIndex = 2;
            this.label6.Text = "Rangefinder COM Port";
            // 
            // AutoDetectCOMPortChkBx
            // 
            this.AutoDetectCOMPortChkBx.AutoSize = true;
            this.AutoDetectCOMPortChkBx.Location = new System.Drawing.Point(13, 114);
            this.AutoDetectCOMPortChkBx.Name = "AutoDetectCOMPortChkBx";
            this.AutoDetectCOMPortChkBx.Size = new System.Drawing.Size(171, 17);
            this.AutoDetectCOMPortChkBx.TabIndex = 4;
            this.AutoDetectCOMPortChkBx.Text = "Detect COM Port automatically";
            this.AutoDetectCOMPortChkBx.UseVisualStyleBackColor = true;
            this.AutoDetectCOMPortChkBx.CheckedChanged += new System.EventHandler(this.AutoDetectCOMPortChkBx_CheckedChanged);
            // 
            // UserNameTB
            // 
            this.UserNameTB.Location = new System.Drawing.Point(13, 31);
            this.UserNameTB.Name = "UserNameTB";
            this.UserNameTB.Size = new System.Drawing.Size(170, 20);
            this.UserNameTB.TabIndex = 1;
            this.UserNameTB.Validated += new System.EventHandler(this.UserNameTB_Validated);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 15);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(60, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "User Name";
            // 
            // IsShowSettingsOnNewProjectChkBx
            // 
            this.IsShowSettingsOnNewProjectChkBx.AutoSize = true;
            this.IsShowSettingsOnNewProjectChkBx.Location = new System.Drawing.Point(13, 205);
            this.IsShowSettingsOnNewProjectChkBx.Name = "IsShowSettingsOnNewProjectChkBx";
            this.IsShowSettingsOnNewProjectChkBx.Size = new System.Drawing.Size(285, 17);
            this.IsShowSettingsOnNewProjectChkBx.TabIndex = 7;
            this.IsShowSettingsOnNewProjectChkBx.Text = "Show PKNail Pro Settings when creating a new project";
            this.IsShowSettingsOnNewProjectChkBx.UseVisualStyleBackColor = true;
            this.IsShowSettingsOnNewProjectChkBx.CheckedChanged += new System.EventHandler(this.IsShowSettingsOnNewProjectChkBx_CheckedChanged);
            // 
            // SettingsAppCtl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.IsShowSettingsOnNewProjectChkBx);
            this.Controls.Add(this.UserNameTB);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.AutoDetectCOMPortChkBx);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.AutoSaveTB);
            this.Controls.Add(this.COMPortTB);
            this.Controls.Add(this.label6);
            this.Name = "SettingsAppCtl";
            this.Size = new System.Drawing.Size(300, 360);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox AutoSaveTB;
        private System.Windows.Forms.TextBox COMPortTB;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.CheckBox AutoDetectCOMPortChkBx;
        private System.Windows.Forms.TextBox UserNameTB;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox IsShowSettingsOnNewProjectChkBx;
    }
}
