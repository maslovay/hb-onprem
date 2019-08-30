using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using HBData;
using UserOperations.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using UserOperations.Utils;
using Swashbuckle.AspNetCore.Annotations;

namespace UserOperations.Controllers.Test
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : Controller
    {
        private readonly IConfiguration _config;
        private readonly ILoginService _loginService;
        private readonly RecordsContext _context;
        private readonly DBOperations _dbOperation;
        private readonly RequestFilters _requestFilters;
        private readonly List<AgeBoarder> _ageBoarders;
        // private readonly ElasticClient _log;

        public TestController(
            IConfiguration config,
            ILoginService loginService,
            RecordsContext context,
            DBOperations dbOperation,
            RequestFilters requestFilters
            // ElasticClient log
            )
        {
            _config = config;
            _loginService = loginService;
            _context = context;
            _dbOperation = dbOperation;
            _requestFilters = requestFilters;
        
        }


        [HttpGet("Origin")]
        public IActionResult EfficiencyDashboardOrigin([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end,
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationIds[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization)
        {
            return Ok();
        }

        [HttpGet("Test")]
        public IActionResult EfficiencyDashboardTest([FromQuery(Name = "begTime")] string beg,
                                                    [FromQuery(Name = "endTime")] string end,
                                                    [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                    [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                    [FromQuery(Name = "corporationIds[]")] List<Guid> corporationIds,
                                                    [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                    [FromHeader] string Authorization)
        {
            return Ok();
        }

    }
}
