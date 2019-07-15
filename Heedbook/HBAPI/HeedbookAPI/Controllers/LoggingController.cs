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
        private ElasticClient _log;

        public LoggingController(ElasticClient log)
        {
            _log = log;
            
        }

        /// <summary>
        /// Sends messages to log file
        /// </summary>
        /// <param name="message">Main info</param>
        /// <param name="severity">Severity: "Info", "Debug", "Error", "Fatal", "Warning"</param>
        [HttpGet("[action]")]
        public async Task<ObjectResult> SendLog(string message, string severity)
        {
            if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(severity))
                return BadRequest("Please, fill ALL parameters! ");
            
            switch ( severity.ToUpper() )
            {
                case "FATAL":
                    _log.Fatal(message);
                    break;
                case "DEBUG":
                    _log.Debug(message);
                    break;
                case "ERROR":
                    _log.Error(message);
                    break;
                case "WARNING":
                    _log.Warning(message);
                    break;
                default: 
                case "INFO":
                    _log.Info(message);
                    break;
            }

            return Ok("Logged");
        }
    }
}