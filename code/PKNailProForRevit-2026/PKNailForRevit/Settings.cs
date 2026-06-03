using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;

namespace PKNailForRevit
{
    public class PKNailAppSettings : ApplicationSettingsBase
    {
        [UserScopedSetting]
        [DefaultSettingValue(@"")]
        public string UserName
        {
            get { return (string)this[@"UserName"]; }
            set { this[@"UserName"] = value; }
        }

        [UserScopedSetting]
        [DefaultSettingValue(@"")]
        public string RangefinderComPort
        {
            get { return (string)this[@"RangefinderComPort"]; }
            set { this[@"RangefinderComPort"] = value; }
        }

        [UserScopedSetting]
        [DefaultSettingValue(@"true")]
        public bool IsAutoDetectComPort
        {
            get { return (bool)this[@"IsAutoDetectComPort"]; }
            set { this[@"IsAutoDetectComPort"] = value; }
        }

        [UserScopedSetting]
        [DefaultSettingValue(@"5")]
        public int AutoSaveInterval
        {
            get { return (int)this[@"AutoSaveInterval"]; }
            set { this[@"AutoSaveInterval"] = value; }
        }

        [UserScopedSetting]
        [DefaultSettingValue(@"true")]
        public bool IsPeriodicUpdateCheck
        {
            get { return (bool)(this[@"IsPeriodicUpdateCheck"]); }
            set { this[@"IsPeriodicUpdateCheck"] = value; }
        }

        [UserScopedSetting]
        [DefaultSettingValue(@"1/1/0001")]
        public DateTime LastUpdateCheck
        {
            get { return (DateTime)(this[@"LastUpdateCheck"]); }
            set { this[@"LastUpdateCheck"] = value; }
        }

        [UserScopedSetting]
        [DefaultSettingValue(@"")]
        public string SkipVersion
        {
            get { return (string)this[@"SkipVersion"]; }
            set { this[@"SkipVersion"] = value; }
        }

        [UserScopedSetting]
        [DefaultSettingValue(@"true")]
        public bool IsOpenProjectSettingsOnCreateDocument
        {
            get { return (bool)(this[@"IsOpenProjectSettingsOnCreateDocument"]); }
            set { this[@"IsOpenProjectSettingsOnCreateDocument"] = value; }
        }

        [UserScopedSetting]
        [DefaultSettingValue(@"")]
        public string LastProjectName
        {
            get { return (string)this[@"LastProjectName"]; }
            set { this[@"LastProjectName"] = value; }
        }
    }

    public class DocumentSettings : JsonSerializable
    {
        private string m_projectName;

        private Distance m_locationRounding;
        private Distance m_wallWidthRounding;
        private Distance m_featureDimensionRounding;
        private Distance m_measurementTolerance;

        private Distance m_wallIntersectFloorElevationOffset;
        private Distance m_fullOpeningTopElevationOffset;
        private Distance m_cutPlaneElevation;

        private RevitDocument.RangefinderKeypadMaps m_rfKeypadMaps;
        private bool m_isStructuralWalls;

        public DocumentSettings()
        {
            m_projectName = @"";

            m_locationRounding = Distance.NoValue;
            m_wallWidthRounding = Distance.NoValue;
            m_featureDimensionRounding = Distance.NoValue;
            m_measurementTolerance = Distance.NoValue;

            m_fullOpeningTopElevationOffset = Distance.NoValue;
            m_wallIntersectFloorElevationOffset = Distance.NoValue;
            m_cutPlaneElevation = Distance.NoValue;

            m_rfKeypadMaps = new RevitDocument.RangefinderKeypadMaps();
            m_isStructuralWalls = false;
        }

        public DocumentSettings(UnitSystem unitSystem)
        {
            m_projectName = @"";

            if (unitSystem == UnitSystem.Imperial)
            {
                m_locationRounding = new Distance((1.0 / 12.0) * 0.25, Distance.Units.Imperial);   // default 1/4 inch
                m_wallWidthRounding = new Distance((1.0 / 12.0) * 0.125, Distance.Units.Imperial);  // default 1/8 inch
                m_featureDimensionRounding = new Distance((1.0 / 12.0) * 0.25, Distance.Units.Imperial);  // default 1/4 inch
                m_measurementTolerance = new Distance((1.0 / 12) * 0.25, Distance.Units.Imperial);  // default 1/4 inch

                m_fullOpeningTopElevationOffset = new Distance(1.0 / (12.0 * 16.0), Distance.Units.Imperial);  // default 1/16 inch
                m_wallIntersectFloorElevationOffset = new Distance((1.0 / 12.0) * 3, Distance.Units.Imperial);  // default 3 inches
                m_cutPlaneElevation = new Distance(4.0, Distance.Units.Imperial);  // default 4 feet
            }
            else  // unitSystem == UnitSystem.Metric
            {
                m_locationRounding = new Distance(10.0, Distance.Units.MetricMillimeter);
                m_wallWidthRounding = new Distance(5.0, Distance.Units.MetricMillimeter);
                m_featureDimensionRounding = new Distance(10.0, Distance.Units.MetricMillimeter);
                m_measurementTolerance = new Distance(10.0, Distance.Units.MetricMillimeter);

                m_fullOpeningTopElevationOffset = new Distance(2.0, Distance.Units.MetricMillimeter);
                m_wallIntersectFloorElevationOffset = new Distance(80.0, Distance.Units.MetricMillimeter);
                m_cutPlaneElevation = new Distance(1500.0, Distance.Units.MetricMillimeter);
            }

            m_rfKeypadMaps = new RevitDocument.RangefinderKeypadMaps();
            m_isStructuralWalls = false;
        }

        public string ProjectName { get { return m_projectName; } set { m_projectName = value; } }

        public Distance LocationRounding { get { return m_locationRounding; } set { m_locationRounding = value; } }
        public Distance WallWidthRounding { get { return m_wallWidthRounding; } set { m_wallWidthRounding = value; } }
        public Distance FeatureDimensionRounding { get { return m_featureDimensionRounding; } set { m_featureDimensionRounding = value; } }
        public Distance MeasurementTolerance { get { return m_measurementTolerance; } set { m_measurementTolerance = value; } }

        public Distance FullOpeningTopElevationOffset { get { return m_fullOpeningTopElevationOffset; } set { m_fullOpeningTopElevationOffset = value; } }
        public Distance WallIntersectFloorElevationOffset { get { return m_wallIntersectFloorElevationOffset; } set { m_wallIntersectFloorElevationOffset = value; } }
        public Distance CutPlaneElevation { get { return m_cutPlaneElevation; } set { m_cutPlaneElevation = value; } }

        public RevitDocument.RangefinderKeypadMaps RFKeypadMaps { get { return m_rfKeypadMaps; } set { m_rfKeypadMaps = value; } }
        public bool IsStructuralWalls { get { return m_isStructuralWalls; } set { m_isStructuralWalls = value; } }
    }
}
