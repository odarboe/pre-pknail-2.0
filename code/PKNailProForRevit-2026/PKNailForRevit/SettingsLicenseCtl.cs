using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace PKNailForRevit
{
    internal partial class SettingsLicenseCtl : UserControl
    {
        private const string cm_activateLinkLabel = @"Activate a license on this computer";
        private const string cm_releaseLinkLabel =  @"De-activate the license from this computer";
        private const string cm_deactivatedInstructions = @"This computer does not have a "
            + ProductInfo.ProductBaseName + @" license activated. Close and restart Revit "
            + @"to activate a license.";
        private const string cm_activateBtnText = @"Activate a license";
        private const string cm_releaseInstructions = @"If you need to transfer the "
            + ProductInfo.ProductBaseName + @" license to another computer, you must de-activate it "
            + @"from this computer first.  Click the button below to de-activate the license. "
            + @"An Internet connection is required.";
        private const string cm_releaseBtnText = @"De-activate the license";
        private const string cm_refillInstructions = @"To add more usage to the available usage "
            + @"balance on this computer, or to return the unused balance back to your account, "
            + @"click on the button below. An Internet connection is required.";
        private const string cm_refillBtnText = @"Add or return usage";
        private const string cm_ExpirationWarningChkBxText = @"Give a warning when exipration date is within";
        private const string cm_ExpirationWarningUnitsLblText = @"days";
        private const string cm_BalanceWarningChkBxText = @"Give a warning when usage balance is less than";

        private PKLicense.PKLicense m_license;

        private string m_cleanWarningTBValue;
        private string m_cleanRefillTBValue;

        internal SettingsLicenseCtl(PKLicense.PKLicense license)
        {
            m_license = license;
            InitializeComponent();
            setState();
        }

        private void setState()
        {
            LicenseTypeLbl.Text = m_license.LicenseType;

            if (!m_license.IsActivated)
            {
                StatusLbl.Visible = false;
                StatusLabelLbl.Visible = false;
                LicenseIDLabelLbl.Visible = false;
                LicenseIDLbl.Visible = false;
                LicenseIDLbl.Text = @"";
                CustomerNameLabelLbl.Visible = false;
                CustomerNameLbl.Text = @""; ;
                InstallationNameLabelLbl.Visible = false;
                InstallationNameLbl.Text = @"";
                WarningChkBx.Visible = false;
                WarningTB.Visible = false;
                WarningUnitsLbl.Visible = false;
                DefaultRefillTB.Visible = false;
                DefaultRefillLbl.Visible = false;
                RefillUnitsLbl.Visible = false;
                InstructionsTB.Text = cm_deactivatedInstructions;
                LicensingBtn.Visible = false;
            }
            else if (m_license.IsPerpetual)
            {
                StatusLbl.Visible = false;
                StatusLabelLbl.Visible = false;
                LicenseIDLabelLbl.Visible = true;
                LicenseIDLbl.Visible = true;
                LicenseIDLbl.Text = m_license.LicenseID.ToString();
                CustomerNameLabelLbl.Visible = true;
                CustomerNameLbl.Text = m_license.CompanyName;
                InstallationNameLabelLbl.Visible = true;
                InstallationNameLbl.Text = m_license.InstallationName;
                WarningChkBx.Visible = false;
                WarningTB.Visible = false;
                WarningUnitsLbl.Visible = false;
                DefaultRefillTB.Visible = false;
                DefaultRefillLbl.Visible = false;
                RefillUnitsLbl.Visible = false;
                InstructionsTB.Text = cm_releaseInstructions;
                LicensingBtn.Visible = true;
                LicensingBtn.Text = cm_releaseBtnText;
            }
            else if (m_license.IsMetered)
            {
                StatusLabelLbl.Visible = true;
                StatusLabelLbl.Text = @"Usage Balance:";
                StatusLbl.Visible = true;
                StatusLbl.Text = m_license.ReserveBalance.ToString() + @" " + m_license.UsageUnitsStr;
                if (m_license.IsInsufficientBalance || m_license.IsBalanceWarning)
                { StatusLbl.ForeColor = Color.Red; }
                else
                { StatusLbl.ForeColor = Color.Black; }
                LicenseIDLabelLbl.Visible = true;
                LicenseIDLbl.Visible = true;
                LicenseIDLbl.Text = m_license.LicenseID.ToString();
                CustomerNameLabelLbl.Visible = true;
                CustomerNameLbl.Text = m_license.CompanyName;
                InstallationNameLabelLbl.Visible = true;
                InstallationNameLbl.Text = m_license.InstallationName;
                WarningChkBx.Visible = true;
                WarningChkBx.Text = cm_BalanceWarningChkBxText;
                WarningUnitsLbl.Visible = true;
                WarningTB.Visible = true;
                WarningUnitsLbl.Text = m_license.UsageUnitsStr;
                int warningAmt = m_license.UsageUnitsWarningLevel;
                WarningChkBx.Checked = (warningAmt != -1);
                WarningTB.Enabled = WarningChkBx.Checked;
                WarningUnitsLbl.Enabled = WarningChkBx.Checked;
                m_cleanWarningTBValue = (warningAmt != -1) ? warningAmt.ToString() : @"";
                WarningTB.Text = m_cleanWarningTBValue;
                DefaultRefillLbl.Visible = true;
                DefaultRefillTB.Visible = true;
                m_cleanRefillTBValue = m_license.DefaultUsageAddAmount.ToString();
                DefaultRefillTB.Text = m_cleanRefillTBValue;
                RefillUnitsLbl.Visible = true;
                RefillUnitsLbl.Text = m_license.UsageUnitsStr;
                InstructionsTB.Text = cm_refillInstructions;
                LicensingBtn.Visible = true;
                LicensingBtn.Text = cm_refillBtnText;
            }
            else if (m_license.IsSubscription)
            {
                StatusLabelLbl.Visible = true;
                StatusLabelLbl.Text = @"Expiration Date:";
                StatusLbl.Visible = true;
                StatusLbl.Text = m_license.ExpirationDate.ToShortDateString();
                if (m_license.IsExpired || m_license.IsExpirationWarning)
                { StatusLbl.ForeColor = Color.Red; }
                else
                { StatusLbl.ForeColor = Color.Black; }
                LicenseIDLabelLbl.Visible = true;
                LicenseIDLbl.Visible = true;
                LicenseIDLbl.Text = m_license.LicenseID.ToString();
                CustomerNameLabelLbl.Visible = true;
                CustomerNameLbl.Text = m_license.CompanyName;
                InstallationNameLabelLbl.Visible = true;
                InstallationNameLbl.Text = m_license.InstallationName;
                WarningChkBx.Visible = true;
                WarningChkBx.Text = cm_ExpirationWarningChkBxText;
                WarningUnitsLbl.Visible = true;
                WarningTB.Visible = true;
                WarningUnitsLbl.Text = @"days";
                int warningAmt = m_license.ExpirationWarningLevel;
                WarningChkBx.Checked = (warningAmt != 1);
                WarningTB.Enabled = WarningChkBx.Checked;
                WarningUnitsLbl.Enabled = WarningChkBx.Checked;
                m_cleanWarningTBValue = (warningAmt != -1) ? warningAmt.ToString() : @"";
                WarningTB.Text = m_cleanWarningTBValue;
                DefaultRefillLbl.Visible = false;
                DefaultRefillTB.Visible = false;
                RefillUnitsLbl.Visible = false;
                InstructionsTB.Text = cm_releaseInstructions;
                LicensingBtn.Visible = true;
                LicensingBtn.Text = cm_releaseBtnText;
            }
        }

        private void LicensingBtn_Click(object sender, EventArgs e)
        {
            if (!m_license.IsActivated)
            {
                ActivateLicenseOnlineDlg dlg = new ActivateLicenseOnlineDlg(m_license);
            }
            else if (m_license.IsMetered)
            {
                UsageBalanceTransferDlg dlg = new UsageBalanceTransferDlg(m_license, false);
                if (dlg.ShowDialog() == DialogResult.OK)
                { setState(); }
            }
            else if (m_license.IsSubscription || m_license.IsPerpetual)
            {
                if (MessageBox.Show(@"Are you sure you want to deactivate your " + ProductInfo.ProductBaseName + @" license on this computer?", @"Confirm Deactivate License",
                                    MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    if (m_license.Deactivate())
                    { MessageBox.Show(@"License deactivated successfully"); }
                    else
                    { MessageBox.Show(@"Could not deactivate license: " + m_license.ErrorMessage); }
                }
            }
            setState();
        }

        private void WarningTB_Leave(object sender, EventArgs e)
        {
            if (!WarningTB.Text.Equals(m_cleanWarningTBValue))
            {
                int warningLevel;
                if (Int32.TryParse(WarningTB.Text, out warningLevel))
                {
                    if (m_license.IsSubscription)
                    { m_license.ExpirationWarningLevel = warningLevel; }
                    else
                    { m_license.UsageUnitsWarningLevel = warningLevel; }
                    m_cleanWarningTBValue = WarningTB.Text;
                }
                else
                { WarningTB.Text = m_cleanWarningTBValue; }
            }
        }

        private void DefaultRefillTB_Leave(object sender, EventArgs e)
        {
            if (!DefaultRefillTB.Text.Equals(m_cleanRefillTBValue))
            {
                int defaultRefillAmt;
                if (Int32.TryParse(DefaultRefillTB.Text, out defaultRefillAmt))
                {
                    m_license.DefaultUsageAddAmount = defaultRefillAmt;
                    m_cleanRefillTBValue = DefaultRefillTB.Text;
                }
                else
                { DefaultRefillTB.Text = m_cleanRefillTBValue; }
            }
        }

        private void WarningChkBx_CheckedChanged(object sender, EventArgs e)
        {
            WarningTB.Text = @"";
            WarningTB.Enabled = WarningChkBx.Checked;
            WarningUnitsLbl.Enabled = WarningChkBx.Checked;
            if (!WarningChkBx.Checked)
            {
                if (m_license.IsSubscription)
                { m_license.ExpirationWarningLevel = -1; }
                else if (m_license.IsMetered)
                { m_license.UsageUnitsWarningLevel = -1; }
            }
            else
            { m_cleanWarningTBValue = WarningTB.Text; }
        }
    }
}
