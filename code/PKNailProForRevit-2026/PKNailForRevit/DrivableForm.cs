using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Reflection;

namespace PKNailForRevit
{
    internal class DrivableForm : Form
    {
        protected List<IMeasurementProvider> m_mps = new List<IMeasurementProvider>();
        protected List<IDialogNavigationProvider> m_dnps = new List<IDialogNavigationProvider>();

        private Dictionary<string, Control> m_taggedControlsDict = new Dictionary<string, Control>();
        private Dictionary<string, Control> m_taggedSubControlsDict = new Dictionary<string, Control>();
        private string m_curControlTag = null;

        protected Button m_OKBtn = null;

        internal DrivableForm()
        {
            foreach (Control ctl in Controls)
            {
                if (ctl.Tag != null)
                {
                    string navigationTag = (string)ctl.Tag;
                    if (navigationTag != null && navigationTag.Length > 0)
                    {
                        string[] tags = navigationTag.Split('|');
                        m_taggedControlsDict.Add(tags[0], ctl);
                        if (tags.Length > 1)
                        { m_taggedSubControlsDict.Add(navigationTag, ctl); }
                    }
                }
            }
        }

        internal DialogResult DriveAndShowDialog(List<IMeasurementProvider> mps, List<IDialogNavigationProvider> dnps)
        {
            DialogResult retval;
            drive(mps, dnps);
            retval = ShowDialog();
            undrive();
            return retval;
        }

        internal DialogResult DriveAndShowDialog(IWin32Window owner, List<IMeasurementProvider> mps, List<IDialogNavigationProvider> dnps)
        {
            DialogResult retval;
            drive(mps, dnps);
            retval = ShowDialog(owner);
            undrive();
            return retval;
        }

        protected void drive(List<IMeasurementProvider> mps, List<IDialogNavigationProvider> dnps)
        {
            if (mps != null)
            {
                foreach (IMeasurementProvider mp in mps)
                {
                    m_mps.Add(mp);
                    if (mp.IsAsyncSupported)
                    { mp.PushContext(handleMeasurementEvent); }
                }

            }

            if (dnps != null)
            {
                foreach (IDialogNavigationProvider dnp in dnps)
                {
                    m_dnps.Add(dnp);
                    dnp.PushContext(handleDialogNavigationEvent);
                }
            }
        }

        protected void undrive()
        {
            foreach (IMeasurementProvider mp in m_mps)
            { if (mp.IsAsyncSupported) { mp.PopContext(); } }
            m_mps.Clear();

            foreach (IDialogNavigationProvider dnp in m_dnps)
            { dnp.PopContext(); }
            m_dnps.Clear();
        }

        private void handleMeasurementEvent(object sender, MeasurementEventArgs e)
        {
            if (this.InvokeRequired)
            { throw new ApplicationException("MeasurementEventHandler failed: method called on the non-GUI thread"); }
            handleMeasurementResult(e.Result);
        }

        private void handleDialogNavigationEvent(object sender, DialogNavigationEventArgs e)
        {
            if (this.InvokeRequired)
            { throw new ApplicationException("NavigationRelativeEventHandler failed: method called on the non-GUI thread"); }
            handleNavigation(e);
        }

        private void handleMeasurementResult(MeasurementResult result)
        {
            Distance d = result.Measurement;
            if (d != null)
            { enterMeasurement(d); }
            else
            { System.Media.SystemSounds.Hand.Play(); }
        }

        protected virtual void enterMeasurement(Distance d)
        {
            Control ctl = this.ActiveControl;
            Tracer.WriteInfo("1: ctl: " + ctl.Name);

            while (ctl.GetType().GetInterface("IDistanceControl") == null
                    && ctl.GetType().GetInterface("IContainerControl") != null)
            {
                ctl = (ctl as IContainerControl).ActiveControl;
                Tracer.WriteInfo("2: ctl: " + ctl.Name);
            }
            Control startCtl = ctl;
            Tracer.WriteInfo("2.5: startCtl: " + startCtl.Name);


            while (ctl.GetType().GetInterface("IDistanceControl") == null)
            {
                SendKeys.SendWait("{TAB}");

                ctl = this.ActiveControl;
                Tracer.WriteInfo("3: ctl: " + ctl.Name);
                while (ctl.GetType().GetInterface("IDistanceControl") == null
                        && ctl.GetType().GetInterface("IContainerControl") != null)
                {
                    ctl = (ctl as IContainerControl).ActiveControl;
                    Tracer.WriteInfo("4: ctl: " + ctl.Name);
                }

                if (ctl == startCtl)
                { ctl = null; break; }
            }
            if (ctl != null)
            {
                (ctl as IDistanceControl).SetDistanceDirty(d);
                SendKeys.SendWait("{TAB}");
            }
            else
            {
                System.Media.SystemSounds.Hand.Play();
            }
        }

        private void handleNavigation(DialogNavigationEventArgs e)
        {
            bool isSuccess = true;

            switch (e.NavigationAction)
            {
                case DialogNavigationAction.TabDown:
                    SendKeys.SendWait("{TAB}");
                    break;
                case DialogNavigationAction.TabUp:
                    SendKeys.SendWait("+{TAB}");
                    break;
                case DialogNavigationAction.Select:
                case DialogNavigationAction.SelectOrToggle:
                case DialogNavigationAction.Deselect:
                    isSuccess = manipulateActiveControl(e.NavigationAction);
                    break;
                case DialogNavigationAction.OK:
                    if (m_OKBtn != null && m_OKBtn.Enabled)
                    { m_OKBtn.PerformClick(); }
                    else
                    { isSuccess = false; }
                    break;
                case DialogNavigationAction.Cancel:
                    SendKeys.SendWait("{ESC}");
                    break;
                case DialogNavigationAction.ToTaggedControl:
                    isSuccess = navigateToTaggedControl(e.ControlTag, e.IsSubControl);
                    break;
            }

            if (!isSuccess) { System.Media.SystemSounds.Hand.Play(); }
        }

        private bool manipulateActiveControl(DialogNavigationAction action)
        {
            Control ctl = ActiveControl;
            bool isSuccess = false;
            Type ctlType = ctl.GetType();

            PropertyInfo selIdxPropInfo = ctlType.GetProperty("SelectedIndex", typeof(Int32));
            PropertyInfo itemsPropInfo = ctlType.GetProperty("Items", typeof(ComboBox.ObjectCollection));
            if (selIdxPropInfo != null && itemsPropInfo != null)
            {
                if (action == DialogNavigationAction.Select
                    || action == DialogNavigationAction.SelectOrToggle)
                {
                    Int32 selIdx = (Int32)selIdxPropInfo.GetValue(ctl, null);
                    ComboBox.ObjectCollection items = (ComboBox.ObjectCollection)itemsPropInfo.GetValue(ctl, null);
                    if (selIdx == items.Count - 1)
                    { selIdx = 0; }
                    else
                    { selIdx++; }
                    selIdxPropInfo.SetValue(ctl, selIdx, null);
                    isSuccess = true;
                }
                return isSuccess;
            }

            PropertyInfo selPropInfo = ctlType.GetProperty("Selected", typeof(bool));
            if (selPropInfo != null)
            {
                bool isSelectAction = true;
                switch (action)
                {
                    case DialogNavigationAction.Select:
                        isSelectAction = true;
                        break;
                    case DialogNavigationAction.Deselect:
                        isSelectAction = false;
                        break;
                    case DialogNavigationAction.Toggle:
                        isSelectAction = !(bool)selPropInfo.GetValue(ctl, null);
                        break;
                    case DialogNavigationAction.SelectOrToggle:
                        isSelectAction = true;
                        break;
                }
                selPropInfo.SetValue(ctl, isSelectAction, null);
                isSuccess = true;
                return isSuccess;
            }

            PropertyInfo checkedPropInfo = ctlType.GetProperty("Checked", typeof(bool));
            if (checkedPropInfo != null)
            {
                bool isCheckAction = true;
                switch (action)
                {
                    case DialogNavigationAction.Select:
                        isCheckAction = true;
                        break;
                    case DialogNavigationAction.Deselect:
                        isCheckAction = false;
                        break;
                    case DialogNavigationAction.Toggle:
                        isCheckAction = !(bool)checkedPropInfo.GetValue(ctl, null);
                        break;
                    case DialogNavigationAction.SelectOrToggle:
                        isCheckAction = !(bool)checkedPropInfo.GetValue(ctl, null);
                        break;
                }
                checkedPropInfo.SetValue(ctl, isCheckAction, null);
                isSuccess = true;
                return isSuccess;
            }

            MethodInfo onClickMethodInfo = ctlType.GetMethod("PerformClick");
            if (onClickMethodInfo != null
                && (action == DialogNavigationAction.Select || action == DialogNavigationAction.SelectOrToggle))
            {
                onClickMethodInfo.Invoke(ctl, null);
                isSuccess = true;
            }
            
            return isSuccess;
        }

        private bool navigateToTaggedControl(string controlTag, bool isSubControl)
        {
            bool isSuccess = true;

            if (!isSubControl)
            {
                m_curControlTag = controlTag;
                if (m_taggedControlsDict.ContainsKey(controlTag))
                { ActiveControl = m_taggedControlsDict[controlTag]; }
            }
            else
            {
                if (m_curControlTag != null)
                {
                    string subControlTag = m_curControlTag + '|' + controlTag;
                    if (m_taggedSubControlsDict.ContainsKey(subControlTag))
                    { ActiveControl = m_taggedSubControlsDict[subControlTag]; }
                }
                else { isSuccess = false; }
            }

            return isSuccess;
        }
    }
}
