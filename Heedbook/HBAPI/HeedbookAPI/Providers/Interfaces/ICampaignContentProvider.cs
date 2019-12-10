using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HBData.Models;

namespace UserOperations.Providers
{
    public interface ICampaignContentProvider
    {
        int GetStatusId(string statusName);
        List<Campaign> GetCampaignForCompanys(List<Guid> companyIds, int statusId);
        Campaign GetCampaign(Guid campaignId);
        void AddInBase<T>(T campaign) where T : class;
        void SaveChanges();
        void RemoveRange<T>(IEnumerable<T> list) where T : class;
        void RemoveEntity<T>(T entity) where T : class;
        List<Content> GetContentsWithActiveStatusId(int activeStatusId, List<Guid> companyIds);
        List<Content> GetContentsWithTemplateIsTrue(List<Guid> companyIds);
        Task SaveChangesAsync();
        Content GetContent(Guid contentId);
        Content GetContentWithIncludeCampaignContent(Guid contentId);
    }
}