using HBLib;
using HBLib.Utils;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;


namespace TestLoging.Controllers
{
    [Route("testlog/[controller]")]
    [ApiController]
    public class LogController: Controller
    {
        private readonly ElasticClient _log;

        public LogController(ElasticClient log)
        {
            _log = log;
        }

        [HttpGet("Log")]
        [SwaggerOperation(Description =
            "Send logs")]
        public IActionResult TestLog()
        {
            _log.Info("Test controller started");
            _log.Error("Test controller error message");
            _log.Fatal("Test controller fatal message");
            _log.Info("Function finished");
            return Ok();
        }
    }
}