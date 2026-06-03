namespace DistoCommTester
{
    partial class DistoTesterForm
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
            this.ConnectBtn = new System.Windows.Forms.Button();
            this.DisconnectBtn = new System.Windows.Forms.Button();
            this.ComPortTB = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.OutputTB = new System.Windows.Forms.TextBox();
            this.ProbeAllChkBx = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // ConnectBtn
            // 
            this.ConnectBtn.Location = new System.Drawing.Point(219, 19);
            this.ConnectBtn.Name = "ConnectBtn";
            this.ConnectBtn.Size = new System.Drawing.Size(75, 35);
            this.ConnectBtn.TabIndex = 0;
            this.ConnectBtn.Text = "Connect";
            this.ConnectBtn.UseVisualStyleBackColor = true;
            this.ConnectBtn.Click += new System.EventHandler(this.ConnectBtn_Click);
            // 
            // DisconnectBtn
            // 
            this.DisconnectBtn.Location = new System.Drawing.Point(300, 19);
            this.DisconnectBtn.Name = "DisconnectBtn";
            this.DisconnectBtn.Size = new System.Drawing.Size(75, 35);
            this.DisconnectBtn.TabIndex = 1;
            this.DisconnectBtn.Text = "Disconnect";
            this.DisconnectBtn.UseVisualStyleBackColor = true;
            this.DisconnectBtn.Click += new System.EventHandler(this.DisconnectBtn_Click);
            // 
            // ComPortTB
            // 
            this.ComPortTB.Location = new System.Drawing.Point(12, 38);
            this.ComPortTB.Name = "ComPortTB";
            this.ComPortTB.Size = new System.Drawing.Size(100, 20);
            this.ComPortTB.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 19);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(105, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Last Open COM Port";
            // 
            // OutputTB
            // 
            this.OutputTB.Location = new System.Drawing.Point(12, 83);
            this.OutputTB.Multiline = true;
            this.OutputTB.Name = "OutputTB";
            this.OutputTB.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.OutputTB.Size = new System.Drawing.Size(550, 570);
            this.OutputTB.TabIndex = 4;
            // 
            // ProbeAllChkBx
            // 
            this.ProbeAllChkBx.AutoSize = true;
            this.ProbeAllChkBx.Location = new System.Drawing.Point(119, 40);
            this.ProbeAllChkBx.Name = "ProbeAllChkBx";
            this.ProbeAllChkBx.Size = new System.Drawing.Size(67, 17);
            this.ProbeAllChkBx.TabIndex = 5;
            this.ProbeAllChkBx.Text = "Probe all";
            this.ProbeAllChkBx.UseVisualStyleBackColor = true;
            // 
            // DistoTesterForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(574, 665);
            this.Controls.Add(this.ProbeAllChkBx);
            this.Controls.Add(this.OutputTB);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.ComPortTB);
            this.Controls.Add(this.DisconnectBtn);
            this.Controls.Add(this.ConnectBtn);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DistoTesterForm";
            this.Text = "Disto Tester";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.DistoTesterForm_FormClosing);
            this.Load += new System.EventHandler(this.DistoTesterForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button ConnectBtn;
        private System.Windows.Forms.Button DisconnectBtn;
        private System.Windows.Forms.TextBox ComPortTB;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox OutputTB;
        private System.Windows.Forms.CheckBox ProbeAllChkBx;
    }
}

