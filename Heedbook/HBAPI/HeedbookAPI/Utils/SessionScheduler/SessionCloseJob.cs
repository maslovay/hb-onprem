using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HBData;
using HBLib;
using Quartz;
using Microsoft.Extensions.DependencyInjection;
using HBData.Models;
using UserOperations.Utils;

namespace UserOperations.Services.Scheduler
{
    public class SessionCloseJob : IJob
    {
        private RecordsContext _context;
        private DBOperations _dbOperation;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ElasticClientFactory _elasticClientFactory;

        public SessionCloseJob(IServiceScopeFactory scopeFactory, ElasticClientFactory elasticClientFactory)
        {
            _scopeFactory = scopeFactory;
            _elasticClientFactory = elasticClientFactory;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                const int OPEN = 6;
                const int CLOSE = 7;
                var _log = _elasticClientFactory.GetElasticClient();
                try
                {
                    _log.Info("Session close start");
                    _context = scope.ServiceProvider.GetRequiredService<RecordsContext>();
                    _dbOperation = scope.ServiceProvider.GetRequiredService<DBOperations>();

                    DateTime today = DateTime.Now.Date;
                    var sessionsForClose = _context.Sessions.Where(x => x.BegTime.Date < today && x.StatusId == OPEN).ToList();
                    CloseSessions(sessionsForClose, CLOSE);
                    _context.SaveChanges();
                }
                catch (Exception e)
                {
                    _log.Fatal($"{e}");
                    throw;
                }
            }
        }

        //---PRIVATE---
        private bool CloseSessions(List<Session> sessions, int CLOSE)
        {
            if (sessions.Count() == 0) return false;
            sessions.Select(p =>
            {
                p.SessionId = p.SessionId;
                p.StatusId = CLOSE;
                p.EndTime = FindNextSessionBegTime(p.BegTime, p.EndTime, p.DeviceId);
                return p;
            }).ToList();
            return true;
        }

        private DateTime FindNextSessionBegTime(DateTime begTime, DateTime endTime, Guid deviceId)
        {
            DateTime endOfADay = begTime.Date.AddDays(1).AddSeconds(-1);
            DateTime? begOfNextSession = _context.Sessions
                        .Where(p => p.DeviceId == deviceId && p.BegTime > begTime)
                        .OrderByDescending(p => p.BegTime)
                        .Select(p => p.BegTime)?.FirstOrDefault();
            DateTime endOfSession = (begOfNextSession != default(DateTime) && begOfNextSession < DateTime.UtcNow) ? (DateTime)begOfNextSession : DateTime.UtcNow;
            return endOfSession < endOfADay ? endOfSession : endOfADay;
        }
    }
}