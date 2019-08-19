using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Configuration;
using UserOperations.AccountModels;
using HBData.Models;
using HBData.Models.AccountViewModels;
using UserOperations.Services;
using UserOperations.Models.AnalyticModels;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using HBData;
using Microsoft.AspNetCore.Cors;
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
        private readonly DBOperations _dbOperation;
        private readonly ElasticClient _log;

        public SessionController(
            RecordsContext context,
            IConfiguration config,
            DBOperations dbOperation,
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
            try
            {
                _log.SetFormat("{ApplicationUserId}");
                _log.SetArgs(data.ApplicationUserId);
                _log.Info($"Session/SessionStatus {data.ApplicationUserId} started");

                var response = new Response();

                if (String.IsNullOrEmpty(data.ApplicationUserId.ToString())) 
                {
                    response.Message = "ApplicationUser is empty";
                    _log.Info($"Session/SessionStatus ApplicationUser is empty");
                    return BadRequest(response);
                }
                if (data.Action != "open" && data.Action != "close") 
                {
                    response.Message = "Wrong action";
                    _log.Info($"Session/SessionStatus {data.ApplicationUserId} Wrong action");
                    return BadRequest(response);
                }

                var actionId = data.Action == "open" ? 6 : 7;
                var curTime = DateTime.UtcNow;
                var oldTime = DateTime.UtcNow.AddDays(-3);
                
                var lastSession = _context.Sessions
                        .Where(p => p.ApplicationUserId == data.ApplicationUserId && p.BegTime >= oldTime && p.BegTime <= curTime)
                        .ToList().OrderByDescending(p => p.BegTime)
                        .FirstOrDefault();    

                var alertOpenCloseSession = new Alert();   
                alertOpenCloseSession.ApplicationUserId = data.ApplicationUserId;
                alertOpenCloseSession.CreationDate = DateTime.UtcNow;
           

                if (lastSession == null)
                {
                    switch (actionId)
                    {
                        case 6:
                            var session = new Session{
                                BegTime = DateTime.UtcNow,
                                EndTime = DateTime.UtcNow.Date.AddDays(1).AddSeconds(-1),
                                ApplicationUserId = data.ApplicationUserId,
                                StatusId = actionId,
                                IsDesktop = (bool)data.IsDesktop
                            };
                            _context.Sessions.Add(session);

                            alertOpenCloseSession.AlertTypeId = _context.AlertTypes.Where(x => x.Name == "session open").FirstOrDefault().AlertTypeId;
                            _context.Alerts.Add(alertOpenCloseSession);

                            _context.SaveChanges();
                            response.Message = "Session successfully opened";
                            _log.Info($"Session successfully opened {data.ApplicationUserId}"); 
                            return Ok(response);
                        case 7:
                            response.Message = "Can't close not opened session";
                            _log.Info($"Session/SessionStatus {data.ApplicationUserId} Can't close not opened session");
                            return Ok(response);
                        default:
                            response.Message = "Wrong action";
                            _log.Info($"Session/SessionStatus {data.ApplicationUserId} Wrong action");
                            return BadRequest(response);
                    }
                }
                else
                {
                    var notClosedSessions = _context.Sessions
                    .Where(p => p.ApplicationUserId == data.ApplicationUserId && p.StatusId == 6 && p.SessionId != lastSession.SessionId)
                    .ToArray(); 
                    for( int i = 0; i < notClosedSessions.Count(); i++ )
                    {                  
                            notClosedSessions[i].StatusId = 7;
                            notClosedSessions[i].EndTime = notClosedSessions[i].BegTime;
                            _context.SaveChanges();
                    }
                    switch (actionId)
                    {
                        case 6:
                            if (lastSession.StatusId == 6) 
                            {
                                response.Message = "Can't open not closed session";
                                _log.Info($"Session/SessionStatus {data.ApplicationUserId} Can't open not closed session");
                                return Ok(response);
                            }
                            var session = new Session{
                                BegTime = DateTime.UtcNow,
                                EndTime = DateTime.UtcNow.Date.AddDays(1).AddSeconds(-1),
                                ApplicationUserId = data.ApplicationUserId,
                                StatusId = actionId,
                                IsDesktop = (bool)data.IsDesktop
                            };
                            _context.Sessions.Add(session);

                            alertOpenCloseSession.AlertTypeId = _context.AlertTypes.Where(x => x.Name == "session open").FirstOrDefault().AlertTypeId;
                            _context.Alerts.Add(alertOpenCloseSession);

                            _context.SaveChanges();

                            response.Message = "Session successfully opened";
                            _log.Info($"Session successfully opened {data.ApplicationUserId}"); 
                            return Ok(response);
                        
                        case 7:
                            if (lastSession.StatusId == 7) 
                            {
                                response.Message = "Can't close not opened session";
                                _log.Info($"Session/SessionStatus {data.ApplicationUserId} Can't close not opened session");
                                return Ok(response);
                            }
                            lastSession.StatusId = 7;
                            lastSession.EndTime = DateTime.UtcNow;

                            //---add alerts---
                            alertOpenCloseSession.AlertTypeId = _context.AlertTypes.FirstOrDefault(x => x.Name == "session close").AlertTypeId;
                            _context.Alerts.Add(alertOpenCloseSession);

                            var dialoquesAmount = _context.Dialogues
                                .Where(x => x.BegTime >= lastSession.BegTime 
                                && x.EndTime <= lastSession.EndTime 
                                && x.ApplicationUserId == lastSession.ApplicationUserId && x.StatusId == 3 && x.InStatistic == true).Count();
                            if( dialoquesAmount == 0 )
                            {
                                var alertNoDialogues = new Alert();   
                                alertNoDialogues.ApplicationUserId = data.ApplicationUserId;
                                alertNoDialogues.CreationDate = DateTime.UtcNow;
                                alertNoDialogues.AlertTypeId = _context.AlertTypes.FirstOrDefault(x => x.Name == "no conversations").AlertTypeId;
                                _context.Alerts.Add(alertNoDialogues);
                            }
                            //---
                            _context.SaveChanges();
                            response.Message = "Session successfully closed";
                            _log.Info($"Session successfully closed {data.ApplicationUserId}"); 
                            return Ok(response);
                        default:
                            response.Message = "Wrong action";
                            _log.Info($"Session/SessionStatus {data.ApplicationUserId} Wrong action");
                            return BadRequest(response);
                    }
                }
            }
            catch (Exception e)
            {
                var response = new Response();
                response.Message = $"Exception occured {e}";
                _log.Fatal($"Exception occurred {e}");
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
                 _log.Fatal($"Exception occurred {e}");
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