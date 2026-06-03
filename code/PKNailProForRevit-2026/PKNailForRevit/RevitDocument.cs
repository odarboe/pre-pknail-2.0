using System;
using System.Collections;
using System.Collections.Generic;

using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.ApplicationServices;

using G2D = PKNailForRevit.Geometry2D;

namespace PKNailForRevit
{
    public sealed class RevitDocument : IProjectModel
    {
        #region Data Members
        private Application m_revitApp;
        private PKNailAppSettings m_appSettings;
        private DocumentSettings m_docSettings;
        private Document m_doc;
        private TransactionGroup m_txnGrp;
        private Transaction m_txn;
        private bool m_isTrialTxn;
        private View3D m_view3D;
        private System.Threading.AutoResetEvent m_txnErrorSignal;
        #endregion

        public RevitDocument(Application revitApp, Document doc)
        {
            m_revitApp = revitApp;
            m_doc = doc;
            m_appSettings = new PKNailAppSettings();
            m_docSettings = null;
            m_txnGrp = null;
            m_txn = null;
            m_view3D = null;
            m_txnErrorSignal = null;
        }

        // IProjectModel interface implementation
        public CommandResult Initialize(ref string msg)
        {
            startTransaction(@"Initialize Project");
            PKNailVersionInfo vi = fetchData<PKNailVersionInfo>();
            if (vi != null)
            {
                vi.PreviousRun = vi.CurrentRun;
                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                vi.CurrentRun = assembly.GetName().Version.ToString();
                storeData<PKNailVersionInfo>(vi);
            }
            commitTransaction();
            return CommandResult.Succeeded;
        }

        // IProjectModel interface implementation
        public CommandResult Finalize(ref string msg)
        {
            return CommandResult.Succeeded;
        }

        // IProjectModel interface implementation
        public CommandResult InitializeTransaction(ref string msg)
        {
            CommandResult retval = CommandResult.Succeeded;

            if (m_txnGrp == null)
            {
                try
                { m_txnGrp = new TransactionGroup(doc); }
                catch
                {
                    msg += @"Document is busy";
                    retval = CommandResult.Failed;
                }
            }
            else
            { 
                msg += @"Attempted to start a nested transaction";
                retval = CommandResult.Failed;
            }

            return retval;
        }

        // IProjectModel interface implementation
        public CommandResult SetTransactionName(string name, ref string msg)
        {
            if (m_txnGrp != null)
            {
                m_txnGrp.SetName(name);
                return CommandResult.Succeeded;
            }
            else
            {
                msg += @"SetTransactionName() called before InitializeTransaction()";
                return CommandResult.Failed;
            }
        }

        // IProjectModel interface implementation
        public CommandResult StartTransaction(ref string msg)
        {
            CommandResult retval = CommandResult.Succeeded;

            if (m_txnGrp != null)
            {
                if (m_txnGrp.GetName() == "")
                { m_txnGrp.SetName(@"PKNail Command"); }

                if (m_txnGrp.Start() != TransactionStatus.Started)
                {
                    msg += @"Transaction did not start successfully";
                    retval = CommandResult.Failed;
                }
            }
            else
            {
                msg += @"Attempted to call StartTransaction() before InitializeTransaction()";
                retval = CommandResult.Failed;
            }

            return retval;
        }

        // IProjectModel interface implementation
        public CommandResult CommitTransaction(ref string msg)
        {
            CommandResult retval = CommandResult.Succeeded;

            if (m_txnGrp == null)
            {
                msg += @"Attempted to commit a non-existent transaction";
                retval = CommandResult.Failed;
            }

            try
            { m_txnGrp.Assimilate(); }
            catch (Exception e)
            {
                msg += @"CommitTransation: " + e.Message;
                retval = CommandResult.Failed;
            }
            finally  { m_txnGrp = null; }

            return retval;
        }

        // IProjectModel interface implementation
        public CommandResult RollBackTransaction(ref string msg)
        {
            CommandResult retval = CommandResult.Succeeded;

            try
            {
                if (m_txnGrp != null && m_txnGrp.GetStatus() == TransactionStatus.Started)
                {
                    if (m_txn != null && m_txn.GetStatus() == TransactionStatus.Started)
                    { m_txn.RollBack(); }
                    m_txnGrp.RollBack();
                }
            }
            catch (Exception e)
            {
                msg += @"CommitTransation: " + e.Message;
                retval = CommandResult.Failed;
            }
            finally  { m_txnGrp = null; }

            return retval;
        }

        // IProjectModel interface implementation
        public CommandResult SetCoordinateSystemFromWall()
        {
            CommandResult retval = CommandResult.Succeeded;

            Autodesk.Revit.UI.UIDocument uidoc = new Autodesk.Revit.UI.UIDocument(m_doc);
            Reference pickPtRef = null;
            try
            {
                pickPtRef = uidoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.PointOnElement,
                                                       new wallFaceFilter(this, false),
                                                       @"Select the wall to use as the basis for the coordinate system");
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            { pickPtRef = null; retval = CommandResult.Cancelled; }

            if (pickPtRef != null)
            {
                startTransaction(@"Set Coordinate System");
                Wall w = (Wall)doc.GetElement(pickPtRef);
                G2D.Radians wAz = RevitSurveyWall.Azimuth(w, SurveyOrientation.NotFlipped);

                // We want the angle to be between -pi/4 and +pi/4
                if (wAz < G2D.Radians.Zero) { wAz = G2D.Radians.TwoPIPlus(wAz); }
                while (wAz > G2D.Radians.PIOverFour) { wAz -= G2D.Radians.PIOverTwo; }
                documentState.SetCoordinateSystemAngle(this, wAz);
                commitTransaction();
            }

            return retval;
        }

        // IProjectModel interface implementation
        public CommandResult ActivateWallPrep(out CommandContext cmdContext, out WallSurveyParams wsParams, ref string msg)
        {
            CommandResult retval = CommandResult.Succeeded;

            // Enclose this operation in a transaction (that is aborted at the end)
            // to prevent persistent modifications to the document.
            try
            {
                startTrialTransaction(@"discardChanges");

                revitCommandContext revitCmdContext = new revitCommandContext(this);
                cmdContext = revitCmdContext;
                wsParams = new WallSurveyParams();
                wsParams.IsFirstWall = false;
                wsParams.IsCreateWall = false;
                wsParams.IsSurveyRequired = true;

                surveyFace refWallFace;
                if (revitCmdContext.UserPickWallToActivate(out refWallFace, ref msg))
                {
                    Wall w = revitCmdContext.RefElement as Wall;
                    SurveySide nearSide = revitCmdContext.SurveyDirection.NearSide;
                    SurveySide farSide = revitCmdContext.SurveyDirection.FarSide;

                    bool isUsingRefOpening = false;
                    if (!refWallFace.IsFaceSurveyed)
                    {
                        // See if there is at least one opening that can be used as a reference in
                        // setting the corners of the wall
                        List<FamilyInstance> openings = refWallFace.GetOpenings();
                        if (openings.Count > 0)
                        {
                            FamilyInstance refOpening = null;
                            List<long> openingIdVals = new List<long>();
                            foreach (FamilyInstance inst in openings)
                            { openingIdVals.Add(inst.Id.Value); }

                            if (revitCmdContext.UserPickRefOpening(openingIdVals, out refOpening, ref msg))
                            {
                                isUsingRefOpening = true;

                                // Adjust the survey direction if necessary, using the opening as the reference for left vs right
                                G2D.Point leftCorner = refWallFace.Corner(SurveySide.Left);
                                G2D.Vector wEdge = new G2D.Vector(leftCorner, refWallFace.Corner(SurveySide.Right));
                                G2D.Point refOpeningLocPt2D = Utilities.To2D(((LocationPoint)refOpening.Location).Point);
                                G2D.Point refOpeningLocPtOnWEdge = wEdge.Project(refOpeningLocPt2D);
                                if (leftCorner.DistanceTo(refOpeningLocPtOnWEdge) > leftCorner.DistanceTo(revitCmdContext.ReferencePoint))
                                { revitCmdContext.SurveyDirection = SurveyDirection.LeftToRight; }
                                else
                                { revitCmdContext.SurveyDirection = SurveyDirection.RightToLeft; }

                                // Set the parameters
                                ReferenceOpeningParams rop = new ReferenceOpeningParams();
                                rop.ReferenceOpeningID = refOpening.Id.ToString();
                                nearSide = revitCmdContext.SurveyDirection.NearSide;
                                farSide = nearSide.Other;
                                rop.WallExtent = refWallFace.DefaultActivateWallExtent();
                                rop.CornerToOpening = refWallFace.DefaultActivateWallCornerToRefOpening(nearSide, refOpening);
                                wsParams.ReferenceOpeningParams = rop;
                            }
                        }
                    }

                    if (!isUsingRefOpening)
                    {
                        if (refWallFace.IsValidReferenceCorner(nearSide))
                        {
                            if (refWallFace.IsFaceSurveyed)
                            { wsParams.Length = refWallFace.WallExtent; }
                            else
                            { wsParams.Length = refWallFace.DefaultSurveyLength(nearSide); }
                        }
                        else
                        {
                            msg += @"You may not activate a wall corner that contains the un-surveyed face of ";
                            msg += @"an unknown-width wall";
                            retval = CommandResult.Failed;
                        }
                    }

                    if (retval == CommandResult.Succeeded)
                    {
                        surveyWallWorker_populateVisibleHeightList(revitCmdContext.CurrentLevel, wsParams);
                        surveyWallWorker_setWidth(refWallFace, farSide, null, wsParams);
                        surveyWallWorker_setZAxisParams(w, revitCmdContext, wsParams);
                        wsParams.LevelOffset = getWallState(w).LevelOffset(revitCmdContext.SurveyOrientation);
                        if (wsParams.LevelOffset.IsZero)  { wsParams.LevelOffset = Distance.NoValue; }
                    }
                }
                else { retval = CommandResult.Cancelled; }
            }
            finally
            { if (retval != CommandResult.TransactionError) { rollBackTransaction(); } }

            return retval;
        }

        // IProjectModel interface implementation
        public CommandResult ActivateWall(CommandContext cmdContext, WallSurveyParams wsParams, ref string msg)
        {
            revitCommandContext revitCmdContext = (revitCommandContext)cmdContext;
            Level lvl = revitCmdContext.CurrentLevel;
            //WallSurveyParams wsp = (WallSurveyParams)data.SurveyParams;

            startTransaction(@"Activate Wall");  // Must be human readable, see case 225

            if (revitCmdContext.IsRefElementWall)
            {
                Wall refW = revitCmdContext.RefElement as Wall;
                Dictionary<long, long> restoredInstanceMap = new Dictionary<long, long>();
                surveyFace f;
                surveyFace.CreateFaceModifyWalls(this, lvl, ref refW, revitCmdContext.SurveyOrientation, revitCmdContext.ReferencePoint, out f, restoredInstanceMap);
                if (wsParams.IsSurveyRequired)
                {

                    if (!surveyExistingFace(lvl, f, wsParams, wsParams.ReferenceOpeningParams, revitCmdContext.SurveyDirection, out f, restoredInstanceMap))
                    { return CommandResult.Failed; }
                }

                surveyWallWorker_setZoomElements(revitCmdContext.ZoomElements, f);
                surveySprite.Place(this, f.Edge, lvl, revitCmdContext.SurveyDirection);
                revitCmdContext.SaveRefWallFaceToModel(f, revitCmdContext.SurveyDirection);
            }
            else
            {
                DetailLine dLine = revitCmdContext.RefElement as DetailLine;
                surveyLineWorker_setZoomElements(revitCmdContext.ZoomElements, dLine, lvl);
                surveySprite.Place(this, Utilities.To2D(dLine.Location), lvl, revitCmdContext.SurveyDirection);
                revitCmdContext.SaveDetailLineToModel(dLine, revitCmdContext.SurveyDirection, revitCmdContext.SurveyOrientation);
            }

            return (commitTransaction() ? CommandResult.Succeeded : CommandResult.TransactionError);
        }

        // IProjectModel interface implementation
        public CommandResult SurveyWallPrep(SurveyDirection dir, out CommandContext cmdContext,
                                            out WallSurveyParams wsParams, ref string msg)
        {
            CommandResult retval = CommandResult.Succeeded;

            // Enclose this operation in a transaction (that is aborted at the end)
            // to prevent persistent modifications to the document.
            // We want all modifications to happen in SurveyWall()
            try
            {
                startTrialTransaction(@"discardChanges");

                revitCommandContext revitCmdContext = new revitCommandContext(this);
                cmdContext = revitCmdContext;
                wsParams = new WallSurveyParams();

                Level baseLvl = revitCmdContext.CurrentLevel;

                // Populate the array of available wall widths
                surveyWallWorker_populateWallWidthList(wsParams);

                // Populate the array of project visible heights
                surveyWallWorker_populateVisibleHeightList(baseLvl, wsParams);

                // See if there are no walls in the model or on the current level
                wsParams.IsFirstWall = false;
                FilteredElementCollector wallColl = new FilteredElementCollector(m_doc).OfClass(typeof(Wall));
                int numWalls = wallColl.ToElements().Count;
                if (numWalls == 0)
                {
                    wsParams.IsFirstWall = true;
                    wsParams.IsCreateWall = true;
                    wsParams.IsSurveyRequired = true;
                    wsParams.ConnectedLevelIdx = -1;
                    wsParams.Height = Distance.NoValue;
                    revitCmdContext.SurveyDirection = (dir != SurveyDirection.FromModel) ? dir : SurveyDirection.LeftToRight;
                    revitCmdContext.SurveyOrientation = SurveyOrientation.NotFlipped;
                }
                else
                {
                    ElementLevelFilter lvlFilter = new ElementLevelFilter(baseLvl.Id);
                    numWalls = wallColl.WherePasses(lvlFilter).ToElements().Count;
                    if (numWalls > 0)
                    {

                        wsParams.IsFirstWall = false;

                        Wall refWall;
                        surveyFace refWallFace;
                        if (revitCmdContext.GetActiveElementFromModel(dir, out refWallFace, ref msg))
                        {
                            if (revitCmdContext.IsRefElementWall)
                            {
                                SurveySide farSide = revitCmdContext.SurveyDirection.FarSide;
                                SurveySide nearSide = farSide.Other;
                                refWall = refWallFace.EndWall(farSide);
                                SurveyOrientation refWallSO = refWallFace.EndWallSO(farSide);
                                
                                // The presence of an outside "continuation wall" at the far corner of the reference wall
                                // affects the default values for corner angle and wall width
                                Wall continuationW = null;
                                SurveyOrientation continuationWSO = SurveyOrientation.NotFlipped;

                                // If there is already a wall joined to this end of the wall, survey it
                                Wall adjacentW; SurveyOrientation adjacentWSO;
                                refWallFace.GetAdjacentCornerWall(farSide, out adjacentW, out adjacentWSO);
                                if (adjacentW != null)
                                {
                                    wsParams.IsCreateWall = false;

                                    surveyFace adjacentFace;
                                    surveyFace.CreateFaceNoModifyWalls(this, baseLvl, adjacentW, adjacentWSO, refWallFace.Corner(farSide), out adjacentFace);
                                    wsParams.IsSurveyRequired = !adjacentFace.IsFaceSurveyed;
                                    wsParams.Length = adjacentFace.DefaultSurveyLength(nearSide);
                                    Wall surveyWall = adjacentFace.EndWall(farSide);
                                    SurveyOrientation surveyWallSO = adjacentFace.EndWallSO(farSide);

                                    // Set the corner angle to the angle between the reference and survey walls
                                    G2D.Radians surveyWallAz = RevitSurveyWall.Azimuth(surveyWall, surveyWallSO);
                                    G2D.Radians adjRefWallAz = (farSide == SurveySide.Right) ? refWallFace.Azimuth + G2D.Radians.PI : refWallFace.Azimuth;
                                    G2D.Radians cornerAngle = surveyWallAz - adjRefWallAz;
                                    if (cornerAngle < G2D.Radians.Zero) { cornerAngle = G2D.Radians.TwoPIPlus(cornerAngle); }
                                    wsParams.CornerAngleList.Add(cornerAngle);

                                    // Set the default width
                                    surveyWallWorker_setWidth(adjacentFace, farSide, continuationW, wsParams);

                                    // Set the connected level and height
                                    surveyWallWorker_setZAxisParams(surveyWall, revitCmdContext, wsParams);
                                    // Take the default level offset from the reference wall
                                    wsParams.LevelOffset = getWallState(refWall).LevelOffset(refWallSO);
                                    if (wsParams.LevelOffset.IsZero)  { wsParams.LevelOffset = Distance.NoValue; }

                                }
                                else
                                {
                                    wsParams.IsCreateWall = true;
                                    wsParams.IsSurveyRequired = true;
                                    wsParams.ReferenceFaceLength = refWallFace.WallExtent;

                                    // Determine whether a space can be closed
                                    if (!surveyWallWorker_calculateCloseWallGeometry(baseLvl, refWallFace, wsParams, farSide, revitCmdContext.SurveyDirection))
                                    { retval = CommandResult.TransactionError; }
                                    else
                                    {
                                        surveyWallWorker_findOutsideContinuationWall(refWall, refWallSO, refWallFace.Corner(farSide),
                                                                                     farSide, baseLvl,
                                                                                     out continuationW, out continuationWSO);

                                        // Get the list of default corner angles
                                        surveyWallWorker_populateDefaultCornerAngleList(refWall, refWallSO, farSide, refWallFace.Azimuth,
                                                                                        continuationW, continuationWSO, false,
                                                                                        wsParams.CornerAngleList);

                                        // Set the default width
                                        surveyWallWorker_setWidth(refWallFace, farSide, continuationW, wsParams);

                                        // Set the connected level, height, and level offset
                                        surveyWallWorker_setZAxisParams(refWall, revitCmdContext, wsParams);
                                        wsParams.LevelOffset = getWallState(refWall).LevelOffset(refWallSO);
                                        if (wsParams.LevelOffset.IsZero)  { wsParams.LevelOffset = Distance.NoValue; }

                                        List<long> excludeWallIDs = new List<long>();
                                        foreach (long wID in refWallFace.FaceWallIDs)  { excludeWallIDs.Add(wID); }
                                        if (refWallFace.ParallelWallId(farSide) != null)
                                        { excludeWallIDs.Add(refWallFace.ParallelWallId(farSide).Value); }
                                        surveyWallWorker_populateMaxLengthList(refWallFace.Corner(farSide), refWallFace.Azimuth, farSide, baseLvl,
                                                                               excludeWallIDs, wsParams.CornerAngleList, wsParams.MaxLengthList);
                                    }
                                }
                            }
                            else   // !IsRefElementWall
                            {
                                msg += @"Please activate a wall on this level to continue surveying walls";
                                retval = CommandResult.Failed;
                            }
                        }
                        else
                        { retval = CommandResult.Failed; }
                    }
                    else
                    {
                        msg += @"There are no walls on this level. Please copy walls from other levels to this level to continue.";
                        retval = CommandResult.Failed;
                    }
                }
            }

            finally
            {
                if (retval != CommandResult.TransactionError) { rollBackTransaction(); }
                if (retval == CommandResult.TransactionError || retval == CommandResult.Failed) { m_txnErrorSignal.Set(); }
            }

            return retval;
        }

        // IProjectModel interface implementation
        public CommandResult SurveyWall(CommandContext cmdContext, WallSurveyParams wsParams, ref string msg)
        {
            revitCommandContext revitCmdContext = (revitCommandContext)cmdContext;

            Level baseLvl = revitCmdContext.CurrentLevel;
            SurveySide nearSide = revitCmdContext.SurveyDirection.NearSide;
            SurveySide farSide = revitCmdContext.SurveyDirection.FarSide;

            surveyFace surveyF = null;
            Dictionary<long, long> ignoreRestoredInstanceMap = new Dictionary<long, long>();

            if (!wsParams.IsCreateWall)
            {
                startTransaction(@"SurveyWall_surveyExistingFace");

                surveyFace refF = revitCmdContext.GetWallFace();
                Wall adjacentW; SurveyOrientation adjacentWSO;
                refF.GetAdjacentCornerWall(farSide, out adjacentW, out adjacentWSO);
                surveyFace.CreateFaceModifyWalls(this, baseLvl, ref adjacentW, adjacentWSO, refF.Corner(farSide), out surveyF, ignoreRestoredInstanceMap);
                if (wsParams.IsSurveyRequired)
                {
                    if (!surveyExistingFace(baseLvl, surveyF, wsParams, wsParams.ReferenceOpeningParams, revitCmdContext.SurveyDirection, out surveyF, ignoreRestoredInstanceMap))
                    { return CommandResult.Failed; }
                }

                if (!commitTransaction())  { return CommandResult.TransactionError; }
            }
            else
            {
                Wall surveyWall;  // This is the wall that will be created

                // Get the parameters that will control the Z-axis parameters of the wall
                Level connectedLvl;
                Distance wallHeight;
                surveyWallWorker_getZAxisData(baseLvl, revitCmdContext.CurrentLevelIdx, wsParams, out connectedLvl, out wallHeight);

                WallState newWallState = new WallState();
                SurveyOrientation newWallSO;
                wallDefinition surveyWallDef;

                if (wsParams.IsFirstWall)  // First wall in the project
                {
                    // TODO: set the azimuth from the surveyor compass direction
                    startTransaction(@"SurveyWall_createFirstWall");
                    WallType wt = surveyWallWorker_getWallType(wsParams, null);
                    G2D.Radians firstWallAz;
                    switch (wsParams.SurveyorCompassDirection)
                    {
                        case CompassDirection.East:
                            firstWallAz = G2D.Radians.ThreePIOverTwo;
                            break;
                        case CompassDirection.South:
                            firstWallAz = G2D.Radians.PI;
                            break;
                        case CompassDirection.West:
                            firstWallAz = G2D.Radians.PIOverTwo;
                            break;
                        case CompassDirection.North:
                        case CompassDirection.Undefined:
                        default:
                            firstWallAz = G2D.Radians.Zero;
                            break;
                    }
                    if (revitCmdContext.SurveyOrientation == SurveyOrientation.Flipped) { firstWallAz += G2D.Radians.PI; }
                    newWallSO = revitCmdContext.SurveyOrientation;

                    G2D.Vector firstWallLocV = new G2D.Vector(new G2D.Point(), firstWallAz, wsParams.Length.ValueRevitInternal);
                    Line firstWallCreateLine = Utilities.ToRevitLine(firstWallLocV);
                    RevitSurveyWall.LocationLineType locLineType = (newWallSO == SurveyOrientation.NotFlipped) ?
                        RevitSurveyWall.LocationLineType.Interior : RevitSurveyWall.LocationLineType.Exterior;
                    newWallState.SetIsFaceSurveyed(newWallSO);
                    newWallState.SetLevelOffset(newWallSO, wsParams.LevelOffset);
                    surveyWallDef = new wallDefinition(firstWallCreateLine, wt, locLineType, baseLvl.Id,
                                                       Utilities.IdFromElement(connectedLvl), wallHeight, Distance.Zero, newWallState);
                    surveyWallDef.CreateNewWall(this, out surveyWall, ignoreRestoredInstanceMap);
                    if (!commitTransaction()) { return CommandResult.TransactionError; }

                    if (!surveyFace.CreateFaceModifyWalls(this, baseLvl, ref surveyWall, newWallSO, nearSide, out surveyF, ignoreRestoredInstanceMap))
                    { return CommandResult.Failed; }
                }

                else   // !wp.IsFirstWall  (new wall to be created at far end of ref wall)
                {
                    surveyFace refWallFace = revitCmdContext.GetWallFace();
                    Wall refWall = refWallFace.EndWall(farSide);
                    ElementId refWallId = refWall.Id;
                    SurveyOrientation refWallSO = refWallFace.EndWallSO(farSide);

                    startTransaction(@"SurveyWall_getWallType");
                    WallType wt = surveyWallWorker_getWallType(wsParams, refWall);
                    if (!commitTransaction()) { return CommandResult.TransactionError; }

                    G2D.Point nearCorner = refWallFace.Corner(farSide);

                    G2D.Radians newWallAz;
                    G2D.Radians newWallNormalAz;
                    surveyWallWorker_calcNewElementAzimuth(refWall, refWallSO, farSide, wsParams.CornerAngle, out newWallAz, out newWallNormalAz);

                    // Set the location of the new wall
                    G2D.Point startPt = (new G2D.Vector(nearCorner, newWallNormalAz, wt.Width / 2.0)).End;
                    G2D.Vector newWallLocV = new G2D.Vector(startPt, newWallAz, wsParams.Length.ValueRevitInternal);

                    // See if the far endpoint is within measurement tolerance of another wall; if so, we want to be
                    // joined with that wall, so extend the far endpoint
                    Wall proximateWall;
                    SurveyOrientation proximateWallSO;
                    G2D.Vector proximateWallEdgeV;
                    surveyWallWorker_findProximateWall(newWallLocV, nearCorner, farSide, baseLvl,
                                                       out proximateWall, out proximateWallSO, out proximateWallEdgeV);
                    if (proximateWall != null)
                    {
                        G2D.Point adjFarCorner = G2D.Vector.Intersection(new G2D.Vector(nearCorner, newWallLocV.Azimuth, 1.0), proximateWallEdgeV);

                        surveyWallWorker_adjustNewWallEndPtsV(ref newWallLocV, nearCorner, adjFarCorner);

                        startTransaction(@"SurveyWall_adjustProximateWall");
                        surveyWallWorker_adjustProximateWall(proximateWall, proximateWallSO, proximateWallEdgeV, adjFarCorner);
                        if (!commitTransaction()) { return CommandResult.TransactionError; }
                    }

                    // Set the survey orientation of the new wall
                    surveyWallWorker_setNewWallOrientation(refWall, refWallSO, farSide, baseLvl, wt, newWallAz, nearCorner,
                                                           ref newWallLocV, out newWallSO);

                    // See if there is a "degenerate" wall at the near corner; if so, it will be deleted later on
                    ElementId degenerateWallId;
                    bool isDegenerateWallMatchRefWallSO;
                    G2D.Point newWallRearEdgeCorner = (new G2D.Vector(nearCorner, newWallNormalAz, wt.Width)).End;
                    G2D.Vector newWallRearEdgeV = new G2D.Vector(newWallRearEdgeCorner, newWallLocV.Azimuth + G2D.Radians.PI, 1.0);
                    surveyWallWorker_findDegenerateWall(baseLvl, refWall, refWallSO, farSide, newWallRearEdgeV,
                                                        out degenerateWallId, out isDegenerateWallMatchRefWallSO);

                    Line surveyWallLocLine = Utilities.ToRevitLine(newWallLocV);
                    RevitSurveyWall.LocationLineType locLineType = (newWallSO == SurveyOrientation.NotFlipped) ?
                        RevitSurveyWall.LocationLineType.Interior : RevitSurveyWall.LocationLineType.Exterior;
                    newWallState.SetIsFaceSurveyed(newWallSO);
                    newWallState.SetLevelOffset(newWallSO, wsParams.LevelOffset);

                    surveyWallDef = new wallDefinition(surveyWallLocLine, wt, locLineType, baseLvl.Id, Utilities.IdFromElement(connectedLvl),
                                                       wallHeight, Distance.Zero, newWallState);

                    // Envelop the transaction used to create the wall in another transaction,
                    // which can be aborted if we need to clean up the wall joins at the opposite end
                    // of the new survey wall.  The inner transaction is needed because the joins are
                    // not created until the transaction that creates the wall is committed.
                    TransactionGroup trialCreateWallTxnGrp = new TransactionGroup(m_doc, "trialCreateWall");
                    trialCreateWallTxnGrp.Start();

                    // Create the wall
                    startTrialTransaction(@"SurveyWall_findJoins");
                    surveyWallDef.CreateNewWall(this, out surveyWall, ignoreRestoredInstanceMap);
                    if (!commitTransaction()) { return CommandResult.TransactionError; }

                    // Begin the process of finding out how to modify the walls that are joined to the new survey wall
                    // might need to be modified to preserve the existing geometry, which might be compromised by the
                    // addition of the survey wall
                    preserveGeometryInfo geometryToPreserve = new preserveGeometryInfo(this, baseLvl);
                    geometryToPreserve.FindWallsJoinedToSurveyWall(surveyWall);

                    // Abort the outer transaction that enveloped the transaction that created the wall.  This will
                    // return the model's state to what it was before the wall was created.
                    trialCreateWallTxnGrp.RollBack();

                    // Split any walls that the new wall joins in the middle
                    startTransaction(@"SurveyWall_splitJoinedMiddleWalls");
                    if (!geometryToPreserve.SplitJoinedWalls(newWallLocV, wt.Width, refWall, connectedLvl, ignoreRestoredInstanceMap))
                    { return CommandResult.Failed; }
                    if (!commitTransaction()) { return CommandResult.TransactionError; }

                    // Record the corners of walls that the new wall joins at the ends
                    // If these corners move after re-creating the new wall, we will add offsets
                    // to bring the corners back to their original value
                    startTrialTransaction(@"SurveyWall_findCornersToPreserve");
                    geometryToPreserve.FindCornersToPreserve(refWall, degenerateWallId, isDegenerateWallMatchRefWallSO);
                    geometryToPreserve.FindWallsToMove();
                    rollBackTransaction();

                    // If the survey wall is of unknown width, we have to determine if this is the second
                    // face of a wall with a parallel unknown width wall already surveyed on the other side.
                    // If this is the case, we should "fill in the gap" between the ends of the unknown-width
                    // walls with a continuation of the wall on the other side of the pair of unknown-width walls.
                    // We may also need to adjust the corner of the unknown-width survey wall so that the
                    // continuation of the end wall is straight
                    wallDefinition gapWallDef = null;

                    if (wsParams.IsUnknownWidth)
                    {
                        // Start an outer transaction so we can roll back the survey wall in case we need to
                        // change the corner
                        TransactionGroup findGapWallTxnGrp = new TransactionGroup(m_doc, @"findGapWall");
                        findGapWallTxnGrp.Start();

                        startTrialTransaction(@"SurveyWall_findGapWall_trialCreateWall");
                        // Re-create the survey wall
                        surveyWallDef.CreateNewWall(this, out surveyWall, ignoreRestoredInstanceMap);
                        if (!commitTransaction()) { return CommandResult.TransactionError; }

                        startTrialTransaction(@"SurveyWall_findGapWall_calcGeometry");
                        gapWallDef = surveyWallWorker_calcGapWallDef(baseLvl, connectedLvl, surveyWall, newWallState, wsParams,
                                                                     ref surveyWallLocLine, newWallLocV, newWallSO, nearSide);
                        rollBackTransaction();

                        // Roll back the creation of the survey wall
                        findGapWallTxnGrp.RollBack();
                    }

                    // After all preparatory work is finished, if there is a degenerate wall, get it out
                    // of the way to avoid Revit errors
                    if (degenerateWallId != null)
                    {
                        startTransaction(@"SurveyWall_deleteDegenerateWall");
                        deleteWall(degenerateWallId);
                        if (!commitTransaction()) { return CommandResult.TransactionError; }
                    }

                    failuresPreprocessor surveyWallFinalCreateFP = new failuresPreprocessor(this, m_txnErrorSignal);
                    startTransaction(@"SurveyWall_finalCreate", surveyWallFinalCreateFP);

                    if (gapWallDef != null)
                    { Wall gapW; gapWallDef.CreateNewWall(this, out gapW, ignoreRestoredInstanceMap); }

                    // If any walls that will join to the survey wall need to be moved in order to preserve
                    // existing geometry, do it now
                    geometryToPreserve.MoveWalls();

                    // Re-create the survey wall
                    surveyWallDef.CreateNewWall(this, out surveyWall, ignoreRestoredInstanceMap);

                    if (!commitTransaction()) { return CommandResult.TransactionError; }  // "SurveyWall_finalCreate"

                    startTransaction(@"SurveyWall_postCreate");

                    // Set offsets for the surveyed sides of any joined walls if the corner
                    // has moved as a result of joining to the new survey wall
                    geometryToPreserve.ApplyOffsets();

                    // If there was an offset defined for the join corner of the reference wall,
                    // delete it, as it has been consumed by the join to the new survey wall
                    if (!surveyWallFinalCreateFP.DeletedWalls.Contains(refWallId))
                    {
                        surveyWalWorker_consumeOffset(baseLvl, refWall, refWallSO, farSide);
                    }

                    if (!surveyFace.CreateFaceModifyWalls(this, baseLvl, ref surveyWall, newWallSO, nearSide, out surveyF, ignoreRestoredInstanceMap))
                    { return CommandResult.Failed; }

                    // See if the opposite endpoint has moved because of joining to another wall;
                    // if so, set the offset for this side to compensate
                    surveyWallWorker_correctFarCorner(surveyF, farSide, wsParams);

                    // Reset the coordinate system if set
                    surveyWallWorker_resetCoordinateSystem();

                    if (!commitTransaction()) { return CommandResult.TransactionError; }   // "SurveyWall_postCreate"
                }

                // Store the width of the newly-added wall for use as the default value next time
                if (!RevitSurveyWall.IsUnknownWidth(surveyWall))
                {
                    startTransaction(@"SurveyWall_storeDefaultWallWidth");
                    documentState.SetLastAddedWallWidth(this, RevitSurveyWall.Width(surveyWall));
                    commitTransaction();
                }
            }

            startTransaction(@"Survey Wall");  // Must be human readable; see case 225

            // Set the elements to be included in the view after re-zooming
            surveyWallWorker_setZoomElements(revitCmdContext.ZoomElements, surveyF);

            // Place the sprite
            surveySprite.Place(this, surveyF.Edge, surveyF.Level, revitCmdContext.SurveyDirection);

            // Save the context
            revitCmdContext.SaveRefWallFaceToModel(surveyF, revitCmdContext.SurveyDirection);

            return (commitTransaction() ? CommandResult.Succeeded : CommandResult.TransactionError);
        }

        // IProjectModel interface implementation
        public CommandResult SurveyLinePrep(SurveyDirection dir, bool isParallel, out CommandContext cmdContext,
                                            out LineSurveyParams lsParams, ref string msg)
        {
            CommandResult retval = CommandResult.Succeeded;

            // Enclose this operation in a transaction (that is aborted at the end)
            // to prevent persistent modifications to the document.
            // We want all modifications to happen in SurveyLine()
            try
            {
                startTrialTransaction(@"discardChanges");

                revitCommandContext revitCmdContext = new revitCommandContext(this);
                cmdContext = revitCmdContext;
                lsParams = new LineSurveyParams();

                surveyFace refWallFace = null;
                if (!revitCmdContext.GetActiveElementFromModel(dir, out refWallFace, ref msg))
                { return CommandResult.Cancelled; }

                G2D.Point startPt = null;
                G2D.Radians faceAz = null;
                if (revitCmdContext.IsRefElementWall)
                {
                    lsParams.IsReferenceFaceWall = true;
                    lsParams.IsSurveyRequired = true;
                    faceAz = refWallFace.Azimuth;
                    lsParams.ReferenceFaceLength = refWallFace.WallExtent;
                }
                else
                {
                    lsParams.IsReferenceFaceWall = false;

                    // Find out if there is another line or wall face joined at the end of the active line; if so, no survey is required,
                    // we simply want to hop over to it
                    ElementId farEndLineID;
                    surveyFace farEndFace;
                    surveyLineWorker_findElementAtFarEndOfRefLine(revitCmdContext, out farEndLineID, out farEndFace);

                    if (farEndLineID != null)
                    { lsParams.IsSurveyRequired = false; }
                    else if (farEndFace != null)
                    {
                        lsParams.IsSurveyRequired = false;
                        if (!farEndFace.IsFaceSurveyed)
                        {
                            m_txnErrorSignal.Set();
                            retval = CommandResult.Cancelled;
                        }
                    }
                    else
                    {
                        lsParams.IsSurveyRequired = true;
                        G2D.Vector refElLocV = Utilities.To2D(revitCmdContext.RefElement.Location);
                        if (revitCmdContext.SurveyOrientation == SurveyOrientation.Flipped) { refElLocV = refElLocV.Reverse(); }
                        faceAz = refElLocV.Azimuth;
                        lsParams.ReferenceFaceLength = new Distance(refElLocV.Length, Distance.Units.RevitInternal);
                        startPt = (revitCmdContext.SurveyDirection == SurveyDirection.LeftToRight) ? refElLocV.End : refElLocV.Start;
                    }
                }

                if (lsParams.IsSurveyRequired)
                {
                    // Reconcile the available line styles with the line style last used
                    // The only way to do this AFAIK is to create a dummy detail line and get the list of available line styles from it
                    // The dummy detail line will be rolled back with the trial transaction
                    lsParams.LineStyles.Clear();
                    DetailCurve dummyLine = doc.Create.NewDetailCurve(doc.ActiveView, Line.CreateBound(new XYZ(0, 0, 0), new XYZ(1, 0, 0)));
                    foreach (ElementId elID in dummyLine.GetLineStyleIds())
                    {
                        string lineStyleName = doc.GetElement(elID).Name;
                        if (lineStyleName.IndexOf(@"PKNail") != -1) { lsParams.LineStyles.Add(lineStyleName); }
                    }
                    if (lsParams.LineStyles.Count > 0)
                    {
                        lsParams.LineStyles.Sort();
                        string lastLineStyle = documentState.GetLastLineStyle(this);
                        if (lastLineStyle != @"" && lsParams.LineStyles.Contains(lastLineStyle))
                        { lsParams.LineStyleIdx = lsParams.LineStyles.IndexOf(lastLineStyle); }
                        else
                        { lsParams.LineStyleIdx = 0; }

                        if (isParallel)
                        {
                            lsParams.IsParallel = true;
                        }
                        else
                        {
                            lsParams.IsParallel = false;
                            // Get the list of default corner angles
                            SurveySide farSide = revitCmdContext.SurveyDirection.FarSide;
                            bool isInsideAngleOnly = revitCmdContext.IsRefElementWall;
                            surveyWallWorker_populateDefaultCornerAngleList(null, SurveyOrientation.NotFlipped, farSide, faceAz, null,
                                                                            SurveyOrientation.NotFlipped, isInsideAngleOnly, lsParams.CornerAngleList);
                            surveyWallWorker_populateMaxLengthList(startPt, faceAz, farSide, revitCmdContext.CurrentLevel, new List<long>(),
                                                                   lsParams.CornerAngleList, lsParams.MaxLengthList);
                        }
                    }
                    else
                    {
                        msg += @"Please define at least one PKNail line style before continuing";
                        retval = CommandResult.Failed;
                    }
                }
            }
            finally
            {
                if (retval != CommandResult.TransactionError) { rollBackTransaction(); }
                if (retval == CommandResult.TransactionError || retval == CommandResult.Failed) { m_txnErrorSignal.Set(); }
            }

            return retval;
        }

        // IProjectModel interface implementation
        public CommandResult SurveyLine(CommandContext cmdContext, LineSurveyParams lsParams, ref string msg)
        {
            revitCommandContext revitCmdContext = (revitCommandContext)cmdContext;

            Level baseLvl = revitCmdContext.CurrentLevel;
            SurveySide nearSide = revitCmdContext.SurveyDirection.NearSide;
            SurveySide farSide = revitCmdContext.SurveyDirection.FarSide;

            // Calc the geometry of the line to be created
            G2D.Point startPt = null;
            G2D.Radians lineAz = null;
            surveyFace farEndIntersectingWallF = null;
            DetailCurve surveyLine = null;
            G2D.Vector surveyLineLocV = null;

            if (revitCmdContext.IsRefElementWall)
            {
                surveyFace f = revitCmdContext.GetWallFace();
                Wall refWall = f.EndWall(farSide);
                ElementId refWallId = refWall.Id;
                SurveyOrientation refWallSO = f.EndWallSO(farSide);

                if (!lsParams.IsParallel)
                {
                    Wall ignoreWall;
                    SurveyOrientation ignoreWallSO;
                    G2D.Point ignoreLoc;
                    Dictionary<long, long> ignoreRestoredInstanceMap = new Dictionary<long, long>();
                    f.LocateInWall(lsParams.Location, nearSide, Distance.NoValue, false, false, out ignoreWall, out ignoreWallSO, out startPt, out ignoreLoc, ignoreRestoredInstanceMap);

                    G2D.Radians dummyAz;
                    surveyWallWorker_calcNewElementAzimuth(refWall, refWallSO, farSide, lsParams.CornerAngle, out lineAz, out dummyAz);
                }
                else
                {
                    // TODO: implement this
                }
            }
            else
            {
                if (lsParams.IsSurveyRequired)
                {
                    G2D.Vector refElementLocV = Utilities.To2D(revitCmdContext.RefElement.Location);
                    if (revitCmdContext.SurveyOrientation == SurveyOrientation.Flipped) { refElementLocV = refElementLocV.Reverse(); }
                    startPt = (revitCmdContext.SurveyDirection == SurveyDirection.LeftToRight) ? refElementLocV.End : refElementLocV.Start;

                    G2D.Radians dummyAz;
                    surveyWallWorker_calcNewElementAzimuth(refElementLocV.Azimuth, farSide, lsParams.CornerAngle, out lineAz, out dummyAz);
                }
                else
                {
                    // Find the next element (line or intersecting wall face) to navigate to
                    ElementId farEndLineID;
                    surveyLineWorker_findElementAtFarEndOfRefLine(revitCmdContext, out farEndLineID, out farEndIntersectingWallF);
                    if (farEndLineID != null)
                    {
                        surveyLine = (DetailCurve)doc.GetElement(farEndLineID);
                        surveyLineLocV = Utilities.To2D(surveyLine.Location);
                    }
                    else if (!farEndIntersectingWallF.IsFaceSurveyed)  // This should be caught in SurveyLinePrep()
                    { return CommandResult.Failed; }
                }
            }

            startTransaction(@"Survey Line");  // Must be human readable; see case 225

            if (lsParams.IsSurveyRequired)
            {
                surveyLineLocV = new G2D.Vector(startPt, lineAz, lsParams.Length.ValueRevitInternal);

                // Orient the vector
                int farEndPtIdx = 1;
                if (revitCmdContext.SurveyOrientation == SurveyOrientation.NotFlipped && revitCmdContext.SurveyDirection == SurveyDirection.RightToLeft
                    || revitCmdContext.SurveyOrientation == SurveyOrientation.Flipped && revitCmdContext.SurveyDirection == SurveyDirection.LeftToRight)
                {
                    surveyLineLocV = surveyLineLocV.Reverse();
                    farEndPtIdx = 0;
                }
                int nearEndPtIdx = (farEndPtIdx == 0) ? 1 : 0;

                surveyLine = doc.Create.NewDetailCurve(doc.ActiveView, Utilities.ToRevitLine(surveyLineLocV));

                // Set the line style, then save it as the "last line style"
                string lastLineStyleName = @"";
                foreach (ElementId lineStyleElID in surveyLine.GetLineStyleIds())
                {
                    Element lineStyleEl = doc.GetElement(lineStyleElID);
                    if (doc.GetElement(lineStyleElID).Name == lsParams.LineStyles[lsParams.LineStyleIdx])
                    { surveyLine.LineStyle = lineStyleEl; lastLineStyleName = lineStyleEl.Name; break; }
                }
                if (lastLineStyleName != @"") { documentState.SetLastLineStyle(this, lastLineStyleName); }
            }


            if (surveyLine != null)
            {
                surveyLineWorker_setZoomElements(revitCmdContext.ZoomElements, surveyLine, baseLvl);
                G2D.Vector lineSOV = new G2D.Vector(surveyLineLocV.Start, surveyLineLocV.End);
                if (revitCmdContext.SurveyOrientation == SurveyOrientation.Flipped)
                { lineSOV = lineSOV.Reverse(); }
                surveySprite.Place(this, lineSOV, baseLvl, revitCmdContext.SurveyDirection);
                revitCmdContext.SaveDetailLineToModel(surveyLine, revitCmdContext.SurveyDirection, revitCmdContext.SurveyOrientation);
            }
            else if (farEndIntersectingWallF != null)
            {
                // There is a "joined" wall face at the end of the detail line that has already been surveyed;
                // activate the wall face (exit line mode)
                surveyWallWorker_setZoomElements(revitCmdContext.ZoomElements, farEndIntersectingWallF);
                surveySprite.Place(this, farEndIntersectingWallF.Edge, baseLvl, revitCmdContext.SurveyDirection);
                revitCmdContext.SaveRefWallFaceToModel(farEndIntersectingWallF, revitCmdContext.SurveyDirection);
            }

            return commitTransaction() ? CommandResult.Succeeded : CommandResult.TransactionError;
        }

        // IProjectModel interface implementation
        public CommandResult SurveyWallHostedObjectPrep(SurveyDirection dir, string familyCategory,
                                                        out CommandContext cmdContext,
                                                        out WallHostedObjectSurveyParams whosParams,
                                                        ref string msg)
        {
            CommandResult retval = CommandResult.Failed;
            cmdContext = null;
            whosParams = null;

            // Enclose this operation in a transaction (that is aborted at the end)
            // to prevent persistent modifications to the document.
            // We want all modifications to happen in SurveyWallHostedObject()
            try
            {
                startTrialTransaction(@"discardChanges");

                revitCommandContext revitCmdContext = new revitCommandContext(this);

                surveyFace f;
                if (revitCmdContext.GetActiveElementFromModel(dir, out f, ref msg) && revitCmdContext.IsRefElementWall)
                {
                    constructSurveyWallHostedObjectData(revitCmdContext, f, familyCategory, out whosParams);
                    cmdContext = revitCmdContext;
                    retval = CommandResult.Succeeded;
                }
            }
            finally
            {
                if (retval != CommandResult.TransactionError) { rollBackTransaction(); }
                if (retval == CommandResult.TransactionError || retval == CommandResult.Failed) { m_txnErrorSignal.Set(); }
            }

            return retval;
        }

        // IProjectModel interface implementation
        public CommandResult SurveyRFCommandMappedObjectPrep(string rfCmdPage, KeypadCommandPhrase cmdPhrase,
                                                             out CommandContext cmdContext,
                                                             out SurveyParams surveyParams,
                                                             ref string msg)
        {
            CommandResult retval = CommandResult.Failed;
            surveyParams = null;
            revitCommandContext revitCmdContext = new revitCommandContext(this);
            cmdContext = revitCmdContext;
            SurveyDirection dir = SurveyDirection.FromModel;

            if (cmdPhrase.Count == 1)
            {
                if (cmdPhrase[0].NumClicks == 1)
                {
                    if (   cmdPhrase[0].CursorKey == KeypadCommand.KeyDef.Left
                        || cmdPhrase[0].CursorKey == KeypadCommand.KeyDef.Right)
                    {
                        // Capturing or navigating to the next wall or line
                        dir = (cmdPhrase[0].CursorKey == KeypadCommand.KeyDef.Left)
                            ? SurveyDirection.RightToLeft : SurveyDirection.LeftToRight;

                        surveyFace f;
                        if (revitCmdContext.GetActiveElementFromModel(dir, out f, ref msg) && !revitCmdContext.IsRefElementWall)
                        {
                            LineSurveyParams lsParams;
                            retval = SurveyLinePrep(dir, false, out cmdContext, out lsParams, ref msg);
                            surveyParams = lsParams;
                        }
                        else
                        {
                            msg = @"";  // Do not propagate; if there are no walls in the project, not an error
                            WallSurveyParams wsParams;
                            retval = SurveyWallPrep(dir, out cmdContext, out wsParams, ref msg);
                            surveyParams = wsParams;
                        }
                    }
                    else
                    {
                        // Capturing a wall-hosted object
                        string familyCategory = docSettings.RFKeypadMaps.LookupFamilyCategoryInPage(rfCmdPage, cmdPhrase[0]);
                        if (familyCategory != @"")
                        {
                            surveyFace f;
                            if (revitCmdContext.GetActiveElementFromModel(dir, out f, ref msg))
                            {
                                if (familyCategory.IsLines())
                                {
                                    LineSurveyParams lsParams;
                                    retval = SurveyLinePrep(dir, false, out cmdContext, out lsParams, ref msg);
                                    surveyParams = lsParams;
                                }
                                else if (revitCmdContext.IsRefElementWall)
                                {
                                    WallHostedObjectSurveyParams whosParams;
                                    constructSurveyWallHostedObjectData(revitCmdContext, f, familyCategory, out whosParams);
                                    surveyParams = whosParams;
                                    retval = CommandResult.Succeeded;
                                }
                            }
                        }
                    }
                }
            }

            else if (cmdPhrase.Count == 2)
            {
                if (cmdPhrase[1].NumClicks == 1)
                {
                    bool isValidCursorKey = false;
                    if (cmdPhrase[1].CursorKey == KeypadCommand.KeyDef.Left)
                    {
                        dir = SurveyDirection.RightToLeft;
                        isValidCursorKey = true;
                    }
                    else if (cmdPhrase[1].CursorKey == KeypadCommand.KeyDef.Right)
                    {
                        dir = SurveyDirection.LeftToRight;
                        isValidCursorKey = true;
                    }

                    if (isValidCursorKey)
                    {
                        string familyCategory = docSettings.RFKeypadMaps.LookupFamilyCategoryInPage(rfCmdPage, cmdPhrase[0]);
                        if (familyCategory != @"")
                        {
                            surveyFace f;
                            if (revitCmdContext.GetActiveElementFromModel(dir, out f, ref msg))
                            {
                                WallHostedObjectSurveyParams whosParams;
                                constructSurveyWallHostedObjectData(revitCmdContext, f, familyCategory, out whosParams);
                                surveyParams = whosParams;
                                retval = CommandResult.Succeeded;
                            }
                        }
                    }
                }
            }

            return retval;
        }

        // IProjectModel interface implementation
        public CommandResult SurveyWallHostedObject(CommandContext cmdContext, WallHostedObjectSurveyParams whosParams, ref string msg)
        {
            revitCommandContext context = (revitCommandContext)cmdContext;
            surveyFace f = context.GetWallFace();
            SurveySide nearSide = context.SurveyDirection.NearSide;

            string familyCategory = whosParams.FamilyCategory;
            startTransaction(@"Survey " + familyCategory);  // Must be human readable; see case 225

            if (familyCategory.IsWindows() && whosParams.IsStackedWindows)
            {
                List<WindowHeights> twh = fetchData<LevelsData>().LevelDataAt(context.CurrentLevelIdx).TypicalWindowHeights;
                for (int i = 0; i < twh.Count; i++)
                {
                    WallHostedObjectSurveyParams stackedWindowParams = new WallHostedObjectSurveyParams(whosParams);
                    stackedWindowParams.SetHeight(twh[i].Height);
                    stackedWindowParams.SetSillHeight(twh[i].SillHeight);
                    wallHostedInstanceDefinition stackedWindowDef
                        = new wallHostedInstanceDefinition(this, f, nearSide, familyCategory, stackedWindowParams);
                    stackedWindowDef.CreateInstance(this);
                }
            }
            else
            {
                wallHostedInstanceDefinition def = new wallHostedInstanceDefinition(this, f, nearSide, familyCategory, whosParams);
                def.CreateInstance(this);
            }

            // Store the survey params so we have default values for next object of same category
            documentState.SetSurveyParamsCache(this, familyCategory, whosParams);

            // Change the reference wall face's survey direction if necessary
            SurveyDirection origDir;
            context.GetSurveyDirectionFromModel(out origDir);
            if (origDir != context.SurveyDirection)
            {
                surveySprite.Place(this, f.Edge, context.CurrentLevel, context.SurveyDirection);
                context.SaveRefWallFaceToModel(f, context.SurveyDirection);
            }

            return (commitTransaction() ? CommandResult.Succeeded : CommandResult.TransactionError);
        }

        // IProjectModel interface implementation
        public CommandResult CopyObjectsPrep(List<string> selection, out CopyObjectsData data, ref string msg)
        {
            CommandResult retval = CommandResult.Succeeded;

            // Enclose this operation in a transaction (that is aborted at the end)
            // to prevent persistent modifications to the document.
            // We want all modifications to happen in CopyObjects()
            try
            {
                startTrialTransaction(@"discardChanges");

                revitCommandContext context = new revitCommandContext(this);
                data = new CopyObjectsData(context);
                data.Selection = new List<string>();

                if (selection.Count != 0)
                {
                    foreach (string selElId in selection)
                    {
                        Element selEl = doc.GetElement(new ElementId(long.Parse(selElId)));
                        if (selEl != null)
                        {
                            if (selEl is Wall)
                            {
                                data.Selection.Add(selElId);
                                data.IsObjectInSelection[(int)CopyObjectsType.Walls] = true;
                            }
                            else if (selEl.Category.Id.Value == (int)BuiltInCategory.OST_Doors)
                            {
                                data.Selection.Add(selElId);
                                data.IsObjectInSelection[(int)CopyObjectsType.Doors] = true;
                            }
                            else if (selEl.Category.Id.Value == (int)BuiltInCategory.OST_Windows)
                            {
                                data.Selection.Add(selElId);
                                data.IsObjectInSelection[(int)CopyObjectsType.Windows] = true;
                            }
                            else if (selEl is FamilyInstance)
                            {
                                FamilyInstance selInst = selEl as FamilyInstance;
                                if (selInst.Host != null && selInst.Host is Wall)
                                {
                                    data.Selection.Add(selElId);
                                    data.IsObjectInSelection[(int)CopyObjectsType.OtherHostedObjects] = true;
                                }
                            }
                        }
                    }
                    // Do not allow copying to the "top" level
                    context.ProjectLevelNames.RemoveAt(context.ProjectLevelNames.Count - 1);
                }
                else
                {
                    msg += @"Please make a selection before continuing";
                    retval = CommandResult.Failed;
                }

            }
            finally
            {
                if (retval != CommandResult.TransactionError) { rollBackTransaction(); }
                if (retval == CommandResult.TransactionError || retval == CommandResult.Failed) { m_txnErrorSignal.Set(); }
            }

            return retval;
        }

        // IProjectModel interface implementation
        public CommandResult CopyObjects(CopyObjectsData data, ref string msg)
        {
            revitCommandContext context = data.CommandContext as revitCommandContext;
            List<Level> levels = getLevels();
            Level fromLvl = context.CurrentLevel;

            List<Wall> wallsToCopy = new List<Wall>();
            List<wallHostedInstanceDefinition> instDefsToCopy = new List<wallHostedInstanceDefinition>();

            foreach (string elID in data.Selection)
            {
                Element el = doc.GetElement(new ElementId(long.Parse(elID)));
                Wall w = doc.GetElement(new ElementId(long.Parse(elID))) as Wall;
                if (w != null)
                {
                    wallsToCopy.Add(w);

                    List<FamilyInstance> hostedInstances = RevitSurveyWall.GetHostedInstances(doc, w);
                    foreach (FamilyInstance inst in RevitSurveyWall.GetHostedInstances(doc, w))
                    {
                        if (data.Selection.Contains(inst.Id.ToString()))  { continue; }  // Will be picked up in outer loop

                        bool isWindow = wallHostedInstance.isWindow(inst);
                        bool isDoor = wallHostedInstance.isDoor(inst);
                        if (   (isWindow && data.IncludedObjects[(int)CopyObjectsType.Windows] == CopyObjectsIncludeScope.All)
                            || (isDoor && data.IncludedObjects[(int)CopyObjectsType.Doors] == CopyObjectsIncludeScope.All)
                            || (!isWindow && !isDoor && data.IncludedObjects[(int)CopyObjectsType.OtherHostedObjects] == CopyObjectsIncludeScope.All) )
                        { instDefsToCopy.Add(new wallHostedInstanceDefinition(this, inst)); }
                    }
                }

                else
                {
                    FamilyInstance selInst = el as FamilyInstance;
                    if (selInst != null)  // This should be enforced in CopyObjectsPrep()
                    {
                        bool isWindow = wallHostedInstance.isWindow(selInst);
                        bool isDoor = wallHostedInstance.isDoor(selInst);
                        if (   (isWindow && data.IncludedObjects[(int)CopyObjectsType.Windows] != CopyObjectsIncludeScope.None)
                            || (isDoor && data.IncludedObjects[(int)CopyObjectsType.Doors] != CopyObjectsIncludeScope.None)
                            || (!isWindow && !isDoor && data.IncludedObjects[(int)CopyObjectsType.OtherHostedObjects] != CopyObjectsIncludeScope.None) )
                        { instDefsToCopy.Add(new wallHostedInstanceDefinition(this, selInst)); }
                    }
                }

            }

            Wall copiedW;
            Dictionary<long, long> ignoreRestoredInstanceMap = new Dictionary<long, long>();

            startTransaction(@"Copy Level-to-Level");  // Must be human readable; see case 225

            for (int lIdx = data.LowerLevelIdx; lIdx <= data.UpperLevelIdx; lIdx++)
            {
                if (lIdx >= levels.Count - 1) { continue; }  // Exclude "top" level from copy

                Level toLvl = levels[lIdx];
                Level nextLvl = levels[lIdx + 1];
                if (fromLvl.Id.Equals(toLvl.Id)) { continue; }  // No need to copy existing windows on "from" level

                Dictionary<long, Wall> copiedWallMap = new Dictionary<long, Wall>();
                foreach (Wall origW in wallsToCopy)
                {
                    copyWall(origW, toLvl, nextLvl, out copiedW, ignoreRestoredInstanceMap);
                    copiedWallMap.Add(origW.Id.Value, copiedW);
                }

                // Use typical sill and head heights for copied windows if defined
                LevelsData ld = fetchData<LevelsData>();
                List<WindowHeights> twhList = ld.LevelDataAt(lIdx).TypicalWindowHeights;
                WindowHeights twh = (twhList != null && twhList.Count == 1) ? twhList[0] : null;

                foreach (wallHostedInstanceDefinition instDef in instDefsToCopy)
                {
                    wallHostedInstanceDefinition copyInstDef = new wallHostedInstanceDefinition(instDef);
                    if (copyInstDef.FamilyCategory.IsWindows() && twh != null)
                    {
                        copyInstDef.Height = twh.Height;
                        copyInstDef.LevelElevation = twh.SillHeight;
                    }

                    Wall newHostW = null;
                    Wall origHostW = copyInstDef.Host;
                    if (copiedWallMap.ContainsKey(origHostW.Id.Value))
                    { newHostW = copiedWallMap[origHostW.Id.Value]; }
                    else
                    { newHostW = copyObjectsWorker_findNewHostWall(copyInstDef, toLvl); }

                    if (newHostW != null)
                    { copyInstDef.CreateInstanceOnNewHostWallRelative(this, newHostW); }
                }
            }

            return (commitTransaction() ? CommandResult.Succeeded : CommandResult.TransactionError);
        }

        // IProjectModel interface implementation
        public CommandResult ConvertUnknownWidthWallsPrep(List<string> selection, out ConvertUnknownWidthWallsData data, ref string msg)
        {
            CommandResult retval = CommandResult.Succeeded;

            // Enclose this operation in a transaction (that is aborted at the end)
            // to prevent persistent modifications to the document.
            // We want all modifications to happen in Convert()
            try
            {
                startTrialTransaction(@"discardChanges");

                revitCommandContext context = new revitCommandContext(this);
                data = new ConvertUnknownWidthWallsData(context);

                foreach (string selElId in selection)
                {
                    Element el = m_doc.GetElement(new ElementId(long.Parse(selElId)));
                    Wall w = el as Wall;
                    if (w != null && RevitSurveyWall.IsUnknownWidth(w))
                    { data.Selection.Add(selElId); }
                }
            }
            finally
            {
                if (retval != CommandResult.TransactionError) { rollBackTransaction(); }
                if (retval == CommandResult.TransactionError || retval == CommandResult.Failed) { m_txnErrorSignal.Set(); }
            }

            return retval;
        }

        private class unknownWidthWallPair
        {
            internal Wall BaseWall;
            internal SurveySide BaseWallSide;
            internal SurveyOrientation BaseWallSO;
            internal Wall IntersectWall;
            internal SurveyOrientation IntersectWallSO;
            internal G2D.Point IntersectLocation;
        }

        private class uwwDeleteInfo
        {
            internal SurveyOrientation DelWOutsideSO;
            internal G2D.Vector DelWOutsideEdge;
            internal G2D.Vector OppWOutsideEdge;
            internal G2D.Vector[] DelWOutsideEdgeJoinedEdges = { null, null };
            internal List<G2D.Vector> ReplacementWallOutsideEdges = new List<G2D.Vector>();
        }

        // IProjectModel interface implementation
        public CommandResult ConvertUnknownWidthWalls(ConvertUnknownWidthWallsData data, ref string msg)
        {
            revitCommandContext context = data.CommandContext as revitCommandContext;

            double makeMeASetting = 2.0;  // Maximum width of replacement wall
            List<ElementId> unconvertedWalls = new List<ElementId>();

            List<Wall> uwWalls = new List<Wall>();

            if (data.Scope == ConvertUnknownWidthWallsScope.Selected)
            {
                foreach (string selElId in data.Selection)
                { uwWalls.Add(doc.GetElement(new ElementId(long.Parse(selElId))) as Wall); }
            }
            else
            {
                FilteredElementCollector wallColl = new FilteredElementCollector(m_doc).OfClass(typeof(Wall));
                foreach (Element wEl in wallColl.ToElements())
                {
                    Wall w = wEl as Wall;
                    if (   w != null && RevitSurveyWall.IsUnknownWidth(w)
                        && (data.Scope == ConvertUnknownWidthWallsScope.All || w.LevelId == context.CurrentLevel.Id))
                    { uwWalls.Add(w); }
                }
            }

            List<unknownWidthWallPair> uwWallPairs = new List<unknownWidthWallPair>();
            foreach (Wall uww in uwWalls)
            {
                try
                {
                    Level lvl = RevitSurveyWall.BaseLevel(uww);
                    List<long> excludeUwwList = new List<long>();
                    excludeUwwList.Add(uww.Id.Value);

                    for (int soIdx = 0; soIdx < 2; soIdx++)
                    {
                        SurveyOrientation so = SurveyOrientation.FromIndex(soIdx);

                        for (int sideIdx = 0; sideIdx < 2; sideIdx++)
                        {
                            SurveySide side = SurveySide.FromIndex(sideIdx);
                            surveyFace f;
                            surveyFace.CreateFaceNoModifyWalls(this, lvl, uww, so, side, out f);
                            G2D.Radians endOffsetAz = (side == SurveySide.Left) ? f.Azimuth : f.Azimuth + G2D.Radians.PI;
                            G2D.Point rayOrigin2D = new G2D.Vector(f.ContinuationCorner(side), endOffsetAz, 0.005).End;

                            // Maximum width of replaced wall is the minimum of the setting value or 1/2 the wall length
                            double maxWidth = Math.Min(makeMeASetting, f.WallExtent.ValueRevitInternal);
                            Reference intersectRef = surveyWallWorker_findClosestWallRef(rayOrigin2D, f.Azimuth - G2D.Radians.PIOverTwo, lvl, excludeUwwList, maxWidth, 0.005);

                            if (intersectRef == null) { continue; }
                            Wall intersectW = m_doc.GetElement(intersectRef) as Wall;
                            if (intersectW == null) { continue; }
                            PlanarFace wPF = intersectW.GetGeometryObjectFromReference(intersectRef) as PlanarFace;
                            if (wPF == null) { continue; }
                            SurveyOrientation intersectWSO = Utilities.SurveyOrientationFromFace(intersectW, wPF);


                            if (   !RevitSurveyWall.IsUnknownWidth(intersectW)
                                || intersectW.LevelId != lvl.Id
                                || RevitSurveyWall.Height(uww) != RevitSurveyWall.Height(intersectW)
                                || RevitSurveyWall.BaseOffset(uww) != RevitSurveyWall.BaseOffset(intersectW))
                            { continue; }

                            // If there is an inside wall joined to the subject wall, ensure that the intersecting
                            // wall is joined to it as well; otherwise there is something else going on and we should
                            // not be converting this wall pair (see test case 0206-04)
                            joinedWallGeometry uwwG = getJoinedWalls(uww, lvl, side, so);
                            Wall insideW = uwwG.InsideWall;
                            if (insideW != null)
                            {
                                joinedWallGeometry intersectWG = getJoinedWalls(intersectW, lvl, side.Other, intersectWSO);
                                if (intersectWG.InsideWall == null || intersectWG.InsideWall.Id != insideW.Id)
                                { continue; }
                                // See whether we are looking at three sides of an unknown-width "box".  If so, this is
                                // probably some sort of mass like a wall column, so don't replace the unknown-width walls.
                                // Let the user figure out how to model this.
                                if (uwwG.OutsideWall == null && intersectWG.OutsideWall == null)
                                { continue; }
                            }

                            // See if this pair corresponds to an already-found pair looking from the other side
                            bool isFoundExistingPair = false;
                            foreach (unknownWidthWallPair existingPair in uwWallPairs)
                            {
                                if (existingPair.BaseWall.Id == intersectW.Id
                                    && existingPair.IntersectWall.Id == uww.Id
                                    && existingPair.BaseWallSide == side.Other)
                                { isFoundExistingPair = true; break; }
                            }
                            if (isFoundExistingPair) { continue; }

                            unknownWidthWallPair pair = new unknownWidthWallPair();
                            pair.BaseWall = uww;
                            pair.BaseWallSide = side;
                            pair.BaseWallSO = so;
                            pair.IntersectWall = intersectW;
                            pair.IntersectLocation = Utilities.To2D(intersectRef.GlobalPoint);
                            pair.IntersectWallSO = Utilities.SurveyOrientationFromFace(intersectW, wPF);
                            uwWallPairs.Add(pair);
                        }
                    }
                }
                catch (HardException)  // If we can't handle the geometry, skip the wall in question and keep going
                {
                    unconvertedWalls.Add(uww.Id);
                    continue;
                }
            }

            Dictionary<ElementId, uwwDeleteInfo> uwWallsToDelete = new Dictionary<ElementId, uwwDeleteInfo>();
            List<ElementId> gapWallsToDelete = new List<ElementId>();
            List<wallDefinition> replacementWallDefs = new List<wallDefinition>();
            List<unknownWidthWallPair> matchedPairs = new List<unknownWidthWallPair>();

            foreach (unknownWidthWallPair refPair in uwWallPairs)
            {
                if (matchedPairs.Contains(refPair)) { continue; }

                foreach (unknownWidthWallPair matchingPair in uwWallPairs)
                {
                    SurveyOrientation baseWOutsideSO = SurveyOrientation.NotFlipped;
                    G2D.Vector baseWOutsideEdge = null;
                    SurveyOrientation intersectWOutsideSO = SurveyOrientation.NotFlipped;
                    G2D.Vector intersectWOutsideEdge = null;
                    G2D.Vector startPtLocV = null;
                    G2D.Vector endPtLocV = null;

                    if (matchingPair.BaseWall.Id == refPair.IntersectWall.Id
                        && matchingPair.IntersectWall.Id == refPair.BaseWall.Id)
                    {
                        baseWOutsideSO = refPair.BaseWallSO.Other;
                        baseWOutsideEdge = RevitSurveyWall.WallEdge(refPair.BaseWall, baseWOutsideSO, true);
                        intersectWOutsideSO = matchingPair.BaseWallSO.Other;
                        intersectWOutsideEdge = RevitSurveyWall.WallEdge(matchingPair.BaseWall, intersectWOutsideSO, true);
                        G2D.Point baseWEdgeEndPt = (refPair.BaseWallSide.Other == SurveySide.Left) ? baseWOutsideEdge.Start : baseWOutsideEdge.End;
                        G2D.Point intersectWEdgeEndPt = (matchingPair.BaseWallSide.Other == SurveySide.Left) ? intersectWOutsideEdge.Start : intersectWOutsideEdge.End;

                        // Match the orientation of the converted wall to the orientation of the reference pair's base wall
                        // (this is arbitrary but we need to be consistent so we can orient the replaced hosted instances properly)
                        if (   baseWOutsideSO == SurveyOrientation.NotFlipped && refPair.BaseWallSide.Other == SurveySide.Left
                            || baseWOutsideSO == SurveyOrientation.Flipped && refPair.BaseWallSide.Other == SurveySide.Right)
                        {
                            startPtLocV = new G2D.Vector(intersectWOutsideEdge.Project(baseWEdgeEndPt), baseWEdgeEndPt);
                            endPtLocV = new G2D.Vector(baseWOutsideEdge.Project(intersectWEdgeEndPt), intersectWEdgeEndPt);
                        }
                        else
                        {
                            startPtLocV = new G2D.Vector(baseWOutsideEdge.Project(intersectWEdgeEndPt), intersectWEdgeEndPt);
                            endPtLocV = new G2D.Vector(intersectWOutsideEdge.Project(baseWEdgeEndPt), baseWEdgeEndPt);
                        }
                    }
                    else if (matchingPair.BaseWall.Id == refPair.BaseWall.Id
                             && matchingPair.IntersectWall.Id == refPair.IntersectWall.Id
                             && matchingPair.BaseWallSide != refPair.BaseWallSide)
                    {
                        baseWOutsideSO = refPair.BaseWallSO.Other;
                        baseWOutsideEdge = RevitSurveyWall.WallEdge(refPair.BaseWall, baseWOutsideSO, true);
                        intersectWOutsideSO = refPair.IntersectWallSO.Other;
                        intersectWOutsideEdge = RevitSurveyWall.WallEdge(refPair.IntersectWall, intersectWOutsideSO, true);

                        // Match the orientation of the converted wall to the orientation of the reference pair's base wall
                        // (this is arbitrary but we need to be consistent so we can orient the replaced hosted instances properly)
                        if (baseWOutsideSO == SurveyOrientation.Flipped)
                        {
                            startPtLocV = new G2D.Vector(intersectWOutsideEdge.Project(baseWOutsideEdge.End), baseWOutsideEdge.End);
                            endPtLocV = new G2D.Vector(intersectWOutsideEdge.Project(baseWOutsideEdge.Start), baseWOutsideEdge.Start);
                        }
                        else
                        {
                            startPtLocV = new G2D.Vector(intersectWOutsideEdge.Project(baseWOutsideEdge.Start), baseWOutsideEdge.Start);
                            endPtLocV = new G2D.Vector(intersectWOutsideEdge.Project(baseWOutsideEdge.End), baseWOutsideEdge.End);
                        }
                    }

                    if (startPtLocV != null)
                    {
                        // Determine the width of the replacement wall
                        // Since the unknown width wall pair may not be exactly parallel, we take the average of the
                        // distances between the outside faces at each end to determine the replacement wall width.
                        // If this width differs from the outside wall distance at either end by more than twice the measurement
                        // tolerance (1x the measurement tolerance on each side), we do not convert the walls
                        Distance replacementWallWidth = new Distance((startPtLocV.Length + endPtLocV.Length) / 2.0, Distance.Units.RevitInternal);
                        replacementWallWidth = replacementWallWidth.Round(docSettings.WallWidthRounding);
                        Distance tol = docSettings.MeasurementTolerance;
                        if (Distance.Abs(replacementWallWidth - new Distance(startPtLocV.Length, Distance.Units.RevitInternal)) < tol
                            && Distance.Abs(replacementWallWidth - new Distance(endPtLocV.Length, Distance.Units.RevitInternal)) < tol)
                        {
                            startTransaction(@"ConvertUWWalls_matchingWallType");
                            WallType wt = this.matchingWallType(replacementWallWidth);
                            if (!commitTransaction()) { return CommandResult.TransactionError; }

                            Level baseLvl = RevitSurveyWall.BaseLevel(refPair.BaseWall);
                            G2D.Vector locLineV = new G2D.Vector(startPtLocV.MidPoint, endPtLocV.MidPoint);

                            wallDefinition def = new wallDefinition(this, refPair.BaseWall);
                            def.LocLine = Utilities.ToRevitLine(locLineV);
                            def.WallTypeId = wt.Id;

                            // Set the state of the new wall to the state of the outside faces of the uw walls
                            def.WallState = new WallState();
                            WallState baseWallWS = getWallState(refPair.BaseWall);
                            WallState intersectWallWS = getWallState(refPair.IntersectWall);
                            if (baseWallWS.IsFaceSurveyed(baseWOutsideSO))
                            {
                                def.WallState.SetIsFaceSurveyed(baseWOutsideSO);
                                def.WallState.SetLevelOffset(baseWOutsideSO, baseWallWS.LevelOffset(baseWOutsideSO));
                            }
                            if (intersectWallWS.IsFaceSurveyed(intersectWOutsideSO))
                            {
                                def.WallState.SetIsFaceSurveyed(baseWOutsideSO.Other);
                                def.WallState.SetLevelOffset(baseWOutsideSO.Other, intersectWallWS.LevelOffset(intersectWOutsideSO));
                            }

                            // Find the wall hosted family instances from both unknown-width walls that are on the location
                            // line of the replacement wall to be created
                            def.HostedInstances = new List<wallHostedInstanceDefinition>();

                            Distance locLineLen = new Distance(locLineV.Length, Distance.Units.RevitInternal);

                            foreach (FamilyInstance inst in RevitSurveyWall.GetHostedInstances(doc, refPair.BaseWall))
                            {
                                if (Utilities.IsPointOnLineSegment(locLineV, Utilities.To2D((inst.Location as LocationPoint).Point)))
                                {
                                    wallHostedInstanceDefinition instDef = new wallHostedInstanceDefinition(this, inst);
                                    instDef.AbsoluteLocationXY = locLineV.Project(instDef.AbsoluteLocationXY);
                                    def.HostedInstances.Add(instDef);
                                }
                            }

                            // If the survey orientation of the outside of both walls is the same, that means that their base
                            // orientations are opposite; that means that any hosted instances to be created on the replacement wall
                            // that came from the ref pair's intersect wall need to be flipped, since the replacement wall's orientation will be
                            // opposite that of the intersect wall
                            bool isFlipInstanceFacing = (baseWOutsideSO == intersectWOutsideSO);

                            foreach (FamilyInstance inst in RevitSurveyWall.GetHostedInstances(doc, refPair.IntersectWall))
                            {
                                if (Utilities.IsPointOnLineSegment(locLineV, Utilities.To2D((inst.Location as LocationPoint).Point)))
                                {
                                    wallHostedInstanceDefinition instDef = new wallHostedInstanceDefinition(this, inst);
                                    instDef.AbsoluteLocationXY = locLineV.Project(instDef.AbsoluteLocationXY);
                                    if (isFlipInstanceFacing)
                                    {
                                        instDef.IsFlipFacing = !instDef.IsFlipFacing;
                                        instDef.IsFlipHand = !instDef.IsFlipHand;
                                    }
                                    def.HostedInstances.Add(instDef);
                                }
                            }

                            // Add the replacement wall definition to the list of walls to create
                            replacementWallDefs.Add(def);

                            // Add the matched unknown-width walls to the list of walls to delete
                            uwwDeleteInfo info;
                            ElementId[] gapWID = new ElementId[2] {null, null};
                            if (!uwWallsToDelete.ContainsKey(refPair.BaseWall.Id))
                            {
                                info = new uwwDeleteInfo();
                                info.DelWOutsideSO = baseWOutsideSO;
                                info.DelWOutsideEdge = baseWOutsideEdge;
                                for (int sideIdx = 0; sideIdx < 2; sideIdx++)
                                {
                                    joinedWallGeometry g = getJoinedWalls(refPair.BaseWall, baseLvl,
                                                                          SurveySide.FromIndex(sideIdx), baseWOutsideSO);
                                    if (g.InsideWall != null)
                                    { info.DelWOutsideEdgeJoinedEdges[sideIdx] = RevitSurveyWall.WallEdge(g.InsideWall, g.InsideWallSO, true); }
                                    if (g.OutsideWall != null && (g.InsideWall == null || g.InsideWall.Id != g.OutsideWall.Id))
                                    { gapWID[sideIdx] = g.OutsideWall.Id; }
                                }
                                info.OppWOutsideEdge = intersectWOutsideEdge;
                                uwWallsToDelete.Add(refPair.BaseWall.Id, info);
                            }
                            else { info = uwWallsToDelete[refPair.BaseWall.Id]; }
                            info.ReplacementWallOutsideEdges.Add(new G2D.Vector(baseWOutsideEdge.Project(startPtLocV.Start),
                                                                                baseWOutsideEdge.Project(endPtLocV.Start)));

                            if (!uwWallsToDelete.ContainsKey(refPair.IntersectWall.Id))
                            {
                                info = new uwwDeleteInfo();
                                info.DelWOutsideSO = intersectWOutsideSO;
                                info.DelWOutsideEdge = intersectWOutsideEdge;
                                for (int sideIdx = 0; sideIdx < 2; sideIdx++)
                                {
                                    joinedWallGeometry g = getJoinedWalls(refPair.IntersectWall, baseLvl,
                                                                          SurveySide.FromIndex(sideIdx), intersectWOutsideSO);
                                    if (g.InsideWall != null)
                                    { info.DelWOutsideEdgeJoinedEdges[sideIdx] = RevitSurveyWall.WallEdge(g.InsideWall, g.InsideWallSO, true); }
                                    if (g.OutsideWall != null && (g.InsideWall == null || g.InsideWall.Id != g.OutsideWall.Id))
                                    {
                                        ElementId outsideWID = g.OutsideWall.Id;
                                        if ((gapWID[0] != null && gapWID[0] == outsideWID) || (gapWID[1] != null && gapWID[1] == outsideWID))
                                        { gapWallsToDelete.Add(outsideWID); }
                                    }
                                }
                                info.OppWOutsideEdge = baseWOutsideEdge;
                                uwWallsToDelete.Add(refPair.IntersectWall.Id, info);
                            }
                            else { info = uwWallsToDelete[refPair.IntersectWall.Id]; }
                            info.ReplacementWallOutsideEdges.Add(new G2D.Vector(intersectWOutsideEdge.Project(startPtLocV.Start),
                                                                                intersectWOutsideEdge.Project(endPtLocV.Start)));

                            matchedPairs.Add(refPair);
                            matchedPairs.Add(matchingPair);
                            break;
                        }
                    }
                }
            }

            // Iterate through the list of unknown-width walls to be deleted and look for segments where there is no new wall to
            // replace them.  If so, we want to fill up these "gaps" with unknown-width walls, so there is still a contiguous
            // wall face
            startTransaction(@"ConvertUWWalls_matchingWallType");
            WallType uwWT = this.matchingWallType(Distance.NoValue);   // designates unknown width
            if (!commitTransaction()) { return CommandResult.TransactionError; }

            foreach (ElementId delWId in uwWallsToDelete.Keys)
            {
                Wall delW = (Wall)m_doc.GetElement(delWId);
                SurveyOrientation delWSO = uwWallsToDelete[delWId].DelWOutsideSO;
                G2D.Vector delWCL = RevitSurveyWall.Centerline(delW);
                G2D.Vector delWOutsideEdge = uwWallsToDelete[delWId].DelWOutsideEdge;
                List<G2D.Vector> rWOutsideEdges = uwWallsToDelete[delWId].ReplacementWallOutsideEdges;
                G2D.Vector oppWOutsideEdge = uwWallsToDelete[delWId].OppWOutsideEdge;
                G2D.Vector[] joinedEdges = uwWallsToDelete[delWId].DelWOutsideEdgeJoinedEdges;

                // First, orient the vectors corresponding to the replacement walls
                for (int i = 0; i < rWOutsideEdges.Count; i++)
                {
                    if (rWOutsideEdges[i].Start.DistanceTo(delWOutsideEdge.Start) > rWOutsideEdges[i].End.DistanceTo(delWOutsideEdge.Start))
                    { rWOutsideEdges[i] = rWOutsideEdges[i].Reverse(); }
                }

                // Next, find the replacement wall closest to the start (left) end of the wall to delete; if the start point
                // of this wall is not coincident with the start point of the wall to delete, we need to add a new unknown width
                // wall to the missing section
                G2D.Point delWOutsideEdgeCursor = delWOutsideEdge.Start;
                bool isCursorAtStartOfEdge = true;
                while (rWOutsideEdges.Count > 0)
                {
                    G2D.Vector closestRWEdge = null;
                    Distance minD = new Distance(delWOutsideEdge.Length, Distance.Units.RevitInternal);
                    foreach (G2D.Vector rWOutsideEdge in rWOutsideEdges)
                    {
                        Distance startToCursorD = rWOutsideEdge.Start.DistanceTo(delWOutsideEdgeCursor);
                        if (startToCursorD < minD)
                        {
                            closestRWEdge = rWOutsideEdge;
                            minD = startToCursorD;
                        }
                    }

                    if (   minD > docSettings.MeasurementTolerance
                        && (   !isCursorAtStartOfEdge
                            || joinedEdges[0] == null
                            || !joinedEdges[0].IsCoincident(oppWOutsideEdge.Project(closestRWEdge.Start)) ) )
                    {
                        wallDefinition def = convertUWWallsWorker_calcUWFillerWallDef(delW, delWSO, delWCL, uwWT,
                                                                                      delWOutsideEdgeCursor, closestRWEdge.Start);
                        replacementWallDefs.Add(def);
                    }

                    delWOutsideEdgeCursor = closestRWEdge.End;
                    rWOutsideEdges.Remove(closestRWEdge);
                    isCursorAtStartOfEdge = false;
                }

                if (delWOutsideEdgeCursor.DistanceTo(delWOutsideEdge.End) > docSettings.MeasurementTolerance
                    && (   joinedEdges[1] == null
                        || joinedEdges[1].IsCoincident(oppWOutsideEdge.Project(delWOutsideEdgeCursor)) ) )
                {
                    wallDefinition def = convertUWWallsWorker_calcUWFillerWallDef(delW, delWSO, delWCL, uwWT,
                                                                                  delWOutsideEdgeCursor, delWOutsideEdge.End);
                    replacementWallDefs.Add(def);
                }
            }

            Dictionary<long, preserveGeometryInfo> geometryToPreserve = new Dictionary<long, preserveGeometryInfo>();
            startTrialTransaction(@"ConvertUWWalls_findJoins");
            foreach (ElementId uwwId in uwWallsToDelete.Keys)
            {
                Wall uww = (Wall)m_doc.GetElement(uwwId);
                Level baseLvl = RevitSurveyWall.BaseLevel(uww);
                preserveGeometryInfo pgi = new preserveGeometryInfo(this, baseLvl);
                pgi.FindWallsJoinedToSurveyWall(uww);
                pgi.FindCornersToPreserve();
                if (geometryToPreserve.ContainsKey(baseLvl.Id.Value))
                { geometryToPreserve[baseLvl.Id.Value].MergePreservedCorners(pgi); }
                else
                { geometryToPreserve.Add(baseLvl.Id.Value, pgi); }
            }
            rollBackTransaction();

            startTransaction(@"ConvertUWWalls_deleteWalls");
            foreach (ElementId id in uwWallsToDelete.Keys)
            { deleteWall(id); }
            foreach (ElementId id in gapWallsToDelete)
            {
                bool isDeleted = false;
                foreach (ElementId delId in uwWallsToDelete.Keys)  { if (delId == id)  { isDeleted = true; break; } }
                if (!isDeleted)  { deleteWall(id); }
            }
            if (!commitTransaction()) { return CommandResult.TransactionError; }

            startTransaction(@"ConvertUWWalls_moveJoinedWalls");
            foreach (preserveGeometryInfo pgi in geometryToPreserve.Values)
            {
                pgi.FindWallsToMove();
                pgi.MoveWalls();
            }
            if (!commitTransaction()) { return CommandResult.TransactionError; }

            Wall replacementW;
            Dictionary<long, long> ignoreRestoredInstanceMap = new Dictionary<long, long>();
            startTransaction(@"ConvertUWWalls_finalCreate");
            foreach (wallDefinition def in replacementWallDefs)
            { def.CreateNewWall(this, out replacementW, ignoreRestoredInstanceMap); }
            if (!commitTransaction()) { return CommandResult.TransactionError; }  // "ConvertUWWalls_finalCreate"

            startTransaction(@"Convert Unknown Width Walls");  // Must be human readable, see case 225
            foreach (preserveGeometryInfo pgi in geometryToPreserve.Values)
            { pgi.ApplyOffsets(); }

            if (replacementWallDefs.Count > 0)
            {
                // The reference wall might have been deleted; clear the reference element and sprite just in case
                context.ClearRefElementAndSave();
                surveySprite.DeleteAll(this);
            }

            if (unconvertedWalls.Count > 0)
            {
                FailureMessage failureMsg = new FailureMessage(CustomFailures.UnconvertedWallsWarningId);
                failureMsg.SetFailingElements(unconvertedWalls);
                doc.PostFailure(failureMsg);
            }

            if (!commitTransaction()) { return CommandResult.TransactionError; }   // "Convert Unknown Width Walls"

            return CommandResult.Succeeded;
        }

        // IProjectModel interface implementation
        public void GetModelState(out ModelState state)
        {
            startTransaction(@"Export Model State");
            state = new RevitModelState(this.doc);
            commitTransaction();
        }
        
        // IProjectModel interface implementation
        public DocumentSettings DocumentSettings
        {
            get
            {
                return docSettings;
            }
            set
            {
                startTransaction(@"DocumentSettings property setter");
                storeObject(doc.ProjectInformation, value);
                commitTransaction();
            }
        }

        // IProjectModel interface implementation
        public LevelsData LevelsData
        {
            get
            {
                startTransaction(@"LevelsData property getter");
                LevelsData retval = fetchObjOfType<LevelsData>(doc.ProjectInformation, false);
                if (retval == null)
                {
                    retval = new LevelsData();
                    updateLevelsDataFromDoc(retval);
                    retval.RecalculateFloor2FloorElevations();
                }
                commitTransaction();
                return retval;
            }
            set
            {
                startTransaction(@"LevelsData property setter");
                updateDocFromLevelsData(value);
                storeObject(doc.ProjectInformation, value);
                commitTransaction();
            }
        }

        // IProjectModel interface implementation
        public CommandResult Save(out SaveError err, ref string msg)
        {
            CommandResult retval;
            try
            {
                if (doc.PathName == null || doc.PathName == @"")
                {
                    err = SaveError.NoPathSet;
                    retval = CommandResult.Failed;
                }
                else if (doc.IsReadOnlyFile)
                {
                    err = SaveError.ReadOnlyFile;
                    retval = CommandResult.Failed;
                }
                else
                {
                    doc.Save();  // Throws on error
                    err = SaveError.None;
                    retval = CommandResult.Succeeded;
                }
            }
            catch (Autodesk.Revit.Exceptions.InvalidOperationException e)
            {
                err = SaveError.InvalidOperation;
                if (msg.Length > 0) { msg += "; "; }
                msg += e.Message;
                retval = CommandResult.Failed;
            }
            catch (Exception e)
            {
                err = SaveError.Other;
                if (msg.Length > 0) { msg += "; "; }
                msg += e.Message;
                retval = CommandResult.Failed;
            }
            return retval;
        }
        
        // IProjectModel interface implementation
        public UnitSystem UnitSystem
        { get { return unitSystem; } }

        // IProjectModel interface implementation
        public List<string> GetRangefinderKeypadMapPages()
        { return docSettings.RFKeypadMaps.GetAllPages(); }
        
        // IProjectModel interface implementation
        public List<string> GetBuiltinFamilyCategories()
        { return FamilyCategoryUtils.GetBuiltinFamilyCategories(doc); }

        // IProjectModel interface implementation
        public List<string> GetCustomFamilyCategories()
        { return FamilyCategoryUtils.GetCustomFamilyCategories(doc); }
            
        // IProjectModel interface implementation
        public System.Threading.AutoResetEvent TransactionErrorSignal
        { set { m_txnErrorSignal = value; } }

        private void surveyWallWorker_populateWallWidthList(WallSurveyParams wp)
        {
            foreach (WallType wt in getWallTypes())
            {
                if (!RevitSurveyWall.IsUnknownWidth(wt))
                {
                    double rwidth = Math.Round(wt.Width, 5);
                    bool isContainsMatchingWidth = false;
                    foreach (Distance w in wp.ProjectWallWidthList)
                    {
                        if (w.ValueRevitInternal == rwidth)
                        { isContainsMatchingWidth = true; break; }
                    }
                    if (!isContainsMatchingWidth)
                    { wp.ProjectWallWidthList.Add(new Distance(rwidth, Distance.Units.RevitInternal)); }
                }
            }
            wp.ProjectWallWidthList.Sort(Distance.Compare);
        }

        private void surveyWallWorker_populateVisibleHeightList(Level lvl, WallSurveyParams wp)
        {
            Distance baseLvlElev = new Distance(lvl.Elevation, Distance.Units.RevitInternal);
            List<Level> levels = getLevels();
            for (int i = 0; i < levels.Count; i++)
            {
                Distance visibleHeight = new Distance(levels[i].Elevation, Distance.Units.RevitInternal)
                                         - baseLvlElev + levelVisibleHeight(levels[i]);
                wp.ProjectLevelVisibleHeightList.Add(visibleHeight);
            }
        }

        private void surveyWallWorker_findOutsideContinuationWall(Wall refW, SurveyOrientation refWSO, G2D.Point farCorner,
                                                                  SurveySide farSide, Level lvl,
                                                                  out Wall continuationW, out SurveyOrientation continuationWSO)
        {
            continuationW = null;
            continuationWSO = SurveyOrientation.NotFlipped;

            joinedWallGeometry g = getJoinedWalls(refW, lvl, farSide, refWSO);
            if (g.OutsideWall != null)
            {
                G2D.Point continuationCorner = RevitSurveyWall.WallIntersectCorner(refW, g.OutsideWall,
                                                                                   farSide, refWSO, g.OutsideWallSO,
                                                                                   true, false);
                if (isWithinMeasurementTolerance(continuationCorner.DistanceTo(farCorner), Distance.Zero))
                {
                    continuationW = g.OutsideWall;
                    continuationWSO = g.OutsideWallSO;
                }
            }
        }

        private void surveyWallWorker_populateDefaultCornerAngleList(Wall refW, SurveyOrientation refWSO, SurveySide farSide, G2D.Radians faceAz,
                                                                     Wall continuationW, SurveyOrientation continuationWSO,
                                                                     bool isInsideAngleOnly,
                                                                     List<G2D.Radians> cornerAngleList)
        {
            G2D.Radians insideCornerAngle = G2D.Radians.PIOverTwo;
            G2D.Radians outsideCornerAngle = G2D.Radians.ThreePIOverTwo;
            G2D.Radians continueCornerAngle = G2D.Radians.Undefined;

            G2D.Radians coordinateSys = documentState.GetCoordinateSystemAngle(this);
            if (!coordinateSys.IsUndefined)
            {
                // For the next wall after changing the coordinate system, we offer the user another
                // pre-defined corner angle-- the "180 degree" equivalent angle, which can be handy to provide
                // (case 402)
                continueCornerAngle = G2D.Radians.PI;

                G2D.Radians reducedFaceAz = faceAz;
                if (reducedFaceAz < G2D.Radians.Zero) { reducedFaceAz = G2D.Radians.TwoPIPlus(reducedFaceAz); }
                while (reducedFaceAz > G2D.Radians.PIOverFour) { reducedFaceAz -= G2D.Radians.PIOverTwo; }

                G2D.Radians adjFaceAz = coordinateSys - reducedFaceAz;
                if (farSide == SurveySide.Left) { adjFaceAz = -adjFaceAz; }
                insideCornerAngle += adjFaceAz;
                continueCornerAngle += adjFaceAz;
                outsideCornerAngle += adjFaceAz;
                if (insideCornerAngle <= G2D.Radians.PIOverFour)
                {
                    insideCornerAngle += G2D.Radians.PIOverTwo;
                    continueCornerAngle += G2D.Radians.PIOverTwo;
                    outsideCornerAngle += G2D.Radians.PIOverTwo;
                }
            }
            else if (refW != null && continuationW != null)
            {
                G2D.Radians continuationWAz = RevitSurveyWall.Azimuth(continuationW, continuationWSO);
                if (farSide == SurveySide.Left)
                { insideCornerAngle = faceAz - continuationWAz; }
                else
                { insideCornerAngle = continuationWAz - faceAz; }
                if (insideCornerAngle < G2D.Radians.Zero) { insideCornerAngle = G2D.Radians.TwoPIPlus(insideCornerAngle); }

                if (insideCornerAngle == G2D.Radians.PIOverTwo)
                { isInsideAngleOnly = true; }
                else
                { outsideCornerAngle = G2D.Radians.PIOverTwo; }
            }

            cornerAngleList.Add(insideCornerAngle);
            if (continueCornerAngle != G2D.Radians.Undefined)  { cornerAngleList.Add(continueCornerAngle); }
            if (!isInsideAngleOnly) { cornerAngleList.Add(outsideCornerAngle); }
        }

        private void surveyWallWorker_populateMaxLengthList(G2D.Point startPt, G2D.Radians faceAz, SurveySide farSide, Level lvl,
                                                            List<long> excludeWallIDs, List<G2D.Radians> cornerAngleList, List<Distance> maxLengthList)
        {
            for (int i = 0; i < cornerAngleList.Count; i++)
            {
                G2D.Radians defAz; G2D.Radians dummyAz;
                Distance len = Distance.NoValue;
                if (startPt != null)
                {
                    surveyWallWorker_calcNewElementAzimuth(faceAz, farSide, cornerAngleList[i], out defAz, out dummyAz);
                    len = surveyWallWorker_getClosestIntersectingWallDistance(startPt, defAz, faceAz, lvl, excludeWallIDs);
                }
                maxLengthList.Add(len);
            }
        }

        private Distance surveyWallWorker_getClosestIntersectingWallDistance(G2D.Point startPt, G2D.Radians az, G2D.Radians faceAz, Level lvl, List<long> excludeWallIDs)
        {
            Distance retval = Distance.NoValue;

            Wall closestWall = null;
            G2D.Vector closestWallEdge = null;
            SurveyOrientation dummySO;

            Reference closestWallRef = surveyWallWorker_findClosestWallRef(startPt, az, lvl, excludeWallIDs,
                                                                           1000.0, docSettings.MeasurementTolerance.ValueRevitInternal);
            if (closestWallRef != null)
            { surveyWallWorker_getWallAndEdgeFromRef(closestWallRef, out closestWall, out closestWallEdge, out dummySO); }

            if (closestWallEdge == null)
            {
                // Check to see if there is an intersecting wall a little to each side of the survey line
                foreach (G2D.Radians rayOriginOffsetAngle in new G2D.Radians[] { G2D.Radians.Zero, -G2D.Radians.PI })
                {
                    G2D.Point rayOriginOffsetPt = (new G2D.Vector(startPt, faceAz + rayOriginOffsetAngle, docSettings.MeasurementTolerance.ValueRevitInternal)).End;
                    closestWallRef = surveyWallWorker_findClosestWallRef(rayOriginOffsetPt, az, lvl, excludeWallIDs,
                                                                         1000.0, docSettings.MeasurementTolerance.ValueRevitInternal);
                    if (closestWallRef != null)
                    {
                        surveyWallWorker_getWallAndEdgeFromRef(closestWallRef, out closestWall, out closestWallEdge, out dummySO);
                        if (closestWallEdge != null) { break; }
                    }
                }
            }

            if (closestWallEdge != null)
            {
                G2D.Point closeWallEndPt = G2D.Vector.Intersection(new G2D.Vector(startPt, az, 1.0), closestWallEdge);
                if (closeWallEndPt != null)
                {
                    G2D.Vector closeLengthV = new G2D.Vector(startPt, closeWallEndPt);
                    retval = new Distance(closeLengthV.Length, Distance.Units.RevitInternal);
                }
            }

            return retval;
        }

        private void surveyWallWorker_resetCoordinateSystem()
        {
            documentState.SetCoordinateSystemAngle(this, G2D.Radians.Undefined);
        }

        private void surveyWallWorker_setWidth(surveyFace f, SurveySide side, Wall continuationW, WallSurveyParams wp)
        {
            wp.IsUnknownWidth = false;

            if (continuationW != null)
            {
                if (RevitSurveyWall.IsUnknownWidth(continuationW))
                {
                    wp.IsUnknownWidth = true;
                    wp.Width = Distance.NoValue;
                }
                else
                {
                    wp.Width = RevitSurveyWall.Width(continuationW);
                }
            }
            else if (wp.IsCreateWall)
            {
                Wall endW = f.EndWall(side);
                if (RevitSurveyWall.IsUnknownWidth(endW))
                {
                    wp.IsUnknownWidth = true;
                    wp.Width = documentState.GetLastAddedWallWidth(this);
                }
                else
                {
                    wp.Width = RevitSurveyWall.Width(endW);
                }
            }
            else
            {
                Distance nominalWidth = Distance.NoValue;
                foreach (long wID in f.FaceWallIDs)
                {
                    Wall w = (Wall)doc.GetElement(new ElementId(wID));
                    if (RevitSurveyWall.IsUnknownWidth(w))
                    { wp.IsUnknownWidth = true; }
                    else
                    {
                        wp.IsUnknownWidth = false;
                        if (nominalWidth.IsNoValue)
                        { nominalWidth = RevitSurveyWall.Width(w); }
                        else if (RevitSurveyWall.Width(w) != nominalWidth)
                        {
                            nominalWidth = Distance.NoValue;
                            break;
                        }
                    }
                }

                if (!wp.IsUnknownWidth) { wp.Width = nominalWidth; }
            }
        }

        private void surveyWallWorker_setZAxisParams(Wall refWall, revitCommandContext context, WallSurveyParams wp)
        {
            Level refWallBaseLevel = RevitSurveyWall.BaseLevel(refWall);
            Level refWallConnectedLevel = RevitSurveyWall.ConnectedLevel(doc, refWall);
            if (refWallConnectedLevel != null)
            {
                wp.ConnectedLevelIdx = context.ProjectLevelNames.IndexOf(refWallConnectedLevel.Name);
                wp.Height = levelVisibleHeight(refWallBaseLevel);
            }
            else
            {
                wp.ConnectedLevelIdx = -1;
                wp.Height = RevitSurveyWall.Height(refWall);
            }
            if (refWallBaseLevel.Id != context.CurrentLevel.Id)
            { wp.Height -= new Distance(context.CurrentLevel.Elevation - refWallBaseLevel.Elevation, Distance.Units.RevitInternal); }
        }

        private WallType surveyWallWorker_getWallType(WallSurveyParams wp, Wall refWall)
        {
            WallType retval = null;

            if (!wp.IsUnknownWidth)
            {
                if (!wp.Width.IsNoValue)
                { retval = matchingWallType(wp.Width); }
                else
                {
                    if (refWall != null)
                    {
                        if (!RevitSurveyWall.IsUnknownWidth(refWall))
                        { retval = refWall.WallType; }
                        else
                        { retval = matchingWallType(wp.Width); }
                    }
                    else
                    { throw new HardException("You must specify a width for the first wall on this level"); }
                }
            }
            else { retval = matchingWallType(Distance.NoValue); }

            return retval;
        }

        private void surveyWallWorker_getZAxisData(Level baseLvl, int currentLvlIdx, WallSurveyParams wp,
                                                   out Level connectedLvl, out Distance wallHeight)
        {
            List<Level> levels = getLevels();
            connectedLvl = (wp.ConnectedLevelIdx != -1) ? levels[wp.ConnectedLevelIdx] : null;
            wallHeight = wp.Height;

            // If no connected level is specified, but the unconnected wall height matches the "visible"
            // (floor to ceiling) height of a level (within tolerance), go ahead and connect to that level
            if (connectedLvl == null)
            {
                Distance baseLvlElevD = new Distance(baseLvl.Elevation, Distance.Units.RevitInternal);
                for (int i = currentLvlIdx; i < levels.Count; i++)
                {
                    Distance visibleHeight = new Distance(levels[i].Elevation, Distance.Units.RevitInternal)
                                             + levelVisibleHeight(levels[i])
                                             - baseLvlElevD;
                    if (isWithinMeasurementTolerance(visibleHeight, wallHeight))
                    {
                        connectedLvl = levels[i + 1];
                        wallHeight = visibleHeight;
                        break;
                    }
                    else if (visibleHeight > wallHeight) { break; }
                }
            }
        }

        private static void surveyWallWorker_calcNewElementAzimuth(G2D.Radians refElementAz, SurveySide farSide, G2D.Radians cornerAngle,
                                                                   out G2D.Radians newElementAz, out G2D.Radians newElementNormalAz)
        {
            Double angleMultiplier = (farSide == SurveySide.Left) ? -1.0 : 1.0;
            G2D.Radians adjCornerAngle = new G2D.Radians((double)cornerAngle * angleMultiplier);
            newElementAz = refElementAz + ((farSide == SurveySide.Right) ? G2D.Radians.PI : G2D.Radians.Zero) + adjCornerAngle;
            newElementNormalAz = refElementAz - G2D.Radians.PIOverTwo + adjCornerAngle;
        }

        private static void surveyWallWorker_calcNewElementAzimuth(Wall refWall, SurveyOrientation refWallSO, SurveySide farSide, G2D.Radians cornerAngle,
                                                                   out G2D.Radians newElementAz, out G2D.Radians newElementNormalAz)
        {
            G2D.Radians refWallAz = RevitSurveyWall.Azimuth(refWall, refWallSO);
            surveyWallWorker_calcNewElementAzimuth(refWallAz, farSide, cornerAngle, out newElementAz, out newElementNormalAz);
        }

        private void surveyWallWorker_findProximateWall(G2D.Vector newWallEndPtsV, G2D.Point nearCorner, SurveySide farSide, Level lvl,
                                                        out Wall proximateWall, out SurveyOrientation proximateWallSO, out G2D.Vector proximateWallEdge)
        {
            proximateWall = null;
            proximateWallSO = SurveyOrientation.NotFlipped;
            proximateWallEdge = null;

            double tol = docSettings.MeasurementTolerance.ValueRevitInternal;

            G2D.Point rayOriginOnFrontEdgePt = (new G2D.Vector(nearCorner, newWallEndPtsV.Azimuth, newWallEndPtsV.Length - (tol * 2.0))).End;
            G2D.Radians faceOffsetAz = newWallEndPtsV.Azimuth + ((farSide == SurveySide.Left) ? G2D.Radians.PIOverTwo : -G2D.Radians.PIOverTwo);
            G2D.Point rayOrigin = (new G2D.Vector(rayOriginOnFrontEdgePt, faceOffsetAz, tol)).End;

            double maxProximity = tol * 4.0;
            Reference closestRef = surveyWallWorker_findClosestWallRef(rayOrigin, newWallEndPtsV.Azimuth, lvl, null, maxProximity, 0.0);
            if (closestRef != null)
            {
                surveyWallWorker_getWallAndEdgeFromRef(closestRef, out proximateWall, out proximateWallEdge, out proximateWallSO);
            }
        }

        private void surveyWallWorker_getWallAndEdgeFromRef(Reference r, out Wall refWall, out G2D.Vector refWallEdge,
                                                            out SurveyOrientation refWallSO)
        {
            refWall = null;
            refWallEdge = null;
            refWallSO = null;

            Wall w = (Wall)m_doc.GetElement(r);
            PlanarFace pf = (PlanarFace)w.GetGeometryObjectFromReference(r);
            XYZ pfNormal = pf.FaceNormal;
            XYZ wOrientation = new XYZ(w.Orientation.X, w.Orientation.Y, w.Orientation.Z);
            if (w.Flipped) { wOrientation = wOrientation.Negate(); }
            G2D.Radians faceAngleToWall = new G2D.Radians(pfNormal.AngleTo(wOrientation));
            if (faceAngleToWall.IsParallelTo(G2D.Radians.Zero))
            {
                refWall = w;
                refWallSO = (pfNormal.IsAlmostEqualTo(wOrientation)) ? SurveyOrientation.Flipped : SurveyOrientation.NotFlipped;
                refWallEdge = RevitSurveyWall.WallEdge(refWall, refWallSO, true);
            }
        }

        private Reference surveyWallWorker_findClosestWallRef(G2D.Point rayOrigin2D, G2D.Radians rayDirAz, Level lvl,
                                                              List<long> excludedWallIDs, double maxProximity, double minProximity)
        {
            Reference retval = null;

            double curProximity = maxProximity - .000001;
            XYZ rayDir = Utilities.To3DUnitVector(rayDirAz);
            ElementClassFilter wallFilter = new ElementClassFilter(typeof(Wall));

            foreach (double rayElev in rayElevations(lvl))
            {
                XYZ rayOrigin = Utilities.To3D(rayOrigin2D, rayElev);
                ReferenceIntersector refIntersector = new ReferenceIntersector(wallFilter, FindReferenceTarget.Face, view3D);
                foreach (ReferenceWithContext iRefWC in refIntersector.Find(rayOrigin, rayDir))
                {
                    Reference iRef = iRefWC.GetReference();
                    Element iEl = m_doc.GetElement(iRef);
                    if (   iEl is Wall
                        && (excludedWallIDs == null || !excludedWallIDs.Contains(iEl.Id.Value))
                        && iEl.GetGeometryObjectFromReference(iRef) is PlanarFace
                        && iRefWC.Proximity > minProximity + .000001
                        && iRefWC.Proximity < curProximity)
                    {
                        retval = iRef;
                        curProximity = iRefWC.Proximity;
                    }
                }
            }

            return retval;
        }

        private void surveyWallWorker_adjustNewWallEndPtsV(ref G2D.Vector newWallEndPtsV, G2D.Point nearCorner, G2D.Point adjFarCorner)
        {
            double adjNewWallLength = (new G2D.Vector(nearCorner, adjFarCorner)).Length;
            if (   adjNewWallLength > newWallEndPtsV.Length
                && adjNewWallLength - newWallEndPtsV.Length < docSettings.MeasurementTolerance.ValueRevitInternal)
            { newWallEndPtsV = new G2D.Vector(newWallEndPtsV.Start, newWallEndPtsV.Azimuth, adjNewWallLength); }
        }

        private void surveyWallWorker_adjustProximateWall(Wall pw, SurveyOrientation pwSO, G2D.Vector pwEdgeV, G2D.Point adjFarCorner)
        {
            G2D.Point endPtOne = (pwSO == SurveyOrientation.NotFlipped) ? pwEdgeV.Start : pwEdgeV.End;
            G2D.Point endPtTwo = (pwSO == SurveyOrientation.NotFlipped) ? pwEdgeV.End : pwEdgeV.Start;
            Distance adjFarCornerToEndPtZero = adjFarCorner.DistanceTo(endPtOne);
            Distance adjFarCornerToEndPtOne = adjFarCorner.DistanceTo(endPtTwo);
            Distance pwLengthChange;
            int pwJoinEndPtIdx;
            if (adjFarCornerToEndPtZero < adjFarCornerToEndPtOne)
            {
                pwLengthChange = adjFarCornerToEndPtZero;
                pwJoinEndPtIdx = 0;
            }
            else
            {
                pwLengthChange = adjFarCornerToEndPtOne;
                pwJoinEndPtIdx = 1;
            }

            LocationCurve proximateWallLoc = (LocationCurve)pw.Location;
            if (   pwLengthChange.ValueRevitInternal > 0.0001
                && pwLengthChange.ValueRevitInternal < docSettings.MeasurementTolerance.ValueRevitInternal
                && proximateWallLoc.get_ElementsAtJoin(pwJoinEndPtIdx).Size == 0)
            {
                // Determine the new location of the proximate wall
                int pwOtherEndPtIdx = (pwJoinEndPtIdx == 0) ? 1 : 0;
                G2D.Vector pwLocV = Utilities.To2D(pw.Location);
                G2D.Radians extendAz = (pwJoinEndPtIdx == 0) ? pwLocV.Azimuth + G2D.Radians.PI : pwLocV.Azimuth;
                pwLocV = new G2D.Vector(pwLocV.EndPt(pwOtherEndPtIdx), extendAz, pwLocV.Length + pwLengthChange.ValueRevitInternal);
                if (pwJoinEndPtIdx == 0) { pwLocV = pwLocV.Reverse(); }
                Line pwNewLocLine = Utilities.ToRevitLine(pwLocV);

                // Grab the state of the wall before we delete it
                wallDefinition newPWDef = new wallDefinition(this, pw);
                // Set the new location line
                newPWDef.LocLine = pwNewLocLine;
                
                // Out with the old...
                deleteWall(pw);
                // ...and in with the new
                Wall newPW;
                Dictionary<long, long> ignoreRestoredInstanceMap = new Dictionary<long, long>();
                newPWDef.CreateNewWall(this, out newPW, ignoreRestoredInstanceMap);
            }
        }

        private void surveyWallWorker_setNewWallOrientation(Wall refWall, SurveyOrientation refWallSO, SurveySide farSide,
                                                            Level lvl, WallType newWT, G2D.Radians newWallAz, G2D.Point corner,
                                                            ref G2D.Vector newWallLocV, out SurveyOrientation newWallSO)
        {
            newWallSO = new SurveyOrientation(refWallSO);

            // If there is an outside "continuation" wall on the back side of the reference wall corner,
            // set the orientation of the new wall to match
            Wall outsideContinuationWall = null;
            joinedWallGeometry g = getJoinedWalls(refWall, lvl, farSide, refWallSO);
            if (g.OutsideWall != null)
            {
                if (g.OutsideWall.WallType.Id.Equals(newWT.Id)
                    && RevitSurveyWall.Azimuth(g.OutsideWall, g.OutsideWallSO).IsAlmostParallelTo(newWallAz))
                {
                    G2D.Point continuationCorner = RevitSurveyWall.WallIntersectCorner(refWall, g.OutsideWall,
                                                                                       farSide, refWallSO, g.OutsideWallSO,
                                                                                       true, false);
                    if (isWithinMeasurementTolerance(continuationCorner.DistanceTo(corner), Distance.Zero))
                    { outsideContinuationWall = g.OutsideWall; }
                }
            }

            bool isMatchedToContinuationWall = false;
            if (outsideContinuationWall != null)
            {
                // Find the join endpoint back to the reference wall
                int newWallJoinPtIdx;
                int outsideWallJoinPtIdx = RevitSurveyWall.JoinPtIdx(outsideContinuationWall, refWall, false);
                if (outsideWallJoinPtIdx != -1)
                {
                    isMatchedToContinuationWall = true;
                    if (outsideWallJoinPtIdx == 0)
                    {
                        newWallLocV = newWallLocV.Reverse();
                        newWallJoinPtIdx = 1;
                    }
                    else
                    {
                        newWallJoinPtIdx = 0;
                    }

                    // Adjust the survey orientation if necessary to match the continuation wall
                    if (newWallJoinPtIdx == RevitSurveyWall.EndPtIndex(farSide, newWallSO))
                    { newWallSO.Flip(); }
                }
            }

            // ...otherwise, match the orientation to that of the reference wall
            if (!isMatchedToContinuationWall)
            {
                if ((farSide == SurveySide.Left && newWallSO == SurveyOrientation.NotFlipped)
                    || (farSide == SurveySide.Right && newWallSO == SurveyOrientation.Flipped))
                { newWallLocV = newWallLocV.Reverse(); }
            }
        }

        private wallDefinition surveyWallWorker_calcGapWallDef(Level baseLvl, Level connectedLvl, Wall surveyWall,
                                                               WallState surveyWallState, WallSurveyParams wp,
                                                               ref Line surveyWallCreateLine, G2D.Vector newWallLocV,
                                                               SurveyOrientation surveyWallSO, SurveySide nearSide)
        {
            // If the survey wall is of unknown width, we have to determine if this is the second
            // face of a wall with a parallel unknown width wall already surveyed on the other side.
            // If this is the case, we should "fill in the gap" between the ends of the unknown-width
            // walls with a continuation of the wall on the other side of the pair of unknown-width walls.
            // We may also need to adjust the corner of the unknown-width survey wall so that the
            // continuation of the end wall is straight

            wallDefinition retval = null;

            // Find the survey orientations that coorespond to the "light" (meaning visible) and
            // "dark" (meaning invisible) edge of the unknown width wall
            SurveyOrientation lightSO = new SurveyOrientation(surveyWallSO);
            SurveyOrientation darkSO = new SurveyOrientation(lightSO); darkSO.Flip();

            surveyFace sWallDarkF;
            surveyFace.CreateFaceNoModifyWalls(this, baseLvl, surveyWall, darkSO, nearSide, out sWallDarkF);

            surveyFace[] darkJoinedCornersF = { null, null };
            for (int sideIdx = 0; sideIdx < 2; sideIdx++)
            { darkJoinedCornersF[sideIdx] = sWallDarkF.AdjacentWallFace(SurveySide.FromIndex(sideIdx), false, true, false); }

            SurveySide darkJoinSide = null;
            if (darkJoinedCornersF[0] != null && darkJoinedCornersF[1] == null)
            { darkJoinSide = SurveySide.FromIndex(0); }
            else if (darkJoinedCornersF[0] == null && darkJoinedCornersF[1] != null)
            { darkJoinSide = SurveySide.FromIndex(1); }

            Wall oppWall = null;
            surveyFace oppWallDarkF = null;
            if (darkJoinSide != null)
            {
                oppWallDarkF = darkJoinedCornersF[darkJoinSide.Index].AdjacentWallFace(darkJoinSide, false, true, false);
                if (oppWallDarkF != null)
                { oppWall = oppWallDarkF.EndWall(darkJoinSide); }
            }

            if (oppWall != null && RevitSurveyWall.IsUnknownWidth(oppWall))
            {
                G2D.Vector sWallLocationLine = RevitSurveyWall.LocationLine(surveyWall);
                G2D.Vector oppWallLocationLine = RevitSurveyWall.LocationLine(oppWall);
                G2D.Point oppWallProjectPt = oppWallLocationLine.MidPoint;
                Distance oppWallToSurveyWallD = sWallLocationLine.Project(oppWallProjectPt).DistanceTo(oppWallProjectPt);
                // The gap wall cannot be longer than 1/4 the length of the shortest connecting wall, and less than 3 feet
                Distance maxGapWallLength = new Distance(Math.Min(Math.Min(sWallLocationLine.Length, oppWallLocationLine.Length) / 4.0, 3.0), Distance.Units.RevitInternal);
                if (G2D.Vector.IsAlmostParallel(oppWallLocationLine, sWallLocationLine)
                    && oppWallToSurveyWallD < maxGapWallLength)
                {
                    // We only need a gap wall if the opposite wall does not already have an inside adjacent wall...
                    surveyFace oppContWInsideF = oppWallDarkF.AdjacentWallFace(darkJoinSide, false, true, false);
                    if (oppContWInsideF == null)
                    {
                        surveyFace oppContWOutsideF = oppWallDarkF.AdjacentWallFace(darkJoinSide, false, false, true);
                        if (oppContWOutsideF != null && !oppContWOutsideF.IsFaceSurveyed)
                        {
                            // ...but has an outside adjacent wall that has not been surveyed on the "outside" face
                            Wall oppContW = oppContWOutsideF.EndWall(darkJoinSide.Other);
                            SurveyOrientation oppContWSO = oppContWOutsideF.EndWallSO(darkJoinSide.Other);

                            surveyFace sWallLightF;
                            surveyFace.CreateFaceNoModifyWalls(this, baseLvl, surveyWall, lightSO, nearSide, out sWallLightF);
                            G2D.Vector sWallLightFaceV = new G2D.Vector(sWallLightF.Corner(SurveySide.Left),
                                                                        sWallLightF.Corner(SurveySide.Right));

                            SurveyOrientation oppContWInsideSO = new SurveyOrientation(oppContWSO); oppContWInsideSO.Flip();
                            surveyFace.CreateFaceNoModifyWalls(this, baseLvl, oppContW, oppContWInsideSO, darkJoinSide,
                                                               out oppContWInsideF);
                            G2D.Vector oppContWInsideFaceV = new G2D.Vector(oppContWInsideF.Corner(SurveySide.Left),
                                                                            oppContWInsideF.Corner(SurveySide.Right));

                            G2D.Point oppContWSWCorner = G2D.Vector.Intersection(sWallLightFaceV, oppContWInsideFaceV);
                            Distance oppContWSWCornerToSWCorner = oppContWSWCorner.DistanceTo(sWallLightF.Corner(darkJoinSide));
                            if (isWithinMeasurementTolerance(oppContWSWCornerToSWCorner, Distance.Zero))
                            {
                                // All conditions have been met -- a gap wall must be created
                                G2D.Point oppContWallOppWallCorner = oppContWInsideF.Corner(darkJoinSide);
                                G2D.Vector oppContWallLocationLine = RevitSurveyWall.LocationLine(oppContW);

                                G2D.Vector gapWLocV;
                                // Match the orientation of the gap wall to the continuation wall
                                if (oppContWOutsideF.EndWallEndPtIndex(darkJoinSide.Other) == 1)
                                {
                                    gapWLocV = new G2D.Vector(oppContWallLocationLine.Project(oppWallDarkF.Corner(darkJoinSide)),
                                                              oppContWallLocationLine.Project(sWallDarkF.Corner(darkJoinSide.Other)));
                                }
                                else
                                {
                                    gapWLocV = new G2D.Vector(oppContWallLocationLine.Project(sWallDarkF.Corner(darkJoinSide.Other)),
                                                              oppContWallLocationLine.Project(oppWallDarkF.Corner(darkJoinSide)));
                                }

                                // Set the data that will be used to create the gap wall
                                retval = new wallDefinition(this, oppContW);
                                retval.LocLine = Utilities.ToRevitLine(gapWLocV);
                                retval.WallState = new WallState();
                                retval.ConnectedLevelId = connectedLvl.Id;
                                if (sWallLightF.InsideCornerWallId(nearSide.Other) == null)
                                {
                                    // Adjust the far corner of the survey wall creation line so that
                                    // the re-created survey wall joins with the gap wall
                                    G2D.Vector adjNewWallLocV;
                                    G2D.Point sWallOppContWIntersection = G2D.Vector.Intersection(sWallLocationLine, oppContWInsideFaceV);
                                    if (sWallOppContWIntersection.DistanceTo(newWallLocV.EndPt(0))
                                        < sWallOppContWIntersection.DistanceTo(newWallLocV.EndPt(1)))
                                    { adjNewWallLocV = new G2D.Vector(sWallOppContWIntersection, newWallLocV.EndPt(1)); }
                                    else
                                    { adjNewWallLocV = new G2D.Vector(newWallLocV.EndPt(0), sWallOppContWIntersection); }

                                    surveyWallCreateLine = Utilities.ToRevitLine(adjNewWallLocV);
                                }
                            }
                        }
                    }
                }
            }

            return retval;
        }

        private void surveyWallWorker_findDegenerateWall(Level baseLvl, Wall refWall, SurveyOrientation refWallSO,
                                                         SurveySide farSide, G2D.Vector newWallRearEdgeV,
                                                         out ElementId degenerateWallId, out bool isDegenerateWallMatchRefWallSO)
        {
            degenerateWallId = null;
            isDegenerateWallMatchRefWallSO = false;

            joinedWallGeometry g = getJoinedWalls(refWall, baseLvl, farSide, refWallSO);
            if (g.ParallelWall != null)
            {
                G2D.Vector refWallRearEdge = RevitSurveyWall.WallEdge(refWall, refWallSO, false);
                G2D.Point rearCorner = G2D.Vector.Intersection(refWallRearEdge, newWallRearEdgeV);
                G2D.Vector parallelWallRearEdge = RevitSurveyWall.WallEdge(g.ParallelWall, g.ParallelWallSO, false);

                G2D.Point parallelWallFarCorner = (farSide == SurveySide.Left) ? parallelWallRearEdge.Start : parallelWallRearEdge.End;
                if (isWithinMeasurementTolerance(parallelWallFarCorner.DistanceTo(rearCorner), Distance.Zero))
                {
                    degenerateWallId = g.ParallelWall.Id;
                    isDegenerateWallMatchRefWallSO = (refWallSO == g.ParallelWallSO);
                }
            }
        }

        private void surveyWalWorker_consumeOffset(Level lvl, Wall refWall, SurveyOrientation refWallSO, SurveySide side)
        {
            WallState refWallState = getWallState(refWall);
            Distance consumedOffset = refWallState.RefPtOffset(side, refWallSO);
            if (consumedOffset != null)
            {
                if (consumedOffset < Distance.Zero)
                {
                    // When the offset is negative, it means it extends past the end of the
                    // wall into a contiguous, un-surveyed wall.  Change the survey state of 
                    // the contiguous wall to surveyed
                    joinedWallGeometry g = getJoinedWalls(refWall, lvl, side, refWallSO);
                    if (g.ParallelWall != null)
                    {
                        WallState pWallState = getWallState(g.ParallelWall);
                        pWallState.SetIsFaceSurveyed(g.ParallelWallSO);
                        setWallState(g.ParallelWall, pWallState);
                    }
                }
                else if (consumedOffset > RevitSurveyWall.Length(refWall) * 0.5)
                {
                    // If the offset is all the way over to the other side of the wall,
                    // then removing the offset means that the remainder of the wall is not
                    // surveyed; change the wall's state to reflect this
                    refWallState.ResetIsFaceSurveyed(refWallSO);
                }

                refWallState.SetRefPtOffset(side, refWallSO, null);
                setWallState(refWall, refWallState);
            }
        }

        private void surveyWallWorker_correctFarCorner(surveyFace surveyWF, SurveySide farSide, WallSurveyParams wp)
        {
            if (!wp.IsCloseWall && wp.CornerAngle != G2D.Radians.PI)
            {
                Distance offset = surveyWF.WallExtent - wp.Length;
                if (!isWithinMeasurementTolerance(offset, Distance.Zero))
                {
                    Wall farEndW = surveyWF.EndWall(farSide);
                    SurveyOrientation farEndWSO = surveyWF.EndWallSO(farSide);
                    WallState farEndWState = getWallState(farEndW);
                    farEndWState.SetRefPtOffset(farSide, farEndWSO, offset);
                    setWallState(farEndW, farEndWState);
                }
            }
        }

        private bool surveyWallWorker_calculateCloseWallGeometry(Level lvl, surveyFace wF, WallSurveyParams wsp,
                                                                 SurveySide farSide, SurveyDirection dir)
        {
            // Walk backwards from the current wall to find the closest wall that either has an offset
            // point defined or is not joined to any other wall.
            int adjacentWallCount = 0;
            G2D.Radians wAz = (new G2D.Vector(wF.Corner(farSide), wF.Corner(farSide.Other))).Azimuth;
            G2D.Vector closeV = new G2D.Vector();
            G2D.Radians closeCornerAngle = G2D.Radians.Zero;
            List<long> traversedWallIds = new List<long>();
            foreach (long wId in wF.FaceWallIDs)  { traversedWallIds.Add(wId); }

            surveyFace backWallF = wF;

            while ((backWallF = backWallF.AdjacentWallFace(farSide.Other, true, false, false)) != null)
            {
                // Avoid infinite loop by checking to see if we have traversed the same wall more than once
                foreach (long wId in backWallF.FaceWallIDs)
                {
                    if (traversedWallIds.Contains(wId))
                    { backWallF = null; break; }
                    else
                    { traversedWallIds.Add(wId); }
                }
                if (backWallF == null)  { break; }
                
                adjacentWallCount++;
                Wall backWall = backWallF.EndWall(farSide.Other);
                SurveyOrientation backWallSO = backWallF.EndWallSO(farSide.Other);

                closeV = new G2D.Vector(wF.Corner(farSide), backWallF.Corner(farSide.Other));
                if (closeV.Length < 1.0)  { backWallF = null; break; }
                closeCornerAngle = closeV.Azimuth - wAz;
                if (closeCornerAngle < G2D.Radians.Zero) { closeCornerAngle = G2D.Radians.TwoPIPlus(closeCornerAngle); }

                if (   (backWallF.InsideCornerWallId(farSide.Other) == null && backWallF.OutsideCornerWallId(farSide.Other) == null)
                    || getWallState(backWall).RefPtOffset(farSide.Other, backWallSO) != null)
                { break; }
            }

            if (backWallF != null && adjacentWallCount > 1)
            {
                // Shoot a ray along the "close vector" to see if there are any intervening walls
                List<long> excludedEndWalls = new List<long>();
                excludedEndWalls.Add(wF.EndWall(farSide).Id.Value);
                excludedEndWalls.Add(backWallF.EndWall(farSide.Other).Id.Value);
                Reference closestWallRef = surveyWallWorker_findClosestWallRef(wF.Corner(farSide), closeV.Azimuth, lvl,
                                                                               excludedEndWalls, closeV.Length, 0.0);
                if (   closestWallRef == null
                    || m_doc.GetElement(closestWallRef).Id.Equals(backWallF.EndWall(farSide.Other).Id))
                {
                    // Find the corner angle at the other end of the wall
                    G2D.Radians backWallAz = (new G2D.Vector(backWallF.Corner(farSide), backWallF.Corner(farSide.Other))).Azimuth;
                    G2D.Radians closeVAbsAz = closeCornerAngle + wAz;
                    G2D.Radians backWallCornerAngle = (closeVAbsAz > backWallAz) ?
                                                       closeVAbsAz - backWallAz :
                                                       backWallAz - closeVAbsAz;
                    if (backWallCornerAngle < G2D.Radians.Zero) { backWallCornerAngle = G2D.Radians.TwoPIPlus(backWallCornerAngle); }

                    // We can close the space if the wall endpoints are not "blind" (meaning that
                    // there is no intervening wall between the two endpoints) and if the corner angles
                    // at each end are greater than 30 degrees
                    if (closeCornerAngle > G2D.Radians.FromDegrees(30.0)
                        && closeCornerAngle < G2D.Radians.FromDegrees(330.0)
                        && backWallCornerAngle > G2D.Radians.FromDegrees(30.0)
                        && backWallCornerAngle < G2D.Radians.FromDegrees(330.0))
                    {
                        wsp.IsCloseWall = true;
                        wsp.CloseCornerAngle = (dir == SurveyDirection.LeftToRight) ? closeCornerAngle : G2D.Radians.TwoPIMinus(closeCornerAngle);
                        wsp.CloseLength = new Distance(closeV.Length, Distance.Units.RevitInternal);
                    }
                }
            }

            return true;
        }

        private void surveyWallWorker_setZoomElements(List<string> zoomElements, surveyFace f)
        {
            for (int sideIdx = 0; sideIdx < 2; sideIdx++)
            {
                SurveySide side = SurveySide.FromIndex(sideIdx);

                // Put the end walls of the current face into the set of elements to show
                string id = f.EndWall(side).Id.ToString();
                if (!zoomElements.Contains(id)) { zoomElements.Add(id); }

                // Find the "neighbor" of the wall face and add them to the list
                surveyFace neighborF = f.AdjacentWallFace(side, false, false, false);
                if (neighborF != null)
                {
                    id = neighborF.EndWall(side).Id.ToString();
                    if (!zoomElements.Contains(id)) { zoomElements.Add(id); }
                    id = neighborF.EndWall(side.Other).Id.ToString();
                    if (!zoomElements.Contains(id)) { zoomElements.Add(id); }
                }
            }
        }

        private void surveyLineWorker_findElementAtFarEndOfRefLine(revitCommandContext context, out ElementId farEndLineID, out surveyFace farEndFace)
        {
            int farEndPtIdx;
            if (   (context.SurveyOrientation == SurveyOrientation.NotFlipped && context.SurveyDirection == SurveyDirection.LeftToRight)
                || (context.SurveyOrientation == SurveyOrientation.Flipped && context.SurveyDirection == SurveyDirection.RightToLeft))
            { farEndPtIdx = 1; }
            else
            { farEndPtIdx = 0; }
            surveyLineWorker_getJoinedElement(context.RefElement, farEndPtIdx, context.CurrentLevel, out farEndLineID, out farEndFace);
        }

        private void surveyLineWorker_setZoomElements(List<string> zoomElements, Element baseEl, Level lvl)
        {
            surveyFace endF = null;

            string idAsString = baseEl.Id.Value.ToString();
            if (!zoomElements.Contains(idAsString)) { zoomElements.Add(idAsString); }

            for (int endPtIdx = 0; endPtIdx < 2; endPtIdx++)
            {
                ElementId nextJoinedElID;
                Element nextJoinedEl;
                Element prevJoinedEl = baseEl;
                surveyLineWorker_getJoinedElement(prevJoinedEl, endPtIdx, lvl, out nextJoinedElID, out endF);
                while (nextJoinedElID != null)
                {
                    idAsString = nextJoinedElID.Value.ToString();
                    if (!zoomElements.Contains(idAsString)) { zoomElements.Add(idAsString); }
                    nextJoinedEl = doc.GetElement(nextJoinedElID);
                    int joinPtIdx = RevitSurveyWall.JoinPtIdx(nextJoinedEl, prevJoinedEl, true);
                    int otherEndPtIdx = (joinPtIdx == 0) ? 1 : 0;
                    prevJoinedEl = nextJoinedEl;
                    surveyLineWorker_getJoinedElement(prevJoinedEl, otherEndPtIdx, lvl, out nextJoinedElID, out endF);
                }

                if (endF != null)
                {
                    for (int sideIdx = 0; sideIdx < 2; sideIdx++)
                    {
                        SurveySide side = SurveySide.FromIndex(sideIdx);
                        // Put the end walls of the end face into the set of elements to show
                        idAsString = endF.EndWall(side).Id.ToString();
                        if (!zoomElements.Contains(idAsString)) { zoomElements.Add(idAsString); }
                    }
                }
            }
        }

        private void surveyLineWorker_getJoinedElement(Element baseEl, int endPtIdx, Level lvl, out ElementId joinedElID, out surveyFace joinedFace)
        {
            joinedElID = null;
            joinedFace = null;

            LocationCurve locCurve = (LocationCurve)baseEl.Location;
            ElementArray joinedEls = locCurve.get_ElementsAtJoin(endPtIdx);
            if (joinedEls.Size == 1)
            {
                joinedElID = joinedEls.get_Item(0).Id;
            }
            else if (joinedEls.Size == 2)
            {
                if (joinedEls.get_Item(0).Id == baseEl.Id) { joinedElID = joinedEls.get_Item(1).Id; }
                else if (joinedEls.get_Item(1).Id == baseEl.Id) { joinedElID = joinedEls.get_Item(0).Id; }
            }
            else if (joinedEls.Size == 0)
            {
                // See if this end is "virtually" joined to a wall face (by close proximity)
                G2D.Vector locV = Utilities.To2D(baseEl.Location);
                if (endPtIdx == 0) { locV = locV.Reverse(); }

                double tol = docSettings.MeasurementTolerance.ValueRevitInternal;
                G2D.Point rayOriginBasePt = (new G2D.Vector(locV.Start, locV.Azimuth, locV.Length - tol)).End;
                foreach (double offsetSideFactor in new double[2] { -1.0, 1.0 })
                {
                    G2D.Radians faceOffsetAz = locV.Azimuth + new G2D.Radians((Math.PI / 2.0) * offsetSideFactor);
                    G2D.Point rayOrigin = (new G2D.Vector(rayOriginBasePt, faceOffsetAz, tol)).End;
                    Reference closestRef = surveyWallWorker_findClosestWallRef(rayOrigin, locV.Azimuth, lvl, null, tol * 2.0, 0.0);
                    if (closestRef != null)
                    {
                        Wall joinedW;
                        G2D.Vector dummyV;
                        SurveyOrientation joinedWSO;
                        surveyWallWorker_getWallAndEdgeFromRef(closestRef, out joinedW, out dummyV, out joinedWSO);
                        if (joinedW != null)
                        {
                            G2D.Point refPt = Utilities.To2D(closestRef.GlobalPoint);
                            surveyFace.CreateFaceNoModifyWalls(this, lvl, joinedW, joinedWSO, refPt, out joinedFace);
                            break;
                        }
                    }
                }
            }
        }

        private void constructSurveyWallHostedObjectData(revitCommandContext context, surveyFace f, string familyCategory,
                                                         out WallHostedObjectSurveyParams whosParams)
        {
            
            whosParams = documentState.GetSurveyParamsCacheByCategory(this, familyCategory);
            wallHostedInstanceDefinition.ReconcileSurveyFamilyTypeDefs(this, familyCategory, whosParams);

            whosParams.HostWallHeight = RevitSurveyWall.Height((Wall)context.RefElement);
            whosParams.HostWallLength = f.WallExtent;
            whosParams.Location = Distance.NoValue;  // No default for location

            if (familyCategory.IsWindows())
            {
                // Get the typical window height and sill height for this level if defined
                LevelsData levelsData = fetchData<LevelsData>();
                List<WindowHeights> typicalWindowHeights = levelsData.LevelDataAt(context.CurrentLevelIdx).TypicalWindowHeights;
                if (typicalWindowHeights.Count > 0)
                {
                    whosParams.IsTypicalWindowHeights = true;
                    whosParams.TypicalHeight = typicalWindowHeights[0].Height;
                    whosParams.TypicalSillHeight = typicalWindowHeights[0].SillHeight;
                    if (typicalWindowHeights.Count > 1) { whosParams.IsStackedWindows = true; }
                }
                else
                {
                    whosParams.IsTypicalWindowHeights = false;
                    whosParams.IsStackedWindows = false;
                }
            }
        }

        private Wall copyObjectsWorker_findNewHostWall(wallHostedInstanceDefinition def, Level toLvl)
        {
            Wall retval = null;

            XYZ rayOrigin = Utilities.To3D(def.AbsoluteLocationXY, def.Level.Elevation);
            XYZ rayDir = new XYZ(0.0, 0.0, (toLvl.Elevation > def.Level.Elevation) ? 1.0 : -1.0);
            ElementClassFilter wallFilter = new ElementClassFilter(typeof(Wall));
            ReferenceIntersector refIntersector = new ReferenceIntersector(wallFilter, FindReferenceTarget.Face, view3D);
            foreach (ReferenceWithContext iRefWC in refIntersector.Find(rayOrigin, rayDir))
            {
                Wall refW = doc.GetElement(iRefWC.GetReference()) as Wall;
                if (refW != null && refW.LevelId == toLvl.Id)
                { retval = refW; break; }
            }

            return retval;
        }

        private wallDefinition convertUWWallsWorker_calcUWFillerWallDef(Wall delW, SurveyOrientation delWSO, G2D.Vector delWCL,
                                                                        WallType uwWT, G2D.Point delWOutsideEdgeCursor, G2D.Point gapEndPt)
        {
            G2D.Vector locLineV = new G2D.Vector(delWCL.Project(delWOutsideEdgeCursor), delWCL.Project(gapEndPt));
            if (delWSO == SurveyOrientation.Flipped) { locLineV = locLineV.Reverse(); }
            Curve locLine = Utilities.ToRevitLine(locLineV);

            wallDefinition def = new wallDefinition(this, delW);
            def.LocLine = locLine;
            def.WallTypeId = uwWT.Id;

            // Only restore those wall hosted instance definitions that fall on the location line
            // of the wall to be created
            def.HostedInstances = new List<wallHostedInstanceDefinition>();
            foreach (FamilyInstance inst in RevitSurveyWall.GetHostedInstances(this.doc, delW))
            {
                if (Utilities.IsPointOnLineSegment(locLineV, Utilities.To2D((inst.Location as LocationPoint).Point)))
                { def.HostedInstances.Add(new wallHostedInstanceDefinition(this, inst)); }
            }
            return def;
        }

        private void copyWall(Wall w, Level toLvl, Level nextLvl, out Wall copiedWall, Dictionary<long, long> restoredInstanceMap)
        {
            wallDefinition def = new wallDefinition(this, w);
            def.WallState = new WallState();
            def.BaseLevelId = toLvl.Id;
            Level connectedLvl = (RevitSurveyWall.ConnectedLevel(doc, w) != null) ? nextLvl : null;
            def.ConnectedLevelId = (connectedLvl != null) ? connectedLvl.Id : null;
            def.HostedInstances = null;
            def.CreateNewWall(this, out copiedWall, restoredInstanceMap);
        }

        private bool surveyExistingFace(Level lvl, surveyFace f, WallSurveyParams wp,
                                        ReferenceOpeningParams op, SurveyDirection dir, out surveyFace newF,
                                        Dictionary<long, long> restoredInstanceMap)
        {
            Wall endWall = f.EndWall(dir.NearSide);

            bool isExistingTxn = isInTransaction;
            if (!isExistingTxn) { startTransaction(@"surveyExistingFace_matchToLevel"); }

            surveyFace.CreateFaceModifyWalls(this, lvl, ref endWall, f.EndWallSO(dir.NearSide), dir.NearSide, out newF, restoredInstanceMap);

            Wall locatedW; SurveyOrientation locatedWSO;
            G2D.Point absLocOnFrontEdge; G2D.Point absLocOnCenterline;
            Level topLvl = (wp.ConnectedLevelIdx != -1) ? getLevels()[wp.ConnectedLevelIdx] : null;

            if (!newF.BindToLevels(lvl, topLvl, out newF, restoredInstanceMap))
            { return false; }

            if (!commitTransaction()) { return false; }

            if (op != null && op.ReferenceOpeningID != @"")
            {
                // Determine whether the original opening instance has been replaced during the creation of the face
                long origRefOpeningId = long.Parse(op.ReferenceOpeningID);
                long refOpeningId = (restoredInstanceMap.ContainsKey(origRefOpeningId)) ? restoredInstanceMap[origRefOpeningId] : origRefOpeningId;

                startTransaction(@"locateNearCorner");

                FamilyInstance refOpeningInst = (FamilyInstance)m_doc.GetElement(new ElementId(refOpeningId));

                // Clear the IsSurveyed state of the wall face
                newF.ResetIsFaceSurveyed();
                Distance farCornerToNewNearCorner = newF.WallExtent
                                - (newF.CornerToOpening(refOpeningInst, dir.NearSide) - op.CornerToOpening);

                newF.LocateInWall(farCornerToNewNearCorner, dir.FarSide, op.LevelOffset, true, true,
                                  out locatedW, out locatedWSO, out absLocOnFrontEdge, out absLocOnCenterline, restoredInstanceMap);
                if (!commitTransaction()) { return false; }

                surveyFace.CreateFaceNoModifyWalls(this, lvl, locatedW, locatedWSO, absLocOnFrontEdge, out newF);

                startTransaction(@"locateFarCorner");
                // Reset the surveyed state so we can locate the other corner
                newF.ResetIsFaceSurveyed();
                newF.LocateInWall(op.WallExtent, dir.NearSide, op.LevelOffset, true, true,
                                  out locatedW, out locatedWSO, out absLocOnFrontEdge, out absLocOnCenterline, restoredInstanceMap);
                if (!commitTransaction()) { return false; }

                if (isExistingTxn)
                { startTransaction(@"post_locateFarCorner"); }

                surveyFace.CreateFaceNoModifyWalls(this, lvl, locatedW, locatedWSO, absLocOnFrontEdge, out newF);
            }
            else
            {
                startTransaction(@"locateFarCorner");
                newF.LocateInWall(wp.Length, dir.NearSide, wp.LevelOffset, true, true,
                                  out locatedW, out locatedWSO, out absLocOnFrontEdge, out absLocOnCenterline, restoredInstanceMap);
                if (!commitTransaction()) { return false; }

                if (isExistingTxn)
                { startTransaction(@"post_locateFarCorner"); }

                surveyFace.CreateFaceNoModifyWalls(this, lvl, locatedW, locatedWSO, absLocOnFrontEdge, out newF);
            }

            return true;
        }

        private void splitWall(Wall w, G2D.Point location, out Wall[] splitWalls, Dictionary<long, long> restoredInstanceMap)
        {
            splitWalls = new Wall[2];

            Line[] splitLines = new Line[2];
            Curve wLocCurve = ((LocationCurve)w.Location).Curve;
            XYZ wLocEndPt0 = wLocCurve.GetEndPoint(0);
            XYZ wLocEndPt1 = wLocCurve.GetEndPoint(1);
            XYZ splitPt = Utilities.To3D(location, wLocEndPt0.Z);

            splitLines[0] = Line.CreateBound(wLocEndPt0, splitPt);
            splitLines[1] = Line.CreateBound(splitPt, wLocEndPt1);

            // Save info from any elements hosted by the wall so they can be re-created
            List<wallHostedInstanceDefinition>[] hostedObjectsToRestore = { new List<wallHostedInstanceDefinition>(),
                                                                            new List<wallHostedInstanceDefinition>() };
            foreach (FamilyInstance inst in RevitSurveyWall.GetHostedInstances(doc, w))
            {
                XYZ locPt = ((LocationPoint)inst.Location).Point;
                int matchLineIdx;
                if (splitLines[0].Project(locPt).Distance < splitLines[1].Project(locPt).Distance)
                { matchLineIdx = 0; }
                else
                { matchLineIdx = 1; }
                hostedObjectsToRestore[matchLineIdx].Add(new wallHostedInstanceDefinition(this, inst));
            }

            // Grab the state of the wall before we delete it
            wallDefinition splitWallDef = new wallDefinition(this, w);
            WallState origWS = new WallState(splitWallDef.WallState);

            // Delete the original wall...
            deleteWall(w);

            // ...then create the two split walls in its place
            for (int i = 0; i < 2; i++)
            {
                splitWallDef.LocLine = splitLines[i];

                // Clear any reference point offsets at the split point
                WallState splitWS = new WallState(origWS);
                if (i == 0)
                {
                    splitWS.SetRefPtOffset(SurveySide.Left, SurveyOrientation.Flipped, null);
                    splitWS.SetRefPtOffset(SurveySide.Right, SurveyOrientation.NotFlipped, null);
                }
                else
                {
                    splitWS.SetRefPtOffset(SurveySide.Left, SurveyOrientation.NotFlipped, null);
                    splitWS.SetRefPtOffset(SurveySide.Right, SurveyOrientation.Flipped, null);
                }
                splitWallDef.WallState = splitWS;

                // Set the subset of hosted instances on the split wall
                splitWallDef.HostedInstances = hostedObjectsToRestore[i];

                splitWallDef.CreateNewWall(this, out splitWalls[i], restoredInstanceMap);
            }
        }

        private bool isSplitWallAtLevels(Wall w, SurveyOrientation wSO, Level lowerLvl, Level upperLvl, out Wall newBaseWall, Dictionary<long, long> restoredInstanceMap)
        {
            bool retval = false;
            newBaseWall = null;

            Level wConnectedLvl = RevitSurveyWall.ConnectedLevel(doc, w);
            ElementId wConnectedLvlId = (wConnectedLvl == null) ? null : wConnectedLvl.Id;
            Distance wBaseElev = RevitSurveyWall.BaseElevation(w);
            Distance wTopElev = RevitSurveyWall.TopElevation(w);

            Distance lowerLvlElev = new Distance(lowerLvl.Elevation, Distance.Units.RevitInternal);
            Distance upperLvlElev = (upperLvl != null) ? new Distance(upperLvl.Elevation, Distance.Units.RevitInternal) : wTopElev;

            if (lowerLvlElev > wBaseElev || upperLvlElev < wTopElev)
            {
                retval = true;

                // Save the info needed to restore any family instance hosted by this wall
                List<wallHostedInstanceDefinition> instancesToRestore = new List<wallHostedInstanceDefinition>();
                foreach (FamilyInstance inst in RevitSurveyWall.GetHostedInstances(doc, w))
                { instancesToRestore.Add(new wallHostedInstanceDefinition(this, inst)); }

                // Grab the state of the wall before we delete it
                Curve origWallCurve = ((LocationCurve)w.Location).Curve;
                WallType wt = w.WallType;
                ElementId wLvlId = w.LevelId;
                WallState newWallState = new WallState(getWallState(w));
                RevitSurveyWall.LocationLineType locLineType = RevitSurveyWall.GetLocLineType(w);
                Distance wBaseOffset = RevitSurveyWall.BaseOffset(w);
                SurveyOrientation oppSO = new SurveyOrientation(wSO); oppSO.Flip();

                Curve underWallCurve = null;
                Curve newBaseWallCurve = null;
                Curve overWallCurve = null;

                if (lowerLvlElev > wBaseElev)
                {
                    // We need to split the wall at the lower level
                    underWallCurve = origWallCurve;
                    Transform t = Transform.CreateTranslation(new XYZ(0.0, 0.0, lowerLvl.Elevation - wBaseElev.ValueRevitInternal));
                    newBaseWallCurve = origWallCurve.CreateTransformed(t);
                }

                if (upperLvlElev < wTopElev)
                {
                    if (newBaseWallCurve == null) { newBaseWallCurve = origWallCurve; }
                    Transform t = Transform.CreateTranslation(new XYZ(0.0, 0.0, upperLvl.Elevation - wBaseElev.ValueRevitInternal));
                    overWallCurve = origWallCurve.CreateTransformed(t);
                }

                deleteWall(w);

                // Clear all state from the face currently being surveyed
                newWallState.ResetIsFaceSurveyed(wSO);
                newWallState.SetLevelOffset(wSO, Distance.NoValue);
                for (int sideIdx = 0; sideIdx < 2; sideIdx++)
                { newWallState.SetRefPtOffset(SurveySide.FromIndex(sideIdx), wSO, null); }

                Distance height;
                wallDefinition wallDef;

                Wall underWall = null;
                if (underWallCurve != null)
                {
                    height = lowerLvlElev - wBaseElev;
                    wallDef = new wallDefinition(underWallCurve, wt, locLineType, wLvlId, lowerLvl.Id, height, wBaseOffset, newWallState);
                    wallDef.CreateNewWall(this, out underWall, restoredInstanceMap);

                    // Clear the level offset on the opposite face for split walls above
                    newWallState.SetLevelOffset(oppSO, Distance.NoValue);
                }
                
                height = (overWallCurve == null) ? wTopElev - lowerLvlElev : upperLvlElev - lowerLvlElev;
                ElementId connectedLvlId = (overWallCurve == null) ? wConnectedLvlId : upperLvl.Id;
                wallDef = new wallDefinition(newBaseWallCurve, wt, locLineType, lowerLvl.Id, connectedLvlId, height, Distance.Zero, newWallState);
                wallDef.CreateNewWall(this, out newBaseWall, restoredInstanceMap);

                // Clear the level offset on the opposite face for split walls above
                newWallState.SetLevelOffset(oppSO, Distance.NoValue);

                Wall overWall = null;
                if (overWallCurve != null)
                {
                    height = wTopElev - upperLvlElev;
                    wallDef = new wallDefinition(overWallCurve, wt, locLineType, upperLvl.Id, wConnectedLvlId, height, Distance.Zero, newWallState);
                    wallDef.CreateNewWall(this, out overWall, restoredInstanceMap);
                }

                foreach (wallHostedInstanceDefinition def in instancesToRestore)
                {
                    FamilyInstance restoredInst;
                    Wall newHostWall;
                    Distance instAbsElev = def.AbsElevation;
                    if (underWall != null && instAbsElev < lowerLvlElev)
                    { newHostWall = underWall; }
                    else if (overWall != null && instAbsElev >= upperLvlElev)
                    { newHostWall = overWall; }
                    else
                    { newHostWall = newBaseWall; }
                    restoredInst = def.CreateInstanceOnNewHostWallAbsolute(this, newHostWall);

                    if (def.OriginalElementId != -1)
                    {
                        if (restoredInstanceMap.ContainsValue(def.OriginalElementId))
                        {
                            foreach (int key in restoredInstanceMap.Keys)
                            { if (restoredInstanceMap[key] == def.OriginalElementId)  { restoredInstanceMap[key] = restoredInst.Id.Value; break; } }
                        }
                        else
                        { restoredInstanceMap.Add(def.OriginalElementId, restoredInst.Id.Value); }
                    }
                }
            }

            return retval;
        }

        private List<double> rayElevations(Level lvl)
        {
            List<double> rayElevations = new List<double>();

            double floorElevation = lvl.Elevation;
            rayElevations.Add(floorElevation + docSettings.WallIntersectFloorElevationOffset.ValueRevitInternal);

            double ceilingHeight = 0.0;
            Distance ceilingHeightD = levelVisibleHeight(lvl);
            if (ceilingHeightD != null)
            {
                ceilingHeight = ceilingHeightD.ValueRevitInternal;
                rayElevations.Add(floorElevation + ceilingHeight - docSettings.FullOpeningTopElevationOffset.ValueRevitInternal);
            }
            if (ceilingHeight > docSettings.CutPlaneElevation.ValueRevitInternal)
            { rayElevations.Add(floorElevation + docSettings.CutPlaneElevation.ValueRevitInternal); }

            return rayElevations;
        }

        private bool isDarkWallFace(Wall w, SurveyOrientation so)
        {
            bool retval = false;

            if (RevitSurveyWall.IsUnknownWidth(w))
            {
                WallState ws = getWallState(w);
                if (!ws.IsFaceSurveyed(so) && ws.IsFaceSurveyed(so.Other))
                { retval = true; }
            }

            return retval;
        }

        private List<WallType> getWallTypes()
        {
            List<WallType> retval = new List<WallType>();
            foreach (Element el in FamilyCategoryUtils.GetAllTypesInCategory(doc, FamilyCategory.Walls))
            { if (el is WallType) { retval.Add(el as WallType); } }
            return retval;
        }

        private WallType matchingWallType(Distance width)
        {
            WallType retval = null;
            List<WallType> wallTypes = getWallTypes();

            // Handle special case: if width is not a value, return the unknown width wall type
            if (width.IsNoValue)
            {
                double minWidth = 100.0;
                foreach (WallType wt in wallTypes)
                {
                    if (RevitSurveyWall.IsUnknownWidth(wt) && wt.Width < minWidth)
                    { retval = wt; minWidth = wt.Width; }
                }
            }

            // Find the first matching wall type (by width) if already present in project
            if (retval == null)
            {
                foreach (WallType wt in wallTypes)
                {
                    if (Math.Round(wt.Width, 5) == Math.Round(width.ValueRevitInternal, 5))
                    { retval = wt; break; }
                }
            }

            // No matching wall type found; create one
            if (retval == null)
            { retval = createWallType(width, wallTypes); }

            return retval;
        }

        private WallType createWallType(Distance width, List<WallType> existingWallTypes)
        {
            WallType retval = null;

            // Arbitrarily choose a wall type that is _not_ unknown width to duplicate
            WallType templateWT = null;
            List<string> wtNames = new List<string>();
            foreach (WallType wt in existingWallTypes)
            {
                wtNames.Add(wt.Name);
                if (!RevitSurveyWall.IsUnknownWidth(wt)) { templateWT = wt; }
            }

            // Set the new wall type's name
            if (templateWT != null)
            {
                string wtWidthName;
                if (!width.IsNoValue)
                { wtWidthName = width.RenderShortDistance(UnitSystem) + Distance.ShortDistanceUnitsLabel(UnitSystem); }
                else
                { wtWidthName = @"Unknown Width"; }

                string wtName = @"PKNail " + wtWidthName;
                if (wtNames.Contains(wtName))
                {
                    // The new wall type has the same name as an existing wall type; disambiguate it
                    int disambiguateNum = 1;
                    string disambiguateName = wtName;
                    while (wtNames.Contains(disambiguateName))
                    { disambiguateNum++; disambiguateName = wtName + @" (" + disambiguateNum.ToString() + @")"; }
                    wtName = disambiguateName;
                }

                // Create a duplicate of the template wall type
                retval = (WallType)templateWT.Duplicate(wtName);

                // Set the width of the wall (there should only be one structural layer and all other layers should have
                // zero width)
                CompoundStructure cs = retval.GetCompoundStructure();
                List<CompoundStructureLayer> layers = new List<CompoundStructureLayer>(cs.GetLayers());
                foreach (CompoundStructureLayer layer in layers)
                {
                    if (layer.Function == MaterialFunctionAssignment.Structure)
                    {
                        if (width.IsNoValue)
                        {
                            if (UnitSystem == PKNailForRevit.UnitSystem.Imperial)
                            { layer.Width = (new Distance(1.0 / 12.0, Distance.Units.Imperial)).ValueRevitInternal; }  // One inch
                            else
                            { layer.Width = (new Distance(30.0, Distance.Units.MetricMillimeter)).ValueRevitInternal; }  // 30mm
                        }
                        else
                        { layer.Width = width.ValueRevitInternal; }
                        break;
                    }
                }
                cs.SetLayers(layers);
                retval.SetCompoundStructure(cs);

                // If the new wall type is the unknown width wall type, set the parameter value
                if (width.IsNoValue)
                { PKNailParameter.WallTypeUnknownWidth.SetInteger(retval, 1); }
            }

            return retval;
        }

        private void startTransaction(string name)
        {
            m_isTrialTxn = false;
            startTxnWorker(name, new failuresPreprocessor(this, m_txnErrorSignal));
        }

        private void startTransaction(string name, IFailuresPreprocessor fp)
        {
            m_isTrialTxn = false;
            startTxnWorker(name, fp);
        }

        private void startTrialTransaction(string name)
        {
            m_isTrialTxn = true;
            startTxnWorker(name, new failuresPreprocessor(this, m_txnErrorSignal));
        }

        private void startTxnWorker(string name, IFailuresPreprocessor fp)
        {
            if (m_txnGrp != null)
            {
                if (m_txn == null)
                {
                    m_txn = new Transaction(doc, name);
                    FailureHandlingOptions opts = m_txn.GetFailureHandlingOptions();
                    opts.SetFailuresPreprocessor(fp);
                    m_txn.SetFailureHandlingOptions(opts);
                    if (m_txn.Start() != TransactionStatus.Started)
                    { throw new HardException(@"Transaction did not start successfully"); }
                }
                else
                { throw new HardException(@"Attempting to start a nested transaction"); }
            }
            else
            { throw new HardException(@"Attempting to start a transaction outside a transaction group"); }
        }

        private bool isInTransaction { get { return (m_txn != null); } }

        private bool commitTransaction()
        {
            bool retval = true;

            if (m_txn != null)
            {
                m_txn.Commit();
                TransactionStatus status = m_txn.GetStatus();
                retval = (status == TransactionStatus.Committed);
            }
            else
            { throw new HardException(@"Called ""commit()"" outside an active transaction"); }

            m_txn = null;
            return retval;
        }

        private bool commitExistingAndStartNewTransaction(string name)
        {
            bool retval = false;

            if (m_txnGrp != null)
            {
                if (m_txn != null)
                {
                    m_txn.Commit();
                    TransactionStatus status = m_txn.GetStatus();
                    retval = (status == TransactionStatus.Committed);
                    m_txn = null;
                }
                startTxnWorker(name, new failuresPreprocessor(this, m_txnErrorSignal));
            }
            else
            { throw new HardException(@"Attempting to start a transaction outside a transaction group"); }

            return retval;
        }

        private void rollBackTransaction()
        {
            if (!m_isTrialTxn)
            { throw new HardException(@"Attempting to roll back a non-trial transaction"); }
            if (m_txn.RollBack() != TransactionStatus.RolledBack)
            { throw new HardException(@"Transaction did not rollback successfully"); }

            m_txn = null;
        }

        private bool isWithinMeasurementTolerance(Distance d1, Distance d2)
        {
            Distance diff = (d1 > d2) ? d1 - d2 : d2 - d1;
            return (diff < docSettings.MeasurementTolerance);
        }

        private View3D view3D
        {
            get
            {
                if (m_view3D == null)
                {
                    FilteredElementCollector coll = new FilteredElementCollector(doc).OfClass(typeof(View3D));
                    foreach (Element vEl in coll.ToElements())
                    {
                        View3D curView = (View3D)vEl;
                        if (!curView.IsTemplate && !curView.IsPerspective) { m_view3D = curView; break; }
                    }
                    if (m_view3D == null) { throw new HardException("No default 3D view in document"); }
                }
                return m_view3D;
            }
        }

        private DocumentSettings docSettings
        {
            get
            {
                if (m_docSettings == null)
                {
                    m_docSettings = fetchObjOfType<DocumentSettings>(doc.ProjectInformation, false);
                    if (m_docSettings == null)
                    { m_docSettings = new DocumentSettings(unitSystem); }
                }
                return m_docSettings;
            }
        }

        private UnitSystem unitSystem
        { get { return (doc.DisplayUnitSystem == DisplayUnit.IMPERIAL) ? UnitSystem.Imperial : UnitSystem.Metric; } }

        private class levelInfo
        {
            internal string Name;
            internal string Id;
            internal double Elevation;

            internal levelInfo(string n, string i, double e)
            { Name = n; Id = i; Elevation = e; }
        }

        private List<Level> getLevels()
        {
            List<Level> retval = new List<Level>();

            FilteredElementCollector coll = new FilteredElementCollector(doc).OfClass(typeof(Level));
            foreach (Element lvlEl in coll.ToElements())
            {
                Level lvl = (Level)lvlEl;
                double elev = lvl.Elevation;

                if (retval.Count == 0)
                { retval.Add(lvl); }
                else
                {
                    for (int i = 0; i < retval.Count; i++)
                    {
                        if (retval[i].Elevation > elev)
                        { retval.Insert(i, lvl); break; }
                        if (i == retval.Count - 1)
                        { retval.Add(lvl); break; }
                    }
                }
            }

            return retval;
        }

        private void updateLevelsDataFromDoc(LevelsData levelsData)
        {
            // Synchronize page data with levels in Revit
            List<Level> levels = getLevels();
            List<Level> newLevels = new List<Level>();
            List<LevelDataItem> orphanData = new List<LevelDataItem>(levelsData.Levels);

            foreach (Level lvl in levels)
            {
                bool isFoundInLevelsData = false;
                string lvlID = lvl.Id.ToString();
                foreach (LevelDataItem ld in levelsData.Levels)
                {
                    if (ld.ElementId == lvlID)
                    {
                        isFoundInLevelsData = true;

                        // Reconcile any changes from the doc to the data
                        if (ld.Name != lvl.Name)
                        { ld.Name = lvl.Name; }
                        if (ld.ProjectElev.ValueRevitInternal != lvl.Elevation)
                        { ld.ProjectElev = new Distance(lvl.Elevation, Distance.Units.RevitInternal); }

                        // Remove the leveldata from the list of "orphans"
                        if (orphanData.Contains(ld)) { orphanData.Remove(ld); }

                        break;
                    }
                }

                if (!isFoundInLevelsData) { newLevels.Add(lvl); }
            }

            foreach (Level lvl in newLevels)
            { levelsData.InsertLevel(lvl.Name, lvl.Id.ToString(), new Distance(lvl.Elevation, Distance.Units.RevitInternal)); }

            foreach (LevelDataItem ld in orphanData)
            { levelsData.Levels.Remove(ld); }
        }

        private void updateDocFromLevelsData(LevelsData levelsData)
        {
            foreach (LevelDataItem ld in levelsData.Levels)
            {
                if (ld.ElementId == "")
                {
                    if (!ld.ProjectElev.IsNoValue)
                    {
                        // Create a new level from the floor to floor elevation
                        double levelElevation = ld.ProjectElev.ValueRevitInternal;
                        Level newLevel = Level.Create(doc, levelElevation);
                        Parameter elevationPara = newLevel.get_Parameter(BuiltInParameter.LEVEL_ELEV);
                        elevationPara.SetValueString(levelElevation.ToString());
                        newLevel.Name = ld.Name;
                        ld.ElementId = newLevel.Id.ToString();

                        // Create a matching view for the new level
                        ElementId viewFamilyTypeId = null;
                        foreach (Element el in new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).ToElements())
                        {
                            ViewFamilyType viewPlanType = el as ViewFamilyType;
                            if (viewPlanType.ViewFamily == ViewFamily.FloorPlan) { viewFamilyTypeId = viewPlanType.Id; break; }
                        }
                        if (viewFamilyTypeId != null)
                        {
                            ViewPlan newLevelView = ViewPlan.Create(doc, viewFamilyTypeId, newLevel.Id);
                            newLevelView.Name = newLevel.Name;
                        }
                    }
                }
                else
                {
                    // Check for updates to level
                    ElementId id = new ElementId(Int64.Parse(ld.ElementId));
                    Level existingLvl = doc.GetElement(id) as Level;
                    if (existingLvl.Name != ld.Name)
                    { existingLvl.Name = ld.Name; }
                    if (existingLvl.Elevation != ld.ProjectElev.ValueRevitInternal)
                    { existingLvl.Elevation = ld.ProjectElev.ValueRevitInternal; }
                }
            }
        }

        private static void parseKeymapString(string keymapStr, out string keymapPage, out KeypadCommand rfCmd)
        {
            keymapPage = @"";
            rfCmd = null;

            char[] delim = {':'};
            string[] split = keymapStr.Split(delim);
            if (split.Length >= 2 && split.Length <= 3)
            {
                keymapPage = split[0];
                KeypadCommand.KeyDef keyDef = KeypadCommand.KeyDefFromString(split[1]);
                int numClicks = (split.Length == 3) ? int.Parse(split[2]) : 1;
                rfCmd = new KeypadCommand(keyDef, numClicks);
            }
        }

        private T fetchData<T>() where T : JsonSerializable
        {
            T retval = null;

            ProjectInfo pi = doc.ProjectInformation;
            if (pi != null)
            {
                retval = fetchObjOfType<T>(doc.ProjectInformation, true);

                if (typeof(T) == typeof(LevelsData))
                {
                    LevelsData ld = retval as LevelsData;
                    updateLevelsDataFromDoc(ld);
                    ld.RecalculateFloor2FloorElevations();
                }
            }

            return retval;
        }

        private Object fetchData(Type t)
        {
            return (Object)fetchObjOfType(t, doc.ProjectInformation, true);
        }

        private T fetchObjOfType<T>(Element boundElement, bool createIfNone) where T : JsonSerializable
        {
            return (T)fetchObjOfType(typeof(T), boundElement, createIfNone);
        }

        // Maps class names to their historical shared parameter storage names (handles class renames)
        private static readonly Dictionary<string, string> s_typeToParamName = new Dictionary<string, string>
        {
            { "DocumentSettings", "Settings" }
        };

        private static string GetStoredParamName(Type t)
        {
            string name;
            return s_typeToParamName.TryGetValue(t.Name, out name) ? name : t.Name;
        }

        private JsonSerializable fetchObjOfType(Type t, Element boundElement, bool createIfNone)
        {
            JsonSerializable retval = null;

            string storedName = GetStoredParamName(t);
            Parameter p = PKNailParameter.GetNamedParameter(boundElement, storedName);
            if (p == null)  { throw new HardException(@"Parameter ""PKNail " + storedName + @""" is undefined"); }
            string serialized = p.AsString();
            if (serialized != null && serialized.Length != 0)
            { retval = (JsonSerializable)JsonSerializable.FromJson(t, serialized); }
            else if (createIfNone)
            {
                System.Reflection.ConstructorInfo ctor = t.GetConstructor(System.Type.EmptyTypes);
                retval = (JsonSerializable)ctor.Invoke(null);
            }

            return retval;
        }

        private void storeData<T>(T obj) where T : JsonSerializable
        {
            if (typeof(T) == typeof(LevelsData))
            { updateDocFromLevelsData(obj as LevelsData); }
            storeObject(doc.ProjectInformation, obj);
        }

        private void storeData(Type t, JsonSerializable obj)
        {
            storeObject(doc.ProjectInformation, obj);
        }

        private void storeObject(Element boundElement, JsonSerializable obj)
        {
            string serialized = obj.ToJson();
            string paramName = GetStoredParamName(obj.GetType());
            Parameter p = PKNailParameter.GetNamedParameter(boundElement, paramName);
            if (p == null)  { throw new HardException(@"Parameter PKNail """ + paramName + @""" is undefined"); }
            p.Set(serialized);
        }

        private Distance levelVisibleHeight(Level lvl)
        {
            Distance retval = Distance.NoValue;

            LevelsData levelsData = fetchData<LevelsData>();
            if (levelsData != null)
            {
                foreach (LevelDataItem item in levelsData.Levels)
                {
                    if (item.ElementId == lvl.Id.ToString())
                    {
                        if (!item.Floor2DropCeilingElev.IsNoValue)
                        { retval = item.Floor2DropCeilingElev; }
                        else if (!item.Floor2CeilingElev.IsNoValue)
                        { retval = item.Floor2CeilingElev; }
                        break;
                    }
                }
            }

            if (retval.IsNoValue)
            {
                List<Level> levels = getLevels();
                for (int i = 0; i < levels.Count - 1; i++)
                {
                    if (levels[i].Id.Equals(lvl.Id))
                    {
                        retval = new Distance(levels[i + 1].Elevation - lvl.Elevation, Distance.Units.RevitInternal);
                        break;
                    }
                }
            }

            return retval;
        }

        private Document doc { get { return m_doc; } }

        private Application app { get { return m_revitApp; } }

        private void initializeDocument() { }

        private class preserveGeometryInfo
        {
            private RevitDocument m_revitDoc;
            private Level m_baseLvl;

            private Wall[] m_joinedMiddleWalls;
            private Dictionary<Wall, int>[] m_joinedCornerWalls;

            private List<long> m_preserveCornerWallIds;
            private List<SurveySide> m_preserveCornerSides;
            private List<SurveyOrientation> m_preserveCornerSOs;
            private List<G2D.Point> m_preserveCornerPoints;

            private Dictionary<long, wallDefinition> m_wDefsToMove;

            internal preserveGeometryInfo(RevitDocument revitDoc, Level baseLvl)
            {
                m_revitDoc = revitDoc;
                m_baseLvl = baseLvl;

                m_joinedMiddleWalls = new Wall[2];
                m_joinedCornerWalls = new Dictionary<Wall, int>[2] { new Dictionary<Wall, int>(), new Dictionary<Wall, int>() };

                m_preserveCornerWallIds = new List<long>();
                m_preserveCornerSides = new List<SurveySide>();
                m_preserveCornerSOs = new List<SurveyOrientation>();
                m_preserveCornerPoints = new List<G2D.Point>();

                m_wDefsToMove = new Dictionary<long, wallDefinition>();
            }

            internal void FindWallsJoinedToSurveyWall(Wall surveyWall)
            {
                for (int sWallEndPtIdx = 0; sWallEndPtIdx < 2; sWallEndPtIdx++)
                {
                    ElementArray joinedWalls = ((LocationCurve)surveyWall.Location).get_ElementsAtJoin(sWallEndPtIdx);
                    foreach (Element jWE in joinedWalls)
                    {
                        Wall jW = jWE as Wall;
                        if (   jW == null
                            || jW.Id.Equals(surveyWall.Id)
                            || !RevitSurveyWall.IsOnLevel(jW, m_baseLvl)
                            || RevitSurveyWall.Azimuth(jW, SurveyOrientation.NotFlipped).IsParallelTo(RevitSurveyWall.Azimuth(surveyWall, SurveyOrientation.NotFlipped)))
                        { continue; }

                        // If the survey wall is not joined to either end of the joined wall, that means that the
                        // survey wall is joined in the middle
                        bool isJoinedAtCorner = false;
                        for (int jWJoinPtIdx = 0; jWJoinPtIdx < 2; jWJoinPtIdx++)
                        {
                            ElementArray joinedToJoined = ((LocationCurve)jW.Location).get_ElementsAtJoin(jWJoinPtIdx);
                            foreach (Element j2jWE in joinedToJoined)
                            {
                                if (j2jWE.Id.Equals(surveyWall.Id))
                                {
                                    isJoinedAtCorner = true;
                                    m_joinedCornerWalls[sWallEndPtIdx].Add(jW, jWJoinPtIdx);
                                    break;
                                }
                            }
                        }

                        if (!isJoinedAtCorner)  // ...
                        {
                            // ... then the new wall joins in the middle.  We will split the wall before
                            // re-creating the new survey wall.
                            m_joinedMiddleWalls[sWallEndPtIdx] = jW;
                        }
                    }
                }
            }

            internal bool SplitJoinedWalls(G2D.Vector newWallLocLine, double newWallWidth, Wall refWall, Level connectedLvl,
                                           Dictionary<long, long> restoredInstanceMap)
            {
                for (int sWallEndPtIdx = 0; sWallEndPtIdx < 2; sWallEndPtIdx++)
                {
                    Wall jMW = m_joinedMiddleWalls[sWallEndPtIdx];
                    if (jMW != null)
                    {
                        // Figure out the survey orientation of the middle wall (side that the survey wall joins on)
                        SurveyOrientation jMWSO = new SurveyOrientation(SurveyOrientation.NotFlipped);
                        G2D.Point newWallLocOnJoinedMiddleWall = RevitSurveyWall.LocationLine(jMW).Project(newWallLocLine.MidPoint);
                        G2D.Vector newWallDirectionV = new G2D.Vector(newWallLocOnJoinedMiddleWall, newWallLocLine.MidPoint);
                        XYZ notFlippedOrientation = RevitSurveyWall.Orientation(jMW, SurveyOrientation.NotFlipped);
                        G2D.Radians notFlippedOrientationAz = (new G2D.Vector(0.0, 0.0, notFlippedOrientation.X, notFlippedOrientation.Y)).Azimuth;
                        if (newWallDirectionV.Azimuth == notFlippedOrientationAz)
                        { jMWSO.Flip(); }

                        // Find the split point on the edge of the joined middle wall
                        G2D.Vector jMWEdge = RevitSurveyWall.WallEdge(jMW, jMWSO, true);
                        G2D.Point splitLocOnEdge = G2D.Vector.Intersection(newWallLocLine, jMWEdge);

                        surveyFace f;
                        if (!surveyFace.CreateFaceModifyWalls(m_revitDoc, m_baseLvl, ref jMW, jMWSO, splitLocOnEdge, out f, restoredInstanceMap))
                        { return false; }

                        // Determine on which side of the middle wall face the new wall intersects
                        SurveySide jMWSide = (splitLocOnEdge.DistanceTo(f.Corner(SurveySide.Left))
                                              < splitLocOnEdge.DistanceTo(f.Corner(SurveySide.Right)))
                            ? SurveySide.Left : SurveySide.Right;

                        // Check to see if either of the split walls to be created is too small; if so, do not split
                        Distance minSplitLen = new Distance(newWallWidth / 2.0, Distance.Units.RevitInternal)
                                               + m_revitDoc.docSettings.MeasurementTolerance;

                        if (splitLocOnEdge.DistanceTo(jMWEdge.Start) <= minSplitLen
                            || splitLocOnEdge.DistanceTo(jMWEdge.End) <= minSplitLen)
                        { continue; }

                        // Handle a special case: if we are joining close to an existing outside wall, do not split
                        // because Revit cannot join the resulting 4 walls properly
                        joinedWallGeometry g = m_revitDoc.getJoinedWalls(jMW, m_baseLvl, jMWSide, jMWSO);
                        if (g.OutsideWall != null)
                        {
                            G2D.Vector outsideCornerInsideEdge = RevitSurveyWall.WallEdge(g.OutsideWall, g.OutsideWallSO, false);
                            G2D.Point insideEdgeAtJMWEdge = G2D.Vector.Intersection(outsideCornerInsideEdge, jMWEdge);
                            if (   f.Corner(jMWSide.Other).DistanceTo(splitLocOnEdge)
                                >= f.Corner(jMWSide.Other).DistanceTo(insideEdgeAtJMWEdge))
                            { continue; }
                        }

                        // If the wall to be split is the reference wall, null out the reference to it
                        // so it won't be used inadvertently later on
                        if (jMW.Id.Equals(refWall.Id)) { refWall = null; }

                        // Split the wall face horizontally at the new survey wall's base and connected levels
                        if (!f.BindToLevels(m_baseLvl, connectedLvl, out f, restoredInstanceMap))
                        { return false; }

                        // Split the wall vertically at the new survey wall's join point
                        G2D.Vector splitLocV = new G2D.Vector(f.Corner(SurveySide.Left), splitLocOnEdge);
                        Wall dummyWall;
                        SurveyOrientation dummyWallSO; G2D.Point dummyAbsLoc1; G2D.Point dummyAbsLoc2;
                        f.LocateInWall(new Distance(splitLocV.Length, Distance.Units.RevitInternal), SurveySide.Left, Distance.NoValue, true, false,
                                       out dummyWall, out dummyWallSO, out dummyAbsLoc1, out dummyAbsLoc2, restoredInstanceMap);
                    }
                }

                return true;
            }

            internal void FindCornersToPreserve(Wall refWall, ElementId degenerateWallId, bool isDegenerateWallMatchRefWallSO)
            {
                for (int sWallEndPtIdx = 0; sWallEndPtIdx < 2; sWallEndPtIdx++)
                {
                    foreach (Wall jW in m_joinedCornerWalls[sWallEndPtIdx].Keys)
                    {
                        int jWEndPtIdx = m_joinedCornerWalls[sWallEndPtIdx][jW];
                        WallState jWState = m_revitDoc.getWallState(jW);
                        for (int soIdx = 0; soIdx < 2; soIdx++)
                        {
                            SurveyOrientation so = SurveyOrientation.FromIndex(soIdx);
                            if (jWState.IsFaceSurveyed(so))
                            {
                                SurveySide side = (RevitSurveyWall.EndPtIndex(SurveySide.Left, so) == jWEndPtIdx)
                                                  ? SurveySide.Left : SurveySide.Right;
                                Wall jWRef = jW;
                                surveyFace f;
                                surveyFace.CreateFaceNoModifyWalls(m_revitDoc, m_baseLvl, jWRef, so, side, out f);

                                Wall cornerW = jW;
                                SurveyOrientation cornerWSO = new SurveyOrientation(so);
                                if (degenerateWallId != null && jW.Id.Equals(degenerateWallId))
                                {
                                    // This wall is going to go away, so use the reference wall to key the join info
                                    cornerW = refWall;
                                    if (!isDegenerateWallMatchRefWallSO) { cornerWSO.Flip(); }
                                }
                                addCornerToPreserve(cornerW.Id.Value, side, cornerWSO, f.Corner(side));
                            }
                        }
                    }
                }
            }

            internal void FindCornersToPreserve()
            { FindCornersToPreserve(null, null, false); }

            internal void MergePreservedCorners(preserveGeometryInfo otherPGI)
            {
                if (otherPGI.m_baseLvl.Id != m_baseLvl.Id) { return; }

                for (int i = 0; i < otherPGI.m_preserveCornerPoints.Count; i++)
                {
                    bool isMatch = false;
                    foreach (G2D.Point thisPreserveCornerPoint in m_preserveCornerPoints)
                    {
                        if (thisPreserveCornerPoint.DistanceTo(otherPGI.m_preserveCornerPoints[i]).IsZero)
                        { isMatch = true; break; }
                    }
                    if (!isMatch)
                    {
                        addCornerToPreserve(otherPGI.m_preserveCornerWallIds[i], otherPGI.m_preserveCornerSides[i],
                                            otherPGI.m_preserveCornerSOs[i], otherPGI.m_preserveCornerPoints[i]);
                    }
                }
            }

            internal void FindWallsToMove()
            {
                for (int i = 0; i < m_preserveCornerPoints.Count; i++)
                {
                    Wall w = (Wall)m_revitDoc.doc.GetElement(new ElementId(m_preserveCornerWallIds[i]));
                    if (w == null)  { continue; }   // The wall was deleted; no need to preserve anything in this case

                    SurveySide side = m_preserveCornerSides[i];
                    SurveyOrientation so = m_preserveCornerSOs[i];
                    G2D.Point preserveCorner = m_preserveCornerPoints[i];

                    surveyFace f;
                    surveyFace.CreateFaceNoModifyWalls(m_revitDoc, m_baseLvl, w, so, side, out f);

                    G2D.Vector currentCornerToPreserveCorner = new G2D.Vector(f.Corner(side), preserveCorner);
                    Distance cornerOffset = new Distance(currentCornerToPreserveCorner.Length, Distance.Units.RevitInternal);

                    if (cornerOffset > Distance.Zero)
                    {
                        G2D.Radians adjFaceAz = (side == SurveySide.Left) ? f.Azimuth + G2D.Radians.PI : f.Azimuth;
                        if (currentCornerToPreserveCorner.Azimuth == adjFaceAz)
                        {
                            G2D.Point newEndPt = RevitSurveyWall.Centerline(w).Project(preserveCorner);
                            addWallToMove(w, RevitSurveyWall.EndPtIndex(side, so), newEndPt);
                        }
                    }
                }
            }

            private class moveWallsFailuresPreprocessor : IFailuresPreprocessor
            {
                protected static Guid sm_overlappingWallsFailureGuid = new Guid(@"988a6cb2-7050-4a5c-a946-60d652df66c3");
                public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
                {
                    FailureProcessingResult retval = FailureProcessingResult.Continue;

                    IList<FailureMessageAccessor> fmas = failuresAccessor.GetFailureMessages();
                    foreach (FailureMessageAccessor fma in fmas)
                    {
                        FailureDefinitionId fId = fma.GetFailureDefinitionId();

                        if (fId == BuiltInFailures.OverlapFailures.WallsOverlap)
                        {
                            FailureHandlingOptions fho = failuresAccessor.GetFailureHandlingOptions();
                            fho.SetClearAfterRollback(true);
                            failuresAccessor.SetFailureHandlingOptions(fho);
                            retval = FailureProcessingResult.ProceedWithRollBack;
                            break;
                        }
                    }

                    return retval;
                }
            }

            internal void MoveWalls()
            {
                if (m_wDefsToMove.Keys.Count > 0)
                {
                    if (!m_revitDoc.isInTransaction)
                    { throw new HardException(@"preserveGeometryInfo.MoveWalls() called outside of transaction"); }
                    string txnName = m_revitDoc.m_txn.GetName();

                    if (m_revitDoc.m_isTrialTxn)
                    { throw new HardException(@"preserveGeometryInfo.MoveWalls() called inside a trial transaction"); }
                    if (!m_revitDoc.commitTransaction())
                    { throw new HardException(@"preserveGeometryInfo.MoveWalls(): initial commit failed"); }

                    Wall movedW;
                    Dictionary<long, long> ignoreRestoredInstanceMap = new Dictionary<long, long>();
                    foreach (long wIdVal in m_wDefsToMove.Keys)
                    {
                        m_revitDoc.startTransaction(@"preserveGeometryInfo_MoveWalls", new moveWallsFailuresPreprocessor());
                        ElementId wId = new ElementId(wIdVal);
                        m_revitDoc.deleteWall(wId);
                        m_wDefsToMove[wIdVal].CreateNewWall(m_revitDoc, out movedW, ignoreRestoredInstanceMap);
                        m_revitDoc.commitTransaction();
                    }

                    m_revitDoc.startTransaction(txnName + @"_postMoveWalls");
                }
            }

            internal void ApplyOffsets()
            {
                for (int i = 0; i < m_preserveCornerPoints.Count; i++)
                {
                    if (!m_wDefsToMove.ContainsKey(m_preserveCornerWallIds[i]))
                    {
                        Wall w = (Wall)m_revitDoc.doc.GetElement(new ElementId(m_preserveCornerWallIds[i]));
                        if (w == null)  { continue; }   // Wall was deleted; no need to preserve the corner anymore

                        SurveySide side = m_preserveCornerSides[i];
                        SurveyOrientation so = m_preserveCornerSOs[i];
                        G2D.Point preserveCorner = m_preserveCornerPoints[i];

                        surveyFace f;
                        surveyFace.CreateFaceNoModifyWalls(m_revitDoc, m_baseLvl, w, so, side, out f);
                        G2D.Vector currentCornerToPreserveCorner = new G2D.Vector(f.Corner(side), preserveCorner);
                        Distance cornerOffset = new Distance(currentCornerToPreserveCorner.Length, Distance.Units.RevitInternal);
                        G2D.Radians adjFaceAz = (side == SurveySide.Left) ? f.Azimuth + G2D.Radians.PI : f.Azimuth;

                        if (!m_revitDoc.isWithinMeasurementTolerance(cornerOffset, Distance.Zero)
                            && currentCornerToPreserveCorner.Azimuth != adjFaceAz)
                        {
                            WallState ws = m_revitDoc.getWallState(w);
                            Distance oldOffset = (ws.RefPtOffset(side, so) != null) ? ws.RefPtOffset(side, so) : Distance.Zero;
                            ws.SetRefPtOffset(side, so, oldOffset + cornerOffset);
                            m_revitDoc.setWallState(w, ws);
                        }
                    }
                }
            }

            private void addCornerToPreserve(long wId, SurveySide side, SurveyOrientation so, G2D.Point corner)
            {
                m_preserveCornerWallIds.Add(wId);
                m_preserveCornerSides.Add(side);
                m_preserveCornerSOs.Add(so);
                m_preserveCornerPoints.Add(corner);
            }

            private void addWallToMove(Wall w, int newEndPtIdx, G2D.Point newEndPtLocPt)
            {
                Curve curLocCurve = (w.Location as LocationCurve).Curve;
                XYZ endPt0;
                XYZ endPt1;
                if (newEndPtIdx == 0)
                {
                    endPt0 = Utilities.To3D(newEndPtLocPt, 0.0);
                    endPt1 = curLocCurve.GetEndPoint(1);
                    endPt1 = new XYZ(endPt1.X, endPt1.Y, 0.0);
                }
                else
                {
                    endPt0 = curLocCurve.GetEndPoint(0);
                    endPt0 = new XYZ(endPt0.X, endPt0.Y, 0.0);
                    endPt1 = Utilities.To3D(newEndPtLocPt, 0.0); 
                }
                Line newLocLine = Line.CreateBound(endPt0, endPt1);

                wallDefinition moveDef = new wallDefinition(m_revitDoc, w);

                // Set the new location
                moveDef.LocLine = newLocLine;

                // Clear any offsets from the moved end of the wall
                if (newEndPtIdx == 0)
                {
                    moveDef.WallState.SetRefPtOffset(SurveySide.Left, SurveyOrientation.NotFlipped, null);
                    moveDef.WallState.SetRefPtOffset(SurveySide.Right, SurveyOrientation.Flipped, null);
                }
                else
                {
                    moveDef.WallState.SetRefPtOffset(SurveySide.Right, SurveyOrientation.NotFlipped, null);
                    moveDef.WallState.SetRefPtOffset(SurveySide.Left, SurveyOrientation.Flipped, null);
                }

                m_wDefsToMove.Add(w.Id.Value, moveDef);
            }
        }

        private class wallJoinInfo
        {
            internal wallJoinInfo(Wall w, SurveySide side, SurveyOrientation so, G2D.Point corner)
            { Wall = w; Side = side; SurveyOrientation = new SurveyOrientation(so); Corner = corner; }
            internal Wall Wall;
            internal SurveySide Side;
            internal SurveyOrientation SurveyOrientation;
            internal G2D.Point Corner;
        }

        private class surveyFace
        {
            private RevitDocument m_revitDoc;
            private List<long> m_faceWallIDs;
            private List<SurveyOrientation> m_faceWallSOs;
            private G2D.Point[] m_corners;
            private long[] m_insideCornerWallIDs;
            private SurveyOrientation[] m_insideCornerWallSOs;
            private long[] m_outsideCornerWallIDs;
            private SurveyOrientation[] m_outsideCornerWallSOs;
            private long[] m_parallelWallIDs;
            private SurveyOrientation[] m_parallelWallSOs;

            private surveyFace() { }
            private surveyFace(RevitDocument revitDoc)
            {
                m_revitDoc = revitDoc;
                m_faceWallIDs = new List<long>();
                m_faceWallSOs = new List<SurveyOrientation>();
                m_corners = new G2D.Point[2] { null, null };
                m_insideCornerWallIDs = new long[2] { -1L, -1L };
                m_insideCornerWallSOs = new SurveyOrientation[2] { SurveyOrientation.NotFlipped, SurveyOrientation.NotFlipped };
                m_outsideCornerWallIDs = new long[2] { -1L, -1L };
                m_outsideCornerWallSOs = new SurveyOrientation[2] { SurveyOrientation.NotFlipped, SurveyOrientation.NotFlipped };
                m_parallelWallIDs = new long[2] { -1L, -1L };
                m_parallelWallSOs = new SurveyOrientation[2] { SurveyOrientation.NotFlipped, SurveyOrientation.NotFlipped };
            }

            internal static void CreateFaceNoModifyWalls(RevitDocument revitDoc, Level lvl, Wall refW, SurveyOrientation refWSO,
                                                         G2D.Point refPt, out surveyFace face)
            {
                face = new surveyFace(revitDoc);

                G2D.Vector refWEdge = RevitSurveyWall.WallEdge(refW, refWSO, true);

                // Get the list of walls that form a contiguous face
                Wall[] outsideCornerWalls;
                Wall[] insideCornerWalls;
                SurveyOrientation[] outsideCornerWallSOs;
                SurveyOrientation[] insideCornerWallSOs;
                face.surveyFaceCtorWorker_getContiguousWalls(lvl, refW, refWSO, refWEdge,
                                                             out insideCornerWalls, out outsideCornerWalls,
                                                             out insideCornerWallSOs, out outsideCornerWallSOs);

                // Find the intersecting walls that define the ends of the face
                for (int sideIdx = 0; sideIdx < 2; sideIdx++)
                {
                    Wall intersectW;
                    SurveyOrientation intersectWSO;
                    Wall joinedFaceW;
                    int joinedFaceWIdx;
                    face.surveyFaceCtorWorker_findIntersectingCorner(lvl, refW, refWEdge, refWSO, sideIdx, refPt, outsideCornerWalls,
                                                                     out intersectW, out intersectWSO, out joinedFaceW, out joinedFaceWIdx);

                    if (joinedFaceW != null)
                    {
                        face.surveyFaceCtorWorker_removeWallsBeyondIntersection(SurveySide.FromIndex(sideIdx), joinedFaceWIdx);
                    }
                    else
                    {
                        face.surveyFaceCtorWorker_findCorner(sideIdx, insideCornerWalls, insideCornerWallSOs,
                                                             outsideCornerWalls, outsideCornerWallSOs);
                    }
                }
            }

            internal static bool CreateFaceModifyWalls(RevitDocument revitDoc, Level lvl, ref Wall refW, SurveyOrientation refWSO,
                                                       G2D.Point refPt, out surveyFace face, Dictionary<long, long> restoredInstanceMap)
            {
                face = new surveyFace(revitDoc);

                G2D.Vector refWEdge = RevitSurveyWall.WallEdge(refW, refWSO, true);

                // Get the list of walls that form a contiguous face
                Wall[] outsideCornerWalls;
                Wall[] insideCornerWalls;
                SurveyOrientation[] outsideCornerWallSOs;
                SurveyOrientation[] insideCornerWallSOs;
                face.surveyFaceCtorWorker_getContiguousWalls(lvl, refW, refWSO, refWEdge,
                                                             out insideCornerWalls, out outsideCornerWalls,
                                                             out insideCornerWallSOs, out outsideCornerWallSOs);

                // Find the intersecting walls that define the ends of the face
                for (int sideIdx = 0; sideIdx < 2; sideIdx++)
                {
                    Wall intersectW;
                    SurveyOrientation intersectWSO;
                    Wall joinedFaceW;
                    int joinedFaceWIdx;
                    face.surveyFaceCtorWorker_findIntersectingCorner(lvl, refW, refWEdge, refWSO, sideIdx, refPt, outsideCornerWalls,
                                                                     out intersectW, out intersectWSO, out joinedFaceW, out joinedFaceWIdx);

                    if (joinedFaceW != null)
                    {
                        if (!face.surveyFaceCtorWorker_splitWallAtIntersection(lvl, sideIdx, ref refW, joinedFaceW, ref joinedFaceWIdx,
                                                                               intersectW, intersectWSO, restoredInstanceMap))
                        { return false; }
                        face.surveyFaceCtorWorker_removeWallsBeyondIntersection(SurveySide.FromIndex(sideIdx), joinedFaceWIdx);
                    }
                    else
                    {
                        face.surveyFaceCtorWorker_findCorner(sideIdx, insideCornerWalls, insideCornerWallSOs,
                                                             outsideCornerWalls, outsideCornerWallSOs);
                    }
                }

                return true;
            }

            internal static void CreateFaceNoModifyWalls(RevitDocument revitDoc, Level lvl, Wall refW, SurveyOrientation refWSO,
                                                         SurveySide refSide, out surveyFace face)
            {
                face = new surveyFace(revitDoc);

                G2D.Vector refWEdge = RevitSurveyWall.WallEdge(refW, refWSO, true);

                // Get the list of walls that form a contiguous face
                Wall[] outsideCornerWalls;
                Wall[] insideCornerWalls;
                SurveyOrientation[] outsideCornerWallSOs;
                SurveyOrientation[] insideCornerWallSOs;
                face.surveyFaceCtorWorker_getContiguousWalls(lvl, refW, refWSO, refWEdge,
                                                             out insideCornerWalls, out outsideCornerWalls,
                                                             out insideCornerWallSOs, out outsideCornerWallSOs);

                int refSideIdx = refSide.Index;
                face.surveyFaceCtorWorker_findCorner(refSideIdx, insideCornerWalls, insideCornerWallSOs,
                                                     outsideCornerWalls, outsideCornerWallSOs);

                Wall intersectW;
                SurveyOrientation intersectWSO;
                Wall joinedFaceW;
                int joinedFaceWIdx;
                int otherSideIdx = refSide.Other.Index;
                face.surveyFaceCtorWorker_findIntersectingCorner(lvl, refW, refWEdge, refWSO, otherSideIdx,
                                                                 face.m_corners[refSideIdx], outsideCornerWalls,
                                                                 out intersectW, out intersectWSO, out joinedFaceW, out joinedFaceWIdx);
                if (joinedFaceW != null)
                {
                    face.surveyFaceCtorWorker_removeWallsBeyondIntersection(SurveySide.FromIndex(otherSideIdx), joinedFaceWIdx);
                }
                else
                {
                    face.surveyFaceCtorWorker_findCorner(otherSideIdx, insideCornerWalls, insideCornerWallSOs,
                                                         outsideCornerWalls, outsideCornerWallSOs);
                }
            }

            internal static bool CreateFaceModifyWalls(RevitDocument revitDoc, Level lvl, ref Wall refW, SurveyOrientation refWSO,
                                                       SurveySide refSide, out surveyFace face, Dictionary<long, long> restoredInstanceMap)
            {
                face = new surveyFace(revitDoc);

                G2D.Vector refWEdge = RevitSurveyWall.WallEdge(refW, refWSO, true);

                // Get the list of walls that form a contiguous face
                Wall[] outsideCornerWalls;
                Wall[] insideCornerWalls;
                SurveyOrientation[] outsideCornerWallSOs;
                SurveyOrientation[] insideCornerWallSOs;
                face.surveyFaceCtorWorker_getContiguousWalls(lvl, refW, refWSO, refWEdge,
                                                             out insideCornerWalls, out outsideCornerWalls,
                                                             out insideCornerWallSOs, out outsideCornerWallSOs);

                int refSideIdx = refSide.Index;
                face.surveyFaceCtorWorker_findCorner(refSideIdx, insideCornerWalls, insideCornerWallSOs,
                                                     outsideCornerWalls, outsideCornerWallSOs);

                Wall intersectW;
                SurveyOrientation intersectWSO;
                Wall joinedFaceW;
                int joinedFaceWIdx;
                int otherSideIdx = refSide.Other.Index;
                face.surveyFaceCtorWorker_findIntersectingCorner(lvl, refW, refWEdge, refWSO, otherSideIdx,
                                                                 face.m_corners[refSideIdx], outsideCornerWalls,
                                                                 out intersectW, out intersectWSO, out joinedFaceW, out joinedFaceWIdx);
                if (joinedFaceW != null)
                {
                    if (!face.surveyFaceCtorWorker_splitWallAtIntersection(lvl, otherSideIdx, ref refW, joinedFaceW, ref joinedFaceWIdx,
                                                                           intersectW, intersectWSO, restoredInstanceMap))
                    { return false; }
                    face.surveyFaceCtorWorker_removeWallsBeyondIntersection(SurveySide.FromIndex(otherSideIdx), joinedFaceWIdx);
                }
                else
                {
                    face.surveyFaceCtorWorker_findCorner(otherSideIdx, insideCornerWalls, insideCornerWallSOs,
                                                         outsideCornerWalls, outsideCornerWallSOs);
                }

                return true;
            }

            internal bool IsFaceSurveyed
            { get { return m_revitDoc.getWallState(wallAtIdx(0)).IsFaceSurveyed(m_faceWallSOs[0]); } }

            internal Distance LevelOffset
            { get { return m_revitDoc.getWallState(wallAtIdx(0)).LevelOffset(m_faceWallSOs[0]); } }

            internal Level Level { get { return RevitSurveyWall.BaseLevel(wallAtIdx(0)); } }

            internal G2D.Vector Edge
            {
                get
                {
                    List<G2D.Point> frontEdgePts = new List<G2D.Point>();
                    Wall w = null;
                    SurveyOrientation wSO = SurveyOrientation.NotFlipped;
                    for (int i = 0; i < m_faceWallIDs.Count; i++)
                    {
                        w = (Wall)m_revitDoc.doc.GetElement(new ElementId(m_faceWallIDs[i]));
                        wSO = m_faceWallSOs[i];
                        G2D.Vector frontEdge = RevitSurveyWall.WallEdge(w, wSO, true);
                        frontEdgePts.Add(frontEdge.Start);
                    }
                    XYZ faceONegated = RevitSurveyWall.Orientation(w, wSO).Negate();
                    G2D.Radians faceONegatedAz = (new G2D.Vector(new G2D.Point(), Utilities.To2D(faceONegated))).Azimuth;
                    G2D.Point farthestFrontEdgePt = Utilities.FarthestPointInDirection(faceONegatedAz, frontEdgePts);
                    G2D.Vector farthestEdgeV = new G2D.Vector(farthestFrontEdgePt, Azimuth, 1.0);

                    return new G2D.Vector(farthestEdgeV.Project(m_corners[0]), farthestEdgeV.Project(m_corners[1]));
                }
            }

            internal Distance WallExtent { get { return new Distance(Edge.Length, Distance.Units.RevitInternal); } }

            internal Distance CornerToOpening(FamilyInstance opening, SurveySide side)
            { return Corner(side).DistanceTo(OpeningLocation(opening, side)); }

            internal G2D.Point Corner(SurveySide side) { return m_corners[side.Index]; }

            internal G2D.Point ContinuationCorner(SurveySide side)
            {
                G2D.Point retval = Corner(side);   // default if no continuation wall

                surveyFace continuationFace = AdjacentWallFace(side, false, false, true);
                if (continuationFace != null)
                {
                    G2D.Vector thisWallEdge = new G2D.Vector(Corner(side.Other), Corner(side));
                    Wall continuationWall = continuationFace.EndWall(side.Other);
                    SurveyOrientation continuationWallSO = continuationFace.EndWallSO(side.Other);
                    G2D.Vector continuationWallRearEdge = RevitSurveyWall.WallEdge(continuationWall,
                                                                                   continuationWallSO, false);
                    retval = G2D.Vector.Intersection(thisWallEdge, continuationWallRearEdge);
                }

                if (retval == null)
                { throw new HardException("ContinuationCorner() returned null"); }

                return retval;
            }

            internal Distance DefaultSurveyLength(SurveySide refSide)
            {
                Distance retval = null;

                G2D.Point otherCorner;
                if (defaultSurveyCorner(refSide.Other, out otherCorner))
                { retval = Corner(refSide).DistanceTo(otherCorner); }
                else
                { retval = Distance.NoValue; }

                return retval;
            }

            internal Distance DefaultActivateWallExtent()
            {
                Distance retval = null;

                G2D.Point leftCorner;
                G2D.Point rightCorner;
                if (defaultSurveyCorner(SurveySide.Left, out leftCorner) && defaultSurveyCorner(SurveySide.Right, out rightCorner))
                { retval = leftCorner.DistanceTo(rightCorner); }
                else
                { retval = Distance.NoValue; }

                return retval;
            }

            internal Distance DefaultActivateWallCornerToRefOpening(SurveySide side, FamilyInstance refOpening)
            {
                Distance retval = null;

                G2D.Point nearCorner;
                if (defaultSurveyCorner(side, out nearCorner))
                { retval = nearCorner.DistanceTo(OpeningLocation(refOpening, side)); }
                else
                { retval = Distance.NoValue; }

                return retval;
            }

            internal bool IsValidReferenceCorner(SurveySide side)
            {
                G2D.Point dummyCorner;
                return (defaultSurveyCorner(side, out dummyCorner));
            }

            internal Wall EndWall(SurveySide side)
            {
                int endWallIdx = (side == SurveySide.Left) ? 0 : m_faceWallIDs.Count - 1;
                return wallAtIdx(endWallIdx);
            }

            internal int EndWallEndPtIndex(SurveySide side)
            {
                int endWallIdx = (side == SurveySide.Left) ? 0 : m_faceWallIDs.Count - 1;
                SurveyOrientation so = m_faceWallSOs[endWallIdx];
                return RevitSurveyWall.EndPtIndex(side, so);
            }

            internal SurveyOrientation EndWallSO(SurveySide side)
            {
                int endWallIdx = (side == SurveySide.Left) ? 0 : m_faceWallIDs.Count - 1;
                return m_faceWallSOs[endWallIdx];
            }

            internal IEnumerable<long> FaceWallIDs  { get { return m_faceWallIDs; } }

            internal void LocateInWall(Distance location, SurveySide side, Distance levelOffset, bool isModifyWall, bool isSetSurveyed,
                                       out Wall locatedWall, out SurveyOrientation locatedWallSO,
                                       out G2D.Point absLocOnFrontEdge, out G2D.Point absLocOnCenterline, Dictionary<long, long> restoredInstanceMap)
            {
                locatedWall = null; locatedWallSO = SurveyOrientation.NotFlipped;
                absLocOnFrontEdge = null; absLocOnCenterline = null;

                G2D.Point nearCorner = Corner(side);
                G2D.Point farCorner = Corner(side.Other);
                G2D.Vector frontEdgeV = new G2D.Vector(nearCorner, farCorner);

                Distance tol = m_revitDoc.docSettings.MeasurementTolerance;

                if (location < Distance.Zero - tol || location > WallExtent + tol)
                {
                    // The location falls outside the face wall
                    if (isModifyWall)
                    {
                        // Extend the wall to the location point
                        SurveySide addSide = (location < Distance.Zero) ? side : side.Other;
                        Wall endWall = EndWall(addSide);
                        SurveyOrientation endWallSO = EndWallSO(addSide);

                        Distance extendLen = (location < Distance.Zero) ? location : location - WallExtent;
                        G2D.Radians extendAz = (new G2D.Vector(Corner(addSide.Other), Corner(addSide))).Azimuth;
                        G2D.Point extendStartPt = RevitSurveyWall.LocationLine(endWall).Project(Corner(addSide));
                        G2D.Vector extendV = new G2D.Vector(extendStartPt, extendAz, extendLen.ValueRevitInternal);
                        G2D.Vector extendLocV = new G2D.Vector(extendStartPt, extendV.End);
                        if (EndWallEndPtIndex(addSide) == 0)
                        { extendLocV = extendLocV.Reverse(); }

                        Line newLocLine = Utilities.ToRevitLine(extendLocV);

                        wallDefinition wDef = new wallDefinition(m_revitDoc, endWall);
                        wDef.LocLine = newLocLine;
                        wDef.WallState = new WallState();
                        if (isSetSurveyed)
                        {
                            wDef.WallState.SetIsFaceSurveyed(endWallSO);
                            wDef.WallState.SetLevelOffset(endWallSO, levelOffset);
                        }
                        wDef.HostedInstances = new List<wallHostedInstanceDefinition>();
                        wDef.CreateNewWall(m_revitDoc, out locatedWall, restoredInstanceMap);
                        locatedWallSO = endWallSO;

                        // Set the output parameters
                        absLocOnCenterline = extendV.End;
                        absLocOnFrontEdge = frontEdgeV.Project(extendV.End);
                    }
                    else
                    { throw new HardException("Cannot locate point in face wall"); }

                    if (isSetSurveyed)
                    { 
                        SetIsFaceSurveyed();
                        SetLevelOffset(levelOffset);
                    }
                }

                else if (location > Distance.Zero + tol && location < WallExtent - tol)
                {
                    // The location falls somewhere along the face wall;
                    // iterate through the face walls until we find the wall containing the location point
                    int startIdx = (side == SurveySide.Left) ? 0 : m_faceWallIDs.Count - 1;
                    int incr = (side == SurveySide.Left) ? 1 : -1;
                    int count = 0;
                    for (int wIdx = startIdx; count < m_faceWallIDs.Count; wIdx += incr)
                    {
                        Wall w = wallAtIdx(wIdx);
                        Level wLvl = RevitSurveyWall.BaseLevel(w);
                        SurveyOrientation wSO = m_faceWallSOs[wIdx];
                        WallState wS = m_revitDoc.getWallState(w);
                        G2D.Vector wEdge = RevitSurveyWall.WallEdge(w, wSO, true);
                        G2D.Vector wCL = RevitSurveyWall.Centerline(w);


                        G2D.Point wFarEnd;
                        if (count == m_faceWallIDs.Count - 1)
                        { wFarEnd = farCorner; }
                        else
                        { wFarEnd = (side == SurveySide.Left) ? wEdge.End : wEdge.Start; }

                        G2D.Point wFarEndOutsideWallFrontEdgeLoc = null;
                        G2D.Point wFarEndOutsideWallRearEdgeLoc = null;
                        joinedWallGeometry g = m_revitDoc.getJoinedWalls(w, wLvl, side.Other, wSO);
                        if (g.OutsideWall != null)
                        {
                            G2D.Vector outsideWallFrontEdge = RevitSurveyWall.WallEdge(g.OutsideWall, g.OutsideWallSO, true);
                            wFarEndOutsideWallFrontEdgeLoc = G2D.Vector.Intersection(wEdge, outsideWallFrontEdge);
                            G2D.Vector outsideWallRearEdge = RevitSurveyWall.WallEdge(g.OutsideWall, g.OutsideWallSO, false);
                            wFarEndOutsideWallRearEdgeLoc = G2D.Vector.Intersection(wEdge, outsideWallRearEdge);
                        }

                        Distance nearCornerToWallFarEnd = nearCorner.DistanceTo(wFarEnd);
                        Distance totalWallExtent = nearCornerToWallFarEnd;
                        if (wFarEndOutsideWallFrontEdgeLoc != null)
                        {
                            Distance nearCornerToOutsideWallFrontEdge = nearCorner.DistanceTo(wFarEndOutsideWallFrontEdgeLoc);
                            if (nearCornerToOutsideWallFrontEdge > nearCornerToWallFarEnd)
                            { totalWallExtent = nearCornerToOutsideWallFrontEdge; }
                        }

                        if (locatedWall == null
                            && (totalWallExtent + m_revitDoc.docSettings.MeasurementTolerance >= location
                                || count == m_faceWallIDs.Count - 1))
                        {
                            // The current wall contains the location point
                            locatedWall = w;
                            locatedWallSO = wSO;
                            WallState locWS = wS;

                            absLocOnFrontEdge = (new G2D.Vector(nearCorner, frontEdgeV.Azimuth, location.ValueRevitInternal)).End;
                            absLocOnCenterline = wCL.Project(absLocOnFrontEdge);

                            if (isModifyWall)
                            {
                                // Determine whether it is necessary to split the wall into two subwalls
                                bool isSplitWall = true;

                                if (wFarEndOutsideWallRearEdgeLoc != null)
                                {
                                    Distance nearCornerToOutsideWallRearEdge
                                        = nearCorner.DistanceTo(wFarEndOutsideWallRearEdgeLoc);
                                    if (location > nearCornerToOutsideWallRearEdge - m_revitDoc.docSettings.MeasurementTolerance)
                                    { isSplitWall = false; }
                                }
                                else if (m_revitDoc.isWithinMeasurementTolerance(location, nearCornerToWallFarEnd))
                                { isSplitWall = false; }

                                if (isSplitWall)
                                {
                                    // Split the wall
                                    G2D.Point splitLoc = RevitSurveyWall.LocationLine(locatedWall).Project(absLocOnFrontEdge);
                                    Wall[] splitWalls;
                                    m_revitDoc.splitWall(locatedWall, splitLoc, out splitWalls, restoredInstanceMap);

                                    locatedWall = splitWalls[RevitSurveyWall.EndPtIndex(side, locatedWallSO)];
                                    locWS = m_revitDoc.getWallState(locatedWall);
                                    // Set a reference point offset to delineate the end of the new survey face
                                    locWS.SetRefPtOffset(side.Other, locatedWallSO, Distance.Zero);
                                }
                                else
                                {
                                    // Instead of splitting the wall, set an offset to record the location
                                    // 
                                    // First, adjust the absolute location if it is close to the inside corner
                                    // of a continuated outside wall
                                    if (wFarEndOutsideWallRearEdgeLoc != null
                                        && m_revitDoc.isWithinMeasurementTolerance(wFarEndOutsideWallRearEdgeLoc.DistanceTo(absLocOnFrontEdge), Distance.Zero))
                                    {
                                        absLocOnFrontEdge = wFarEndOutsideWallRearEdgeLoc;
                                        absLocOnCenterline = wCL.Project(absLocOnFrontEdge);
                                    }

                                    G2D.Vector locToWFarEndV = new G2D.Vector(absLocOnFrontEdge, wFarEnd);
                                    Distance offset = new Distance(locToWFarEndV.Length, Distance.Units.RevitInternal);
                                    if (!m_revitDoc.isWithinMeasurementTolerance(offset, Distance.Zero))
                                    {
                                        G2D.Radians wAz = RevitSurveyWall.Azimuth(locatedWall, locatedWallSO);
                                        double offsetDirectionFactor = 1.0;
                                        if ((side == SurveySide.Left && locToWFarEndV.Azimuth != wAz)
                                            || (side == SurveySide.Right && locToWFarEndV.Azimuth == wAz))
                                        { offsetDirectionFactor = -1.0; }
                                        offset *= offsetDirectionFactor;
                                        locWS.SetRefPtOffset(side.Other, locatedWallSO, offset);
                                    }
                                }

                                if (isSetSurveyed)
                                {
                                    locWS.SetIsFaceSurveyed(locatedWallSO);
                                    locWS.SetLevelOffset(locatedWallSO, levelOffset);
                                }
                                m_revitDoc.setWallState(locatedWall, locWS);
                            }
                        }

                        else if (isSetSurveyed)
                        {
                            if (locatedWall == null)
                            {
                                wS.SetIsFaceSurveyed(wSO);
                                wS.SetLevelOffset(wSO, levelOffset);
                                wS.SetRefPtOffset(side, locatedWallSO, null);
                            }
                            m_revitDoc.setWallState(w, wS);
                        }

                        count++;
                        if (count == m_faceWallIDs.Count && locatedWall == null)
                        {
                            // The location is beyond the last wall in the face; it must
                            // be the corner of a wall joined to the last wall
                            //
                            // Set the located wall to the last wall in the face
                            locatedWall = w;
                            locatedWallSO = wSO;
                            absLocOnFrontEdge = (new G2D.Vector(nearCorner, frontEdgeV.Azimuth, location.ValueRevitInternal)).End;
                            absLocOnCenterline = wCL.Project(absLocOnFrontEdge);
                        }
                    }
                }

                else   // The location is within measurement tolerance of one of the corners
                {
                    absLocOnFrontEdge = (new G2D.Vector(nearCorner, frontEdgeV.Azimuth, location.ValueRevitInternal)).End;

                    int locWallIdx;
                    SurveySide locSide;
                    Distance offset;
                    if (m_revitDoc.isWithinMeasurementTolerance(location, Distance.Zero))
                    {
                        locWallIdx = (side == SurveySide.Left) ? 0 : m_faceWallIDs.Count - 1;
                        locSide = side;
                        offset = location;
                        if (location < Distance.Zero) { absLocOnFrontEdge = nearCorner; }
                    }
                    else  // isWithinMeasurementTolerance(location, WallExtent)
                    {
                        locWallIdx = (side == SurveySide.Left) ? m_faceWallIDs.Count - 1 : 0;
                        locSide = side.Other;
                        offset = WallExtent - location;
                        if (location > WallExtent) { absLocOnFrontEdge = farCorner; }

                        if (isSetSurveyed)
                        {
                            // The entire face is surveyed
                            SetIsFaceSurveyed();
                            SetLevelOffset(levelOffset);
                        }
                    }

                    locatedWall = wallAtIdx(locWallIdx);
                    locatedWallSO = m_faceWallSOs[locWallIdx];
                    absLocOnCenterline = RevitSurveyWall.Centerline(locatedWall).Project(absLocOnFrontEdge);

                    if (isSetSurveyed && !offset.IsZero)
                    {
                        // If there is an outside wall at this corner and the location matches the inside continuation corner,
                        // we assume that the wall continues on "this" side; set an offset for the end wall so we can survey
                        // the continuation wall instead of assuming this is an outside corner
                        joinedWallGeometry g = m_revitDoc.getJoinedWalls(locatedWall, RevitSurveyWall.BaseLevel(locatedWall), locSide, locatedWallSO);
                        if (g.OutsideWall != null && g.ParallelWall == null && g.InsideWall == null)
                        {
                            G2D.Vector outsideWallRearEdge = RevitSurveyWall.WallEdge(g.OutsideWall, g.OutsideWallSO, false);
                            G2D.Point outsideWallInsideCorner = G2D.Vector.Intersection(frontEdgeV, outsideWallRearEdge);
                            if (m_revitDoc.isWithinMeasurementTolerance(outsideWallInsideCorner.DistanceTo(absLocOnFrontEdge), Distance.Zero))
                            {
                                WallState locWS = m_revitDoc.getWallState(locatedWall);
                                Distance oldOffset = locWS.RefPtOffset(locSide, locatedWallSO);
                                if (oldOffset == null || oldOffset == Distance.NoValue)
                                { oldOffset = Distance.Zero; }
                                locWS.SetRefPtOffset(locSide, locatedWallSO, oldOffset + offset);
                                m_revitDoc.setWallState(locatedWall, locWS);
                            }
                        }
                    }
                }
            }

            internal ElementId InsideCornerWallId(SurveySide side)
            {
                if (m_insideCornerWallIDs[side.Index] != -1)
                { return new ElementId(m_insideCornerWallIDs[side.Index]); }
                else
                { return null; }
            }

            internal SurveyOrientation InsideCornerWallSO(SurveySide side)
            { return m_insideCornerWallSOs[side.Index]; }

            internal ElementId OutsideCornerWallId(SurveySide side)
            {
                if (m_outsideCornerWallIDs[side.Index] != -1)
                { return new ElementId(m_outsideCornerWallIDs[side.Index]); }
                else
                { return null; }
            }

            internal SurveyOrientation OutsideCornerWallSO(SurveySide side)
            { return m_outsideCornerWallSOs[side.Index]; }

            internal ElementId ParallelWallId(SurveySide side)
            {
                if (m_parallelWallIDs[side.Index] != -1)
                { return new ElementId(m_parallelWallIDs[side.Index]); }
                else
                { return null; }
            }

            internal SurveyOrientation ParallelWallSO(SurveySide side)
            { return m_parallelWallSOs[side.Index]; }

            internal surveyFace AdjacentWallFace(SurveySide side, bool isSurveyedOnly, bool isInsideCornerOnly, bool isOutsideCornerOnly)
            {
                surveyFace retval = null;
                long adjWID = -1L;
                SurveyOrientation adjWSO = SurveyOrientation.NotFlipped;

                if (!isOutsideCornerOnly && m_insideCornerWallIDs[side.Index] != -1)
                {
                    adjWID = m_insideCornerWallIDs[side.Index];
                    adjWSO = m_insideCornerWallSOs[side.Index];
                }
                else if (!isInsideCornerOnly && m_outsideCornerWallIDs[side.Index] != -1)
                {
                    adjWID = m_outsideCornerWallIDs[side.Index];
                    adjWSO = m_outsideCornerWallSOs[side.Index];
                }

                if (adjWID != -1)
                {
                    Wall adjW = (Wall)m_revitDoc.doc.GetElement(new ElementId(adjWID));
                    if (!isSurveyedOnly || m_revitDoc.getWallState(adjW).IsFaceSurveyed(adjWSO))
                    {
                        surveyFace.CreateFaceNoModifyWalls(m_revitDoc, Level, adjW, adjWSO, side.Other, out retval);
                    }
                }

                return retval;
            }

            internal void GetAdjacentCornerWall(SurveySide side, out Wall adjacentWall, out SurveyOrientation adjacentWallSO)
            {
                adjacentWall = null;
                adjacentWallSO = SurveyOrientation.NotFlipped;

                long adjWID = -1L;
                if (m_insideCornerWallIDs[side.Index] != -1)
                {
                    adjWID = m_insideCornerWallIDs[side.Index];
                    adjacentWallSO = m_insideCornerWallSOs[side.Index];
                }
                else if (m_parallelWallIDs[side.Index] == -1 && m_outsideCornerWallIDs[side.Index] != -1) 
                {
                    adjWID = m_outsideCornerWallIDs[side.Index];
                    adjacentWallSO = m_outsideCornerWallSOs[side.Index];
                }

                if (adjWID != -1) { adjacentWall = (Wall)m_revitDoc.doc.GetElement(new ElementId(adjWID)); }
            }

            internal G2D.Radians AdjacentWallFaceAngle(SurveySide side)
            {
                G2D.Radians retval = G2D.Radians.Undefined;

                long adjWID = m_insideCornerWallIDs[side.Index];
                SurveyOrientation adjWSO = m_insideCornerWallSOs[side.Index];
                if (adjWID == -1)
                {
                    adjWID = m_outsideCornerWallIDs[side.Index];
                    adjWSO = m_outsideCornerWallSOs[side.Index];
                }

                if (adjWID != -1)
                {
                    Wall adjW = (Wall)m_revitDoc.doc.GetElement(new ElementId(adjWID));
                    G2D.Radians adjFaceAz = RevitSurveyWall.Azimuth(adjW, adjWSO);
                    if (side == SurveySide.Left) { adjFaceAz += G2D.Radians.PI; }

                    G2D.Radians thisFaceAz = this.Azimuth;
                    if (side == SurveySide.Right) { thisFaceAz += G2D.Radians.PI; }

                    retval = adjFaceAz - thisFaceAz;
                    if (retval < G2D.Radians.Zero) { retval = G2D.Radians.TwoPIPlus(retval); }
                }

                return retval;
            }

            internal bool BindToLevels(Level baseLvl, Level topLvl, out surveyFace newFace, Dictionary<long, long> restoredInstanceMap)
            {
                newFace = this;

                Wall leftWall = wallAtIdx(0);
                Wall newWall = null;
                bool isNewWall = m_revitDoc.isSplitWallAtLevels(leftWall, m_faceWallSOs[0], baseLvl, topLvl, out newWall, restoredInstanceMap);
                if (isNewWall) { leftWall = newWall; }

                for (int wIdx = 1; wIdx < m_faceWallIDs.Count; wIdx++)
                {
                    if (m_revitDoc.isSplitWallAtLevels(wallAtIdx(wIdx), m_faceWallSOs[wIdx], baseLvl, topLvl, out newWall, restoredInstanceMap))
                    { isNewWall = true; }
                }

                if (isNewWall)
                {
                    if (m_revitDoc.commitExistingAndStartNewTransaction(@"surveyFace.BindToLevels"))
                    {
                        if (!surveyFace.CreateFaceModifyWalls(m_revitDoc, baseLvl, ref leftWall, m_faceWallSOs[0], SurveySide.Left, out newFace, restoredInstanceMap))
                        { return false; }
                    }
                }

                return true;
            }

            internal G2D.Radians Azimuth
            {
                get
                {
                    Wall w = (Wall)m_revitDoc.doc.GetElement(new ElementId(m_faceWallIDs[0]));
                    return (RevitSurveyWall.Azimuth(w, m_faceWallSOs[0]));
                }
            }

            internal void SetIsFaceSurveyed()
            {
                for (int i = 0; i < m_faceWallIDs.Count; i++)
                {
                    Wall w = wallAtIdx(i);
                    WallState ws = m_revitDoc.getWallState(w);
                    ws.SetIsFaceSurveyed(m_faceWallSOs[i]);
                    m_revitDoc.setWallState(w, ws);
                }
            }

            internal void ResetIsFaceSurveyed()
            {
                for (int i = 0; i < m_faceWallIDs.Count; i++)
                {
                    Wall w = wallAtIdx(i);
                    WallState ws = m_revitDoc.getWallState(w);
                    ws.ResetIsFaceSurveyed(m_faceWallSOs[i]);
                    m_revitDoc.setWallState(w, ws);
                }
            }

            internal void SetLevelOffset(Distance levelOffset)
            {
                for (int i = 0; i < m_faceWallIDs.Count; i++)
                {
                    Wall w = wallAtIdx(i);
                    WallState ws = m_revitDoc.getWallState(w);
                    ws.SetLevelOffset(m_faceWallSOs[i], levelOffset);
                    m_revitDoc.setWallState(w, ws);
                }
            }

            internal List<FamilyInstance> GetOpenings()
            {
                List<FamilyInstance> retval = new List<FamilyInstance>();

                FilteredElementCollector doorColl = new FilteredElementCollector(m_revitDoc.doc).OfClass(typeof(FamilyInstance));
                doorColl.OfCategory(BuiltInCategory.OST_Doors);
                FilteredElementCollector windowColl = new FilteredElementCollector(m_revitDoc.doc).OfClass(typeof(FamilyInstance));
                windowColl.OfCategory(BuiltInCategory.OST_Windows);
                doorColl.UnionWith(windowColl);

                // TODO: Replace this with a LINQ query or Parameter filter
                foreach (Element instEl in doorColl.ToElements())
                {
                    FamilyInstance inst = (FamilyInstance)instEl;
                    if (m_faceWallIDs.Contains(inst.Host.Id.Value))
                    { retval.Add(inst); }
                }

                return retval;
            }

            internal G2D.Point OpeningLocation(FamilyInstance opening, SurveySide side)
            {
                G2D.Vector frontEdge = new G2D.Vector(Corner(side.Other), Corner(side));
                G2D.Point openingCenter = Utilities.To2D(((LocationPoint)opening.Location).Point);
                G2D.Point openingCenterOnEdge = frontEdge.Project(openingCenter);
                G2D.Vector openingCenterToEnd = new G2D.Vector(openingCenterOnEdge, frontEdge.Azimuth,
                                                               wallHostedInstance.Width(m_revitDoc, opening) / 2.0);
                return openingCenterToEnd.End;
            }

            private void surveyFaceCtorWorker_getContiguousWalls(Level lvl, Wall refW, SurveyOrientation refWSO, G2D.Vector refWEdge,
                                                                 out Wall[] insideCornerWalls, out Wall[] outsideCornerWalls,
                                                                 out SurveyOrientation[] insideCornerWallSOs, out SurveyOrientation[] outsideCornerWallSOs)
            {
                WallState refWState = m_revitDoc.getWallState(refW);

                outsideCornerWalls = new Wall[2] { null, null };
                insideCornerWalls = new Wall[2] { null, null };
                outsideCornerWallSOs = new SurveyOrientation[2] { SurveyOrientation.NotFlipped, SurveyOrientation.NotFlipped };
                insideCornerWallSOs = new SurveyOrientation[2] { SurveyOrientation.NotFlipped, SurveyOrientation.NotFlipped };

                m_faceWallIDs.Add(refW.Id.Value);
                m_faceWallSOs.Add(refWSO);

                // Go along the face in both directions, finding the extent of contiguous walls
                for (int sideIdx = 0; sideIdx < 2; sideIdx++)
                {
                    SurveySide side = SurveySide.FromIndex(sideIdx);

                    Wall nextW = refW;
                    SurveyOrientation nextWSO = refWSO;
                    bool isFoundEndOfFace = false;
                    while (!isFoundEndOfFace)
                    {
                        bool isNextWallRefPtOffset = (m_revitDoc.getWallState(nextW).RefPtOffset(side, nextWSO) != null);
                        
                        joinedWallGeometry g = m_revitDoc.getJoinedWalls(nextW, lvl, side, nextWSO);
                        Wall contiguousWall = null;
                        if (g.InsideWall == null && g.ParallelWall != null)
                        {
                            WallState parallelWallState = m_revitDoc.getWallState(g.ParallelWall);

                            Distance parallelWallRefPtOffset = parallelWallState.RefPtOffset(side.Other, g.ParallelWallSO);
                            if (parallelWallRefPtOffset == null || !parallelWallRefPtOffset.IsZero)
                            {
                                if (parallelWallState.IsFaceSurveyed(g.ParallelWallSO) == refWState.IsFaceSurveyed(refWSO)
                                    && parallelWallState.LevelOffset(g.ParallelWallSO) == refWState.LevelOffset(refWSO))
                                {
                                    contiguousWall = g.ParallelWall;
                                    SurveyOrientation contiguousWallSO = g.ParallelWallSO;
                                }
                                else
                                {
                                    m_parallelWallIDs[sideIdx] = g.ParallelWall.Id.Value;
                                    m_parallelWallSOs[sideIdx] = g.ParallelWallSO;
                                }
                            }
                        }

                        if (contiguousWall != null && !isNextWallRefPtOffset)
                        {
                            if (side == SurveySide.Left)
                            {
                                m_faceWallIDs.Insert(0, g.ParallelWall.Id.Value);
                                m_faceWallSOs.Insert(0, g.ParallelWallSO);
                            }
                            else
                            {
                                m_faceWallIDs.Add(g.ParallelWall.Id.Value);
                                m_faceWallSOs.Add(g.ParallelWallSO);
                            }
                            nextW = g.ParallelWall;
                            nextWSO = g.ParallelWallSO;
                        }
                        else
                        {
                            // Found the end of the contiguous face
                            isFoundEndOfFace = true;

                            if (g.InsideWall != null)
                            {
                                insideCornerWalls[sideIdx] = g.InsideWall;
                                insideCornerWallSOs[sideIdx] = g.InsideWallSO;
                            }
                            else if (g.OutsideWall != null && g.InsideWall == null && g.ParallelWall == null)
                            {
                                outsideCornerWalls[sideIdx] = g.OutsideWall;
                                outsideCornerWallSOs[sideIdx] = g.OutsideWallSO;
                            }
                        }
                    }
                }
            }

            private void surveyFaceCtorWorker_findCorner(int sideIdx,
                                                         Wall[] insideCornerWalls, SurveyOrientation[] insideCornerWallSOs,
                                                         Wall[] outsideCornerWalls, SurveyOrientation[] outsideCornerWallSOs)
            {
                SurveySide side = SurveySide.FromIndex(sideIdx);
                int endWallIdx = (sideIdx == 0) ? 0 : m_faceWallIDs.Count - 1;
                Wall endWall = (Wall)m_revitDoc.doc.GetElement(new ElementId(m_faceWallIDs[endWallIdx]));
                SurveyOrientation endWallSO = m_faceWallSOs[endWallIdx];
                G2D.Vector endWallEdge = RevitSurveyWall.WallEdge(endWall, endWallSO, true);

                if (insideCornerWalls[sideIdx] != null)
                {
                    G2D.Vector cornerWallEdge = RevitSurveyWall.WallEdge(insideCornerWalls[sideIdx],
                                                                         insideCornerWallSOs[sideIdx], true);
                    m_corners[sideIdx] = G2D.Vector.Intersection(endWallEdge, cornerWallEdge);
                    m_insideCornerWallIDs[sideIdx] = insideCornerWalls[sideIdx].Id.Value;
                    m_insideCornerWallSOs[sideIdx] = insideCornerWallSOs[sideIdx];
                }
                else if (outsideCornerWalls[sideIdx] != null)
                {
                    G2D.Vector cornerWallEdge = RevitSurveyWall.WallEdge(outsideCornerWalls[sideIdx],
                                                                         outsideCornerWallSOs[sideIdx], true);
                    m_corners[sideIdx] = G2D.Vector.Intersection(endWallEdge, cornerWallEdge);
                    m_outsideCornerWallIDs[sideIdx] = outsideCornerWalls[sideIdx].Id.Value;
                    m_outsideCornerWallSOs[sideIdx] = outsideCornerWallSOs[sideIdx];
                }
                else
                {
                    m_corners[sideIdx] = (side == SurveySide.Left) ? endWallEdge.Start : endWallEdge.End;
                }

                // If the end wall has a recorded offset, apply it
                Distance refPtOffset = m_revitDoc.getWallState(endWall).RefPtOffset(side, endWallSO);
                if (refPtOffset != null)
                {
                    double dirMultiplier = (side == SurveySide.Left) ? 1.0 : -1.0;
                    G2D.Vector offsetV = new G2D.Vector(m_corners[sideIdx], RevitSurveyWall.Azimuth(endWall, endWallSO),
                                                        refPtOffset.ValueRevitInternal * dirMultiplier);
                    m_corners[sideIdx] = offsetV.End;
                    // No corner walls if there's an offset
                    m_insideCornerWallIDs[sideIdx] = -1;
                    m_outsideCornerWallIDs[sideIdx] = -1;
                }
            }

            private void surveyFaceCtorWorker_findIntersectingCorner(Level lvl, Wall refW, G2D.Vector refWEdge, SurveyOrientation refWSO,
                                                                     int sideIdx, G2D.Point refPt, Wall[] outsideCornerWalls,
                                                                     out Wall intersectW, out SurveyOrientation intersectWSO,
                                                                     out Wall joinedFaceW, out int joinedFaceWIdx)
            {
                SurveySide side = SurveySide.FromIndex(sideIdx);
                joinedFaceW = null;
                joinedFaceWIdx = -1;
                G2D.Vector intersectWEdge;
                findIntersectingWall(lvl, refW, refWSO, refPt, side, out intersectW, out intersectWSO, out intersectWEdge);

                // Make sure the closest intersecting wall is joined to the face
                if (intersectW != null)
                {
                    // See if the intersecting wall is joined to one of the face walls
                    LocationCurve loc = (LocationCurve)intersectW.Location;
                    for (int endPtIdx = 0; endPtIdx < 2; endPtIdx++)
                    {
                        foreach (Element jEl in loc.get_ElementsAtJoin(endPtIdx))
                        {
                            if (jEl.Id != intersectW.Id && m_faceWallIDs.Contains(jEl.Id.Value))
                            {
                                joinedFaceW = (Wall)jEl;
                                joinedFaceWIdx = m_faceWallIDs.IndexOf(jEl.Id.Value);
                                break;
                            }
                        }
                        if (joinedFaceW != null) { break; }
                    }

                    if (joinedFaceW != null)
                    {
                        // Set the survey face corner to the inside corner of the face and intersecting walls
                        m_corners[sideIdx] = G2D.Vector.Intersection(refWEdge, intersectWEdge);

                        if (!(outsideCornerWalls[sideIdx] != null
                              && intersectW.Id.Equals(outsideCornerWalls[sideIdx].Id)))
                        {
                            m_insideCornerWallIDs[sideIdx] = intersectW.Id.Value;
                            m_insideCornerWallSOs[sideIdx] = intersectWSO;
                        }
                    }
                }
            }

            private void findIntersectingWall(Level lvl, Wall refW, SurveyOrientation refWSO, G2D.Point refPt, SurveySide side,
                                              out Wall intersectW, out SurveyOrientation intersectWSO, out G2D.Vector intersectWEdge)
            {
                G2D.Radians refWAz = RevitSurveyWall.Azimuth(refW, refWSO);
                G2D.Radians sideAdjAz = (side == SurveySide.Left) ? refWAz + G2D.Radians.PI : refWAz;
                XYZ rayDir = Utilities.To3DUnitVector(sideAdjAz);

                // Adjust the ref point to coincide with the farthest-protruding wall in the face
                List<G2D.Point> frontEdgePts = new List<G2D.Point>();
                for (int i = 0; i < m_faceWallIDs.Count; i++)
                {
                    Wall w = (Wall)m_revitDoc.doc.GetElement(new ElementId(m_faceWallIDs[i]));
                    G2D.Vector frontEdge = RevitSurveyWall.WallEdge(w, m_faceWallSOs[i], true);
                    frontEdgePts.Add(frontEdge.Start);
                }
                XYZ faceONegated = RevitSurveyWall.Orientation(refW, refWSO).Negate();
                G2D.Radians faceONegatedAz = (new G2D.Vector(new G2D.Point(), Utilities.To2D(faceONegated))).Azimuth;
                G2D.Point farthestFrontEdgePt = Utilities.FarthestPointInDirection(faceONegatedAz, frontEdgePts);
                G2D.Vector refPtV = new G2D.Vector(refPt, faceONegatedAz, 1.0);
                G2D.Point adjRefPt = refPtV.Project(farthestFrontEdgePt);

                double offset = m_revitDoc.docSettings.MeasurementTolerance.ValueRevitInternal;
                double epsilon = (1.0 / 12.0) / 16.0;   // 1/16"
                offset += epsilon;  // Go just beyond the measurement tolerance
                XYZ faceOffset = RevitSurveyWall.Orientation(refW, refWSO).Multiply(-offset);
                XYZ edgeOffset = rayDir.Multiply(-epsilon);
                XYZ rayOriginBase = Utilities.To3D(adjRefPt, 0.0) + faceOffset + edgeOffset;

                // Find the closest intersecting wall
                intersectW = null;
                intersectWSO = null;
                intersectWEdge = null;
                ElementClassFilter wallFilter = new ElementClassFilter(typeof(Wall));

                double minProximity = 10000.0;
                foreach (double rayElev in m_revitDoc.rayElevations(lvl))
                {
                    XYZ rayOrigin = rayOriginBase + new XYZ(0.0, 0.0, rayElev);
                    ReferenceIntersector refIntersector = new ReferenceIntersector(wallFilter, FindReferenceTarget.Face, m_revitDoc.view3D);
                    IList<ReferenceWithContext> intersectRefsWC = refIntersector.Find(rayOrigin, rayDir);

                    // Organize the intersections by wall
                    Dictionary<long, List<ReferenceWithContext>> intWallRefsWC = new Dictionary<long, List<ReferenceWithContext>>();
                    foreach (ReferenceWithContext iRefWC in intersectRefsWC)
                    {
                        Reference iRef = iRefWC.GetReference();
                        Wall iRefWall = m_revitDoc.doc.GetElement(iRef) as Wall;
                        if (iRefWall != null)
                        {
                            PlanarFace iRefWallPF = iRefWall.GetGeometryObjectFromReference(iRef) as PlanarFace;
                            if (iRefWallPF != null)
                            {
                                // Make sure the intersection is with a "face" of the wall, not the "end"
                                XYZ wallNormal = iRefWall.Orientation;
                                XYZ faceNormal = iRefWallPF.FaceNormal;
                                if (faceNormal.IsAlmostEqualTo(wallNormal) || faceNormal.IsAlmostEqualTo(wallNormal.Negate()))
                                {
                                    long wID = iRefWall.Id.Value;
                                    if (!intWallRefsWC.ContainsKey(wID)) { intWallRefsWC.Add(wID, new List<ReferenceWithContext>()); }
                                    intWallRefsWC[wID].Add(iRefWC);
                                }
                            }
                        }
                    }

                    // Find the closest wall that intersects the ray twice (front and back)
                    foreach (long wID in intWallRefsWC.Keys)
                    {
                        if (intWallRefsWC[wID].Count > 1)
                        {
                            // Find the face closest to the ray origin (front face)
                            double minWallProximity = 1000000.0;
                            Reference closestRef = null;
                            foreach (ReferenceWithContext wRefWC in intWallRefsWC[wID])
                            {
                                if (wRefWC.Proximity < minWallProximity)
                                {
                                    closestRef = wRefWC.GetReference();
                                    minWallProximity = wRefWC.Proximity;
                                }
                            }

                            if (minWallProximity < minProximity)
                            {
                                intersectW = (Wall)m_revitDoc.doc.GetElement(new ElementId(wID));
                                PlanarFace pf = intersectW.GetGeometryObjectFromReference(closestRef) as PlanarFace;
                                intersectWSO = Utilities.SurveyOrientationFromFace(intersectW, pf);
                                intersectWEdge = RevitSurveyWall.EdgeFromFace(pf);
                                minProximity = minWallProximity;
                            }
                        }
                    }
                }
            }

            private bool surveyFaceCtorWorker_splitWallAtIntersection(Level lvl, int sideIdx, ref Wall refW, Wall joinedFaceW, ref int joinedFaceWIdx,
                                                                      Wall intersectW, SurveyOrientation intersectWSO, Dictionary<long, long> restoredInstanceMap)
            {
                SurveySide side = SurveySide.FromIndex(sideIdx);

                int joinedFaceWallJoinPtIdx = RevitSurveyWall.JoinPtIdx(joinedFaceW, intersectW, false);
                if (joinedFaceWallJoinPtIdx == -1)
                {
                    // The intersecting wall is joined to the face in the middle of a face wall;
                    // we have to split the face wall at the join point
                    bool isSplitJoinedFaceWall = true;

                    // If one of the edges of the joined wall is close to the end of the
                    // joined face wall, do not split the joined face wall
                    G2D.Vector joinedFaceWFrontEdge = RevitSurveyWall.WallEdge(joinedFaceW, m_faceWallSOs[joinedFaceWIdx], true);
                    G2D.Vector intWFrontEdge = RevitSurveyWall.WallEdge(intersectW, intersectWSO, true);
                    G2D.Point intWFrontEdgeIntersection = G2D.Vector.Intersection(intWFrontEdge, joinedFaceWFrontEdge);
                    G2D.Vector intWRearEdge = RevitSurveyWall.WallEdge(intersectW, intersectWSO, false);
                    G2D.Point intWRearEdgeIntersection = G2D.Vector.Intersection(intWRearEdge, joinedFaceWFrontEdge);
                    if (m_revitDoc.isWithinMeasurementTolerance(intWFrontEdgeIntersection.DistanceTo(joinedFaceWFrontEdge.Start), Distance.Zero)
                        || m_revitDoc.isWithinMeasurementTolerance(intWFrontEdgeIntersection.DistanceTo(joinedFaceWFrontEdge.End), Distance.Zero)
                        || m_revitDoc.isWithinMeasurementTolerance(intWRearEdgeIntersection.DistanceTo(joinedFaceWFrontEdge.Start), Distance.Zero)
                        || m_revitDoc.isWithinMeasurementTolerance(intWRearEdgeIntersection.DistanceTo(joinedFaceWFrontEdge.End), Distance.Zero))
                    { isSplitJoinedFaceWall = false; }

                    G2D.Point splitLoc = G2D.Vector.Intersection(RevitSurveyWall.LocationLine(intersectW),
                                                                 RevitSurveyWall.LocationLine(joinedFaceW));

                    if (isSplitJoinedFaceWall)
                    {
                        // If the joined face wall is at the end of the face and there is an outside wall joined
                        // to it, do not split if the split point is inside of the "join area"
                        bool isJoinedAtEndOfFace = false;
                        SurveySide joinedAtEndSide = SurveySide.Left;
                        if (m_faceWallIDs.Count == 1)
                        { isJoinedAtEndOfFace = true; joinedAtEndSide = side; }
                        else if (joinedFaceWIdx == 0)
                        { isJoinedAtEndOfFace = true; joinedAtEndSide = SurveySide.Left; }
                        else if (joinedFaceWIdx == m_faceWallIDs.Count - 1)
                        { isJoinedAtEndOfFace = true; joinedAtEndSide = SurveySide.Right; }

                        if (isJoinedAtEndOfFace)
                        {
                            joinedWallGeometry g = m_revitDoc.getJoinedWalls(joinedFaceW, lvl, joinedAtEndSide, m_faceWallSOs[joinedFaceWIdx]);
                            if (g.OutsideWall != null)
                            {
                                G2D.Vector outsideWInsideEdge = RevitSurveyWall.WallEdge(g.OutsideWall, g.OutsideWallSO, false);
                                G2D.Point outsideWInsideEdgeAtFace = G2D.Vector.Intersection(outsideWInsideEdge, joinedFaceWFrontEdge);
                                G2D.Point oppEndPt = (side == SurveySide.Left) ? joinedFaceWFrontEdge.End : joinedFaceWFrontEdge.Start;
                                if (oppEndPt.DistanceTo(m_corners[sideIdx]) > oppEndPt.DistanceTo(outsideWInsideEdgeAtFace))
                                { isSplitJoinedFaceWall = false; }
                            }
                        }
                    }

                    if (isSplitJoinedFaceWall)
                    {
                        // Remember whether the joined wall is also the reference wall; if so,
                        // we need to set a new reference wall to one of the split walls
                        bool isJoinedFaceWEqualToRefW = joinedFaceW.Id.Equals(refW.Id);

                        Wall[] splitFaceWalls;
                        m_revitDoc.splitWall(joinedFaceW, splitLoc, out splitFaceWalls, restoredInstanceMap);

                        if (!m_revitDoc.commitExistingAndStartNewTransaction(@"constructSurveyFace_postSplitWall"))
                        { return false; }

                        // True-up the face wall list to take the split into account
                        SurveyOrientation joinedFaceWSO = m_faceWallSOs[joinedFaceWIdx];
                        m_faceWallIDs.RemoveAt(joinedFaceWIdx);
                        m_faceWallSOs.RemoveAt(joinedFaceWIdx);
                        int splitWallFirstInsertIdx = (joinedFaceWSO == SurveyOrientation.NotFlipped) ? 1 : 0;
                        int splitWallSecondInsertIdx = (splitWallFirstInsertIdx == 0) ? 1 : 0;
                        m_faceWallIDs.Insert(joinedFaceWIdx, splitFaceWalls[splitWallFirstInsertIdx].Id.Value);
                        m_faceWallSOs.Insert(joinedFaceWIdx, joinedFaceWSO);
                        m_faceWallIDs.Insert(joinedFaceWIdx, splitFaceWalls[splitWallSecondInsertIdx].Id.Value);
                        m_faceWallSOs.Insert(joinedFaceWIdx, joinedFaceWSO);
                        if (side == SurveySide.Left) { joinedFaceWIdx++; }

                        // If the wall that was split was the reference wall, replace it with the proper split wall
                        int endPtIdx = RevitSurveyWall.EndPtIndex(side, joinedFaceWSO);
                        if (isJoinedFaceWEqualToRefW)
                        {
                            int newRefWIdx = (endPtIdx == 0) ? 1 : 0;
                            refW = splitFaceWalls[newRefWIdx];
                        }
                    }
                }

                return true;
            }

            private void surveyFaceCtorWorker_removeWallsBeyondIntersection(SurveySide side, int joinedFaceWIdx)
            {
                // Remove walls beyond the corner from the set of face walls
                if (side == SurveySide.Left)
                {
                    if (joinedFaceWIdx > 0)
                    {
                        m_faceWallIDs.RemoveRange(0, joinedFaceWIdx);
                        m_faceWallSOs.RemoveRange(0, joinedFaceWIdx);
                    }
                }
                else
                {
                    if (joinedFaceWIdx < m_faceWallIDs.Count - 1)
                    {
                        m_faceWallIDs.RemoveRange(joinedFaceWIdx + 1, m_faceWallIDs.Count - (joinedFaceWIdx + 1));
                        m_faceWallSOs.RemoveRange(joinedFaceWIdx + 1, m_faceWallSOs.Count - (joinedFaceWIdx + 1));
                    }
                }
            }

            private bool defaultSurveyCorner(SurveySide side, out G2D.Point surveyCorner)
            {
                bool retval = true;
                surveyCorner = null;

                Wall w = EndWall(side);
                if (m_revitDoc.isManuallyCreated(w))
                {  surveyCorner = Corner(side); }
                else
                {
                    Wall cornerW = null;
                    SurveyOrientation cornerWSO = SurveyOrientation.NotFlipped;
                    if (m_insideCornerWallIDs[side.Index] != -1)
                    {
                        cornerW = m_revitDoc.doc.GetElement(new ElementId(m_insideCornerWallIDs[side.Index])) as Wall;
                        cornerWSO = m_insideCornerWallSOs[side.Index];
                        if (!m_revitDoc.isDarkWallFace(cornerW, cornerWSO))
                        { surveyCorner = ContinuationCorner(side); }
                        else
                        { retval = false; }
                    }
                    else if (m_outsideCornerWallIDs[side.Index] != -1
                             && m_parallelWallIDs[side.Index] == -1)
                    {
                        cornerW = m_revitDoc.doc.GetElement(new ElementId(m_outsideCornerWallIDs[side.Index])) as Wall;
                        cornerWSO = m_outsideCornerWallSOs[side.Index];
                        if (!m_revitDoc.isDarkWallFace(cornerW, cornerWSO.Other))
                        { surveyCorner = ContinuationCorner(side); }
                        else if (!m_revitDoc.isDarkWallFace(cornerW, cornerWSO))
                        { surveyCorner = Corner(side); }
                    }
                    else
                    { surveyCorner = ContinuationCorner(side); }
                }

                return retval;
            }

            private Wall wallAtIdx(int i)
            { return (Wall)m_revitDoc.doc.GetElement(new ElementId(m_faceWallIDs[i])); }
        }

        private static class surveySprite
        {
            private const string cm_familySymbolNameBase = "PKNail Survey Sprite ";
            private static string[] cm_familySymbolDirSuffix = { @"LR", @"RL" };

            internal static void Place(RevitDocument revitDoc, G2D.Vector locV, Level lvl, SurveyDirection dir)
            {
                // Delete the old sprite(s)
                DeleteAll(revitDoc);

                // Get the azimuth of the vector we will use to locate the sprite
                // TODO: if the corner angle between joined walls is < 90 deg, calculate
                // the proper azimuth and distance to ensure the sprite does not overlap
                // the joined wall
                SurveySide side;
                G2D.Radians wAz = locV.Azimuth;
                G2D.Radians locAz = locV.Azimuth;
                if (dir == SurveyDirection.LeftToRight)
                {
                    side = SurveySide.Left;
                    locAz -= new G2D.Radians(0.611);  // 35 degrees
                }
                else
                {
                    side = SurveySide.Right;
                    locAz += G2D.Radians.PI + new G2D.Radians(0.611);
                }
                G2D.Point spriteCorner = (side == SurveySide.Left) ? locV.Start : locV.End;
                G2D.Vector spriteLocV = new G2D.Vector(spriteCorner, locAz, 1.75);
                XYZ spriteLoc = Utilities.To3D(spriteLocV.End, 0.0);

                // Get the family symbol
                FamilySymbol sym = null;
                string familySymbolName = cm_familySymbolNameBase + cm_familySymbolDirSuffix[dir.Index];
                FilteredElementCollector coll = new FilteredElementCollector(revitDoc.doc);
                coll.WhereElementIsElementType().OfCategory(BuiltInCategory.OST_GenericModel);
                FilteredElementIterator iter = coll.GetElementIterator();
                iter.Reset();
                while (iter.MoveNext())
                { if (iter.Current.Name == familySymbolName) { sym = iter.Current as FamilySymbol; break; } }

                if (sym == null)
                { throw new HardException(@"Family """ + familySymbolName + @""" must be loaded into this project"); }
                if (!sym.IsActive){ sym.Activate(); }
                FamilyInstance sprite = revitDoc.doc.Create.NewFamilyInstance(spriteLoc, sym, lvl,
                                                                              Autodesk.Revit.DB.Structure.StructuralType.NonStructural);

                // Rotate the sprite to align with the bound wall
                Line axis = Line.CreateBound(spriteLoc, spriteLoc + new XYZ(0.0, 0.0, 1.0));
                sprite.Location.Rotate(axis, (double)wAz);
            }

            internal static void DeleteAll(RevitDocument revitDoc)
            {
                FilteredElementCollector coll = (new FilteredElementCollector(revitDoc.doc)).OfCategory(BuiltInCategory.OST_GenericModel).OfClass(typeof(FamilyInstance));
                List<Element> els = new List<Element>(coll.ToElements());
                foreach (Element el in els)
                {
                    FamilyInstance inst = (FamilyInstance)el;
                    if (inst.Symbol.Name.Substring(0, Math.Min(inst.Symbol.Name.Length, cm_familySymbolNameBase.Length)) == cm_familySymbolNameBase)
                    { revitDoc.doc.Delete(inst.Id); }
                }
            }
        }

        private static class FamilyCategoryUtils
        {
            internal static string GetFamilyCategory(FamilySymbol sym)
            {
                string retval = @"";
                string s = PKNailParameter.FamilyCategory.GetString(sym);
                if (s != @"")
                {
                    retval = FamilyCategory.ToBuiltinFamilyCategory(s);
                }
                else
                {
                    switch (sym.Category.Id.Value)
                    {
                        case (int)BuiltInCategory.OST_Walls:
                            retval = FamilyCategory.Walls;
                            break;
                        case (int)BuiltInCategory.OST_Windows:
                            retval = FamilyCategory.Windows;
                            break;
                        case (int)BuiltInCategory.OST_Doors:
                            retval = FamilyCategory.Doors;
                            break;
                        default:
                            break;
                    }
                }
                return retval;
            }

            internal static string GetFamilyName(FamilySymbol sym)
            {
                string retval = PKNailParameter.Family.GetString(sym);
                if (retval == @"") { retval = sym.Family.Name; }
                return retval;
            }

            internal static List<Element> GetAllTypesInCategory(Document doc, string familyCategory)
            {
                ElementFilter categoryFilter = getCategoryFilter(doc, familyCategory);
                FilteredElementCollector coll = (new FilteredElementCollector(doc)).WhereElementIsElementType().WherePasses(categoryFilter);
                return new List<Element>(coll.ToElements());
            }

            internal static List<Element> GetAllInstancesInCategory(Document doc, string familyCategory)
            {
                ElementFilter categoryFilter = getCategoryFilter(doc, familyCategory);
                FilteredElementCollector coll = (new FilteredElementCollector(doc)).WhereElementIsNotElementType().WherePasses(categoryFilter);
                return new List<Element>(coll.ToElements());
            }

            internal static List<string> GetBuiltinFamilyCategories(Document doc)
            {
                return FamilyCategory.GetBuiltinFamilyCategories();
            }

            internal static List<string> GetCustomFamilyCategories(Document doc)
            {
                List<string> retval = new List<string>();
                FilteredElementCollector coll = (new FilteredElementCollector(doc)).WhereElementIsElementType().OfClass(typeof(FamilySymbol));
                foreach (Element el in coll.ToElements())
                {
                    string familyCategory = PKNailParameter.FamilyCategory.GetString(el);
                    if (familyCategory != @"" && !familyCategory.IsBuiltinFamilyCategory() && !retval.Contains(familyCategory))
                    { retval.Add(familyCategory); }
                }
                retval.Sort();
                return retval;
            }

            private static ElementFilter getCategoryFilter(Document doc, string familyCategory)
            {
                Parameter p = getCategoryParameter(doc);
                ParameterValueProvider pvp = new ParameterValueProvider(p.Id);
                string familyCategoryFilterVal = familyCategory;
                string altFamilyCategoryFilterVal = null;
                if (familyCategory.IsWalls())
                {
                    familyCategoryFilterVal = FamilyCategory.Walls;
                    altFamilyCategoryFilterVal = FamilyCategory.WallsAlt;
                }
                else if (familyCategory.IsDoors())
                {
                    familyCategoryFilterVal = FamilyCategory.Doors;
                    altFamilyCategoryFilterVal = FamilyCategory.DoorsAlt;
                }
                else if (familyCategory.IsWindows())
                {
                    familyCategoryFilterVal = FamilyCategory.Windows;
                    altFamilyCategoryFilterVal = FamilyCategory.WindowsAlt;
                }
                else if (familyCategory.IsLines())
                {
                    familyCategoryFilterVal = FamilyCategory.Lines;
                    altFamilyCategoryFilterVal = FamilyCategory.LinesAlt;
                }

                FilterStringRule rule = new FilterStringRule(pvp, new FilterStringEquals(), familyCategoryFilterVal);
                ElementParameterFilter familyCategoryFilter = new ElementParameterFilter(rule);
                if (altFamilyCategoryFilterVal != null)
                {
                    FilterStringRule altRule = new FilterStringRule(pvp, new FilterStringEquals(), altFamilyCategoryFilterVal);
                    ElementParameterFilter altFamilyCategoryFilter = new ElementParameterFilter(altRule);
                    return new LogicalOrFilter(familyCategoryFilter, altFamilyCategoryFilter);
                }
                else
                { return familyCategoryFilter; }
            }

            private static Parameter getCategoryParameter(Document doc)
            {
                // Grab an arbitrary wall type for the purpose of getting the parameter
                FilteredElementCollector wtcoll = new FilteredElementCollector(doc);
                wtcoll.WhereElementIsElementType().OfCategory(BuiltInCategory.OST_Walls);
                Parameter retval = PKNailParameter.FamilyCategory.GetParameterObject(wtcoll.FirstElement());
                if (retval == null)  { throw new HardException(@"Parameter ""PKNail Family Category"" is undefined in WallType"); }
                return retval;
            }
        }

        private class wallDefinition
        {
            private Curve m_locLine;
            private ElementId m_wtId;
            private RevitSurveyWall.LocationLineType m_locLineType;
            private ElementId m_baseLvlId;
            private ElementId m_connectedLvlId;
            private Distance m_height;
            private Distance m_baseOffset;
            private WallState m_wState;
            private List<wallHostedInstanceDefinition> m_hostedInstances;

            internal wallDefinition(RevitDocument revitDoc, Wall w)
            {
                List<wallHostedInstanceDefinition> instDefs = new List<wallHostedInstanceDefinition>();
                foreach (FamilyInstance inst in RevitSurveyWall.GetHostedInstances(revitDoc.doc, w))
                { instDefs.Add(new wallHostedInstanceDefinition(revitDoc, inst)); }
                Level connectedLvl = RevitSurveyWall.ConnectedLevel(revitDoc.doc, w);
                ElementId connectedLvlId = (connectedLvl != null) ? connectedLvl.Id : null;
                construct((w.Location as LocationCurve).Curve, w.WallType, RevitSurveyWall.GetLocLineType(w), w.LevelId, connectedLvlId,
                          new Distance(RevitSurveyWall.Height(w)), new Distance(RevitSurveyWall.BaseOffset(w)), new WallState(revitDoc.getWallState(w)), instDefs);
            }

            internal wallDefinition(Curve locLine, WallType wt, RevitSurveyWall.LocationLineType locLineType, ElementId baseLvlId,
                                    ElementId connectedLvlId, Distance height, Distance baseOffset, WallState wState)
            {
                construct(locLine, wt, locLineType, baseLvlId, connectedLvlId, height, baseOffset, wState, null);
            }

            internal void CreateNewWall(RevitDocument revitDoc, out Wall newWall, Dictionary<long, long> restoredInstanceMap)
            {
                bool isStructural = revitDoc.docSettings.IsStructuralWalls;
                newWall = Wall.Create(revitDoc.doc, m_locLine, m_wtId, m_baseLvlId, m_height.ValueImperial, m_baseOffset.ValueImperial, false, isStructural);
                revitDoc.doc.Regenerate();
                revitDoc.setWallState(newWall, m_wState);
                RevitSurveyWall.SetLocLineType(newWall, m_locLineType);
                if (m_connectedLvlId != null)  { newWall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(m_connectedLvlId); }

                if (m_hostedInstances != null)
                {
                    foreach (wallHostedInstanceDefinition def in m_hostedInstances)
                    {
                        FamilyInstance restoredInst = def.CreateInstanceOnNewHostWallAbsolute(revitDoc, newWall);
                        if (def.OriginalElementId != -1)
                        {
                            if (restoredInstanceMap.ContainsValue(def.OriginalElementId))
                            {
                                foreach (int key in restoredInstanceMap.Keys)
                                { if (restoredInstanceMap[key] == def.OriginalElementId)  { restoredInstanceMap[key] = restoredInst.Id.Value; break; } }
                            }
                            else
                            { restoredInstanceMap.Add(def.OriginalElementId, restoredInst.Id.Value); }
                        }
                    }
                }
            }

            internal Curve LocLine { get { return m_locLine; } set { m_locLine = value; } }

            internal ElementId WallTypeId { get { return m_wtId; } set { m_wtId = new ElementId(value.Value); } }

            internal ElementId BaseLevelId { get { return m_baseLvlId; } set { m_baseLvlId = new ElementId(value.Value); } }

            internal ElementId ConnectedLevelId
            {
                get { return m_connectedLvlId; }
                set { m_connectedLvlId = (value != null) ? new ElementId(value.Value) : null; }
            }

            internal WallState WallState { get { return m_wState; } set { m_wState = value; } }

            internal List<wallHostedInstanceDefinition> HostedInstances { get { return m_hostedInstances; } set { m_hostedInstances = value; } }

            private void construct(Curve locLine, WallType wt, RevitSurveyWall.LocationLineType locLineType, ElementId baseLvlId,
                                   ElementId connectedLvlId, Distance height, Distance baseOffset, WallState wState,
                                   List<wallHostedInstanceDefinition> hostedInstances)
            {
                m_locLine = locLine;
                m_wtId = wt.Id;
                m_locLineType = locLineType;
                m_baseLvlId = baseLvlId;
                m_connectedLvlId = connectedLvlId;
                m_height = height;
                m_baseOffset = baseOffset;
                m_wState = wState;
                m_hostedInstances = (hostedInstances == null) ? null : hostedInstances;
            }
        }

        private static class wallHostedInstance
        {
            internal static Wall Host(FamilyInstance inst)
            {
                Wall retval = null;
                Parameter p = inst.get_Parameter(BuiltInParameter.HOST_ID_PARAM);
                if (p != null)
                {
                    ElementId wId = p.AsElementId();
                    retval = (Wall)inst.Document.GetElement(wId);
                }
                else
                { throw new HardException("Survey family \"" + inst.Symbol.Name + "\" does not have a \"Host\" parameter"); }
                return retval;
            }

            internal static bool isDoor(FamilyInstance inst)
            { return (inst.Category.Id.Value == (int)BuiltInCategory.OST_Doors); }

            internal static bool isWindow(FamilyInstance inst)
            { return (inst.Category.Id.Value == (int)BuiltInCategory.OST_Windows); }

            internal static double Width(RevitDocument revitDoc, FamilyInstance inst)
            {
                Parameter p = inst.Symbol.LookupParameter(@"Width");
                return (p != null) ? p.AsDouble() : 0.0;
            }

            internal static double Height(RevitDocument revitDoc, FamilyInstance inst)
            {
                Parameter p = inst.Symbol.LookupParameter(@"Height");
                return (p != null) ? p.AsDouble() : 0.0;
            }

            internal static double Depth(RevitDocument revitDoc, FamilyInstance inst)
            {
                Parameter p = inst.Symbol.LookupParameter(@"Depth");
                return (p != null) ? p.AsDouble() : 0.0;
            }
        }

        private class wallHostedInstanceDefinition
        {
            protected string m_familyCategory;
            protected long m_origElementId;
            protected Wall m_host;
            protected Level m_level;
            protected Distance m_levelAbsElevation;
            protected long m_familySymbolID;
            protected string m_pkFamilyName;
            protected bool m_isFlipHand;
            protected bool m_isFlipFacing;
            protected G2D.Point m_instAbsLocationXY;
            protected Distance m_instAbsElevation;
            protected Distance m_width;
            protected Distance m_height;
            protected Distance m_depth;

            internal wallHostedInstanceDefinition(RevitDocument revitDoc, FamilyInstance inst)
            {
                m_origElementId = inst.Id.Value;
                m_host = wallHostedInstance.Host(inst);
                m_level = revitDoc.doc.GetElement(inst.LevelId) as Level;
                m_levelAbsElevation = new Distance(m_level.Elevation, Distance.Units.RevitInternal);
                m_isFlipHand = inst.HandFlipped;
                m_isFlipFacing = inst.FacingFlipped;

                FamilySymbol symbol = inst.Symbol;

                LocationPoint locPt = (LocationPoint)inst.Location;
                XYZ locPtXYZ = locPt.Point;
                m_instAbsLocationXY = new PKNailForRevit.Geometry2D.Point(locPtXYZ.X, locPtXYZ.Y);
                m_instAbsElevation = new Distance(locPtXYZ.Z, Distance.Units.RevitInternal);

                m_familySymbolID = inst.Symbol.Id.Value;
                m_familyCategory = FamilyCategoryUtils.GetFamilyCategory(symbol);
                m_pkFamilyName = FamilyCategoryUtils.GetFamilyName(symbol);

                m_width = new Distance(wallHostedInstance.Width(revitDoc, inst), Distance.Units.RevitInternal);
                m_height = new Distance(wallHostedInstance.Height(revitDoc, inst), Distance.Units.RevitInternal);
                m_depth = new Distance(wallHostedInstance.Depth(revitDoc, inst), Distance.Units.RevitInternal);
            }

            internal wallHostedInstanceDefinition(RevitDocument revitDoc, surveyFace f, SurveySide side,
                                                  string familyCategory, WallHostedObjectSurveyParams op)
            {
                Distance locInWall = op.Location;
                Distance width = op.GetWidth();
                if (!width.IsNoValue && !op.GetIsMeasureLocationCentered())
                { locInWall += new Distance(width.ValueRevitInternal / 2.0, Distance.Units.RevitInternal); }

                Wall host;
                SurveyOrientation hostSO;
                G2D.Point absLocOnCenterline;
                G2D.Point absLocOnFace;
                Dictionary<long, long> ignoreRestoredInstanceMap = new Dictionary<long, long>();
                f.LocateInWall(locInWall, side, Distance.NoValue, false, false,
                               out host, out hostSO, out absLocOnFace, out absLocOnCenterline, ignoreRestoredInstanceMap);

                // Handle a special case: if the sill height is 0 and the object height is the same as the wall height,
                // the geometry of the wall will be obscured, which makes it impossible to determine the true extent of
                // the wall (in later calls to RevitSurveyWall.WallEdge()).  So we have to fudge things a bit by reducing
                // the height of the object so that we leave a sliver of wall above the object, preserving the wall's
                // solid geometry
                Distance sillHeight = op.GetSillHeight();
                if (   (sillHeight.IsNoValue || sillHeight.IsZero)
                    && op.GetHeight() >= op.HostWallHeight)
                { op.SetHeight(op.HostWallHeight - revitDoc.docSettings.FullOpeningTopElevationOffset); }

                construct(revitDoc, host, hostSO, absLocOnCenterline, f.LevelOffset, familyCategory, op);
            }

            internal wallHostedInstanceDefinition(wallHostedInstanceDefinition from)
            {
                m_familyCategory = from.m_familyCategory;
                m_origElementId = from.m_origElementId;
                m_host = from.m_host;
                m_level = from.m_level;
                m_levelAbsElevation = from.m_levelAbsElevation;
                m_familySymbolID = from.m_familySymbolID;
                m_pkFamilyName = from.m_pkFamilyName;
                m_isFlipHand = from.m_isFlipHand;
                m_isFlipFacing = from.m_isFlipFacing;
                m_instAbsLocationXY = new G2D.Point(from.m_instAbsLocationXY);
                m_instAbsElevation = new Distance(from.m_instAbsElevation);
                m_width = new Distance(from.m_width);
                m_height = new Distance(from.m_height);
                m_depth = new Distance(from.m_depth);
            }

            internal FamilyInstance CreateInstance(RevitDocument revitDoc)
            {
                FamilyInstance retval;

                FamilySymbol symbol = revitDoc.getFamilySymbol(m_familySymbolID, m_familyCategory, m_pkFamilyName, m_width, m_height, m_depth);
                if (!symbol.IsActive) { symbol.Activate(); }
                XYZ loc = Utilities.To3D(m_instAbsLocationXY, m_instAbsElevation.ValueRevitInternal);
                retval = revitDoc.doc.Create.NewFamilyInstance(loc, symbol, m_host, m_level,
                                                               Autodesk.Revit.DB.Structure.StructuralType.NonStructural);

                Parameter sillHeightP = retval.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM);
                if (sillHeightP != null)  { sillHeightP.Set(this.LevelElevation.ValueRevitInternal); }
                Parameter PKElevationP = PKNailParameter.FamilyInstanceElevation.GetParameterObject(retval);
                if (PKElevationP != null) { PKElevationP.Set(this.LevelElevation.ValueRevitInternal); }

                if (m_isFlipHand && retval.CanFlipHand)  { retval.flipHand(); }
                if (m_isFlipFacing)
                {
                    if (!retval.CanFlipFacing)
                    {
                        throw new HardException(@"Family """ + symbol.Family.FamilyCategory.Name + @":" + symbol.Family.Name
                                                + @""" does not contain a ""Flip Facing"" control");
                    }
                    retval.flipFacing();
                }

                try
                {
                    Parameter p = retval.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM);
                    if (p != null) { p.Set(m_level.Id); }
                }
                catch (Autodesk.Revit.Exceptions.InvalidOperationException) { }

                return retval;
            }

            internal FamilyInstance CreateInstanceOnNewHostWallRelative(RevitDocument revitDoc, Wall newHost)
            {
                double origLevelElevation = m_instAbsElevation.ValueRevitInternal - m_levelAbsElevation.ValueRevitInternal;
                wallHostedInstanceDefinition defOnNewHost = new wallHostedInstanceDefinition(this);
                defOnNewHost.m_host = newHost;
                Level newHostLvl = RevitSurveyWall.BaseLevel(newHost);
                defOnNewHost.m_instAbsElevation = new Distance(newHostLvl.Elevation + origLevelElevation, Distance.Units.RevitInternal);
                defOnNewHost.setLevel(revitDoc);
                return defOnNewHost.CreateInstance(revitDoc);
            }

            internal FamilyInstance CreateInstanceOnNewHostWallAbsolute(RevitDocument revitDoc, Wall newHost)
            {
                wallHostedInstanceDefinition defOnNewHost = new wallHostedInstanceDefinition(this);
                defOnNewHost.m_host = newHost;
                return defOnNewHost.CreateInstance(revitDoc);
            }

            internal long OriginalElementId  { get { return m_origElementId; } }

            internal Wall Host  { get { return m_host; } }

            internal Level Level  { get { return m_level; } }

            internal Distance Width
            {
                get { return m_width; }
                set { m_width = new Distance(value); }
            }

            internal Distance Height
            {
                get { return m_height; }
                set { m_height = new Distance(value); }
            }

            internal Distance LevelElevation
            {
                get { return m_instAbsElevation - m_levelAbsElevation; }
                set { m_instAbsElevation = m_levelAbsElevation + value; }
            }

            internal Distance AbsElevation  { get { return m_instAbsElevation; } }

            internal G2D.Point AbsoluteLocationXY 
            {
                get { return m_instAbsLocationXY; }
                set { m_instAbsLocationXY = value; }
            }

            internal bool IsFlipFacing
            {
                get { return m_isFlipFacing; }
                set { m_isFlipFacing = value; }
            }

            internal bool IsFlipHand
            {
                get { return m_isFlipHand; }
                set { m_isFlipHand = value; }
            }

            internal string FamilyCategory  { get { return m_familyCategory; } }

            internal static void ReconcileSurveyFamilyTypeDefs(RevitDocument revitDoc, string familyCategory, WallHostedObjectSurveyParams op)
            {
                // Remember the family type def pointed to by the family type index
                // Since the order of the defs could change, the index might need to change too
                SurveyFamilyDef indexedDef = null;
                if (   op.SurveyFamilyDefIdx >= 0
                    && op.SurveyFamilyDefIdx < op.ProjectSurveyFamilyDefs.Count)
                { indexedDef = op.ProjectSurveyFamilyDefs[op.SurveyFamilyDefIdx]; }

                // Get a list of the project's family types
                Dictionary<string, FamilySymbol> symbolTypeMap = new Dictionary<string, FamilySymbol>();
                List<Element> els = FamilyCategoryUtils.GetAllTypesInCategory(revitDoc.doc, familyCategory);
                foreach (Element el in els)
                {
                    FamilySymbol sym = (FamilySymbol)el;
                    string familyName = FamilyCategoryUtils.GetFamilyName(sym);
                    if (!symbolTypeMap.ContainsKey(familyName))
                    { symbolTypeMap.Add(familyName, (FamilySymbol)el); }
                }

                SortedDictionary<int, SurveyFamilyDef> reconciledDefs = new SortedDictionary<int, SurveyFamilyDef>();
                int maxOrdinal = symbolTypeMap.Count;

                foreach (string familyName in symbolTypeMap.Keys)
                {
                    FamilySymbol sym = symbolTypeMap[familyName];

                    // Find the matching family type def in the data (if any)
                    SurveyFamilyDef matchingDef = null;
                    foreach (SurveyFamilyDef def in op.ProjectSurveyFamilyDefs)
                    { if (def.Name == familyName) { matchingDef = def; break; } }

                    int ordinal = PKNailParameter.FamilyOrder.GetInteger(symbolTypeMap[familyName]);
                    if (ordinal == -1)  // Means this family type is disabled
                    { continue; }

                    if (ordinal == 0 || reconciledDefs.ContainsKey(ordinal)) { ordinal = ++maxOrdinal; }
                    else { maxOrdinal = Math.Max(ordinal, maxOrdinal); }

                    SurveyFamilyDef reconciledDef = matchingDef;
                    if (reconciledDef == null)  // There was no match
                    {
                        reconciledDef = new SurveyFamilyDef();
                        reconciledDef.Name = familyName;
                    }

                    reconciledDef.FixedDimensionsVal = SurveyFamilyDef.FixedDimensions.None;
                    string[] fixedDims = PKNailParameter.FixedDimensions.GetString(sym).ToLower().Split(':');
                        
                    foreach (string fixedDim in fixedDims)
                    {
                        if (fixedDim == @"width")
                        { reconciledDef.FixedDimensionsVal |= SurveyFamilyDef.FixedDimensions.Width; }
                        else if (fixedDim == @"height")
                        { reconciledDef.FixedDimensionsVal |= SurveyFamilyDef.FixedDimensions.Height; }
                        else if (fixedDim == @"depth")
                        { reconciledDef.FixedDimensionsVal |= SurveyFamilyDef.FixedDimensions.Depth; }
                        else if (fixedDim == @"sillheight")
                        { reconciledDef.FixedDimensionsVal |= SurveyFamilyDef.FixedDimensions.SillHeight; }
                    }

                    Parameter p = sym.LookupParameter(@"Width");
                    if (p != null)
                    {
                        if (p.IsReadOnly)
                        { reconciledDef.FixedDimensionsVal |= SurveyFamilyDef.FixedDimensions.Width; }
                        if ((reconciledDef.FixedDimensionsVal & SurveyFamilyDef.FixedDimensions.Width) != SurveyFamilyDef.FixedDimensions.None)
                        { reconciledDef.Width = new Distance(p.AsDouble(), Distance.Units.RevitInternal); }
                    }
                    else
                    { reconciledDef.FixedDimensionsVal |= SurveyFamilyDef.FixedDimensions.Width; }

                    p = sym.LookupParameter(@"Height");
                    if (p != null)
                    {
                        if (p.IsReadOnly)
                        { reconciledDef.FixedDimensionsVal |= SurveyFamilyDef.FixedDimensions.Height; }
                        if ((reconciledDef.FixedDimensionsVal & SurveyFamilyDef.FixedDimensions.Height) != SurveyFamilyDef.FixedDimensions.None)
                        { reconciledDef.Height = new Distance(p.AsDouble(), Distance.Units.RevitInternal); }
                    }
                    else
                    { reconciledDef.FixedDimensionsVal |= SurveyFamilyDef.FixedDimensions.Height; }

                    p = sym.LookupParameter(@"Depth");
                    if (p != null)
                    {
                        if (p.IsReadOnly)
                        { reconciledDef.FixedDimensionsVal |= SurveyFamilyDef.FixedDimensions.Depth; }
                        if ((reconciledDef.FixedDimensionsVal & SurveyFamilyDef.FixedDimensions.Depth) != SurveyFamilyDef.FixedDimensions.None)
                        { reconciledDef.Depth = new Distance(p.AsDouble(), Distance.Units.RevitInternal); }
                    }
                    else
                    { reconciledDef.FixedDimensionsVal |= SurveyFamilyDef.FixedDimensions.Depth; }

                    reconciledDef.LateralOrientationTypeVal = SurveyFamilyDef.LateralOrientationType.None;
                    string s = PKNailParameter.FamilyLateralOrientation.GetString(sym);
                    if (s == "Hinged")
                    { reconciledDef.LateralOrientationTypeVal = SurveyFamilyDef.LateralOrientationType.Hinged; }
                    else if (s == "Sliding")
                    { reconciledDef.LateralOrientationTypeVal = SurveyFamilyDef.LateralOrientationType.Sliding; }

                    reconciledDef.FacingOrientationTypeVal = SurveyFamilyDef.FacingOrientationType.None;
                    s = PKNailParameter.FamilyFacingOrientation.GetString(sym);
                    if (s == "Swinging")
                    { reconciledDef.FacingOrientationTypeVal = SurveyFamilyDef.FacingOrientationType.Swinging; }
                    else if (s == "Facing")
                    { reconciledDef.FacingOrientationTypeVal = SurveyFamilyDef.FacingOrientationType.Facing; }

                    reconciledDef.IsMeasureLocationCentered = (PKNailParameter.FamilyMeasureLocationCentered.GetInteger(sym) != 0);

                    reconciledDefs.Add(ordinal, reconciledDef);
                }

                op.ProjectSurveyFamilyDefs.Clear();
                foreach (SurveyFamilyDef def in reconciledDefs.Values)
                { op.ProjectSurveyFamilyDefs.Add(def); }

                // Reset the index; if the family type is no longer in the project, reset to 0
                int newIdx = op.ProjectSurveyFamilyDefs.IndexOf(indexedDef);
                op.SurveyFamilyDefIdx = (newIdx != -1) ? newIdx : 0;
            }

            private void construct(RevitDocument revitDoc, Wall host, SurveyOrientation hostSO,
                                   G2D.Point locXY, Distance levelOffset, string familyCategory, WallHostedObjectSurveyParams op)
            {
                m_familyCategory = familyCategory;
                m_origElementId = -1;
                m_instAbsLocationXY = locXY;

                SurveyFamilyDef familyTypeDef = null;
                if (op.ProjectSurveyFamilyDefs.Count > 0 && op.SurveyFamilyDefIdx < op.ProjectSurveyFamilyDefs.Count)
                { familyTypeDef = op.ProjectSurveyFamilyDefs[op.SurveyFamilyDefIdx]; }
                else
                { throw new HardException(@"Survey Family TypeDef Index is invalid"); }

                m_host = host;
                m_familySymbolID = -1;
                m_pkFamilyName = familyTypeDef.Name;
                m_width = op.GetWidth();
                m_height = op.GetHeight();
                m_depth = op.GetDepth();

                // Find the level for the instance based on the host and sill height
                Distance levelElevation = (!op.GetSillHeight().IsNoValue) ? op.GetSillHeight() : Distance.Zero;
                if (levelOffset != Distance.NoValue)  { levelElevation += levelOffset; }
                Level hostLvl = RevitSurveyWall.BaseLevel(m_host);
                m_instAbsElevation = new Distance(hostLvl.Elevation, Distance.Units.RevitInternal) + levelElevation;
                setLevel(revitDoc);

                if (familyTypeDef.LateralOrientationTypeVal != SurveyFamilyDef.LateralOrientationType.None)
                {
                    m_isFlipHand = (   (hostSO == SurveyOrientation.NotFlipped && op.GetLateralOrientation() == 0)
                                    || (hostSO == SurveyOrientation.Flipped && op.GetLateralOrientation() == 1));
                }
                else
                { m_isFlipHand = (hostSO == SurveyOrientation.NotFlipped); }

                if (familyTypeDef.FacingOrientationTypeVal != SurveyFamilyDef.FacingOrientationType.None)
                {
                    m_isFlipFacing = (   (hostSO == SurveyOrientation.NotFlipped && op.GetFacingOrientation() == 1)
                                      || (hostSO == SurveyOrientation.Flipped && op.GetFacingOrientation() == 0));
                }
                else  // familyTypeDef.FacingOrientation == None
                { m_isFlipFacing = (hostSO == SurveyOrientation.NotFlipped); }
            }

            private void setLevel(RevitDocument revitDoc)
            {
                m_level = null;
                List<Level> levels = revitDoc.getLevels();
                if (levels.Count == 0) { return; }
                else if (levels.Count == 1) { m_level = levels[0]; }
                else
                {
                    for (int i = 0; i < levels.Count; i++)
                    { if (levels[i + 1].Elevation > m_instAbsElevation.ValueRevitInternal) { m_level = levels[i]; break; } }
                    if (m_level == null)  { m_level = levels[levels.Count - 1]; }
                }
                if (m_level != null)  { m_levelAbsElevation = new Distance(m_level.Elevation, Distance.Units.RevitInternal); }
            }
        }

        private class joinedWallGeometry
        {
            internal Wall ParallelWall = null;
            internal SurveyOrientation ParallelWallSO;
            internal Wall InsideWall = null;
            internal SurveyOrientation InsideWallSO;
            internal Wall OutsideWall = null;
            internal SurveyOrientation OutsideWallSO;
        }

        private class joinedWallInfo
        {
            internal Wall wall = null;
            internal SurveyOrientation so;
            internal G2D.Radians angle = null;
            internal Distance distFromRefWall = null;
            internal joinedWallInfo(Wall w, SurveyOrientation s, G2D.Radians a, Distance d)
            { wall = w; so = s; angle = a; distFromRefWall = d; }
        }

        private joinedWallGeometry getJoinedWalls(Wall w, Level lvl, SurveySide side, SurveyOrientation so)
        {
            joinedWallGeometry retval = new joinedWallGeometry();

            G2D.Radians parallelAngleTolerance = G2D.Radians.FromDegrees(0.5);
            G2D.Radians overlappingAngleTolerance = G2D.Radians.PI - parallelAngleTolerance;
            List<int> parallelAngleIdxs = new List<int>();
            G2D.Radians maxAngleRight = G2D.Radians.Zero; int maxAngleRightIdx = -1;
            G2D.Radians maxAngleLeft = G2D.Radians.Zero; int maxAngleLeftIdx = -1;

            int endPtIdx = RevitSurveyWall.EndPtIndex(side, so);
            int otherEndPtIdx = endPtIdx == 0 ? 1 : 0;
            G2D.Radians wRefAngle = RevitSurveyWall.Azimuth(w, so);
            if (side == SurveySide.Left) { wRefAngle += G2D.Radians.PI; }

            List<Wall> allJoined = RevitSurveyWall.JoinedWalls(w, lvl, endPtIdx);
            if (allJoined.Count == 0)  { return retval; }  // Save some time

            // Handle a special case (Case 361): detect whether at joined wall is "behind" the reference wall
            // (i.e. parallel at "zero" degrees instead of 180 degrees).  This condition can happen when two
            // parallel walls are joined to a third, thicker wall at its "butt end"
            G2D.Vector frontEdge = RevitSurveyWall.WallEdge(w, so, true);
            double rayOriginLoc = (side == SurveySide.Left) ?
                Distance.VerySmall.ValueRevitInternal : frontEdge.Length - Distance.VerySmall.ValueRevitInternal;
            G2D.Point rayOriginPt = frontEdge.Start.Translate(new G2D.Vector(new G2D.Point(), frontEdge.Azimuth, rayOriginLoc));
            G2D.Radians rayDir = frontEdge.Azimuth + G2D.Radians.PIOverTwo;
            List<long> excludeRefWall = new List<long>();
            excludeRefWall.Add(w.Id.Value);
            Distance tenFeet = new Distance(10.0, Distance.Units.Imperial);
            Reference behindWallRef = surveyWallWorker_findClosestWallRef(rayOriginPt, rayDir, lvl, excludeRefWall, tenFeet.ValueRevitInternal, Distance.VerySmall.ValueRevitInternal);

            List<joinedWallInfo> jWInfo = new List<joinedWallInfo>();
            SurveyOrientation jwSO;
            G2D.Radians wAz = RevitSurveyWall.Azimuth(w, so);
            G2D.Vector wCL = RevitSurveyWall.Centerline(w);
            Distance distToOutsideEdge = RevitSurveyWall.Width(w) * 0.5;
            foreach (Wall jw in allJoined)
            {
                G2D.Vector jwLocV = RevitSurveyWall.LocationLine(jw);
                G2D.Vector wCLTojwEndPt0 = new G2D.Vector(wCL.Project(jwLocV.EndPt(0)), jwLocV.EndPt(0));
                G2D.Vector wCLTojwEndPt1 = new G2D.Vector(wCL.Project(jwLocV.EndPt(1)), jwLocV.EndPt(1));
                Distance jwEndPt0WallDistance = new Distance(wCLTojwEndPt0.Length, Distance.Units.PKInternal);
                Distance jwEndPt1WallDistance = new Distance(wCLTojwEndPt1.Length, Distance.Units.PKInternal);

                G2D.Radians jwAz = RevitSurveyWall.Azimuth(jw, so);
                if (jwAz.IsParallelTo(wAz))
                {
                    // Joined wall is parallel to the reference wall

                    // Handle the special case where the parallel wall is "behind" the reference wall
                    if ( behindWallRef != null && doc.GetElement(behindWallRef).Id == jw.Id)
                    { continue; }

                    jwSO = new SurveyOrientation(so);
                    if (jwAz != wAz) { jwSO.Flip(); }
                    jWInfo.Add(new joinedWallInfo(jw, jwSO, G2D.Radians.Zero, Distance.Zero));
                }
                else if (jwEndPt0WallDistance <= distToOutsideEdge && jwEndPt1WallDistance <= distToOutsideEdge)
                {
                    // The joined all is a small wall segment ("pygmy" wall) that can and should be ignored
                    continue;
                }

                else
                {
                    if ((jwEndPt0WallDistance > distToOutsideEdge && jwEndPt1WallDistance > distToOutsideEdge)
                        && wCLTojwEndPt0.Azimuth == wCLTojwEndPt1.Azimuth)
                    {
                        if (wCLTojwEndPt0.Length > wCLTojwEndPt1.Length)
                        {
                            jwSO = new SurveyOrientation(so);
                            if (endPtIdx == 1) { jwSO.Flip(); }
                            jWInfo.Add(new joinedWallInfo(jw, jwSO, jwLocV.Reverse().Azimuth - wRefAngle, jwEndPt0WallDistance));
                        }
                        else
                        {
                            jwSO = new SurveyOrientation(so);
                            if (endPtIdx == 0) { jwSO.Flip(); }
                            jWInfo.Add(new joinedWallInfo(jw, jwSO, jwLocV.Azimuth - wRefAngle, jwEndPt1WallDistance));
                        }
                    }
                    else
                    {
                        if (jwEndPt0WallDistance > distToOutsideEdge)
                        {
                            jwSO = new SurveyOrientation(so);
                            if (endPtIdx == 1) { jwSO.Flip(); }
                            jWInfo.Add(new joinedWallInfo(jw, jwSO, jwLocV.Reverse().Azimuth - wRefAngle, jwEndPt0WallDistance));
                        }
                        if (jwEndPt1WallDistance > distToOutsideEdge)
                        {
                            jwSO = new SurveyOrientation(so);
                            if (endPtIdx == 0) { jwSO.Flip(); }
                            jWInfo.Add(new joinedWallInfo(jw, jwSO, jwLocV.Azimuth - wRefAngle, jwEndPt1WallDistance));
                        }
                    }
                }
            }

            for (int jwiIdx = 0; jwiIdx < jWInfo.Count; jwiIdx++)
            {
                G2D.Radians jwAdjAngle = jWInfo[jwiIdx].angle;
                if (jwAdjAngle < G2D.Radians.Zero) { jwAdjAngle = G2D.Radians.TwoPIPlus(jwAdjAngle); }

                if (jwAdjAngle > G2D.Radians.PI)
                {
                    jwAdjAngle = G2D.Radians.TwoPIMinus(jwAdjAngle);
                    if ((jwAdjAngle < overlappingAngleTolerance)
                        && ((jwAdjAngle == maxAngleRight
                                && maxAngleRightIdx > -1
                                && jWInfo[jwiIdx].distFromRefWall > jWInfo[maxAngleRightIdx].distFromRefWall)
                            || (jwAdjAngle > maxAngleRight)))
                    { maxAngleRight = jwAdjAngle; maxAngleRightIdx = jwiIdx; }
                }
                else
                {
                    if ((jwAdjAngle < overlappingAngleTolerance)
                        && ((jwAdjAngle == maxAngleLeft
                                && maxAngleLeftIdx > -1
                                && jWInfo[jwiIdx].distFromRefWall > jWInfo[maxAngleLeftIdx].distFromRefWall)
                            || (jwAdjAngle > maxAngleLeft)))
                    { maxAngleLeft = jwAdjAngle; maxAngleLeftIdx = jwiIdx; }
                }

                if (jwAdjAngle <= parallelAngleTolerance)
                { parallelAngleIdxs.Add(jwiIdx); }
            }

            if (parallelAngleIdxs.Count > 0)
            {
                int parallelAngleIdx;
                if (parallelAngleIdxs.Count == 1)
                { parallelAngleIdx = parallelAngleIdxs[0]; }
                else
                {
                    G2D.Radians protrudingEdgeAz = RevitSurveyWall.Azimuth(w, so) - G2D.Radians.PIOverTwo;
                    List<G2D.Point> parallelEdgePts = new List<G2D.Point>();
                    foreach (int i in parallelAngleIdxs)
                    { parallelEdgePts.Add(RevitSurveyWall.WallEdge(jWInfo[i].wall, jWInfo[i].so, true).End); }
                    parallelAngleIdx = parallelEdgePts.IndexOf(Utilities.FarthestPointInDirection(protrudingEdgeAz, parallelEdgePts));
                }

                retval.ParallelWall = jWInfo[parallelAngleIdx].wall;
                retval.ParallelWallSO = jWInfo[parallelAngleIdx].so;
            }

            if (maxAngleRightIdx != -1 && !parallelAngleIdxs.Contains(maxAngleRightIdx))
            {
                if (side == SurveySide.Right)
                {
                    retval.InsideWall = jWInfo[maxAngleRightIdx].wall;
                    retval.InsideWallSO = jWInfo[maxAngleRightIdx].so;
                }
                else
                {
                    retval.OutsideWall = jWInfo[maxAngleRightIdx].wall;
                    retval.OutsideWallSO = jWInfo[maxAngleRightIdx].so;
                }
            }

            if (maxAngleLeftIdx != -1 && !parallelAngleIdxs.Contains(maxAngleLeftIdx))
            {
                if (side == SurveySide.Right)
                {
                    retval.OutsideWall = jWInfo[maxAngleLeftIdx].wall;
                    retval.OutsideWallSO = jWInfo[maxAngleLeftIdx].so;
                }
                else
                {
                    retval.InsideWall = jWInfo[maxAngleLeftIdx].wall;
                    retval.InsideWallSO = jWInfo[maxAngleLeftIdx].so;
                }
            }

            // Handle a special case (Case 374): Detect a three-way join, where one of the joined walls
            // is not detected because is is not joined (in the Revit sense) to the reference wall, but
            // is joined to a wall that is in turn joined to the reference wall
            if (retval.OutsideWall != null && retval.InsideWall == null)
            {
                rayOriginLoc = (side == SurveySide.Left) ? Distance.VerySmall.ValueRevitInternal
                                                         : frontEdge.Length - Distance.VerySmall.ValueRevitInternal;
                rayOriginPt = frontEdge.Start.Translate(new G2D.Vector(new G2D.Point(), frontEdge.Azimuth, rayOriginLoc));
                G2D.Radians faceOffsetAz = frontEdge.Azimuth - G2D.Radians.PIOverTwo;
                rayOriginPt = rayOriginPt.Translate(new G2D.Vector(new G2D.Point(), faceOffsetAz, Distance.VerySmall.ValueRevitInternal));
                rayDir = (side == SurveySide.Left) ? frontEdge.Azimuth + G2D.Radians.PI : frontEdge.Azimuth;
                Reference intRef = surveyWallWorker_findClosestWallRef(rayOriginPt, rayDir, lvl, null, tenFeet.ValueRevitInternal, 0.0);
                if (intRef != null)
                {
                    Element wEl = doc.GetElement(intRef);
                    GeometryObject wGO = wEl.GetGeometryObjectFromReference(intRef);
                    if (wEl is Wall && wGO is PlanarFace)
                    {
                        Wall intW = wEl as Wall;
                        PlanarFace intWPF = wGO as PlanarFace;
                        bool isIntWJoinedToRefWall = false;
                        foreach (Wall wJoinedW in allJoined)
                        { if (wJoinedW.Id == intW.Id) { isIntWJoinedToRefWall = true; break; } }
                        if (!isIntWJoinedToRefWall)
                        {
                            int intWJoinPtIdx = RevitSurveyWall.JoinPtIdx(intW, w, true);
                            bool isIntWJoined = false;
                            List<Wall> intWJoinedWalls = RevitSurveyWall.JoinedWalls(intW, lvl, intWJoinPtIdx);
                            foreach (Wall intWJoinedW in intWJoinedWalls)
                            {
                                foreach (Wall wJoinedW in allJoined)
                                {
                                    if (wJoinedW.Id == intWJoinedW.Id
                                        && RevitSurveyWall.JoinPtIdx(wJoinedW, intW, true) == RevitSurveyWall.JoinPtIdx(wJoinedW, w, true))
                                    {
                                        isIntWJoined = true; break;
                                    }
                                }
                                if (isIntWJoined) { break; }
                            }
                            if (isIntWJoined)
                            {
                                retval.InsideWall = intW;
                                retval.InsideWallSO = Utilities.SurveyOrientationFromFace(intW, intWPF);
                            }
                        }
                    }
                }
            }

            return retval;
        }

        private FamilySymbol getFamilySymbol(long origSymbolID, string familyCategory, string pkFamilyName,
                                             Distance width, Distance height, Distance depth)
        {
            FamilySymbol retval = null;
            FamilySymbol templateSymbol = null;
            List<FamilySymbol> allFamilySymbols = new List<FamilySymbol>();

            if (origSymbolID != -1)
            {
                FamilySymbol origSymbol = doc.GetElement(new ElementId(origSymbolID)) as FamilySymbol;
                Parameter p = origSymbol.LookupParameter(@"Width");
                Distance origSymbolWidth = (p != null) ? new Distance(p.AsDouble(), Distance.Units.RevitInternal) : Distance.NoValue;
                p = origSymbol.LookupParameter(@"Height");
                Distance origSymbolHeight = (p != null) ? new Distance(p.AsDouble(), Distance.Units.RevitInternal) : Distance.NoValue;
                p = origSymbol.LookupParameter(@"Depth");
                Distance origSymbolDepth = (p != null) ? new Distance(p.AsDouble(), Distance.Units.RevitInternal) : Distance.NoValue;
                if (   (origSymbolWidth.IsNoValue || origSymbolWidth == width)
                    && (origSymbolHeight.IsNoValue || origSymbolHeight == height)
                    && (origSymbolDepth.IsNoValue || origSymbolDepth == depth) )
                { retval = origSymbol; }
                else
                {
                    templateSymbol = origSymbol;
                    foreach (ElementId eid in origSymbol.Family.GetFamilySymbolIds())
                    { allFamilySymbols.Add((FamilySymbol)doc.GetElement(eid)); }
                }
            }

            else
            {
                foreach (Element elType in FamilyCategoryUtils.GetAllTypesInCategory(doc, familyCategory))
                {
                    FamilySymbol sym = elType as FamilySymbol;
                    if (FamilyCategoryUtils.GetFamilyName(sym) == pkFamilyName)
                    { allFamilySymbols.Add(sym); }
                }
                if (allFamilySymbols.Count == 0)
                { throw new HardException(@"No family named """ + pkFamilyName + @""" in PKNail family category """
                                          + familyCategory + @""""); }

                if (width.IsNoValue)
                { retval = allFamilySymbols[0]; }
                else
                { templateSymbol = allFamilySymbols[0]; }
            }

            if (retval == null)
            {
                // Find the family type with matching width and height
                foreach (FamilySymbol fs in allFamilySymbols)
                {
                    Parameter p = fs.LookupParameter(@"Width");
                    Distance elWidth = (p != null) ? new Distance(p.AsDouble(), Distance.Units.RevitInternal) : Distance.NoValue;
                    p = fs.LookupParameter(@"Height");
                    Distance elHeight = (p != null) ? new Distance(p.AsDouble(), Distance.Units.RevitInternal) : Distance.NoValue;
                    p = fs.LookupParameter(@"Depth");
                    Distance elDepth = (p != null) ? new Distance(p.AsDouble(), Distance.Units.RevitInternal) : Distance.NoValue;
                    if (   (elWidth.IsNoValue || elWidth == width)
                        && (elHeight.IsNoValue || elHeight == height)
                        && (elDepth.IsNoValue || elDepth == depth) )
                    { retval = fs; break; }
                }
            }

            if (retval == null)
            {
                // No matching family type-- make one
                string typeName = renderFamilyTypeName(templateSymbol, UnitSystem, width, height, depth);
                retval = (FamilySymbol)templateSymbol.Duplicate(typeName);

                // Set the parameters that determine the instance's geometry
                Parameter p = retval.LookupParameter(@"Width");
                if (p != null && !width.IsNoValue) { p.Set(width.ValueRevitInternal); }
                p = retval.LookupParameter(@"Height");
                if (p != null && !height.IsNoValue) { p.Set(height.ValueRevitInternal); }
                p = retval.LookupParameter(@"Depth");
                if (p != null && !depth.IsNoValue) { p.Set(depth.ValueRevitInternal); }

                // Copy all the PKNail parameters from the old type to the new type
                foreach (Parameter templateParam in templateSymbol.Parameters)
                {
                    if (templateParam.Definition.Name.IndexOf(@"PKNail") == -1) { continue; }

                    Parameter newTypeParam = retval.get_Parameter(templateParam.Definition);

                    switch (templateParam.StorageType)
                    {
                        case StorageType.Double:
                            newTypeParam.Set(templateParam.AsDouble());
                            break;
                        case StorageType.Integer:
                            newTypeParam.Set(templateParam.AsInteger());
                            break;
                        case StorageType.String:
                            newTypeParam.Set(templateParam.AsString());
                            break;
                        case StorageType.ElementId:
                            newTypeParam.Set(templateParam.AsElementId());
                            break;
                        default:
                            break;
                    }
                }

                doc.Regenerate();
            }

            return retval;
        }

        private static string renderFamilyTypeName(FamilySymbol templateSymbol, UnitSystem unitSystem, Distance width, Distance height, Distance depth)
        {
            string retval = @"";

            string widthStr = width.RenderShortDistance(unitSystem);
            string heightStr = height.RenderShortDistance(unitSystem);
            string depthStr = depth.RenderShortDistance(unitSystem);
            if (widthStr != @"" && (templateSymbol.LookupParameter(@"Width") != null))
            {
                retval += widthStr;
                if (unitSystem == UnitSystem.Imperial)  { retval += Distance.ShortDistanceUnitsLabel(unitSystem); }
            }
            if (heightStr != @"" && (templateSymbol.LookupParameter(@"Height") != null))
            {
                retval += @" x " + heightStr;
                if (unitSystem == UnitSystem.Imperial)  { retval += Distance.ShortDistanceUnitsLabel(unitSystem); }
            }
            if (depthStr != @"" && (templateSymbol.LookupParameter(@"Depth") != null))
            {
                retval += @" x " + depthStr;
                if (unitSystem == UnitSystem.Imperial)  { retval += Distance.ShortDistanceUnitsLabel(unitSystem); }
            }
            if (unitSystem == UnitSystem.Metric)  { retval += Distance.ShortDistanceUnitsLabel(unitSystem); }

            return retval;
        }

        internal class failuresPreprocessor : IFailuresPreprocessor
        {
            protected RevitDocument m_revitDoc;
            protected List<ElementId> m_deletedWallIds;
            protected bool m_isFailed;
            protected bool m_isFailuresProcessorExecuted;
            protected System.Threading.AutoResetEvent m_txnErrorSignal;

            // We wish to resolve the error condition where a degenerate wall ends up being
            // "reversed".  I don't know what that means, but the transaction works if the
            // offending element (wall) is deleted.
            // There is a failure named "BuiltInFailures.EditingFailures.ElementReversed"
            // which you would think matches this error, but it does not, so we need to match by guid
            protected static Guid sm_elementReversedFailureGuid = new Guid(@"c31ef50f-fc52-49f1-8b3a-aff5fab42f1a");

            // This is the "wall is slightly off axis and may cause inaccuracies"
            // warning when a wall is slightly off square.  Since we have control
            // over all the angles, whenever this happens, it is intended
            // There is a failure named "BuiltinFailures.InaccurateFailures.InaccurateWall"
            // but once again it does not match
            protected static Guid sm_inaccurateWallFailureGuid = new Guid(@"fb1d4583-6b62-4ccb-9cb8-7c2e849b4d43");

            public failuresPreprocessor(RevitDocument revitDoc, System.Threading.AutoResetEvent txnErrorSignal)
            {
                m_revitDoc = revitDoc;
                m_deletedWallIds = new List<ElementId>();
                m_isFailuresProcessorExecuted = false;
                m_txnErrorSignal = txnErrorSignal;
                m_isFailed = false;
            }

            internal bool IsFailuresProcessorExecuted { get { return m_isFailuresProcessorExecuted; } }

            internal List<ElementId> DeletedWalls
            {
                get
                {
                    if (!m_isFailuresProcessorExecuted)
                    { throw new HardException("failuresPreprocessor.DeletedWalls property called before failures processed"); }
                    return m_deletedWallIds;
                }
            }

            internal bool IsFailed
            {
                get
                {
                    if (!m_isFailuresProcessorExecuted)
                    { throw new HardException("failuresPreprocessor.IsFailed property called before failures processed"); }
                    return m_isFailed;
                }
            }

            public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
            {
                FailureProcessingResult retval = FailureProcessingResult.Continue;

                IList<FailureMessageAccessor> fmas = failuresAccessor.GetFailureMessages();
                foreach (FailureMessageAccessor fma in fmas)
                {
                    FailureDefinitionId fId = fma.GetFailureDefinitionId();

                    if (fId.Guid.Equals(sm_elementReversedFailureGuid))
                    {
                        foreach (ElementId failedElementId in fma.GetFailingElementIds())
                        { m_deletedWallIds.Add(failedElementId); }
                        fma.SetCurrentResolutionType(FailureResolutionType.DeleteElements);
                        failuresAccessor.ResolveFailure(fma);
                        retval = FailureProcessingResult.ProceedWithCommit;
                    }
                    else
                    {
                        if (!m_revitDoc.m_isTrialTxn)
                        {
                            if (fId.Guid.Equals(sm_inaccurateWallFailureGuid))
                            {
                                failuresAccessor.DeleteWarning(fma);
                                retval = FailureProcessingResult.ProceedWithCommit;
                            }
                            else
                            {
                                m_isFailed = true;
                                if (m_txnErrorSignal != null) { m_txnErrorSignal.Set(); }
                            }
                        }
                        else  // isTrialCreateWall
                        {
                            // Delete all warnings during the "trial" transactions (these are going
                            // to be rolled back anyway) so the user is not subjected to them multiple times
                            if (fma.GetSeverity() == FailureSeverity.Warning)
                            { failuresAccessor.DeleteWarning(fma); }
                            else
                            {
                                m_isFailed = true;
                                if (m_txnErrorSignal != null) { m_txnErrorSignal.Set(); }
                            }
                        }
                    }
                }

                m_isFailuresProcessorExecuted = true;

                return retval;
            }
        }

        private class revitCommandContext : CommandContext
        {
            private RevitDocument m_revitDoc;
            private Level m_curLevel;
            private Element m_refElement;
            private G2D.Point m_refElementFacePoint;

            internal revitCommandContext(RevitDocument revitDoc)
                : base()
            {
                m_revitDoc = revitDoc;
                m_refElement = null;
                m_refElementFacePoint = null;

                // Set the levels
                List<Level> levels = revitDoc.getLevels();
                for (int i = 0; i < levels.Count; i++)
                { ProjectLevelNames.Add(levels[i].Name); }

                View v = revitDoc.doc.ActiveView;
                if (v != null)
                { m_curLevel = v.GenLevel; }
                if (m_curLevel != null)
                { CurrentLevelIdx = ProjectLevelNames.IndexOf(m_curLevel.Name); }
                else
                { throw new HardException("Please select a floor plan view to continue"); }

                // Set the display units
                DisplayUnitSystem = m_revitDoc.UnitSystem;

                // Copy the Model Settings
                ModelSettings = revitDoc.docSettings;
            }

            internal Element RefElement { get { return m_refElement; } }

            internal bool IsRefElementWall { get { return (m_refElement != null && m_refElement is Wall); } }

            internal G2D.Point ReferencePoint { get { return m_refElementFacePoint; } }

            internal Level CurrentLevel { get { return m_curLevel; } }

            internal Level NextLevel
            {
                get
                {
                    if (CurrentLevelIdx < ProjectLevelNames.Count - 1)
                    {
                        List<Level> levels = m_revitDoc.getLevels();
                        return levels[CurrentLevelIdx + 1];
                    }
                    else { return null; }
                }
            }

            internal bool UserPickWallToActivate(out surveyFace refWallFace, ref string msg)
            {
                refWallFace = null;

                Reference pickPtRef = null;
                // We use the fully qualified name for UIDocument to indicate that this logic
                // is an exception to the rule that the RevitDocument class does not include any UI logic
                Autodesk.Revit.UI.UIDocument uidoc = new Autodesk.Revit.UI.UIDocument(m_revitDoc.doc);
                try
                {
                    pickPtRef = uidoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.PointOnElement,
                                                           new wallFaceFilter(m_revitDoc, true),
                                                           @"Select the wall face and direction to activate");
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                { return false; }

                if (pickPtRef == null)  { return false; }
                
                m_refElement = m_revitDoc.doc.GetElement(pickPtRef);
                if (m_refElement == null) { return false; }
                m_refElementFacePoint = Utilities.To2D(pickPtRef.GlobalPoint);

                Wall pickWall = m_refElement as Wall;

                // Get the survey orientation coresponding to this face
                SurveyOrientation = Utilities.SurveyOrientationFromFace(pickWall, pickWall.GetGeometryObjectFromReference(pickPtRef) as PlanarFace);

                Wall refWall = m_refElement as Wall;
                surveyFace.CreateFaceNoModifyWalls(m_revitDoc, m_curLevel, refWall, SurveyOrientation, m_refElementFacePoint, out refWallFace);
                m_refElement = refWall;
                G2D.Vector locV = new G2D.Vector(refWallFace.Corner(SurveySide.Left), refWallFace.Corner(SurveySide.Right));

                // Get the desired survey direction by finding the corner closest to the pick point
                SurveySide side = surveySideFromLocV(locV, pickPtRef.GlobalPoint);
                SurveyDirection = (side == SurveySide.Left) ? SurveyDirection.LeftToRight : SurveyDirection.RightToLeft;

                return true;
            }

            internal bool UserPickRefOpening(List<long> openingIDs, out FamilyInstance opening, ref string msg)
            {
                bool retval = true; opening = null;

                Reference pickPtRef = null;
                // We use the fully qualified name for UIDocument to indicate that this logic
                // is an exception to the rule that the RevitDocument class does not include any UI logic
                Autodesk.Revit.UI.UIDocument uidoc = new Autodesk.Revit.UI.UIDocument(m_revitDoc.doc);
                try
                {
                    while (opening == null)
                    {
                        pickPtRef = uidoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element,
                                                               new refOpeningFilter(openingIDs),
                                                               @"Select an opening to be used as a reference for surveying the wall face");
                        if (pickPtRef != null)
                        {
                            Element el = m_revitDoc.doc.GetElement(pickPtRef);
                            if (el != null)  { opening = el as FamilyInstance; break; }
                        }
                    }
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                { retval = false; }

                return retval;
            }

            internal void SaveRefWallFaceToModel(surveyFace f, SurveyDirection dir)
            {
                m_refElement = f.EndWall(dir.NearSide);
                m_refElementFacePoint = f.Corner(dir.NearSide);
                SurveyOrientation = f.EndWallSO(dir.NearSide);
                SurveyDirection = dir;

                saveToDocumentState();
            }

            internal void SaveDetailLineToModel(DetailCurve dLine, SurveyDirection dir, SurveyOrientation so)
            {
                m_refElement = dLine;
                m_refElementFacePoint = null;
                SurveyDirection = dir;
                SurveyOrientation = so;

                saveToDocumentState();
            }

            internal void ClearRefElementAndSave()
            { documentState.ClearRefElementState(m_revitDoc); }

            internal bool GetSurveyDirectionFromModel(out SurveyDirection dir)
            {
                dir = SurveyDirection.LeftToRight;
                refElementState res = documentState.GetRefElementState(m_revitDoc);
                if (res != null)
                {
                    dir = res.RefElementDir;
                    return true;
                }
                else
                { return false; }
            }

            internal bool GetActiveElementFromModel(SurveyDirection dir, out surveyFace refWallFace, ref string msg)
            {
                bool retval = true;
                refWallFace = null;

                refElementState rws = documentState.GetRefElementState(m_revitDoc);
                if (rws != null && rws.RefElementID != -1)
                {
                    m_refElement = m_revitDoc.doc.GetElement(new ElementId(rws.RefElementID));
                    m_refElementFacePoint = rws.RefElementFacePoint;
                    SurveyDirection = rws.RefElementDir;
                    SurveyOrientation = rws.RefElementSO;

                    if (IsRefElementWall)
                    {
                        Wall refWall = m_refElement as Wall;
                        surveyFace.CreateFaceNoModifyWalls(m_revitDoc, CurrentLevel, refWall, SurveyOrientation, m_refElementFacePoint, out refWallFace);
                        if (dir != SurveyDirection.FromModel && dir != SurveyDirection)
                        {
                            // Set the data to the other side
                            SurveySide otherSide = dir.NearSide;
                            m_refElement = refWallFace.EndWall(otherSide);
                            SurveyOrientation = refWallFace.EndWallSO(otherSide);
                            SurveyDirection = dir;
                        }
                    }
                    else if (m_refElement is DetailCurve)
                    {
                        if (dir != SurveyDirection.FromModel)  { SurveyDirection = dir; }
                    }
                    else
                    { retval = false; }
                }
                else
                { retval = false; }

                if (retval == false)
                { msg += @"Please activate a wall before attempting to survey new objects"; }

                return retval;
            }

            internal surveyFace GetWallFace()
            {
                surveyFace retval = null;

                if (IsRefElementWall)
                {
                    Wall refWall = m_refElement as Wall;
                    surveyFace.CreateFaceNoModifyWalls(m_revitDoc, CurrentLevel, refWall, SurveyOrientation, m_refElementFacePoint, out retval);
                }

                return retval;
            }

            private static SurveySide surveySideFromLocV(G2D.Vector locV, XYZ pickPt)
            {
                G2D.Point pickPt2D = Utilities.To2D(pickPt);
                return (locV.Start.DistanceTo(pickPt2D) < locV.End.DistanceTo(pickPt2D)) ? SurveySide.Left : SurveySide.Right;
            }

            private void saveToDocumentState()
            {
                refElementState rws = new refElementState(m_refElement.Id);
                rws.RefElementSO = SurveyOrientation;
                rws.RefElementFacePoint = m_refElementFacePoint;
                rws.RefElementDir = SurveyDirection;
                documentState.SetRefElementState(m_revitDoc, rws);
            }
        }

        private class wallFaceFilter : Autodesk.Revit.UI.Selection.ISelectionFilter
        {
            protected RevitDocument m_revitDoc = null;
            protected bool m_isValidateActivation = false;

            internal wallFaceFilter(RevitDocument revitDoc, bool isValidateActivation)
            {
                m_revitDoc = revitDoc;
                m_isValidateActivation = isValidateActivation;
            }

            public bool AllowElement(Element elem)
            { return (elem is Wall); }

            public bool AllowReference(Reference reference, XYZ position)
            {
                Element el = m_revitDoc.doc.GetElement(reference);
                Wall w = el as Wall;
                if (w == null) { return false; }
                PlanarFace pf = el.GetGeometryObjectFromReference(reference) as PlanarFace;
                if (pf == null) { return false; }

                // Check to make sure this is a "side" face, not an "end" face
                XYZ faceNormal = pf.FaceNormal;
                if (!faceNormal.IsAlmostEqualTo(w.Orientation) && !faceNormal.Negate().IsAlmostEqualTo(w.Orientation))
                { return false; }

                if (!m_isValidateActivation) { return true; }

                // Don't activate the "dark" (un-surveyed) face of an unknown-width wall
                SurveyOrientation so = Utilities.SurveyOrientationFromFace(w, pf);
                if (m_revitDoc.isDarkWallFace(w, so)) { return false; }

                return true;
            }
        }

        private class refOpeningFilter : Autodesk.Revit.UI.Selection.ISelectionFilter
        {
            private List<long> m_validOpeningIDs;

            public refOpeningFilter(List<long> validOpeningIDs) { m_validOpeningIDs = validOpeningIDs; }

            public bool AllowElement(Element elem)
            {
                FamilyInstance inst = elem as FamilyInstance;
                return (inst != null && m_validOpeningIDs.Contains(inst.Id.Value));
            }

            public bool AllowReference(Reference reference, XYZ position)
            { return true; }
        }

        [System.Reflection.Obfuscation(Feature="renaming", ApplyToMembers=true)]
        private class refElementState : JsonSerializable
        {
            public long RefElementID;
            public G2D.Point RefElementFacePoint;
            public SurveyOrientation RefElementSO;
            public SurveyDirection RefElementDir;

            public refElementState()
            { RefElementID = -1; }

            public refElementState(ElementId refElementId)
            { RefElementID = refElementId.Value; }
        }

        public class RangefinderKeypadMaps
        {
            internal const string StandardMapName = @"< Standard >";

            private Dictionary<string, Dictionary<string, string>> m_maps;

            public RangefinderKeypadMaps()
            { m_maps = new Dictionary<string, Dictionary<string, string>>(); }

            // This property is here only because it is required for Json serialization, it should not be used elsewhere
            public Dictionary<string, Dictionary<string, string>> Maps { get { return m_maps; } set { m_maps = value; } }

            internal void AddPage(string page)
            {
                init();
                m_maps.Add(page, new Dictionary<string, string>());
            }

            internal  void DeletePage(string page)
            {
                init();
                if (page != StandardMapName)  { m_maps.Remove(page); }
            }

            internal List<string> GetAllPages()
            {
                init();
                return new List<string>(m_maps.Keys);
            }

            internal Dictionary<KeypadCommand, string> GetMapAtPage(string page)
            {
                Dictionary<KeypadCommand, string> retval = new Dictionary<KeypadCommand, string>();
                init();
                foreach (string rfCmdJson in m_maps[page].Keys)
                { retval.Add(JsonSerializable.FromJson<KeypadCommand>(rfCmdJson), m_maps[page][rfCmdJson]); }
                return retval;
            }

            internal void AddCommandInPage(string page, KeypadCommand rfCmd, string familyCategory)
            {
                init();
                if (page != StandardMapName)
                {
                    string rfCmdJson = rfCmd.ToJson();
                    if (m_maps[page].ContainsKey(rfCmdJson))  { m_maps[page].Remove(rfCmdJson); }
                    m_maps[page].Add(rfCmd.ToJson(), familyCategory);
                }
            }

            internal void DeleteCommandinPage(string page, KeypadCommand rfCmd)
            {
                init();
                string rfCmdJson = rfCmd.ToJson();
                if (page != StandardMapName)
                {
                    Dictionary<string, string> map = m_maps[page];
                    if (map.ContainsKey(rfCmdJson)) { map.Remove(rfCmdJson); }
                }
            }

            internal string LookupFamilyCategoryInPage(string page, KeypadCommand rfCmd)
            {
                string retval;
                init();
                string rfCmdJson = rfCmd.ToJson();
                if (!m_maps[page].TryGetValue(rfCmdJson, out retval))  { retval = @""; }
                return retval;
            }

            private void init()
            {
                if (m_maps.Count == 0)
                {
                    m_maps.Add(StandardMapName, new Dictionary<string, string>());
                    m_maps[StandardMapName].Add((new KeypadCommand(KeypadCommand.KeyDef.UpLeft, 1)).ToJson(), FamilyCategory.Windows);
                    m_maps[StandardMapName].Add((new KeypadCommand(KeypadCommand.KeyDef.Up, 1)).ToJson(), FamilyCategory.Lines);
                    m_maps[StandardMapName].Add((new KeypadCommand(KeypadCommand.KeyDef.UpRight, 1)).ToJson(), FamilyCategory.Doors);                    
                }
            }
        }

        private static class documentState
        {
            internal static void SetRefElementState(RevitDocument revitDoc, refElementState rws)
            {
                documentStateContainer cont = fetch(revitDoc);
                cont.RefElementState = rws;
                store(revitDoc, cont);
            }

            internal static void ClearRefElementState(RevitDocument revitDoc)
            { SetRefElementState(revitDoc, null); }

            internal static refElementState GetRefElementState(RevitDocument revitDoc)
            { return (fetch(revitDoc).RefElementState); }

            internal static void SetLastAddedWallWidth(RevitDocument revitDoc, Distance d)
            {
                documentStateContainer cont = fetch(revitDoc);
                cont.LastAddedWallWidth = d;
                store(revitDoc, cont);
            }

            internal static Distance GetLastAddedWallWidth(RevitDocument revitDoc)
            { return (fetch(revitDoc).LastAddedWallWidth); }

            internal static void SetCoordinateSystemAngle(RevitDocument revitDoc, G2D.Radians angle)
            {
                documentStateContainer cont = fetch(revitDoc);
                cont.CoordinateSystemAngle = angle;
                store(revitDoc, cont);
            }

            internal static G2D.Radians GetCoordinateSystemAngle(RevitDocument revitDoc)
            { return (fetch(revitDoc).CoordinateSystemAngle); }

            internal static WallHostedObjectSurveyParams GetSurveyParamsCacheByCategory(RevitDocument revitDoc, string familyCategory)
            {
                WallHostedObjectSurveyParams retval = null;
                documentStateContainer cont = fetch(revitDoc);
                if (cont.WallHostedObjectSurveyParamsCache.ContainsKey(familyCategory))
                {
                    retval = cont.WallHostedObjectSurveyParamsCache[familyCategory];
                    if (retval.FamilyCategory == null || retval.FamilyCategory == @"")
                    { retval.FamilyCategory = familyCategory; }
                }
                else
                { retval = new WallHostedObjectSurveyParams(familyCategory); }
                return retval;
            }

            internal static void SetSurveyParamsCache(RevitDocument revitDoc, string familyCategory, WallHostedObjectSurveyParams sp)
            {
                documentStateContainer cont = fetch(revitDoc);
                if (cont.WallHostedObjectSurveyParamsCache.ContainsKey(familyCategory))
                { cont.WallHostedObjectSurveyParamsCache[familyCategory] = sp; }
                else
                { cont.WallHostedObjectSurveyParamsCache.Add(familyCategory, sp); }
                store(revitDoc, cont);
            }

            internal static string GetLastLineStyle(RevitDocument revitDoc)
            { return fetch(revitDoc).LastLineStyle; }

            internal static void SetLastLineStyle(RevitDocument revitDoc, string lineStyle)
            {
                documentStateContainer cont = fetch(revitDoc);
                cont.LastLineStyle = lineStyle;
                store(revitDoc, cont);
            }

            private static documentStateContainer fetch(RevitDocument revitDoc)
            { return revitDoc.fetchData<documentStateContainer>(); }

            private static void store(RevitDocument revitDoc, documentStateContainer cont)
            { revitDoc.storeData<documentStateContainer>(cont); }
        }

        [System.Reflection.Obfuscation(Feature="renaming", ApplyToMembers=true)]
        private class documentStateContainer : JsonSerializable
        {
            public documentStateContainer()
            {
                AutoSaveActionCount = 0;
                CoordinateSystemAngle = G2D.Radians.Undefined;
                LastAddedWallWidth = Distance.NoValue;
                WallHostedObjectSurveyParamsCache = new Dictionary<string, WallHostedObjectSurveyParams>();
                LastLineStyle = @"";
            }

            public int AutoSaveActionCount;
            public refElementState RefElementState;
            public Distance LastAddedWallWidth;
            public G2D.Radians CoordinateSystemAngle;
            public Dictionary<string, WallHostedObjectSurveyParams> WallHostedObjectSurveyParamsCache;
            public string LastLineStyle;
        }

        [System.Reflection.Obfuscation(Feature="renaming", ApplyToMembers=true)]
        public class PKNailVersionInfo : JsonSerializable
        {
            public string CurrentRun = @"";
            public string PreviousRun = @"";
        }

        private WallState getWallState(Wall w)
        { return fetchObjOfType<WallState>(w, true); }

        private void setWallState(Wall w, WallState ws)
        {
            ws.ApplyToWall(doc, UnitSystem, w);
            storeObject(w, ws);
        }

        private bool isManuallyCreated(Wall w)
        {
            WallState ws = fetchObjOfType<WallState>(w, false);
            return (ws == null);
        }

        private void deleteWall(Wall w)
        {
            getWallState(w).RemoveFromWall(doc, w);
            doc.Delete(w.Id);
        }

        private void deleteWall(ElementId wId)
        {
            Wall w = doc.GetElement(wId) as Wall;
            if (w != null)  { deleteWall(w); }   // if wall is null, assume already deleted
        }
    }
}

