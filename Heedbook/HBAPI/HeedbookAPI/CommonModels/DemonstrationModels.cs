using System;
using System.Collections;
using System.Collections.Generic;
using HBData.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace UserOperations.CommonModels
{
    [SwaggerTag("Get content")]
    public class ContentReturnOnDeviceModel// --for swagger
    {
        public List<object> Campaigns;  
        public Hashtable htmlRaws;
        public List<string> blobMedia;
    }

     public class ContentWithId
    {
        // public ContentWithId()
        // {
        //     htmlId = Guid.NewGuid().ToString();
        // }
        public Content contentWithId;
        public string htmlId;
        public Guid campaignContentId;
    }

    public class ContentModel
    {
        public Guid Id {get; set;}
        public Guid CampaignContentId {get; set;}
        public string HTML {get; set;}
        public int Duration {get; set;}
        public string Type {get; set;}
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