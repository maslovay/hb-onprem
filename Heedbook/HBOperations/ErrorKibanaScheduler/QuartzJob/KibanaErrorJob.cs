using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HBLib;
using HBLib.Utils;
using Quartz;
using Elasticsearch.Net;
using Nest;
using Nest.JsonNetSerializer;
using Attachment = HBLib.Utils.Attachment;
using ElasticClient = Nest.ElasticClient;


namespace ErrorKibanaScheduler.QuartzJob
{
    public class KibanaErrorJob : IJob
    {
        private ElasticClientFactory _elasticClientFactory;
        private SlackClient _slackClient;

        public KibanaErrorJob(ElasticClientFactory elasticClientFactory,
            SlackClient slackClient)
        {
            _elasticClientFactory = elasticClientFactory;
            _slackClient = slackClient;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var _log = _elasticClientFactory.GetElasticClient();
            _log.Info("Try to connect");
            var pool = new SingleNodeConnectionPool(new Uri("http://13.74.249.78:9200"));
            var settings = new ConnectionSettings(pool, sourceSerializer: JsonNetSerializer.Default);
            var client = new ElasticClient(settings);
            try
            {
                var searchRequest = client.Search<SearchSetting>(source => source
                    .Source(s => s
                        .Includes(i => i
                            .Fields(f => f.FunctionName, f => f.OriginalFormat, f => f.Timestamp)))
                    .Take(10000)
                    .Index($"logstash-{DateTime.Today:yyyy.MM.dd}")
                    .Query(q => q.Match(m => m.Field(f => f.LogLevel).Query("Fatal"))));

                var documents = searchRequest.Documents
                    .Where(item => item.Timestamp >= DateTime.UtcNow.AddHours(-2)).ToList();

                var dmp = new TextCompare();

                for (var i = 0; i < documents.Count; i++)
                {
                    var payload = new Payload()
                    {
                        Attachments = new List<Attachment>()
                    };
                    var count = 1;        
                    var attachment = new Attachment()
                    {
                        Color = "#FF0000",
                        Title = $"{documents[i].FunctionName}",
                        Text = $"{documents[i].OriginalFormat}"
                    };
                    for (int j = documents.Count - 1; j > i; j--)
                    {
                        var percentageMatch = dmp.CompareText(documents[i].OriginalFormat, documents[j].OriginalFormat);
                        if (percentageMatch >= 80)
                        {
                            count += 1;
                            documents.RemoveAt(j);
                        }
                    }
                    attachment.AuthorName = count.ToString();
                    payload.Attachments.Add(attachment);
                    _slackClient.PostMessage(payload);
                }
            }
            catch (Exception e)
            {
                _log.Fatal($"{e}");
            }
            _log.Info("Function finished");
        }
    }
}