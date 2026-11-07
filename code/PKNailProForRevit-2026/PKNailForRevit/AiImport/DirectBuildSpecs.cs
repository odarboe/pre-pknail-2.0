#nullable enable

using PKNail.AiImport.Mapping;

namespace PKNailForRevit
{
    /// <summary>
    /// Epic 13 — one wall for <see cref="IAiImportTarget.CreateWallDirect"/>. Every measurement is a
    /// <see cref="Distance"/> (converted from meters at the import boundary by <c>DirectBuildImporter</c>).
    /// The wall is drawn on its centerline from Start to End.
    /// </summary>
    public sealed class DirectWallSpec
    {
        public Distance StartX = Distance.Zero;
        public Distance StartY = Distance.Zero;
        public Distance EndX = Distance.Zero;
        public Distance EndY = Distance.Zero;
        public Distance Thickness = Distance.NoValue;
        public Distance Height = Distance.NoValue;

        /// <summary>Base offset above the level (the AI's LevelOffsetMeters).</summary>
        public Distance BaseOffset = Distance.Zero;

        /// <summary>Revit Level element id to host on (from <c>LevelDataItem.ElementId</c>).</summary>
        public long LevelElementId = -1;

        /// <summary>Written to the wall's Mark (the AI wall id, or W{n}).</summary>
        public string Mark = string.Empty;

        /// <summary>Written to the wall's Comments when present.</summary>
        public string? Comment;
    }

    /// <summary>
    /// Epic 13 — one door/window for <see cref="IAiImportTarget.CreateOpeningDirect"/>, placed at its
    /// CENTRE <see cref="LocationFromStart"/> along the host wall's location line.
    /// </summary>
    public sealed class DirectOpeningSpec
    {
        public bool IsDoor;
        public Distance LocationFromStart = Distance.Zero;
        public Distance Width = Distance.NoValue;
        public Distance Height = Distance.NoValue;

        /// <summary>Bottom of the opening above the wall's base (doors usually 0).</summary>
        public Distance Sill = Distance.Zero;

        /// <summary>Door only; null = not provided by the AI (keep the Revit default).</summary>
        public HingeSideKind? Hinge;

        /// <summary>Door only; null = not provided by the AI (keep the Revit default).</summary>
        public SwingDirectionKind? Swing;

        /// <summary>The AI's <c>OpeningType</c> (v2.1), e.g. "Sliding"; null when not provided. Chooses the
        /// PKNAIL survey family first (see <c>OpeningFamilyResolver</c>).</summary>
        public string? OpeningType;

        /// <summary>The AI's <c>FamilyDefIdx</c>: index into the project's PKNAIL survey-family list (the
        /// 1.0 Capture list order). Chooses the family when <see cref="OpeningType"/> does not.</summary>
        public int FamilyDefIdx;

        /// <summary>Unit plan direction (unitless) pointing to the wall's interior side.</summary>
        public double InteriorX;
        public double InteriorY;

        /// <summary>Written to the instance Mark (the AI opening id, or W{n}-D{k}/N{k}).</summary>
        public string Mark = string.Empty;

        /// <summary>Written to the instance Comments when present.</summary>
        public string? Comment;
    }
}
