using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using HBLib.Utils;
using Microsoft.Azure.WebJobs;

namespace HBLib.AzureFunctions
{
    public static class Misc
    {
        public static DateTime ConvertFromUnixTimestamp(double timestamp)
        {
            var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return origin.AddSeconds(timestamp);
        }

        public static double ConvertToUnixTimestamp(DateTime date)
        {
            var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var diff = date.ToUniversalTime() - origin;
            return Math.Floor(diff.TotalSeconds);
        }

        public static string BinPath(ExecutionContext dir)
        {
            var isDebug = EnvVar.Get("DEBUG");
            if (isDebug == "0")
            {
                var afPath = Directory.GetParent(dir.FunctionDirectory).FullName;
                var binPath = Path.Combine(afPath, "bin");
                return binPath;
            }

            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase);
            path = path.Substring(6);

            var pattern = @"(.+\\bin).+\\bin";

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

        public static string GetTempPath()
        {
            var isDebug = EnvVar.Get("DEBUG");
            string res;
            if (isDebug == "0")
                res = @"D:\local\Temp";
            else
                res = "temp";
            Directory.CreateDirectory(res);
            return res;
        }

        public static string GenSessionId()
        {
            return $"session_{Guid.NewGuid()}_{DT.Format(DateTime.Now)}";
        }

        public static string GenLocalDir(string sessionId)
        {
            var path = Path.Combine(GetTempPath(), "data", sessionId + "/");
            Directory.CreateDirectory(path);
            return path;
        }
    }
}