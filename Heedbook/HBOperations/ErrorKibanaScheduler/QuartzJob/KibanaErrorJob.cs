using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HBLib;
using HBLib.Utils;
using Quartz;
using Elasticsearch.Net;
using Microsoft.Azure.KeyVault.Models;
using Nest;
using Nest.JsonNetSerializer;
using ElasticClient = Nest.ElasticClient;
using Field = Nest.Field;
using Newtonsoft.Json;
using RabbitMqEventBus.Events;
using RabbitMqEventBus;

namespace ErrorKibanaScheduler.QuartzJob
{
    public class KibanaErrorJob : IJob
    {
        private ElasticClientFactory _elasticClientFactory;
        private MessengerClient _client;
        private UriPathOnKibana _path;
        private readonly INotificationPublisher _publisher;

        public KibanaErrorJob(
            ElasticClientFactory elasticClientFactory,
            MessengerClient client,
            UriPathOnKibana path, 
            INotificationPublisher publisher)
        {
            _path = path;
            _elasticClientFactory = elasticClientFactory;
            _client = client;
            _publisher = publisher;            
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var _log = _elasticClientFactory.GetElasticClient();
            _log.Info("Try to connect");
            var pool = new SingleNodeConnectionPool(new Uri($"{_path.UriPath}"));
            var settings = new ConnectionSettings(pool, sourceSerializer: JsonNetSerializer.Default);
            var client = new ElasticClient(settings);
            try
            {
                var period = DateTime.UtcNow.AddHours(-1);
                var searchRequest = client.Search<SearchSetting>(source => source
                    .Source(s => s
                        .Includes(i => i
                            .Fields(f => f.FunctionName,
                                f => f.OriginalFormat,
                                f => f.Timestamp,
                                f => f.FunctionName,
                                f => f.InvocationId,
                                f => f.LogLevel)))
                    .Take(10000)
                    .Index($"logstash-{DateTime.Today:yyyy.MM.dd}")
                    .Sort(x => x.Descending(a => a.Timestamp))
                    .Query(q => q
                        .Bool(m => m       
                            .Should(s => s
                                .MatchPhrase(mp => mp
                                    .Field(fd => fd.LogLevel)
                                    .Query("Fatal")), 
                                s => s
                                    .MatchPhrase(mp => mp
                                        .Field(fd => fd.LogLevel)
                                        .Query("Error")))) && q.DateRange(r=>r
                                    .Field(fd=>fd.Timestamp)
                                    .GreaterThanOrEquals(period))));
                List<SearchSetting> documents = searchRequest.Documents.ToList();
                var alarm = new MessengerMessageRun()
                {
                    logText = "<b>8000 or more error</b>",
                    ChannelName = "LogSender"
                };
                if (documents.Count >= 8000)
                {
                    _publisher.Publish(alarm);
                }
                
                var dmp = new TextCompare();
                System.Console.WriteLine($"documents count: {documents.Count}");

                ///---remove the same OriginalFormats of errors
                for (var i = 0; i < documents.Count; i++)
                {
                    for (int j = documents.Count - 1; j > i; j--)
                    {
                        var percentageMatch = dmp.CompareText(documents[i].OriginalFormat, documents[j].OriginalFormat);
                        if (percentageMatch >= 70)
                        {
                            documents[i].Count++;
                            documents.RemoveAt(j);
                        }
                    } 
                }

                var groupingByName = documents.GroupBy(x => x.FunctionName);

                var errMsg = $"PERIOD: {period.ToShortTimeString()} - {DateTime.UtcNow.ToShortTimeString()}";
                foreach (var function in groupingByName)
                {
                    errMsg += String.Concat(function.Select(x => "<details><summary>"+ 
                                    String.Concat(x.OriginalFormat.Take(100))+ "</summary>"+ 
                                    $"<b> {x.LogLevel} </b>+ {x.OriginalFormat} \n (invokationId: {x.InvocationId})\n" + "</details>"));
                    var message = new MessengerMessageRun()
                    {
                        logText =
                          $"<b>{function.Key}</b> \r Count: {function.Sum(x => x.Count)} \r\n<b>Errors:</b>\n {errMsg} \r\n",
                        ChannelName = "LogSender"
                    };
                    _publisher.Publish(message);
                    errMsg = "";
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine($"Exception: \n{e}");
                _log.Fatal($"{e}");
            }

            _log.Info("Function finished");
        }
    }
}