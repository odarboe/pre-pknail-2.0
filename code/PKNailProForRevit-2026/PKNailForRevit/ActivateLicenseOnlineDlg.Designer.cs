namespace PKNailForRevit
{
    partial class ActivateLicenseOnlineDlg
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
            this.PasswordTB = new System.Windows.Forms.TextBox();
            this.InstNameTB = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.CancelBtn = new System.Windows.Forms.Button();
            this.OKBtn = new System.Windows.Forms.Button();
            this.IDTB = new System.Windows.Forms.TextBox();
            this.ActivationStatusLbl = new System.Windows.Forms.Label();
            this.RegistrationLinkLbl = new System.Windows.Forms.LinkLabel();
            this.StatusLbl = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(22, 11);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(391, 36);
            this.label1.TabIndex = 10;
            this.label1.Text = "To activate PKNail Pro for Revit on this computer, please enter your PointKnown u" +
    "ser ID and password.  An Internet connection is required. ";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // PasswordTB
            // 
            this.PasswordTB.Location = new System.Drawing.Point(107, 110);
            this.PasswordTB.Name = "PasswordTB";
            this.PasswordTB.PasswordChar = '*';
            this.PasswordTB.Size = new System.Drawing.Size(134, 20);
            this.PasswordTB.TabIndex = 3;
            this.PasswordTB.TextChanged += new System.EventHandler(this.PasswordTB_TextChanged);
            // 
            // InstNameTB
            // 
            this.InstNameTB.Location = new System.Drawing.Point(107, 136);
            this.InstNameTB.Name = "InstNameTB";
            this.InstNameTB.Size = new System.Drawing.Size(134, 20);
            this.InstNameTB.TabIndex = 5;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(22, 85);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(86, 18);
            this.label2.TabIndex = 0;
            this.label2.Text = "User ID:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(19, 111);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(89, 18);
            this.label3.TabIndex = 2;
            this.label3.Text = "Password:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(11, 137);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(97, 18);
            this.label4.TabIndex = 4;
            this.label4.Text = "Installation Name:";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(248, 140);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(147, 31);
            this.label5.TabIndex = 6;
            this.label5.Text = "Optional (e.g. \"Field laptop\")";
            // 
            // CancelBtn
            // 
            this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelBtn.Location = new System.Drawing.Point(347, 221);
            this.CancelBtn.Name = "CancelBtn";
            this.CancelBtn.Size = new System.Drawing.Size(75, 23);
            this.CancelBtn.TabIndex = 9;
            this.CancelBtn.Text = "Cancel";
            this.CancelBtn.UseVisualStyleBackColor = true;
            // 
            // OKBtn
            // 
            this.OKBtn.Location = new System.Drawing.Point(266, 221);
            this.OKBtn.Name = "OKBtn";
            this.OKBtn.Size = new System.Drawing.Size(75, 23);
            this.OKBtn.TabIndex = 8;
            this.OKBtn.Text = "Activate";
            this.OKBtn.UseVisualStyleBackColor = true;
            this.OKBtn.Click += new System.EventHandler(this.OKBtn_Click);
            // 
            // IDTB
            // 
            this.IDTB.Location = new System.Drawing.Point(107, 84);
            this.IDTB.Name = "IDTB";
            this.IDTB.Size = new System.Drawing.Size(134, 20);
            this.IDTB.TabIndex = 1;
            this.IDTB.TextChanged += new System.EventHandler(this.IDTB_TextChanged);
            // 
            // ActivationStatusLbl
            // 
            this.ActivationStatusLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ActivationStatusLbl.ForeColor = System.Drawing.Color.Blue;
            this.ActivationStatusLbl.Location = new System.Drawing.Point(107, 164);
            this.ActivationStatusLbl.Name = "ActivationStatusLbl";
            this.ActivationStatusLbl.Size = new System.Drawing.Size(315, 42);
            this.ActivationStatusLbl.TabIndex = 7;
            // 
            // RegistrationLinkLbl
            // 
            this.RegistrationLinkLbl.AutoSize = true;
            this.RegistrationLinkLbl.Location = new System.Drawing.Point(115, 52);
            this.RegistrationLinkLbl.Name = "RegistrationLinkLbl";
            this.RegistrationLinkLbl.Size = new System.Drawing.Size(204, 13);
            this.RegistrationLinkLbl.TabIndex = 11;
            this.RegistrationLinkLbl.TabStop = true;
            this.RegistrationLinkLbl.Text = "Register or retrieve lost User ID/password";
            this.RegistrationLinkLbl.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.RegistrationLinkLbl_LinkClicked);
            // 
            // StatusLbl
            // 
            this.StatusLbl.AutoSize = true;
            this.StatusLbl.Location = new System.Drawing.Point(68, 164);
            this.StatusLbl.Name = "StatusLbl";
            this.StatusLbl.Size = new System.Drawing.Size(40, 13);
            this.StatusLbl.TabIndex = 13;
            this.StatusLbl.Text = "Status:";
            // 
            // ActivateLicenseOnlineDlg
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CancelBtn;
            this.ClientSize = new System.Drawing.Size(434, 256);
            this.Controls.Add(this.StatusLbl);
            this.Controls.Add(this.RegistrationLinkLbl);
            this.Controls.Add(this.ActivationStatusLbl);
            this.Controls.Add(this.OKBtn);
            this.Controls.Add(this.CancelBtn);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.InstNameTB);
            this.Controls.Add(this.PasswordTB);
            this.Controls.Add(this.IDTB);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ActivateLicenseOnlineDlg";
            this.Text = "Activate PKNail Pro for Revit";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ActivateLicenseOnlineDlg_FormClosing);
            this.Load += new System.EventHandler(this.ActivateLicenseOnlineDlg_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox PasswordTB;
        private System.Windows.Forms.TextBox InstNameTB;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button CancelBtn;
        private System.Windows.Forms.Button OKBtn;
        private System.Windows.Forms.TextBox IDTB;
        private System.Windows.Forms.Label ActivationStatusLbl;
        private System.Windows.Forms.LinkLabel RegistrationLinkLbl;
        private System.Windows.Forms.Label StatusLbl;

    }
}