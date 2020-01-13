using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using HBData.Models;
using HBData;
using UserOperations.Utils;
using HBLib.Utils;
using UserOperations.Models.Session;
using HBData.Repository;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace UserOperations.Services
{
    public class SessionService
    {
       // private readonly RecordsContext _context;
        private readonly ElasticClient _log;
        private readonly IGenericRepository _repository;
        private readonly LoginService _loginService;

        public SessionService(
           // RecordsContext context,
            ElasticClient log,
            IGenericRepository repository,
            LoginService loginService
            )
        {
            //_context = context;
            _log = log;
            _repository = repository;
            _loginService = loginService;
        }

        public Response SessionStatus(SessionParams data, string Authorization)
        {
            if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                throw new Exception("Token wrong");
            Guid.TryParse(userClaims["companyId"], out Guid companyIdInToken);
            var company = _repository.GetWithIncludeOne<Company>(x => x.CompanyId == companyIdInToken, x => x.Devices, x => x.ApplicationUser);
            if(!company.Devices.Any( x => x.DeviceId == data.DeviceId))
                throw new Exception("The device is not owned by the company");
            if (data.ApplicationUserId != null && !company.ApplicationUser.Any(x => x.Id == data.ApplicationUserId))
                throw new Exception("User does not belong to the company");

            const int OPEN = 6;
            const int CLOSE = 7;
            _log.Info($"session on device {data.DeviceId} try {data.Action}");
            var response = new Response();
            if (String.IsNullOrEmpty(data.DeviceId.ToString()))
            {
                response.Message = "DeviceId is empty";
                return response;
            }
            var curTime = DateTime.UtcNow;
            var oldTime = DateTime.UtcNow.AddHours(-24);

            var oldSessions = _repository.GetAsQueryable<Session>().Where(p => p.DeviceId == data.DeviceId && p.StatusId == OPEN).ToList();
            var lastSession = oldSessions.OrderByDescending(p => p.BegTime).FirstOrDefault();


            if (data.Action == "open")
            {
                if (lastSession != null && lastSession.BegTime > oldTime && lastSession.ApplicationUserId == data.ApplicationUserId)//---means active session on this device
                {
                    response.Message = $"Can't open session. Already opened";
                    return response;
                }

                //---CLOSE OLD SESSIONS ON THIS DEVICE--- 
                if (CloseSessions(oldSessions, CLOSE, curTime))
                _repository.Save();

                //---CLOSE OLD SESSION FROM THIS USER
                if (data.ApplicationUserId != null)
                    CloseSessions(_repository.GetAsQueryable <Session>().Where(p => p.ApplicationUserId == data.ApplicationUserId && p.StatusId == OPEN).ToList(), CLOSE, curTime);

                //---OPEN (CREATE) NEW SESSION
                CreateNewSession(data, OPEN);
                _repository.Save();
                response.Message = "Session successfully opened";
                _log.Info($"Session successfully opened on device: {data.DeviceId}");
            }

            if (data.Action == "close")
            {
                //---CLOSE OLD SESSIONS ON THIS DEVICE--- 
                if (CloseSessions(oldSessions, CLOSE, curTime))
                    _repository.Save();

                if (lastSession == null)
                {
                    response.Message = $"Can't close session. Not opened";
                    return response;
                }
                response.Message = "session successfully closed";
                _log.Info($"session successfully closed for device: {data.DeviceId}");
            }
            return response;
        }

        public object SessionStatus(Guid deviceId, Guid? applicationUserId, string Authorization)
        {
            if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                throw new Exception("Token wrong");
            Guid.TryParse(userClaims["companyId"], out Guid companyIdInToken);
            var company = _repository.GetWithIncludeOne<Company>(x => x.CompanyId == companyIdInToken, x => x.Devices, x => x.ApplicationUser);
            if (!company.Devices.Any(x => x.DeviceId == deviceId))
                throw new Exception("The device is not owned by the company");
            if (applicationUserId != null && !company.ApplicationUser.Any(x => x.Id == applicationUserId))
                throw new Exception("User does not belong to the company");

            var session = _repository.GetAsQueryable<Session>()
                    .Where(p => p.DeviceId == deviceId && p.ApplicationUserId == applicationUserId)
                        ?.OrderByDescending(p => p.BegTime)
                        ?.FirstOrDefault();
            var result = new { session?.BegTime, session?.StatusId };
            return result;
        }      

        public string AlertNotSmile([FromBody] Guid applicationUserId)
        {
            //var response = new Response();
            if (String.IsNullOrEmpty(applicationUserId.ToString())) 
            {
                // response.Message  "ApplicationUser is empty";
                // return response.Message;
                return "ApplicationUser is empty";
            }

            var newAlert = new Alert
            {
                CreationDate = DateTime.UtcNow,
                ApplicationUserId = applicationUserId,
                AlertTypeId = _repository.GetAsQueryable<AlertType>().FirstOrDefault(x => x.Name == "client does not smile").AlertTypeId
            };
            _repository.Create<Alert>(newAlert);
            _repository.Save();
            // response.Message = "Alert saved";
            // return response.Message;
            return "Alert saved";            
        }

        //---PRIVATE---
        private bool CloseSessions(List<Session> sessions, int CLOSE, DateTime curTime)
        {
            if (sessions.Count() == 0) return false;
            sessions.Select(p =>
                  {
                      p.SessionId = p.SessionId;
                      p.StatusId = CLOSE;
                      p.EndTime = p.BegTime.AddHours(24) > curTime ? curTime : p.BegTime.AddHours(24);
                      return p;
                  }).ToList();
            return true;
        }

        private void CreateNewSession(SessionParams data, int OPEN)
        {
            var session = new Session
            {
                BegTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.Date.AddDays(1).AddSeconds(-1),
                ApplicationUserId = data.ApplicationUserId,
                DeviceId = data.DeviceId,
                StatusId = OPEN,
                IsDesktop = (bool)data.IsDesktop
            };
            _repository.Create<Session>(session);
        }
    }    
}