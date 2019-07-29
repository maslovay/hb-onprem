using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace QuartzExtensions.Jobs
{
    public class UserWeeklyData
    {
        public string Authorization{get; set;}
        public string UserFullName{get; set;}
	    public string UserName{get; set;}
        public string Password{get; set;}
        public bool Remember{get; set;}
    }
}

