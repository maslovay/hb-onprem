using HBLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HBLib.Utils.Interfaces
{
    public interface IFileRefUtils
    {
        string GetFileLink(string directory, string file, DateTime exp = default);
        string GetFileUrlFast(string path);
    }
}