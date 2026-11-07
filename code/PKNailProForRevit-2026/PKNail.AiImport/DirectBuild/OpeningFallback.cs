namespace PKNail.AiImport.DirectBuild;

/// <summary>
/// Epic 13 — what Direct Build does when Revit cannot build a door/window type at the measured size or
/// in the host wall (e.g. the imperial default "Door-Passage-Single-Flush" family fails in a 100 mm
/// thick wall at any width: "Profile sketch is empty", "Can't make type …"). The order is:
/// <list type="number">
/// <item>the sized type (normal case);</item>
/// <item>an existing type whose width is within <see cref="MaxWidthDifferenceMeters"/> of the measured
/// width — still a real door/window, close to the right size;</item>
/// <item>a plain rectangular opening of the exact measured size.</item>
/// </list>
/// Every fallback is reported; nothing is silently resized or dropped.
/// </summary>
public static class OpeningFallback
{
    /// <summary>An existing type is only used when it is at most this much wider or narrower (10 cm).</summary>
    public const double MaxWidthDifferenceMeters = 0.10;

    /// <summary>
    /// Index of the width closest to <paramref name="wantedMeters"/> that is within
    /// <paramref name="maxDifferenceMeters"/>, or -1 when none is close enough. Ties go to the first.
    /// Non-positive or non-finite widths (unreadable types) are ignored.
    /// </summary>
    public static int ChooseNearest(IReadOnlyList<double> widthsMeters, double wantedMeters,
                                    double maxDifferenceMeters = MaxWidthDifferenceMeters)
    {
        ArgumentNullException.ThrowIfNull(widthsMeters);
        int best = -1;
        double bestDiff = double.MaxValue;
        for (int i = 0; i < widthsMeters.Count; i++)
        {
            double w = widthsMeters[i];
            if (!(w > 0.0) || double.IsInfinity(w)) continue;
            double diff = Math.Abs(w - wantedMeters);
            if (diff <= maxDifferenceMeters + 1e-9 && diff < bestDiff) { best = i; bestDiff = diff; }
        }
        return best;
    }
}
