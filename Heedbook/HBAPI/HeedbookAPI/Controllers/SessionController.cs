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
using UserOperations.Utils;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SessionController : Controller
    {
        private readonly RecordsContext _context;
        private readonly IConfiguration _config;
        private readonly DBOperations _dbOperation;

        public SessionController(
            RecordsContext context,
            IConfiguration config,
            DBOperations dbOperation
            )
        {
            _context = context;
            _config = config;
            _dbOperation = dbOperation;
        }

        [HttpPost("SessionStatus")]
        public IActionResult SessionStatus([FromBody] SessionParams data)
        {
            try
            {
                if (String.IsNullOrEmpty(data.ApplicationUserId.ToString())) return BadRequest("ApplicationUser is empty");
                if (data.Action != "open" && data.Action != "close") return BadRequest("Wrong action");
                var actionId = data.Action == "open" ? 6 : 7;
                var curTime = DateTime.UtcNow;
                var oldTime = DateTime.UtcNow.AddDays(-3);
                var lastSession = _context.Sessions
                        .Where(p => p.ApplicationUserId == data.ApplicationUserId && p.BegTime >= oldTime && p.BegTime <= curTime)
                        .ToList().OrderByDescending(p => p.BegTime)
                        .FirstOrDefault();

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
                            return Ok("Session successfully opened");
                        case 7:
                            return Ok("Can't close not opened session");
                        default:
                            return BadRequest();
                    }
                }
                else
                {
                    switch (actionId)
                    {
                        case 6:
                            if (lastSession.StatusId == 6) return Ok("Can't open not closed session");
                            var session = new Session{
                                BegTime = DateTime.UtcNow,
                                EndTime = DateTime.UtcNow.Date.AddDays(1).AddSeconds(-1),
                                ApplicationUserId = data.ApplicationUserId,
                                StatusId = actionId,
                                IsDesktop = (bool)data.IsDesktop
                            };

                            _context.Sessions.Add(session);
                            _context.SaveChanges();

                            return Ok("Session successfully opened");
                        
                        case 7:
                            if (lastSession.StatusId == 7) return Ok("Can't close not opened session");
                            lastSession.StatusId = 7;
                            lastSession.EndTime = DateTime.UtcNow;

                            _context.SaveChanges();
                            return Ok("Session successfully closed");
                        default:
                            return BadRequest();
                    }
                }
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }
    }

     public class SessionParams
    {
        public Guid ApplicationUserId;
        public string Action;
        public bool? IsDesktop;
    }
}