namespace PKNailForRevit
{
    partial class CopyObjectsDlg
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
            this.CancelBtn = new System.Windows.Forms.Button();
            this.OKBtn = new System.Windows.Forms.Button();
            this.UpperLvlCB = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.LowerLvlCB = new System.Windows.Forms.ComboBox();
            this.CurLvlLbl = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.CopyWindowsGB = new System.Windows.Forms.GroupBox();
            this.CopyWindowsNoneRB = new System.Windows.Forms.RadioButton();
            this.CopyWindowsSelRB = new System.Windows.Forms.RadioButton();
            this.CopyWindowsAllRB = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.CopyDoorsNoneRB = new System.Windows.Forms.RadioButton();
            this.CopyDoorsSelRB = new System.Windows.Forms.RadioButton();
            this.CopyDoorsAllRB = new System.Windows.Forms.RadioButton();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.CopyObjsNoneRB = new System.Windows.Forms.RadioButton();
            this.CopyObjsSelRB = new System.Windows.Forms.RadioButton();
            this.CopyObjsAllRB = new System.Windows.Forms.RadioButton();
            this.CopyWindowsGB.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // CancelBtn
            // 
            this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CancelBtn.Location = new System.Drawing.Point(120, 367);
            this.CancelBtn.Name = "CancelBtn";
            this.CancelBtn.Size = new System.Drawing.Size(75, 28);
            this.CancelBtn.TabIndex = 12;
            this.CancelBtn.Text = "Cancel";
            this.CancelBtn.UseVisualStyleBackColor = true;
            // 
            // OKBtn
            // 
            this.OKBtn.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.OKBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.OKBtn.Location = new System.Drawing.Point(39, 367);
            this.OKBtn.Name = "OKBtn";
            this.OKBtn.Size = new System.Drawing.Size(75, 28);
            this.OKBtn.TabIndex = 11;
            this.OKBtn.Text = "OK";
            this.OKBtn.UseVisualStyleBackColor = true;
            this.OKBtn.Click += new System.EventHandler(this.OKBtn_Click);
            // 
            // UpperLvlCB
            // 
            this.UpperLvlCB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.UpperLvlCB.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.UpperLvlCB.FormattingEnabled = true;
            this.UpperLvlCB.Location = new System.Drawing.Point(9, 30);
            this.UpperLvlCB.Margin = new System.Windows.Forms.Padding(8, 4, 8, 14);
            this.UpperLvlCB.Name = "UpperLvlCB";
            this.UpperLvlCB.Size = new System.Drawing.Size(215, 24);
            this.UpperLvlCB.TabIndex = 1;
            this.UpperLvlCB.SelectedIndexChanged += new System.EventHandler(this.UpperLvlCB_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(6, 10);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(120, 16);
            this.label1.TabIndex = 0;
            this.label1.Text = "To: Upper Level";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(6, 101);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(118, 16);
            this.label3.TabIndex = 3;
            this.label3.Text = "To: Lower Level";
            // 
            // LowerLvlCB
            // 
            this.LowerLvlCB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.LowerLvlCB.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LowerLvlCB.FormattingEnabled = true;
            this.LowerLvlCB.Location = new System.Drawing.Point(9, 121);
            this.LowerLvlCB.Margin = new System.Windows.Forms.Padding(8, 4, 8, 14);
            this.LowerLvlCB.Name = "LowerLvlCB";
            this.LowerLvlCB.Size = new System.Drawing.Size(215, 24);
            this.LowerLvlCB.TabIndex = 4;
            this.LowerLvlCB.SelectedIndexChanged += new System.EventHandler(this.LowerLvlCB_SelectedIndexChanged);
            // 
            // CurLvlLbl
            // 
            this.CurLvlLbl.AutoSize = true;
            this.CurLvlLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CurLvlLbl.Location = new System.Drawing.Point(6, 70);
            this.CurLvlLbl.Name = "CurLvlLbl";
            this.CurLvlLbl.Size = new System.Drawing.Size(142, 16);
            this.CurLvlLbl.TabIndex = 2;
            this.CurLvlLbl.Text = "From: Current Level";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(6, 162);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(110, 16);
            this.label2.TabIndex = 5;
            this.label2.Text = "Copy Windows";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(6, 228);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(90, 16);
            this.label4.TabIndex = 7;
            this.label4.Text = "Copy Doors";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(6, 294);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(196, 16);
            this.label5.TabIndex = 9;
            this.label5.Text = "Copy Other Hosted Objects";
            // 
            // CopyWindowsGB
            // 
            this.CopyWindowsGB.Controls.Add(this.CopyWindowsNoneRB);
            this.CopyWindowsGB.Controls.Add(this.CopyWindowsSelRB);
            this.CopyWindowsGB.Controls.Add(this.CopyWindowsAllRB);
            this.CopyWindowsGB.Location = new System.Drawing.Point(9, 180);
            this.CopyWindowsGB.Name = "CopyWindowsGB";
            this.CopyWindowsGB.Size = new System.Drawing.Size(215, 36);
            this.CopyWindowsGB.TabIndex = 6;
            this.CopyWindowsGB.TabStop = false;
            // 
            // CopyWindowsNoneRB
            // 
            this.CopyWindowsNoneRB.AutoSize = true;
            this.CopyWindowsNoneRB.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CopyWindowsNoneRB.Location = new System.Drawing.Point(152, 11);
            this.CopyWindowsNoneRB.Name = "CopyWindowsNoneRB";
            this.CopyWindowsNoneRB.Size = new System.Drawing.Size(59, 20);
            this.CopyWindowsNoneRB.TabIndex = 2;
            this.CopyWindowsNoneRB.TabStop = true;
            this.CopyWindowsNoneRB.Text = "None";
            this.CopyWindowsNoneRB.UseVisualStyleBackColor = true;
            // 
            // CopyWindowsSelRB
            // 
            this.CopyWindowsSelRB.AutoSize = true;
            this.CopyWindowsSelRB.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CopyWindowsSelRB.Location = new System.Drawing.Point(61, 11);
            this.CopyWindowsSelRB.Name = "CopyWindowsSelRB";
            this.CopyWindowsSelRB.Size = new System.Drawing.Size(80, 20);
            this.CopyWindowsSelRB.TabIndex = 1;
            this.CopyWindowsSelRB.TabStop = true;
            this.CopyWindowsSelRB.Text = "Selected";
            this.CopyWindowsSelRB.UseVisualStyleBackColor = true;
            // 
            // CopyWindowsAllRB
            // 
            this.CopyWindowsAllRB.AutoSize = true;
            this.CopyWindowsAllRB.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CopyWindowsAllRB.Location = new System.Drawing.Point(7, 11);
            this.CopyWindowsAllRB.Name = "CopyWindowsAllRB";
            this.CopyWindowsAllRB.Size = new System.Drawing.Size(41, 20);
            this.CopyWindowsAllRB.TabIndex = 0;
            this.CopyWindowsAllRB.TabStop = true;
            this.CopyWindowsAllRB.Text = "All";
            this.CopyWindowsAllRB.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.CopyDoorsNoneRB);
            this.groupBox1.Controls.Add(this.CopyDoorsSelRB);
            this.groupBox1.Controls.Add(this.CopyDoorsAllRB);
            this.groupBox1.Location = new System.Drawing.Point(9, 246);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(215, 36);
            this.groupBox1.TabIndex = 8;
            this.groupBox1.TabStop = false;
            // 
            // CopyDoorsNoneRB
            // 
            this.CopyDoorsNoneRB.AutoSize = true;
            this.CopyDoorsNoneRB.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CopyDoorsNoneRB.Location = new System.Drawing.Point(152, 11);
            this.CopyDoorsNoneRB.Name = "CopyDoorsNoneRB";
            this.CopyDoorsNoneRB.Size = new System.Drawing.Size(59, 20);
            this.CopyDoorsNoneRB.TabIndex = 2;
            this.CopyDoorsNoneRB.TabStop = true;
            this.CopyDoorsNoneRB.Text = "None";
            this.CopyDoorsNoneRB.UseVisualStyleBackColor = true;
            // 
            // CopyDoorsSelRB
            // 
            this.CopyDoorsSelRB.AutoSize = true;
            this.CopyDoorsSelRB.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CopyDoorsSelRB.Location = new System.Drawing.Point(61, 11);
            this.CopyDoorsSelRB.Name = "CopyDoorsSelRB";
            this.CopyDoorsSelRB.Size = new System.Drawing.Size(80, 20);
            this.CopyDoorsSelRB.TabIndex = 1;
            this.CopyDoorsSelRB.TabStop = true;
            this.CopyDoorsSelRB.Text = "Selected";
            this.CopyDoorsSelRB.UseVisualStyleBackColor = true;
            // 
            // CopyDoorsAllRB
            // 
            this.CopyDoorsAllRB.AutoSize = true;
            this.CopyDoorsAllRB.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CopyDoorsAllRB.Location = new System.Drawing.Point(7, 11);
            this.CopyDoorsAllRB.Name = "CopyDoorsAllRB";
            this.CopyDoorsAllRB.Size = new System.Drawing.Size(41, 20);
            this.CopyDoorsAllRB.TabIndex = 0;
            this.CopyDoorsAllRB.TabStop = true;
            this.CopyDoorsAllRB.Text = "All";
            this.CopyDoorsAllRB.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.CopyObjsNoneRB);
            this.groupBox2.Controls.Add(this.CopyObjsSelRB);
            this.groupBox2.Controls.Add(this.CopyObjsAllRB);
            this.groupBox2.Location = new System.Drawing.Point(9, 312);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(215, 36);
            this.groupBox2.TabIndex = 10;
            this.groupBox2.TabStop = false;
            // 
            // CopyObjsNoneRB
            // 
            this.CopyObjsNoneRB.AutoSize = true;
            this.CopyObjsNoneRB.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CopyObjsNoneRB.Location = new System.Drawing.Point(152, 11);
            this.CopyObjsNoneRB.Name = "CopyObjsNoneRB";
            this.CopyObjsNoneRB.Size = new System.Drawing.Size(59, 20);
            this.CopyObjsNoneRB.TabIndex = 2;
            this.CopyObjsNoneRB.TabStop = true;
            this.CopyObjsNoneRB.Text = "None";
            this.CopyObjsNoneRB.UseVisualStyleBackColor = true;
            // 
            // CopyObjsSelRB
            // 
            this.CopyObjsSelRB.AutoSize = true;
            this.CopyObjsSelRB.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CopyObjsSelRB.Location = new System.Drawing.Point(61, 11);
            this.CopyObjsSelRB.Name = "CopyObjsSelRB";
            this.CopyObjsSelRB.Size = new System.Drawing.Size(80, 20);
            this.CopyObjsSelRB.TabIndex = 1;
            this.CopyObjsSelRB.TabStop = true;
            this.CopyObjsSelRB.Text = "Selected";
            this.CopyObjsSelRB.UseVisualStyleBackColor = true;
            // 
            // CopyObjsAllRB
            // 
            this.CopyObjsAllRB.AutoSize = true;
            this.CopyObjsAllRB.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CopyObjsAllRB.Location = new System.Drawing.Point(7, 11);
            this.CopyObjsAllRB.Name = "CopyObjsAllRB";
            this.CopyObjsAllRB.Size = new System.Drawing.Size(41, 20);
            this.CopyObjsAllRB.TabIndex = 0;
            this.CopyObjsAllRB.TabStop = true;
            this.CopyObjsAllRB.Text = "All";
            this.CopyObjsAllRB.UseVisualStyleBackColor = true;
            // 
            // CopyObjectsDlg
            // 
            this.AcceptButton = this.OKBtn;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CancelBtn;
            this.ClientSize = new System.Drawing.Size(234, 411);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.CurLvlLbl);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.LowerLvlCB);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.UpperLvlCB);
            this.Controls.Add(this.CancelBtn);
            this.Controls.Add(this.OKBtn);
            this.Controls.Add(this.CopyWindowsGB);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CopyObjectsDlg";
            this.Text = "Copy Level-to-Level";
            this.Load += new System.EventHandler(this.CopyObjectsDlg_Load);
            this.CopyWindowsGB.ResumeLayout(false);
            this.CopyWindowsGB.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button CancelBtn;
        private System.Windows.Forms.Button OKBtn;
        private System.Windows.Forms.ComboBox UpperLvlCB;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox LowerLvlCB;
        private System.Windows.Forms.Label CurLvlLbl;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.GroupBox CopyWindowsGB;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.RadioButton CopyWindowsNoneRB;
        private System.Windows.Forms.RadioButton CopyWindowsSelRB;
        private System.Windows.Forms.RadioButton CopyWindowsAllRB;
        private System.Windows.Forms.RadioButton CopyDoorsNoneRB;
        private System.Windows.Forms.RadioButton CopyDoorsSelRB;
        private System.Windows.Forms.RadioButton CopyDoorsAllRB;
        private System.Windows.Forms.RadioButton CopyObjsNoneRB;
        private System.Windows.Forms.RadioButton CopyObjsSelRB;
        private System.Windows.Forms.RadioButton CopyObjsAllRB;
    }
}