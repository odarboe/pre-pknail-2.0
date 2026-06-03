namespace PKNailForRevit
{
    partial class SettingsDlg
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
            System.Windows.Forms.TreeNode treeNode1 = new System.Windows.Forms.TreeNode("Measurement Rounding");
            System.Windows.Forms.TreeNode treeNode2 = new System.Windows.Forms.TreeNode("Rangefinder Keypad Maps");
            System.Windows.Forms.TreeNode treeNode3 = new System.Windows.Forms.TreeNode("General");
            System.Windows.Forms.TreeNode treeNode4 = new System.Windows.Forms.TreeNode("Project Settings", new System.Windows.Forms.TreeNode[] {
            treeNode1,
            treeNode2,
            treeNode3});
            System.Windows.Forms.TreeNode treeNode5 = new System.Windows.Forms.TreeNode("Global Settings");
            System.Windows.Forms.TreeNode treeNode6 = new System.Windows.Forms.TreeNode("Licensing");
            System.Windows.Forms.TreeNode treeNode7 = new System.Windows.Forms.TreeNode("Product Updates");
            System.Windows.Forms.TreeNode treeNode8 = new System.Windows.Forms.TreeNode("About");
            this.DoneBtn = new System.Windows.Forms.Button();
            this.SettingsPageTV = new System.Windows.Forms.TreeView();
            this.SuspendLayout();
            // 
            // DoneBtn
            // 
            this.DoneBtn.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.DoneBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.DoneBtn.Location = new System.Drawing.Point(409, 386);
            this.DoneBtn.Name = "DoneBtn";
            this.DoneBtn.Size = new System.Drawing.Size(75, 28);
            this.DoneBtn.TabIndex = 1;
            this.DoneBtn.Text = "Done";
            this.DoneBtn.UseVisualStyleBackColor = true;
            // 
            // SettingsPageTV
            // 
            this.SettingsPageTV.HotTracking = true;
            this.SettingsPageTV.Indent = 10;
            this.SettingsPageTV.Location = new System.Drawing.Point(12, 12);
            this.SettingsPageTV.Name = "SettingsPageTV";
            treeNode1.Name = "RoundingNode";
            treeNode1.Text = "Measurement Rounding";
            treeNode2.Name = "RFKeypadMapsNode";
            treeNode2.Text = "Rangefinder Keypad Maps";
            treeNode3.Name = "ProjectGeneralNode";
            treeNode3.Text = "General";
            treeNode4.Name = "ProjectSettingsNode";
            treeNode4.Text = "Project Settings";
            treeNode5.Name = "ApplicationSettingsNode";
            treeNode5.Text = "Global Settings";
            treeNode6.Name = "LicensingSettingsNode";
            treeNode6.Text = "Licensing";
            treeNode7.Name = "UpdatesSettingsNode";
            treeNode7.Text = "Product Updates";
            treeNode8.Name = "AboutSettingsNode";
            treeNode8.Text = "About";
            this.SettingsPageTV.Nodes.AddRange(new System.Windows.Forms.TreeNode[] {
            treeNode4,
            treeNode5,
            treeNode6,
            treeNode7,
            treeNode8});
            this.SettingsPageTV.ShowLines = false;
            this.SettingsPageTV.Size = new System.Drawing.Size(160, 360);
            this.SettingsPageTV.TabIndex = 0;
            this.SettingsPageTV.BeforeSelect += new System.Windows.Forms.TreeViewCancelEventHandler(this.SettingsPageTV_BeforeSelect);
            this.SettingsPageTV.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.SettingsPageTV_AfterSelect);
            this.SettingsPageTV.KeyUp += new System.Windows.Forms.KeyEventHandler(this.SettingsPageTV_KeyUp);
            // 
            // SettingsDlg
            // 
            this.AcceptButton = this.DoneBtn;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(496, 426);
            this.Controls.Add(this.SettingsPageTV);
            this.Controls.Add(this.DoneBtn);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsDlg";
            this.Text = "PKNail Pro Settings";
            this.Load += new System.EventHandler(this.SettingsDlg_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button DoneBtn;
        private System.Windows.Forms.TreeView SettingsPageTV;
    }
}