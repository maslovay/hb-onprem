using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using IntegrationAPITestsService.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Notifications.Base;
using RabbitMqEventBus;

namespace IntegrationAPITestsService.Tasks
{
    public class ExternalResourceTestsRunner : TestsRunner
    {
        public ExternalResourceTestsRunner(
            HbApiTesterSettings settings, 
            IConfiguration configuration, 
            //NLog.ILogger logger, 
            Checker checker, 
            IServiceProvider serviceProvider,
            INotificationPublisher publisher) : 
            base(configuration, checker, serviceProvider)
            // base(configuration, logger, checker, serviceProvider, publisher)
        {
            _taskFactory = new TaskFactory(settings);
            _settings = settings;
            _publisher = publisher;
            Load();
        }
        
        protected override string TestsFilter => "ExternalResource";
    }
}