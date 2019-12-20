using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserOperations.Services;
using Swashbuckle.AspNetCore.Annotations;
using UserOperations.Models.Post;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SiteController : Controller
    {
        private readonly SiteService _siteService;

        public SiteController(
            SiteService siteService
            )
        {
            _siteService = siteService;
        }

        [AllowAnonymous]
        [HttpPost("Feedback")]
        [SwaggerOperation(Summary = "Feedback from site", Description = "For not loggined users")]
        [SwaggerResponse(400, "The user data is invalid", typeof(string))]
        [SwaggerResponse(200, "Sended")]
        public string Feedback([FromBody]FeedbackEntity feedback) =>
            _siteService.Feedback(feedback);
    }
}