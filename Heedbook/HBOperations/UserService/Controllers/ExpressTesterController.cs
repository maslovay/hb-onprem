using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;

namespace UserService.Controllers
{
    [Route("user/[controller]")]
    [ApiController]
    public class ExpressTesterController : ControllerBase
    {
        private readonly INotificationPublisher _publisher; 
        public ExpressTesterController(INotificationPublisher publisher)
        {
            _publisher = publisher;
        }      
        
        [HttpPost("[action]")]
        public void PublishUnitTestResults([FromBody]PublishUnitTestResultsModel model)
        {
            PublishUnitTestResults(model.TrxTextBase64);
        }
        private void PublishUnitTestResults(string textBase64)
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

            var messageText =
                $"TestRun for TestFixture \"{testFixture}\" started: {startDateTime} finished: {finishDateTime}";

            foreach (var res in testResults)
            {
                var testId = res.Attribute("testId")?.Value;
                var testXml = testDefs.FirstOrDefault(elem =>
                    elem.Name.LocalName == "UnitTest" && elem.Attributes().Any(a => a.Name.LocalName == "id" && a.Value == testId));

                var testName = testXml?.Attribute("name")?.Value;
                var testOutcome = res.Attribute("outcome");

                messageText += $"<pre> {testName} : {testOutcome} </pre>";
            }
            var message = new MessengerMessageRun()
            {
                logText = messageText,
                ChannelName = "IntegrationTester",
            };
            
            System.Console.WriteLine($"{JsonConvert.SerializeObject(message)}");
            _publisher.Publish(message);            
        }
    }
    public class PublishUnitTestResultsModel
    {
        public string TrxTextBase64 { get; set; }
    }
}