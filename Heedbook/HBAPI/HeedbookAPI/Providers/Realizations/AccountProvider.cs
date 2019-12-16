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
using HBData.Repository;

namespace UserOperations.Providers
{
    public class AccountProvider : IAccountProvider
    {
        private readonly LoginService _loginService;
        private readonly IGenericRepository _repository;
        public AccountProvider(
            LoginService loginService,
            IGenericRepository repository
        )
        {
            _loginService = loginService;
            _repository = repository;
        }

        public int GetStatusId(string statusName)
        {
            var task = _repository.FindOrNullOneByConditionAsync<Status>(p => p.StatusName == statusName);
            task.Wait();
            var statusId = task.Result.StatusId;
            return statusId;
        }
        public async Task<bool> CompanyExist(string companyName)
        {
            var companys = await _repository.GetAsQueryable<Company>().Where(x => x.CompanyName == companyName).ToListAsync();            
            return companys.Any();
        }  
        
        public async Task<bool> EmailExist(string email)
        {
            var emails = await _repository.GetAsQueryable<ApplicationUser>().Where(x => x.NormalizedEmail == email.ToUpper()).ToListAsync();            
            return emails.Any();
        }  
        
        public Company AddNewCompanysInBase(UserRegister message)
        {
            var company = new Company
            {
                CompanyIndustryId = message.CompanyIndustryId,
                CompanyName = message.CompanyName,
                LanguageId = message.LanguageId,
                CreationDate = DateTime.UtcNow,
                CountryId = message.CountryId,
                CorporationId = message.CorporationId,
                StatusId = GetStatusId("Inactive")
            };
            _repository.Create<Company>(company);
            return company;
        }
        public async Task<ApplicationUser> AddNewUserInBase(UserRegister message, Guid? companyId)
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
            await _repository.CreateAsync<ApplicationUser>(user);
            _loginService.SavePasswordHistory(user.Id, user.PasswordHash);
            return user;
        }      
        public async Task AddUserRoleInBase(UserRegister message, ApplicationUser user)
        {
            message.Role = message.Role ?? "Manager";
            var roleId = _repository.GetAsQueryable<ApplicationRole>().FirstOrDefault(p => p.Name == message.Role).Id;
            var userRole = new ApplicationUserRole()
            {
                UserId = user.Id,
                RoleId = roleId
            };
            await _repository.CreateAsync<ApplicationUserRole>(userRole);
        }  
        public async Task<int> GetTariffsAsync(Guid? companyId)
        {
            var tariffs = await _repository.FindByConditionAsync<Tariff>(item => item.CompanyId == companyId);
            return tariffs.Count();
        }
        public async Task CreateCompanyTariffAndTransaction(Company company)
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
                StatusId = (await _repository.FindOrNullOneByConditionAsync<Status>(p => p.StatusName == "Trial")).StatusId//---Trial
            };            
            //---5---transaction---
            var transaction = new HBData.Models.Transaction
            {
                TransactionId = Guid.NewGuid(),
                Amount = 0,
                OrderId = "",
                PaymentId = "",
                TariffId = tariff.TariffId,
                StatusId = (await _repository.FindOrNullOneByConditionAsync<Status>(p => p.StatusName == "Finished")).StatusId,//---finished
                PaymentDate = DateTime.UtcNow,
                TransactionComment = "TRIAL TARIFF;FAKE TRANSACTION"
            };
            
            company.StatusId = GetStatusId("Active");
//                        _log.Info("Transaction created");
            _repository.Create<Tariff>(tariff);
            _repository.Create<Transaction>(transaction);
        }
        public async Task AddWorkerType(Company company)
        {
            var workerType = new WorkerType
            {
                WorkerTypeId = Guid.NewGuid(),
                CompanyId = company.CompanyId,
                WorkerTypeName = "Employee"
            };
            await _repository.CreateAsync<WorkerType>(workerType);
        }
        public async Task AddContentAndCampaign(Company company)
        {
            Guid contentPrototypeId = new Guid("07565966-7db2-49a7-87d4-1345c729a6cb");
            var content = await _repository.FindOrNullOneByConditionAsync<Content>(p => p.ContentId == contentPrototypeId);
            if (content != null)
            {
                content.ContentId = Guid.NewGuid();
                content.CompanyId = company.CompanyId;
                content.StatusId = GetStatusId("Active");
                _repository.Create<Content>(content);

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
                _repository.Create<Campaign>(campaign);
                CampaignContent campaignContent = new CampaignContent
                {
                    CampaignContentId = Guid.NewGuid(),
                    CampaignId = campaign.CampaignId,
                    ContentId = content.ContentId,
                    SequenceNumber = 1,
                    StatusId = GetStatusId("Active")
                };
                _repository.Create<CampaignContent>(campaignContent);
            }
        }
        public async Task SaveChangesAsync()
        {
            await _repository.SaveAsync();
        }        
        public void SaveChanges()
        {
            _repository.Save();
        }

        public ApplicationUser GetUserIncludeCompany(string email)
        {
            var user = _repository.GetWithIncludeOne<ApplicationUser>(p => p.NormalizedEmail == email.ToUpper(), o => o.Company);
            return user;
        }
        public ApplicationUser GetUserIncludeCompany(Guid userId, AccountAuthorization message)
        {
            var user = _repository.GetWithIncludeOne<ApplicationUser>(x => x.Id == userId && x.NormalizedEmail == message.UserName.ToUpper(), o => o.Company);
            return user;
        }
        public async Task RemoveAccountWithSave(string email)
        {            
            var user = _repository.GetAsQueryable<ApplicationUser>().FirstOrDefault(p => p.Email == email);
            var company = _repository.GetAsQueryable<Company>().FirstOrDefault(x => x.CompanyId == user.CompanyId);
            var users = _repository.GetWithInclude<ApplicationUser>(x => x.CompanyId == company.CompanyId, o => o.UserRoles).ToList();            
            var tariff = _repository.GetAsQueryable<Tariff>().FirstOrDefault(x => x.CompanyId == company.CompanyId);
            
            var taskTransactions = _repository.GetAsQueryable<Transaction>().Where(x => x.TariffId == tariff.TariffId).ToListAsync();
            taskTransactions.Wait();
            var transactions = taskTransactions.Result;            
            
            var userRoles = users.SelectMany(x => x.UserRoles).ToList();    

            var workerTypeTask = _repository.GetAsQueryable<WorkerType>().Where(x => x.CompanyId == company.CompanyId).ToListAsync();
            workerTypeTask.Wait();
            var workerTypes = workerTypeTask.Result;
            
            var taskContents = _repository.GetAsQueryable<Content>().Where(x => x.CompanyId == company.CompanyId).ToListAsync();            
            taskContents.Wait();
            var contents = taskContents.Result;

            var campaigns = _repository.GetWithInclude<Campaign>(x => x.CompanyId == company.CompanyId, p => p.CampaignContents).ToList();
            var campaignContents = campaigns.SelectMany(x => x.CampaignContents).ToList();
            var taskPhraseCompany = _repository.GetAsQueryable<PhraseCompany>().Where(x => x.CompanyId == company.CompanyId).ToListAsync();
            taskPhraseCompany.Wait();
            var phrases = taskPhraseCompany.Result;
            
            var taskPasswordHistory = _repository.GetAsQueryable<PasswordHistory>().Where(x => users.Select(p=>p.Id).Contains( x.UserId)).ToListAsync();
            taskPasswordHistory.Wait();
            var pswdHist = taskPasswordHistory.Result;
            if (pswdHist.Count() != 0)
                _repository.Delete<PasswordHistory>(pswdHist);            
            if (phrases != null && phrases.Count() != 0)
                _repository.Delete<PhraseCompany>(phrases);                
            if (campaignContents.Count() != 0)
                _repository.Delete<CampaignContent>(campaignContents); 
            if (campaigns.Count() != 0)
                _repository.Delete<Campaign>(campaigns);
            if (contents.Count() != 0)
                _repository.Delete<Content>(contents);
            _repository.Delete<WorkerType>(workerTypes);
            _repository.Delete<ApplicationUserRole>(userRoles);
            _repository.Delete<Transaction>(transactions);
            _repository.Delete<ApplicationUser>(users);
            _repository.Delete<Tariff>(tariff);
            _repository.Delete<Company>(company);
            await _repository.SaveAsync();
        }
    }
}