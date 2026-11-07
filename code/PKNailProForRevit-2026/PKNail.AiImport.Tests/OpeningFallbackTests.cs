using NUnit.Framework;

using PKNail.AiImport.DirectBuild;

namespace PKNail.AiImport.Tests;

/// <summary>Epic 13 — choosing an existing door/window type when Revit cannot build the measured size.</summary>
[TestFixture]
public class OpeningFallbackTests
{
    [Test]
    public void PicksTheClosestWidth_WithinTenCentimetres()
    {
        double[] widths = { 0.762, 0.813, 0.864, 0.610 };
        Assert.That(OpeningFallback.ChooseNearest(widths, 0.628), Is.EqualTo(3));   // 610 mm, 18 mm off
        Assert.That(OpeningFallback.ChooseNearest(widths, 0.800), Is.EqualTo(1));   // 813 mm
    }

    [Test]
    public void ReturnsMinusOne_WhenNothingIsCloseEnough()
    {
        // The 2026-10-12 failure: a 571 mm door, nearest stock type 762 mm (191 mm off) ⇒ plain opening instead.
        double[] widths = { 0.762, 0.813, 0.864, 0.914 };
        Assert.That(OpeningFallback.ChooseNearest(widths, 0.571), Is.EqualTo(-1));
    }

    [Test]
    public void IgnoresUnreadableWidths_AndBreaksTiesToTheFirst()
    {
        double[] widths = { 0.0, double.NaN, 0.70, 0.80 };
        Assert.That(OpeningFallback.ChooseNearest(widths, 0.75), Is.EqualTo(2));
        Assert.That(OpeningFallback.ChooseNearest(Array.Empty<double>(), 0.75), Is.EqualTo(-1));
    }

    [Test]
    public void ExactlyAtTheTolerance_IsAccepted()
    {
        Assert.That(OpeningFallback.ChooseNearest(new[] { 0.90 }, 0.80), Is.EqualTo(0));
    }
}
