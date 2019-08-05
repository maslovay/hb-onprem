using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlarmSender;
using HbApiTester;
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


        public ExpressTesterController(CommandManager commandManager)
            => _commandManager = commandManager;

        [HttpGet("[action]")]
        public ActionResult<ObjectResult> StartApiTests()
        {
            try
            {
                _commandManager.RunCommand("/api_tests");
                return Ok("Api tests started!");
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult("Exception occurred: "  + ex.Message);
            }
        }
    }
}