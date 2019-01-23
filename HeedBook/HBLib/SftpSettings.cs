﻿using System;
using System.Collections.Generic;
using System.Text;

namespace HBLib
{
    public class SftpSettings
    {
        public String Host { get; set; }
        
        public Int32 Port { get; set; }

        public String UserName { get; set; }

        public String Password { get; set; }

        public String DestinationPath { get; set; }
    }
}
