using System;
using System.Collections;
using System.Collections.Generic;
using HBData.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace UserOperations.Models
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

    public class ContentWithScreenshotModel
    {
        public Guid ContentId { set; get; }
        public Guid? CompanyId  { set; get; }
        public DateTime? CreationDate  { set; get; }
        public int Duration  { set; get; }
        public bool IsTemplate  { set; get; }
        public string JSONData  { set; get; }
        public string Name  { set; get; }
        public string RawHTML  { set; get; }
        public int? StatusId  { set; get; }
        public DateTime? UpdateDate  { set; get; }
        public string Url  { set; get; }
        public ContentWithScreenshotModel(Content content, string screenshot)
        {
            ContentId = content.ContentId;
            CompanyId = content.CompanyId;
            CreationDate = content.CreationDate;
            Duration = content.Duration;
            IsTemplate = content.IsTemplate;
            JSONData = content.JSONData;
            Name = content.Name;
            RawHTML = content.RawHTML;
            StatusId = content.StatusId;
            UpdateDate = content.UpdateDate;
            Url = screenshot;
        }
    }

    public class ContentReturnOnDeviceModel// --for swagger
    {
        public List<object> Campaigns;
        public Hashtable htmlRaws;
        public List<string> blobMedia;
    }

    public class ContentWithId
    {
        public Content contentWithId;
        public string htmlId;
        public Guid campaignContentId;
    }

    public class ContentModel
    {
        public Guid Id { get; set; }
        public Guid CampaignContentId { get; set; }
        public string HTML { get; set; }
        public int Duration { get; set; }
        public string Type { get; set; }
    }

    public class CampaignModel
    {
        public Guid Id;
        public int Gender;
        public int? BegAge;
        public int? EndAge;
        public List<ContentModel> Content;
        public bool IsSplashScreen;
    }
}