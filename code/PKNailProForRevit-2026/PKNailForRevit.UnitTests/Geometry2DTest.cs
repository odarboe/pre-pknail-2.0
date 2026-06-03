using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;
using NUnit.Framework.Constraints;

using PKNailForRevit.Geometry2D;

namespace PKNailForRevit.UnitTests
{
    [TestFixture]
    public class Geometry2DTest
    {
        [Test]
        public void VectorTest()
        {
            Double az;
            az = (Double)(new Vector(new Point(0.0, 0.0), new Point(15.5, 0))).Azimuth;
            Assert.That(az, new EqualConstraint(0.0));
            az = (Double)(new Vector(new Point(2703.82, -499.03), new Point(2703.92, -499.03))).Azimuth;
            Assert.That(az, new EqualConstraint(0.0));
            az = (Double)(new Vector(new Point(0.0, 0.0), new Point(1.0, 1.0))).Azimuth;
            Assert.That(Math.Round(az, 5), new EqualConstraint(Math.Round(Math.PI * 45.0 / 180.0, 5)));
            az = (Double)(new Vector(new Point(0.0, 0.0), new Point(-1.0, 1.0))).Azimuth;
            Assert.That(Math.Round(az, 5), new EqualConstraint(Math.Round(Math.PI * 135.0 / 180.0, 5)));
            az = (Double)(new Vector(new Point(0.0, 0.0), new Point(-1.0, -1.0))).Azimuth;
            Assert.That(Math.Round(az, 5), new EqualConstraint(Math.Round(Math.PI * 225.0 / 180.0, 5)));
            az = (Double)(new Vector(new Point(0.0, 0.0), new Point(1.0, -1.0))).Azimuth;
            Assert.That(Math.Round(az, 5), new EqualConstraint(Math.Round(Math.PI * 315.0 / 180.0, 5)));

            Vector v;
            v = new Vector(new Point(0.0, 0.0), new Radians(Math.PI / 4.0), 5.0);
            Assert.That(Math.Round(v.End.X, 5), new EqualConstraint(3.53553));
            Assert.That(Math.Round(v.End.Y, 5), new EqualConstraint(3.53553));

            Vector v1; Vector v2;
            v1 = new Vector(26.0, -4.0, 28.0, -1.0);
            v2 = new Vector(5.0, 9.0, 9.0, 15.0);
            Assert.IsTrue(Geometry2D.Vector.IsParallel(v1, v2));

            v1 = new Vector(0.0, 0.0, 100000.0, 100000.0);
            v2 = new Vector(0.0, 0.0, 0.01, -0.01);
            Assert.IsTrue(Geometry2D.Vector.IsPerpendicular(v1, v2));

            v1 = new Vector(0.0, 0.0, 100000.0, 100001.0);
            Assert.IsFalse(Geometry2D.Vector.IsPerpendicular(v1, v2));

            v1 = new Vector(-1.0, -1.0, 1.0, 1.0);
            v2 = new Vector(-1.0, 1.0, 1.0, -1.0);
            Point i = Vector.Intersection(v1, v2);
            Assert.That(Math.Round(i.X, 5), new EqualConstraint(0.0));
            Assert.That(Math.Round(i.Y, 5), new EqualConstraint(0.0));

            // Non-intersecting vectors
            v1 = new Vector(-6.0, -3.0, -1.0, -3.0);
            v2 = new Vector(0.0, 0.0, 0.0, -10.0);
            i = Vector.Intersection(v1, v2);
            Assert.That(Math.Round(i.X, 5), new EqualConstraint(0.0));
            Assert.That(Math.Round(i.Y, 5), new EqualConstraint(-3.0));

            v1 = new Vector(10.0, 10.0, 50.0, 25.0);
            v2 = new Vector(15.0, 0.0, 30.0, 30.0);
            i = Vector.Intersection(v1, v2);
            Assert.That(Math.Round(i.X, 5), new EqualConstraint(22.30769));
            Assert.That(Math.Round(i.Y, 5), new EqualConstraint(14.61538));

        }
    }
}
