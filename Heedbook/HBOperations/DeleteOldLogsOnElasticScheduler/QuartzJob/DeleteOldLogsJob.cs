using System;
using System.Threading.Tasks;
using Elasticsearch.Net;
using ErrorKibanaScheduler;
using HBLib;
using Nest;
using Nest.JsonNetSerializer;
using Quartz;
using ElasticClient = Nest.ElasticClient;

namespace DeleteOldLogsOnElasticScheduler.QuartzJob
{
    public class DeleteOldLogsJob : IJob
    {
        private ElasticClientFactory _elasticClientFactory;
        private UriPathOnKibana _uriPath;

        public DeleteOldLogsJob(ElasticClientFactory elasticClientFactory,
            UriPathOnKibana uriPath)
        {
            _uriPath = uriPath;
            _elasticClientFactory = elasticClientFactory;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var _log = _elasticClientFactory.GetElasticClient();

            var pool = new SingleNodeConnectionPool(new Uri($"{_uriPath.UriPath}"));
            var settings = new ConnectionSettings(pool, sourceSerializer: JsonNetSerializer.Default);
            var client = new ElasticClient(settings);
            
            try
            {
                var searchRequest = await client.DeleteByQueryAsync<SearchSetting>(del => del
                    .AllIndices()
                    .Query(q => q.DateRange(r => r
                        .Field(fd => fd.Timestamp)
                        .LessThanOrEquals(DateTime.UtcNow.AddDays(-14)))));
            }
            catch (Exception e)
            {
                _log.Fatal($"{e}");
            }
        }
    }
}