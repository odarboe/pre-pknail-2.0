using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Reflection;

namespace PKNailForRevit
{
    internal partial class SettingsAboutCtl : UserControl
    {
        internal SettingsAboutCtl()
        {
            InitializeComponent();

            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assemblyPath);
            CopyrightLbl.Text = fvi.LegalCopyright;

            // Center the upper labels after setting their values
            ProductNameLbl.Text = fvi.ProductName;
            ProductNameLbl.Location = new Point((this.Width - ProductNameLbl.Width) / 2, ProductNameLbl.Location.Y);

            VersionLbl.Text = ProductInfo.GetUserVersion(fvi.FileVersion);
            int VersionNameLblWidth = VersionNameLbl.Width;
            int VersionLblWidth = VersionLbl.Width;
            int VersionLblSpacing = VersionLbl.Location.X - VersionNameLblWidth - VersionNameLbl.Location.X;
            int VersionWidth = VersionNameLblWidth + VersionLblSpacing + VersionLblWidth;
            int VersionLocX = (this.Width - VersionWidth) / 2;
            VersionNameLbl.Location = new Point(VersionLocX, VersionNameLbl.Location.Y);
            VersionLbl.Location = new Point(VersionLocX + VersionNameLblWidth + VersionLblSpacing, VersionLbl.Location.Y);
        }

        private void PKWebsiteLinkLbl_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        { Process.Start(PKWebsiteLinkLbl.Text); }

        private void SupportEmailLinkLbl_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        { Process.Start(@"mailto:" + SupportEmailLinkLbl.Text); }

        private void EULALinkLbl_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string dir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            System.Diagnostics.Process.Start(Path.Combine(dir, @"PKNailProForRevitEULA.pdf"));
        }
    }
}
