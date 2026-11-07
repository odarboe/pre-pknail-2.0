using System.Collections.Generic;

namespace PKNailForRevit
{
    /// <summary>
    /// Epic 11e — the focused role interface the AI-import bridge (<c>AiModelImporter</c>) drives. It
    /// exposes ONLY the ~16 members the importer needs, instead of the full 41-member
    /// <see cref="IProjectModel"/>. <c>RevitDocument</c> implements both (the full model in-product); the
    /// <c>RecordingProjectModel</c> test fake implements just this, so it stays small. This is the
    /// Interface-Segregation half of R15.
    ///
    /// <para>Note: the members are declared here AND on <see cref="IProjectModel"/> (which is not
    /// restructured, being a wide legacy seam). A future cleanup could make
    /// <c>IProjectModel : IAiImportTarget</c> to remove the overlap; for now both are satisfied by
    /// <c>RevitDocument</c>'s single implementation of each method.</para>
    /// </summary>
    public interface IAiImportTarget
    {
        // --- Atomic transaction group (one import = one assimilated group = one undo entry) ---
        CommandResult InitializeTransaction(ref string msg);
        CommandResult SetTransactionName(string name, ref string msg);
        CommandResult StartTransaction(ref string msg);
        CommandResult CommitTransaction(ref string msg);
        CommandResult RollBackTransaction(ref string msg);

        // --- Levels ---
        LevelsData LevelsData { get; set; }

        // --- v2 absolute placement + headless engine activation ---
        CommandResult CreateWallAbsolute(double startXMeters, double startYMeters,
                                         double endXMeters, double endYMeters,
                                         Distance thickness, Distance height, int levelIdx,
                                         out long wallElementId, ref string msg);
        CommandResult CreateOpeningOnWall(long wallElementId, double locationFromStartMeters,
                                          Distance width, Distance height, Distance sillHeight,
                                          bool isDoor, out long openingElementId, ref string msg);
        CommandResult ActivateWallById(long wallId, bool interiorFace, bool nearEndIsStart, ref string msg);

        // --- Epic 13 Direct Build: native Revit walls/openings straight from the AI geometry ---
        // No survey engine, no active/reference wall, no PKNAIL survey families required. Each call runs in
        // its own inner transaction inside the import transaction group.
        CommandResult CreateWallDirect(DirectWallSpec spec, out long wallElementId, ref string msg);

        /// <summary>Places a door/window centred on the wall. <paramref name="note"/> receives non-fatal
        /// remarks (e.g. "fixed-size family: measured size not applied"); empty when there are none.</summary>
        CommandResult CreateOpeningDirect(long wallElementId, DirectOpeningSpec spec,
                                          out long openingElementId, out string note, ref string msg);

        // --- v1 survey-chain placement (kept for the length+angle contract) ---
        CommandResult SurveyWallPrep(SurveyDirection dir, out CommandContext cmdContext,
                                     out WallSurveyParams wsParams, ref string msg);
        CommandResult SurveyWall(CommandContext cmdContext, WallSurveyParams wsParams, ref string msg);
        CommandResult SurveyWallHostedObjectPrep(SurveyDirection dir, string familyCategory,
                                                 out CommandContext cmdContext,
                                                 out WallHostedObjectSurveyParams whoParams,
                                                 ref string msg);
        CommandResult SurveyWallHostedObject(CommandContext cmdContext, WallHostedObjectSurveyParams whosParams, ref string msg);

        // --- Best-effort openings + Revit-warning capture (Epic 7 / 11c) ---
        void AbortPendingTransaction();
        void BeginImportWarningCapture();
        IReadOnlyList<string> EndImportWarningCapture();
    }
}
