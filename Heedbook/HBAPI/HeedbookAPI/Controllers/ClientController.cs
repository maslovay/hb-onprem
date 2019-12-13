using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;
using UserOperations.Models;
using Microsoft.AspNetCore.Authorization;
using UserOperations.Services;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ControllerExceptionFilter]
    public class ClientController : Controller
    {
        private readonly ClientService _clientService;
        public ClientController( ClientService ñlientService )
        {
            _clientService = ñlientService;
        }


        [HttpGet("List")]
        [SwaggerOperation(Summary = "list of cliets", Description = "with dialogue ids and client notes")]
        [SwaggerResponse(200, "Clients[]", typeof(List<GetClient>))]
        public async Task<List<GetClient>> ClientsGet([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end,
                                                        [FromQuery(Name = "genders[]")] List<string> genders,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "begAge")] int begAge = 0,
                                                        [FromQuery(Name = "endAge")] int endAge = 100 ) 
            => await _clientService.GetAll( beg, end, genders, companyIds, begAge, endAge);
    }
}
