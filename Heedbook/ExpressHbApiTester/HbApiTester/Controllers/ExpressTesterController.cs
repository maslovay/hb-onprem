using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlarmSender;
using HbApiTester;
using HbApiTester.Models;
using HbApiTester.Settings;
using HbApiTester.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace HbApiTester.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExpressTesterController : ControllerBase
    {
        private readonly CommandManager _commandManager;
        private readonly ResultsPublisher _resultsPublisher;

        public ExpressTesterController(CommandManager commandManager, ResultsPublisher resultsPublisher)
        {
            _resultsPublisher = resultsPublisher;
            _commandManager = commandManager;
        }

        [HttpGet("[action]")]
        public IActionResult StartApiTests()
        {
            try
            {
                _commandManager.RunCommand("api_tests");
                return Ok("Api tests started!");
            }
            catch (Exception ex)
            {
                return BadRequest("Exception occurred: "  + ex.Message);
            }
        }
        
        [HttpPost("[action]")]
        public void PublishUnitTestResults([FromBody]PublishUnitTestResultsModel model)
        {
            _resultsPublisher.PublishUnitTestResults(model.TrxText);
        }
    }
}