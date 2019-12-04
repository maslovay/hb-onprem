using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using UserOperations.Services;
using UserOperations.Models.AnalyticModels;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using HBData;
using UserOperations.Utils;
using HBLib.Utils;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticRatingController : Controller
    {
      private readonly IConfiguration _config;        
        private readonly ILoginService _loginService;
        private readonly RecordsContext _context;
        private readonly IDBOperations _dbOperation;
        private readonly IRequestFilters _requestFilters;
        private readonly AnalyticRatingService _analyticRatingService;

        public AnalyticRatingController(
            IConfiguration config,
            ILoginService loginService,
            RecordsContext context,
            IDBOperations dbOperation,
            IRequestFilters requestFilters,
            AnalyticRatingService analyticRatingService
            )
        {
            _config = config;
            _loginService = loginService;
            _context = context;
            _dbOperation = dbOperation;
            _requestFilters = requestFilters;
            _analyticRatingService = analyticRatingService;
        }

        [HttpGet("Progress")]
        public IActionResult RatingProgress([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization) =>
            _analyticRatingService.RatingProgress(
                beg,
                end,
                applicationUserIds,
                companyIds,
                corporationIds,
                workerTypeIds,
                Authorization
            );
        


        [HttpGet("RatingUsers")]
        public IActionResult RatingUsers([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization) =>
            _analyticRatingService.RatingUsers(
                beg,
                end,
                applicationUserIds,
                companyIds,
                corporationIds,
                workerTypeIds,
                Authorization
            );
        


        [HttpGet("RatingOffices")]
        public IActionResult RatingOffices([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization) =>
            _analyticRatingService.RatingOffices(
                beg,
                end,
                companyIds,
                corporationIds,
                workerTypeIds,
                Authorization
            );
                 
    }
}