using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using AlarmSender;
using HbApiTester.Settings;
using HbApiTester.Utils;
using Microsoft.Extensions.Configuration;

namespace HbApiTester
{
    public class LogsPublisher
    {
        private readonly List<Sender> _senders = new List<Sender>(1);
        private readonly NLog.ILogger _logger;
        private readonly IConfiguration _configuration;
        
        public LogsPublisher(HbApiTesterSettings settings, IConfiguration configuration, IServiceProvider serviceProvider)
        {
           // _logger = logger;
            _configuration = configuration;
            
            Helper.FetchSenders(_logger, settings, _senders, serviceProvider);
        }

        public void PublishLogs(string logsText) 
            => SendTextMessage(logsText);
        
        
        private void SendTextMessage(string text)
        {
            foreach (var sender in _senders)
                sender.Send(text, "LogSender", true);
        }
    }
}