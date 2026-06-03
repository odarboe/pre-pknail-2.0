using System;
using System.Collections.Generic;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.ApplicationServices;

using G2D = PKNailForRevit.Geometry2D;

namespace PKNailForRevit
{
    internal static class RevitSurveyWall
    {
        internal enum LocationLineType { Exterior = 2, Interior = 3 }

        internal static Distance Width(Wall w)
        { return new Distance(w.Width, Distance.Units.RevitInternal); }

        internal static Distance Height(Wall w)
        { return new Distance(w.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM).AsDouble(), Distance.Units.RevitInternal); }

        internal static Distance BaseOffset(Wall w)
        { return new Distance(w.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET).AsDouble(), Distance.Units.RevitInternal); }

        internal static bool IsUnknownWidth(WallType wt)
        { return (PKNailParameter.WallTypeUnknownWidth.GetInteger(wt) == 1); }

        internal static bool IsUnknownWidth(Wall w)
        { return IsUnknownWidth(w.WallType); }

        internal static LocationLineType GetLocLineType(Wall w)
        { return (LocationLineType)(w.get_Parameter(BuiltInParameter.WALL_KEY_REF_PARAM).AsInteger()); }

        internal static void SetLocLineType(Wall w, LocationLineType t)
        { w.get_Parameter(BuiltInParameter.WALL_KEY_REF_PARAM).Set((int)t); }

        internal static Level BaseLevel(Wall w)
        { return w.Document.GetElement(w.LevelId) as Level; }

        internal static Level ConnectedLevel(Document doc, Wall w)
        {
            Parameter p = w.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE);
            ElementId elId = p.AsElementId();
            return doc.GetElement(elId) as Level;
        }

        internal static Distance BaseElevation(Wall w)
        { return (new Distance(BaseLevel(w).Elevation, Distance.Units.RevitInternal)) + BaseOffset(w); }

        internal static Distance TopElevation(Wall w)
        { return BaseElevation(w) + Height(w); }

        internal static bool IsOnLevel(Wall w, Level level)
        {
            Distance levelElevation = new Distance(level.Elevation, Distance.Units.RevitInternal);
            return (   w.LevelId == level.Id
                    || (BaseElevation(w) <= levelElevation && TopElevation(w) > levelElevation));
        }

        internal static XYZ Orientation(Wall w, SurveyOrientation so)
        {
            XYZ retval = new XYZ(w.Orientation.X, w.Orientation.Y, w.Orientation.Z);
            // Compensate for flipped wall
            if (w.Flipped) { retval = retval.Multiply(-1.0); }
            double soMultiplier = (so == SurveyOrientation.NotFlipped) ? 1.0 : -1.0;
            retval = retval.Multiply(soMultiplier);
            return retval;
        }

        internal static G2D.Radians Azimuth(Wall w, SurveyOrientation so)
        {
            XYZ orientation = Orientation(w, so);
            G2D.Vector oVec = new G2D.Vector(0.0, 0.0, orientation.X, orientation.Y);
            G2D.Radians az = oVec.Azimuth + G2D.Radians.ThreePIOverTwo;   // 270 degrees
            return az;
        }

        internal static int EndPtIndex(SurveySide side, SurveyOrientation so)
        {
            int retval;

            if (so == SurveyOrientation.NotFlipped)
            { retval = (side == SurveySide.Left) ? 0 : 1; }
            else
            { retval = (side == SurveySide.Left) ? 1 : 0; }

            return retval;
        }

        internal static G2D.Vector LocationLine(Wall w)
        { return Utilities.To2D(w.Location); }

        internal static G2D.Vector Centerline(Wall w)
        {
            G2D.Vector frontEdge = WallEdge(w, SurveyOrientation.NotFlipped, true);
            G2D.Vector rearEdge = WallEdge(w, SurveyOrientation.NotFlipped, false);
            G2D.Vector leftSideEdge = new G2D.Vector(frontEdge.Start, rearEdge.Start);
            G2D.Vector rightSideEdge = new G2D.Vector(frontEdge.End, rearEdge.End);

            return new G2D.Vector(leftSideEdge.MidPoint, rightSideEdge.MidPoint);
        }

        internal static Distance Length(Wall w)
        { return new Distance(((LocationCurve)w.Location).Curve.ApproximateLength, Distance.Units.RevitInternal); }

        internal static List<Wall> JoinedWalls(Wall w, Level lvl, int endPtIdx)
        {
            List<Wall> retval = new List<Wall>();
            if (endPtIdx == -1)  { return retval; }

            ElementArray allJoined = (w.Location as LocationCurve).get_ElementsAtJoin(endPtIdx);
            foreach (Element el in allJoined)
            {
                if (el.Id.Equals(w.Id)) { continue; }  // Wall is "joined to itself"
                Wall jw = el as Wall;
                if (jw != null)
                {
                    // Only add the wall to the return list if it is not already there
                    // (yes, I know I should use EqualityComparer<Wall>, but...)
                    bool isInList = false;
                    foreach (Wall existing in retval)
                    { if (existing.Id.Equals(jw.Id)) { isInList = true; break; } }
                    if (!isInList && IsOnLevel(jw, lvl)) { retval.Add(jw); }

                    // Sometimes Revit does not return all the walls at the join point if
                    // a wall was created with a slight offset from the join point of two
                    // existing walls.  So we want to get all the walls that are associated
                    // with the join point, whether Revit thinks they are joined or not.

                    // Find the join end point of the joined wall
                    int jwJoinPtIdx = JoinPtIdx(jw, w, false);
                    if (jwJoinPtIdx != -1)
                    {
                        ElementArray jwJoined = (jw.Location as LocationCurve).get_ElementsAtJoin(jwJoinPtIdx);
                        foreach (Element jwJoinedEl in jwJoined)
                        {
                            if (jwJoinedEl.Id.Equals(w.Id)) { continue; }
                            Wall jwJoinedW = jwJoinedEl as Wall;
                            if (jwJoinedW != null)
                            {
                                // Only add the wall to the return list if it is not already there
                                // (yes, I know I should use EqualityComparer<Wall>, but...)
                                isInList = false;
                                foreach (Wall existing in retval)
                                { if (existing.Id.Equals(jwJoinedW.Id)) { isInList = true; break; } }
                                if (!isInList && IsOnLevel(jwJoinedW, lvl)) { retval.Add(jwJoinedW); }
                            }
                        }
                    }
                }
            }

            return retval;
        }

        internal static int JoinPtIdx(Element el, Element refEl, bool isForce)
        {
            int retval = -1;

            LocationCurve elLocCurve = el.Location as LocationCurve;
            for (int elEndPtIdx = 0; elEndPtIdx < 2; elEndPtIdx++)
            {
                ElementArray elJoined = elLocCurve.get_ElementsAtJoin(elEndPtIdx);
                foreach (Element jEl in elJoined)
                {
                    if (jEl.Id.Equals(refEl.Id)) { retval = elEndPtIdx; break; }
                }
            }

            if (retval == -1)
            {
                Wall elW = el as Wall;
                Wall refElW = refEl as Wall;
                if (elW != null && refElW != null)
                {
                    G2D.Vector elWLocLine = RevitSurveyWall.LocationLine(elW);
                    G2D.Vector refElWLocLine = RevitSurveyWall.LocationLine(refElW);
                    G2D.Point intersection = G2D.Vector.Intersection(elWLocLine, refElWLocLine);
                    if (intersection != null)
                    {
                        if (elWLocLine.Start.DistanceTo(intersection) < elWLocLine.End.DistanceTo(intersection))
                        {
                            if (isForce || elWLocLine.End.DistanceTo(intersection) >= new Distance(elWLocLine.Length - (refElW.Width / 2.0), Distance.Units.RevitInternal))
                            { retval = 0; }
                        }
                        else
                        {
                            if (isForce || elWLocLine.Start.DistanceTo(intersection) >= new Distance(elWLocLine.Length - (refElW.Width / 2.0), Distance.Units.RevitInternal))
                            { retval = 1; }
                        }
                    }
                }
            }

            return retval;
        }

        internal static G2D.Point WallIntersectCorner(Wall refWall, Wall joinedWall, SurveySide refWallSide,
                                                      SurveyOrientation refWallSO, SurveyOrientation joinedWallSO, bool isRefWallFrontEdge, bool isJoinedWallFrontEdge)
        {
            // The survey corner is the intersection of the walls' front edges
            G2D.Vector sWallEdge = WallEdge(refWall, refWallSO, isRefWallFrontEdge);
            G2D.Vector jWallEdge = WallEdge(joinedWall, joinedWallSO, isJoinedWallFrontEdge);

            return (G2D.Vector.Intersection(sWallEdge, jWallEdge));
        }

        internal static List<FamilyInstance> GetHostedInstances(Document doc, Wall w)
        {
            List<FamilyInstance> retval = new List<FamilyInstance>();

            ParameterValueProvider pvp = new ParameterValueProvider(new ElementId(BuiltInParameter.HOST_ID_PARAM));
            FilterElementIdRule rule = new FilterElementIdRule(pvp, new FilterNumericEquals(), w.Id);
            ElementParameterFilter filt = new ElementParameterFilter(rule);
            FilteredElementCollector coll = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).WherePasses(filt);
            foreach (Element el in coll.ToElements())
            { retval.Add((FamilyInstance)el); }

            return retval;
        }

        /// <summary>
        /// Returns either a front or rear wall edge (based on the orientation parameter),
        /// depending on the value of isFrontEdge.
        /// </summary>
        /// <param name="wall"></param>
        /// <param name="orientation"></param>
        /// <param name="isFrontEdge"></param>
        /// <returns>A vector representing an arbitrary front or rear wall edge.</returns>
        internal static G2D.Vector WallEdge(Wall wall, SurveyOrientation so, bool isFrontEdge)
        {
            G2D.Vector retval = null;

            XYZ orientation = RevitSurveyWall.Orientation(wall, so);
            List<G2D.Point> corners = new List<G2D.Point>();

            Options opts = new Options();
            opts.DetailLevel = ViewDetailLevel.Undefined;
            IEnumerator<GeometryObject> geoEnum = wall.get_Geometry(opts).GetEnumerator();
            while (geoEnum.MoveNext())
            {
                Solid solid = geoEnum.Current as Solid;
                if (solid == null) { continue; }

                bool isFoundPlanarFace = false;
                foreach (Face f in solid.Faces)
                {
                    PlanarFace pf = f as PlanarFace;
                    if (pf == null)
                    { continue; }

                    XYZ faceNormal = pf.FaceNormal;
                    if (isFrontEdge) { faceNormal = faceNormal.Negate(); }
                    if (faceNormal.IsAlmostEqualTo(orientation))
                    {
                        isFoundPlanarFace = true;
                        EdgeArrayArray eaa = pf.EdgeLoops;
                        foreach (EdgeArray ea in eaa)
                        {
                            foreach (Edge edge in ea)
                            {
                                List<XYZ> endpts = new List<XYZ>(edge.Tessellate());
                                if (Math.Abs(endpts[0].Z - endpts[1].Z) < 0.00001)
                                {
                                    corners.Add(Utilities.To2D(endpts[0]));
                                    corners.Add(Utilities.To2D(endpts[1]));
                                }
                            }
                        }
                    }   
                }
                if (!isFoundPlanarFace)
                { throw new HardException("Cannot get edge of non-planar wall face"); }
            }

            // Find the edges that are furthest away from the center of the wall
            // (sometimes, when a wall is joined to 2 or more walls of different
            // widths, you get "hidden" edges inside the join of the walls; this
            // edge must get filtered out)
            XYZ towardEdgeDir = (isFrontEdge) ? orientation.Negate() : orientation;
            G2D.Radians towardEdgeAz = (new G2D.Vector(new G2D.Point(), Utilities.To2D(towardEdgeDir))).Azimuth;
            G2D.Point farthestEdgePt = Utilities.FarthestPointInDirection(towardEdgeAz, corners);
            G2D.Vector farthestEdgeV = new G2D.Vector(farthestEdgePt, towardEdgeAz + G2D.Radians.PIOverTwo, 1.0);
            List<G2D.Point> ptsToFilter = new List<G2D.Point>();
            foreach (G2D.Point pt in corners)
            { if (farthestEdgeV.Project(pt).DistanceTo(pt) > Distance.Zero) { ptsToFilter.Add(pt); } }
            foreach (G2D.Point pt in ptsToFilter)
            { corners.Remove(pt); }

            // Find the longest edge by finding the two points whose distance between is largest
            Distance max = Distance.Zero;
            G2D.Point p1Max = null; G2D.Point p2Max = null;
            foreach (G2D.Point p1 in corners)
            {
                foreach (G2D.Point p2 in corners)
                {
                    if (p1.Equals(p2)) { continue; }
                    if (p1.DistanceTo(p2) > max)
                    { p1Max = p1; p2Max = p2; max = p1.DistanceTo(p2); }
                }
            }

            // Orient the returned vector to be the same as the wall's azimuth
            retval = new G2D.Vector(p1Max, p2Max);
            if (retval.Azimuth == RevitSurveyWall.Azimuth(wall, so))
            { return retval; }
            else
            { return new G2D.Vector(p2Max, p1Max); }
        }

        internal static G2D.Vector EdgeFromFace(PlanarFace face)
        {
            G2D.Vector retval = null;

            EdgeArrayArray eaa = face.EdgeLoops;
            foreach (EdgeArray ea in eaa)
            {
                foreach (Edge edge in ea)
                {
                    List<XYZ> endpts = new List<XYZ>(edge.Tessellate());
                    if (endpts.Count != 2) { continue; }
                    if (Math.Abs(endpts[0].Z - endpts[1].Z) < 0.000001)
                    {
                        retval = new G2D.Vector(endpts[0].X, endpts[0].Y, endpts[1].X, endpts[1].Y);
                        break;
                    }
                }
            }

            return retval;
        }
    }
}
