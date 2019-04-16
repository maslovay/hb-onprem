using System;
using System.IO;

namespace HBLib.Utils
{
    public class PathClient
    {
        public String BinPath()
        {
            return Path.Combine(Directory.GetCurrentDirectory(), "bin");
        }

        public String GetTempPath()
        {
            Directory.CreateDirectory("temp");
            return "temp";
        }

        public String GenSessionId()
        {
            return $"session_{Guid.NewGuid()}_{DT.Format(DateTime.Now)}";
        }

        public String GenLocalDir(String sessionId)
        {
            var path = Path.Combine(GetTempPath(), "data", sessionId + "/");
            Directory.CreateDirectory(path);
            return path;
        }
    }
}