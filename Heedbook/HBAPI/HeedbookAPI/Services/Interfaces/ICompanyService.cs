using HBData.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UserOperations.Services
{
    public interface ICompanyService
    {
        Task AddDefaultWorkingTime(Guid companyId);
        Task<Company> AddNewCompanyAsync(Company company, Guid? corporationId = null);
        Task AddOneWorkingTimeAsync(Guid companyId, DateTime? beg, DateTime? end, int day);
        IEnumerable<Company> GetCompaniesForAdmin();
        IEnumerable<Company> GetCompaniesForSupervisorAsync(Guid? corporationId);
        Company GetCompanyByIdAsync(Guid companyId);
        Task<IEnumerable<Corporation>> GetCorporationAsync();
        Task<Company> UpdateCompanAsync(List<WorkingTime> times);
    }
}