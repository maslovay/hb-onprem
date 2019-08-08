using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using AlarmSender;
using HbApiTester.Settings;
using HbApiTester.Utils;
using Microsoft.Extensions.Configuration;


namespace HbApiTester.Tasks
{
    public abstract class TestsRunner : IRunner
    {
        private readonly Checker _checker;
        private readonly IConfiguration _configuration;
        private string _token;
        private readonly List<TestTask> _tasks = new List<TestTask>(5);
        private readonly List<Sender> _senders = new List<Sender>(1);
        private readonly NLog.ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        protected RunnerSettings _settings;
        protected TaskFactory _taskFactory;
        
        public delegate void ApiEvent(TestResponse response);
        public delegate void RunnerEvent(string message);
        
        public event RunnerEvent TestRunStatus;
        public event ApiEvent ApiError;
        public event ApiEvent ApiSuccess;

        protected TestsRunner(IConfiguration configuration, NLog.ILogger logger, Checker checker, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _checker = checker;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
        }

        protected virtual string TestsFilter { get; }

        protected void Load()
        {
            Console.WriteLine($"Loading {nameof(this.GetType)}...");
            //_logger.Info( "Loading Runner...");
            
            Helper.FetchSenders(_logger, _settings, _senders, _serviceProvider);
            FetchSenderEvents();
            if (_settings.Tests?.Count == 0)
            {
                //_logger.Error( "Loading Runner... no tests in config!"); 
                Console.WriteLine("Loading Runner... no tests in config!");
                return;
            }

            foreach (var testName in _settings.Tests)
            {
                try
                {
                    // Filtering tests in order to find our category
                    if (!Regex.IsMatch(testName, $"{TestsFilter}(.*)"))
                        continue;

                    //_logger.Info($"Invoking test: {testName}");
                    MethodInfo method = null;
                    method = _taskFactory.GetType().GetMethod($"Generate{testName}Task");
                    
                    if (method == null)
                    {
                        Console.WriteLine($"No implementation for test: {testName}");
                        // _logger.Error($"No implementation for test: {testName}");
                        continue;
                    }

                    var task = (method.GetParameters().Any() ? method.Invoke(_taskFactory, 
                            new object[] {_token}) : method.Invoke(_taskFactory, new object[] {})) as TestTask;
                    _tasks.Add(task);
                }
                catch (Exception ex)
                {
                    //_logger.Error($"Exception occurred: {ex.Message}");
                    Console.WriteLine($"Exception occurred: {ex.Message}");
                }
            }
        }

        private void FetchSenderEvents()
        {
            //_logger.Info( "Loading sender events...");
            Console.WriteLine("Loading sender events...");
            if (!_senders.Any())
            {
                //_logger.Error("No senders registered!");
                Console.WriteLine("No senders registered!");
                return;
            }

            ApiError += resp =>
            {
                foreach (var sender in _senders)
                {
                    sender.Send($"ERROR: <b>{resp.TaskName}</b>: <b>{resp.ResultMessage}</b>: " +
                                $"__{resp.Timestamp.ToLocalTime().ToString(CultureInfo.InvariantCulture)}__ " +
                                $"Body: {resp.Body} URL: {resp.Url} info: {resp.Info}", "ApiTester");
                }
            };

            ApiSuccess += resp =>
            {
                foreach (var sender in _senders)
                {
                    sender.Send($"SUCCESS: <i>{resp.TaskName}: {resp.ResultMessage}</i>: " +
                                $"<i>{resp.Timestamp.ToLocalTime().ToString(CultureInfo.InvariantCulture)}</i> URL: {resp.Url}", "ApiTester");
                }
            };

            TestRunStatus += message =>
            {
                foreach (var sender in _senders)
                    sender.Send( $"{DateTime.Now.ToLocalTime().ToString(CultureInfo.InvariantCulture)} {message}", "ApiTester");
            };
        }

        public void RunTests(bool needAuth = true)
        {
            TestRunStatus?.Invoke($"Running {TestsFilter} tests started...");
            if (needAuth)
            {
                var authResponse = _checker.Check(_taskFactory.GenerateLoginTask());

                if (!authResponse.IsPositive)
                    InvokeError(authResponse);
                else
                    InvokeSuccess(authResponse);

                _token = "Bearer " + authResponse.Body;
            }

            foreach (var task in _tasks)
                DoTest(task);
            
            TestRunStatus?.Invoke($"Running {TestsFilter} tests finished...");
        }
        
        public void DoTest(TestTask task, int tries = 3, int delay = 2000)
        {
            task.Token = _token;
            var response = _checker.Check(task);
            var i = 0;
            while (!response.IsPositive &&  i < tries)
            {
                Thread.Sleep(delay);
                ++i;
                response = _checker.Check(task);
            }
            
            if (!response.IsPositive)
                InvokeError(response);
        }
        
        public void InvokeSuccess(TestResponse response)
        {
            ApiSuccess?.Invoke(response);
        }
        
        public void InvokeError(TestResponse response)
        {
            ApiError?.Invoke(response);
        }
    }
}