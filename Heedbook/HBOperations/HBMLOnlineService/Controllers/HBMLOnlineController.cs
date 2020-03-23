using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBData;
using HBLib;
using HBLib.Utils;
using HBMLHttpClient;
using HBMLOnlineService.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Notifications.Base;
using RabbitMqEventBus.Events;
using Swashbuckle.AspNetCore.Annotations;

namespace HBMLOnlineService.Controllers
{
    [Route("face")]
  //  [Authorize(AuthenticationSchemes = "Bearer")]
    [ApiController]
    public class HBMLOnlineService : Controller
    {
        private readonly HBMLOnlineFaceService _hbmlservice;
        private readonly ElasticClientFactory _elasticClientFactory;
        private readonly ElasticClient _log;

        public HBMLOnlineService( HBMLOnlineFaceService hbmlservice,
            ElasticClientFactory elasticClientFactory
        )
        {
            _hbmlservice = hbmlservice;
            _elasticClientFactory = elasticClientFactory;
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
            [FromBody] string base64String 
        )
        {
            var _log = _elasticClientFactory.GetElasticClient();
            _log.SetFormat("{DeviceId}");
            _log.SetArgs(deviceId);

            try
            {
                var stringFormat = "yyyyMMddHHmmss";
                var dateTime = DateTime.UtcNow.ToString(stringFormat);
                var filename = $"{companyId}_{deviceId}_{dateTime}.jpg";

                _log.Info($"Saving file {filename}");

                if(base64String != null)
                {   
                    var faceResults = await _hbmlservice.UploadFrameAndGetFaceResultAsync(base64String, filename, description, emotions, headpose, attributes);
                    if (faceResults.Any())
                    {
                        faceResults = faceResults.OrderByDescending(p => p.Rectangle.Height * p.Rectangle.Width).ToList();
                        _hbmlservice.PublishMessageToRabbit(deviceId, companyId, filename, faceResults);
                    }
                    _log.Info("Function finished");
                    return Ok(JsonConvert.SerializeObject(faceResults));
                }
                else
                {
                    _log.Info("No files found");
                    _log.Info("Function finished");
                    return BadRequest("No files found");
                }
            }
            catch (Exception e)
            {
                // System.Console.WriteLine($"Exception occured {e}");
                _log.Fatal("Exception occured {e}");
                return BadRequest($"Exception occured {e}");
            }
        }
    }

}