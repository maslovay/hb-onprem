using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using HBData.Models;
using HBData;
using UserOperations.Utils;
using HBLib.Utils;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SessionController : Controller
    {
        private readonly RecordsContext _context;
        private readonly IConfiguration _config;
        private readonly IDBOperations _dbOperation;
        private readonly ElasticClient _log;

        public SessionController(
            RecordsContext context,
            IConfiguration config,
            IDBOperations dbOperation,
            ElasticClient log
            )
        {
            _context = context;
            _config = config;
            _dbOperation = dbOperation;
            _log = log;
        }

        [HttpPost("SessionStatus")]
        public IActionResult SessionStatus([FromBody] SessionParams data)
        {
            const int OPEN = 6;
            const int CLOSE = 7;
            try
            {
//                _log.SetFormat("{ApplicationUserId}");
//                _log.SetArgs(data.ApplicationUserId);
                _log.Info($"session /sessionStatus {data.ApplicationUserId} try {data.Action}");
                var response = new Response();
                if (String.IsNullOrEmpty(data.ApplicationUserId.ToString())) 
                {
//                    _log.Info($"Session/SessionStatus ApplicationUser is empty");
                    response.Message = "ApplicationUser is empty";
                    return BadRequest(response);
                }
                if (data.Action != "open" && data.Action != "close") 
                {
//                    _log.Info($"Session/SessionStatus {data.ApplicationUserId} Wrong action");
                    response.Message = "Wrong action";
                    return BadRequest(response);
                }

                var actionId = data.Action == "open" ? OPEN : CLOSE;
                var curTime = DateTime.UtcNow;
                var oldTime = DateTime.UtcNow.AddDays(-3);
                
                //---last session for 3 days
                var lastSession = _context.Sessions
                        .Where(p => p.ApplicationUserId == data.ApplicationUserId && p.BegTime >= oldTime && p.BegTime <= curTime)
                        .OrderByDescending(p => p.BegTime)
                        .FirstOrDefault();

                var alertOpenCloseSession = new Alert();
                alertOpenCloseSession.ApplicationUserId = data.ApplicationUserId;
                alertOpenCloseSession.CreationDate = DateTime.UtcNow;

                //---START CLOSE OLD SESSIONS IF NOT CLOSED and DEVIDE TIME FOR LONG SESSIONS---
                var lastSessionId = lastSession != null ? lastSession.SessionId : Guid.Empty;
              
                var veryLongSessions = _context.Sessions
                          .Where(p => p.ApplicationUserId == data.ApplicationUserId
                           && p.SessionId != lastSessionId
                           && p.EndTime.Subtract(p.BegTime).TotalHours > 24 )
                          .ToArray();
                for (int i = 0; i < veryLongSessions.Count(); i++)
                {
                    var longSes = veryLongSessions[i];
                    var longSesBeg = longSes.BegTime;
                    var longSesEnd = longSes.EndTime;

                    var sessionDuration =  longSesEnd.Subtract(longSesBeg).TotalHours;

                    for (var begTime = longSesBeg.AddHours(24); begTime < longSesEnd; begTime = begTime.AddHours(24))
                    {
                        Session session = new Session();
                        session.ApplicationUserId = longSes.ApplicationUserId;
                        session.BegTime = begTime;
                        session.EndTime = begTime.AddHours(24) < longSesEnd ? begTime.AddHours(24) : longSesEnd;
                        session.IsDesktop = longSes.IsDesktop;
                        session.StatusId = CLOSE;
                        _context.Sessions.Add(session);
                    }
                    longSes.EndTime = longSesBeg.AddHours(24);
                }
              //  _context.SaveChanges();

                var notClosedSessions = _context.Sessions
                        .Where(p => p.ApplicationUserId == data.ApplicationUserId
                        && p.SessionId != lastSessionId
                        && p.StatusId == OPEN)
                        .ToArray();
                for (int i = 0; i < notClosedSessions.Count(); i++)
                {
                    var nextSessionByTime = _context.Sessions
                            .Where(x => x.BegTime > notClosedSessions[i].BegTime 
                             && x.ApplicationUserId == data.ApplicationUserId)
                             .OrderBy(x => x.BegTime).FirstOrDefault();
                    if (nextSessionByTime != null && nextSessionByTime.BegTime.Subtract(notClosedSessions[i].BegTime).TotalHours < 24)
                        notClosedSessions[i].EndTime = nextSessionByTime.BegTime;                  
                    notClosedSessions[i].StatusId = CLOSE;
                }
                _context.SaveChanges();
                //---END

                if (lastSession != null && actionId == lastSession.StatusId)
                {
                    response.Message = $"Can't {data.Action} session";
                    return Ok(response);
                    // _log.Info($"Session/SessionStatus {data.ApplicationUserId} Can't {data.Action} session");
                }

                if (lastSession == null && actionId == CLOSE)
                {
                    response.Message = "Can't close not opened session";
                    return Ok(response);
                    // _log.Info($"Session/SessionStatus {data.ApplicationUserId} Can't close not opened session");
                }

                if ( actionId == OPEN )
                {
                    var session = new Session {
                        BegTime = DateTime.UtcNow,
                        EndTime = DateTime.UtcNow.Date.AddDays(1).AddSeconds(-1),
                        ApplicationUserId = data.ApplicationUserId,
                        StatusId = actionId,
                        IsDesktop = (bool)data.IsDesktop
                    };
                    alertOpenCloseSession.AlertTypeId = _context.AlertTypes.Where(x => x.Name == "session open").FirstOrDefault().AlertTypeId;
                    _context.Sessions.Add(session);
                    _context.Alerts.Add(alertOpenCloseSession);
                    _context.SaveChanges();
                    response.Message = "Session successfully opened";
                    _log.Info($"Session successfully opened {data.ApplicationUserId}"); 
                    return Ok(response);
                }

                if (lastSession != null && actionId == CLOSE)
                {
                    lastSession.StatusId = CLOSE;
                    lastSession.EndTime = DateTime.UtcNow;

                    //---add alerts-- -
                    alertOpenCloseSession.AlertTypeId = _context.AlertTypes.FirstOrDefault(x => x.Name == "session close").AlertTypeId;
                    _context.Alerts.Add(alertOpenCloseSession);

                    var dialoquesAmount = _context.Dialogues
                        .Where(x => x.BegTime >= lastSession.BegTime
                        && x.EndTime <= lastSession.EndTime
                        && x.ApplicationUserId == lastSession.ApplicationUserId && x.StatusId == 3 && x.InStatistic == true).Count();
                    if (dialoquesAmount == 0)
                    {
                        var alertNoDialogues = new Alert();
                        alertNoDialogues.ApplicationUserId = data.ApplicationUserId;
                        alertNoDialogues.CreationDate = DateTime.UtcNow;
                        alertNoDialogues.AlertTypeId = _context.AlertTypes.FirstOrDefault(x => x.Name == "no conversations").AlertTypeId;
                        _context.Alerts.Add(alertNoDialogues);
                    }
                    //---
                    _context.SaveChanges();
                    response.Message = "session successfully closed";
                    _log.Info($"session successfully closed {data.ApplicationUserId}"); 
                    return Ok(response);
                }

                response.Message = "Wrong action";
//              _log.Info($"Session/SessionStatus {data.ApplicationUserId} Wrong action");
                return BadRequest(response);
            }
            catch (Exception e)
            {
                var response = new Response();
                response.Message = $"Exception occured {e}";
//              _log.Fatal($"Exception occurred {e}");
                return BadRequest(response);
            }
        }

        [HttpGet("SessionStatus")]
        public IActionResult SessionStatus([FromQuery] Guid applicationUserId)
        {
            try
            {
                var session = _context.Sessions
                        .Where(p => p.ApplicationUserId == applicationUserId)
                         ?.OrderByDescending(p => p.BegTime)
                         ?.FirstOrDefault();
                var result = new { session?.BegTime, session?.StatusId };
//                _log.Info($"Get Session/SessionStatus {applicationUserId}");
                return Ok(result);
            }
            catch (Exception e)
            {
                var response = new Response();
                response.Message = $"Exception occured {e}";
//                _log.Fatal($"Exception occurred {e}");
                return BadRequest(response);
            }
        }      


        [HttpPost("AlertNotSmile")]
        public IActionResult AlertNotSmile([FromBody] Guid applicationUserId)
        {
            try
            {
                var response = new Response();
                if (String.IsNullOrEmpty(applicationUserId.ToString())) 
                {
                    response.Message = "ApplicationUser is empty";
                    return BadRequest(response);
                }

                var newAlert = new Alert();
                newAlert.CreationDate = DateTime.UtcNow;
                newAlert.ApplicationUserId = applicationUserId;
                newAlert.AlertTypeId = _context.AlertTypes.FirstOrDefault(x => x.Name == "client does not smile").AlertTypeId;
                _context.Alerts.Add(newAlert);
                _context.SaveChanges();
                response.Message = "Alert saved";
                return Ok(response);
            }
            catch (Exception e)
            {
//                 _log.Fatal($"Exception occurred {e}");
                return BadRequest();
            }
        }
    }

    public class Response
    {
        public string Message;
    }

    public class SessionParams
    {
        public Guid ApplicationUserId;
        public string Action;
        public bool? IsDesktop;
    }
}