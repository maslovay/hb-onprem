using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;
using UserOperations.Models;
using Microsoft.AspNetCore.Authorization;
using UserOperations.Services;
using HBData.Models;
using HBLib.Utils;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ControllerExceptionFilter]
    public class ClientController : Controller
    {
        private readonly ClientService _clientService;
        private readonly ClientNoteService _clientNoteService;
        public ClientController( ClientService clientService, ClientNoteService clientNoteService)
        {
            _clientService = clientService;
            _clientNoteService = clientNoteService;
        }


        [HttpGet("List")]
        [SwaggerOperation(Summary = "list of cliets", Description = "with dialogue ids and client notes")]
        [SwaggerResponse(200, "Clients[]", typeof(List<GetClient>))]
        public async Task<ICollection<GetClient>> ClientsGet([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end,
                                                        [FromQuery(Name = "genders[]")] List<string> genders,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "begAge")] int begAge = 0,
                                                        [FromQuery(Name = "endAge")] int endAge = 100 ) 
            => await _clientService.GetAll( beg, end, genders, companyIds, begAge, endAge);

        [HttpGet]
        [SwaggerOperation(Summary = "one client by id", Description = "with dialogue ids and client notes")]
        [SwaggerResponse(200, "Client", typeof(GetClient))]
        public async Task<GetClient> ClientGet([FromQuery(Name = "clientId")] Guid clientId)
         => await _clientService.Get(clientId);

        [HttpPut]
        [SwaggerOperation(Summary = "set client email, name, phone", Description = "")]
        [SwaggerResponse(200, "Client", typeof(Client))]
        public async Task<Client> ClientUpdate([FromBody] PutClient client)
         => await _clientService.Update(client);

        [HttpDelete]
        [SwaggerOperation(Summary = "delete client and set null client's id in dialodues", Description = "")]
        [SwaggerResponse(200, "Deleted", typeof(string))]
        public async Task<string> ClientDelete([FromQuery] Guid clientId)
        => await _clientService.Delete(clientId);


        [HttpGet("Clientnote")]
        [SwaggerOperation(Summary = "list of client's notes", Description = "for one client")]
        [SwaggerResponse(200, "ClientNotes[]", typeof(List<ClientNote>))]
        public async Task<ICollection<GetClientNote>> ClientNotesGet([FromQuery(Name = "clientId")] Guid clientId)
           => await _clientNoteService.GetAll(clientId);

        [HttpPost("Clientnote")]
        [SwaggerOperation(Summary = "add new note for client", Description = "")]
        [SwaggerResponse(200, "ClientNote", typeof(ClientNote))]
        public async Task<ClientNote> ClientNotesCreate([FromBody] PostClientNote clientNote)
        => await _clientNoteService.Create(clientNote);

        [HttpPut("Clientnote")]
        [SwaggerOperation(Summary = "change text of note", Description = "")]
        [SwaggerResponse(200, "ClientNote", typeof(ClientNote))]
        public async Task<ClientNote> ClientNotesUpdate([FromBody] PutClientNote clientNote)
         => await _clientNoteService.Update(clientNote);

        [HttpDelete("Clientnote")]
        [SwaggerOperation(Summary = "delete client's note", Description = "")]
        [SwaggerResponse(200, "Deleted", typeof(string))]
        public async Task<string> ClientNotesDelete([FromQuery] Guid clientNoteId)
        => await _clientNoteService.Delete(clientNoteId);
    }
}
