using System;
using System.Collections.Generic;

using Autodesk.Revit.DB;

using G2D = PKNailForRevit.Geometry2D;
using AiActivation = PKNail.AiImport.Mapping.WallActivationGeometry;

namespace PKNailForRevit
{
    // Epic 11c — the PKNAIL 2.0 AI-import surface, isolated from the ~7,100-line RevitDocument God class:
    // v2 absolute wall/opening placement, headless engine activation, the re-import guard, Revit-warning
    // capture, and the RAII TransactionScope. Everything here is a partial member of RevitDocument, so it
    // shares its private state (doc, m_txn, startTransaction/commitTransaction, matchingWallType, ...).
    public sealed partial class RevitDocument
    {
        // Non-null only while an AI import is capturing Revit warnings. The shared failuresPreprocessor
        // (in RevitDocument.cs) collects + dismisses warnings into this list instead of showing a dialog.
        private List<string> m_importWarnings;

        // RAII wrapper over the engine's inner-transaction helpers (Epic 11c). Constructing a scope starts
        // an inner transaction inside the current group; Commit() commits it; Dispose() rolls it back if it
        // was NOT committed — so an exception or an early return can never leak a dangling inner transaction
        // (which would break the next operation in the group). Migrated the AI-import methods first; legacy
        // engine methods keep their existing start/commit calls until they are test-covered.
        private sealed class TransactionScope : IDisposable
        {
            private readonly RevitDocument m_doc;
            private bool m_committed;
            private bool m_disposed;

            internal TransactionScope(RevitDocument doc, string name)
            {
                m_doc = doc;
                m_doc.startTransaction(name);
            }

            // Commit the inner transaction. Returns false if Revit did not report Committed.
            internal bool Commit()
            {
                m_committed = true;
                return m_doc.commitTransaction();
            }

            public void Dispose()
            {
                if (m_disposed) { return; }
                m_disposed = true;
                if (!m_committed) { m_doc.AbortPendingTransaction(); }
            }
        }

        // IProjectModel interface implementation
        //
        // Direct absolute wall placement for the v2 AI contract. The v2 walls are independent segments at
        // real plan coordinates (NOT a connected survey chain), so we place each one directly at its own
        // start/end rather than chaining it off the previous wall via SurveyWall. Runs inside the caller's
        // transaction group; opens/commits only its own inner transaction (like the SurveyWall path).
        public CommandResult CreateWallAbsolute(double startXMeters, double startYMeters,
                                                double endXMeters, double endYMeters,
                                                Distance thickness, Distance height, int levelIdx,
                                                out long wallElementId, ref string msg)
        {
            wallElementId = -1;

            // meters -> Revit internal feet at the import boundary.
            double sx = new Distance(startXMeters, Distance.Units.MetricMeter).ValueRevitInternal;
            double sy = new Distance(startYMeters, Distance.Units.MetricMeter).ValueRevitInternal;
            double ex = new Distance(endXMeters, Distance.Units.MetricMeter).ValueRevitInternal;
            double ey = new Distance(endYMeters, Distance.Units.MetricMeter).ValueRevitInternal;

            XYZ startPt = new XYZ(sx, sy, 0.0);
            XYZ endPt = new XYZ(ex, ey, 0.0);

            // Degenerate (zero-length) wall: skip rather than let Revit throw.
            if (startPt.DistanceTo(endPt) < m_revitApp.ShortCurveTolerance)
            {
                msg += @"CreateWallAbsolute: start and end points are coincident (zero-length wall)";
                return CommandResult.Failed;
            }

            // Resolve the base level for the requested index; default to the lowest level if invalid.
            List<Level> levels = getLevels();
            if (levels.Count == 0)
            {
                msg += @"CreateWallAbsolute: the document has no levels";
                return CommandResult.Failed;
            }
            Level level = (levelIdx >= 0 && levelIdx < levels.Count) ? levels[levelIdx] : levels[0];

            try
            {
                using (var txn = new TransactionScope(this, @"Create AI Wall"))
                {
                    WallType wt = matchingWallType(thickness);
                    if (wt == null)
                    {
                        msg += @"CreateWallAbsolute: no matching wall type could be resolved";
                        return CommandResult.Failed;   // scope Dispose rolls back the (empty) inner txn
                    }

                    Line locLine = Line.CreateBound(startPt, endPt);
                    Wall wall = Wall.Create(m_doc, locLine, wt.Id, level.Id, height.ValueRevitInternal, 0.0, false, false);
                    if (wall == null)
                    {
                        msg += @"CreateWallAbsolute: Wall.Create returned null";
                        return CommandResult.Failed;
                    }

                    wallElementId = wall.Id.Value;

                    // Tag the wall as a PKNAIL survey wall (stores a WallState), exactly as SurveyWall does, so
                    // (a) the engine's opening-hosting flow treats it as a valid host and (b) a subsequent import
                    // can detect existing PKNAIL content (re-import guard). An empty WallState is a pure marker:
                    // ApplyToWall is a no-op when no level offsets are set.
                    setWallState(wall, new WallState());

                    return txn.Commit() ? CommandResult.Succeeded : CommandResult.TransactionError;
                }
            }
            catch (Exception e)
            {
                // TransactionScope.Dispose already rolled the inner transaction back.
                msg += @"CreateWallAbsolute: " + e.Message;
                return CommandResult.TransactionError;
            }
        }

        // IProjectModel interface implementation
        //
        // Direct opening (window/door) hosting on an absolute-placed wall for the v2 AI contract. The
        // opening is placed at locationFromStartMeters measured from the host wall's start point, along its
        // location line. Primary path: a hosted family instance from the correct category; fallback (no such
        // family available): a rectangular wall opening at the exact size so the void still shows. Runs
        // inside the caller's transaction group; opens/commits only its own inner transaction.
        public CommandResult CreateOpeningOnWall(long wallElementId, double locationFromStartMeters,
                                                 Distance width, Distance height, Distance sillHeight,
                                                 bool isDoor, out long openingElementId, ref string msg)
        {
            openingElementId = -1;

            try
            {
                Wall wall = doc.GetElement(new ElementId(wallElementId)) as Wall;
                if (wall == null)
                {
                    msg += @"CreateOpeningOnWall: no wall found for element id " + wallElementId;
                    return CommandResult.Failed;
                }

                LocationCurve locCurve = wall.Location as LocationCurve;
                if (locCurve == null || locCurve.Curve == null)
                {
                    msg += @"CreateOpeningOnWall: host wall has no location curve";
                    return CommandResult.Failed;
                }

                Curve curve = locCurve.Curve;
                XYZ start = curve.GetEndPoint(0);
                XYZ end = curve.GetEndPoint(1);
                XYZ dir = end - start;
                if (dir.GetLength() < m_revitApp.ShortCurveTolerance)
                {
                    msg += @"CreateOpeningOnWall: host wall is degenerate (zero length)";
                    return CommandResult.Failed;
                }
                XYZ dirN = dir.Normalize();

                double locFt = new Distance(locationFromStartMeters, Distance.Units.MetricMeter).ValueRevitInternal;
                double widthFt = width.IsNoValue ? 0.0 : width.ValueRevitInternal;
                double heightFt = height.IsNoValue ? 0.0 : height.ValueRevitInternal;
                double sillFt = sillHeight.IsNoValue ? 0.0 : sillHeight.ValueRevitInternal;

                XYZ hostPoint = start + dirN * locFt;

                // Base level of the host wall (fall back to the lowest document level if unresolved).
                Level level = doc.GetElement(wall.LevelId) as Level;
                if (level == null)
                {
                    List<Level> levels = getLevels();
                    if (levels.Count == 0)
                    {
                        msg += @"CreateOpeningOnWall: no level available to host on";
                        return CommandResult.Failed;
                    }
                    level = levels[0];
                }

                BuiltInCategory cat = isDoor ? BuiltInCategory.OST_Doors : BuiltInCategory.OST_Windows;

                try
                {
                    using (var txn = new TransactionScope(this, @"Create AI Opening"))
                    {
                        // Primary: the first loadable family symbol in the correct category.
                        FamilySymbol symbol = null;
                        FilteredElementCollector symColl = new FilteredElementCollector(doc)
                            .OfClass(typeof(FamilySymbol)).OfCategory(cat);
                        foreach (Element e in symColl)
                        {
                            symbol = e as FamilySymbol;
                            if (symbol != null) { break; }
                        }

                        if (symbol != null)
                        {
                            if (!symbol.IsActive)
                            {
                                symbol.Activate();
                                doc.Regenerate();
                            }

                            FamilyInstance inst = doc.Create.NewFamilyInstance(
                                hostPoint, symbol, wall, level,
                                Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                            if (inst == null)
                            {
                                msg += @"CreateOpeningOnWall: NewFamilyInstance returned null";
                                return CommandResult.Failed;
                            }

                            // Sill height (windows; doors are typically sill 0).
                            Parameter sillP = inst.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM);
                            if (sillP != null && !sillP.IsReadOnly) { sillP.Set(sillFt); }

                            // Best-effort width/height — only when the family exposes them as INSTANCE params.
                            // Type-only dimensions are left at the family default rather than failing.
                            if (widthFt > 0.0)
                            {
                                Parameter wP = inst.LookupParameter(@"Width");
                                if (wP != null && !wP.IsReadOnly) { wP.Set(widthFt); }
                            }
                            if (heightFt > 0.0)
                            {
                                Parameter hP = inst.LookupParameter(@"Height");
                                if (hP != null && !hP.IsReadOnly) { hP.Set(heightFt); }
                            }

                            openingElementId = inst.Id.Value;
                            return txn.Commit() ? CommandResult.Succeeded : CommandResult.TransactionError;
                        }

                        // Fallback: a rectangular wall opening (a hole) at the exact size and elevation, so the
                        // opening still shows even when no window/door family is loaded in the document.
                        double baseElev = level.Elevation;
                        XYZ c1 = new XYZ(hostPoint.X - dirN.X * (widthFt / 2.0),
                                         hostPoint.Y - dirN.Y * (widthFt / 2.0),
                                         baseElev + sillFt);
                        XYZ c2 = new XYZ(hostPoint.X + dirN.X * (widthFt / 2.0),
                                         hostPoint.Y + dirN.Y * (widthFt / 2.0),
                                         baseElev + sillFt + heightFt);
                        Opening op = doc.Create.NewOpening(wall, c1, c2);
                        if (op == null)
                        {
                            msg += @"CreateOpeningOnWall: NewOpening returned null";
                            return CommandResult.Failed;
                        }
                        openingElementId = op.Id.Value;
                        return txn.Commit() ? CommandResult.Succeeded : CommandResult.TransactionError;
                    }
                }
                catch (Exception e)
                {
                    // TransactionScope.Dispose already rolled the inner transaction back.
                    msg += @"CreateOpeningOnWall: " + e.Message;
                    return CommandResult.TransactionError;
                }
            }
            catch (Exception e)
            {
                msg += @"CreateOpeningOnWall: " + e.Message;
                return CommandResult.Failed;
            }
        }

        // IProjectModel interface implementation (Epic 7)
        //
        // Begin collecting Revit warnings raised during placement instead of letting them pop modal
        // dialogs. The shared failuresPreprocessor checks m_importWarnings on every transaction it
        // processes; while it is non-null, any WARNING (not an error) is dismissed and its text recorded.
        public void BeginImportWarningCapture()
        {
            m_importWarnings = new List<string>();
        }

        // Stop capturing and return the warnings collected since BeginImportWarningCapture. Idempotent:
        // returns an empty list if capture was never started.
        public IReadOnlyList<string> EndImportWarningCapture()
        {
            List<string> collected = m_importWarnings ?? new List<string>();
            m_importWarnings = null;
            return collected;
        }

        // IProjectModel interface implementation (1.0-adaptation for the v2 absolute path)
        //
        // Headlessly make an absolute-placed wall the engine's "active/reference wall" so the PROVEN 1.0
        // opening-hosting flow (SurveyWallHostedObjectPrep -> SurveyWallHostedObject) can host doors/windows
        // on it — applying family-def resolution and hinge/swing/facing exactly like the interactive survey
        // tool. This replicates revitCommandContext.saveToDocumentState() but sources the ref element from
        // the given wall + the scan's interior/exterior and near-end convention instead of a PickObject.
        //
        // interiorFace: true when the scan surveyed the interior face (IsScanFromInterior) -> NotFlipped.
        // nearEndIsStart: true when opening locations are measured from the wall's start point.
        // Precondition: doc.ActiveView is the floor plan whose GenLevel is the wall's base level (the caller
        // ensures this); the subsequent SurveyWallHostedObjectPrep builds its context from the active view.
        public CommandResult ActivateWallById(long wallId, bool interiorFace, bool nearEndIsStart, ref string msg)
        {
            try
            {
                Wall wall = doc.GetElement(new ElementId(wallId)) as Wall;
                if (wall == null)
                {
                    msg += @"ActivateWallById: element " + wallId + @" is not a wall";
                    return CommandResult.Failed;
                }

                LocationCurve lc = wall.Location as LocationCurve;
                if (lc == null || lc.Curve == null)
                {
                    msg += @"ActivateWallById: wall has no location curve";
                    return CommandResult.Failed;
                }

                Level level = RevitSurveyWall.BaseLevel(wall);

                // Decision 1 — which face the scan surveyed (unit-tested in WallActivationGeometryTests):
                // interior -> NotFlipped, exterior -> Flipped. Translate the Revit-free kind to the engine type.
                SurveyOrientation so =
                    (AiActivation.FaceOrientation(interiorFace) == PKNail.AiImport.Mapping.SurveyOrientationKind.Flipped)
                        ? SurveyOrientation.Flipped : SurveyOrientation.NotFlipped;

                // Read the wall's front-edge geometry from the solid (Revit-only): a plan point on the chosen
                // face (front-edge midpoint; the face-point match is loose) and the two face corners.
                G2D.Vector frontEdge = RevitSurveyWall.WallEdge(wall, so, true);
                G2D.Point facePoint = frontEdge.MidPoint;

                surveyFace f;
                surveyFace.CreateFaceNoModifyWalls(this, level, wall, so, facePoint, out f);

                XYZ nearXYZ = nearEndIsStart ? lc.Curve.GetEndPoint(0) : lc.Curve.GetEndPoint(1);
                G2D.Point near = Utilities.To2D(nearXYZ);
                G2D.Point cLeft = f.Corner(SurveySide.Left);
                G2D.Point cRight = f.Corner(SurveySide.Right);

                // Decision 2 — choose the survey direction so the near corner is the desired wall end (the JSON
                // measures opening locations from the wall's start). Also unit-tested, convention-independent.
                SurveyDirection dir =
                    (AiActivation.NearEndDirection(
                        new AiActivation.PlanPoint(cLeft.X, cLeft.Y),
                        new AiActivation.PlanPoint(cRight.X, cRight.Y),
                        new AiActivation.PlanPoint(near.X, near.Y)) == AiActivation.Direction.LeftToRight)
                        ? SurveyDirection.LeftToRight : SurveyDirection.RightToLeft;

                // Persist the active/reference element exactly like saveToDocumentState(). This writes a
                // shared parameter, so it needs its own inner transaction inside the caller's group.
                refElementState rws = new refElementState(wall.Id);
                rws.RefElementSO = so;
                rws.RefElementFacePoint = facePoint;
                rws.RefElementDir = dir;

                using (var txn = new TransactionScope(this, @"Activate AI Wall"))
                {
                    documentState.SetRefElementState(this, rws);
                    return txn.Commit() ? CommandResult.Succeeded : CommandResult.TransactionError;
                }
            }
            catch (Exception e)
            {
                // TransactionScope.Dispose rolled back any open inner transaction (never leave one dangling —
                // it would break the next operation in the group).
                msg += @"ActivateWallById: " + e.Message;
                return CommandResult.Failed;
            }
        }

        // IProjectModel interface implementation — roll back a dangling inner transaction (if any) WITHOUT
        // touching the surrounding transaction group. Used by TransactionScope.Dispose, and by the batch
        // importer after the legacy SurveyWallHostedObject throws (it opens its own transaction and can throw
        // before committing) so a single un-hostable opening does not roll back the whole import.
        public void AbortPendingTransaction()
        {
            if (m_txn != null)
            {
                try { if (m_txn.GetStatus() == TransactionStatus.Started) { m_txn.RollBack(); } }
                catch { /* best effort — the group rollback is the backstop */ }
                m_txn = null;
            }
        }

        // IProjectModel interface implementation — true if the document already contains PKNAIL-created walls
        // (walls carrying a stored WallState). Used by the import command's re-import guard (Q5: warn +
        // require an empty PKNAIL context rather than silently appending a second building).
        public bool HasPKNailWalls()
        {
            foreach (Element el in new FilteredElementCollector(doc).OfClass(typeof(Wall)).ToElements())
            {
                Wall w = el as Wall;
                if (w != null && !isManuallyCreated(w)) { return true; }
            }
            return false;
        }

        // IProjectModel interface implementation — the engine extracts wall-face solid geometry through a
        // default 3D view (see the view3D getter). A brand-new project (e.g. a stock metric template) may
        // have no usable 3D view, which would make opening hosting fail with "No default 3D view in
        // document". Create an isometric one up front if none exists. Runs in its own standalone transaction
        // (this is called BEFORE the import transaction group, like EnsureDocumentParameters).
        public CommandResult EnsureDefault3DView(ref string msg)
        {
            try
            {
                foreach (Element vEl in new FilteredElementCollector(doc).OfClass(typeof(View3D)).ToElements())
                {
                    View3D existing = vEl as View3D;
                    if (existing != null && !existing.IsTemplate && !existing.IsPerspective)
                    { return CommandResult.Succeeded; }
                }

                ViewFamilyType vft = null;
                foreach (Element tEl in new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).ToElements())
                {
                    ViewFamilyType t = tEl as ViewFamilyType;
                    if (t != null && t.ViewFamily == ViewFamily.ThreeDimensional) { vft = t; break; }
                }
                if (vft == null)
                {
                    msg += @"EnsureDefault3DView: no 3D view family type available in the document";
                    return CommandResult.Failed;
                }

                using (Transaction t = new Transaction(doc, @"Create default 3D view"))
                {
                    t.Start();
                    View3D.CreateIsometric(doc, vft.Id);
                    t.Commit();
                }
                return CommandResult.Succeeded;
            }
            catch (Exception e)
            {
                msg += @"EnsureDefault3DView: " + e.Message;
                return CommandResult.Failed;
            }
        }
    }
}
