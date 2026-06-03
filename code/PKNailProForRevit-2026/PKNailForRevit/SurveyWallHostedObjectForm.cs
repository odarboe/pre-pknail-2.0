using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PKNailForRevit
{
    internal class SurveyWallHostedObjectForm : DrivableForm
    {
        protected CommandContext m_cmdContext;
        protected WallHostedObjectSurveyParams m_whosParams;

        protected DistanceControl m_locationDC;
        protected DistanceControl m_widthDC;
        protected Label m_widthLbl;
        protected DistanceControl m_heightDC;
        protected Label m_heightLbl;
        protected DistanceControl m_depthDC;
        protected Label m_depthLbl;
        protected DistanceControl m_sillHeightDC;
        protected Label m_sillHeightLbl;

        protected CheckBox m_centerChkBx;
        protected ComboBox m_typeCB;

        internal SurveyWallHostedObjectForm(CommandContext cmdContext, WallHostedObjectSurveyParams whosParams)
        {
            m_cmdContext = cmdContext;
            m_whosParams = whosParams;

            m_locationDC = null;
            m_widthDC = null;
            m_widthLbl = null;
            m_heightDC = null;
            m_heightLbl = null;
            m_depthDC = null;
            m_depthLbl = null;
            m_sillHeightDC = null;
            m_sillHeightLbl = null;
            m_centerChkBx = null;
            m_typeCB = null;
        }

        public SurveyWallHostedObjectForm()
        {
            m_locationDC = null;
            m_widthDC = null;
            m_widthLbl = null;
            m_heightDC = null;
            m_heightLbl = null;
            m_depthDC = null;
            m_depthLbl = null;
            m_sillHeightDC = null;
            m_sillHeightLbl = null;
            m_centerChkBx = null;
            m_typeCB = null;
        }

        public WallHostedObjectSurveyParams Params { get { return m_whosParams; } }

        protected void bindControls(DistanceControl locationDC,
                                    DistanceControl widthDC,
                                    Label widthLbl,
                                    DistanceControl heightDC,
                                    Label heightLbl,
                                    DistanceControl depthDC,
                                    Label depthLbl,
                                    DistanceControl sillHeightDC,
                                    Label sillHeightLbl,
                                    CheckBox centerChkBx,
                                    ComboBox typeCB,
                                    Button OKBtn)
        {
            m_locationDC = locationDC;
            m_widthDC = widthDC;
            m_widthLbl = widthLbl;
            m_heightDC = heightDC;
            m_heightLbl = heightLbl;
            m_depthDC = depthDC;
            m_depthLbl = depthLbl;
            m_sillHeightDC = sillHeightDC;
            m_sillHeightLbl = sillHeightLbl;
            m_centerChkBx = centerChkBx;
            m_typeCB = typeCB;
            m_OKBtn = OKBtn;

            Distance.Units displayUnits = (m_cmdContext.DisplayUnitSystem == UnitSystem.Imperial) ?
                Distance.Units.Imperial : Distance.Units.MetricMillimeter;

            m_locationDC.DisplayUnits = displayUnits;
            m_locationDC.TextChanged += new EventHandler(dc_TextChanged);
            m_locationDC.RoundingIncrement = m_cmdContext.ModelSettings.LocationRounding;

            m_centerChkBx.CheckedChanged += new EventHandler(centerChkBx_CheckedChanged);
            m_typeCB.SelectedIndexChanged += new EventHandler(typeCB_SelectedIndexChanged);

            if (m_widthDC != null)
            {
                m_widthDC.DisplayUnits = displayUnits;
                m_widthDC.TextChanged += new EventHandler(dc_TextChanged);
                m_widthDC.Validated += new EventHandler(widthDC_Validated);
                m_widthDC.RoundingIncrement = m_cmdContext.ModelSettings.FeatureDimensionRounding;
            }
            if (m_heightDC != null)
            {
                m_heightDC.DisplayUnits = displayUnits;
                m_heightDC.TextChanged += new EventHandler(dc_TextChanged);
                m_heightDC.RoundingIncrement = m_cmdContext.ModelSettings.FeatureDimensionRounding;
            }
            if (m_depthDC != null)
            {
                m_depthDC.DisplayUnits = displayUnits;
                m_depthDC.TextChanged += new EventHandler(dc_TextChanged);
                m_depthDC.RoundingIncrement = m_cmdContext.ModelSettings.FeatureDimensionRounding;
            }
            if (m_sillHeightDC != null)
            {
                m_sillHeightDC.DisplayUnits = displayUnits;
                m_sillHeightDC.TextChanged += new EventHandler(dc_TextChanged);
                m_sillHeightDC.RoundingIncrement = m_cmdContext.ModelSettings.LocationRounding;
            }

            m_OKBtn.Click += new EventHandler(OKBtn_Click);

            this.Load += new EventHandler(form_Load);
        }

        protected virtual void form_Load(object sender, EventArgs e)
        {
            m_locationDC.SetDistanceClean(m_whosParams.Location);

            foreach (SurveyFamilyDef familyTypeDef in m_whosParams.ProjectSurveyFamilyDefs)
            { m_typeCB.Items.Add(familyTypeDef.Name); }

            switch (m_whosParams.ProjectSurveyFamilyDefs.Count)
            {
                case 0:
                    m_typeCB.Enabled = false;
                    break;
                case 1:
                    m_typeCB.SelectedIndex = 0;
                    m_typeCB.Enabled = false;
                    break;
                default:
                    m_typeCB.SelectedIndex = m_whosParams.SurveyFamilyDefIdx;
                    break;
            }

            setState();
        }

        protected virtual void centerChkBx_CheckedChanged(object sender, EventArgs args)
        {
            if (m_centerChkBx.Checked)
            {
                setCenteredLocation();
                m_locationDC.Enabled = false;
            }
            else
            {
                m_locationDC.ClearDistance();
                m_locationDC.Enabled = true;
            }

            setState();
        }

        protected virtual void typeCB_SelectedIndexChanged(object sender, EventArgs args)
        {
            if (m_typeCB.SelectedIndex != -1)
            {
                m_whosParams.SurveyFamilyDefIdx = m_typeCB.SelectedIndex;
                if (m_widthDC != null) { m_widthDC.SetDistanceClean(m_whosParams.GetWidth()); }
                if (m_heightDC != null) { m_heightDC.SetDistanceClean(m_whosParams.GetHeight()); }
                if (m_depthDC != null) { m_depthDC.SetDistanceClean(m_whosParams.GetDepth()); }
                if (m_sillHeightDC != null)
                {
                    Distance SillHeight = m_whosParams.GetSillHeight();
                    if (!SillHeight.IsNoValue)
                    { m_sillHeightDC.SetDistanceClean(SillHeight); }
                    else
                    { m_sillHeightDC.SetDistanceClean(Distance.Zero); }
                }
            }
            else
            {
                if (m_widthDC != null) { m_widthDC.ClearDistance(); }
                if (m_heightDC != null) { m_heightDC.ClearDistance(); }
                if (m_depthDC != null) { m_depthDC.ClearDistance(); }
            }

            if (m_centerChkBx.Checked) { setCenteredLocation(); }

            setState();
        }

        protected virtual void OKBtn_Click(object sender, EventArgs args)
        {
            m_whosParams.SurveyFamilyDefIdx = m_typeCB.SelectedIndex;
            m_whosParams.Location = (m_centerChkBx.Checked) ? m_locationDC.DistanceValueNotRounded : m_locationDC.DistanceValue;
            SurveyFamilyDef.FixedDimensions fixedDims = m_whosParams.GetFixedDimensions();
            if (   m_widthDC != null
                && (fixedDims & SurveyFamilyDef.FixedDimensions.Width) == SurveyFamilyDef.FixedDimensions.None)
            { m_whosParams.SetWidth(m_widthDC.DistanceValue); }
            if (   m_heightDC != null
                && (fixedDims & SurveyFamilyDef.FixedDimensions.Height) == SurveyFamilyDef.FixedDimensions.None)
            { m_whosParams.SetHeight(m_heightDC.DistanceValue); }
            if (   m_depthDC != null
                && (fixedDims & SurveyFamilyDef.FixedDimensions.Depth) == SurveyFamilyDef.FixedDimensions.None)
            { m_whosParams.SetDepth(m_depthDC.DistanceValue); }
            if (m_sillHeightDC != null) { m_whosParams.SetSillHeight(m_sillHeightDC.DistanceValue); }
        }

        protected virtual void widthDC_Validated(object sender, EventArgs args)
        {
            if (m_centerChkBx.Checked) { setCenteredLocation(); }
        }

        protected void dc_TextChanged(object sender, EventArgs e)
        { setState(); }

        protected virtual void setCenteredLocation()
        {
            if ((m_widthDC != null && !m_widthDC.IsDistanceNoValue) || m_whosParams.GetIsMeasureLocationCentered())
            {
                Distance centerD = new Distance(m_whosParams.HostWallLength);
                if (!m_widthDC.IsDistanceNoValue && !m_whosParams.GetIsMeasureLocationCentered())
                { centerD -= m_widthDC.DistanceValueNotRounded; }
                centerD *= 0.5;
                m_locationDC.SetDistanceClean(centerD);
            }
            else
            { m_locationDC.ClearDistance(); }
        }

        protected virtual void setState()
        {
            SurveyFamilyDef.FixedDimensions fixedDims = m_whosParams.GetFixedDimensions();
            if (m_widthDC != null)
            {
                if ((fixedDims & SurveyFamilyDef.FixedDimensions.Width) != SurveyFamilyDef.FixedDimensions.None)
                { m_widthDC.Enabled = false; m_widthLbl.Enabled = false; }
                else
                { m_widthDC.Enabled = true; m_widthLbl.Enabled = true; }
            }
            if (m_heightDC != null)
            {
                if ((fixedDims & SurveyFamilyDef.FixedDimensions.Height) != SurveyFamilyDef.FixedDimensions.None)
                { m_heightDC.Enabled = false; m_heightLbl.Enabled = false; }
                else
                { m_heightDC.Enabled = true; m_heightLbl.Enabled = true; }
            }
            if (m_depthDC != null)
            {
                if ((fixedDims & SurveyFamilyDef.FixedDimensions.Depth) != SurveyFamilyDef.FixedDimensions.None)
                { m_depthDC.Enabled = false; m_depthLbl.Enabled = false; }
                else
                { m_depthDC.Enabled = true; m_depthLbl.Enabled = true; }
            }
            if (m_sillHeightDC != null)
            {
                if ((fixedDims & SurveyFamilyDef.FixedDimensions.SillHeight) != SurveyFamilyDef.FixedDimensions.None)
                { m_sillHeightDC.Enabled = false; m_sillHeightLbl.Enabled = false; }
                else
                { m_sillHeightDC.Enabled = true; m_sillHeightLbl.Enabled = true; }
            }


            if (   !m_locationDC.DistanceValue.IsNoValue
                && (m_widthDC == null || !m_widthDC.DistanceValue.IsNoValue || !m_widthDC.Enabled)
                && (m_heightDC == null || !m_heightDC.DistanceValue.IsNoValue || !m_heightDC.Enabled)
                && (m_depthDC == null || !m_depthDC.DistanceValue.IsNoValue || !m_depthDC.Enabled) )
            { m_OKBtn.Enabled = true; }
            else
            { m_OKBtn.Enabled = false; }
        }
    }
}
