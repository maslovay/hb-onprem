using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using HBData.Models;
using UserOperations.Services;
using Swashbuckle.AspNetCore.Annotations;
using UserOperations.CommonModels;
using Microsoft.AspNetCore.Authorization;
using UserOperations.Controllers;

namespace UserOperations.ControllersV2
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ControllerExceptionFilter]
    public class DemonstrationV2Controller : Controller
    {
        private readonly DemonstrationV2Service _demonstrationService;

        public DemonstrationV2Controller( DemonstrationV2Service demonstrationService )
        {
            _demonstrationService = demonstrationService;
        }

        [HttpPost("FlushStats")]
        [SwaggerOperation(Summary = "Save contents display", Description = "Saves data about content display on device (content, user, content type, start and end date) for statistic")]
        [SwaggerResponse(400, "Invalid parametrs or error in DB connection", typeof(string))]
        [SwaggerResponse(200, "all sessions were saved")]
        public Task FlushStats([FromBody, 
            SwaggerParameter("campaignContentId, begTime, endTime, contentType", Required = true)] 
            List<SlideShowSession> stats) =>
            _demonstrationService.FlushStats(stats);


        [HttpGet("GetContents")]
        [SwaggerOperation(Summary = "Return content on device", Description = "Get all content for loggined company with RawHtml data and url on media. Specially  for device")]
        [SwaggerResponse(400, "Invalid userId or error in DB connection", typeof(string))]
        [SwaggerResponse(200, "Content", typeof(ContentReturnOnDeviceModel))]
        public async Task<List<object>> GetContents() =>
            await _demonstrationService.GetContents();
        
    
        [HttpPost("PollAnswer")]
        [SwaggerOperation(Summary = "Save answer from poll", Description = "Receive answer from device and save it connected to campaign and content. User Id take from token")]
        [SwaggerResponse(400, "Invalid data or error in DB connection", typeof(string))]
        [SwaggerResponse(200, "Saved")]
        public async Task<string> PollAnswer([FromBody] CampaignContentAnswer answer) =>
            await _demonstrationService.PollAnswer(answer);
    }   
}