using NUnit.Framework;

using PKNail.AiImport.DirectBuild;

namespace PKNail.AiImport.Tests;

/// <summary>
/// Epic 12c on the Direct Build path — which PKNAIL survey family an opening is built from. The lists
/// below are the PKNAIL template's families in <c>PKNail Family Order</c>, as confirmed in the Capture
/// dialog during the 2026-11-06 Revit check.
/// </summary>
[TestFixture]
public class OpeningFamilyResolverTests
{
    private static readonly SurveyFamilyInfo[] Doors =
    {
        new("Single", FamilyLateralKind.Hinged),
        new("Double", FamilyLateralKind.None),
        new("Opening", FamilyLateralKind.None),
        new("Sliding Glass", FamilyLateralKind.Sliding),
        new("Sliding", FamilyLateralKind.Sliding),
        new("Pocket", FamilyLateralKind.Sliding),
    };

    private static readonly SurveyFamilyInfo[] Windows =
    {
        new("Fixed", FamilyLateralKind.None),
        new("Double Hung", FamilyLateralKind.None),
    };

    private static string Pick(SurveyFamilyInfo[] families, string? openingType, int familyDefIdx, out string? note)
    {
        int i = OpeningFamilyResolver.Resolve(families, openingType, familyDefIdx, out note);
        return i < 0 ? "(none)" : families[i].Name;
    }

    [TestCase(0, "Single")]
    [TestCase(4, "Sliding")]
    [TestCase(5, "Pocket")]
    public void FamilyDefIdx_IsTheCaptureListIndex(int familyDefIdx, string expected)
    {
        Assert.That(Pick(Doors, null, familyDefIdx, out string? note), Is.EqualTo(expected));
        Assert.That(note, Is.Null);
    }

    [TestCase(6)]
    [TestCase(-1)]
    public void FamilyDefIdx_OutsideTheList_UsesTheFirstFamilyAndSaysSo(int familyDefIdx)
    {
        Assert.That(Pick(Doors, null, familyDefIdx, out string? note), Is.EqualTo("Single"));
        Assert.That(note, Does.Contain($"FamilyDefIdx {familyDefIdx}").And.Contain("6 PKNAIL families").And.Contain("\"Single\""));
    }

    [TestCase("Sliding", "Sliding", Description = "exact name wins over the first sliding family (Sliding Glass)")]
    [TestCase("sliding glass", "Sliding Glass")]
    [TestCase("Pocket", "Pocket")]
    [TestCase("Hinged", "Single", Description = "alias")]
    [TestCase("DoubleHinged", "Double", Description = "alias")]
    public void OpeningType_ChoosesTheDoorFamily_AndOverridesFamilyDefIdx(string openingType, string expected)
    {
        Assert.That(Pick(Doors, openingType, 2, out string? note), Is.EqualTo(expected));
        Assert.That(note, Is.Null);
    }

    [TestCase("DoubleHung", "Double Hung")]
    [TestCase("double-hung", "Double Hung")]
    [TestCase("Fixed", "Fixed")]
    public void OpeningType_IgnoresCaseSpacesAndPunctuation(string openingType, string expected)
    {
        Assert.That(Pick(Windows, openingType, 0, out string? note), Is.EqualTo(expected));
        Assert.That(note, Is.Null);
    }

    [Test]
    public void OpeningType_WithoutANamedFamily_FallsBackToTheHandingKind()
    {
        SurveyFamilyInfo[] renamed =
        {
            new("Passage", FamilyLateralKind.None),
            new("Swing Door", FamilyLateralKind.Hinged),
            new("Barn Door", FamilyLateralKind.Sliding),
        };
        Assert.That(Pick(renamed, "Hinged", 0, out _), Is.EqualTo("Swing Door"));
        Assert.That(Pick(renamed, "Sliding", 0, out _), Is.EqualTo("Barn Door"));
    }

    [Test]
    public void OpeningType_Unknown_FallsBackToFamilyDefIdxAndSaysSo()
    {
        Assert.That(Pick(Doors, "Bifold", 4, out string? note), Is.EqualTo("Sliding"));
        Assert.That(note, Does.Contain("OpeningType \"Bifold\"").And.Contain("used \"Sliding\""));

        Assert.That(Pick(Windows, "Casement", 9, out note), Is.EqualTo("Fixed"));
        Assert.That(note, Does.Contain("OpeningType \"Casement\"").And.Contain("FamilyDefIdx 9"));
    }

    [Test]
    public void NoSurveyFamilies_ReturnsMinusOne()
    {
        Assert.That(OpeningFamilyResolver.Resolve(Array.Empty<SurveyFamilyInfo>(), "Sliding", 4, out string? note), Is.EqualTo(-1));
        Assert.That(note, Is.Null);
    }
}
