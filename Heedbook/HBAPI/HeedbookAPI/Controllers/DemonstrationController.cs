using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using HBData.Models;
using UserOperations.Services;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.AspNetCore.Authorization;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ControllerExceptionFilter]
    [AllowAnonymous]
    public class DemonstrationController : Controller
    {
        private readonly DemonstrationService _demonstrationService;

        public DemonstrationController (
            DemonstrationService demonstrationService
            )
        {
            _demonstrationService = demonstrationService;
        }

        [HttpPost("FlushStats")]
        [SwaggerOperation(Summary = "Save contents display", Description = "Saves data about content display on device (content, user, content type, start and end date) for statistic")]
        [SwaggerResponse(400, "Invalid parametrs or error in DB connection", typeof(string))]
        [SwaggerResponse(200, "all sessions were saved")]
        [AllowAnonymous]
        public Task FlushStats([FromBody, 
            SwaggerParameter("campaignContentId, applicationUserId, begTime, endTime, contentType", Required = true)] 
            List<SlideShowSession> stats) =>
            _demonstrationService.FlushStats(stats);
        
    
        [HttpPost("PollAnswer")]
        [SwaggerOperation(Summary = "Save answer from poll", Description = "Receive answer from device ande save it connected to campaign and content")]
        [SwaggerResponse(400, "Invalid data or error in DB connection", typeof(string))]
        [SwaggerResponse(200, "Saved")]
        [AllowAnonymous]
        public async Task<string> PollAnswer([FromBody] CampaignContentAnswer answer) =>
            await _demonstrationService.PollAnswer(answer);
    }   
}