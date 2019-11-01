using System;
using System.Linq;
using System.Threading.Tasks;
using HBData;
using HBData.Models;
using HBData.Models.AccountViewModels;
using Microsoft.EntityFrameworkCore;
using UserOperations.AccountModels;
using UserOperations.Services;
using Newtonsoft.Json;

namespace UserOperations.Providers
{
    public class AccountProvider : IAccountProvider
    {
        private readonly RecordsContext _context;
        private readonly ILoginService _loginService;
        public AccountProvider(
            RecordsContext context,
            ILoginService loginService
        )
        {
            _context = context;
            _loginService = loginService;
        }

        public int GetStatusId(string statusName)
        {
            var statusId = _context.Statuss.FirstOrDefault(p => p.StatusName == statusName).StatusId;//---active
            return statusId;
        }
        public async Task<bool> CompanyExist(string companyName)
        {
            var companys = await _context.Companys.Where(x => x.CompanyName == companyName).ToListAsync();
            return companys.Any();
        }  
        
        public async Task<bool> EmailExist(string email)
        {
            var emails = await _context.ApplicationUsers.Where(x => x.NormalizedEmail == email.ToUpper()).ToListAsync();
            return emails.Any();
        }  
        
        public async Task<Company> AddNewCompanysInBase(UserRegister message, Guid companyId)
        {            
            var company = new Company
            {
                CompanyId = companyId,
                CompanyIndustryId = message.CompanyIndustryId,
                CompanyName = message.CompanyName,
                LanguageId = message.LanguageId,
                CreationDate = DateTime.UtcNow,
                CountryId = message.CountryId,
                CorporationId = message.CorporationId,
                StatusId = GetStatusId("Inactive")//---inactive
            };
            await _context.Companys.AddAsync(company);
            return company;
        }
        public async Task<ApplicationUser> AddNewUserInBase(UserRegister message, Guid companyId)
        {            
            var user = new ApplicationUser
            {
                UserName = message.Email,
                NormalizedUserName = message.Email.ToUpper(),
                Email = message.Email,
                NormalizedEmail = message.Email.ToUpper(),
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                CreationDate = DateTime.UtcNow,
                FullName = message.FullName,
                PasswordHash = _loginService.GeneratePasswordHash(message.Password),
                StatusId = GetStatusId("Active")
            };
            await _context.AddAsync(user);
            _loginService.SavePasswordHistory(user.Id, user.PasswordHash);
            return user;
        }      
        public async Task AddUserRoleInBase(UserRegister message, ApplicationUser user)
        {
            message.Role = message.Role ?? "Manager";
            var userRole = new ApplicationUserRole()
            {
                UserId = user.Id,
                RoleId = _context.Roles.First(p => p.Name == message.Role).Id //Manager or Supervisor role
            };
            await _context.ApplicationUserRoles.AddAsync(userRole);
        }  
        public int GetTariffs(Guid companyId)
        {
            var tariffsCount =_context.Tariffs.Where(item => item.CompanyId == companyId).ToList().Count;
            return tariffsCount;
        }
        public async Task CreateCompanyTariffAndtransaction(Company company)
        {
            var tariff = new Tariff
            {
                TariffId = Guid.NewGuid(),
                TotalRate = 0,
                CompanyId = company.CompanyId,
                CreationDate = DateTime.UtcNow,
                CustomerKey = "",
                EmployeeNo = 2,
                ExpirationDate = DateTime.UtcNow.AddDays(5),
                isMonthly = false,
                Rebillid = "",
                StatusId = _context.Statuss.FirstOrDefault(p => p.StatusName == "Trial").StatusId//---Trial
            };
            //---5---transaction---
            var transaction = new HBData.Models.Transaction
            {
                TransactionId = Guid.NewGuid(),
                Amount = 0,
                OrderId = "",
                PaymentId = "",
                TariffId = tariff.TariffId,
                StatusId = _context.Statuss.FirstOrDefault(p => p.StatusName == "Finished").StatusId,//---finished
                PaymentDate = DateTime.UtcNow,
                TransactionComment = "TRIAL TARIFF;FAKE TRANSACTION"
            };
            company.StatusId = GetStatusId("Active");
//                        _log.Info("Transaction created");
            await _context.Tariffs.AddAsync(tariff);
            await _context.Transactions.AddAsync(transaction);
        }
        public async Task AddWorkerType(Company company)
        {
            var workerType = new WorkerType
            {
                WorkerTypeId = Guid.NewGuid(),
                CompanyId = company.CompanyId,
                WorkerTypeName = "Employee"
            };
//                        _log.Info("WorkerTypes created");
            await _context.WorkerTypes.AddAsync(workerType);
        }
        public async Task AddContentAndCampaign(Company company)
        {
            Guid contentPrototypeId = new Guid("07565966-7db2-49a7-87d4-1345c729a6cb");
            var content = _context.Contents.FirstOrDefault(x => x.ContentId == contentPrototypeId);
            if (content != null)
            {
                content.ContentId = Guid.NewGuid();
                content.CompanyId = company.CompanyId;
                content.StatusId = GetStatusId("Active");
                await _context.Contents.AddAsync(content);

                Campaign campaign = new Campaign
                {
                    CampaignId = Guid.NewGuid(),
                    CompanyId = company.CompanyId,
                    BegAge = 0,
                    BegDate = DateTime.Now.AddDays(-1),
                    CreationDate = DateTime.Now,
                    EndAge = 100,
                    EndDate = DateTime.Now.AddDays(30),
                    GenderId = 0,
                    IsSplash = true,
                    Name = "PROTOTYPE",
                    StatusId = GetStatusId("Active")
                };
                await _context.Campaigns.AddAsync(campaign);
                CampaignContent campaignContent = new CampaignContent
                {
                    CampaignContentId = Guid.NewGuid(),
                    CampaignId = campaign.CampaignId,
                    ContentId = content.ContentId,
                    SequenceNumber = 1,
                    StatusId = GetStatusId("Active")
                };
                await _context.CampaignContents.AddAsync(campaignContent);
            }
        }
        public async void SaveChangesAsync()
        {
            await _context.SaveChangesAsync();           
        }        
        public void SaveChanges()
        {
            _context.SaveChanges();
        }

        public ApplicationUser GetUserIncludeCompany(string email)
        {
            var user = _context.ApplicationUsers.First(p => p.NormalizedEmail == email.ToUpper());
            return user;
        }
        public ApplicationUser GetUserIncludeCompany(Guid userId, AccountAuthorization message)
        {
            var user = _context.ApplicationUsers.FirstOrDefault(x => x.Id == userId && x.NormalizedEmail == message.UserName.ToUpper());
            return user;
        }
        public void RemoveAccount(string email)
        {
            var user = _context.ApplicationUsers.FirstOrDefault(p => p.Email == email);
            Company company = _context.Companys.FirstOrDefault(x => x.CompanyId == user.CompanyId);
            var users = _context.ApplicationUsers.Include(x=>x.UserRoles).Where(x => x.CompanyId == company.CompanyId).ToList();
            var tariff = _context.Tariffs.FirstOrDefault(x => x.CompanyId == company.CompanyId);
            var transactions = _context.Transactions.Where(x => x.TariffId == tariff.TariffId).ToList();
            var userRoles = users.SelectMany(x => x.UserRoles).ToList();
            var workerTypes = _context.WorkerTypes.Where(x => x.CompanyId == company.CompanyId).ToList();
            var contents = _context.Contents.Where(x => x.CompanyId == company.CompanyId).ToList();
            var campaigns = _context.Campaigns.Include(x => x.CampaignContents).Where(x => x.CompanyId == company.CompanyId).ToList();
            var campaignContents = campaigns.SelectMany(x => x.CampaignContents).ToList();
            var phrases = _context.PhraseCompanys.Where(x => x.CompanyId == company.CompanyId).ToList();
            var pswdHist = _context.PasswordHistorys.Where(x => users.Select(p=>p.Id).Contains( x.UserId)).ToList();

            if (pswdHist.Count() != 0)
                _context.RemoveRange(pswdHist);
            if (phrases != null && phrases.Count() != 0)
            _context.RemoveRange(phrases);
            if (campaignContents.Count() != 0)
                _context.RemoveRange(campaignContents);
            if (campaigns.Count() != 0)
                _context.RemoveRange(campaigns);
            if (contents.Count() != 0)
                _context.RemoveRange(contents);
            _context.RemoveRange(workerTypes);
            _context.RemoveRange(userRoles);
            _context.RemoveRange(transactions);
            _context.RemoveRange(users);
            _context.Remove(tariff);
            _context.Remove(company);
            _context.SaveChanges();
        }
    }
}