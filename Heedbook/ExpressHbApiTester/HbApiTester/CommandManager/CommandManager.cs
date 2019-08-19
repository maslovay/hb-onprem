using System;
using System.Collections.Generic;
using AlarmSender;
using HbApiTester.Settings;
using HbApiTester.Tasks;
using HbApiTester.Utils;
using Microsoft.Extensions.Configuration;
using NLog;

namespace HbApiTester
{
    public class CommandManager
    {
        private readonly ApiTestsRunner _apiTestsRunner;
        private readonly ExternalResourceTestsRunner _externalResourceTestsRunner;
        private readonly List<Sender> _senders = new List<Sender>(5);
        private readonly ILogger _logger;
        private readonly HbApiTesterSettings _settings;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private bool _started = false;
        
        
        public CommandManager(ApiTestsRunner apiTestsRunner, ExternalResourceTestsRunner externalResourceTestsRunner, 
            ILogger logger, HbApiTesterSettings settings, IConfiguration configuration, IServiceProvider serviceProvider)
        {
            _apiTestsRunner = apiTestsRunner;
            _externalResourceTestsRunner = externalResourceTestsRunner;
            _configuration = configuration;
            _settings = settings;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public void Start()
        {
            Console.WriteLine("CommandManager Start()");
            if (_started) 
                return;
            Console.WriteLine("CommandManager Start() loading senders...");            
            Helper.FetchSenders(_logger, _settings, _senders, _serviceProvider);
            Console.WriteLine("CommandManager Start() _sender.Count=" + _senders.Count);
            foreach (var sender in _senders)
            {
                Console.WriteLine("CommandManager Start() sender: " + sender.GetType().Name);
                sender.CommandReceived += CommandWorker;
            }

            _started = true;
        }

        public void RunCommand(string command)
            => CommandWorker(command);
        
        private void CommandWorker(string command)
        {
            Console.WriteLine($"CommandWorker(): command '{command}'");
            switch (command.Trim())
            {
                case "api_tests":
                    Console.WriteLine($"CommandWorker(): running {command}");
                    _apiTestsRunner.RunTests();
                    break;
                case "ext_res_tests":
                    Console.WriteLine($"CommandWorker(): running {command}");
                    _externalResourceTestsRunner.RunTests(false);
                    break;
            }
        }
    }
}