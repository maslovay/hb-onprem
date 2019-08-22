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
using System.Linq;
using System.Threading.Tasks;
using HBData;
using HBLib;
using Microsoft.EntityFrameworkCore;


namespace DialogueAndSessionsNested.QuartzJobs
{
    public class DialogueAndSessionsNestedJob : IJob
    {
        private readonly ElasticClient _log;
        private readonly RecordsContext _context;
        private readonly INotificationPublisher _publisher;
        private readonly ElasticClientFactory _elasticClientFactory;

        public DialogueAndSessionsNestedJob(IServiceScopeFactory factory,
            INotificationPublisher publisher,
            ElasticClientFactory elasticClientFactory)
        {
            _context = factory.CreateScope().ServiceProvider.GetRequiredService<RecordsContext>();
            _publisher = publisher;
            _elasticClientFactory = elasticClientFactory;
        }

        public async Task Execute(IJobExecutionContext context)
        {    
            var _log = _elasticClientFactory.GetElasticClient();    
            _log.Info("Function started");
            var forLastDays = 1;
            var sessions = _context.Sessions
                .Include(p => p.ApplicationUser)
                .Include(p => p.ApplicationUser.Company)
                .Where(p => p.BegTime >= DateTime.Now.Date.AddDays(-(forLastDays+1)))                
                .ToList();
            
            var dialogues = _context.Dialogues
                .Include(p => p.ApplicationUser)
                .Where(p => p.BegTime >= DateTime.Now.Date.AddDays(-(forLastDays))
                    && !(p.StatusId == 8 || p.StatusId == 12 || p.StatusId == 13))
                .Where(p => 
                    !(sessions.Where(k => k.ApplicationUserId == p.ApplicationUserId
                        && p.BegTime >= k.BegTime 
                        && p.EndTime <= k.EndTime)
                    .Any()))
                .ToList();
            _log.Info($"Dialogues not nested in sessions count: {dialogues.Count}");                        
            
            foreach(var d in dialogues)
            {                
                CheckSessionForDialogue(d, sessions);
                _log.Info($"dialogue {d.DialogueId} passed nesting test");
            }
            _context.SaveChanges();          
        }
       
        private void CheckSessionForDialogue(Dialogue dialogue, List<Session> sessions)
        {
            var applicationUser = dialogue.ApplicationUser;
            var applicationUserId = dialogue.ApplicationUserId;            
            
            var intersectSession = sessions.Where(p => p.ApplicationUserId == applicationUserId
                    && (p.StatusId == 6 || p.StatusId == 7)
                    && ((p.BegTime <= dialogue.BegTime
                            && p.EndTime > dialogue.BegTime
                            && p.EndTime < dialogue.EndTime) 
                        || (p.BegTime < dialogue.EndTime
                            && p.BegTime > dialogue.BegTime
                            && p.EndTime >= dialogue.EndTime)
                        || (p.BegTime >= dialogue.BegTime
                            && p.EndTime <= dialogue.EndTime)
                        || (p.BegTime < dialogue.BegTime
                            && p.EndTime > dialogue.EndTime)))
                .ToList();
            
            if(dialogue is null)
            {
                _log.Info($"DialogueAndSessionNested: dialogue is null, applicationUserId: {applicationUserId}");
                return;
            }
            if (!intersectSession.Any())
            {
                var curTime = DateTime.UtcNow;
                var oldTime = DateTime.UtcNow.AddDays(-3);
                var lastSession = _context.Sessions
                        .Where(p => p.ApplicationUserId == applicationUserId 
                            && p.BegTime >= oldTime 
                            && p.EndTime <= dialogue.BegTime)
                        .OrderByDescending(p => p.BegTime)
                        .ToList()
                        .FirstOrDefault();
                if(lastSession != null && lastSession.StatusId == 6)
                {
                    lastSession.EndTime = dialogue.EndTime;
                }
                else
                {      
                    var tempSession = new Session
                    {
                        SessionId = Guid.NewGuid(),
                        ApplicationUserId = applicationUserId,
                        ApplicationUser = applicationUser,
                        BegTime = dialogue.BegTime,
                        EndTime = dialogue.EndTime,
                        StatusId = 7
                    };           
                    sessions.Add(tempSession);
                    _log.Info($"For dialogue {dialogue.DialogueId} created session {tempSession}");
                }
                return;
            } 

            var dialogueBeginSession = intersectSession.FirstOrDefault(p => p.BegTime <= dialogue.BegTime
                    && p.EndTime > dialogue.BegTime);
            var dialogueEndSession = intersectSession.FirstOrDefault(p => p.BegTime < dialogue.EndTime
                    && p.EndTime >= dialogue.EndTime);      

            if(dialogueBeginSession == null && dialogueEndSession == null)
            {
                var insideSessions = intersectSession.Where(p => p.BegTime > dialogue.BegTime
                    && p.EndTime < dialogue.EndTime).OrderBy(p => p.BegTime);
                if(insideSessions.Any())
                {
                    var lastInsideSession = insideSessions.LastOrDefault();
                    if(lastInsideSession.StatusId == 6)
                    {
                        lastInsideSession.BegTime = dialogue.BegTime;
                        lastInsideSession.EndTime = dialogue.EndTime;
                        foreach(var s in insideSessions.Where(p => p!=lastInsideSession))
                        {
                            s.StatusId = 8;
                        }
                    }
                    else
                    {                        
                        var tempSession = new Session
                        {
                            SessionId = Guid.NewGuid(),
                            ApplicationUserId = applicationUserId,
                            ApplicationUser = applicationUser,
                            BegTime = dialogue.BegTime,
                            EndTime = dialogue.EndTime,
                            StatusId = 7
                        };
                        sessions.Add(tempSession);  
                        _log.Info($"For dialogue {dialogue.DialogueId} created session {tempSession}");
                        foreach(var s in insideSessions)
                        {
                            s.StatusId = 8;
                        } 
                    }                 
                }
            }
            else if(dialogueBeginSession != null 
                && dialogueEndSession == null)
            {
                var insideSessions = intersectSession.Where(p => p.BegTime >= dialogueBeginSession.EndTime && p.EndTime < dialogue.EndTime)
                    .OrderBy(p => p.BegTime).ToList();
                
                if(insideSessions.Any())
                {
                    var lastInsideSession = insideSessions.LastOrDefault();
                    if(lastInsideSession.StatusId == 6)
                    {                        
                        dialogueBeginSession.EndTime = dialogue.BegTime;
                        lastInsideSession.BegTime = dialogue.BegTime;
                        lastInsideSession.EndTime = dialogue.EndTime;
                        foreach(var s in insideSessions.Where(p => p!=lastInsideSession))
                        {
                            s.StatusId = 8;
                        }
                    }
                    else
                    {
                        dialogueBeginSession.EndTime = dialogue.EndTime;                        
                        foreach(var s in insideSessions)
                        {
                            s.StatusId = 8;
                        }
                    }                    
                }
                else
                {
                    dialogueBeginSession.EndTime = dialogue.EndTime;
                }
            }  
            else if(dialogueBeginSession == null 
                && dialogueEndSession != null)
            {
                var insideSessions = intersectSession.Where(p => p.BegTime > dialogue.BegTime && p.EndTime <= dialogueEndSession.BegTime)
                    .OrderBy(p => p.BegTime).ToList();
                
                if(insideSessions.Any())
                {
                    dialogueEndSession.BegTime = dialogue.BegTime;       
                    foreach(var s in insideSessions)
                    {
                        s.StatusId = 8;
                    }             
                }
                else
                {
                    dialogueEndSession.BegTime = dialogue.BegTime;
                }
            }          
            else if(dialogueBeginSession != null 
                && dialogueEndSession != null 
                && dialogueBeginSession.SessionId != dialogueEndSession.SessionId)
            {
                var insideSession = intersectSession.Where(p => p.BegTime >= dialogueBeginSession.EndTime 
                        && p.EndTime <= dialogueEndSession.BegTime)
                    .ToList();
                if(insideSession.Any())
                {
                    foreach(var s in insideSession)
                    {
                        s.StatusId = 8;
                    }
                }                
                dialogueEndSession.BegTime = dialogueBeginSession.BegTime;
                dialogueBeginSession.StatusId = 8;                
            }   
        }
    }
}