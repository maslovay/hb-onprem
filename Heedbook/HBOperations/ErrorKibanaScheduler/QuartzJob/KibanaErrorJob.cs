using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using HBLib;
using HBLib.Utils;
using Quartz;
using Elasticsearch.Net;
using Elasticsearch.Net.Specification.IndicesApi;
using Nest;
using Nest.JsonNetSerializer;
using Newtonsoft.Json;
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
            
            var searchRequest = client.Search<SearchSetting>(source => source
                .Source(sourcefield => sourcefield.IncludeAll()
                ).Index("logstash-2019.07.29")
                .Query(q => q.MatchAll()
                )
            );
        }

    }
}