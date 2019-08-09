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
    public class ResultsPublisher
    {
        private readonly List<Sender> _senders = new List<Sender>(1);
        private readonly NLog.ILogger _logger;
        private readonly IConfiguration _configuration;
        
        public ResultsPublisher(HbApiTesterSettings settings, IConfiguration configuration, IServiceProvider serviceProvider)
        {
           // _logger = logger;
            _configuration = configuration;
            
            Helper.FetchSenders(_logger, settings, _senders, serviceProvider);
        }

        public void PublishUnitTestResults(string textBase64)
        {
            var bytes = Convert.FromBase64String(textBase64);
            var text = Encoding.UTF8.GetString(Convert.FromBase64String(textBase64));
            if ( text[0] == 65279 )
                text = text.Remove(0, 1);

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
                Console.Out.WriteLine($"Can't find testResults for a TestRun!");
                return;
            }

            if (!testDefs.Any())
            {
                Console.WriteLine($"Can't find testDefinitions for a TestRun!");
                return;
            }

            var testFixture =
                testDefs.FirstOrDefault()?.Elements().FirstOrDefault(elem => elem.Name.LocalName == "TestMethod")?.Attribute("className")?.Value;

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
        
        private void SendTextMessage(string text)
        {
            foreach (var sender in _senders)
                sender.Send(text, "IntegrationTester", true);
        }
    }
}