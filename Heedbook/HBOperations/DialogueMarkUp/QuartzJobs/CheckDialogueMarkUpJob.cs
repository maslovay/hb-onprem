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
using HBData;
using Microsoft.EntityFrameworkCore;



namespace DialogueMarkUp.QuartzJobs
{
    public class CheckDialogueMarkUpJob : IJob
    {
        // private readonly ElasticClient _log;
        private readonly IGenericRepository _repository;
        private readonly RecordsContext _context;

        public CheckDialogueMarkUpJob(IServiceScopeFactory factory)
        {
            _repository = factory.CreateScope().ServiceProvider.GetRequiredService<IGenericRepository>();
            _context = factory.CreateScope().ServiceProvider.GetRequiredService<RecordsContext>();
        }
        // {
        //     _repository = factory.CreateScope().ServiceProvider.GetRequiredService<IGenericRepository>();
        //     _log = log;
        //    // _context = context;
        // }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                //_log.Info("Audion analyze scheduler started.");
                Console.WriteLine("start");
                var endTime = DateTime.UtcNow.AddMinutes(-30);

                // var frames = _context.FrameAttributes
                //     .Include(p => p.FileFrame)
                //     .Where(p => p.FileFrame.StatusNNId == )

                Console.WriteLine("end");
                //_log.Info("Audion analyze scheduler finished.");
            }
            catch (Exception e)
            {
                System.Console.WriteLine($"{e}");
            }
        }
    }
}