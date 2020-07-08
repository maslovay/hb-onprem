using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using UserOperations.Models;
using HBLib.Utils;
using UserOperations.Services.Interfaces;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ControllerExceptionFilter]
    public class SalesStageController : Controller
    {
        private readonly ISalesStageService _salesStageService;
        public SalesStageController(ISalesStageService salesStageService)
        {
            _salesStageService = salesStageService;
        }

        [HttpGet("SalesStages")]
        [SwaggerOperation(Summary = "Return a list of sales stages ", Description = "Get list of SalesStage for one companyId")]
        [SwaggerResponse(200, "SalesStage[]", typeof(List<GetSalesStage>))]
        public async Task<ICollection<GetSalesStage>> SalesStagesGet([FromQuery(Name = "companyId")] Guid? companyId) 
            => await _salesStageService.GetAll(companyId);
    }
}
