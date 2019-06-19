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
using HBLib;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;


namespace SessionStatusSchedule.QuartzJobs
{
    public class CheckSessionStatusScheduleJob : IJob
    {
        private ElasticClient _log;
        private RecordsContext _context;
        private readonly IServiceScopeFactory _factory;

        private readonly ElasticClientFactory _elasticClientFactory;

        public CheckSessionStatusScheduleJob(IServiceScopeFactory factory,
            ElasticClientFactory elasticClientFactory)
        {
            _factory = factory;
            _elasticClientFactory = elasticClientFactory;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            System.Console.WriteLine("function started");
            using (var scope = _factory.CreateScope())
            {
                _log = _elasticClientFactory.GetElasticClient();
                _log.Info("Function started");
                try
                {
                    var dt = DateTime.UtcNow.AddHours(-1);

                    _context = scope.ServiceProvider.GetRequiredService<RecordsContext>();
                    var activeSessions = _context.Sessions.Where(p => p.StatusId == 6 && p.BegTime <= dt).ToList();
                    var activeUsers = activeSessions.Select(p => p.ApplicationUserId).ToList();

                    var lastVideosUsers = _context.FileVideos
                        .Where(p => p.EndTime >= dt && activeUsers.Contains(p.ApplicationUserId))
                        .Select(p => p.ApplicationUserId)
                        .Distinct().ToList();
                    var usersNotActive = activeUsers
                        .Except(lastVideosUsers).ToList();

                    activeSessions.Where(p => usersNotActive.Contains(p.ApplicationUserId))
                        .ToList()
                        .ForEach(p => { 
                            p.StatusId = 7;
                            p.EndTime = DateTime.UtcNow;
                        });
                    _context.SaveChanges();
                    _log.Info("Function DialogueMarkUp finished");                
                }
                catch (Exception e)
                {
                    System.Console.WriteLine($"Exception occured {e}");
                    _log.Fatal($"Exception while executing DialogueMarkUp occured {e}");
                    throw;
                }
            }
        }
    }
}