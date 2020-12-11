using System.Collections.Generic;
using HBData.Models;

namespace ApiPerformance.Models
{
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