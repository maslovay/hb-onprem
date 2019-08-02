using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using AlarmSender;
using HbApiTester.Settings;
using HbApiTester.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HbApiTester.Tasks
{
    public class RunnerEventArgs : EventArgs
    {
        private TestResponse Response { get; set; }
    }

    public class ApiTestsRunner : TestsRunner
    {
        public ApiTestsRunner(HbApiTesterSettings settings, IConfiguration configuration, NLog.ILogger logger, Checker checker, IServiceProvider serviceProvider) : 
            base(configuration, logger, checker, serviceProvider)
        {
            _taskFactory = new TaskFactory(settings);
            _settings = settings;
            
            Load();
        }
        
        protected override string TestsFilter => "Api";
    }
}