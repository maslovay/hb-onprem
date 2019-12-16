using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using HBData.Models;
using UserOperations.Services;
using Microsoft.EntityFrameworkCore;
using HBData;
using HBLib.Utils;
using UserOperations.Utils;
using System.Collections;
using System.Text.RegularExpressions;
using static HBLib.Utils.SftpClient;
using Swashbuckle.AspNetCore.Annotations;
using UserOperations.CommonModels;
using UserOperations.Services;
using Microsoft.AspNetCore.Authorization;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]    
    [ControllerExceptionFilter]
    [AllowAnonymous]
    public class DemonstrationController : Controller
    {
        private readonly RecordsContext _context;
        private readonly IConfiguration _config;
        private readonly DBOperations _dbOperation;
        private readonly SftpClient _sftpClient;
        private readonly LoginService _loginService;
        private readonly DemonstrationService _demonstrationService;

        public DemonstrationController(
            RecordsContext context,
            IConfiguration config,
            DBOperations dbOperation,
            SftpClient sftpClient,
            LoginService loginService,
            DemonstrationService demonstrationService
            )
        {
            _context = context;
            _config = config;
            _dbOperation = dbOperation;
            _sftpClient = sftpClient;
            _loginService = loginService;
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
        


        [HttpGet("GetContents")]
        [SwaggerOperation(Summary = "Return content on device", Description = "Get all content for loggined company with RowHtml data and url on media. Specially  for device")]
        [SwaggerResponse(400, "Invalid userId or error in DB connection", typeof(string))]
        [SwaggerResponse(200, "Content", typeof(ContentReturnOnDeviceModel))]
        [AllowAnonymous]
        public async Task<List<object>> GetContents([FromQuery] string userId) =>
            await _demonstrationService.GetContents(userId);
        
    
        [HttpPost("PollAnswer")]
        [SwaggerOperation(Summary = "Save answer from poll", Description = "Receive answer from device ande save it connected to campaign and content")]
        [SwaggerResponse(400, "Invalid data or error in DB connection", typeof(string))]
        [SwaggerResponse(200, "Saved")]
        [AllowAnonymous]
        public async Task<string> PollAnswer([FromBody] CampaignContentAnswer answer) =>
            await _demonstrationService.PollAnswer(answer);
    }   
}