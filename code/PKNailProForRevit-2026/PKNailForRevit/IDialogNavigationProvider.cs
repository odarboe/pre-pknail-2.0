using System;
using System.Collections.Generic;
using System.Text;

namespace PKNailForRevit
{
    public enum DialogNavigationAction { Unknown, ToTaggedControl, TabUp, TabDown, Select, Toggle, SelectOrToggle, Deselect, OK, Cancel };   

    public class DialogNavigationEventArgs : EventArgs
    {
        private string m_controlTag;
        private bool m_isSubControl;
        private DialogNavigationAction m_navigationAction;

        internal DialogNavigationEventArgs(string controlTag, bool isSubControl)
        {
            m_controlTag = controlTag;
            m_isSubControl = isSubControl;
            m_navigationAction = DialogNavigationAction.ToTaggedControl;
        }

        internal DialogNavigationEventArgs(DialogNavigationAction action)
        {
            m_navigationAction = action;
            m_controlTag = null;
        }

        internal DialogNavigationAction NavigationAction { get { return m_navigationAction; } }
        internal bool IsSubControl { get { return m_isSubControl; } }
        internal string ControlTag { get { return (m_isSubControl ? null : m_controlTag); } }
        internal string SubControlTag { get { return (m_isSubControl ? m_controlTag : null); } }        
    }

    public delegate void DialogNavigationEventHandler(object sender, DialogNavigationEventArgs e);

    public interface IDialogNavigationProvider
    {
        void AddControlTag(string tag);
        void AddSubControlTag(string tag);

        void PushContext(DialogNavigationEventHandler handler);
        void PopContext();
    }
}
