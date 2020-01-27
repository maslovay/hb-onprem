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
    public class DelayedTestsRunner : TestsRunner
    {
        public DelayedTestsRunner(
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

            Load();
        }

        protected override string TestsFilter => "Delayed";
    }
}