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
using HBLib.Utils;

namespace SessionCloseSchedule
{
    public class SessionCloseJob : IJob
    {
        private readonly RecordsContext _context;
        private readonly INotificationPublisher _publisher;
        private readonly ElasticClient _log;

        public SessionCloseJob(IServiceScopeFactory factory,
            ElasticClient log)
        {
            _context = factory.CreateScope().ServiceProvider.GetRequiredService<RecordsContext>();
            _log = log;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                const int OPEN = 6;
                const int CLOSE = 7;

                DateTime today = DateTime.Now.Date;
                var sessionsForClose = _context.Sessions.Where(x => x.BegTime.Date < today && x.StatusId == OPEN).ToList();
                CloseSessions(sessionsForClose, CLOSE);
                _context.SaveChanges();

                _log.Info($"Closed {sessionsForClose.Count()} sessions");
            }
            catch (Exception e)
            {
                _log.Fatal($"Exception while executing SessionClose occured {e}");
                throw;
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