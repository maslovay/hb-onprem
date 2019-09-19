using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CloneFtpOnAzure;
using HBData;
using HBLib.Utils;
using Microsoft.AspNetCore.Mvc;

namespace LinkToBlobController.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class LinkController : ControllerBase
    {
        private CreateLinkToBlob _create;
        private StorageAccInfo _accInfo;

        public LinkController(CreateLinkToBlob create,
            StorageAccInfo accInfo)
        {
            _accInfo = accInfo;
            _create = create;
        }

        [HttpGet("GetBlobFile")]
        public List<string> GetBlobFile([FromQuery] string dialogId)
        {
            var key = _accInfo.AccKey;

            var dict = new Dictionary<string, string>()
            {
                {_accInfo.VideoName, dialogId + ".mkv"},
                {_accInfo.AvatarName, dialogId + ".jpg"},
                {_accInfo.AudioName, dialogId + ".wav"}
            };

            var token = new List<string>();
            foreach (var (k, v) in dict)
            {
                var uriPath = _accInfo.UriPath + k + "/" + v;
                token.Add(_create.CreateSasUri(uriPath, k, v));
            }
            return token;
        }
    }
}