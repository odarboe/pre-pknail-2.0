using System;
using System.Collections.Generic;
using System.Text;

namespace PKNailForRevit.TestCore
{
    public class TestConfig
    {
        private const string cm_testConfigFilename = @"PKNailTestConfig.txt";
        private const string cm_surveyModelFilename = @"PKNailModelState.txt";

        public static string TestConfigFilename { get { return cm_testConfigFilename; } }

        public string RevitProgramFilePath;
        public string RevitProjectFilePath;
        public string AutomationScriptFilePath;
        public string ReferenceModelStateFilePath;

        public TestConfig()
        {
            RevitProgramFilePath = @"";
            RevitProjectFilePath = @"";
            AutomationScriptFilePath = @"";
            ReferenceModelStateFilePath = @"";
        }

        public static TestConfig FromFileAtDir(string dirPath)
        { return fromFile(dirPath + TestConfigFilename); }

        public static TestConfig FromFile()
        { return fromFile(TestConfigFilename); }

        public static string ExportModelFilename
        { get { return cm_surveyModelFilename; } }

        private static TestConfig fromFile(string path)
        {
            TestConfig retval = null;
            using (System.IO.StreamReader testConfigSR = System.IO.File.OpenText(path))
            {
                string configJson = testConfigSR.ReadToEnd();
                if (configJson.Length > 0)
                { retval = (TestConfig)Json.JsonMapper.ToObject(typeof(TestConfig), configJson); }
            }
            return retval;
        }
    }
}
