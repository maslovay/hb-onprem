using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserOperations.Services;
using UserOperations.Models;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using UserOperations.Utils;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ControllerExceptionFilter]
    public class FillingFileFrameController : Controller
    {
        private readonly FillingFileFrameService _fillingFileFrameService;
        public FillingFileFrameController(FillingFileFrameService fillingFileFrameService)
        {
            _fillingFileFrameService = fillingFileFrameService;
        }
        [HttpPost("FillingFileFrame")]
        [SwaggerOperation(Summary = "Save FileFrames from device", Description = "Create new FileFrame, FrameAttribute, FrameEmotion and save in data base")]
        [SwaggerResponse(200, "model added in data base")]
        public object FileFramePost([FromBody, SwaggerParameter("Send frames", Required = true)] 
                                List<FileFramePostModel> frames ) =>
            _fillingFileFrameService.FillingFileFrame(frames);
    }    
}