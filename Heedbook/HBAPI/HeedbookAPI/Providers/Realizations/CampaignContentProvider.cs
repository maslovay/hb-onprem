using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Repository;
using Microsoft.EntityFrameworkCore;

namespace UserOperations.Providers
{
    public class CampaignContentProvider : ICampaignContentProvider
    {
        private readonly IGenericRepository _repository;
        public CampaignContentProvider(IGenericRepository repository)
        {
            _repository = repository;
        }
        public int GetStatusId(string statusName)
        {
            var statusId = _repository.GetAsQueryable<Status>()
                .FirstOrDefault(p => p.StatusName == statusName).StatusId;
            return statusId;
        }
        public List<Campaign> GetCampaignForCompanys(List<Guid> companyIds, int statusId)
        {
            var campaigns = _repository.GetAsQueryable<Campaign>()
                .Include(x => x.CampaignContents)
                .Where(x => companyIds.Contains(x.CompanyId) 
                    && x.StatusId != statusId).ToList();
            return campaigns;
        }
        public Campaign GetCampaign(Guid campaignId)
        {
            var campaignEntity = _repository.GetAsQueryable<Campaign>()
                .Include(x => x.CampaignContents)
                .Where(p => p.CampaignId == campaignId)
                .FirstOrDefault();
            return campaignEntity;
        }
        public void AddInBase<T>(T campaign) where T : class
        {
            _repository.Create<T>(campaign);
        }
        public void SaveChanges()
        {
            _repository.Save();
        }
        public void RemoveRange<T>(IEnumerable<T> list) where T : class
        {
            _repository.Delete<T>(list);
        }
        public void RemoveEntity<T>(T entity) where T : class
        {
            _repository.Delete<T>(entity);
        }
        public List<Content> GetContentsWithActiveStatusId(int activeStatusId, List<Guid> companyIds)
        {
            var contents = _repository.GetAsQueryable<Content>()
                .Where(x => x.StatusId == activeStatusId 
                    && (x.IsTemplate == true || companyIds.Contains((Guid)x.CompanyId)))
                .ToList();
            return contents;
        }
        public List<Content> GetContentsWithTemplateIsTrue(List<Guid> companyIds)
        {
            var contents = _repository.GetAsQueryable<Content>()
                .Where(x => x.IsTemplate == true || companyIds.Contains((Guid)x.CompanyId))
                .ToList();
            return contents;
        }
        public Content GetContent(Guid contentId)
        {
            var contentEntity = _repository.GetAsQueryable<Content>()
                .Where(p => p.ContentId == contentId)
                .FirstOrDefault();
            return contentEntity;
        }
        public Content GetContentWithIncludeCampaignContent(Guid contentId)
        {
            var content = _repository.GetAsQueryable<Content>()
                .Include(x => x.CampaignContents)
                .Where(p => p.ContentId == contentId)
                .FirstOrDefault();
            return content;
        }
        public async Task SaveChangesAsync()
        {
            await _repository.SaveAsync();
        }
    }
}