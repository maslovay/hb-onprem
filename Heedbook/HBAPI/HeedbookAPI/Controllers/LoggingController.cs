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
using System.Threading;

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
        /// <param name="customDimensions">Custom dimensions. Example: {"TestParam":"1230932"}, {"DialogueId": "890238091238901283"}</param>
        [HttpPost("SendLog")]
        public async Task<ObjectResult> SendLogPost([FromQuery]string message, [FromQuery]string severity, 
            [FromQuery]string functionName, [FromBody]JObject customDimensions = null)
        {
            return SendLogInner(message, severity, functionName, customDimensions);
        }

        /// <summary>
        /// Sends messages to log file
        /// </summary>
        /// <param name="message">Main info</param>
        /// <param name="severity">Severity: "Info", "Debug", "Error", "Fatal", "Warning"</param>
        /// <param name="functionName">Function name for filtering</param>
        [HttpGet("SendLog")]
        public async Task<ObjectResult> SendLog(string message, string severity, string functionName)
        {
            return SendLogInner(message, severity, functionName, null);
        }

        private ObjectResult SendLogInner(string message, string severity, string functionName, JObject customDimensions)
        {
           ThreadPool.GetMaxThreads(out int workerThreads, out int completionPortThreads);
            ThreadPool.GetAvailableThreads(out int workerThreadsAvailable, out int completionPortThreadsAvailable);
            Console.WriteLine($"max: workerT {workerThreads}, completionPortT: {completionPortThreads}" );
            Console.WriteLine($"available: workerT {workerThreadsAvailable}, completionportT: {completionPortThreadsAvailable}");


            if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(severity))
                return BadRequest("Please, fill 'message' and 'severity'! ");

            _settings.FunctionName = functionName;

            var log = new ElasticClient(_settings);
            if (customDimensions != null)
            {
                var parseDoc = customDimensions;
                var parNames = string.Empty;
                var parValues = new List<object>(5);

                foreach (var (key, value) in parseDoc)
                {
                    parNames += "{" + key + "},";
                    parValues.Add(value);
                }

                log.SetFormat(parNames);
                log.SetArgs(parValues.ToArray());
            }

            switch (severity.ToUpper())
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