namespace PKNailForRevit
{
    partial class UsageBalanceTransferDlg
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
            this.AddRB = new System.Windows.Forms.RadioButton();
            this.ReturnRB = new System.Windows.Forms.RadioButton();
            this.label1 = new System.Windows.Forms.Label();
            this.RefillTB = new System.Windows.Forms.TextBox();
            this.UnitsLbl1 = new System.Windows.Forms.Label();
            this.UnitsLbl2 = new System.Windows.Forms.Label();
            this.AccountPasswordLbl = new System.Windows.Forms.Label();
            this.PasswordTB = new System.Windows.Forms.TextBox();
            this.OKBtn = new System.Windows.Forms.Button();
            this.ForgotPasswordLinkLbl = new System.Windows.Forms.LinkLabel();
            this.StatusLbl = new System.Windows.Forms.Label();
            this.StatusTB = new System.Windows.Forms.TextBox();
            this.IDTB = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.CurrentBalanceLbl = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // CancelBtn
            // 
            this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelBtn.Location = new System.Drawing.Point(233, 294);
            this.CancelBtn.Name = "CancelBtn";
            this.CancelBtn.Size = new System.Drawing.Size(75, 23);
            this.CancelBtn.TabIndex = 15;
            this.CancelBtn.Text = "Cancel";
            this.CancelBtn.UseVisualStyleBackColor = true;
            // 
            // AddRB
            // 
            this.AddRB.AutoSize = true;
            this.AddRB.Location = new System.Drawing.Point(12, 55);
            this.AddRB.Name = "AddRB";
            this.AddRB.Size = new System.Drawing.Size(79, 17);
            this.AddRB.TabIndex = 3;
            this.AddRB.TabStop = true;
            this.AddRB.Text = "Add usage:";
            this.AddRB.UseVisualStyleBackColor = true;
            this.AddRB.CheckedChanged += new System.EventHandler(this.AddRB_CheckedChanged);
            // 
            // ReturnRB
            // 
            this.ReturnRB.AutoSize = true;
            this.ReturnRB.Location = new System.Drawing.Point(12, 90);
            this.ReturnRB.Name = "ReturnRB";
            this.ReturnRB.Size = new System.Drawing.Size(273, 17);
            this.ReturnRB.TabIndex = 6;
            this.ReturnRB.TabStop = true;
            this.ReturnRB.Text = "Return this computer\'s usage balance to my account";
            this.ReturnRB.UseVisualStyleBackColor = true;
            this.ReturnRB.CheckedChanged += new System.EventHandler(this.ReturnRB_CheckedChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(120, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Current Usage Balance:";
            // 
            // RefillTB
            // 
            this.RefillTB.Location = new System.Drawing.Point(94, 54);
            this.RefillTB.Name = "RefillTB";
            this.RefillTB.Size = new System.Drawing.Size(36, 20);
            this.RefillTB.TabIndex = 4;
            this.RefillTB.TextChanged += new System.EventHandler(this.RefillTB_TextChanged);
            this.RefillTB.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.RefillTB_KeyPress);
            // 
            // UnitsLbl1
            // 
            this.UnitsLbl1.AutoSize = true;
            this.UnitsLbl1.Location = new System.Drawing.Point(163, 20);
            this.UnitsLbl1.Name = "UnitsLbl1";
            this.UnitsLbl1.Size = new System.Drawing.Size(51, 13);
            this.UnitsLbl1.TabIndex = 2;
            this.UnitsLbl1.Text = "UnitsLbl1";
            // 
            // UnitsLbl2
            // 
            this.UnitsLbl2.AutoSize = true;
            this.UnitsLbl2.Location = new System.Drawing.Point(136, 57);
            this.UnitsLbl2.Name = "UnitsLbl2";
            this.UnitsLbl2.Size = new System.Drawing.Size(51, 13);
            this.UnitsLbl2.TabIndex = 5;
            this.UnitsLbl2.Text = "UnitsLbl2";
            // 
            // AccountPasswordLbl
            // 
            this.AccountPasswordLbl.AutoSize = true;
            this.AccountPasswordLbl.Location = new System.Drawing.Point(11, 170);
            this.AccountPasswordLbl.Name = "AccountPasswordLbl";
            this.AccountPasswordLbl.Size = new System.Drawing.Size(56, 13);
            this.AccountPasswordLbl.TabIndex = 9;
            this.AccountPasswordLbl.Text = "Password:";
            this.AccountPasswordLbl.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // PasswordTB
            // 
            this.PasswordTB.Location = new System.Drawing.Point(67, 167);
            this.PasswordTB.Name = "PasswordTB";
            this.PasswordTB.PasswordChar = '*';
            this.PasswordTB.Size = new System.Drawing.Size(134, 20);
            this.PasswordTB.TabIndex = 10;
            this.PasswordTB.TextChanged += new System.EventHandler(this.PasswordTB_TextChanged);
            // 
            // OKBtn
            // 
            this.OKBtn.Location = new System.Drawing.Point(152, 294);
            this.OKBtn.Name = "OKBtn";
            this.OKBtn.Size = new System.Drawing.Size(75, 23);
            this.OKBtn.TabIndex = 14;
            this.OKBtn.Text = "OK";
            this.OKBtn.UseVisualStyleBackColor = true;
            this.OKBtn.Click += new System.EventHandler(this.OKBtn_Click);
            // 
            // ForgotPasswordLinkLbl
            // 
            this.ForgotPasswordLinkLbl.AutoSize = true;
            this.ForgotPasswordLinkLbl.Location = new System.Drawing.Point(64, 190);
            this.ForgotPasswordLinkLbl.Name = "ForgotPasswordLinkLbl";
            this.ForgotPasswordLinkLbl.Size = new System.Drawing.Size(91, 13);
            this.ForgotPasswordLinkLbl.TabIndex = 11;
            this.ForgotPasswordLinkLbl.TabStop = true;
            this.ForgotPasswordLinkLbl.Text = "Forgot password?";
            this.ForgotPasswordLinkLbl.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.ForgotPasswordLinkLbl_LinkClicked);
            // 
            // StatusLbl
            // 
            this.StatusLbl.AutoSize = true;
            this.StatusLbl.Location = new System.Drawing.Point(27, 217);
            this.StatusLbl.Name = "StatusLbl";
            this.StatusLbl.Size = new System.Drawing.Size(40, 13);
            this.StatusLbl.TabIndex = 12;
            this.StatusLbl.Text = "Status:";
            // 
            // StatusTB
            // 
            this.StatusTB.BackColor = System.Drawing.SystemColors.Control;
            this.StatusTB.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.StatusTB.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.StatusTB.ForeColor = System.Drawing.Color.Blue;
            this.StatusTB.Location = new System.Drawing.Point(67, 217);
            this.StatusTB.Multiline = true;
            this.StatusTB.Name = "StatusTB";
            this.StatusTB.Size = new System.Drawing.Size(241, 62);
            this.StatusTB.TabIndex = 13;
            // 
            // IDTB
            // 
            this.IDTB.Location = new System.Drawing.Point(67, 141);
            this.IDTB.Name = "IDTB";
            this.IDTB.Size = new System.Drawing.Size(134, 20);
            this.IDTB.TabIndex = 8;
            this.IDTB.TextChanged += new System.EventHandler(this.IDTB_TextChanged);
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(5, 141);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(62, 18);
            this.label2.TabIndex = 7;
            this.label2.Text = "User ID:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // CurrentBalanceLbl
            // 
            this.CurrentBalanceLbl.AutoSize = true;
            this.CurrentBalanceLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CurrentBalanceLbl.Location = new System.Drawing.Point(138, 20);
            this.CurrentBalanceLbl.Name = "CurrentBalanceLbl";
            this.CurrentBalanceLbl.Size = new System.Drawing.Size(0, 13);
            this.CurrentBalanceLbl.TabIndex = 1;
            // 
            // UsageBalanceTransferDlg
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CancelBtn;
            this.ClientSize = new System.Drawing.Size(320, 329);
            this.Controls.Add(this.CurrentBalanceLbl);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.IDTB);
            this.Controls.Add(this.StatusTB);
            this.Controls.Add(this.StatusLbl);
            this.Controls.Add(this.ForgotPasswordLinkLbl);
            this.Controls.Add(this.OKBtn);
            this.Controls.Add(this.PasswordTB);
            this.Controls.Add(this.AccountPasswordLbl);
            this.Controls.Add(this.UnitsLbl2);
            this.Controls.Add(this.UnitsLbl1);
            this.Controls.Add(this.RefillTB);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.ReturnRB);
            this.Controls.Add(this.AddRB);
            this.Controls.Add(this.CancelBtn);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "UsageBalanceTransferDlg";
            this.Text = "Add or Return Usage";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.UsageBalanceTransferDlg_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button CancelBtn;
        private System.Windows.Forms.RadioButton AddRB;
        private System.Windows.Forms.RadioButton ReturnRB;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox RefillTB;
        private System.Windows.Forms.Label UnitsLbl1;
        private System.Windows.Forms.Label UnitsLbl2;
        private System.Windows.Forms.Label AccountPasswordLbl;
        private System.Windows.Forms.TextBox PasswordTB;
        private System.Windows.Forms.Button OKBtn;
        private System.Windows.Forms.LinkLabel ForgotPasswordLinkLbl;
        private System.Windows.Forms.Label StatusLbl;
        private System.Windows.Forms.TextBox StatusTB;
        private System.Windows.Forms.TextBox IDTB;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label CurrentBalanceLbl;
    }
}