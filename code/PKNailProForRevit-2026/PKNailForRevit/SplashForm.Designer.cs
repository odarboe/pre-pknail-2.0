namespace PKNailForRevit
{
    partial class SplashForm
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
            this.ProgressBar = new System.Windows.Forms.ProgressBar();
            this.StatusLbl = new System.Windows.Forms.Label();
            this.VersionLbl = new System.Windows.Forms.Label();
            this.CopyrightLbl = new System.Windows.Forms.Label();
            this.RightsLbl = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // ProgressBar
            // 
            this.ProgressBar.Location = new System.Drawing.Point(391, 302);
            this.ProgressBar.Name = "ProgressBar";
            this.ProgressBar.Size = new System.Drawing.Size(190, 20);
            this.ProgressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.ProgressBar.TabIndex = 2;
            this.ProgressBar.UseWaitCursor = true;
            // 
            // StatusLbl
            // 
            this.StatusLbl.AutoSize = true;
            this.StatusLbl.Font = new System.Drawing.Font("Verdana", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.StatusLbl.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(216)))), ((int)(((byte)(216)))), ((int)(((byte)(216)))));
            this.StatusLbl.Location = new System.Drawing.Point(388, 280);
            this.StatusLbl.Name = "StatusLbl";
            this.StatusLbl.Size = new System.Drawing.Size(62, 16);
            this.StatusLbl.TabIndex = 3;
            this.StatusLbl.Text = "(status)";
            this.StatusLbl.UseWaitCursor = true;
            // 
            // VersionLbl
            // 
            this.VersionLbl.AutoSize = true;
            this.VersionLbl.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.VersionLbl.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(216)))), ((int)(((byte)(216)))), ((int)(((byte)(216)))));
            this.VersionLbl.Location = new System.Drawing.Point(18, 280);
            this.VersionLbl.Name = "VersionLbl";
            this.VersionLbl.Size = new System.Drawing.Size(50, 13);
            this.VersionLbl.TabIndex = 4;
            this.VersionLbl.Text = "Version";
            this.VersionLbl.UseWaitCursor = true;
            // 
            // CopyrightLbl
            // 
            this.CopyrightLbl.AutoSize = true;
            this.CopyrightLbl.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CopyrightLbl.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(216)))), ((int)(((byte)(216)))), ((int)(((byte)(216)))));
            this.CopyrightLbl.Location = new System.Drawing.Point(18, 300);
            this.CopyrightLbl.Name = "CopyrightLbl";
            this.CopyrightLbl.Size = new System.Drawing.Size(63, 13);
            this.CopyrightLbl.TabIndex = 5;
            this.CopyrightLbl.Text = "Copyright";
            this.CopyrightLbl.UseWaitCursor = true;
            // 
            // RightsLbl
            // 
            this.RightsLbl.AutoSize = true;
            this.RightsLbl.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.RightsLbl.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(216)))), ((int)(((byte)(216)))), ((int)(((byte)(216)))));
            this.RightsLbl.Location = new System.Drawing.Point(18, 315);
            this.RightsLbl.Name = "RightsLbl";
            this.RightsLbl.Size = new System.Drawing.Size(118, 13);
            this.RightsLbl.TabIndex = 6;
            this.RightsLbl.Text = "All Rights Reserved";
            this.RightsLbl.UseWaitCursor = true;
            // 
            // SplashForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.BackgroundImage = global::PKNailForRevit.Properties.Resources.splashBackground;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.ClientSize = new System.Drawing.Size(600, 340);
            this.ControlBox = false;
            this.Controls.Add(this.RightsLbl);
            this.Controls.Add(this.CopyrightLbl);
            this.Controls.Add(this.VersionLbl);
            this.Controls.Add(this.StatusLbl);
            this.Controls.Add(this.ProgressBar);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "SplashForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.UseWaitCursor = true;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ProgressBar ProgressBar;
        private System.Windows.Forms.Label StatusLbl;
        private System.Windows.Forms.Label VersionLbl;
        private System.Windows.Forms.Label CopyrightLbl;
        private System.Windows.Forms.Label RightsLbl;

    }
}