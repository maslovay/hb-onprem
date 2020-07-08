using HBData.Models;
using HBData.Repository;
using HBLib.Utils;
using HBLib.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserOperations.Services.Interfaces;

namespace UserOperations.Services
{
    public class CompanyService : ICompanyService
    {
        private readonly IGenericRepository _repository;
        private readonly ILoginService _loginService;
        private readonly IRequestFilters _requestFilters;

        private readonly int activeStatus;
        private readonly int disabledStatus;

        public CompanyService(
            IGenericRepository repository,
            ILoginService loginService,
            IRequestFilters requestFilters)
        {
            _repository = repository;
            _loginService = loginService;
            _requestFilters = requestFilters;
            activeStatus = 3;
            disabledStatus = 4;
        }

        //---COMPANY----
        public Company GetCompanyByIdAsync(Guid companyId)
        {
            var company =  _repository.GetWithIncludeOne<Company>(p => p.CompanyId == companyId, p => p.WorkingTimes);
            if (company == null) throw new NoFoundException("No such company");
            return company;
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

        public async Task<Company> UpdateCompanAsync(List<WorkingTime> times)
        {
            var roleInToken = _loginService.GetCurrentRoleName();
            var companyId = _loginService.GetCurrentCompanyId();
            var company = GetCompanyByIdAsync(companyId);
            

            //_requestFilters.IsCompanyBelongToUser(companyInParams.CompanyId);

            var workingTimes = company.WorkingTimes.ToList();
            foreach (var time in times)
            {
                if (time.BegTime == null ^ time.EndTime == null) continue;
                var timeEntity = workingTimes.Where(x => x.Day == time.Day).FirstOrDefault();
                timeEntity.BegTime = time.BegTime;
                timeEntity.EndTime = time.EndTime;
            }
            await _repository.SaveAsync();
            return GetCompanyByIdAsync(companyId);
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
            await AddOneWorkingTimeAsync(companyId, null, null, 0);
        }

        public async Task AddOneWorkingTimeAsync(Guid companyId, DateTime? beg, DateTime? end, int day)
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
