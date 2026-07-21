using System;
using System.Collections.Generic;
using System.Threading;

using PKNailForRevit;

namespace PKNail.AiImport.IntegrationTests
{
    /// <summary>
    /// A hand-rolled, in-process fake <see cref="IProjectModel"/> (no Moq). It records the ordered
    /// sequence of driver calls and the params/Distance values it receives, and lets a test force a
    /// failure at a chosen step to exercise rollback. <c>*Prep</c> methods return a valid
    /// <see cref="CommandContext"/> and params with a populated <see cref="SurveyFamilyDef"/> list so the
    /// importer's <c>SetWidth/SetHeight/...</c> calls don't throw.
    /// </summary>
    internal sealed class RecordingProjectModel : IProjectModel
    {
        // --- Recorded call log ------------------------------------------------------
        public readonly List<string> Calls = new List<string>();

        public readonly List<WallSurveyParams> SurveyWallCalls = new List<WallSurveyParams>();
        public readonly List<WallHostedObjectSurveyParams> SurveyHostedObjectCalls =
            new List<WallHostedObjectSurveyParams>();

        /// <summary>Records each direct absolute wall placement (v2 path) with the meters coords received.</summary>
        public sealed class AbsoluteWallCall
        {
            public double StartXMeters;
            public double StartYMeters;
            public double EndXMeters;
            public double EndYMeters;
            public Distance Thickness;
            public Distance Height;
            public int LevelIdx;
            public long ReturnedId;
        }
        public readonly List<AbsoluteWallCall> CreateWallAbsoluteCalls = new List<AbsoluteWallCall>();
        private long m_nextWallId = 1000;

        /// <summary>Records each direct opening placement (v2 path) on a host wall.</summary>
        public sealed class OpeningCall
        {
            public long WallElementId;
            public double LocationFromStartMeters;
            public Distance Width;
            public Distance Height;
            public Distance SillHeight;
            public bool IsDoor;
            public long ReturnedId;
        }
        public readonly List<OpeningCall> CreateOpeningOnWallCalls = new List<OpeningCall>();
        private long m_nextOpeningId = 5000;

        public int InitializeTransactionCount;
        public int StartTransactionCount;
        public int CommitTransactionCount;
        public int RollBackTransactionCount;
        public string TransactionName;

        public LevelsData AssignedLevelsData;
        public DocumentSettings AssignedDocumentSettings;

        // --- Forced-failure controls ------------------------------------------------
        /// <summary>When non-null, the named step returns this result instead of Succeeded.</summary>
        public string FailAtStep;
        public CommandResult FailWith = CommandResult.Failed;

        private CommandResult resultFor(string step)
        {
            Calls.Add(step);
            if (FailAtStep != null && step.StartsWith(FailAtStep, StringComparison.Ordinal))
            { return FailWith; }
            return CommandResult.Succeeded;
        }

        // --- Transaction group ------------------------------------------------------
        public CommandResult InitializeTransaction(ref string msg)
        { InitializeTransactionCount++; return resultFor("InitializeTransaction"); }

        public CommandResult SetTransactionName(string name, ref string msg)
        { TransactionName = name; return resultFor("SetTransactionName"); }

        public CommandResult StartTransaction(ref string msg)
        { StartTransactionCount++; return resultFor("StartTransaction"); }

        public CommandResult CommitTransaction(ref string msg)
        { CommitTransactionCount++; return resultFor("CommitTransaction"); }

        public CommandResult RollBackTransaction(ref string msg)
        { RollBackTransactionCount++; return resultFor("RollBackTransaction"); }

        // --- Walls ------------------------------------------------------------------
        public CommandResult SurveyWallPrep(SurveyDirection dir, out CommandContext cmdContext,
                                            out WallSurveyParams wsParams, ref string msg)
        {
            cmdContext = new CommandContext { SurveyDirection = dir };
            wsParams = new WallSurveyParams();
            return resultFor("SurveyWallPrep");
        }

        public CommandResult SurveyWall(CommandContext cmdContext, WallSurveyParams wsParams, ref string msg)
        {
            SurveyWallCalls.Add(wsParams);
            return resultFor("SurveyWall");
        }

        public CommandResult CreateWallAbsolute(double startXMeters, double startYMeters,
                                                double endXMeters, double endYMeters,
                                                Distance thickness, Distance height, int levelIdx,
                                                out long wallElementId, ref string msg)
        {
            CommandResult result = resultFor("CreateWallAbsolute");
            wallElementId = m_nextWallId++;
            CreateWallAbsoluteCalls.Add(new AbsoluteWallCall
            {
                StartXMeters = startXMeters,
                StartYMeters = startYMeters,
                EndXMeters = endXMeters,
                EndYMeters = endYMeters,
                Thickness = thickness,
                Height = height,
                LevelIdx = levelIdx,
                ReturnedId = wallElementId,
            });
            return result;
        }

        public CommandResult CreateOpeningOnWall(long wallElementId, double locationFromStartMeters,
                                                 Distance width, Distance height, Distance sillHeight,
                                                 bool isDoor, out long openingElementId, ref string msg)
        {
            CommandResult result = resultFor("CreateOpeningOnWall");
            openingElementId = m_nextOpeningId++;
            CreateOpeningOnWallCalls.Add(new OpeningCall
            {
                WallElementId = wallElementId,
                LocationFromStartMeters = locationFromStartMeters,
                Width = width,
                Height = height,
                SillHeight = sillHeight,
                IsDoor = isDoor,
                ReturnedId = openingElementId,
            });
            return result;
        }

        // --- Wall-hosted objects ----------------------------------------------------
        public CommandResult SurveyWallHostedObjectPrep(SurveyDirection dir, string familyCategory,
                                                        out CommandContext cmdContext,
                                                        out WallHostedObjectSurveyParams whoParams,
                                                        ref string msg)
        {
            cmdContext = new CommandContext { SurveyDirection = dir };
            whoParams = new WallHostedObjectSurveyParams(familyCategory);
            // Populate at least one family-def so SetWidth/SetHeight/... don't throw HardException.
            whoParams.ProjectSurveyFamilyDefs.Add(new SurveyFamilyDef { Name = "Default" });
            whoParams.SurveyFamilyDefIdx = 0;
            return resultFor("SurveyWallHostedObjectPrep");
        }

        public CommandResult SurveyWallHostedObject(CommandContext cmdContext,
                                                    WallHostedObjectSurveyParams whosParams, ref string msg)
        {
            SurveyHostedObjectCalls.Add(whosParams);
            return resultFor("SurveyWallHostedObject");
        }

        // --- Properties -------------------------------------------------------------
        private DocumentSettings m_documentSettings = new DocumentSettings(UnitSystem.Metric);
        public DocumentSettings DocumentSettings
        {
            get { return m_documentSettings; }
            set { m_documentSettings = value; AssignedDocumentSettings = value; Calls.Add("set_DocumentSettings"); }
        }

        // Seed one existing "Revit level" so the importer's merge-into-existing-levels logic has a target
        // (mirrors a real document, which always has at least one Level with an ElementId + ProjectElev).
        private LevelsData m_levelsData = CreateSeedLevels();
        public LevelsData LevelsData
        {
            get { return m_levelsData; }
            set { m_levelsData = value; AssignedLevelsData = value; Calls.Add("set_LevelsData"); }
        }

        private static LevelsData CreateSeedLevels()
        {
            var ld = new LevelsData();
            ld.Levels.Add(new LevelDataItem("Level 1", "1", Distance.Zero));
            return ld;
        }

        public UnitSystem UnitSystem { get { return UnitSystem.Metric; } }

        public AutoResetEvent TransactionErrorSignal { set { /* ignored in fake */ } }

        // --- Import warning capture (Epic 7) ----------------------------------------
        /// <summary>Warnings a test wants EndImportWarningCapture to surface (simulating Revit warnings
        /// collected during placement). Empty by default.</summary>
        public readonly List<string> WarningsToReturn = new List<string>();
        public int BeginImportWarningCaptureCount;
        public int EndImportWarningCaptureCount;
        private bool m_capturing;

        public void BeginImportWarningCapture()
        { BeginImportWarningCaptureCount++; m_capturing = true; Calls.Add("BeginImportWarningCapture"); }

        public IReadOnlyList<string> EndImportWarningCapture()
        {
            EndImportWarningCaptureCount++;
            Calls.Add("EndImportWarningCapture");
            // Mirror RevitDocument's idempotency: only the first drain after a Begin yields the warnings.
            if (!m_capturing) { return new List<string>(); }
            m_capturing = false;
            return new List<string>(WarningsToReturn);
        }

        // --- Unused IProjectModel surface (record-only / no-op) ---------------------
        public CommandResult Initialize(ref string msg) { return resultFor("Initialize"); }
        public CommandResult Finalize(ref string msg) { return resultFor("Finalize"); }
        public CommandResult EnsureDocumentParameters(ref string msg) { return resultFor("EnsureDocumentParameters"); }
        public CommandResult SetCoordinateSystemFromWall() { Calls.Add("SetCoordinateSystemFromWall"); return CommandResult.Succeeded; }

        public CommandResult ActivateWallPrep(out CommandContext cmdContext, out WallSurveyParams wsParams, ref string msg)
        { cmdContext = new CommandContext(); wsParams = new WallSurveyParams(); return resultFor("ActivateWallPrep"); }
        public CommandResult ActivateWall(CommandContext cmdContext, WallSurveyParams wsParams, ref string msg)
        { return resultFor("ActivateWall"); }

        public CommandResult SurveyLinePrep(SurveyDirection dir, bool isParallel, out CommandContext cmdContext,
                                            out LineSurveyParams lsParams, ref string msg)
        { cmdContext = new CommandContext(); lsParams = new LineSurveyParams(); return resultFor("SurveyLinePrep"); }
        public CommandResult SurveyLine(CommandContext cmdContext, LineSurveyParams lsParams, ref string msg)
        { return resultFor("SurveyLine"); }

        public CommandResult SurveyRFCommandMappedObjectPrep(string rfCmdPage, KeypadCommandPhrase rfCmdPhrase,
                                                             out CommandContext cmdContext, out SurveyParams sParams, ref string msg)
        { cmdContext = new CommandContext(); sParams = new SurveyParams(""); return resultFor("SurveyRFCommandMappedObjectPrep"); }

        public CommandResult CopyObjectsPrep(List<string> selectionIDs, out CopyObjectsData data, ref string msg)
        { data = new CopyObjectsData(new CommandContext()); return resultFor("CopyObjectsPrep"); }
        public CommandResult CopyObjects(CopyObjectsData data, ref string msg) { return resultFor("CopyObjects"); }

        public CommandResult ConvertUnknownWidthWallsPrep(List<string> selectionIDs, out ConvertUnknownWidthWallsData data, ref string msg)
        { data = new ConvertUnknownWidthWallsData(new CommandContext()); return resultFor("ConvertUnknownWidthWallsPrep"); }
        public CommandResult ConvertUnknownWidthWalls(ConvertUnknownWidthWallsData data, ref string msg)
        { return resultFor("ConvertUnknownWidthWalls"); }

        public void GetModelState(out ModelState state) { state = new FakeModelState(); }

        public CommandResult Save(out SaveError err, ref string msg) { err = SaveError.None; return resultFor("Save"); }

        public List<string> GetRangefinderKeypadMapPages() { return new List<string>(); }
        public List<string> GetBuiltinFamilyCategories() { return FamilyCategory.GetBuiltinFamilyCategories(); }
        public List<string> GetCustomFamilyCategories() { return new List<string>(); }

        private sealed class FakeModelState : ModelState
        {
            public override bool Compare(ModelState other, out string diffMsg) { diffMsg = ""; return true; }
        }
    }
}
