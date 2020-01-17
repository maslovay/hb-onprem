using HBLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UserOperations.Utils.CommonOperations
{
    public class FileRefUtils
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
            return $"http://{_sftpSettings.Host}/{path}";
        }

        public string GetFileLink(string directory, string file, DateTime exp = default)
        {
            return $"http://{_sftpSettings.Host}/{directory}/{file}";
        }
    }
}
