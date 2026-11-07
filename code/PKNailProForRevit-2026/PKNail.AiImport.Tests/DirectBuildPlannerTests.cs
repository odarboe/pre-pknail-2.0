using NUnit.Framework;

using PKNail.AiImport.DirectBuild;
using PKNail.AiImport.Mapping;
using PKNail.AiImport.Model;

namespace PKNail.AiImport.Tests;

/// <summary>
/// Epic 13 — Direct Build planning: the AI file's independent walls become a complete, final build plan.
/// Nothing is deleted, every change is reported, and a bad element is skipped without blocking the file.
/// </summary>
[TestFixture]
public class DirectBuildPlannerTests
{
    private static string OctoberDir => Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "October5");

    public static IEnumerable<string> OctoberFiles() =>
        Directory.GetFiles(Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "October5"), "*.json")
                 .Select(Path.GetFileName)
                 .OrderBy(n => n)!;

    private static DetectedWall Wall(double sx, double sy, double ex, double ey, params DetectedOpening[] openings) => new()
    {
        StartMeters = new DetectedPoint2D { X = sx, Y = sy },
        EndMeters = new DetectedPoint2D { X = ex, Y = ey },
        ThicknessMeters = 0.115,
        Openings = openings.ToList(),
    };

    private static DetectedOpening Opening(double centre, double width, string kind = "Window") => new()
    {
        LocationFromStartMeters = centre,
        WidthMeters = width,
        HeightMeters = 1.2,
        SillHeightMeters = kind == "Door" ? 0.0 : 0.8,
        Kind = kind,
    };

    private static DetectedBuilding Building(params DetectedWall[] walls) => new()
    {
        SchemaVersion = "2.1",
        Levels =
        {
            new DetectedLevel
            {
                Floor2FloorMeters = 2.75, Floor2CeilingMeters = 2.45,
                TypicalWindowSillHeightMeters = 0.8, TypicalWindowHeadHeightMeters = 2.0,
                Walls = walls.ToList(),
            },
        },
    };

    // --- The real October exports ------------------------------------------------------------------

    [TestCaseSource(nameof(OctoberFiles))]
    public void OctoberExport_EveryWallAndOpeningIsPlanned_NothingDeleted(string file)
    {
        DetectedBuilding building = AiModelReader.Load(Path.Combine(OctoberDir, file));
        ValidationReport validation = AiModelValidator.Validate(building, elementIssuesAreWarnings: true);
        Assert.That(validation.IsValid, Is.True, validation.ToString());

        DirectBuildPlan plan = DirectBuildPlanner.Plan(building);

        int jsonWalls = building.Levels.Sum(l => l.Walls.Count);
        int jsonOpenings = building.Levels.Sum(l => l.Walls.Sum(w => w.Openings.Count));
        Assert.That(plan.WallCount, Is.EqualTo(jsonWalls), "Direct Build never drops a wall");
        Assert.That(plan.OpeningCount, Is.EqualTo(jsonOpenings), "Direct Build never drops an opening");
        Assert.That(plan.Skipped, Is.Empty, string.Join(Environment.NewLine, plan.Skipped));
    }

    [TestCaseSource(nameof(OctoberFiles))]
    public void OctoberExport_OpeningsFitInsideTheirWalls_AndLevelSitsAtZero(string file)
    {
        DirectBuildPlan plan = DirectBuildPlanner.Plan(AiModelReader.Load(Path.Combine(OctoberDir, file)));

        Assert.That(plan.Levels[0].ElevationMeters, Is.EqualTo(0.0).Within(1e-9));
        foreach (PlannedWall w in plan.Levels.SelectMany(l => l.Walls))
            foreach (PlannedOpening o in w.Openings)
            {
                Assert.That(o.CentreFromStartMeters - (o.WidthMeters / 2.0), Is.GreaterThanOrEqualTo(-1e-9), $"{o.Label} start");
                Assert.That(o.CentreFromStartMeters + (o.WidthMeters / 2.0), Is.LessThanOrEqualTo(w.LengthMeters + 1e-9), $"{o.Label} end");
            }
    }

    [TestCaseSource(nameof(OctoberFiles))]
    public void OctoberExport_IsRebasedToTheOrigin(string file)
    {
        DirectBuildPlan plan = DirectBuildPlanner.Plan(AiModelReader.Load(Path.Combine(OctoberDir, file)));
        IEnumerable<PlanPoint> pts = plan.Levels.SelectMany(l => l.Walls).SelectMany(w => new[] { w.Start, w.End });

        // Gap-closing can move an end by at most the join tolerance, so the minimum stays at ~0.
        Assert.That(pts.Min(p => p.X), Is.EqualTo(0.0).Within(CleanupOptions.Default.JoinToleranceMeters));
        Assert.That(pts.Min(p => p.Y), Is.EqualTo(0.0).Within(CleanupOptions.Default.JoinToleranceMeters));
        Assert.That(plan.RebaseXMeters, Is.Not.EqualTo(0.0), "the October survey frame is far from 0,0");
    }

    // --- Openings ----------------------------------------------------------------------------------

    [Test]
    public void Opening_StaysAtTheSamePlanPosition_WhenTheWallStartIsExtended()
    {
        // W2 stops 60 mm short of W1; the cleaner extends its start, so the centre is re-measured +60 mm.
        DetectedBuilding b = Building(Wall(0, 0, 10, 0), Wall(5, 0.06, 5, 4, Opening(2.0, 0.9)));
        DirectBuildPlan plan = DirectBuildPlanner.Plan(b);

        PlannedWall w2 = plan.Levels[0].Walls[1];
        Assert.That(w2.Start.Y, Is.EqualTo(0.0).Within(1e-9));
        Assert.That(w2.Openings[0].CentreFromStartMeters, Is.EqualTo(2.06).Within(1e-9));
    }

    [Test]
    public void Opening_OverhangingTheWallEnd_IsSlidInside_AndReported()
    {
        DirectBuildPlan plan = DirectBuildPlanner.Plan(Building(Wall(0, 0, 4, 0, Opening(3.8, 0.6))));

        PlannedOpening o = plan.Levels[0].Walls[0].Openings[0];
        Assert.That(o.CentreFromStartMeters, Is.EqualTo(3.7).Within(1e-9));
        Assert.That(plan.Adjustments, Has.Some.Contains("W1-N1: moved 100 mm along W1"));
    }

    [Test]
    public void BadElements_AreSkipped_TheRestIsStillPlanned()
    {
        DetectedBuilding b = Building(
            Wall(0, 0, 4, 0, Opening(2.0, 5.0)),                    // opening wider than its wall
            Wall(1, 1, 1, 1),                                        // zero length
            new DetectedWall { ThicknessMeters = 0.1, LengthMeters = 3 }, // v1 wall: no coordinates
            Wall(0, 0, 0, 3, Opening(1.0, 0.8, "Door")));

        DirectBuildPlan plan = DirectBuildPlanner.Plan(b);

        Assert.That(plan.WallCount, Is.EqualTo(2));
        Assert.That(plan.OpeningCount, Is.EqualTo(1));
        Assert.That(plan.Skipped, Has.Count.EqualTo(3));
        Assert.That(plan.Skipped, Has.Some.Contains("W1-N1").And.Contains("wide"));
        Assert.That(plan.Skipped, Has.Some.Contains("W2").And.Contains("zero-length"));
        Assert.That(plan.Skipped, Has.Some.Contains("W3").And.Contains("no StartMeters/EndMeters"));
    }

    [Test]
    public void OverlappingOpenings_AreBothPlaced_AndFlagged()
    {
        DirectBuildPlan plan = DirectBuildPlanner.Plan(Building(Wall(0, 0, 6, 0, Opening(2.0, 1.0), Opening(2.9, 1.0))));

        Assert.That(plan.OpeningCount, Is.EqualTo(2));
        Assert.That(plan.Warnings, Has.Some.Contains("W1-N1 and W1-N2 overlap by 100 mm"));
    }

    [Test]
    public void DoorOrientation_IsNullWhenNotProvided_AndCarriedWhenProvided()
    {
        DetectedOpening given = Opening(1.0, 0.8, "Door");
        given.HingeSideIndex = 1;
        given.SwingDirectionIndex = 1;
        DirectBuildPlan plan = DirectBuildPlanner.Plan(Building(Wall(0, 0, 6, 0, Opening(4.0, 0.8, "Door"), given)));

        List<PlannedOpening> doors = plan.Levels[0].Walls[0].Openings.ToList();
        Assert.That(doors[0].Hinge, Is.Null);
        Assert.That(doors[0].Swing, Is.Null);
        Assert.That(doors[1].Hinge, Is.EqualTo(HingeSideKind.Right));
        Assert.That(doors[1].Swing, Is.EqualTo(SwingDirectionKind.In));
    }

    // --- Labels, heights, levels, interior side -------------------------------------------------

    [Test]
    public void Labels_UseTheAiIds_ElseWallAndKindNumbering()
    {
        DetectedWall withIds = Wall(0, 0, 6, 0, Opening(1.0, 0.8, "Door"));
        withIds.Id = "W7";
        withIds.Openings[0].Id = "W7-D9";
        DirectBuildPlan plan = DirectBuildPlanner.Plan(Building(
            withIds, Wall(0, 0, 0, 5, Opening(1.0, 0.8, "Door"), Opening(3.0, 0.8), Opening(4.2, 0.8, "Door"))));

        Assert.That(plan.Levels[0].Walls[0].Label, Is.EqualTo("W7"));
        Assert.That(plan.Levels[0].Walls[0].Openings[0].Label, Is.EqualTo("W7-D9"));
        Assert.That(plan.Levels[0].Walls[1].Label, Is.EqualTo("W2"));
        Assert.That(plan.Levels[0].Walls[1].Openings.Select(o => o.Label), Is.EqualTo(new[] { "W2-D1", "W2-N1", "W2-D2" }));
    }

    [Test]
    public void WallHeight_UsesHeightMeters_ElseTheLevelCeiling()
    {
        DetectedWall tall = Wall(0, 0, 6, 0);
        tall.HeightMeters = 3.1;
        DirectBuildPlan plan = DirectBuildPlanner.Plan(Building(tall, Wall(0, 0, 0, 5)));

        Assert.That(plan.Levels[0].Walls[0].HeightMeters, Is.EqualTo(3.1));
        Assert.That(plan.Levels[0].Walls[1].HeightMeters, Is.EqualTo(2.45));
    }

    [Test]
    public void Levels_AreRebasedSoTheLowestSitsAtZero()
    {
        var b = new DetectedBuilding
        {
            Levels =
            {
                new DetectedLevel { Name = "Upper", ElevationMeters = 5.35, Floor2FloorMeters = 2.7, Floor2CeilingMeters = 2.4, Walls = { Wall(0, 0, 4, 0) } },
                new DetectedLevel { Name = "Ground", ElevationMeters = 2.6, Floor2FloorMeters = 2.75, Floor2CeilingMeters = 2.45, Walls = { Wall(0, 0, 4, 0) } },
            },
        };
        DirectBuildPlan plan = DirectBuildPlanner.Plan(b);

        Assert.That(plan.Levels.Select(l => l.Name), Is.EqualTo(new[] { "Ground", "Upper" }), "sorted lowest-first");
        Assert.That(plan.Levels[0].ElevationMeters, Is.EqualTo(0.0).Within(1e-9));
        Assert.That(plan.Levels[1].ElevationMeters, Is.EqualTo(2.75).Within(1e-9));
        Assert.That(plan.RebaseZMeters, Is.EqualTo(2.6).Within(1e-9));
    }

    [Test]
    public void Levels_WithoutElevation_StackFloorToFloorHeights()
    {
        var b = new DetectedBuilding
        {
            Levels =
            {
                new DetectedLevel { Floor2FloorMeters = 3.0, Floor2CeilingMeters = 2.5, Walls = { Wall(0, 0, 4, 0) } },
                new DetectedLevel { Floor2FloorMeters = 2.8, Floor2CeilingMeters = 2.4, Walls = { Wall(0, 0, 4, 0) } },
            },
        };
        DirectBuildPlan plan = DirectBuildPlanner.Plan(b);

        Assert.That(plan.Levels[1].ElevationMeters, Is.EqualTo(3.0).Within(1e-9));
        Assert.That(plan.Levels[1].Name, Is.EqualTo("Level 2"));
    }

    [Test]
    public void InteriorNormal_PointsIntoTheRoom()
    {
        DirectBuildPlan plan = DirectBuildPlanner.Plan(Building(
            Wall(0, 0, 5, 0), Wall(5, 0, 5, 4), Wall(5, 4, 0, 4), Wall(0, 4, 0, 0)));

        var centre = new PlanPoint(2.5, 2.0);
        foreach (PlannedWall w in plan.Levels[0].Walls)
        {
            PlanPoint mid = (w.Start + w.End) * 0.5;
            Assert.That((centre - mid).Dot(w.InteriorNormal), Is.GreaterThan(0), w.Label);
            Assert.That(w.InteriorNormal.Length, Is.EqualTo(1.0).Within(1e-9));
        }
    }

    // The 2026-11-06 Revit check file: the family index and door handing of every door reach the plan,
    // and each opening keeps its measured centre (expected_openings.csv).
    [Test]
    public void RevitCheckFile_DoorFamilyIndexHandingAndCentres_ReachThePlan()
    {
        string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "October31", "1_v2_ai_pknail_v2_doortypes.json");
        DirectBuildPlan plan = DirectBuildPlanner.Plan(AiModelReader.Load(path));

        Assert.That(plan.WallCount, Is.EqualTo(14));
        Assert.That(plan.Skipped, Is.Empty);
        List<PlannedOpening> doors = plan.Levels[0].Walls.SelectMany(w => w.Openings).Where(o => o.Kind == OpeningKind.Door).ToList();
        List<PlannedOpening> windows = plan.Levels[0].Walls.SelectMany(w => w.Openings).Where(o => o.Kind == OpeningKind.Window).ToList();
        Assert.That(doors.Count, Is.EqualTo(7));
        Assert.That(windows.Count, Is.EqualTo(6));
        Assert.That(doors.Count(d => d.FamilyDefIdx == 4), Is.EqualTo(4), "sliding doors");
        Assert.That(doors.Count(d => d.FamilyDefIdx == 0), Is.EqualTo(3), "hinged doors");
        Assert.That(windows.All(w => w.FamilyDefIdx == 0), Is.True);

        PlannedWall wall6 = plan.Levels[0].Walls.Single(w => w.Label == "W7");   // JSON wall index 6
        Assert.That(wall6.Openings.Select(o => o.FamilyDefIdx), Is.EqualTo(new[] { 4, 0, 0 }));
        Assert.That(wall6.Openings.Select(o => o.CentreFromStartMeters), Is.EqualTo(new[] { 2.3255, 3.351, 4.4445 }).Within(1e-6));
        Assert.That(wall6.Openings[1].Hinge, Is.EqualTo(HingeSideKind.Right));
        Assert.That(wall6.Openings[1].Swing, Is.EqualTo(SwingDirectionKind.In));
        Assert.That(wall6.Openings[2].Hinge, Is.EqualTo(HingeSideKind.Left));
        Assert.That(wall6.InteriorNormal.X, Is.EqualTo(1.0).Within(1e-9), "wall 6 runs +Y; the plan centroid is to its east");

        PlannedWall wall9 = plan.Levels[0].Walls.Single(w => w.Label == "W10");  // JSON wall index 9
        Assert.That(wall9.Openings[0].Hinge, Is.EqualTo(HingeSideKind.Right));
        Assert.That(wall9.Openings[0].Swing, Is.EqualTo(SwingDirectionKind.Out));
        Assert.That(wall9.InteriorNormal.Y, Is.EqualTo(-1.0).Within(1e-9), "wall 9 runs +X; the plan centroid is to its south");
    }

    [Test]
    public void InteriorSide_FromTheAi_OverridesTheCentroidGuess()
    {
        // A lone wall running +X: Left = +Y, Right = -Y.
        DetectedWall left = Wall(0, 0, 4, 0); left.InteriorSide = "Left";
        DetectedWall right = Wall(0, 0, 4, 0); right.InteriorSide = "right";
        DetectedWall bogus = Wall(0, 0, 4, 0); bogus.InteriorSide = "Up";

        Assert.That(DirectBuildPlanner.Plan(Building(left)).Levels[0].Walls[0].InteriorNormal.Y, Is.EqualTo(1.0).Within(1e-9));
        Assert.That(DirectBuildPlanner.Plan(Building(right)).Levels[0].Walls[0].InteriorNormal.Y, Is.EqualTo(-1.0).Within(1e-9));
        Assert.That(DirectBuildPlanner.Plan(Building(bogus)).Warnings, Has.Some.Contains("InteriorSide 'Up'"));
    }
}
