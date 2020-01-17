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
                                    .GreaterThanOrEquals(DateTime.UtcNow.AddHours(-4)))));
                System.Console.WriteLine(JsonConvert.SerializeObject(searchRequest));
                var documents = searchRequest.Documents.ToList();
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
                for (var i = 0; i < documents.Count; i++)
                {
                    var count = 1;
                    for (int j = documents.Count - 1; j > i; j--)
                    {
                        var percentageMatch = dmp.CompareText(documents[i].OriginalFormat, documents[j].OriginalFormat);
                        if (percentageMatch >= 80)
                        {
                            count += 1;
                            documents.RemoveAt(j);
                        }
                    }

                    var message = new MessengerMessageRun()
                    {
                        logText =
                            $"<b>FunctionName:</b>{documents[i].FunctionName} \r\n<b>LogLevel:</b> {documents[i].LogLevel} \r\n<b>Count:</b> {count} \r\n<b>ErrorHead:</b> {String.Concat(documents[i].OriginalFormat.Take(200))} \r\n<b>InvocationId:</b> {documents[i].InvocationId}",
                        ChannelName = "LogSender"
                    };
                    _publisher.Publish(message);
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