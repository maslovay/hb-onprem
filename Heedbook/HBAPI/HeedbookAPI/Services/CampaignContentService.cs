using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HBData.Models;
using Microsoft.EntityFrameworkCore;
using UserOperations.Models;
using UserOperations.Utils;
using System.Reflection;
using System.Net;
using HBData.Repository;
using UserOperations.Controllers;
using HBLib.Utils;
using UserOperations.Utils.CommonOperations;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace UserOperations.Services
{
    public class CampaignContentService
    {
        private readonly LoginService _loginService;
        private readonly RequestFilters _requestFilters;
        private readonly IGenericRepository _repository;
        private readonly SftpClient _sftpClient;
        private readonly FileRefUtils _fileRef;

        private const string _containerName = "screenshots";

        public CampaignContentService(
            LoginService loginService,
            RequestFilters requestFilters,
            IGenericRepository repository,
            SftpClient sftpClient,
            FileRefUtils fileRef
            )
        {
            try
            {
                _loginService = loginService;
                _requestFilters = requestFilters;
                _repository = repository;
                _sftpClient = sftpClient;
                _fileRef = fileRef;
            }
            catch (Exception e)
            {
            }
        }

        public List<Campaign> CampaignGet( List<Guid> companyIds, List<Guid> corporationIds )
        {
            var role = _loginService.GetCurrentRoleName();
            var companyId = _loginService.GetCurrentCompanyId();
            _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);

            var statusInactiveId =  GetStatusId("Inactive");
            var campaigns = GetCampaignForCompanys(companyIds, statusInactiveId);

            var result = campaigns
                .Select(p =>
                    {
                        p.CampaignContents = p.CampaignContents.Where(x => p.CampaignContents != null
                                && p.CampaignContents.Count != 0
                                && x.StatusId != statusInactiveId)
                            .ToList();
                        return p;
                    })
                .ToList();
            return result;
        }

        public Campaign CampaignPost( CampaignPutPostModel model )
        {
            var companyId = _loginService.GetCurrentCompanyId();

            var activeStatus = GetStatusId("Active");
            Campaign campaign = model.Campaign;
            campaign.CompanyId = (Guid)companyId;
            campaign.CreationDate = DateTime.UtcNow;
            campaign.StatusId = activeStatus;
            campaign.CampaignContents = new List<CampaignContent>();
            Add<Campaign>(campaign);
            SaveChanges();
            foreach (var campCont in model.CampaignContents)
            {
                campCont.CampaignId = campaign.CampaignId;
                campCont.StatusId = activeStatus;
                Add<CampaignContent>(campCont);
            }
            SaveChanges();
            return campaign;
        }

        public Campaign CampaignPut( CampaignPutPostModel model )
        {
                var roleInToken = _loginService.GetCurrentRoleName();
                var companyIdInToken = _loginService.GetCurrentCompanyId();
                var corporationIdInToken = _loginService.GetCurrentCorporationId();

                Campaign modelCampaign = model.Campaign;

                var campaignEntity = GetCampaign(modelCampaign.CampaignId);
                if (campaignEntity == null) return null;
                _requestFilters.IsCompanyBelongToUser(corporationIdInToken, companyIdInToken, campaignEntity.CompanyId, roleInToken);

                foreach (var p in typeof(Campaign).GetProperties())
                {
                    if (p.GetValue(modelCampaign, null) != null && p.GetValue(modelCampaign, null).ToString() != Guid.Empty.ToString())
                        p.SetValue(campaignEntity, p.GetValue(modelCampaign, null), null);
                }

                var inactiveStatusId = GetStatusId("Inactive");
                var activeStatusId = GetStatusId("Active");
                var activeCampaignContents = campaignEntity.CampaignContents.Where(x => x.StatusId != inactiveStatusId).ToList();

                if (model.CampaignContents != null && model.CampaignContents.Count != 0)
                {
                    var modelCampaignContentIds = model.CampaignContents.Select(x => x.CampaignContentId);
                    activeCampaignContents.Where(p => !modelCampaignContentIds.Contains(p.CampaignContentId))
                        .Select(p => 
                            {
                                p.StatusId = inactiveStatusId;
                                return p;
                            }).ToList();
                    var activeCampaignContentsIds = activeCampaignContents.Select(x => x.CampaignContentId);
                    model.CampaignContents.Where(p => !activeCampaignContentsIds.Contains(p.CampaignContentId))
                        .Select(p =>
                            {
                                p.CampaignId = campaignEntity.CampaignId;
                                p.StatusId = activeStatusId;
                                Add<CampaignContent>(p);
                                return p;
                            }).ToList();
                }
                SaveChanges();
                campaignEntity.CampaignContents = campaignEntity.CampaignContents.Where(x => x.StatusId != inactiveStatusId).ToList();
                return campaignEntity;
        }
        public string CampaignDelete( Guid campaignId )
        {
            var roleInToken = _loginService.GetCurrentRoleName();
            var companyIdInToken = _loginService.GetCurrentCompanyId();
            var corporationIdInToken = _loginService.GetCurrentCorporationId();

            var campaign = GetCampaign(campaignId);
            _requestFilters.IsCompanyBelongToUser(corporationIdInToken, companyIdInToken, campaign.CompanyId, roleInToken);
            if (campaign != null)
            {
                var inactiveStatusId = GetStatusId("Inactive");
                var links = campaign.CampaignContents.ToList();
                foreach (var campaignContent in links)
                {
                    campaignContent.StatusId = inactiveStatusId;
                }
                campaign.StatusId = inactiveStatusId;
                SaveChanges();
                try
                {
                    RemoveRange<CampaignContent>(campaign.CampaignContents);
                    RemoveEntity<Campaign>(campaign);
                    SaveChanges();
                }
                catch
                {
                    return "Set inactive";
                }
                return "Deleted";
            }
            return "No such campaign";
        }

        public async Task<string> ContentGet( List<Guid> companyIds, List<Guid> corporationIds, bool inactive, bool screenshot)
        {
                var roleInToken = _loginService.GetCurrentRoleName();
                var companyIdInToken = _loginService.GetCurrentCompanyId();
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, roleInToken, companyIdInToken);
                if (screenshot == true)
                {
                    var contentsWithScreen = GetContentsByStatusIdWithUrls(inactive, companyIds);
                    return JsonConvert.SerializeObject(contentsWithScreen);
                }
                    var contents = GetContentsByStatusId(inactive, companyIds);
                    return JsonConvert.SerializeObject(contents);
        }

        public async Task<object> ContentPaginatedGet( List<Guid> companyIds, List<Guid> corporationIds,
                                 bool inactive, int limit = 10, int page = 0,
                                 string orderBy = "Name", string orderDirection = "desc")
        {
                var roleInToken = _loginService.GetCurrentRoleName();
                var companyIdInToken = _loginService.GetCurrentCompanyId();
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, roleInToken, companyIdInToken);

                var activeStatusId = GetStatusId("Active");
                List<Content> contents = GetContentsByStatusId(inactive, companyIds);
                if(contents.Count == 0) return contents;

                ////---PAGINATION---
                var pageCount = (int)Math.Ceiling((double)contents.Count() / limit);//---round to the bigger 

                Type contentType = contents.First().GetType();
                PropertyInfo prop = contentType.GetProperty(orderBy);
                
                if (orderDirection == "asc")
                {
                    var contentsList = contents.OrderBy(p => prop.GetValue(p)).Skip(page * limit).Take(limit).ToList();
                    return new { contentsList, pageCount, orderBy, limit, page };
                }
                else
                {
                    var contentsList = contents.OrderByDescending(p => prop.GetValue(p)).Skip(page * limit).Take(limit).ToList();
                    return new { contentsList, pageCount, orderBy, limit, page };
                }
        }

        public async Task<ContentWithScreenshotModel> ContentPost(IFormCollection formData)
        {
            var roleInToken = _loginService.GetCurrentRoleName();
            var companyIdInToken = _loginService.GetCurrentCompanyId();

            if (formData.Files.Count == 0) throw new NoDataException();
            var contentDataJson = formData.FirstOrDefault(x => x.Key == "data").Value.ToString();
            Content content = JsonConvert.DeserializeObject<Content>(contentDataJson);

            if (!content.IsTemplate) content.CompanyId = companyIdInToken; // only for not templates we create content for partiqular company/ Templates have no any compane relations
            content.CreationDate = DateTime.UtcNow;
            content.UpdateDate = DateTime.UtcNow;
            content.StatusId = GetStatusId("Active");
            Add<Content>(content);
            SaveChanges();

            FileInfo fileInfo = new FileInfo(formData.Files[0].FileName);
            string screenshot = content.ContentId + fileInfo.Extension;
            return new ContentWithScreenshotModel(content, _fileRef.GetFileLink(_containerName, screenshot, default));
        }

        public async Task<ContentWithScreenshotModel> ContentPut(IFormCollection formData)
        {
            var roleInToken = _loginService.GetCurrentRoleName();
            var companyIdInToken = _loginService.GetCurrentCompanyId();
            var corporationIdInToken = _loginService.GetCurrentCorporationId();

            var contentDataJson = formData.FirstOrDefault(x => x.Key == "data").Value.ToString();
            Content content = JsonConvert.DeserializeObject<Content>(contentDataJson);

            _requestFilters.IsCompanyBelongToUser(corporationIdInToken, companyIdInToken, content.CompanyId, roleInToken);

            var contentEntity = GetContent(content.ContentId);
            foreach (var p in typeof(Content).GetProperties())
            {
                if (p.GetValue(content, null) != null && p.GetValue(content, null).ToString() != Guid.Empty.ToString())
                    p.SetValue(contentEntity, p.GetValue(content, null), null);
            }
            contentEntity.UpdateDate = DateTime.UtcNow;
            await SaveChangesAsync();

            string screenshot = string.Empty;
            if (formData.Files.Count != 0)
            {
                await Task.Run(() => _sftpClient.DeleteFileIfExistsAsync($"{_containerName}/{content.ContentId}"));
                FileInfo fileInfo = new FileInfo(formData.Files[0].FileName);
                screenshot = content.ContentId + fileInfo.Extension;
                var memoryStream = formData.Files[0].OpenReadStream();
                await _sftpClient.UploadAsMemoryStreamAsync(memoryStream, $"{_containerName}/", screenshot, true);
            }
            return new ContentWithScreenshotModel(contentEntity, screenshot);
        }

        public async Task<string> ContentDelete( Guid contentId )
        {
            var roleInToken = _loginService.GetCurrentRoleName();
            var companyIdInToken = _loginService.GetCurrentCompanyId();
            var corporationIdInToken = _loginService.GetCurrentCorporationId();

            var content = GetContentWithIncludeCampaignContent(contentId);
            if (content == null) throw new NoFoundException("No such content");
            _requestFilters.IsCompanyBelongToUser(corporationIdInToken, companyIdInToken, content.CompanyId, roleInToken);

            var inactiveStatusId = GetStatusId("Inactive");
            var links = content.CampaignContents.ToList();
            if (links.Count() != 0)
            {
                foreach (var campaignContent in links)
                {
                    campaignContent.StatusId = inactiveStatusId;
                }
            }
            content.StatusId = inactiveStatusId;
            SaveChanges();
            try
            {
                RemoveRange<CampaignContent>(links);
                RemoveEntity(content);
                SaveChanges();
                await _sftpClient.DeleteFileIfExistsAsync($"{_containerName}/{content.ContentId+".png"}");
            }
            catch
            {
                return "Set inactive";
            }

            return "Removed";
        }

        public async Task<Dictionary<string, string>> GetResponseHeaders( string url)
        {
                var MyClient = WebRequest.Create(url) as HttpWebRequest;
                MyClient.Method = WebRequestMethods.Http.Get;
                MyClient.Headers.Add(HttpRequestHeader.ContentType, "application/json");
                var response = (await MyClient.GetResponseAsync()) as HttpWebResponse;
                var answer = new Dictionary<string, string>();
                for (int i = 0; i < response.Headers.Count; i++)
                    answer[response.Headers.GetKey(i)] = response.Headers.Get(i).ToString();
                return answer;
        }

        //---PRIVATE---
        private int GetStatusId(string statusName)
        {
            var statusId = _repository.GetAsQueryable<Status>()
                .FirstOrDefault(p => p.StatusName == statusName).StatusId;
            return statusId;
        }
        private List<Campaign> GetCampaignForCompanys(List<Guid> companyIds, int statusId)
        {
            var campaigns = _repository.GetAsQueryable<Campaign>()
                .Include(x => x.CampaignContents)
                .Where(x => companyIds.Contains(x.CompanyId)
                    && x.StatusId != statusId).ToList();
            return campaigns;
        }
        private Campaign GetCampaign(Guid campaignId)
        {
            var campaignEntity = _repository.GetAsQueryable<Campaign>()
                .Include(x => x.CampaignContents)
                .Where(p => p.CampaignId == campaignId)
                .FirstOrDefault();
            return campaignEntity;
        }
        private void Add<T>(T entity) where T : class
        {
            _repository.Create<T>(entity);
        }
        private void SaveChanges()
        {
            _repository.Save();
        }
        public void RemoveRange<T>(IEnumerable<T> list) where T : class
        {
            _repository.Delete<T>(list);
        }
        private void RemoveEntity<T>(T entity) where T : class
        {
            _repository.Delete<T>(entity);
        }
        private List<Content> GetContentsByStatusId(bool Inactive, List<Guid> companyIds)
        {
            var activeStatusId = GetStatusId("Active");
            if (Inactive == false)
            {
                return _repository.GetAsQueryable<Content>()
                   .Where(x => x.StatusId == activeStatusId
                       && (x.IsTemplate == true || companyIds.Contains((Guid)x.CompanyId)))
                   .ToList();
            }
            return _repository.GetAsQueryable<Content>()
                .Where(x => x.IsTemplate == true || companyIds.Contains((Guid)x.CompanyId))
                .ToList();
        }

        private List<ContentWithScreenshotModel> GetContentsByStatusIdWithUrls(bool Inactive, List<Guid> companyIds)
        {
            var activeStatusId = GetStatusId("Active");
            if (Inactive == false)
            {
                return  _repository.GetAsQueryable<Content>()
                .Where(x => x.StatusId == activeStatusId
                    && (x.IsTemplate == true || companyIds.Contains((Guid)x.CompanyId)))
                .ToList()
                .Select(x => new ContentWithScreenshotModel(x, _fileRef.GetFileLink(_containerName, x.ContentId.ToString() + ".png", default)))
                .ToList();
            }
            return _repository.GetAsQueryable<Content>()
                .Where(x => x.IsTemplate == true || companyIds.Contains((Guid)x.CompanyId))
                .ToList()
                .Select(x => new ContentWithScreenshotModel(x, _fileRef.GetFileLink(_containerName, x.ContentId.ToString() + ".png", default)))
                .ToList();
        }

        private Content GetContent(Guid contentId)
        {
            var contentEntity = _repository.GetAsQueryable<Content>()
                .Where(p => p.ContentId == contentId)
                .FirstOrDefault();
            return contentEntity;
        }
        private Content GetContentWithIncludeCampaignContent(Guid contentId)
        {
            var content = _repository.GetAsQueryable<Content>()
                .Include(x => x.CampaignContents)
                .Where(p => p.ContentId == contentId)
                .FirstOrDefault();
            return content;
        }
        private async Task SaveChangesAsync()
        {
            await _repository.SaveAsync();
        }
    }
}