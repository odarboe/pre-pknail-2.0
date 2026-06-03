namespace PKNailForRevit
{
    partial class AddLvlDlg
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
            this.components = new System.ComponentModel.Container();
            this.DoneBtn = new System.Windows.Forms.Button();
            this.NameEP = new System.Windows.Forms.ErrorProvider(this.components);
            this.NameTB = new System.Windows.Forms.TextBox();
            this.PromptTB = new System.Windows.Forms.TextBox();
            this.YesRB = new System.Windows.Forms.RadioButton();
            this.NoRB = new System.Windows.Forms.RadioButton();
            ((System.ComponentModel.ISupportInitialize)(this.NameEP)).BeginInit();
            this.SuspendLayout();
            // 
            // DoneBtn
            // 
            this.DoneBtn.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.DoneBtn.Location = new System.Drawing.Point(131, 202);
            this.DoneBtn.Margin = new System.Windows.Forms.Padding(4);
            this.DoneBtn.Name = "DoneBtn";
            this.DoneBtn.Size = new System.Drawing.Size(83, 28);
            this.DoneBtn.TabIndex = 4;
            this.DoneBtn.Text = "Done";
            this.DoneBtn.UseVisualStyleBackColor = true;
            this.DoneBtn.Click += new System.EventHandler(this.DoneBtn_Click);
            // 
            // NameEP
            // 
            this.NameEP.ContainerControl = this;
            // 
            // NameTB
            // 
            this.NameTB.Location = new System.Drawing.Point(56, 125);
            this.NameTB.Margin = new System.Windows.Forms.Padding(4);
            this.NameTB.Name = "NameTB";
            this.NameTB.Size = new System.Drawing.Size(205, 22);
            this.NameTB.TabIndex = 2;
            this.NameTB.Validating += new System.ComponentModel.CancelEventHandler(this.NameTB_Validating);
            // 
            // PromptTB
            // 
            this.PromptTB.BackColor = System.Drawing.SystemColors.Control;
            this.PromptTB.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.PromptTB.Location = new System.Drawing.Point(12, 12);
            this.PromptTB.Multiline = true;
            this.PromptTB.Name = "PromptTB";
            this.PromptTB.ReadOnly = true;
            this.PromptTB.Size = new System.Drawing.Size(320, 70);
            this.PromptTB.TabIndex = 0;
            this.PromptTB.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // YesRB
            // 
            this.YesRB.AutoSize = true;
            this.YesRB.Location = new System.Drawing.Point(37, 100);
            this.YesRB.Name = "YesRB";
            this.YesRB.Size = new System.Drawing.Size(215, 20);
            this.YesRB.TabIndex = 1;
            this.YesRB.TabStop = true;
            this.YesRB.Text = "Yes, add a new level with name:";
            this.YesRB.UseVisualStyleBackColor = true;
            this.YesRB.CheckedChanged += new System.EventHandler(this.YesRB_CheckedChanged);
            // 
            // NoRB
            // 
            this.NoRB.AutoSize = true;
            this.NoRB.Location = new System.Drawing.Point(37, 166);
            this.NoRB.Name = "NoRB";
            this.NoRB.Size = new System.Drawing.Size(184, 20);
            this.NoRB.TabIndex = 3;
            this.NoRB.TabStop = true;
            this.NoRB.Text = "No, do not add a new level";
            this.NoRB.UseVisualStyleBackColor = true;
            // 
            // AddLvlDlg
            // 
            this.AcceptButton = this.DoneBtn;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(344, 243);
            this.Controls.Add(this.NoRB);
            this.Controls.Add(this.YesRB);
            this.Controls.Add(this.PromptTB);
            this.Controls.Add(this.NameTB);
            this.Controls.Add(this.DoneBtn);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AddLvlDlg";
            this.Text = "Add New Level";
            ((System.ComponentModel.ISupportInitialize)(this.NameEP)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button DoneBtn;
        private System.Windows.Forms.ErrorProvider NameEP;
        private System.Windows.Forms.TextBox NameTB;
        private System.Windows.Forms.TextBox PromptTB;
        private System.Windows.Forms.RadioButton NoRB;
        private System.Windows.Forms.RadioButton YesRB;
    }
}