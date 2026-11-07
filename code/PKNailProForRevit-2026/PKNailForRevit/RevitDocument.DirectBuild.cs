using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.DB;

using PKNail.AiImport.DirectBuild;

namespace PKNailForRevit
{
    // Epic 13 — Direct Build: draw the AI geometry as native Revit walls, doors and windows with plain
    // Revit API calls. Unlike the v2 engine path (CreateWallAbsolute + ActivateWallById +
    // SurveyWallHostedObject) nothing here depends on the 1.0 survey engine or an active reference wall.
    // Every geometric decision was already made by the Revit-free DirectBuildPlanner; these methods only
    // translate it into Revit elements. Doors and windows are built from the PKNAIL survey family the AI
    // asked for (OpeningType / FamilyDefIdx) when the project has survey families, else from the first
    // wall-hosted family of the category.
    //
    // Revit errors never reach the user as a dialog: each element is created in its own inner transaction
    // with a directBuildFailures preprocessor that records the error text and rolls just that element back
    // silently. Success is only reported when the element still exists after the commit.
    public sealed partial class RevitDocument
    {
        private const string DirectBuildTypePrefix = @"PKNail AI ";

        // Width / height type parameters tried in order. Standard door, window and generic families use
        // one of these; anything else falls back to a "Width"/"Height" parameter looked up by name.
        private static readonly BuiltInParameter[] s_widthParams =
        {
            BuiltInParameter.DOOR_WIDTH, BuiltInParameter.WINDOW_WIDTH,
            BuiltInParameter.FAMILY_WIDTH_PARAM, BuiltInParameter.GENERIC_WIDTH,
        };
        private static readonly BuiltInParameter[] s_heightParams =
        {
            BuiltInParameter.DOOR_HEIGHT, BuiltInParameter.WINDOW_HEIGHT,
            BuiltInParameter.FAMILY_HEIGHT_PARAM, BuiltInParameter.GENERIC_HEIGHT,
        };

        // Sized types Revit already refused in this import ("family id|type name"), so the same failing size
        // is not rebuilt (and re-failed) for every later opening.
        private readonly HashSet<string> m_unbuildableOpeningTypes = new HashSet<string>(StringComparer.Ordinal);

        // The project's PKNAIL survey families per PKNAIL family category ("Doors" / "Windows"), in the
        // 1.0 Capture-list order. Read once per import: sized duplicates keep their family's name, so they
        // never change the list.
        private readonly Dictionary<string, List<SurveyFamilyDef>> m_surveyFamilyDefs =
            new Dictionary<string, List<SurveyFamilyDef>>(StringComparer.Ordinal);

        // Door/window types (element ids) this import has placed at least once, i.e. types known to build.
        private readonly HashSet<long> m_placedOpeningTypes = new HashSet<long>();

        // Family categories already reported as having no PKNAIL survey family (one report line each).
        private readonly HashSet<string> m_reportedNoSurveyFamilies = new HashSet<string>(StringComparer.Ordinal);

        // The family an opening is built from, and which of the AI's orientation values it can take.
        private sealed class openingFamily
        {
            internal FamilySymbol Base;
            internal bool IsSurveyFamily;
            internal bool TakesHinge = true;     // false: sliding / pocket / plain-opening survey family
            internal bool TakesFacing = true;    // false: survey family with no facing or swing side
        }

        // Records Revit errors and asks for a rollback (made silent by TransactionScope's ClearAfterRollback);
        // absorbs warnings into the import report like the Epic 7 capture does. The warnings are held until
        // the element is known to exist (KeepWarnings): an attempt that is rolled back is reported once,
        // with its error text, not once more per warning Revit raised on the way.
        private sealed class directBuildFailures : IFailuresPreprocessor
        {
            private readonly RevitDocument m_doc;
            private readonly List<string> m_errors = new List<string>();
            private readonly List<string> m_warnings = new List<string>();

            internal directBuildFailures(RevitDocument doc) { m_doc = doc; }

            internal string ErrorText => m_errors.Count == 0 ? null : string.Join(@" / ", m_errors.Distinct());

            // Call after the element's transaction committed and the element still exists.
            internal void KeepWarnings()
            {
                if (m_doc.m_importWarnings == null) { return; }
                foreach (string w in m_warnings.Distinct()) { m_doc.m_importWarnings.Add(w); }
            }

            public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
            {
                bool anyError = false;
                foreach (FailureMessageAccessor fma in failuresAccessor.GetFailureMessages())
                {
                    if (fma.GetSeverity() == FailureSeverity.Warning)
                    {
                        // "Wall is slightly off axis" is expected noise for deliberately angled walls.
                        if (!fma.GetFailureDefinitionId().Guid.Equals(failuresPreprocessor.sm_inaccurateWallFailureGuid))
                        { m_warnings.Add(oneLine(fma.GetDescriptionText())); }
                        failuresAccessor.DeleteWarning(fma);
                    }
                    else
                    {
                        anyError = true;
                        m_errors.Add(oneLine(fma.GetDescriptionText()));
                    }
                }
                return anyError ? FailureProcessingResult.ProceedWithRollBack : FailureProcessingResult.Continue;
            }

            // Revit's family errors span several lines ("... Error Information: ..."); the report is one line each.
            private static string oneLine(string text)
            {
                return string.Join(@" ", (text ?? string.Empty).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                                              .Select(s => s.Trim()));
            }
        }

        // IAiImportTarget (Epic 13)
        public CommandResult CreateWallDirect(DirectWallSpec spec, out long wallElementId, ref string msg)
        {
            wallElementId = -1;
            var failures = new directBuildFailures(this);
            try
            {
                Level level = resolveLevel(spec.LevelElementId);
                if (level == null)
                {
                    msg += @"CreateWallDirect: the document has no levels";
                    return CommandResult.Failed;
                }

                XYZ a = new XYZ(spec.StartX.ValueRevitInternal, spec.StartY.ValueRevitInternal, level.Elevation);
                XYZ b = new XYZ(spec.EndX.ValueRevitInternal, spec.EndY.ValueRevitInternal, level.Elevation);
                if (a.DistanceTo(b) < m_revitApp.ShortCurveTolerance)
                {
                    msg += @"CreateWallDirect: wall " + spec.Mark + @" is shorter than Revit's minimum curve length";
                    return CommandResult.Failed;
                }

                using (var txn = new TransactionScope(this, @"Direct Build Wall", failures))
                {
                    WallType wt = matchingWallType(spec.Thickness);
                    if (wt == null)
                    {
                        msg += @"CreateWallDirect: no wall type could be resolved for thickness " + spec.Thickness.RenderMetricMillimeters() + @" mm";
                        return CommandResult.Failed;
                    }

                    Wall wall = Wall.Create(doc, Line.CreateBound(a, b), wt.Id, level.Id,
                                            spec.Height.ValueRevitInternal, spec.BaseOffset.ValueRevitInternal,
                                            false, false);
                    if (wall == null)
                    {
                        msg += @"CreateWallDirect: Wall.Create returned null for " + spec.Mark;
                        return CommandResult.Failed;
                    }
                    ElementId wallId = wall.Id;

                    // The AI points are the wall centerline.
                    Parameter keyRef = wall.get_Parameter(BuiltInParameter.WALL_KEY_REF_PARAM);
                    if (keyRef != null && !keyRef.IsReadOnly) { keyRef.Set((int)WallLocationLine.WallCenterline); }

                    setTextParam(wall, BuiltInParameter.ALL_MODEL_MARK, spec.Mark);
                    setTextParam(wall, BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS, spec.Comment);

                    // Same PKNAIL marker as surveyed walls: lets the re-import guard see this content and
                    // lets the 1.0 tools treat the wall as a PKNAIL wall afterwards.
                    setWallState(wall, new WallState());

                    if (!txn.Commit() || doc.GetElement(wallId) == null)
                    {
                        msg += @"CreateWallDirect(" + spec.Mark + @"): Revit rejected the wall"
                               + (failures.ErrorText != null ? @": " + failures.ErrorText : @"");
                        return CommandResult.TransactionError;
                    }
                    failures.KeepWarnings();
                    wallElementId = wallId.Value;
                    return CommandResult.Succeeded;
                }
            }
            catch (Exception e)
            {
                msg += @"CreateWallDirect(" + spec.Mark + @"): " + e.Message;
                return CommandResult.TransactionError;
            }
        }

        // Where and how big an opening is, resolved once from its host wall (no transaction needed).
        private sealed class openingPlacement
        {
            internal Wall Wall;
            internal Level Level;
            internal XYZ Centre;
            internal XYZ Dir;
            internal double WidthFt;
            internal double HeightFt;
            internal double SillFt;       // above the level: the AI sill plus the wall's base offset
            internal BuiltInCategory Category;
            internal string WidthMm;
            internal string HeightMm;
        }

        // IAiImportTarget (Epic 13). Tries, in order: (1) a type of the measured size, (2) the nearest
        // existing type within OpeningFallback.MaxWidthDifferenceMeters, (3) a plain opening of the exact
        // size. Each attempt is its own silently-rolled-back transaction, and every fallback is described
        // in `note` for the import report. A family can fail because of the wall, not the size (the stock
        // imperial "Door-Passage-Single-Flush" cannot be built in a 100 mm wall at any width); the note
        // says so when a type that was placed elsewhere in this import fails in this wall.
        public CommandResult CreateOpeningDirect(long wallElementId, DirectOpeningSpec spec,
                                                 out long openingElementId, out string note, ref string msg)
        {
            openingElementId = -1;
            note = string.Empty;

            openingPlacement place;
            try
            {
                place = resolveOpeningPlacement(wallElementId, spec, ref msg);
                if (place == null) { return CommandResult.Failed; }
            }
            catch (Exception e)
            {
                msg += @"CreateOpeningDirect(" + spec.Mark + @"): " + e.Message;
                return CommandResult.Failed;
            }

            string kind = spec.IsDoor ? @"door" : @"window";
            string size = place.WidthMm + @" x " + place.HeightMm + @" mm";
            var familyNotes = new List<string>();
            openingFamily family;
            try
            {
                family = pickOpeningFamily(spec, place.Category, kind, familyNotes);
            }
            catch (Exception e)
            {
                msg += @"CreateOpeningDirect(" + spec.Mark + @"): " + e.Message;
                return CommandResult.Failed;
            }
            FamilySymbol baseSymbol = family?.Base;
            string why = null;
            long id;

            if (baseSymbol != null)
            {
                // (1) The sized type.
                string key = baseSymbol.Family.Id.Value + @"|" + sizedTypeName(spec);
                if (!m_unbuildableOpeningTypes.Contains(key))
                {
                    var notes = new List<string>(familyNotes);
                    if (tryPlaceInstance(place, spec, family, () => sizedOpeningSymbol(baseSymbol, spec, notes), notes, out id, out why))
                    {
                        openingElementId = id;
                        note = string.Join(@"; ", notes);
                        return CommandResult.Succeeded;
                    }
                    m_unbuildableOpeningTypes.Add(key);
                }
                else { why = @"the same size already failed earlier in this import"; }

                string wallMm = new Distance(place.Wall.Width, Distance.Units.RevitInternal).RenderMetricMillimeters();
                string failed = string.Join(@"; ", familyNotes.Concat(new[]
                    { @"Revit could not build """ + baseSymbol.FamilyName + @""" at " + size + @" in this " + wallMm
                      + @" mm thick wall (" + why + @")" }));

                // (2) The nearest existing type that is close enough in width.
                FamilySymbol nearest = nearestExistingType(family, place.Category, spec.Width.ValueMetricMeters, out double nearestWidthM);
                if (nearest != null)
                {
                    var notes = new List<string>();
                    if (tryPlaceInstance(place, spec, family, () => nearest, notes, out id, out string why2))
                    {
                        openingElementId = id;
                        notes.Insert(0, failed + @". Placed the nearest existing type """ + nearest.FamilyName + @": "
                                        + nearest.Name + @""" (" + Math.Round(nearestWidthM * 1000.0).ToString(@"0")
                                        + @" mm wide) instead");
                        note = string.Join(@"; ", notes);
                        return CommandResult.Succeeded;
                    }
                    failed += m_placedOpeningTypes.Contains(nearest.Id.Value)
                        ? @"; """ + nearest.Name + @""", which this import placed in another wall, fails here too, so the family"
                          + @" does not fit this wall whatever the " + kind + @" size"
                        : @"; the nearest type """ + nearest.Name + @""" also failed (" + why2 + @")";
                }

                // (3) A plain opening of the exact size.
                if (tryCutOpening(place, out id, out string why3))
                {
                    openingElementId = id;
                    note = failed + @". Cut a plain " + size + @" opening instead; swap in a suitable " + kind + @" family in Revit";
                    return CommandResult.Succeeded;
                }
                msg += @"CreateOpeningDirect(" + spec.Mark + @"): " + failed + @"; a plain opening also failed (" + why3 + @")";
                return CommandResult.Failed;
            }

            // No door/window family in the project at all: cut a plain opening of the measured size.
            if (tryCutOpening(place, out id, out why))
            {
                openingElementId = id;
                note = @"no " + kind + @" family is loaded, so a plain " + size + @" opening was cut";
                return CommandResult.Succeeded;
            }
            msg += @"CreateOpeningDirect(" + spec.Mark + @"): no " + kind + @" family is loaded and a plain opening failed (" + why + @")";
            return CommandResult.Failed;
        }

        private openingPlacement resolveOpeningPlacement(long wallElementId, DirectOpeningSpec spec, ref string msg)
        {
            Wall wall = doc.GetElement(new ElementId(wallElementId)) as Wall;
            LocationCurve lc = wall?.Location as LocationCurve;
            if (wall == null || lc?.Curve == null)
            {
                msg += @"CreateOpeningDirect: element " + wallElementId + @" is not a wall with a location line";
                return null;
            }
            Level level = (doc.GetElement(wall.LevelId) as Level) ?? resolveLevel(-1);
            if (level == null)
            {
                msg += @"CreateOpeningDirect: no level to host on";
                return null;
            }

            XYZ start = lc.Curve.GetEndPoint(0);
            XYZ dir = (lc.Curve.GetEndPoint(1) - start).Normalize();
            Parameter baseOffP = wall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET);
            double baseOffset = baseOffP != null ? baseOffP.AsDouble() : 0.0;

            return new openingPlacement
            {
                Wall = wall,
                Level = level,
                Centre = start + (dir * spec.LocationFromStart.ValueRevitInternal),
                Dir = dir,
                WidthFt = spec.Width.ValueRevitInternal,
                HeightFt = spec.Height.ValueRevitInternal,
                SillFt = spec.Sill.ValueRevitInternal + baseOffset,
                Category = spec.IsDoor ? BuiltInCategory.OST_Doors : BuiltInCategory.OST_Windows,
                WidthMm = Math.Round(spec.Width.ValueMetricMillimeters).ToString(@"0"),
                HeightMm = Math.Round(spec.Height.ValueMetricMillimeters).ToString(@"0"),
            };
        }

        // One attempt at a hosted door/window. True only if the instance still exists after the commit.
        private bool tryPlaceInstance(openingPlacement p, DirectOpeningSpec spec, openingFamily family,
                                      Func<FamilySymbol> getSymbol, List<string> notes, out long id, out string why)
        {
            id = -1;
            why = null;
            var failures = new directBuildFailures(this);
            try
            {
                using (var txn = new TransactionScope(this, @"Direct Build Opening", failures))
                {
                    FamilySymbol symbol = getSymbol();
                    if (!symbol.IsActive) { symbol.Activate(); }

                    // The type may have been duplicated and resized a moment ago: regenerate it before
                    // hosting, and host at the final height. In the 2026-11-06 runs the only instances that
                    // were not touched again after creation (the two W2 windows: no flip needed) were also
                    // the only ones that left their wall uncut. ensureWallIsCut below remains the backstop.
                    doc.Regenerate();
                    XYZ hostPoint = new XYZ(p.Centre.X, p.Centre.Y, p.Level.Elevation + p.SillFt);
                    FamilyInstance inst = doc.Create.NewFamilyInstance(
                        hostPoint, symbol, p.Wall, p.Level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                    if (inst == null) { why = @"NewFamilyInstance returned nothing"; return false; }
                    ElementId instId = inst.Id;

                    Parameter sillP = inst.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM);
                    if (sillP != null && !sillP.IsReadOnly) { sillP.Set(p.SillFt); }
                    setTextParam(inst, BuiltInParameter.ALL_MODEL_MARK, spec.Mark);
                    setTextParam(inst, BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS, spec.Comment);

                    doc.Regenerate();
                    orientOpening(inst, spec, family, notes);
                    ElementId symbolId = symbol.Id;

                    if (!txn.Commit() || doc.GetElement(instId) == null)
                    {
                        why = failures.ErrorText ?? @"Revit rolled the opening back";
                        return false;
                    }
                    failures.KeepWarnings();
                    m_placedOpeningTypes.Add(symbolId.Value);
                    id = instId.Value;
                }
            }
            catch (Exception e)
            {
                why = failures.ErrorText ?? e.Message;
                return false;
            }

            ensureWallIsCut(p, spec, notes);
            return true;
        }

        // A hosted door/window normally cuts its own hole. Check that the wall really is open at the middle
        // of the opening; if the family left the wall solid there, cut the measured opening ourselves so the
        // instance does not sit on an uncut wall, and say so. Never fails the opening.
        private void ensureWallIsCut(openingPlacement p, DirectOpeningSpec spec, List<string> notes)
        {
            try
            {
                if (!wallIsSolidAtOpening(p)) { return; }

                string kind = spec.IsDoor ? @"door" : @"window";
                if (tryCutOpening(p, out long _, out string why) && !wallIsSolidAtOpening(p))
                { notes.Add(@"the " + kind + @" family did not cut the wall, so the " + p.WidthMm + @" x " + p.HeightMm + @" mm opening was cut behind it"); }
                else
                {
                    notes.Add(@"the " + kind + @" was placed but the wall is still solid behind it"
                              + (why != null ? @" (cutting the opening failed: " + why + @")" : @""));
                }
            }
            catch (Exception) { /* a check only: the opening itself is placed */ }
        }

        // True when the wall's own solid still fills the middle of the opening (probed straight through
        // the wall's thickness at the opening's centre and mid-height).
        private bool wallIsSolidAtOpening(openingPlacement p)
        {
            XYZ across = new XYZ(-p.Dir.Y, p.Dir.X, 0.0);
            XYZ mid = new XYZ(p.Centre.X, p.Centre.Y, p.Level.Elevation + p.SillFt + (p.HeightFt / 2.0));
            Line probe = Line.CreateBound(mid - (across * p.Wall.Width), mid + (across * p.Wall.Width));

            foreach (GeometryObject go in p.Wall.get_Geometry(new Options()))
            {
                Solid solid = go as Solid;
                if (solid == null || solid.Volume <= 0.0) { continue; }
                SolidCurveIntersection inside = solid.IntersectWithCurve(probe, new SolidCurveIntersectionOptions());
                if (inside != null && inside.SegmentCount > 0) { return true; }
            }
            return false;
        }

        // A plain rectangular wall opening of the exact measured size. True only if it exists after commit.
        private bool tryCutOpening(openingPlacement p, out long id, out string why)
        {
            id = -1;
            why = null;
            var failures = new directBuildFailures(this);
            try
            {
                using (var txn = new TransactionScope(this, @"Direct Build Plain Opening", failures))
                {
                    XYZ half = p.Dir * (p.WidthFt / 2.0);
                    XYZ lo = new XYZ(p.Centre.X - half.X, p.Centre.Y - half.Y, p.Level.Elevation + p.SillFt);
                    XYZ hi = new XYZ(p.Centre.X + half.X, p.Centre.Y + half.Y, p.Level.Elevation + p.SillFt + p.HeightFt);
                    Opening hole = doc.Create.NewOpening(p.Wall, lo, hi);
                    if (hole == null) { why = @"NewOpening returned nothing"; return false; }
                    ElementId holeId = hole.Id;

                    if (!txn.Commit() || doc.GetElement(holeId) == null)
                    {
                        why = failures.ErrorText ?? @"Revit rolled the opening back";
                        return false;
                    }
                    failures.KeepWarnings();
                    id = holeId.Value;
                    return true;
                }
            }
            catch (Exception e)
            {
                why = failures.ErrorText ?? e.Message;
                return false;
            }
        }

        private Level resolveLevel(long levelElementId)
        {
            if (levelElementId > 0 && doc.GetElement(new ElementId(levelElementId)) is Level byId) { return byId; }
            List<Level> levels = getLevels();
            return levels.Count > 0 ? levels[0] : null;
        }

        private static void setTextParam(Element e, BuiltInParameter bip, string value)
        {
            if (string.IsNullOrEmpty(value)) { return; }
            Parameter p = e.get_Parameter(bip);
            if (p != null && !p.IsReadOnly && p.StorageType == StorageType.String) { p.Set(value); }
        }

        private static bool isWallHosted(FamilySymbol s)
        {
            Parameter hosting = s.Family?.get_Parameter(BuiltInParameter.FAMILY_HOSTING_BEHAVIOR);
            return hosting != null && hosting.AsInteger() == 1;   // 1 = wall-hosted
        }

        // The family to build this opening from. When the project has PKNAIL survey families of the kind,
        // the AI's OpeningType / FamilyDefIdx picks one from the 1.0 Capture list (OpeningFamilyResolver),
        // so a sliding door is a sliding door. Otherwise (stock template) the first wall-hosted family is
        // used for every opening, and that is reported once if the AI had asked for a specific family.
        private openingFamily pickOpeningFamily(DirectOpeningSpec spec, BuiltInCategory cat, string kind, List<string> notes)
        {
            string familyCategory = spec.IsDoor ? FamilyCategory.Doors : FamilyCategory.Windows;
            List<SurveyFamilyDef> defs = surveyFamilyDefs(familyCategory);
            if (defs.Count > 0)
            {
                List<SurveyFamilyInfo> infos = defs.Select(d => new SurveyFamilyInfo(d.Name, lateralKind(d))).ToList();
                int i = OpeningFamilyResolver.Resolve(infos, spec.OpeningType, spec.FamilyDefIdx, out string resolveNote);
                FamilySymbol surveySymbol = surveyFamilyBaseSymbol(familyCategory, defs[i].Name);
                if (surveySymbol != null)
                {
                    if (resolveNote != null) { notes.Add(resolveNote); }
                    return new openingFamily
                    {
                        Base = surveySymbol,
                        IsSurveyFamily = true,
                        TakesHinge = defs[i].LateralOrientationTypeVal == SurveyFamilyDef.LateralOrientationType.Hinged,
                        TakesFacing = spec.IsDoor
                            ? defs[i].FacingOrientationTypeVal == SurveyFamilyDef.FacingOrientationType.Swinging
                            : defs[i].FacingOrientationTypeVal != SurveyFamilyDef.FacingOrientationType.None,
                    };
                }
            }

            FamilySymbol stock = pickOpeningSymbol(cat);
            if (stock == null) { return null; }
            bool askedForFamily = spec.FamilyDefIdx != 0 || !string.IsNullOrEmpty(spec.OpeningType);
            if (askedForFamily && m_reportedNoSurveyFamilies.Add(familyCategory))
            {
                notes.Add(@"this project has no PKNAIL " + kind + @" families, so OpeningType / FamilyDefIdx cannot choose one; """
                          + stock.FamilyName + @""" is used for every " + kind);
            }
            return new openingFamily { Base = stock };
        }

        private static FamilyLateralKind lateralKind(SurveyFamilyDef def)
        {
            switch (def.LateralOrientationTypeVal)
            {
                case SurveyFamilyDef.LateralOrientationType.Hinged: return FamilyLateralKind.Hinged;
                case SurveyFamilyDef.LateralOrientationType.Sliding: return FamilyLateralKind.Sliding;
                default: return FamilyLateralKind.None;
            }
        }

        // The survey-family list exactly as the 1.0 Capture dialog builds it (same reconcile, same order),
        // so FamilyDefIdx means the same thing on both paths. Empty when the document has no PKNAIL family
        // parameters or no family carries the PKNAIL family category.
        private List<SurveyFamilyDef> surveyFamilyDefs(string familyCategory)
        {
            if (!m_surveyFamilyDefs.TryGetValue(familyCategory, out List<SurveyFamilyDef> defs))
            {
                var op = new WallHostedObjectSurveyParams(familyCategory);
                try { wallHostedInstanceDefinition.ReconcileSurveyFamilyTypeDefs(this, familyCategory, op); }
                catch (Exception) { op.ProjectSurveyFamilyDefs.Clear(); }
                defs = op.ProjectSurveyFamilyDefs;
                m_surveyFamilyDefs[familyCategory] = defs;
            }
            return defs;
        }

        // The type to size from for a PKNAIL survey family: its first type that is not one of our own sized
        // duplicates (those are only used when nothing else is left).
        private FamilySymbol surveyFamilyBaseSymbol(string familyCategory, string pkFamilyName)
        {
            FamilySymbol sized = null;
            foreach (Element el in FamilyCategoryUtils.GetAllTypesInCategory(doc, familyCategory).OrderBy(e => e.Id.Value))
            {
                FamilySymbol s = el as FamilySymbol;
                if (s == null || FamilyCategoryUtils.GetFamilyName(s) != pkFamilyName) { continue; }
                if (!s.Name.StartsWith(DirectBuildTypePrefix, StringComparison.Ordinal)) { return s; }
                if (sized == null) { sized = s; }
            }
            return sized;
        }

        // The stock base type to size from: the first wall-hosted family type in the category (only a
        // wall-hosted family cuts its opening into the wall), else the first type. Our own sized duplicates
        // are skipped so the choice stays stable across imports.
        private FamilySymbol pickOpeningSymbol(BuiltInCategory cat)
        {
            FamilySymbol first = null;
            foreach (FamilySymbol s in new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).OfCategory(cat)
                                          .Cast<FamilySymbol>().OrderBy(s => s.Id.Value))
            {
                if (s.Name.StartsWith(DirectBuildTypePrefix, StringComparison.Ordinal)) { continue; }
                if (first == null) { first = s; }
                if (isWallHosted(s)) { return s; }
            }
            return first;
        }

        // Fallback (2): the existing type (our own sized duplicates included — they are known to build)
        // whose width is closest to the measured width, within the tolerance. A PKNAIL survey family only
        // falls back to its own types, so a sliding door is never replaced by a swing door; the stock
        // family falls back to any type of the category, wall-hosted ones preferred.
        private FamilySymbol nearestExistingType(openingFamily family, BuiltInCategory cat, double wantedMeters, out double widthMeters)
        {
            widthMeters = 0.0;
            List<FamilySymbol> candidates;
            if (family.IsSurveyFamily)
            {
                candidates = family.Base.Family.GetFamilySymbolIds().Select(id => doc.GetElement(id)).OfType<FamilySymbol>()
                                   .OrderBy(s => s.Id.Value).ToList();
            }
            else
            {
                List<FamilySymbol> all = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).OfCategory(cat)
                                             .Cast<FamilySymbol>().OrderBy(s => s.Id.Value).ToList();
                List<FamilySymbol> hosted = all.Where(isWallHosted).ToList();
                candidates = hosted.Count > 0 ? hosted : all;
            }

            var widths = candidates.Select(s => readTypeWidthMeters(s)).ToList();
            int i = OpeningFallback.ChooseNearest(widths, wantedMeters);
            if (i < 0) { return null; }
            widthMeters = widths[i];
            return candidates[i];
        }

        private static double readTypeWidthMeters(FamilySymbol s)
        {
            foreach (BuiltInParameter bip in s_widthParams)
            {
                Parameter p = s.get_Parameter(bip);
                if (p != null && p.StorageType == StorageType.Double && p.AsDouble() > 0.0)
                { return new Distance(p.AsDouble(), Distance.Units.RevitInternal).ValueMetricMeters; }
            }
            Parameter byName = s.LookupParameter(@"Width");
            if (byName != null && byName.StorageType == StorageType.Double && byName.AsDouble() > 0.0)
            { return new Distance(byName.AsDouble(), Distance.Units.RevitInternal).ValueMetricMeters; }
            return 0.0;
        }

        private static string sizedTypeName(DirectOpeningSpec spec) =>
            DirectBuildTypePrefix + (spec.IsDoor ? @"Door " : @"Window ")
            + Math.Round(spec.Width.ValueMetricMillimeters).ToString(@"0") + @" x "
            + Math.Round(spec.Height.ValueMetricMillimeters).ToString(@"0");

        // A type of the measured size, e.g. "PKNail AI Door 760 x 1990", duplicated from the base type and
        // reused on later openings of the same size. This fixes the "every opening gets the default size"
        // problem of the instance-parameter approach (Epic 12 Bug 1B).
        private FamilySymbol sizedOpeningSymbol(FamilySymbol baseSymbol, DirectOpeningSpec spec, List<string> notes)
        {
            string name = sizedTypeName(spec);
            foreach (ElementId id in baseSymbol.Family.GetFamilySymbolIds())
            {
                if (doc.GetElement(id) is FamilySymbol existing && existing.Name == name) { return existing; }
            }

            FamilySymbol sized = baseSymbol.Duplicate(name) as FamilySymbol;
            if (sized == null)
            {
                notes.Add(@"could not create a sized type; placed """ + baseSymbol.Name + @""" at its own size");
                return baseSymbol;
            }

            bool widthOk = setFirstLength(sized, s_widthParams, @"Width", spec.Width.ValueRevitInternal);
            bool heightOk = setFirstLength(sized, s_heightParams, @"Height", spec.Height.ValueRevitInternal);
            if (!widthOk || !heightOk)
            {
                notes.Add(@"family """ + baseSymbol.FamilyName + @""" has no editable "
                          + (!widthOk && !heightOk ? @"width or height" : (!widthOk ? @"width" : @"height"))
                          + @" type parameter, so that dimension uses the family's own size");
            }
            return sized;
        }

        private static bool setFirstLength(Element e, BuiltInParameter[] candidates, string fallbackName, double valueFt)
        {
            foreach (BuiltInParameter bip in candidates)
            {
                Parameter p = e.get_Parameter(bip);
                if (p != null && !p.IsReadOnly && p.StorageType == StorageType.Double) { p.Set(valueFt); return true; }
            }
            Parameter byName = e.LookupParameter(fallbackName);
            if (byName != null && !byName.IsReadOnly && byName.StorageType == StorageType.Double) { byName.Set(valueFt); return true; }
            return false;
        }

        // Apply the Revit-free flip rules in OpeningOrientation (windows face the exterior; doors follow the
        // AI swing/hinge when provided, otherwise keep the family default). A PKNAIL survey family that has
        // no swing or no hinge (sliding, pocket, plain opening) keeps its default there: the AI's values
        // for it are placeholders.
        private void orientOpening(FamilyInstance inst, DirectOpeningSpec spec, openingFamily family, List<string> notes)
        {
            var interior = new PlanPoint(spec.InteriorX, spec.InteriorY);
            PlanPoint facing = new PlanPoint(inst.FacingOrientation.X, inst.FacingOrientation.Y);

            bool flipFacing = family.TakesFacing
                && (spec.IsDoor
                    ? OpeningOrientation.DoorNeedsFlipFacing(facing, interior, spec.Swing)
                    : OpeningOrientation.WindowNeedsFlipFacing(facing, interior));
            if (flipFacing)
            {
                if (inst.CanFlipFacing) { inst.flipFacing(); doc.Regenerate(); }
                else { notes.Add(@"family has no Flip Facing control, so the facing/swing could not be set"); }
            }

            if (spec.IsDoor && spec.Hinge != null && family.TakesHinge)
            {
                PlanPoint hand = new PlanPoint(inst.HandOrientation.X, inst.HandOrientation.Y);
                if (OpeningOrientation.DoorNeedsFlipHand(hand, interior, spec.Hinge))
                {
                    if (inst.CanFlipHand) { inst.flipHand(); }
                    else { notes.Add(@"family has no Flip Hand control, so the hinge side could not be set"); }
                }
            }
        }
    }
}
