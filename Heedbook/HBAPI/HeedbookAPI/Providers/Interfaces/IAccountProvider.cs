using HBData;
using HBData.Models;
using HBData.Models.AccountViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserOperations.AccountModels;
using UserOperations.Models.AnalyticModels;

namespace UserOperations.Providers
{
    public interface IAccountProvider
    {
        int GetStatusId(string statusName);
        Task<bool> CompanyExist(string companyName);
        Task<bool> EmailExist(string email);
        Task<Company> AddNewCompanysInBase(UserRegister message, Guid companyId);
        Task<ApplicationUser> AddNewUserInBase(UserRegister message, Guid companyId);
        Task AddUserRoleInBase(UserRegister message, ApplicationUser user);
        int GetTariffs(Guid companyId);
        Task CreateCompanyTariffAndtransaction(Company company);
        Task AddWorkerType(Company company);
        Task AddContentAndCampaign(Company company);
        void SaveChangesAsync();
        void SaveChanges();
        ApplicationUser GetUserIncludeCompany(string email);
        ApplicationUser GetUserIncludeCompany(Guid userId, AccountAuthorization message);
        void RemoveAccount(string email);
    }
}