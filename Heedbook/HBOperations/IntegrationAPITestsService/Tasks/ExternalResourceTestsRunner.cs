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
            INotificationHandler handler) : 
            base(configuration, checker, serviceProvider, handler)
            // base(configuration, logger, checker, serviceProvider, publisher)
        {
            _taskFactory = new TaskFactory(settings);
            _settings = settings;
            
            Load();
        }
        
        protected override string TestsFilter => "ExternalResource";
    }
}