using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using IntegrationAPITestsService.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMqEventBus;

namespace IntegrationAPITestsService.Tasks
{
    public class RunnerEventArgs : EventArgs
    {
        private TestResponse Response { get; set; }
    }

    public class ApiTestsRunner : TestsRunner
    {
        public ApiTestsRunner(
            HbApiTesterSettings settings, 
            IConfiguration configuration, 
            //NLog.ILogger logger, 
            Checker checker, 
            IServiceProvider serviceProvider,
            INotificationPublisher publisher) : 
            base(configuration, checker, serviceProvider, publisher)
            // base(configuration, logger, checker, serviceProvider, publisher)
        {
            _taskFactory = new TaskFactory(settings);
            _settings = settings;
            System.Console.WriteLine($"TestsCount: {_settings.Tests?.Count}");
            Load();
        }
        
        protected override string TestsFilter => "Api";
    }
}