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
        

        public IEnumerable<Company> GetCompaniesForAdmin()
        {
            var companies =  _repository.GetWithInclude<Company>(p => p.StatusId == activeStatus || p.StatusId == disabledStatus, p=>p.WorkingTimes);
            return companies ?? new List<Company>();
        }

        public IEnumerable<Company> GetCompaniesForSupervisorAsync(Guid? corporationId)
        {
            if (corporationId == null || corporationId == Guid.Empty) return new List<Company>();
            var companies = _repository.GetWithInclude<Company>(p => 
                        p.CorporationId == corporationId 
                        && (p.StatusId == activeStatus || p.StatusId == disabledStatus), p=> p.WorkingTimes);
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

            var workingTimes = companyInParams.WorkingTimes;
            foreach (var time in workingTimes)
            {
                var timeEntity = await _repository
                        .FindOrNullOneByConditionAsync<WorkingTime>(x => x.Day == time.Day && x.CompanyId == time.CompanyId);
                foreach (var p in typeof(WorkingTime).GetProperties())
                {
                    var val = p.GetValue(time, null);
                    if (val != null && val.ToString() != Guid.Empty.ToString())
                        p.SetValue(timeEntity, p.GetValue(time, null), null);
                }
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
            await AddDefaultWorkingTime(company.CompanyId);
            await _repository.SaveAsync();
            return company;
        }

        //---PRIVATE---

        public async Task AddDefaultWorkingTime(Guid companyId)
        {
            await AddOneWorkingTimeAsync(companyId, new DateTime(1,1,1,10,0,0), new DateTime(1,1,1, 19, 0, 0), 1);
            await AddOneWorkingTimeAsync(companyId, new DateTime(1,1,1,10,0,0), new DateTime(1,1,1, 19, 0, 0), 2);
            await AddOneWorkingTimeAsync(companyId, new DateTime(1,1,1,10,0,0), new DateTime(1,1,1, 19, 0, 0), 3);
            await AddOneWorkingTimeAsync(companyId, new DateTime(1,1,1,10,0,0), new DateTime(1,1,1, 19, 0, 0), 4);
            await AddOneWorkingTimeAsync(companyId, new DateTime(1, 1, 1, 10, 0, 0), new DateTime(1, 1, 1, 19, 0, 0), 5);
            await AddOneWorkingTimeAsync(companyId, null, null, 6);
            await AddOneWorkingTimeAsync(companyId, null, null, 7);
        }

        private async Task AddOneWorkingTimeAsync(Guid companyId, DateTime? beg, DateTime? end, int day)
        {
            WorkingTime time = new WorkingTime
            {
                CompanyId = companyId,
                Day = day,
                BegTime = beg,
                EndTime = end
            };
            await _repository.CreateAsync<WorkingTime>(time);
        }
    }
}
