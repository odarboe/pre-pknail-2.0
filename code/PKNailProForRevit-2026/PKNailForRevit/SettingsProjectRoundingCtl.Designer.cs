namespace PKNailForRevit
{
    partial class SettingsProjectRoundingCtl
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
            this.GeometryAdjustmentSDC = new PKNailForRevit.ShortDistanceContol();
            this.FeatureRoundingSDC = new PKNailForRevit.ShortDistanceContol();
            this.WallWidthSDC = new PKNailForRevit.ShortDistanceContol();
            this.LocationRoundingSDC = new PKNailForRevit.ShortDistanceContol();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // GeometryAdjustmentSDC
            // 
            this.GeometryAdjustmentSDC.Location = new System.Drawing.Point(14, 190);
            this.GeometryAdjustmentSDC.Name = "GeometryAdjustmentSDC";
            this.GeometryAdjustmentSDC.Size = new System.Drawing.Size(80, 22);
            this.GeometryAdjustmentSDC.TabIndex = 7;
            this.GeometryAdjustmentSDC.Validated += new System.EventHandler(this.GeometryAdjustmentSDC_Validated);
            // 
            // FeatureRoundingSDC
            // 
            this.FeatureRoundingSDC.Location = new System.Drawing.Point(14, 138);
            this.FeatureRoundingSDC.Name = "FeatureRoundingSDC";
            this.FeatureRoundingSDC.Size = new System.Drawing.Size(80, 22);
            this.FeatureRoundingSDC.TabIndex = 5;
            this.FeatureRoundingSDC.Validated += new System.EventHandler(this.FeatureRoundingSDC_Validated);
            // 
            // WallWidthSDC
            // 
            this.WallWidthSDC.Location = new System.Drawing.Point(14, 86);
            this.WallWidthSDC.Name = "WallWidthSDC";
            this.WallWidthSDC.Size = new System.Drawing.Size(80, 22);
            this.WallWidthSDC.TabIndex = 3;
            this.WallWidthSDC.Validated += new System.EventHandler(this.WallWidthSDC_Validated);
            // 
            // LocationRoundingSDC
            // 
            this.LocationRoundingSDC.Location = new System.Drawing.Point(14, 34);
            this.LocationRoundingSDC.Name = "LocationRoundingSDC";
            this.LocationRoundingSDC.Size = new System.Drawing.Size(80, 22);
            this.LocationRoundingSDC.TabIndex = 1;
            this.LocationRoundingSDC.Validated += new System.EventHandler(this.LocationRoundingSDC_Validated);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(13, 174);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(131, 13);
            this.label6.TabIndex = 6;
            this.label6.Text = "Geometry Adjustment Limit";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(13, 122);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(139, 13);
            this.label5.TabIndex = 4;
            this.label5.Text = "Object Dimension Rounding";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(13, 70);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(108, 13);
            this.label4.TabIndex = 2;
            this.label4.Text = "Wall Width Rounding";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(13, 18);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(193, 13);
            this.label3.TabIndex = 0;
            this.label3.Text = "Wall Length/Object Location Rounding";
            // 
            // SettingsProjectRoundingCtl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.GeometryAdjustmentSDC);
            this.Controls.Add(this.FeatureRoundingSDC);
            this.Controls.Add(this.WallWidthSDC);
            this.Controls.Add(this.LocationRoundingSDC);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Name = "SettingsProjectRoundingCtl";
            this.Size = new System.Drawing.Size(300, 360);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private ShortDistanceContol GeometryAdjustmentSDC;
        private ShortDistanceContol FeatureRoundingSDC;
        private ShortDistanceContol WallWidthSDC;
        private ShortDistanceContol LocationRoundingSDC;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;

    }
}
