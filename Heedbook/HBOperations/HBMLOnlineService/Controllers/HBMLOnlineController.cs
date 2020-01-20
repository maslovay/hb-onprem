using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBData;
using HBLib.Utils;
using HBMLHttpClient;
using HBMLOnlineService.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Notifications.Base;
using RabbitMqEventBus.Events;
using Swashbuckle.AspNetCore.Annotations;

namespace HBMLOnlineService.Controllers
{
    [Route("face/[controller]")]
    [ApiController]
    public class HBMLOnlineService : Controller
    {
        private readonly HBMLOnlineFaceService _hbmlservice;
        
        public HBMLOnlineService(  HBMLOnlineFaceService hbmlservice)
        {
            _hbmlservice = hbmlservice;
        }

        //Descriptor=true&Emotions=true&Headpose=true&Attributes=true&DeviceId=null&CompanyId=4f318be9-7f1e-4a8b-96ec-c6ac2226cae6
        [HttpPost("Face")]
        [SwaggerOperation(Description = "Controller analyze frames, find clients and add all information to database and storage")]
        public async Task<IActionResult> Face(
            [FromQuery] Guid? deviceId,
            [FromQuery] Guid? companyId,
            [FromQuery] bool description,
            [FromQuery] bool emotions,
            [FromQuery] bool headpose,
            [FromQuery] bool attributes,
            [FromBody] byte[] file 
        )
        {
            try
            {
                var stringFormat = "yyyyMMddHHmmss";
                var dateTime = DateTime.UtcNow.ToString(stringFormat);
                var filename = $"{companyId}_{deviceId}_{dateTime}.jpg";

                if(file != null)
                {   
                    var faceResults = await _hbmlservice.UploadFrameAndGetFaceResultAsync(file, filename, description, emotions, headpose, attributes);
                    if (faceResults.Any())
                    {
                        _hbmlservice.PublishMessageToRabbit(deviceId, companyId, filename, faceResults);
                    }
                    return Ok(JsonConvert.SerializeObject(faceResults));
                }
                else
                {
                    return BadRequest("No files found");
                }
            }
            catch (Exception e)
            {
                return BadRequest($"Exception occured {e}");
            }
        }
    }

}