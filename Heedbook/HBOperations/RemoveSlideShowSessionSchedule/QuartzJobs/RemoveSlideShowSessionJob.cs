using HBData.Models;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using RabbitMqEventBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HBData;
using HBLib;
using HBData.Repository;
using Newtonsoft.Json;

namespace RemoveSlideShowSession
{
    public class RemoveSlideShowSessionJob : IJob
    {
        private readonly IGenericRepository _repository;
        private readonly ElasticClientFactory _elasticClientFactory;

        public RemoveSlideShowSessionJob(IServiceScopeFactory factory,
            ElasticClientFactory elasticClientFactory,
            IGenericRepository repository)
        {
            _repository = repository;
            _elasticClientFactory = elasticClientFactory;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var _log = _elasticClientFactory.GetElasticClient();
            try
            {
                System.Console.WriteLine($"started");
                var counter = 0;
                
                while(true)
                {
                    try
                    {
                        var slideShowSessions = _repository.GetAsQueryable<SlideShowSession>()
                            .Where(p => p.DialogueId == null
                                && p.BegTime <= DateTime.Now.AddDays(-14))
                            .Take(1000)
                            .ToList();

                        if(slideShowSessions == null || slideShowSessions.Count == 0)
                            break;

                        _repository.Delete<SlideShowSession>(slideShowSessions);
                        _repository.Save();
                        System.Console.WriteLine($"{counter++} {slideShowSessions.First().SlideShowSessionId}");
                    }
                    catch(Exception e)
                    {
                        System.Console.WriteLine($"{e}");
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                _log.Fatal($"Exception while executing SessionClose occured {e}");
                throw;
            }
        }
    }
}