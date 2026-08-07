using PKNail.AiImport.Model;

namespace PKNail.AiImport.Mapping;

/// <summary>
/// Derives the PKNAIL relative wall chain (length + interior corner angle) from v2 absolute wall
/// endpoints, so the v2 contract can drive the same proven survey engine as v1 — but with correct,
/// unambiguous geometry. The engine convention is interior angle: <c>π/2</c> = a 90° inside corner,
/// <c>π</c> = straight, <c>3π/2</c> = concave 90°. This was verified end-to-end in Revit 2026 with the
/// known-good demo (a 4-wall room with all-<c>π/2</c> corners).
/// </summary>
internal static class WallGeometry
{
    private const double TwoPi = System.Math.PI * 2.0;

    public static double Length(DetectedPoint2D a, DetectedPoint2D b)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y;
        return System.Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// How far this wall's bearing deviates from the nearest multiple of 90° in the plan frame, in
    /// radians and always non-negative (range <c>[0, π/4]</c>). <c>0</c> means perfectly axis-aligned.
    /// <para>
    /// The v2 absolute path places <c>Start</c>/<c>End</c> verbatim — the mapper's orthogonal snapping
    /// only ever applied to the v1 <c>CornerAngleRad</c> chain — so near-square drift from the AI engine
    /// survives into Revit uncorrected. <see cref="Model.AiModelValidator"/> uses this to warn about it.
    /// </para>
    /// </summary>
    public static double OrthogonalDeviation(DetectedPoint2D a, DetectedPoint2D b)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y;
        if (dx == 0.0 && dy == 0.0) { return 0.0; }

        const double Quarter = System.Math.PI / 2.0;
        double bearing = System.Math.Atan2(dy, dx);
        return System.Math.Abs(bearing - (System.Math.Round(bearing / Quarter) * Quarter));
    }

    /// <summary>
    /// Winding sign of the wall loop using the start points: <c>+1</c> for CCW, <c>-1</c> for CW.
    /// Used to make the interior-angle computation independent of the order the AI engine walked the room.
    /// </summary>
    public static double WindingSign(IReadOnlyList<DetectedWall> walls)
    {
        double area2 = 0.0;
        for (int i = 0; i < walls.Count; i++)
        {
            DetectedPoint2D a = walls[i].StartMeters!;
            DetectedPoint2D b = walls[(i + 1) % walls.Count].StartMeters!;
            area2 += (a.X * b.Y) - (b.X * a.Y);
        }
        return area2 >= 0.0 ? 1.0 : -1.0;
    }

    /// <summary>
    /// Interior corner angle (radians, normalized to [0, 2π)) at the vertex between the previous wall and
    /// the current wall, given absolute endpoints and the loop's <paramref name="windingSign"/>.
    /// </summary>
    public static double InteriorCornerAngle(DetectedWall prev, DetectedWall curr, double windingSign)
    {
        double inX = prev.EndMeters!.X - prev.StartMeters!.X;
        double inY = prev.EndMeters!.Y - prev.StartMeters!.Y;
        double outX = curr.EndMeters!.X - curr.StartMeters!.X;
        double outY = curr.EndMeters!.Y - curr.StartMeters!.Y;

        // Signed turn from the incoming to the outgoing direction (CCW positive).
        double turn = System.Math.Atan2(inX * outY - inY * outX, inX * outX + inY * outY);

        // Interior angle on the consistent inside of the loop. CCW: π−turn; CW: π+turn.
        double interior = System.Math.PI - (windingSign * turn);

        interior %= TwoPi;
        if (interior < 0) interior += TwoPi;
        return interior;
    }
}
