using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBLib.Utils;
using HBMLHttpClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Notifications.Base;
using RabbitMqEventBus.Events;
using Swashbuckle.AspNetCore.Annotations;

namespace UserService.Controllers
{
    [Route("user/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ApiController]
    public class FaceController : ControllerBase
    {
        private readonly INotificationHandler _handler;
        private readonly HbMlHttpClient _client;
        private readonly CheckTokenService _service;

        public FaceController(INotificationHandler handler, HbMlHttpClient client, CheckTokenService service)
        {
            _handler = handler;
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _service = service;
        }

        [HttpPost]
        [SwaggerOperation(Description =
            "Analyze frame. Detect faces and calculate emotions and face attributes such as gender and age")]
        public async Task FaceAnalyzeRun([FromBody] FaceAnalyzeRun message)
        {
            _service.CheckIsUserAdmin();
            _handler.EventRaised(message);
        }

        [HttpPost("[action]")]
        [SwaggerOperation(Description = "Analyze frame. Detect face, return gender and age")]
        public async Task<IActionResult> FrameAnalyze([FromBody] string imageBase64)
        {
            _service.CheckIsUserAdmin();
            try
            {            
                var faceResult = await _client.GetFaceResult(imageBase64); 
                var result = faceResult.Select(p => new 
                    {
                        p.Attributes.Age,
                        p.Attributes.Gender,
                        p.Descriptor
                    }).FirstOrDefault();
                var jsonResult = JsonConvert.SerializeObject(result);
                System.Console.WriteLine(jsonResult);
                return Ok(jsonResult);
            }
            catch(Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                return BadRequest(ex.Message);
            }            
        }        
    }
}