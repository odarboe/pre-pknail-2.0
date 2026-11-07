using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Autodesk.Revit.DB;

namespace PKNailForRevit
{
    // Epic 11e.3 — bring the shipped PKNAIL door/window survey families into a project that has none.
    // A project made from a stock Revit template only has that template's door and window family, so an
    // AI file's sliding doors would be built as swing doors (and the imperial stock door cannot be built in
    // thin walls at all). Loading the PKNAIL families and tagging them exactly as the PKNAIL template does
    // gives the import, and the 1.0 Capture list, the same families in the same order on any template.
    public sealed partial class RevitDocument
    {
        private sealed class shippedSurveyFamily
        {
            internal readonly string File;       // family file name; the family is named after it
            internal readonly string Name;       // "PKNail Family"
            internal readonly string Category;   // "PKNail Family Category"
            internal readonly int Order;         // "PKNail Family Order" (1-based position in the Capture list)
            internal readonly string Lateral;    // "PKNail Family Lateral Orientation"
            internal readonly string Facing;     // "PKNail Family Facing Orientation"

            internal shippedSurveyFamily(string file, string name, string category, int order, string lateral, string facing)
            { File = file; Name = name; Category = category; Order = order; Lateral = lateral; Facing = facing; }
        }

        // The door and window families of the PKNAIL template, with the values its types carry.
        private static readonly shippedSurveyFamily[] sm_shippedSurveyFamilies =
        {
            new shippedSurveyFamily(@"PKNail Single.rfa", @"Single", FamilyCategory.Doors, 1, @"Hinged", @"Swinging"),
            new shippedSurveyFamily(@"PKNail Double.rfa", @"Double", FamilyCategory.Doors, 2, @"None", @"Swinging"),
            new shippedSurveyFamily(@"PKNail Opening.rfa", @"Opening", FamilyCategory.Doors, 3, @"None", @"None"),
            new shippedSurveyFamily(@"PKNail Sliding Glass.rfa", @"Sliding Glass", FamilyCategory.Doors, 4, @"Sliding", @"None"),
            new shippedSurveyFamily(@"PKNail Sliding.rfa", @"Sliding", FamilyCategory.Doors, 5, @"Sliding", @"None"),
            new shippedSurveyFamily(@"PKNail Pocket.rfa", @"Pocket", FamilyCategory.Doors, 6, @"Sliding", @"None"),
            new shippedSurveyFamily(@"PKNail Fixed.rfa", @"Fixed", FamilyCategory.Windows, 1, @"None", @"Facing"),
            new shippedSurveyFamily(@"PKNail Double Hung.rfa", @"Double Hung", FamilyCategory.Windows, 2, @"None", @"Facing"),
        };

        // A family already in the project is left exactly as it is.
        private sealed class keepProjectFamily : IFamilyLoadOptions
        {
            public bool OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
            { overwriteParameterValues = false; return false; }

            public bool OnSharedFamilyFound(Family sharedFamily, bool familyInUse, out FamilySource source, out bool overwriteParameterValues)
            { source = FamilySource.Project; overwriteParameterValues = false; return false; }
        }

        // Loads the shipped PKNAIL door families when the project has no PKNAIL door family, and likewise
        // for windows. A project that already has survey families of a kind (the PKNAIL template, or a
        // customised one) is not touched for that kind. Call after EnsureDocumentParameters and before the
        // import transaction group; runs in its own transaction. `note` says what was loaded (empty when
        // nothing was needed). A failure is not fatal: the import then uses the project's own families.
        public CommandResult EnsureSurveyFamilies(out string note, ref string msg)
        {
            note = string.Empty;
            var failures = new directBuildFailures(this);
            try
            {
                List<shippedSurveyFamily> wanted = new List<shippedSurveyFamily>();
                foreach (string category in new[] { FamilyCategory.Doors, FamilyCategory.Windows })
                {
                    if (FamilyCategoryUtils.GetAllTypesInCategory(doc, category).Count == 0)
                    { wanted.AddRange(sm_shippedSurveyFamilies.Where(f => f.Category == category)); }
                }
                if (wanted.Count == 0) { return CommandResult.Succeeded; }

                string folder = shippedFamilyFolder(wanted[0].File, out string searched);
                if (folder == null)
                {
                    msg += @"The PKNAIL door/window family files were not found (looked in " + searched + @")";
                    return CommandResult.Failed;
                }

                List<string> loaded = new List<string>();
                using (Transaction t = new Transaction(doc, @"Load PKNail Families"))
                {
                    t.Start();
                    FailureHandlingOptions opts = t.GetFailureHandlingOptions();
                    opts.SetFailuresPreprocessor(failures);
                    opts.SetClearAfterRollback(true);
                    t.SetFailureHandlingOptions(opts);

                    foreach (shippedSurveyFamily f in wanted)
                    {
                        string path = Path.Combine(folder, f.File);
                        if (!System.IO.File.Exists(path)) { continue; }

                        string familyName = Path.GetFileNameWithoutExtension(f.File);
                        Family family = findFamily(familyName);
                        if (family == null)
                        {
                            doc.LoadFamily(path, new keepProjectFamily(), out family);
                            if (family == null) { family = findFamily(familyName); }
                        }
                        if (family == null) { continue; }

                        foreach (ElementId id in family.GetFamilySymbolIds())
                        {
                            FamilySymbol sym = doc.GetElement(id) as FamilySymbol;
                            if (sym == null) { continue; }
                            PKNailParameter.FamilyCategory.SetString(sym, f.Category);
                            PKNailParameter.FamilyOrder.SetInteger(sym, f.Order);
                            // These three travel inside the family file; fill them only if it came without.
                            if (PKNailParameter.Family.GetString(sym) == @"") { PKNailParameter.Family.SetString(sym, f.Name); }
                            if (PKNailParameter.FamilyLateralOrientation.GetString(sym) == @"")
                            { PKNailParameter.FamilyLateralOrientation.SetString(sym, f.Lateral); }
                            if (PKNailParameter.FamilyFacingOrientation.GetString(sym) == @"")
                            { PKNailParameter.FamilyFacingOrientation.SetString(sym, f.Facing); }
                        }
                        loaded.Add(f.Name);
                    }

                    if (loaded.Count == 0) { t.RollBack(); }
                    else if (t.Commit() != TransactionStatus.Committed)
                    {
                        msg += @"Revit rejected the PKNAIL family load" + (failures.ErrorText != null ? @": " + failures.ErrorText : @"");
                        return CommandResult.TransactionError;
                    }
                }

                if (loaded.Count == 0)
                {
                    msg += @"None of the PKNAIL door/window families in " + folder + @" could be loaded";
                    return CommandResult.Failed;
                }

                // The lists read before this point are stale now.
                m_surveyFamilyDefs.Clear();
                note = @"Loaded the PKNAIL families " + string.Join(@", ", loaded)
                       + @" into this project (it had none of that kind), from " + folder;
                return CommandResult.Succeeded;
            }
            catch (Exception e)
            {
                msg += @"Loading the PKNAIL families failed: " + (failures.ErrorText ?? e.Message);
                return CommandResult.Failed;
            }
        }

        private Family findFamily(string familyName)
        {
            foreach (Element el in new FilteredElementCollector(doc).OfClass(typeof(Family)))
            { if (el.Name == familyName) { return el as Family; } }
            return null;
        }

        // The folder holding the shipped family files for this project's unit system: next to (or above)
        // the add-in in a development build ("RevitFiles\Metric"), or the installed library
        // ("%ProgramData%\PointKnown\PKNail Pro For Revit <year>\Libraries\US Metric").
        private string shippedFamilyFolder(string probeFile, out string searched)
        {
            bool imperial = (unitSystem == UnitSystem.Imperial);
            string devName = imperial ? @"Imperial" : @"Metric";
            string installedName = imperial ? @"US Imperial" : @"US Metric";

            List<string> candidates = new List<string>();
            string dir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            for (int up = 0; up < 5 && !string.IsNullOrEmpty(dir); up++)
            {
                candidates.Add(Path.Combine(dir, @"RevitFiles", devName));
                candidates.Add(Path.Combine(dir, @"Libraries", installedName));
                dir = Path.GetDirectoryName(dir);
            }

            string vendorDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"PointKnown");
            if (Directory.Exists(vendorDir))
            {
                foreach (string productDir in Directory.GetDirectories(vendorDir, @"PKNail Pro For Revit*"))
                { candidates.Add(Path.Combine(productDir, @"Libraries", installedName)); }
            }

            searched = string.Join(@"; ", candidates);
            return candidates.FirstOrDefault(c => System.IO.File.Exists(Path.Combine(c, probeFile)));
        }
    }
}
