using System;
using System.Collections.Generic;
using System.Linq;
using UserOperations.Utils;
using System.Threading.Tasks;
using HBData.Repository;
using HBData.Models;
using UserOperations.Models;
using UserOperations.Controllers;

namespace UserOperations.Services
{
    public class ClientService 
    {
        private readonly LoginService _loginService;
        private readonly DBOperations _dbOperation;
        private readonly RequestFilters _requestFilters;
        private readonly IGenericRepository _repository;

        public ClientService(
            LoginService loginService,
            DBOperations dbOperation,
            RequestFilters requestFilters,
            IGenericRepository repository
            )
        {
            _loginService = loginService;
            _dbOperation = dbOperation;
            _requestFilters = requestFilters;
            _repository = repository;
        }
        public async Task<List<GetClient>> GetAll( string beg, string end, List<string> genders, List<Guid> companyIds, int begAge, int endAge)
        {
            var role = _loginService.GetCurrentRoleName();
            var companyId = _loginService.GetCurrentCompanyId();
            var begTime = _requestFilters.GetBegDate(beg);
            var endTime = _requestFilters.GetEndDate(end);
            _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, null, role, companyId);

            var data = _repository.GetAsQueryable<Client>()
                .Where(c => 
                    (c.Age >= begAge)
                    &&  (c.Age <= endAge)
                    && c.Dialogues.Any(d => d.BegTime >= begTime && d.EndTime <= endTime)
                    && (!genders.Any() || genders.Contains(c.Gender))
                    && (!companyIds.Any() ||companyIds.Contains(c.CompanyId)))
                .Select( c => new GetClient () {
                    ClientId = c.ClientId,
                    Age = c.Age,
                    Avatar = c.Avatar,
                    CompanyId = c.CompanyId,
                    CorporationId = c.CorporationId,
                    Email = c.Email,
                    Name = c.Name,
                    Gender = c.Gender,
                    Phone = c.Phone,
                    StatusId = c.StatusId,
                    ClientNotes = c.ClientNotes,
                    DialogueIds =  c.Dialogues.Where(d => d.BegTime >= begTime && d.EndTime <= endTime).Select(d => d.DialogueId)
                });
            return data.ToList();
            }
        

        public async Task<string> Update(PutClient client)
        {
            var role = _loginService.GetCurrentRoleName();
            var companyId = _loginService.GetCurrentCompanyId();
            var corporationId = _loginService.GetCurrentCorporationId();
            Client clientEntity = await _repository.FindOrExceptionOneByConditionAsync<Client>(c => c.ClientId == client.ClientId);
            _requestFilters.IsCompanyBelongToUser(corporationId, companyId, clientEntity.CompanyId, role);

            Type clientType = clientEntity.GetType();
            foreach (var p in typeof(PutClient).GetProperties())
            {
                var val = p.GetValue(client, null);
                if (val != null && val.ToString() != Guid.Empty.ToString() && p.Name != "ClientId")
                {
                    clientType.GetProperty(p.Name).SetValue(clientEntity, val);
                }
            }
            _repository.Update<Client>(clientEntity);
            await _repository.SaveAsync();
            return "Saved";
        }

        public async Task<string> Delete(Guid clientId)
        {
            var role = _loginService.GetCurrentRoleName();
            var companyId = _loginService.GetCurrentCompanyId();
            var corporationId = _loginService.GetCurrentCorporationId();
            Client clientEntity = await _repository.FindOrExceptionOneByConditionAsync<Client>(c => c.ClientId == clientId);
            _requestFilters.IsCompanyBelongToUser(corporationId, companyId, clientEntity.CompanyId, role);
            var dialogues = await _repository.FindByConditionAsync<Dialogue>(c => c.ClientId == clientId);

            dialogues.Select(d => { d.ClientId = null; return d; }).ToList();
            _repository.Delete<Client>(clientEntity);
            await _repository.SaveAsync();
            return "Deleted";
        }
    }
}