﻿using HBLib;
using HBLib.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            return $"https://{_sftpSettings.Host}/{path}";
        }

        public string GetFileLink(string directory, string file, DateTime exp = default)
        {
            return $"https://{_sftpSettings.Host}/{directory}/{file}";
        }
    }
}