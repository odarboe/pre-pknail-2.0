using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace PKNailForRevit
{
    internal partial class SelectLicenseToActivateDlg : Form
    {
        private List<PKLicense.PKLicense.LicenseActivationData> m_licenseActivationData;

        internal int SelectedLicenseIndex
        {
            get
            {
                int numSelRows = SelectLicenseDGV.SelectedRows.Count;
                if (numSelRows > 0)
                { return SelectLicenseDGV.SelectedRows[0].Index; }
                else
                { return -1; }
            }
        }

        internal SelectLicenseToActivateDlg(List<PKLicense.PKLicense.LicenseActivationData> licenseActivationData)
        {
            m_licenseActivationData = licenseActivationData;
            InitializeComponent();
        }

        private void SelectLicenseToActivateDlg_Load(object sender, EventArgs e)
        {
            foreach (PKLicense.PKLicense.LicenseActivationData lad in m_licenseActivationData)
            {
                DateTime expDate = DateTime.Parse(lad.expirationDate);
                string expDateDGVFormat = expDate.ToShortDateString();
                SelectLicenseDGV.Rows.Add(new object[] { false, lad.licenseID, expDateDGVFormat, lad.lastInstallationName });
            }
            SelectLicenseDGV.ClearSelection();
            OKBtn.Enabled = false;
        }

        private void SelectLicenseDGV_SelectionChanged(object sender, EventArgs e)
        {
            for (int rowIdx = 0; rowIdx < SelectLicenseDGV.Rows.Count; rowIdx++)
            { SelectLicenseDGV.Rows[rowIdx].Cells[0].Value = false; }
            if (SelectLicenseDGV.SelectedRows.Count > 0)
            {
                SelectLicenseDGV.SelectedRows[0].Cells[0].Value = true;
                OKBtn.Enabled = true;
            }
            else
            { OKBtn.Enabled = false; }
        }
    }
}
