namespace PKNailForRevit
{
    partial class SettingsProjectGeneralCtl
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
            this.IsStructuralChkBx = new System.Windows.Forms.CheckBox();
            this.ProjectNameTB = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // IsStructuralChkBx
            // 
            this.IsStructuralChkBx.AutoSize = true;
            this.IsStructuralChkBx.Location = new System.Drawing.Point(13, 75);
            this.IsStructuralChkBx.Name = "IsStructuralChkBx";
            this.IsStructuralChkBx.Size = new System.Drawing.Size(200, 17);
            this.IsStructuralChkBx.TabIndex = 2;
            this.IsStructuralChkBx.Text = "Create structural walls during capture";
            this.IsStructuralChkBx.UseVisualStyleBackColor = true;
            this.IsStructuralChkBx.CheckedChanged += new System.EventHandler(this.IsStructuralChkBx_CheckedChanged);
            // 
            // ProjectNameTB
            // 
            this.ProjectNameTB.Location = new System.Drawing.Point(13, 32);
            this.ProjectNameTB.Name = "ProjectNameTB";
            this.ProjectNameTB.Size = new System.Drawing.Size(170, 20);
            this.ProjectNameTB.TabIndex = 1;
            this.ProjectNameTB.Leave += new System.EventHandler(this.ProjectNameTB_Leave);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(71, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Project Name";
            // 
            // SettingsProjectGeneralCtl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.ProjectNameTB);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.IsStructuralChkBx);
            this.Name = "SettingsProjectGeneralCtl";
            this.Size = new System.Drawing.Size(300, 360);
            this.Load += new System.EventHandler(this.SettingsProjectGeneralCtl_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox IsStructuralChkBx;
        private System.Windows.Forms.TextBox ProjectNameTB;
        private System.Windows.Forms.Label label1;
    }
}
