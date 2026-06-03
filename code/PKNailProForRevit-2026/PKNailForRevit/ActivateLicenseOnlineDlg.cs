using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PKNailForRevit
{
    internal partial class ActivateLicenseOnlineDlg : Form
    {
        private PKLicense.PKLicense m_license;
        private bool m_isActivationSuccessful;
        private string m_password;

        internal ActivateLicenseOnlineDlg(PKLicense.PKLicense license)
        {
            m_license = license;
            m_isActivationSuccessful = false;
            m_password = @"";
            InitializeComponent();
        }

        internal string Password  { get { return m_password; } }

        private void OKBtn_Click(object sender, EventArgs e)
        {
            string userID = IDTB.Text;
            string userPassword = PasswordTB.Text;
            string instName = InstNameTB.Text;
            string licenseIDToActivate = @"";
            string customerPassword = @"";

            // This probably doesn't show b/c we don't get back to the message pump until later
            // If we really want this, we have to put the task into a worker thread
            ActivationStatusLbl.Text = "Activating...";

            List<PKLicense.PKLicense.LicenseActivationData> availableLicenses;
            bool isOK = m_license.GetAvailableLicenses(userID, userPassword, out availableLicenses);
            if (isOK)
            {
                if (availableLicenses.Count == 0)
                {
                    setActivationDisabledState();
                    MessageBox.Show(  @"You have no license subscriptions available to activate. "
                                    + @"Please contact PointKnown Sales to purchase license subscriptions");
                }
                else if (availableLicenses.Count == 1)
                {
                    licenseIDToActivate = availableLicenses[0].licenseID;
                    customerPassword = availableLicenses[0].customerPassword;
                }
                else
                {
                    SelectLicenseToActivateDlg dlg = new SelectLicenseToActivateDlg(availableLicenses);
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        licenseIDToActivate = availableLicenses[dlg.SelectedLicenseIndex].licenseID;
                        customerPassword = availableLicenses[dlg.SelectedLicenseIndex].customerPassword;
                    }
                    else
                    { setActivationDisabledState(); }
                }
            }
            else
            {
                PasswordTB.Text = @"";
                setState();
                ActivationStatusLbl.Text = m_license.ErrorMessage;
            }

            if (licenseIDToActivate.Length > 0)
            {
                setActivationDisabledState();
                m_isActivationSuccessful = m_license.Activate(licenseIDToActivate, customerPassword, instName);
                if (m_isActivationSuccessful)
                {
                    m_license.LastUserID = userID;
                    m_password = userPassword;
                    ActivationStatusLbl.Text = @"Activation Successful!";
                }
                else
                {
                    ActivationStatusLbl.ForeColor = Color.Red;
                    ActivationStatusLbl.Text = @"Activation Failed:" + m_license.ErrorMessage;
                }
            }
        }

        private void ActivateLicenseOnlineDlg_Load(object sender, EventArgs e)
        { setState(); }

        private void setState()
        {
            ActivationStatusLbl.Text = @"";
            OKBtn.Enabled = (IDTB.Text.Length > 0 && PasswordTB.Text.Length > 0 && m_isActivationSuccessful == false);
        }

        private void setActivationDisabledState()
        {
            OKBtn.Enabled = false;
            CancelBtn.Text = @"Done";
            IDTB.Text = @"";
            IDTB.Enabled = false;
            PasswordTB.Text = @"";
            PasswordTB.Enabled = false;
            InstNameTB.Text = @"";
            InstNameTB.Enabled = false;
        }

        private void ActivateLicenseOnlineDlg_FormClosing(object sender, FormClosingEventArgs e)
        { DialogResult = (m_isActivationSuccessful) ? DialogResult.OK : DialogResult.Cancel; }

        private void IDTB_TextChanged(object sender, EventArgs e)
        { setState(); }

         private void PasswordTB_TextChanged(object sender, EventArgs e)
        { setState(); }

         private void RegistrationLinkLbl_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
         { System.Diagnostics.Process.Start(ProductInfo.ProductLoginUrlRoot); }
    }
}
