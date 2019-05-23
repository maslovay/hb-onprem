using AsrHttpClient;
using HBData.Models;
using HBData.Repository;
using HBLib.Model;
using HBLib.Utils;
using LemmaSharp;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Quartz;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace DialogueMarkUp.QuartzJobs
{
    public class CheckDialogueMarkUpJob : IJob
    {
        private readonly ElasticClient _log;
        private readonly IGenericRepository _repository;

        public CheckDialogueMarkUpJob(IServiceScopeFactory factory,
            ElasticClient log)
        {
            _repository = factory.CreateScope().ServiceProvider.GetRequiredService<IGenericRepository>();
            _log = log;
        }

        public async Task Execute(IJobExecutionContext context)
        {
           System.Console.WriteLine("start");
           System.Console.WriteLine("end");
        }
    }
}