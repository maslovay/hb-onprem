using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HBLib;
using HBLib.Utils;
using Quartz;
using Elasticsearch.Net;
using Elasticsearch.Net.Specification.IndicesApi;
using ErrorKibanaScheduler.DiffMathPath;
using Nest;
using Nest.JsonNetSerializer;
using Newtonsoft.Json;
using ElasticClient = Nest.ElasticClient;
using Field = Nest.Field;

namespace ErrorKibanaScheduler.QuartzJob
{
    public class KibanaErrorJob : IJob
    {
        private ElasticClientFactory _elasticClientFactory;
        private SlackClient _slackClient;
        private MatchSetting _matchSetting;

        public KibanaErrorJob(ElasticClientFactory elasticClientFactory,
            SlackClient slackClient,
            MatchSetting matchSetting)
        {
            _matchSetting = matchSetting;
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

            var searchRequest = client.Search<SearchSetting>(source => source
                .Source(s => s
                    .Includes(i => i
                        .Fields(f => f.FunctionName, f => f.OriginalFormat, f => f.Timestamp)))
                .Take(10000)
                .Index($"logstash-{DateTime.Today:yyyy.MM.dd}")
                .Query(q => q.Match(m => m.Field(f => f.LogLevel).Query("Fatal"))));

            var documents = searchRequest.Documents
                .Where(item => item.Timestamp >= DateTime.UtcNow.AddHours(-15)).ToList();

            var dmp = new diff_match_patch();

            var list<MatchSetting>
            for (var i = 0; i < documents.Count; i++)
            {
                for (int j = documents.Count - 1; j > i; j--)
                {
                    dmp.Match_Threshold = 0.3f;
                    var matchMain = dmp.match_main(documents[i].OriginalFormat, documents[j].OriginalFormat, 1000);
                   
                }
            }
        }
    }
}