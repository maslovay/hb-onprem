using HBData.Models;
using HBData.Repository;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UserOperations.Utils;

namespace UserOperations.Services
{
    public class CompanyService
    {
        private readonly IGenericRepository _repository;
        private readonly LoginService _loginService;
        private readonly RequestFilters _requestFilters;

        private readonly int activeStatus;
        private readonly int disabledStatus;

        public CompanyService(
            IGenericRepository repository, 
            LoginService loginService,
            RequestFilters requestFilters)
        {
            _repository = repository;
            _loginService = loginService;
            _requestFilters = requestFilters;
            activeStatus = 3;
            disabledStatus = 4;
        }

        //---COMPANY----
        public async Task<Company> GetCompanyByIdAsync(Guid companyId)
        {
            return await _repository.FindOrExceptionOneByConditionAsync<Company>(p => p.CompanyId == companyId);
        }
        

        public async Task<IEnumerable<Company>> GetCompaniesForAdminAsync()
        {
            var companies =  await _repository.FindByConditionAsync<Company>(p => p.StatusId == activeStatus || p.StatusId == disabledStatus);
            return companies ?? new List<Company>();
        }

        public async Task<IEnumerable<Company>> GetCompaniesForSupervisorAsync(Guid? corporationId)
        {
            if (corporationId == null || corporationId == Guid.Empty) return new List<Company>();
            var companies = await _repository.FindByConditionAsync<Company>(p => 
                        p.CorporationId == corporationId 
                        && (p.StatusId == activeStatus || p.StatusId == disabledStatus));
            return companies ?? new List<Company>();
        }

        public async Task<IEnumerable<Corporation>> GetCorporationAsync()
        {
            return await _repository.FindAllAsync<Corporation>();
        }

        public async Task<Company> UpdateCompanAsync(Company companyInParams)
        {
            var roleInToken = _loginService.GetCurrentRoleName();
            var entity = await GetCompanyByIdAsync(companyInParams.CompanyId);

            _requestFilters.IsCompanyBelongToUser(companyInParams.CompanyId);

            foreach (var p in typeof(Company).GetProperties())
            {
                var val = p.GetValue(companyInParams, null);
                if (val != null && val.ToString() != Guid.Empty.ToString())
                    p.SetValue(entity, p.GetValue(companyInParams, null), null);
            }
            await _repository.SaveAsync();
            return entity;
        }

        public async Task<Company> AddNewCompanyAsync(Company company, Guid? corporationId = null)
        {
            company.CreationDate = DateTime.UtcNow;
            company.StatusId = activeStatus;
            company.CorporationId = corporationId;
            _repository.Create<Company>(company);
            await _repository.SaveAsync();
            return company;
        }

        //---PRIVATE---
    }
}
