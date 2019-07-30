using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlarmSender;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Common
{
    public class TestResultsPublisher
    {
        private IConfiguration _configuration;
        private List<Sender> _senders = new List<Sender>(5);
        public delegate void TestFixtureStartedDelegate(string testFixtureName, string message);
        public delegate void TestResultReceivedDelegate(string testName, bool isPassed, string message, string errorMessage);
        public delegate void TestFixtureFinishedDelegate(string testFixtureName, string message);
        public event TestFixtureStartedDelegate TestFixtureStarted;
        public event TestResultReceivedDelegate TestResultReceived;
        public event TestFixtureFinishedDelegate TestFixtureFinished;

        public TestResultsPublisher()
        {
            TestFixtureStarted += (name, message) => SendTextMessage(message);
            TestFixtureFinished += (name, message) => SendTextMessage(message);
            TestResultReceived += (name, isPassed, message, errorMessage) =>
            {
                var text = $"{message} " + (isPassed ? string.Empty : errorMessage);
                SendTextMessage(text);
            };
        }

        private void SendTextMessage(string text)
        {
            foreach (var sender in _senders)
                sender.Send(text, true);
        }
        
        
        protected void PublisherSetup(IConfiguration configuration, IServiceProvider serviceProvider)
        {
            _configuration = configuration;
            FetchSenders(serviceProvider);
            var dateTime = DateTime.Now.ToLocalTime().ToString("dd.MM.yyyy hh:mm:ss: ");
            var testFixtureName = TestContext.CurrentContext.Test.ClassName;
            TestFixtureStarted?.Invoke(testFixtureName, $"{dateTime} Test fixture:  {testFixtureName}: Started");
        }

        protected void PublisherTearDown()
        {
            var dateTime = DateTime.Now.ToLocalTime().ToString("dd.MM.yyyy hh:mm:ss: ");
            var testFixtureName = TestContext.CurrentContext.Test.ClassName;
            TestFixtureFinished?.Invoke(testFixtureName, $"{dateTime} Test fixture:  {testFixtureName}: Finished");
        }

        [OneTimeSetUp]
        protected void PublisherEachTestSetup()
        {
            var dateTime = DateTime.Now.ToLocalTime().ToString("dd.MM.yyyy hh:mm:ss: ");
            var testName = TestContext.CurrentContext.Test.MethodName;
            TestResultReceived?.Invoke(testName, false, $"<pre>{dateTime} Test:  {testName} Started</pre>", string.Empty);            
        }

        [OneTimeTearDown]
        protected void PublisherEachTestTearDown()
        {
            var status = TestContext.CurrentContext.Result.Outcome.Status;
            var testName = TestContext.CurrentContext.Test.FullName;
            var message = DateTime.Now.ToLocalTime().ToString("dd.MM.yyyy hh:mm:ss: ");
            
            switch (status)
            {
                case TestStatus.Failed:
                    message += $" <pre>{testName} result: Failed</pre>";
                    break;
                case TestStatus.Passed:
                    message += $" <pre>{testName} result: Passed</pre>";
                    break;
                case TestStatus.Skipped:
                    message += $" <pre>{testName} result: Skipped</pre>";
                    break;                   
                case TestStatus.Warning:
                    message += $" <pre>{testName} result: Warning</pre>";
                    break;   
                default:
                case TestStatus.Inconclusive:
                    message += $" <pre>{testName} result: Inconclusive</pre>";
                    break;      
            }
            
            TestResultReceived?.Invoke(testName, status == TestStatus.Passed, message, 
                TestContext.CurrentContext.Result.Message);  
        }
        
        public void FetchSenders(IServiceProvider serviceProvider)
        {
                try
                {
                    if (_configuration.GetSection("AlarmSender") == null)
                        return;

                    var senderStrings = _configuration.GetSection("AlarmSender").GetChildren().Select(c => c.Key);
                    
                    // logger.Info("Loading senders...");
                    Console.WriteLine("Loading senders...");
                    if ( !senderStrings.Any())
                    {
                        Console.WriteLine("No senders in config!");
                        //logger.Error("No senders in config!");
                        return;
                    }

                    foreach (var handler in senderStrings)
                    {
                        Console.WriteLine($"Loading senders... {handler}");
                        switch (handler)
                        {
                            default:
                            case "Telegram":
                            if (_senders.All(s => s.GetType() != typeof(TelegramSender)))
                                _senders.Add((TelegramSender)serviceProvider.GetService(typeof(TelegramSender)));
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Helper.FetchSenders() exception: " + ex.Message);
                }
        }
    }
}