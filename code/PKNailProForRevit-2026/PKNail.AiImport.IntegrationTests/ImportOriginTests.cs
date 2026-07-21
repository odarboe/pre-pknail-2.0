using System.IO;
using System.Linq;

using NUnit.Framework;

using PKNailForRevit;
using PKNailForRevit.AiImport;
using PKNail.AiImport.Model;
using PKNail.AiImport.Mapping;

namespace PKNail.AiImport.IntegrationTests
{
    /// <summary>
    /// Epic 10 (Coordinate System &amp; Project Origin). The v2 absolute path lands the building at a
    /// deterministic, documented origin: every endpoint is shifted by the building-wide min X / min Y so
    /// the model sits at the project origin regardless of its far-flung survey coordinates. The applied
    /// offset is a pure function of the input (re-importing yields byte-identical geometry) and is recorded
    /// on the report so the landing point relative to the survey frame is documented. Phase-1 decision
    /// (Q13): default coordinate system on import; the user runs <c>Set Coord Sys</c> afterward — the
    /// interactive PickObject path is never invoked headlessly.
    /// </summary>
    [TestFixture]
    public class ImportOriginTests
    {
        private static string RealPath =>
            Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "new_final_combined_pknail.json");
        private static string V2ExamplePath =>
            Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "ai_engine_pknail_results.v2.example.json");

        private static MappedBuilding LoadMapped(string path) => AiModelMapper.Map(AiModelReader.Load(path));

        [Test]
        public void RealBuilding_RebasesToOrigin_AndReportsTheOffset()
        {
            var proj = new RecordingProjectModel();
            ImportReport report = new AiModelImporter().Import(proj, LoadMapped(RealPath));

            Assert.That(report.Committed, Is.True, report.ToString());
            Assert.That(report.Rebased, Is.True, "the real building sits far from the origin (Y ~ -100 m)");

            // After re-basing, the minimum placed X and Y are both 0 — the model is anchored at the origin.
            double minX = proj.CreateWallAbsoluteCalls.Min(w => System.Math.Min(w.StartXMeters, w.EndXMeters));
            double minY = proj.CreateWallAbsoluteCalls.Min(w => System.Math.Min(w.StartYMeters, w.EndYMeters));
            Assert.That(minX, Is.EqualTo(0.0).Within(1e-6), "min X anchored to origin");
            Assert.That(minY, Is.EqualTo(0.0).Within(1e-6), "min Y anchored to origin");

            // The recorded offset equals the original survey-frame minimum (X positive, Y negative here).
            Assert.That(report.RebaseXMeters, Is.GreaterThan(0.0));
            Assert.That(report.RebaseYMeters, Is.LessThan(0.0));
            Assert.That(report.ToString(), Does.Contain("origin re-based to project 0,0"));
        }

        [Test]
        public void Reimport_IsDeterministic_ByteIdenticalGeometry()
        {
            var a = new RecordingProjectModel();
            var b = new RecordingProjectModel();
            new AiModelImporter().Import(a, LoadMapped(RealPath));
            new AiModelImporter().Import(b, LoadMapped(RealPath));

            Assert.That(a.CreateWallAbsoluteCalls.Count, Is.EqualTo(b.CreateWallAbsoluteCalls.Count));
            for (int i = 0; i < a.CreateWallAbsoluteCalls.Count; i++)
            {
                RecordingProjectModel.AbsoluteWallCall wa = a.CreateWallAbsoluteCalls[i];
                RecordingProjectModel.AbsoluteWallCall wb = b.CreateWallAbsoluteCalls[i];
                Assert.That(wa.StartXMeters, Is.EqualTo(wb.StartXMeters).Within(0.0), $"wall {i} start X deterministic");
                Assert.That(wa.StartYMeters, Is.EqualTo(wb.StartYMeters).Within(0.0), $"wall {i} start Y deterministic");
                Assert.That(wa.EndXMeters, Is.EqualTo(wb.EndXMeters).Within(0.0), $"wall {i} end X deterministic");
                Assert.That(wa.EndYMeters, Is.EqualTo(wb.EndYMeters).Within(0.0), $"wall {i} end Y deterministic");
            }
        }

        [Test]
        public void RoomAlreadyAtOrigin_IsNotRebased()
        {
            var proj = new RecordingProjectModel();
            ImportReport report = new AiModelImporter().Import(proj, LoadMapped(V2ExamplePath));

            // The v2 example room already starts at (0,0), so the offset is zero and no re-base note appears.
            Assert.That(report.Rebased, Is.False);
            Assert.That(report.RebaseXMeters, Is.EqualTo(0.0).Within(1e-9));
            Assert.That(report.RebaseYMeters, Is.EqualTo(0.0).Within(1e-9));
            Assert.That(report.ToString(), Does.Not.Contain("origin re-based"));
        }
    }
}
