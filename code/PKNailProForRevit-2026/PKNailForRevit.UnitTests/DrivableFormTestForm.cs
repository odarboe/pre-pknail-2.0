using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PKNailForRevit.UnitTests
{
    public partial class DrivableFormTestForm : DrivableForm
    {
        public DrivableFormTestForm()
        {
            InitializeComponent();
        }

        private void DrivableFormTestForm_Load(object sender, EventArgs e)
        {
            System.Diagnostics.Trace.WriteLine("DrivableFormTestForm_Load: Thread: " + System.Threading.Thread.CurrentThread.GetHashCode().ToString());
            groupBox1.TabStop = false;
            DC1.Select();
        }
    }
}
