using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using HBLib;
using HBLib.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoggingController : ControllerBase
    {
        private readonly ElasticSettings _settings;

        public LoggingController(ElasticSettings settings)
        {
            _settings = new ElasticSettings {Host = settings.Host, Port = settings.Port};
        }

        /// <summary>
        /// Sends messages to log file
        /// </summary>
        /// <param name="message">Main info</param>
        /// <param name="severity">Severity: "Info", "Debug", "Error", "Fatal", "Warning"</param>
        /// <param name="functionName">Function name for filtering</param>
        /// <param name="customDimensionsBase64">Custom dimensions. Example: Base64([{"TestParam":"1230932"}, {"DialogueId": "890238091238901283"}])</param>
        [HttpGet("[action]")]
        public async Task<ObjectResult> SendLog(string message, string severity, string functionName, string customDimensionsBase64 = null)
        {
            if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(severity))
                return BadRequest("Please, fill 'message' and 'severity'! ");
            
            _settings.FunctionName = functionName;
            
            var log = new ElasticClient(_settings);
            if (customDimensionsBase64 != null)
            {
                var customDimensions = Encoding.UTF8.GetString(Convert.FromBase64String(customDimensionsBase64));

                if (!string.IsNullOrEmpty(customDimensions))
                {
                    var parseDoc = JToken.Parse(customDimensions);
                    var parNames = string.Empty;
                    var parValues = new List<object>(5);

                    foreach (var obj in parseDoc.Children<JObject>())
                    {
                        foreach (var (key, value) in obj)
                        {
                            parNames += "{" + key + "},";
                            parValues.Add(value);
                        }
                    }

                    log.SetFormat(parNames);
                    log.SetArgs(parValues.ToArray());
                }
            }

            switch ( severity.ToUpper() )
            {
                case "FATAL":
                    log.Fatal(message);
                    break;
                case "DEBUG":
                    log.Debug(message);
                    break;
                case "ERROR":
                    log.Error(message);
                    break;
                case "WARNING":
                    log.Warning(message);
                    break;
                default: 
                case "INFO":
                    log.Info(message);
                    break;
            }

            return Ok("Logged");
        }
    }
}