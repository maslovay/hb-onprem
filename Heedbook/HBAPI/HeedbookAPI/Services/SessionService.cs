using System;
using System.Linq;
using HBData.Models;
using HBLib.Utils;
using UserOperations.Models.Session;
using HBData.Repository;
using System.Collections.Generic;

namespace UserOperations.Services
{
    public class SessionService
    {
        private readonly ElasticClient _log;
        private readonly IGenericRepository _repository;
        private readonly LoginService _loginService;

        public SessionService(
            ElasticClient log,
            IGenericRepository repository,
            LoginService loginService
            )
        {
            _log = log;
            _repository = repository;
            _loginService = loginService;
        }

        public Response SessionStatus(SessionParams data)
        {
            var response = new Response();
            if (String.IsNullOrEmpty(data.DeviceId.ToString()) || String.IsNullOrEmpty(data.ApplicationUserId.ToString()))
            {
                response.Message = "DeviceId or UserId is empty";
                return response;
            }
            var companyIdInToken = _loginService.GetCurrentCompanyId();
            var company = _repository.GetWithIncludeOne<Company>(x => x.CompanyId == companyIdInToken, x => x.Devices, x => x.ApplicationUser);
            if(!company.Devices.Any( x => x.DeviceId == data.DeviceId))
                throw new Exception("The device is not owned by the company");
            if (!company.ApplicationUser.Any(x => x.Id == data.ApplicationUserId))
                throw new Exception("User does not belong to the company");

            _log.Info($"session on device {data.DeviceId} try {data.Action}");

            //------------------------------------------------------------------
            const int OPEN = 6;
            const int CLOSE = 7;
            var curTime = DateTime.UtcNow;
            var oldTime = DateTime.UtcNow.AddHours(-24);

            var lastSession = _repository.GetAsQueryable<Session>().Where(p => p.DeviceId == data.DeviceId).OrderByDescending(p => p.BegTime)?.FirstOrDefault();
            if(lastSession != null)
            {
                    var oldSessionsOnDevice = _repository.GetAsQueryable<Session>()
                                    .Where(p => p.DeviceId == data.DeviceId 
                                                && p.StatusId == OPEN && p.SessionId != lastSession.SessionId).ToList();
                    //---CLOSE OLD SESSIONS ON THIS DEVICE--- 
                    if (CloseSessions(oldSessionsOnDevice, CLOSE)) _repository.Save();

                    //---CLOSE OLD SESSION FROM THIS USER
                    var oldSessionsFromUser = _repository.GetAsQueryable<Session>()
                                    .Where(p => p.ApplicationUserId == data.ApplicationUserId
                                                && p.StatusId == OPEN && p.SessionId != lastSession.SessionId).ToList();
                    if (CloseSessions(oldSessionsFromUser, CLOSE)) _repository.Save();
            }

            if (data.Action == "open")
            {
                if (lastSession != null && lastSession.BegTime > oldTime && lastSession.ApplicationUserId == data.ApplicationUserId && lastSession.StatusId == OPEN)//---means active session on this device
                {
                    response.Message = $"Can't open session. Already opened";
                    return response;
                }

                //---OPEN (CREATE) NEW SESSION
                CreateNewSession(data, OPEN);
                _repository.Save();
                response.Message = "Session successfully opened";
                _log.Info($"Session successfully opened on device: {data.DeviceId}");
            }

            if (data.Action == "close")
            {
                if (lastSession == null || lastSession.StatusId == CLOSE)
                {
                    response.Message = $"Can't close session. Not opened";
                    return response;
                }
                //---CLOSE OLD SESSIONS ON THIS DEVICE--- 
                if (CloseSession(lastSession, CLOSE))
                    _repository.Save();
                response.Message = "session successfully closed";
                _log.Info($"session successfully closed for device: {data.DeviceId}");
            }
            return response;
        }

        public object SessionStatus(Guid deviceId, Guid? applicationUserId)
        {
            var companyIdInToken = _loginService.GetCurrentCompanyId();
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

        public string AlertNotSmile(Guid applicationUserId)
        {
            if (String.IsNullOrEmpty(applicationUserId.ToString())) 
                return "ApplicationUser is empty";
            var newAlert = new Alert
            {
                CreationDate = DateTime.UtcNow,
                ApplicationUserId = applicationUserId,
                AlertTypeId = _repository.GetAsQueryable<AlertType>().FirstOrDefault(x => x.Name == "client does not smile").AlertTypeId
            };
            _repository.Create<Alert>(newAlert);
            _repository.Save();
            return "Alert saved";
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

        private bool CloseSession(Session session, int CLOSE)
        {
            session.StatusId = CLOSE;
            session.EndTime = FindNextSessionBegTime(session.BegTime, session.EndTime, session.DeviceId);
            return true;
        }

        private DateTime FindNextSessionBegTime(DateTime begTime, DateTime endTime, Guid deviceId)
        {
            DateTime endOfADay = begTime.Date.AddDays(1).AddSeconds(-1);
            DateTime? begOfNextSession = _repository.GetAsQueryable<Session>()
                        .Where(p => p.DeviceId == deviceId && p.BegTime > begTime)
                        .OrderByDescending(p => p.BegTime)
                        .Select(p => p.BegTime)?.FirstOrDefault();
            DateTime endOfSession = (begOfNextSession != default(DateTime) && begOfNextSession < DateTime.UtcNow) ? (DateTime)begOfNextSession : DateTime.UtcNow;
            return endOfSession < endOfADay ? endOfSession : endOfADay;
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