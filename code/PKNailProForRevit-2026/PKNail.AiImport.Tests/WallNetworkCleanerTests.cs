using NUnit.Framework;

using PKNail.AiImport.DirectBuild;

namespace PKNail.AiImport.Tests;

/// <summary>Epic 13 — the Direct Build wall tidy-up: square up, close small gaps, flag overlaps, never delete.</summary>
[TestFixture]
public class WallNetworkCleanerTests
{
    private static CleanWall W(string label, double sx, double sy, double ex, double ey, double t = 0.115) =>
        new(label, new PlanPoint(sx, sy), new PlanPoint(ex, ey), t);

    private static (List<string> Adj, List<string> Warn) Run(IList<CleanWall> walls, CleanupOptions? o = null)
    {
        var adj = new List<string>();
        var warn = new List<string>();
        WallNetworkCleaner.Clean(walls, o ?? CleanupOptions.Default, adj, warn);
        return (adj, warn);
    }

    private static void AssertAt(PlanPoint p, double x, double y) =>
        Assert.That(p.DistanceTo(new PlanPoint(x, y)), Is.LessThan(1e-9), $"expected ({x}, {y}) but was ({p.X}, {p.Y})");

    [Test]
    public void SquaresUp_NearHorizontalWall_KeepsItInPlace()
    {
        var walls = new List<CleanWall> { W("W1", 0, 0, 5, 0.05) };   // ~0.57° off the X axis
        var (adj, _) = Run(walls);

        AssertAt(walls[0].Start, 0, 0.025);
        AssertAt(walls[0].End, 5, 0.025);
        Assert.That(adj, Has.Count.EqualTo(1));
        Assert.That(adj[0], Does.Contain("W1").And.Contain("squared up"));
    }

    [Test]
    public void LeavesDeliberatelyAngledWallAlone()
    {
        var walls = new List<CleanWall> { W("W1", 0, 0, 3, 3) };     // 45°
        var (adj, _) = Run(walls);

        AssertAt(walls[0].Start, 0, 0);
        AssertAt(walls[0].End, 3, 3);
        Assert.That(adj, Is.Empty);
    }

    [Test]
    public void ExtendsShortEnd_OntoTJunction()
    {
        var walls = new List<CleanWall> { W("W1", 0, 0, 10, 0), W("W2", 5, 0.06, 5, 4) };
        var (adj, _) = Run(walls);

        AssertAt(walls[1].Start, 5, 0);       // now touches W1's centerline → Revit T-joins it
        AssertAt(walls[0].Start, 0, 0);       // the host wall is untouched
        AssertAt(walls[0].End, 10, 0);
        Assert.That(adj.Single(), Does.Contain("W2 start").And.Contain("extended 60 mm").And.Contain("W1"));
    }

    [Test]
    public void TrimsSmallOvershoot_AtCorner()
    {
        var walls = new List<CleanWall> { W("W1", 0, 0, 10, 0), W("W2", 10, -0.04, 10, 5) };
        var (adj, _) = Run(walls);

        AssertAt(walls[1].Start, 10, 0);
        Assert.That(adj.Single(), Does.Contain("trimmed 40 mm"));
    }

    [Test]
    public void ClosesLCornerGap_OnBothWalls()
    {
        var walls = new List<CleanWall> { W("W1", 0, 0, 9.95, 0), W("W2", 10, 0.05, 10, 5) };
        Run(walls);

        AssertAt(walls[0].End, 10, 0);
        AssertAt(walls[1].Start, 10, 0);
    }

    [Test]
    public void LeavesGapsLargerThanTheTolerance()
    {
        var walls = new List<CleanWall> { W("W1", 0, 0, 10, 0), W("W2", 5, 0.5, 5, 4) };
        var (adj, _) = Run(walls);

        AssertAt(walls[1].Start, 5, 0.5);
        Assert.That(adj, Is.Empty);
    }

    [Test]
    public void ReportsOverlappingWalls_ButKeepsBoth()
    {
        var walls = new List<CleanWall> { W("W1", 0, 0, 6, 0), W("W2", 2, 0.03, 8, 0.03) };
        var (_, warn) = Run(walls);

        Assert.That(walls, Has.Count.EqualTo(2), "the cleaner never deletes a wall");
        Assert.That(warn.Single(), Does.Contain("W1 and W2 overlap for 4 m").And.Contain("Both were drawn"));
    }

    [Test]
    public void Disabled_ChangesNothing_ButStillReportsOverlaps()
    {
        var walls = new List<CleanWall>
        {
            W("W1", 0, 0, 6, 0.05), W("W2", 3, 0.06, 3, 4), W("W3", 1, 0.02, 5, 0.04),
        };
        var (adj, warn) = Run(walls, new CleanupOptions { Enabled = false });

        AssertAt(walls[0].End, 6, 0.05);
        AssertAt(walls[1].Start, 3, 0.06);
        Assert.That(adj, Is.Empty);
        Assert.That(warn, Has.Count.EqualTo(1));
    }

    [Test]
    public void EndsThatAlreadyMeet_AreNotMoved()
    {
        var walls = new List<CleanWall> { W("W1", 0, 0, 10, 0), W("W2", 10, 0, 10, 5), W("W3", 4, 0, 4, 5) };
        var (adj, _) = Run(walls);

        Assert.That(adj, Is.Empty);
    }
}
