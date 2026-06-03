namespace PKNailForRevit
{
    partial class TypicalWindowHeightDlg
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
            this.StackedHeightsLB = new System.Windows.Forms.ListBox();
            this.label4 = new System.Windows.Forms.Label();
            this.AddStackedBtn = new System.Windows.Forms.Button();
            this.RemoveStackedBtn = new System.Windows.Forms.Button();
            this.CloseBtn = new System.Windows.Forms.Button();
            this.WindowHeightDiagramPnl = new System.Windows.Forms.Panel();
            this.StackedHeightsChkBox = new System.Windows.Forms.CheckBox();
            this.HeadHeightRB = new System.Windows.Forms.RadioButton();
            this.WindowHeightRB = new System.Windows.Forms.RadioButton();
            this.HeadHeightDC = new PKNailForRevit.DistanceControl();
            this.WindowHeightDC = new PKNailForRevit.DistanceControl();
            this.SillHeightDC = new PKNailForRevit.DistanceControl();
            this.SuspendLayout();
            // 
            // StackedHeightsLB
            // 
            this.StackedHeightsLB.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.StackedHeightsLB.FormattingEnabled = true;
            this.StackedHeightsLB.ItemHeight = 16;
            this.StackedHeightsLB.Location = new System.Drawing.Point(10, 34);
            this.StackedHeightsLB.Name = "StackedHeightsLB";
            this.StackedHeightsLB.Size = new System.Drawing.Size(172, 68);
            this.StackedHeightsLB.TabIndex = 1;
            this.StackedHeightsLB.SelectedIndexChanged += new System.EventHandler(this.StackedHeightsLB_SelectedIndexChanged);
            this.StackedHeightsLB.EnabledChanged += new System.EventHandler(this.StackedHeightsLB_EnabledChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(120, 274);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(79, 16);
            this.label4.TabIndex = 9;
            this.label4.Text = "Sill Height";
            // 
            // AddStackedBtn
            // 
            this.AddStackedBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.AddStackedBtn.Location = new System.Drawing.Point(188, 34);
            this.AddStackedBtn.Name = "AddStackedBtn";
            this.AddStackedBtn.Size = new System.Drawing.Size(75, 28);
            this.AddStackedBtn.TabIndex = 2;
            this.AddStackedBtn.Text = "Add";
            this.AddStackedBtn.UseVisualStyleBackColor = true;
            this.AddStackedBtn.Click += new System.EventHandler(this.AddStackedBtn_Click);
            // 
            // RemoveStackedBtn
            // 
            this.RemoveStackedBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.RemoveStackedBtn.Location = new System.Drawing.Point(188, 68);
            this.RemoveStackedBtn.Name = "RemoveStackedBtn";
            this.RemoveStackedBtn.Size = new System.Drawing.Size(75, 28);
            this.RemoveStackedBtn.TabIndex = 3;
            this.RemoveStackedBtn.Text = "Remove";
            this.RemoveStackedBtn.UseVisualStyleBackColor = true;
            this.RemoveStackedBtn.Click += new System.EventHandler(this.RemoveStackedBtn_Click);
            // 
            // CloseBtn
            // 
            this.CloseBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CloseBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CloseBtn.Location = new System.Drawing.Point(120, 359);
            this.CloseBtn.Margin = new System.Windows.Forms.Padding(4);
            this.CloseBtn.Name = "CloseBtn";
            this.CloseBtn.Size = new System.Drawing.Size(75, 28);
            this.CloseBtn.TabIndex = 11;
            this.CloseBtn.Text = "Done";
            this.CloseBtn.UseVisualStyleBackColor = true;
            // 
            // WindowHeightDiagramPnl
            // 
            this.WindowHeightDiagramPnl.Location = new System.Drawing.Point(11, 128);
            this.WindowHeightDiagramPnl.Name = "WindowHeightDiagramPnl";
            this.WindowHeightDiagramPnl.Size = new System.Drawing.Size(100, 210);
            this.WindowHeightDiagramPnl.TabIndex = 4;
            this.WindowHeightDiagramPnl.Paint += new System.Windows.Forms.PaintEventHandler(this.WindowHeightDiagramPnl_Paint);
            // 
            // StackedHeightsChkBox
            // 
            this.StackedHeightsChkBox.AutoSize = true;
            this.StackedHeightsChkBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.StackedHeightsChkBox.Location = new System.Drawing.Point(11, 11);
            this.StackedHeightsChkBox.Name = "StackedHeightsChkBox";
            this.StackedHeightsChkBox.Size = new System.Drawing.Size(135, 20);
            this.StackedHeightsChkBox.TabIndex = 0;
            this.StackedHeightsChkBox.Text = "Stacked Windows";
            this.StackedHeightsChkBox.UseVisualStyleBackColor = true;
            this.StackedHeightsChkBox.CheckedChanged += new System.EventHandler(this.StackedHeightsChkBox_CheckedChanged);
            // 
            // HeadHeightRB
            // 
            this.HeadHeightRB.AutoSize = true;
            this.HeadHeightRB.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.HeadHeightRB.Location = new System.Drawing.Point(123, 121);
            this.HeadHeightRB.Name = "HeadHeightRB";
            this.HeadHeightRB.Size = new System.Drawing.Size(113, 20);
            this.HeadHeightRB.TabIndex = 5;
            this.HeadHeightRB.TabStop = true;
            this.HeadHeightRB.Text = "Head Height";
            this.HeadHeightRB.UseVisualStyleBackColor = true;
            // 
            // WindowHeightRB
            // 
            this.WindowHeightRB.AutoSize = true;
            this.WindowHeightRB.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.WindowHeightRB.Location = new System.Drawing.Point(123, 188);
            this.WindowHeightRB.Name = "WindowHeightRB";
            this.WindowHeightRB.Size = new System.Drawing.Size(129, 20);
            this.WindowHeightRB.TabIndex = 7;
            this.WindowHeightRB.TabStop = true;
            this.WindowHeightRB.Text = "Window Height";
            this.WindowHeightRB.UseVisualStyleBackColor = true;
            this.WindowHeightRB.CheckedChanged += new System.EventHandler(this.WindowHeightRB_CheckedChanged);
            // 
            // HeadHeightDC
            // 
            this.HeadHeightDC.Location = new System.Drawing.Point(123, 143);
            this.HeadHeightDC.Margin = new System.Windows.Forms.Padding(0);
            this.HeadHeightDC.Name = "HeadHeightDC";
            this.HeadHeightDC.RoundingIncrement = distance1;
            this.HeadHeightDC.Size = new System.Drawing.Size(183, 31);
            this.HeadHeightDC.TabIndex = 6;
            // 
            // WindowHeightDC
            // 
            this.WindowHeightDC.Location = new System.Drawing.Point(123, 210);
            this.WindowHeightDC.Margin = new System.Windows.Forms.Padding(0);
            this.WindowHeightDC.Name = "WindowHeightDC";
            this.WindowHeightDC.RoundingIncrement = distance1;
            this.WindowHeightDC.Size = new System.Drawing.Size(183, 31);
            this.WindowHeightDC.TabIndex = 8;
            // 
            // SillHeightDC
            // 
            this.SillHeightDC.Location = new System.Drawing.Point(123, 294);
            this.SillHeightDC.Margin = new System.Windows.Forms.Padding(0);
            this.SillHeightDC.Name = "SillHeightDC";
            this.SillHeightDC.RoundingIncrement = distance1;
            this.SillHeightDC.Size = new System.Drawing.Size(183, 31);
            this.SillHeightDC.TabIndex = 10;
            // 
            // TypicalWindowHeightDlg
            // 
            this.AcceptButton = this.CloseBtn;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(314, 400);
            this.Controls.Add(this.SillHeightDC);
            this.Controls.Add(this.WindowHeightDC);
            this.Controls.Add(this.HeadHeightDC);
            this.Controls.Add(this.WindowHeightRB);
            this.Controls.Add(this.HeadHeightRB);
            this.Controls.Add(this.StackedHeightsChkBox);
            this.Controls.Add(this.WindowHeightDiagramPnl);
            this.Controls.Add(this.CloseBtn);
            this.Controls.Add(this.RemoveStackedBtn);
            this.Controls.Add(this.AddStackedBtn);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.StackedHeightsLB);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "TypicalWindowHeightDlg";
            this.Text = "Typical Sill/Window Height";
            this.Load += new System.EventHandler(this.TypicalWindowHeightDlg_Load);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.TypicalWindowHeightDlg_Paint);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox StackedHeightsLB;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button AddStackedBtn;
        private System.Windows.Forms.Button RemoveStackedBtn;
        private System.Windows.Forms.Button CloseBtn;
        private System.Windows.Forms.Panel WindowHeightDiagramPnl;
        private System.Windows.Forms.CheckBox StackedHeightsChkBox;
        private System.Windows.Forms.RadioButton HeadHeightRB;
        private System.Windows.Forms.RadioButton WindowHeightRB;
        private DistanceControl HeadHeightDC;
        private DistanceControl WindowHeightDC;
        private DistanceControl SillHeightDC;
    }
}