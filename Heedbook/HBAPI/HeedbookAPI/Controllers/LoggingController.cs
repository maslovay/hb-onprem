using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using HBLib;
using HBLib.Utils;


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
        /// <param name="functionName">Function nme for filtering</param>
        [HttpGet("[action]")]
        public async Task<ObjectResult> SendLog(string message, string severity, string functionName)
        {
            if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(severity))
                return BadRequest("Please, fill ALL parameters! ");

            _settings.FunctionName = functionName;
            var log = new ElasticClient(_settings);
            
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