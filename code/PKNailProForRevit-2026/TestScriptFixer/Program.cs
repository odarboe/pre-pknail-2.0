using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace TestScriptFixer
{
    class Program
    {
        static void Main(string[] args)
        {
            DirectoryInfo di = new DirectoryInfo(Environment.CurrentDirectory);
            FileInfo[] fis = di.GetFiles();

            foreach (FileInfo fi in fis)
            {
                string name = fi.Name;
                if (Regex.IsMatch(name, @"PKNailModelState.*txt$"))
                { processFile(name); }
            }
        }

        static void processFile(string fname)
        {
            using (StreamReader instr = File.OpenText(fname))
            {
                using (StreamWriter outstr = File.CreateText(fname + @".new"))
                {
                    while (!instr.EndOfStream)
                    {
                        string line = instr.ReadLine();
                        if (Regex.IsMatch(line, @"Measured")) { continue; }
                        if (Regex.IsMatch(line, @"SillHeight")) { line = Regex.Replace(line, @",", @""); }
                        //line = Regex.Replace(line, @",\\""Location\\"":-10000001.0001", @"");
                        //line = Regex.Replace(line, @"LocationInHost", @"Location");
                        //line = Regex.Replace(line, @"AbsoluteLocation\\"".*Original", @"Wall");
                        //line = Regex.Replace(line, @"OriginalSurveyOrientation", @"WallSurveyOrientation");
                        //line = Regex.Replace(line, @"WALL_HEIGHT_TYPE""(\s*): 0,", @"WALL_HEIGHT_TYPE""$1: 9946,");
                        //line = Regex.Replace(line, @"WALL_BASE_CONSTRAINT""(\s*): 0,", @"WALL_BASE_CONSTRAINT""$1: 30,");
                        //line = Regex.Replace(line, @"CurrentLevelID\\"":\\""30\\"",\\""NextLevelID\\"":\\""9946\\""", @"ProjectLevelNames\"":[\""Level 0\"",\""Level 1\""],\""CurrentLevelIdx\"":0");
                        //line = Regex.Replace(line, @",\\""Height\\"":-10000001.0001,", @",\""Height\"":10.0,");
                        //line = Regex.Replace(line, @",\\""CornerAngle\\"":", @",\""ConnectedLevelIdx\"":foo,\""CornerAngle\"":");
                        //line = Regex.Replace(line, @"MeasuredWidth""(\s*): -10000001.0001,", @"MeasuredWidth""$1: 0.0,");
                        //line = Regex.Replace(line, @"LevelName""(\s*): null,", @"LevelName""$1: ""Level 1"",");
                        //line = Regex.Replace(line, @"Selection\\"":(.*?)}", @"Selection\"":$1,\""CurrentLevelID\"":\""30\"",\""NextLevelID\"":\""9946\""}");
                        //line = Regex.Replace(line, @"Selection\\"":(.*?),", @"Selection\"":$1,\""CurrentLevelID\"":\""30\"",\""NextLevelID\"":\""9946\"",");
                        //line = Regex.Replace(line, @"Selection\\"":(.*?)}", @"Selection\"":$1,\""CurrentLevelID\"":\""30\"",\""NextLevelID\"":\""9946\""}");
                        //line = Regex.Replace(line, @"Selection\\"":(\w+),", @"Selection\\"":[\\""(\d+)\\""],\\""CurrentLevelID\\"":\\""30\\"",\\""NextLevelID\\"":\\""9946\\"",");
                        //line = Regex.Replace(line, @"Selection\\"":\\""(\d+)\\""", @"Selection\"":[\""$1\""]");
                        //line = Regex.Replace(line, @"SurveyOrientation\\"":-1", @"SurveyOrientation\"":1");
                        //line = Regex.Replace(line, @"SurveyOrientation"" : 1", @"SurveyOrientation"" : 0");
                        //line = Regex.Replace(line, @"SurveyOrientation"" : -1", @"SurveyOrientation"" : 1");
                        outstr.WriteLine(line);
                    }
                }
            }

            File.Move(fname, fname + @".old");
            File.Move(fname + @".new", fname);
        }
    }
}
