using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using HBData.Models;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using UserOperations.CommonModels;
using UserOperations.Utils;
using System.Reflection;
using System.Net;
using HBData.Repository;
using UserOperations.Controllers;

namespace UserOperations.Services
{
    public class CampaignContentService
    {
        private readonly LoginService _loginService;
        private readonly RequestFilters _requestFilters;
        private readonly IGenericRepository _repository;

        public CampaignContentService(
            LoginService loginService,
            RequestFilters requestFilters,
            IGenericRepository repository
            )
        {
            try
            {
                _loginService = loginService;
                _requestFilters = requestFilters;
                _repository = repository;
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
            AddInBase<Campaign>(campaign);
            SaveChanges();
            foreach (var campCont in model.CampaignContents)
            {
                campCont.CampaignId = campaign.CampaignId;
                campCont.StatusId = activeStatus;
                AddInBase<CampaignContent>(campCont);
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
                                AddInBase<CampaignContent>(p);
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

        public async Task<List<Content>> ContentGet( List<Guid> companyIds, List<Guid> corporationIds, bool? inactive )
        {
                var roleInToken = _loginService.GetCurrentRoleName();
                var companyIdInToken = _loginService.GetCurrentCompanyId();
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, roleInToken, companyIdInToken);

                var activeStatusId = GetStatusId("Active");
                List<Content> contents;
                if (inactive == false)
                    contents = GetContentsWithActiveStatusId(activeStatusId, companyIds);
                else
                    contents = GetContentsWithTemplateIsTrue(companyIds);
                return contents;
        }

        public async Task<object> ContentPaginatedGet( List<Guid> companyIds, List<Guid> corporationIds,
                                 bool? inactive, int limit = 10, int page = 0,
                                 string orderBy = "Name", string orderDirection = "desc")
        {
                var roleInToken = _loginService.GetCurrentRoleName();
                var companyIdInToken = _loginService.GetCurrentCompanyId();
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, roleInToken, companyIdInToken);

                var activeStatusId = GetStatusId("Active");
                List<Content> contents;
                if(inactive == false)
                    contents = GetContentsWithActiveStatusId(activeStatusId, companyIds);
                else
                    contents = GetContentsWithTemplateIsTrue(companyIds);

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

        public async Task<Content> ContentPost( Content content )
        {
            var roleInToken = _loginService.GetCurrentRoleName();
            var companyIdInToken = _loginService.GetCurrentCompanyId();

            if (!content.IsTemplate) content.CompanyId = companyIdInToken; // only for not templates we create content for partiqular company/ Templates have no any compane relations
            content.CreationDate = DateTime.UtcNow;
            content.UpdateDate = DateTime.UtcNow;
            content.StatusId = GetStatusId("Active");
            AddInBase<Content>(content);
            SaveChanges();
            return content;
        }

        public async Task<Content> ContentPut( Content content )
        {
            var roleInToken = _loginService.GetCurrentRoleName();
            var companyIdInToken = _loginService.GetCurrentCompanyId();
            var corporationIdInToken = _loginService.GetCurrentCorporationId();

            _requestFilters.IsCompanyBelongToUser(corporationIdInToken, companyIdInToken, content.CompanyId, roleInToken);

            var contentEntity = GetContent(content.ContentId);
            foreach (var p in typeof(Content).GetProperties())
            {
                if (p.GetValue(content, null) != null && p.GetValue(content, null).ToString() != Guid.Empty.ToString())
                    p.SetValue(contentEntity, p.GetValue(content, null), null);
            }
            contentEntity.UpdateDate = DateTime.UtcNow;
            await SaveChangesAsync();
            return contentEntity;
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
        private void AddInBase<T>(T campaign) where T : class
        {
            _repository.Create<T>(campaign);
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
        private List<Content> GetContentsWithActiveStatusId(int activeStatusId, List<Guid> companyIds)
        {
            var contents = _repository.GetAsQueryable<Content>()
                .Where(x => x.StatusId == activeStatusId
                    && (x.IsTemplate == true || companyIds.Contains((Guid)x.CompanyId)))
                .ToList();
            return contents;
        }
        private List<Content> GetContentsWithTemplateIsTrue(List<Guid> companyIds)
        {
            var contents = _repository.GetAsQueryable<Content>()
                .Where(x => x.IsTemplate == true || companyIds.Contains((Guid)x.CompanyId))
                .ToList();
            return contents;
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