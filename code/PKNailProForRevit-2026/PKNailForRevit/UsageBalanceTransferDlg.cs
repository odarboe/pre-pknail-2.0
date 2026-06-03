using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PKNailForRevit
{
    internal partial class UsageBalanceTransferDlg : Form
    {
        private PKLicense.PKLicense m_license;
        private bool m_isReturnUsage;
        private string m_password;
        private bool m_isTransferSuccessful;

        internal UsageBalanceTransferDlg(PKLicense.PKLicense license, bool isReturnUsage)
        {
            m_license = license;
            m_isReturnUsage = isReturnUsage;
            m_password = @"";
            construct();
        }

        internal UsageBalanceTransferDlg(PKLicense.PKLicense license, bool isReturnUsage, string password)
        {
            m_license = license;
            m_isReturnUsage = isReturnUsage;
            m_password = password;
            construct();
        }

        private void construct()
        {
            InitializeComponent();

            m_isTransferSuccessful = false;

            setCurrentBalanceTBState();
            UnitsLbl1.Text = m_license.UsageUnitsStr;
            UnitsLbl2.Text = m_license.UsageUnitsStr;

            if (!m_isReturnUsage)
            {
                AddRB.Checked = true;
                if (m_license.DefaultUsageAddAmount > 0)
                { RefillTB.Text = m_license.DefaultUsageAddAmount.ToString(); }
            }
            else
            { ReturnRB.Checked = true; }

            IDTB.Text = m_license.LastUserID;
            if (!m_password.Equals(@""))
            {
                PasswordTB.Text = m_password;
                PasswordTB.Enabled = false;
                IDTB.Enabled = false;
            }

            setState();
            RefillTB.Focus();
        }

        private void AddRB_CheckedChanged(object sender, EventArgs e)
        {
            if (AddRB.Checked && ReturnRB.Checked) { ReturnRB.Checked = false; }
            setState();
        }

        private void ReturnRB_CheckedChanged(object sender, EventArgs e)
        {
            if (ReturnRB.Checked && AddRB.Checked) { AddRB.Checked = false; }
            setState();
        }

        private void OKBtn_Click(object sender, EventArgs e)
        {
            int usageToAdd;
            if (AddRB.Checked)
            { Int32.TryParse(RefillTB.Text, out usageToAdd); }
            else
            { usageToAdd = -m_license.ReserveBalance; }

            m_isTransferSuccessful = m_license.ReserveUsage(IDTB.Text, PasswordTB.Text, usageToAdd);
            if (m_isTransferSuccessful)
            {
                m_license.LastUserID = IDTB.Text;

                StatusTB.ForeColor = Color.Blue;
                StatusTB.Text = @"Success!";
                setCurrentBalanceTBState();
                AddRB.Enabled = false;
                ReturnRB.Enabled = false;
                RefillTB.Enabled = false;
                UnitsLbl2.Enabled = false;
                PasswordTB.Enabled = false;
                AccountPasswordLbl.Enabled = false;
                ForgotPasswordLinkLbl.Enabled = false;
                OKBtn.Enabled = false;
                CancelBtn.Text = @"Done";
            }
            else
            {
                StatusTB.ForeColor = Color.Red;
                StatusTB.Text = @"FAILED: " + m_license.ErrorMessage;
                OKBtn.Enabled = false;
            }
        }

        private void setState()
        {
            StatusTB.Text = @"";
            RefillTB.Enabled = AddRB.Checked;
            UnitsLbl2.Enabled = AddRB.Checked;
            int dummy;
            if (   (ReturnRB.Checked || (AddRB.Checked && Int32.TryParse(RefillTB.Text, out dummy)))
                && (IDTB.Text.Length > 0) && (PasswordTB.Text.Length > 0))
            { OKBtn.Enabled = true; }
            else
            { OKBtn.Enabled = false; }
        }

        private void setCurrentBalanceTBState()
        {
            if (m_license.IsInsufficientBalance || m_license.IsBalanceWarning)
            { CurrentBalanceLbl.ForeColor = Color.Red; }
            else
            { CurrentBalanceLbl.ForeColor = Color.Black; }
            CurrentBalanceLbl.Text = m_license.ReserveBalance.ToString();
        }

        private void IDTB_TextChanged(object sender, EventArgs e)
        { setState(); }

        private void PasswordTB_TextChanged(object sender, EventArgs e)
        { setState(); }

        private void RefillTB_TextChanged(object sender, EventArgs e)
        { setState(); }

        private void RefillTB_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsDigit(e.KeyChar))
            { e.Handled = true; }
        }

        private void UsageBalanceTransferDlg_FormClosing(object sender, FormClosingEventArgs e)
        { DialogResult = (m_isTransferSuccessful) ? DialogResult.OK : DialogResult.Cancel; }

        private void ForgotPasswordLinkLbl_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        { System.Diagnostics.Process.Start(ProductInfo.ProductLoginUrlRoot); }
    }
}
