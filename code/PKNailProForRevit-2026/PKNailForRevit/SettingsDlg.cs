using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PKNailForRevit
{
    internal partial class SettingsDlg : Form
    {
        private DocumentSettings m_docSettings;
        private PKNailAppSettings m_appSettings;
        private List<string> m_familyCategories;
        private PKLicense.PKLicense m_license;
        private UnitSystem m_displayUS;
        private UserControl m_curSettingsPage;

        internal SettingsDlg(PKNailAppSettings appSettings, DocumentSettings docSettings,
                             List<string> familyCategories, PKLicense.PKLicense license, UnitSystem displayUS)
        {
            m_docSettings = docSettings;
            m_appSettings = appSettings;
            m_familyCategories = familyCategories;
            m_license = license;
            m_displayUS = displayUS;
            m_curSettingsPage = null;

            InitializeComponent();
        }

        internal DocumentSettings DocSettings  { get { return m_docSettings; } }

        internal PKNailAppSettings AppSettings  { get { return m_appSettings; } }

        private void SettingsPageTV_AfterSelect(object sender, TreeViewEventArgs e)
        {
            TreeNode node = e.Node;
            if (node.IsSelected)
            {
                if (e.Node.Name == @"RoundingNode")
                {
                    if (m_curSettingsPage != null)
                    {
                        if (!(m_curSettingsPage is SettingsProjectRoundingCtl))
                        { clearCurSettingsPage(); }
                    }

                    if (m_curSettingsPage == null)
                    { setCurSettingsPage(new SettingsProjectRoundingCtl(m_docSettings, m_displayUS)); }
                }

                else if (e.Node.Name == @"RFKeypadMapsNode")
                {
                    if (m_curSettingsPage != null)
                    {
                        if (!(m_curSettingsPage is SettingsRFKeypadMapCtl))
                        { clearCurSettingsPage(); }
                    }

                    if (m_curSettingsPage == null)
                    { setCurSettingsPage(new SettingsRFKeypadMapCtl(m_docSettings, m_familyCategories)); }
                }

                else if (e.Node.Name == @"ProjectGeneralNode")
                {
                    if (m_curSettingsPage != null)
                    {
                        if (!(m_curSettingsPage is SettingsProjectGeneralCtl))
                        { clearCurSettingsPage(); }
                    }

                    if (m_curSettingsPage == null)
                    { setCurSettingsPage(new SettingsProjectGeneralCtl(m_appSettings, m_docSettings)); }
                }

                else if (e.Node.Name == @"ApplicationSettingsNode")
                {
                    if (m_curSettingsPage != null)
                    {
                        if (!(m_curSettingsPage is SettingsAppCtl))
                        { clearCurSettingsPage(); }
                    }

                    if (m_curSettingsPage == null)
                    { setCurSettingsPage(new SettingsAppCtl(m_appSettings)); }
                }

                else if (e.Node.Name == @"LicensingSettingsNode")
                {
                    if (m_curSettingsPage != null)
                    {
                        if (!(m_curSettingsPage is SettingsLicenseCtl))
                        { clearCurSettingsPage(); }
                    }

                    if (m_curSettingsPage == null)
                    {
                        setCurSettingsPage(new SettingsLicenseCtl(m_license));
                    }
                }

                else if (e.Node.Name == @"UpdatesSettingsNode")
                {
                    if (m_curSettingsPage != null)
                    {
                        if (!(m_curSettingsPage is SettingsUpdatesCtl))
                        { clearCurSettingsPage(); }
                    }

                    if (m_curSettingsPage == null)
                    { setCurSettingsPage(new SettingsUpdatesCtl(m_appSettings)); }
                }

                else if (e.Node.Name == @"AboutSettingsNode")
                {
                    if (m_curSettingsPage != null)
                    {
                        if (!(m_curSettingsPage is SettingsAboutCtl))
                        { clearCurSettingsPage(); }
                    }

                    if (m_curSettingsPage == null)
                    { setCurSettingsPage(new SettingsAboutCtl()); }
                }
            }
        }

        private void setCurSettingsPage(UserControl settingsCtl)
        {
            m_curSettingsPage = settingsCtl;
            m_curSettingsPage.Parent = this;
            m_curSettingsPage.Location = new Point(SettingsPageTV.Location.X + SettingsPageTV.Size.Width + 12,
                                                   SettingsPageTV.Location.Y);
            m_curSettingsPage.Show();
        }

        private void clearCurSettingsPage()
        {
            if (m_curSettingsPage == null) { return; }
            m_curSettingsPage.Hide();
            m_curSettingsPage.Dispose();
            m_curSettingsPage = null;
        }

        private void SettingsDlg_Load(object sender, EventArgs e)
        {
            if (!m_license.IsValid)
            {
                disableNode(SettingsPageTV, findNode(SettingsPageTV, @"ApplicationSettingsNode"));
                disableNode(SettingsPageTV, findNode(SettingsPageTV, @"UpdatesSettingsNode"));
                disableNode(SettingsPageTV, findNode(SettingsPageTV, @"ProjectSettingsNode"));
            }

            if (m_appSettings == null)
            {
                disableNode(SettingsPageTV, findNode(SettingsPageTV, @"ApplicationSettingsNode"));
                disableNode(SettingsPageTV, findNode(SettingsPageTV, @"UpdatesSettingsNode"));
            }

            if (m_docSettings == null)
            {
                disableNode(SettingsPageTV, findNode(SettingsPageTV, @"ProjectSettingsNode"));
            }

            SettingsPageTV.ExpandAll();
            SettingsPageTV.Focus();
        }

        private TreeNode findNode(TreeView tv, string nodeName)
        {
            TreeNode retval = null;
            foreach (TreeNode node in tv.Nodes)
            {
                if (node.Name == nodeName)
                { retval = node; break; }
                else
                {
                    foreach (TreeNode subNode in node.Nodes)
                    { if (subNode.Name == nodeName)  { retval = subNode; break; } }
                }
                if (retval != null)  { break; }
            }
            return retval;
        }

        private void disableNode(TreeView tv, TreeNode node)
        {
            node.ForeColor = SystemColors.GrayText;
            foreach (TreeNode subNode in node.Nodes)
            { disableNode(tv, subNode); }
        }


        private void SettingsPageTV_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            e.Cancel = (e.Node.ForeColor == SystemColors.GrayText);
        }

        private void SettingsPageTV_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Control)
            {
                int idx = -1;
                switch (e.KeyCode)
                {
                    case Keys.P:  idx = 0; break;
                    case Keys.A:  idx = 1; break;
                    case Keys.L:  idx = 2; break;
                    case Keys.B:  idx = 3; break;
                    default:               break;
                }
                if (idx != -1)
                { SettingsPageTV.SelectedNode = SettingsPageTV.Nodes[idx]; }
            }
        }
    }
}
