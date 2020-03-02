using System;

namespace HBLib
{
    public class SftpSettings
    {
        public String Host { get; set; }

        public Int32 Port { get; set; }

        public String UserName { get; set; }

        public String Password { get; set; }

        public String DestinationPath { get; set; }

        public String DownloadPath { get; set; }
    }
}