namespace PKNailForRevit
{
    partial class SettingsUpdatesCtl
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
            this.PeriodicUpdateCheckCB = new System.Windows.Forms.CheckBox();
            this.CheckNowLinkLbl = new System.Windows.Forms.LinkLabel();
            this.SuspendLayout();
            // 
            // PeriodicUpdateCheckCB
            // 
            this.PeriodicUpdateCheckCB.AutoSize = true;
            this.PeriodicUpdateCheckCB.Location = new System.Drawing.Point(15, 32);
            this.PeriodicUpdateCheckCB.Name = "PeriodicUpdateCheckCB";
            this.PeriodicUpdateCheckCB.Size = new System.Drawing.Size(254, 17);
            this.PeriodicUpdateCheckCB.TabIndex = 0;
            this.PeriodicUpdateCheckCB.Text = "Periodically check for product updates at startup";
            this.PeriodicUpdateCheckCB.UseVisualStyleBackColor = true;
            this.PeriodicUpdateCheckCB.CheckedChanged += new System.EventHandler(this.PeriodicUpdateCheckCB_CheckedChanged);
            // 
            // CheckNowLinkLbl
            // 
            this.CheckNowLinkLbl.AutoSize = true;
            this.CheckNowLinkLbl.Location = new System.Drawing.Point(15, 67);
            this.CheckNowLinkLbl.Name = "CheckNowLinkLbl";
            this.CheckNowLinkLbl.Size = new System.Drawing.Size(156, 13);
            this.CheckNowLinkLbl.TabIndex = 1;
            this.CheckNowLinkLbl.TabStop = true;
            this.CheckNowLinkLbl.Text = "Check for product updates now";
            this.CheckNowLinkLbl.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.CheckNowLinkLbl_LinkClicked);
            // 
            // SettingsUpdatesCtl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.CheckNowLinkLbl);
            this.Controls.Add(this.PeriodicUpdateCheckCB);
            this.Name = "SettingsUpdatesCtl";
            this.Size = new System.Drawing.Size(300, 360);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox PeriodicUpdateCheckCB;
        private System.Windows.Forms.LinkLabel CheckNowLinkLbl;
    }
}
