using HBLib.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace HBLib
{
    public class ElasticClientFactory
    {
        private readonly ElasticSettings _elasticSettings;
        public ElasticClientFactory(ElasticSettings settings)
        {
            _elasticSettings = settings;
        }

        public ElasticClient GetElasticClient()
        {
            return new ElasticClient(_elasticSettings);
        }
    }
}
