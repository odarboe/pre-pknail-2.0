using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;

namespace PKLicense.UnitTests
{
    [TestFixture]
    class UsageDataTest
    {
        private const int cm_dailyLicenseID = 61499298;
        private const int cm_hourlyLicenseID = 61499299;
        private const string cm_password = @"Qxhizoxs";
        private const string cm_instName = @"PKLicense.UnitTests";

        //[Test]
        //public void TestUsageDataJson()
        //{
        //    UsageData ud = new UsageData();
        //    ud.UsageRecordCounter = 57;
        //    ud.UsageUnitsBalance = 29;
        //    ud.LastAllocationDateTime = new DateTime(2011, 3, 14, 13, 09, 44);
        //    ud.LastSyncDateTime = new DateTime(2014, 1, 22, 8, 38, 50);
        //    ud.UsageRecords.Add(new UsageRecord(60991888, 56, @"Billy", @"Phillips", @"fieldhouse", true,
        //                                        new DateTime(2014, 2, 4, 15, 12, 8), new DateTime(2014, 2, 4, 16, 3, 16), 3));
        //    string json = ud.ToJson();
        //    Assert.NotNull(json);
        //}

        [Test]
        public void TestRecordUsageDaily()
        {
            //PKLicense dailyLicense = new PKLicense();
            //Assert.IsTrue(dailyLicense.Activate(cm_dailyLicenseID, cm_password, cm_instName));
            //Assert.IsTrue(dailyLicense.RecordUsage(@"Billy", @"Phillips Exeter fieldhouse"));
            //Assert.IsTrue(dailyLicense.Deactivate());
        }

        [Test]
        public void TestRecordUsageHourly()
        {
            //string userName1 = @"Billy";
            //string userName2 = @"Bob";
            //string projName1 = @"Phillips Exeter fieldhouse";
            //string projName2 = @"UMass library";

            //PKLicense hourlyLicense = new PKLicense();
            //Assert.IsTrue(hourlyLicense.Activate(cm_hourlyLicenseID, cm_password, cm_instName));
            //Assert.IsTrue(hourlyLicense.RecordUsage(userName1, projName1));
            //Assert.AreEqual(hourlyLicense.UsageUnitsBalance, 0);
            //System.Threading.Thread.Sleep(61000);  // Exceeds grace period in debug mode
            
            //// Simulate restarting application
            //hourlyLicense = new PKLicense();
            //Assert.IsTrue(hourlyLicense.IsMeteredHourly);
            //Assert.IsTrue(hourlyLicense.IsValid);
            //Assert.AreEqual(hourlyLicense.LastProjectName, projName1);
            //Assert.AreEqual(hourlyLicense.UsageUnitsBalance, 0);

            //Assert.IsTrue(hourlyLicense.RecordUsage(userName2, projName2));
            //Assert.AreEqual(hourlyLicense.LastProjectName, projName2);
            //Assert.AreEqual(hourlyLicense.UsageUnitsBalance, -1);

            //Assert.IsTrue(hourlyLicense.Deactivate());
        }

        [Test]
        public void TestUsageToSOLO()
        {
            //string userName1 = @"Billy";
            //string userName2 = @"Bob";
            //string projName1 = @"Phillips Exeter fieldhouse";
            //string projName2 = @"UMass library";

            //PKLicense hourlyLicense = new PKLicense();
            //Assert.IsTrue(hourlyLicense.Activate(cm_hourlyLicenseID, cm_password, cm_instName));
            //Assert.IsTrue(hourlyLicense.RecordUsage(userName1, projName1));
            //System.Threading.Thread.Sleep(61000);
            //Assert.IsTrue(hourlyLicense.RecordUsage(userName1, projName1));
            //System.Threading.Thread.Sleep(31000);
            //Assert.IsTrue(hourlyLicense.RecordUsage(userName2, projName2));
            //System.Threading.Thread.Sleep(61000);
            //Assert.IsTrue(hourlyLicense.RecordUsage(userName2, projName2));
            //System.Threading.Thread.Sleep(31000);
            //Assert.IsTrue(hourlyLicense.RecordUsage(userName2, projName2));

            //hourlyLicense.PostConsumedUsage();

            //Assert.AreEqual(hourlyLicense.LastProjectName, projName2);

            //Assert.IsTrue(hourlyLicense.Deactivate());
        }

        [Test]
        public void TestActivateHourly()
        {
            //PKLicense hourlyLicense = new PKLicense();
            //Assert.IsTrue(hourlyLicense.Activate(cm_hourlyLicenseID, cm_password, cm_instName));
            //Assert.IsTrue(hourlyLicense.IsValid);
            //Assert.IsFalse(hourlyLicense.IsBalanceWarning);
        }

        [Test]
        public void TestDeactivate()
        {
            //PKLicense hourlyLicense = new PKLicense();
            //Assert.IsTrue(hourlyLicense.Deactivate());
        }

        [Test]
        public void TestRecordUsage()
        {
            //PKLicense hourlyLicense = new PKLicense();
            //Assert.IsTrue(hourlyLicense.RecordUsage(@"Bob", @"Phillips Exeter fieldhouse"));
        }

        [Test]
        public void TestPostToSOLO()
        {
            //PKLicense hourlyLicense = new PKLicense();
            //hourlyLicense.PostConsumedUsage();
        }
    }
}
