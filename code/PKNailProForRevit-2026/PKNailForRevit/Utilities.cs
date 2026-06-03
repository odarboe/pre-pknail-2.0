using System;
using System.Collections.Generic;
using System.Text;
using Autodesk.Revit.DB;

using G2D = PKNailForRevit.Geometry2D;

namespace PKNailForRevit
{
    internal static class Utilities
    {
        internal static G2D.Point To2D(XYZ threeDPt) { return new G2D.Point(threeDPt.X, threeDPt.Y); }

        internal static XYZ To3D(G2D.Point twoDPt, double z) { return new XYZ(twoDPt.X, twoDPt.Y, z); }

        internal static G2D.Vector To2D(Location loc)
        {
            Curve locCurve = ((LocationCurve)loc).Curve;
            XYZ pt0 = locCurve.GetEndPoint(0);
            XYZ pt1 = locCurve.GetEndPoint(1);
            return new G2D.Vector(pt0.X, pt0.Y, pt1.X, pt1.Y);
        }

        internal static XYZ To3DUnitVector(G2D.Radians az)
        {
            G2D.Vector twoDUnit = new G2D.Vector(new G2D.Point(), az, 1.0);
            return new XYZ(twoDUnit.End.X, twoDUnit.End.Y, 0.0);
        }

        internal static Distance Length(Location loc)
        { return new Distance(To2D(loc).Length, Distance.Units.RevitInternal); }

        internal static G2D.Radians Azimuth(Location loc)
        { return To2D(loc).Azimuth; }

        internal static Line ToRevitLine(G2D.Vector twoDline)
        {
            XYZ pt0 = To3D(twoDline.Start, 0.0);
            XYZ pt1 = To3D(twoDline.End, 0.0);
            return Line.CreateBound(pt0, pt1);
        }

        internal static ElementId IdFromElement(Element el)
        { return (el == null) ? null : el.Id; }

        internal static SurveyOrientation SurveyOrientationFromFace(Wall w, PlanarFace pf)
        {
            XYZ fnormal = pf.FaceNormal;
            if (w.Flipped) { fnormal = fnormal.Multiply(-1.0); }
            if (fnormal.IsAlmostEqualTo(w.Orientation))
            { return new SurveyOrientation(SurveyOrientation.Flipped); }
            else
            { return new SurveyOrientation(SurveyOrientation.NotFlipped); }
        }

        internal static int EndPtIdx(SurveyOrientation so, SurveySide side)
        {
            if (so == SurveyOrientation.NotFlipped && side == SurveySide.Left
                || so == SurveyOrientation.Flipped && side == SurveySide.Right)
            { return 0; }
            else
            { return 1; }
        }

        internal static G2D.Point FarthestPointInDirection(G2D.Radians direction, List<G2D.Point> points)
        {
            if (points.Count == 0) { return null; }
            if (points.Count == 1) { return points[0]; }

            G2D.Vector unitV = new G2D.Vector(new G2D.Point(), direction, 1.0);
            List<G2D.Point> projectedPoints = new List<G2D.Point>();
            foreach (G2D.Point p in points) { projectedPoints.Add(unitV.Project(p)); }
            Dictionary<G2D.Vector, G2D.Point> spanVs = new Dictionary<G2D.Vector, G2D.Point>();
            for (int i1 = 0; i1 < projectedPoints.Count; i1++)
            {
                for (int i2 = 0; i2 < projectedPoints.Count; i2++)
                {
                    if (   projectedPoints[i1] == projectedPoints[i2]
                        || projectedPoints[i1].DistanceTo(projectedPoints[i2]).IsZero)
                    { continue; }
                    G2D.Vector v = new G2D.Vector(projectedPoints[i1], projectedPoints[i2]);
                    if (v.Azimuth == unitV.Azimuth) { spanVs.Add(v, points[i2]); }
                }
            }

            G2D.Point retval = null;
            if (spanVs.Count > 0)
            {
                double maxLen = 0.0;
                foreach (G2D.Vector v in spanVs.Keys)
                {
                    double len = v.Length;
                    if (len > maxLen) { retval = spanVs[v]; maxLen = len; }
                }
            }
            else { retval = points[0]; }  // If spanVs.Count == 0, that means all points are coincident

            return retval;
        }

        internal static bool IsPointOnLineSegment(G2D.Vector segment, G2D.Point point)
        {
            Distance segmentLen = new Distance(segment.Length, Distance.Units.PKInternal);
            G2D.Point pointOnSegment = segment.Project(point);
            return (   pointOnSegment.DistanceTo(segment.Start) <= segmentLen
                    && pointOnSegment.DistanceTo(segment.End) <= segmentLen);
        }
    }
}
