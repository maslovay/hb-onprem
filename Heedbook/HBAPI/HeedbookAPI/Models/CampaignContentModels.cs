using System;
using System.Collections;
using System.Collections.Generic;
using HBData.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace UserOperations.CommonModels
{
    [SwaggerTag("Get content")]
    public class CampaignGetModel// --for swagger
    {
        public Guid CampaignId { get; set; }
        public String Name { get; set; }
        public Boolean IsSplash { get; set; }
        public Int32 GenderId { get; set; }
        public Int32? BegAge { get; set; }

        public Int32? EndAge { get; set; }
        public DateTime? BegDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? CreationDate { get; set; }
        public Guid CompanyId { get; set; }
        public Int32? StatusId { get; set; }

        public ICollection<CampaignContent> CampaignContents { get; set; }
    }  

    public class CampaignPutPostModel
    {
        public CampaignPutPostModel(Campaign cmp, List<CampaignContent> campaignContents)
        {
            Campaign = cmp;
            CampaignContents = campaignContents;
        }
        public Campaign Campaign { get; set; }
        public List<CampaignContent> CampaignContents { get; set; }
    }
}