using System;
using System.Collections.Generic;
using System.Text;

namespace PKNailForRevit
{
    public class LevelsData : JsonSerializable
    {
        #region Data Members
        private bool m_isLevelsLocked = false;
        private LevelDataList m_LevelData = new LevelDataList();
        #endregion

        #region Properties
        public bool IsLevelsLocked   { get { return m_isLevelsLocked; } set { m_isLevelsLocked = value; } }
        public LevelDataList Levels   { get { return m_LevelData; } set { m_LevelData = value; } }
        public LevelDataItem LevelDataAt(int idx)    { return m_LevelData[m_LevelData.Count - 1 - idx]; }
        #endregion

        public LevelsData()  { }

        public LevelsData(LevelsData copyFrom)
        {
            m_isLevelsLocked = copyFrom.m_isLevelsLocked;
            for (int i = 0; i < copyFrom.m_LevelData.Count; i++)
            { m_LevelData.Add(new LevelDataItem(copyFrom.m_LevelData[i])); }
        }

        internal LevelDataItem InsertLevel(int idx, string name)
        {
            LevelDataItem retval = new LevelDataItem(name);
            m_LevelData.Insert(idx, retval);
            return retval;
        }

        internal LevelDataItem InsertLevel(string name, string elId, Distance projElev)
        {
            LevelDataItem retval = null;

            if (m_LevelData.Count == 0)
            { retval = insertLevel(0, name, elId, projElev); }
            else
            {
                bool isLevelCreated = false;
                for (int lvlIdx = 0; lvlIdx < m_LevelData.Count; lvlIdx++)
                {
                    if (m_LevelData[lvlIdx].ProjectElev < projElev)
                    {
                        retval = insertLevel(lvlIdx, name, elId, projElev);
                        isLevelCreated = true;
                        break;
                    }
                }
                if (!isLevelCreated)
                {
                    // elevation of Revit level is below the lowest level
                    retval = insertLevel(m_LevelData.Count, name, elId, projElev);
                }
            }

            return retval;
        }

        internal void RecalculateFloor2FloorElevations()
        {
            if (Levels.Count < 2) { return; }

            int lvlIdx;
            for (lvlIdx = Levels.Count - 1; lvlIdx >= 1; lvlIdx--)
            {
                if (   Levels[lvlIdx].ProjectElev != Distance.NoValue
                    && Levels[lvlIdx - 1].ProjectElev != Distance.NoValue)
                { Levels[lvlIdx].Floor2FloorElev = Levels[lvlIdx - 1].ProjectElev - Levels[lvlIdx].ProjectElev; }
            }
            Levels[0].Floor2FloorElev = Distance.NoValue;
        }

        internal void RecalculateProjectElevations()
        {
            if (Levels.Count == 0) { return; }

            // Find the level with reference project elevation
            int refLvlIdx;
            for (refLvlIdx = 0; refLvlIdx < Levels.Count; refLvlIdx++)
            { if (Levels[refLvlIdx].IsRefElev) { break; } }
            if (refLvlIdx == Levels.Count)
            {
                // No reference level; if there is a level with zero elevation, set that as ref
                for (refLvlIdx = 0; refLvlIdx < Levels.Count; refLvlIdx++)
                {
                    if (Levels[refLvlIdx].ProjectElev.IsZero)
                    { Levels[refLvlIdx].IsRefElev = true; break; }
                }

                if (refLvlIdx == Levels.Count)
                {
                    // No zero-elevation level; set the ref level to be the lowest level
                    // and give it an elevation of zero if not already set
                    refLvlIdx = Levels.Count - 1;
                    Levels[refLvlIdx].IsRefElev = true;
                    if (Levels[refLvlIdx].ProjectElev.IsNoValue)
                    { Levels[refLvlIdx].ProjectElev = Distance.Zero; }
                }
            }

            // Calculate project elevations relative to reference level
            // First, calc from reference level down to bottom...
            int lvlIdx;
            for (lvlIdx = refLvlIdx + 1; lvlIdx < Levels.Count; lvlIdx++)
            {
                if (!Levels[lvlIdx - 1].ProjectElev.IsNoValue
                    && !Levels[lvlIdx].Floor2FloorElev.IsNoValue)
                {
                    Levels[lvlIdx].ProjectElev = Levels[lvlIdx - 1].ProjectElev
                                                 - Levels[lvlIdx].Floor2FloorElev;
                }
                else
                { Levels[lvlIdx].ProjectElev = Distance.NoValue; }
            }
            // ...then from reference level up to top
            for (lvlIdx = refLvlIdx - 1; lvlIdx >= 0; lvlIdx--)
            {
                if (!Levels[lvlIdx + 1].ProjectElev.IsNoValue
                    && !Levels[lvlIdx + 1].Floor2FloorElev.IsNoValue)
                {
                    Levels[lvlIdx].ProjectElev = Levels[lvlIdx + 1].ProjectElev
                                                 + Levels[lvlIdx + 1].Floor2FloorElev;
                }
                else
                { Levels[lvlIdx].ProjectElev = Distance.NoValue; }
            }
        }

        private LevelDataItem insertLevel(int idx, string name, string elId, Distance projElev)
        {
            LevelDataItem retval = new LevelDataItem(name, elId, projElev);
            m_LevelData.Insert(idx, retval);

            // Set floor to floor elevations for new level and adjust for the level below
            if (idx != 0)
            {
                m_LevelData[idx].Floor2FloorElev = m_LevelData[idx - 1].ProjectElev - m_LevelData[idx].ProjectElev;
            }
            if (idx != m_LevelData.Count - 1)
            {
                m_LevelData[idx + 1].Floor2FloorElev = m_LevelData[idx].ProjectElev - m_LevelData[idx + 1].ProjectElev;
            }
            return retval;
        }
    }

    public class LevelDataList : List<LevelDataItem> { }

    public class LevelDataItem
    {
        #region Data Members
        private string m_name;
        private string m_elementId;
        private bool m_isRefElev;
        private Distance m_projectElev;
        private Distance m_floor2FloorElev;
        private Distance m_floor2CeilingElev;
        private bool m_isDropCeiling;
        private Distance m_floor2DropCeilingElev;
        private List<WindowHeights> m_typicalWindowHeights;
        #endregion

        public LevelDataItem()
        {
            m_name = "";
            m_elementId = "";
            m_isRefElev = false;
            m_projectElev = Distance.NoValue;
            m_floor2FloorElev = Distance.NoValue;
            m_floor2CeilingElev = Distance.NoValue;
            m_isDropCeiling = false;
            m_floor2DropCeilingElev = Distance.NoValue;
            m_typicalWindowHeights = new List<WindowHeights>();
        }

        public LevelDataItem(LevelDataItem copyFrom)
        {
            m_name = copyFrom.m_name;
            m_elementId = copyFrom.m_elementId;
            m_isRefElev = copyFrom.m_isRefElev;
            m_projectElev = new Distance(copyFrom.m_projectElev);
            m_floor2FloorElev = new Distance(copyFrom.m_floor2FloorElev);
            m_floor2CeilingElev = new Distance(copyFrom.m_floor2CeilingElev);
            m_isDropCeiling = copyFrom.m_isDropCeiling;
            m_floor2DropCeilingElev = new Distance(copyFrom.m_floor2DropCeilingElev);
            m_typicalWindowHeights = new List<WindowHeights>();
        }

        public LevelDataItem(string name, string elId, Distance projElev)
        {
            m_name = name;
            m_elementId = elId;
            m_isRefElev = false;
            m_projectElev = new Distance(projElev);
            m_floor2FloorElev = Distance.NoValue;
            m_floor2CeilingElev = Distance.NoValue;
            m_isDropCeiling = false;
            m_floor2DropCeilingElev = Distance.NoValue;
            m_typicalWindowHeights = new List<WindowHeights>();
        }

        public LevelDataItem(string name)
        {
            m_name = name;
            m_elementId = "";
            m_isRefElev = false;
            m_projectElev = Distance.NoValue;
            m_floor2FloorElev = Distance.NoValue;
            m_floor2CeilingElev = Distance.NoValue;
            m_isDropCeiling = false;
            m_floor2DropCeilingElev = Distance.NoValue;
            m_typicalWindowHeights = new List<WindowHeights>();
        }

        public string Name
        { get { return m_name; } set { m_name = value; } }

        public string ElementId
        { get { return m_elementId; } set { m_elementId = value; } }

        public bool IsRefElev
        { get { return m_isRefElev; } set { m_isRefElev = value; } }

        public Distance ProjectElev
        {
            get { return m_projectElev; }
            set { m_projectElev = new Distance(value); }
        }

        public Distance Floor2FloorElev
        {
            get { return m_floor2FloorElev; }
            set { m_floor2FloorElev = new Distance(value); }
        }

        public Distance Floor2CeilingElev
        {
            get { return m_floor2CeilingElev; }
            set { m_floor2CeilingElev = new Distance(value); }
        }

        public bool IsDropCeiling
        { get { return m_isDropCeiling; } set { m_isDropCeiling = value; } }

        public Distance Floor2DropCeilingElev
        {
            get { return m_floor2DropCeilingElev; }
            set { m_floor2DropCeilingElev = new Distance(value); }
        }

        public List<WindowHeights> TypicalWindowHeights
        { get { return m_typicalWindowHeights; } set { m_typicalWindowHeights = value; } }

        public bool IsComplete
        {
            get
            {
                return (!Floor2FloorElev.IsNoValue
                        && !Floor2CeilingElev.IsNoValue
                        && !(IsDropCeiling && Floor2DropCeilingElev.IsNoValue));
            }
        }

        public bool IsEmpty
        {
            get
            {
                return (Floor2FloorElev.IsNoValue
                        && Floor2CeilingElev.IsNoValue
                        && Floor2DropCeilingElev.IsNoValue);
            }
        }

        internal void CopyElevations(LevelDataItem src)
        {
            m_floor2FloorElev = new Distance(src.m_floor2FloorElev);
            m_floor2CeilingElev = new Distance(src.m_floor2CeilingElev);
            m_isDropCeiling = src.m_isDropCeiling;
            m_floor2DropCeilingElev = new Distance(src.m_floor2DropCeilingElev);
        }
    }

    public class WindowHeights
    {
        private Distance m_sillHeight;
        private Distance m_height;

        public WindowHeights()
        {
            m_sillHeight = Distance.NoValue;
            m_height = Distance.NoValue;
        }

        public WindowHeights(Distance height, Distance sillHeight)
        {
            m_sillHeight = sillHeight;
            m_height = height;
        }

        public Distance SillHeight
        {
            get { return m_sillHeight; }
            set { m_sillHeight = value; }
        }

        public Distance Height
        {
            get { return m_height; }
            set { m_height = value; }
        }

        public Distance HeadHeight
        {
            get { return m_sillHeight + m_height; }
        }
    }
}
