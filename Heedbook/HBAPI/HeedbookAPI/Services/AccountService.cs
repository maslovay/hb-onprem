using System;
using System.Linq;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Models.AccountViewModels;
using Microsoft.EntityFrameworkCore;
using UserOperations.AccountModels;
using HBData.Repository;
using System.Transactions;
using System.Collections.Generic;
using UserOperations.Services.Interfaces;
using UserOperations.Utils.Interfaces;
using HBLib.Utils.Interfaces;

namespace UserOperations.Services
{
    public class AccountService
    {
        private readonly ILoginService _loginService;
        private readonly ICompanyService _companyService;
        private readonly ISalesStageService _salesStageService;
        private readonly IGenericRepository _repository;
        private readonly IMailSender _mailSender;
        private readonly ISpreadsheetDocumentUtils _helpProvider;


        public AccountService(
            ILoginService loginService,
            ICompanyService companyService,
            ISalesStageService salesStageService,
            IGenericRepository repository,
            IMailSender mailSender,
            ISpreadsheetDocumentUtils helpProvider
        )
        {
            _loginService = loginService;
            _companyService = companyService;
            _salesStageService = salesStageService;
            _repository = repository;
            _mailSender = mailSender;
            _helpProvider = helpProvider;
        }

        public async Task<Dictionary<string, bool>> ValidateToken(string token)
        {
            try
            {
                return new Dictionary<string, bool>() { { "status", _loginService.GetDataFromToken(token, out var claims) } };
            }
            catch
            {
                return new Dictionary<string, bool>() { { "status", false } };
            }
        }

            public async Task RegisterNewCompanyAndUser(UserRegister message)
        {
            var statusActiveId = GetStatusId("Active");
            if (await CompanyExist(message.CompanyName) || await EmailExist(message.Email))
                throw new Exception("Company name or user email not unique");

            var company = AddNewCompanysInBase(message);
            var user = await AddNewUserInBase(message, company?.CompanyId);
            await AddUserRoleInBase(message, user);
            await _repository.SaveAsync();
            
            if (await GetTariffsAsync(company?.CompanyId) == 0)
            {
                await CreateCompanyTariffAndTransaction(company);
                await AddContentAndCampaign(company);
            }
            await AddWorkingTime(company.CompanyId, message);
            await _salesStageService.CreateSalesStageForNewAccount(company.CompanyId, company.CorporationId);
            await _repository.SaveAsync();
            
            try
            {
                await _mailSender.SendRegisterEmail(user);
            }
            catch(Exception e)
            {
                System.Console.WriteLine(e);
            }
        }

       

        public string GenerateToken(AccountAuthorization message)
        {
                var user = GetUserIncludeCompany(message.UserName);
                if (user.StatusId != GetStatusId("Active")) throw new Exception("User not activated");

                if (_loginService.CheckUserLogin(message.UserName, message.Password))
                      return _loginService.CreateTokenForUser(user);
                else
                    throw new UnauthorizedAccessException("Error in username or password");
        }

        public async Task<string> ChangePassword(AccountAuthorization message, string token = null)
        {
                ApplicationUser user = null;
            //---FOR LOGGINED USER CHANGE PASSWORD WITH INPUT (receive new password in body message.Password)
            try
            {
                _loginService.GetDataFromToken(token, out var userClaims);
                var userId = Guid.Parse(userClaims["applicationUserId"]);
                user = GetUserIncludeCompany(userId, message);
                user.PasswordHash = _loginService.GeneratePasswordHash(message.Password);
                await _repository.SaveAsync();
                return "Password changed";
            }
            catch { }
            //---IF USER NOT LOGGINED HE RECEIVE GENERATED PASSWORD ON EMAIL
                user = GetUserIncludeCompany(message.UserName);
                string password = _loginService.GeneratePass(6);
                await _mailSender.SendPasswordChangeEmail(user, password);
                user.PasswordHash = _loginService.GeneratePasswordHash(password);
                await _repository.SaveAsync();
                return "Password changed";
        }

        public async Task<string> ChangePasswordOnDefault(string email)
        {
            _loginService.GetCurrentUserId();
            var user = GetUserIncludeCompany(email);
            user.PasswordHash = _loginService.GeneratePasswordHash("Test_User12345");
            await _repository.SaveAsync();
            return "Password changed";
        }

        public async Task<string> DeleteCompany(string email)//for own use
        {
            _loginService.GetCurrentUserId();
            using (var transactionScope = new TransactionScope(TransactionScopeOption.Suppress, new TransactionOptions()
                       { IsolationLevel = IsolationLevel.Serializable }))
            {
                try{
                    RemoveAccountWithSave(email);
                    transactionScope.Complete();
                    return "Removed";
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    return "fail";
                }
            }
        }

        public void AddPhrasesFromExcel(string fileName)//for own use
        {
            _loginService.GetCurrentUserId();
            _helpProvider.AddComanyPhrases(fileName);
        }
        public void DeleteUser(string email)
        {
            var user = _repository.GetAsQueryable<ApplicationUser>().Include(x => x.UserRoles).FirstOrDefault(p => p.Email == email);

            _repository.Delete<ApplicationUserRole>(user.UserRoles);
            _repository.Delete<ApplicationUser>(user);
            _repository.Save();
        }


        //---PRIVATE---
        private int GetStatusId(string statusName)
        {
            var task = _repository.FindOrNullOneByConditionAsync<Status>(p => p.StatusName == statusName);
            task.Wait();
            var statusId = task.Result.StatusId;
            return statusId;
        }
        private async Task<bool> CompanyExist(string companyName)
        {
            var companys = await _repository.GetAsQueryable<Company>().Where(x => x.CompanyName == companyName).ToListAsync();
            return companys.Any();
        }
        private async Task<bool> EmailExist(string email)
        {
            var emails = await _repository.GetAsQueryable<ApplicationUser>().Where(x => x.NormalizedEmail == email.ToUpper()).ToListAsync();
            return emails.Any();
        }
        private Company AddNewCompanysInBase(UserRegister message)
        {
            var company = new Company
            {
                CompanyIndustryId = message.CompanyIndustryId,
                CompanyName = message.CompanyName,
                LanguageId = message.LanguageId,
                CreationDate = DateTime.UtcNow,
                CountryId = message.CountryId,
                CorporationId = message.CorporationId,
                StatusId = GetStatusId("Inactive"),
                IsExtended = message.IsExtended?? true
            };
            _repository.Create<Company>(company);
            return company;
        }
        private async Task<ApplicationUser> AddNewUserInBase(UserRegister message, Guid? companyId)
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
        private async Task AddUserRoleInBase(UserRegister message, ApplicationUser user)
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
        private async Task<int> GetTariffsAsync(Guid? companyId)
        {
            var tariffs = await _repository.FindByConditionAsync<Tariff>(item => item.CompanyId == companyId);
            return tariffs.Count();
        }
        private async Task CreateCompanyTariffAndTransaction(Company company)
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
            _repository.Create<HBData.Models.Transaction>(transaction);
        }
        private async Task AddWorkingTime(Guid companyId, UserRegister mess)
        {
            TimeSpan timeZone;
            if(mess.TimeZone is null)
                timeZone = TimeSpan.Zero;
            else
            {
                var tmpTimeZone = mess.TimeZone.Contains("-") 
                    ? -TimeSpan.Parse(mess.TimeZone.Trim(new char[]{'+', '-'})) 
                    : TimeSpan.Parse(mess.TimeZone.Trim(new char[]{'+', '-'}));
                timeZone = (tmpTimeZone.Hours >= -12 && tmpTimeZone.Hours <= 14) ? tmpTimeZone : TimeSpan.Zero;
            } 
            
            if(mess.MondayBeg == null)
            {
                await AddOneWorkingTimeAsync(companyId, new DateTime(1, 1, 1, 10, 0, 0), new DateTime(1, 1, 1, 19, 0, 0), 1, timeZone);
                await AddOneWorkingTimeAsync(companyId, new DateTime(1, 1, 1, 10, 0, 0), new DateTime(1, 1, 1, 19, 0, 0), 2, timeZone);
                await AddOneWorkingTimeAsync(companyId, new DateTime(1, 1, 1, 10, 0, 0), new DateTime(1, 1, 1, 19, 0, 0), 3, timeZone);
                await AddOneWorkingTimeAsync(companyId, new DateTime(1, 1, 1, 10, 0, 0), new DateTime(1, 1, 1, 19, 0, 0), 4, timeZone);
                await AddOneWorkingTimeAsync(companyId, new DateTime(1, 1, 1, 10, 0, 0), new DateTime(1, 1, 1, 19, 0, 0), 5, timeZone);
                await AddOneWorkingTimeAsync(companyId, null, null, 6, timeZone);
                await AddOneWorkingTimeAsync(companyId, null, null, 0, timeZone);
            }
            else
            {
                await AddOneWorkingTimeAsync(companyId, mess.MondayBeg, mess.MondayEnd, 1, timeZone);
                await AddOneWorkingTimeAsync(companyId, mess.TuesdayBeg, mess.TuesdayEnd, 2, timeZone);
                await AddOneWorkingTimeAsync(companyId, mess.WednesdayBeg, mess.WednesdayEnd, 3, timeZone);
                await AddOneWorkingTimeAsync(companyId, mess.ThursdayBeg, mess.ThursdayEnd, 4, timeZone);
                await AddOneWorkingTimeAsync(companyId, mess.FridayBeg, mess.FridayEnd, 5, timeZone);
                await AddOneWorkingTimeAsync(companyId, mess.SaturdayBeg, mess.SaturdayEnd, 6, timeZone);
                await AddOneWorkingTimeAsync(companyId, mess.SundayBeg, mess.SundayEnd, 0, timeZone);
            }
        }

        private async Task AddOneWorkingTimeAsync(Guid companyId, DateTime? beg, DateTime? end, int day, TimeSpan timeShift)
        {
            WorkingTime time = new WorkingTime
            {
                CompanyId = companyId,
                Day = day,
                BegTime = beg is null ? (DateTime?)null : ((DateTime)beg).Add(timeShift),
                EndTime = end is null ? (DateTime?)null : ((DateTime)end).Add(timeShift)
            };
            await _repository.CreateAsync<WorkingTime>(time);
        }
        private async Task AddContentAndCampaign(Company company)
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
        private ApplicationUser GetUserIncludeCompany(string email)
        {
            var user = _repository.GetWithIncludeOne<ApplicationUser>(p => p.NormalizedEmail == email.ToUpper(), o => o.Company);
            if (user is null) throw new Exception("No such user");
            return user;
        }
        private ApplicationUser GetUserIncludeCompany(Guid userId, AccountAuthorization message)
        {
            var user = _repository.GetWithIncludeOne<ApplicationUser>(x => x.Id == userId && x.NormalizedEmail == message.UserName.ToUpper(), o => o.Company);
            if (user is null) throw new Exception("No such user");
            return user;
        }
        private void RemoveAccountWithSave(string email)
        {
            var user = _repository.GetAsQueryable<ApplicationUser>().FirstOrDefault(p => p.Email == email);
            var company = _repository.GetAsQueryable<Company>().FirstOrDefault(x => x.CompanyId == user.CompanyId);
            var users = _repository.GetWithInclude<ApplicationUser>(x => x.CompanyId == company.CompanyId, o => o.UserRoles).ToList();
            var tariff = _repository.GetAsQueryable<Tariff>().FirstOrDefault(x => x.CompanyId == company.CompanyId);
            
            var taskTransactions = _repository.GetAsQueryable<HBData.Models.Transaction>().Where(x => x.TariffId == tariff.TariffId).ToListAsync();
            taskTransactions.Wait();
            var transactions = taskTransactions.Result;
            var userRoles = users.SelectMany(x => x.UserRoles).ToList();
            var contents = _repository.GetAsQueryable<Content>().Where(x => x.CompanyId == company.CompanyId).ToList();
            var campaigns = _repository.GetWithInclude<Campaign>(x => x.CompanyId == company.CompanyId, p => p.CampaignContents).ToList();
            var campaignContents = campaigns.SelectMany(x => x.CampaignContents).ToList();
            var phrases = _repository.GetAsQueryable<PhraseCompany>().Where(x => x.CompanyId == company.CompanyId).ToList();
            var workingTimes = _repository.GetAsQueryable<WorkingTime>().Where(x => x.CompanyId == company.CompanyId).ToList();
            var salesStages = _repository.GetAsQueryable<SalesStagePhrase>().Where(x => x.CompanyId == company.CompanyId).ToList();



            if (phrases != null && phrases.Count() != 0)
                _repository.Delete<PhraseCompany>(phrases);
            if (campaignContents.Count() != 0)
                _repository.Delete<CampaignContent>(campaignContents); 
            if (campaigns.Count() != 0)
                _repository.Delete<Campaign>(campaigns);
            if (contents.Count() != 0)
                _repository.Delete<Content>(contents);
            
            _repository.Delete<ApplicationUserRole>(userRoles);
            _repository.Delete<HBData.Models.Transaction>(transactions);
            _repository.Delete<ApplicationUser>(users);
            _repository.Delete<Tariff>(tariff);
            _repository.Delete<WorkingTime>(workingTimes);
            _repository.Delete<SalesStagePhrase>(salesStages);

            _repository.Delete<Company>(company);
            _repository.Save();
        }


        public async Task<string> CreateEmptyToken() => _loginService.CreateTokenEmpty();
    }
}