using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace MetricsController.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly AzureConnector _connector;
        public ValuesController(AzureConnector connector)
        {
            _connector = connector;
        }
 
        [HttpGet]
        public IActionResult Get()
        {
            _connector.GetMetrics();
            return Ok();
        }
       
    }
}
