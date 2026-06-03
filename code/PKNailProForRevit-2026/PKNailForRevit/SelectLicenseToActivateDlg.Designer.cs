namespace PKNailForRevit
{
    partial class SelectLicenseToActivateDlg
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle9 = new System.Windows.Forms.DataGridViewCellStyle();
            this.label1 = new System.Windows.Forms.Label();
            this.SelectLicenseDGV = new System.Windows.Forms.DataGridView();
            this.CancelBtn = new System.Windows.Forms.Button();
            this.OKBtn = new System.Windows.Forms.Button();
            this.CheckBoxCol = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.LicenseIDCol = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ExpDateCol = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.LastInstallationNameCol = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.SelectLicenseDGV)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(48, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(352, 26);
            this.label1.TabIndex = 1;
            this.label1.Text = "Select the license you want to activate on this computer:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // SelectLicenseDGV
            // 
            this.SelectLicenseDGV.AllowUserToAddRows = false;
            this.SelectLicenseDGV.AllowUserToDeleteRows = false;
            this.SelectLicenseDGV.AllowUserToResizeColumns = false;
            this.SelectLicenseDGV.AllowUserToResizeRows = false;
            this.SelectLicenseDGV.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.ColumnHeader;
            dataGridViewCellStyle7.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle7.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle7.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle7.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle7.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle7.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle7.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.SelectLicenseDGV.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle7;
            this.SelectLicenseDGV.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.SelectLicenseDGV.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.CheckBoxCol,
            this.LicenseIDCol,
            this.ExpDateCol,
            this.LastInstallationNameCol});
            dataGridViewCellStyle8.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle8.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle8.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle8.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle8.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle8.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle8.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.SelectLicenseDGV.DefaultCellStyle = dataGridViewCellStyle8;
            this.SelectLicenseDGV.Location = new System.Drawing.Point(12, 46);
            this.SelectLicenseDGV.MultiSelect = false;
            this.SelectLicenseDGV.Name = "SelectLicenseDGV";
            dataGridViewCellStyle9.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle9.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle9.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle9.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle9.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle9.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle9.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.SelectLicenseDGV.RowHeadersDefaultCellStyle = dataGridViewCellStyle9;
            this.SelectLicenseDGV.RowHeadersVisible = false;
            this.SelectLicenseDGV.RowTemplate.ReadOnly = true;
            this.SelectLicenseDGV.RowTemplate.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.SelectLicenseDGV.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.SelectLicenseDGV.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.SelectLicenseDGV.Size = new System.Drawing.Size(425, 150);
            this.SelectLicenseDGV.TabIndex = 2;
            this.SelectLicenseDGV.SelectionChanged += new System.EventHandler(this.SelectLicenseDGV_SelectionChanged);
            // 
            // CancelBtn
            // 
            this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelBtn.Location = new System.Drawing.Point(362, 212);
            this.CancelBtn.Name = "CancelBtn";
            this.CancelBtn.Size = new System.Drawing.Size(75, 23);
            this.CancelBtn.TabIndex = 3;
            this.CancelBtn.Text = "Cancel";
            this.CancelBtn.UseVisualStyleBackColor = true;
            // 
            // OKBtn
            // 
            this.OKBtn.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.OKBtn.Location = new System.Drawing.Point(281, 212);
            this.OKBtn.Name = "OKBtn";
            this.OKBtn.Size = new System.Drawing.Size(75, 23);
            this.OKBtn.TabIndex = 4;
            this.OKBtn.Text = "OK";
            this.OKBtn.UseVisualStyleBackColor = true;
            // 
            // CheckBoxCol
            // 
            this.CheckBoxCol.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.CheckBoxCol.FillWeight = 14F;
            this.CheckBoxCol.HeaderText = "Select";
            this.CheckBoxCol.MinimumWidth = 10;
            this.CheckBoxCol.Name = "CheckBoxCol";
            this.CheckBoxCol.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            // 
            // LicenseIDCol
            // 
            this.LicenseIDCol.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.LicenseIDCol.FillWeight = 28F;
            this.LicenseIDCol.HeaderText = "License ID";
            this.LicenseIDCol.Name = "LicenseIDCol";
            this.LicenseIDCol.ReadOnly = true;
            this.LicenseIDCol.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            // 
            // ExpDateCol
            // 
            this.ExpDateCol.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ExpDateCol.FillWeight = 34F;
            this.ExpDateCol.HeaderText = "Expiration Date";
            this.ExpDateCol.Name = "ExpDateCol";
            this.ExpDateCol.ReadOnly = true;
            this.ExpDateCol.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            // 
            // LastInstallationNameCol
            // 
            this.LastInstallationNameCol.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.LastInstallationNameCol.FillWeight = 60F;
            this.LastInstallationNameCol.HeaderText = "Last Installation Name";
            this.LastInstallationNameCol.Name = "LastInstallationNameCol";
            this.LastInstallationNameCol.ReadOnly = true;
            this.LastInstallationNameCol.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            // 
            // SelectLicenseToActivateDlg
            // 
            this.AcceptButton = this.OKBtn;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CancelBtn;
            this.ClientSize = new System.Drawing.Size(449, 247);
            this.Controls.Add(this.OKBtn);
            this.Controls.Add(this.CancelBtn);
            this.Controls.Add(this.SelectLicenseDGV);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SelectLicenseToActivateDlg";
            this.Text = "Activate PKNail for Revit";
            this.Load += new System.EventHandler(this.SelectLicenseToActivateDlg_Load);
            ((System.ComponentModel.ISupportInitialize)(this.SelectLicenseDGV)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DataGridView SelectLicenseDGV;
        private System.Windows.Forms.Button CancelBtn;
        private System.Windows.Forms.Button OKBtn;
        private System.Windows.Forms.DataGridViewCheckBoxColumn CheckBoxCol;
        private System.Windows.Forms.DataGridViewTextBoxColumn LicenseIDCol;
        private System.Windows.Forms.DataGridViewTextBoxColumn ExpDateCol;
        private System.Windows.Forms.DataGridViewTextBoxColumn LastInstallationNameCol;

    }
}