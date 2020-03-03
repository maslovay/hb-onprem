using System;
using System.Collections.Generic;
using System.Linq;
using UserOperations.Utils;
using System.Threading.Tasks;
using HBData.Repository;
using HBData.Models;
using UserOperations.Models;

namespace UserOperations.Services
{
    public class ClientNoteService 
    {
        private readonly LoginService _loginService;
        private readonly RequestFilters _requestFilters;
        private readonly IGenericRepository _repository;

        public ClientNoteService(
            LoginService loginService,
            RequestFilters requestFilters,
            IGenericRepository repository
            )
        {
            _loginService = loginService;
            _requestFilters = requestFilters;
            _repository = repository;
        }
        public async Task<ICollection<GetClientNote>> GetAll(Guid clientId)
        {
            var role = _loginService.GetCurrentRoleName();
            var companyId = _loginService.GetCurrentCompanyId();
            var corporationId = _loginService.GetCurrentCorporationId();

            Client clientEntity = await _repository.FindOrExceptionOneByConditionAsync<Client>(c => c.ClientId == clientId);
            _requestFilters.IsCompanyBelongToUser(corporationId, companyId, clientEntity.CompanyId, role);

            return _repository.GetAsQueryable<ClientNote>()
                .Where(c => c.ClientId == clientId)
                .Select(c => new GetClientNote(c, c.ApplicationUser))
                .ToList();
        }

        public async Task<ClientNote> Create(PostClientNote clientNote)
        {
            var role = _loginService.GetCurrentRoleName();
            var companyId = _loginService.GetCurrentCompanyId();
            var corporationId = _loginService.GetCurrentCorporationId();
            Guid? userId = null;
            try
            {
                userId = _loginService.GetCurrentUserId();
            }
            catch { }

            Guid clientCompanyId = _repository.GetAsQueryable<Client>()
                        .Where(x => x.ClientId == clientNote.ClientId)
                        .Select(x => x.CompanyId)
                        .FirstOrDefault();
            _requestFilters.IsCompanyBelongToUser(corporationId, companyId, clientCompanyId, role);

            ClientNote newNote = new ClientNote
            {
                ApplicationUserId = userId,
                ClientId = clientNote.ClientId,
                CreationDate = DateTime.UtcNow,
                Text = clientNote.Text
            };

            await _repository.CreateAsync<ClientNote>(newNote);
            await _repository.SaveAsync();
            return newNote;
        }


        public async Task<ClientNote> Update(PutClientNote clientNote)
        {
            var role = _loginService.GetCurrentRoleName();
            var companyId = _loginService.GetCurrentCompanyId();
            var corporationId = _loginService.GetCurrentCorporationId();

            ClientNote clientNoteEntity = await _repository
                    .FindOrExceptionOneByConditionAsync<ClientNote>(x => x.ClientNoteId == clientNote.ClientNoteId);

            Guid clientCompanyId = _repository.GetAsQueryable<Client>()
                        .Where(x => x.ClientId == clientNoteEntity.ClientId)
                        .Select(x => x.CompanyId)
                        .FirstOrDefault();
            _requestFilters.IsCompanyBelongToUser(corporationId, companyId, clientCompanyId, role);

            Type clientNoteType = clientNoteEntity.GetType();
            foreach (var p in typeof(PutClientNote).GetProperties())
            {
                var val = p.GetValue(clientNote, null);
                if (val != null && val.ToString() != Guid.Empty.ToString() && p.Name != "ClientNoteId")
                {
                    clientNoteType.GetProperty(p.Name).SetValue(clientNoteEntity, val);
                }
            }
            _repository.Update<ClientNote>(clientNoteEntity);
            await _repository.SaveAsync();
            return clientNoteEntity;
        }

        public async Task<string> Delete(Guid clientNoteId)
        {
            var role = _loginService.GetCurrentRoleName();
            var companyId = _loginService.GetCurrentCompanyId();
            var corporationId = _loginService.GetCurrentCorporationId();

            ClientNote clientNoteEntity = await _repository
                    .FindOrExceptionOneByConditionAsync<ClientNote>(x => x.ClientNoteId == clientNoteId);

            Guid clientCompanyId = _repository.GetAsQueryable<Client>()
                        .Where(x => x.ClientId == clientNoteEntity.ClientId)
                        .Select(x => x.CompanyId)
                        .FirstOrDefault();
            _requestFilters.IsCompanyBelongToUser(corporationId, companyId, clientCompanyId, role);

            _repository.Delete<ClientNote>(clientNoteEntity);
            await _repository.SaveAsync();
            return "Deleted";
        }
    }
}