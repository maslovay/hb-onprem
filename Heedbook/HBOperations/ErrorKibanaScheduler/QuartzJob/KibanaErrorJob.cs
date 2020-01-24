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
            var pool = new SingleNodeConnectionPool(new Uri($"{_path.UriPath}"));
            var settings = new ConnectionSettings(pool, sourceSerializer: JsonNetSerializer.Default);
            var client = new ElasticClient(settings);
            try
            {
                var period = DateTime.UtcNow.AddHours(-4);
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
                List<SearchSetting> documents = searchRequest.Documents.OrderByDescending(x => x.Timestamp).ToList();
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

                for (int i = 0; i < documents.Count(); i++)
                {
                    documents[i].OriginalFormat = dmp.ReplaceForMainError(documents[i].OriginalFormat);
                }

                ///---remove the same OriginalFormats of errors
                for (var i = 0; i < documents.Count; i++)
                {
                    for (int j = documents.Count - 1; j > i; j--)
                    {
                        var percentageMatch = dmp.CompareText(documents[i].OriginalFormat, documents[j].OriginalFormat);
                        if (percentageMatch >= 80)
                        {
                            documents[i].Count++;
                            documents.RemoveAt(j);
                        }
                    } 
                }

                //---)

                var groupingByName = documents.GroupBy(x => x.FunctionName);
                

                var errMsg = $"<b>PERIOD: {period.ToShortTimeString()} - {DateTime.UtcNow.ToShortTimeString()}</b>";
                var head = new MessengerMessageRun()
                {
                    logText = errMsg,
                    ChannelName = "LogSender"
                };
                _publisher.Publish(head);
                var functionName = "OnPremExtractFramesFromVideo";


                errMsg = @"https://heedbookapitest.westeurope.cloudapp.azure.com/app/kibana#/discover?_g=(refreshInterval:(pause:!f,value:5000),time:(from:'"+ period + @"',mode:absolute,to:'"+ period.AddHours(4) + @"'))&_a=(columns:!(_source),filters:!(('$state':(store:appState),meta:(alias:!n,disabled:!f,index:b4623df0-36b1-11ea-9d1c-c54632dc01ff,key:LogLevel,negate:!t,params:(query:Information,type:phrase),type:phrase,value:Information),query:(match:(LogLevel:(query:Information,type:phrase)))),('$state':(store:appState),meta:(alias:!n,disabled:!f,index:b4623df0-36b1-11ea-9d1c-c54632dc01ff,key:FunctionName,negate:!f,params:(query:"+ functionName + ",type:phrase),type:phrase,value:" + functionName + "),query:(match:(FunctionName:(query:" + functionName + @",type:phrase))))),index:b4623df0-36b1-11ea-9d1c-c54632dc01ff,interval:auto,query:(language:lucene,query:''),sort:!('@timestamp',desc))";
                _log.Info($"errMsg: { errMsg}");
                var test = new MessengerMessageRun()
                {
                    logText = $"<a href=\"{errMsg}\"> View </a>",
                    ChannelName = "LogSender"
                };
                _publisher.Publish(test);


                foreach (var function in groupingByName)
                {

                    //errMsg = String.Concat(function.Select(x => "<details><summary>" +
                    //               String.Concat(x.OriginalFormat.Take(20)) + "</summary>" +
                    //               $"<b> {x.LogLevel} </b>+ {String.Concat(x.OriginalFormat.Take(150))} \n (invokationId: {x.InvocationId})\n" + "</details>"));
                    //_log.Info($"errMsg: { errMsg}");

                  

                    errMsg = String.Concat(function.Select(x => 
                                  $"<b>{x.LogLevel}({x.Count}): </b> { x.OriginalFormat.Take(150) } (last error: {x.Timestamp.ToLongTimeString()})\n\n"));
                    var message = new MessengerMessageRun()
                    {
                        logText =
                          $"<b>{function.Key}</b> \r Count: {function.Sum(x => x.Count)} \r\n>{errMsg} \r\n",
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
        }
    }
}