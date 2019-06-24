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
                _log.Info("Session/SessionStatus started"); 
                var response = new Response();

                if (String.IsNullOrEmpty(data.ApplicationUserId.ToString())) 
                {
                    response.Message = "ApplicationUser is empty";
                    return BadRequest(response);
                }
                if (data.Action != "open" && data.Action != "close") 
                {
                    response.Message = "Wrong action";
                    return BadRequest(response);
                }
                var actionId = data.Action == "open" ? 6 : 7;
                var curTime = DateTime.UtcNow;
                var oldTime = DateTime.UtcNow.AddDays(-3);
                var lastSession = _context.Sessions
                        .Where(p => p.ApplicationUserId == data.ApplicationUserId && p.BegTime >= oldTime && p.BegTime <= curTime)
                        .ToList().OrderByDescending(p => p.BegTime)
                        .FirstOrDefault();

                //----------CLOSE ALL NOT CLOSED SESSIONS--------        
                var notClosedSessions = _context.Sessions
                        .Where(p => p.ApplicationUserId == data.ApplicationUserId && p.StatusId == 6 && p.SessionId != lastSession.SessionId)
                        .ToArray(); 
                for( int i = 0; i < notClosedSessions.Count(); i++ )
                {                  
                        notClosedSessions[i].StatusId = 7;
                        notClosedSessions[i].EndTime = notClosedSessions[i].BegTime;
                        _context.SaveChanges();
                }
                //----------CHANGE STATUS OR OPEN NEW-----------------------------------
                if (lastSession == null)
                {
                    switch (actionId)
                    {
                        case 6:
                            var session = new Session{
                                BegTime = DateTime.UtcNow,
                                EndTime = DateTime.UtcNow,
                                ApplicationUserId = data.ApplicationUserId,
                                StatusId = actionId,
                                IsDesktop = (bool)data.IsDesktop
                            };
                            _context.Sessions.Add(session);
                            _context.SaveChanges();
                            response.Message = "Session successfully opened";
                            return Ok(response);
                        case 7:
                            response.Message = "Can't close not opened session";
                            return Ok(response);
                        default:
                            response.Message = "Wrong action";
                            return BadRequest(response);
                    }
                }
                else
                {
                    switch (actionId)
                    {
                        case 6:
                            if (lastSession.StatusId == 6) 
                            {
                                response.Message = "Can't open not closed session";
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
                            _context.SaveChanges();

                            response.Message = "Session successfully opened";
                            return Ok(response);
                        
                        case 7:
                            if (lastSession.StatusId == 7) 
                            {
                                response.Message = "Can't close not opened session";
                                return Ok(response);
                            }
                            lastSession.StatusId = 7;
                            lastSession.EndTime = DateTime.UtcNow;

                            _context.SaveChanges();
                            response.Message = "Session successfully closed";
                            return Ok(response);
                        default:
                            response.Message = "Wrong action";
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