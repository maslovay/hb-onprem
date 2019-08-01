using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlarmSender;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Xml.Linq;
using NUnit.Engine;
using NUnit.Framework;
using Telegram.Bot.Types;
using File = System.IO.File;

namespace Common
{
    public class TestResultsPublisher
    {
        private IConfiguration _configuration;
        private List<Sender> _senders = new List<Sender>(5);

        protected TestResultsPublisher()
        {
        }

        private void SendTextMessage(string text)
        {
            foreach (var sender in _senders)
                sender.Send(text, true);
        }

        public void Publish(string pathToTrx)
        {
            TestContext.Out.WriteLine("Publishing test results...");
            if (!File.Exists(pathToTrx))
            {
                TestContext.Out.WriteLine($"Can't find TRX file {pathToTrx}");
                return;
            }

            var text = File.ReadAllText(pathToTrx);

            TestContext.Out.WriteLine("Publishing test results: " + text);
            var trxDoc = XDocument.Parse(text);

            if (trxDoc.Root == null)
                return;

            var testRunElement = trxDoc.Root;
            var startDateTime = testRunElement.Elements().FirstOrDefault(elem => elem.Name.LocalName == "Times")
                ?.Attribute("start")
                ?.Value;
            var finishDateTime = testRunElement.Elements().FirstOrDefault(elem => elem.Name.LocalName == "Times")
                ?.Attribute("finish")
                ?.Value;

            var testResults = testRunElement.Elements().FirstOrDefault(elem => elem.Name.LocalName == "Results")
                ?.Elements()
                .Where(elem => elem.Name.LocalName == "UnitTestResult").ToArray();

            var testDefs = testRunElement.Elements().FirstOrDefault(elem => elem.Name.LocalName == "TestDefinitions")
                ?.Elements()
                .Where(elem => elem.Name.LocalName == "UnitTest").ToArray();

            if (testResults == null || !testDefs.Any())
            {
                TestContext.Out.WriteLine($"Can't find testResults for a TestRun!");
                return;
            }

            if (!testDefs.Any())
            {
                TestContext.Out.WriteLine($"Can't find testDefinitions for a TestRun!");
                return;
            }

            var testFixture = testDefs.FirstOrDefault()?.Elements().FirstOrDefault(elem => elem.Name.LocalName == "TestMethod")?.Attribute("className")?.Value;

            var message =
                $"TestRun for TestFixture \"{testFixture}\" started: {startDateTime} finished: {finishDateTime}";

            foreach (var res in testResults)
            {
                var testId = res.Attribute("testId")?.Value;
                var testXml = testDefs.FirstOrDefault(elem =>
                    elem.Name.LocalName == "UnitTest" && elem.Attributes().Any(a => a.Name.LocalName == "id" && a.Value == testId));

                var testName = testXml?.Attribute("name")?.Value;
                var testOutcome = res.Attribute("outcome");

                message += $"<pre> {testName} : {testOutcome} </pre>";
            }

            SendTextMessage(message);
        }
        
        protected void PublisherSetup(IConfiguration configuration, IServiceProvider serviceProvider)
        {
            _configuration = configuration;
            FetchSenders(serviceProvider);
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
                if (!senderStrings.Any())
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
                                _senders.Add((TelegramSender) serviceProvider.GetService(typeof(TelegramSender)));
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