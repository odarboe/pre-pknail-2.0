using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;
using NUnit.Framework.Constraints;

using PKNailForRevit;

namespace PKNailForRevit.UnitTests
{
    [TestFixture]
    public class DistanceTest
    {
        [Test]
        public void CtorsTest()
        {
            Distance testee = new Distance();
            Assert.That(testee.IsNoValue);

            testee = new Distance(10.5, Distance.Units.Imperial);
            Assert.That(testee.ValueImperial, new EqualConstraint(10.5));

            Distance testee2 = new Distance(testee);
            Assert.That(testee2.ValueImperial, new EqualConstraint(10.5));

            testee = new Distance(6543.21, Distance.Units.PKInternal);
            Assert.That(testee2.ValuePKInternal, new EqualConstraint(10.5));

            testee = new Distance(1.75, Distance.Units.MetricMeter);
            Assert.That(testee.ValueMetricMeters, new EqualConstraint(1.75));

            testee = new Distance(31415.926, Distance.Units.MetricMillimeter);
            Assert.That(testee.ValueMetricMillimeters.ToString(@"F3"), new EqualConstraint(@"31415.926"));
            Assert.That(testee.ValueMetricMeters.ToString(@"F6"), new EqualConstraint(@"31.415926"));
        }

        [Test]
        public void OperatorsTest()
        {
            // TODO: Implement the tests
        }

        [Test]
        public void ParseImperialTest()
        {
            Distance testee = Distance.ParseImperial("", "");
            Assert.That(testee.IsNoValue);

            Distance zeroDistance = new Distance(0.0, Distance.Units.Imperial);
            testee = Distance.ParseImperial("0", "");
            Assert.That(testee, new EqualConstraint(zeroDistance));
            testee = Distance.ParseImperial("-0", "");
            Assert.That(testee, new EqualConstraint(zeroDistance));
            testee = Distance.ParseImperial("0.0", "");
            Assert.That(testee, new EqualConstraint(zeroDistance));
            testee = Distance.ParseImperial("-0.0", "");
            Assert.That(testee, new EqualConstraint(zeroDistance));
            testee = Distance.ParseImperial("", "0");
            Assert.That(testee, new EqualConstraint(zeroDistance));
            testee = Distance.ParseImperial("", "-0");
            Assert.That(testee, new EqualConstraint(zeroDistance));
            testee = Distance.ParseImperial("", "0.0");
            Assert.That(testee, new EqualConstraint(zeroDistance));
            testee = Distance.ParseImperial("", "-0.0");
            Assert.That(testee, new EqualConstraint(zeroDistance));
            testee = Distance.ParseImperial("0", "0");
            Assert.That(testee, new EqualConstraint(zeroDistance));
            testee = Distance.ParseImperial("-0", "-0");
            Assert.That(testee, new EqualConstraint(zeroDistance));
            testee = Distance.ParseImperial("0.0", "0.0");
            Assert.That(testee, new EqualConstraint(zeroDistance));
            testee = Distance.ParseImperial("-0.0", "-0.0");
            Assert.That(testee, new EqualConstraint(zeroDistance));

            testee = Distance.ParseImperial("00", "00 00/00");
            Assert.That(testee, new EqualConstraint(zeroDistance));
            testee = Distance.ParseImperial("6", "6 6/0");
            Assert.That(testee.IsNoValue);
            testee = Distance.ParseImperial("6", "6 0/0");
            Assert.That(testee, new EqualConstraint(new Distance(6.5, Distance.Units.Imperial)));
            testee = Distance.ParseImperial("6", "6 00/00");
            Assert.That(testee, new EqualConstraint(new Distance(6.5, Distance.Units.Imperial)));

            testee = Distance.ParseImperial("28.5352", "");
            Assert.That(testee.ValueImperial, new EqualConstraint(28.5352));

            testee = Distance.ParseImperial("-3", "5 3/8");
            Assert.That(testee.ValueImperial.ToString("F4"), new EqualConstraint("-3.4479"));

            testee = Distance.ParseImperial("- 49", "5 1/4");
            Assert.That(testee.ValueImperial.ToString("F4"), new EqualConstraint("-49.4375"));

            testee = Distance.ParseImperial("", "-11 3/4");
            Assert.That(testee.ValueImperial.ToString("F4"), new EqualConstraint("-0.9792"));

            testee = Distance.ParseImperial("", "- 1 1/2");
            Assert.That(testee.ValueImperial.ToString("F4"), new EqualConstraint("-0.1250"));

            testee = Distance.ParseImperial("", "-5/8");
            Assert.That(testee.ValueImperial.ToString("F4"), new EqualConstraint("-0.0521"));

            testee = Distance.ParseImperial("", "38.7237");
            Assert.That(testee.ValueImperial.ToString("F4"), new EqualConstraint("3.2270"));

            testee = Distance.ParseImperial("271", "6.01");
            Assert.That(testee.ValueImperial.ToString("F4"), new EqualConstraint("271.5008"));

            testee = Distance.ParseImperial("", "-.4");
            Assert.That(testee.ValueImperial.ToString("F4"), new EqualConstraint("-0.0333"));

            testee = Distance.ParseImperial("4.999", "0.4567");
            Assert.That(testee.ValueImperial.ToString("F4"), new EqualConstraint("5.0371"));

            testee = Distance.ParseImperial("-1999.0", "-11.0");
            Assert.That(testee.IsNoValue);

            testee = Distance.ParseImperial("1999.0", "-11.0");
            Assert.That(testee.IsNoValue);

            testee = Distance.ParseImperial("4 3/16", "");
            Assert.That(testee.ValueImperial.ToString("F4"), new EqualConstraint("4.1875"));

            testee = Distance.ParseImperial("10 2/13", "5.5");
            Assert.That(testee.ValueImperial.ToString("F4"), new EqualConstraint("10.6122"));
        }

        [Test]
        public void RenderImperialTest()
        {
            // TODO: Add boundary condition tests
            String feetStr = "foo"; String inchesStr = "bar";

            (new Distance()).RenderImperial(out feetStr, out inchesStr);
            Assert.That(feetStr, new EqualConstraint(""));
            Assert.That(inchesStr, new EqualConstraint(""));

            (new Distance(0.0, Distance.Units.Imperial)).RenderImperial(out feetStr, out inchesStr);
            Assert.That(feetStr, new EqualConstraint("0"));
            Assert.That(inchesStr, new EqualConstraint("0"));

            (new Distance(0.25, Distance.Units.Imperial)).RenderImperial(out feetStr, out inchesStr);
            Assert.That(feetStr, new EqualConstraint(""));
            Assert.That(inchesStr, new EqualConstraint("3"));

            // Ensure minus sign is rendered in the right place
            (new Distance(-0.25, Distance.Units.Imperial)).RenderImperial(out feetStr, out inchesStr);
            Assert.That(feetStr, new EqualConstraint(""));
            Assert.That(inchesStr, new EqualConstraint("-3"));

            (new Distance(28.0 + ((5.0 + (7.0 / 8.0)) / 12.0), Distance.Units.Imperial)).RenderImperial(out feetStr, out inchesStr);
            Assert.That(feetStr, new EqualConstraint("28"));
            Assert.That(inchesStr, new EqualConstraint("5 7/8"));

            // Ensure minus sign is rendered in the right place
            (new Distance(-(28.0 + ((5.0 + (7.0 / 8.0)) / 12.0)), Distance.Units.Imperial)).RenderImperial(out feetStr, out inchesStr);
            Assert.That(feetStr, new EqualConstraint("-28"));
            Assert.That(inchesStr, new EqualConstraint("5 7/8"));

            (new Distance(27.9999, Distance.Units.Imperial)).RenderImperial(out feetStr, out inchesStr);
            Assert.That(feetStr, new EqualConstraint("28"));
            Assert.That(inchesStr, new EqualConstraint(""));

            (new Distance(28.0001, Distance.Units.Imperial)).RenderImperial(out feetStr, out inchesStr);
            Assert.That(feetStr, new EqualConstraint("28"));
            Assert.That(inchesStr, new EqualConstraint(""));
        }

        [Test]
        public void ParseMetricTest()
        {
            Distance testee = Distance.ParseMetricMeters(@"");
            Assert.That(testee.IsNoValue);
            testee = Distance.ParseMetricMillimeters(@"");
            Assert.That(testee.IsNoValue);

            Distance zeroDistance = new Distance(0.0, Distance.Units.Imperial);
            testee = Distance.ParseMetricMeters(@"0");
            Assert.That(testee, new EqualConstraint(zeroDistance));
            testee = Distance.ParseMetricMeters(@"-0");
            Assert.That(testee, new EqualConstraint(zeroDistance));
            testee = Distance.ParseMetricMeters(@"- 0");
            Assert.That(testee, new EqualConstraint(zeroDistance));
            testee = Distance.ParseMetricMeters(@"0.0");
            Assert.That(testee, new EqualConstraint(zeroDistance));
            testee = Distance.ParseMetricMeters(@"-0.0");
            Assert.That(testee, new EqualConstraint(zeroDistance));
            testee = Distance.ParseMetricMeters(@"- 0.0");
            Assert.That(testee, new EqualConstraint(zeroDistance));
            testee = Distance.ParseMetricMillimeters(@"0");
            Assert.That(testee, new EqualConstraint(zeroDistance));
            testee = Distance.ParseMetricMillimeters(@"-0");
            Assert.That(testee, new EqualConstraint(zeroDistance));
            testee = Distance.ParseMetricMillimeters(@"- 0");
            Assert.That(testee, new EqualConstraint(zeroDistance));
            testee = Distance.ParseMetricMillimeters(@"0.0");
            Assert.That(testee, new EqualConstraint(zeroDistance));
            testee = Distance.ParseMetricMillimeters(@"-0.0");
            Assert.That(testee, new EqualConstraint(zeroDistance));
            testee = Distance.ParseMetricMillimeters(@"- 0.0");
            Assert.That(testee, new EqualConstraint(zeroDistance));

            testee = Distance.ParseMetricMeters(@"100000.001");
            Assert.That(testee.ValueMetricMeters, new EqualConstraint(100000.001));
            testee = Distance.ParseMetricMillimeters(@".010101");
            Assert.That(testee.ValueMetricMillimeters.ToString(@"F6"), new EqualConstraint(@"0.010101"));
            testee = Distance.ParseMetricMillimeters(@"- 660.2");
            Assert.That(testee.ValueMetricMillimeters.ToString(@"F1"), new EqualConstraint(@"-660.2"));
            testee = Distance.ParseMetricMillimeters(@"-660. 2");
            Assert.That(testee.IsNoValue);
            testee = Distance.ParseMetricMillimeters(@"66a.2");
            Assert.That(testee.IsNoValue);
       }

        [Test]
        public void RenderMetricTest()
        {
            string rendered = (new Distance()).RenderMetricMeters();
            Assert.That(rendered, new EqualConstraint(@""));
            rendered = (new Distance()).RenderMetricMillimeters();
            Assert.That(rendered, new EqualConstraint(@""));
            rendered = (new Distance(123.0, Distance.Units.MetricMeter)).RenderMetricMeters();
            Assert.That(rendered, new EqualConstraint(@"123"));
            rendered = (new Distance(123.0, Distance.Units.MetricMeter)).RenderMetricMillimeters();
            Assert.That(rendered, new EqualConstraint(@"123000"));
            rendered = (new Distance(0.8765, Distance.Units.MetricMeter)).RenderMetricMeters();
            Assert.That(rendered, new EqualConstraint(@".877"));
            rendered = (new Distance(123456.78, Distance.Units.MetricMillimeter)).RenderMetricMillimeters();
            Assert.That(rendered, new EqualConstraint(@"123457"));
        }

        class DistanceTestContainer : JsonSerializable
        {
            public Distance Dist;
        }

        [Test]
        public void SerializerTest()
        {
            DistanceTestContainer serializeObj = new DistanceTestContainer();
            serializeObj.Dist = new Distance();

            string s = serializeObj.ToJson();
            Assert.That(s, new EqualConstraint("{\"Dist\":-10000001.0001}"));
            DistanceTestContainer deserializeObj = JsonSerializable.FromJson<DistanceTestContainer>(s);
            Assert.That(deserializeObj.Dist == serializeObj.Dist);

            serializeObj.Dist = new Distance(55.5555, Distance.Units.MetricMeter);
            s = serializeObj.ToJson();
            Assert.That(s, new EqualConstraint("{\"Dist\":182.26870106445}"));
            deserializeObj = JsonSerializable.FromJson<DistanceTestContainer>(s);
            Assert.That(deserializeObj.Dist == serializeObj.Dist);
        }
    }
}
