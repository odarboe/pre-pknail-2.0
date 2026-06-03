using System;
using System.Collections.Generic;
using System.Text;

namespace PKNailForRevit.Geometry2D
{
    public class Point
    {
        protected double m_x;
        protected double m_y;

        public Point() { m_x = 0.0; m_y = 0.0; }
        public Point(double x, double y) { m_x = x; m_y = y; }
        public Point(Point from) { m_x = from.m_x; m_y = from.m_y; }

        public double X { get { return m_x; } set { m_x = value; } }
        public double Y { get { return m_y; } set { m_y = value; } }

        public Distance DistanceTo(Point other)
        { return new Distance((new Vector(this, other)).Length, Distance.Units.PKInternal); }

        public Point Translate(Vector v)
        { return new Point(m_x + (v.End.X - v.Start.X), m_y + (v.End.Y - v.Start.Y)); }

        public static Point operator +(Point lhs, Point rhs)
        { return new Point(lhs.m_x + rhs.m_x, lhs.m_y + rhs.m_y); }
        public static Point operator -(Point lhs, Point rhs)
        { return new Point(rhs.m_x - lhs.m_x, rhs.m_y - lhs.m_y); }
    }

    public class Vector
    {
        protected Point m_start;
        protected Point m_end;
        protected const double sm_tolerance = 0.00001;

        public Vector() { m_start = new Point(); m_end = new Point(); }
        public Vector(Point start, Point end) { m_start = new Point(start); m_end = new Point(end); }
        public Vector(double startX, double startY, double endX, double endY)
        { m_start = new Point(startX, startY); m_end = new Point(endX, endY); }

        public Vector(Point start, Radians azimuth, double length)
        {
            double dx; double dy;
            if (azimuth <= Radians.PIOverTwo)
            {
                dx = Math.Cos((double)azimuth);
                dy = Math.Sin((double)azimuth);
            }
            else if (azimuth <= Radians.PI)
            {
                dx = -Math.Sin((double)(azimuth - Radians.PIOverTwo));
                dy = Math.Cos((double)(azimuth - Radians.PIOverTwo));
            }
            else if (azimuth <= Radians.ThreePIOverTwo)
            {
                dx = -Math.Cos((double)(azimuth - Radians.PI));
                dy = -Math.Sin((double)(azimuth - Radians.PI));
            }
            else
            {
                dx = Math.Sin((double)(azimuth - Radians.ThreePIOverTwo));
                dy = -Math.Cos((double)(azimuth - Radians.ThreePIOverTwo));
            }
            dx *= length;
            dy *= length;

            m_start = new Point(start);
            m_end = new Point(start.X + dx, start.Y + dy);
        }

        public Point Start { get { return m_start; } }
        public Point End { get { return m_end; } }

        public Point EndPt(int i) { return ((i == 0) ? m_start : m_end); }

        public double Length
        { get
            { return Math.Sqrt(Math.Pow(m_end.X - m_start.X, 2.0) + Math.Pow(m_end.Y - m_start.Y, 2.0)); }
        }

        public Radians Azimuth
        {
            get
            {
                Radians retval;

                double trans_x = m_end.X - m_start.X;
                double trans_y = m_end.Y - m_start.Y;

                if (trans_x > 0.0 && trans_y >= 0.0)   // First quadrant
                { retval =  new Radians(System.Math.Atan(trans_y / trans_x)); }
                else if (trans_x <= 0.0 && trans_y > 0.0)  // Second quadrant
                { retval = new Radians(System.Math.Atan(-trans_x / trans_y)) + Radians.PIOverTwo; }
                else if (trans_x < 0.0 && trans_y <= 0.0)  // Third quadrant
                { retval = new Radians(System.Math.Atan(-trans_y / -trans_x)) + Radians.PI; }
                else  // Fourth quadrant
                { retval = new Radians(System.Math.Atan(trans_x / -trans_y)) + Radians.ThreePIOverTwo; }

                return retval;
            }
        }

        public static bool IsParallel(Vector v1, Vector v2)
        {
            return isParallelWithinTolerance(v1, v2, sm_tolerance);
        }

        public static bool IsAlmostParallel(Vector v1, Vector v2)
        {
            return isParallelWithinTolerance(v1, v2, 1.0 - Math.Cos(Math.PI / 180.0));
        }

        public static bool IsPerpendicular(Vector v1, Vector v2)
        {
            Double dot = DotProduct(v1, v2);
            Double tol = sm_tolerance * Math.Min(v1.Length, v2.Length);
            return (Math.Abs(dot) < tol);
        }

        public static double DotProduct(Vector v1, Vector v2)
        {
            Point o1 = new Point(v1.End.X - v1.Start.X, v1.End.Y - v1.Start.Y);
            Point o2 = new Point(v2.End.X - v2.Start.X, v2.End.Y - v2.Start.Y);
            return (o1.X * o2.X + o1.Y * o2.Y);
        }

        public Vector Normalize()
        {
            double len = Length;
            return new Vector(Start, Azimuth, 1.0);
        }

        public Vector Reverse()  { return new Vector(End, Start); }

        public static Point Intersection(Vector v1, Vector v2)
        {
            // This is a bit of a misnomer because we are really finding the intersection
            // of two lines, not two vectors (line segments), but it suits the purpose
            double denom = ((v1.Start.X - v1.End.X) * (v2.Start.Y - v2.End.Y))
                           - ((v1.Start.Y - v1.End.Y) * (v2.Start.X - v2.End.X));
            if (Math.Abs(denom) < sm_tolerance) { return null; }   // Parallel

            double sub1 = v1.Start.X * v1.End.Y - v1.Start.Y * v1.End.X;
            double sub2 = v2.Start.X * v2.End.Y - v2.Start.Y * v2.End.X;
            double retvalX = (  (v2.Start.X - v2.End.X) * sub1
                              - (v1.Start.X - v1.End.X) * sub2) / denom;
            double retvalY = (  (v2.Start.Y - v2.End.Y) * sub1
                              - (v1.Start.Y - v1.End.Y) * sub2) / denom;

            return new Point(retvalX, retvalY);
        }

        public Point MidPoint
        { get
            {
                double dx = (m_end.X - m_start.X) / 2.0;
                double dy = (m_end.Y - m_start.Y) / 2.0;
                return new Point(m_start.X + dx, m_start.Y + dy);
            }
        }

        public Point Project(Point p)
        {
            Radians perpAz = Azimuth + Radians.PIOverTwo;
            return Intersection(this, new Vector(p, perpAz, 1.0));
        }

        public bool IsCoincident(Point p)
        { return ((new Vector(Project(p), p)).Length < sm_tolerance); }

        private static bool isParallelWithinTolerance(Vector v1, Vector v2, double tol)
        {
            double dot = DotProduct(v1, v2);
            double v1_len = v1.Length;
            double v2_len = v2.Length;
            return (Math.Abs(dot / (v1_len * v2_len)) > (1.0 - tol));
        }
    }

    public class Radians
    {
        protected double m_value;

        public Radians()
        { constructRadians(sm_undefinedVal); }
        public Radians(double value)
        { constructRadians(value); }

        private void constructRadians(double val)
        {
            if (val != sm_undefinedVal)
            {
                while (val > (2.0 * Math.PI) - sm_tolerance) { val -= (2.0 * Math.PI); }
                while (val < (-2.0 * Math.PI) + sm_tolerance) { val += (2.0 * Math.PI); }
            }
            m_value = val;
        }

        public static Json.ExporterFunc<Radians> JsonExporter = delegate(Radians r, Json.JsonWriter w)
        { w.Write(r.m_value); };
        public static Json.ImporterFunc<double, Radians> JsonImporter = delegate(double d)
        { return new Radians(d); };

        public static explicit operator double(Radians r)
        {
            if (!r.IsUndefined) { return r.m_value; }
            else { throw new ArgumentException(@"Value of Radians argument is undefined"); }
        }

        public double AsDegrees()
        {
            if (!IsUndefined) { return m_value * 180.0 / Math.PI; }
            else { throw new ArgumentException(@"Value of Radians argument is undefined"); }
        }

        public static bool operator==(Radians lhs, Radians rhs)
        {
            return isEqual(lhs, rhs, sm_tolerance);
        }

        public static bool operator !=(Radians lhs, Radians rhs)
        { return !(lhs == rhs); }
        public override bool Equals(Object obj)
        { return obj is Radians && this == (Radians)obj; }
        public override int GetHashCode()
        { return m_value.GetHashCode(); }

        public static bool operator <=(Radians lhs, Radians rhs)
        {
            if (!lhs.IsUndefined && !rhs.IsUndefined)
            {
                if (lhs == rhs) { return true; }
                else if (lhs.m_value < rhs.m_value) { return true; }
                else { return false; }
            }
            else { throw new ArgumentException(@"Value of Radians argument is undefined"); }
        }

        public static bool operator >=(Radians lhs, Radians rhs)
        {
            if (!lhs.IsUndefined && !rhs.IsUndefined)
            {
                if (lhs == rhs) { return true; }
                else if (lhs.m_value > rhs.m_value) { return true; }
                else { return false; }
            }
            else { throw new ArgumentException(@"Value of Radians argument is undefined"); }
        }

        public static bool operator <(Radians lhs, Radians rhs)
        {
            if (!lhs.IsUndefined && !rhs.IsUndefined)  { return !(lhs >= rhs); }
            else { throw new ArgumentException(@"Value of Radians argument is undefined"); }
        }

        public static bool operator >(Radians lhs, Radians rhs)
        {
            if (!lhs.IsUndefined && !rhs.IsUndefined)  { return !(lhs <= rhs); }
            else { throw new ArgumentException(@"Value of Radians argument is undefined"); }
        }

        public static Radians operator +(Radians lhs, Radians rhs)
        {
            if (!lhs.IsUndefined && !rhs.IsUndefined) { return new Radians(lhs.m_value + rhs.m_value); }
            else { throw new ArgumentException(@"Value of Radians argument is undefined"); }
        }

        public static Radians operator -(Radians lhs, Radians rhs)
        {
            if (!lhs.IsUndefined && !rhs.IsUndefined) { return new Radians(lhs.m_value - rhs.m_value); }
            else { throw new ArgumentException(@"Value of Radians argument is undefined"); }
        }

        public static Radians operator -(Radians operand)
        {
            return new Radians(-operand.m_value);
        }

        public static Radians TwoPIPlus(Radians operand)
        { return new Radians((Math.PI * 2.0) + (double)operand); }

        public static Radians TwoPIMinus(Radians operand)
        { return new Radians((Math.PI * 2.0) - (double)operand); }

        public bool IsUndefined { get { return (m_value == sm_undefinedVal); } }

        public bool IsParallelTo(Radians other)
        {
            return isParallelTo(other, sm_tolerance);
        }

        public bool IsAlmostParallelTo(Radians other)
        {
            return isParallelTo(other, sm_oneDegree);
        }

        public bool IsPerpendicularTo(Radians other)
        {
            if (!this.IsUndefined && !other.IsUndefined)
            {
                Radians diff = this - other;
                return (diff == Radians.PIOverTwo || diff == Radians.ThreePIOverTwo);
            }
            else { throw new ArgumentException(@"Value of Radians argument is undefined"); }
        }

        public static Radians FromDegrees(double deg)
        { return new Radians(deg * sm_oneDegree); }

        public static Radians FromScaleneTriangle(double adjSide1, double adjSide2, double oppSide)
        {
            if (adjSide1 <= 0.0 || adjSide2 <= 0.0 || oppSide <= 0.0)
            { throw new ArgumentOutOfRangeException("Argument must not be less than or equal to zero"); }

            // Use the Law of Cosines to find the angle
            double numer = (adjSide1 * adjSide1) + (adjSide2 * adjSide2) - (oppSide * oppSide);
            double denom = 2.0 * adjSide1 * adjSide2;
            return new Radians(Math.Acos(numer / denom));
        }

        private static bool isEqual(Radians lhs, Radians rhs, double tol)
        {
            if (System.Object.ReferenceEquals(lhs, rhs)) { return true; }
            if ((object)lhs == null || (object)rhs == null) { return false; }
            return (Math.Abs(lhs.m_value - rhs.m_value) < tol);
        }

        private bool isParallelTo(Radians other, double tol)
        {
            if (!this.IsUndefined && !other.IsUndefined)
            {
                Radians diff = this - other;
                return (   isEqual(diff, Radians.Zero, tol)
                        || isEqual(diff, Radians.PI, tol)
                        || isEqual(diff, new Radians(-Math.PI), tol));
            }
            else { throw new ArgumentException(@"Value of Radians argument is undefined"); }
        }

        private const double sm_tolerance = 0.00001;
        private const double sm_oneDegree = Math.PI / 180.0;
        private const double sm_undefinedVal = -1000001.01;

        public static Radians Undefined = new Radians(sm_undefinedVal);
        public static Radians Zero = new Radians(0.0);
        public static Radians PI = new Radians(Math.PI);
        public static Radians PIOverFour = new Radians(Math.PI / 4.0);
        public static Radians PIOverTwo = new Radians(Math.PI / 2.0);
        public static Radians ThreePIOverTwo = new Radians(Math.PI * 1.5);
    }
}
