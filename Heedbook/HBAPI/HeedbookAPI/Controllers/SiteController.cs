using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserOperations.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SiteController : Controller
    {
        private readonly MailSender _mailSender;

        public SiteController(
            MailSender mailSender
            )
        {
            _mailSender = mailSender;
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
                return Ok("Sended");
            }
            catch (Exception e)
            {
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