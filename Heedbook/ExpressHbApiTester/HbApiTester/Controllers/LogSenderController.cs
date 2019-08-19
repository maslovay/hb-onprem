using System;
using HbApiTester.Models;
using Microsoft.AspNetCore.Mvc;

namespace HbApiTester.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LogSenderController : ControllerBase
    {
        private readonly CommandManager _commandManager;
        private readonly LogsPublisher _logsPublisher;

        public LogSenderController(CommandManager commandManager, LogsPublisher logsPublisher)
        {
            _logsPublisher = logsPublisher;
            _commandManager = commandManager;
        }
        
        [HttpPost("[action]")]
        public void PublishUnitTestResults([FromBody]PublishLogsModel model) 
            => _logsPublisher.PublishLogs(model.LogText);
    }
}