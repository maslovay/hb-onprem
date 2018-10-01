using HBLib.Utils;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace HBLib.AzureFunctions
{
    public static class Misc
    {
        public static DateTime ConvertFromUnixTimestamp(double timestamp)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return origin.AddSeconds(timestamp);
        }

        public static double ConvertToUnixTimestamp(DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan diff = date.ToUniversalTime() - origin;
            return Math.Floor(diff.TotalSeconds);
        }

        /*public static string BinPath(ExecutionContext dir)
        {
            string isDebug = EnvVar.Get("DEBUG");
            if (isDebug == "0")
            {
                string afPath = Directory.GetParent(dir.FunctionDirectory).FullName;
                string binPath = Path.Combine(afPath, "bin");
                return binPath;
            } 
            else
            {
                var path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase); 
                path = path.Substring(6);

                string pattern = @"(.+\\bin).+\\bin";

                try
                {
                    var m = Regex.Matches(path, pattern)[0];
                    path = m.Groups[1].ToString();
                }
                catch (Exception e)
                {
                    throw new Exception("Bin directory not found");
                }

                return path;
            }
        }*/

        public static string GetTempPath()
        {
            string isDebug = EnvVar.Get("DEBUG");
            string res;
            if (isDebug == "0")
            {
                res = @"D:\local\Temp";
            }
            else
            {
                res = "temp";
            }
            Directory.CreateDirectory(res);
            return res;
        }

        //public static string GetCommonPath() {
        //    string isDebug = EnvVar.Get("DEBUG");
        //    string res;
        //    if (isDebug == "0") {
        //        return @"D:\home\data\common";
        //    } else {
        //        return "common/";
        //    }
        //}

        public static string GenSessionId()
        {
            return $"session_{Guid.NewGuid()}_{DT.Format(DateTime.Now)}";
        }

        public static string GenLocalDir(string sessionId)
        {
            string path = Path.Combine(GetTempPath(), "data", sessionId + "/");
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
