using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;
using UserOperations.Models;
using Microsoft.AspNetCore.Authorization;
using UserOperations.Services;
using HBData.Models;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ControllerExceptionFilter]
    public class ClientController : Controller
    {
        private readonly ClientService _clientService;
        public ClientController( ClientService clientService)
        {
            _clientService = clientService;
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

        [HttpPut]
        [SwaggerOperation(Summary = "set client email, name, phone", Description = "")]
        [SwaggerResponse(200, "Saved", typeof(string))]
        public async Task<string> ClientUpdate([FromBody] PutClient client)
         => await _clientService.Update(client);

        [HttpDelete]
        [SwaggerOperation(Summary = "delete client and set null client's id in dialodues", Description = "")]
        [SwaggerResponse(200, "Deleted", typeof(string))]
        public async Task<string> ClientDelete([FromQuery] Guid clientId)
        => await _clientService.Delete(clientId);
    }
}
