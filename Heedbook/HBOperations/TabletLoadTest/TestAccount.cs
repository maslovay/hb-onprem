using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Repository;
using Newtonsoft.Json;
using UserOperations.Services.Interfaces;

namespace TabletLoadTest
{
    public class TestAccount
    {
        private readonly IGenericRepository _repository;
        private Guid deviceId;
        private Guid companyId;
        private Guid applicationUserId;
        private Guid industryId = Guid.Parse("d0e72913-d987-4a1a-86d7-c2eae1b2101d");
        public Guid campaignContentId;
        public Device _device;
        public Company _company;
        public ApplicationUser _applicationUser;
        private Campaign _campaign;
        private Content _content;
        private CampaignContent _campaignContent;
        public string JWTtoken;
        public readonly bool IsExtended;
        public TestAccount(IGenericRepository repository, bool isExtended)
        {
            _repository = repository;
            deviceId = Guid.NewGuid();
            companyId = Guid.NewGuid();
            applicationUserId = Guid.NewGuid();
            campaignContentId = Guid.NewGuid();
            IsExtended = isExtended;
        }
        public async Task PrepareTestAccount()
        {
            try
            {
                _device = new Device
                {
                    DeviceId = deviceId,
                    DeviceTypeId = Guid.Parse("b29a6c53-fbdf-4dba-930b-95a267e4e313"),
                    CompanyId = companyId,
                    Code = RandomDeviceCode(),
                    StatusId = 3,
                    Name = "TabletLoadTestAccount",
                };
                _company = new Company
                {
                    CompanyId = companyId,
                    CompanyName = "TabletLoadTestCompany",
                    IsExtended = IsExtended,
                    CompanyIndustryId = industryId,
                    CreationDate = DateTime.Now,
                    LanguageId = 2,
                    StatusId = 2
                };
                var employeeRoleId = Guid.Parse("67b8e535-d460-49c0-97a4-f6e4de46e525");
                _applicationUser = new ApplicationUser
                {
                    Id = applicationUserId,
                    FullName = "TabletLoadTestUser",
                    CreationDate = DateTime.Now,
                    CompanyId = companyId,
                    StatusId = 3,
                    Email = $"{applicationUserId}@TestHeedbook.com",
                    NormalizedEmail = $"{applicationUserId}@TestHeedbook.com".ToUpper(),
                    PasswordHash = $"WUgnqUuL/fNGhlRBwdEPvt7/W4VAeIJEdeukLBWeXK4=",
                    UserRoles = new List<ApplicationUserRole>{new ApplicationUserRole{RoleId = employeeRoleId}}
                };
                _campaign = new Campaign
                {
                    CampaignId = Guid.NewGuid(),
                    CompanyId = _company.CompanyId,
                    Name = "TestCampaign",
                    IsSplash = true,
                    GenderId = 2,
                    BegAge = 10,
                    EndAge = 60,
                    BegDate = DateTime.Now.AddDays(-5),
                    EndDate = DateTime.Now.AddDays(5),
                    CreationDate = DateTime.Now.AddDays(-6),
                    StatusId = 3
                };
                _content = new Content
                {
                    ContentId = Guid.NewGuid(),
                    RawHTML = "<body>body</body>",
                    Name = "TestContent",
                    Duration = 30,
                    CompanyId = companyId,
                    IsTemplate = true,
                    CreationDate = DateTime.Now.AddDays(-4),
                    UpdateDate = DateTime.Now.AddDays(-3),
                    StatusId = 3                
                };
                _campaignContent = new CampaignContent
                {
                    CampaignContentId = campaignContentId,
                    SequenceNumber = 1,
                    ContentId = _content.ContentId,
                    CampaignId = _campaign.CampaignId,
                    StatusId = 3               
                };
                // System.Console.WriteLine($"_device: {JsonConvert.SerializeObject(_device)}");
                // System.Console.WriteLine($"_company: {JsonConvert.SerializeObject(_company)}");
                // System.Console.WriteLine($"_applicationUser: {JsonConvert.SerializeObject(_applicationUser)}");
                // System.Console.WriteLine($"_campaign: {JsonConvert.SerializeObject(_campaign)}");
                // System.Console.WriteLine($"_content: {JsonConvert.SerializeObject(_content)}");
                // System.Console.WriteLine($"_campaignContent: {JsonConvert.SerializeObject(_campaignContent)}");
                _repository.Create<Device>(_device);
                _repository.Create<Company>(_company);
                _repository.Create<ApplicationUser>(_applicationUser);
                _repository.Create<Campaign>(_campaign);
                _repository.Create<Content>(_content);
                _repository.Create<CampaignContent>(_campaignContent);
                await _repository.SaveAsync();
            }
            catch(Exception e)
            {
                System.Console.WriteLine(e);
            }
        }
        private string RandomDeviceCode()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var stringChars = new char[6];
            var random = new Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            return new String(stringChars);
        }
        public async Task DeleteTestAccountData()
        {
            _repository.Delete<Device>(_device);
            _repository.Delete<Company>(_company);
            _repository.Delete<ApplicationUser>(_applicationUser);
            _repository.Delete<Campaign>(_campaign);
            _repository.Delete<Content>(_content);
            _repository.Delete<CampaignContent>(_campaignContent);
            _repository.Delete<SlideShowSession>(p => p.CampaignContentId == _campaignContent.CampaignContentId);
            _repository.Delete<CampaignContentAnswer>(p => p.CampaignContentId == _campaignContent.CampaignContentId);
            _repository.Delete<Alert>(p => p.DeviceId == _device.DeviceId);
            var fileFrameIds = _repository.GetAsQueryable<FileFrame>()
                .Where(p => p.DeviceId == _device.DeviceId)
                .Select(p => p.FileFrameId)
                .ToList();
            _repository.Delete<FileFrame>(p => fileFrameIds.Contains(p.FileFrameId));
            _repository.Delete<FrameAttribute>(p => fileFrameIds.Contains(p.FileFrameId));
            _repository.Delete<FrameEmotion>(p => fileFrameIds.Contains(p.FileFrameId));
            _repository.Delete<FileVideo>(p => p.DeviceId == _device.DeviceId);
            await _repository.SaveAsync();
            System.Console.WriteLine($"TestAccountDataDeleted");            
        }
    }
}