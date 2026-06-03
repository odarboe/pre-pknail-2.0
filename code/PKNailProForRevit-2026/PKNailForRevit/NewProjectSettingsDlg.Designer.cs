namespace PKNailForRevit
{
    partial class NewProjectSettingsDlg
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
            this.label1 = new System.Windows.Forms.Label();
            this.ProjectNameTB = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.UserNameTB = new System.Windows.Forms.TextBox();
            this.DontShowChkBx = new System.Windows.Forms.CheckBox();
            this.DoneBtn = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.LocationRoundingSDC = new PKNailForRevit.ShortDistanceContol();
            this.WallWidthSDC = new PKNailForRevit.ShortDistanceContol();
            this.FeatureRoundingSDC = new PKNailForRevit.ShortDistanceContol();
            this.GeometryAdjustmentSDC = new PKNailForRevit.ShortDistanceContol();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 14);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(71, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Project Name";
            // 
            // ProjectNameTB
            // 
            this.ProjectNameTB.Location = new System.Drawing.Point(13, 30);
            this.ProjectNameTB.Name = "ProjectNameTB";
            this.ProjectNameTB.Size = new System.Drawing.Size(170, 20);
            this.ProjectNameTB.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 61);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(60, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "User Name";
            // 
            // UserNameTB
            // 
            this.UserNameTB.Location = new System.Drawing.Point(13, 77);
            this.UserNameTB.Name = "UserNameTB";
            this.UserNameTB.Size = new System.Drawing.Size(170, 20);
            this.UserNameTB.TabIndex = 3;
            // 
            // DontShowChkBx
            // 
            this.DontShowChkBx.AutoSize = true;
            this.DontShowChkBx.Location = new System.Drawing.Point(13, 342);
            this.DontShowChkBx.Name = "DontShowChkBx";
            this.DontShowChkBx.Size = new System.Drawing.Size(205, 17);
            this.DontShowChkBx.TabIndex = 12;
            this.DontShowChkBx.Text = "Don\'t show this again for new projects";
            this.DontShowChkBx.UseVisualStyleBackColor = true;
            // 
            // DoneBtn
            // 
            this.DoneBtn.Location = new System.Drawing.Point(190, 380);
            this.DoneBtn.Name = "DoneBtn";
            this.DoneBtn.Size = new System.Drawing.Size(75, 23);
            this.DoneBtn.TabIndex = 13;
            this.DoneBtn.Text = "Done";
            this.DoneBtn.UseVisualStyleBackColor = true;
            this.DoneBtn.Click += new System.EventHandler(this.DoneBtn_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 122);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(193, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Wall Length/Object Location Rounding";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 174);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(108, 13);
            this.label4.TabIndex = 6;
            this.label4.Text = "Wall Width Rounding";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 226);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(139, 13);
            this.label5.TabIndex = 8;
            this.label5.Text = "Object Dimension Rounding";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(12, 278);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(131, 13);
            this.label6.TabIndex = 10;
            this.label6.Text = "Geometry Adjustment Limit";
            // 
            // LocationRoundingSDC
            // 
            this.LocationRoundingSDC.Location = new System.Drawing.Point(13, 138);
            this.LocationRoundingSDC.Name = "LocationRoundingSDC";
            this.LocationRoundingSDC.Size = new System.Drawing.Size(80, 22);
            this.LocationRoundingSDC.TabIndex = 5;
            this.LocationRoundingSDC.Validated += new System.EventHandler(this.LocationRoundingSDC_Validated);
            // 
            // WallWidthSDC
            // 
            this.WallWidthSDC.Location = new System.Drawing.Point(13, 190);
            this.WallWidthSDC.Name = "WallWidthSDC";
            this.WallWidthSDC.Size = new System.Drawing.Size(80, 22);
            this.WallWidthSDC.TabIndex = 7;
            this.WallWidthSDC.Validated += new System.EventHandler(this.WallWidthSDC_Validated);
            // 
            // FeatureRoundingSDC
            // 
            this.FeatureRoundingSDC.Location = new System.Drawing.Point(13, 242);
            this.FeatureRoundingSDC.Name = "FeatureRoundingSDC";
            this.FeatureRoundingSDC.Size = new System.Drawing.Size(80, 22);
            this.FeatureRoundingSDC.TabIndex = 9;
            this.FeatureRoundingSDC.Validated += new System.EventHandler(this.FeatureRoundingSDC_Validated);
            // 
            // GeometryAdjustmentSDC
            // 
            this.GeometryAdjustmentSDC.Location = new System.Drawing.Point(13, 294);
            this.GeometryAdjustmentSDC.Name = "GeometryAdjustmentSDC";
            this.GeometryAdjustmentSDC.Size = new System.Drawing.Size(80, 22);
            this.GeometryAdjustmentSDC.TabIndex = 11;
            this.GeometryAdjustmentSDC.Validated += new System.EventHandler(this.GeometryAdjustmentSDC_Validated);
            // 
            // NewProjectSettingsDlg
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(277, 415);
            this.Controls.Add(this.GeometryAdjustmentSDC);
            this.Controls.Add(this.FeatureRoundingSDC);
            this.Controls.Add(this.WallWidthSDC);
            this.Controls.Add(this.LocationRoundingSDC);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.DoneBtn);
            this.Controls.Add(this.DontShowChkBx);
            this.Controls.Add(this.UserNameTB);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.ProjectNameTB);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "NewProjectSettingsDlg";
            this.Text = "PKNail Pro New Project Settings";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox ProjectNameTB;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox UserNameTB;
        private System.Windows.Forms.CheckBox DontShowChkBx;
        private System.Windows.Forms.Button DoneBtn;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private ShortDistanceContol LocationRoundingSDC;
        private ShortDistanceContol WallWidthSDC;
        private ShortDistanceContol FeatureRoundingSDC;
        private ShortDistanceContol GeometryAdjustmentSDC;
    }
}