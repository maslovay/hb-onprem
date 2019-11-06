using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBMLHttpClient;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Notifications.Base;
using RabbitMqEventBus.Events;
using Swashbuckle.AspNetCore.Annotations;

namespace UserService.Controllers
{
    [Route("user/[controller]")]
    [ApiController]
    public class FaceController : ControllerBase
    {
        private readonly INotificationHandler _handler;
        private readonly HbMlHttpClient _client;

        public FaceController(INotificationHandler handler, HbMlHttpClient client)
        {
            _handler = handler;
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        [HttpPost]
        [SwaggerOperation(Description =
            "Analyze frame. Detect faces and calculate emotions and face attributes such as gender and age")]
        public async Task FaceAnalyzeRun([FromBody] FaceAnalyzeRun message)
        {
            _handler.EventRaised(message);
        }

        [HttpPost("[action]")]
        [SwaggerOperation(Description = "Analyze frame. Detect face, return gender and age")]
        public async Task<IActionResult> FrameAnalyze([FromForm] IFormCollection formData)
        {
            try
            {
                var memoryStream = formData.Files.FirstOrDefault().OpenReadStream();
                var byteArray = StreamToByteArray(memoryStream);
                var base64String = Convert.ToBase64String(byteArray);
                var faceResult = await _client.GetFaceResult(base64String);                
                //System.Console.WriteLine(faceResult);
                //_handler.EventRaised(message);
                var result = faceResult.Select(p => new 
                    {
                        p.Attributes.Age,
                        p.Attributes.Gender
                    });
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
        private byte[] StreamToByteArray(Stream input)
        {
            byte[] buffer = new byte[16*1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
    }
}