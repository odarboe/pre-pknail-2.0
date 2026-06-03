namespace PKNailForRevit.UnitTests
{
    partial class DrivableFormTestForm
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
            PKNailForRevit.Distance distance1 = new PKNailForRevit.Distance();
            this.OKBtn = new System.Windows.Forms.Button();
            this.CancelBtn = new System.Windows.Forms.Button();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.radioButton1 = new System.Windows.Forms.RadioButton();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.DC1 = new PKNailForRevit.DistanceControl();
            this.DC2 = new PKNailForRevit.DistanceControl();
            this.DC3 = new PKNailForRevit.DistanceControl();
            this.DC4 = new PKNailForRevit.DistanceControl();
            this.DC5 = new PKNailForRevit.DistanceControl();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // OKBtn
            // 
            this.OKBtn.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.OKBtn.Location = new System.Drawing.Point(58, 380);
            this.OKBtn.Name = "OKBtn";
            this.OKBtn.Size = new System.Drawing.Size(75, 23);
            this.OKBtn.TabIndex = 8;
            this.OKBtn.Text = "OK";
            this.OKBtn.UseVisualStyleBackColor = true;
            // 
            // CancelBtn
            // 
            this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelBtn.Location = new System.Drawing.Point(139, 380);
            this.CancelBtn.Name = "CancelBtn";
            this.CancelBtn.Size = new System.Drawing.Size(75, 23);
            this.CancelBtn.TabIndex = 9;
            this.CancelBtn.Text = "Cancel";
            this.CancelBtn.UseVisualStyleBackColor = true;
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(97, 117);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(80, 17);
            this.checkBox1.TabIndex = 2;
            this.checkBox1.Text = "checkBox1";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(97, 186);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(100, 20);
            this.textBox1.TabIndex = 4;
            // 
            // radioButton1
            // 
            this.radioButton1.AutoSize = true;
            this.radioButton1.Location = new System.Drawing.Point(6, 19);
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.Size = new System.Drawing.Size(46, 17);
            this.radioButton1.TabIndex = 0;
            this.radioButton1.Text = "RB1";
            this.radioButton1.UseVisualStyleBackColor = true;
            // 
            // radioButton2
            // 
            this.radioButton2.AutoSize = true;
            this.radioButton2.Location = new System.Drawing.Point(70, 19);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(46, 17);
            this.radioButton2.TabIndex = 1;
            this.radioButton2.Text = "RB2";
            this.radioButton2.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.radioButton1);
            this.groupBox1.Controls.Add(this.radioButton2);
            this.groupBox1.Location = new System.Drawing.Point(97, 259);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(134, 48);
            this.groupBox1.TabIndex = 6;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "groupBox1";
            // 
            // DC1
            // 
            this.DC1.Location = new System.Drawing.Point(97, 31);
            this.DC1.Margin = new System.Windows.Forms.Padding(0);
            this.DC1.Name = "DC1";
            this.DC1.RoundingIncrement = distance1;
            this.DC1.Size = new System.Drawing.Size(183, 31);
            this.DC1.TabIndex = 10;
            // 
            // DC2
            // 
            this.DC2.Location = new System.Drawing.Point(97, 66);
            this.DC2.Margin = new System.Windows.Forms.Padding(0);
            this.DC2.Name = "DC2";
            this.DC2.RoundingIncrement = distance1;
            this.DC2.Size = new System.Drawing.Size(183, 31);
            this.DC2.TabIndex = 11;
            // 
            // DC3
            // 
            this.DC3.Location = new System.Drawing.Point(97, 141);
            this.DC3.Margin = new System.Windows.Forms.Padding(0);
            this.DC3.Name = "DC3";
            this.DC3.RoundingIncrement = distance1;
            this.DC3.Size = new System.Drawing.Size(183, 31);
            this.DC3.TabIndex = 12;
            // 
            // DC4
            // 
            this.DC4.Location = new System.Drawing.Point(97, 221);
            this.DC4.Margin = new System.Windows.Forms.Padding(0);
            this.DC4.Name = "DC4";
            this.DC4.RoundingIncrement = distance1;
            this.DC4.Size = new System.Drawing.Size(183, 31);
            this.DC4.TabIndex = 13;
            // 
            // DC5
            // 
            this.DC5.Location = new System.Drawing.Point(97, 314);
            this.DC5.Margin = new System.Windows.Forms.Padding(0);
            this.DC5.Name = "DC5";
            this.DC5.RoundingIncrement = distance1;
            this.DC5.Size = new System.Drawing.Size(183, 31);
            this.DC5.TabIndex = 14;
            // 
            // DrivableFormTestForm
            // 
            this.AcceptButton = this.OKBtn;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CancelBtn;
            this.ClientSize = new System.Drawing.Size(292, 415);
            this.Controls.Add(this.DC5);
            this.Controls.Add(this.DC4);
            this.Controls.Add(this.DC3);
            this.Controls.Add(this.DC2);
            this.Controls.Add(this.DC1);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.CancelBtn);
            this.Controls.Add(this.OKBtn);
            this.Name = "DrivableFormTestForm";
            this.Text = "DrivableFormTestForm";
            this.Load += new System.EventHandler(this.DrivableFormTestForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button OKBtn;
        private System.Windows.Forms.Button CancelBtn;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.RadioButton radioButton1;
        private System.Windows.Forms.RadioButton radioButton2;
        private System.Windows.Forms.GroupBox groupBox1;
        private DistanceControl DC1;
        private DistanceControl DC2;
        private DistanceControl DC3;
        private DistanceControl DC4;
        private DistanceControl DC5;
    }
}