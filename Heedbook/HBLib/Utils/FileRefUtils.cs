using HBLib;
using HBLib.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using  HBLib.Utils.Interfaces;

namespace HBLib.Utils
{
    public class FileRefUtils : IFileRefUtils
    {
        private readonly SftpSettings _sftpSettings;
        public FileRefUtils(SftpSettings sftpSettings)
        {
            _sftpSettings = sftpSettings;
        }

        /// <summary>
        /// Get url to file. 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public string GetFileUrlFast(String path)
        {
            return $"https://heedbook-files.open.ru/{path}";
        }

        public string GetFileLink(string directory, string file, DateTime exp = default)
        {
            return $"https://heedbook-files.open.ru/{directory}/{file}";
        }
    }
}
