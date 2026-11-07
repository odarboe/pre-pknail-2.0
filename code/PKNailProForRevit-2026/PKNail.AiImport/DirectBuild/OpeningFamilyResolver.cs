namespace PKNail.AiImport.DirectBuild;

/// <summary>How a PKNAIL survey family is handed (its <c>PKNail Family Lateral Orientation</c>).</summary>
public enum FamilyLateralKind { None, Hinged, Sliding }

/// <summary>One entry of the project's PKNAIL survey-family list for doors or for windows.</summary>
/// <param name="Name">The PKNAIL family name, e.g. <c>"Single"</c>, <c>"Sliding"</c>, <c>"Double Hung"</c>.</param>
public readonly record struct SurveyFamilyInfo(string Name, FamilyLateralKind Lateral);

/// <summary>
/// Epic 12c on the Direct Build path — Revit-free choice of the PKNAIL survey family for one opening.
/// The list is the project's survey families in <c>PKNail Family Order</c>, i.e. exactly the list the 1.0
/// Capture dialog shows, so index <c>n</c> here is the family a surveyor would pick as entry <c>n</c>.
/// <list type="number">
/// <item><c>OpeningType</c> (contract v2.1), when it names a family: the same name ignoring case and
/// spaces (<c>"DoubleHung"</c> = "Double Hung"), then a built-in alias (<c>Hinged</c> = "Single",
/// <c>DoubleHinged</c> = "Double"), then the first family that is hinged / sliding.</item>
/// <item>else <c>FamilyDefIdx</c> (contract v1/v2.0; 0 when absent), the 1.0 behaviour (Q3).</item>
/// <item>an index outside the list falls back to the first family.</item>
/// </list>
/// Every fallback is described in the note; a clean match has no note.
/// </summary>
public static class OpeningFamilyResolver
{
    private static readonly (string Type, string Family)[] s_aliases =
    {
        ("hinged", "single"),
        ("doublehinged", "double"),
    };

    /// <summary>
    /// Index into <paramref name="families"/> of the family to build from, or -1 when the list is empty
    /// (the project has no PKNAIL survey families of this kind).
    /// </summary>
    public static int Resolve(IReadOnlyList<SurveyFamilyInfo> families, string? openingType, int familyDefIdx,
                              out string? note)
    {
        ArgumentNullException.ThrowIfNull(families);
        note = null;
        if (families.Count == 0) return -1;

        string? unmatchedType = null;
        if (!string.IsNullOrWhiteSpace(openingType))
        {
            int byType = MatchType(families, Normalize(openingType));
            if (byType >= 0) return byType;
            unmatchedType = openingType.Trim();
        }

        int index = familyDefIdx;
        if (index < 0 || index >= families.Count)
        {
            note = $"FamilyDefIdx {familyDefIdx} is outside this project's {families.Count} PKNAIL families; "
                   + $"used the first one, \"{families[0].Name}\"";
            index = 0;
        }
        if (unmatchedType != null)
        {
            string used = $"OpeningType \"{unmatchedType}\" matches no PKNAIL family in this project; used \"{families[index].Name}\"";
            note = note == null ? used : used + " (" + note + ")";
        }
        return index;
    }

    private static int MatchType(IReadOnlyList<SurveyFamilyInfo> families, string type)
    {
        int byName = IndexOfName(families, type);
        if (byName >= 0) return byName;

        foreach ((string aliasType, string aliasFamily) in s_aliases)
        {
            if (type != aliasType) continue;
            int byAlias = IndexOfName(families, aliasFamily);
            if (byAlias >= 0) return byAlias;
        }

        FamilyLateralKind wanted = type switch
        {
            "hinged" => FamilyLateralKind.Hinged,
            "sliding" => FamilyLateralKind.Sliding,
            _ => FamilyLateralKind.None,
        };
        if (wanted != FamilyLateralKind.None)
        {
            for (int i = 0; i < families.Count; i++)
                if (families[i].Lateral == wanted) return i;
        }
        return -1;
    }

    private static int IndexOfName(IReadOnlyList<SurveyFamilyInfo> families, string normalizedName)
    {
        for (int i = 0; i < families.Count; i++)
            if (Normalize(families[i].Name) == normalizedName) return i;
        return -1;
    }

    // Letters and digits only, lower case: "Double Hung", "DoubleHung" and "double-hung" compare equal.
    private static string Normalize(string? s) =>
        new string((s ?? string.Empty).Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}
