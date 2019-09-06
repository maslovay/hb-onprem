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
using HBData.Models;
using HBData.Models.AccountViewModels;
using UserOperations.Services;
using UserOperations.AccountModels;
using HBData;


using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Annotations;
using HBLib.Utils;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SiteController : Controller
    {
        private readonly MailSender _mailSender;
        private readonly ElasticClient _log;

        public SiteController(
            MailSender mailSender,
            ElasticClient log
            )
        {
            _mailSender = mailSender;
            _log = log;
        }
   

        [AllowAnonymous]
        [HttpPost("Feedback")]
        [SwaggerOperation(Summary = "Feedback from site", Description = "For not loggined users")]
        [SwaggerResponse(400, "The user data is invalid", typeof(string))]
        [SwaggerResponse(200, "Sended")]
        public IActionResult Feedback([FromBody]FeedbackEntity feedback)
        {
            try
            {
                _log.Info("Site/Feedback started"); 
                if (string.IsNullOrEmpty(feedback.name)
                      || string.IsNullOrEmpty(feedback.phone)
                      || string.IsNullOrEmpty(feedback.email)
                      || string.IsNullOrEmpty(feedback.body))
                    throw new Exception();


                string text = string.Format("<table>" +
                    "<tr><td>name:</td><td> {0}</td></tr>" +
                    "<tr><td>email:</td><td> {1}</td></tr>" +
                    "<tr><td>phone:</td><td> {2}</td></tr>" +
                    "<tr><td>message:</td><td> {3}</td></tr>" +
                    "</table>", feedback.name, feedback.email, feedback.phone, feedback.body);
                _mailSender.SendSimpleEmail("info@heedbook.com", "Message from site", text);
                _log.Info("Site/Feedback finished"); 
                return Ok("Sended");
            }
            catch (Exception e)
            {
                _log.Fatal($"Exception occurred {e}");
                return BadRequest($"Could not send email {e}");
            }
        }        
    }
      public class FeedbackEntity
        {
            public string name { get; set; }
            public string phone { get; set; }
            public string body { get; set; }
            public string email { get; set; }
        }
}