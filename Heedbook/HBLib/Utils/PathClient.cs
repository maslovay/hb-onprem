using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Repository;
using HBLib;
using HBLib.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace HBLib.Utils
{
    public class PathClient
    {
        public string BinPath()
        {
            return Path.Combine(Directory.GetCurrentDirectory(), "bin");
        }

        public string GetTempPath()
        {
            Directory.CreateDirectory("temp");
            return "temp";
        }

        public string GenSessionId()
        {
            return $"session_{Guid.NewGuid()}_{DT.Format(DateTime.Now)}";
        }

        public string GenLocalDir(string sessionId)
        {
            var path = Path.Combine(GetTempPath(), "data", sessionId + "/");
            Directory.CreateDirectory(path);
            return path;
        }        
    }

}
