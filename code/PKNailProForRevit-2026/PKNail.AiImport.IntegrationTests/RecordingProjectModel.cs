using System;
using System.Collections.Generic;

using PKNailForRevit;

namespace PKNail.AiImport.IntegrationTests
{
    /// <summary>
    /// A hand-rolled, in-process fake <see cref="IAiImportTarget"/> (no Moq) — the focused import surface,
    /// NOT the full 41-member <see cref="IProjectModel"/> (Epic 11e). It records the ordered sequence of
    /// driver calls and the params/Distance values it receives, and lets a test force a failure at a chosen
    /// step to exercise rollback. <c>*Prep</c> methods return a valid <see cref="CommandContext"/> and
    /// params with a populated <see cref="SurveyFamilyDef"/> list so the importer's
    /// <c>SetWidth/SetHeight/...</c> calls don't throw.
    /// </summary>
    internal sealed class RecordingProjectModel : IAiImportTarget
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
            public Distance Thickness = null!;   // always set via the object initializer at record time
            public Distance Height = null!;
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
            public Distance Width = null!;       // always set via the object initializer at record time
            public Distance Height = null!;
            public Distance SillHeight = null!;
            public bool IsDoor;
            public long ReturnedId;
        }
        public readonly List<OpeningCall> CreateOpeningOnWallCalls = new List<OpeningCall>();
        private long m_nextOpeningId = 5000;

        public int InitializeTransactionCount;
        public int StartTransactionCount;
        public int CommitTransactionCount;
        public int RollBackTransactionCount;
        public string? TransactionName;

        public LevelsData? AssignedLevelsData;

        // --- Forced-failure controls ------------------------------------------------
        /// <summary>When non-null, the named step returns this result instead of Succeeded.</summary>
        public string? FailAtStep;
        public CommandResult FailWith = CommandResult.Failed;

        /// <summary>When non-null, the named step THROWS instead of returning — exercises the importer's
        /// outer exception handler (real engine methods can throw, e.g. a family that cannot flip facing).</summary>
        public string? ThrowAtStep;

        private CommandResult resultFor(string step)
        {
            Calls.Add(step);
            if (ThrowAtStep != null && step.StartsWith(ThrowAtStep, StringComparison.Ordinal))
            { throw new InvalidOperationException("forced throw at " + step); }
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

        // --- Wall activation / re-import guard (1.0-adaptation) ----------------------
        /// <summary>Records each headless wall activation the absolute opening path performs.</summary>
        public sealed class ActivateCall
        {
            public long WallId;
            public bool InteriorFace;
            public bool NearEndIsStart;
        }
        public readonly List<ActivateCall> ActivateWallByIdCalls = new List<ActivateCall>();
        public int AbortPendingTransactionCount;

        public CommandResult ActivateWallById(long wallId, bool interiorFace, bool nearEndIsStart, ref string msg)
        {
            ActivateWallByIdCalls.Add(new ActivateCall
            { WallId = wallId, InteriorFace = interiorFace, NearEndIsStart = nearEndIsStart });
            return resultFor("ActivateWallById");
        }

        public void AbortPendingTransaction()
        { AbortPendingTransactionCount++; Calls.Add("AbortPendingTransaction"); }

        // --- Wall-hosted objects ----------------------------------------------------
        /// <summary>Test knob: when false, SurveyWallHostedObjectPrep returns no survey family defs
        /// (simulating a stock template with no PKNAIL families), which drives the absolute path's
        /// direct-hosting fallback. Default true.</summary>
        public bool ProvideSurveyFamilyDefs = true;

        public CommandResult SurveyWallHostedObjectPrep(SurveyDirection dir, string familyCategory,
                                                        out CommandContext cmdContext,
                                                        out WallHostedObjectSurveyParams whoParams,
                                                        ref string msg)
        {
            cmdContext = new CommandContext { SurveyDirection = dir };
            whoParams = new WallHostedObjectSurveyParams(familyCategory);
            // Populate at least one family-def so SetWidth/SetHeight/... don't throw HardException — unless a
            // test is exercising the "no PKNAIL family" case.
            if (ProvideSurveyFamilyDefs)
            {
                whoParams.ProjectSurveyFamilyDefs.Add(new SurveyFamilyDef { Name = "Default" });
                whoParams.SurveyFamilyDefIdx = 0;
            }
            return resultFor("SurveyWallHostedObjectPrep");
        }

        public CommandResult SurveyWallHostedObject(CommandContext cmdContext,
                                                    WallHostedObjectSurveyParams whosParams, ref string msg)
        {
            SurveyHostedObjectCalls.Add(whosParams);
            return resultFor("SurveyWallHostedObject");
        }

        // --- Properties -------------------------------------------------------------
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
    }
}
